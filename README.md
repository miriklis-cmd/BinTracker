# BinTracker v0.5.0-alpha.5.6.2

BinTracker is a .NET 8 Windows desktop application for tracking reusable container movements, customer/container balances, operational reporting and audited business activity.

## Current functional areas

- Local authentication, roles and user administration.
- Append-only audit trail.
- Customer management with Account / Cash-COD classification.
- Configurable Container Types.
- Customer/Container-Type unsaved-change protection with explicit Save / Discard / Cancel.
- Batch Entry and Single Entry IN/OUT movements.
- Container-specific balances and customer movement history.
- PDF Customer Statements.
- Customer Statement is available from both Customers and Reports with shared save/open workflow.
- Two-page Market Floor report.
- As-of-Date Outstanding Containers query with PDF/CSV export in a dedicated report window.
- Daily Movements report with Today/Yesterday shortcuts, future-date guard, filters and PDF/CSV export.
- Weekly Movements report with Monday-Sunday selection, Daily Detail / Weekly Overview, filters, PDF/CSV export and current-week activity-through-today semantics.
- Integrated full-size Movement History page with inclusive date-range search, responsive columns, reversal scan badges, future-date guard and audited PDF/CSV export.
- Monthly Summary report with selected-month OUT/IN/net totals, customer/container breakdown, filters, PDF and CSV.
- Daily Print Pack combining selected-date Outstanding Summary and physical Movement Detail in one audited PDF.
- Compact Reports launcher architecture: Market Floor inline, detailed reports in dedicated single-instance windows.
- Configurable Business Information/report identity.
- Transactional legacy Excel Import Wizard with Analyse, Map, Review, balance reconciliation and Step 4 execution.
- ImportRun SHA-256 exact-reimport protection.
- Relational ImportRun provenance on generated import movements.
- Changed-workbook/same-cutover correction with explicit comparison and atomic replacement.
- Administrator Import Run history/details with replacement-chain and generated-movement provenance.
- Developer database backup/load/fresh tools for testing.

## Important current limitations

See `KNOWN-ISSUES.md` and `docs/Roadmap.md`.

Most important remaining items include:

- Remaining reporting: Daily Print Pack acceptance and final report consistency/real-world print pass;
- Batch Entry operator acceptance for edit/remove/Enter/Esc navigation cleanup and unfinished-batch recovery;
- dashboard operational pass;
- Business Information branding expansion: logo + shared report/email output identity;
- real Email/SMS reminder delivery;
- controlled movement correction/reversal;
- production backup/restore;
- installer/deployment/security hardening.

## Excel Import

The Import Wizard supports `.xlsm` and `.xlsx` legacy migration.

Current workflow:

1. **Analyse** workbook.
2. **Map** sheets as Source / Validation / Report / Ignore.
3. **Review** customer matches, create/skip decisions and container mappings.
4. Reconcile Excel B/Fwd against current BinTracker balance.
5. **Import** transactionally.

The workbook's B/Fwd is treated as the authoritative cutover opening position. Cutover-day OUT and IN remain real movements. Exact completed-workbook re-import is blocked using SHA-256.

Changed workbooks for an already completed cutover date use the controlled **Replace / Correct** workflow, with persisted correction provenance and atomic replacement of prior ImportRun-linked movements.

## Database

SQLite is currently used for single-PC operation and development. Configuration is under the local application data area.

Do **not** deploy separate SQLite installations as a simultaneous shared multi-user system. A central database architecture is still required for that scenario.

## Developer database testing

Administrators can use:

**Settings → Developer Tools → Developer Database**

to back up the current SQLite test database, stage/load a previous database on restart, or restart with a fresh database.

These tools are intended for development/import testing and are not the final production Backup/Restore workflow.

## Build

From PowerShell:

```powershell
.\Build-BinTracker.bat
```

A valid local build requires successful restore, build and automated tests.

## Documentation

- `docs/Roadmap.md` — current priority plan.
- `KNOWN-ISSUES.md` — active defects/limitations.
- `TECH-DEBT.md` — engineering debt.
- `TEST-CHECKLIST.md` — current acceptance checklist.
- `docs/FunctionalSpecification.md` — functional requirements.
- `docs/BusinessRules.md` — core rules.
- `docs/ImportWizard.md` — importer behaviour.
- `docs/ReimportSafety.md` — import idempotency/correction rules.
- `docs/CHANGELOG.md` — historical release changes.
- `docs/RELEASE-NOTES.md` — current release notes.

## Versioning

The application version is defined in `Directory.Build.props`.

- Interactive report windows use live filter refresh; Customer free-text applies on Enter and no Run Report button is required.


## Requirements source of truth

See `docs/RequirementsAcceptanceRegister.md` for permanent requirement IDs/status/scope and `docs/ReconciliationReport.md` for the alpha.23.5.2 historical reconciliation.
