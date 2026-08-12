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

- Output is A4 portrait.
- Front and reverse must each remain one page.
- Front: Account customers owing occupy two columns.
- Front: Cash/COD customers owing occupy the third column.
- All customer credits appear under the Cash/COD section.
- Special Floor Report Containers appear in the separate special-container section.
- Reverse: Account customers are on the left.
- Reverse: Cash/COD customers are on the right.
- Reverse columns are Buyer / Out / In / B/Fwd / Total.
- B/Fwd includes all regular-container movements before the selected report date.
- Total = B/Fwd + OUT - IN.
- Negative totals display as CREDIT.

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
