# Active BinTracker Continuation Checkpoint

**Status:** ACTIVE

**Purpose:** Give a new session enough verified state, frozen movement-lineage semantics, dependency ordering and acceptance limits to prepare the next BIN-LIN-IMP-07 slice safely without relying on prior chat history or mistaking it for implemented work.

**Reconciled:** 2 September 2026 (Australia/Sydney)

This record is governed by **Conversation Context Capacity / Continuity Hard Gate** in `docs/DevelopmentWorkflow.md`. It supplements the authoritative requirements and architecture; it does not replace them. Repository reality always wins, and any conflict must be investigated before application-code changes.

## Exact current checkpoint and operative sequencing

- Repository root: `C:/Users/jackm/Desktop/build/BinTracker-Codex-Clone`.
- Branch: `codex/movement-correction`.
- **Verified baseline entering this pre-IMP-07 checkpoint:** `0e7752f45d74def016a945be5f4c9335aa39deeb` (`Reconcile IMP-06 checkpoint for IMP-07`), then synchronized with `origin/codex/movement-correction` at 0 ahead / 0 behind. This document does not predict the commit SHA that contains the checkpoint; after checkpoint commit/push, mechanically verify actual HEAD/upstream and divergence rather than inferring them from this record.
- **Reviewed IMP-05 implementation checkpoint:** `c0dfc7e51cae1296fd5a5da31876e54364901405` (`Add trusted movement mutation planner`). Do not describe this older implementation commit as the current repository HEAD.
- Version remains `0.5.0-alpha.8.7`; assembly/file version remains `0.5.0.0`.
- This checkpoint contains the bounded pre-IMP-07 schema-17 publication/live-validation lifecycle prerequisite and the approved 17-case schema-16 characterization at `tests/BinTracker.IntegrationTests/MovementEntryCharacterizationTests.cs`. The unrelated `.codex-evidence/` and `BIN-LIN-IMP-03A-HANDOFF.tmp` paths are not part of this checkpoint. No production Single/Batch generation-zero writer exists and IMP-07 production writing has not started.
- BIN-LIN-IMP-01, IMP-02/02A/02B/02C, IMP-03/03A/03B, IMP-04/04A, IMP-05/05B/05C, IMP-05D and IMP-06 are complete.
- **BIN-LIN-IMP-06 is committed, independently reviewed and source-gate verified.** It adds only the caller-owned transaction audit primitive described below and makes no runtime registration or authority change.
- **BIN-LIN-IMP-07 generation-zero Single/Batch integration is the next dependency-ordered slice.** Its direction is approved in principle, but it is not implemented and production/application-code edits are not yet authorized.
- The pre-IMP-07 schema-17 validation-lifecycle prerequisite separates strict migration-publication proof from live AlreadyComplete structural/current-health validation. It does not implement generation-zero entry writing or activate schema 17.

Normal startup and runtime authority remain schema 16 and the accepted alpha.8 correction/reversal model. Schema 17 is implemented only as a dormant, explicit, unregistered migrator used by isolated tests. There is no production lineage writer, lineage operation-envelope persistence, root CAS/idempotency execution, runtime/DbContext registration, startup migration activation, corrected-activity/report projection cutover, Restore UI or other WinForms cutover.

## Completed-checkpoint summary

Git history and `.codex-evidence/` retain the detailed development diaries, intermediate compiler/test failures and review exports. They are evidence, not operative sequencing. The completed work is:

