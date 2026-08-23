## v0.5.0-alpha.5.1

- Corrected the two nullable-flow warnings in Movement History badge painting found by the Windows alpha.5 release-gate build.
- Structurally guards the WinForms-supplied cell style and graphics surface before custom painting; no nullable suppression, null-forgiving operator or warning disable was introduced.
- Preserves the accepted alpha.5 integrated page, responsive columns, badges, tooltips, exports, reversal behaviour and audit semantics unchanged.

## v0.5.0-alpha.5

- Integrated Movement History into the main BinTracker workspace as a full-size page; all existing filter, sort, PDF/CSV, audit and reversal workflows are retained.
- Replaced content autosizing with responsive column allocation: structured fields remain compact, Customer/Status/Notes share surplus width, and readable minimums trigger horizontal scrolling only when the page is genuinely narrow.
- Added single-line green IN, red OUT and amber/orange reversal status badges with full Status/Notes tooltips and selection-aware rendering.
- Added consistent PDF/CSV suggested filenames containing a Windows-safe stable customer code only when an applied customer filter resolves to exactly one customer.
- Added automated filename/sanitization coverage and permanent BT-HIST-002..005 requirements/audit gates.
- Manual Windows/maximized-width/DPI acceptance for alpha.5 remains pending; no such acceptance is claimed by the automated build.

## v0.5.0-alpha.4

- Manual Windows acceptance completed: Jack reported all 8/8 alpha.4 smoke tests passed.
- Selectively merged stronger concurrency/portability foundations found by actual source comparison with the alternate alpha.3.
- Strengthened import and reversal payload identity beyond that alternate implementation and added regression coverage.
- Added schema V14, architecture documentation and expanded BT-ARCH-008..015 auditing while retaining Work's accepted reversal UX and packaging fixes.

## v0.5.0-alpha.3

- Added permanent BT-ARCH-008..015 central-service, concurrency and portability gates.
- Added request-capable user, business clock and client context abstractions while retaining local SQLite adapters.
- Made the reversal database invariant authoritative under races and removed provider SQL from the shared EF model.
- Added derived Movement History reversal status and selection-aware Reverse disabling without modifying original ledger Notes.

## v0.5.0-alpha.1.1

## v0.5.0-alpha.2

- Refined Movement Correction/Reversal authorization after operator smoke review: Administrator and Operator may reverse ordinary Manual/Batch operational movements.
- Viewer remains read-only.
- Opening Adjustment and Excel Import/ImportRun-linked records are excluded from the generic reversal path and reserved for Administrator-controlled adjustment/import correction workflows.
- Added service-layer enforcement, UI visibility/messaging, integration coverage and permanent requirement `BT-CORR-005`.


## v0.5.0-alpha.1.3

- Reissued the unaccepted alpha.1.2 layout candidate as alpha.1.3 after the source audit correctly rejected a nonconforming current Release Notes heading.
- Release Notes now use the exact audited candidate heading format while preserving the reversal-dialog layout fix.


## v0.5.0-alpha.1.2

- Fixed reversal dialog action-row clipping at supported runtime/DPI by allocating explicit reason/action row heights and a larger fixed dialog client area.
- Reconciled historical/current version wording after the alpha.1.1 documentation bump.


- Fixed Batch Entry Esc/reset edit-state resurrection at the DataGridView source: reset now suppresses SelectionChanged, clears selection and clears CurrentCell so CurrentRow cannot immediately reload Update Line.
- Added the dedicated Security, Data Integrity & Code Quality Hardening roadmap workstream immediately after Movement Correction/Reversal and before Branding/Communications.
- Added `docs/SecurityHardeningRegister.md` with permanent BT-SH-001..BT-SH-050 tracking for all external audit findings supplied on 19 August 2026.
- Added BT-SEC-008..011 governance requirements and a per-build hard gate protecting finding completeness/dispositions and roadmap ordering; v1.0 is mechanically blocked while CONFIRMED-V1/REVIEW-V1 findings remain.

## 0.5.0-alpha.1.1

