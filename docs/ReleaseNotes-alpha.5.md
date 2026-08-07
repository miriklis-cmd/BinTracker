# BinTracker v0.2.0-alpha.5

## Customer identity fixes

- Customer codes are now validated case-insensitively.
- `Albury`, `ALBURY`, and `albury` are treated as the same business code.
- Codes continue to be normalised to uppercase when saved.
- A case-insensitive SQLite unique index is added automatically when the existing database contains no older duplicate-code collisions.
- Older alpha databases that already contain case-only duplicates can still open so the test records can be corrected through the UI.

## Customer types

- Added `Account` and `Cash / COD` customer classifications.
- Customer type is visible/editable on the customer form.
- Customer type is displayed in the customer list.
- Existing customers default to Account.

## UI polish

- Settings `Manage Users` and `View Audit Trail` buttons now have identical fixed dimensions and alignment.
