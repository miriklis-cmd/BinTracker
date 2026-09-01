# Documentation Audit Record

## 2 September 2026 — IMP-06 checkpoint governance reconciliation

- Mechanically reconciled the active continuation to committed HEAD/upstream `eb84a61567cce232238cf66ace8048851650988c` (`Add transaction-compatible audit primitive`), 0 ahead / 0 behind, version `0.5.0-alpha.8.7`, and the verified clean tracked/staged starting state with only `.codex-evidence/` plus `BIN-LIN-IMP-03A-HANDOFF.tmp` untracked. This reconciliation itself leaves only `docs/CONTINUATION.md` and `docs/DocumentationAudit.md` modified and unstaged.
- Corrected operative sequencing to record IMP-06 complete, independently reviewed and followed by a canonical BAT pass of 443/443 automated tests with 0 failed, 0 skipped, 0 warnings and 0 errors. IMP-07 generation-zero Single/Batch integration is next in dependency order but remains unimplemented and is not yet authorized for production/application-code editing.
- Recorded the IMP-07 characterization gate: characterization must precede the first production edit; production code must not be changed merely to manufacture a characterization seam; an otherwise inaccessible failure mode receives truthful observable-behavior characterization first and new failure-injection proof only during or after approved implementation.
- This reconciliation changes documentation governance only. It changes no frozen semantics, architecture, requirement ID/status, roadmap scope, source, tests, schema, version, runtime behavior or acceptance classification.
- `Audit-BinTracker.ps1` passed at v0.5.0-alpha.8.7 with 259 permanent requirement IDs, 27 Markdown files inventoried and configured contradiction guards passed. `git diff --check` passed with only line-ending notices.

## 2 September 2026 — BIN-LIN-IMP-06 caller-transaction audit primitive

- Started from clean tracked/staged checkpoint `e08acc9fbe07bb8f0f9fe48b33549b8a283560ed`, synchronized with `origin/codex/movement-correction`, with only `.codex-evidence/` and `BIN-LIN-IMP-03A-HANDOFF.tmp` untracked and version `0.5.0-alpha.8.7`.
- Satisfied BT-REL-011 before the first production edit: 43/43 accepted correction/reversal/audit workflow characterization cases passed, 0 failed/skipped.
- Added the unregistered Data-boundary `TransactionAuditAppender`. It requires an active caller-owned DbContext transaction, tracks exactly one new primary AuditEvent and owns no DbContext, transaction, SaveChanges or commit. Existing independent `AuditService.WriteAsync` source/behavior remains unchanged.
- Focused appender tests passed 4/4 and prove unsaved Added state, exact caller-commit persistence, atomic rollback of both the audit and legitimate sibling customer state saved in the same caller transaction, fail-closed missing transaction and unchanged independent audit persistence. The combined appender plus correction/reversal/workflow/concurrency filter passed 53/53 before the tests-only proof correction, 0 failed/skipped.
- Existing dormant tests already proved unique complete structured legacy audit mapping and required audit column/index presence. The added `Audit_operation_association_is_restrict_foreign_key_and_unique` test supplies the previously missing behavioral proof that `AuditEvent.MovementCorrectionOperationId` is a RESTRICT FK and rejects a second primary audit for the same operation. These three exact structural tests passed 3/3 without schema registration or production activation.
- Release solution build passed with 0 warnings/errors. `Audit-BinTracker.ps1` passed at v0.5.0-alpha.8.7 with 259 permanent requirement IDs and 27 Markdown files inventoried; `git diff --check` passed with only line-ending notices. The direct audit invocation was blocked by local execution policy before running, so the same repository script was executed successfully with a process-scoped `-ExecutionPolicy Bypass`.
- Independent review approved the bounded IMP-06 source design and corrected proof. The subsequent canonical `Build-BinTracker.bat` passed source/package-state audit, restore and Debug build with 0 warnings/errors; 266/266 UnitTests and 177/177 IntegrationTests passed, totaling 443/443 with 0 failed/skipped. No package or Windows/operator acceptance is implied.
- Reconciled only current lineage/audit status and evidence documents. No requirement ID/status, schema, migration registration, runtime service registration, existing audit service behavior, version or release acceptance changed.
- Schema 17 remains dormant/unregistered and normal authority remains schema 16/alpha.8. No production lineage writer, generation-zero integration, Correct/Reverse/Restore execution, CAS/idempotency execution, projection/report/numeric cutover, review/detail expansion or UI work occurred. IMP-07 remains unstarted and unauthorized.
- This is static/focused automated/source-gate evidence plus the independent IMP-06 review recorded above; no retained-production-database rehearsal, package or Windows/operator acceptance is claimed.

## 1 September 2026 — BIN-LIN-IMP-05D post-approval checkpoint reconciliation

- After the earlier IMP-05/05B/05C audit entry, independent external source/diff review approved the dormant mutation-planning boundary. Canonical `Build-BinTracker.bat` passed at v0.5.0-alpha.8.7: source/package-state audit, restore and build PASS; 438/438 automated tests passed, 0 failed and 0 skipped; no compiler warnings/errors were reported.
- Checkpoint `c0dfc7e51cae1296fd5a5da31876e54364901405` (`Add trusted movement mutation planner`) was committed, pushed and verified synchronized with `origin/codex/movement-correction` at 0 ahead / 0 behind before this documentation-only reconciliation began.
- Reconciled current-state review-pending, unstaged-implementation and old-HEAD wording across the six affected Markdown authorities while retaining explicitly historical pending-at-that-handoff chronology. No requirement IDs or lineage semantics changed.
- Schema 17 remains dormant/unregistered and DatabaseSetup/normal startup/runtime authority remains schema 16/alpha.8. No production lineage writer, CAS/idempotency execution, runtime registration, report/projection cutover or WinForms cutover occurred. IMP-06/07 remain unstarted. This checkpoint does not imply Windows/operator acceptance or retained-production-database migration rehearsal.

## 1 September 2026 — BIN-LIN-IMP-05/05B/05C dormant mutation planner (pre-approval audit)

