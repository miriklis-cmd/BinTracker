# BinTracker Automated Testing

## Characterization-before-change

Task20C's temporary ordinary intentional-red boundary is historical and fully closed. No activation test is skipped, filtered from normal discovery or permitted to remain red. BT-20-P2 changes normal runtime authority only after pre-edit schema16 characterization passed; explicit schema16 compatibility fixtures preserve the old numbered-migration and legacy-service behavior where still required.

Full suites and `Build-BinTracker.bat` remain blocking. Red evidence is never passing coverage or operator acceptance. Task19 Print Pack semantics remain frozen; R7 changes remain limited to the four demonstrated result-affecting gaps.

Before changing accepted behaviour or placing new authority beside it, identify existing tests that precisely characterize the affected path. Where coverage is inadequate, add characterization coverage and run it before the change, then rerun it afterward. If the existing behaviour is intentionally defective, characterize it where useful and add a separate expected-behaviour regression test; do not preserve a known defect merely because it was characterized.

## BT-20-P2 atomic normal schema17/runtime activation — 16 September 2026

Before the first activation production edit, 175 normal schema16 startup, Single/Batch, correction/reversal, report/balance/customer/import and coordinator integration cases passed with zero skips; both developer-database configuration unit cases also passed. The old behavior remains covered through explicit schema16 compatibility fixtures rather than being silently rewritten.

`Task20RuntimeActivationTests` use production `AddBinTrackerData` plus business-service composition and the real coordinator. Eight ordinary cases prove: fresh normal activation; schema16→17 migration; catalogue17 with a separate catalogue16 compatibility boundary; native initial-lineage/receipt/mutation/projection registrations; session-held runtime lease; native Single/Batch generation zero; immutable Single retry; legacy mutation rejection; logical correction and reversal using captured generations; projection-backed balance/report/customer/Dashboard results; fail-closed unrooted evidence without raw fallback; existing17 revalidation without migration replay/new backup; future-schema rejection without mutation; and coordinated developer Load/Fresh marker completion.

The first complete UnitTests run exposed one stale catalogue16 assertion and no production failure; it was retargeted to normal catalogue17, with compatibility16 separately asserted in integration. The first complete IntegrationTests run passed483 and failed eight `SqliteMigrationTests` because those explicit compatibility fixtures compared their persisted16 result with normal latest17. Their assertions now name `LatestSchema16CompatibilityVersion`; the focused class passes15/15 and the rerun passes491/491. Final Release evidence is build0 warnings/errors, UnitTests279/279 and IntegrationTests491/491, all with zero skips. Canonical `Build-BinTracker.bat` passes mechanical audit, restore and Debug build with0 warnings/errors plus the same279/279 and491/491 suites (770 automated tests total),0 failed/skipped. No retained production database, Batch #30, native detail UI, Restore UI or Windows/operator acceptance is part of this evidence.

## BT-20-P3 native Audit/History detail — 16 September 2026

The existing native no-detail characterization was changed before production and failed 1/1 as expected: healthy schema17 correction evidence returned `null`. The completed path retains the R3 persisted-association classifier and affected-root operation-audit health validator, then has Data project the authoritative operation/root/generation, permanent line state/action/mask and pointer evidence, source/physical movement evidence and transformation provenance. Before exposing historical prior/transformation evidence, the reader proves the predecessor's root/permanent-line ownership and exact preceding generation, and proves each operation ledger link's permanent-line owner equals its introducing generation line. Services maps that provider-neutral result to Audit detail and reads it before a native review mutation; WinForms only displays it. Schema16/migrated legacy operations remain on the accepted legacy detail path, while partial/mixed or cross-line native evidence fails before detail/review mutation and has no fallback.

Focused Release evidence: the two cross-line adversarial cases first reproduce 2/2 red (referentially valid cross-line predecessor and ledger-introduction pointers both returned plausible detail), then `Task20Audit` passes 12/12 with zero skips. This includes correction, reversal, repeated-generation selected-line chronology, physical/logical collision, malformed native envelope, cross-line detail/review rollback and migrated-legacy controls. The adjacent legacy correction/detail plus Task20 audit filter passes 52/52, and adjacent Task20 logical mutation filter passes 12/12, all zero failed/skipped. Windows/DPI, retained-database and canonical BAT evidence remain pending.

## BT-20-FIX1 native evidence correction — 16 September 2026

Independent review found that P1's first classifier treated `RequestSchemaVersion=NULL` as sufficient legacy evidence even when root, expected/result generations and the generation-to-operation association still proved native shape. Before the production correction, two new adversarial tests created real native operations, nulled that discriminator and made their audits legacy-looking: detail incorrectly returned reconstructed alpha.8 lines, and review committed an acknowledgement audit. Both failed for the suspected downgrade reason. No existing assertion was changed.

The corrected Data classifier reads RequestJson, RequestSchemaVersion, LogicalMovementBatchId, ExpectedGenerationNumber, ResultGenerationNumber and the generation association together. Any native indicator requires a complete schema1 envelope, exact successor generation, one generation association and authoritative logical-root audit identity before the existing affected-root health validator runs. A partial or contradictory shape throws the existing controlled audit-health failure before parsing or review mutation. A positive test creates a real schema16 correction, migrates it to schema17, proves its structurally associated operation has all native envelope fields null and no generation association, then proves legacy detail and Administrator review still work.

Focused Release evidence: the two adversarial cases reproduced2/2 red before production editing and passed2/2 afterward; all seven R3 activation cases passed; the complete Task20 activation filter passed40/40; and the combined audit/history/review, native mutation, migration/legacy, R1/R2, EF-membership and R7 filter passed204/204. Release build passed with0 warnings/errors, UnitTests279/279 and full IntegrationTests483/483 passed, all with0 failed/skipped. The canonical `Build-BinTracker.bat` passed mechanical audit/restore/Debug build with0 warnings/errors and the same279/279 UnitTests plus483/483 IntegrationTests,0 failed/skipped. Final post-BAT audit and diff check pass. No permanent requirement status changed. Normal schema17 activation, native detail UI, Restore UI, retained-data work and operator acceptance remain pending.

## Historical BT-20-P1 remaining pre-activation blockers — 15 September 2026

Pre-edit characterization ran the complete 37-case Task20 activation filter: 28 passed and exactly the four R3, one EF-membership and four R7 cases listed in the historical Task20F section below failed for their approved reasons. R1 and every R2 control remained green; no additional Task20 red existed. No expected-behavior assertion was weakened, skipped or removed.

P1 routed native detail/review through persisted `AuditEvent.MovementCorrectionOperationId`, the associated logical root and the existing affected-root operation/audit health validator inside the caller transaction. Its original focused filter passed15/15 and activation filter passed37/37, but independent review later exposed the mixed-shape discriminator downgrade now corrected by BT-20-FIX1 above. This historical evidence is not rewritten as proof that the gap never existed.

EF uses client no-action semantics for loaded movement-batch relationships, while Data owns the catalogue-specific persisted shapes: fresh/existing schema16 requires accepted `SET NULL` and schema17 requires `RESTRICT/NO ACTION`. A first full-suite attempt exposed the capability validator still deriving schema16's action from EF and produced 30 startup-coordinator failures; this was corrected to validate the explicit catalogue rule. The complete coordinator/startup/membership filter then passed83/83.

R7 reuses `ITransactionalOperationalMovementProjectionAuthority.ReadSnapshotAsync` and `QueryInTransactionAsync` for exactly four result boundaries: Import replacement evidence plus projection, Dashboard threshold plus projection, customer-search visible metadata plus position and customer-summary customer/container metadata plus position. The four old characterization assertions that deliberately expected hybrid defect values were converted to retain complete before/after scenario proof; the stronger activation tests remain unchanged and continue asserting the complete mid-operation result is coherent. The R7-adjacent suite passed79/79.

