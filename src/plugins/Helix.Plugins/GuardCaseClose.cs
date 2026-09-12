using System;
using System.ServiceModel;
using Microsoft.Xrm.Sdk;

namespace Helix.Plugins
{
    /// <summary>
    /// FR-CM-05, BR-03.
    /// Step: Update / hlx_case / PreValidation (10) / Synchronous / rank 10,
    /// filtering attributes "statecode,statuscode", Pre Image "pre" over
    /// statecode, statuscode, hlx_resolutionsummary.
    ///
    /// PreValidation because cancelling before the transaction opens is cheap and
    /// cancelling inside it is a rollback [handle-exceptions]. There is no depth guard
    /// and no import bypass: BR-03 says the rule holds for every caller, and a rule
    /// with a bypass is a suggestion. Phase 2's INC-004 is the bill for that decision.
    ///
    /// Plain-language version: this is a Dataverse plug-in. It is registered to run
    /// automatically, BEFORE the platform even opens a database transaction, whenever
    /// an existing "Case" (hlx_case) row is about to be updated - but only when the
    /// update touches statecode or statuscode (that's what "filtering attributes" means:
    /// an update that leaves both columns untouched never triggers this step at all).
    /// Its whole job is a single yes/no check: "is this update trying to move the case
    /// into the Closed status without a resolution summary?" If so, it throws and the
    /// update never happens. Otherwise it does nothing and gets out of the way.
    /// </summary>
    public sealed class GuardCaseClose : IPlugin
    {
        // The name given to the "Pre Image" in the step's registration. A Pre Image is a
        // snapshot of the row's columns as they stood immediately BEFORE this update -
        // Dataverse takes it for you and hands it back under whatever alias you chose when
        // registering the step. We need it because an Update's Target only ever carries the
        // columns the caller actually changed; anything the caller left untouched (for
        // example hlx_resolutionsummary, if only statuscode was submitted) simply isn't in
        // Target at all, so the "old" values have to come from somewhere else - this image.
        private const string PreImageAlias = "pre";

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
            // contains the actual row data being saved, plus any registered images.
            var context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));

            // First trace line: a breadcrumb of exactly which operation invoked us. Depth
            // counts how many plug-ins deep we are in a chain of triggered operations (1 =
            // the direct caller, 2 = a plug-in that itself caused this update, and so on) -
            // useful for spotting runaway recursion. IsInTransaction is false here on
            // purpose: PreValidation runs before Dataverse has even opened the database
            // transaction for this update, which is exactly why a throw at this point is
            // cheap - there is nothing to roll back yet.
            tracing.Trace("GuardCaseClose: message={0} stage={1} depth={2} inTransaction={3}",
                context.MessageName, context.Stage, context.Depth, context.IsInTransaction);

            // "Target" is the SDK's name for the row being created/updated. It's optional
            // in the InputParameters bag in general (not every message has one), so this
            // guards against a misconfigured registration where it's simply missing.
            if (!context.InputParameters.Contains("Target") || !(context.InputParameters["Target"] is Entity))
            {
                tracing.Trace("GuardCaseClose: no Target entity; nothing to guard.");
                return;
            }

            // Entity is the SDK's generic "row" type - a loosely-typed bag of column
            // name -> value. Because the step's filtering attributes are statecode and
            // statuscode, `target` here only ever contains the columns the caller actually
            // submitted, which is why the guard below cannot assume every column is present.
            var target = (Entity)context.InputParameters["Target"];

            // Defensive check: this plug-in step should only ever be registered on
            // hlx_case. If it somehow runs against another table (bad registration,
            // copy/paste mistake in the step config), bail out rather than crash.
            if (!string.Equals(target.LogicalName, Names.Case, StringComparison.Ordinal))
            {
                tracing.Trace("GuardCaseClose: received '{0}'; the step is misregistered.", target.LogicalName);
                return;
            }

            // context.PreEntityImages is the collection of pre-images the step registration
            // asked Dataverse to attach, keyed by the alias given at registration time
            // (PreImageAlias, "pre"). Checking Contains first avoids a KeyNotFoundException
            // if the registration is ever edited and the image alias renamed or removed.
            if (!context.PreEntityImages.Contains(PreImageAlias))
            {
                // Fail loudly rather than guess. A missing image means the registration is
                // incomplete, and a guard that silently passes is worse than no guard at all.
                tracing.Trace("GuardCaseClose: Pre Image '{0}' is not registered.", PreImageAlias);
                throw new InvalidPluginExecutionException(
                    "This case cannot be updated because a required configuration is missing. "
                    + "Quote GuardCaseClose to your administrator.");
            }

            // `pre` is an Entity just like `target`, except it represents the row as it
            // existed right before this update - the "old" values to compare against.
            var pre = context.PreEntityImages[PreImageAlias];

            // Everything that can fail lives in this try block, so every failure path can be
            // funneled through the catch clauses below into a friendly error message.
            try
            {
                // OptionSetValue is the SDK's wrapper for a choice/option-set column - it
                // carries the underlying integer plus (sometimes) label metadata. The old
                // status always comes from the pre-image, because Target may not carry
                // statuscode at all if this update only touched statecode.
                var oldStatus = pre.GetAttributeValue<OptionSetValue>(Names.StatusCode);

                // The new status is whatever the caller is trying to set. If the caller
                // didn't submit statuscode in this particular update, it isn't changing, so
                // the old value carries forward unchanged - target.Contains(...) is how you
                // ask "is this column present in the bag at all", distinct from "is its
                // value null".
                var newStatus = target.Contains(Names.StatusCode)
                    ? target.GetAttributeValue<OptionSetValue>(Names.StatusCode)
                    : oldStatus;

                // OptionSetValue can itself be null (column genuinely unset), so both sides
                // are normalised to a plain int for comparison; -1 is not a real status
                // value in this model, so it safely means "no status".
                var oldValue = oldStatus == null ? -1 : oldStatus.Value;
                var newValue = newStatus == null ? -1 : newStatus.Value;

                tracing.Trace("GuardCaseClose: statuscode {0} -> {1} (Closed is {2}).",
                    oldValue, newValue, HelixOptions.CaseStatusClosed);

                if (newValue != HelixOptions.CaseStatusClosed || oldValue == newValue)
                {
                    // Not a transition into Closed. Includes an already-closed case being edited.
                    return;
                }

                // Same "present in Target, else fall back to the pre-image" pattern as the
                // status check above: the resolution summary might be part of this same
                // update, or it might already be sitting on the row from an earlier save.
                var summary = target.Contains(Names.CaseResolutionSummary)
                    ? target.GetAttributeValue<string>(Names.CaseResolutionSummary)
                    : pre.GetAttributeValue<string>(Names.CaseResolutionSummary);

                if (string.IsNullOrWhiteSpace(summary))
                {
                    // InvalidPluginExecutionException is special: Dataverse shows its Message
                    // text directly to the end user (e.g. on the case form) and cancels the
                    // operation - because this is PreValidation, that cancellation happens
                    // before any database transaction was even opened.
                    tracing.Trace("GuardCaseClose: blocking the close - no resolution summary.");
                    throw new InvalidPluginExecutionException(
                        "This case cannot be closed until a resolution summary has been entered. "
                        + "Add a resolution summary, save, and then close the case.");
                }

                tracing.Trace("GuardCaseClose: close permitted; resolution summary is {0} characters.", summary.Length);
            }
            // Three catch clauses, ordered from most-specific/expected to least, each
            // deciding what the end user ultimately sees:
            catch (InvalidPluginExecutionException)
            {
                throw; // already carries a sentence written for a case worker; do not re-wrap it
            }
            catch (FaultException<OrganizationServiceFault> fault)
            {
                // A FaultException here means Dataverse itself rejected part of this
                // operation (e.g. a transient platform error, throttling, or a permissions
                // problem). The raw fault is traced for diagnosis, but the user gets a
                // generic, non-technical message plus a name to quote to support.
                tracing.Trace("GuardCaseClose: platform fault: {0}", fault.ToString());
                throw new InvalidPluginExecutionException(
                    "The case could not be closed. Please try again. "
                    + "If this keeps happening, quote GuardCaseClose to your administrator.", fault);
            }
            catch (Exception ex)
            {
                // Catch-all safety net for anything not anticipated above (a bug in this
                // plug-in, an unexpected null, etc.). Same pattern: trace the real exception
                // for developers, surface a safe message to the user.
                tracing.Trace("GuardCaseClose: unexpected error: {0}", ex.ToString());
                throw new InvalidPluginExecutionException(
                    "An unexpected error occurred while closing the case. "
                    + "Quote GuardCaseClose to your administrator.", ex);
            }
        }
    }
}