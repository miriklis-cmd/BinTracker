# BinTracker Functional Specification

## Customer Management

- BT-CUST-001: Every customer must have a customer code.
- BT-CUST-002: Customer code is the primary visible customer identifier.
- BT-CUST-003: Customer codes must be unique without regard to case.
- BT-CUST-004: Customer codes are normalised to uppercase.
- BT-CUST-005: Customers are classified as Account or Cash / COD.
- BT-CUST-006: Customers can be deactivated/reactivated without destroying history.
- BT-CUST-007: Customer balances are maintained separately per Container Type.
- BT-CUST-008: Unsaved customer edits must not be silently discarded; navigation away from dirty data requires Save / Discard / Cancel.

## Containers

- BT-CONT-001: Blue Bin is the default normal bin type.
- BT-CONT-002: Container types are data-driven and administrator-manageable.
- BT-CONT-003: Balances are maintained independently per container type.
- BT-CONT-004: Container types can be marked as Special Floor Report Containers.

## Movements

- BT-MOVE-001: IN means Returned.
- BT-MOVE-002: OUT means Taken.
- BT-MOVE-003: A customer may have a credit balance.
- BT-MOVE-004: Batch Entry supports separate IN and OUT workflows.
- BT-MOVE-005: A batch contains one movement direction.
- BT-MOVE-006: Batch Entry can contain multiple customers and container types.
- BT-MOVE-007: Saving a batch is transactional.
- BT-MOVE-008: Saved batches are audited.
- BT-MOVE-009: Corrections/reversals must preserve the original movement and audit trail.
- BT-MOVE-010: Administrator and Operator may reverse ordinary Manual/Batch operational movements; Viewer cannot.
- BT-MOVE-011: Generic reversal must reject Opening Adjustments and Excel Import/ImportRun-linked movements; those use Administrator-controlled adjustment or Replace / Correct workflows respectively.
- BT-MOVE-012: Movement History shows derived reversal Status on original and reversal rows and disables Reverse when the selected row is already reversed or is itself a reversal.
- BT-MOVE-013: Eligible ordinary movements can be corrected by an atomic append-only neutraliser plus corrected replacement covering date/customer/container/direction/quantity/reference/notes.
- BT-MOVE-014: Whole persisted Batch Entry correction changes the common date and/or direction for every line, uses MovementBatch identity and never partially succeeds.
- BT-MOVE-015: A correction neutraliser uses the original movement date; the replacement uses the corrected date so day/week/month history is moved rather than merely offset today.
- BT-MOVE-016: Operator corrections/reversals take effect immediately and await persistent Administrator acknowledgement; acknowledgement is review, not approval.
- BT-MOVE-017: Operational Daily/Weekly/Monthly, customer-recent, statement and Market Floor datasets represent a correction once through its replacement after persisted-lineage suppression of the consumed original and correction neutraliser. Movement History/Audit retain all three roles; ordinary reversal reporting is unchanged.
- BT-MOVE-018: Correction replacements may themselves be corrected without a maximum chain depth. Movement History retains every lineage relationship, including simultaneous replacement and later-corrected roles, while only the latest replacement remains operationally effective and eligible.
- BT-MOVE-019: Audit Trail review presentation explicitly distinguishes Needs review, Reviewed and not-applicable/blank and provides All/Needs review/Reviewed filtering.
- BT-MOVE-020: Mark Selected Reviewed is available only for an unreviewed, review-required Operator correction/reversal selected by an Administrator. Review evidence includes reviewer and UTC timestamp, acknowledgement is audited, and duplicate acknowledgement is rejected; authorization remains enforced by the service boundary.
- BT-MOVE-021: Audit Trail detail actions are context-sensitive. View Batch Detail is available only for events backed by authoritative persisted MovementBatch detail; future supported entity types route to their authoritative detail surface and events without meaningful detail expose no enabled action.
- BT-MOVE-023 / BT-AUD-013: Administrator sessions require a persistent, non-blocking review infobar across main navigation whenever Operator correction/reversal reviews are outstanding. It shows the current count, explains that Operator movement changes require review, opens the pending Audit Trail set, refreshes after review-state changes and disappears at zero. It is Administrator-only, does not block operations, and supplements rather than replaces the login popup. State/count/navigation contracts are presentation-independent for WinForms and future WinUI 3.
- BT-MOVE-024: Movement History synchronizes its initial logical selection after loading/sorting results, and Reverse/Correct Selected eligibility is recalculated consistently from that selected movement.
- BT-MOVE-025: Whole-batch date/direction controls automatically select a correction field when its proposed value differs from persisted state and clear it when the proposed value returns to persisted state. Manual unticking remains effective until that proposed value changes again. Checkbox selection alone is insufficient: at least one selected value must differ semantically, otherwise no correction artifacts or audit event may be written.
- BT-AUD-010/014: Correction/reversal detail and an exact single-event review acknowledgement resolve the referenced audit event's persisted lineage and explain only actual before/after field differences plus original/neutraliser/replacement IDs. Invalid identity fails closed. Esc returns detail to Audit Trail and Audit Trail to BinTracker.