Adjacent R1/R2/schema16/native mutation passed162/162; audit/history/membership/migration passed89/89; UnitTests passed279/279; and the corrected full IntegrationTests passed480/480. All runs had zero skips. Final Release solution build passed with zero warnings/errors. Canonical `Build-BinTracker.bat` then passed mechanical audit/restore/Debug build with zero warnings/errors and 279/279 UnitTests plus 480/480 IntegrationTests, zero failed/skipped. Normal schema17 activation, retained-database/operator acceptance, Batch #30 mutation and Restore/RemainReversed UI are outside this pass. Counts overlap and must not be summed.

## Task20F FIX1 corrected R2 logical correction/reversal activation — 14 September 2026

Task20F implements only frozen R2 under explicit native17 composition. Before its production editing, `Task20MutationActivationTests` failed all four existing R2 cases for the intended gaps: native Reversed-line physical preview returned eligible, and legacy `correct`, `reverse` and `whole-batch` commands appended alpha.8 artifacts. The accepted alpha.8/native mutation characterization filter (`Task20LogicalMutationSafetyTests`, all `MovementCorrectionWorkflowTests`, `MovementCorrectionSqliteTests`, `MovementCorrectionConcurrencyTests` and `MovementMutationExecutionSchema17Tests`) passed 100/100 with zero skips. Independent review then found that direct `ExecuteLogicalAsync` could bypass preview eligibility by embedding valid Restore/RemainReversed decisions. Before the R1 production correction, the new two-case real-service regression failed2/2 because both commands committed; no source-string or timing assertion was used.

The focused R2 class now proves schema16 legacy compatibility/dormant preview; stable root and permanent-line resolution from movement, root-original/native-output batch and root anchors; validated current values/state; preview-captured expected generation; complete correction/reversal generations and physical roles; current-pointer advance once; exact committed-result replay; changed-payload reuse conflict; deterministic stale-preview rejection with byte-for-byte unchanged persisted state; Active-only whole-root preview and direct execution; typed rejection of explicit Restore and RemainReversed bypass attempts with unchanged current pointer, movements, generations, operations, audits, physical outputs, legacy correction lines and complete database state; and typed rejection of all three legacy write routes before artifacts. The existing planner/writer remains the authority for complete generations, Restore/RemainReversed, idempotency, physical output and CAS; the preserved mixed-decision integration fixture directly proves the lower planner still represents its frozen result before proving the activation boundary rejects execution. No duplicate calculator or persistence path was added.

The retained future filter now discovers 37 ordinary unskipped cases: 28 pass and exactly nine remain intentional red. All original four R2 cases are green, along with the five new R2 facts including both direct-decision bypass cases; no R3, EF-membership or R7 case became green unexpectedly. Remaining failures are:

- R3 audit/review: `Native_operation_association_prevents_alpha8_detail_parsing_even_when_payload_matches_legacy_shape`; `Native_review_rejects_unhealthy_operation_audit_evidence_without_writing_review` for `missing-association` and `corrupt-operation`; `Native_reversal_root_id_collision_never_returns_another_roots_physical_evidence`.
- EF membership: `Loaded_schema17_batch_delete_does_not_nullify_tracked_immutable_member_identity`.
- R7 snapshots: `Replacement_comparison_previous_run_evidence_and_projection_share_one_snapshot`; `Customer_summary_visible_container_and_position_share_one_snapshot`; `Dashboard_attention_and_position_share_one_snapshot`; `Customer_search_visible_active_row_and_position_share_one_snapshot`.

These nine failures remain ordinary discovered tests and are not waived or filtered from the project. The canonical BAT/full-release gate remains suspended until the intentional-red boundary closes. Task20F FIX1 changes only the R2 activation-policy defect and necessary tests/docs; it includes no Task20E/R1 receipt change, R3/R7/EF implementation, normal WinForms/schema17 activation, product package, retained-database rehearsal or operator acceptance. Its external evidence retains the historical filename `BT-CODEX-20260914-20F-R1`; new run identifiers never reuse that ambiguous suffix. TEST REQUIRED: Targeted independent source/diff review, followed later by the complete canonical gate after remaining intentional reds close.

Task20F FIX1 validation: affected Services build and Release solution build pass with zero warnings/errors; focused bypass2/2, complete R2 9/9, accepted correction/reversal/native mutation100/100, schema16 correction/reversal compatibility54/54 and UnitTests279/279 pass with zero skips. Full IntegrationTests runs480 ordinary tests:471 pass and only the exact9 intentional futures above fail, with zero skips. The dedicated future filter independently reports28 pass/9 expected red across37. Mechanical audit passes with259 permanent IDs/27 Markdown files. Frozen R1–R4 equality, accepted Task20E/R1 production-file non-diff, normal schema16 dormancy checks and `git diff --check` pass. Counts overlap and must not be summed. BT-20F-FIX2 re-verifies these facts without production changes and records its final evidence in the current Documentation Audit entry.

## Historical Task20E R1 Single-entry activation — 14 September 2026

Task20E implements only frozen R1 under explicit schema17 composition. Before production editing, the existing Single/Task20 characterization and safety filter passed 24/24, the existing schema17 entry suite passed 18/18, and all seven original `Task20SingleActivationTests` cases failed for the frozen missing behavior: zero transaction projection calls, ignored projection/cancellation/overflow failures, wrapped integer overflow, recomputed retry result after later activity, and silent pre-receipt legacy replay. No fixture, compile, skip or unrelated failure was classified as expected red.

The deterministic persistence proof now covers one physical movement, one complete Initial root/link, one typed response receipt and one audit using the same in-transaction checked projected position; exact native retry returns the original receipt without another projection; migrated/pre-receipt retry returns `SingleMovementReplayUnavailableException` without duplicate, lineage rewrite or synthetic receipt. Separate failure cases prove full rollback for projection failure, cancellation, overflow, receipt failure after insert and audit SaveChanges failure. A postcommit interleaving proves no independent balance query is needed to construct the response. Schema16 characterization remains green and is not native17 authority.

Focused final results before the documentation-only reconciliation: combined R1 Single/legacy/characterization, schema17 entry and migration 108/108 PASS (R1/legacy/characterization35 plus entry/migration73); mutation execution 43/43 PASS; operational projection 57/57 PASS; Task20 coordinator/characterization/safety 83/83 PASS; existing multi-user idempotency 4/4 PASS; complete UnitTests 279/279 PASS. All reported runs had zero skips. The retained future filter runs 32 ordinary tests: all eleven current R1 Single tests and all eight Task20D startup tests pass, while these thirteen unrelated cases remain red for their intended missing production behavior:

- R2 mutation/preview: `Current_whole_batch_preview_blocks_a_native_reversed_line_until_explicit_decision_UI_exists`; `Enabled_schema17_rejects_legacy_commands_without_appending_alpha8_artifacts` for `correct`, `reverse` and `whole-batch`.
- R3 audit/review: `Native_operation_association_prevents_alpha8_detail_parsing_even_when_payload_matches_legacy_shape`; `Native_review_rejects_unhealthy_operation_audit_evidence_without_writing_review` for `missing-association` and `corrupt-operation`; `Native_reversal_root_id_collision_never_returns_another_roots_physical_evidence`.
- EF membership: `Loaded_schema17_batch_delete_does_not_nullify_tracked_immutable_member_identity`.
- R7 snapshots: `Replacement_comparison_previous_run_evidence_and_projection_share_one_snapshot`; `Customer_summary_visible_container_and_position_share_one_snapshot`; `Dashboard_attention_and_position_share_one_snapshot`; `Customer_search_visible_active_row_and_position_share_one_snapshot`.

