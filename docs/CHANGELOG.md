# Changelog

## 0.1.0-alpha.3

- Added local login and role-based access foundation.
- Added secure password hashing.
- Added first-run administrator creation.
- Added user administration.
- Added append-only audit trail and administrator audit viewer.
- Added auditing for login, logout, failed login, user creation and user status changes.
- Added backward-compatible SQLite schema upgrade from Alpha 2.

## 0.1.0-alpha.2

- Corrected .NET 8 package compatibility and build issues.

## 0.1.0-alpha.3.2

- Fixed first-run administrator form layout for DPI scaling and smaller displays.
- Added scrolling and a persistent bottom action bar so buttons remain visible.

## 0.1.0-alpha.3.5

- Applied DPI-aware, responsive layouts across the application.
- Rebuilt the dashboard cards to prevent clipped headings, values and subtitles.
- Removed the dashboard's unnecessary horizontal scrollbar.
- Made header text and signed-in details resize safely.
- Updated Users, Add User and Audit Trail windows for high-DPI and smaller displays.
- Added minimum sizes, wrapping and scroll-safe layouts to current application forms.


## 0.1.0-alpha.3.7
- Kept SQLite as the active database for simple development/install.
- Isolated database provider and connection configuration.
- Added provider-neutral database settings.
- Prepared the data layer for a later PostgreSQL multi-user migration.
- Prevented future connection settings from being committed to Git.


## 0.2.0-alpha.1
- Added customer management.
- Added customer reminder contact preferences.
- Added customer audit events.
- Added reminder delivery persistence groundwork.
- Renamed Standard Bin to Blue Bin while preserving Id/history.

## 0.2.0-alpha.2
- Made customer code mandatory and code-first throughout Customer Management.
- Expanded customer search fields and default sort order.
- Added PDF Customer Statements using QuestPDF.
- Added statement-period selection and report audit events.
- Fixed Audit Trail grid sizing at high DPI.

## 0.2.0-alpha.3
- Aligned login action buttons and placed Log in before Cancel.
- Main application now opens maximized.


## 0.2.0-alpha.4
- Fixed clipped Customer Statement Period actions at high DPI.
- Fixed SaveFileDialog/OLE crash by keeping WinForms startup on the STA thread.


## 0.2.0-alpha.5
- Enforced customer-code uniqueness ignoring case.
- Added Account / Cash-COD customer classification.
- Added customer type to the customer workspace/list.
- Aligned Settings action buttons.


## 0.2.0-alpha.6
- Added schema-versioned SQLite upgrades.
- Fixed Alpha 5 SQLite startup syntax error.
- Added unit and integration test projects.
- Added migration regression tests.
- Updated build script to run tests.
- Added Functional Specification, Business Rules, and Testing documentation.


## 0.2.0-alpha.6.1
- Fixed missing xUnit namespaces in new test projects.
- Fixed build script false-success reporting.
- Added batch build/test launcher.
- Removed EF1002 schema-upgrade warnings.
- Removed duplicate WinForms DPI manifest configuration.

## 0.2.0-alpha.6.2
- Added self-service password changes, administrator reset, forced change, lockout/unlock, session information, audit events, and security tests.


## 0.2.0-alpha.6.2.1
- Fixed blank Settings page caused by FlowLayoutPanel/AutoSize interaction.
- Rebuilt Settings using explicit TableLayoutPanel sections.

## 0.2.0-alpha.6.2.2
- UI polish for password/user/settings screens.
- Added administrator manual Lock / Unlock.

## 0.2.0-alpha.6.2.3
- Removed unnecessary Add User vertical scrolling.
- Simplified Users grid to a single Status column and improved sizing.

## 0.2.0-alpha.6.2.4
- Fixed Users toolbar/status clipping.
- Added administrator Change Role workflow with audit logging.

## 0.2.0-alpha.7.0
- Added operational Batch Entry.
- Added transactional MovementService batch save and audit record.
- Added customer-code autocomplete and live customer balances.
- Added movement unit/integration tests.
- Added release test checklist.

## 0.2.0-alpha.7.1
- Added persistent in-memory Batch Entry drafts across navigation.
- Added Current vs With Draft live balance preview.
- Added Enter-on-Quantity/Reference and Ctrl+Enter keyboard workflow.
- Dashboard now displays live saved movement totals.
- Polished User Management colours, status wording and context-sensitive actions.
- Improved Customer recent movement grid on smaller screens.
- Added application version display and Known Issues.

