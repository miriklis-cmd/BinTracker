# Technical Debt

These are engineering improvements, not current user-facing defects.

## UI
- Consolidate repeated WinForms card/button/grid construction into shared UI helpers.
- Continue high-DPI regression testing across 100%, 125% and 150% scaling.
- Consider centralising typography/spacing constants.

## Import
- Refactor wizard steps into separate Analyse / Map / Review / Import view components as functionality grows.
- Introduce reusable import-profile abstractions so legacy/custom workbooks do not leak rules into the generic import engine.
- Add a standard BinTracker import template/profile for future customers.

## Reports
- Extract more reusable report layout primitives as the report catalogue grows.
- Keep legacy report-layout inference separate from core report generation.

## Testing
- Add broader fixture coverage for complex/custom Excel workbooks without storing private production data in the repository.
