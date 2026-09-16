# BinTracker Active Test Checklist

Task 19's shared-snapshot prerequisite was reviewed/canonically verified at commit `930f2c77b1df7cd4629be2faff662750431854fe`; BT-20-P2 now includes that authority in normal schema17 composition. Windows/operator real preview/print and retained-database acceptance remain separate.

Current baseline: **v0.5.0-alpha.8.7**

This is the practical operator/release checklist. `docs/RequirementsAcceptanceRegister.md` remains the authoritative implementation/status ledger; historical candidate detail belongs in `docs/CHANGELOG.md` and `docs/DocumentationAudit.md`.

Status legend:

- `[A]` — operator/manual accepted. Explicit recorded human acceptance exists for every behavior represented by the checklist line.
- `[S]` — implemented/static/automated evidence exists, but no manual acceptance is claimed unless separately stated. This includes internal or dormant implementation for which manual UI testing is not the relevant gate.
- `[R]` — implementation exists, but a specific current manual/operator/Windows/DPI/preview/print/real-workbook retest remains required.
- `[P]` — genuinely pending, incomplete or not yet implemented.
- `[G]` — repeat for every applicable candidate/release; a previous pass is historical evidence only.

Jack reported the v0.5.0-alpha.4 baseline smoke set accepted (8/8). Later candidate-specific changes retain their own `[R]` status until actually retested.

## Release, audit and packaging

- [S] Task20C R1–R4 documentation freeze records the approved activation semantics without production implementation or requirement-status promotion.
- [S] BT-20-P2 atomically activates the reviewed Task20D/E/F/P1/FIX1 seams in normal composition: one Data coordinator, catalogue17, native entry/receipt/mutation writers, validated projection consumers, logical WinForms correction/reversal routes, retained runtime session and coordinated developer Load/Fresh markers. Focused activation8/8 and canonical audit/restore/zero-warning build plus UnitTests279/279 and IntegrationTests491/491 (770 total) pass with zero failures/skips; retained-data and Windows/operator acceptance remain separate.
- [S] Startup/identity/bootstrap/Load/Fresh A2/A3/A8–A11/A13–A15, schema17 structural/current health, R1/R2/R3/EF/R7 and schema16 compatibility remain ordinary automated coverage. Full native detail, Restore UI and later acceptance are not implied.

- [G] Before accepted behaviour changes, identify precise characterization; add missing coverage, run it before the change and rerun it afterward.
- [G] Structured input/persisted-state boundaries have relevant malformed/adversarial coverage and fully validate or fail closed without partial state.
- [G] Perform semantic reconciliation of every governed Markdown file and record the review separately from the mechanical audit.
- [G] Validate the substantive Codex prompt's matching canonical working/return markers before work; reconcile each promoted status from authoritative criterion through enforcing path and proof.
- [G] `Audit-BinTracker.ps1` passes its mechanical source/governance checks.
- [G] `Build-BinTracker.bat` reports the current version/resolved SDK, restores, builds with zero warnings/errors and passes every unit/integration test without skips.
- [G] A failed restore/build/test cannot continue to `BUILD SUCCESSFUL`.
- [G] When packaging is authorised, `Package-BinTracker.ps1` verifies ZIP filename/root/Version/InformationalVersion against the current `Directory.Build.props` Version and excludes unexpected `global.json`.

## Authentication, users and shell

- [R] First-run Administrator, login/logout/login-again, failed-login lockout/unlock and password/role/active workflows work on the current candidate.
- [R] Password fields start masked and supported eye controls reveal/re-hide correctly.
- [R] Audit Trail and Settings actions respect roles at UI and service boundaries.
- [R] Login, Main, integrated reports and dialogs use the BinTracker icon; taskbar branding is present before login.
- [R] Startup splash shows BinTracker branding/version and exits without artificial delay (BT-UI-003 is implemented-static; current-candidate visual retest remains pending).
- [A] Sidebar logo and full BinTracker wordmark remain aligned and unclipped.

## Customers, containers and business information

- [R] Customer code/name search and no-result clearing work; dirty edits prompt Save/Discard/Cancel on all navigation/logout/close paths.
- [R] Balances remain separated by Container Type; Current Position/Recent History remain usable without a large blank band.
- [R] Duplicate container name/short code is rejected; rename preserves history; display order and inactive-history rules work.
- [R] Container create/update/deactivate/reactivate and Special Floor changes are audited and role-protected.
- [R] Business Information saves/reloads/audits; report header fallback is Default Report Header → Trading Name → Business Name → BinTracker.
- [P] Business logo storage and shared generated-output branding.

## Single and Batch Entry

