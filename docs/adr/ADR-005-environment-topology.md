# ADR-005 — Environment topology

| | |
|---|---|
| **Status** | Accepted |
| **Date** | 18 August 2026 |
| **Stage** | Phase 1, Stage 2 |
| **Relates to** | Charter constraints `C3`, `C7`, `C13`; `NFR-PRI-03`, `NFR-PRT-01` |

## Context

Helix needs somewhere to be built. Three project documents disagree about how many
environments that means. The Implementation Guide (§2.1) specifies three — DEV, TEST
and PROD-demo. The Phase 1 charter's constraint `C7` states a single environment, and
Phase 2's `C13` repeats it, with DEV doubling as "PROD-demo".

Two facts settle the argument, and neither is in any of those three documents.

First, a managed solution cannot be imported into the environment that contains its
originating unmanaged solution; testing a managed solution requires a separate
environment. That makes a second environment a platform requirement, not a preference:
under `C7` the managed half of the ALM story would be unexecutable, and a portfolio
project whose ALM has never once been run is a claim rather than evidence.

Second, the Power Apps Developer Plan is the only licence available (`C3`), and it
permits only Developer-type environments. Its documented ceiling — "up to three
environments" in the admin centre, alongside one created automatically at sign-up — is
ambiguous about whether that three is a total or an addition. It was therefore tested
empirically rather than assumed.

**Observed on 18 August 2026:** [record what happened when a third environment was
attempted — created, or refused with this exact message: "..."]. Conclusion:
[three in total / three in addition].

## Decision

Two Developer-type environments now, and a third conditionally.

| Environment | Purpose | Created |
|---|---|---|
| `Helix-DEV` | The build environment. All unmanaged work | 18 August 2026 |
| `Helix-TEST` | Managed-solution import target. Exists to make the ALM round trip performable | 18 August 2026 |
| `Helix-PROD` | Deferred to Stage 12, and only if capacity permits | Not created |

Both environments carry identical settings, because an environment that differs from
DEV in region, currency or collation is not a test of DEV:

| Setting | Value | Reversible? |
|---|---|---|
| Type | `Developer` | No |
| Region | `United Kingdom` | No — satisfies `NFR-PRI-03` |
| Dataverse data store | `Yes` | No |
| Currency | `GBP` | No, in practice |
| Language | `English` | No — sets the collation |
| Dynamics 365 apps | `No` | No — and unavailable in Developer environments regardless |
| Security group | `None` | Yes |

`Helix-DEV` environment ID: `[paste]`
`Helix-TEST` environment ID: `[paste]`

Security group `None` is chosen because the tenant has one user (`C3`). There is no
group to select and nobody to exclude. With more than one user this would become a
real decision, and the answer would be a group per environment.

The scripted equivalent, recorded so the topology is reproducible:

    pac admin create \
      --name "Helix-DEV" \
      --type Developer \
      --region unitedkingdom \
      --currency GBP \
      --language English \
      --domain helix-dev

Region defaults to `unitedstates` and currency to `USD`, so both must be passed
explicitly. Region token verified against `pac --version` `[paste]` on 18 August 2026.

## Alternatives considered

**Three environments now, per the Implementation Guide.** Rejected on two grounds. It
may exceed the licence ceiling, and a `Helix-PROD` created today would sit unused for
roughly four months — directly into a 30-day inactivity clock that cannot be turned
off. Deferring costs nothing, because PROD has no job until Stage 12.

**One environment, per charter `C7`.** Rejected because it makes the managed import
unperformable. `C7` is recorded as **superseded with reason** rather than deleted; the
charter should carry the supersession so the original reasoning stays visible.

**Deleting the personal developer environment to free a slot.** Rejected. It is a
working smoke-test target with an independent identity, useful precisely because it is
not Helix; trading it for an environment that will be idle for four months is a bad
exchange.

## Consequences

- The managed-solution import becomes a thing that can be executed and evidenced. This
  is the point of the second environment and the whole justification for the cost.
- Both environments are on the inactivity clock: disabled at 30 days, deleted at 45,
  unrecoverable after 52. `Helix-TEST` is idle by design and is the most exposed asset
  in the project. A fortnightly calendar reminder is now a project control, and a RAID
  risk records it.
- `Helix-PROD` must be re-evaluated at Stage 12, including re-running the capacity test
  — licensing changes, and the 18 August 2026 result is a fact about a date.
- Every `pac` command from here on acts on a selected authentication profile. Profiles
  `helix-dev` and `helix-test` exist and are named after their targets; `pac org who`
  before any destructive or exporting command is now standard practice.
- `Helix-TEST` has a database and nothing else. It gets business units at Stage 12, as
  part of the import rehearsal, so that the rehearsal exercises the checklist in
  `docs/governance/business-units.md` rather than assuming it.