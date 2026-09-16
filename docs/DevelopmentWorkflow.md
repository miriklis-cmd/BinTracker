# Development Workflow

## Characterization and structured-input hard gate

Before modifying, replacing, refactoring or placing new authoritative logic beside accepted behaviour, identify tests that precisely characterize the affected behaviour. If adequate coverage is missing, add and run characterization tests before the change and rerun them afterward. When correcting a known defect, retain useful characterization separately from the expected-behaviour regression test; characterization never makes a defect permanent.

Every parser, deserializer, importer, migration reader, recovery-manifest reader or other structured persisted-input boundary must have format-appropriate adversarial tests. The boundary must either accept and fully validate the input or fail with a controlled outcome and no partially accepted or persisted state. Exercise relevant truncation, omission, duplicates, wrong types, unsupported values, invalid IDs/relationships/dates, overflows, malformed shape, inconsistent checksums and mid-operation cancellation/failure; do not add irrelevant cases for formats the component does not consume. In particular, an undefined persisted enum value in an older schema must not acquire a new meaning merely because a later schema allocates that number.

## Planned lineage migration hard gate

Before any lineage migration touches a database, BT-CORR-030 and BT-OPS-011/012 require read-only relationship preflight, exclusive database-scoped upgrade ownership and a unique provider-consistent recovery backup verified for the exact source by hash, integrity/FKs/schema/table counts and preflight equivalence. Failure aborts before schema writes. Normal schema16-to17 activation uses these typed prerequisites through the single Data-owned startup coordinator; explicit schema16 fixtures and existing developer backup tools do not substitute for the gate.

## Conversation Context Capacity / Continuity Hard Gate

Before beginning another substantial implementation, audit, refactor, packaging operation, architecture change, multi-step debugging task or comparable work unit, ChatGPT/Codex must conservatively assess whether the current conversation has become large enough that context-window exhaustion or forced rollover is a meaningful risk. Do not claim or imply that an exact remaining-token or remaining-context counter exists unless the product actually exposes one.

Warn early, while enough context remains to construct a reliable continuation checkpoint. Use wording substantially like:

> This chat is becoming context-heavy. We can continue the current coherent task, but before beginning another substantial piece of work we should create/update the repository continuation checkpoint so an unexpected chat cutoff cannot lose project state.

A warning does not automatically stop active work. Where practical, finish the current coherent work unit, complete its directly associated verification and record the resulting state. Once meaningful context pressure has been identified, however, do not begin another major work unit until continuity is protected. In particular, when remaining context appears materially low, finish the current coherent unit where practical, ensure `docs/CONTINUATION.md` is current if continuation is needed, and do not start another major semantic change immediately before likely compaction. No arbitrary universal percentage substitutes for this judgment.

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

Before issuing or executing a new implementation slice, mechanically compare the active continuation/checkpoint sequencing claims with the exact starting HEAD. There must be one unambiguous current and next safe action; historical entries must be explicitly non-operative and must not remain textually capable of directing the new session. Reconcile stale sequencing before application-code implementation, and stop if the comparison exposes a substantive authority conflict rather than stale historical wording.

This continuity gate supplements rather than replaces every existing BinTracker roadmap, roadmap-matrix, architecture, requirements, security, audit, testing, known-issues, technical-debt, versioning, packaging and release gate.

The user must not have to notice an almost-full conversation and request preservation manually. ChatGPT/Codex must proactively assess context pressure, warn early, protect continuity before beginning another major work unit once risk becomes meaningful, create the extensive repository handoff when rollover becomes advisable and provide the user with a matching detailed human-readable rollover summary. Never claim an exact context or token remainder unless the product exposes it.

### Mandatory post-compaction repository re-anchor

Whenever ChatGPT/Codex reports or clearly indicates context compaction while a substantive BinTracker task is live, pause further substantive judgment and repository editing. Before continuing:

1. reread `AGENTS.md` and the active `docs/CONTINUATION.md`;
2. reread the authoritative task and frozen sections materially governing the current work;
3. mechanically recheck repository root, branch, HEAD, upstream, divergence, staging/worktree and version;
4. reread the current task ID and permitted scope;
5. inspect the current diff; and
6. compare the compacted summary with repository authority and confirm no conflict.

