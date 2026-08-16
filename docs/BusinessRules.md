# BinTracker Business Rules

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

- Successful line entry clears non-carry-forward entry fields and returns focus to Customer entry.
- Draft surviving navigation/logout does not imply crash/power-loss persistence; that is a separate production capability.

## Communications

- Current provider direction is Google Workspace for email and Texto for SMS.
- Automatic reminder policy is intended to contact customers owing empty bins by Friday or earlier, while allowing business-rule refinement before production.

## Central database

- PostgreSQL is the intended direction for eventual multi-user central deployment.
- Services + `IDbContextFactory<BinTrackerDbContext>` remain the application boundary; database-provider-specific concerns belong in infrastructure/migration code.

## As-of-date reporting

- “As of” means the position at the end of the selected movement date: include movements dated on or before that date and exclude later movements.
- Positive positions are outstanding; negative positions are credit.
- Container types are never combined when calculating historical positions.
- Inactive customers remain part of historical truth and may be included in historical reporting.


## Outstanding report presentation

- Outstanding report default ordering is Customer → configured Container Type display order so multiple container positions for one customer remain together.


## Report catalogue navigation

- Market Floor Sheet remains the first/inline operational report.
- Detailed reports use dedicated windows so filters, tables and export/print actions have full working space.
- “Today” is a shortcut inside the relevant report window, not a separate report.


## Interactive report sorting and printing

- Interactive report columns must sort according to their underlying data type. Numeric positions/quantities sort numerically, never lexicographically by formatted text.
- Outstanding Containers PDF generation is a printable snapshot of the current on-screen dataset and therefore follows the operator's current grid row order/sort.
- Changing grid sort order is presentation only; it does not modify movement history, balances or stored report data.


## Interactive report export ordering

- Outstanding Containers CSV export follows the operator's current displayed grid order/sort, the same as PDF generation.
- PDF and CSV therefore represent the current on-screen dataset ordering; neither export changes authoritative movement/balance data.


## Daily movement reporting

- Daily Movements defaults to physical activity: Manual/Single Entry, Batch Entry and ExcelImport physical IN/OUT rows.
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
