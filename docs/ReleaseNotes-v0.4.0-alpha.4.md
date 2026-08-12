# BinTracker v0.4.0-alpha.4 — Import Wizard Polish & Snapshot Model

## UI
- Wizard step numbers are now circular and connected by the approved horizontal progress line.
- Added `View all worksheets`.
- Removed the duplicate bottom Analyse button.
- `Next` is enabled after a successful workbook analysis.
- Improved successful-analysis summary.

## Legacy workbook model
- Added explicit snapshot-row detection for sheets containing Buyer plus Out / In / B/Fwd / Total.
- The importer now models legacy cutover data as:
  - B/Fwd = opening position before the import day;
  - OUT = real movement on the import day;
  - IN = real movement on the import day;
  - Excel Total = validation only.
- `CalculatedTotal = B/Fwd + OUT - IN`.
- A mismatched Excel Total is flagged for review in later mapping/review stages.
- This does not yet write any imported data to the database.
