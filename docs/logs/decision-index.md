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