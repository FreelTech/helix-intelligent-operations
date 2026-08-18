# ADR-010 — Azure foundation posture

| | |
|---|---|
| **Status** | Accepted |
| **Date** | 18 August 2026 |
| **Stage** | Phase 1, Stage 2 |
| **Relates to** | `NFR-SEC-01`, `NFR-SEC-02`, `NFR-PRI-03`, `NFR-CAP-05`; charter constraint `C6` |
| **Note** | The charters cite an `ADR-010` concerning canvas field capture. That is the legacy numbering; see `docs/logs/adr-mapping.md` |

## Context

Stage 7 puts an Azure Function and Azure AI Language behind the triage façade, and both
need secrets. `NFR-SEC-01` forbids secrets in source control, configuration files or
connector definitions; `NFR-SEC-02` requires them to be held in a managed vault. The
vault must therefore exist before anything that produces a secret does.

Azure resources live on a **personal** subscription, separate from the M365 tenant
(`C6`). That split is deliberate and has consequences beyond billing: the Azure CLI is
authenticated to a different directory, so it cannot mint tokens for Dataverse.

Three Key Vault properties are fixed at creation and cannot be changed afterwards: the
name, which is globally unique across the service, and the soft-delete retention
period, which defaults to 90 days. A soft-deleted vault keeps its name reserved for the
whole retention period. Purge protection, if enabled, prevents even a deliberate early
purge.

Helix is a portfolio project that may be torn down and rebuilt. The cost profile of a
mistake here is not the cost profile of a production system.

## Decision

| Resource | Value |
|---|---|
| Subscription | `3da201a0-b0d6-4381-bf97-6574beb1ebb8` (personal, `C6`) |
| Resource group | `rg-helix-uk` |
| Location | `uksouth` — satisfies `NFR-PRI-03` |
| Budget | `budget-helix-monthly`, £10/month, actual-cost alerts at 50%, 80% and 100% |
| Key Vault | `kv-helix-8bee26` |

Key Vault configuration, stated explicitly rather than relying on defaults:

| Setting | Value | Reasoning |
|---|---|---|
| `--retention-days` | `7` | The default is 90 and is immutable after creation. Seven days is the documented minimum and bounds the cost of a naming mistake to a week rather than a quarter |
| `--enable-rbac-authorization` | `true` | Already the default for new vaults; stated so this record is self-evidencing rather than dependent on a default that may drift |
| Purge protection | **off** | Deliberately not enabled. With it on, a mistake cannot be purged early even when that is exactly what is wanted |
| `--sku` | `standard` | No HSM requirement anywhere in the design |

Data-plane access is provisioned as part of creation, not afterwards: the creating user
is granted **Key Vault Secrets Officer** at vault scope. Verified by a round trip with
a non-secret value, which was then deleted and purged.

The £10 budget is created now, ahead of the Implementation Guide's schedule, because
`NFR-CAP-05` requires bounded and alerted cloud spend and five minutes here removes a
category of surprise from thirteen remaining stages.

## Alternatives considered

**The default 90-day retention.** Rejected. Immutable after creation, on a globally
unique name, in a project likely to be rebuilt. The default is right for production and
wrong here.

**Purge protection on.** Rejected for the same reason. It is a genuine production best
practice and a genuine liability on a disposable project: it converts a typo into a
mandatory wait.

**Vault access policies instead of RBAC.** Rejected. RBAC is the default for new
vaults, is the direction Azure is moving, and gives one consistent model for both
management and data plane. Access policies would be a deliberate step backwards for no
benefit here.

**Deferring the budget until Azure resources actually cost something, as the
Implementation Guide does.** Rejected. `NFR-CAP-05` is a Phase 1 requirement, Key Vault
operations are billable from the first call, and a spend guard created after the spend
is not a guard.

**A vault name without random characters, such as `kv-helix-uk`.** Rejected. Key Vault
names are globally unique across all of Azure; an obvious project name is likely taken
and, worse, likely to collide with a soft-deleted vault whose name is reserved.

## Consequences

- The vault exists and holds nothing. `NFR-SEC-02` is satisfied in the same negative
  sense `NFR-SEC-01` was at the end of Stage 1: there is a correct place for secrets,
  and no secrets yet.
- Creating a vault grants no ability to read from it. Every future identity that needs
  a secret — the Stage 7 Function's managed identity above all — needs its own role
  assignment, and that assignment is part of provisioning rather than a debugging step.
- Secrets are never passed as command-line literals, because shell history is a file
  and `NFR-SEC-01` applies to it. From Stage 7, values are supplied from a prompt or a
  file.
- The cross-tenant split (`C6`) is now exercised rather than theoretical. It is why
  `ADR-008`'s Web API route needs a token this machine cannot produce, and it is what
  Stage 7's API-key authentication between connector and Function exists to bridge.
- Deleting `rg-helix-uk` deletes everything inside it. The vault survives for seven
  days in a soft-deleted state, with its name reserved for that period.