# BinTracker Business Rules

## Concurrent command identity

Single Entry, Batch Entry, reversal and import treat `ClientOperationId` as command identity. The same canonical payload returns the prior result; reusing the ID with a different payload is rejected. Database uniqueness remains authoritative under races. Container Type names use normalized `NameKey`, and only one current ImportRun owns a cutover date.

This document records application behaviour independently of implementation details.

## Customers

- Customer codes are unique case-insensitively.
- Positive balances mean the customer owes containers.
- Negative balances mean CREDIT.
- Historical records remain valid when customers are deactivated.

## Container Types

- Container Types are master data.
- Display Order controls operational selection ordering.
- System Code is a stable internal identifier and does not change after creation.
- A type referenced by movement history is preserved; deactivate it rather than deleting its history.
- Dashboard Colour is presentation metadata and does not describe the physical colour of the container.
- Special Floor Report Container determines whether the type appears in the separate special-container section.

## Movements

- OUT increases the customer position.
- IN decreases the customer position.
- Single Entry saves one manual movement.
- Batch Entry saves its movements atomically.
- Movement History derives reversal status from relational linkage without editing the original movement Notes. Already-reversed originals and reversal rows cannot offer the Reverse action; service/database enforcement remains authoritative.

## Market Floor Sheet

- Blue Bin is the standard/default floor bin and is implicit on the printed floor sheet.
- Non-standard regular bins (for example Yellow) are shown explicitly and are never aggregated into Blue.
- `IsSpecialFloorReportContainer` determines which configured container types appear in Special Containers.
- Cash/COD credits remain in the Cash/COD area.
- Account credits appear in the separate CREDIT area.
- Import `Adjustment` movements contribute to B/Fwd/opening position, not physical daily OUT/IN.
- Front and reverse pages adapt typography/spacing to the actual rendered row load and target exactly two physical A4 pages.
- The report is operationally read from around 4am, so use the largest type that safely fits.

## Business Information

- Business Information is administrator-maintained master data.
- Project documentation remains business-neutral.
- Default Report Header overrides Trading Name for report headings.
- Trading Name overrides Business Name for display/report identity when no explicit report header exists.
- Business Information changes are audited.

## Audit

Important security, master-data and movement changes create audit events.

## Legacy import cutover

- Legacy spreadsheets may contain only B/Fwd plus one day's IN/OUT.
- B/Fwd represents opening position, not a physical movement that occurred on the cutover day.
- Cutover-day IN/OUT are retained as real movements.
- The workbook's Total is validation data and must not be imported as a second balance.
- Total must reconcile as `B/Fwd + OUT - IN`; mismatches require review.


## Import Correction

- Same-cutover correction reconstructs the workbook position from legitimate movement history strictly before the cutover date.
- Movements linked to the previous ImportRun are excluded/replaced.
- Manual/Batch activity on the cutover date or later is subsequent real activity and remains on top of the corrected imported position.


- Correction must be visible before execution: when same-cutover replacement is required, Step 4 presents Replace / Correct and the comparison before any write attempt.


- Import correction comparison identity is Customer + configured Container Type. Legacy tokens/display labels do not define separate balances.


- Import Run history is read-only. Import history is provenance/audit evidence, not an editable operational ledger.


- Correction differences are immutable provenance. Store the resolved Customer + Container Type change snapshot on the corrected ImportRun before removing the previous run's generated rows.


- Unsaved Container Type edits must never be silently discarded when the operator selects another type, starts a new type, or closes the editor.
- Existing-customer Import decisions: **Accept match** confirms the automatically proposed BinTracker customer; **Override match** records an explicit choice of a different existing customer.


- Unsaved Customer edits must never be silently discarded. The operator must explicitly Save, Discard or Cancel before leaving the edited customer/workflow.
- Unsaved-change dialog buttons must describe the actual action: **Save / Discard / Cancel**.

## Historical balances

- Historical outstanding position is derived from the immutable movement ledger as of the requested date; a separate daily snapshot table is not required merely to answer historical balances.
- Daily/Weekly/Monthly reporting must preserve Container Type separation.