1. The alpha.8 foundation delivered immutable individual correction, ordinary reversal and repeated uniform whole-physical-batch correction, with the existing physical-batch eligibility guard and `EffectiveMovementQuery` still authoritative at runtime.
2. The architecture/design freeze established stable logical roots and lines, complete generations, restoration and `RemainReversed`, root concurrency, corrected activity/`PositionAsOf`, provider/client neutrality, fail-closed migration and health semantics, and permanent requirements `BT-ARCH-016..018`, `BT-AUD-015..017`, `BT-HIST-008..009`, `BT-CORR-018..033` and `BT-OPS-011..012`.
3. IMP-01 added dormant provider-neutral Core lineage identities, statuses, actions, roles and field masks with persisted-value guards.
4. IMP-02/02A added dormant read-only schema-16 preflight, alias-safe per-physical-database shared/exclusive upgrade coordination, exact-source verified recovery backup/manifest/checksum evidence and controlled recovery classification. IMP-02B permanently allocated `MovementCorrectionKind.Reverse=2`, `MovementCorrectionKind.Restore=3` and the schema 16 -> 17 lineage migration without production activation. IMP-02C froze the schema-17 migration-population contract, including zero fabricated historical physical-output rows.
5. IMP-03 added the explicit dormant schema-16-to-17 migrator/backfill/postflight and failure-injection coverage. IMP-03A made every schema-16 correction-operation `Kind` outside historical values 0/1 a database-wide blocker so corrupt values cannot acquire schema-17 Reverse/Restore meaning. IMP-03B reconciled evidence classifications without converting static evidence into operator acceptance.
6. IMP-04/04A added an unregistered application-facing current-root resolver plus immutable, non-forgeable validated models and a provider-neutral current-snapshot invariant validator. It loads one selected root and required current proof in one read transaction, fails closed on malformed identity/state, and performs no full-history diagnostic scan or runtime registration. Migration postflight reuses current validation while retaining its separate global migration-only checks.
7. IMP-05/05B/05C added the dormant infrastructure-internal trusted planning materializer and pure provider-neutral complete-generation mutation/physical-output planner. The corrected boundary requires explicit per-Reversed-line Restore/RemainReversed decisions, line-specific restoration overrides, complete existing or typed plan-local result pointers, generic import/adjustment exclusion, a separate authoritative business date, deterministic persisted reversal-date proof, exact applied-field masks and artifact-free complete no-op handling. Each current Reversed terminal must be proven to reverse the exact persisted LastEffective movement, be equal-and-opposite for customer/container/quantity, have Manual provenance, have no ImportRun or physical-batch membership, and have no future current dates. Active current effective dates also cannot be future-dated.
8. IMP-05D reconciled current documentation after independent external source/diff approval and the canonical build. Commit `d1cc1d1...` is documentation-only relative to reviewed implementation checkpoint `c0dfc7e...`; it changed no application source, tests, schema, version, requirements or runtime behavior.
9. IMP-06 adds `TransactionAuditAppender` at the Data persistence boundary. It accepts one new `AuditEvent` and a caller-owned `BinTrackerDbContext` with an active caller-owned transaction, tracks exactly that event, and performs no context/transaction creation, save or commit. It is deliberately unregistered and does not alter the existing independent `AuditService.WriteAsync` path. Independent review approved this bounded implementation, and the subsequent canonical BAT passed 443/443 automated tests with 0 failed, 0 skipped, 0 warnings and 0 errors.
10. The current bounded pre-IMP-07 prerequisite preserves strict MigrationBaseline-only publication postflight for the actual 16 -> 17 transaction, while schema-17 AlreadyComplete uses separate structural/current-health validation and the existing provider-neutral current-root validator. A valid mixed Baseline+Initial database is accepted; malformed native current lineage remains rejected. Schema 17 remains dormant and IMP-07 writing is not implemented.

The committed IMP-06 checkpoint contains exactly these implementation-slice files relative to `e08acc9f...`:

- source: `src/BinTracker.Data/TransactionAuditAppender.cs`;
- tests: `tests/BinTracker.IntegrationTests/TransactionAuditAppenderTests.cs` and the focused BT-AUD-015 constraint proof in `tests/BinTracker.IntegrationTests/LineageSchema17MigrationTests.cs`;
- current-state reconciliation: `KNOWN-ISSUES.md`, `TEST-CHECKLIST.md`, `docs/Architecture.md`, `docs/AuditCoverage.md`, `docs/CONTINUATION.md`, `docs/DocumentationAudit.md`, `docs/Roadmap.md`, `docs/RoadmapCoverageMatrix.md` and `docs/Testing.md`.

The pre-IMP-07 checkpoint content is limited to the lifecycle prerequisite, its focused tests and directly affected documentation, plus the approved characterization described above. `.codex-evidence/` and `BIN-LIN-IMP-03A-HANDOFF.tmp` remain unrelated evidence outside this checkpoint.