- BIN-LIN-IMP-05C corrected the externally reviewed trusted reversal-pair defect: the materializer now carries persisted `ReversesMovementId`, rejects contradictory current ordinary-reversal business facts before returning a trusted snapshot, and planning rejects future current effective/terminal dates against the separate authoritative business date. At this pre-approval audit, the implementation remained dormant/unregistered and awaited external source/diff review; the later IMP-05D entry above records its approval.
- BIN-LIN-IMP-05B previously corrected the externally reviewed request, result-pointer, trusted-construction, domain-eligibility, business-date and deterministic-test defects; all eight corrections remain part of the current review baseline.
- IMP-05C ran all 37 `MovementCorrectionWorkflowTests` before correction editing. Adversarial unit and persisted disposable-schema tests now cover every reviewed pair/date corruption while the positive restoration/remain/mixed cases remain.
- The first IMP-05 implementation did not execute the required alpha.8 characterization before its first application edit. IMP-05B recovered the gate before correction editing by running all 37 `MovementCorrectionWorkflowTests`; this is recovery evidence, not retroactive ordering compliance. Post-correction results are recorded in the IMP-05B evidence.
- Authority classification now truthfully records the already approved NEW trusted snapshot/materialization boundary, controlled complete-generation plan contract and IMP-05A per-action AppliedFieldMask equality design; these were not invented during IMP-05B.
- At this pre-approval audit, continuation sequencing used exact HEAD `8011cc6b4004a4c786d5f0018e5d11517d2bfbd7`, identified IMP-04/04A as complete and the then-unstaged IMP-05B plus IMP-05C correction as its external-review scope, and prohibited IMP-06/07. This is historical chronology superseded by the later IMP-05D checkpoint entry above.
- Added the permanent exact-HEAD continuation sequencing cross-check to `docs/DevelopmentWorkflow.md` and separately passed the mechanical audit after the governance-only repair.
- Recorded the formally frozen AppliedFieldMask meaning, trusted exact-fact snapshot, pure complete-generation planner and physical-output predicate without claiming persistence, runtime registration, schema-17 activation, command/CAS/idempotency/audit work or operator acceptance.
- Stabilized the directly exercised alpha.8 reversal regression so its fixed historical start date uses the actual current reversal date as the query end; this preserves the assertion and removes calendar-dependent exclusion of the row under test.
- Reviewed all 27 governed Markdown files by inventory and mechanical sequencing/status/version/dormancy searches plus direct review of the affected lineage authorities. Repaired the active continuation's stale initial-baseline/current-phase labels, and reconciled current Roadmap coverage and Known Issues status. Historical chronology was preserved and no operative instruction directs IMP-06/07 or claims runtime/schema activation.

## 31 August 2026 — BIN-LIN-IMP-04A current-validation correction

- Corrected the dormant IMP-04 contract for non-forgeable validated models, current-proof-only ledger relevance, original physical-batch/null-single proof, preserved status reason and ordinal structure.
- Preserved broader MigrationBaseline/global postflight checks and did not introduce historical runtime diagnostics or later-slice behavior.
- Reconciled all 27 governed files separately from the mechanical audit; static evidence remains `[S]`, schema 17 remains dormant and no manual acceptance is claimed.

## 31 August 2026 — BIN-LIN-IMP-04 dormant current-root resolver

- Reconciled the validation-gated CURRENT-root slice without claiming production activation or broad requirement completion.
- Ordinary resolution validates exactly the selected current snapshot under one read transaction; full-chain diagnostics and future mutation predecessor/planner/CAS validation remain separate.
- Schema 17 remains dormant, normal startup remains schema 16, historical PhysicalOutput baseline remains zero and IMP-05/06/07 remain pending.
- Static/automated evidence remains `[S]`; no operator acceptance was promoted. Mechanical audit and 27-file semantic review remain separate evidence.

## 31 August 2026 — BIN-LIN-CHECKPOINT-03 reviewed foundation checkpoint

- External oversight approved the accumulated BIN-LIN-IMP-01, IMP-02A plus IMP-03A correction, IMP-02C, IMP-03 and IMP-03B dormant foundation for one local checkpoint commit.
- Reconciled current-state status wording without changing the frozen lineage semantics: schema 17 remains dormant, normal production startup remains schema 16, unsupported schema-16 operation Kind outside 0/1 remains a database-wide blocker, and no runtime/report/service/UI cutover is claimed.
- The 27-file governed Markdown inventory and targeted contradiction checks remain separate from the mechanical audit. Historical IMP handoff statements retain their at-the-time evidence status; this entry records the later approval.
- BIN-LIN-IMP-04 has not started. Retained-database rehearsal, production activation and Windows/operator acceptance remain later gates.

## 31 August 2026 — BIN-LIN-IMP-03B evidence/checklist reconciliation

- External oversight found documentation/evidence-classification defects in IMP-03A, not a new lineage implementation defect.
- Reconciled every active `[A]` checklist line against explicit `IMPLEMENTED-ACCEPTED` ledger/history evidence; split mixed Batch Entry, Import Replace/Correct and Market Floor lines, and separated existing statement running balances from the planned lineage statement-boundary rule.
- Added `[S]` for implemented static/automated/internal evidence where no operator retest is the relevant gate; retained `[R]` only for a specific outstanding manual/Windows/DPI/preview/print/real-workbook retest.
- Replaced the 03A matrix's blanket `Read fully? Yes` claim with conservative review-method evidence acknowledging tool truncation. The semantic findings remain unchanged.
- No application, runtime, migration, schema or test file changed in 03B; schema 17 remains dormant and IMP-04 remains blocked pending external approval.

## 30 August 2026 — BIN-LIN-IMP-03A full governed-Markdown semantic reconciliation

This review is separate from `Audit-BinTracker.ps1`. The script is a mechanical gate; it cannot prove that prose/status across all documents is semantically current. All 27 Git-governed Markdown files were inventoried and semantically reviewed using the evidence methods recorded below. A combined raw read was truncated by the Codex display layer, so this record does not claim an uninterrupted full-file read for every file. Subsequent targeted searches, relevant excerpts and individual-file checks supported the recorded conclusions. Current-state claims were compared with the requirements register, source/test evidence and recorded acceptance; historical documents were retained as history.

