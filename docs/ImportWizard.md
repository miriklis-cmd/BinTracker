# Excel Import Wizard

## Goal

Import the historical Excel workflow safely without silently damaging existing BinTracker data.

## Phase 1 — Workbook Analysis (v0.4.0-alpha.1)

The first implementation is intentionally read-only.

Administrators can select `.xlsx` or `.xlsm` files. BinTracker then:

- opens the workbook without requiring Microsoft Excel;
- lists worksheets and used dimensions;
- detects columns headed `Buyer`;
- previews candidate customer codes;
- infers Account/Cash from source sheet names where possible;
- flags missing expected operational sheets;
- flags potential duplicate buyer codes;
- records `IMPORT_WORKBOOK_ANALYSED` in the audit trail.

No customer, movement, balance, container type or user data is changed in this phase.

## Next phase

The next import step will compare workbook candidates against the database and classify them as:

- matched existing customer;
- new customer;
- possible duplicate/conflict;
- ignored/non-customer row.

Only after that preview is reliable will Merge / Replace / Fresh Database actions be enabled.

## Legacy snapshot workbooks

BinTracker does not assume every workbook contains complete history.

For a legacy workbook that is overwritten daily and contains:

- B/Fwd
- today's OUT
- today's IN
- current Total

the import model is:

1. B/Fwd becomes the opening position at cutover.
2. Today's OUT is imported as a real OUT movement.
3. Today's IN is imported as a real IN movement.
4. BinTracker calculates the resulting total.
5. The Excel Total is used only to validate the calculation.

If the calculated total does not equal the workbook Total, the row must be reviewed before import.

This preserves the correct current position without inventing historical movements that do not exist.

## Analysis counts

The analyser distinguishes:

- **Unique customers**: case-insensitive distinct Buyer/customer codes or names.
- **Occurrences found**: every detected appearance of a Buyer/customer in source sheets.

A custom workbook can contain the same customer on multiple operational/report sheets, so occurrence count is expected to be larger than unique customer count.
