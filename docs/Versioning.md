# Versioning

BinTracker uses semantic pre-release versions such as `0.4.0-alpha.19.12.4`.

## Single source of truth

The release version is defined in the repository root:

`Directory.Build.props`

```xml
<Version>0.4.0-alpha.19.12.4</Version>
```

The same value is used by:

- .NET assembly package/informational version metadata;
- the application's status bar through `AppVersion`;
- `Build-BinTracker.bat`;
- future packaging/installer scripts.

Do not add a second hard-coded application version to WinForms code.

## Releasing a new build

For a release:

1. Update `Version` and `InformationalVersion` in `Directory.Build.props`.
2. Update numeric `AssemblyVersion` / `FileVersion` only when appropriate.
3. Update CHANGELOG, release notes and TEST-CHECKLIST.
4. Run `Build-BinTracker.bat`.
5. Confirm the BAT header and BinTracker status bar display the same version.

## Acceptance and Git workflow

Typical alpha workflow:

1. Create a versioned candidate.
2. Run `Build-BinTracker.bat`.
3. Complete the required manual smoke level from `docs/Testing.md`.
4. Fix defects before acceptance; do not mark checklist items complete prematurely.
5. Commit/push the accepted checkpoint to GitHub.
6. Use tags/releases for meaningful milestones when appropriate.

The build script must truthfully propagate restore/build/test failures.