| File | Review method / evidence | Classification | Relevant? | Stale/contradiction found? | Change? | Reason/evidence |
|---|---|---|---|---|---|---|
| `AGENTS.md` | Targeted semantic/search review | Current governance | Yes | No | No | Existing truth/acceptance/Git gates remain controlling. |
| `KNOWN-ISSUES.md` | Targeted semantic/search review | Current state | Yes | Yes | Yes | “Lineage not implemented” ignored dormant IMP-01/02A/03 work; runtime limitation retained. |
| `README.md` | Targeted semantic/search review | Current overview | Yes | Yes | Yes | Breakout report-window wording contradicted BT-RPT-001/018 and source. |
| `TECH-DEBT.md` | Targeted semantic/search review | Current debt | Yes | Yes | Yes | Report windows, migration backup and already-implemented Administrator review/reminder wording were stale. |
| `TEST-CHECKLIST.md` | Targeted semantic/search review | Current acceptance | Yes | Yes | Yes | Mixed historical checkboxes falsely made implemented-static/accepted behavior look unimplemented; replaced by explicit evidence states and active checks. |
| `docs/Architecture.md` | Targeted semantic/search review | Current authority | Yes | Yes | Yes | Partial dormant implementation status and schema-16 unknown-Kind blocker required clarification. |
| `docs/AuditCoverage.md` | Targeted semantic/search review | Current authority | Yes | Yes | Yes | Dormant infrastructure existed, but production audit wiring remains activation-time work. |
| `docs/BalanceReconciliation.md` | Targeted semantic/search review | Current authority | Yes | Yes | Yes | v0.4.0-alpha.13 no-write statement contradicted implemented transactional reconciliation. |
| `docs/BusinessRules.md` | Targeted semantic/search review | Current authority | Yes | Yes | Yes | Dedicated report-window rules contradicted integrated pages. |
| `docs/CHANGELOG.md` | Historical/reference review | Historical record | Yes | No current-state defect | No | Old window/version/status statements are dated historical evidence. |
| `docs/CONTINUATION.md` | Targeted semantic/search review | Active working state | Yes | Yes | Yes | IMP-03 recorded the unsafe ReadOnly Kind behavior and lacked 03A state. |
| `docs/Database.md` | Targeted semantic/search review | Current authority | Yes | Yes | Yes | Required explicit schema-16 Kind 0/1-only blocker rule. |
| `docs/DevelopmentWorkflow.md` | Targeted semantic/search review | Current governance | Yes | Yes | Yes | Mechanical audit and semantic reconciliation were not distinguished strongly enough; enum migration trap added. |
| `docs/DocumentationAudit.md` | Targeted semantic/search review | Governance/history | Yes | Yes | Yes | Needed this semantic inventory and separate gate evidence. |
| `docs/FunctionalSpecification.md` | Targeted semantic/search review | Current authority | Yes | Yes | Yes | Single report-window instance rule contradicted integrated page hosting. |
| `docs/ImportWizard.md` | Targeted semantic/search review | Current authority | Yes | No | No | Current Replace/Correct/read-only review semantics remain consistent. |
| `docs/LegacyContainerRules.md` | Not materially applicable; targeted consistency check | Current reference | No | No | No | Legacy container mapping remains consistent and unrelated. |
| `docs/MasterData.md` | Not materially applicable; targeted consistency check | Current authority | No | No | No | Text branding/logo status remains accurate. |
| `docs/ReconciliationReport.md` | Historical/reference review | Dated reconciliation record | Yes | No current-state defect | No | Older inspected-state claims are explicitly part of that historical reconciliation. |
| `docs/ReimportSafety.md` | Targeted semantic/search review | Current authority | Yes | No | No | Import domain boundary remains consistent with lineage. |
| `docs/RELEASE-NOTES.md` | Historical/reference review | Current package/history | Yes | No | No | Describes released alpha.8.7 and documentation-freeze package, not unaccepted working-tree implementation. |
| `docs/RequirementsAcceptanceRegister.md` | Targeted semantic/search review | Current authority | Yes | Yes | Yes | Added fail-closed old-enum rule and separate mechanical/semantic audit requirement without new IDs. |
| `docs/Roadmap.md` | Targeted semantic/search review | Current sequencing | Yes | Yes | Yes | “Implementation not begun” and Monthly breakout-window wording were stale. |
| `docs/RoadmapCoverageMatrix.md` | Targeted semantic/search review | Current coverage | Yes | Yes | Yes | Logical-lineage row omitted dormant implementation progress. |
| `docs/SecurityHardeningRegister.md` | Targeted semantic/search review | Current finding ledger | Yes | Yes | Yes | Alpha.6.5 was finding-capture baseline, not current application version; labels now distinguish both. |
| `docs/Testing.md` | Targeted semantic/search review | Current authority | Yes | Yes | Yes | Report-window and “lineage not implemented” wording stale; hostile old-enum rule added. |
| `docs/Versioning.md` | Targeted semantic/search review | Current governance | Yes | No | No | Version source/package rules remain correct; examples are illustrative. |

Additional reconciliation outcomes:

- Schema-16 `MovementCorrectionOperations.Kind` values outside historical 0/1 are database-wide migration blockers, never root-scoped ReadOnly evidence and never schema-17 Reverse/Restore.
- Checklist status now distinguishes accepted manual evidence, implemented-static/automated evidence, specific manual retest, genuine pending work and per-candidate gates without claiming new human acceptance.
- Narrow mechanical guards were added for the exact stale report/reconciliation/lineage phrases fixed here. They supplement rather than replace semantic review.
- No historical changelog/release record was rewritten merely because its older behavior differs from current state.

## 29 August 2026 — movement-lineage architecture documentation freeze

- [x] Reconciled approved design reviews and read-only schema-v16 preflight without source/schema/tests/version changes.
- [x] Added planned BT-ARCH-016..018, BT-AUD-015..017, BT-HIST-008..009, BT-CORR-018..033 and BT-OPS-011..012; retained BT-CORR-009/015 as traceable alpha.8 foundations.
- [x] Froze roots/lines/full generations, restoration/RemainReversed/no-op, corrected activity/PositionAsOf, output predicate, idempotency/CAS, portability, corruption split, migration/preflight/backup and acceptance gates.
- [x] Corrected API/PostgreSQL implementation to post-v1 and protected the post-lineage whole-codebase layer audit.
- [x] Preserved BT-CORR-013/014 post-v1 and ImportRun as a separate correction domain.
- [x] Mechanical audit passed with 257 unique permanent IDs and 26 governed Markdown files; targeted contradiction searches and `git diff --check` passed.

## v0.5.0-alpha.8.7 Windows-acceptance interaction corrections

- [x] Inspected Development Workflow, Roadmap, Known Issues, Changelog, requirements register, specification, business rules and active acceptance checklist before implementation.
- [x] Added BT-CORR-016..017 and BT-MOVE-024..025 for selected-row action synchronization and semantic whole-batch field activation/no-op rejection.
- [x] Preserved immutable chained-correction behavior and added focused proposal/service regression coverage. User Windows acceptance passed Movement History selection/action synchronization, whole-batch auto-tick/auto-clear, manual no-op rejection and focus-stable `Confirm Every Line` rendering.
- [x] User-ran canonical `Build-BinTracker.bat` passed source/package-state audit (231 permanent requirement IDs / 26 Markdown files), restore, zero-warning/zero-error build and 310/310 automated tests with 0 failed or skipped.