If a conflict appears, stop and investigate it before continuing. A compacted conversational summary is never authority and must not replace this repository re-anchor.

## Codex task identity, authority and anti-drift hard gate

### Canonical task/run identifier

Every substantive BinTracker Codex implementation, correction, review, audit, reconciliation, packaging or comparable repository pass uses exactly one canonical identifier:

- `BT-<roadmap-id>` for the roadmap task;
- `BT-<roadmap-id>-P<n>` for a major implementation/pass number;
- `BT-<roadmap-id>-FIX<n>` for a correction after review.

`P<n>` means implementation/pass only and `FIX<n>` means correction only. `R<n>` is prohibited in a new Codex task/run identifier and remains reserved for architecture/requirement labels such as Task20 R1..R7. Dates are not part of the canonical ID. Prefer the existing roadmap identifier and do not create a parallel naming scheme. Historical artifacts retain their historical names; new artifacts use the current canonical ID.

### Four-boundary working/return envelope gate

Every substantive task uses this role-separated envelope, with each applicable boundary on its own line:

```text
=== BINTRACKER CODEX WORKING: <TASK-ID> START ===
=== RETURN TO CHATGPT: <TASK-ID> START ===
=== RETURN TO CHATGPT: <TASK-ID> END ===
=== BINTRACKER CODEX WORKING: <TASK-ID> END ===
```

All four IDs must match exactly and pass the canonical identifier rule. Order is mandatory and each applicable boundary appears exactly once. `R<n>` remains prohibited in new run IDs; Task20 R1..R7 remain architecture labels only. The weaker two-marker prompt convention is not a parallel valid form.

Validate the two roles separately with `Test-BinTrackerCodexPrompt.ps1`:

- **Input/task validation** requires the attributable user task's first non-blank line to be its working START with the expected canonical ID. A marker in an arbitrary quotation, inline example, fenced example or later unrelated text cannot prove task identity.
- **Output/handoff validation** requires the role-separated complete envelope: the validated input working START plus the actual assistant/Codex return START, return END and working END. The returned handoff must begin with return START, end with return END then working END, use the exact same ID, preserve order and contain exactly one applicable instance of each output boundary. Prompt markers cannot supply missing returned-handoff boundaries.

Before substantive repository work, mechanically validate the exact attributable input/task text; locating/reading the prompt/session record and repository entry-state inspection are permitted solely to perform this gate. Before claiming completion, mechanically validate the actual returned handoff independently. Report `INPUT/TASK VALIDATION` and `OUTPUT/HANDOFF VALIDATION` separately. A valid input alone never proves a valid returned handoff. Stop on either applicable failure; do not infer or repair an intended ID conversationally.

### Authority precedence

When sources differ, use this hierarchy:

1. Governance hard gates: `AGENTS.md` and this workflow.
2. Frozen product/business/architecture semantics: `docs/Architecture.md`, `docs/BusinessRules.md`, `docs/FunctionalSpecification.md`, `docs/Database.md`, and permanent requirement semantics in `docs/RequirementsAcceptanceRegister.md`.
3. Roadmap/acceptance contract: `docs/Roadmap.md`, `docs/RoadmapCoverageMatrix.md`, `docs/RequirementsAcceptanceRegister.md`, and `TEST-CHECKLIST.md`.
4. Implementation/testing/status evidence: `docs/Testing.md`, `docs/DocumentationAudit.md`, `KNOWN-ISSUES.md`, `TECH-DEBT.md`, security/audit registers and other governed current-state evidence.
5. Continuation/handoff: `docs/CONTINUATION.md`.
6. Historical evidence/context: old investigations/handoffs/reviews, changelogs, conversation summaries, JSONL, generated notes and model memory.