## Batch Entry

- Successful Add to Batch clears Customer, Quantity, Reference, Notes and the customer-position preview, then returns focus to Customer entry. Movement Date, Batch Type and Container Type intentionally carry forward for rapid entry.
- Esc is state-based: cancel draft-line edit and clear its editor; otherwise clear only the current unsaved entry; otherwise leave Batch Entry for Dashboard. Existing draft lines are retained in all three cases, and Dashboard navigation state must stay synchronised.
- Draft lines/date/direction persist to an atomic LocalApplicationData recovery file. After normal close, process restart, crash or power loss, BinTracker explicitly offers Continue Batch / Save Batch / Discard Batch (in that visual order) rather than silently resuming. Discard is destructive and confirmed; successful Save Batch, Clear Batch and confirmed Discard remove the recovery file.
- In Edit mode, Enter from Quantity/Reference/Notes updates the selected draft row rather than adding another row. Successful Update or Remove clears the editor and returns to Add to Batch mode; deleting the final row must not leave stale edit state.

## Communications

- Current provider direction is Google Workspace for email and Texto for SMS.
- Automatic reminder policy is intended to contact customers owing empty bins by Friday or earlier, while allowing business-rule refinement before production.

## Central database

- The permanent target is multiple desktop/remote clients through an authenticated BinTracker service/API to central PostgreSQL. Remote clients never connect directly to PostgreSQL.
- Production business operations assume concurrent authenticated users. Request user/client identity is scoped to the operation; it is never shared process-wide on a server.
- Business dates use the configured business timezone and audit timestamps use an injected UTC clock, never implicit server-local time.
- Database constraints are authoritative for concurrent invariants. A losing request receives a stable business result, and retryable remote commands require idempotency identity before the API is enabled.
- Services + `IDbContextFactory<BinTrackerDbContext>` remain the local application boundary; database-provider-specific SQL, configuration and migrations belong in infrastructure.
- Remote import/export contracts carry content/streams and metadata, not a client path such as `C:\\...`. The current SQLite desktop adapter may continue using local paths until central deployment exists.

## As-of-date reporting

- “As of” means the position at the end of the selected movement date: include movements dated on or before that date and exclude later movements.
- Positive positions are outstanding; negative positions are credit.
- Container types are never combined when calculating historical positions.
- Inactive customers remain part of historical truth and may be included in historical reporting.


## Outstanding report presentation

- Outstanding report default ordering is Customer → configured Container Type display order so multiple container positions for one customer remain together.


## Report catalogue navigation

- Market Floor Sheet remains the first/inline operational report.
- Detailed reports normally use dedicated windows so filters, tables and export/print actions have full working space. Movement History is the explicit exception: it uses the full main-application workspace because it also hosts the operational reversal action.
- “Today” is a shortcut inside the relevant report window, not a separate report.


## Interactive report sorting and printing

- Interactive report columns must sort according to their underlying data type. Numeric positions/quantities sort numerically, never lexicographically by formatted text.
- Outstanding Containers PDF generation is a printable snapshot of the current on-screen dataset and therefore follows the operator's current grid row order/sort.
- Changing grid sort order is presentation only; it does not modify movement history, balances or stored report data.


## Interactive report export ordering

- Outstanding Containers CSV export follows the operator's current displayed grid order/sort, the same as PDF generation.
- PDF and CSV therefore represent the current on-screen dataset ordering; neither export changes authoritative movement/balance data.


## Daily movement reporting

- Daily Movements reports authoritative operational activity by `MovementDate`. Alpha.8 obtains that result through effective-movement suppression; planned BT-HIST-008/BT-CORR-020 replaces the mechanism with validated current-generation projection so correction/restoration bookkeeping cannot inflate totals.
- Opening Adjustment rows are not physical daily movements and are excluded by default; the operator can explicitly include them for investigation.
- Today and Yesterday are shortcuts that set the report date and rerun the same report logic.
- Numeric Quantity sorting uses the underlying integer quantity.
- PDF and CSV preserve the current displayed grid order/sort.


## Daily Movements export notes