## Dashboard

- BT-DASH-001: Dashboard shows today’s Returned and Taken quantities.
- BT-DASH-002: Dashboard shows current outstanding positions.
- BT-DASH-003: Dashboard identifies customers/positions requiring attention.
- BT-DASH-004: Attention items should be actionable/drillable rather than only a headline count.
- BT-DASH-005: Dashboard rules must be based on explicit business thresholds/ageing rules.

## Security and Audit

- BT-SEC-001: Login events are audited.
- BT-SEC-002: User administration is audited.
- BT-SEC-003: Customer creation/change/status events are audited.
- BT-SEC-004: Report generation is audited.
- BT-SEC-005: Saved movement history is not silently deleted or overwritten.
- BT-SEC-006: Administrative actions are role-restricted.
- BT-SEC-007: Credentials/secrets for external providers must not be stored in plain text.

## Reporting

- BT-PRINT-001: Daily Print Pack contains Outstanding Summary and Movement Detail.
- BT-PRINT-002: Customer statements show opening position, movements, running position and closing position.
- BT-PRINT-003: Outstanding Containers report shows current position by customer/container.
- BT-PRINT-004: Daily Movements report shows selected-day movement detail.
- BT-PRINT-005: Movement History supports date-range/customer/container/source filters.
- BT-PRINT-006: Monthly Summary provides monthly OUT, IN and net movement reporting.
- BT-PRINT-007: Market Floor Sheet is a two-page front/reverse operational report.
- BT-PRINT-008: Market Floor Blue is implicit; non-standard regular containers are explicit.
- BT-PRINT-009: Special Floor Report Containers use the dedicated special section.
- BT-PRINT-010: Import opening adjustments contribute to opening/B/Fwd reporting and are not physical daily OUT/IN.

## Reminders / Communications

- BT-COMM-001: Customers can independently allow Email reminders and SMS reminders.
- BT-COMM-002: Customer opt-out overrides automatic reminder sending.
- BT-COMM-003: Reminder delivery attempts record channel, destination, status, provider response and relevant outstanding snapshot.
- BT-COMM-004: Failed sends can be retried safely without accidental duplicate sends.
- BT-COMM-005: Reminder sends/runs are auditable.
- BT-COMM-006: Provider credentials are administrator-configured and securely stored.

## Migration

- BT-IMPORT-001: Excel brought-forward positions establish authoritative cutover opening position.
- BT-IMPORT-002: `(Y)` maps to Yellow Bin for the legacy profile.
- BT-IMPORT-003: `(Bulk)` maps to Bulk Bin for the legacy profile.
- BT-IMPORT-004: `(Chep)` maps to CHEP Pallet where configured/resolved.
- BT-IMPORT-005: Unprefixed legacy customer rows map to Blue Bin.
- BT-IMPORT-006: Unknown explicit container tokens must be resolved, not guessed.
- BT-IMPORT-007: Import execution is transactional.
- BT-IMPORT-008: Exact completed-workbook re-import is blocked.
- BT-IMPORT-009: Changed-workbook/same-cutover correction must be explicit and must not duplicate prior imported movements.
- BT-IMPORT-010: Import-generated movements must link relationally to the Import Run that created them; non-import movements remain unlinked.
- BT-IMPORT-011: Same-cutover correction must calculate the corrected import from pre-cutover legitimate history and preserve same-day/later non-import activity on top.
- BT-IMPORT-012: Step 4 must surface changed-workbook/same-cutover correction before execution with an explicit Replace / Correct action; execution-time rejection alone is not an acceptable workflow.
- BT-IMPORT-013: Correction comparisons must use resolved configured container identity, not legacy/display container strings.
- BT-IMPORT-014: Administrators must be able to inspect Import Run provenance, replacement relationships and generated movement records through a read-only history UI.
- BT-IMPORT-015: A corrected ImportRun must persist the exact resolved customer/container difference snapshot before the prior generated rows are removed, so replacement intent remains auditable later.