A lower-authority source may report progress but cannot broaden, narrow or reinterpret a higher-authority rule. Multiple lower-authority sources repeating one interpretation are not independent corroboration. Historical statements remain historical unless formally re-adopted. If implementation or evidence appears to conflict with frozen authority, stop and investigate; never rewrite the frozen authority merely to match code. A current completion claim must trace to its authoritative acceptance rule.

### Acceptance trace before implementation

Before substantive production implementation, construct task working evidence equivalent to:

| Acceptance rule | Authoritative source | Enforcement path | Proof/evidence | Current status |
|---|---|---|---|---|

Every in-scope rule needs an authority; every implementation claim needs its actual enforcing path; every automatable criterion needs meaningful proof; manual/operator acceptance remains explicitly manual. Missing enforcement or proof is a `GAP`, and production implementation cannot begin while an in-scope gap is unexplained. Tests prove only the path they execute. UI/preview eligibility does not prove service/data execution enforcement, static source existence does not prove runtime activation, and historical evidence does not prove current behavior.

Task20 R2 is the standing example: a Reversed current line blocks current whole-root correction until disposition is separately sequenced. A preview reporting ineligible and a lower planner requiring decisions are insufficient. Authoritative `Correct + WholeRoot` execution itself must reject the trusted current root when any line is Reversed, including commands supplying Restore/RemainReversed decisions.

### Status-promotion gate

Before changing a roadmap, checklist or permanent requirement status:

1. enumerate every acceptance criterion represented by the status;
2. map each criterion to enforcing code or explicit non-code authority;
3. map each automatable criterion to actual evidence/tests;
4. identify every human/operator/manual item still pending;
5. select only the strongest existing repository status justified by that evidence.

Do not promote status merely because current tests are green, Codex reports completion, several Markdown files agree, the source audit passes, or implementation looks correct. Preserve the existing register vocabulary and checklist `[A]`, `[S]`, `[R]`, `[P]`, `[G]` definitions; never invent generic `[x]` acceptance for governed current status.

### Semantic-drift exit reconciliation

At the end of every substantive production pass, mechanically repeat `requirement -> enforcement path -> evidence -> status`, inventory every governed Markdown file, and explicitly answer:

1. Did a lower-authority document broaden, narrow or reinterpret higher authority?
2. Does any completion/status claim exceed implementation or evidence?
3. Does a current-state document contradict architecture or business rules?
4. Did implementation introduce a second authority?
5. Is a preview/UI check being mistaken for execution/data enforcement?
6. Was historical material silently promoted into current authority?
7. Are current counts/status summaries mechanically accurate?
8. Are static, manual/operator and release gates still distinct?
9. Did a test become green for the wrong reason?
10. Did accepted behavior disappear or change unintentionally?

Any material contradiction fails the exit gate and prohibits a completion claim. Regex/source-presence checks may protect deterministic wording or inventory but must never pretend to prove semantic architecture.

### Independent adversarial review and JSONL evidence

Independent ChatGPT review validates the repository, not the Codex handoff. Inspect authoritative requirements, actual code/diff, actual test results, attributable Codex session JSONL, and status/documentation changes. Codex handoff and JSONL are evidence, never authority.

For mutation/data-authority work, adversarial review may probe direct service bypass, UI/preview bypass, stale generation/input, exact and changed-payload retry, alternate legacy route, physical/logical ID collision, cancellation/failure midpoint, concurrent writer, corrupt persisted state and ambiguous/missing identity. For reporting/projection work it may probe inconsistent snapshots, raw fallback, wrong as-of date, correction/reversal/restoration chronology, stale/mismatched master data, invalid/unrooted lineage, screen/PDF/CSV disagreement and commits between reads. These are scoped review heuristics, not permission to expand unrelated work.

For substantive passes, use exact attributable raw Codex session JSONL when accessible; never reconstruct it. A JSONL captured while the substantive Codex turn is still active must be labelled exactly `PARTIAL SESSION PREFIX`. It is interim evidence only and must not be described as complete task-session evidence, a full session, a complete reasoning trail or proof of anything after its byte/record boundary.