## 0.2.0-alpha.7.2
- Added editable pending batch lines and further responsive UI polish.

## 0.2.0-alpha.7.2.1
- Cleaned the remaining WinForms nullable-reference warnings.

## 0.2.0-alpha.7.2.2
- Fixed the three remaining CS8602 warnings in DataGridView cell-formatting handlers.

## 0.2.0-alpha.7.2.3
- Fixed Customer screen lower-right clipping by removing fixed panel minimum heights and giving Movement History the remaining vertical space.

## 0.2.0-alpha.7.2.4
- Removed unused Customer details height and redistributed it to Current Position and Recent Movement History.

## 0.2.0-alpha.7.2.5
- Tightened Customer layout and redistributed lower-grid space.
- Disabled automatic Batch Entry grid tooltips.
- Quantity now starts blank and requires positive input.

## 0.2.0-alpha.7.2.6
- Rebuilt Customer details layout to eliminate the phantom whitespace.
- Widened movement-history Date, Direction and Entered By columns.
- Added top-right Logout and return-to-login session flow.

## 0.2.0-alpha.7.2.7
- Tightened Customer editor height and widened movement-history columns.

## 0.2.0-alpha.7.2.8
- Fixed shared page-title clipping at scaled DPI settings.
- Added built-in Windows icons to the left navigation.

## 0.2.0-alpha.7.2.9
- Replaced navigation font glyphs with embedded PNG icons.
- Added logout icon.
- Added reusable show/hide password eye controls throughout the application.

## 0.2.0-alpha.7.2.10
- Integrated password eye controls into field styling.
- Fixed Logout button clipping/alignment.
- Replaced Settings navigation artwork with a standard cog icon.

## 0.2.0-alpha.7.2.11
- Approved login/header/navigation polish.

## 0.2.0-alpha.7.2.12
- Applied approved icon artwork and fixed Logout clipping.

## 0.2.0-alpha.7.2.13
- Replaced problematic image rendering for password eye and Logout with DPI-safe custom WinForms drawing.

## 0.2.0-alpha.8.0
- Implemented the full Single Entry manual movement workflow.
- Added manual-movement service persistence and audit logging.

## 0.2.0-alpha.8.0.1
- Fixed Customer action buttons being clipped below the visible details area.

## 0.2.0-alpha.8.0.2
- Cleaned up Single Entry text alignment and removed redundant Ready status.

## 0.2.0-alpha.8.0.3
- Single Entry now fully resets after a successful save.

## 0.2.0-alpha.9.0
- Added Reports hub and two-page duplex Market Floor Sheet.
- Added date-based B/Fwd / Out / In / Total calculation.
- Added Account/Cash front and reverse grouping plus special-container summary.

## 0.2.0-alpha.9.0.1
- Corrected Market Floor Sheet to A4 portrait on both sides.
- Fixed clipped Generate & Open button.

## 0.3.0-alpha.1
- Added configurable Container Type master data and management UI.
- Added SQLite schema migration v7 without changing existing container IDs.
- Market Floor Sheet now uses explicit special-container metadata.

## v0.3.0-alpha.2
- Future-proofed SQLite migration tests by deriving the expected latest schema version.
- Added Container Type master-data migration regression coverage.

## v0.3.0-alpha.3
- Added Business Information master data and report-header integration.
- Added SQLite schema migration v8 and related tests/documentation.

## v0.3.0-alpha.4
- Fixed Business Information Settings button alignment/caption fit.
- Fixed clipped Save/Close buttons in the Business Information dialog.
- Cleared MainForm CA1859 analyzer messages using concrete private UI types.

## v0.4.0-alpha.1
- Added first Excel Import Wizard stage: safe workbook analysis and candidate preview.

## v0.4.0-alpha.2
- Fixed Excel Import analysis build errors caused by ClosedXML cell coordinate API usage.
- Removed nullable SourceCell warning.

## v0.4.0-alpha.3
- Rebuilt the Excel Import Wizard layout to prevent clipped controls and match the approved workflow.

## v0.4.0-alpha.4
- Matched Import Wizard progress styling to the approved design.
- Added View all worksheets and removed duplicate Analyse action.
- Added legacy B/Fwd + IN/OUT snapshot analysis model.

