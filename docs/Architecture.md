# BinTracker Architecture

Current baseline: **v0.5.0-alpha.8.7**

## Display and DPI boundary

The required frequently-used laptop acceptance configuration is Windows 11 at **1920x1080 with 150% Windows scaling**. Every ordinary workflow and modal must remain accessible: action bands stay visible, content fits or scrolls within the working area, text does not clip, controls do not overlap, report grids remain usable, and normal WinForms DPI scaling is preserved.

The primary production environment uses a substantially larger display. The laptop configuration is an acceptance floor for usability, not a direction to globally reduce information density or optimise every screen specifically for a 14-inch panel. Layout concepts should remain presentation-neutral where practical so a future WinUI 3 client can replace WinForms presentation without moving business rules into UI controls; no WinUI dependency is introduced in v1.

Detail/investigation windows use available working-area space for compact identifiers and values, wrapping only genuine prose such as notes/reasons. Main report grids preserve single-line structured fields and selectively wrap long semantic Status/Notes cells with auto-height so the primary workspace does not become unnecessarily wide.

## Permanent target and hard gate

BinTracker's future architecture is **WinForms / web / phone / other clients → authenticated BinTracker application/API contracts → provider-specific Data implementation → suitable SQL database**. SQLite is the current local provider. PostgreSQL is a likely future central provider, not an architectural assumption or the only permitted SQL provider. Clients never receive database credentials or connect directly. v1 remains the local SQLite WinForms product; its mandatory groundwork is client-neutral services, provider-neutral semantics, concurrency/idempotency correctness and replaceable presentation, not API/central-provider deployment.

All production business code must assume multiple authenticated remote users can execute the same operation concurrently. Core, Services, UI contracts and the shared EF model remain provider-neutral; provider migrations and local database tooling stay in `BinTracker.Data`.

The layer boundary is a hard rule:

- Core owns provider-neutral domain semantics, lineage rules, immutable contracts and business invariants.
- Services owns provider-neutral and client-neutral application workflows/orchestration and may internally use the shared provider-neutral EF model, EF/LINQ and `IDbContextFactory<BinTrackerDbContext>` as established by BT-ARCH-004.
- Application-facing/public service contracts expose only intent, stable IDs, DTOs/results and appropriate cancellation/context abstractions; they must not expose EF Core, `BinTrackerDbContext`, database connections/transactions, provider selection, credentials, local-file assumptions or presentation-specific behavior. Internal persistence participation must not become a public application interface.
- Data owns provider configuration, schema/migrations, backup/recovery, provider-specific SQL and database-specific transaction/locking/storage mechanics. SQLite-specific PRAGMAs, locking assumptions, raw SQL and provider quirks remain in Data and must never define business semantics. Do not create a generic repository merely to hide EF.
- WinForms owns presentation only and submits stable IDs/user intent. Future web/phone/other clients must consume the same application semantics without knowing or caring which database provider is underneath. Another suitable SQL provider must be possible without rewriting lineage, reporting, import, authorization, idempotency, concurrency or correction semantics.

Task 19's review correction keeps `IOutstandingReportService` and `IDailyMovementsReportService` as query/result-only public contracts. Two internal Services participant interfaces, implemented explicitly by the existing internal report classes, may continue using `BinTrackerDbContext` because they are non-client-facing internal composition under the existing BT-ARCH-004 Services/Data boundary. The reports continue using their one existing implementation; Data's projection authority retains ownership and database-specific mechanics of this shared read transaction. Integration-test wrappers access these internal participants through a test-only friend assembly, not public application APIs. This documentation clarification requires no implementation change; do not reopen the shared-snapshot implementation without a concrete defect or expand it into a whole-codebase persistence refactor or runtime registration change.

## Host composition and request context

