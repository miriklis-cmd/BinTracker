# BinTracker Active Test Checklist

Task 19's shared-snapshot prerequisite was reviewed/canonically verified at commit `930f2c77b1df7cd4629be2faff662750431854fe`; BT-20-P2 now includes that authority in normal schema17 composition. Windows/operator real preview/print and retained-database acceptance remain separate.

Current baseline: **v0.5.0-alpha.13**

This is the practical operator/release checklist. `docs/RequirementsAcceptanceRegister.md` remains the authoritative implementation/status ledger; historical candidate detail belongs in `docs/CHANGELOG.md` and `docs/DocumentationAudit.md`.

Status legend:

- `[A]` — operator/manual accepted. Explicit recorded human acceptance exists for every behavior represented by the checklist line.
- `[S]` — implemented/static/automated evidence exists, but no manual acceptance is claimed unless separately stated. This includes internal or dormant implementation for which manual UI testing is not the relevant gate.
- `[R]` — implementation exists, but a specific current manual/operator/Windows/DPI/preview/print/real-workbook retest remains required.
- `[P]` — genuinely pending, incomplete or not yet implemented.
- `[G]` — repeat for every applicable candidate/release; a previous pass is historical evidence only.

Jack reported the v0.5.0-alpha.4 baseline smoke set accepted (8/8). Later candidate-specific changes retain their own `[R]` status until actually retested.

## Release, audit and packaging

- [S] BT-SH-001 routes Daily, both Weekly views, Monthly, Outstanding and Movement History CSV rows through one structured serializer. Text beginning with `=`, `+`, `-` or `@`, including after leading whitespace/control/format characters, is apostrophe-neutralized before normal CSV escaping; typed numeric values remain invariant and unmodified. Pre-fix characterization proved all five duplicated helpers vulnerable, and focused post-fix regression/security coverage passes. This does not disposition BT-SH-002 or any later finding and is not manual spreadsheet acceptance.

- [S] BT-SH-002 validates the pending developer database-operation marker at its sole Data-owned reader before any state transition, replacement or cleanup. Active identity comes from configured startup state; the only Load stage is derived beside the trusted marker; Fresh may not name a stage; cleanup never uses marker path text. Forged absolute/relative paths, undefined enums, malformed/duplicate/extended shape, active mismatch and a missing required stage fail with the marker, active database and unrelated files unchanged. Existing valid Load/Fresh and publication-ready recovery remain covered.
- [S] BT-SH-003 atomically replaces the desktop `UserSession` with anonymous state before logout auditing and before each new login attempt. Focused integration coverage proves stale identity/Administrator authority cannot survive logout, audit cancellation, failed/cancelled login or repeated logout, while successful login/re-login and trusted logout attribution remain intact. Future API request-scoped `IUserContext` semantics are unchanged.

- [A] BT-ARCH-018-P1 remediates the four accepted layer-audit findings: user administration lists a credential-free immutable Services DTO; developer database mutations require an authenticated Administrator in a Services facade before unchanged Data mechanics; public audit queries return DTOs while only internal Services appenders may write and CSV exports submit bounded typed intent; Monthly Summary and Movement History preserve displayed row order while retaining Services-supplied totals. Independent ChatGPT review passed with no blocking correction, and Jack/operator's exact-candidate canonical BAT passed audit/build with zero warnings/errors and 841/841 tests with none failed/skipped under SDK 10.0.401. Commit `cd53f9ca19de1a10ea9ec375b63e294fdddfad23` is the accepted implementation; the architecture gate is closed. No Windows UI acceptance or Security Hardening remediation is implied.