## Backup / Recovery

- BT-OPS-001: Production data can be backed up safely.
- BT-OPS-002: Restore requires explicit confirmation and database validation.
- BT-OPS-003: Upgrades protect existing data and include recovery guidance.

## Central service and concurrency

- BT-ARCH-008..015: All production business code is designed for multiple authenticated remote users executing concurrently through a central service backed by PostgreSQL.
- The current local SQLite deployment remains supported until the server/API exists; its desktop session, device and filesystem adapters must not leak into business contracts.
- Central enablement requires request-scoped authenticated identity, client metadata, configured business time, database-enforced invariants, idempotent retryable commands and content-based file transport.


- BT-UI-006: Editable Container Type master data must warn before navigation/close discards unsaved changes and offer Save / Discard / Cancel.
- BT-IMPORT-016: Import History must keep provenance, correction changes and linked movement data readable at supported desktop sizes/DPI.


- BT-CUSTOMER-009: Customer editor changes must never be silently discarded. Selection, filtering, New Customer, page navigation, logout and application close must offer explicit Save / Discard / Cancel.
- BT-UI-007: Unsaved-change prompts use explicit action labels rather than Yes / No where the actions are Save / Discard / Cancel.

## Historical Querying

- BT-HIST-001: BinTracker can calculate outstanding position by customer/container **as of a selected historical date** from the movement ledger.
- BT-HIST-002: Daily, Weekly and Monthly operational query/reporting are explicit product requirements.
- BT-HIST-003: Quick operational periods include today/yesterday, selected week and current/previous month.

## Customer Analytics

- BT-CUST-010: Customer lists support useful sorting by code/name, outstanding, credit and last movement.
- BT-CUST-011: Customer detail/reporting can expose lifetime OUT/Taken and IN/Returned totals where operationally useful.

## Batch Entry Acceptance

- BT-BATCH-001: Successful Add to Batch clears Customer/Quantity/Reference/Notes and customer preview, returns focus to Customer, and carries Movement Date / Batch Type / Container Type forward.
- BT-BATCH-002: Esc behaviour is explicit and ordered: cancel and clear current draft-line edit; otherwise clear current unsaved entry fields; otherwise exit Batch Entry to Dashboard, always retaining pending draft lines and synchronising navigation state.
- BT-BATCH-003: Draft survival covers navigation/logout and normal close/process restart/crash/power loss through a LocalApplicationData recovery file. A startup-recovered draft requires explicit Continue / Save / Discard choices in that visual order rather than silent resume; successful Save, Clear or confirmed Discard removes recovery state. Enter while editing updates rather than duplicates, and Update/Remove return the editor to clean Add mode.

## Statement Workflow

- BT-PRINT-011: Customer Statement supports an operator flow to generate, view/open and print the PDF.

## Dashboard Visualisation

- BT-DASH-006: Dashboard includes useful visual trend/container reporting where it improves operational understanding; configured Dashboard Colour is presentation metadata only.

## Backup Scheduling

- BT-OPS-004: Production deployments support scheduled automatic backup with retention and recovery verification.

## Central Database Readiness

- BT-OPS-005: Before multi-user central deployment, perform a PostgreSQL readiness audit covering provider-specific SQL/migrations, connection handling, backup tooling and integration tests.

## Communications Provider Direction

- BT-COMM-007: Email delivery targets Google Workspace integration unless the business deliberately changes provider.
- BT-COMM-008: SMS delivery targets Texto integration unless the business deliberately changes provider.
- BT-COMM-009: Automatic reminder scheduling supports the agreed operational direction of contacting customers owing empty bins by Friday or earlier, subject to configurable policy.

## Outstanding / Historical Reporting

- BT-PRINT-012: Outstanding reporting supports an As-of Date and calculates end-of-date position from movements dated on or before that date.
- BT-PRINT-013: Historical outstanding positions remain separate by configured Container Type.
- BT-PRINT-014: Outstanding reporting supports customer and container filtering and may optionally include credit positions.
- BT-PRINT-015: Future-dated movements never affect an earlier As-of-Date result.


