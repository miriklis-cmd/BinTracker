# BinTracker Full Reconciliation Report

Reconciliation candidate: **v0.4.0-alpha.23.5.2**

## What was actually inspected

This reconciliation did **not** claim access to a queryable raw transcript of every ChatGPT conversation ever held. The available personal-context history query returned no additional BinTracker entries.

The reconciliation therefore used the strongest sources actually available:

1. all BinTracker requirements/decisions/defects surfaced in the current conversation context;
2. the complete current alpha.23.5.1 source package;
3. **166 BinTracker ZIP archives** present in the working file archive, spanning early v0.1 builds through the current v0.4 line;
4. historical Roadmap/README/Test Checklist/Specification documentation recovered from those archives, including the large alpha.19.8 acceptance checklist and early v0.2 README;
5. the current **125 C# source/test files** and **21 pre-reconciliation Markdown files**;
6. current build/audit scripts and version/package metadata.

This is deliberately narrower than saying “every raw chat turn,” but substantially broader than the previous current-doc-only audits.

## Reconciliation defects found in alpha.23.5.1

The previous “full audit” was not complete. This pass found:

1. `TEST-CHECKLIST.md` baseline and Build gate still said alpha.23.5 rather than alpha.23.5.1.
2. `TEST-CHECKLIST.md` said historical alpha checklists had been removed while still containing many alpha-specific historical sections.
3. Outstanding Containers acceptance still referred to a separate **Run Report** button, contradicting the later agreed live-filter / Customer-on-Enter interaction standard.
4. `docs/Testing.md` still required the header to report an **8.0.x SDK** and a compatible .NET 8 SDK, contradicting the corrected current build policy (net8 targets, compatible installed SDK 10.0.400, no restrictive pin).
5. `TECH-DEBT.md` still said SDK drift was resolved by repository-root `global.json`, directly contradicting the later removal of that invalid pin.
6. `docs/ImportWizard.md` said a changed workbook/same cutover “still needs” the controlled replacement workflow even though Replace/Correct is implemented.
7. `docs/ImportWizard.md` listed **Import Run history/details UI** as remaining even though the current code/docs state says it is implemented.
8. `docs/DocumentationAudit.md` contained corrupted duplicate candidate headings caused by earlier broad version replacements: three unrelated entries all labelled alpha.22.6 and two unrelated entries labelled alpha.23.4.1.
9. `Audit-BinTracker.ps1` only checked a small handful of current-version/roadmap terms and a brittle hard-coded count of 21 Markdown files. It did not protect the acceptance checklist, requirement identity, stale SDK statements or contradictory importer state.
10. Final ZIP identity was documented as mandatory but not backed by a repository packaging/verification script.

These findings are why the new requirements register and stronger mechanical gates were added in this candidate.

## Small/permanent requirements recovered and retained

Historical acceptance material contained small behaviors worth keeping permanently rather than relying on memory:

- Login Enter/default behavior, masked passwords and consistent password-eye behavior.
- Keyboard-first Single Entry and Batch Entry shortcuts.
- Single Entry's complete post-save reset/focus behavior.
- Batch draft-line edit/update/remove/recalculation and Esc edit-mode behavior.
- Customer screen Current Position/Recent History usability and no large blank bands.
- Container Type duplicate-name/short-code protection, rename-link preservation, display-order effects, inactive history, Special Floor flag and audit events.
- Business Information header fallback behavior and Business Information audit.
- Market Floor exact operational page/duplex rules, B/Fwd semantics, credit formatting and historical regeneration.
- Import Review no-write behavior, explicit decisions/mappings and resizable/DPI-safe review behavior.

These are represented in `docs/RequirementsAcceptanceRegister.md` even when they originated in an old alpha-specific checklist.

## Post-v1 details recovered from archived roadmaps

The current roadmap had retained shorthand for several post-v1 ideas but had lost useful detail. Recovered details now remain explicit:

- Customer-list-only import sources: names-only, code+name, CSV/XLSX master lists and custom workbooks.
- Explicit import intents: **Customers only**, **Customers + opening balances**, **Full migration**.
- Customer-only mode must reuse matching/normalisation/merge preview but must not require container mapping/B-Fwd/OUT/IN/reconciliation.
- Import Profiles include legacy/custom profiles, a standard BinTracker import template and configurable mappings for other businesses.
- Legacy token aliases can eventually persist in Import Profiles.
- Fuzzy suggestions remain opt-in suggestions only; never automatic merge.
- Custom Report Designer and legacy report-template/layout import remain separate from data import.

## Historical ideas preserved but not silently promoted

A few items appeared in limited early documentation and are therefore recorded as `NEEDS-CONFIRMATION`, not assumed v1 commitments:

- additional Audit Trail screen filtering/export polish;
- automatic application updates;
- pre-beta README screenshot set;
- expanded developer-documentation set.

This distinction prevents both forms of scope loss: silently forgetting an old idea and silently turning a weak historical mention into a firm v1 requirement.

## Current code-state checks used in reconciliation

Static source inspection confirmed:

- report services remain behind Services + EF Core/`IDbContextFactory` boundaries;
- all current CSV-capable reports contain their report-specific CSV audit event;
- product icon/splash/common Form infrastructure exists;
- email/SMS customer flags and `ReminderDelivery` persistence groundwork exist, but provider delivery is not implemented;
- business text identity exists, but business-logo/output-branding implementation remains planned;
- movement correction/reversal workflow remains planned; current movement model does not yet provide the required full linked correction workflow;
- Daily Print Pack service/UI/audit wiring exists and still requires real Windows print acceptance;
- Batch draft crash/power-loss persistence is implemented; operator restart/kill smoke acceptance remains pending.

## Gate changes made by this reconciliation

- Added permanent `RequirementsAcceptanceRegister.md` with stable IDs, scope, status and provenance.
- Rebuilt `TEST-CHECKLIST.md` as an active acceptance checklist instead of an accidental mixture of active and historical build notes.
- Strengthened `Audit-BinTracker.ps1` to validate the register, checklist version, required documents, stale contradictory phrases and key requirement IDs.
- Added `Package-BinTracker.ps1` to create and reopen the ZIP and verify sole root/version/package identity.
- Reconciled stale SDK, importer and technical-debt statements.
- Repaired identifiable corrupted headings in the historical Documentation Audit and documented the repair.
- Restored detailed post-v1 import requirements to the roadmap/register.

## Remaining verification boundary

This environment can statically inspect source and package structure, but it is not the user's Windows production/test machine. `Build-BinTracker.bat` remains the compile/automated-test gate, and manual WinForms/PDF/print/DPI acceptance remains explicitly human-tested rather than being falsely marked complete.