- [R] Single Entry lookup/validation/preview, atomic save/audit, Viewer denial, `Ctrl+Enter` and post-save reset/focus work.
- [A] Batch `Ctrl+Enter`, Tab/Shift+Tab, same-process draft retention and post-add reset/focus remain accepted.
- [S] Batch pending Current/With Draft preview is implemented; no separate manual acceptance is claimed.
- [R] Batch Enter/add/update/remove/Esc transitions never duplicate or resurrect a stale selected row; Dashboard navigation/highlight and retained draft remain correct.
- [R] Crash/close recovery offers Continue Batch / Save Batch / Discard Batch in order; Save/Discard/Clear remove persisted state.
- [R] Cancellation/late async loads cannot restore Update Line after Esc/Clear.
- [S] After schema17 readiness, normal eligible Single/Batch saves atomically create complete generation-zero lineage or nothing; native Single also persists its typed response receipt. Windows/operator acceptance remains pending.

## Excel import and re-import

- [R] Fresh real-workbook import and populated-database merge reconcile Blue/Yellow/Bulk balances.
- [R] Analyse/Map/Review are read-only; unknown tokens and create/skip/match decisions remain explicit and stable through navigation.
- [R] Exact source cannot import twice; post-preflight file change is rejected.
- [A] Changed workbook/same cutover exposes Replace/Correct before execution.
- [S] Replace/Correct comparison and execution preserve legitimate same-day/later activity outside the pre-cutover baseline; normal schema17 composition uses corrected projection in the caller-owned serializable execution transaction while retaining physical previous-run evidence until deliberate deletion. Ordinary new import preserves its whole-ledger temporal scope through `DateOnly.MaxValue`; projection failure/cancellation leaves no import write or raw fallback.
- [R] Import History exposes source/SHA/cutover/user/count/status/replacement/linked movement and separate opening-reconciliation/correction evidence truthfully.
- [R] Forced post-SaveChanges failure rolls back and allows exact-source retry; non-Administrator history access is denied.
- [P] Before v1, execution failures identify useful row/customer/container context.

## Reports and statements

- [A] Market Floor generates the accepted two-page duplex front/reverse output.
- [S] Market Floor Account/Cash/credit grouping, Blue implicit/Yellow explicit treatment and opening-adjustment B/Fwd semantics are implemented; no manual acceptance is claimed for those rules.
- [P] Complete a genuinely high-Yellow-day Market Floor density stress pass.
- [R] Reports hub keeps Market Floor inline and opens each detailed report as one integrated main-workspace page with `Reports › <Report Name>` navigation.
- [R] Integrated pages fit laptop/large-monitor working areas; filters/actions stay visible and grids use remaining space.
- [R] Interactive filters, Customer-on-Enter, typed multi-sort/indicators and PDF/CSV displayed-order preservation work without layout shifts.
- [A] Outstanding multi-column sorting and readable balance selector remain accepted; current-candidate report regressions still use `[R]` above.
- [R] Outstanding, Daily, Weekly and Movement History filters/date semantics/totals/audited exports remain correct; Movement History remains forensic and preserves persisted IDs.
- [A] Monthly Summary month/range/totals/filters/PDF/CSV workflow remains accepted.
- [A] Customer Statement shared Customers/Reports workflow, date guard, Generate PDF/Open and printable output remain accepted.
- [S] Existing Customer Statement running balances reconcile opening, movement and closing positions by container.
- [S] Normal schema17 Customer Statement uses Opening = `PositionAsOf(StartDate - 1)`, inclusive StartDate..EndDate corrected activity and Closing = `PositionAsOf(EndDate)` without StartDate double counting; Windows/real-PDF acceptance remains pending.
- [R] Daily Print Pack contains Outstanding Summary plus physical Movement Detail in one audited, readable PDF.
- [S] Normal schema17 Daily Print Pack uses Task 19's one-snapshot Outstanding/detail/metadata composition; deterministic correction/reversal/restoration interleavings remain covered and projection failure/cancellation fails the pack. Real preview/print and Windows/operator acceptance remain separate and unproven.
- [P] Decide whether native Excel report export adds enough value beyond CSV.

## Activated correction, reversal and Administrator review authority

- [S] Normal native mutation/projection paths reuse IMP-04/04A CURRENT-root validation under one SQLite read transaction and expose non-forgeable read-only success models only after exact membership, current-pointer/role/introduction, RootOriginal physical-batch/null-single and ordinal validation. Unrelated historical-only links remain migration/diagnostic scope; `StatusReasonCode` is preserved.

- [R] Administrator/Operator may reverse or correct eligible ordinary Manual/Batch movements; Viewer is denied; Opening Adjustment/ImportRun rows remain outside generic mutation.
- [R] Original evidence is immutable; reversal and correction evidence/reason/actor/time/idempotency/concurrency/audit remain transactional.
- [S] Activated whole-root preview/execution requires every current line Active; all alpha.8 mutation writes fail closed and validated native projection is the sole normal numeric authority. Explicit schema16 compatibility fixtures retain the former guard/query behavior.
- [A] Movement History selection/action synchronization (BT-CORR-016) and whole-batch auto-select/clear/no-op/focus behavior (BT-CORR-017) remain accepted.
- [A] Administrator review state/filtering, acknowledgement, end-to-end Operator review, persistent reminder and Esc hierarchy remain accepted (BT-AUD-007/008/011/013/014).
- [R] Alpha.8.7 acknowledgement drill-through/differences/readability, Movement History wrapping and batch-dialog DPI layout need current Windows retest.
- [R] Audit detail uses authoritative movement/batch identity and fails closed; BT-AUD-009/010 current-candidate extensions remain pending acceptance.
- [P] Broader Audit Trail search/general filtering/CSV export remains a release decision, not an implemented claim.