## v0.4.0-alpha.5
- Fixed Import Wizard clipping at the progress indicator, worksheet controls and analysis summary.
- Added unique-customer versus occurrence counts.

## v0.4.0-alpha.6
- Centralised application/build versioning in Directory.Build.props.
- Build-BinTracker.bat now clearly prints the release version at start and completion.

## v0.4.0-alpha.7
- Added Import Wizard duplicate diagnostics dialog and concise warning panel.
- Fixed Business Information Save/Close alignment and increased Address height.
- Audited KNOWN-ISSUES.md and separated technical debt / future enhancements.

## v0.4.0-alpha.8
- Split Excel Import Wizard into Analyse and Map pages.
- Added worksheet Source/Validation/Report/Ignore classification and Source-only customer preview.
- Fixed remaining worksheet/duplicate dialog header clipping.

## v0.4.0-alpha.9
- Fixed Analyse result footer clipping, progress-step subtitle clipping and Map reason truncation.
- Fixed oversized Business Information Close button.

## v0.4.0-alpha.10
- Added Import Wizard Review page and database customer-code matching preview.
- Added Existing/New/Type mismatch/Source conflict safety checks.
- Widened Candidates and Occurrences headers.

## v0.4.0-alpha.10.1
- Fixed Review planner unit-test compilation failure (CS8752).

## v0.4.0-alpha.10.2
- Fixed Import Wizard Map classification display/state loss after Back navigation.

## v0.4.0-alpha.10.3
- Normalised legacy Buyer prefixes into customer identity + container hint and fixed Review status clipping.

## v0.4.0-alpha.11
- Added conservative customer-name/code normalization and explainable match reasons.
- Added legacy Y -> Yellow Bin and Bulk -> Bulk Bin container-hint resolution.
- Reworked Review grid to eliminate normal horizontal scrolling/clipped match fields.

## v0.4.0-alpha.12
- Added Developer Database Backup / Load / Fresh testing tools.
- Added restart-safe SQLite database switching.
- Promoted re-import/idempotency protection to a required pre-Import milestone.

## v0.4.0-alpha.13
- Added authoritative Excel-target balance reconciliation planning and Review preview.

## v0.4.0-alpha.13.1
- Fixed WinForms build failure caused by invalid DataGridViewRow tooltip usage.

## v0.4.0-alpha.13.2
- Fixed Review planner grouping so normalized customer variants consolidate before matching.

## v0.4.0-alpha.13.3
- Fixed Step 3 Review crash caused by untranslatable BalanceService EF Core/SQLite LINQ.
- Added real SQLite BalanceService regression coverage.
- Added post-v1.0 customer-list-only import mode to roadmap.

## v0.4.0-alpha.13.4
- Fixed BalanceService ReadOnlySpan/Contains EF parameter-extraction failure.

## v0.4.0-alpha.13.5
- Fixed Step 3 Review clipping for headers and values.
- Fixed Developer Database Tools vertical clipping.
- Removed xUnit2031 integration-test warnings.

## v0.4.0-alpha.13.6
- Widened Import Review and Developer Database Tools; removed normal horizontal scrolling from Review grids.

## v0.4.0-alpha.14
- Added Blue Bin as the legacy default when no container token is present.
- Unknown explicit container tokens now block reconciliation instead of being guessed.

## v0.4.0-alpha.15
- Added manual mapping of unknown legacy container tokens from Import Review.

## v0.4.0-alpha.15.1
- Fixed missing `containerTokenMappings` field in Import Wizard.

## v0.4.0-alpha.16
- Added editable new-customer confirmation with Create/Skip decisions and reconciliation blocking.

## v0.4.0-alpha.16.1
- Fixed CS0165 in new-customer decision reconciliation.

## v0.4.0-alpha.16.2
- Updated stale fresh-database reconciliation test to supply explicit Create decision.

## v0.4.0-alpha.16.3
- Fixed stale fresh-database reconciliation assertion: explicit Create decision now expects Ready.
- Consolidated per-alpha release-note files into `docs/RELEASE-NOTES.md`; detailed history remains in this changelog.

## v0.4.0-alpha.17
- Added existing-customer match confirmation and override.
- Fixed Developer Database Tools newline rendering.
- Fixed clipped new-customer bulk-action buttons.