- Fixed the remaining Batch Entry async edit-state race: a late customer lookup can no longer restore Update Line after Esc/Clear has cancelled edit mode.
- Clear Batch now fully resets Customer/Quantity/Reference/Notes/preview and Add to Batch state even when no draft rows remain.
- Normalised recovered-batch action button layout so Continue Batch / Save Batch / Discard Batch use a common fixed row, vertical alignment and text centring.
- Retained the current larger application-icon trial unchanged.

## 0.4.0-alpha.24.2.20

- Fixed Batch Entry Enter/update/remove/Esc editor-state regressions found during operator smoke testing.
- Synchronised programmatic Dashboard navigation with the selected left-nav item.
- Moved Batch Entry status text beneath the pending totals and clarified unfinished-batch recovery across normal close/restart/crash/power loss.
- Restored the previous larger application-icon trial.

## 0.4.0-alpha.24.2.19

- Fixed Batch Entry grid-rebind auto-selection that repopulated Customer/Quantity immediately after Add to Batch.
- Added explicit recovered-batch Continue / Save / Discard decision flow for crash/power-loss recovery, including last-saved time in the recovery summary.
- Added startup-only recovery-state tracking so same-process drafts are not presented as crash recovery.
- Updated the application icon trial to the newly supplied latest hybrid ICO.

## 0.4.0-alpha.24.2.17

- Implemented explicit Batch Entry Esc state handling: cancel edit, clear current entry, then leave to Dashboard while retaining the draft.
- Finalised post-add carry-forward behaviour and Customer focus/reset.
- Added atomic LocalApplicationData Batch Entry draft persistence/restart recovery plus unit tests.
- Replaced the application icon trial with the latest user-supplied hybrid ICO.

## 0.4.0-alpha.24.2.16

- Reserved report sort-indicator width at grid configuration time instead of widening columns when a sort becomes active.
- Prevented sort clicks from shifting neighbouring report columns horizontally.
- Strengthened BT-RPT-017 to cover stable column widths during single- and multi-column sorting.

## 0.4.0-alpha.24.2.15

- Prevented active multi-sort captions (`▲1`, `▼1`, `▲2`, etc.) from wrapping onto a second header line; narrow sorted columns are widened as needed instead, so sorting no longer changes the report-grid header layout.
- Replaced the application icon trial with the newly supplied `New BinTracker-hybrid.zip` icon asset for visual comparison. This remains a user trial, not permanent branding approval.
- Strengthened BT-RPT-017 and the audit gate to require single-line active sort indicators.

## 0.4.0-alpha.24.2.14

- Changed report multi-sort header indicators from thin arrows to filled triangles (`▲1`, `▼1`, `▲2`, etc.) for better visibility while preserving sort priority numbering and comparator behaviour.

# Changelog

## 0.4.0-alpha.24.2.12

- Made report sort direction and multi-sort priority explicitly visible in header captions (`↑1`, `↓1`, `↑2`, `↓2`, etc.) instead of relying on unreliable WinForms native sort glyph rendering.
- Added automatic width headroom for active sort indicators.
- Widened Daily Movements and Weekly Movements Direction columns to prevent indicator clipping.
- Added BT-RPT-017 and audit coverage for the visual sort-state requirement.

## 0.4.0-alpha.24.2.11

- Fixed report Position sorting so CREDIT values use their true negative business balance and OUT values use their positive balance.
- Added typed sort-value selectors to the shared report multi-sort engine.
- Outstanding Containers now sorts Position directly from `OutstandingReportRow.Balance` and Last movement from its typed date.
- Added a defensive generic CREDIT-as-negative numeric fallback and strengthened BT-RPT-016 audit coverage.

## 0.4.0-alpha.24.2.10

- Fixed shared report multi-column sorting to compare numeric quantities/positions numerically instead of lexically.
- Added chronological report-date sorting.
- Removed the obsolete Outstanding Containers-specific sorting implementation.
- Reapply active multi-column sort criteria after report refreshes.
- Strengthened BT-RPT-016 audit coverage for type-aware shared sorting.

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

## v0.4.0-alpha.24.2.10

- Widened Outstanding Containers Balance selector for DPI-safe readability.
- Added trial Shift+click multi-column sorting to Outstanding Containers, retaining sorted grid order in PDF/CSV.
- Recorded Monthly Summary user acceptance.
- Added permanent requirement BT-RPT-015.

