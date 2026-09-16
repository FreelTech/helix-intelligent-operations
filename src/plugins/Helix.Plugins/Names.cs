namespace Helix.Plugins
{
    /// <summary>
    /// Logical names, cross-checked against the unpacked Helix_Core solution (B1).
    /// Members are named for the ROLE the name plays, not the string it holds, which is
    /// why Contact and CaseContact are separate: one is a table, one is a lookup on
    /// hlx_case pointing at it. Same string, different jobs.
    /// </summary>
    internal static class Names
    {
        // Tables
        internal const string Case     = "hlx_case";       // display "Case",         user-owned
        internal const string CaseType = "hlx_casetype";   // display "Case type",    org-owned
        internal const string Division = "hlx_division";   // display "Division",     org-owned
        internal const string Contact  = "hlx_contact";    // display "Helix contact", user-owned

        // hlx_case columns
        internal const string CaseReference        = "hlx_casereference";    // primary name, max 20, Optional
        internal const string CaseSummary          = "hlx_summary";
        internal const string CaseCaseType         = "hlx_casetype";         // lookup -> hlx_casetype, required
        internal const string CaseContact          = "hlx_helixcontact";          // lookup -> hlx_contact, optional
        internal const string CaseDivision         = "hlx_division";         // lookup -> hlx_division, ADR-012
        internal const string CasePriority         = "hlx_priority";         // global choice, owned by BR-04
        internal const string CaseResolutionSummary = "hlx_resolutionsummary";

        // hlx_casetype columns
        internal const string CaseTypeName     = "hlx_casetypename";         // primary name
        internal const string CaseTypeDivision = "hlx_division";             // lookup -> hlx_division, required

        // hlx_division columns
        internal const string DivisionName = "hlx_divisionname";             // primary name
        internal const string DivisionCode = "hlx_divisioncode";             // max 3 chars, Business required

        // hlx_contact columns
        internal const string ContactFullName     = "hlx_fullname";          // primary name
        internal const string ContactIsVulnerable = "hlx_isvulnerable";      // Yes/No, default No

        // Platform columns
        internal const string StateCode  = "statecode";
        internal const string StatusCode = "statuscode";
    }
}