Where possible, independent-review full-session JSONL is captured only after the substantive turn has ended, using a later read-only evidence-capture step/session. Record the original substantive session ID, source JSONL path, byte count, record count, final-record completeness, final event/timestamp, whether the substantive returned handoff is present, whether a context-compaction/summary-replacement event is present, and SHA-256. If the environment cannot supply full-session JSONL after completion, say so explicitly; never fabricate completeness.

When JSONL exposes context compaction or summary replacement, independent review must inspect that boundary. If the UI reports compaction but a captured JSONL does not contain it, record that the capture predates compaction and is partial; absence from a `PARTIAL SESSION PREFIX` never proves no compaction occurred.

### Coherent major-pass sizing

Default to the largest coherent roadmap pass that can be safely implemented and independently verified without combining unrelated acceptance boundaries. Do not create micro-passes merely for more checkpoints, names, repeated full suites or repeated document rewrites. Split at genuine unresolved architecture, migration/data transformation, transaction/authority, materially distinct release risk, manual/operator acceptance, or independent-verification boundaries. Larger scope never permits looser interpretation of frozen semantics.

## Execution-efficiency hard gate

Efficiency means broad safe milestones with narrow execution overhead. It removes duplicated ceremony, never engineering standards. Implementation scope remains as broad as frozen architecture, roadmap sequence and acceptance boundaries safely permit; do not fragment coherent work into unnecessary micro-passes. After mandatory core authority is read, use targeted authority reading, characterization, development tests, source searches and one stabilized documentation reconciliation rather than repeating whole-repository work without a material reason.

### Codex responsibilities and failure-boundary coverage

Reserve Codex primarily for repository-local reasoning and editing: inspect relevant source and authority, identify characterization, implement production/tests, run focused development tests, investigate failures, perform risk-relevant source audits, reconcile documentation after source stabilization and report genuine uncertainty or defects. Codex must not routinely spend its constrained working window duplicating mechanical work assigned to the operator or independent reviewer.

When Codex introduces or changes a state transition, transaction, migration, replacement, publication, cleanup, retry, concurrency, idempotency, backup, recovery or persistence boundary, it **must** add and run focused tests for the material success and failure paths relevant to that changed boundary. As applicable, cover failure before publication; failure after publication but before cleanup/completion; retry after partial external cleanup; cancellation at a newly changed boundary; truthful persisted-state reporting after irreversible publication; cleanup failure after otherwise successful work; and idempotent replay/re-entry. This targeted rule is not a reason to run every broad suite repeatedly, and efficiency never excuses missing failure-path correctness.

### Canonical BAT and full-suite ownership

`Build-BinTracker.bat` remains the mandatory final candidate-level source-audit, restore, build and automated-test gate, but it is **Jack/operator-run by default**. The normal sequence is:

1. Codex completes the final uncommitted candidate.
2. Codex runs the focused characterization and development tests materially required by the changed behavior.
3. Codex returns the candidate for independent review/operator validation.
4. Jack runs `Build-BinTracker.bat` locally on Windows against that exact final uncommitted candidate and provides the exact output to independent ChatGPT.
5. Independent ChatGPT verifies the BAT output against the candidate/diff as canonical mechanical evidence.
6. No staging, commit or push occurs unless the canonical BAT passes.

A successful BAT must establish the script's source/mechanical audit, restore and build passes; zero compiler warnings/errors; every UnitTest and IntegrationTest passing; and zero failed/skipped tests. Any source or documentation change after a successful BAT invalidates that evidence and requires Jack to rerun the BAT. Codex may run the BAT only when explicitly instructed for a specific reason, such as the operator being unable to run it; it must not run BAT merely to duplicate operator evidence.

During implementation prefer focused tests. Codex must not routinely run full UnitTests, full IntegrationTests and then BAT in the same pass when BAT will repeat both suites. A broader/full suite remains justified for unusually broad impact, focused evidence suggesting cross-cutting risk, composition/DI/startup changes affecting many consumers, an explicit repository pre-BAT requirement, or a fix after broad evidence that needs wider regression confirmation. Otherwise leave the canonical full suites to the operator-run BAT.

### Authority, search and documentation efficiency