## v0.4.0-alpha.24.2.7

- Added explicit Outstanding Containers balance modes: Outstanding only, Credits only, and All non-zero.
- Added integration coverage for credits-only historical positions.
- Added permanent requirement BT-RPT-014.

## v0.4.0-alpha.24.2.6

- Removed the redundant Reports landing-page PDF/CSV/date information bar.
- Rebalanced the viewport-filling Reports layout to give Explore Reports more vertical room, addressing clipped descriptions/buttons at the affected DPI/display size.

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

## v0.4.0-alpha.20.0.7.3
- Fixed Outstanding Position sorting to compare signed numeric balances rather than formatted strings.
- Attached typed report models to DataGridView rows for type-correct interaction.
- Outstanding PDF generation now preserves the current displayed grid row order/sort.
- Added report interaction standard and reconciled roadmap/spec/business/testing/tech-debt/release documentation.

## v0.4.0-alpha.20.0.7.4
- Fixed Outstanding CSV export to preserve current displayed grid row order/sort.
- Reused the same displayed-result snapshot approach as PDF generation.
- Recorded CSV/PDF visible-order consistency as the standard for future reports.
- Reconciled roadmap/spec/business/testing/tech-debt/release documentation.

## v0.4.0-alpha.20.0.8
- Added Daily Movements report service and responsive report window.
- Added Today/Yesterday shortcuts and customer/container/direction/source filters.
- Excluded Opening Adjustments by default with explicit opt-in.
- Added typed Quantity sorting.
- Added PDF/CSV outputs preserving current displayed grid order.
- Added audited Daily Movements PDF generation.
- Added SQLite integration coverage and reconciled all current-state docs.

## v0.4.0-alpha.20.0.8.1
- Fixed Daily Movements Generate & Open ampersand rendering.
- Widened Direction selector for All directions at production DPI.
- Added opt-in Notes column to Daily Movements PDF.
- Tightened default Daily PDF spacing to improve page utilisation.
- Recorded Notes-in-print business rule and reconciled all current-state docs.

## v0.4.0-alpha.20.0.8.2
- Removed Opening Adjustment from the Daily Movements Source dropdown.
- Renamed the checkbox to Include opening adjustments.
- Made the adjustment inclusion toggle the single explicit adjustment-control path.
- Reconciled report UX rules and full current-state documentation.

## v0.4.0-alpha.20.0.8.3
- Fixed Daily Movements action buttons being clipped after alpha.20.0.8.2.
- Split report controls into auto-sized Filters / Options / Actions rows.
- Removed dependency on a fixed/minimum controls-card height for button visibility.
- Reconciled the full documentation/audit set and preserved the Dashboard design gate.

## v0.4.0-alpha.20.0.8.4
- Renamed Daily Movements option to Include notes in exports.
- Applied the Notes option consistently to both PDF and CSV.
- CSV now conditionally includes/omits its Notes column and values.
- Reconciled full documentation and preserved the Dashboard design gate.

## v0.4.0-alpha.21
- Added first-class Weekly Movements report.
- Added Monday-Sunday week boundaries and This Week/Last Week shortcuts.
- Added detail and customer/container OUT/IN/Net summary views.
- Added customer/container/source filters and optional opening-adjustment inclusion.
- Added CSV export with optional Notes.
- Added SQLite integration coverage.
- Added post-v1 Windows UI v2 / WinUI 3 evaluation milestone.
- Reconciled full documentation and preserved the Dashboard design gate.


## v0.4.0-alpha.21.1
- Completed Weekly Movements PDF support with Generate PDF and Generate & Open.
- Weekly PDF now follows the selected Movement Detail or Customer / Container Summary tab and preserves the current grid order.
- Added independent Include notes in PDF and Include notes in CSV options.
- Replaced ambiguous Week containing wording with Select date and an explicit resolved Monday-Sunday Week range.
- Widened the weekly Date column and added result-driven customer-code sizing in detail and summary grids.
- Added audited WEEKLY_MOVEMENTS_REPORT_GENERATED events including week, view, totals, Notes option and output filename.
- Expanded the mandatory Dashboard design gate to compare WinForms v1 with future WinUI 3 v2, explicitly including the Reports launcher and individual report screens as reference UI.
- Reconciled roadmap, functional specification, test checklist and audit coverage.

