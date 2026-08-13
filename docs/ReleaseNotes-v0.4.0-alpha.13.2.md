# BinTracker v0.4.0-alpha.13.2 — Normalized Review Grouping Fix

- Fixed a real Review-planner logic bug caught by automated tests.
- `S & J`, `S&J`, `S  &  J`, `(Bulk) S&J` and similar conservative normalization variants are now grouped before customer matching.
- Review emits one customer row when those variants resolve to the same normalized customer identity.
- Existing customer-code spelling is preferred for the consolidated display value when available.
- Legacy variants and container hints remain preserved for audit/review.
- No database write/import execution logic changed.