At normal task entry Codex always reads `AGENTS.md`, the required sections of this workflow, the active `docs/CONTINUATION.md` section and the specific authoritative sections relevant to the milestone. For other documents, search by requirement IDs, milestone names and architectural terms, read matching authoritative sections, and expand to full-document reading only when context, ambiguity or contradiction requires it. Context compaction, continuation recovery, contradictory repository state or uncertain authority may require broader rereading under the existing continuity hard gate. Do not reread every Markdown file merely because it exists.

Whole-codebase searches must target the changed architectural risk, such as startup authority, old numeric authority, dormant-writer registrations, direct persistence bypasses, legacy mutation routes or duplicated business rules. Do not repeat an unchanged broad search after every small edit; rerun the relevant search once near final source stabilization when needed.

Prefer this documentation sequence: characterize; implement; focused validation; stabilize source; reconcile current-state documentation once; then perform a final targeted consistency check. Edit documentation earlier only to preserve a safety-critical decision. Historical evidence stays truthful, and implementation without required retained-data/operator/manual evidence never justifies status inflation.

### Mechanical and independent-review ownership

By default, Codex does not use its reasoning window for final Git-state capture, SHA-256 generation, review ZIP creation, byte-equality verification, commit/push script construction or post-push GitHub verification. Jack/operator owns the final BAT, mechanical packaging/hashes when an external helper exists, and guarded local commit/push execution. Independent ChatGPT owns semantic diff/source review, architecture and frozen-requirement comparison, documentation/status review, full JSONL/governance review, commit authorization and post-push GitHub verification. Codex may create review artifacts when specifically requested and no external helper exists, but that is exceptional rather than preferred.

Codex still performs reasonable self-checking and must not knowingly return an inconsistent candidate, but it does not replace deep independent adversarial review. Keep the handoff concise and evidence-focused: repository state, change/reason, focused characterization/validation, changed-file inventory, limitations, unexpected findings, excluded scope and session ID/JSONL path. Do not repeat mechanically derivable facts unless they explain a risk or decision.

Operator-provided BAT output is candidate-level mechanical evidence only for the exact source/document state on which it ran. Independent ChatGPT must compare it with the repository/diff and review candidate; any later candidate-file change requires another BAT run.

### No weakening

This policy does not weaken characterization-before-change, frozen architecture/roadmap compliance, security/audit gates, migration safety, backup/recovery truthfulness, provider-neutral business semantics, version consistency, zero-warning and zero-failure/zero-skip canonical acceptance, independent review before commit, Git-write restrictions, continuity/context-compaction gates, retained-database rehearsal or Windows/operator acceptance. If efficiency and correctness conflict, correctness wins.

## Implementation passes

For each meaningful BinTracker change:

1. Implement code immediately when the user asks to implement; do not substitute a mockup unless explicitly requested.
2. Add/retain automated regression coverage where practical.
3. Run focused development tests, including the mandatory changed-boundary success/failure paths above; run broader suites only when materially justified.
4. After source stabilizes, reconcile applicable current-state documentation once, including the audit defined in `docs/Testing.md` and `docs/RoadmapCoverageMatrix.md` when the candidate is packaged.
5. Return the final uncommitted candidate for independent review/operator validation; create mechanical review/package artifacts only when specifically requested or otherwise required.
6. Jack/operator runs `Build-BinTracker.bat` on Windows against the exact final candidate as the canonical restore/build/test gate; Codex execution is exceptional and explicit.
7. Perform the required manual smoke level:
   - UI → Full;
   - business logic → Targeted;
   - UI + logic → Full;
   - reports → real preview/print;
   - importer → real workbook when applicable.
8. Only after acceptance should the candidate be committed/pushed as the accepted checkpoint.

## Build gate

`Build-BinTracker.bat` is the mandatory canonical local gate and is Jack/operator-run by default against the exact final uncommitted candidate. Codex runs it only under explicit task-specific instruction. It must return failure when audit, restore, compilation or automated tests fail; a misleading success banner is itself a release-blocking defect. Any later candidate-file change invalidates the result and requires a rerun before Git write.

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
