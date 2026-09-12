using System;
using System.Globalization;
using System.ServiceModel;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace Helix.Plugins
{
    /// <summary>
    /// FR-CM-02, FR-CM-03, BR-01.
    /// Step: Create / hlx_case / PreOperation (20) / Synchronous / rank 10.
    /// Resolves the division from the case type, stamps hlx_division, and writes a
    /// division-prefixed reference into hlx_casereference before commit. The reference
    /// is the table's primary name column and is guarded by the hlx_ak_casereference
    /// alternate key, so a duplicate here is a failed create, not a bad row.
    ///
    /// Plain-language version: this is a Dataverse plug-in. It is registered to run
    /// automatically, inside the platform's own transaction, immediately BEFORE a new
    /// "Case" (hlx_case) row is written to the database ("PreOperation" on "Create").
    /// Its whole job is to compute two column values on that not-yet-saved row:
    ///   1. hlx_division      - copied over from the case's case type
    ///   2. hlx_casereference - a human-readable code like "OPS-2026-000042"
    /// Because it runs PreOperation and Synchronous, any change it makes to the "target"
    /// Entity object is included in the same database write - there is no second save.
    /// </summary>
    public sealed class SetCaseReference : IPlugin
    {
        // Matches the hlx_casereference column's max length in the data model. If the
        // computed reference would not fit, we fail loudly instead of silently truncating.
        private const int MaxReferenceLength = 20;

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
            tracing.Trace("SetCaseReference: message={0} table={1} stage={2} mode={3} depth={4} initiating={5}",
                context.MessageName, context.PrimaryEntityName, context.Stage,
                context.Mode, context.Depth, context.InitiatingUserId);

            // "Target" is the SDK's name for the row being created/updated. It's optional
            // in the InputParameters bag in general (not every message has one), so this
            // guards against a misconfigured registration where it's simply missing.
            if (!context.InputParameters.Contains("Target") || !(context.InputParameters["Target"] is Entity))
            {
                tracing.Trace("SetCaseReference: no Target entity in InputParameters; nothing to do.");
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
                tracing.Trace("SetCaseReference: registered against '{0}' but received '{1}'. The step is misregistered.",
                    Names.Case, target.LogicalName);
                return;
            }

            // If a caller (a form, a Flow, an import) already set hlx_casereference, that
            // value is intentionally ignored - BR-01 says the reference is always
            // system-generated, never user-supplied. We just log that we're overriding it.
            var supplied = target.GetAttributeValue<string>(Names.CaseReference);
            if (!string.IsNullOrWhiteSpace(supplied))
            {
                tracing.Trace("SetCaseReference: caller supplied '{0}'; discarding it. BR-01 has no opt-out.", supplied);
            }

            // Everything that can fail lives in this try block, so every failure path can be
            // funneled through the catch clauses below into a friendly error message.
            try
            {
                // A case must reference a case type (a lookup column) before we can figure
                // out its division. GetAttributeValue<T> returns null/default if the column
                // isn't set on this row, so a null check here is how we detect "not chosen".
                var caseTypeRef = target.GetAttributeValue<EntityReference>(Names.CaseCaseType);
                if (caseTypeRef == null)
                {
                    // InvalidPluginExecutionException is special: Dataverse shows its Message
                    // text directly to the end user (e.g. on the case creation form) and
                    // rolls back the whole transaction - nothing gets saved.
                    throw new InvalidPluginExecutionException(
                        "A case must have a case type before it can be saved. Choose a case type and try again.");
                }

                // Two point retrieves, two columns. Never RetrieveMultiple where Retrieve will do,
                // and never ColumnSet(true) inside a synchronous transaction.
                // service.Retrieve(table, id, columns) fetches exactly one row by its
                // primary key, and the ColumnSet restricts which columns come back - here
                // just hlx_division, because that's all this code needs from the case type.
                var caseTypeRow = service.Retrieve(Names.CaseType, caseTypeRef.Id,
                    new ColumnSet(Names.CaseTypeDivision));
                var divisionRef = caseTypeRow.GetAttributeValue<EntityReference>(Names.CaseTypeDivision);
                if (divisionRef == null)
                {
                    // Data-integrity guard: the case type table SHOULD always have a division
                    // set (it's marked required in the model), but plug-in code can't trust
                    // that blindly, so it re-checks and fails with a clear message if not.
                    throw new InvalidPluginExecutionException(
                        "The selected case type is not linked to a division, so a case reference cannot be generated. "
                        + "Ask your administrator to check the case type configuration.");
                }

                // Second point retrieve: now go from the division record to its short code
                // (e.g. "OPS"), which becomes the prefix of the human-readable reference.
                var divisionRow = service.Retrieve(Names.Division, divisionRef.Id,
                    new ColumnSet(Names.DivisionCode));
                var code = divisionRow.GetAttributeValue<string>(Names.DivisionCode);
                if (string.IsNullOrWhiteSpace(code))
                {
                    throw new InvalidPluginExecutionException(
                        "The division for this case type has no division code, so a case reference cannot be generated. "
                        + "Ask your administrator to check the division configuration.");
                }

                // hlx_division has exactly one writer and this is it. Stamping it here means every
                // downstream reader - the flow, the app, the security model - sees it immediately.
                // This is the first of the two column values this plug-in sets: it writes
                // straight into `target`, the same Entity object Dataverse is about to save.
                target[Names.CaseDivision] = new EntityReference(Names.Division, divisionRef.Id);

                // Build the reference prefix, e.g. "OPS-2026-". Division code is uppercased
                // and trimmed defensively (in case anyone typed it in lowercase or with
                // stray whitespace), and the year comes from UTC "now" so the reference
                // resets its numbering each calendar year.
                var prefix = string.Format(CultureInfo.InvariantCulture, "{0}-{1}-",
                    code.Trim().ToUpperInvariant(),
                    DateTime.UtcNow.Year.ToString(CultureInfo.InvariantCulture));

                // Ask the helper below for the next number in this prefix's sequence, then
                // pad it to 6 digits (e.g. 42 -> "000042") so references sort and align
                // consistently. Final shape: "OPS-2026-000042".
                var sequence = NextSequence(service, tracing, prefix);
                var reference = prefix + sequence.ToString("000000", CultureInfo.InvariantCulture);

                // Belt-and-braces check against the column's actual max length (20 chars).
                // A very long division code, or a far-future/absurd year, could in theory
                // push past that limit - better to fail clearly here than let the platform
                // reject the save with a generic "string too long" error.
                if (reference.Length > MaxReferenceLength)
                {
                    throw new InvalidPluginExecutionException(
                        "The generated case reference is longer than the case reference column allows. "
                        + "Ask your administrator to check the division code length.");
                }

                // Second (and final) column this plug-in sets. Because this is PreOperation,
                // setting it here means it's included in the same INSERT the platform is
                // about to perform - there's no follow-up update needed.
                target[Names.CaseReference] = reference;
                tracing.Trace("SetCaseReference: division={0} reference={1}", divisionRef.Id, reference);
            }
            // Three catch clauses, ordered from most-specific/expected to least, each
            // deciding what the end user ultimately sees:
            catch (InvalidPluginExecutionException)
            {
                throw; // already carries a sentence written for a case worker; do not re-wrap it
            }
            catch (FaultException<OrganizationServiceFault> fault)
            {
                // A FaultException here means Dataverse itself rejected one of our Retrieve/
                // RetrieveMultiple calls (e.g. a transient platform error, throttling, or a
                // permissions problem). The raw fault is traced for diagnosis, but the user
                // gets a generic, non-technical message plus a name to quote to support.
                tracing.Trace("SetCaseReference: platform fault: {0}", fault.ToString());
                throw new InvalidPluginExecutionException(
                    "The case reference could not be generated. Please try again. "
                    + "If this keeps happening, quote SetCaseReference to your administrator.", fault);
            }
            catch (Exception ex)
            {
                // Catch-all safety net for anything not anticipated above (a bug in this
                // plug-in, an unexpected null, etc.). Same pattern: trace the real exception
                // for developers, surface a safe message to the user.
                tracing.Trace("SetCaseReference: unexpected error: {0}", ex.ToString());
                throw new InvalidPluginExecutionException(
                    "An unexpected error occurred while generating the case reference. "
                    + "Quote SetCaseReference to your administrator.", ex);
            }
        }

        /// <summary>
        /// ADR-023. Read-highest-and-increment. Correct under the single-writer conditions of
        /// Phase 1 and demonstrably wrong under concurrent create: two simultaneous transactions
        /// read the same highest value and one of them loses to the alternate key. The weakness
        /// is deliberate, named in BR-01, and is the seed of Phase 2's INC-004.
        ///
        /// Plain-language version: given a prefix like "OPS-2026-", this finds the
        /// highest-numbered existing case reference that starts with it and returns the
        /// next integer. It is NOT concurrency-safe - see the ADR note above - which is
        /// why the caller relies on the hlx_ak_casereference alternate key as a backstop:
        /// if two cases race for the same number, one create simply fails and (presumably)
        /// retries, rather than silently producing a duplicate reference.
        /// </summary>
        private static int NextSequence(IOrganizationService service, ITracingService tracing, string prefix)
        {
            // QueryExpression builds a query against Dataverse (roughly "SELECT
            // hlx_casereference FROM hlx_case WHERE hlx_casereference LIKE 'prefix%'
            // ORDER BY hlx_casereference DESC" limited to 1 row).
            var query = new QueryExpression(Names.Case)
            {
                ColumnSet = new ColumnSet(Names.CaseReference), // only fetch the column we need
                NoLock = true,   // read without taking a lock - this is a best-effort read, not a reservation
                TopCount = 1     // we only care about the single highest match
            };
            query.Criteria.AddCondition(Names.CaseReference, ConditionOperator.BeginsWith, prefix);
            query.AddOrder(Names.CaseReference, OrderType.Descending);

            var rows = service.RetrieveMultiple(query);
            if (rows.Entities.Count == 0)
            {
                // No case has ever used this division+year prefix before, so this is the
                // first one - start the sequence at 1.
                tracing.Trace("SetCaseReference: no existing case for prefix '{0}'; starting at 1.", prefix);
                return 1;
            }

            // Because references sort as plain strings (not numbers), fixed-width, zero-
            // padded numbering ("000041" before "000042") is what makes DESC string order
            // match numeric order. Strip the prefix off the highest match to get just the
            // digits, e.g. "OPS-2026-000041" -> "000041".
            var highest = rows.Entities[0].GetAttributeValue<string>(Names.CaseReference) ?? string.Empty;
            var tail = highest.Length > prefix.Length ? highest.Substring(prefix.Length) : string.Empty;

            int parsed;
            // NumberStyles.None means "digits only" - no leading sign, no thousands
            // separators. If the tail isn't a clean number (e.g. the row was hand-edited
            // or came from a different naming scheme), we don't crash - we just restart
            // the sequence rather than propagate garbage.
            if (!int.TryParse(tail, NumberStyles.None, CultureInfo.InvariantCulture, out parsed))
            {
                tracing.Trace("SetCaseReference: could not parse the sequence out of '{0}'; restarting at 1.", highest);
                return 1;
            }

            tracing.Trace("SetCaseReference: highest existing '{0}'; next sequence {1}.", highest, parsed + 1);
            return parsed + 1;
        }
    }
}