## v0.4.0-alpha.21.2
- Fixed Weekly Generate & Open literal ampersand rendering.
- Renamed Weekly tabs to Daily Detail and Weekly Overview.
- Formalised weekly Customer/Container OUT/IN/Net overview inside the existing Weekly report.
- Made CSV, like PDF, export the currently selected report tab and current sort order.
- Disabled detail Notes options while Weekly Overview is selected.
- Reconciled the complete documentation/audit set and retained future WinUI 3 Dashboard/Reports launcher/report-window evaluation requirements.

## v0.4.0-alpha.21.3
- Unified Weekly Notes export option across PDF and CSV.
- Prevented future Weekly report selection and future movement leakage.
- Added current-week activity-through-date semantics.
- Switched Weekly Container filter from Outstanding totals to authoritative Container Type master data.
- Preserved inactive Container Types for historical filtering with explicit inactive labels.
- Added future-date integration regression coverage and reconciled full documentation.

## v0.4.0-alpha.21.4
- Prevented Daily Movements date selection after today.
- Added service-level future-date clamping for Daily Movements.
- Added integration regression coverage for future Daily report dates.
- Added Business Information logo/custom-header/generated-output branding design milestone.
- Reconciled full documentation/audit state.

## v0.4.0-alpha.21.4.1
- Fixed Weekly Movements integration-test compile failure caused by referencing an internal concrete service.
- Test now independently calculates expected Monday week start and verifies the public result.
- Reconciled test/audit/version documentation.

## v0.4.0-alpha.21.5
- Completed historical roadmap reconciliation.
- Promoted Movement Correction, Branding and Email/SMS Communications into the explicit pre-v1 execution sequence.
- Restored/confirmed Batch Entry recovery/polish, customer analytics, statement workflow, remaining reports, scheduled backups, audit coverage and release-discipline requirements.
- Added RoadmapCoverageMatrix.md.
- Retained post-v1 WinUI 3, customer portal, barcode scanning and multiple depots.


## v0.4.0-alpha.21.5.1
- Audited every Markdown file against current implementation and roadmap state.
- Fixed stale current-state documentation for Weekly Movements, Notes export semantics, version references and importer status.
- Reconciled roadmap ordering/duplication and clarified existing Default Report Header vs planned logo/shared branding.
- Added stronger documentation/version consistency rules while preserving historical changelog truth.


## v0.4.0-alpha.21.5.2
- Made the full code/state + all-Markdown audit a mandatory gate for every packaged build.
- Added explicit Roadmap Coverage, version, spec, Known Issues, Tech Debt, test and release-document reconciliation requirements.
- Added a permanent per-build checklist and rolling Documentation Audit requirement.


## v0.4.0-alpha.22
- Added Movement History date-range report.
- Added customer/container/direction/source filters and optional Opening Adjustment inclusion.
- Added authoritative active/inactive Container Type filtering.
- Added future-date guards and range normalization.
- Added Last 7 Days / Last 30 Days / This Month shortcuts.
- Added audited PDF and CSV export preserving visible grid order and shared Notes option.
- Added SQLite integration coverage.
- Completed mandatory full Markdown/current-state audit and reconciled roadmap coverage.


## v0.4.0-alpha.22.1
- Removed Run Report buttons from interactive report windows.
- Added automatic refresh for date/dropdown/result-affecting checkbox filters.
- Added Customer-on-Enter refresh while avoiding per-keystroke database queries.
- Fixed Movement History This Month button sizing.
- Established live-filter behaviour as the standard for future interactive report windows.
- Completed mandatory full documentation/current-state audit.


## v0.4.0-alpha.22.2
- Fixed Weekly Movements wrapped-row action-button clipping.
- Added visible Enter-to-search cues to interactive report Customer searches.
- Integrated supplied BinTracker product icon and restrained sidebar logo.
- Documented product-vs-business branding separation.
- Completed mandatory full documentation/current-state audit.


## v0.4.0-alpha.22.3.1
- Replaced Weekly Movements auto-sized Panel control-card boundary with an auto-sizing TableLayoutPanel.
- Removed brittle fixed action-row height workaround.
- Fixed summary-row overlap when Weekly filters wrap at laptop width/DPI.
- Completed mandatory full documentation audit.