BT-20-P2 activates the previously reviewed Task20D/E/F/P1/FIX1 seams as one normal-runtime boundary. WinForms builds the host, calls the single Data-owned `IStartupDatabaseCoordinator`, and resolves no application service until it receives schema17 readiness. The host retains `StartupDatabaseSession` until every service scope/factory is disposed. Normal SQLite composition registers the native initial-lineage writer, typed Single receipt store, logical mutation writer and transactional operational projection authority; the schema17 catalogue entry cannot execute through the legacy numbered callback. Developer Load/Fresh markers are claimed and validated by the same coordinator, and are removed only after the selected database has been reclassified, activated and revalidated. Normal correction/reversal UI submits stable logical identity, preview-captured generation and explicit changed-field intent; every alpha.8 mutation write route remains closed under native17. Explicit schema16 compatibility fixtures may omit these authorities, but they are not normal application composition.

Task20D established the R4/R5 coordinator, Task20E R1 receipt/replay, Task20F R2 preview/CAS/legacy closure, and BT-20-P1/FIX1 the bounded R3, EF-membership and R7 safety prerequisites. BT-20-P2 changes their normal composition and startup authority. BT-20-P3 completes native Audit/History detail by projecting the already validated operation/root/generation/line/ledger evidence through a provider-neutral service result. Before it attributes historical prior or transformation evidence, Data proves the predecessor has this root/permanent line and the operation's exact preceding generation, and proves the ledger owner and introducing generation-line have the same permanent line. It neither creates a second audit/lineage authority nor changes correction/reversal semantics. Restore/RemainReversed UI, retained-database rehearsal, Windows/operator acceptance, the protected layer audit and Security Hardening remain separate.

The configured Data adapter implements `IStartupDatabaseCoordinator.StartAsync` without persistence types on its result contract. `StartupDatabaseSession` retains runtime participation and a physical-file pin until disposed; `StartupDatabaseException` distinguishes failure reason, lifecycle phase, attempted schema mutation, replacement publication and a fresh persisted schema/physical-identity observation. An unknown observation stays Unknown, and committed17 is never described as rolled-back16. Task20D-R1 retains the newly published replacement identity through runtime acquisition and final validation, rejecting a competing replacement with IdentityChanged. Concurrent bootstrap StartAsync calls wait on a cancellation-aware kernel completion rendezvous, then the losing call reclassifies without a new caller retry. This completion ordering does not confer database ownership: physical identity leases/pins remain mandatory. Internal lifecycle hooks support deterministic failure/interleaving proof. Existing physical-identity leases, preflight/backup, strict migration publication, provider-neutral current-root validation and operational projection remain their respective authorities. Provider shape inspection compares actual SQLite capabilities against the same DDL used by migration, with shared-model base-column checks; it introduces no second lineage/business validator.

Task20 activation is governed by the exact R1–R4 freeze below. R1 is now normal-runtime behavior after schema17 readiness. R5 requires native schema17 startup to prove tables, columns, PKs, FKs with RESTRICT, unique indexes, relevant CHECKs, current root/generation health and absence of relevant orphan ordinary evidence. The activated coordinator reuses the existing native validator and projection authority rather than creating a second validator. Valid Initial and later native generations need no migration artifacts. Strict baseline postflight remains migration-specific, and a failure after COMMIT preserves the actual committed17 state and original diagnosis.

R6: companion leases coordinate participating BinTracker processes only, not old binaries, SQLite tools or arbitrary external writers. Retained-database rehearsal/deployment requires old and nonparticipating clients stopped. R7 fixes only the four deterministically characterized result-affecting gaps: Import replacement comparison, Dashboard attention, customer search and customer summary now combine metadata/evidence and corrected projection through the existing provider-consistent snapshot seam. Task19 Daily Print Pack shared-snapshot authority remains frozen. The EF relationship now uses client no-action semantics so deleting a loaded schema17 batch cannot null immutable tracked membership; Data preserves schema16 `SET NULL` compatibility and the schema17 capability validator continues requiring persisted `RESTRICT/NO ACTION`.

`AddBinTrackerBusinessServices()` registers provider-neutral business services. A future API must provide request-scoped `IUserContext` and `IClientContext`, and may supply its business-clock configuration. It must not inherit the desktop singleton `UserSession`.

`AddBinTrackerServices()` is the current desktop composition and adds the local session, client identity, authentication adapter and crash-draft storage before registering business services.

- `IUserContext` carries authenticated user identity and role.
- `IBusinessClock` supplies UTC and configured business-local time/date.
- `IClientContext` carries client provenance; an API must not record its own host as the operator workstation.

