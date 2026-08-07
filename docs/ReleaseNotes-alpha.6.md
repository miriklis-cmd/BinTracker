# BinTracker v0.2.0-alpha.6 — Customer Stability

## Fixed

- Removed the invalid raw SQLite duplicate-code query that caused startup to fail with:
  `SQLite Error 1: near ";": syntax error`.
- Customer code is normalised to uppercase when leaving the code field.
- New duplicate-code attempts are blocked case-insensitively.

## Database upgrades

- Added an explicit `SchemaVersion` table.
- Added numbered SQLite upgrade steps.
- Each upgrade runs transactionally.
- Existing test databases containing `Albury` / `ALBURY` can still upgrade.
- The case-insensitive database index is created automatically once old collisions are resolved.

## Automated tests

- Split tests into unit and integration suites.
- Added regression coverage for the Alpha 5 SQLite migration failure scenario.
- Added tests for customer-code normalisation, credits, customer types, database upgrades, and Blue Bin migration.
- `Build-BinTracker.ps1` now runs `dotnet test` after the build.

## Documentation

- Added Functional Specification.
- Added Known Business Rules.
- Added Testing guide.