- Daily export notes are opt-in because free-text notes can materially increase PDF width/page count and add unnecessary CSV detail.
- When Include notes in exports is off, Notes remain visible on-screen but are omitted from both PDF and CSV.
- When enabled, PDF and CSV both add a Notes column; PDF uses a slightly denser layout to retain readability.


## Daily Movements source/adjustment controls

- The Source filter intentionally excludes Opening Adjustment. It represents normal entry origin: Single Entry, Batch Entry or Excel Import.
- Opening adjustments are controlled only by the separate **Include opening adjustments** checkbox.
- With the checkbox off, adjustment rows are excluded regardless of other filters.
- With the checkbox on and Source = All sources, adjustment rows may appear alongside physical movement rows and are clearly identified by Source.


## Daily Movements UI layout

- Daily Movements layout has no business-data effect: moving filters/options/actions between visual rows does not change query, balance, PDF or CSV semantics.


## Weekly movement reporting

- Operational weeks run Monday through Sunday.
- Weekly net movement is OUT minus IN; it is movement for the week, not the customer's outstanding balance.
- Opening adjustments are excluded by default because they are position-establishing adjustments rather than ordinary corrected operational activity.
- Weekly summary remains separated by Customer and Container Type.


## Weekly detail versus overview

- Weekly Movements contains two views rather than separate reports.
- **Daily Detail** shows individual movement rows across the selected Monday-Sunday week.
- **Weekly Overview** aggregates the selected week's activity by Customer + Container Type.
- Weekly Overview shows total OUT, total IN and Net (OUT minus IN). Example: if CLAMMS takes 45 Yellow Bins and returns 45 Yellow Bins in the week, the overview shows `45 OUT`, `45 IN`, `Net 0`.
- PDF and CSV export the currently selected view and preserve that view's current grid order.
- **Include notes in exports** applies to Daily Detail PDF/CSV only because Weekly Overview contains aggregated rows rather than individual movement notes.


## Weekly report date limits

- Weekly Movements is an actual-history report, not a forecasting report.
- The selected date cannot be later than today.
- The report service also defensively clamps a future selected date to today.
- For the current Monday-Sunday week, the displayed week remains the calendar week but movement data stops at today; future days are never interpreted as zero activity.
- Future/predictive reporting belongs to later forecasting/analytics work, not Weekly Movements.

## Report container filters

- Container selectors are populated from configured BinTracker Container Types master data, not from whichever types happen to have a non-zero current outstanding balance.
- Active and inactive configured container types are available so historical reports can still filter a type that has since been deactivated.
- Inactive types are explicitly labelled `(inactive)`.


## Daily report date limits

- Daily Movements is actual-history reporting, not forecasting.
- The selected date cannot be later than today.
- The service defensively clamps a future requested date to today so future-dated movement rows cannot leak into Daily Movements.


## Business branding

- Current generated-report identity is textual: Default Report Header, otherwise Trading Name, otherwise Business Name, otherwise `BinTracker`.
- Logo support is not implemented yet.
- Future logo/email/report branding must use one authoritative Business Information branding configuration rather than format-specific identity settings.


## Movement History reporting

- Movement History is actual historical movement reporting, not forecasting.
- Movement History is an integrated full-size main-application page rather than a floating report window.
- Movement ID is the persisted `BinMovement` identifier used by correction/reversal/audit references. It is displayed after Date, sorts as a number, remains attached to the typed row during filtering/multi-column sorting, and is included in PDF/CSV history exports; no synthetic row number is substituted.
- Predictable structured columns remain compact. Customer, Status and Notes share remaining width responsively; readable minimums are preserved and horizontal scrolling is allowed only when the host becomes too narrow.
- Rows remain single-height. Direction is presented with restrained green IN/red OUT badges; reversal status uses amber/orange. Badge/status presentation never changes ledger Notes or authoritative correction state.
- Truncated Status and Notes cells expose their complete displayed text through tooltips.
- PDF and CSV use the same suggested filename rule: an applied customer filter that resolves the displayed report to exactly one CustomerId adds its Windows-sanitized stable customer code; otherwise naming remains generic.
- Correct Entire Batch keeps its action band outside the scrollable content area so long batch previews or DPI scaling cannot make Cancel or final confirmation inaccessible.
- Date ranges are inclusive and cannot extend past today.
- Opening adjustments are excluded by default because they are not physical activity.
- Historical Container Type filtering includes inactive configured types because old movement rows remain legitimate history.
- OUT/IN/net are movement totals for the selected range, not outstanding balances.
- PDF/CSV use the current displayed row order and one shared Notes export option.


