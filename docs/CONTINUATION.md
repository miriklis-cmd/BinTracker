# Active BinTracker Continuation Checkpoint

**Status:** ACTIVE

**Purpose:** Preserve complete continuity before movement-lineage production implementation begins.

**Created:** 29 August 2026 (Australia/Sydney)

**Implementation state:** The movement-lineage architecture is frozen in authoritative documentation, but production implementation has **not begun**.

This record is governed by **Conversation Context Capacity / Continuity Hard Gate** in `docs/DevelopmentWorkflow.md`. A new ChatGPT/Codex session must read the repository-root `AGENTS.md`, that workflow section and this active continuation before modifying the repository. This handoff supplements the authoritative requirements and architecture; it does not replace them.

## Mechanically verified repository baseline

- Repository root: `C:/Users/jackm/Desktop/build/BinTracker-Codex-Clone`
- Branch: `codex/movement-correction`
- HEAD: `faaafb205d9c0ee215026c92a4784a10582e689e`
- HEAD subject: `Freeze movement lineage architecture requirements`
- HEAD commit date: `2026-08-29T09:19:05+10:00`
- Upstream: `origin/codex/movement-correction`
- Upstream relationship when checked: `+0/-0`
- Version source of truth: `Directory.Build.props`
- Current version: `0.5.0-alpha.8.7`
- Assembly/File version: `0.5.0.0`
- Informational version: `0.5.0-alpha.8.7`

Before this continuation file was created, `git status --porcelain=v2 --branch` showed exactly two unstaged tracked modifications and no staged or untracked files:

- `AGENTS.md` — modified, unstaged;
- `docs/DevelopmentWorkflow.md` — modified, unstaged.

Those two changes are intentional governance work from the current session and must be preserved exactly. They add:

- the concise repository-root pointer making conversation-context continuity a hard gate; and
- the authoritative, detailed **Conversation Context Capacity / Continuity Hard Gate**.

This new `docs/CONTINUATION.md` is expected to appear as an untracked file until the user explicitly authorizes staging/commit. Nothing is staged. No commit or push has been authorized for these local governance/continuity changes.

If actual branch, HEAD, version, upstream or worktree state differs when a later session starts, stop and investigate before changing application code. Do not reset, discard, overwrite or auto-merge the three intended local documentation changes.

## Evidence levels and last known validation

Do not turn historical evidence into a newer claim.

### Last known accepted alpha.8 baseline

The last recorded canonical user-run gate for the accepted alpha.8 correction interaction state was:

- source/package-state audit passed;
- restore passed;
- build passed with 0 warnings and 0 errors;
- 310/310 automated tests passed, with 0 failed or skipped;
- the relevant Movement History selection/action synchronization, whole-batch auto-tick/auto-clear, manual no-op rejection and focus-stable confirmation behavior received Windows acceptance.

This evidence predates the documentation-only lineage freeze. It is the last known full build/test baseline; it is not proof that a full suite ran at the current documentation-freeze commit.

The current release notes also retain a manual Windows 11 1920x1080/150% and larger-display smoke requirement for the complete alpha.8.7 candidate. Do not infer broad visual acceptance beyond the explicitly recorded interaction checks.

### Documentation-freeze validation at current HEAD

The committed documentation freeze at `faaafb2...` changed no source, tests, schema or version. `docs/DocumentationAudit.md` records:

- 257 unique permanent requirement IDs;
- 26 governed Markdown files;
- the mechanical governance audit passed;
- targeted contradiction searches passed;
- `git diff --check` passed.

No newer full application build/test run was claimed for that documentation-only commit.

### Current local changes

The current governance and continuation Markdown edits are not application-tested and do not require an application build merely to establish continuity. Before implementation, mechanically verify the baseline and run the focused/full tests appropriate to each implementation slice. Before a distributable lineage activation, run the canonical `Build-BinTracker.bat`, mandatory audit, migration tests against copies of representative databases, and required Windows acceptance. Never claim those gates before they actually pass.

## Current development phase and original objective

The project is immediately before substantial production implementation of lineage-aware correction/reversal/restoration.

The original objective was to extend the safe but limited alpha.8 immutable correction/reversal workflow so whole-batch correction remains truthful after individual corrections, partial reversals, repeated corrections, restoration, mixed dates, partial no-ops and explicit `RemainReversed` decisions. Investigation was deliberately completed before code because a false lineage or balance can silently corrupt operational reporting.

The immediate next task, once explicitly authorized, is implementation planning/execution from the frozen specification. No production lineage entity, migration, resolver, command, projection or Restore UI exists yet.

## How the project reached this stopping point

### 1. Accepted alpha.8 foundation