- [S] BT-20-FIX2 reproduces the historical V13-upgraded schema16 shape without a `ReversesMovementId` self-FK, accepts only that absent form or the exact fresh-schema16 RESTRICT self-FK, fails malformed alternatives closed, and makes the governed schema17 rebuild publish and revalidate the exact final self-FK. The controlled exact-copy retained schema16-to17 startup/backup rehearsal passed; the actual retained production database was not mutated.
- [S] BT-20-FIX3 enriches the client-neutral reversed-line preview with authoritative customer/container display values and makes Customer, Container, Direction, Quantity, Date, reversal context and Decision primary. Technical lineage IDs remain secondary evidence; no decision, disposition or persistence authority moved into WinForms.
- [S] BT-20-FIX4 resolves original and lineage-native output physical batches through one typed authoritative anchor, keeps `RootMovementBatchId` immutable, uses current effective lines for subsequent whole-root correction, rejects missing/ambiguous/cross-root associations, distinguishes Restoration from Reversal through persisted transformation role and increases only the disposition dialog's default client size.
- [S] BT-20-FIX6 keeps `ResolveReversedLinesAsync` root-only, then reacquires the originally selected physical batch through `PreviewLogicalForBatchAsync` after a committed Restore. Continuation requires the same stable root, exact post-Restore generation, matching typed anchor and fresh all-Active state; missing, wrong-root, ambiguous, cross-root and changed-generation evidence fails closed.
- [A] BT-20-FIX7-A/B/C passed on the fresh alpha.9.6 rehearsal database: valid date edits preserved the opposite boundary, invalid edited From clamped to To, rapid To changes settled controls/summary/grid on the final range, and a separately committed Restore was immediately visible after the later correction dialog was cancelled. Automated coverage separately protects the successful-correction refresh path.
- [A] The alpha.9.6 successful later-correction refresh smoke passed on an isolated copy of the accepted post-Restore generation-1 rehearsal. Selecting original physical anchor #1080 opened the current two-line root directly; a Direction-only OUT correction committed atomically and Movement History immediately showed #1084/#1085/#1086/#1087 with the summary changing from 29 rows / 158 OUT / 264 IN / Net -106 to 33 rows / 168 OUT / 264 IN / Net -96, without date/filter/sort interaction.
- [A] Task20 item 6 passed corrected Daily/Weekly/Monthly activity, PositionAsOf/current position, Outstanding filters and signed numeric sorting. Five initial CSV exports matched audits #592–596; final alpha.9.7 Daily proof preserved the explicit Quantity-descending/Direction-ascending grid order (`IN 2`, `IN 1`, `OUT 1`) in CSV, and audit #599 recorded the leaf filename, 3 rows and complete active context without an absolute path. Read-only revisit/domain observers were unchanged and integrity/FKs passed.
- [A] Alpha.9.10 passed its canonical source/package-state audit, build and 827/827 automated tests with 0 failed/skipped under .NET SDK 10.0.401. Item-8 Windows acceptance then passed on Windows 11 at 1920×1080/150% with MainForm maximized and default dialogs, and on a separate Windows PC at 1920×1080/100% as the substantially larger effective workspace. The complete required surface matrix passed; Resolve Reversed Batch Lines showed the full typed `28/08/2026` value in its explicit `dd/MM/yyyy` 125-pixel Date column with no horizontal-scroll regression.
- [M] Read-only post-smoke verification passed at schema17 with integrity/FKs clean and 500 movements. Root #23 retained exactly permanent lines #175/#1080 and #176/#1081, advanced from accepted Restore generation 1 to complete correction generation 2/operation #12, linked output Batch #31 only to replacement movements #1085/#1087, and ended its ledger at #1087. Native lineage correctly leaves `CorrectedByMovementId` null; this is not a legacy-pointer defect.

- [S] Task20C R1–R4 documentation freeze records the approved activation semantics without production implementation or requirement-status promotion.
- [S] BT-20-P2 atomically activates the reviewed Task20D/E/F/P1/FIX1 seams in normal composition: one Data coordinator, catalogue17, native entry/receipt/mutation writers, validated projection consumers, logical WinForms correction/reversal routes, retained runtime session and coordinated developer Load/Fresh markers. Focused activation8/8 and canonical audit/restore/zero-warning build plus UnitTests279/279 and IntegrationTests491/491 (770 total) pass with zero failures/skips; retained-data and Windows/operator acceptance remain separate.
- [S] Startup/identity/bootstrap/Load/Fresh A2/A3/A8–A11/A13–A15, schema17 structural/current health, R1/R2/R3/EF/R7 and schema16 compatibility remain ordinary automated coverage. BT-20-P3 adds native Audit/History detail from validated operation/root/generation/line/ledger evidence. BT-20-P4 adds separately sequenced Restore/RemainReversed UI and focused success/no-write/stale/authorization/retry evidence; Windows/operator acceptance is not implied.

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
- [A] Daily Print Pack contains Outstanding Summary plus physical Movement Detail in one audited, readable PDF; bounded Task20 Item-7 Windows acceptance opened the real viewer Print dialog, proved all pages/sections printable without cropping and cancelled without requiring physical paper.
- [A] Normal schema17 Daily Print Pack uses Task 19's one-snapshot Outstanding/detail/metadata composition; deterministic correction/reversal/restoration interleavings remain covered and projection failure/cancellation fails the pack. Bounded Task20 Item-7 real preview/print acceptance and the separate Item-8 Windows/DPI display matrix passed.
- [P] Decide whether native Excel report export adds enough value beyond CSV.

