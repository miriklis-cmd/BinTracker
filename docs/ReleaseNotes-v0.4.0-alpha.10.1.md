# BinTracker v0.4.0-alpha.10.1 — Review Test Build Fix

- Fixed CS8752 in `ExcelImportReviewPlannerTests.cs`.
- Replaced target-typed `new(...)` expressions used as `params` arguments with explicit `new ImportCustomerCandidate(...)`.
- No production Review/import logic changed.
- Reviewed `KNOWN-ISSUES.md` and `TECH-DEBT.md`; current implementation priority remains unchanged.
