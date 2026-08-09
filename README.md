# BinTracker

BinTracker is a Windows desktop application for managing returnable containers (bins, pallets and other reusable assets). It replaces the previous Excel workbook with a multi-user SQLite-backed application that records every movement and provides reporting, auditing and customer balance tracking.

## Current status

**Version:** v0.2.0-alpha.7.2.9

Implemented:

- Windows desktop application (.NET 8 WinForms)
- SQLite database (automatic daily persistence)
- Customer management
- Batch Entry and Single Entry workflows
- Running customer/container balances
- Dashboard
- Reporting framework
- User authentication
- Administrator / Operator roles
- Password policy and forced password changes
- Audit trail
- Customer statements
- Unsaved batch draft recovery
- Logout support
- Embedded navigation icons
- Show/Hide password controls
- Automated unit and integration tests

## Project structure

- `src/BinTracker.Core` – domain models
- `src/BinTracker.Data` – SQLite persistence
- `src/BinTracker.Services` – business logic
- `src/BinTracker.WinForms` – desktop UI
- `tests` – unit and integration tests
- `docs` – changelog and release notes

## Development principles

- Strong separation of UI, business logic and data access.
- Nullable reference warnings fixed.
- Commented code for non-obvious logic.
- Every significant feature accompanied by tests where practical.
- Incremental alpha releases with release notes.

## Planned before beta

- Rich reporting
- Statement printing/export
- Email/SMS reminder integration
- Improved dashboard analytics
- Backup/restore tools
- Installer and automatic updates
