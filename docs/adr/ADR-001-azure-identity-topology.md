# ADR-001 — Azure identity topology: one directory, not two

- **Status:** Accepted
- **Date:** 2026-08-17
- **Deciders:** Ibn (sole architect)
- **Supersedes:** the cross-tenant split anticipated by `Helix_Phase1_Project_Charter.md` constraints C2 and C6
- **Related:** ADR-002 (planned, Stage 2 — DLP policy), Stage 7, Stage 12

## Context

Helix spans two platforms: Power Platform / Dataverse in the tenant
`learnwithpowerup.onmicrosoft.com`, and Azure resources (Function App, Key Vault,
Azure AI Language F0) that back the `TriageText` façade.

The Phase 1 charter recorded constraint C2 — "no Entra / tenant admin rights" — and
derived C6 from it: a separate personal Azure subscription in its own directory, with
API-key authentication from connector to Function as the only available option.

That premise is incorrect. The tenant was created by me and I hold Global
Administrator. Entra app registrations, service principals and self-service
subscription settings are all available. C2 is retired, which removes the sole
justification for C6.

## Decision

The Azure subscription is created **inside the existing tenant directory**, by signing
up with the work account `<your UPN>`. Power Platform and Azure share one Entra
directory and one set of identities.

The Developer Plan environments already present in the tenant are reused rather than
recreated, so the auto-provisioned environment becomes `Helix-DEV` and carries an
earlier creation date than its siblings.

Day-to-day administration is performed as Global Administrator rather than as a
narrower Power Platform Administrator, because there is one operator. This is a
knowing deviation from least-privilege guidance and is confined to the operator
account; all functional testing from Stage 11 is performed as non-admin persona users.

## Alternatives considered

| Alternative | Rejected because |
|---|---|
| **Two directories** — Azure in its own directory under a personal Microsoft account | Loses managed-identity-to-Dataverse as an option; forces two service principals and a split CI/CD pipeline at Stage 12; doubles the admin surface; and requires an ADR to explain a constraint that does not exist. It would be inheriting a workaround for a problem I do not have. |
| **Two directories with a cross-tenant B2B trust** | All the cost of the split plus configuration complexity, for no capability Helix needs. |
| **Delay the decision until Stage 7** | A subscription's directory is chosen at creation. Deferring the decision means deferring the signup, which defers the discovery of signup friction to the day the façade is due. |

## Consequences

**Positive**
- Managed identity is available from the Function to Key Vault, and to Dataverse if ever needed.
- One Entra app registration can serve both halves of the Stage 12 pipeline, so CI/CD is real rather than commented out.
- One portal, one sign-in, one cost view.

**Negative / accepted**
- The subscription is in the same tenant as everything else, so a compromised tenant admin account reaches both platforms. Accepted: single-operator project, no production data, no real personal data (charter assumption A3).
- Operating as Global Administrator means missing privileges are never surfaced by my own usage. Mitigated by mandatory persona-user testing from Stage 11 onward.

**Neutral**
- API-key authentication from custom connector to Function is retained for Stage 8, now as a deliberate simplicity choice rather than a forced one. The key is held in Key Vault per FR-GOV-09.

## Charter amendments required

- C2 — retired. Reason recorded here.
- C6 — reversed. Reason recorded here.
- C7 — already reversed by the Phase 1 baseline decision (three environments).
- DEBT-4 — expected to close at Stage 11 rather than Phase 2 Epic E, because persona users now exist.
- NFR-SEC-03 — moves from "untestable" to "testable at Stage 11".