namespace Helix.Plugins
{
    /// <summary>
    /// Option values observed in Helix-DEV (docs/logs/option-values.md). A comparison
    /// against a value no row ever holds is a branch that never runs and never complains,
    /// so these are the highest-risk five lines in the assembly.
    /// </summary>
    internal static class HelixOptions
    {
        // hlx_priority
        internal const int PriorityHigh     = 740000002; // [replace with YOUR value]
        internal const int PriorityCritical = 740000003; // [replace with YOUR value]

        // statuscode on hlx_case
        internal const int CaseStatusClosed = 740000004; // [replace with YOUR value]

        // statecode on hlx_case — platform constants, safe to hard-code
        internal const int StateActive   = 0;
        internal const int StateInactive = 1;
    }
}