## v0.4.0-alpha.18
- Added ImportRuns provenance schema and SHA-256 exact re-import preflight.
- Added Step 4 Import preflight screen.
- Fixed blank inactive existing-customer match display.
- Humanised existing-match decision labels.

## v0.4.0-alpha.18.1
- Fixed clipped Balance Reconciliation area.
- Fixed Step 3 readiness gate preventing advancement to Step 4 after a fully resolved review.
- Centralised Review readiness policy.

## v0.4.0-alpha.18.2
- Fixed Review readiness source ordering that caused CS0841 compilation failure.

## v0.4.0-alpha.18.3
- Fixed root cause of collapsed Step 3 Review grids by disabling AutoSize on the fill-region card.

## v0.4.0-alpha.18.4
- Enlarged Balance Reconciliation and added a full-size viewer.
- Added targeted cutover-math regression tests.
- Made Step 4 workbook-lock/access failures recoverable instead of fatal.

## v0.4.0-alpha.18.5
- Redesigned Step 3 Import Review summary into six metric cards.
- Rearranged Review actions and made the large reconciliation viewer persistently visible.
- Increased the primary reconciliation grid area and clarified formula headers.
- Updated password visibility eye / eye-slash artwork.

## v0.4.0-alpha.18.6
- Simplified reconciliation column headers.
- Fixed metric-card clipping.
- Replaced Unicode Review icons with custom-drawn mockup-matching vector icons.

## v0.4.0-alpha.18.7
- Replaced Review runtime vectors with embedded raster PNG icons matching the approved mockup style.
- Fixed clipped Review action buttons.
- Fixed reconciliation ordering so pending CLAMMS Blue/Bulk/Yellow rows retain container identity and cutover preview maths.

## v0.4.0-alpha.18.8
- Replaced recreated Review icons with artwork extracted from the approved original mockup.
- Matched bold-primary / grey-secondary metric-card typography.
- Added counts to Review actions and widened the large reconciliation action.
- Increased Review wizard width.

## v0.4.0-alpha.18.9
- Simplified Review metric cards.
- Fixed Review action-button/icon clipping.
- Shortened reconciliation action to Open reconciliation.
- Removed three xUnit2031 warnings.
- Fixed Analyse warning hanging-indent layout.

## v0.4.0-alpha.18.10
- Removed duplicate Analyse warning icon.
- Fixed container icon cropping in summary/action UI.
- Standardised secondary grey Review-card metrics.

## v0.4.0-alpha.19
- Enabled real transactional Step 4 Excel import.
- Added live database revalidation before execution.
- Added confirmed new-customer creation.
- Added opening adjustment and cutover OUT/IN persistence.
- Added completed ImportRun/audit persistence.
- Added workbook-change and exact-reimport execution guards.
- Logged remaining importer icon/rounded-tile work as deferred UI polish.

## v0.4.0-alpha.19.1
- Corrected Market Floor handling of import opening adjustments.
- Kept Cash/COD credits in Cash section; Account credits remain separate.
- Restored Market Floor to front + back with three-column reverse layout.
- Added adaptive front-page typography.
- Differentiated opening adjustments in customer statement/history.
- Removed stale Import-disabled copy and duplicate Analyse warning icon.
- Fixed First Run Administrator button alignment.

## v0.4.0-alpha.19.2
- Fixed case-sensitive customer search.
- Fixed async customer grid/detail selection race.
- Increased Market Floor front-page readability.
- Prevented Cash/COD CREDIT value wrapping.

## v0.4.0-alpha.19.3
- Widened Market Floor credit/total columns.
- Prevented CREDIT label wrapping.
- Increased front and reverse Market Floor text sizes.

## v0.4.0-alpha.19.4
- Separated Market Floor regular balances by container.
- Added Bin column to front and reverse sheets.
- Prevented Blue/Yellow/Bulk balances being presented as one physical collection quantity.

## v0.4.0-alpha.19.5
- Made Blue implicit on Market Floor front/reverse.
- Displayed Yellow/Bulk inline with buyer.
- Treated Bulk as a normal operational floor bin.
- Removed dedicated Bin columns to recover width.
- Retuned front font sizing to restore a two-page report.