## v0.5.0-alpha.8.7 Windows-acceptance corrective/readability build

- [x] Clean committed/pushed alpha.8.6 checkpoint verified at `e2ade92`; governing requirements/checklists and exact touched UI/service paths inspected before edits.
- [x] Alpha.8.6 manually accepted review-state/filter/routing/confirmation/count/infobar/Esc behavior retained and register statuses reconciled.
- [x] Existing BT-AUD-007..014, BT-UI-015 and BT-REPORT-HISTORY-013 extended without duplicate IDs; correction transaction path unchanged.
- [x] Exact comparison/action-label/acknowledgement-resolution tests added; existing correction/history/export gates retained.
- [x] Source/governance/security audit passed at 229 unique permanent IDs/25 Markdown files; restore succeeded; canonical BAT built with 0 warnings/0 errors and passed 212 unit + 90 integration tests.
- [x] Package audit passed with ZIP filename, sole root, Version and InformationalVersion all `0.5.0-alpha.8.7`.
- [ ] Alpha.8.7 Windows 11 1920x1080/150% and larger-display visual acceptance remains required.

## v0.5.0-alpha.8.6 Administrator review-workflow corrective build

- [x] Governing Markdown/register/checklist set and clean pushed alpha.8.5 checkpoint inspected before implementation; accepted correction semantics and permanent IDs retained.
- [x] Existing per-`AuditEvent` review storage retained; no competing review store or correction transaction change introduced.
- [x] BT-AUD-007..010/013 reconciled to implemented-static/manual-acceptance-pending state; BT-AUD-014 adds the explicit Esc hierarchy.
- [x] Review filters, eligibility, exact-event acknowledgement/count transitions, exact lineage resolution and fail-closed behaviour receive deterministic service/helper regression coverage.
- [x] Source audit passed at 229 unique permanent requirement IDs/25 Markdown files; restore succeeded; canonical BAT built with 0 warnings/0 errors and passed 204 unit + 89 integration tests.
- [x] Package audit passed with ZIP filename, sole root, Version and InformationalVersion all `0.5.0-alpha.8.6`.
- [ ] Full Windows 11 1920x1080/150% and larger-display smoke acceptance remains required; this document does not claim visual acceptance.

## v0.5.0-alpha.8.5 Movement History identity and batch-dialog DPI correction

- [x] All repository Markdown files and release/build/audit scripts enumerated; governing architecture, requirements, security, testing, versioning, checklist and current release state reviewed before source inspection.
- [x] Actual Movement History query, typed row, grid population/sort, PDF/CSV snapshot and correction selection paths traced to the persisted `BinMovement.Id`.
- [x] Correct Entire Batch clipping traced to a fixed-height DPI-scaled form whose one top-down flow appended the only final action after all content.
- [x] BT-HIST-007 and BT-UI-015 added without changing BT-AUD-006..013 scope or correction/audit semantics.
- [x] Required Windows 11 1920x1080/150% laptop acceptance configuration distinguished from the substantially larger primary production display and manual-only visual proof recorded.
- [x] Source/governance/security audit passed: v0.5.0-alpha.8.5, 228 permanent requirement IDs, 25 Markdown files and all contradiction/security-register gates.
- [x] Canonical restore/build/test gate passed on .NET SDK 10.0.400 with 0 warnings, 0 errors and 278/278 tests (192 unit + 86 integration).
- [x] Package audit passed with ZIP filename, sole root folder, Version and InformationalVersion all `0.5.0-alpha.8.5`.
- [ ] Manual Windows 11 1920x1080/150% and larger-display DPI smoke acceptance remains required; automated gates do not claim visual proof.

## v0.5.0-alpha.8.4 Correct Entire Batch corrective build

- [x] Authoritative Markdown/current-state registers and the intentional dirty correction/reversal diff reviewed before changes.
- [x] Correct Entire Batch construction path, correction services, lineage, authorization, review, concurrency and rollback tests reviewed.
- [x] Current-state version surfaces updated without rewriting alpha.8.3 history.
- [x] BT-AUD-013 added uniquely as planned; existing BT-AUD-006..012 meanings preserved and no infobar implementation claimed.
- [x] Single Entry stable Save Movement wording and production-DPI size recorded/gated.

## v0.5.0-alpha.8.3 chained-correction history correction

- Reconciles Movement History with correction chains where one immutable movement is both an earlier replacement and a later correction original.
- Preserves complete lineage, effective-report semantics, blue correction/amber reversal presentation, export readiness, and full quick-range captions.
- Windows/manual acceptance remains open.

## v0.5.0-alpha.8.2 correction-report semantics and dialog layout

- Revalidated effective operational reporting against persisted correction lineage and preserved raw Movement History/Audit evidence.
- Added permanent BT-CORR-015 and source/test gates, clarified correction terminology, and recorded the deferred duplicate Container Type display-order issue.
- Advanced current candidate/version surfaces without claiming Windows/manual acceptance.

## v0.5.0-alpha.8.1 correction-dialog runtime fix

- Recorded the alpha.8 manual-smoke failure, exact pre-parent WinForms data-binding root cause, persistent-ID-safe fix, regression coverage and renewed manual acceptance boundary.
- Reconciled current version surfaces to alpha.8.1 without rewriting alpha.8 history.

## v0.5.0-alpha.8 movement correction reconciliation

- Reconciled correction semantics, persisted-batch identity, concurrency/idempotency, authorization, Administrator acknowledgement, Audit Trail detail, schema v16 and wrong-date report behavior across current-state documents.
- Added BT-CORR-007 through BT-CORR-014; post-v1 risk/period policy remains explicitly undecided and unimplemented.
- Windows/operator acceptance remains open; automated implementation status is not represented as manual acceptance.

## v0.5.0-alpha.6.5 Import History readability correction

- [x] User screenshot confirmed `Customers`, `Movements` and lower-grid `Direction` headings were clipped.
- [x] Import Run History client width increased from 1400 to 1520.
- [x] Customers widened to 125, Movements to 130 and Direction to 110.
- [x] Existing replacement/correction and historical-reconciliation behavior preserved.
- [x] Permanent source-audit readability gate added.
- [ ] Windows audit/build/full test rerun and visual smoke required.

