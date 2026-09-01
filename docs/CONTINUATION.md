# Active BinTracker Continuation Checkpoint

**Status:** ACTIVE

**Purpose:** Preserve complete continuity through the dormant movement-lineage implementation slices before runtime authority activation.

**Created:** 29 August 2026 (Australia/Sydney)

**Implementation state:** Current checkpoint HEAD is `c0dfc7e51cae1296fd5a5da31876e54364901405` (`Add trusted movement mutation planner`), pushed and synchronized with `origin/codex/movement-correction` at 0 ahead / 0 behind. BIN-LIN-IMP-05/05B/05C completed independent external source/diff review and was approved for its dormant mutation-planning boundary. Canonical `Build-BinTracker.bat` passed at v0.5.0-alpha.8.7: source/package-state audit, restore and build PASS; 438/438 automated tests passed, 0 failed and 0 skipped; no compiler warnings/errors were reported. Schema 17 remains dormant and unregistered, and DatabaseSetup/normal startup/runtime authority remains schema 16/alpha.8. No production lineage correction writer, CAS/idempotency execution, runtime registration, report/projection cutover or WinForms cutover has occurred. IMP-06 and IMP-07 remain unstarted and must not begin without separate authorization. No Windows/operator acceptance or retained-production-database migration rehearsal is implied by this checkpoint.

This record is governed by **Conversation Context Capacity / Continuity Hard Gate** in `docs/DevelopmentWorkflow.md`. A new ChatGPT/Codex session must read the repository-root `AGENTS.md`, that workflow section and this active continuation before modifying the repository. This handoff supplements the authoritative requirements and architecture; it does not replace them.

## Current session update — BIN-LIN-IMP-05C trusted current reversal-pair correction

BIN-LIN-IMP-05C strengthens the dormant, unregistered mutation-planning trust boundary without expanding it into history diagnostics or command execution. The materializer now loads persisted `ReversesMovementId` and returns a trusted snapshot only after each current Reversed terminal row is proven to reverse that exact LastEffective movement, be opposite with equal customer/container/quantity, use Manual provenance and have no ImportRun or physical MovementBatch membership. Planning rechecks those facts and rejects terminal, LastEffective and Active effective dates after the separate authoritative business date. The genuine pre-05C 37-test alpha.8 workflow characterization passed before correction editing. All eight IMP-05B corrections remain: infrastructure-internal materialization; explicit per-Reversed-line Restore/RemainReversed decisions and line-specific restore overrides/masks; complete existing/plan-local result pointers; generic import/adjustment exclusion; separate business date; deterministic persisted reversal-date characterization; truthful approved-NEW classification; and truthful recovered IMP-05 ordering. AppliedFieldMask, no-op, restoration, provenance and physical-output semantics remain unchanged. There are still no persistence, audit, CAS, idempotency, runtime-registration or schema-activation writes. IMP-06/07 remain unstarted; normal startup remains schema 16.

Independent external source/diff review approved this dormant IMP-05/05B/05C boundary. The canonical BAT and synchronized checkpoint evidence are recorded in the operative implementation state above; they do not convert static/automated evidence into Windows/operator acceptance.

IMP-04A corrects the reviewed current-validation contract without expanding the slice. Validated root/line/resolution types are sealed get-only classes with non-public construction, so normal consumers cannot fabricate or `with`-clone successful operational truth. The reader now loads only RootOriginal and current effective/terminal proof links; unrelated superseded historical links do not decide ordinary projectability, while migration postflight retains its global baseline ledger/introduction checks. Current proof now validates exact original `MovementBatchId` membership against `RootMovementBatchId` (or null membership for single roots), preserves `StatusReasonCode`, and rejects negative/duplicate `OriginalDisplayOrdinal`. Schema 17 remains dormant; the later IMP-05 working-tree update above supersedes this section's former sequencing status.

IMP-04 adds an unregistered application-facing resolver contract, immutable current-root/line model, minimal Resolved/NotFound/Unhealthy result and typed current-validation failures. An internal provider-neutral validator proves Active/ReadOnly projectability from exact permanent/current membership, state pointer shape, movement ownership/roles, RootOriginal ownership and same-root/line introductions. Initializing, Invalid and malformed/tampered structured state fail closed. The SQLite reader uses one read transaction and loads only the requested root, permanent lines, selected current generation/state and required evidence; validation performs no lazy reads and no full-history scan.