## Report UI Architecture

- BT-REPORT-UI-001: The Reports page is a compact launcher as the report catalogue grows.
- BT-REPORT-UI-002: Market Floor Sheet remains directly accessible inline because it is the primary daily operational report.
- BT-REPORT-UI-003: Filter-heavy/data-grid reports open as integrated main-workspace pages under the Reports hub; Movement History uses the same host and also provides operational reversal actions.
- BT-REPORT-UI-004: Only one live instance of a given report window should exist per MainForm session; reopening brings the existing window forward.


- BT-REPORT-UI-005: Integrated detailed report pages size themselves from the available main-workspace area within sensible minimum/maximum bounds.
- BT-REPORT-UI-006: Report filter/action controls must remain fully visible; the result dataset consumes remaining window space and resizes with the window/monitor.

- BT-REPORT-UI-007: Report customer-code columns dynamically size to the longest visible customer code, with sensible minimum and maximum widths so codes remain readable without crowding other report data.

- BT-REPORT-UI-008: Outstanding report Code and Type columns resize from the currently visible result set after each report run, using sensible minimum/maximum widths.


## Dashboard Design Gate

- BT-DASH-DESIGN-001: Dashboard implementation must not begin until the operator and developer review alternative layouts, charts, drill-through behaviour, exception/attention concepts and forecasting hooks together.
- BT-DASH-DESIGN-002: Dashboard design must consider both laptop and large-monitor operation.
- BT-DASH-DESIGN-003: Forecasting/ML hooks are future-facing derived analytics and must never modify authoritative movement/balance records.
- BT-DASH-DESIGN-004: The Dashboard design discussion must explicitly compare what is appropriate for WinForms v1 versus what a future WinUI 3 v2 could materially improve.
- BT-DASH-DESIGN-005: The WinUI 3 discussion must use both the current Reports launcher and representative individual report pages as reference screens, including report discovery/navigation, responsive layout, filters, grids, exports and visual hierarchy.


- BT-REPORT-UI-009: Numeric report columns sort numerically even when their display text includes labels such as OUT/CREDIT.
- BT-PRINT-016: Outstanding Containers PDF preserves the currently displayed grid row order so operator-selected sorting is reflected in the printable report.


- BT-EXPORT-001: Outstanding Containers CSV export preserves the currently displayed grid row order/sort.
- BT-EXPORT-002: Printable/exported report outputs use the same visible dataset ordering so CSV and PDF do not silently disagree with the operator's on-screen view.


## Daily Movements Report

- BT-REPORT-DAILY-001: Query movement rows for one selected MovementDate.
- BT-REPORT-DAILY-002: Provide Today and Yesterday shortcuts.
- BT-REPORT-DAILY-003: Filter by customer, container, IN/OUT direction and movement source.
- BT-REPORT-DAILY-004: Opening Adjustment rows are excluded by default and can be explicitly included.
- BT-REPORT-DAILY-005: Show customer/type/container/direction/quantity/source/reference/notes/entered-by detail and per-container OUT/IN totals.
- BT-REPORT-DAILY-006: Quantity sorts numerically.
- BT-REPORT-DAILY-007: PDF and CSV preserve the current displayed grid ordering.
- BT-REPORT-DAILY-008: PDF generation is audited.


- BT-REPORT-DAILY-009: Daily Movements provides an optional **Include notes in exports** setting; Notes are omitted from both PDF and CSV by default.
- BT-REPORT-DAILY-010: Daily report action labels and filter values must remain fully readable at supported DPI, including literal `Generate & Open` and `All directions`.


- BT-REPORT-DAILY-011: The Daily Movements Source selector contains normal entry origins only; Opening Adjustment is not offered as a Source choice.
- BT-REPORT-DAILY-012: Opening Adjustment visibility is controlled solely by the explicit **Include opening adjustments** option.


- BT-REPORT-DAILY-013: Daily Movements lays out core filters, report options and report actions in separate auto-sized rows so DPI/wrapping cannot hide action buttons.

- BT-REPORT-DAILY-014: The Include notes in exports setting applies consistently to PDF and CSV.


## Weekly Movements Report

