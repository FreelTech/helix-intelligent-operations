# Estate inventory

Recorded at Stage 0. Update whenever anything below changes.

## Tenant and identity

| Item | Value |
|---|---|
| Tenant primary domain | freelTechnologies.onmicrosoft.com |
| Operator account | _ibncastro@FreelTechnologies.onmicrosoft.com_ |
| Entra roles held | Global Administrator |
| M365 licence | Microsoft 365 Business Basic |

## Power Platform

| Environment | Type | Region | Dataverse | Notes |
|---|---|---|---|---|
| | | | | |

Developer-environment allocation used: __ of 3 (operator) · __ of 3 (helix.finance) · __ of 3 (helix.field)

## Persona users

| Account | Persona | Admin role | Dev Plan | Business unit (from Stage 2) |
|---|---|---|---|---|
| helix.finance@… | A-01 NexaFinance Complaints Handler | None | Yes | NexaFinance |
| helix.field@… | A-03 NexaWorks Field Engineer | None | Yes | NexaWorks |

## Azure

| Item | Value |
|---|---|
| Subscription name | |
| Subscription ID | |
| Directory | freelTechnologies.onmicrosoft.com |
| Spending limit | On |
| Budget | budget-helix-monthly, £10/month, forecast 50% / 90%, actual 100% |
| Resource groups | none — Stage 7 |

## GitHub

| Item | Value |
|---|---|
| Repository | |
| Visibility | Public |
| Ruleset | protect-main — PR required, 0 approvals, force pushes blocked |
| Required status checks | guard-secrets · guard-artefacts · guard-adr · guard-pr-title |
| Secret scanning / push protection | Enabled |
| Scan allowlist | .ci/scanignore — 3 entries at Stage 0 |