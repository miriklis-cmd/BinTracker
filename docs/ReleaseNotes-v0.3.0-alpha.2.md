# BinTracker v0.3.0-alpha.2 — Migration Test Hardening

## Test infrastructure
- Removed hard-coded schema version `6` assertions from the SQLite migration tests.
- Tests now compare the upgraded database against `DatabaseSetup.LatestSchemaVersion`.
- `LatestSchemaVersion` is derived from the registered SQLite migration catalogue.
- Added a dedicated regression test for Container Type master-data columns.
- Added verification that CHEP receives its expected short/system codes and Special Floor Report flag.

## Settings
- Clarified the non-administrator message to include Container Type administration.

No operational data is changed by this patch beyond the already-existing v7 Container Type migration.
