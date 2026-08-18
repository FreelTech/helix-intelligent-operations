RAID entries, numbered on from file 01's:

Type	Entry
Risk	.NET Framework 4.6.2 support ends 12 January 2027 [D14]; Dataverse support for 4.8 is stated as Q4 2026 in source markdown, and one stale localised page still says June 2026 — a date already passed. Mitigation: retarget is a one-line change; review at Stage 12 and again before any interview
Risk	Azure CLI on macOS is migrating from the Homebrew Core formula to a cask, currently preview [D9]. A future brew upgrade may not behave as expected. Mitigation: pin to the formula; re-evaluate at Stage 7
Assumption	Node 24 Active LTS satisfies PCF tooling at Stage 6. Node 26 becomes Active LTS on 28 October 2026 [D12]; revisit then, not mid-stage
Issue	guard-adr's success path relies on a trailing echo to absorb the exit status of its final test. It works; it is fragile. Revisit when the pipeline gains a build step at Stage 5
Issue	The authoritative Microsoft page for plug-in framework support was last updated 27 September 2022 [D6] while the page linking to it was updated 2 July 2026 [D5]. Treat 4.8 guidance as unsettled and re-check before Stage 5