Those thirteen failures are neither waived nor filtered from the project. Final Release solution build passes with0 warnings/errors; the mechanical audit passes with259 permanent IDs and27 Markdown files; frozen R1–R4 equality and `git diff --check` pass. The canonical BAT/full-release gate remains suspended until the intentional reds are implemented and green. Task20E includes no normal WinForms/schema17 activation, retained-database rehearsal, application package or Windows/operator acceptance. TEST REQUIRED: Targeted independent source/diff review, followed later by the complete canonical gate after the retained future boundary closes.

## Historical Task20D-R1 independent-review concurrency corrections — 14 September 2026

Independent review found two gaps in the original20D evidence: a Load/Fresh operation could return readiness for a later competing replacement after releasing publication ownership, and A13 only proved an entirely new retry call. These were defects, not accepted semantics.

The corrected A13 test holds the real creator at BootstrapReserved, observes the second call at BootstrapWaiting, releases the creator and awaits BOTH original calls. It asserts creator count1, same physical identity, native17 loser classification and one verified-backup manifest. Additional tests cancel the waiting call before creation without disturbing the owner, and prove a waiting call rejects the same zero-byte physical identity after interrupted creation rather than recreating it. Waiting uses one kernel mutex acquisition probe followed by a cancellation-aware WaitAny, with no polling or sleeps. Mutex acquisition/release stays on a dedicated worker to respect Windows thread affinity; the existing physical lease/pin remains database ownership authority after creation.

A15 deterministically pauses each Load/Fresh operation at ReplacementRuntimeTransition, after both publication leases are disposed. A second real coordinator Load publishes and validates a different identity with quantity11. The original call then fails IdentityChanged, never returns readiness, reports the actual new selected identity/schema17 and leaves the competitor's complete state unchanged. Separate Load/Fresh cancellation cases at that boundary retain truthful replacement publication and schema16/17 state. The expected identity is the NEW publication, not the discarded old target.

Focused before-correction A13/A15: 8/8 PASS characterized the weaker20D boundary. Corrected focused14/14 PASS; final Release integration354/354 and relevant unit68/68 PASS; coordinator/startup75/75 is included. Future28 remains8 GREEN/20 unchanged EXPECTED RED, zero skips/wrong reasons; Release solution build0 warnings/errors, audit/diff/frozen equality PASS. Exact filters and evidence are recorded in the R1 continuation and external handoff. These tests remain ordinary unskipped behavioral tests. No R1 receipt/R2/R3/R7/EF membership change or normal runtime activation is included.

## Task20D production foundation evidence — 13 September 2026 (historical; R1 supersedes concurrency proof)

The real Data coordinator now supplies A2/A3/A8–A11/A13–A15 lifecycle evidence under explicit isolated composition. Normal startup remains schema16/dormant; the coordinator does not install writers. The eight startup future tests use typed StartupDatabaseException outcomes; the existing normal-startup defect characterization is retained. No other future assertion was weakened or made green.

Final Release evidence: integration348/348 PASS, zero failed/skipped, comprising adjacent migration/lease/backup/source-identity/entry/mutation/projection and Task20 characterization/safety279, plus coordinator61 and startup activation8. The Task20 characterization/safety subset is23/23. Relevant unit developer-configuration/lineage-contract/current-root/planner tests68/68 PASS. Full retained future filter28: startup8 GREEN, Single7 RED, mutation4 RED, audit4 RED, EF membership1 RED, R7 snapshot4 RED; zero skipped or wrong-reason failures. All28 test names/outcomes and20 error messages mechanically equal the earlier same-session classification. These counts overlap; do not add the repeated eight startup cases as distinct coverage.

Release solution build passed with0 compiler/analyzer warnings and0 errors. Final TRX files under `%TEMP%/BT-CODEX-20260913-20D-results`: `20D-release-green-final.trx`, `20D-unit-final.trx`, `20D-release-future-final.trx`; build log `20D-solution-release.log`. Before production, migration/infrastructure/startup characterization82/82 passed and the eight startup futures failed at the intended rejection gaps. Earlier focused54/54, expanded69/69 and adjacent279/279 are historical incremental evidence superseded by the final Release run.

Behavioral lifecycle coverage includes native Initial/later generations without migration prerequisites; backup before first DDL; full persisted equivalence inside the migration write transaction; strict publication rollback; committed17 after postcommit/cancellation/validation/transition failures; final identity/schema/health reacquisition; retained runtime lease/pin; barrier-controlled competing bootstrap and interrupted bootstrap; pre16 progression; active-session replacement blocking; stable provider snapshot Load; staged invalid/future rejection; old-target preservation before swap and truthful new identity after swap. The original20D bootstrap test used actual OS ownership and a deterministic barrier, but only proved a separate caller retry after the losing call threw. Task20D-R1 above supersedes that incomplete A13 claim with same-call waiting/reclassification; adjacent infrastructure separately proves process lease exclusion. CHECK/index token quoting and boundaries are challenged with adversarial declarations, and base columns/types/nullability/PK/FK/uniqueness are inspected independently of row population.

Final review corrected unsafe post-swap-only validation of loaded native17, delimiter/token canonicalization, final source-verification placement, retained stage ownership, replacement publication reporting and overly broad health-fault translation. One development capability test initially dropped only one of two valid customer-code unique indexes; the final test removes both, preserving the requirement to accept equivalent uniqueness. All new/changed test bodies and production files were reviewed. No source-string/reflection-only test, broad exception acceptance, hidden skip or test-keyed production branch was introduced.

TEST REQUIRED: Targeted for independent Task20D review. No full-suite/BAT, retained-database rehearsal, Windows/operator acceptance, application package or runtime cutover is claimed. R1/R2/R3/R7, EF membership and later preview/receipt/detail contracts remain pending. The historical Task20C evidence below records its original all-red state and unavailable seams; coordinator unavailability is superseded only by this foundation.

## Historical Task20C evidence — 12 September 2026

### Corrective review pass — BT-CODEX-20260912-20C-R1, 13 September 2026

The seven independent-review corrections are documentation/tests only. R1 authority is validated operational projection -> authoritative committed-snapshot position -> BOTH response receipt and audit; neither receipt nor audit prose/JSON is balance/projection/lineage authority, and receipt is not audit-source authority. Universal retry wording now states the narrowly approved migrated/pre-receipt Single exception; Batch/logical mutation exact-result guarantees remain. BT-ARCH-013 stays IMPLEMENTED-STATIC for implemented command identity/duplicate prevention/conflict only; pending R1 receipt/replay behavior is mapped under PLANNED-V1 BT-CORR-026 and its decision record.

The migrated Single fixture now executes the actual schema16 `SaveSingleAsync` with dormant writers and the exact retry ClientOperationId. Before migration it asserts physical identity, customer/container/date/type/quantity/reference/null notes, Manual provenance, created identity/time and the service-produced successful MOVEMENT_RECORDED audit including payload, actor/session/device. Schema remains16 with no lineage/receipt tables. A separate green fixture test verifies migration retains the original audit and creates MigrationBaseline without native operations/physical outputs. The future retry test compares all persisted table values and structural declarations as well as artifact counts: no duplicate, new audit, lineage rewrite or invented receipt may survive. Audit is never used to derive a receipt. When receipt storage exists, strengthen the green fixture test with its actual per-command absence seam.

