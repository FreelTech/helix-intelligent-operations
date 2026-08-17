# Helix — Intelligent Operations Platform

One governed Dataverse case-management core serving three regulated divisions of the
fictional NexaCorp Group, through three purpose-fitted experiences, with an AI layer
that is auditable by construction.

**Status:** Phase 1 (build) in progress — Stage 0 of 15 complete.

**Governing invariant:** redact before store. Free-text customer content passes through
the `TriageText` façade; only the redacted form is persisted. PII categories are
recorded for audit; PII values never are.

## Repository layout

| Path | Contents |
|---|---|
| `src/solutions/` | Unpacked Dataverse solution source (`pac solution unpack` output) |
| `src/plugins/` | C# plugin projects, net462 |
| `src/pcf/` | PCF code components, TypeScript |
| `src/functions/` | Azure Functions, dotnet-isolated |
| `src/connectors/` | Custom connector definitions |
| `src/scripts/` | JavaScript web resources and the step-registration console app |
| `docs/adr/` | Architecture Decision Records |
| `docs/stages/` | Stage walkthrough documents |
| `docs/logs/` | RAID log, lessons-learned log, exam gap log, estate inventory |
| `docs/diagrams/` | Architecture and sequence diagrams |
| `.github/workflows/` | CI workflows. `pr-checks.yml` runs on every pull request; deployment jobs are added at Stage 12 |
| `.ci/` | CI configuration, including the secret-scan allowlist |

## Conventions

Publisher `NexaCorp Helix` (`nexacorp`) · prefix `hlx` · choice prefix `74000` ·
GitHub Flow · Conventional Commits · solution source committed unpacked, never as archives.

## Setup

See `docs/stages/01-LEARN+LAB-Accounts-And-Cloud-Provisioning.md`.
