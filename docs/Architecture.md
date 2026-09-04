# BinTracker Architecture

Current baseline: **v0.5.0-alpha.8.7**

## Display and DPI boundary

The required frequently-used laptop acceptance configuration is Windows 11 at **1920x1080 with 150% Windows scaling**. Every ordinary workflow and modal must remain accessible: action bands stay visible, content fits or scrolls within the working area, text does not clip, controls do not overlap, report grids remain usable, and normal WinForms DPI scaling is preserved.

The primary production environment uses a substantially larger display. The laptop configuration is an acceptance floor for usability, not a direction to globally reduce information density or optimise every screen specifically for a 14-inch panel. Layout concepts should remain presentation-neutral where practical so a future WinUI 3 client can replace WinForms presentation without moving business rules into UI controls; no WinUI dependency is introduced in v1.

Detail/investigation windows use available working-area space for compact identifiers and values, wrapping only genuine prose such as notes/reasons. Main report grids preserve single-line structured fields and selectively wrap long semantic Status/Notes cells with auto-height so the primary workspace does not become unnecessarily wide.

## Permanent target and hard gate

BinTracker's post-v1 target architecture is **Windows desktop / remote clients → authenticated BinTracker API/service → central PostgreSQL**. Clients never receive database credentials or connect directly. v1 remains the local SQLite WinForms product; its mandatory groundwork is client-neutral services, provider-neutral semantics, concurrency/idempotency correctness and replaceable presentation, not API/PostgreSQL deployment.

All production business code must assume multiple authenticated remote users can execute the same operation concurrently. Core, Services, UI contracts and the shared EF model remain provider-neutral; provider migrations and local database tooling stay in `BinTracker.Data`.

## Host composition and request context

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
- Single Entry, Batch Entry, reversal and import persist client operation IDs. A retry returns the existing result only when the canonical payload identity matches; reuse with a different payload is rejected.
- Reversal and import uniqueness constraints are authoritative under races.
- Correction extends the same invariant: the unique neutraliser FK (`ReversesMovementId`) arbitrates Reverse-vs-Reverse, Reverse-vs-Correct and Correct-vs-Correct; correction-operation identity/fingerprint makes identical retries return persisted lineage and rejects changed payload reuse.
- The alpha.8 correction transaction writes neutralisers, replacements, operation/line evidence, consumed links and audit. Its physical-`MovementBatchId` whole-batch guard remains safe until the frozen logical-root model below replaces it; do not remove the guard independently.
- The alpha.8 effective query suppresses correction-consumed originals/neutralisers while retaining immutable Movement History. The frozen model below replaces that technique with validated current-generation activity without weakening accepted results.
- Administrator acknowledgement is a review record, not an effectiveness gate: Operator corrections/reversals remain operationally effective immediately. Review authorization and duplicate prevention remain service/database concerns even when the Audit Trail disables ineligible UI actions.
- The outstanding-review count/state and navigation action must be exposed through a presentation-independent service/state/navigation contract. Current WinForms may render that contract with a reusable infobar-style `UserControl` or panel; a future WinUI 3 client replaces only that presentation with native `InfoBar`, retaining the underlying contract. Review business logic must not live in a disposable UI control.
- Audit detail navigation is keyed by authoritative persisted entity identity. MovementBatch detail is never inferred from description text; future contextual routes may use authoritative ImportRun or correction-lineage identity when available.
- Authentication counters and account/credential mutations use conditional or atomic database updates.

Moving to PostgreSQL still requires an API host, provider/schema migrations, authentication/authorization deployment, central backup/monitoring and real PostgreSQL integration tests. It must not require rewriting accepted business, reversal, import, report or audit semantics.

## Frozen movement-lineage architecture (dormant entry and unified-mutation execution implemented; runtime planned v1)

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

The dormant schema-17 foundation includes a validation-gated resolver for one requested CURRENT logical root. SQLite materialization/transaction mechanics remain in Data; invariant validation and the immutable result are provider-neutral. `CurrentGenerationNumber` selects the sole current generation, exact permanent/current membership is required, and validated success objects have no public construction/mutation surface. Ordinary proof covers RootOriginal plus current effective/terminal ledger links and their introductions, not unrelated superseded historical links; broader history remains migration/diagnostic authority. `RootMovementBatchId` is exposed only after exact original movement membership in that physical batch is proven (or null original membership for a single root). Ordinary resolution does not scan full history. The resolver is unregistered and unused by normal schema-16 startup and application consumers.

Schema-16 -> 17 migration-publication postflight remains a strict proof that the migration transaction created only the frozen generation-zero `MigrationBaseline` shape. Re-entry against an already-schema-17 database instead performs separate structural/current-health validation, reusing the same provider-neutral current-root invariant validator, so legitimate lineage-native `Initial` roots and later native generations are not reclassified as migration corruption. This lifecycle split is dormant and does not register or activate schema 17.

