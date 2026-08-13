# BinTracker v0.4.0-alpha.8 — Multi-page Import Wizard & Map

- Split the Import Wizard into separate Analyse and Map pages.
- Analyse is now focused on workbook selection, high-level counts and diagnostics only.
- Map gets a full page for worksheet classification and customer preview.
- Added worksheet classifications: Source, Validation, Report and Ignore.
- Added sensible defaults for the current legacy workbook:
  - Update Account = Source
  - Update Cash = Source
  - CREDITS = Validation
  - Print This = Report
  - Print this on reverse side = Report
  - Summary = Ignore
  - check-style sheets = Validation
- Customer preview on Map now includes only Source-sheet candidates.
- Changing a sheet classification immediately refreshes the Source customer preview/counts.
- Fixed All Worksheets `Columns` and `Candidates` header widths.
- Fixed duplicate-dialog `Occurrences` header width.
- No database import is performed yet.
