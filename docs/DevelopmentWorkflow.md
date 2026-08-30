# Development Workflow

## Characterization and structured-input hard gate

Before modifying, replacing, refactoring or placing new authoritative logic beside accepted behaviour, identify tests that precisely characterize the affected behaviour. If adequate coverage is missing, add and run characterization tests before the change and rerun them afterward. When correcting a known defect, retain useful characterization separately from the expected-behaviour regression test; characterization never makes a defect permanent.

Every parser, deserializer, importer, migration reader, recovery-manifest reader or other structured persisted-input boundary must have format-appropriate adversarial tests. The boundary must either accept and fully validate the input or fail with a controlled outcome and no partially accepted or persisted state. Exercise relevant truncation, omission, duplicates, wrong types, unsupported values, invalid IDs/relationships/dates, overflows, malformed shape, inconsistent checksums and mid-operation cancellation/failure; do not add irrelevant cases for formats the component does not consume. In particular, an undefined persisted enum value in an older schema must not acquire a new meaning merely because a later schema allocates that number.

## Planned lineage migration hard gate

Before any lineage migration touches a database, BT-CORR-030 and BT-OPS-011/012 require read-only relationship preflight, exclusive database-scoped upgrade ownership and a unique provider-consistent recovery backup verified for the exact source by hash, integrity/FKs/schema/table counts and preflight equivalence. Failure aborts before schema writes. Dormant schema-17 migration tests use these typed prerequisites against isolated databases; normal production startup remains schema 16 and existing developer backup tools do not substitute for the gate.

## Conversation Context Capacity / Continuity Hard Gate

Before beginning another substantial implementation, audit, refactor, packaging operation, architecture change, multi-step debugging task or comparable work unit, ChatGPT/Codex must conservatively assess whether the current conversation has become large enough that context-window exhaustion or forced rollover is a meaningful risk. Do not claim or imply that an exact remaining-token or remaining-context counter exists unless the product actually exposes one.

Warn early, while enough context remains to construct a reliable continuation checkpoint. Use wording substantially like:

> This chat is becoming context-heavy. We can continue the current coherent task, but before beginning another substantial piece of work we should create/update the repository continuation checkpoint so an unexpected chat cutoff cannot lose project state.

A warning does not automatically stop active work. Where practical, finish the current coherent work unit, complete its directly associated verification and record the resulting state. Once meaningful context pressure has been identified, however, do not begin another major work unit until continuity is protected.

Before a deliberate rollover caused by context pressure, create or update the durable repository continuation record at `docs/CONTINUATION.md`. Do not create an empty or meaningless placeholder merely because this rule exists; create or update the file when an active continuity checkpoint is actually needed.

The continuation record must be intentionally extensive and self-contained. Where applicable, it must record:

- the current BinTracker version, branch and HEAD;
- the exact worktree, staged, modified and untracked state;
- the exact active task and original objective;
- completed work, partial work and the exact stopping point;
- accepted functional and UI/UX behaviour;
- accepted architecture decisions and data-semantics/integrity rules;
- accepted security, testing and release decisions;
- decisions and behaviour that must not regress;
- approaches considered or rejected and why important approaches were rejected;
- provisional or incomplete experiments that must not be mistaken for accepted work;
- outstanding work and exact recommended next steps;
- unresolved bugs and symptoms;
- compiler warnings and test failures, counts and results;
- source, audit and release-gate failures;
- relevant technical debt;
- current build, test, audit/source-gate, release and package state;
- relevant files, classes, services, tests, scripts, migrations and documents;
- useful commands already run and their material results;
- commands and tests the next session should run before modifications;
- architecture, data-integrity, concurrency, security and compatibility constraints;
- traps, subtle behaviour and anything the next session must preserve or must not assume.

Do not optimise the rollover handoff for brevity. A new ChatGPT/Codex session with repository access but no access to the previous conversation must be able to understand the project state, understand why important decisions were made, verify the baseline and continue safely without asking the user to reconstruct the old conversation. Reference stable authoritative repository documents instead of copying entire specifications where appropriate, but explain why those documents matter and record current-session state that they do not contain.

Before declaring the rollover handoff complete, explicitly ask:

> Could a new session that cannot see this conversation safely continue this exact work using only the repository and this handoff?

If the answer is no, expand the handoff. This is a hard gate.

At the start of a new session, before modifying the repository, read the applicable `AGENTS.md`, this workflow including this continuity gate, `docs/CONTINUATION.md` when it exists and represents an active continuation, and every other governing document normally required for the task. Mechanically verify repository reality rather than blindly trusting the handoff. Where applicable verify branch, HEAD, worktree, version, build baseline, test baseline and release/audit gates. If repository reality conflicts with the continuation record, stop and investigate before modifying application code.

This continuity gate supplements rather than replaces every existing BinTracker roadmap, roadmap-matrix, architecture, requirements, security, audit, testing, known-issues, technical-debt, versioning, packaging and release gate.

The user must not have to notice an almost-full conversation and request preservation manually. ChatGPT/Codex must proactively assess context pressure, warn early, protect continuity before beginning another major work unit once risk becomes meaningful, create the extensive repository handoff when rollover becomes advisable and provide the user with a matching detailed human-readable rollover summary. Never claim an exact context or token remainder unless the product exposes it.

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

`Audit-BinTracker.ps1` is a **mechanical audit**: it checks required files, permanent-ID integrity, version surfaces, selected forbidden strings/source gates and other mechanically expressible policy. A pass does not prove that every Markdown statement is semantically current.

**Semantic documentation reconciliation** is a separate human/agent review. Inventory every governed Markdown file; read current-state authorities relevant to the change; compare their claims with the requirements register, current implementation and acceptance evidence; distinguish implemented-static from human acceptance and genuinely pending work; preserve historical records as history; and stop on contradictory authorities rather than silently choosing one. Handoffs and `docs/DocumentationAudit.md` must report mechanical-audit and semantic-reconciliation results separately.


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