## v0.4.0-alpha.19.6
- Restored Bulk to configured Special Containers.
- Replaced fixed Market Floor front sizing with row-load-driven font/padding/spacing.
- Added density response for high Yellow-bin days.

## v0.4.0-alpha.19.7
- Widened Market Floor Cash/CREDIT area.
- Increased normal-day front-page size/spacing.
- Added fully dynamic reverse-side density handling.

## v0.4.0-alpha.19.8
- Corrected reverse-side pagination using rendered-line-aware adaptive sizing.

## v0.4.0-alpha.19.8.1
- Audited and reset project documentation against the current codebase.
- Replaced stale Roadmap alpha-progress accumulation with current priority order.
- Cleaned Known Issues and Technical Debt.
- Updated Import Wizard/Re-import Safety/Functional Specification/Testing/README.
- Consolidated release documentation by deleting obsolete per-alpha release-note files; historical details remain in this changelog.

## v0.4.0-alpha.19.9
- Added deterministic test-only Import execution failure injection.
- Added SQLite regression test forcing failure after final SaveChanges and before Commit.
- Proved customer, movements, ImportRun and completion audit all roll back.
- Proved failed exact source remains eligible for retry.
- Reconciled rollback status across Roadmap, Known Issues, Technical Debt, Test Checklist and importer documentation.

## v0.4.0-alpha.19.10
- Added nullable BinMovement.ImportRunId relationship and index.
- Linked all Step 4 generated Adjustment/ExcelImport movements to their ImportRun.
- Added SQLite schema migration V10 with conservative alpha.19.x provenance backfill.
- Added provenance and migration regression tests.
- Reconciled Roadmap, Known Issues, Technical Debt, Test Checklist and importer documentation.

## v0.4.0-alpha.19.11
- Added ImportRun CutoverDate and ReplacesImportRunId metadata.
- Added same-cutover changed-workbook detection and correction comparison.
- Added atomic replacement of only prior ImportRun-linked movements.
- Preserved Manual/Batch movements and customer records.
- Added migration V11 and correction regression coverage.
- Reconciled current importer documentation.

## v0.4.0-alpha.19.11.1
- Fixed same-cutover replacement baseline absorbing post-cutover Manual activity.
- Replacement reconciliation now uses legitimate history strictly before CutoverDate.
- Preserves same-day and later Manual/Batch activity on top of corrected import.
- Strengthened replacement integration regression for same-day and next-day Manual movements.
- Reconciled current-state documentation with corrected replacement semantics.

## v0.4.0-alpha.19.11.2
- Fixed Step 4 preflight omitting CutoverDate.
- Changed-workbook same-cutover state now appears before execution.
- Added explicit Replace / Correct Step 4 action and amber previous-run warning.
- Strengthened dated-preflight replacement regression assertions.
- Reconciled current-state documentation and left manual UI acceptance open.

## v0.4.0-alpha.19.11.3
- Fixed false correction differences caused by `Blue` vs `Blue Bin` display-string keys.
- Correction comparison now keys containers by ContainerTypeId.
- Strengthened regression: one Blue OUT change produces exactly one +1 position change.
- Removed Greek delta notation from correction dialog.
- Widened Replace / Correct Step 4 button.
- Reconciled current-state documentation; manual acceptance remains open.

## v0.4.0-alpha.19.12
- Added Administrator Import Run history/details service and UI.
- Added replacement-chain, SHA-256/source metadata and linked movement detail.
- Added Administrator-only history integration tests.
- Recorded alpha.19.11.3 real-workbook correction smoke test as passed.
- Reconciled current-state documentation; new history UI manual acceptance remains open.

## v0.4.0-alpha.19.12.1
- Persisted immutable correction-difference snapshots on corrected ImportRuns.
- Added migration V12 / `CorrectionChangesJson`.
- Import History now displays previous → corrected → change per customer/container.
- Explicitly labels older replacement runs whose differences were not captured.
- Added migration, execution and history regression coverage.
- Recorded alpha.19.12 Import History UI smoke checks and reconciled current-state docs.

## v0.4.0-alpha.19.12.2
- Fixed Import History heading overlap and lower-grid starvation.
- Widened/readjusted Import History columns and detail layout.
- Added inline Accept match / Override match explanation.
- Added Container Types unsaved-change Save / Discard / Cancel protection.
- Reconciled current-state documentation; manual UI acceptance remains open.

