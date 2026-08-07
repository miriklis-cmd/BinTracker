# BinTracker v0.1.0-alpha.3

This release adds local authentication, roles, user administration, and an append-only audit trail.

## First launch

1. Open `BinTracker.sln`.
2. Build and press F5.
3. Create the first administrator account when prompted.
4. Log in using that account.

Passwords must be at least 10 characters and include uppercase, lowercase, and a number.

## Audit coverage

The audit table now records successful and failed logins, logout, initial administrator creation, new users, user activation/deactivation, machine name, session ID, success/failure and before/after values where applicable.

The audit service is ready to record customer changes, movements, batch reversals, settings, backups/restores, Excel imports/exports, and generated reports as those features are implemented.

Existing Alpha 2 data is preserved. Startup performs an idempotent database upgrade that adds the security tables if they do not exist.


## Multi-computer note

This alpha currently stores SQLite data locally under `%LOCALAPPDATA%`. Do not use separate installations as a shared multi-user production system yet. A central database provider will be added before multi-computer deployment.


## Database deployment strategy

BinTracker currently uses SQLite for fast single-PC development and testing.

Database access is now isolated in `BinTracker.Data` and configured through:

`%LOCALAPPDATA%\BinTracker\database.json`

PostgreSQL is the planned central database before simultaneous multi-PC deployment.
The PostgreSQL provider is not enabled yet, so this release has no extra server/install requirement.


## v0.2.0-alpha.1
Customer Management is now functional from the left navigation.

## v0.2.0-alpha.2

Customer codes are now the primary visible identifier in Customer Management. Select a customer and use **Customer Statement** to generate an audited PDF for a chosen period.