Alpha.8 implemented immutable individual correction, ordinary reversal and repeated ordinary whole-physical-batch correction. Correction creates neutraliser/replacement evidence and preserves movement history; ordinary reversal creates an immutable reversing row. Existing automated and Windows checks established that repeated uniform physical replacement batches remained coherent, correction history/audit remained navigable, effective totals were correct under the alpha.8 suppression model, and selected-row/dialog interaction defects were corrected.

That implementation is safe for its accepted scope but does not represent the richer logical model below.

### 2. Why implementation stopped

Physical `MovementBatchId` proved insufficient as the sole continuing identity when a physical batch can contain members with different descendant states. The gap appears with:

- a member individually corrected while siblings are unchanged;
- partial ordinary reversal;
- repeated correction chains;
- restoration of an erroneously reversed line;
- mixed inherited/corrected dates;
- partial no-op generations;
- an explicit `RemainReversed` line;
- a whole-root result that cannot truthfully be represented by one common physical batch header.

The existing physical whole-batch eligibility guard must not simply be removed. It remains the safety boundary until logical lineage, migration and projections replace it coherently.

### 3. Architecture investigation results

The investigations established:

- immutable `BinMovement` forensic evidence;
- a stable `LogicalMovementBatch` root distinct from physical batch identity;
- permanent `LogicalMovementLine` membership for each original business line;
- root-wide, complete generations containing one state row for every permanent line after every substantive mutation;
- `CurrentGenerationNumber` as sole current-state and root-concurrency authority;
- explicit Active/Reversed logical state;
- corrected authoritative operational activity, distinct from immutable evidence;
- `PositionAsOf(D)` and `CurrentPosition` semantics;
- unified Correct/Reverse/Restore operation, idempotency and audit envelopes;
- root-wide optimistic concurrency for v1;
- optional correction-output physical batches only when truthful and complete;
- separate import correction ownership;
- no SQLite-trigger or provider-specific business semantics;
- WinForms as a client, never business/persistence authority.

### 4. Mandatory adversarial revisions

The adversarial review approved the design only after four mandatory revisions:

1. `LogicalMovementBatch.RootMovementBatchId` is the sole authority identifying the original physical Batch Entry. The correction-output association must not duplicate it.
2. `LogicalMovementLedgerLink.IntroducedByGenerationLineId` may be temporarily null only during transaction construction/backfill to resolve insertion order. It must be populated before an Active/ReadOnly state commits. Correctness cannot depend on deferred FKs or triggers.
3. Generation lines do not persist authoritative `BeforeValuesJson` or `ResultValuesJson`. Structured pointers/state are authority. The canonical versioned operation request JSON and field mask preserve intent, audit explanation and idempotency only.
4. Every lineage-aware numeric result validates the relevant complete current snapshots and projects them in the same read transaction/snapshot. Operational corruption never silently omits a root, falls back to raw arithmetic or returns a plausible partial number.

### 5. Final-freeze actual-data preflight

The configured schema-v16 user-like SQLite database was inspected read-only. The recorded facts were:

- 495 `BinMovements`;
- 30 `MovementBatches`;
- 10 correction operations;
- 17 correction triples;
- 7 ordinary reversals;
- 28 deterministic ordinary logical roots;
- repeated individual corrections;
- repeated whole-batch replacement chains;
- partial reversals, including the retained Batch #30 fixture;
- no new lineage shape;
- no ambiguous or invalid ordinary root;
- no cross-domain import/generic edge;
- no global migration blocker.

This is evidence about that inspected database only. Every real database must repeat the read-only preflight immediately before migration. Never assume another user database has the same shape.

### 6. Final-freeze policies

- Persisted operational statuses: `Initializing`, `Active`, `ReadOnly`, `Invalid`.
- `Initializing` is transaction-construction only; finding it committed is critical corruption.
- `Active` has proven complete current state and is mutable subject to audit health/authorization.
- `ReadOnly` has a provable current projection but mutation safety/history is unsupported; it remains reportable and evidence-navigable.
- `Invalid` means current projection or ownership cannot be proved; affected numeric reads and mutations fail.
- Unsupported/legacy conditions are reason codes, not another status.
- Operational mathematical integrity and audit/compliance health are separate.
- After-the-fact isolated audit corruption does not intentionally suppress mathematically proven balances. It blocks affected mutations, Administrator Review and compliance/evidence outputs, and raises critical health.
- Operational-lineage corruption fails every affected numeric result.
- `OriginalDisplayOrdinal` is immutable presentation metadata, not identity or arithmetic authority.
- Legacy audit links are created only from a unique structured persisted-ID match; prose, timestamps and matching business values are unsafe.
- A verified, provider-consistent pre-lineage backup is mandatory before schema mutation.