The original IMP-05 edit missed the required pre-edit alpha.8 characterization ordering. IMP-05B ran all 37 `MovementCorrectionWorkflowTests` before its correction edits and again afterward as truthful recovery evidence; this did not retroactively satisfy the missed ordering. IMP-05C ran the same 37-test suite before and after its own correction edit. Preserve this evidence distinction.

## Original objective and model gap

The objective is to extend the safe but limited alpha.8 immutable correction/reversal workflow so a whole logical batch remains truthful after individual corrections, partial reversals, repeated corrections, restoration, mixed dates, partial no-ops and explicit `RemainReversed` decisions.

Physical `MovementBatchId` cannot be the continuing logical identity when members acquire different descendant states. The existing physical whole-batch guard must remain until logical lineage, mutations and all operational projections replace it coherently. A partial activation or piecemeal report cutover can silently produce false operational balances and is prohibited.

## Frozen lineage and operational semantics

The detailed authority is in `docs/BusinessRules.md`, `docs/FunctionalSpecification.md`, `docs/Architecture.md`, `docs/Database.md` and the permanent requirements. The following summary is operative and must not regress.

### Identity, evidence and complete current state

- `BinMovement` remains immutable ledger/forensic evidence. Generic correction, reversal and restoration never edit or delete historical movements.
- A physical `MovementBatch` means rows genuinely persisted together with truthful shared header semantics. It is not continuing lineage identity.
- `LogicalMovementBatch` is the stable root; roots never merge or split. `RootMovementBatchId` is the sole original physical-batch authority, and a single-entry root has none.
- `LogicalMovementLine` permanently represents one original ordinary business line and remains a member while Reversed. Persisted IDs, not display/business-value reconstruction, are identity.
- Every substantive mutation advances one root-wide generation containing exactly one generation-line state for every permanent line. Sparse current generations are forbidden.
- `CurrentGenerationNumber` is the sole current-state and root-wide optimistic-concurrency authority. Do not introduce competing per-line current pointers.
- Active state emits exactly its current effective movement. Reversed state carries the last effective movement and exact terminal ordinary reversal, which net to zero at and after the reversal business date.
- Transformation role (`RootOriginal`, `CorrectionNeutraliser`, `CorrectionReplacement`, `OrdinaryReversal`, `Restoration`) is separate from `MovementSource` provenance. Do not add `MovementSource.Correction`.
- `OriginalDisplayOrdinal` is immutable presentation metadata, never identity or arithmetic authority. Missing, duplicate, ambiguous or cross-owned persisted identity fails closed.
- `LogicalMovementLedgerLink.IntroducedByGenerationLineId` may be temporarily null only while one transaction resolves insertion order. It must be complete before Active/ReadOnly state commits; correctness cannot rely on deferred FKs or triggers.
- Structured pointers/state are authority. Do not persist authoritative `BeforeValuesJson` or `ResultValuesJson`; canonical versioned request JSON and field masks record intent, audit explanation and idempotency only.

### Correct, Reverse, Restore and planning

- Correct, Reverse and Restore use one controlled movement-change operation envelope and one complete-generation semantic plan.
- Corrected operational history is current retrospectively corrected truth ordered and filtered by `MovementDate`; it is neither raw forensic history nor full bitemporal "what was known then" history.
- `GenerationNumber`, `MovementDate` and `CreatedUtc` are independent mutation, business and forensic orders.
- Restoration means an ordinary reversal was erroneous. Its baseline is the last legitimate effective state before that reversal; unselected fields inherit and explicit fields override. A legitimate later movement after a correct reversal is a new ordinary movement/logical line, not restoration.
- Whole-root work requires an explicit Restore or RemainReversed decision for every and only currently Reversed line. Restore overrides and masks are line-specific.
- `RemainReversed` retains the line with zero contribution and creates no fake movement.
- `CarriedForward` means not targeted by an individual operation. `AlreadyMatches` means considered by a whole-root operation but requiring no field change. They are not interchangeable.
- A complete semantic no-op creates no generation, operation, movement, batch or audit. Restoration is substantive even with no field override.
- Valid historical business dates at or before the separate authoritative business date may be corrected/restored. Future operational dates are prohibited. Do not use client-local `DateTime.Today` as authority.
- Correction neutralisers are Manual and use the prior effective date; ordinary reversals are Manual and use the authoritative planning date. Replacements/restorations inherit provenance and every unselected value. Explicit selected values, including clear/null, are normalized exactly as frozen.
- Applied field masks retain selected-equal fields and the exact per-action rules frozen and tested by IMP-05.
- Eligibility, planning, field precedence, no-op, authorization, CAS, idempotency, persistence, audit and projection authority belong below presentation. WinForms collects intent and displays results only.

