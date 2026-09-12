using System;
using System.ServiceModel;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace Helix.Plugins
{
    /// <summary>
    /// NFR-CMP-01, BR-02, BR-04.
    /// Step: Create / hlx_case / PreOperation (20) / Synchronous / rank 20.
    /// Raises hlx_priority when the linked contact is flagged vulnerable. Deterministic,
    /// in-transaction, and independent of any external service - which is the whole point:
    /// a regulated fairness rule must not depend on Azure AI Language being reachable.
    /// The triage flow (Stage 8) owns status routing and must never write this column.
    ///
    /// Plain-language version: this is a Dataverse plug-in. It is registered to run
    /// automatically, inside the platform's own transaction, immediately BEFORE a new
    /// "Case" (hlx_case) row is written to the database ("PreOperation" on "Create"),
    /// right after SetCaseReference (rank 10) has already run at rank 20. Its whole job
    /// is to look at the case's linked contact and, if that contact is flagged
    /// vulnerable, raise hlx_priority to High - never higher, never lower than an
    /// existing Critical. Because it runs PreOperation and Synchronous, any change it
    /// makes to the "target" Entity object is included in the same database write -
    /// there is no second save.
    /// </summary>
    public sealed class SetVulnerablePriority : IPlugin
    {
        // Dataverse calls this method for every step this plug-in is registered against.
        // IServiceProvider is how the platform hands the plug-in its context, logging,
        // and a client for talking back to Dataverse - it is not related to your own DI setup.
        public void Execute(IServiceProvider serviceProvider)
        {
            // ITracingService.Trace(...) writes to the plug-in trace log (visible in the
            // Power Platform admin tools). It is the only "logging" available inside a
            // plug-in - there's no console or debugger attached in production.
            var tracing = (ITracingService)serviceProvider.GetService(typeof(ITracingService));

            // IPluginExecutionContext describes the operation currently in flight: which
            // message (Create/Update/...), which table, which pipeline stage, who
            // triggered it, and - most importantly here - the InputParameters bag that
            // contains the actual row data being saved.
            var context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));

            // The factory builds an IOrganizationService - the SDK client used to read/write
            // other rows in Dataverse (Retrieve, RetrieveMultiple, etc.). Passing
            // context.UserId means the calls run impersonating whoever triggered this
            // operation, so security/permissions still apply as expected.
            var factory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
            var service = factory.CreateOrganizationService(context.UserId);

            // First trace line: a breadcrumb of exactly which operation invoked us, useful
            // when reading the trace log after the fact or when a step fires unexpectedly.
            tracing.Trace("SetVulnerablePriority: message={0} stage={1} depth={2}",
                context.MessageName, context.Stage, context.Depth);

            // "Target" is the SDK's name for the row being created/updated. It's optional
            // in the InputParameters bag in general (not every message has one), so this
            // guards against a misconfigured registration where it's simply missing.
            if (!context.InputParameters.Contains("Target") || !(context.InputParameters["Target"] is Entity))
            {
                tracing.Trace("SetVulnerablePriority: no Target entity; nothing to do.");
                return;
            }

            // Entity is the SDK's generic "row" type - a loosely-typed bag of column
            // name -> value. Because this runs PreOperation, `target` is the exact object
            // the platform is about to persist; edits made to it below flow into the write.
            var target = (Entity)context.InputParameters["Target"];

            // Defensive check: this plug-in step should only ever be registered on
            // hlx_case. If it somehow runs against another table (bad registration,
            // copy/paste mistake in the step config), bail out rather than crash.
            if (!string.Equals(target.LogicalName, Names.Case, StringComparison.Ordinal))
            {
                tracing.Trace("SetVulnerablePriority: received '{0}'; the step is misregistered.", target.LogicalName);
                return;
            }

            // Everything that can fail lives in this try block, so every failure path can be
            // funneled through the catch clauses below into a friendly error message.
            try
            {
                // A case must reference a contact (a lookup column) before we can check
                // vulnerability. GetAttributeValue<T> returns null/default if the column
                // isn't set on this row, so a null check here is how we detect "not chosen".
                var contactRef = target.GetAttributeValue<EntityReference>(Names.CaseContact);
                if (contactRef == null)
                {
                    // A case with no contact is legitimate - UC-08's field incidents often have none.
                    // There is no vulnerability to read, so there is nothing to decide.
                    tracing.Trace("SetVulnerablePriority: no contact on the case; leaving priority untouched.");
                    return;
                }

                // One point retrieve, one column. Never RetrieveMultiple where Retrieve will do,
                // and never ColumnSet(true) inside a synchronous transaction.
                // service.Retrieve(table, id, columns) fetches exactly one row by its
                // primary key, and the ColumnSet restricts which columns come back - here
                // just hlx_isvulnerable, because that's all this code needs from the contact.
                var contact = service.Retrieve(Names.Contact, contactRef.Id,
                    new ColumnSet(Names.ContactIsVulnerable));
                var isVulnerable = contact.GetAttributeValue<bool>(Names.ContactIsVulnerable);

                tracing.Trace("SetVulnerablePriority: contact={0} isVulnerable={1}", contactRef.Id, isVulnerable);

                // Not vulnerable - the fairness rule has nothing to do, and any priority the
                // caller already set on `target` is left exactly as they chose it.
                if (!isVulnerable)
                {
                    return;
                }

                // Fairness rule only ever raises priority, never lowers it: if the case is
                // already Critical, High would be a downgrade, so we leave it untouched.
                var current = target.GetAttributeValue<OptionSetValue>(Names.CasePriority);
                if (current != null && current.Value == HelixOptions.PriorityCritical)
                {
                    tracing.Trace("SetVulnerablePriority: already Critical; a fairness rule never lowers a priority.");
                    return;
                }

                // The one column value this plug-in sets. Because this is PreOperation,
                // setting it here means it's included in the same INSERT the platform is
                // about to perform - there's no follow-up update needed.
                target[Names.CasePriority] = new OptionSetValue(HelixOptions.PriorityHigh);
                tracing.Trace("SetVulnerablePriority: raised priority to High ({0}) for a vulnerable contact.",
                    HelixOptions.PriorityHigh);
            }
            // Three catch clauses, ordered from most-specific/expected to least, each
            // deciding what the end user ultimately sees:
            catch (InvalidPluginExecutionException)
            {
                throw; // already carries a sentence written for a case worker; do not re-wrap it
            }
            catch (FaultException<OrganizationServiceFault> fault)
            {
                // A FaultException here means Dataverse itself rejected our Retrieve call
                // (e.g. a transient platform error, throttling, or a permissions problem).
                // The raw fault is traced for diagnosis, but the user gets a generic,
                // non-technical message plus a name to quote to support.
                tracing.Trace("SetVulnerablePriority: platform fault: {0}", fault.ToString());
                throw new InvalidPluginExecutionException(
                    "The case could not be prioritised. Please try again. "
                    + "If this keeps happening, quote SetVulnerablePriority to your administrator.", fault);
            }
            catch (Exception ex)
            {
                // Catch-all safety net for anything not anticipated above (a bug in this
                // plug-in, an unexpected null, etc.). Same pattern: trace the real exception
                // for developers, surface a safe message to the user.
                tracing.Trace("SetVulnerablePriority: unexpected error: {0}", ex.ToString());
                throw new InvalidPluginExecutionException(
                    "An unexpected error occurred while prioritising the case. "
                    + "Quote SetVulnerablePriority to your administrator.", ex);
            }
        }
    }
}