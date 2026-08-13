# BinTracker v0.4.0-alpha.16.1 — Customer Decision Build Fix

- Fixed CS0165 in `ImportBalanceReconciliationPlanner`.
- Decision lookup now uses explicit nullable state before checking Unconfirmed / Skip.
- Missing decision entries continue to block new-customer reconciliation safely.
- Added regression coverage for an absent decision entry.
- No customer-decision business rules changed.