### Physical output and import separation

- A physical correction-output batch is optional. It exists only for a physical-origin WholeRoot plan where every line becomes newly Active through Corrected/Restored output, exactly one new effective replacement/restoration exists per line, provenance is Batch and non-import, all output has one truthful date and direction, and membership is exact.
- Neutralisers and reversals never belong to a correction-output batch. Mixed dates/directions, partial change, `AlreadyMatches`, `CarriedForward`, Reversed/`RemainReversed`, Manual provenance or import involvement produce a logical generation without a fabricated physical batch.
- Historical schema-16 correction-output batches remain evidenced by existing operation IDs, `MovementBatch` rows and exact movement membership. Migration creates zero historical `LogicalMovementPhysicalOutput` rows and never claims generation 0 created them. A later conversion would require a separately authorized deterministic process.
- ImportRun/ExcelImport and Adjustment rows stay outside generic lineage. Generic Correct/Reverse/Restore is prohibited for them. Import Replace/Correct must fail closed if generic lineage/evidence references would make its controlled deletion unsafe.

### Health, audit and review

- Persisted operational statuses are `Initializing`, `Active`, `ReadOnly` and `Invalid`. A committed `Initializing` row is critical corruption. Active is completely proven and mutable subject to authorization/audit health. ReadOnly has a provable projection but unsupported mutation/history. Invalid cannot produce a proven current projection.
- Operational mathematical integrity and audit/compliance health are distinct. Operational-lineage corruption fails every affected numeric result; it must never silently omit a root or fall back to raw arithmetic.
- Isolated after-the-fact audit corruption does not suppress a mathematically proven balance, but it blocks affected mutation, Administrator Review and compliance/evidence output and raises critical health.
- Administrator Review acknowledges an already-effective atomic operation; it is not preapproval.
- Legacy audit/operation links may be created only from unique structured persisted-ID proof. Never infer them from prose, timestamps or matching customer/container/date/quantity values.
- New mutation audit must be transaction-compatible and use the caller-owned DbContext/transaction. Calling the current independent-context `IAuditService.WriteAsync` inside a lineage transaction is unsafe; add a focused appender/factory rather than a competing audit system.

## Persistence, migration, concurrency and recovery safety

- Permanent `MovementCorrectionKind` values are Single=0, WholeBatch=1, Reverse=2 and Restore=3. Never renumber persisted enums without an explicit data migration. In a schema-16 source, any value other than 0/1 is a database-wide migration blocker and is never normalized or reinterpreted.
- Schema 17 has dormant lineage tables, operation-envelope columns, a nullable unique/RESTRICT primary operation link on AuditEvent, output-only physical associations and RESTRICT/NO ACTION evidence relationships. Normal `DatabaseSetup`, `EnsureCreatedAsync`, the registered migration catalogue and runtime remain schema 16 until coherent activation.
- MigrationBaseline creates root-wide generation 0 with complete lines/states, Active/Reversed pointers, ownership/roles and non-null same-root/line introduction links. Generation 0 has no operation/predecessor; legacy request/schema/expected/result generation fields remain null; zero historical physical-output rows are fabricated.
- Existing `MovementCorrectionOperations` evolves into the lineage operation envelope. Legacy `MovementCorrectionLines` remains forensic evidence and is not populated as a competing authority for new generations.
- The existing `BinMovement -> MovementBatch` relationship changes from SET NULL to RESTRICT only inside dormant schema 17. Import deletion and developer whole-database reset require regression proof.
- Preflight is read-only, private/non-pooled and fail-closed. It checks exact schema/tables, integrity, FKs, correction/reversal chains, ordinary ownership, physical-batch relationships, import separation and graph cycles using persisted structure only.
- A verified provider-consistent exact-source recovery artifact is mandatory before schema mutation. It includes unique no-overwrite backup, manifest, hashes, source physical identity, schema, counts, integrity/FKs and structural fingerprint comparison; it is retained and never automatically deleted for v1.
- Every database-using process must participate in the alias-safe physical-database shared/exclusive coordination before activation. One exclusive upgrade lease spans preflight, backup/source comparison, migration and postflight. A pending-operation marker and competing process abort before schema writes.
- A rolled-back migration that leaves the active source valid must abort startup, preserve backup/evidence and leave the source in place. Controlled restore is permitted only when the active database is unusable or a committed failure requires it; preserve the failed DB, reverify evidence, restore while clients are stopped and revalidate before startup. Never auto-restore after every error.
- Root CAS is the concurrency authority. Provider uniqueness conflicts for competing generation N+1 inserts must translate to the same stable provider-neutral stale/concurrency-lost result as a failed CAS.
- Movement-change `ClientOperationId` is unique within its operation envelope, not globally across unrelated command domains. Concurrent identical requests return the one committed result; same ID/different fingerprint returns a stable idempotency conflict. Normalize provider exceptions rather than leaking SQLite/future PostgreSQL details.
- One transaction must contain operation reservation, every ledger movement, the generation and complete generation lines, ownership/introduction links, optional physical output batch and membership, primary audit, root CAS and current-pointer advance. No partial externally observable artifact may survive failure.
- The write transaction must revalidate root generation, trusted current facts, authorization and master-data activity; a plan is not permission to persist stale facts.

