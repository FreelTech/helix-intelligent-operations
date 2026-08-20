# Environment configuration that solutions do not carry

Every setting below is environment-level. Importing a solution does NOT apply it.
Each must be set by hand in every environment, and verified before that
environment is used. This file is the checklist for the Stage 12 rehearsal.

| Setting | Where | Helix-DEV | Helix-TEST | Set at |
|---|---|---|---|---|
| Dataverse auditing: Start Auditing | PPAC > env > Settings > Audit and logs | On | **Off** | Stage 3 |
| Dataverse auditing: retention | PPAC > env > Settings > Audit and logs | 365 days | **Not set** | Stage 3 |
| Tenant data policy `Helix - Tenant Baseline` | PPAC > Security > Data policy | Applied (all environments) | Applied | Stage 2 |
| Business units (4, names matching exactly) | Maker > Settings > Business units | Created | **Not created** | Stage 2 |

## Verification

Before any import into a target environment, walk this table top to bottom and
tick every row. A solution import that succeeds into a misconfigured environment
is the most expensive kind of green.