R7 compares complete result-affecting BEFORE and AFTER states. Dashboard includes returned/taken/outstanding/attention; customer search includes every visible row's ID/code/name/type/activity/position; summary includes customer identity and the complete ordered container/name/position list. Exclusion is accepted only by equality to the proven full AFTER result, never an early return on one missing row. Import success compares previous run ID/cutover/movement count, actual previous/proposed movement counts, changed-coordinate count and every coordinate's previous/proposed/difference quantities; its legitimate AFTER outcome is the established controlled stale previous-run rejection. File/actor metadata is not made an arithmetic requirement. Daily Print Pack is untouched.

For absent production error contracts, `Task20FailureBoundary` is a TRANSITIONAL behavioral/diagnostic assertion, not a permanent API: require failure, no misleading result/evidence and unchanged persisted state, then recognize the intended domain concepts without requiring a generic exception class or exact new message. Argument/programming, I/O/provider, cancellation, authorization and aggregate faults (including inner causes) cannot pass as the intended failure. Existing typed/frozen paths retain their exact failure enums or established diagnostics. Native detail permits controlled unsupported/integrity failure as well as existing no-detail output. These provisional semantic diagnostic checks MUST be replaced with exact typed result/error/code assertions when those production contracts are introduced; new contracts must not be designed to match these temporary strings. Arbitrary exceptions are unexpected failures.

Task20C originally used transitional DatabaseSetup-facing startup reds. Task20D retargets those eight cases to typed failures at the real Data coordinator and preserves the normal-runtime defect characterization. DatabaseSetup remains unchanged and is not a second activation authority. Final Task20 acceptance requires coordinator-level A2/A3/A8–A11/A13–A15 lifecycle tests plus the relevant structural/current-health/future/partial-state rejection paths. The tests remain ordinary, discovered and unskipped; the coordinator is production Data infrastructure used through explicit isolated composition.

Corrective evidence: baseline before edits 77/77 PASS; final baseline plus adjacent projection/partial-schema coverage 135/135 PASS; characterization/safety 23/23 PASS (the former22 plus the truthful legacy fixture proof); future classes 28/28 EXPECTED RED, no unexpected final failures or skips. R7 actual tuples: Dashboard BEFORE `(0,30,30,1)`, observed `(0,30,30,0)`, AFTER `(0,60,60,1)`; customer old active `PROJ-A`/`Projection A` row has observed11 instead of before7, AFTER exclusion; summary old Blue Bin row has observed11 instead of before7, AFTER complete list excludes Blue. Import previous effect11/proposed15/difference4 becomes mixed11/26/15 with run/count evidence unchanged (run1, counts2/2/2, one changed coordinate); AFTER is stale request. Build passes with zero compiler/analyzer warnings/errors. One development-only wrong exception-type assertion in the existing missing-decision safety test was corrected to its source-established diagnostic; it is not expected-red evidence. Exact TRX names/messages/commands are retained in the external handoff. The original20C run below is historical evidence; these corrective results supersede its test-quality assessment.

Existing entry/correction baseline: 77/77 passed. Final new characterization/safety: 22/22 passed (0 failed/skipped). Adjacent existing projection fixture plus partial-schema migration rejection: 58/58 passed (0 failed/skipped). Test-project Debug compilation and transitive Core/Data/Services compilation passed without compiler/analyzer warnings or errors; Release/full solution build and BAT were not run.

The 28 future tests failed at their intended behavioral assertions, with 0 unexpected failures in the final runs. These are **EXPECTED RED — missing approved Task20 production behavior**, not passing coverage:

| Explicit future class | Cases | Observed missing behavior |
|---|---:|---|
| `Task20SingleActivationTests` | 7 | Transaction projection calls 0 rather than 1; projection/cancellation/overflow injections ignored; integer overflow commits a wrapped result; retry returns 12 instead of original 7; migrated pre-receipt retry returns success rather than controlled unavailable outcome. |
| `Task20StartupActivationTests` | 8 | Existing initialization accepts missing unique index, wrong RESTRICT FK, weakened CHECK, Invalid root, missing current generation, unrooted ordinary evidence, future schema18 and partial schema16 shape. |
| `Task20MutationActivationTests` | 4 | Enabled17 still accepts legacy Correct/Reverse/whole-batch commands with no preview generation; physical preview reports a native reversed-line root eligible. |
| `Task20AuditActivationTests` | 4 | Native root ID routes to another root's physical evidence; legacy-shaped native payload enters alpha8 parser; missing association/corrupt operation does not block review. Healthy native Operator review remains a passing control. |
| `Task20MembershipActivationTests` | 1 | Removing a loaded batch nullifies the tracked member's physical batch ID. Separate loaded/unloaded persisted schema17 rollback controls pass. |
| `Task20SnapshotActivationTests` | 4 | Dashboard attention 0 instead of 1; customer/container visible position 11 instead of 7; Import comparison proposed net 26 instead of 15 after concurrent previous-run replacement. |

Run these classes only through explicit `FullyQualifiedName~<class>` filters during Task20C. Passing classes are `Task20SingleCharacterizationTests`, `Task20SingleSafetyTests`, `Task20StartupCharacterizationTests`, `Task20MembershipCharacterizationTests`, `Task20LogicalMutationSafetyTests`, `Task20AuditCharacterizationTests` and `Task20SnapshotCharacterizationTests`. The external handoff records exact individual results; CONTINUATION maps original A–F coverage and unavailable production seams. The passing cases include current defects and existing lower-level invariants, not implemented activation requirements.

Resolved development-only failures: incorrect fixture argument order, xUnit assertion analyzer guidance and a catch-variable name collision caused compile errors and were fixed in tests. An initial Import fixture lost its only projected position and did not demonstrate numeric mixing; tracing the calculation led to a stronger fixture with surviving ordinary activity. No production change or assertion weakening was used to claim the gap. Final failures are all intended red assertions. TEST REQUIRED: Targeted; coordinator/preview/receipt-specific proof gaps and later Windows/operator/retained-data/full-canonical acceptance remain outstanding.

## Structured-input fail-closed stress testing

Parsers, deserializers, importers, migration readers, recovery-manifest readers and structured persisted-input boundaries require adversarial malformed-input coverage appropriate to their actual format. Input must be fully accepted and validated or fail with a controlled/stable outcome and no partially accepted or persisted state. Relevant cases include truncation, missing/extra fields, duplicate keys or records, wrong types, unsupported enum values, invalid IDs/FKs/dates, numeric overflow/extremes, empty/null/whitespace edges, malformed JSON or worksheet/database graphs, inconsistent manifest/checksum metadata, and cancellation/failure during an operation. NaN/Infinity and other format-specific cases apply only where the boundary actually accepts those representations.

For schema 16 -> 17, the structured input is the persisted legacy database graph. Migration coverage therefore includes malformed correction/reversal ownership, cross-domain ImportRun relationships, invalid operation kinds/schema state, partial lineage artifacts, prerequisite tampering and transaction-stage failure injection. An undefined persisted enum value in the source schema is hostile/invalid input: it must not acquire meaning merely because a later schema allocates the same number. Every case must classify deterministically or fail closed without a partially committed schema 17.

## Logical-lineage implementation and acceptance gate

BT-CORR-018..033 is documentation-frozen. Dormant Core contracts, migration-safety infrastructure, schema-17 migration/postflight and a validation-gated CURRENT-root resolver exist under isolated tests. Resolver coverage exercises non-forgeable success construction, current membership/pointers, required current ledger ownership/roles/introductions, exact root-batch or null-single original membership, preserved ReadOnly reason, unique non-negative ordinals, nonprojectable statuses and undefined relevant enums. An unrelated historical-only link does not determine ordinary current projectability, while migration postflight remains globally strict. Ordinary resolution selects only `CurrentGenerationNumber` under one consistent read boundary; it does not scan full history. Normal startup remains schema 16 and production lineage authority is not activated or accepted.