Migration postflight reuses this current validator for migrated Active/ReadOnly roots while retaining generation-zero/MigrationBaseline action-mask-predecessor rules, legacy-null fields, global migration ownership, zero historical PhysicalOutput and FK checks as migration-only authority. The resolver is not registered in startup, DbContext or services; schema 16 remains normal authority. The later IMP-05 working-tree update above supersedes this section's former sequencing status; IMP-06/07 remain unstarted.

## Historical non-operative update — BIN-LIN-CHECKPOINT-03 reviewed foundation checkpoint

On 31 August 2026, external oversight approved the accumulated BIN-LIN-IMP-01, IMP-02A plus IMP-03A correction, IMP-02C, IMP-03 and IMP-03B dormant lineage foundation for one reviewed local checkpoint commit. The checkpoint records the accepted contracts, fail-closed migration preflight and verified recovery evidence, dormant schema-17 DDL/backfill/postflight, schema-16 Kind 0/1-only correction, tests and governance reconciliation.

At that historical checkpoint, this approval did not activate schema 17. Production startup remained schema 16, the alpha.8 physical whole-batch guard remained authoritative, and no correction writer, resolver, balance/report service or UI had cut over to lineage. The then-current statement that BIN-LIN-IMP-04 had not started and was the next separately authorized slice is historical chronology only and is superseded by the exact-HEAD implementation state above. A real retained-database migration rehearsal, production activation and Windows/operator acceptance remain later gates. No push was claimed by this checkpoint record.

## Historical non-operative update — BIN-LIN-IMP-03B evidence/checklist reconciliation

On 31 August 2026, external review of IMP-03A found evidence-classification and documentation-strength defects, not a new lineage code defect. `TEST-CHECKLIST.md` now separates operator/manual acceptance `[A]`, implemented static/automated evidence `[S]`, a specific outstanding manual retest `[R]`, genuinely pending work `[P]` and repeatable candidate gates `[G]`. Mixed Import Replace/Correct, Market Floor, Batch Entry and Customer Statement lines were split against the permanent requirements ledger, and dormant lineage infrastructure moved from `[R]` to `[S]` because external architectural/oversight approval—not an operator UI test—is the relevant remaining gate.

The IMP-03A 27-Markdown matrix no longer claims `Read fully = Yes` for every file. It records conservative review methods and explicitly acknowledges that the combined raw read was tool-truncated and followed by targeted searches, relevant excerpts and individual-file checks. The semantic conclusions remain unchanged; only the strength and terminology of the evidence claim were corrected.

The substantive IMP-03A schema-16 Kind correction remains unchanged: only historical values 0/1 are valid, every other value blocks migration database-wide, and no unsupported value can acquire schema-17 Reverse/Restore meaning. Schema 17 remains dormant. BIN-LIN-IMP-03B governance correction was complete with external oversight approval pending at that handoff; the later CHECKPOINT-03 update above records the subsequent approval. Its statement that IMP-04 remained blocked and unstarted is historical chronology only and is superseded by the exact-HEAD implementation state above.

## Historical non-operative update — BIN-LIN-IMP-03A full drift repair/reconciliation

On 30 August 2026, independent oversight withheld IMP-03 approval after identifying a cross-slice migration defect: IMP-02A classified a schema-16 `MovementCorrectionOperations.Kind` outside historical values 0/1 as root-scoped ReadOnly, and IMP-03 migrated it unchanged even though schema 17 allocates 2=Reverse and 3=Restore. That could fabricate new semantics from corrupt legacy evidence.

Corrected permanent rule: in a schema-16 source, Kind 0 is Single and Kind 1 is WholeBatch; every other value is a database-wide migration blocker. Preflight now returns `GlobalBlocker/UnsupportedCorrectionKind`, and the migrator independently rejects any prerequisite containing that reason before schema mutation. Values are never normalized or reinterpreted. Legitimate ReadOnly classification remains available for separately proven projection-safe reasons; this fix removes only the unsafe enum case.

New adversarial integration coverage exercises schema-16 Kind 2, 3, -1 and 99 at both preflight and migration boundaries. It proves stable blocking, schema version 16, no committed lineage artifacts, retained verified recovery evidence and unchanged raw Kind. Schema-17 Core/schema tests continue to pin Single=0, WholeBatch=1, Reverse=2 and Restore=3.