## Interactive report refresh behaviour

- Interactive report pages do not require a separate Run Report button.
- Date pickers, dropdown filters and result-affecting checkboxes refresh the report when changed.
- Free-text Customer search does **not** query on every keystroke; pressing Enter applies the customer filter.
- Shortcut buttons such as Today, Yesterday, This Week, Last Week, Last 7 Days, Last 30 Days and This Month apply their date selection and refresh immediately.
- This interaction standard applies to Outstanding Containers, Daily Movements, Weekly Movements, Movement History and future interactive report pages unless a report has a specific reason to behave differently.


## Report customer-search cue

- Interactive report customer fields visibly tell the operator to **press Enter** to apply a free-text customer search.
- This cue exists because dropdown/date/checkbox filters are live but customer text deliberately does not query per keystroke.


## BinTracker product branding

- BinTracker's product icon/logo identifies the application itself.
- Login, the main shell and every standalone dialog window should use the BinTracker executable icon in title bars/taskbar; integrated report pages inherit the main shell icon.
- The main sidebar shows the BinTracker product logo.
- Future Business Information logo/header configuration belongs to the user's business and must not replace/confuse the BinTracker application identity.


## Customer Statement entry points

- Customers → selected customer → Customer Statement is the contextual shortcut.
- Reports → Customer Statement is the report-discovery path for users who start from Reports.
- Both paths use the same shared statement generation workflow and therefore the same date validation, Generate PDF and Generate & Open behaviour.
- Inactive customers remain available from the Reports statement selector when explicitly included because historical statements remain legitimate.


## Monthly Summary reporting

- Monthly Summary reports calendar-month movement, not outstanding balances.
- OUT and IN are physical movement quantities unless Opening Adjustments are explicitly included.
- Net is `OUT - IN`; positive Net means more containers went OUT than came IN during the selected month.
- Current-month data stops at today and future months are not permitted.
- Customer/container rows remain separated by configured Container Type.
- Historical inactive Container Types remain selectable because their historical movements remain valid.
- PDF/CSV follow the currently displayed row order.


## Report export audit trail

- PDF generation and CSV export are auditable report-output actions.
- CSV audit events identify the report, user/time through the audit system, date/range, row count, filename and filters/context.
- Do not store full exported report contents in AuditEvent data.
- A CSV successfully written to disk with a failed audit write must produce an operator warning.


## Movement correction / reversal authorization