## v0.5.0-alpha.6.4 projected-row compile correction

- [x] Alpha.6.3 source audit passed.
- [x] Windows compilation exposed three CS1061 errors in replacement-comparison movement counting.
- [x] Root cause: `proposedRows` is now a validated `{ Row, ContainerTypeId }` projection, while the movement-count expression still accessed original-row fields directly.
- [x] All three accesses corrected to `x.Row...`; BT-IMP-022 strengthened to preserve the projection contract.
- [ ] Windows source audit/build/full automated suite must be rerun against alpha.6.4 with 0 warnings / 0 errors.

## v0.5.0-alpha.6.3 BT-IMP-022 audit correction

- [x] Alpha.6.2 Windows source audit failed before restore/build on BT-IMP-022.
- [x] Exact audit predicate reproduced against exact alpha.6.2 source.
- [x] Failure isolated to stale `item.CurrentBinTrackerBalance` source-shape assertion after the nullable-safe projection change.
- [x] BT-IMP-022 now asserts the structural validation/projection and snapshot-consumption path without weakening provenance requirements.
- [x] Runtime alpha.6.2 implementation remains unchanged.
- [ ] Windows audit/build/full automated suite must be rerun against alpha.6.3.

## v0.5.0-alpha.6.2 zero-warning correction

- [x] Alpha.6.1 Windows source audit passed and all 245 automated tests passed.
- [x] Alpha.6.1 build exposed seven CS8629 nullable warnings in ImportExecutionService.
- [x] Each warning traced to nullable flow across LINQ lambdas.
- [x] Required values now pass through explicit validation/projection before use; no suppression or null-forgiving fix applied.
- [x] `TreatWarningsAsErrors` enabled and permanently source-audited.
- [ ] Windows audit/build/full test rerun required; candidate must produce 0 warnings / 0 errors.

## v0.5.0-alpha.6.1 schema-version test correction

- [x] Alpha.6 Windows build compiled with 0 warnings/0 errors.
- [x] Alpha.6 automated suite exposed three stale migration-test assertions expecting schema version 14 after migration v15.
- [x] All three assertions replaced with the derived `DatabaseSetup.LatestSchemaVersion`.
- [x] Permanent source-audit regression guard added.
- [x] Runtime alpha.6 implementation preserved unchanged.
- [ ] Windows source audit/build/full automated suite must be rerun against alpha.6.1.

## v0.5.0-alpha.6 import reconciliation provenance

- [x] Hard-gate Roadmap, Requirements register, Audit script, Architecture, Testing, Known Issues and current DB schema reviewed before implementation.
- [x] Controlled #6 → #7 Replace/Correct test proved current correction lineage/delta persistence works correctly.
- [x] DB trace proved normal later-cutover runs can generate opening adjustments without an immutable before/Excel/adjustment snapshot.
- [x] Schema migration v15 adds `OpeningReconciliationChangesJson`; no historical provenance is fabricated.
- [x] Import execution persists all future non-zero opening reconciliation rows.
- [x] Import History distinguishes normal opening reconciliation from Replace/Correct correction changes and exposes explicit capture state.
- [x] BT-IMP-022 added and hard-gated; BT-AUD-006 explicitly parked post-v1 for Audit Trail search/filter/export.
- [x] Stale breakout-report checklist/testing wording reconciled.
- [ ] Windows source audit, zero-warning build, full automated tests, migration-on-existing-DB smoke and Import History UI acceptance remain required.

## v0.5.0-alpha.5.6.4.2 controlled-document version audit

- [x] External review finding reproduced: current TEST-CHECKLIST baseline was correct while packaging gate was stale at alpha.5.5.3.
- [x] All Markdown version references inventoried and classified as current-state versus historical.
- [x] Current packaging gate made version-relative to Directory.Build.props.
- [x] Obsolete breakout-report/alpha.1.1 icon-trial instruction removed from the current checklist.
- [x] Historical release records intentionally retained.
- [x] Audit strengthened against recurrence.
- [x] No runtime source change; alpha.5.6.4.1 Windows build and focused Reports smoke evidence remains applicable.
- [ ] Windows source audit/build confirmation required for this documentation/audit-only corrective candidate.

## v0.5.0-alpha.5.6.4.1 BT-RPT-021 audit correction

- [x] Alpha.5.6.4 Windows source audit failed before build on BT-RPT-021.
- [x] Actual Weekly source inspected against the exact audit expression.
- [x] Runtime code confirmed to contain the required Source grouping, row-aware Notes state, tab refresh and post-load refresh.
- [x] Brittle audit expression replaced with explicit tab-change and load-finally assertions without weakening BT-RPT-021.
- [ ] Windows audit/build/test and focused alpha.5.6.4 behavior smoke remain required.

## v0.5.0-alpha.5.6.4 integrated Reports smoke corrections

- [x] Report smoke pass completed across Outstanding Containers, Daily Movements, Weekly Movements, Customer Statement, Monthly Summary and Movement History.
- [x] Outstanding/Daily/Monthly/Movement History integrated layouts accepted for this pass.
- [x] Weekly Source wrapping correction implemented.
- [x] Weekly Notes enablement state unified for reload and tab-change paths.
- [x] Customer Statement search cue polish implemented.
- [x] Reports left-nav reselect returns from detailed report to overview.
- [x] BT-RPT-002 wording reconciled; BT-RPT-020..021 added and hard-gated.
- [ ] Windows source audit/build/test and focused smoke confirmation remain required.

## v0.5.0-alpha.5.6.3 embedded report dead-space correction

- [x] Hard-gate Roadmap, Requirements register and source audit reviewed before code change.
- [x] Operator-highlighted top and bottom dead bands traced to retained standalone root padding and Close-only footer allocation.
- [x] Embedded root padding/margin removed.
- [x] Close-only footer rows collapse; shared action rows retain non-Close actions.
- [x] Duplicate large title rows collapse to zero while explanatory text remains.
- [x] BT-RPT-019 added and permanent source audit strengthened.
- [ ] Windows source audit/build/test and report visual smoke acceptance remain required.

## v0.5.0-alpha.5.6.2 embedded report header cleanup

- [x] Operator screenshot identified duplicate `Outstanding Containers` / `Outstanding Containers — As of Date` headings and wasted vertical space.
- [x] Explanatory sentence retained in the shell subtitle.
- [x] Legacy standalone title/header block suppressed for all newly embedded detailed reports.
- [x] Permanent Option-B source gate strengthened for this presentation contract.
- [ ] Windows build/test/audit and visual smoke acceptance remain required.

