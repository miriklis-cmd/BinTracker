# BinTracker v0.4.0-alpha.16.2 — Fresh Database Decision Test Fix

- Updated the fresh-database balance reconciliation test for the alpha.16 customer-decision rules.
- New customers now require an explicit `Create` decision before reconciliation can calculate opening adjustments.
- Added regression coverage confirming a fresh new customer with no decision remains blocked.
- No production importer logic changed.
