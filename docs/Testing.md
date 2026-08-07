# BinTracker Automated Testing

From Alpha 6 onward, the repository contains separate unit and integration test projects.

## Unit tests

`tests/BinTracker.UnitTests`

Covers business rules that do not require a database, including:

- credit balances
- outstanding balances
- customer-code normalisation
- case-insensitive customer-code identity
- customer classification

## Integration tests

`tests/BinTracker.IntegrationTests`

Covers database behaviour, including:

- fresh SQLite schema upgrade
- schema versioning
- existing case-only duplicate customer codes
- Blue Bin terminology migration

## Regression rule

When a defect is discovered:

1. Add an automated test that reproduces the defect when practical.
2. Fix the defect.
3. Keep the test permanently.

## Local validation

Run:

```powershell
.\Build-BinTracker.ps1
```

A release is considered locally valid only when restore, build, and tests all succeed.