## Projection and atomic cutover requirements

- `PositionAsOf(D)` aggregates corrected authoritative activity where `MovementDate <= D`; `CurrentPosition` equals `PositionAsOf(authoritative business today)`.
- An inclusive statement `StartDate..EndDate` uses opening `PositionAsOf(StartDate - 1 day)`, activity within the inclusive range and closing `PositionAsOf(EndDate)`.
- Each numerical result validates and projects all relevant complete snapshots in one consistent read transaction/snapshot. Daily Print Pack Outstanding and Detail share one snapshot; Dashboard headline and attention/outstanding figures share one refresh snapshot.
- Coherently cut over Daily, Weekly, Monthly, Market Floor, Outstanding/AsOf, Statements, Daily Print Pack, Dashboard, customer balances, `BalanceService`, current-position and relevant entry/import/reconciliation previews plus PDF/CSV DTO outputs.
- Movement History, Audit, physical Batch Detail and Import History remain immutable/forensic views with added lineage navigation/context; they do not become corrected-only activity.
- `EffectiveMovementQuery` remains runtime authority until every operational consumer has moved together, alpha.8 equivalence and new restoration cases pass, and hidden consumers are excluded. Never switch reports piecemeal.

## Architecture boundaries and protected scope

- v1 remains .NET 8 WinForms/Win32 -> client-neutral application/services -> provider-neutral shared EF model -> local SQLite adapter.
- The permanent target remains remote clients -> authenticated service/API -> central PostgreSQL, but PostgreSQL/API implementation is post-v1 and is not a prerequisite for lineage. Do not add WinUI/MSIX, portal/mobile or speculative server migration work.
- No SQLite triggers, rowid/locking quirks, provider-specific business SQL, WinForms state or presentation validation may define business correctness.
- Keep business logic testable outside controls and keep the presentation replaceable. Do only extractions required to establish one authority; the protected whole-codebase layer-delineation audit follows lineage acceptance.
- Security hardening remains the protected pre-v1 workstream immediately after Movement Correction/Reversal and before Branding/Communications. Do not change its requirement IDs, ordering or v1 block inside lineage work.
- Formal period locking (`BT-CORR-013/014`) remains post-v1. Preserve all still-valid alpha.8 requirements and behavior until coherent replacement.

## Retained Batch #30 acceptance requirement

Do not modify or delete the intentionally retained partially reversed Batch #30 during lineage development.

Conceptual state:

- Blue: active `IN 4`;
- Yellow: original `IN 1` plus ordinary reversal `OUT 1`;
- Yellow current contribution: zero.

Approximate prior movement IDs were `#1080` Blue, `#1081` Yellow original and `#1082` Yellow reversal. These display IDs are not authority. Query the selected database read-only and prove the persisted batch/movement/reversal relationships before relying on them.

