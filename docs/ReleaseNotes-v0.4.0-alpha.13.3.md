# BinTracker v0.4.0-alpha.13.3 — BalanceService SQLite Crash Fix

- Fixed the Step 3 Review crash in `BalanceService.GetBalancesAsync()`.
- Root cause: EF Core 8 / SQLite could not translate the previous query that joined navigation properties, grouped by customer/container names and projected directly into `BalanceRow`.
- New query strategy:
  - SQL groups/sums by scalar `CustomerId` + `ContainerTypeId`;
  - customer/container names are loaded after the aggregate query;
  - `BalanceRow` objects and final display sorting are created in C#.
- Added SQLite integration tests that exercise the real `IBalanceService` through DI.
- No balance/import business rules changed.
- Added post-v1.0 roadmap item for customer-list-only import (customer names/codes without balances or movements).
