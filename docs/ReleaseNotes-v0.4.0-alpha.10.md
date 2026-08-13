# BinTracker v0.4.0-alpha.10 — Import Review

- Added a real Review page as wizard step 3.
- Review compares Source-sheet customer codes against the existing BinTracker database.
- Customer review statuses:
  - Existing — match
  - New candidate
  - Type mismatch
  - Source conflict
- Matching is case-insensitive on customer code, consistent with BinTracker customer-code rules.
- Validation/Report/Ignore worksheets are excluded from the customer match plan.
- Review reports Source B/Fwd/daily snapshot row count and formula-total mismatch count.
- Import remains deliberately disabled.
- Widened `Candidates` and `Occurrences` columns to stop header clipping.
- Reviewed and updated Known Issues / Technical Debt.