This round also completed a semantic review of all 27 governed Markdown files, recorded file-by-file in `docs/DocumentationAudit.md`; IMP-03B subsequently corrected that matrix's review-evidence terminology after external review found the blanket full-read claim stronger than the raw session evidence. It corrected stale current-state report-window wording, balance-reconciliation status, lineage implementation status, security finding-capture/current-version labels, Audit/Admin review debt, migration-backup debt and the active test checklist. The checklist now uses the clarified `[A]`/`[S]`/`[R]`/`[P]`/`[G]` model recorded above. No automated/static evidence was converted into new manual acceptance.

Governance now explicitly separates the mechanical `Audit-BinTracker.ps1` result from semantic all-Markdown reconciliation. Narrow guards protect the exact stale phrases fixed here; they do not claim to automate semantic review. Undefined persisted enum values acquiring later-schema meaning is now an explicit structured-input fail-closed example in Testing, Workflow and BT-REL-012.

Final IMP-03A validation: the corrected focused lineage migration suites passed 55/55; complete Release UnitTests passed 219/219 and IntegrationTests passed 153/153 (372 total), with 0 failed/skipped; Release solution build passed with 0 warnings/errors; 259 permanent requirement IDs had zero duplicates; the mechanical audit passed with 27 Markdown files inventoried; and `git diff --check` reported no whitespace error (only existing LF-to-CRLF notices). The canonical `Build-BinTracker.bat` then passed audit, restore, Debug build with 0 warnings/errors, 219 unit and 153 integration tests. No package, retained-database rehearsal or Windows UI/manual acceptance was performed.

IMP-03A implementation complete; external oversight approval pending. Schema 17 remains dormant, `DatabaseSetup.LatestSchemaVersion` and normal startup remain 16, the retained/user-like database was not touched, and alpha.8 correction/effective-query/report authority is unchanged. IMP-04 requires explicit oversight approval and has not started.

## Historical non-operative update — BIN-LIN-IMP-03 dormant schema 17 and deterministic migration

On 30 August 2026, BIN-LIN-IMP-03 reverified committed HEAD `1d9b9ab7dfaa338629f1c7901b1a8051cd056553`, branch/upstream `codex/movement-correction` / `origin/codex/movement-correction` at `+0/-0`, version `0.5.0-alpha.8.7`, no staging, and exactly the reviewed IMP-01/02A/02C dirty state. Before schema changes, 6 focused unit characterization tests and 69 correction/reversal/import/migration integration tests passed; `SqliteMigrationTests` were explicitly rerun 15/15. No unexpected schema/runtime implementation existed.

The local dormant implementation now adds `MovementCorrectionKind.Reverse=2` and `Restore=3`, persistence-only lineage classes not registered in the production DbContext, and `SqliteLineageSchema17Migrator`. The explicit migrator requires the exact-source upgrade lease, schema-16 read-only preflight and verified recovery artifact from IMP-02A. It is absent from `DatabaseSetup` and `SqliteSchemaMigrations`; `DatabaseSetup.LatestSchemaVersion` remains 16 and `EnsureCreatedAsync` still creates the schema-16 shape.

The migration transaction rebuilds the existing correction-operation table with the frozen operation-envelope columns and kind constraint, creates `LogicalMovementBatches`, `LogicalMovementLines`, `LogicalMovementGenerations`, `LogicalMovementGenerationLines`, `LogicalMovementLedgerLinks` and output-only `LogicalMovementPhysicalOutputs`, adds the nullable unique AuditEvent operation FK, and rebuilds `BinMovements` while preserving every column/index/membership and changing only its MovementBatch delete action from SET NULL to RESTRICT. It then uses persisted correction/reversal/batch/import IDs only to create root-wide generation-0 MigrationBaseline state, zero-based RootMovementId ordinals, Active/Reversed pointers, ownership roles and non-null baseline introduction links. Legacy operation request/schema/expected/result generation fields remain null; generation 0 references no operation; historical PhysicalOutput backfill is exactly zero; operation root and AuditEvent links use unique structural proof only. Import/Adjustment rows remain outside generic lineage. A structurally complete root carrying a separately approved projection-safe ReadOnly reason may receive a complete projection and stable reason while unrelated roots remain Active; schema-16 unsupported operation Kind is not such a reason and blocks the database before writes. Invalid/GlobalBlocker/unscoped ReadOnly evidence fails before writes rather than receiving invented state.