## Transport boundary

Imports cross the service boundary as `ImportSourceDocument` content plus safe metadata. `SourceClientPath` is provenance only and is never treated by business/server logic as a readable path. PDF/report services return bytes; the desktop client chooses where to save or open them. Local crash-draft recovery and developer database tools remain explicitly desktop-local.

## Concurrency, retries and invariants

- Customer, Container Type and Application Settings use optimistic `Revision` tokens.
- Container Type `NameKey` uses provider-neutral Unicode/case normalization and a unique database index.
- A nullable unique `ImportRun.CurrentCutoverDate` gives one current run ownership of a cutover; replaced runs retain provenance with null ownership.
- ImportRun provenance has two distinct immutable snapshots: `CorrectionChangesJson` for same-cutover Replace/Correct comparisons, and `OpeningReconciliationChangesJson` for non-zero opening adjustments generated from normal authoritative cutovers. NULL means the historical build did not capture that snapshot; `[]` means capture occurred with no changes.
- Single Entry, Batch Entry, reversal and import persist client operation IDs. Exact canonical replay returns the original persisted result where truthful persisted result evidence exists; reuse with a different payload is rejected. Normal schema17 composition implements R1 with an immutable typed Single receipt and its sole compatibility exception: a migrated/pre-receipt Single recognizes identical command identity without duplication or lineage rewrite, but returns the controlled legacy-replay-result-unavailable outcome rather than fabricated prior numbers. Batch and logical mutation exact-result replay guarantees remain intact. Explicit schema16 compatibility Single retains its legacy behavior and may recompute a retry balance; it is not the activated17 authority.
- Reversal and import uniqueness constraints are authoritative under races.
- Correction extends the same invariant: the unique neutraliser FK (`ReversesMovementId`) arbitrates Reverse-vs-Reverse, Reverse-vs-Correct and Correct-vs-Correct; correction-operation identity/fingerprint makes identical retries return persisted lineage and rejects changed payload reuse.
- The alpha.8 correction transaction writes neutralisers, replacements, operation/line evidence, consumed links and audit. Its physical-`MovementBatchId` whole-batch guard remains safe until the frozen logical-root model below replaces it; do not remove the guard independently.
- The alpha.8 effective query suppresses correction-consumed originals/neutralisers while retaining immutable Movement History. The frozen model below replaces that technique with validated current-generation activity without weakening accepted results.
- Administrator acknowledgement is a review record, not an effectiveness gate: Operator corrections/reversals remain operationally effective immediately. Review authorization and duplicate prevention remain service/database concerns even when the Audit Trail disables ineligible UI actions.
- The outstanding-review count/state and navigation action must be exposed through a presentation-independent service/state/navigation contract. Current WinForms may render that contract with a reusable infobar-style `UserControl` or panel; a future WinUI 3 client replaces only that presentation with native `InfoBar`, retaining the underlying contract. Review business logic must not live in a disposable UI control.
- Audit detail navigation is keyed by authoritative persisted entity identity. MovementBatch detail is never inferred from description text; future contextual routes may use authoritative ImportRun or correction-lineage identity when available.
- Authentication counters and account/credential mutations use conditional or atomic database updates.

Moving to PostgreSQL still requires an API host, provider/schema migrations, authentication/authorization deployment, central backup/monitoring and real PostgreSQL integration tests. It must not require rewriting accepted business, reversal, import, report or audit semantics.

## Frozen movement-lineage architecture (normal schema17 runtime activated; acceptance remains planned v1)

### Authority and state

- `BinMovement` is immutable forensic ledger evidence. `MovementBatch` is immutable physical persistence evidence; operational calculations use movement rows, not a conflicting header.
- `LogicalMovementBatch` is the stable root and roots never merge/split. Its nullable `RootMovementBatchId` is the sole original physical-batch link; single-entry roots have none.
- `LogicalMovementLine` is one permanent original-business-line identity with one RootMovementId and remains a member while reversed.
- Every substantive mutation creates a root-wide generation with one full state row for every permanent line. `CurrentGenerationNumber` is sole current-state/root-concurrency authority; lines have no competing current pointer.
- Active state references one result-effective movement. Reversed state references the last effective movement and terminal ordinary reversal.
- Movement links own RootOriginal, CorrectionNeutraliser, CorrectionReplacement, OrdinaryReversal or Restoration transformation role. `MovementSource` remains provenance; no Correction source is added.
- The existing physical correction-operation table evolves as the single `MovementChangeOperation` envelope. Existing correction lines remain legacy evidence, not a second authority.