## v0.5.0-alpha.5.6.1 BT-HIST audit reconciliation

- [x] Alpha.5.6 Windows source audit correctly exposed a stale Movement History host-pattern assertion.
- [x] Root cause identified: Movement History now uses the shared Option-B `OpenEmbeddedReport(...)` host, while the older gate still required the removed typed-page launch implementation.
- [x] Gate updated to verify shared host invariants without weakening Movement History-specific layout/export protections.
- [ ] Windows source audit/build/test and integrated-report visual smoke acceptance remain required.

## v0.5.0-alpha.5.6 integrated Reports implementation

- [x] Hard-gate review of Roadmap and RequirementsAcceptanceRegister completed before implementation.
- [x] v1 Option B applied to Outstanding Containers, Daily Movements, Weekly Movements, Movement History, Customer Statement and Monthly Summary.
- [x] Reports landing page remains the hub; Market Floor remains inline.
- [x] Shared shell breadcrumb is the report return navigation.
- [x] Existing report business/export/audit behavior preserved; integration changes are hosting/chrome changes.
- [x] BT-RPT-001/018 and ReportsArchitectureTests reconciled.
- [ ] Windows source audit/build/test and per-report visual/interaction smoke acceptance remain required.

## v0.5.0-alpha.5.5.3 report-breadcrumb decision

- [x] Operator accepted the alpha.5.5.2 Movement History compact maximised layout and chose not to pursue Date truncation or narrow-window WinForms redesign now.
- [x] Replaced standalone `← Reports` row with shell-level `Reports › Movement History` breadcrumb.
- [x] Recorded v1 Option B: Reports landing remains the hub; detailed reports integrate into the main workspace with the shared breadcrumb.
- [x] Recorded Option C persistent Reports workspace as post-WinForms / WinUI 3 discussion scope.
- [x] Added BT-RPT-018 and BT-UI-014; BT-HIST gate updated for the new header/navigation structure.
- [ ] Alpha.5.5.3 Windows source audit/build/test and breadcrumb smoke acceptance remain required.

## v0.5.0-alpha.5.5.2 direct-row layout correction

- [x] Alpha.5.5.1 runtime screenshot proved the controls card still consumed excessive vertical space.
- [x] Removed the controls card entirely; Filters / Options / Actions / Summary are direct AutoSize root rows and Grid is the only Percent row.
- [x] BT-HIST audit updated to reject reintroduction of the failed controls-card structure.
- [ ] Alpha.5.5.2 Windows audit/build/test and compact-layout smoke acceptance remain required.

## v0.5.0-alpha.5.5.1 audit-gate correction

- [x] Alpha.5.5 Windows build stopped correctly because the BT-HIST source audit still required the removed alpha.5.4 calculated-height implementation.
- [x] Audit gate updated to enforce alpha.5.5 direct AutoSize/GrowAndShrink content-height layout instead of `controlsRowStyle` / `ResizeControlsCard`.
- [ ] Alpha.5.5.1 Windows source audit/build/test and compact-layout smoke acceptance remain required.

## v0.5.0-alpha.5.5 Movement History compact-height correction

- [x] Alpha.5.4 manual Windows smoke confirmed the action buttons are fully visible.
- [x] Alpha.5.4 notes-enabled Movement History PDF confirmed Status and optional Notes export together.
- [x] Alpha.5.4 manual UI smoke exposed an excessive blank band between the actions and summary/grid.
- [x] Alpha.5.5 removes the separately reserved controls-row height and uses direct AutoSize content height.
- [ ] Alpha.5.5 Windows source audit/build/test and compact-layout smoke acceptance remain required.

## v0.5.0-alpha.5.4 Movement History action-row correction

- [x] Alpha.5.3 Windows source audit passed with 204 permanent requirement IDs / 25 Markdown files.
- [x] Alpha.5.3 Windows build passed with zero warnings/errors and 242/242 automated tests.
- [x] Alpha.5.3 PDF smoke confirmed the new Status column; Notes was intentionally absent because Include notes in exports was unticked.
- [x] Alpha.5.3 manual UI smoke still showed the entire action row vertically clipped.
- [x] Alpha.5.4 replaces the nested auto-size measurement path with a direct structural TableLayoutPanel controls card; no broader report redesign is included.
- [ ] Alpha.5.4 Windows audit/build/test and manual action-row/DPI acceptance remain required.
- [ ] Re-test Movement History PDF with Include notes in exports ticked.

## v0.5.0-alpha.5.3 Movement History Windows-smoke correction

- [x] Preserved the integrated Movement History design after operator review.
- [x] Added explicit Back to Reports navigation instead of relying only on left navigation.
- [x] Corrected clipped action-row layout and rebalanced structured/flexible columns from the alpha.5.1 Windows screenshot.
- [x] Preserved BT-HIST-002..005, BT-ARCH-008..015 and security-register identities.
- [ ] Alpha.5.2 Windows source audit, zero-warning build, full tests, package audit and DPI/UI acceptance remain required.

## v0.5.0-alpha.5.1 warning-gate correction

- [x] Recorded alpha.5 as built with 242/242 automated tests passing but blocked from release by two `CS8602` warnings.
- [x] Issued a corrective alpha.5.1 candidate rather than overwriting the delivered alpha.5 artifact.
- [x] Preserved BT-HIST-002..005, BT-ARCH-008..015 and all security/requirements register identities.
- [x] Alpha.5.1 Windows source audit/build/test gate passed with zero warnings, zero errors and 242/242 automated tests; manual UI smoke then found the action-row/column-allocation defects corrected in alpha.5.2.

## v0.5.0-alpha.5 Movement History integration

- [x] Recorded v0.5.0-alpha.4 manual acceptance as 8/8 smoke tests passed before follow-up source changes.
- [x] Reconciled Movement History's intentional full-size main-workspace exception across Roadmap, Requirements, Business Rules, Functional Specification, Testing, Technical Debt, Release Notes and active checklist.
- [x] Added permanent BT-HIST-002..005 requirements plus source-audit protection for integrated hosting, responsive columns/minimums, badges/tooltips and customer-code PDF/CSV filename rules.
- [x] Preserved BT-ARCH-008..015 architecture requirements and the Security Hardening register without renumbering or dropping IDs.
- [ ] Windows restore/build/test/package audit and manual maximized-width/DPI acceptance remain outstanding until executed on a suitable Windows/.NET host.