### 7. Documentation freeze

Commit `faaafb205d9c0ee215026c92a4784a10582e689e` froze the approved architecture into authoritative repository documents. It added planned permanent requirements:

- `BT-ARCH-016..018`;
- `BT-AUD-015..017`;
- `BT-HIST-008..009`;
- `BT-CORR-018..033`;
- `BT-OPS-011..012`.

The freeze retained still-valid alpha.8 requirements and guards, kept `BT-CORR-013/014` post-v1, corrected PostgreSQL/API implementation to post-v1, and added a protected whole-codebase presentation/application/domain/infrastructure delineation audit after lineage acceptance.

### 8. PostgreSQL/API provenance conclusion

A read-only history review found that the earlier placement of PostgreSQL central deployment/authenticated API before v1 was accidental scope creep/documentation drift, not a technical prerequisite for a current v1 feature. There was no evidence that root CAS, idempotency, audit identity, business clock or same-snapshot reads require shipping a server database.

Approved scope remains:

- v1: WinForms -> client-neutral application/services -> SQLite infrastructure;
- post-v1: multiple clients -> authenticated API/application services -> PostgreSQL.

Pre-v1 must still preserve provider-neutral semantics, request-capable identity/clock/client contexts, concurrency/idempotency behavior, portable relational constraints and clean presentation/service/data boundaries. Do not implement PostgreSQL or an API “to be safe.”

### 9. Read-only implementation-plan verdict

The implementation mapping verdict was **READY TO IMPLEMENT WITH ORDERING CONSTRAINTS**. No semantic blocker remained, but activated persistence migration and incomplete application cutover would be unsafe.

An implementation-plan export exists outside the repository at:

`C:/Users/jackm/Desktop/BinTracker-Codex-Lineage-Implementation-Plan.txt`

Other currently verified external supporting exports are:

- `C:/Users/jackm/Desktop/BinTracker-Codex-Design-20260828-Lineage-Adversarial-Review.txt`;
- `C:/Users/jackm/Desktop/BinTracker-Codex-Design-20260828-Lineage-Final-Freeze-Review.txt`;
- `C:/Users/jackm/Desktop/BinTracker-Codex-PostgreSQL-PreV1-Provenance-Review.txt`.

These are supporting evidence only. The repository’s current authoritative Markdown and permanent requirements control. A previously referenced schema/transactions/migration export was not present at its expected Desktop path when this checkpoint was created; do not assume it exists or block on it because the approved decisions are frozen in-repository.

## Authoritative documents and why they matter

Read targeted current sections before implementation:

- `AGENTS.md` — repository hard gates and start-with-truth order.
- `docs/DevelopmentWorkflow.md` — workflow, build/package rules, lineage migration gate and conversation continuity gate.
- `docs/Roadmap.md` and `docs/RoadmapCoverageMatrix.md` — protected sequence/scope; lineage precedes security hardening and later work, while PostgreSQL/API implementation remains post-v1.
- `docs/RequirementsAcceptanceRegister.md` — permanent IDs, scope and planned/unimplemented status. Focus on `BT-ARCH-016..018`, `BT-AUD-015..017`, `BT-HIST-008..009`, `BT-CORR-018..033`, `BT-OPS-011..012`, plus still-valid `BT-CORR-001..015`.
- `docs/Architecture.md` — source-of-truth, service boundaries, portability, concurrency and presentation neutrality.
- `docs/BusinessRules.md` — correction, reversal, restoration, dates, reporting and operator semantics.
- `docs/FunctionalSpecification.md` — observable workflows and client-neutral preview/commit behavior.
- `docs/Database.md` — relational model, migration/preflight/backup, same-snapshot projection and delete behavior.
- `docs/Testing.md` and `TEST-CHECKLIST.md` — automated, migration, concurrency, failure-injection and Windows acceptance obligations.
- `docs/AuditCoverage.md` — atomic operation audit, review acknowledgement and legacy association.
- `docs/ImportWizard.md` and `docs/ReimportSafety.md` — ImportRun ownership and Replace/Correct safety.
- `TECH-DEBT.md`, `KNOWN-ISSUES.md`, `docs/SecurityHardeningRegister.md` — deferred work and protected gates that lineage must not erase.
- `docs/DocumentationAudit.md`, `docs/ReconciliationReport.md`, `docs/CHANGELOG.md`, `docs/RELEASE-NOTES.md` — evidence/history; they do not override current requirements.

## Frozen implementation-critical semantics

The detailed specification is in the documents/IDs above. The following is a non-negotiable implementation summary:

- `BinMovement` remains immutable ledger/forensic evidence. Generic correction/reversal/restoration never edits or deletes historical rows.
- A physical `MovementBatch` records rows genuinely persisted together with truthful shared header semantics. It is not continuing logical lineage identity.
- `LogicalMovementBatch` is the stable root. Roots never merge or split.
- `RootMovementBatchId` is the sole original physical-batch relationship. A single-entry root has none.
- `LogicalMovementLine` permanently represents one original ordinary business line and remains a root member while reversed.
- Every substantive mutation advances one root-wide generation containing exactly one generation-line state for every permanent line. Sparse current-state generations are forbidden.
- `CurrentGenerationNumber` is the sole current-state and root-wide optimistic-concurrency authority. Do not add a competing current pointer to each line.
- Active state emits exactly its current effective movement.
- Reversed state emits its last effective movement plus terminal ordinary reversal; these net to zero at and after the reversal business date.
- Corrected authoritative operational history is current retrospectively corrected truth ordered/filterable by `MovementDate`, not raw evidence and not “what was known then” bitemporal history.
- `PositionAsOf(D)` aggregates corrected authoritative activity with `MovementDate <= D`; `CurrentPosition` equals `PositionAsOf(business today)`.
- `GenerationNumber`, `MovementDate` and `CreatedUtc` are independent mutation, business and forensic orders.
- Restoration means the ordinary reversal was erroneous. Its baseline is the last legitimate effective state before that reversal; unselected fields inherit, explicit fields override.
- A legitimate later movement after a correct reversal is a new ordinary movement/logical line, not restoration.
- `RemainReversed` keeps the line as a root member with zero contribution and creates no fake movement.
- `CarriedForward` means untargeted by an individual operation. `AlreadyMatches` means considered by a whole-root operation but no field change was needed. Do not collapse them.
- A complete semantic no-op creates no generation, operation, movement or audit. Restoration is substantive even with no field override.
- Transformation role (`RootOriginal`, `CorrectionNeutraliser`, `CorrectionReplacement`, `OrdinaryReversal`, `Restoration`) is distinct from `MovementSource` provenance. Do not add `MovementSource.Correction`.
- A physical correction-output batch is optional and exists only when the complete newly created effective output for a physical-origin whole root is Active, newly created for every line, uniformly dated/directed, Batch provenance, non-import, truthfully headed and exactly membered. Neutralisers/reversals never belong to it.
- Mixed dates, partial changes, `AlreadyMatches`, `CarriedForward` or `RemainReversed` produce a logical generation without a fabricated physical batch.
- ImportRun/ExcelImport data remains in the Administrator Replace/Correct import domain. Generic Correct/Reverse/Restore is prohibited, and import replacement must fail closed if generic lineage/evidence references exist.
- Administrator Review acknowledges an already-effective atomic operation; it is not preapproval.
- No formal v1 period locking. `BT-CORR-013/014` remain post-v1. Valid historical dates at or before business today may be corrected/restored; future operational dates are prohibited.
- No SQLite trigger, SQLite rowid/locking behavior, client-local `DateTime.Today`, WinForms state or provider-specific business SQL may define correctness.
- WinForms collects intent/displays results only. Eligibility, planning, field precedence, no-op, authorization, CAS, idempotency, persistence, audit and projections live below presentation.
- PostgreSQL/API implementation remains post-v1; v1 architecture must remain portable without building those systems now.

## Retained Batch #30 acceptance fixture

Do not modify or delete the intentionally retained partially reversed Batch #30 during lineage development.

Conceptual state:

- Blue: active `IN 4`;
- Yellow: original `IN 1`;
- Yellow: ordinary reversal `OUT 1`;
- Yellow current contribution: zero.

Approximate movement IDs from prior accepted testing:

- `#1080` Blue `IN 4`;
- `#1081` Yellow `IN 1`;
- `#1082` Yellow `OUT 1` reversal.

These IDs are not a substitute for persisted identity. Verify the actual selected database, batch and movement relationships before relying on them.

Later acceptance must use this fixture where practical for:

- `RemainReversed`;
- Restore;
- whole-root correction;
- mixed dates;
- repeated whole-root and selected corrections;
- navigation from original, reversal, correction, restoration and no-physical-batch descendants;
- immutable physical Batch Detail;
- Movement History;
- Audit Detail and Administrator Review;
- Daily/Weekly/Monthly and current/Outstanding balances;
- Windows 11 1920x1080/150% and larger-display behavior.

## Approved implementation dependency chain