Transaction-bound postflight checks required tables, no committed Initializing root, exact root/line/current-generation completeness, baseline action/mask/predecessor/operation shape, Active/Reversed pointers, pointer ownership and transformation roles, non-null same-root/line introduction links, complete ordinary movement ownership, null legacy new-write-only fields, zero historical PhysicalOutput and zero FK violations. Twelve deterministic failure-injection checkpoints cover prerequisites-to-schema, every backfill stage, legacy associations, FK rebuild and postflight/publication. Every injected failure opens a fresh connection and proves schema 16, no committed lineage tables, valid FKs and preserved verified recovery evidence.

New schema tests use only disposable SQLite databases. They cover schema-16 startup nonactivation; exact baseline and Batch-30-like Active/Reversed shape; unique structured audit association; DDL tables/indexes/FKs/CHECK/UNIQUE/output selector; future generation-linked PhysicalOutput capability; MovementBatch RESTRICT membership; ImportRun-owned movement deletion; whole-file developer reset; schema-17 rerun; partial schema/wrong prerequisites; all failure checkpoints; Invalid graph rejection; fail-closed unsupported schema-16 operation kinds; and postflight pointer-ownership tamper rejection. `MovementLineageContractTests` plus new unit guards pin all enum/schema/checkpoint identities.

Development iterations retained for continuity: the first Data build exposed four `DbTransaction`/`SqliteTransaction` compile errors; the first integration compile exposed a static fixture call and wrong gate type; the first run had 20 sandbox-denied lock-path failures; the next had two fixture/assertion/lease-cleanup failures; a later added physical-output test failed once because its raw MovementBatch insert omitted required `IsReversed`; and one investigation `rg` command had a PowerShell quote terminator error. Each introduced problem was corrected without suppression. The final focused schema suite passed 28/28; the combined lineage/migration/alpha.8/import regression filter passed 113/113 before the last two adversarial schema cases were added; final complete UnitTests passed 219/219 and complete IntegrationTests passed 146/146. Release solution build passed with 0 warnings/errors; `Audit-BinTracker.ps1` passed with 259 permanent IDs/27 Markdown files; and the final canonical `Build-BinTracker.bat` passed restore, Debug build (0 warnings/errors), 219 unit and 146 integration tests. This is automated/static evidence only: no real retained database migration rehearsal, packaging, Windows UI/DPI or operator acceptance occurred.

Governance now includes BT-REL-011 characterization-before-change and BT-REL-012 structured-input fail-closed testing, with matching rules in `docs/Testing.md`, `TEST-CHECKLIST.md` and `docs/DevelopmentWorkflow.md`. `docs/Database.md` records schema 17 as implemented only behind isolated dormant migration tests. Runtime mutation/projection/report/UI semantics are not marked implemented.

IMP-03 stopping point: review this dormant schema/migration layer. Do not activate it or begin IMP-04 automatically. The next dependency-ordered slice, only after oversight approval, is the client-neutral resolver plus operational/audit invariant-health validator foundation; it must not yet switch writers or numeric consumers.

## Historical non-operative update — BIN-LIN-IMP-02C schema-capability/population reconciliation

On 30 August 2026, BIN-LIN-IMP-02C reverified committed HEAD `1d9b9ab7dfaa338629f1c7901b1a8051cd056553`, branch/upstream `codex/movement-correction` / `origin/codex/movement-correction` at `+0/-0`, version `0.5.0-alpha.8.7`, no staging and exactly the accepted IMP-01/02A plus provisional 02B dirty files. Schema remains 16; no IMP-03 implementation or runtime-authority change exists. This round changed authoritative documentation only.

The retained adversarial/final-freeze evidence and reviewed implementation plan resolve the 02B ambiguity by separating schema capability from migration population:

- schema 17 retains `LogicalMovementPhysicalOutput` with exactly one generation or uniquely-proven legacy-operation selector;
- schema-16 -> 17 MigrationBaseline creates **no** `LogicalMovementPhysicalOutput` rows for historical correction-output batches and never claims generation 0 created them;
- the seven historical output batches observed in the final-freeze database remain evidenced by `MovementCorrectionOperations.ReplacementBatchId`, their `MovementBatch` rows and exact `BinMovement.MovementBatchId` membership;
- every new truthful lineage-native correction-output batch created after activation must have exactly one generation-linked output row;
- any later conversion of legacy physical-output evidence would require a separately authorized deterministic process and cannot fabricate chronology.

