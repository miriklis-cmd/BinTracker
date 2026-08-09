# BinTracker Architecture

BinTracker uses a layered .NET 8 architecture:

- **BinTracker.Core** — domain models and shared types.
- **BinTracker.Data** — Entity Framework Core / SQLite persistence.
- **BinTracker.Services** — application/business services.
- **BinTracker.WinForms** — desktop UI.
- **Tests** — unit and integration coverage.

## Dependency direction

The WinForms UI depends on Services/Core rather than directly embedding business rules.
Services coordinate application behaviour and persistence.
Data owns database-specific implementation.

## Application state

Short-lived UI state such as an unsaved Batch Entry draft is stored in `ApplicationState`.
Committed operational movements remain in SQLite.

## Security

Authentication, role enforcement, password changes and audit events are implemented through Services rather than individual form-specific database logic.

This document should be expanded before beta with diagrams and explicit dependency boundaries.
