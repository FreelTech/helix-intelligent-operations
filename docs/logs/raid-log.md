# RAID log — Risks, Assumptions, Issues, Dependencies

| ID | Type | Description | Likelihood | Impact | Mitigation | Status | Opened |
|---|---|---|---|---|---|---|---|
| RISK-01 | Risk | A developer environment goes 30 days without activity and is automatically disabled. Part-time cadence makes TEST and PROD the likely victims. Failure is silent. | Medium | Medium | Fortnightly diary entry to open each environment and touch one row | Open | Stage 0 |
| RISK-02 | Risk | After converting to pay-as-you-go at Stage 7, the spending limit is gone permanently and only a budget email stands between usage and a bill | Medium | Medium | All AI resources on F0; budget alerts on forecast at 50% and 90%; read pricing before creating any resource | Open | Stage 0 |
| RISK-03 | Risk | Persona users hold no Microsoft 365 licence and therefore have no mailbox; any Stage 8 flow that emails a case owner will fail against them | High | Low | Use a Teams or Dataverse notification path for persona testing, or assign a mailbox at Stage 8 if genuinely needed | Open | Stage 0 |
| ASSUM-01 | Assumption | Developer Plan entitlements, including three environments per user and premium connector access, remain available for the project's duration | — | High | Solution source committed unpacked so the build survives an environment rebuild | Open | Stage 0 |
| ASSUM-02 | Assumption | No real personal data is used at any point | — | High | Non-negotiable; the redact-before-store invariant holds regardless | Open | Stage 0 |
| DEP-01 | Dependency | Azure subscription must be live and converted to PAYG before Stage 7 | — | High | Diarised at the start of Stage 7 | Open | Stage 0 |
| ISSUE-— | | *(none yet)* | | | | | |