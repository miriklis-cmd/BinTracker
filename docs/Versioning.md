# Versioning

BinTracker uses semantic pre-release versions such as `0.4.0-alpha.18.6`.

## Single source of truth

The release version is defined in the repository root:

`Directory.Build.props`

```xml
<Version>0.4.0-alpha.18.6</Version>
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