- Administrator and Operator are trusted operational roles and may reverse ordinary physical movements entered through Single Entry (`Manual`) or Batch Entry (`Batch`), including movements originally entered by another operator.
- Viewer remains read-only and cannot reverse movements.
- Reversal remains append-only: the original movement is never edited/deleted; an equal and opposite linked movement is created with mandatory reason, actor/time and audit provenance.
- Opening Adjustments are sensitive brought-forward-position records and cannot use the generic Movement History reversal action. They require an Administrator-controlled adjustment workflow.
- Excel Import movements, including movements linked to an ImportRun, cannot be reversed individually through the generic reversal action. They must use the Administrator Replace / Correct import workflow so import provenance and reconciliation remain internally consistent.
- Reversal movements cannot themselves be reversed, and an original movement cannot be reversed twice.
- If formal period locking/close is introduced later, historical-period reversal authorization must be explicitly defined before enabling it.
- Administrator and Operator may correct eligible ordinary Manual/Batch movements; Viewer may not. Routine correction is effective immediately and has no approval queue.
- Correction preserves the original, creates an opposite neutraliser dated on the original date, and creates the corrected replacement dated on the corrected operational date. This removes the wrong-period report effect and applies it in the right period.
- Alpha.8 operational views suppress correction-consumed originals/neutralisers and show the corrected replacement; the planned lineage projection preserves that accepted result by resolving a validated current logical generation. Movement History/Audit always retain complete evidence.
- Alpha.8 whole-batch correction remains safely limited to one persisted `MovementBatchId`. Planned logical-root correction supersedes that limitation only through BT-CORR-018..033; the current guard must not be removed independently.
- Movement History action availability is computed from the actual selected typed movement after every load/sort and selection change. Reverse and Correct Selected share the ordinary-movement eligibility basis; displayed selection and logical action state must agree.
- In whole-batch correction, changing the proposed date or direction away from the persisted value automatically selects that correction field, and returning it to the persisted value automatically clears the field. Operators may untick a changed field; its selection is recalculated only when that proposed value changes again. A checked field with its persisted value is not a change; the UI and service reject a request with no semantic date/direction change before any neutraliser, replacement, correction record or audit event is created.
- One database-unique neutraliser per original arbitrates reverse/correct races. Client operation identity makes an identical retry idempotent and rejects different payload reuse.
- Operator movement changes require later Administrator acknowledgement. Existing pre-migration events are not backfilled as pending.
- Administrator acknowledgement records review only and never controls the operational effectiveness of an Operator correction/reversal. The review record retains reviewer and UTC time, produces an acknowledgement audit event, and cannot be acknowledged twice.
- Audit Trail must display review state directly and make outstanding review-required events practically filterable. Mark Selected Reviewed is unavailable unless the current selection is an unreviewed, review-required Operator correction/reversal; role enforcement remains authoritative at the service boundary.
- While any such reviews remain outstanding, Administrator sessions must retain a non-blocking navigation-wide reminder with the live outstanding count and direct pending-review action. It disappears at zero, is invisible to other roles, supplements the login popup and never changes the immediate operational effectiveness of Operator changes.
- View Batch Detail is unavailable unless the selected audit event has authoritative persisted MovementBatch detail. Audit event types without an authoritative supported detail surface must not offer a knowingly invalid detail action.

## Frozen logical correction rules (planned v1)

- Every eligible ordinary original has one stable logical root and permanent logical line. Physical batches are immutable persistence evidence, not continuing lineage identity; roots/lines never merge or split.
- Each substantive correction/reversal/restoration advances the root generation and writes one full state decision for every permanent line. Active means one effective movement; Reversed means last effective plus terminal reversal.
- Corrected authoritative operational activity is projected from the complete validated current generation. PositionAsOf(D) signs that activity through MovementDate D. GenerationNumber, MovementDate and CreatedUtc remain semantic, business and forensic time respectively.
- Correction retrospectively restates an erroneous historical movement. Valid dates through today are allowed; future dates are prohibited. Formal period locking/high-risk approval remains post-v1.
- Restoration declares an ordinary reversal erroneous. It restores the last legitimate pre-reversal values, then applies only explicitly selected fields. A later legitimate movement is new activity, not restoration.
- Whole-root correction defaults reversed lines to an explicit operator decision. RemainReversed retains zero contribution/no fake movement; Restored creates an effective movement. AlreadyMatches and CarriedForward write state evidence but no ledger row.
- Complete semantic no-op writes nothing. Restoration is substantive even without field overrides. Request intent distinguishes absent, clear and value fields.
- A physical correction-output batch is optional and only represents a complete uniform newly created result for every logical line. Mixed dates, partial no-op and remain-reversed results have no fabricated batch.
- Root generation is optimistic concurrency authority. Exact operation retry returns its committed result; changed operation-ID reuse fails. WinForms state is never authorization, eligibility or planning authority.
- ImportRun/ExcelImport movements remain in the Administrator Replace/Correct import domain and cannot enter generic lineage. Import replacement must fail if ordinary lineage/evidence references would be deleted.
- Operationally corrupt/incomplete lineage fails every potentially affected numeric result without omission/raw fallback. Audit-only corruption does not falsify proven mathematics, but blocks mutation/review/evidence-completeness output and raises critical health.
