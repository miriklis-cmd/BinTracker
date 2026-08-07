# BinTracker v0.1.0-alpha.3.7

## Database architecture preparation

This release keeps SQLite as the active database so development and installation remain simple.

It prepares BinTracker for a later move to PostgreSQL by:

- Moving connection/provider settings into a dedicated `DatabaseConfiguration`.
- Introducing `DatabaseProvider` and `DatabaseSettings`.
- Keeping SQLite-specific initialisation inside the data project.
- Preventing services and UI code from depending on SQLite details.
- Creating `%LOCALAPPDATA%\BinTracker\database.json` on first run.
- Updating the status bar to display the active provider.
- Ignoring `database.json` in Git so future server credentials are not committed.

## Important

PostgreSQL is deliberately **not enabled yet**. No extra database server is required for this alpha.

When multi-user deployment is ready, the intended change is confined primarily to
`BinTracker.Data`: add the Npgsql EF Core provider, add PostgreSQL migrations/initialisation,
and change `database.json` to point workstations at the central server.