The complete population contract is now explicit in `docs/Database.md` and the relevant permanent requirements. Active/ReadOnly migration must build full generation 0, lines, state pointers, ownership/roles and non-null ledger introduction pointers. A baseline `IntroducedByGenerationLineId` points to that line's generation-0 state to mean “introduced into the logical-lineage model”, not that the historical movement was created then. Generation predecessor/operation and generation-line predecessor are null for MigrationBaseline. Legacy operation `RequestJson`, `RequestSchemaVersion`, `ExpectedGenerationNumber` and `ResultGenerationNumber` remain null because they cannot be truthfully reconstructed; `LogicalMovementBatchId` is populated only from a unique structural root proof. Existing operation IDs/fingerprint/kind/original/replacement evidence is preserved unchanged. No separate target-line FK is frozen.

Schema-17 migration populates `AuditEvent.MovementCorrectionOperationId` only for the unique complete structured-ID matches governed by BT-AUD-017; weak/unmatched legacy evidence stays null with a diagnostic. Exact legacy correction-line association on a ledger link is likewise conditional structural evidence. Existing `BinMovement.MovementBatchId` values are preserved while schema 17 changes ordinary delete behavior from SET NULL to RESTRICT/NO ACTION; Import Replace/Correct movement deletion and developer whole-database reset require regression coverage but do not justify detachable physical evidence.

The inspected 495-movement/30-batch/10-operation/17-triple/7-reversal database would therefore produce 28 Active roots with complete baselines and ledger ownership, including Batch #30's Active/Reversed pair; retain all 10 legacy operations, conditionally root-link them, leave their new request/generation fields null, conditionally link the strong audits, and create zero historical physical-output association rows for the seven legacy outputs. Every real database must still repeat preflight.

Files changed by 02C: `docs/Database.md`, `docs/RequirementsAcceptanceRegister.md`, `docs/Architecture.md` and this continuation only. No source, tests, EF, migration, schema constant, startup, database or runtime behavior changed. The permanent enum values and schema-17 allocation from 02B were not reopened. `Audit-BinTracker.ps1` passed at v0.5.0-alpha.8.7 with 257 permanent IDs/27 Markdown files/current-state contradiction checks; corrected first-column validation found 257 IDs and zero duplicates; targeted migration-population contradiction searches found no remaining current-authority conflict; `git diff --check` passed with only line-ending notices. The final expected worktree adds these four modified documentation files to the accepted IMP-01/02A dirty state, remains unstaged, and is still `+0/-0`. IMP-03 remains prohibited until oversight approves this reconciliation; all repository facts must be rechecked mechanically by the next session.

## Historical non-operative update — BIN-LIN-IMP-02B persistence-contract documentation freeze

On 30 August 2026, BIN-LIN-IMP-02B mechanically reverified the same committed HEAD/branch/upstream/version and exactly the accepted IMP-01/IMP-02A dirty state; nothing was staged and no IMP-03/schema/runtime implementation existed. This round changed documentation only.

Source, migrations, tests, current authoritative documents and retained reviewed/final-freeze evidence were reconciled before allocation. `MovementCorrectionKind.Single=0` and `WholeBatch=1` are the only current enum members; migration/schema SQL contains no competing operation-kind CHECK; no current or historical migration assigns values 2/3 another meaning. The reviewed implementation plan explicitly assigned `Reverse=2` and `Restore=3`, and no later frozen decision superseded those meanings. BT-CORR-025 and `docs/Database.md` now make all four values permanent persisted identities. Production C# remains deliberately unchanged until the replacement IMP-03 implementation slice.

The active SQLite migration catalogue and derived `DatabaseSetup.LatestSchemaVersion` end at 16, no hidden/uncommitted schema implementation exists, IMP-02A preflight explicitly expects source schema 16, and no other feature owns 17. The logical-lineage migration is now permanently allocated as **schema 16 -> schema 17** in BT-CORR-030 and `docs/Database.md`. This allocation does not change `LatestSchemaVersion`, create a migration or activate startup.

