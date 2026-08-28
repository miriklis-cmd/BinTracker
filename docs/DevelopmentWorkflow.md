# Development Workflow

## Planned lineage migration hard gate

Before any lineage migration touches a database, BT-CORR-030 and BT-OPS-011/012 require read-only relationship preflight and a unique provider-consistent recovery backup verified by hash, integrity/FKs/schema/table counts and preflight equivalence. Failure aborts before schema writes. This documentation freeze authorizes no migration and existing developer backup tools do not substitute for the gate.

## Implementation passes

For each meaningful BinTracker change:

1. Implement code immediately when the user asks to implement; do not substitute a mockup unless explicitly requested.
2. Add/retain automated regression coverage where practical.
3. Perform the full documentation audit defined in `docs/Testing.md`, including `docs/RoadmapCoverageMatrix.md`.
4. Produce the candidate build/ZIP.
5. Run `Build-BinTracker.bat` on Windows as the canonical restore/build/test gate.
6. Perform the required manual smoke level:
   - UI → Full;
   - business logic → Targeted;
   - UI + logic → Full;
   - reports → real preview/print;
   - importer → real workbook when applicable.
7. Only after acceptance should the candidate be committed/pushed as the accepted checkpoint.

## Build gate

`Build-BinTracker.bat` is the canonical local gate. It must return failure when restore, compilation or automated tests fail; a misleading success banner is itself a release-blocking defect.

## Documentation discipline

Current-state documents must describe the current product, not accumulate contradictory historical alpha notes. Release history belongs in `docs/CHANGELOG.md`.


## Version consistency

`Directory.Build.props` is the version source of truth. README, Known Issues, Test Checklist and current Release Notes must match it for each packaged candidate.


## Mandatory full-build audit gate

Every packaged BinTracker build must pass a **full audit before the ZIP is handed to the operator**. This is mandatory for feature builds, bug-fix builds, documentation-only builds and release candidates.

For every build:

1. **Implementation/state audit**
   - review the files changed in the build;
   - inspect directly affected call sites, services, UI construction, persistence and tests;
   - check for obvious regressions or inconsistent parallel implementations.

2. **Full Markdown audit**
   - enumerate every `*.md` file in the package;
   - review every current-state document for stale status, obsolete UI wording, completed items still shown as pending, duplicated roadmap entries and contradictions;
   - preserve historical changelog entries as historical truth rather than rewriting history.

3. **Roadmap coverage audit**
   - reconcile `docs/Roadmap.md` with `docs/RoadmapCoverageMatrix.md`;
   - verify major agreed pre-v1/post-v1 workstreams have not disappeared;
   - update milestone order/status when scope changes.

4. **Version audit**
   - `Directory.Build.props` is the source of truth;
   - reconcile README, Known Issues, Test Checklist, current Release Notes and any other current-version references.

5. **Requirements/spec audit**
   - reconcile Functional Specification, Business Rules, Testing, Known Issues and Tech Debt with the actual implemented behaviour;
   - remove superseded current-state wording while keeping historical changelog entries.

6. **Test audit**
   - add/update automated regression coverage where practical;
   - explicitly classify the operator test requirement as:
     - automated build/test only;
     - targeted smoke test; or
     - full smoke test;
   - UI changes require a full smoke test;
   - business-logic changes require at least a targeted smoke test.

7. **Release documentation**
   - update `docs/CHANGELOG.md`;
   - replace `docs/RELEASE-NOTES.md` with the current candidate's notes;
   - record material documentation reconciliation in `docs/DocumentationAudit.md`.

A build is **not complete** merely because code was changed or static checks passed. The audit is part of the build itself.




## Local SDK and MSBuild worker policy

BinTracker targets `net8.0` / `net8.0-windows`, but the local build SDK is not pinned to .NET 8. The development machine currently has .NET SDK 10.0.400 installed and that SDK can build the project's .NET 8 target frameworks.

Do not add a restrictive `global.json` unless the required SDK is first confirmed installed on the development machine.

`Build-BinTracker.bat` hardens local builds against intermittent MSBuild SDK-resolver worker shutdowns by:

- shutting down stale `dotnet` build-server processes before restore;
- setting `DOTNET_CLI_USE_MSBUILD_SERVER=0`;
- setting `MSBUILDDISABLENODEREUSE=1`;
- passing `/nr:false`;
- using single-node MSBuild (`-m:1`);
- using non-parallel NuGet restore (`--disable-parallel`);
- printing the resolved SDK version.

Restore/build/test commands use direct `command || goto :fail` guards. A failed command must never fall through to `BUILD SUCCESSFUL`.


## ZIP overlay stale-file handling

Extracting a newer BinTracker ZIP over an existing build folder overwrites matching files but does not remove files that were deleted from the newer package.

alpha.23.4.1 specifically self-heals the obsolete `global.json` created by alpha.23.3. The BAT deletes it only when its contents exactly identify the BinTracker-created SDK 8.0.100 / `latestFeature` pin. An unrelated/user-managed `global.json` is never deleted automatically.

For the cleanest manual workflow, deleting the old extracted BinTracker folder before extracting a new full ZIP remains preferable.


## Mechanical source/package-state audit

`Audit-BinTracker.ps1` is run by `Build-BinTracker.bat` before restore. It validates current version documents, the permanent Requirements & Acceptance Register (unique IDs and approved scope/status values), required documentation, known stale/contradictory current-state phrases, unexpected `global.json`, major roadmap workstreams and selected implemented source paths. `Package-BinTracker.ps1` separately creates and reopens a version-authoritative ZIP and verifies the sole root folder plus embedded Version/InformationalVersion.

Before a ZIP is handed to the operator, packaging must additionally verify that the ZIP filename, single top-level folder, `Version` and `InformationalVersion` all identify the exact same candidate. A mismatch is a failed artifact and must not be delivered.