Actions are Initial, MigrationBaseline, CarriedForward, AlreadyMatches, Corrected, Reversed, Restored and RemainReversed. Complete semantic no-op creates no artifact. Restoration is substantive without overrides. `OriginalDisplayOrdinal` is immutable presentation metadata (request order; zero for single; ascending RootMovementId on migration), never identity.

`AppliedFieldMask` is line-level evidence of the complete explicit business-field selection governing that line's requested result, including selected-but-equal values. Initial, MigrationBaseline, CarriedForward, Reversed and RemainReversed use `None`; Corrected and AlreadyMatches use the exact nonempty correction selection; Restored uses the exact restoration override selection and may use `None`. It is not a changed-value, movement-creation, substantive-operation, no-op or physical-output flag. Action/state/pointers remain result authority and canonical RequestJson retains absent/null/value intent.

Movement History/Audit preserve all evidence. Normal reports project retrospectively corrected current state: Active emits its effective movement once; Reversed emits last effective plus terminal reversal. PositionAsOf(D) signs those rows where `MovementDate <= D`; current position uses the injected business date. GenerationNumber, MovementDate and CreatedUtc are semantic, reporting and forensic order respectively.

Restoration means the reversal was erroneous: start from the last legitimate pre-reversal state, inherit unselected fields and apply explicit overrides. Legitimate later activity is a new line. A whole-root request may RemainReversed with zero contribution/no fake movement. Historical correction dates through today are valid; future dates are not. Period/high-risk approval remains post-v1.

### Physical output, operations and integrity

A correction-output physical batch and its output-only link are optional. It exists only for a whole physical-origin root whose every line receives a newly created Active Corrected/Restored row in that generation, with no carried/already-matching/reversed/remain-reversed line, one truthful date/direction/Batch provenance, no ImportRun, exact members/header and atomic creation. Neutralisers/reversals never join it.

Canonical versioned request JSON/fingerprint records intent (including absent/null/value), never state/report truth; generation lines have structured pointers/action/field mask and no authoritative before/result JSON. Exact ClientOperationId retry returns its committed result even after later generations; changed reuse fails.

Root-wide CAS is v1 concurrency authority. Portable PK/FK/RESTRICT/UNIQUE/CHECK/index constraints combine with transactions/validators. No business rule depends on SQLite triggers, rowid, locking, deferred FKs or disabled FK enforcement. `IntroducedByGenerationLineId` is nullable only during construction/backfill and populated before Active or ReadOnly commit.

Every numeric read validates relevant complete current snapshots, projects movement IDs and aggregates in one provider-consistent read snapshot. Invalid/unrooted data fails potentially affected results without omission/raw fallback. ReadOnly roots remain projectable but immutable.

Under normal schema17 composition, Daily Print Pack delegates both Outstanding PositionAsOf and exact-date Daily Activity through one provider-owned read snapshot on one caller-owned context. The existing transactional projection authority owns that read boundary; the existing report services participate sequentially and retain their sole filter/group/order/total logic. Their customer/container reads share the transaction because active status affects Outstanding inclusion and names/types/display order must agree across sections. The pack uses the projection dependency only to coordinate the read boundary, never to reconstruct lineage or calculate a competing report. Missing participation support, projection failure or cancellation fails the entire pack without independent/raw fallback. The read transaction/context closes before business-header lookup, unchanged PDF rendering and the single success audit. Explicit schema16 compatibility composition retains independent delegated reads.