## Activated lineage foundation and remaining acceptance

- [S] Core contracts pin lineage vocabulary and persisted enum values; normal runtime composes their Data adapters only after schema17 readiness.
- [S] Read-only preflight, exact-source verified recovery artifact, exclusive upgrade lease and recovery classification are active normal migration prerequisites; retained production rehearsal/operator acceptance is still pending.
- [S] Normal schema16→17 migration creates truthful complete MigrationBaseline state through the coordinator; direct legacy callback execution of catalogue17 fails closed.
- [S] Migration-publication postflight remains strictly MigrationBaseline-only, while schema17 re-entry separately reuses current-root validation so valid native Initial/later roots are accepted and malformed native current lineage is rejected.
- [S] Automated migration evidence proves schema-16 operation Kind outside historical 0/1 blocks before mutation and can never become schema-17 Reverse/Restore.
- [S] Normal startup uses the upgrade coordinator and never auto-restores over a valid rolled-back source; safe retained-production rehearsal remains pending.
- [S] IMP-07 generation-zero Single/Batch lineage is now normal schema17 behavior. Exact native roots/lines/order, retry/conflict, fail-closed health and rollback boundaries remain covered; retained-database rehearsal and operator acceptance remain pending.
- [S] The reviewed IMP-05C trusted snapshot and pure provider-neutral complete-generation planner are consumed by normal native mutation while remaining independently write-free. Their reversal-pair, business-date, import exclusion, field-mask, no-op and physical-output proofs remain green; Windows/operator and retained-data evidence remain separate.
- [S] Normal native mutation uses IMP-06's caller-transaction primary-audit primitive; focused coverage retains commit/rollback, missing-transaction, unique association and unchanged independent `AuditService.WriteAsync` proofs. Full native detail/operator acceptance remains pending.
- [S] Normal native Correct/Reverse/Restore persistence publishes the planned complete generation atomically with exact generation-line identities, root CAS, replay/idempotency, optional physical output and one primary audit. Target-root audit health and transient-contention failure classification remain covered. Restore UI, retained-database rehearsal and operator acceptance remain pending.
- [S] Normal correction/reversal uses Task20F's client-neutral logical preview bridge, preview-captured expected generation and root-wide CAS; stale previews fail without artifacts, alpha.8 writes reject, and whole-root correction requires every line Active. Restore/RemainReversed UI remains pending.
- [S] The provider-neutral corrected activity/PositionAsOf authority is normal numeric authority across balances, reports, statements, Dashboard, customer/container reads, entry responses, import reconciliation and Print Pack. Relevant invalid/incomplete/unrooted evidence fails without raw fallback; explicit schema16 compatibility composition remains separate.
- [S] Audit-only corruption remains mathematically separate: otherwise proven projection can continue, while affected mutation/review/evidence completeness fails closed.
- [P] Run required root races, retries/lost response, import collision and transaction failure-injection acceptance.
- [P] Retained Batch #30 passes RemainReversed, Restore, whole-root and selected correction, mixed dates, repeated correction, evidence navigation, immutable Batch Detail, Movement History, Audit Detail/Administrator Review, Daily/Weekly/Monthly corrected activity, current/Outstanding positions and Windows/DPI acceptance. It remains protected from mutation/deletion; verify its persisted relationships read-only before using approximate display IDs.
- [P] After lineage acceptance, complete the protected whole-codebase layer audit before later major work.

## Remaining pre-v1 product and release gates

- [P] Dashboard design discussion precedes implementation; then validate KPIs, attention/drill-through, activity, summaries, charts/ageing and display layouts.
- [P] Google Workspace email + Texto SMS delivery, templates, secure settings, manual/automatic sends, opt-out, history, retry/idempotency and audit.
- [P] Production user-facing Backup/Restore, scheduled retention and recovery drill (separate from pre-lineage migration safety).
- [P] Security/Data Integrity/Code Quality hardening and all required BT-SH dispositions.
- [P] Installer/upgrade/deployment acceptance and full v1 production regression.

## Current manual acceptance floor

- [G] Windows 11, 1920x1080 at 150%: required actions remain reachable; no harmful clipping/overlap; report grids remain usable.
- [G] Recheck materially changed UI on the substantially larger production display.
- [G] Reports/printing changes receive real PDF preview/print evidence; importer behavior depending on the production workbook receives a real-workbook pass.
- [G] Automated success never marks a manual/DPI/preview/print item accepted.
