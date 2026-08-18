# ADR-009 — Data policy, and its specification as the source of truth

| | |
|---|---|
| **Status** | Accepted |
| **Date** | 18 August 2026 |
| **Stage** | Phase 1, Stage 2 |
| **Relates to** | `NFR-CMP-04`, `NFR-MNT-05`, `NFR-PRT-01`; charter constraint `C1`; `docs/governance/dlp-policy-helix-baseline.md` |

## Context

`NFR-CMP-04` requires every connector to sit within a data policy, with a target of
100% classified. A data policy sorts connectors into Business, Non-Business and Blocked
groups and enforces one rule: connectors from different groups cannot be combined in
the same app or flow.

The problem is not the policy. It is that the policy cannot be put under version
control from this machine. `pac admin dlp-policy` offers `list` and `show` and no write
verb. The documented authoring route is the
`Microsoft.PowerApps.Administration.PowerShell` module, which uses .NET Framework and
is therefore incompatible with PowerShell 6.0 and later — it requires Windows
PowerShell 5.x, which does not exist on macOS (`C1`). PowerShell 7 runs here; those
modules do not.

So the policy is authored by hand in a wizard, and a wizard leaves no artefact.
`NFR-MNT-05` (decisions reconstructable years later) and `NFR-PRT-01` (rebuildable from
committed source) both have a hole in them at exactly this point.

## Decision

One tenant-level policy, `Helix — Tenant Baseline`, scoped to **all environments** with
no exclusions. Default group for new connectors: **Non-Business**. Custom-connector
wildcard rule (`*`): **Ignore**.

**`docs/governance/dlp-policy-helix-baseline.md` is normative.** The Power Platform
admin centre renders that document. Where the two disagree, the document is correct and
the portal has drifted; the difference is a defect to be closed, not a fact to be
copied back into the file.

The specification records the full Business list, the full Blocked list, the default
group, the scope, the ordered custom-connector URL patterns, and a deviations table for
connectors that could not be blocked because they are on Microsoft's unblockable list.

Verification, at every stage boundary:

    pac admin dlp-policy list
    pac admin dlp-policy show --policy-name <policy-guid>

diffed by eye against the specification. Note that the CLI view shows only connectors
explicitly classified, so a connector absent from the output is one correctly sitting
in Non-Business by default.

Two classifications are deliberate rather than obvious and are called out here because
they will look like mistakes later:

- **HTTP, HTTP Webhook and "When an HTTP request is received" are Business.** Child
  flows carry an internal dependency on the HTTP connector; classifying it elsewhere
  breaks them.
- **The custom-connector wildcard stays Ignore.** Setting it to Blocked would kill the
  Stage 8 custom connector fronting the `TriageText` Function — silently, and weeks
  after the change, because no custom connector exists today for it to break.

## Alternatives considered

**Environment-level policies instead of one tenant policy.** Rejected. Environment
policies cannot override tenant policies, they must be maintained per environment, and
they would leave the default environment and the personal developer environment outside
any policy — which fails `NFR-CMP-04`'s 100% target by construction.

**Default group Blocked, "so nothing new can surprise us".** Rejected. Unblockable
connectors added in future are mapped to Non-Business regardless, so the setting buys
inconsistency rather than strictness — and every genuinely new connector arrives dead,
with the failure surfacing in whichever stage first needs it. Microsoft's own
recommendation is Non-Business.

**Authoring via PowerShell on a borrowed Windows machine or a VM.** Rejected for the
same reason `ADR-003` rejected a Windows VM for the Plug-in Registration Tool: an
unstated escape hatch stops a constraint from producing good engineering. Had a VM been
available, the specification document would never have been written — and the document
is a better artefact than the script would have been.

**Advanced connector policies (ACP).** Not chosen. ACP's allowlist model is a better
fit for "deny by default", but it applies to certified connectors only; custom and HTTP
connectors — precisely the ones Helix depends on — are still governed by classic data
policies. Revisit if ACP's coverage widens.

## Consequences

- `NFR-CMP-04` is satisfied by construction: a tenant policy over all environments
  leaves nothing unclassified.
- The policy took effect immediately, tenant-wide, including the default environment.
  Nothing broke because nothing existed, which is exactly why this was the right moment.
- Drift between portal and document is now a named risk with a named check. Nothing
  automated will catch it; the stage-boundary diff is the whole control.
- Any connector added to a Helix component in a later stage must be classified in the
  specification **first** and in the portal second. Reversing that order is how the
  document becomes fiction.
- Blocking decisions are constrained by Microsoft's unblockable list. Where a connector
  intended for Blocked could not be blocked, it sits in Non-Business and is recorded in
  the deviations table rather than quietly left in Business.