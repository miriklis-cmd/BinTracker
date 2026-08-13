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

## Multi-page workflow

### 1. Analyse
Select a workbook and inspect high-level workbook counts/warnings. No worksheet/customer grids are crowded onto this page.

### 2. Map
Classify every worksheet as:

- Source — authoritative data that may be imported.
- Validation — derived/checking data used to reconcile results only.
- Report — report/output layout, never treated as authoritative import data.
- Ignore — unrelated or unsupported sheet.

The customer preview is generated only from Source sheets and updates immediately when classifications change.

For the current legacy workbook the default profile treats Update Account and Update Cash as Source, CREDITS as Validation, the front/reverse print sheets as Report, and Summary as Ignore.

### 3. Review
Next implementation milestone. It will show the exact proposed customers, opening positions, movements, ignored sheets and reconciliation warnings before database writes are allowed.

### 4. Import
Will apply the approved Review plan transactionally.

## Review customer matching

Review compares only customers from worksheets currently classified as Source.

Matching uses the BinTracker customer code case-insensitively. A Source customer is classified as:

- **Existing — match**: same customer code exists and customer type agrees.
- **New candidate**: no existing BinTracker customer has that code.
- **Type mismatch**: the code exists but Account/Cash-COD classification disagrees.
- **Source conflict**: the same code appears on Source sheets with conflicting detected customer types.

Report, Validation and Ignore sheets do not participate in the customer match plan.

The Review page is read-only in v0.4.0-alpha.10. New-customer creation, container mapping and transactional database import remain intentionally disabled.


## Legacy Buyer prefixes

The Jack Miriklis legacy workbook sometimes encodes a container type in the Buyer text.

Examples:

- `(Bulk) Clamms` means customer `Clamms` with container hint `Bulk`.
- `(Y) Barwon` means customer `Barwon` with container hint `Y`.

These prefixes are not part of the customer identity. Review groups prefixed and unprefixed occurrences under the same customer code/name and preserves the prefix as a container hint for the later container-mapping stage.

This rule belongs to the legacy workbook profile and must not be assumed for every future customer's spreadsheet.

## Normalized customer matching

Review uses progressively stronger automatic matches:

1. Exact customer code.
2. Customer code ignoring case.
3. Customer code after ignoring spaces and punctuation.
4. Customer name after ignoring spaces and punctuation, only when exactly one existing customer matches.

Examples:

- `S & J` and `S&J` normalize to the same comparison key.
- `(Bulk) S&J` is first parsed as customer `S&J` plus container hint `Bulk`, then customer matching is performed.
- Fuzzy/edit-distance matching is not used in the current importer.

Ambiguous normalized matches are never silently merged.

## Confirmed legacy container aliases

For the Jack Miriklis workbook profile:

- `(Y)` means **Yellow Bin**.
- `(Bulk)` means **Bulk Bin**.

The resolver also checks current Container Type names and short codes. These rules belong to the legacy workbook profile and are not assumed for unrelated commercial workbook profiles.

## Balance reconciliation

Review includes a Balance reconciliation tab. Excel is authoritative at cutover:

`Opening adjustment = Excel B/Fwd - current BinTracker balance`

The workbook day's OUT and IN are then preserved as real movements. This prevents test/current balances from being added on top of the Excel target.


## Future customer-only import (post-v1.0)

A future import mode will support businesses that only have customer master data and do not want to migrate balances.

Planned import intents:

- **Customers only** — create/match customer master records; no container or balance data required.
- **Customers + opening balances** — establish customers and authoritative cutover balances.
- **Full migration** — customers, opening position and applicable movements.

Customer-only mode will reuse the same normalization, duplicate detection and merge-confirmation safeguards but will bypass container mapping and balance reconciliation.

## Legacy default container

For the Jack Miriklis legacy profile, no bracket/container token means **Blue Bin**.

This default applies only when the workbook provides no explicit container token.

If the workbook explicitly supplies an unknown token, such as `(Tub)`, BinTracker does not guess. The row is marked as requiring container mapping and cannot proceed to Import until the token is resolved.

## New customer decisions

Review includes **Confirm new customers...**. Each unmatched customer can be renamed and explicitly marked **Create**, **Skip**, or left **Unconfirmed**. Unconfirmed customers block reconciliation; skipped customers and their balance rows are deliberately excluded. Decisions persist while the current wizard remains open.