`characterise alpha.8`
-> `Core lineage contracts/enums`
-> `preflight/backup infrastructure`
-> `EF schema/migration`
-> `resolver/invariant validator`
-> `planner/physical-output policy`
-> `transaction-compatible audit primitive`
-> `generation-zero Single/Batch integration`
-> `unified Correct/Reverse/Restore commands`
-> `root CAS/idempotency`
-> `corrected activity / PositionAsOf`
-> `atomic cutover of all operational numeric consumers`
-> `audit/history detail`
-> `Restore/RemainReversed WinForms UI`
-> `failure injection/concurrency/full automated gates`
-> `Batch #30 Windows acceptance`
-> `protected whole-codebase layer-delineation audit`.

Dormant contracts, pure planner tests, read-only preflight and backup verification infrastructure may land earlier. An activated migration with incomplete entry/mutation/projection integration is **not** a safe distributable state. Registering new EF entities can also affect fresh `EnsureCreated` databases, so an “unactivated” schema checkpoint must remain internal/test-only.

The existing alpha.8 physical-batch guard remains until the coherent logical replacement is active. `EffectiveMovementQuery` remains authoritative until the coherent numerical cutover. Do not expose Restore UI early.

## Implementation-plan amendments from review

These refinements are mandatory even where the earlier exported implementation plan differs.

### 1. Concurrency and uniqueness translation

Root CAS is the conceptual concurrency authority. Two writers may both plan Generation N+1 and encounter `UNIQUE(root,generation)` before the CAS update. Infrastructure must translate any provider uniqueness/concurrency conflict representing this race into the same stable provider-neutral stale/concurrency-lost business outcome. Do not leak raw SQLite or future PostgreSQL exceptions.

Apply the same normalization to duplicate concurrent movement-change `ClientOperationId`: one commit wins; the loser reloads and either returns the identical committed result or reports a stable idempotency conflict.

### 2. Idempotency namespace

Do **not** invent a global `ClientOperationId` namespace across unrelated command domains unless a frozen permanent requirement explicitly requires it. Preserve existing domain/command-scoped idempotency. Movement-change IDs are unique within the movement-change operation envelope. A GUID coincidence with unrelated Single Entry, Batch Entry or Import work must not itself cause rejection.

This amendment supersedes the implementation-plan suggestion to check every operation-ID-bearing table globally.

### 3. Composite numerical snapshots

A user-visible result composed of related figures that are expected to reconcile must use one consistent read snapshot:

- Daily Print Pack Outstanding Summary and Daily Movement Detail share one read session/snapshot.
- Dashboard headline and attention/outstanding figures that form one refresh share one coherent projection snapshot.

Do not make each card/section independently query changing state and then present the combination as one reconciled result.

### 4. Statement boundary

For an inclusive statement range `StartDate..EndDate`:

- Opening = `PositionAsOf(StartDate - 1 day)`;
- Activity = corrected authoritative movements where `MovementDate >= StartDate && MovementDate <= EndDate`;
- Closing = `PositionAsOf(EndDate)`.

Test this explicitly so StartDate activity is not double-counted.

### 5. Migration failure and recovery

Do not replace the active database automatically after every migration error. If the migration transaction rolled back and the pre-migration active database remains valid:

- abort normal startup;
- preserve the verified backup;
- report and diagnose the failure;
- leave the valid source database in place.

Use controlled recovery only when the active database is unusable or a committed migration/postflight failure requires restoration. Then stop clients, preserve the failed migrated DB, revalidate backup/manifest/hash, restore while stopped, and validate integrity/FKs/schema/preflight before startup.

### 6. Concrete exclusive upgrade gate

Before implementation chooses/activates migration, specify and test a concrete v1 infrastructure mechanism that excludes competing BinTracker processes and database operations throughout preflight, backup, source/backup comparison, migration and postflight. “Exclusive gate” must not remain an undefined diagram box. This is SQLite/Windows infrastructure policy, not domain semantics, and must have failure behavior that aborts before schema writes.

## Current codebase findings and implementation seams