## v0.5.0-alpha.4 selective branch comparison

- Inspected the actual attached alternate alpha.3 source rather than relying on its summary.
- Reconciled the selective concurrency/portability merge against the controlled Markdown set, requirement register and BT-ARCH-008..015 hard gate.
- Preserved Work's reversal Status/Source UX and packaging protections; documented schema V14, content transport, host composition, payload identity and PostgreSQL verification limits.
- Promoted the merged candidate to alpha.4 because source, schema and requirements changed.

Audit date: 16 August 2026

Baseline audited: v0.4.0-alpha.21.5.1

Resulting documentation baseline: v0.4.0-alpha.21.5.2

All Markdown files in the package were reviewed. Historical changelog entries are preserved as historical truth; current-state documents were corrected where stale or contradictory.

## Files reviewed

- [x] `KNOWN-ISSUES.md`
- [x] `README.md`
- [x] `TECH-DEBT.md`
- [x] `TEST-CHECKLIST.md`
- [x] `docs/AuditCoverage.md`
- [x] `docs/BalanceReconciliation.md`
- [x] `docs/BusinessRules.md`
- [x] `docs/CHANGELOG.md`
- [x] `docs/Database.md`
- [x] `docs/DevelopmentWorkflow.md`
- [x] `docs/FunctionalSpecification.md`
- [x] `docs/ImportWizard.md`
- [x] `docs/LegacyContainerRules.md`
- [x] `docs/MasterData.md`
- [x] `docs/RELEASE-NOTES.md`
- [x] `docs/ReimportSafety.md`
- [x] `docs/Roadmap.md`
- [x] `docs/RoadmapCoverageMatrix.md`
- [x] `docs/Testing.md`
- [x] `docs/Versioning.md`

## Material corrections

- Weekly Movements status/behaviour reconciled across current-state docs.
- One `Include notes in exports` control is the only current Weekly Notes semantic documented.
- Current version references reconciled with `Directory.Build.props`.
- Roadmap execution order made authoritative and duplicate backup planning removed.
- Existing textual Default Report Header distinguished from future logo/shared branding.
- Import Replace/Correct/provenance/history status explicitly recorded.
- Roadmap coverage matrix retained as the major-workstream completeness guard.


## Ongoing requirement

This file is not a one-off audit artifact. Every packaged build must append or refresh an audit record confirming that all Markdown/current-state files were reviewed for that candidate. The audit is part of the definition of done.


## Candidate: v0.4.0-alpha.21.5.2

- [x] 21 Markdown files enumerated/reviewed.
- [x] Mandatory audit-gate documentation reconciled.
- [x] Roadmap Coverage Matrix updated.
- [x] Version references reconciled.
- [x] Release Notes and Changelog updated.
- [x] Runtime behaviour unchanged.


## Candidate: v0.4.0-alpha.22

- [x] 21 Markdown files enumerated/reviewed.
- [x] Movement History implementation/status reconciled across README, Known Issues, Roadmap, Coverage Matrix, specification, business rules and testing.
- [x] Current version references reconciled with `Directory.Build.props`.
- [x] Changelog and Release Notes updated.
- [x] Test requirement classified as **full smoke test**.


## Candidate: v0.4.0-alpha.22.1

- [x] 21 Markdown files enumerated/reviewed.
- [x] Live-filter report interaction standard reconciled across current-state documentation.
- [x] Roadmap Coverage Matrix reviewed; no agreed workstream removed.
- [x] Version references reconciled with `Directory.Build.props`.
- [x] Changelog and Release Notes updated.
- [x] Test requirement classified as **full smoke test**.


## Candidate: v0.4.0-alpha.22.2

- [x] 21 Markdown files enumerated/reviewed.
- [x] Weekly wrapped-layout fix and Customer Enter cue reconciled across UI standard documentation.
- [x] BinTracker product icon/logo documented separately from business branding.
- [x] Roadmap Coverage Matrix reviewed; no agreed workstream removed.
- [x] Version references reconciled with `Directory.Build.props`.
- [x] Changelog and Release Notes updated.
- [x] Test requirement classified as **full smoke test**.


## Candidate: v0.4.0-alpha.22.3.1

- [x] 21 Markdown files enumerated/reviewed.
- [x] Weekly wrapped-layout defect and structural fix documented.
- [x] Roadmap Coverage Matrix reviewed; no agreed workstream removed.
- [x] Version references reconciled.
- [x] Changelog / Release Notes / Testing / Test Checklist / Tech Debt reconciled.
- [x] Test requirement classified as **full smoke test**.


## Candidate: v0.4.0-alpha.22.3.2

- [x] 21 Markdown files enumerated/reviewed.
- [x] Application icon/sidebar branding behaviour reconciled across specs/business rules/testing/roadmap/tech debt.
- [x] Roadmap Coverage Matrix reviewed; no agreed workstream removed.
- [x] Version references reconciled.
- [x] Changelog and Release Notes updated.
- [x] Test requirement classified as **full smoke test**.


## Candidate: v0.4.0-alpha.22.3.3

- [x] Source tree and Markdown/current-state documentation reviewed.
- [x] Weekly Movements reclaimed-grid-space change documented.
- [x] Sidebar circular transparent product-logo treatment documented.
- [x] Startup splash implementation documented.
- [x] Roadmap/current workstreams reviewed; no agreed workstream removed.
- [x] Version references reconciled with `Directory.Build.props`.
- [x] Changelog and Release Notes updated.
- [x] Test requirement classified as **full smoke test**.


## Candidate: v0.4.0-alpha.22.3.4

- [x] 21 Markdown files enumerated/reviewed.
- [x] Sidebar branding regression documented.
- [x] Roadmap Coverage Matrix reviewed; no agreed workstream removed.
- [x] Version references reconciled.
- [x] Changelog / Release Notes / Testing / Test Checklist / Tech Debt reconciled.
- [x] Test requirement classified as **targeted UI smoke test**.


## Candidate: v0.4.0-alpha.22.5

- [x] 21 Markdown files enumerated/reviewed.
- [x] Customer Statement generate/save/open workflow documented.
- [x] Statement future-date restriction documented.
- [x] Roadmap/current-state workstreams checked: Movement Correction, SMS, Email, Dashboard, WinUI 3, branding.
- [x] Changelog / Release Notes / Testing / Test Checklist reconciled.
- [x] Test requirement classified as **targeted functional/UI test**.


## Candidate: v0.4.0-alpha.22.6