- BT-REPORT-WEEKLY-001: A selected week is Monday through Sunday.
- BT-REPORT-WEEKLY-002: Provide This Week and Last Week shortcuts.
- BT-REPORT-WEEKLY-003: Show **Daily Detail** and **Weekly Overview** views.
- BT-REPORT-WEEKLY-004: Summary shows OUT, IN and net movement (OUT minus IN).
- BT-REPORT-WEEKLY-005: Filter by customer, container and normal entry source.
- BT-REPORT-WEEKLY-006: Opening adjustments are excluded by default and included only explicitly.
- BT-REPORT-WEEKLY-007: CSV exports the selected view, preserves its current grid order, and optionally includes Notes for Daily Detail.
- BT-REPORT-WEEKLY-008: The date selector resolves explicitly to and displays its Monday-Sunday week; the UI labels the input Select date rather than the ambiguous Week containing.
- BT-REPORT-WEEKLY-009: Weekly Movements supports Generate PDF and Generate & Open.
- BT-REPORT-WEEKLY-010: Weekly PDF uses the currently selected Daily Detail or Weekly Overview tab and preserves that grid's current sort order.
- BT-REPORT-WEEKLY-011: Weekly PDF respects all active report filters and opening-adjustment inclusion; the shared **Include notes in exports** option controls Notes for Daily Detail PDF/CSV.
- BT-REPORT-WEEKLY-012: Weekly report Date and customer-code columns are wide enough for visible values, with customer-code width adapting to the result set.


- BT-REPORT-WEEKLY-013: Weekly Movements exposes **Daily Detail** and **Weekly Overview** as views within the same report.
- BT-REPORT-WEEKLY-014: Weekly Overview aggregates by Customer + Container Type and displays total OUT, total IN and Net for the week.
- BT-REPORT-WEEKLY-015: Weekly PDF and CSV export the currently selected view and preserve its current grid ordering.
- BT-REPORT-WEEKLY-016: The shared Notes export option applies to Daily Detail only; Weekly Overview has no single movement-note field.
- BT-REPORT-WEEKLY-017: Weekly Generate & Open must render a literal ampersand in WinForms.


- BT-REPORT-WEEKLY-018: Weekly Movements uses one **Include notes in exports** option for both PDF and CSV.
- BT-REPORT-WEEKLY-019: The selected date cannot be later than today; service logic defensively clamps future dates.
- BT-REPORT-WEEKLY-020: Current-week results include movement data only through today even though the calendar week ends on Sunday.
- BT-REPORT-WEEKLY-021: Container filter options come from configured Container Types rather than outstanding-balance results.
- BT-REPORT-WEEKLY-022: Inactive Container Types remain selectable for historical reporting and are visibly labelled inactive.


- BT-REPORT-DAILY-015: Daily Movements date selection cannot go later than today.
- BT-REPORT-DAILY-016: Daily Movements service defensively clamps future requested dates to today.


## Business Branding

- BT-BRAND-001: Existing Business Information provides Business Name, Trading Name and Default Report Header as the current textual report identity.
- BT-BRAND-002: Pre-v1 branding expansion adds a configurable business logo and one authoritative branding configuration for reports, statements, email and other generated output.
- BT-BRAND-003: Logo storage/file rules, dimensions, fallbacks, placement and per-output enablement are agreed before implementation.
- BT-BRAND-004: Branding must not create separate contradictory identity/header systems for PDF and email output.


## Movement History Report

- Uses the full main BinTracker content area rather than a floating window.
- Reallocates columns on resize: structured fields stay compact while Customer, Status and Notes share surplus width; below useful minimums the grid scrolls horizontally instead of crushing fields.
- Keeps single-height rows, full Status/Notes tooltips, green IN/red OUT badges and amber/orange reversal badges without persisting presentation state.
- PDF and CSV suggested filenames include a sanitized stable customer code only when an applied customer filter resolves to one customer.