- `src/BinTracker.Core/Domain.cs` currently owns `MovementBatch`, `BinMovement`, `MovementCorrectionOperation`, `MovementCorrectionLine`, `ImportRun`, `AuditEvent` and persisted enums. Prefer a focused lineage Core file rather than further crowding it.
- `src/BinTracker.Data/BinTrackerDbContext.cs` owns current indexes/FKs. Existing useful constraints include unique batch/movement operation IDs, unique reversal target and unique correction-line original/neutraliser/replacement IDs.
- The current `BinMovement -> MovementBatch` FK uses `SET NULL`; intended immutable physical evidence requires `RESTRICT/NO ACTION` during lineage implementation, with actual deletion workflows tested.
- `src/BinTracker.Data/SqliteSchemaMigrations.cs` currently runs through schema 16.
- `src/BinTracker.Data/DatabaseSetup.cs` calls `EnsureCreatedAsync` before SchemaVersion/migration processing. This is the critical startup seam: the lineage preflight/backup coordinator must execute before `EnsureCreated` or any schema mutation on an existing database.
- `DatabaseConfiguration` resolves `%LOCALAPPDATA%/BinTracker/BinTracker.db` by default and supports configured SQLite paths.
- `DeveloperDatabaseService` uses `Microsoft.Data.Sqlite.BackupDatabase` and restart staging, but lacks the frozen migration-grade hash, manifest, integrity/FK/count and preflight-equivalence verification.
- `MovementCorrectionService` is the alpha.8 authority. Convert it into a temporary compatibility facade over focused lineage services; do not bypass it before replacement is coherent.
- `EffectiveMovementQuery` suppresses legacy correction originals/neutralisers and retains ordinary reversal pairs. It cannot model restoration or full logical generations.
- `AuditService.WriteAsync` opens an independent DbContext and cannot be used within the single atomic movement-change transaction. Add a caller-context/transaction-compatible audit appender or factory rather than a second audit system.
- Numerical semantics are fragmented: Daily/Weekly/Monthly/Market Floor substantially use `EffectiveMovementQuery`; Outstanding and several balance/Dashboard paths use raw arithmetic; statements use effective rows; Movement History is raw forensic evidence.
- `MovementHistoryReportForm` and correction dialogs currently derive some eligibility/result values. They may display hints but final authority must move to services.
- `ImportExecutionService` physically deletes superseded import movements during controlled replacement. It must fail closed when generic lineage/evidence references would make deletion unsafe.
- Existing failure-injection design in Import execution is a useful pattern for lineage transaction checkpoints.

## Expected persistence and service shape

The authoritative database details are in `docs/Database.md`. Implementation planning mapped these concepts:

- `LogicalMovementBatch` with status, sole `RootMovementBatchId` and current generation;
- `LogicalMovementLine` with immutable `RootMovementId` and presentation ordinal;
- `LogicalMovementGeneration` with root/number/predecessor/operation;
- `LogicalMovementGenerationLine` with complete state/action/field mask and structured movement pointers;
- `LogicalMovementLedgerLink` with one movement owner, role and introducing generation line;
- output-only logical-generation to physical-batch association;
- evolved existing `MovementCorrectionOperations` table for Correct/Reverse/Restore request/idempotency envelope;
- nullable unique primary operation FK on `AuditEvent` for new operations;
- legacy `MovementCorrectionLines` retained as forensic evidence, not populated as a second authority for new generations.

Expected persisted enum values must be explicit and never renumber old values. Existing `MovementCorrectionKind.Single=0` and `WholeBatch=1` remain; planned additions are Reverse and Restore. New statuses/actions/states/roles must match the frozen docs and tests.

Recommended focused client-neutral services include a lineage resolver, invariant validator, pure change planner, movement-change command service, corrected-activity/position projection, physical-output policy, migration preflight/classifier, transaction-compatible audit appender and no-op/test failure injector. Do not let `MovementCorrectionService` grow into another monolith.

## Numerical cutover scope

All related operational numeric consumers must switch coherently to the validated projection. At minimum inspect and migrate:

- Daily Movements;
- Weekly Movements;
- Monthly Summary;
- Market Floor;
- Outstanding Containers/AsOf;
- Customer Statements, including exact opening/activity/closing boundary;
- Daily Print Pack under one snapshot;
- Dashboard Today;
- Dashboard Outstanding/attention under one refresh snapshot;
- Customer screen balances;
- `BalanceService` and current-position calculations;
- Single/Batch Entry current/with-draft previews where they consume authoritative current position;
- import/reconciliation previews where authoritative operational position is used;
- PDF/CSV outputs through their service DTOs.

Movement History, Audit, physical Batch Detail and Import History remain immutable/forensic evidence views. They gain navigation/role/status context but must not become corrected-only activity.

Do not delete `EffectiveMovementQuery` until every operational consumer has moved, old accepted cases remain equivalent, restoration/new cases pass, and no hidden consumer depends on it.

## Transaction, concurrency and test expectations

One transaction must contain every generated ledger row, operation, generation and all generation lines, ledger ownership links, optional physical output batch/membership, primary audit, root CAS and current pointer advance. Nothing externally observable may survive partial failure.

The transaction-compatible audit primitive must use the caller-owned DbContext/transaction. Calling the current independent-context `IAuditService.WriteAsync` from a lineage transaction is unsafe.

Failure injection must cover at least:

- operation/idempotency reservation;
- generation insertion;
- each neutraliser;
- each replacement/restoration/reversal;
- generation-line creation;
- ledger-link creation;
- introduction-link completion;
- optional physical output batch/membership;
- audit creation;
- before CAS;
- after CAS but before commit.