Corrected dormant IMP-05 automated coverage exercises authoritative fact materialization, an infrastructure-internal materializer and non-forgeable trusted snapshots/plans, complete existing/plan-local result pointer shapes, explicit whole-root decisions for every Reversed line, mixed Corrected/Restored/RemainReversed results, no-op, every later-generation action, absent/clear/value normalization, exact AppliedFieldMask selection including selected-equal and override-free Restore, generic ImportRun/ExcelImport/Adjustment exclusion, separate authoritative business-date input, master-data activity, physical-output eligibility and predicate failures. IMP-05C adversarial unit and disposable-schema integration coverage corrupts each current reversal-pair fact independently: wrong/null `ReversesMovementId`, same direction, customer/container/quantity mismatch, non-Manual source, physical-batch or ImportRun membership, and future terminal/LastEffective/Active-effective dates. Positive RemainReversed, standalone Restore, whole-root Restore and mixed Corrected+Restored cases remain. Integration tests prove the materializer uses the validated current root and exact current persisted pair under one SQLite read transaction, ignores unrelated historical-only rows and performs no writes. The original IMP-05 pre-edit execution ordering was missed; IMP-05B recovery remains recorded truthfully, while IMP-05C ran the 37-test alpha.8 characterization before correction editing. Independent review approved the IMP-05 boundary.

IMP-06 integration coverage proves the persistence-boundary audit appender requires an active caller-owned transaction, leaves one new AuditEvent Added and unsaved, persists exactly once when the caller saves/commits, rolls back together with legitimate sibling state saved in the same transaction and rejects a missing transaction without tracking. A separate regression proves existing independent `AuditService.WriteAsync` still creates and saves its own event. Dormant schema-17 coverage proves unique structured legacy association (`Legacy_audit_mapping_requires_one_unique_complete_structured_match`), required audit column/index presence (`Schema_17_contains_required_tables_indexes_and_restrict_membership_fk`) and the RESTRICT operation FK plus duplicate-primary-audit rejection (`Audit_operation_association_is_restrict_foreign_key_and_unique`). The unified mutation writer now consumes this primitive only through explicit schema-17 composition; normal runtime and operator acceptance remain pending.

IMP-07 satisfied BT-REL-011 immediately before its first production edit with 17/17 `MovementEntryCharacterizationTests` passing, 0 failed/skipped. Its reviewed schema-17 integration class passes 18/18 and proves exact native Single/Batch generation zero; first-successful request-order ordinals; identical and reordered retries without rewrite/duplication; changed-payload conflicts; zero artifacts after authorization/master-data rejection; schema-16 and malformed-schema-17 fail-closed behavior before new physical persistence; rollback after physical, mid-lineage, post-lineage, null-introduction-link and audit failures; unchanged migrated Single/Batch `MigrationBaseline` retries; current-root resolution; and AlreadyComplete acceptance of native `Initial` roots. The complete migration class passes 55/55, the provider-neutral contract/current-root unit filter passes 31/31, and the adjacent integration filter passes 59/59, all with 0 failed/skipped. A release `dotnet build BinTracker.sln --no-restore` passed with 0 warnings/errors, and the mechanical audit plus `git diff --check` passed. This is targeted/adjacent automated and release-build evidence after independent source review, not the canonical BAT, retained-production-database rehearsal, runtime activation or Windows/operator acceptance.

Unified schema-17 mutation coverage exercises Correct, Reverse and Restore persistence; whole-root mixed decisions; exact persisted generation-line identity; canonical replay/idempotency and expected-generation CAS; operation/audit uniqueness; rollback/failure injection; optional physical output; target-root native audit-health isolation; and primary audit before/after business-state completeness. Deterministic injected SQLite busy/locked failures prove exhausted contention becomes PersistenceFailure rather than stale-generation or integrity failure. The canonical `Build-BinTracker.bat` passed source audit, restore and Debug build with 0 warnings/errors; 279/279 UnitTests and 259/259 IntegrationTests passed, totaling 538/538 with 0 failed/skipped. This does not prove retained-production-database rehearsal, runtime schema activation, projection/UI behavior, packaging or Windows/operator acceptance.

The dormant corrected-projection authority has 10 focused schema-17 integration tests covering Active, Reversed, repeated correction/current generation, mixed Active/Reversed, Restore, RemainReversed, ReadOnly projection with mutation still prohibited, Adjustment/ExcelImport union, PositionAsOf boundaries and signs, corrected-dimension/date relevance, relevant Invalid/incomplete failure, unexpected unrooted ordinary failure, provably disjoint corrupt-root isolation, unknown relevance and non-registration/schema-16 rejection. The adjacent schema-17 integration set passed 126/126, provider-neutral lineage/planner unit set 63/63 and unchanged alpha.8 correction/balance/report characterization set 68/68. Canonical `Build-BinTracker.bat` then passed audit/restore/build with 0 warnings/errors and 279/279 UnitTests plus 274/274 IntegrationTests, totaling 553/553 with 0 failed/skipped. This remains static/automated evidence only: no consumer cutover, retained-database rehearsal, package, UI/manual or operator acceptance occurred.

Import comparison/execution projection coverage proves corrected pre-cutover arithmetic where raw physical history differs, correction/reversal/restoration semantics, strict replacement cutover/later exclusion, prior ImportRun exclusion exactly once, Adjustment and ExcelImport inclusion exactly once, and persisted previous-run count/net separation. Execution coverage proves one transaction-participating `PositionAsOf(CutoverDate - 1 day)` call for replacement and one `PositionAsOf(DateOnly.MaxValue)` call for ordinary new import, preserving the accepted all-representable-dates baseline. Projection corruption and cancellation propagate with no persistence/raw fallback; the caller-owned serializable transaction spans eligibility, baseline, deletion and every import write. Schema-16 no-projection compatibility and the `DateOnly.MinValue` empty-domain case remain covered. This is dormant source/automated evidence, not runtime activation or operator acceptance.

Normal composition still supplies no-op initial-lineage and mutation writers and remains schema 16. Explicit isolated schema-17 composition alone exercises the SQLite writers; tests must continue proving that default writers perform no schema probe/query/write. Core construction validation remains provider-neutral, Services remains client-neutral authority, and Data owns SQLite mechanics. No PostgreSQL/API/web/mobile/handheld execution evidence is implied.

Windows acceptance retains Batch #30 and covers RemainReversed, Restore, mixed dates, repeated whole-root correction, selected/whole correction in both orders, later reversal/restoration, navigation from original/reversal/correction/restoration evidence and descendants without physical output, immutable physical Batch Detail, Movement History, Audit Detail, Administrator Review, Daily/Weekly/Monthly corrected operational activity and current/Outstanding positions at the DPI floor and larger production displays. Before using approximate display IDs #1080/#1081/#1082, verify the persisted Batch #30 movement/reversal relationships read-only; those numbers are not identity authority. Automated success never claims this acceptance.

## Multi-user readiness regression

SQLite integration tests exercise retry identity, different-payload rejection, stale-edit rejection, current-cutover ownership and schema migration. They protect provider-neutral semantics but are not evidence of PostgreSQL/API execution; that requires a real fixture.

BinTracker uses separate unit and SQLite integration test projects.

## Unit tests

`tests/BinTracker.UnitTests`

Business-rule and regression coverage includes customer identity, balances, import planning/reconciliation, Market Floor rules/layout policy and other non-database logic.

## Integration tests

`tests/BinTracker.IntegrationTests`

