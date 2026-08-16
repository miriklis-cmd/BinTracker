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

- BT-BATCH-001: Successful line entry clears non-carry-forward entry fields and returns focus to the Customer field/code entry.
- BT-BATCH-002: Esc behaviour is explicit and consistent for current-line edit/clear/exit states.
- BT-BATCH-003: In-memory draft survival across navigation/logout is supported; crash/power-loss recovery is a separate production decision.

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
- BT-REPORT-UI-003: Filter-heavy/data-grid reports open in dedicated report windows.
- BT-REPORT-UI-004: Only one live instance of a given report window should exist per MainForm session; reopening brings the existing window forward.


- BT-REPORT-UI-005: Dedicated report windows size themselves from the active monitor's working area within sensible minimum/maximum bounds.
- BT-REPORT-UI-006: Report filter/action controls must remain fully visible; the result dataset consumes remaining window space and resizes with the window/monitor.

- BT-REPORT-UI-007: Report customer-code columns dynamically size to the longest visible customer code, with sensible minimum and maximum widths so codes remain readable without crowding other report data.

- BT-REPORT-UI-008: Outstanding report Code and Type columns resize from the currently visible result set after each report run, using sensible minimum/maximum widths.


## Dashboard Design Gate

- BT-DASH-DESIGN-001: Dashboard implementation must not begin until the operator and developer review alternative layouts, charts, drill-through behaviour, exception/attention concepts and forecasting hooks together.
- BT-DASH-DESIGN-002: Dashboard design must consider both laptop and large-monitor operation.
- BT-DASH-DESIGN-003: Forecasting/ML hooks are future-facing derived analytics and must never modify authoritative movement/balance records.


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