- [x] 21 Markdown files enumerated/reviewed.
- [x] Customer Statement Reports entry point and shared workflow reconciled.
- [x] Roadmap/coverage updated to mark Customer Statement workflow complete.
- [x] Major workstreams retained: Movement Correction, SMS, Email, Dashboard, WinUI 3, branding, Monthly Summary, Daily Print Pack.
- [x] Changelog / Release Notes / Testing / Test Checklist / Tech Debt reconciled.
- [x] Test requirement classified as **full smoke test**.


## Candidate: v0.4.0-alpha.22.6.1

- [x] 21 Markdown files enumerated/reviewed.
- [x] Compile defect and owner-interface fix documented.
- [x] Roadmap Coverage Matrix reviewed; no agreed workstream removed.
- [x] Version references reconciled.
- [x] Changelog / Release Notes / Testing / Test Checklist / Tech Debt reconciled.
- [x] Test requirement classified as **automated build gate + existing Customer Statement smoke test**.


## Candidate: v0.4.0-alpha.23

- [x] 21 Markdown files enumerated/reviewed.
- [x] Monthly Summary implementation/status reconciled across README, Known Issues, Roadmap, Coverage Matrix, Functional Specification, Business Rules and Testing.
- [x] PostgreSQL/provider-neutral report boundary documented.
- [x] Agreed workstreams retained: Movement Correction, SMS, Email, Dashboard, WinUI 3, branding, Daily Print Pack, PostgreSQL.
- [x] Changelog and Release Notes updated.
- [x] Test requirement classified as **full smoke test**.


## Candidate: v0.4.0-alpha.23.1
- [x] 21 Markdown files enumerated/reviewed.
- [x] Both Windows failures diagnosed as new integration-test fixture/setup defects.
- [x] Production date behaviour retained.
- [x] Changelog / Release Notes / Test Checklist reconciled.
- [x] Test requirement remains full smoke test after automated suite passes.


## Candidate: v0.4.0-alpha.23.2
- [x] 21 Markdown files enumerated/reviewed.
- [x] CSV audit gap audited across all current CSV-capable reports.
- [x] Export auditing rules added to Functional Specification and Business Rules.
- [x] Monthly Summary year-picker regression included in acceptance checks.
- [x] Roadmap workstreams retained.
- [x] Test requirement classified as full report-export smoke test.


## Candidate: v0.4.0-alpha.23.4

- [x] 21 Markdown files enumerated/reviewed.
- [x] alpha.23.3 SDK-pinning mistake documented and corrected.
- [x] BAT false-success failure path documented and corrected.
- [x] Useful MSBuild worker-resilience changes retained.
- [x] Agreed roadmap workstreams retained: Movement Correction, SMS, Email, Dashboard, WinUI 3, branding, Daily Print Pack, PostgreSQL.
- [x] Changelog / Release Notes / Development Workflow / Testing / Test Checklist / Tech Debt reconciled.


## Candidate: v0.4.0-alpha.23.5.1

Full audit performed after alpha.23.5 packaging drift was reported.

Findings corrected:
- [x] alpha.23.5 ZIP/root/Version/InformationalVersion identity was checked directly.
- [x] `docs/RELEASE-NOTES.md` was stale at alpha.23.4.1 and replaced.
- [x] `docs/Roadmap.md` planning baseline was stale at alpha.22.2 and reconciled.
- [x] `KNOWN-ISSUES.md` current release was stale at alpha.22.2 and reconciled.
- [x] Historical-requirements section incorrectly still marked Movement History, Monthly Summary and Daily Print Pack pending; reconciled.
- [x] Audit Coverage omitted Movement History, Monthly Summary, Daily Print Pack and CSV exports; reconciled.
- [x] Development Workflow described obsolete BAT subroutine handling; reconciled to direct command guards.
- [x] Inline Market Floor / Daily Print Pack selectors lacked the future-date UI guard used by breakout reports; fixed.
- [x] 21 Markdown files enumerated and reviewed as current-state/historical documents.
- [x] Major pre-v1/post-v1 workstreams retained: Batch Entry cleanup, Movement Correction, Branding, Email/SMS, Dashboard design gate, Backup/Restore, PostgreSQL readiness, installer/acceptance, post-v1 WinUI 3.
- [x] Mechanical source audit added to stop current-version-document drift before build.
- [x] Package identity gate documented as mandatory.

Test classification: **full report smoke/print test** after Windows automated build/test gate.


## Historical audit-log repair — alpha.23.5.2

The reconciliation found candidate headings that had been corrupted by prior broad version-string replacement. Headings were repaired only where the section content and archived build chronology identified the candidate unambiguously: sidebar branding → alpha.22.3.4; Customer Statement generate/open → alpha.22.5; compile owner fix → alpha.22.6.1; CSV audit pass → alpha.23.2; SDK/build correction → alpha.23.4. The Customer Statement Reports-entry section remains alpha.22.6. No unverified historical claim was invented.

## Candidate: v0.4.0-alpha.24.2.1

- [x] Reconciliation inspected current source/docs plus 166 archived BinTracker ZIPs and all BinTracker history surfaced in the current conversation context.
- [x] Personal-context history query returned no additional BinTracker entries; raw complete chat transcript access was not claimed.
- [x] Permanent Requirements & Acceptance Register created with stable IDs/scope/status/provenance.
- [x] Active Test Checklist rebuilt; historical alpha-specific blocks removed after permanent behaviors were migrated.
- [x] Stale SDK, importer and tech-debt contradictions corrected.
- [x] Historical DocumentationAudit headings repaired where archived chronology/content made identity unambiguous.
- [x] Audit gate strengthened and packaging identity verifier added.
- [x] Lost detailed post-v1 import requirements restored.
- [x] Current package identity mechanically verified before delivery.


## Candidate: v0.4.0-alpha.24.2.1
- [x] Containers navigation decision reconciled into the permanent Requirements & Acceptance Register.
- [x] Permission compromise implemented: view for all roles; mutations Administrator-only.
- [x] Settings duplication removed.
- [x] Unsaved-change navigation protection preserved.
- [x] 176 unique permanent requirement IDs verified.
- [x] Package/version metadata gate required before handoff.


## Candidate: v0.4.0-alpha.24.2.1
- [x] False BT-CT-005 audit failure root cause identified as undefined `$requirementsText`.
- [x] Requirement presence checks now use parsed `$reqRows.Id`.
- [x] BT-UI-013 is also explicitly guarded by the mechanical gate.
- [x] Version/package metadata reconciled.
