# Active BinTracker Continuation Checkpoint

## Task 19 completion checkpoint — 11 September 2026

Task 19 implementation and independent review are complete at pushed commit `930f2c77b1df7cd4629be2faff662750431854fe` (`Add shared-snapshot Daily Print Pack prerequisite`). HEAD and upstream are synchronized at 0/0. Jack's canonical `Build-BinTracker.bat` passed at v0.5.0-alpha.8.7: mechanical audit, restore and build passed; 259 permanent requirement IDs across 27 Markdown files; 632/632 automated tests passed with 0 failed and 0 skipped. Task 19 remains dormant preparation only: schema 17 is unregistered/inactive and runtime activation has not begun. Windows/operator real preview/print, retained-database migration/recovery rehearsal, packaging/deployment and alternative-provider acceptance remain unproven. Actual HEAD/upstream must always be mechanically established; this continuation does not claim the SHA of a future documentation-reconciliation commit. The detailed task-19 paragraphs below are historical evidence and do not direct current sequencing.

**Status:** ACTIVE

**Purpose:** Preserve the completed, pushed Task 19 Daily Print Pack shared-snapshot checkpoint, its canonical evidence and the later separately authorized activation sequence.

**Reconciled:** 11 September 2026 (Australia/Sydney)

**Task-19 completion update:** Task 19 implementation and independent review are complete at pushed commit `930f2c77b1df7cd4629be2faff662750431854fe` (`Add shared-snapshot Daily Print Pack prerequisite`). HEAD and upstream are synchronized at 0/0. Jack's canonical `Build-BinTracker.bat` passed at v0.5.0-alpha.8.7: mechanical audit, restore and build passed; 632/632 automated tests passed with 0 failed and 0 skipped. This records dormant preparation only: schema 17 remains unregistered/inactive and runtime activation has not begun. Windows/operator real preview/print, retained-database migration/recovery rehearsal, packaging/deployment and alternative-provider acceptance remain unproven. Actual HEAD/upstream must always be mechanically established; this continuation must not claim the SHA of a future documentation-only reconciliation commit.

## Task 19 independent-review correction — 11 September 2026

Review found a blocking architecture defect: the two public report interfaces exposed `QueryInTransactionAsync` with `BinTrackerDbContext`. The shared-snapshot behavior is retained, but the public contracts must return to ordinary client-neutral `QueryAsync` only. The authorized correction moves participation to two internal Services interfaces, explicitly implemented by the existing report classes, with test-only friend access for wrappers. Data snapshot/projection mechanics, normal DI/startup/schema and report business/PDF behavior remain unchanged. PostgreSQL is a likely future provider, not an exclusive architectural assumption; see the strengthened Architecture hard rule. Task 20/activation/BAT/staging/commits remain prohibited.

Correction starting state was mechanically verified: same HEAD/upstream `8b389588aec7e7b2c4ff702fac901bb8dca3408c`, branch `codex/movement-correction`, 0 ahead/behind, version `0.5.0-alpha.8.7`; exactly 16 modified tracked files (including `KNOWN-ISSUES.md`), no staged files and the same 15 unrelated untracked evidence/helper files. Before correction production edits, the affected shared-snapshot/pack/Daily/Outstanding characterization passed 34/34. Two new public-contract reflection regressions failed as expected, showing the extra context-bearing public methods. The internal-boundary correction is implemented; the focused set passes 38/38 including the four compiled contract/visibility cases. Complete projection/consumer plus alpha.8 correction and Import execution/history regressions passed 109/109. All had 0 failed/skipped. Release solution build passed with 0 warnings/errors; the unchanged canonical audit invocation passed with 259 permanent IDs and 27 Markdown files, and `git diff --check` passed. No compiler/test/audit failure remains. Earlier task-19 evidence below does not approve the rejected public interface; a new independent review of this correction is required before Jack's BAT. No full suite, canonical BAT, activation or operator/real-print acceptance has occurred in this correction.

The correction changes the three report Services files and two existing test files; `BinTracker.Services.csproj` grants internal access only to the integration-test assembly. `IOutstandingReportSnapshotParticipant` and `IDailyMovementsReportSnapshotParticipant` live beside their existing Services implementations, remain internal and are implemented explicitly. The public report interfaces each expose only `QueryAsync(query, CancellationToken)`. Pack composition checks both participants before opening its read boundary; it cannot silently fall back to independent queries. Test wrappers exercise that internal boundary; ordinary public fakes need no persistence API. Data's snapshot/projection implementation and all deterministic interleaving, metadata, corruption, cancellation and transaction-ownership assertions are preserved. Exact verification commands: `dotnet build BinTracker.sln -c Release --no-restore -m:1 /nr:false`, the unchanged `powershell -NoProfile -ExecutionPolicy Bypass -File .\Audit-BinTracker.ps1`, and `git diff --check`; test filters cover DailyPrintPackServiceTests, all Daily/Outstanding and shared-snapshot cases (38), then OperationalMovementProjectionSchema17Tests, MovementCorrectionWorkflowTests, ImportExecutionSqliteTests and ImportRunHistorySqliteTests (109). These sets overlap and are not a full-suite count.