Database-backed coverage includes schema upgrades, movement/balance behaviour and transactional importer behaviour, including transaction-boundary failure injection, relational ImportRunId provenance, and same-cutover replacement preserving same-day/later Manual activity outside the corrected baseline.

## Regression rule

When a real defect is found:

1. reproduce it with an automated test where practical;
2. fix the defect;
3. keep the regression test permanently.

## Current high-value gaps

- broader production-scale/custom workbook fixtures without private business data;
- Release-build acceptance;
- stress coverage for high-density Market Floor days.

## Local validation

Run:

```powershell
.\Build-BinTracker.bat
```

A local candidate is valid only when restore, build and automated tests all succeed with zero warnings.


## UI/business workflow acceptance

Automated integration coverage verifies that a changed workbook preflight supplied with the cutover date returns `RequiresReplacement = true`. Manual UI acceptance must additionally prove that Step 4 uses that state to show **Replace / Correct** before execution.


Correction regression coverage includes the real smoke-test shape: changing one Blue OUT quantity from 1 to 2 must produce exactly one changed customer/container position with a +1 difference.


Import Run history service integration coverage verifies replacement-chain lookup, linked movement detail and Administrator-only access. The WinForms history screen requires manual UI acceptance because this release changes UI.


Replacement integration coverage verifies `CorrectionChangesJson` is persisted by execution. Import History integration coverage verifies the stored snapshot is parsed and exposes previous/corrected/difference values.

Normal-cutover reconciliation coverage verifies `OpeningReconciliationChangesJson` persists every non-zero approved opening adjustment and that Import History exposes previous BinTracker position, Excel B/Fwd/target and adjustment. NULL means the historical build did not capture this provenance; `[]` means capture occurred and there were no non-zero opening adjustments.


The current UI smoke pass includes Import History readability at the operator's DPI plus Customer and Container Types unsaved-change **Save / Discard / Cancel** behaviour. These are manual acceptance checks because they depend on WinForms focus/navigation and layout.


Customer dirty-state protection requires full manual UI acceptance because selection, search/filter events, main-page navigation, logout and FormClosing are WinForms event-order behaviours. Container Types prompt wording and Import History no-wrap metadata are included in the same UI smoke pass.

## Manual testing policy

Every candidate build must be classified explicitly:

- **UI changed → Full smoke test.**
- **Business logic changed → Targeted smoke test** covering the affected workflow/calculation.
- **UI + business logic changed → Full smoke test**, with special attention to the affected logic.
- **Pure internal/refactor → Automated tests** unless a specific operational risk warrants manual verification.
- **Reports/printing changed → Real preview/print test** of the affected report.
- **Importer changed → Real-workbook test** when the behaviour depends on the production workbook/operator flow.

The release response should state `TEST REQUIRED: None / Targeted / Full` and list the exact checks.

## Documentation/audit policy

Every meaningful implementation pass must reconcile the current implementation against:

- `docs/Roadmap.md`;
- `docs/RoadmapCoverageMatrix.md`;
- `KNOWN-ISSUES.md`;
- `TECH-DEBT.md`;
- `TEST-CHECKLIST.md`;
- `docs/FunctionalSpecification.md`;
- `docs/BusinessRules.md`;
- relevant feature docs;
- `docs/RELEASE-NOTES.md`;
- `docs/CHANGELOG.md`.

Completed work is closed/removed from active lists; superseded statements are deleted rather than allowed to contradict current state. Historical release detail belongs in the changelog.

## Conversation-to-requirements reconciliation

Periodically compare the repository plan against the original product requirements and accepted operator decisions so requirements raised during bug-fixing do not disappear merely because they were not initially added to the roadmap.

## Historical reporting coverage

`OutstandingReportSqliteTests` verifies:

- future movements do not leak into an earlier As-of-Date result;
- OUT/IN produce the correct signed position;
- configured Container Types remain separate;
- credits are hidden by default and optionally included;
- customer/container/inactive filters work.

Because alpha.20.0 adds Reports UI, manual acceptance is a **Full smoke test** under the project testing policy.


Outstanding reporting regression coverage also checks customer/container adjacency so a multi-container customer is not visually split into separate large container blocks.


## Report launcher UI acceptance

The report launcher architecture is manually verified because integrated main-workspace page ownership/navigation is WinForms UI behaviour. Outstanding report calculation remains covered independently by SQLite integration tests.


Integrated-report UI acceptance includes a laptop-sized display and a larger desktop/27-inch monitor resolution. Verify filters/actions remain visible and the dataset expands with available client space.


## Compile-time dependency wiring

When a WinForms form/service constructor gains a required dependency, audit **all construction sites** in the solution. A build failure caused by a stale constructor call is a regression in dependency wiring and should be fixed before any runtime smoke test.


## Report action layout

Detailed report pages use a two-row control layout when necessary: filters on the first row and report actions on a dedicated second row. At supported DPI/resolutions, button labels must remain fully visible; wrapping may move whole controls but must never hide part of the action row.


## Interactive report sort/print acceptance

For Outstanding Containers:

- Position numeric sort must order by signed numeric balance, not formatted display text (`9`, `8`, `72`, `7` is invalid numeric descending order).
- Sort by Type/Code/Customer/Container should continue to use ordinary text ordering.
- Generate PDF / Generate & Open must preserve the grid's current displayed row order.


CSV preserves the grid's current displayed row order in the same way as PDF. After sorting Position, Type, Customer, Code or Container, exported CSV rows must appear in that order.


## Daily Movements coverage

`DailyMovementsReportSqliteTests` verifies:

- only the selected MovementDate is returned;
- opening adjustments are excluded by default and included only explicitly;
- OUT/IN totals are correct;
- customer/container/direction/source filters work.

Manual UI acceptance verifies responsive layout, Today/Yesterday, numeric Quantity sorting, and PDF/CSV visible-order consistency.


Daily Movements UI acceptance also verifies:

- the literal `Generate & Open` label is visible (ampersand is not consumed as a mnemonic);
- `All directions` is fully readable in the Direction selector;
- Include notes in exports off omits Notes from both PDF and CSV;
- Include notes in exports on adds the Notes column to both PDF and CSV;
- the denser default PDF layout does not reduce operational readability.


Daily Movements source-control acceptance verifies that Opening Adjustment is not present in the Source selector and is controlled solely by **Include opening adjustments**.


The three-row Daily Movements control layout is manually verified at production DPI: filter wrapping and option text must not push the action row beneath the summary/results area.


## Weekly Movements coverage

`WeeklyMovementsReportSqliteTests` verifies Monday-Sunday boundaries, exclusion of opening adjustments, OUT/IN/net totals, summary aggregation and customer/container/source filters.

Manual acceptance verifies This Week/Last Week, responsive layout, Detail/Summary tabs, numeric sorting and CSV visible-order behaviour.


## Weekly Overview export acceptance

- Daily Detail lists each movement row.
- Weekly Overview aggregates Customer + Container Type across the full Monday-Sunday week.
- Example validation: 45 OUT and 45 IN for the same customer/container appears as `OUT 45 / IN 45 / Net 0`.
- PDF and CSV export whichever tab is selected and preserve the current sort of that tab.
- One **Include notes in exports** option controls both PDF and CSV for Daily Detail and is disabled on Weekly Overview.
- `Generate & Open` visibly includes the literal ampersand.


## Weekly future-date/container-filter acceptance

- Date picker cannot select later than today.
- Service queried with a future date clamps to the current week.
- Future-dated movement rows are excluded.
- If the current week has future calendar days, the UI/PDF says activity is through today rather than implying future days had zero movements.
- Container filter is populated from configured Container Types and therefore includes Yellow/Bulk/etc. even when a type has no current outstanding balance.
- Inactive historical container types remain filterable and display `(inactive)`.
- One Include notes in exports option controls both PDF and CSV.