## Activated correction, reversal and Administrator review authority

- [S] Normal native mutation/projection paths reuse IMP-04/04A CURRENT-root validation under one SQLite read transaction and expose non-forgeable read-only success models only after exact membership, current-pointer/role/introduction, RootOriginal physical-batch/null-single and ordinal validation. Unrelated historical-only links remain migration/diagnostic scope; `StatusReasonCode` is preserved.

- [R] Administrator/Operator may reverse or correct eligible ordinary Manual/Batch movements; Viewer is denied; Opening Adjustment/ImportRun rows remain outside generic mutation.
- [R] Original evidence is immutable; reversal and correction evidence/reason/actor/time/idempotency/concurrency/audit remain transactional.
- [S] Activated whole-root preview/execution requires every current line Active; all alpha.8 mutation writes fail closed and validated native projection is the sole normal numeric authority. Explicit schema16 compatibility fixtures retain the former guard/query behavior.
- [A] Movement History selection/action synchronization (BT-CORR-016) and whole-batch auto-select/clear/no-op/focus behavior (BT-CORR-017) remain accepted.
- [A] Administrator review state/filtering, acknowledgement, end-to-end Operator review, persistent reminder and Esc hierarchy remain accepted (BT-AUD-007/008/011/013/014).
- [A] The bounded Task20 acknowledgement drill-through/differences/readability, Movement History wrapping, correction/disposition-dialog layout and related Item-8 DPI surfaces passed on alpha.9.10 at 1920x1080/150% and on the separate 1920x1080/100% larger effective workspace. General candidate/release UI regression remains governed by the separate `[R]`/`[G]` rows.
- [A] Task20 item 5 passed the complete native Administrator-review boundary: exact Operator correction/reversal detail, two-to-zero reminder/Needs Review flow, persisted reviewer/time, non-reviewable acknowledgement drill-through, malformed-acknowledgement fail-closed/no-write behavior, Operator/Viewer service denial and duplicate-review prevention. Broader Audit Trail search/export remains separate below.
- [P] Broader Audit Trail search/general filtering/CSV export remains a release decision, not an implemented claim.

## Activated lineage foundation and remaining acceptance