The reusable projection authority keeps Core ownership of the immutable provider-neutral scope/result, conservative influence-envelope relevance, Active/Reversed contribution rules and OUT-positive/IN-negative aggregation. Data supplies `SqliteOperationalMovementProjectionAuthority`, which reads root/line/generation/link/movement/import facts, validates and projects relevant roots, unions legitimate Adjustment/ExcelImport facts and completes aggregation inside one SQLite read transaction. Unknown relevance widens rather than suppresses validation; a provably disjoint corrupt root may be excluded from a narrow query, while relevant Invalid/incomplete lineage or unexpected unrooted Manual/Batch evidence fails closed. BT-20-P2 registers this authority for every normal prepared consumer; `EffectiveMovementQuery` remains only in explicit schema16 compatibility branches.

Import comparison and execution consume the normal projection authority. Replacement comparison obtains corrected position through the day before cutover from one `PositionAsOf` query, then removes the previous ImportRun's persisted pre-cutover effect by authoritative ImportRun ownership. Execution uses the same subtraction exactly once, but its projection participates in the caller-owned serializable write transaction so eligibility, baseline, prior-run deletion, replacement state, new ImportRun/movements/audit and commit share one transactionally safe state. Ordinary new-import execution uses `PositionAsOf(DateOnly.MaxValue)`, equivalent to the accepted whole-ledger baseline across every representable movement date. Projection failure/cancellation propagates without raw fallback or surviving import writes; `DateOnly.MinValue` has an empty pre-cutover domain. Explicit schema16 compatibility composition retains the legacy raw path.

The schema17 foundation includes a validation-gated resolver for one requested CURRENT logical root. SQLite materialization/transaction mechanics remain in Data; invariant validation and the immutable result are provider-neutral. `CurrentGenerationNumber` selects the sole current generation, exact permanent/current membership is required, and validated success objects have no public construction/mutation surface. Ordinary proof covers RootOriginal plus current effective/terminal ledger links and their introductions, not unrelated superseded historical links; broader history remains migration/diagnostic authority. `RootMovementBatchId` is exposed only after exact original movement membership in that physical batch is proven (or null original membership for a single root). The resolver remains an internal Data component rather than a public registration and is consumed by activated writers/validators.

Schema-16 -> 17 migration-publication postflight remains a strict proof that the migration transaction created only the frozen generation-zero `MigrationBaseline` shape. Re-entry against an already-schema17 database instead performs separate structural/current-health validation, reusing the same provider-neutral current-root invariant validator, so legitimate lineage-native `Initial` roots and later native generations are not reclassified as migration corruption. The normal coordinator enforces this lifecycle split before host readiness.

Operational lineage health is separate from audit health. New operation/audit/review state commits atomically. The unified schema-17 mutation writer uses the IMP-06 caller-transaction appender for exactly one primary AuditEvent without changing the existing independent AuditService path. Native operation/audit health is validated for the affected logical root, so corruption in root A blocks A without invalidating a healthy root B; global structural/current lineage validation remains separate. Later external audit corruption does not falsify proven mathematics but blocks affected mutation/review and evidence-completeness output with critical health.

IMP-07's initial-lineage writer sits behind the existing Single/Batch `MovementService`, which remains the authority for authentication, authorization, validation, master-data checks, idempotency, physical movement/batch creation, audit and returned results. Normal activated composition supplies its SQLite implementation, typed receipt store and transaction-participating projection; explicit schema16 compatibility composition supplies dormant stores. For a newly committed eligible Single/Batch entry, the lineage writer creates one generation-zero `Initial` logical root atomically, with one permanent line per original movement, ordinal zero for Single and first-successful request enumeration order for Batch. It completes RootOriginal ownership and introduction links, validates the graph, then activates generation0. Single then projects PositionAsOf the captured business date, persists one typed receipt and audit with the same signed result, and commits. Exact native retry reads the receipt; migrated/pre-receipt retry returns the controlled unavailable outcome. SQLite mechanics stay in Data; Core owns typed invariants and Services owns the client-neutral workflow. The production DbContext still does not map lineage entities, and no PostgreSQL/API/client work is implied.

WinForms supplies stable IDs, expected generation and intent only. Client-neutral services own planning, authorization, projection, concurrency, persistence, audit and balances.