- BT-REPORT-HISTORY-001: Query actual movement rows for an inclusive selected date range.
- BT-REPORT-HISTORY-002: Start/end dates cannot go later than today; service logic defensively clamps future requests.
- BT-REPORT-HISTORY-003: Reversed ranges are normalized to chronological order.
- BT-REPORT-HISTORY-004: Filter by customer, configured Container Type, IN/OUT direction and normal entry source.
- BT-REPORT-HISTORY-005: Opening Adjustment is excluded by default and included only by explicit option.
- BT-REPORT-HISTORY-006: Container choices come from authoritative configured Container Types, including inactive types for historical filtering.
- BT-REPORT-HISTORY-007: Provide Last 7 Days, Last 30 Days and This Month shortcuts.
- BT-REPORT-HISTORY-008: Date and Quantity sort by typed values, not formatted strings.
- BT-REPORT-HISTORY-009: PDF and CSV preserve current visible grid order.
- BT-REPORT-HISTORY-010: One Include notes in exports option controls Notes in both PDF and CSV.
- BT-REPORT-HISTORY-011: PDF generation is audited as `MOVEMENT_HISTORY_REPORT_GENERATED`.
- BT-REPORT-HISTORY-012: The on-screen grid and PDF/CSV exports show the authoritative persisted Movement ID used by correction, reversal and audit references. It sorts numerically and remains associated with its typed movement row through filtering and multi-column sorting.
- BT-REPORT-HISTORY-013: Correct Entire Batch keeps heading/context, correction fields and Cancel/final action visible; only a genuinely long persisted movement list scrolls vertically. Ordinary small batches have no form/content horizontal or vertical scrollbar at Windows 11 1920x1080/150%.


## BinTracker Product Branding

- BT-BRAND-PRODUCT-001: The supplied BinTracker fish/bin artwork is the **product** logo and Windows application icon.
- BT-BRAND-PRODUCT-002: Product branding is separate from future customer/business branding configured in Business Information.
- BT-BRAND-PRODUCT-003: Product branding should be restrained in WinForms v1 and may be reconsidered during the post-v1 WinUI 3 redesign.


## BinTracker application branding

- BT-BRAND-APP-001: The BinTracker executable icon is the authoritative window/taskbar icon for Login, main shell, integrated report surfaces, import/admin dialogs and other WinForms windows.
- BT-BRAND-APP-002: Windows Forms inherit application icon behaviour from a common BinTracker form base rather than setting icons ad hoc.
- BT-BRAND-APP-003: The main left navigation shows the BinTracker product logo separately from future customer/business branding.
- BT-BRAND-APP-004: BinTracker product branding and configurable Business Information branding are separate concepts.


## Customer Statement entry points

- BT-REPORT-STATEMENT-010: Customer Statement workflow is available from both the Customers screen and the Reports launcher.
- BT-REPORT-STATEMENT-011: Both entry points use one shared generation workflow; do not duplicate save/open logic.
- BT-REPORT-STATEMENT-012: Reports entry point supports Customer search on Enter, optional inactive-customer inclusion, customer selection and statement generation.
- BT-REPORT-STATEMENT-013: Double-clicking a customer in the Reports statement window opens the shared statement workflow.


## Monthly Summary Report

- BT-REPORT-MONTHLY-001: Select a calendar month and summarize physical OUT, IN and Net movement.
- BT-REPORT-MONTHLY-002: Future months are not selectable; service logic defensively clamps future requests to the current month.
- BT-REPORT-MONTHLY-003: Current-month activity runs only through today.
- BT-REPORT-MONTHLY-004: Provide This Month and Last Month shortcuts.
- BT-REPORT-MONTHLY-005: Filter by customer, authoritative configured Container Type and normal movement Source.
- BT-REPORT-MONTHLY-006: Opening Adjustments are excluded by default and included only explicitly.
- BT-REPORT-MONTHLY-007: Customer free-text filter applies on Enter; month/dropdown/checkbox filters refresh live.
- BT-REPORT-MONTHLY-008: Summary rows are grouped by customer + Container Type and show OUT, IN and Net.
- BT-REPORT-MONTHLY-009: OUT/IN/Net columns sort by numeric values, not formatted strings.
- BT-REPORT-MONTHLY-010: PDF and CSV preserve the current visible grid order.
- BT-REPORT-MONTHLY-011: PDF generation is audited as `MONTHLY_SUMMARY_REPORT_GENERATED`.


## Report export auditing

- BT-REPORT-EXPORT-AUDIT-001: Every successful report CSV export writes an audit event.
- BT-REPORT-EXPORT-AUDIT-002: CSV audit context records report identity, relevant date/date range, row count, exported filename and applicable filters/view options.
- BT-REPORT-EXPORT-AUDIT-003: CSV contents are not copied into the audit trail.
- BT-REPORT-EXPORT-AUDIT-004: If the CSV file is created but audit persistence fails, warn the operator explicitly rather than silently pretending the export was audited.
