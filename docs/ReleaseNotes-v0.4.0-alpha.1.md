# BinTracker v0.4.0-alpha.1 — Excel Import Analysis

- Added Settings > Import Excel.
- Added read-only `.xlsm` / `.xlsx` workbook analysis using ClosedXML.
- Added worksheet dimension and Buyer-column detection.
- Added Account/Cash customer candidate preview.
- Added structural and duplicate-candidate warnings.
- Added audit event `IMPORT_WORKBOOK_ANALYSED`.
- Database writes remain deliberately disabled until the preview/matching rules are validated against the real workbook.