## Daily future-date acceptance

- Daily Movements date picker cannot select later than today.
- A future date supplied directly to the service is clamped to today.
- Future-dated movement rows are not returned by Daily Movements.


## Test visibility boundary

Integration tests should verify public behaviour through registered interfaces and returned results. They should not depend on internal concrete service classes merely to reuse helper methods; duplicate a tiny expected-value calculation in the test when that better preserves the implementation boundary.


## Build audit acceptance gate

Before packaging every candidate:

- enumerate all Markdown files;
- reconcile current-state docs against actual implementation;
- compare Roadmap against Roadmap Coverage Matrix;
- reconcile version references with `Directory.Build.props`;
- confirm Known Issues contains real current limitations only;
- confirm Tech Debt contains unresolved engineering debt rather than already-fixed defects;
- confirm Functional Specification / Business Rules describe current behaviour;
- confirm Test Checklist includes the new candidate's acceptance requirement;
- preserve superseded historical behaviour only in CHANGELOG;
- record the audit in `docs/DocumentationAudit.md`.

Failure of this audit blocks packaging in the same way as a failed automated test.


## Movement History coverage

- Open Movement History from Reports and confirm it replaces the main content area at full size rather than opening a floating window. Navigate away and back; confirm one clean page instance and all filters/actions remain functional.
- At the normal maximized production width, confirm predictable columns are compact, Customer/Status/Notes use the available remainder, ordinary values are readable and no unnecessary horizontal scrollbar appears.
- Resize narrower and confirm useful minimum widths are retained before horizontal scrolling appears. Confirm rows stay single-height at supported DPI settings.
- Confirm IN uses a restrained green badge, OUT a restrained red badge, and both reversed originals and reversal rows use amber/orange Status badges. Select each row type and confirm badge and row text remain readable.
- Hover long/truncated Status and Notes values and confirm the full text tooltip. Confirm derived Status wording and persisted ledger Notes are unchanged.
- Apply a customer filter resolving to exactly one customer and confirm PDF and CSV suggested filenames use its sanitized stable code. Confirm empty/unfiltered and multi-customer results use the generic filename.

`MovementHistoryReportSqliteTests` verifies:

- inclusive date-range boundaries;
- default exclusion of Opening Adjustments;
- OUT/IN/net totals;
- customer/container/direction/source filtering;
- future-date clamping;
- reversed-range normalization.

Manual acceptance verifies responsive layout, Last 7 Days / Last 30 Days / This Month shortcuts, authoritative Container Type choices, typed Date/Quantity sorting and PDF/CSV visible-order consistency.

Movement History identity acceptance additionally verifies that the displayed/exported Movement ID is the persisted identifier used by correction/reversal, remains paired with the correct row after filtering and sorting, sorts numerically (for example 2 before 10), and participates in Shift+click multi-column sorting without changing selection semantics.

Required DPI smoke configuration: **Windows 11, 1920x1080, 150% Windows scaling**. Movement History must show the full Movement ID header and Entered by column without normal-case horizontal scrolling; Status/Notes alone may wrap with auto-height. Correct Entire Batch must keep heading/context/fields/reason/actions visible, show no form/content scrollbar for a two-line batch, and scroll only the movement list for a genuinely long batch. Also confirm the substantially larger primary production display is not degraded. This is a manual visual gate; build/unit success does not prove it.


## Interactive report refresh acceptance

For Outstanding Containers, Daily Movements, Weekly Movements and Movement History:

- Run Report button is absent.
- Changing date/dropdown/result-affecting checkbox filters refreshes results.
- Typing a Customer does not query on each character.
- Pressing Enter in Customer applies the search.
- Shortcut buttons still refresh immediately.
- Export/PDF continues to use the resulting on-screen dataset/order.


## Report wrapped-layout/customer-cue acceptance

- Weekly Movements at laptop width may wrap filters, but all shortcut/PDF/CSV buttons remain fully visible.
- Outstanding, Daily, Weekly and Movement History visibly explain that Customer search applies on Enter.
- Customer Enter continues to refresh exactly once.
- Live dropdown/date/checkbox behaviour remains unchanged.
- BinTracker executable/window icon uses the supplied product icon and the sidebar displays the supplied product logo without dominating the navigation.


## Weekly wrapped-control layout regression

Weekly Movements controls must remain fully visible when the filter row wraps at laptop width/DPI scaling. The filter/options/action area must contribute its true preferred height before the summary/grid rows are laid out. Test with the Source filter wrapped to a second line and verify all action buttons are fully visible.


## Application icon/branding acceptance

- Launch BinTracker and verify Login shows the BinTracker icon in its title bar and taskbar before authentication.
- Verify Main, integrated report surfaces, Import/Admin and other dialog Forms use the same icon.
- Verify the left navigation shows the BinTracker product logo beside the BinTracker wordmark.
- Verify no form fails to open if icon extraction unexpectedly fails.


## Sidebar wordmark clipping regression

- Verify the full `BinTracker` wordmark is visible at the standard laptop resolution/DPI.
- Verify logo and wordmark remain vertically aligned.
- Verify the wider sidebar does not cause navigation labels or main content to clip.
- Verify logo still reads cleanly against the navy background.


## Customer Statement generate/open workflow

- Select a customer and open Customer Statement.
- Confirm From/To cannot be moved beyond today.
- Confirm an invalid From/To range is rejected.
- Confirm **Generate PDF** prompts for a PDF location and saves a valid statement.
- Confirm **Generate & Open** does not prompt for a save location and opens the generated PDF in the Windows default PDF application.
- From the opened PDF viewer, confirm the statement can be printed normally.
- Confirm the generated statement still uses the selected customer and selected period.


## Customer Statement Reports launcher

- Open Reports → Customer Statement.
- Confirm typing Customer text does not search on each keystroke; Enter applies the search.
- Confirm Include inactive refreshes the customer list.
- Confirm double-click and Customer Statement button launch the same period/generation workflow used from Customers.
- Confirm Generate PDF / Generate & Open results are identical regardless of whether the workflow was entered from Customers or Reports.


## Customer Statement owner compile boundary

The shared Customer Statement workflow accepts `IWin32Window` as its owner. Callers must explicitly provide a compatible `IWin32Window`; do not use `FindForm() ?? this` where the operands have different concrete types (`Form` and `UserControl`).


## Monthly Summary coverage

`MonthlySummaryReportSqliteTests` verifies:

- inclusive calendar-month boundaries;
- default exclusion of Opening Adjustments;
- OUT / IN / Net calculations;
- customer/container/source filtering;
- future-month clamping to the current month and activity-through-today semantics.

Manual acceptance verifies This Month / Last Month shortcuts, authoritative Container Type choices, live filter behaviour, Customer-on-Enter search, numeric sorting and PDF/CSV visible-order consistency.


## CSV export audit coverage

Manually export CSV from Outstanding Containers, Daily Movements, Weekly Movements (both views), Movement History and Monthly Summary. Confirm each successful export creates its report-specific `*_CSV_EXPORTED` AuditEvent with filename, row count and relevant report/filter context.


## Build tooling resilience

Build acceptance includes running `Build-BinTracker.bat` repeatedly from a normal Windows developer shell.

Verify:
- the header reports the actually resolved compatible installed SDK (currently 10.0.400 on the development PC);
- stale build-server shutdown does not block the build;
- restore/build/test complete with MSBuild server/node reuse disabled;
- the build no longer intermittently reports MSB4242 SDK Resolver Failure / worker node shutdown under normal use;
- the project continues targeting net8.0/net8.0-windows without requiring an uninstalled restrictive SDK pin.


