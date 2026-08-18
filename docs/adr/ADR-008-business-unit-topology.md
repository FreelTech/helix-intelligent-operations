# ADR-008 — Business unit topology

| | |
|---|---|
| **Status** | Accepted |
| **Date** | 18 August 2026 |
| **Stage** | Phase 1, Stage 2 |
| **Relates to** | `NFR-SEC-03`, `NFR-SEC-08`; charter constraints `C3`, `C16`; `docs/governance/business-units.md` |

## Context

Business units are the backbone of the Dataverse security model: every user belongs to
exactly one, every user-owned row is owned within one, and security roles belong to
one. In the classic model, access flows down the hierarchy — a role granted at
parent-child level sees everything beneath it.

NexaCorp Group has three divisions with genuinely different regulatory profiles and
genuinely separate data. They are not decoration; each forces a different technical
requirement elsewhere in the build.

Two platform facts shape what is possible here. The root business unit's name is
derived from the domain used when the environment was provisioned, and it cannot be
changed on the business-unit form — the documented route is the Web API. And business
units are rows in the `businessunit` table: they are data, not metadata, and they do
not travel inside a solution.

## Decision

A flat, four-node hierarchy in `Helix-DEV`.

| Name | Parent | Division |
|---|---|---|
| `NexaCorp Group` | — (root) | Group |
| `NexaFinance` | `NexaCorp Group` | Consumer credit — FCA Consumer Duty framing |
| `NexaServe` | `NexaCorp Group` | Public services — Welsh language duty, WCAG 2.2 AA |
| `NexaWorks` | `NexaCorp Group` | Field operations — RIDDOR incident evidence |

No nesting below the divisions.

The root was renamed from its provisioning default (`[record the original name]`) to
`NexaCorp Group` on 18 August 2026, via the Business Unit table's data grid in the
maker portal: **Tables → All → Business Unit → Data →** the row with an empty parent.
This route is community-known rather than documented; Microsoft documents a Web API
`PATCH` to `/api/data/v9.2/businessunits(<id>)` with `{"name": "NexaCorp Group"}`,
which requires a bearer token for the Dataverse endpoint that the Azure CLI cannot mint
from this machine (`C6` — the Azure CLI is authenticated to a different tenant). The
`ServiceClient` console app committed to in `ADR-003` is the eventual home for
operations of this shape.

Because business units do not move with solutions, `docs/governance/business-units.md`
records the topology and a recreation checklist, and is treated as normative.

## Alternatives considered

**A nested hierarchy — divisions under a regional or functional layer.** Rejected.
Depth creates implicit privilege that is easy to add and unpleasant to audit: every
extra level is another place where "who can see this row" needs a diagram to answer.
The fiction describes three peer divisions, and the model should say what is true.

**Leaving the root at its provisioning name.** Rejected. The root business unit appears
in the security-role UI, in owner lookups and in every conversation about the model. A
root named after a tenant domain undermines the fiction for the sake of avoiding one
awkward rename.

**Per-row sharing instead of business units, to sidestep the topology question
entirely.** Rejected on the record, because `NFR-SEC-08` forbids automated per-row
sharing as an access-control mechanism. Sharing is a deliberate exception to a security
model, not a substitute for one.

## Consequences

- Cross-division access is not free. A user who needs data from two divisions needs a
  security role from the second business unit, or an owner team — a Stage 11 decision
  that this topology deliberately leaves open.
- `Helix-TEST`, and any future environment, must have all four business units created
  by hand, with names matching character-for-character, before any solution containing
  security roles is imported. This is an explicit step of the Stage 12 rehearsal, not a
  prerequisite assumed to be done.
- The single-user tenant (`C3`, `C16`) means the isolation this topology provides
  cannot be genuinely exercised in Phase 1. The model is correct by design and untested
  in practice; `NFR-SEC-03` records that honestly rather than claiming coverage.
- The root rename route is observed rather than documented and may stop working. If it
  does, the Web API remains, and the console app becomes urgent rather than optional.