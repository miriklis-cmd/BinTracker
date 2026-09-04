# BinTracker Active Test Checklist

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

- [G] Before accepted behaviour changes, identify precise characterization; add missing coverage, run it before the change and rerun it afterward.
- [G] Structured input/persisted-state boundaries have relevant malformed/adversarial coverage and fully validate or fail closed without partial state.
- [G] Perform semantic reconciliation of every governed Markdown file and record the review separately from the mechanical audit.
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
- [P] After lineage activation, eligible Single/Batch saves atomically create complete generation-zero lineage or nothing.

## Excel import and re-import

- [R] Fresh real-workbook import and populated-database merge reconcile Blue/Yellow/Bulk balances.
- [R] Analyse/Map/Review are read-only; unknown tokens and create/skip/match decisions remain explicit and stable through navigation.
- [R] Exact source cannot import twice; post-preflight file change is rejected.
- [A] Changed workbook/same cutover exposes Replace/Correct before execution.
- [S] Replace/Correct preserves legitimate same-day/later Manual/Batch activity outside replaced import-generated movements.
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
- [P] The future lineage numerical cutover must implement and prove Opening = `PositionAsOf(StartDate - 1)`, inclusive StartDate..EndDate corrected activity and Closing = `PositionAsOf(EndDate)` without StartDate double counting.
- [R] Daily Print Pack contains Outstanding Summary plus physical Movement Detail in one audited, readable PDF.
- [P] Decide whether native Excel report export adds enough value beyond CSV.

## Alpha.8 correction, reversal and Administrator review authority

- [S] Dormant IMP-04/04A resolves one requested schema-17 CURRENT root under one SQLite read transaction and exposes non-forgeable read-only success models only after exact membership, current-pointer/role/introduction, RootOriginal physical-batch/null-single and ordinal validation. Unrelated historical-only links remain migration/diagnostic scope; `StatusReasonCode` is preserved. It remains unregistered with no production consumer.

- [R] Administrator/Operator may reverse or correct eligible ordinary Manual/Batch movements; Viewer is denied; Opening Adjustment/ImportRun rows remain outside generic mutation.
- [R] Original evidence is immutable; reversal and correction evidence/reason/actor/time/idempotency/concurrency/audit remain transactional.
- [R] Alpha.8 physical whole-batch guard remains active and rejects partial lineage; `EffectiveMovementQuery` remains current runtime authority.
- [A] Movement History selection/action synchronization (BT-CORR-016) and whole-batch auto-select/clear/no-op/focus behavior (BT-CORR-017) remain accepted.
- [A] Administrator review state/filtering, acknowledgement, end-to-end Operator review, persistent reminder and Esc hierarchy remain accepted (BT-AUD-007/008/011/013/014).
- [R] Alpha.8.7 acknowledgement drill-through/differences/readability, Movement History wrapping and batch-dialog DPI layout need current Windows retest.
- [R] Audit detail uses authoritative movement/batch identity and fails closed; BT-AUD-009/010 current-candidate extensions remain pending acceptance.
- [P] Broader Audit Trail search/general filtering/CSV export remains a release decision, not an implemented claim.

## Dormant lineage foundation and remaining cutover

- [S] Dormant Core contracts pin lineage vocabulary and persisted enum values; no runtime authority changed.
- [S] Read-only preflight, exact-source verified recovery artifact, exclusive upgrade lease and recovery classification exist under isolated automated tests; no operator acceptance is claimed.
- [S] Dormant schema-16→17 DDL/backfill/postflight creates truthful complete MigrationBaseline state under isolated automated tests and leaves normal startup at schema 16; the foundation is externally reviewed/approved while production activation remains pending.
- [S] Migration-publication postflight remains strictly MigrationBaseline-only, while the dormant schema-17 AlreadyComplete path separately reuses current-root validation so valid native Initial roots are accepted and malformed native current lineage is rejected without activating schema 17.
- [S] Automated migration evidence proves schema-16 operation Kind outside historical 0/1 blocks before mutation and can never become schema-17 Reverse/Restore.
- [P] Activate the upgrade coordinator only after explicit approval and safe production rehearsal; do not auto-restore over a valid rolled-back source.
- [S] IMP-07 implements generation-zero Single/Batch lineage atomically behind explicit isolated schema-17 composition. Targeted evidence proves exact native roots/lines/order, retry/conflict behavior, fail-closed schema/health checks and rollback at physical, lineage, introduction-link and audit boundaries. Independent review approved this source/evidence level. Normal composition still uses the schema-16 dormant no-op writer; activation, retained-database rehearsal, canonical BAT for this slice and operator acceptance remain pending.
- [S] IMP-05C-corrected dormant planning implements the infrastructure-internal trusted snapshot and pure provider-neutral complete-generation planner, including explicit per-reversed-line whole-root decisions, complete existing/plan-local result pointers, exact persisted equal/opposite ordinary-reversal pair proof, current-pair business-date guards, generic import/adjustment exclusion, exact AppliedFieldMask rules, complete no-op and the frozen physical-output predicate. Independent external source/diff review approved this dormant planning boundary; canonical `Build-BinTracker.bat` then passed at v0.5.0-alpha.8.7 with source/package-state audit, restore and build PASS, 438/438 automated tests passed, 0 failed and 0 skipped, and no reported compiler warnings/errors. Checkpoint `c0dfc7e51cae1296fd5a5da31876e54364901405` (`Add trusted movement mutation planner`) is pushed and synchronized with `origin/codex/movement-correction`. It remains unregistered/write-free and schema 17 remains dormant; this static/automated/external-review evidence does not imply Windows/operator acceptance or a retained-production-database migration rehearsal.
- [S] IMP-06 adds the unregistered persistence-boundary primitive that tracks exactly one new primary AuditEvent in a caller-owned DbContext/active transaction without creating a context/transaction or saving/committing. Focused automated coverage proves caller commit, rollback of both audit and sibling state saved in the same transaction, missing-transaction rejection and unchanged independent `AuditService.WriteAsync`. Dormant schema-17 tests separately prove unique structured association, the RESTRICT operation FK and rejection of a second primary audit for one operation. Independent review approved the bounded implementation/proof; canonical `Build-BinTracker.bat` passed source/package-state audit, restore and Debug build with 0 warnings/errors plus 266 UnitTests and 177 IntegrationTests (443/443 total, 0 failed/skipped). Production operation/audit wiring and operator acceptance remain pending.
- [P] Implement client-neutral Correct/Reverse/Restore/whole-root command persistence, root CAS/idempotency and publication of the already planned generation using the caller-transaction audit primitive.
- [P] Atomically cut all operational numeric consumers to validated corrected activity/PositionAsOf while Movement History/Audit remain evidence.
- [P] Prove Invalid/unrooted fail-closed numeric behavior and audit-only corruption separation.
- [P] Run required root races, retries/lost response, import collision and transaction failure-injection acceptance.
- [P] Retained Batch #30 passes RemainReversed, Restore, mixed dates, repeated correction, navigation/details/audit/review, reports/balances and Windows/DPI acceptance.
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
