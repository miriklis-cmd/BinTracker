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