Architecture, AGENTS and Business Rules explicitly preserve Services' internal use of the shared provider-neutral EF model, EF/LINQ and `IDbContextFactory<BinTrackerDbContext>` under BT-ARCH-004 while prohibiting persistence/provider exposure on application-facing/public service contracts. Data owns provider configuration, schema/migrations, backup/recovery, provider-specific SQL and database-specific transaction/locking/storage mechanics; SQLite PRAGMAs, locking assumptions, raw SQL and provider quirks stay there and never define business semantics. Task-19 internal Services snapshot participants may retain `BinTrackerDbContext` as non-client-facing internal composition; this wording clarification does not reopen the implementation without a concrete defect. The requirements register records the user-authorized BT-ARCH-004/008 wording clarification without changing IDs/scopes/statuses or implementing any provider/client. Database, Testing, DocumentationAudit and this handoff explain the corrected internal boundary. Full task inventory is now 19 modified tracked files, no staged files, no new untracked files; this includes all original task-19 files plus AGENTS, BusinessRules and the Services project file. The unrelated 15 untracked files remain untouched. Task 20 is prohibited.

## Completed task 19 — shared Print Pack snapshot prerequisite

BT-CODEX-20260910-19 explicitly authorizes resolving the Daily Print Pack shared cross-section snapshot prerequisite before atomic runtime activation. Starting mechanical state: HEAD `8b389588aec7e7b2c4ff702fac901bb8dca3408c` (`Reconcile import execution checkpoint evidence`), branch `codex/movement-correction`, upstream `origin/codex/movement-correction`, 0 ahead/0 behind, no tracked/staged changes; the four unrelated artifact groups listed below remain untouched. Version is `0.5.0-alpha.8.7`, assembly/file version `0.5.0.0`. Starting source independently confirmed separate delegated projection transactions; task 19 fixes that dormant path. Task 18 independent review/canonical BAT are already complete as recorded below; stale references to awaiting those gates were reconciled before production edits. This task does not authorize activation, the canonical BAT, packaging, staging or commits.

Implemented design for the existing BT-CORR-029 snapshot invariant: reuse the same transaction-participating projection authority and both existing report implementations. `ReadSnapshotAsync` owns a deferred Serializable SQLite transaction on the caller's context, rejects existing transactions, enlists EF metadata reads and disposes its own transaction without save/commit. The two existing projection scopes run sequentially through `QueryInTransactionAsync`; no extra projection query, per-row query, local lineage reconstruction or duplicated filter/order/total logic is added. Customer activity affects inclusion and shared customer/container names/types/order must remain consistent, so their lookups share the transaction. Business-header lookup, unchanged PDF rendering and report audit remain outside the boundary. Missing participation support or projection failure fails the pack without independent/raw fallback. Core, startup, schema catalogue, migration/backfill, normal DI and version are unchanged.

The following starting-state and evidence paragraphs are historical/non-operative records from the Task-19 work session. They do not override the verified completion checkpoint or current sequencing above.

Task 19 evidence and current stopping point:

- Before production edits, the accepted pack/Outstanding/Daily/projection/alpha.8 correction filter passed 100/100. Strengthened pack-result/PDF/audit characterization passed 6/6. The separate deterministic regression then failed against unchanged production code with Outstanding 7 versus Daily 11 after a committed correction between sections; this is resolved, not a remaining failure.
- Post-edit initial focused tests passed 8/8 and the expanded snapshot/pack boundary set passed 16/16. Combined characterization plus complete projection/consumer and Import execution tests passed 121/121; adjacent lineage/planner/report unit tests passed 65/65. The completed snapshot/pack set plus Import History passed 25/25. Final review retained standalone Daily date-clamp evaluation before asynchronous context creation; the affected Daily/pack/snapshot filter then passed 29/29. All had 0 failed/skipped. These counts overlap and must not be summed as a full-suite result.
- Final Release `dotnet build BinTracker.sln -c Release --no-restore -m:1 /nr:false` passed with 0 warnings/errors. `powershell -NoProfile -ExecutionPolicy Bypass -File .\Audit-BinTracker.ps1` (the BAT's existing audit invocation) passed with 259 permanent requirement IDs and 27 Markdown files; `git diff --check` passed. Direct script invocation was initially blocked by local execution policy; the unchanged canonical invocation resolved it. Initial SDK first-use filesystem permission failure was resolved through tool escalation before characterization; no unresolved compiler, test or audit failure remains. No full suite or canonical BAT has run for this task.
- Deterministic writer callbacks commit correction, reversal, restoration and metadata updates on separate connections before the second section reads, while the pack read transaction remains open. Captured actual section DTOs stay before-state; a fresh pack sees after-state. Failure/cancellation tests return no PDF and persist no success audit. Transaction tests cover existing-owner rejection, participation without ownership, disposal on success/failure/cancellation and both initially closed and caller-opened connections.
- The application edits are `src/BinTracker.Data/SqliteOperationalMovementProjectionAuthority.cs` and Services `OutstandingReportService.cs`, `DailyMovementsReportService.cs`, `DailyPrintPackService.cs`; the review correction also adds test-only friend access in `BinTracker.Services.csproj`. Tests changed are IntegrationTests `OperationalMovementProjectionSchema17Tests.cs` and `DailyPrintPackServiceTests.cs`. Current-state documentation changes are AGENTS, Known Issues, Test Checklist, Architecture, Business Rules, Database, Testing, Roadmap/coverage, RequirementsAcceptanceRegister, DocumentationAudit and this continuation. Tech Debt and release/version documents need no task-specific change.
- Historical pre-commit state: task edits were uncommitted/unstaged at that earlier stopping point. The implementation is now committed and pushed at `930f2c77b1df7cd4629be2faff662750431854fe`; canonical BAT evidence is complete. Unrelated artifacts remain untouched. Windows/operator, retained-database, packaging/deployment and alternative-provider evidence remain unproven.
- Historical next-action statement superseded: independent review and Jack's canonical BAT are complete. Runtime activation remains separately authorized work and has not begun.

This record is governed by **Conversation Context Capacity / Continuity Hard Gate** in `docs/DevelopmentWorkflow.md`. It supplements the authoritative requirements and architecture; it does not replace them. Repository reality always wins, and any conflict must be investigated before application-code changes.

## Verified current checkpoint and operative sequencing

Current checkpoint: commit `930f2c77b1df7cd4629be2faff662750431854fe` (`Add shared-snapshot Daily Print Pack prerequisite`), pushed and synchronized with `origin/codex/movement-correction` at divergence 0/0. The canonical BAT evidence is complete as recorded above. Any older hashes, pending-review statements or uncommitted-state descriptions below are historical records only; actual HEAD/upstream must always be established mechanically.

- Repository root: `C:/Users/jackm/Desktop/build/BinTracker-Codex-Clone`.
- Branch: `codex/movement-correction`.
- **Historical task 19 starting state:** `8b389588aec7e7b2c4ff702fac901bb8dca3408c`; this is retained only as historical evidence. The verified completed checkpoint is `930f2c77b1df7cd4629be2faff662750431854fe`, and actual HEAD/upstream must always be established mechanically.
- **Historical continuation baseline:** `904b04cdbb52c2d0d4fca372227e50008e0e0751` (`Reconcile movement lineage continuation checkpoint`) remains ancestry evidence, not the current checkpoint or task 19 parent.
- **Earlier reviewed Daily Print Pack implementation checkpoint:** `fbcf20f3c94011909637c103b8de596f83f7ee8c` (`Prove Daily Print Pack projection composition`), retained as historical evidence beneath later Import reconciliation checkpoints.
- **Parent implementation checkpoint:** `0c2aeac2f31626544bf6862f8d7ffb14d7add21a` (`Prepare dormant Market Floor projection read`).
- **Reviewed projection-authority checkpoint:** `24f6fe381a0c94ba57425128cf8cb45b093c2250` (`Add dormant operational movement projection authority`). This is an older completed foundation checkpoint, not the current repository HEAD.
- **Verified baseline entering the unified-mutation checkpoint commit:** `1929bfac52f937c40be759dee546fa50da78dd0c`. This remains historical ancestry evidence, not an operative checkpoint.
- **Reviewed IMP-05 implementation checkpoint:** `c0dfc7e51cae1296fd5a5da31876e54364901405` (`Add trusted movement mutation planner`). Do not describe this older implementation commit as the current repository HEAD.
- Version remains `0.5.0-alpha.8.7`; assembly/file version remains `0.5.0.0`.
- **Reviewed Import replacement-comparison implementation checkpoint:** `5cec97ee1ae2bb927d0142b0645d013d29bc0b87` (`Project import replacement comparison through lineage`). This is the reviewed implementation checkpoint beneath any later documentation-only continuation correction; mechanically verify actual HEAD/upstream and divergence at session start. Task `BT-CODEX-20260909-17` is complete at source, focused-test, independent-review and canonical build/test evidence levels. Canonical `Build-BinTracker.bat` passed 608/608 automated tests with 0 failed, 0 skipped and 0 warnings/errors. Schema 17 remains dormant/unregistered; Import execution operational reconciliation was the next production slice at that completed checkpoint.
- **Reviewed Import execution operational-reconciliation implementation checkpoint:** `7eb3c58ffa9ec5e233eb0c403021744c50449c86` (`Reconcile import execution through lineage`). Task `BT-CODEX-20260910-18` is complete at source, focused-test, independent-review and canonical build/test evidence levels. Canonical `Build-BinTracker.bat` passed 613/613 automated tests with 0 failed, 0 skipped and 0 warnings/errors. Schema 17 remains dormant/unregistered. Actual HEAD/upstream must always be established mechanically at session start rather than inferred from this continuation. Task 19 now implements the shared Print Pack snapshot prerequisite before any separately authorized atomic schema-17/runtime cutover.
- The unrelated `.codex-evidence/`, `BIN-LIN-IMP-03A-HANDOFF.tmp`, `BinTracker-MarketFloor-Checkpoint-R4.ps1` and `github claude.ps1` paths are not part of the current checkpoint.
- BIN-LIN-IMP-01 through IMP-07 and the unified schema-17 mutation implementation are complete at their recorded source/automated evidence levels.
- Dormant read-side projection-consumer preparation is complete through Daily Print Pack at its recorded source/automated evidence level. This is not runtime activation or operator acceptance.
- **BIN-LIN-IMP-06 is committed, independently reviewed and source-gate verified.** It adds only the caller-owned transaction audit primitive described below and makes no runtime registration or authority change.
- **BIN-LIN-IMP-07 generation-zero Single/Batch integration is implemented and independently approved at source plus targeted/adjacent automated-evidence level.** It is dormant behind explicit isolated schema-17 composition and does not authorize runtime activation or later lineage work.
- `MovementService.SaveSingleAsync` transaction participation is part of completed IMP-07: physical movement, enabled initial lineage and audit share its caller-owned transaction, and focused rollback/retry tests cover the boundary. Earlier sequencing text naming it as unfinished was stale and has been removed.
- Import comparison and execution now optionally consume the explicit dormant corrected projection authority. Replacement uses strict pre-cutover corrected truth and excludes the selected previous ImportRun once; ordinary new import preserves its whole-ledger scope through `DateOnly.MaxValue`. Execution projection participates in its caller-owned serializable transaction. Normal schema-16 behavior remains unchanged.
- The pre-IMP-07 schema-17 validation-lifecycle prerequisite separates strict migration-publication proof from live AlreadyComplete structural/current-health validation. It does not implement generation-zero entry writing or activate schema 17.

Normal startup and runtime authority remain schema 16 and the accepted alpha.8 correction/reversal model. Schema 17 remains dormant, explicit and unregistered. Normal composition injects no-op initial-lineage and mutation writers that perform no schema probe/query/write and does not register the projection authority; explicit isolated schema-17 composition supplies the SQLite implementations and exercises the optional prepared read-consumer paths. There is no SQLite schema-17 writer, projection authority or lineage-entity registration in normal runtime, startup migration activation, activated corrected-activity/report cutover, Restore UI or other WinForms cutover. No runtime activation is authorized.

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
9. IMP-06 adds `TransactionAuditAppender` at the Data persistence boundary. It accepts one new `AuditEvent` and a caller-owned `BinTrackerDbContext` with an active caller-owned transaction, tracks exactly that event, and performs no context/transaction creation, save or commit. It does not alter the existing independent `AuditService.WriteAsync` path. Normal DI now registers the primitive for `MovementCorrectionService`, but normal composition also registers `DormantMovementMutationWriter`, so the SQLite operation/audit path remains inactive. Independent review approved the bounded implementation, and the subsequent canonical BAT passed 443/443 automated tests with 0 failed, 0 skipped, 0 warnings and 0 errors.
10. The bounded pre-IMP-07 prerequisite preserves strict MigrationBaseline-only publication postflight for the actual 16 -> 17 transaction, while schema-17 AlreadyComplete uses separate structural/current-health validation and the existing provider-neutral current-root validator. A valid mixed Baseline+Initial database is accepted; malformed native current lineage remains rejected.
11. IMP-07 keeps `MovementService` as the sole Single/Batch authority and adds a default dormant no-op writer plus an explicit isolated SQLite schema-17 writer. A successful new entry creates one atomic native `Initial` root: one generation-zero line per original physical movement, Single ordinal 0 or first-successful Batch request order, RootOriginal ownership and completed introduction links before activation. Construction and schema health fail closed; exact/reordered retries never rewrite committed lineage; migrated roots stay `MigrationBaseline`; failures roll back physical movement/batch, lineage and existing audit. Generation zero creates no correction operation, no physical-output link and no new audit action. Core validation and Services workflow remain provider/client-neutral; SQLite mechanics remain in Data.
12. The unified mutation slice composes the trusted planner with atomic Correct/Reverse/Restore persistence, complete generations, root CAS, canonical replay/idempotency, optional physical output and one primary operation-linked audit. Native operation/audit health is target-root scoped while global structural/current lineage health remains intact. Every persisted generation-line identity comes from `LogicalMovementGenerationLines.Id`; none is synthesized from a logical-line, movement, ordinal or other identity. SQLite busy/locked exhaustion follows PersistenceFailure after bounded retries and rollback/fresh-state classification rather than stale or integrity diagnosis. Primary audit BeforeValues contains trusted current pointers and business state; AfterValues contains the result generation and complete per-line action/state/mask/pointer/business facts with relevant new/output movement identities.
13. The reviewed and committed corrected-projection slice adds an immutable provider-neutral scope/result and Core relevance/contribution/signed-aggregation authority plus an explicit unregistered SQLite materializer. One serializable read transaction captures conservative root influence, current proof, all ownership facts, excluded-domain evidence and ImportRun identity; Active emits current effective once, Reversed emits last effective plus exact terminal reversal, Adjustment/ExcelImport union once, and relevant invalid/incomplete/unknown/unrooted evidence fails closed without raw fallback. ReadOnly projects while existing mutation guards remain. No existing operational consumer is switched.
14. The reviewed dormant read-side consumer sequence then prepared the existing numeric/read consumers without registering the authority or changing normal schema-16 behavior: `432b79e` characterization, `8db2159` slice-one position reads, `41809d6` customer reads, `3cfe468` Dashboard, `3507deb` excluded-import provenance hardening, `bf4425b` Outstanding, `c88c41c` Daily, `8985db0` Weekly, `1f6afd9` Monthly, `0c2aeac` Market Floor and `fbcf20f` Daily Print Pack composition proof. These are reviewed source/automated preparation checkpoints, not an activated piecemeal runtime cutover.
15. Import replacement comparison now uses one explicit dormant `PositionAsOf(CutoverDate - 1 day)` projection when representable, preserves the previous ImportRun's physical count/net evidence and removes any prior-run pre-cutover effect from the proposed baseline. Projection corruption fails closed without raw fallback. `DateOnly.MinValue` has an empty pre-cutover domain. With no projection authority, schema-16 comparison is unchanged; Import execution reconciliation is not part of this slice.
16. Import execution operational reconciliation uses the same projection authority through a caller-transaction participation contract. Replacement uses one `PositionAsOf(CutoverDate - 1 day)` result and removes the selected previous ImportRun effect exactly once; ordinary new import uses `PositionAsOf(DateOnly.MaxValue)` to retain its accepted all-representable-dates baseline. The caller-owned serializable transaction spans eligibility, projection, previous-run deletion, new ImportRun/movements/audit and commit. Projection corruption/cancellation leaves no import write or raw fallback. Normal no-authority schema-16 execution remains unchanged.

At the historical fbcf20f checkpoint, Daily Print Pack required no production change. `DailyPrintPackService` deliberately remains composed through the existing projection-capable `IOutstandingReportService` and `IDailyMovementsReportService`; adding direct projection calculations there was rejected as redundant architecture drift. Task 19 later used that dependency solely for transaction coordination, retaining both reports as the only section-policy owners. Explicit isolated schema-17 composition proves transitive `PositionAsOf` plus exact-date `Activity`, with correction, reversal, consumed-original and superseded-generation semantics still owned by those underlying consumers. Relevant projection failure propagates without raw/schema-16 fallback. That earlier checkpoint's independent-review/BAT state was superseded by the completed Task-19 checkpoint `930f2c77b1df7cd4629be2faff662750431854fe`; its 605/605 result and pending review/BAT wording remain historical evidence only. The current Task-19 implementation shares a provider-owned read transaction across both delegated sections and customer/container metadata, with deterministic committed-writer tests; independent review and the canonical BAT are complete at 632/632.

The committed IMP-06 checkpoint contains exactly these implementation-slice files relative to `e08acc9f...`:

- source: `src/BinTracker.Data/TransactionAuditAppender.cs`;
- tests: `tests/BinTracker.IntegrationTests/TransactionAuditAppenderTests.cs` and the focused BT-AUD-015 constraint proof in `tests/BinTracker.IntegrationTests/LineageSchema17MigrationTests.cs`;
- current-state reconciliation: `KNOWN-ISSUES.md`, `TEST-CHECKLIST.md`, `docs/Architecture.md`, `docs/AuditCoverage.md`, `docs/CONTINUATION.md`, `docs/DocumentationAudit.md`, `docs/Roadmap.md`, `docs/RoadmapCoverageMatrix.md` and `docs/Testing.md`.

The reviewed IMP-07 implementation content is exactly:

- modified: `src/BinTracker.Core/MovementLineagePersistence.cs`, `src/BinTracker.Data/LineageSchema17Migration.cs`, `src/BinTracker.Services/MovementServices.cs`, `src/BinTracker.Services/Services.cs` and `tests/BinTracker.UnitTests/MovementLineageContractTests.cs`;
- new: `src/BinTracker.Data/SqliteInitialMovementLineageWriter.cs` and `tests/BinTracker.IntegrationTests/MovementEntryLineageSchema17Tests.cs`.

The lifecycle prerequisite, approved characterization and these seven IMP-07 paths form the implementation checkpoint. `.codex-evidence/` and `BIN-LIN-IMP-03A-HANDOFF.tmp` remain unrelated evidence outside it.

The unified mutation implementation content is exactly:

- modified: `src/BinTracker.Core/MovementLineageContracts.cs`, `src/BinTracker.Core/MovementLineageCurrentRoot.cs`, `src/BinTracker.Core/MovementLineagePersistence.cs`, `src/BinTracker.Data/SqliteMovementPlanningSnapshotMaterializer.cs`, `src/BinTracker.Services/MovementCorrectionService.cs`, `src/BinTracker.Services/Services.cs`, `tests/BinTracker.UnitTests/AuditReviewPolicyTests.cs`, `tests/BinTracker.UnitTests/LogicalMovementCurrentRootValidatorTests.cs`, `tests/BinTracker.UnitTests/MovementLineageContractTests.cs` and `tests/BinTracker.UnitTests/MovementMutationPlannerTests.cs`;
- new: `src/BinTracker.Data/SqliteMovementMutationWriter.cs` and `tests/BinTracker.IntegrationTests/MovementMutationExecutionSchema17Tests.cs`.

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
- Each numerical result validates and projects all relevant complete snapshots in one consistent read transaction/snapshot. The frozen activated-state requirement is that Daily Print Pack Outstanding and Detail share one snapshot and Dashboard headline and attention/outstanding figures share one refresh snapshot. Task 19's dormant Print Pack composition now shares one transaction across both delegated reads and customer/container metadata, with deterministic interleaving proof; independent review and Jack's canonical BAT are complete before the separately authorized atomic runtime cutover.
- Coherently cut over Daily, Weekly, Monthly, Market Floor, Outstanding/AsOf, Statements, Daily Print Pack, Dashboard, customer balances, `BalanceService`, current-position and relevant entry/import/reconciliation previews plus PDF/CSV DTO outputs.
- Movement History, Audit, physical Batch Detail and Import History remain immutable/forensic views with added lineage navigation/context; they do not become corrected-only activity.
- `EffectiveMovementQuery` remains runtime authority until every operational consumer has moved together, alpha.8 equivalence and new restoration cases pass, and hidden consumers are excluded. Never switch reports piecemeal.

## Architecture boundaries and protected scope

- v1 remains .NET 8 WinForms/Win32 -> client-neutral application/services -> provider-neutral shared EF model -> local SQLite adapter.
- The permanent boundary is clients -> authenticated application/API contracts -> provider-specific Data -> suitable SQL database. PostgreSQL is a likely future central provider, not an exclusive assumption. Application-facing contracts must expose no EF/context/connection/transaction or provider details. Central-provider/API implementation remains post-v1, not a prerequisite for lineage. Do not add WinUI/MSIX, portal/mobile or speculative server migration work.
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
-> **`IMP-07 generation-zero Single/Batch integration (complete; independently approved source + targeted evidence; dormant)`**
-> **`unified Correct/Reverse/Restore commands (implemented; dormant)`**
-> **`root CAS/idempotency/provider translation (implemented; dormant)`**
-> **`corrected activity and PositionAsOf authority (implemented/tested/reviewed/committed; dormant)`**
-> **`dormant read-side projection-consumer preparation through Daily Print Pack (complete at source/automated level; unactivated)`**
-> **`MovementService.SaveSingleAsync transaction participation (complete within reviewed IMP-07)`**
-> **`Import replacement comparison (implemented, independently reviewed, canonically verified and committed as 5cec97e; dormant/unactivated)`**
-> **`Import execution operational reconciliation (reviewed, canonically verified and committed; dormant)`**
-> `Daily Print Pack shared cross-section snapshot prerequisite (Task 19 complete at pushed 930f2c77b1df7cd4629be2faff662750431854fe; canonical 632/632 passed, 0 failed, 0 skipped; dormant/unactivated)`
-> `atomic schema-17/runtime cutover`
-> `audit/history detail`
-> `Restore/RemainReversed WinForms UI`
-> `failure/concurrency/full automated gates`
-> `retained-database migration/backup/recovery rehearsal`
-> `Windows/operator acceptance, including Batch #30 and real report preview/print`
-> `protected whole-codebase layer-delineation audit`
-> `Security Hardening`.

The stages through unified mutation execution, root CAS/idempotency/provider translation, corrected activity/PositionAsOf authority, dormant read-side consumer preparation, SaveSingle transaction participation, Import comparison/execution reconciliation and Task 19's Daily Print Pack shared-snapshot prerequisite are implemented at their recorded dormant source/review/canonical-build levels. The projection authority remains unregistered and unactivated. The next safe action is the separately authorized atomic schema-17/runtime cutover. Audit/history integration, Restore/RemainReversed UI, broader failure/concurrency/full gates, retained-database rehearsal, Windows/operator/real-print acceptance, whole-codebase layer audit and Security Hardening remain later gated work in the established order. Do not activate schema 17, register runtime lineage/projection, rehearse a retained database or start UI/post-v1 work without the required separate authorization.

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
- Immediately before the first IMP-07 production edit, `MovementEntryCharacterizationTests` passed 17/17 with 0 failed/skipped, satisfying BT-REL-011 for this slice. This does not retroactively alter the historical IMP-05 characterization-order miss.
- After independent review corrections, `MovementEntryLineageSchema17Tests` passed 18/18, `LineageSchema17MigrationTests` passed 55/55, `MovementLineageContractTests` plus `LogicalMovementCurrentRootValidatorTests` passed 31/31, and the adjacent integration regression filter passed 59/59, all with 0 failed/skipped. Release `dotnet build BinTracker.sln --no-restore` passed with 0 warnings/errors; the mechanical audit passed at v0.5.0-alpha.8.7 with 259 permanent IDs and 27 Markdown files, and `git diff --check` passed.
- The unified mutation correction task passed its focused, standalone mutation, unit, combined schema-17, alpha.8 and adjacent regression gates. Canonical `Build-BinTracker.bat` then passed source audit and restore, built with 0 warnings/errors, and passed 279/279 UnitTests plus 259/259 IntegrationTests: 538/538 total with 0 failed/skipped.
- Before the corrected-projection production edit, the accepted correction/temporal characterization passed 42/42 and current-root/lineage contract unit tests passed 34/34. The final projection suite passed 10/10, adjacent schema-17 integration passed 126/126, provider-neutral lineage/planner unit tests passed 63/63 and unchanged alpha.8 correction/balance/report tests passed 68/68, all with 0 failed/skipped. Canonical `Build-BinTracker.bat` passed mechanical audit/restore/build with 0 warnings/errors and 279/279 UnitTests plus 274/274 IntegrationTests: 553/553 total, 0 failed/skipped. The post-documentation mechanical audit also passed with 259 permanent IDs and 27 Markdown files; after a bounded result-contract clarification the complete canonical gate was repeated and passed with the same result. External source/diff review passed, final `git diff --check` was clean, and the reviewed authority was committed and pushed as `24f6fe381a0c94ba57425128cf8cb45b093c2250`.
- Dormant read-side projection-consumer preparation progressed through the reviewed checkpoints listed above. The final Daily Print Pack slice made no production change and proved composition through the already prepared Outstanding and Daily consumers. After independent source/evidence review, canonical `Build-BinTracker.bat` passed source/package-state audit and build with 0 warnings/errors and all 605/605 automated tests with 0 failed and 0 skipped. This does not prove schema-17 runtime activation, cross-section snapshot concurrency, retained-database behavior or real preview/print/operator acceptance.
- Before the Import replacement production edit, the existing `ImportExecutionSqliteTests` passed 5/5 and the `ImportBalanceReconciliationPlannerTests` plus `ImportExecutionContractTests` filter passed 19/19. The strengthened replacement characterization then passed 5/5 before production editing. After implementation, the focused replacement filter passed 4/4; the full projection/consumer class passed 40/40; Import execution/history passed 9/9; and adjacent import planner/decision contracts passed 23/23, all with 0 failed/skipped. Release solution build passed with 0 warnings/errors, and the mechanical audit passed with 259 permanent IDs and 27 Markdown files. Independent ChatGPT source/diff review then passed. Canonical `Build-BinTracker.bat` passed source/package-state audit, built with 0 warnings/errors and passed all 608/608 automated tests with 0 failed and 0 skipped. The reviewed implementation was committed and pushed as `5cec97ee1ae2bb927d0142b0645d013d29bc0b87`. This proves corrected/raw divergence, correction/restoration, cutover boundaries, excluded domains, physical previous-run evidence, fail-closed behavior, schema-16 compatibility and minimum-date handling at source/automated evidence level; runtime activation, retained-database rehearsal, Windows/operator acceptance, real print, packaging and deployment remain unproven.
- Before task 18's first production edit, strengthened Import execution characterization passed 6/6 and the import reconciliation/execution/existing-customer decision contracts passed 21/21. Post-edit, focused projection-backed Import tests passed 6/6, combined projection/import execution passed 50/50, Import execution/history passed 10/10, adjacent contracts passed 21/21 and the complete projection/consumer class passed 44/44, all with 0 failed/skipped. Release solution build passed with 0 warnings/errors; the mechanical audit passed with 259 permanent IDs, 27 Markdown files and configured contradiction guards. This proves one transaction-participating serializable projection call, corrected/raw divergence, replacement cutoff/prior-run exclusion, new-import all-date scope, Adjustment/ExcelImport once, fail-closed corruption/cancellation, deletion/provenance/snapshots, and schema-16/no-registration behavior. Independent ChatGPT source/diff review passed. Canonical `Build-BinTracker.bat` then passed source/package-state audit and build with 0 warnings/errors and all 613/613 automated tests with 0 failed and 0 skipped. The reviewed implementation was committed and pushed as `7eb3c58ffa9ec5e233eb0c403021744c50449c86`.
- No current compiler warning, automated-test failure or source-gate failure is known. This is checkpoint evidence, not a promise about later changes.
- No retained-production-database migration rehearsal, schema-17 production activation, package build, PostgreSQL equivalence proof, complete Windows UI/DPI interaction pass or operator acceptance has occurred for lineage.
- Static implementation, focused automated tests, full automated suite, source/build gate, external code review, migration rehearsal, packaging and Windows/operator acceptance are distinct evidence levels. Never convert one into another or mark `IMPLEMENTED-ACCEPTED` without explicit human evidence.
- The last accepted alpha.8 Windows interaction evidence covered Movement History selection/action synchronization, whole-batch auto-tick/clear, manual no-op rejection and focus-stable confirmation behavior. It does not imply broad lineage or full-candidate visual acceptance.
- Version remains `0.5.0-alpha.8.7`; no current package represents lineage implementation.

The unified mutation integration covers its implemented reservation, generation/movement/link/output/audit/CAS rollback boundaries and replay/conflict paths. Later activation/cutover validation must retain those proofs and add the projection/report-during-mutation, retained-database and Windows/operator coverage appropriate to the activated system.

## IMP-07 characterization evidence

Before the first IMP-07 production edit, the precise observable Single/Batch entry characterization passed 17/17. The later schema-17 integration proof added new failure-injection coverage without changing that evidence level. Preserve this ordering record and do not claim it repairs the original IMP-05 pre-edit miss.

## Authoritative documents and key implementation seams

Before starting the next authorized lineage slice, read the current applicable sections of:

- `AGENTS.md` and `docs/DevelopmentWorkflow.md` for hard gates;
- `docs/Roadmap.md`, `docs/RoadmapCoverageMatrix.md` and `docs/RequirementsAcceptanceRegister.md` for sequence, scope and permanent IDs;
- `docs/Architecture.md`, `docs/BusinessRules.md`, `docs/FunctionalSpecification.md` and `docs/Database.md` for semantics and boundaries;
- `docs/Testing.md`, `TEST-CHECKLIST.md` and `docs/AuditCoverage.md` for evidence, transaction audit and acceptance;
- `docs/ImportWizard.md` and `docs/ReimportSafety.md` for separate ImportRun ownership;
- `KNOWN-ISSUES.md`, `TECH-DEBT.md`, `docs/SecurityHardeningRegister.md`, `docs/DocumentationAudit.md` and release/version documents for protected limitations and gates.

Useful seams to inspect rather than trusting this summary alone:

- `src/BinTracker.Core/Domain.cs` and the focused lineage contracts;
- `src/BinTracker.Data/BinTrackerDbContext.cs`, `DatabaseSetup.cs`, `SqliteSchemaMigrations.cs`, dormant lineage migration/materialization and recovery infrastructure;
- `src/BinTracker.Data/TransactionAuditAppender.cs`, `SqliteMovementMutationWriter.cs` and their focused integration tests;
- `src/BinTracker.Core/OperationalMovementProjection.cs`, `src/BinTracker.Data/SqliteOperationalMovementProjectionAuthority.cs` and `tests/BinTracker.IntegrationTests/OperationalMovementProjectionSchema17Tests.cs` for the reviewed and committed dormant projection authority;
- the prepared read-side consumer services and their focused tests, especially `OutstandingReportService`, `DailyMovementsReportService`, `MarketFloorReportService`, `DailyPrintPackService` and `DailyPrintPackServiceTests`, when assessing the completed dormant consumer sequence or planning activation;
- `src/BinTracker.Data/SqliteInitialMovementLineageWriter.cs`, the initial-lineage contracts/service seam and `tests/BinTracker.IntegrationTests/MovementEntryLineageSchema17Tests.cs`;
- `src/BinTracker.Services/MovementCorrectionService.cs`, `EffectiveMovementQuery.cs`, audit and balance services, the trusted planner and resolver;
- correction, migration, audit, balance, report, import and lineage tests;
- WinForms correction/reversal/history surfaces only when their authorized slice arrives.

`AuditService.WriteAsync` still opens an independent DbContext and saves its own event. The IMP-06 appender is a separate transaction participation primitive used by the explicit schema-17 mutation composition, not a replacement audit service. Normal composition supplies `DormantInitialMovementLineageWriter` and `DormantMovementMutationWriter`; SQLite writers appear only in isolated composition. `EffectiveMovementQuery` and the normal correction path remain alpha.8 runtime authorities. `DatabaseSetup` calls `EnsureCreatedAsync` before registered numbered migrations, whose catalogue still ends at 16, and the production DbContext has no lineage mappings. These are safety-critical seams; later production edits still require explicit authorization.

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
2. Read the current authoritative documents applicable to the next authorized lineage slice, including the protected Roadmap/coverage and permanent requirements.
3. Mechanically verify repository root, branch, actual HEAD/upstream and divergence, staged/tracked/untracked worktree state and `Directory.Build.props` version. Reconcile those facts against this continuation and stop on any unexpected difference.
4. Confirm from source/migration registration that schema 17 is still dormant, schema 16/alpha.8 is normal authority, normal composition still uses both no-op writers and does not register the projection authority, and no hidden runtime/UI cutover has appeared.
5. Confirm actual HEAD/upstream are synchronized and descend from the verified continuation baseline `904b04cdbb52c2d0d4fca372227e50008e0e0751`; confirm the reviewed implementation checkpoint `fbcf20f3c94011909637c103b8de596f83f7ee8c` remains in that ancestry. Do not require actual HEAD to equal the recorded baseline, because an authorized continuation-only commit necessarily advances it. Preserve unrelated evidence artifacts and stop if ancestry or repository reality is unexpected.
6. Before the first production edit in the next slice, identify and run the precise accepted characterization applicable to the authority being changed.
7. Obtain independent ChatGPT source/diff review before requesting the canonical BAT. Treat static review, targeted tests, focused builds, canonical BAT, migration/recovery rehearsal, concurrency evidence and Windows/operator acceptance as distinct evidence levels.
8. Perform semantic documentation reconciliation for each meaningful pass. Before any staging/checkpoint request, mechanically classify every tracked, staged and untracked path. Do not stage, commit or push without explicit authorization.
9. Preserve the frozen architecture: no second authority, local lineage reconstruction, raw fallback after projection failure, provider leakage into Services/Core, hidden schema/runtime activation, speculative architecture or post-v1 scope creep.
10. Reapply this continuity hard gate before future context rollover; repository reality always wins over continuation prose or historical evidence.

The remaining bounded production sequence is operative and must not be skipped or merged without separate authorization:

1. Atomic schema-17/runtime cutover (separate authorization required; Task 19's implementation, independent review and canonical BAT are complete at `930f2c77b1df7cd4629be2faff662750431854fe`).
3. Audit/history detail.
4. Restore/RemainReversed WinForms UI.
5. Failure/concurrency/full automated gates.
6. Retained-database migration/backup/recovery rehearsal.
7. Windows/operator acceptance, including Batch #30 and real report preview/print.
8. Whole-codebase layer-delineation audit.
9. Security Hardening.

Task 19 does not authorize runtime activation. Its shared-snapshot source/concurrency evidence and canonical BAT are complete at the pushed checkpoint; broader failure/concurrency/full gates, retained-database rehearsal and Windows/operator acceptance remain separate later gates.

Task 18 independent review and the user-run canonical BAT are complete. Task 19 is complete at pushed checkpoint `930f2c77b1df7cd4629be2faff662750431854fe`, with canonical 632/632 evidence; the next separately authorized production slice is atomic schema-17/runtime cutover. Preserve task 18's single transaction-participating projection authority, distinct replacement/new-import temporal scopes, previous ImportRun deletion/provenance evidence, schema-16 compatibility and fail-closed behavior. Atomic activation, retained-database rehearsal and later UI work require separate authorization. Task 20 has not begun.

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

**Answer:** YES. It records task 17 as completed and independently reviewed, with canonical `Build-BinTracker.bat` evidence of 608/608 automated tests passed, 0 failed, 0 skipped and 0 warnings/errors, and implementation checkpoint `5cec97ee1ae2bb927d0142b0645d013d29bc0b87`. It records task 18 as completed, independently reviewed and committed at `7eb3c58ffa9ec5e233eb0c403021744c50449c86`, with canonical `Build-BinTracker.bat` evidence of 613/613 automated tests passed, 0 failed, 0 skipped and 0 warnings/errors; actual repository HEAD must still always be established mechanically rather than predicted by this document. It preserves the source/build/automated evidence limits and frozen semantics, records Task 19's dormant shared-snapshot implementation as complete at pushed checkpoint `930f2c77b1df7cd4629be2faff662750431854fe` with 632/632 automated tests passed, 0 failed and 0 skipped, and retains architecture, migration, audit/history, Restore UI, Batch #30, Git and startup hard gates without authorizing runtime activation.