The IMP-05 boundary materializes one non-forgeable trusted planning snapshot through an infrastructure-internal SQLite materializer from the validated CURRENT root plus exact current movement and master-data facts under one provider-consistent read transaction. It cross-proves current Reversed pairs and authoritative business-date limits. A pure provider-neutral planner then produces NoOp or a complete-generation semantic plan with exact pointer/action/mask semantics. The planner itself remains write-free; normal native mutation now consumes it without moving SQLite mechanics or business rules across layers.

The unified schema17 mutation path composes that planner with atomic Correct/Reverse/Restore persistence, root-wide expected-generation CAS, canonical idempotency/replay, optional truthful physical output and one operation-linked primary audit. Provider-owned lookups resolve physical selection anchors to the stable root; Services maps the trusted snapshot to immutable preview DTOs; execution consumes the preview generation without refresh. Correct+WholeRoot rejects any Reversed current line until the later explicit-decision UI. Normal native17 composition rejects `ReverseAsync`, `CorrectAsync` and `CorrectBatchAsync` before alpha.8 writes; explicit schema16 compatibility composition retains them. Persisted generation-line identity comes only from `LogicalMovementGenerationLines.Id`; provider conflicts are classified after rollback/fresh inspection. Normal composition registers `SqliteMovementMutationWriter`; Restore/RemainReversed UI remains pending.

### Migration and protected layer audit

Migration uses persisted IDs/FKs/correction/reversal relationships only, creates truthful MigrationBaseline rather than fabricated history, and classifies Initializing/Active/ReadOnly/Invalid. A schema-16 correction-operation Kind outside historical values 0/1 is a database-wide migration blocker: later schema-17 values 2/3 must never reinterpret corrupt legacy evidence as Reverse/Restore. Schema capability does not imply baseline population: historical physical outputs remain evidenced by legacy ReplacementBatchId/membership and receive no output-link row, while every new lineage-native truthful output is linked. Baseline ledger-link introduction pointers identify introduction into the lineage model, not historical movement creation. ImportRun remains separate. Every schema16-to17 migration requires read-only preflight and verified recoverable provider-consistent backup before its schema mutation; native17 startup follows R4/R5 without synthetic schema16 prerequisites.

After lineage services and WinForms integration are accepted, a protected whole-codebase presentation/application/domain/infrastructure audit must finish before subsequent major pre-v1 work. API/PostgreSQL deployment, portal/handheld clients and WinUI evaluation remain post-v1.


## Task20 activation decisions — user-approved 12 September 2026

The following R1–R4 wording is frozen exactly as approved. BT-20-P2 activates these requirements in normal composition at source/automated evidence level without claiming retained-data, Windows/operator or global Task20 acceptance. R1 explicitly refines prior broad Single retry wording for pre-receipt commands; no historical result is fabricated.

R1 authority clarification: validated operational projection supplies the authoritative resulting position for the committed snapshot; BOTH typed response receipt and audit record that position. The sequence below is transaction ordering, not an authority transfer from receipt to audit. Receipt is command-response evidence only, never current-balance, projection, lineage-state or audit-source authority; audit prose/JSON is never balance or result authority.

APPROVED TASK-20 DECISIONS — FREEZE EXACTLY

DECISION R1 — SINGLE ENTRY RESPONSE RECEIPT

Freeze these semantics:

* New activated-schema17 Single Entries persist an immutable typed Single-entry command-response receipt atomically with first success.
* Receipt records the exact successful response semantics required to replay the original command result, including:

  * movement/command identity;
  * captured authoritative business date used for the response;
  * signed resulting position returned/audited at that committed snapshot.
* Receipt is command-response evidence only.
* Receipt is NEVER current-balance authority, lineage-state authority or report authority.
* Current position remains exclusively from validated operational projection.
* First success must:
  physical movement
  -> Initial lineage publication inside caller transaction
  -> validated in-transaction PositionAsOf(captured business today)
  -> typed receipt
  -> audit using that SAME resulting position
  -> commit
  -> return.
* Projection/cancellation/overflow/audit/receipt failure rolls back the entire Single Entry attempt.
* No postcommit independent balance query.
* No raw fallback.
* Do NOT create a generic operation-result framework.
* Do NOT create ResultValuesJson or parse formatted audit text as result authority.
* Do NOT backfill invented receipts for migrated legacy entries.
* A retry of a migrated/pre-receipt Single command must:

  * verify it is the identical persisted command;
  * create no duplicate;
  * not rewrite lineage;
  * but fail with a specific controlled "legacy replay result unavailable" outcome if the exact original response cannot truthfully be recovered.
