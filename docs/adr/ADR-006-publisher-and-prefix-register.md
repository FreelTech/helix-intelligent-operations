# ADR-006 — Publisher and prefix register

| | |
|---|---|
| **Status** | Accepted |
| **Date** | 18 August 2026 |
| **Stage** | Phase 1, Stage 2 |
| **Relates to** | `ADR-007`; Implementation Guide §0 locked conventions |

## Context

Every component in Dataverse carries a logical name assembled as `<hlx>_<name>`, and
that name is permanent: metadata names cannot be changed after creation. The prefix is
taken from the publisher of the solution context active at the instant of creation —
not from the publisher of the solution the component later ends up in.

A publisher also carries a choice value prefix, a five-digit number prepended to the
integer value of every option added to a choice column, so that two vendors' options
cannot collide on import.

Separately, the publisher of a component that has shipped inside a managed solution
cannot afterwards be changed. Microsoft's guidance follows directly from that: define a
single publisher, so the freedom to change the layering model across solutions is
preserved.

Helix has five solutions and a scheduled re-layering pass at Stage 13.

## Decision

One publisher owns all five Helix solutions and every component in them.

| Field | Value |
|---|---|
| Display name | `NexaCorp Helix` |
| Unique name | `nexacorp` |
| Customisation prefix | `hlx` |
| Choice value prefix | `74000` |

Created in the Power Apps maker portal on 18 August 2026, in `Helix-DEV`, before any
component existed. Verified by reading all four fields back from the saved record, and
again from `src/solutions/Helix_Core/Other/Solution.xml` after the first export.

`Helix_Core` is set as the **preferred solution**, so that components created outside
any solution context land under this publisher rather than under the default one.

## Alternatives considered

**A publisher per solution.** Rejected. It buys a cosmetic tidiness and permanently
forfeits the ability to move component ownership between solutions, because publisher
cannot be changed once a component ships managed. It would also mean five choice value
prefixes to keep straight, with no benefit at any point in the project's life.

**Letting `pac solution init` create the publisher.** Rejected. The CLI takes only a
unique name and a prefix; it cannot set the display name or the choice value prefix,
both of which are effectively irreversible. Creating the publisher from the CLI would
have meant either hand-editing `Solution.xml` before the first import or accepting a
machine-chosen choice value prefix. The one irreversible act in this stage belongs on
the screen that shows all four fields at once.

**Accepting the portal's auto-generated choice value prefix.** Rejected. It is derived
from the customisation prefix and is not `74000`, which the Implementation Guide locks.
A generated value would work, but it would not be the value every other project
document names.

## Consequences

- Every Helix component is named `hlx_*` and every Helix choice option is numbered
  `740000nnn`. Both are visible at a glance in any solution XML diff, which is a small
  but real review benefit.
- Any component created outside a Helix solution context inherits the default
  publisher's random prefix, silently and permanently. The preferred-solution setting
  is the countermeasure; the deliberate breakage in file 03 §B11 demonstrates what
  happens without it.
- A component created with the wrong prefix cannot be renamed. The remedy is always
  delete and recreate, which is cheap now and expensive after Stage 5 registers
  plug-in steps against table names.
- This publisher is created by hand in every environment that receives Helix work —
  or, more usually, arrives with the first solution import, which carries the publisher
  definition with it.