## v0.4.0-alpha.22.3.2
- Centralized WinForms application icon behaviour in `BinTrackerForm`.
- Applied BinTracker icon automatically to Login, reports, import/admin dialogs and other Forms.
- Fixed pre-login taskbar/title-bar icon.
- Reworked sidebar product-logo layout so image and wordmark cannot overlap.
- Completed mandatory full documentation/current-state audit.

## v0.4.0-alpha.22.3.3
- Removed the Weekly Movements dead vertical space so the report grid receives the available height.
- Converted the sidebar product mark to a circular transparent asset with a light contrast ring for the navy navigation background.
- Fine-tuned sidebar BinTracker wordmark alignment with the product mark.
- Added a real BinTracker startup splash screen while startup/database initialisation runs.
- Kept splash/login/report/admin windows on the shared `BinTrackerForm` icon path.
- Completed mandatory source/documentation/current-state audit for this patch.


## v0.4.0-alpha.22.6
- Fixed clipped BinTracker sidebar wordmark.
- Slightly widened sidebar, reduced logo column and wordmark size, and adjusted alignment.
- Completed mandatory full documentation/current-state audit.


## v0.4.0-alpha.22.6
- Customer Statement workflow upgraded from save-only generation.
- Added **Generate PDF** and **Generate & Open** choices.
- Generate & Open creates the statement in BinTracker's temporary statement area and opens it with the Windows default PDF application for immediate viewing/printing.
- Statement date pickers now prevent future dates.
- Completed mandatory full documentation/current-state audit.


## v0.4.0-alpha.22.6
- Added Customer Statement to the Reports launcher.
- Added dedicated searchable customer-selection window for statements.
- Extracted shared `CustomerStatementWorkflow` used by Customers and Reports.
- Preserved Generate PDF / Generate & Open behaviour while eliminating duplicated orchestration.
- Marked Customer Statement operational workflow complete in the reporting roadmap.
- Completed mandatory full documentation/current-state audit.


## v0.4.0-alpha.22.6.4
- Fixed WinForms compile failure caused by `FindForm() ?? this` mixing `Form` and `CustomersView`.
- Normalised Customer Statement workflow owner to `IWin32Window`.
- Completed mandatory full documentation/current-state audit.


## v0.4.0-alpha.22.6.4
- Fixed Customer Statement bottom action bar: full Customer Statement label, consistent button sizing, and bottom-right alignment.
- Mandatory documentation/current-state audit completed.


## v0.4.0-alpha.23
- Added Monthly Summary report with calendar-month OUT/IN/Net totals.
- Added This Month / Last Month shortcuts and future-month/current-month guards.
- Added customer/container/source filters and optional Opening Adjustments.
- Added typed numeric sorting, audited PDF, Generate & Open and CSV preserving visible order.
- Added SQLite integration coverage while keeping report architecture provider-neutral for PostgreSQL.
- Updated Reports launcher and marked Monthly Summary implemented in the roadmap.
- Completed mandatory full documentation/current-state audit.


## v0.4.0-alpha.23.1
- Fixed Monthly Summary integration-test fixtures/setup found by the Windows automated test run.
- Kept production current-month activity-through-today behaviour unchanged.
- Completed mandatory documentation/current-state audit.


## v0.4.0-alpha.23.4.1
- Added audit events for CSV exports from every current CSV-capable report.
- Added shared CSV audit helper with explicit warning if audit persistence fails after file creation.
- Added report/filter/date/row-count/filename context without storing CSV contents in audit events.
- Widened Monthly Summary month/year picker.
- Completed mandatory full documentation/current-state audit.


## v0.4.0-alpha.23.4.1
- Removed the invalid SDK 8.0.100 `global.json` pin introduced in alpha.23.3.
- Restored use of the compatible installed SDK (10.0.400 on the development PC).
- Fixed BAT exit-code handling so restore/build/test failures cannot falsely report BUILD SUCCESSFUL.
- Retained stale build-server cleanup, disabled node/server reuse and conservative parallelism.
- Completed mandatory full documentation/current-state audit.


