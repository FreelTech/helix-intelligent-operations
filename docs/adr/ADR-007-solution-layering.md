# ADR-007 — Solution layering and dependency direction

| | |
|---|---|
| **Status** | Accepted |
| **Date** | 18 August 2026 |
| **Stage** | Phase 1, Stage 2 |
| **Relates to** | `NFR-MNT-02`, `NFR-PRT-01`, `NFR-MNT-03`; `ADR-006` |
| **Note** | The charters cite an `ADR-007` concerning the Azure AI façade. That is the legacy numbering; see `docs/logs/adr-mapping.md` |

## Context

Solutions are the unit of transport between environments, and the platform tracks
dependencies between them. A solution that depends on a base solution cannot be
installed before it, and the base cannot be uninstalled while a dependent remains.
Import order is therefore enforced, not merely conventional.

`NFR-MNT-02` requires that solution dependencies flow one way from a single core, with
no cycles, verified by dependency inspection at export.

The Implementation Guide §0 locks a five-solution structure. This ADR records the
dependency direction and the reasoning, because with five empty solutions the platform
has nothing to discover and this document is the only place the graph exists.

## Decision

Five unmanaged solutions in `Helix-DEV`, all owned by publisher `nexacorp`, all at
version `1.0.0.0` at creation.

| Solution | Unique name | Holds | Depends on |
|---|---|---|---|
| Helix Core | `Helix_Core` | Tables, columns, relationships, choices, alternate keys, security roles, environment variable definitions | — |
| Helix Integration | `Helix_Integration` | Custom connectors, Azure Function references, webhooks | Core |
| Helix Automation | `Helix_Automation` | Cloud flows, connection references | Core, Integration |
| Helix AI | `Helix_AI` | Copilot Studio agents, AI Builder models, prompts | Core, Automation |
| Helix Apps | `Helix_Apps` | Model-driven app, canvas app, Power Pages, PCF, sitemap | Core |

Import order is the same direction: Core, Integration, Automation, AI, Apps.

Two rules that make the graph enforceable rather than aspirational:

1. **Nothing depends on `Helix_Apps`.** An app that other solutions depended on would
   be an app that could never be replaced.
2. **No component belongs in two solutions.** Where it is ambiguous — a security role
   that only an app uses, for instance — it goes in `Helix_Core`, because Core is the
   only solution every other one already depends on.

## Alternatives considered

**One monolithic solution.** Rejected. It removes all dependency questions by removing
all boundaries, and with them the ability to ship or roll back one layer independently.
It also removes the thing `NFR-MNT-02` is there to verify.

**Splitting by division — a NexaFinance solution, a NexaServe solution, a NexaWorks
solution.** Rejected. The divisions are a *security* boundary, expressed through
business units and roles, not a *component* boundary. All three divisions share the
same case table; splitting by division would put one table in three solutions or force
a shared-core solution anyway, arriving back here with extra steps.

**Deferring the split until there is something to split.** Rejected. Solutions are
cheap to create and expensive to reorganise once components are in them, because moving
a component between solutions is straightforward only while everything is unmanaged.

## Consequences

- Until Stage 3 puts tables in `Helix_Core`, the platform reports no dependencies at
  all. This document is the baseline against which the first real dependency report is
  checked; a dependency that appears and is not on the table above is a design error,
  not a surprise.
- The AI solution depends on Automation because agents invoke flows, and Automation
  depends on Integration because the triage flow calls the custom connector. If either
  of those implementation choices changes, this ADR changes with it.
- Stage 12's import into `Helix-TEST` must follow the order above. Getting it wrong
  produces a missing-dependency error, which is the platform doing its job.
- Stage 13's hygiene pass may re-layer. `ADR-006`'s single publisher is what keeps that
  option open.