The persistence map was reconfirmed without semantic redesign: `RootMovementBatchId` remains the sole original physical-batch authority; `LogicalMovementPhysicalOutput` is named and mandatory only as the output association when an optional truthful physical output exists; the existing operation table evolves rather than being replaced; `MovementCorrectionLines` remains legacy forensic evidence; `AuditEvent.MovementCorrectionOperationId` is nullable/unique/RESTRICT for one primary new-operation audit; evidence relationships use RESTRICT/NO ACTION; no `MovementSource.Correction` or authoritative before/result JSON is introduced.

Files changed by BIN-LIN-IMP-02B: `docs/Database.md`, `docs/RequirementsAcceptanceRegister.md` and this continuation. No requirements were added or renumbered. `Audit-BinTracker.ps1` passed at v0.5.0-alpha.8.7 with 257 permanent requirement IDs and 27 Markdown files; a correct first-column register check found 257 IDs and zero duplicates; targeted contradiction searches found no `v17-or-next` or superseded `LogicalMovementPhysicalBatchLink` wording in current authoritative files; `git diff --check` passed with only existing LF-to-CRLF notices. No application build/test was run because this was documentation-only.

Final expected unstaged state after this round is modified `docs/CONTINUATION.md`, `docs/Database.md`, `docs/RequirementsAcceptanceRegister.md` and the pre-existing IMP-02A `src/BinTracker.Data/DatabaseConfiguration.cs`; untracked IMP-01/02A Core contracts, migration infrastructure and their two test files; nothing staged. IMP-03 was not started. The next safe step after oversight approval is a replacement schema/migration prompt using these permanent identifiers.

## Historical non-operative update — dormant Core lineage foundation

On 30 August 2026, repository reality was reverified at committed HEAD `1d9b9ab7dfaa338629f1c7901b1a8051cd056553` (`Add conversation continuity hard gate`), branch `codex/movement-correction`, upstream `origin/codex/movement-correction` at `+0/-0`, with a clean worktree and version `0.5.0-alpha.8.7`. No unexpected lineage implementation existed.

Before production edits, the existing correction/reversal characterization suites were run unchanged:

```text
dotnet test tests/BinTracker.IntegrationTests/BinTracker.IntegrationTests.csproj --no-restore --filter "FullyQualifiedName~MovementCorrectionSqliteTests|FullyQualifiedName~MovementCorrectionWorkflowTests|FullyQualifiedName~MovementCorrectionConcurrencyTests" --logger "console;verbosity=minimal"
Passed: 49, Failed: 0, Skipped: 0
```

The current local implementation adds only:

- `src/BinTracker.Core/MovementLineageContracts.cs` — dormant client-neutral logical root/line/generation identity value types, root status, line state, generation action, transformation role and explicit movement-change field mask;
- `tests/BinTracker.UnitTests/MovementLineageContractTests.cs` — persisted numeric-value/identity/flag guards.

Focused post-change validation recorded:

- new contract tests: 4 passed, 0 failed/skipped;
- the same alpha.8 correction/reversal suites: 49 passed, 0 failed/skipped;
- builds performed by those focused `dotnet test` commands produced no warnings/errors.

No DbSet/EF configuration, schema migration/version, startup behavior, movement writer, correction/reversal service, `EffectiveMovementQuery`, report/balance path, import behavior or WinForms surface changed. `MovementCorrectionKind.Single=0` and `WholeBatch=1` remain protected; BIN-LIN-IMP-02B has now authoritatively allocated `Reverse=2` and `Restore=3`, but production C# must add them only in the approved schema implementation slice with persisted-value tests.

Historical IMP-01 stopping point: review the dormant foundation before the read-only migration-safety slice. That later slice is now present and corrected as recorded below; do not repeat it.

## Historical non-operative update — BIN-LIN-IMP-02/02A migration-safety infrastructure

On 30 August 2026, BIN-LIN-IMP-02 started from the same committed HEAD/branch/upstream/version and exactly the accepted three-file BIN-LIN-IMP-01 dirty state; nothing was staged and no unexpected lineage implementation existed. Focused pre-edit characterization passed: 6 unit tests covering database configuration and the dormant lineage contracts, and all 15 existing `SqliteMigrationTests`.

The current local slice additionally adds:

- `src/BinTracker.Data/DatabaseConfiguration.cs` — frozen LocalAppData recovery/companion-lock locations, separate from ordinary developer backups;
- `src/BinTracker.Data/LineageMigrationInfrastructure.cs` — dormant provider-facing contracts and SQLite/Windows implementations for a physical-database-scoped shared-runtime/exclusive-upgrade gate, read-only schema-v16 structural lineage preflight, exact-source verified recovery backup/manifest/checksums, and recovery disposition;
- `tests/BinTracker.IntegrationTests/LineageMigrationInfrastructureTests.cs` — isolated temporary-database coverage for gate contention/release/path scoping/no database mutation, deterministic correction/reversal/repeated-chain/import/partial-batch preflight, invalid/cross-domain/physical-batch rejection, backup/hash/manifest/integrity/FK/schema/table-count/preflight equivalence, tamper rejection, and recovery classification;
- `docs/Database.md` — durable implementation-detail record that this safety infrastructure exists but remains unactivated.

The corrected concrete gate derives a companion lock identity from the selected database's Windows volume/file ID, so normalized paths and hard-link aliases cannot bypass it. Normal participating runtimes can hold shared read leases; upgrade/recovery requires an exclusive lease and checks the pending-operation marker before ownership is returned. It is scoped per physical database rather than making BinTracker globally single-instance, releases on disposal or process termination, and has a real child-process contention/termination test. Production activation must make every database-using process acquire the runtime lease before opening/using the database, and must retain one exclusive upgrade lease over the complete preflight/backup/comparison/migration/postflight critical section. This is the mechanical no-conflicting-operation contract; the current slice deliberately does not hook it into startup.

Preflight opens SQLite with `Mode=ReadOnly`, `query_only=ON`, private non-pooled connections and FK enforcement. It verifies the expected schema/tables, full `integrity_check`, `foreign_key_check`, correction triples, ordinary reversal links, correction chains, physical batch relationships, generic-lineage/import-or-adjustment separation and graph cycles. It emits stable classification/reason codes, relevant category counts, exact application-table counts and a canonical structural SHA-256 fingerprint. It never matches customer/container/date/quantity/text/timestamps and writes no lineage data.

The frozen production recovery directory is `%LOCALAPPDATA%\BinTracker-RecoveryPreUpgrade`, separate from developer/ordinary backup retention and never automatically deleted in v1. Backup names are `BinTracker-pre-lineage-v<schema>-<yyyyMMddTHHmmssfffZ>-<short-guid>.db`; an atomic no-overwrite publish prevents collision overwrite. The service requires a non-empty provider-consistent SQLite `BackupDatabase` result, reruns source/backup preflight, and compares integrity, FKs, schema, exact application-table counts and structural fingerprint. Its versioned manifest records artifact/purpose/application/provider, canonical and physical source identity, source schema/size/timestamp/journal mode, backup size/hash, counts/classification/fingerprint and recovery-policy instructions. Adjacent checksum evidence independently binds both backup and manifest hashes. `VerifyForSourceAsync` must match the expected active database path, path hash, Windows physical file identity and schema; a valid backup for database A is not recovery evidence for database B. Failed construction deletes only its own incomplete unique artifacts where possible and never returns them as verified.

Recovery classification now has three stable outcomes: preserve a mathematically valid active database; allow controlled restore only when the active database is invalid/unusable and a backup verifies after a known migration failure; or prohibit recovery when the backup is not verified/conditions are unknown. It performs no automatic restore.

IMP-02A corrected oversight gaps in destination/naming/no-overwrite, non-zero verification, manifest evidence, dual checksums, exact-source binding, alias-safe physical identity, shared/exclusive cross-process participation, pending-operation proof and frozen retention. Development iterations included one compile failure from an initially selected source-generated interop declaration under the repository's unsafe-code policy, one compile failure for conversion/nullability issues, one test compile failure while test helpers were incomplete, and one 16/19 test run exposing shared-lock-file creation and child-process argument defects; each introduced defect was corrected rather than suppressed. Final corrective validation passed 20/20 infrastructure tests, 6/6 targeted unit tests, and 84/84 combined integration tests (20 infrastructure + all 15 `SqliteMigrationTests` + the retained 49 alpha.8 correction/reversal characterization tests), with no failures/skips or build warnings/errors. `Audit-BinTracker.ps1` passed at version `0.5.0-alpha.8.7` with 257 permanent requirement IDs and 27 Markdown files. This is focused automated/static evidence, not the canonical BAT/full suite, a production migration rehearsal, or Windows/operator acceptance.

