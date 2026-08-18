# Decision index

Single authoritative ADR register. The Phase 1 charter and the Implementation Guide
each assigned overlapping numbers to different subjects; this file resolves the
collision, and the numbers here win.

| ADR | Title | Status | Stage |
|---|---|---|---|
| ADR-001 | Azure identity topology: one directory, not two | Accepted | 0 |
| ADR-002 | *reserved* — DLP policy design and connector classification | Planned | 2 |
| ADR-003 | *reserved* — business unit structure per division | Planned | 2 |
| ADR-004 | *reserved* — solution layering and dependency direction | Planned | 2 |
| ADR-005 | *reserved* — Managed Environment controls, designed but not licensed | Planned | 2 |
| ADR-006 | *reserved* — declarative logic versus plugin: the responsibility boundary | Planned | 4 |
| ADR-007 | *reserved* — the six-field façade contract | Planned | 7 |
| ADR-008 | *reserved* — connector-to-Function authentication | Planned | 8 |
| ADR-009 | *reserved* — RAG banding derivation, formula column versus client logic | Planned | 6 |
| ADR-010 | *reserved* — canvas field capture, Patch() over EditForm | Planned | 10 |

> Numbering rule: allocate the next free number when the decision is *made*, not when
> it is anticipated. Reserved rows above exist to prevent the collision that occurred
> between the earlier planning documents; if a reserved decision turns out not to be
> needed, mark the row `Withdrawn` rather than reusing the number.


RAID entries, numbered on from file 02's:

Type	Entry
Risk	Developer environments are disabled after 30 days with no user activity and deleted 15 days later, with a further 7 days to recover [D4]. Helix-TEST is idle by design and is the most exposed. Mitigation: fortnightly calendar reminder; Trigger environment activity in the admin centre; from Stage 8, consider a low-frequency scheduled flow as a keep-alive, costing runs from the 750/month allowance [D2]. Review at every stage boundary
Risk	The Developer Plan environment ceiling is ambiguous in the documentation [D2] and was resolved empirically on 18 August 2026 (see ADR-005). The answer may change with licensing. Mitigation: Helix-PROD is deferred and conditional; re-test before Stage 12 rather than assuming
Risk	The data policy is not in source control and cannot be, on macOS [D11][D13]. Portal drift from docs/governance/dlp-policy-helix-baseline.md would be undetected. Mitigation: pac admin dlp-policy show diffed against the file at every stage boundary; deviations table maintained in the specification
Risk	Business units do not travel in solutions [D10]. A Stage 12 import into Helix-TEST will bind security roles to business units that do not exist there. Mitigation: docs/governance/business-units.md recreation checklist, executed as an explicit step of the import rehearsal, not as a prerequisite assumed to be done
Assumption	AI Builder use rights are not included in the Power Apps Developer Plan [D2]. Stage 12's AI Builder work depends on a trial. Revisit before Stage 9, not during it
Issue	Managed Environments are unreachable: use rights are not in the Developer Plan and every user running assets in a managed environment needs a premium licence [D2]. Recorded as charter constraint C11; documented as a gap rather than simulated (A7.2)
Issue	Nothing in CI validates that committed solution source matches the environment. A portal edit after an export leaves the repository stale with no signal (A8). Closes at Stage 12 when export becomes a pipeline step
Issue	ADR numbering collision between the charters' legacy references and the register (ADR-007, ADR-010). Mapping file created; full reconciliation deferred to Stage 13