## v0.4.0-alpha.23.5.1
- Full audit corrected stale current-state documentation left in the alpha.23.5 package.
- Reconciled README, Known Issues, Roadmap, Coverage Matrix and Audit Coverage with implemented reports.
- Added future-date UI guards to inline Market Floor and Daily Print Pack selectors.
- Added `Audit-BinTracker.ps1` and wired it into Build-BinTracker.bat before restore/build/test.
- Made exact ZIP/root/version/current-document identity an explicit release-blocking package gate.
\n\n## v0.4.0-alpha.23.5.2\n- Performed historical requirements reconciliation using current conversation context plus 166 archived BinTracker ZIPs.\n- Added permanent Requirements & Acceptance Register and Reconciliation Report.\n- Rebuilt active Test Checklist and removed contradictory historical-alpha blocks after migrating permanent behaviors.\n- Corrected stale SDK/global.json, ImportWizard and Technical Debt contradictions.\n- Repaired identifiable corrupted DocumentationAudit candidate headings.\n- Restored detailed post-v1 customer-list/import-intent/Import-Profile requirements.\n- Strengthened source audit and added mechanical ZIP package verifier.\n


## v0.4.0-alpha.24.2.1
- Redesigned Reports launcher to approved Quick Reports + Explore Reports mock-up.
- Embedded the exact approved report-card icon artwork from the mock-up.
- Added compact 3×2 Explore Reports grid and two side-by-side Quick Report cards.
- Added Reports page subtitle to the main header.
- Preserved report generation/filter/date behavior.
- Recorded Containers-in-left-nav as a pending decision rather than silently changing navigation.
- Completed mandatory source/current-state/documentation/version/package audit.


## v0.4.0-alpha.24.2.1
- Promoted Containers to the main left navigation directly below Customers.
- Added permission-aware read-only Container Types access for Operator/Viewer roles.
- Kept Container Type mutations Administrator-only.
- Removed Container Types from the Settings administration button group.
- Added navigation unsaved-change protection for embedded Container Types management.
- Added dedicated Containers navigation icon.


## v0.4.0-alpha.24.2.1
- Fixed false audit failure for BT-CT-005 caused by checking an undefined PowerShell variable.
- Audit now validates BT-CT-005 and BT-UI-013 against the parsed requirements-register IDs.
- Includes the alpha.24.2 Reports layout correction and alpha.24.1 Containers navigation change.

## v0.4.0-alpha.24.2.2
- Reconciled BT-CT-005 Containers navigation/permission requirements with current source and documentation.
- Removed stale active documentation claiming Containers remained in Settings or was still pending a navigation decision.
- Strengthened the permanent audit gate to reject those known Containers-state contradictions.
- Preserved parsed-register validation for BT-CT-005 and BT-UI-013.

## v0.4.0-alpha.24.2.3

- Corrected Outstanding Containers and Daily Movements report filters to use configured Container Types master data instead of today's Outstanding totals.
- Preserved inactive Container Types in report filters for historical reporting.
- Converted Explore Reports `Open` footer text to actual blue buttons.
- Removed the broken font-dependent Generate PDF pseudo-icon.
- Removed duplicate host scrolling from the Reports landing page to eliminate the stray horizontal scrollbar.
- Added permanent audit coverage for report container-filter master-data semantics.



## v0.4.0-alpha.24.2.5

- Replaced the Reports landing-page AutoScroll/viewport-fitting approach with a viewport-filling TableLayout that allocates remaining height between Quick Reports and Explore Reports.
- Removed the normal Reports-page scrollbar rather than compensating for scrollbar-induced client-size changes.
- Custom-painted report action icon/caption pairs as single-line content to prevent DPI wrapping and glyph clipping.
- Increased Explore Open button/footer height so the full word `Open`, including its descender, remains visible.


## v0.4.0-alpha.24.2.4

- Reworked Reports scrolling to fit content to the live client viewport and prevent the residual horizontal scrollbar at DPI-scaled sizes.
- Removed report-card minimum widths that could force horizontal overflow.
- Added vector-drawn document icons to Generate PDF buttons.
- Replaced text-arrow glyphs on Generate & Open / Open actions with vector-drawn external-link icons.
- Added permanent requirement and audit coverage for Reports viewport/action-icon behaviour.