## Build script failure-path regression

For build-tooling changes, verify both success and failure paths:

- normal build reports the installed/resolved SDK and completes successfully;
- a deliberately invalid solution/project argument returns `BUILD FAILED`;
- restore failure stops immediately and does not continue to build/test;
- build failure stops immediately and does not continue to tests;
- test failure returns `BUILD FAILED`;
- the script must never print `BUILD SUCCESSFUL` after any failed dotnet command.


## Stale global.json self-heal regression

- Place the exact alpha.23.3 generated `global.json` beside `Build-BinTracker.bat`.
- Run the BAT and confirm it deletes that obsolete file, resolves the installed SDK and continues.
- Place a different/user-managed `global.json` beside the BAT and confirm BinTracker refuses to delete it and reports BUILD FAILED.
- Force a restore/build/test failure and confirm the `command || goto :fail` path prints BUILD FAILED and never BUILD SUCCESSFUL.


## Daily Print Pack acceptance

Task 19 review correction retains the deterministic behavior tests and adds reflection tests against the compiled contracts: both public report interfaces expose only their normal `QueryAsync(query, CancellationToken)` returning typed results, while snapshot participants and implementations are internal with explicit private method implementations. Integration wrappers participate through test-only friend access; ordinary report fakes implement only the public query contract. These architecture checks supplement, not replace, the snapshot/interleaving/ownership behavioral tests.

Task BT-CODEX-20260910-19 satisfied characterization-before-change: the existing pack, Outstanding, Daily, complete projection/consumer and alpha.8 correction workflow filter passed 100/100 before production editing; strengthened pack-section/PDF/audit characterization passed 6/6. A separate deterministic expected-behavior regression then failed against unchanged production code with Outstanding 7 / Detail 11 after a committed correction between reads.

Dormant schema-17 tests now capture the actual delegated results supplied to PDF generation. They interleave committed correction, reversal and restoration on separate connections while the pack's read transaction stays open, verify both sections retain the before-state, then verify a fresh pack sees the after-state. Customer code/name/type/activity and container name/order updates exercise the same metadata boundary; active-only Outstanding and historical Daily inclusion remain distinct accepted filters. No sleeps or timing-dependent scheduling are used. Relevant corruption, second-projection failure, pre/mid-read cancellation, unsupported participants and transaction/connection ownership are covered. Schema-16 compatibility, accepted filters/order/totals/date clamping and PDF/audit tests remain required. These tests do not constitute real preview/print or Windows acceptance.

- Reports inline Market Floor and Daily Print Pack date pickers cannot select a future date.
- Daily Print Pack Outstanding Summary is calculated as at the selected date.
- Movement Detail contains physical movements only; Opening Adjustments are excluded.
- Generate PDF and Generate & Open produce one readable two-section PDF.
- `DAILY_PRINT_PACK_GENERATED` is written exactly once per generated pack.
- Real preview/print validation is required before the Reporting milestone closes.

## Mechanical audit regression

Run `Audit-BinTracker.ps1` and confirm it rejects a deliberately stale README/current Release Notes/Roadmap baseline, unexpected `global.json`, or mismatched Version/InformationalVersion, and passes after reconciliation.


## Requirements register audit regression

- Confirm every table requirement ID in `docs/RequirementsAcceptanceRegister.md` is unique.
- Confirm Scope/Status values are from the approved register enums.
- Remove or duplicate a required ID in a temporary copy and confirm `Audit-BinTracker.ps1` fails.
- Introduce a known stale phrase (old SDK pin, stale importer remaining item, separate Run Report expectation) and confirm the audit fails.


## Reports landing-page redesign acceptance

Verify the approved alpha.24 Reports mock-up implementation:

- Quick Reports shows exactly two prominent side-by-side cards: Market Floor Sheet and Daily Print Pack.
- Quick Report cards use the approved report icons, Date selector, Generate PDF and blue Generate & Open action.
- Explore Reports is 3 columns × 2 rows at normal desktop width and contains the six approved report cards.
- Every Explore Reports card uses the approved icon artwork and Open → footer action.
- Report cards open the accepted integrated main-workspace report pages; no report business/export behavior is duplicated or replaced.
- Reports header subtitle reads `Generate operational sheets and explore detailed reports.`
- Normal maximized 1080p desktop must not show a Reports landing-page scrollbar.
- Generate PDF must show its document icon; Generate & Open must stay on one line; every Explore Open button must render the full word `Open` without clipping.
- Test at 100%, 125% and 150% Windows display scaling.
- Verify Containers appears immediately below Customers in the left navigation for every signed-in role; non-admin users receive a clearly read-only view and Settings does not duplicate Container Types administration.

## Movement correction/reversal authorization regression

Integration coverage must verify Operator/Admin ordinary-reversal authorization for Manual and Batch movements, Viewer denial, generic-workflow denial for Opening Adjustment and Excel Import rows, immutable original/reversal linkage, double-reversal protection and reversal-of-reversal protection. Manual Windows acceptance verifies role-based action visibility and the sensitive-source operator messaging.

Correction coverage additionally verifies every editable field, wrong-date day/week/month semantics, persisted whole-batch date/direction replacement, all-or-nothing conflict behavior, database-enforced reverse/correct exclusion, payload-aware retries, Operator review state, audited acknowledgement, migration backfill and authoritative MovementBatch detail. UI/dialog/DPI and real multi-window interaction remain Windows/operator acceptance.
- Correction reporting regression must cover quantity, wrong-date, direction, customer and container changes across effective Daily/Weekly/Monthly totals, customer recent movements, statements and Market Floor; immutable Movement History must retain and distinguish original/neutraliser/replacement, and ordinary reversal visibility must remain unchanged.

Audit Trail review Windows/regression acceptance must prove the complete sequence: Operator correction/reversal; persisted review requirement; later Administrator login and one consolidated notification; visibly discoverable `Needs review` row through the review-state display/filter; selection enabling **Mark Selected Reviewed**; successful acknowledgement; transition to `Reviewed`; persisted reviewer/UTC time; visible acknowledgement audit event; and rejected/disabled duplicate acknowledgement.

Persistent-reminder acceptance (BT-AUD-013) must additionally prove Administrator-only visibility across main navigation, accurate initial and refreshed outstanding counts, direct routing to the pending Audit Trail set, persistence until the last review is acknowledged, automatic disappearance at zero, retention of the existing login popup and no approval/blocking effect. Contract tests should cover presentation-independent review state/count/navigation separately from WinForms rendering; future WinUI 3 uses the same contract.

Movement-change detail acceptance must verify exact original/neutraliser/replacement lineage, actor/time/reason, field-accurate before/after differences and persisted batch IDs, including exclusion of unchanged/unrelated rows and fail-closed identity. An exact single-event review acknowledgement must open the same referenced lineage with reviewer/time, remain non-reviewable, and reject missing/invalid references. Esc hierarchy remains BT-AUD-010/014.

Selection-state acceptance must verify **Mark Selected Reviewed** is disabled for no selection, Administrator corrections/reversals, login/logout, report generation, customer/container/import events, already-reviewed rows and every other non-reviewable event. Viewer and Operator must fail unauthorized Administrator review attempts at the service boundary regardless of UI visibility.

Audit detail acceptance must verify **View Batch Detail** is disabled for no selection and non-batch events and enabled for an event with authoritative persisted MovementBatch detail. Existing detail contents remain authoritative; contextual ImportRun and correction-lineage routes are future tracked behavior and are not part of the current implementation claim.