Later Windows/operator acceptance must exercise this fixture where practical for `RemainReversed`, Restore, whole-root and selected correction, mixed dates, repeated work, navigation from all evidence roles and descendants without a physical output batch, immutable Batch Detail, Movement History, Audit Detail/Administrator Review, all operational balances/reports, and Windows 11 1920x1080 at 150% plus larger displays.

## IMP-06 implemented boundary

IMP-06 adds only the smallest audit primitive that appends through a caller-owned DbContext and caller-owned transaction. It does not create an independent DbContext or transaction, call `SaveChanges` implicitly, commit, or otherwise take transaction ownership. It supports exactly one new, untracked primary `AuditEvent` for a future movement-change operation and fails closed without an active caller transaction. Existing independent `AuditService.WriteAsync` behavior remains unchanged.

Focused coverage proves the appender initially leaves the event Added and unpersisted, caller commit persists exactly one event, caller rollback removes both the audit and legitimate sibling state saved in the same transaction, missing caller transaction is rejected without tracking, and existing independent audit behavior still saves correctly. Dormant schema-17 tests separately prove unique structured legacy association plus the RESTRICT operation FK and unique one-primary-audit constraint. IMP-06 does not activate schema 17 or add a production lineage writer, generation-zero integration, CAS/idempotency execution, projection/report cutover or UI.

## Approved dependency order

The protected order is:

`alpha.8 characterization`
-> `Core lineage contracts`
-> `preflight/backup/exclusive-gate infrastructure`
-> `dormant schema-17 migration/backfill/postflight`
-> `current resolver/invariant validator`
-> `trusted planner and physical-output policy`
-> **`IMP-06 transaction-compatible audit primitive (complete, independently reviewed and source-gate verified)`**
-> **`schema-17 publication/live-validation lifecycle prerequisite (included in this checkpoint)`**
-> **`IMP-07 generation-zero Single/Batch integration (next; approved in principle, not implemented or authorized for production editing)`**
-> `unified Correct/Reverse/Restore commands`
-> `root CAS/idempotency/provider translation`
-> `corrected activity and PositionAsOf`
-> `atomic cutover of every operational numeric consumer`
-> `audit/history detail`
-> `Restore/RemainReversed WinForms UI`
-> `failure/concurrency/full automated gates`
-> `representative retained-database migration rehearsal`
-> `Batch #30 Windows acceptance`
-> `protected whole-codebase layer-delineation audit`
-> `Security Hardening`.

The stages through IMP-06 are complete and reviewed. IMP-07 is next in the protected sequence, but approval in principle is not authorization to edit production/application code. Do not infer permission for command activation, schema registration, report/UI cutover or any later slice merely because they follow in this chain.

An activated migration with incomplete entry/mutation/projection integration is not distributable. Do not ship schema-only or engine-only internal checkpoints, remove the alpha.8 guard, allow new Manual/Batch entries without lineage after activation, or leave old writers active after migration.

## Validation and acceptance state