* This is the explicitly approved compatibility exception to exact prior-result replay for pre-receipt legacy Single entries.

DECISION R2 — LOGICAL MUTATION PREVIEW / COMMAND BRIDGE

Freeze these semantics:

* Activated schema17 correction/reversal execution must use the existing logical lineage mutation authority only.
* Existing alpha.8 legacy mutation writes must be unreachable/fail closed under activated schema17.
* Add the minimum client-neutral logical mutation preview/intent bridge needed to supply:

  * stable logical root ID;
  * logical line ID(s);
  * ExpectedGenerationNumber captured at preview;
  * current effective/reversed state needed by the existing workflow;
  * explicit user intent.
* Execution must use that preview generation.
* Do NOT silently fetch the latest generation at execution to replace a missing expected generation.
* Stale preview must lose through root CAS/revalidation.
* Whole-batch correction:

  * remains available when every current line is Active and the accepted date/direction correction can be represented truthfully;
  * if any current line is Reversed, the existing workflow must block with a controlled outcome until the separately sequenced Restore/RemainReversed UI can collect explicit decisions;
  * no implicit Restore;
  * no implicit RemainReversed;
  * no removal of the current safety guard merely to keep the button enabled.
* Restore/RemainReversed visual workflow remains a later separately sequenced milestone.
* Schema16 compatibility paths may remain for tests/legacy composition but must reject active17 writes.

DECISION R3 — MINIMUM NATIVE AUDIT/REVIEW SAFETY

Freeze these semantics:

* Task20 does NOT pull the whole later Audit/History-detail milestone forward.
* Before native schema17 mutations can be exposed:

  * audit/detail routing must inspect authoritative EntityType / persisted operation association;
  * LogicalMovementBatch IDs must never be interpreted as physical BinMovement IDs;
  * native correction/reversal audit payloads must never be passed through alpha.8 detail parsing merely because action text matches;
  * acknowledgement/review must validate affected native-root audit health before mutating review state.
* If complete native detail UI is not yet implemented, return/display a controlled unsupported/native-detail outcome or disable that detail action.
* Never display unrelated/ambiguous evidence.
* Full native audit/history UX remains the next separate milestone after cutover.

DECISION R4 — ONE STARTUP FUNNEL

Freeze these semantics:

One Data-owned coordinator owns ALL startup database entry paths.

NO DATABASE:

* acquire a Data-owned path/bootstrap reservation before creation;
* only one process may create the configured DB;
* build the existing supported baseline through schema16;
* once physical identity exists, transition into governed schema16->17 activation.

EXISTING SCHEMA <16:

* acquire exclusive maintenance ownership;
* run existing supported numbered migrations to schema16;
* then continue, still under controlled startup ownership, into governed 16->17 activation.

EXISTING SCHEMA16:

* exclusive upgrade lease;
* exact/partial-state classification;
* migration preflight;
* verified non-overwriting backup;
* exact source identity/equivalence verification;
* schema17 migration/backfill;
* strict migration-publication postflight;
* publish schema17;
* native structural/current-health validation;
* transition/reacquire runtime participation with exact identity/schema/health revalidation;
* only then compose activated host.

EXISTING SCHEMA17:

* no schema16 preflight;
* no synthetic migration prerequisites;
* no required retained pre-lineage backup for normal startup;
* exact schema/capability validation;
* native current-lineage/projection health validation;
* activated host only after success.

SCHEMA >17:

* fail before mutation/EnsureCreated/application runtime.

DEVELOPER LOAD/FRESH:

* may not replace/delete the active DB outside coordination;
* must execute under the same Data-owned maintenance/startup protocol;
* after replacement/fresh creation, discard all assumptions about the previous physical identity and restart classification;
* no NoConflictProbe or fake conflict bypass.

The bootstrap/path reservation is only for creation-before-physical-identity. It does not replace the existing physical-file identity lease.
