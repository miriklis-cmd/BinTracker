# BinTracker Current Release Notes

## v0.4.0-alpha.23.5.2

### Full historical requirements reconciliation / audit-gate hardening

This candidate is intentionally documentation/audit/release-discipline focused. It was created after the operator correctly raised concern that prior audits had allowed version, packaging and current-state contradictions through.

Actual reconciliation sources:
- all BinTracker history surfaced in the current conversation context;
- current alpha.23.5.1 source package;
- 166 archived BinTracker ZIPs available in the working archive;
- recovered historical roadmaps/readmes/test checklists/specifications;
- current C# source/test inventory and Markdown documentation.

Important limitation: a direct personal-context query returned no additional BinTracker chat history, so this release does **not** falsely claim a raw-turn review of every historical ChatGPT conversation.

Changes:
- added `docs/RequirementsAcceptanceRegister.md` permanent requirements ledger;
- added `docs/ReconciliationReport.md` with sources, limitations, findings and recovered requirements;
- rebuilt active `TEST-CHECKLIST.md` and migrated permanent small behaviors out of historical alpha blocks;
- corrected stale .NET SDK/global.json statements;
- corrected stale ImportWizard Replace/Correct and Import History “remaining” statements;
- repaired identifiable corrupted headings in `docs/DocumentationAudit.md`;
- restored detailed post-v1 customer-list/import-intent/Import-Profile requirements;
- strengthened `Audit-BinTracker.ps1` to validate register identity/status/scope, current checklist version, required docs and known stale contradictions;
- added `Package-BinTracker.ps1` to create and reopen a version-matched ZIP and verify sole root + version metadata.

No product business behavior was deliberately changed in this reconciliation candidate.

### Test requirement

Run `Build-BinTracker.bat` once. The strengthened source audit must pass before restore. Windows compile/tests remain the authoritative executable gate. Daily Print Pack / Monthly Summary manual acceptance remains open from the preceding Reporting work.