- Independent external source/diff review approved the dormant IMP-05/05B/05C boundary at implementation checkpoint `c0dfc7e...`.
- Canonical `Build-BinTracker.bat` at that implementation checkpoint passed source/package-state audit, restore and build with no reported compiler warnings/errors; 438/438 automated tests passed, 0 failed and 0 skipped.
- IMP-05D documentation reconciliation passed `Audit-BinTracker.ps1` at v0.5.0-alpha.8.7 with 259 permanent requirement IDs and 27 Markdown files inventoried, plus `git diff --check`. The application BAT was not rerun for that documentation-only reconciliation.
- IMP-06 satisfied BT-REL-011 before production editing: 43/43 accepted movement-correction workflow and SQLite audit/reversal characterization cases passed. The corrected appender proof passed 4/4, including atomic rollback with sibling state; the combined audit/correction/reversal/concurrency regression filter passed 53/53 before this tests-only proof correction, with 0 failed/skipped. Exact dormant BT-AUD-015 structural mapping/FK/uniqueness tests passed 3/3.
- Release `dotnet build BinTracker.sln --no-restore` passed with 0 warnings and 0 errors. `Audit-BinTracker.ps1` passed with 259 permanent requirement IDs and 27 Markdown files inventoried; `git diff --check` passed with only line-ending notices.
- After independent IMP-06 approval, canonical `Build-BinTracker.bat` passed at v0.5.0-alpha.8.7: source/package-state audit and restore passed; Debug build passed with 0 warnings and 0 errors; 266/266 UnitTests and 177/177 IntegrationTests passed, for 443/443 automated tests with 0 failed and 0 skipped.
- The current pre-IMP-07 lifecycle prerequisite was characterized red before production editing: a valid test-fixture-only native Initial root resolved successfully but the old AlreadyComplete path failed it with `LINEAGE_POSTFLIGHT_INVARIANT_FAILURE`. After the validation split, focused lifecycle cases passed 4/4, the complete migration class passed 55/55, the combined adjacent integration filter passed 100/100 and the provider-neutral validator/contract unit filter passed 25/25, all with 0 failed/skipped. This is targeted evidence, not a canonical BAT.
- `Audit-BinTracker.ps1` passed for this prerequisite at v0.5.0-alpha.8.7 with 259 permanent requirement IDs, 27 Markdown files inventoried and configured contradiction guards passed.
- No current compiler warning, automated-test failure or source-gate failure is known. This is checkpoint evidence, not a promise about later changes.
- No retained-production-database migration rehearsal, schema-17 production activation, package build, PostgreSQL equivalence proof, complete Windows UI/DPI interaction pass or operator acceptance has occurred for lineage.
- Static implementation, focused automated tests, full automated suite, source/build gate, external code review, migration rehearsal, packaging and Windows/operator acceptance are distinct evidence levels. Never convert one into another or mark `IMPLEMENTED-ACCEPTED` without explicit human evidence.
- The last accepted alpha.8 Windows interaction evidence covered Movement History selection/action synchronization, whole-batch auto-tick/clear, manual no-op rejection and focus-stable confirmation behavior. It does not imply broad lineage or full-candidate visual acceptance.
- Version remains `0.5.0-alpha.8.7`; no current package represents lineage implementation.

For future mutation work, failure injection must cover reservation, generation insertion, every neutraliser/replacement/restoration/reversal, generation-line and ledger/introduction links, optional physical output, audit, before CAS and after CAS before commit. A fresh context must prove rollback and an exact-once retry. Concurrency/idempotency coverage must include Correct/Correct, Reverse/Correct, Restore/Correct, whole/whole, different lines in one root, stale preview, identical and conflicting operation IDs, lost response after a newer generation, report-during-mutation and import-replacement collision.

## IMP-07 characterization-before-change constraint

Before the first IMP-07 production edit, identify and run precise characterization of the currently observable accepted Single/Batch entry behavior. Missing characterization may be added as tests first, but production code must not be added or modified merely to create a characterization seam. If current code cannot expose a requested failure mode without production changes, characterize the currently observable accepted behavior truthfully and add the new failure-injection proof only during or after the approved implementation. Do not repeat the original IMP-05 pre-edit ordering defect.

## Authoritative documents and key implementation seams

Before starting IMP-07, read the current applicable sections of:

- `AGENTS.md` and `docs/DevelopmentWorkflow.md` for hard gates;
- `docs/Roadmap.md`, `docs/RoadmapCoverageMatrix.md` and `docs/RequirementsAcceptanceRegister.md` for sequence, scope and permanent IDs;
- `docs/Architecture.md`, `docs/BusinessRules.md`, `docs/FunctionalSpecification.md` and `docs/Database.md` for semantics and boundaries;
- `docs/Testing.md`, `TEST-CHECKLIST.md` and `docs/AuditCoverage.md` for evidence, transaction audit and acceptance;
- `docs/ImportWizard.md` and `docs/ReimportSafety.md` for separate ImportRun ownership;
- `KNOWN-ISSUES.md`, `TECH-DEBT.md`, `docs/SecurityHardeningRegister.md`, `docs/DocumentationAudit.md` and release/version documents for protected limitations and gates.

Useful seams to inspect rather than trusting this summary alone:

- `src/BinTracker.Core/Domain.cs` and the focused lineage contracts;
- `src/BinTracker.Data/BinTrackerDbContext.cs`, `DatabaseSetup.cs`, `SqliteSchemaMigrations.cs`, dormant lineage migration/materialization and recovery infrastructure;
- `src/BinTracker.Data/TransactionAuditAppender.cs` and `tests/BinTracker.IntegrationTests/TransactionAuditAppenderTests.cs`;
- `src/BinTracker.Services/MovementCorrectionService.cs`, `EffectiveMovementQuery.cs`, audit and balance services, the trusted planner and resolver;
- correction, migration, audit, balance, report, import and lineage tests;
- WinForms correction/reversal/history surfaces only when their authorized slice arrives.

