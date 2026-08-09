# BinTracker Developer Guide

## Build

Run:

```bat
Build-BinTracker.bat
```

Expected result: clean build, zero warnings, zero failed tests.

## Projects

- `BinTracker.Core`
- `BinTracker.Data`
- `BinTracker.Services`
- `BinTracker.WinForms`
- Unit and integration test projects

## Working rules

- Put business rules in Core/Services, not directly in forms.
- Use async database/service methods where appropriate.
- Treat unexpected data loss as a high-priority defect.
- Add regression tests for repeatable logic bugs.
- Keep release notes, changelog and test checklist current.
- Prefer comments that explain why a decision exists.
- Preserve keyboard workflow and DPI/responsive layout behaviour.

## Release packaging

Bump the displayed application version, update documentation and package the whole repository into a versioned ZIP.

This guide should be expanded before beta with setup instructions, debugging notes, database migration procedures and deployment details.