- [S] Core contracts pin lineage vocabulary and persisted enum values; normal runtime composes their Data adapters only after schema17 readiness.
- [S] Read-only preflight, exact-source verified recovery artifact, exclusive upgrade lease and recovery classification are active normal migration prerequisites. A SHA-256-verified exact copy of the retained schema16 source passed the normal schema16-to17 migration/backup/native-health rehearsal with physical counts preserved; actual production-database mutation and recovery execution remain out of scope.
- [S] Normal schema16→17 migration creates truthful complete MigrationBaseline state through the coordinator; direct legacy callback execution of catalogue17 fails closed.
- [S] Migration-publication postflight remains strictly MigrationBaseline-only, while schema17 re-entry separately reuses current-root validation so valid native Initial/later roots are accepted and malformed native current lineage is rejected.
- [S] Automated migration evidence proves schema-16 operation Kind outside historical 0/1 blocks before mutation and can never become schema-17 Reverse/Restore.
- [S] Normal startup uses the upgrade coordinator and never auto-restores over a valid rolled-back source; the controlled exact-copy retained rehearsal passed, while actual retained-production mutation/recovery remains pending.
- [S] IMP-07 generation-zero Single/Batch lineage is now normal schema17 behavior. Exact native roots/lines/order, retry/conflict, fail-closed health and rollback boundaries remain covered; the controlled exact-copy retained migration rehearsal passed and corrected operator acceptance remains pending.
- [S] The reviewed IMP-05C trusted snapshot and pure provider-neutral complete-generation planner are consumed by normal native mutation while remaining independently write-free. Their reversal-pair, business-date, import exclusion, field-mask, no-op and physical-output proofs remain green; Windows/operator and retained-data evidence remain separate.
- [S] Normal native mutation uses IMP-06's caller-transaction primary-audit primitive; focused coverage retains commit/rollback, missing-transaction, unique association and unchanged independent `AuditService.WriteAsync` proofs. Full native detail/operator acceptance remains pending.
- [S] Normal native Correct/Reverse/Restore persistence publishes the planned complete generation atomically with exact generation-line identities, root CAS, replay/idempotency, optional physical output and one primary audit. Target-root audit health and transient-contention failure classification remain covered. Exact-copy retained Restore evidence and the protected bounded post-Restore FIX6 Windows continuation passed; broader permanent-requirement/operator scope outside the bounded Task20 inventory remains pending under its own acceptance criteria.
- [A] Whole-root correction still requires every current line Active. Alpha.9.6 passed the protected Batch #30 FIX6 continuation, FIX7 date/range/latest-refresh/Restore-plus-later-Cancel checks, successful later correction/refresh through original anchor #1080, the item-2 mixed-date/line-state sequence through generation 7, and the item-3 lineage-output Batch #31 plus real multi-window stale/conflict path through generation 8. Alpha.9.10 passed the complete 1920x1080/150% plus larger-effective-workspace Item-8 matrix.
- [S] The provider-neutral corrected activity/PositionAsOf authority is normal numeric authority across balances, reports, statements, Dashboard, customer/container reads, entry responses, import reconciliation and Print Pack. Relevant invalid/incomplete/unrooted evidence fails without raw fallback; explicit schema16 compatibility composition remains separate.
- [S] Audit-only corruption remains mathematically separate: otherwise proven projection can continue, while affected mutation/review/evidence completeness fails closed.
- [S] BT-CORR-033 transactional/concurrency acceptance is PASS at the automated/static evidence level. The added real schema17 `ReplacePreviousCutover` collision test commits a legitimate prior import and audit, retains a valid quarantined lineage FK to one import-owned movement, then proves the constrained delete rolls back atomically: the prior run/current-cutover pointer, all movements, lineage evidence and committed audit remain; no replacement run/movement/completion audit or partial artifact remains; integrity/FKs pass. The exact test passed 1/1 after removing an over-specific SQLite extended-code expectation, and the focused collision/success/preflight filter passed 3/3 with zero failed/skipped. Broader BT-CORR-033 Windows/report/detail acceptance remains separately pending.
- [A] The bounded Task20 acceptance inventory is complete. The alpha.9.6 exact-copy rehearsal passed hash identity, schema16-to17 migration, integrity/FKs, physical-count preservation, lineage backfill, recovery-backup hash and protected Batch #30 FIX7-A/B/C plus FIX6 continuation. Items 2–4 passed the mutation, output-anchor/multi-window and forensic-detail matrices; Item 5 passed native Administrator review and authorization; Item 6 passed corrected reports/positions and export ordering/context; Item 7 passed real PDF preview/print; and alpha.9.10 Item 8 passed every required surface at 1920×1080/150% and on a separate 1920×1080/100% PC. Final read-only verification remained `integrity_check=ok`, zero foreign-key violations and unchanged domain counts.
- [P] After lineage acceptance, complete the protected whole-codebase layer audit before later major work.

## Remaining pre-v1 product and release gates

- [P] Dashboard design discussion precedes implementation; then validate KPIs, attention/drill-through, activity, summaries, charts/ageing and display layouts.
- [P] Google Workspace email + Texto SMS delivery, templates, secure settings, manual/automatic sends, opt-out, history, retry/idempotency and audit.
- [P] Production user-facing Backup/Restore, scheduled retention and recovery drill (separate from pre-lineage migration safety).
- [P] Complete Security/Data Integrity/Code Quality hardening and all remaining required BT-SH dispositions after fixed BT-SH-001.
- [P] Installer/upgrade/deployment acceptance and full v1 production regression.

## Current manual acceptance floor

- [G] Windows 11, 1920x1080 at 150%: required actions remain reachable; no harmful clipping/overlap; report grids remain usable.
- [G] Recheck materially changed UI on the substantially larger production display.
- [G] Reports/printing changes receive real PDF preview/print evidence; importer behavior depending on the production workbook receives a real-workbook pass.
- [G] Automated success never marks a manual/DPI/preview/print item accepted.