`AuditService.WriteAsync` still opens an independent DbContext and saves its own event. The new appender is a separate transaction participation primitive, not a replacement audit service, and is unregistered. `MovementCorrectionService` and `EffectiveMovementQuery` remain alpha.8 authorities. `DatabaseSetup` calls `EnsureCreatedAsync` before registered numbered migrations. These are safety-critical seams; IMP-07 production edits still require explicit authorization.

## Rejected approaches and traps

- Do not use physical batch identity as logical identity, create a physical output for every generation, use sparse generations or remove the alpha.8 guard early.
- Do not fabricate historical generations/chronology or match identity from display order, values, text or timestamps.
- Do not make JSON duplicated state authority, add `MovementSource.Correction`, use UI/SQLite triggers as integrity authority or implement per-line merge concurrency for v1.
- Do not make audit corruption suppress proven numbers or operational corruption return plausible numbers.
- Do not assume Batch #30 IDs, external exports, a selected database shape, full-history validity from a current-root resolver result, or that a plan can be persisted without transaction-time revalidation.
- Do not run the application against retained user data merely to inspect it; startup can write/migrate. Never migrate retained data without the verified preflight/backup/exclusive gate and separately required rehearsal authority.
- Do not auto-restore a valid source, use a global cross-domain operation-ID namespace, correct imported rows generically, switch reports piecemeal, expose Restore UI early or implement PostgreSQL/API/WinUI as lineage work.
- Do not stage, commit, push, merge, rebase, reset or discard evidence/user changes without explicit instruction.

## Next-session hard gate and exact next action

Before modifying application code, the next session must:

1. Read repository-root `AGENTS.md`, the full **Conversation Context Capacity / Continuity Hard Gate** and this continuation completely.
2. Read the current authoritative documents applicable to IMP-07, including the protected Roadmap/coverage and permanent requirements.
3. Mechanically verify repository root, branch, actual HEAD/upstream and divergence, staged/tracked/untracked worktree state and `Directory.Build.props` version. Reconcile those facts against this continuation and stop on any unexpected difference.
4. Confirm from source/migration registration that schema 17 is still dormant, schema 16/alpha.8 is normal authority and no writer/CAS/idempotency/projection/UI cutover has appeared.
5. Mechanically verify that the checkpoint commit containing this prerequisite descends from the verified entering baseline `0e7752f45d74def016a945be5f4c9335aa39deeb` (`Reconcile IMP-06 checkpoint for IMP-07`); do not require the actual future HEAD to equal that baseline or infer it from this document. Then inspect the completed IMP-06 appender/tests/evidence, the existing Single/Batch entry paths and relevant schema-17 seams. Preserve unrelated evidence artifacts and stop if ancestry or repository reality is unexpected.
6. Before the first production edit, satisfy the IMP-07 characterization constraint above without changing production code merely to manufacture a seam.
7. IMP-07 is the next dependency-ordered slice, but do not begin its production/application-code edits or activate runtime/schema/report/UI authority without explicit authorization.
8. Keep evidence classifications truthful and run `git diff --check` before handoff. Do not stage, commit or push without explicit instruction.

Useful mechanical baseline commands:

```powershell
git rev-parse --show-toplevel
git branch --show-current
git rev-parse HEAD
git rev-parse '@{upstream}'
git status --porcelain=v2 --branch
git diff --cached --name-status
git diff --name-status
git ls-files --others --exclude-standard
rg -n "<(Version|AssemblyVersion|FileVersion|InformationalVersion)>" Directory.Build.props
git diff --check
```

## Continuity completeness self-check

**Question:** Could a new session that cannot see this conversation safely continue this exact work using only the repository and this handoff?

**Answer:** YES. It records the exact current IMP-06 checkpoint and evidence, identifies IMP-07 as next without claiming implementation or production-edit authorization, preserves the characterization-before-change constraint, and retains frozen semantics, architecture and safety boundaries, evidence limitations, Batch #30, dependency ordering and the startup hard gate.
