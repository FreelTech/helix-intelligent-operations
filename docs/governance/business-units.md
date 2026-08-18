# Business unit topology — Helix

Business units are Dataverse **data**, not solution components. They do not move with
a solution import. Every environment that receives a Helix solution must have this
hierarchy created by hand, with these exact names, before the import.

| Name | Parent | Division | Notes |
|---|---|---|---|
| `NexaCorp Group` | *(none — root)* | Group | Renamed from the provisioning default. Cannot be deleted |
| `NexaFinance` | `NexaCorp Group` | Consumer credit | FCA Consumer Duty framing |
| `NexaServe` | `NexaCorp Group` | Public services | Welsh language duty, WCAG 2.2 AA |
| `NexaWorks` | `NexaCorp Group` | Field operations | RIDDOR incident evidence |

**Flat by decision** — see `ADR-008`. No nesting below the divisions.

**Recreation checklist for a new environment**
1. Rename the root business unit to `NexaCorp Group` (maker portal → Tables → Business Unit → Data).
2. Create the three division business units with the root as parent.
3. Verify names character-for-character; security role imports bind by name.

Applies to: `Helix-DEV` (created 18 August 2026), `Helix-TEST` (pending, Stage 12).