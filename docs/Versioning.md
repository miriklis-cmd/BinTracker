# Versioning

BinTracker uses semantic pre-release versions such as `0.4.0-alpha.21`.

## Single source of truth

The release version is defined in the repository root:

`Directory.Build.props`

```xml
<Version>0.4.0-alpha.21</Version>
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


## Pre-release milestone policy

Version milestones follow **real scope boundaries**, not a fixed count of phases.

Examples:

- `0.4.0-alpha.*` — current operational completion / Reports phase.
- A substantial new workstream such as Dashboard may start a new minor milestone, e.g. `0.5.0-alpha.1.1`.
- Communications, correction/reversal, production operations, PostgreSQL/multi-user or other major workstreams may each receive their own `0.x.0` milestone when their scope justifies it.
- It is valid to use `0.10.0`, `0.11.0`, etc. if additional pre-1.0 milestones are needed.

Do not force all remaining work into a pre-decided `0.5 → 0.6 → 1.0` sequence.

## Alpha increment rule

For **new milestones from the next clean milestone onward**, use:

- Feature/development increment: `0.5.0-alpha.1.1`
- Next feature/development increment: `0.5.0-alpha.2`
- Bug/polish fix specifically to alpha 2: `0.5.0-alpha.2.1`
- Another fix to alpha 2: `0.5.0-alpha.2.2`
- Next feature after those fixes: `0.5.0-alpha.3`

Hard rule: **maximum two numeric components after `alpha`** for the new scheme.

Do not create forms such as:

- `alpha.3.1.1`
- `alpha.4.2.1`
- deeper nested alpha suffixes.

The existing `0.4.0-alpha.20.0.x` history is retained as historical truth and is **not renumbered retrospectively**. The clean rule begins at the next genuine milestone.

## Bug-fix builds in the current legacy 0.4 series

Until the next clean milestone starts, a direct bug-fix to an already-issued legacy candidate may use one additional `.1`, `.2`, etc. suffix solely to avoid rewriting history. This is transitional only and must not continue into the new milestone scheme.


## Package identity gate

For every packaged candidate, all of the following must match exactly:

- ZIP filename version;
- the ZIP's single top-level folder version;
- `Directory.Build.props` `Version`;
- `Directory.Build.props` `InformationalVersion`;
- build-script displayed version;
- README / Known Issues / current Release Notes / Roadmap current baseline.

Packaging mismatch is a release-blocking audit failure.


## Mechanical package gate

`Package-BinTracker.ps1` must create `BinTracker-v<Version>.zip`, stage the sole root as `BinTracker-v<Version>`, reopen the archive, and verify embedded Version/InformationalVersion before a package is treated as releasable. `docs/RequirementsAcceptanceRegister.md` is also part of the source audit gate.