Operational lineage health is separate from audit health. New operation/audit/review state commits atomically. The unified schema-17 mutation writer uses the IMP-06 caller-transaction appender for exactly one primary AuditEvent without changing the existing independent AuditService path. Native operation/audit health is validated for the affected logical root, so corruption in root A blocks A without invalidating a healthy root B; global structural/current lineage validation remains separate. Later external audit corruption does not falsify proven mathematics but blocks affected mutation/review and evidence-completeness output with critical health.

IMP-07 adds an explicit isolated initial-lineage writer behind the existing Single/Batch `MovementService`, which remains the sole authority for authentication, authorization, validation, master-data checks, idempotency, physical movement/batch creation, audit and returned results. Normal composition supplies a dormant no-op writer that performs no schema probe, query or lineage write; only isolated schema-17 composition supplies the SQLite implementation. For a newly committed eligible Single/Batch entry, that implementation creates one generation-zero `Initial` logical root atomically, with one permanent line per original movement, zero for the Single ordinal and first-successful request enumeration order for Batch ordinals. It completes RootOriginal ownership and introduction links, validates the temporary provider-neutral graph, then activates the root at generation 0. It creates no generation-zero correction operation or physical-output link and does not change existing entry audit actions. Equivalent reordered Batch retries preserve the committed root and ordinals, and migrated `MigrationBaseline` roots remain unchanged. SQLite transaction/schema mechanics stay in Data; Core owns provider-neutral construction invariants and Services owns the client-neutral workflow. This reviewed source and targeted evidence do not activate schema 17, register lineage in the production DbContext/startup catalogue, or implement any PostgreSQL/API/client work.

WinForms supplies stable IDs, expected generation and intent only. Client-neutral services own planning, authorization, projection, concurrency, persistence, audit and balances.

The corrected dormant IMP-05 boundary materializes one non-forgeable trusted planning snapshot through an infrastructure-internal SQLite materializer from the validated CURRENT root plus exact current movement and master-data facts under one provider-consistent read transaction. IMP-05C additionally loads the persisted reversal relationship and cross-proves every current Reversed pair: TerminalReversal reverses that exact LastEffective movement, is opposite in direction with equal customer/container/quantity, has Manual provenance with no ImportRun or physical batch membership, and both current dates are through the separate authoritative business date; an Active current effective date must also be through that date. Ledger role labels cannot override contradictory immutable movement facts. A pure provider-neutral planner receives the authoritative business date separately from canonical client intent and produces either NoOp with no artifacts or a controlled immutable complete-generation semantic plan. Whole-root correction requires an explicit Restore or RemainReversed decision for every and only currently Reversed line; restoration overrides are line-specific. Every planned result carries either the exact existing effective/terminal pointer identities or typed plan-local references to its new effective/terminal movement specifications, so persistence does not reconstruct semantics. It performs no writes, persistence success claims, audit, CAS, idempotency execution or runtime registration; schema 17 and normal schema-16 authority remain unchanged. Independent external source/diff review approved this IMP-05/05B/05C dormant planning boundary.

The unified schema-17 mutation slice now composes that planner with atomic Correct/Reverse/Restore persistence, root-wide expected-generation CAS, canonical operation idempotency/replay, optional truthful physical output and one operation-linked primary audit. Persisted generation-line identity comes only from `LogicalMovementGenerationLines.Id`; no logical-line, movement, ordinal or other identity is substituted. SQLite constraint/CAS conflicts are reclassified only after rollback and fresh committed-state inspection, while unresolved SQLite busy/locked exhaustion follows the stable persistence-failure path and cannot become stale-generation or lineage-integrity diagnosis. The primary audit records trusted current lineage and business state before the change and the resulting generation, per-line action/state/mask/pointers/business state and relevant new/output movement identities after it. This execution remains available only through explicit isolated schema-17 composition; normal composition registers `DormantMovementMutationWriter`, not `SqliteMovementMutationWriter`. Schema activation, projection/report cutover and Restore UI are not implemented by this slice.

### Migration and protected layer audit

Migration uses persisted IDs/FKs/correction/reversal relationships only, creates truthful MigrationBaseline rather than fabricated history, and classifies Initializing/Active/ReadOnly/Invalid. A schema-16 correction-operation Kind outside historical values 0/1 is a database-wide migration blocker: later schema-17 values 2/3 must never reinterpret corrupt legacy evidence as Reverse/Restore. Schema capability does not imply baseline population: historical physical outputs remain evidenced by legacy ReplacementBatchId/membership and receive no output-link row, while every new lineage-native truthful output is linked. Baseline ledger-link introduction pointers identify introduction into the lineage model, not historical movement creation. ImportRun remains separate. Every database requires read-only preflight and verified recoverable provider-consistent backup before mutation.

After lineage services and WinForms integration are accepted, a protected whole-codebase presentation/application/domain/infrastructure audit must finish before subsequent major pre-v1 work. API/PostgreSQL deployment, portal/handheld clients and WinUI evaluation remain post-v1.