## v0.4.0-alpha.19.12.3
- Audited Customers and confirmed unsaved-change protection was previously missing.
- Added Customer dirty-state snapshots and Save / Discard / Cancel protection across selection, filtering, New Customer, page navigation, logout and application close.
- Added shared explicit Save / Discard / Cancel dialog and applied it to Container Types.
- Prevented Import History Completed metadata from wrapping.
- Closed the customer-unsaved Known Issue and reconciled roadmap/current-state documentation.

## v0.4.0-alpha.19.12.4
- Documentation/planning-only reconciliation against original conversation requirements.
- Reclassified Batch Entry as mostly complete with three focused acceptance items.
- Restored historical/as-of-date and explicit Weekly reporting requirements.
- Restored customer sorting/lifetime totals, Statement view/print, Dashboard chart, scheduled backup, provider-direction and post-v1 requirements.
- Added PostgreSQL readiness audit without introducing a generic Repository layer.
- Added formal testing/audit/development workflow documentation.

## v0.4.0-alpha.20.0
- Began Reports phase.
- Added ledger-derived Historical / As-of-Date Outstanding service.
- Added Reports UI with date/customer/container filters, credits/inactive options and per-container totals.
- Added Outstanding CSV export.
- Added SQLite integration coverage for historical cutoff, container separation, credits and filters.
- Reconciled current-state documentation; PDF/print remains open.

## v0.4.0-alpha.20.0.1
- Restored Market Floor Sheet as the first report on the Reports page.
- Increased Outstanding export button width so `Export CSV` is not clipped at production DPI.

## v0.4.0-alpha.20.0.2
- Changed Outstanding default ordering from Container-first to Customer-first.
- Multi-container customer rows now stay adjacent in configured container order.
- Reduced Outstanding grid viewport so the full card remains visible with an internal scrollbar.
- Added ordering regression assertion and reconciled current-state docs.

## v0.4.0-alpha.20.0.3
- Converted Reports into a compact launcher architecture.
- Kept Market Floor Sheet first and inline.
- Moved Outstanding Containers into a dedicated full report window.
- Added single-instance Outstanding window activation through MainForm.
- Preserved existing historical reporting logic and CSV export.
- Reconciled current-state documentation and roadmap.

## v0.4.0-alpha.20.0.4
- Made Outstanding Containers window responsive to the active monitor working area.
- Reserved reliable filter/action height to prevent clipped controls.
- Result grid now consumes remaining space and grows on larger displays.
- Added laptop/large-monitor acceptance requirements and reconciled current-state docs.

## v0.4.0-alpha.20.0.5
- Outstanding Containers now dynamically sizes the Customer Code column to the longest visible code.
- Added sensible 110 px minimum and 260 px maximum widths so spare report space is used without allowing pathological values to dominate the grid.

## v0.4.0-alpha.20.0.6
- Fixed ineffective Customer Code dynamic sizing caused by relying on DataBindingComplete with manually added DataGridView rows.
- Code and Type columns now resize immediately after each report population.
- Code range: 130–300 px; Type range: 130–220 px.
- Reconciled reporting UI documentation and regression checklist.

## v0.4.0-alpha.20.0.7
- Completed Outstanding Containers with audited landscape PDF generation.
- Added Generate PDF and Generate & Open actions to the responsive report window.
- PDF uses the already-calculated current report result so UI filters and exported presentation cannot silently diverge.
- Reconciled roadmap, known issues, technical debt, functional spec, testing, README, versioning and release notes.
- Normalized stale documentation baseline version 20.0.6.1 to 20.0.7.

## v0.4.0-alpha.20.0.7.1
- Fixed stale `OutstandingContainersReportForm` constructor call in MainForm after PDF service dependency was added.
- Audited all Outstanding report construction sites.
- Formalised milestone-based versioning and the future `alpha.N` / `alpha.N.M` rule.
- Added compile-time dependency-wiring guidance and reconciled current-state documentation.

## v0.4.0-alpha.20.0.7.2
- Split Outstanding report controls into dedicated filter and action rows.
- Prevented Generate PDF / Generate & Open / Export CSV buttons from being hidden by DPI wrapping.
- Recorded mandatory Dashboard design-discussion gate before any Dashboard implementation.
- Reconciled current-state roadmap/spec/testing/tech-debt/release documentation.