After each injected failure, a fresh DbContext must prove no partial movements, generation, batch, audit/review, operation reservation or current-pointer change. Retry must succeed exactly once.

Concurrency/idempotency tests must include Correct/Correct, Reverse/Correct, Restore/Correct, whole/whole, different lines in one root, stale preview, concurrent identical operation ID, same ID/different fingerprint, lost response followed by a newer generation, report during mutation and import-replacement collision. Provider exceptions must be normalized to stable business outcomes.

Every numerical query must validate and project in one snapshot. Never silently omit an invalid root or raw-fallback. Operational invalidity fails affected numerical results. Audit-only corruption preserves proven numbers but blocks mutation, Administrator Review and compliance/evidence outputs and raises critical health.

## Safe implementation checkpoints

1. Characterize alpha.8 with tests before changing authority.
2. Add dormant Core contracts/enums and pure planner tests.
3. Add unactivated read-only preflight/backup verification and tests.
4. Develop EF schema/migration/backfill under explicit integration tests only; do not distribute this incomplete state.
5. Develop resolver/validator/planner/commands/projections against upgraded test DBs while production activation remains off.
6. Make one coherent backend activation containing startup gate/migration, generation-zero entries, all mutation writers, validators, corrected/position projections and every numerical consumer, import guard, atomic audit link and alpha.8 UI adapter.
7. Add logical-root preview, Restore/RemainReversed and navigation UI only after backend authority is complete.
8. Run failure/concurrency/full automated gates, representative-database migration rehearsal and Batch #30 Windows acceptance.
9. After lineage integration is accepted, perform the protected whole-codebase layer-delineation audit before subsequent major roadmap work.

Do not ship the schema-only or engine-only internal checkpoints. Do not remove old guards, activate partial lineage for only new rows, or switch only some reports.

## Approaches rejected or constrained

- Physical `MovementBatchId` as sole logical identity: rejected because partial/mixed descendants make it untruthful.
- Always creating a correction replacement batch: rejected because partial no-op, reversed lines and mixed dates violate batch invariants.
- Removing the alpha.8 whole-batch guard before lineage: rejected as unsafe.
- Sparse generations: rejected because complete current state and evidence of `AlreadyMatches`/`RemainReversed` would be ambiguous.
- Per-line merge concurrency in v1: rejected in favor of safe root-wide generation conflicts/re-preview.
- Full historical generation fabrication during migration: rejected because legacy chronology cannot always be proved; use truthful MigrationBaseline.
- Value/date/customer/quantity/text matching during backfill: prohibited; only structural persisted identity is authoritative.
- JSON before/result state as authority: rejected as duplicate truth.
- `MovementSource.Correction`: rejected because provenance and transformation role are separate.
- SQLite triggers or UI validation as integrity authority: rejected for portability/security.
- Full bitemporal reporting: rejected for v1; normal reports are current retrospectively corrected truth and Audit preserves action time.
- Formal v1 period locking: rejected without product evidence; post-v1 requirements remain.
- Generic correction of imported rows: rejected because ImportRun has its own replacement lifecycle.
- Shipping PostgreSQL/API pre-v1: rejected as documentation drift without technical dependency.
- Automatically restoring a backup for every rolled-back migration failure: rejected because it can overwrite a still-valid source database.
- A global cross-domain `ClientOperationId` namespace: not approved; idempotency remains command/domain scoped.

## Relevant outstanding work

No implementation has begun. Outstanding work is the complete dependency chain above, including:

- select and test the concrete exclusive SQLite/Windows upgrade gate;
- characterize current behavior and add lineage test fixtures/builders;
- create Core persisted types/enums without renumbering legacy values;
- implement migration-grade read-only preflight and verified backup/manifest/recovery;
- implement schema-v17-or-next lineage migration/backfill and postflight;
- implement root/line/generation resolver and invariant/health validators;
- integrate generation zero into Single/Batch Entry atomically;
- implement unified change planner/commands, CAS, retry and provider exception translation;
- implement physical output predicate;
- implement transaction-compatible audit and review/detail changes;
- implement corrected activity/PositionAsOf/current position under same snapshots;
- switch all numerical consumers coherently;
- implement logical-root/Restore/RemainReversed UI;
- execute failure, concurrency, migration, reporting and Windows acceptance;
- perform the protected whole-codebase layer audit after acceptance.

No current compiler warning, test failure or source-gate failure is known from the last verified full baseline. That is historical evidence, not a promise about future implementation. No current package represents lineage implementation. Version remains `0.5.0-alpha.8.7` and must not be bumped merely to start unaccepted work unless the versioning/release workflow later requires it.

