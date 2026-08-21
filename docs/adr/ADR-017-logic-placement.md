# ADR-017 — Where business logic lives in Helix

## Context
Helix must enforce rules that arrive from four different write paths: a model-driven
app, a canvas app, cloud flows, and raw Web API calls. Dataverse offers six
mechanisms for logic without code (business rules in three scopes, formula columns,
calculated columns, rollup columns, classic workflows, cloud flows) plus plug-ins.
Choosing per-requirement by instinct produces an estate nobody can reason about, and
PL-400 tests the choice explicitly. A single stated rule is cheaper than twelve
defensible one-offs.

## Decision
Every requirement is placed by four questions, asked in order, stopping at the first
"no":
1. Does the logic need anything other than the row being written? If yes, it is not a
   business rule.
2. Must it hold on every write path, or only where a human is looking?
3. Must it be true at the moment of the write, or is eventually enough?
4. Is the answer a value, or a verdict?

Question 1 is decisive and is grounded in an observed platform limit, not a
preference. Observed in Helix-DEV on 2026-08-20: the business rule condition designer
for the Case table offers only the Case table as a condition source; there is no route
to a related row. Rollup columns are absent from the field list, consistent with the
documented exclusion. [record the formula-column observation from B5 here]

Placements are recorded in the table at A7.1 of file 05 and are binding.

## Alternatives considered
- **Case-by-case judgement, documented per requirement.** Rejected: it produces
  inconsistent placements that cannot be defended as a system, and it is the pattern
  the exam bullet "determine where to implement business logic" is testing against.
- **Plug-ins for everything, for uniformity.** Rejected: it discards the platform's
  declarative layer, makes trivial validation a deployment event, and would be a poor
  portfolio signal — the ability to *not* write code is the skill on display.
- **Business rules for everything low-code, plug-ins only when a rule errors.**
  Rejected: the failure mode is silent, not an error. A rule with a model-driven-only
  action activates cleanly and enforces nothing server-side.

## Consequences
- BR-02 (vulnerable-contact prioritisation) cannot be a business rule and is deferred
  to Stage 5's SetVulnerablePriority plug-in. This is a platform limit, not a design
  preference, and is now evidenced.
- BR-04's single-writer rule follows: the plug-in layer owns hlx_priority, the flow
  layer owns status routing, and no business rule touches either.
- Any future requirement that needs related-row data is a plug-in or a flow by
  default, and the estimate must reflect that.
- If a future platform release adds related-table conditions to business rules, this
  ADR is superseded rather than amended, and the observation date above is what makes
  that reassessment possible.