# BinTracker v0.4.0-alpha.13.4 — BalanceService Lookup Fix

- Fixed the remaining BalanceService integration-test failure.
- SQL aggregation by CustomerId/ContainerTypeId remains unchanged.
- Removed EF queries that filtered customer/container IDs using array `Contains`.
- The current .NET 8 / EF Core 8 expression interpreter was attempting to evaluate those as `ReadOnlySpan<int>` and throwing before SQL execution.
- Customer and Container Type ID/name dictionaries are now loaded directly from their small master tables.
- Added regression coverage proving unrelated customers without movements do not appear in balance results.
- No import/reconciliation business logic changed.
