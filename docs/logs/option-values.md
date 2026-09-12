# Option values observed in Helix-DEV

Read from the maker portal on [today's date]. These are the integers the Stage 5
plug-ins compare against. If a value here is wrong, the plug-ins fail silently:
a comparison against a value no row ever holds is a branch that never runs.

## hlx_priority

| Label | Value |
|---|---|
| Low | 740,000,000 |
| Normal | 740,000,001|
| High | 740,000,002 |
| Critical | 740,000,003 |

## statuscode (Status Reason) on hlx_case

| Label | Value |
|---|---|
| New | 1 |
| Triaging | 740,000,001 |
| In Progress | 740,000,002 |
| Awaiting Customer | 740,000,003 |
| Resolved | 2 |
| Closed | 740,000,004 |

## statecode on hlx_case

| Label | Value |
|---|---|
| Active | 0 |
| Inactive | 1 |