This remains deliberately dormant: `DatabaseSetup.InitializeSqliteAsync` still calls `EnsureCreatedAsync` before numbered migrations; schema stays v16; no runtime or upgrade lease is registered/invoked during startup; no backup/preflight runs automatically; no lineage row is persisted; no correction/reversal/report/UI authority changed. Do not begin the replacement IMP-03 until oversight approves the IMP-02B persistence-contract freeze.

## Historical initial mechanically verified repository baseline (non-operative)

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

This was the baseline when the original continuation was created. It is superseded by the exact active implementation state at the top and the current next-session instructions below. A later session must still stop and investigate any discrepancy against those active sections; do not use this historical file list to discard or overwrite newer reviewed work.

## Historical evidence levels retained from continuation creation

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

### Documentation-freeze validation at its historical HEAD

The committed documentation freeze at `faaafb2...` changed no source, tests, schema or version. `docs/DocumentationAudit.md` records:

- 257 unique permanent requirement IDs;
- 26 governed Markdown files;
- the mechanical governance audit passed;
- targeted contradiction searches passed;
- `git diff --check` passed.

No newer full application build/test run was claimed for that documentation-only commit.

### Historical local changes at continuation creation

The current governance and continuation Markdown edits are not application-tested and do not require an application build merely to establish continuity. Before implementation, mechanically verify the baseline and run the focused/full tests appropriate to each implementation slice. Before a distributable lineage activation, run the canonical `Build-BinTracker.bat`, mandatory audit, migration tests against copies of representative databases, and required Windows acceptance. Never claim those gates before they actually pass.

## Current development phase and original objective

The project has completed the externally reviewed dormant Core, migration-safety, schema-17 migration, IMP-04/04A current-root validation and IMP-05/05B/05C mutation-planning foundations. The planning checkpoint is committed and synchronized at `c0dfc7e51cae1296fd5a5da31876e54364901405`. Runtime correction/reversal/restoration and reporting authority have not changed.

The original objective was to extend the safe but limited alpha.8 immutable correction/reversal workflow so whole-batch correction remains truthful after individual corrections, partial reversals, repeated corrections, restoration, mixed dates, partial no-ops and explicit `RemainReversed` decisions. Investigation was deliberately completed before code because a false lineage or balance can silently corrupt operational reporting.

IMP-06/07 remain unstarted and must not begin without separate authorization. Dormant persistence types/migration/resolver/planner exist only behind isolated tests; no production registration, activated migration, command writer, projection cutover or Restore UI exists yet.

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

## Baseline codebase findings and implementation seams

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

Expected persisted enum values are explicit and permanent. `MovementCorrectionKind.Single=0`, `WholeBatch=1`, `Reverse=2` and `Restore=3`; statuses/actions/states/roles must match `docs/Database.md` and the frozen contract tests. Never renumber persisted values without an explicit data migration.

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

The reviewed dormant/unactivated foundation is recorded by BIN-LIN-CHECKPOINT-03. Outstanding work in the dependency chain includes:

- retain and extend the focused alpha.8 characterization and isolated lineage fixtures as each authority changes;
- keep schema-17 production activation blocked pending its later rehearsal and activation gates;
- preserve the completed dormant IMP-04/04A current-root resolver and validator without broadening their boundary;
- preserve the externally approved IMP-05/05B/05C dormant planner boundary; do not begin IMP-06/07 without separate authorization;
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
7. Inspect checkpoint `c0dfc7e51cae1296fd5a5da31876e54364901405` (`Add trusted movement mutation planner`) and any remaining working-tree files. Preserve unrelated evidence artifacts; investigate any discrepancy before application-code changes.
8. Confirm that checkpoint is synchronized with `origin/codex/movement-correction`, contains the externally approved dormant/unactivated IMP-01 through IMP-05/05B/05C foundation, leaves schema 17 dormant and normal runtime authority at schema 16/alpha.8, and has no production writer/CAS/idempotency/runtime/report/UI cutover. IMP-06/07 are unstarted and prohibited until separately authorized. If repository reality differs, stop and reconcile it rather than layering work over it.
9. Preserve the truthful characterization record: the original IMP-05 ordering gate was missed; the full 37-test alpha.8 workflow suite was executed before IMP-05B correction edits as recovery evidence and rerun afterward. IMP-05C separately ran that exact suite before and after its correction edit. The later canonical BAT passed 438/438 with 0 failed/skipped and no reported compiler warnings/errors. Do not reinterpret those automated results as Windows/operator acceptance or retained-production-database migration rehearsal.
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
