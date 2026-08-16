# Development Workflow

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