## Next-session startup instructions

Before modifying any repository file, the next session must:

1. Read repository-root `AGENTS.md`.
2. Read `docs/DevelopmentWorkflow.md`, including the continuity and lineage migration hard gates.
3. Read this active `docs/CONTINUATION.md` completely.
4. Read current `docs/Roadmap.md`, `docs/RoadmapCoverageMatrix.md` and `docs/RequirementsAcceptanceRegister.md`.
5. Read the applicable lineage sections in `docs/Architecture.md`, `docs/BusinessRules.md`, `docs/FunctionalSpecification.md`, `docs/Database.md`, `docs/Testing.md`, `TEST-CHECKLIST.md`, `docs/AuditCoverage.md` and import safety documents.
6. Mechanically verify root, branch, HEAD, upstream, worktree/staging/untracked state and `Directory.Build.props` version.
7. Inspect the complete uncommitted diff for `AGENTS.md`, `docs/DevelopmentWorkflow.md` and this continuation. Preserve it; investigate any discrepancy before application-code changes.
8. Confirm no lineage implementation has already appeared unexpectedly. If it has, stop and reconcile it against the frozen requirements rather than layering work over it.
9. Before the first implementation modification, characterize the relevant alpha.8 behavior and decide which focused tests establish the baseline. Run broader gates in proportion to the slice; do not invent a current full-suite result.
10. Do not stage, commit or push without explicit user instruction.

Useful baseline commands:

```powershell
git rev-parse --show-toplevel
git branch --show-current
git rev-parse HEAD
git rev-parse --abbrev-ref --symbolic-full-name '@{upstream}'
git status --porcelain=v2 --branch
git diff --cached --name-status
git diff --name-status
git ls-files --others --exclude-standard
rg -n "<(Version|AssemblyVersion|FileVersion|InformationalVersion)>" Directory.Build.props
git diff -- AGENTS.md docs/DevelopmentWorkflow.md docs/CONTINUATION.md
git diff --check
```

Before implementation, use targeted searches to re-open the actual seams rather than trusting filenames alone:

- `src/BinTracker.Core/Domain.cs`;
- `src/BinTracker.Data/BinTrackerDbContext.cs`;
- `src/BinTracker.Data/DatabaseSetup.cs`;
- `src/BinTracker.Data/SqliteSchemaMigrations.cs`;
- `src/BinTracker.Data/DeveloperDatabaseService.cs`;
- `src/BinTracker.Services/MovementServices.cs`;
- `src/BinTracker.Services/MovementCorrectionService.cs`;
- `src/BinTracker.Services/EffectiveMovementQuery.cs`;
- `src/BinTracker.Services/Services.cs` audit/balance areas;
- report/customer/outstanding/import services;
- `src/BinTracker.WinForms/MovementHistoryReportForm.cs` and correction/reversal dialogs;
- the correction, migration, report, balance, import and audit tests.

## Continuity traps and non-assumptions

- Do not assume `docs/CONTINUATION.md` being present means its claims outrank repository reality.
- Do not assume the external design exports are authoritative or all exist.
- Do not assume current HEAD has passed a full 310-test gate; that result belongs to the earlier accepted alpha.8 run.
- Do not assume Batch #30 IDs without querying the selected database read-only.
- Do not run the app against user data merely to inspect it; startup can migrate/write.
- Do not run migrations or alter the retained database before verified preflight/backup/exclusivity exists.
- Do not activate EF lineage entities for an existing database without considering `EnsureCreatedAsync` ordering.
- Do not let new Manual/Batch entries exist without lineage after activation.
- Do not let old correction/reversal writers remain active after migration.
- Do not switch reports piecemeal into contradictory numbers.
- Do not make audit corruption suppress a mathematically valid balance, and do not let operational corruption produce a number.
- Do not treat a physical output batch as required for navigation, review, retry or lineage.
- Do not infer identity from display order or business values.
- Do not implement PostgreSQL/API/WinUI/customer portal/mobile as part of v1 lineage.
- Do not perform unrelated cleanup or the full whole-codebase layer refactor inside the lineage implementation; only fix leakage necessary to establish one authority, then run the protected audit later.

## Continuity completeness self-check

**Question:** Could a new session that cannot see this conversation safely continue this exact work using only the repository and this handoff?

**Answer:** YES. This record identifies the mechanical baseline, intentional dirty files, evidence levels, original objective, completed design/freeze work, accepted behavior and semantics, rejected approaches, implementation dependencies and amendments, exact stopping point, tests/gates, key files, risks, next commands and instructions. The next session must still verify repository reality mechanically and stop on discrepancies.
