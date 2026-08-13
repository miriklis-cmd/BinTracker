# BinTracker v0.4.0-alpha.12 — Developer Database Tools

- Added `Settings > Developer Tools > Developer Database`.
- Added safe SQLite Backup Database.
- Added Load Database:
  - validates the selected BinTracker database;
  - automatically backs up the current database;
  - stages the selected database;
  - restarts BinTracker;
  - swaps the file before EF/database services start.
- Added Start Fresh Test Database:
  - automatically backs up the current database;
  - restarts BinTracker;
  - removes the active test database before startup;
  - BinTracker creates a fresh database and returns to first-run Administrator setup.
- Developer backups are stored under the BinTracker local application-data folder.
- Added restart-safe pending database operations rather than replacing a live SQLite file.
- Re-import duplicate protection is now explicitly a hard blocker before Import execution can be enabled.
- Added `docs/ReimportSafety.md`.
