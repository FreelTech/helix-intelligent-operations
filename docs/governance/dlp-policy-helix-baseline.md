# Data policy specification — "Helix — Tenant Baseline"

**Status:** normative. The Power Platform admin centre renders this document; where the
two disagree, this document is correct and the portal is drifted. See `ADR-009`.

**Why a document:** the write path for data policies is
`Microsoft.PowerApps.Administration.PowerShell`, which requires Windows PowerShell 5.x
and is incompatible with PowerShell 6+ / .NET Core. `pac admin dlp-policy` is read-only.
On macOS there is no scripted authoring route, so the policy is not in source control.
This file is the compensating control. Satisfies `NFR-CMP-04`, supports `NFR-MNT-05`.

| Property | Value |
|---|---|
| Policy name | `Helix — Tenant Baseline` |
| Scope | Tenant-level, **all environments** (no exclusions) |
| Default group for new connectors | **Non-Business** |
| Custom connector wildcard (`*`) rule | **Ignore** |
| Created | 18 August 2026 |
| Owner | Ibn |

## Business group — Helix's real data path

These connectors may be combined with each other and with nothing else.

| Connector | Why it is here |
|---|---|
| Microsoft Dataverse | The system of record. Unblockable by design |
| Microsoft Dataverse (legacy) | Same service, older connector. Classified together to avoid a split |
| Microsoft 365 Outlook | Notification path. Unblockable |
| SharePoint | Document evidence path. Unblockable |
| Microsoft Teams | Copilot Studio agent surface (Stage 11). Unblockable |
| Microsoft Copilot Studio | The agent itself. Unblockable |
| Approvals | Human-in-the-loop gate for agent actions. Unblockable |
| Notifications | Used by Approvals. Unblockable |
| Azure Key Vault | Secret retrieval (Stage 7) |
| HTTP | **Deliberate.** Child flows carry an internal dependency on the HTTP connector; classifying it elsewhere would break them |
| HTTP Webhook | Same reasoning as HTTP |
| When an HTTP request is received | Same reasoning as HTTP |
| Office 365 Users | Owner and assignment lookups |

## Blocked group — consumer services with no place in a regulated flow

X (Twitter) · Facebook · Instagram · Gmail · Outlook.com · Google Drive · Dropbox ·
Box · Bitly · Pinterest · Reddit · YouTube · MSN Weather · Yammer *(if blockable in
your tenant; Yammer appears on the unblockable list — if the Block action is not
offered, place it in Non-Business and note the deviation here)*

**Rule:** if the Block action is unavailable for a connector listed above, it is on
Microsoft's unblockable list. Place it in **Non-Business**, and record it in the
deviations table below rather than silently leaving it in Business.

## Non-Business group — everything else

Every connector not named above stays in Non-Business, including all connectors
Microsoft adds in future (that is what the default group setting does).

## Custom connectors

Tenant-level policies match custom connectors by ordered URL pattern, not by name.

| Order | Pattern | Action | Reason |
|---|---|---|---|
| 1 | `https://*.azurewebsites.net/*` | Business | The Stage 7 `TriageText` Function façade lives here |
| 2 (last) | `*` | **Ignore** | Platform default. Defers classification of anything else |

⚠️ Changing rule 2 to Blocked would kill the Stage 8 custom connector **silently and
weeks later**, because no custom connector exists at the time of the change.

## Deviations observed at build time

| Connector | Intended group | Actual group | Reason |
|---|---|---|---|
| *(none at 18 August 2026)* | | | |

## Verification

    pac admin dlp-policy list
    pac admin dlp-policy show --policy-name <policy-guid>

Compare the output against this file. Any difference is a defect in one of the two.