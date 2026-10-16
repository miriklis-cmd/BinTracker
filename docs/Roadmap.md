# BinTracker Roadmap

Current planning baseline: **v0.5.0-alpha.15**

This roadmap tracks work that is still relevant. Completed alpha-by-alpha history belongs in `docs/CHANGELOG.md`, not here.

### Dashboard milestone design gate

**Do not begin Dashboard implementation immediately.** Before writing Dashboard code, stop and evaluate the design with the operator.

The discussion must cover:

- useful charts and chart types;
- forecasting hooks and future predictive/ML readiness;
- drill-through behaviour from cards/charts/customers/containers/alerts;
- attention/exception detection;
- recent activity and operational trends;
- customer/container comparisons;
- ageing/outstanding behaviour;
- anomaly/risk/forecast ideas;
- laptop vs large-monitor layouts;
- alternative dashboard concepts and trade-offs;
- how a future WinUI 3 v2 could materially improve the Dashboard (responsive cards, charts, drill-through, visual states, navigation and large-monitor presentation), while separating v1 WinForms work from v2-only polish;
- the current Reports launcher as an explicit v1 reference screen: report discovery, categorisation, cards, density, search/favourites/recent-report possibilities and responsive growth as more reports are added;
- representative individual report pages as v1 reference screens, comparing WinForms vs WinUI 3 for filters, grids, sorting, summaries, exports and richer visualisation.

Only implement after a preferred direction is agreed. Dashboard is intentionally allowed its own milestone because experimentation may materially change scope.

This design gate applies when Dashboard is reached in the authoritative current execution order below. It does not move Dashboard ahead of Communications, user-scoped draft ownership, Container Type ordering or Branding. The remaining importer failure-detail message and deferred Review cosmetics can be handled alongside the applicable phases without reopening the completed core import safety work.


## Current execution order — revised 16 October 2026

This order is the authoritative pre-v1 sequence. `v0.5.0-alpha.1` began the clean milestone scheme with Movement Correction/Reversal; later milestone numbers remain scope-driven.

1. **Finish v0.4 Reporting** — Monthly Summary is user-accepted; validate Daily Print Pack, then complete the final report consistency/real-world print acceptance pass.
2. **Batch Entry acceptance cleanup** — verify Esc, post-entry field clearing/focus, and implement/decide crash/power-loss draft recovery before production.
3. **Movement Correction / Reversal** — alpha.8 foundation implemented and accepted; corrective interactions/canonical gate retained.
4. **Logical movement-lineage implementation and acceptance** — implement frozen BT-CORR-018..033/BT-HIST-008..009 architecture, per-database preflight/backup, migration, services, reporting, WinForms integration and Batch #30 adversarial acceptance. Do not remove the physical-batch guard independently.
5. **Whole-codebase layer delineation audit — HARD GATE** — Stage A completed the protected audit; alpha.10 P1 implemented the four accepted findings, passed independent ChatGPT review and passed the exact-candidate canonical BAT at 841/841 with zero warnings/errors or failed/skipped tests. The accepted implementation is commit `cd53f9ca19de1a10ea9ec375b63e294fdddfad23`; this gate is closed.
6. **Security, Data Integrity & Code Quality Hardening — HARD GATE** — remediate/disposition every protected security finding before branding/communications and v1. The lineage and layer-audit items above are protected completion gates within Movement Correction/Reversal, not intervening feature work.
7. **Email, SMS & Customer Communications** — the first user-facing feature after Security Hardening: Google Workspace email + Texto SMS, manual Customer sends, bulk/automatic reminders, preferences/opt-out, templates, delivery history, retry/duplicate protection, audit, outstanding snapshot and the statement attachment/link decision.
8. **User-scoped unsaved Batch Entry recovery/ownership** — implement BT-BATCH-012 so persisted unfinished drafts are owned by stable persisted user ID and fail closed across users below WinForms.
9. **Deterministic configurable Container Type ordering** — design duplicate-order handling and reorder UX without hard-coded container priorities.
10. **Business Information & Branding** — logo, shared header/branding and generated-output source.
11. **Dashboard** — mandatory design discussion before code; future clients remain comparison context only.
12. **Customer operational analytics/polish** — sorting, lifetime totals and statement integration.
13. **Production Backup / Restore / Recovery** — user backup, validated restore, scheduling/retention and recovery drill; the earlier lineage migration backup is a blocking safety gate, not a substitute.
14. **Final security, audit & reliability release review** — authorization/audit matrices, secrets, logging, restart, integrity, Release and DPI.
15. **Installer / upgrade / deployment** — Windows package, safe upgrades, signing and production configuration.
16. **Full v1 acceptance / regression** — fresh install/database/import/balances/lineage/reports/communications/branding/dashboard/backup/restart/upgrade.
17. **v1.0 production release** — accepted replacement for the daily Excel workflow.

Licensing/activation is already permanently tracked as post-v1 requirement BT-PV1-009, not as a v1 milestone, so it is not inserted into this pre-v1 order. Post-v1 remains separate: **authenticated API + PostgreSQL central deployment**, licensing/activation and commercial support tooling, Windows UI v2 / WinUI 3 evaluation, customer portal, handheld/mobile clients, barcode scanning, multiple depots and Custom Report Designer. Pre-v1 preserves boundaries/semantics only; it does not implement those clients/providers.

## Priority 0 — close data-integrity risks before more features

### 1. Finish Excel Import safety and provenance

The importer is functionally implemented and has been exercised against the current production Excel workbook. Analyse, Map, Review, customer decisions, container mapping, balance reconciliation and transactional Step 4 execution are working.

Remaining:

- [x] **Forced-failure rollback verification** — integration test now forces failure after the final `SaveChangesAsync` but before `CommitAsync` and verifies that customer, movements, ImportRun and completion audit all disappear; exact-source retry remains allowed.
- [x] **`BinMovement.ImportRunId` relational provenance** — nullable FK + index implemented; all new import Adjustment/ExcelImport movements link to their ImportRun, while Manual/Batch rows remain unlinked. Migration V10 safely backfills eligible alpha.19.x `IMPORT-<id>` rows.
- [x] **Correction comparison identity** — previous/proposed positions are now keyed by Customer identity + `ContainerTypeId`, not display labels, eliminating false `Blue` vs `Blue Bin` differences. One-value correction regression requires exactly one genuine changed position.
- [x] **Step 4 correction discovery/UI** — preflight now receives the cutover date, so changed-workbook same-cutover state is surfaced before execution as an explicit **Replace / Correct** action rather than dead-ending at the backend guard.
- [x] **Changed-workbook / same-cutover workflow** — structural CutoverDate detects a changed completed workbook for the same date; Step 4 shows a comparison and requires explicit Replace/Correct. Reconciliation reconstructs the pre-cutover baseline, replaces only prior ImportRun-linked movements, and preserves same-day/later Manual/Batch activity on top of the corrected workbook position. Normal schema17 composition gives comparison and execution the corrected projection authority, with execution participating in its caller-owned serializable transaction; explicit schema16 compatibility retains the legacy path.
- [x] **Import Run history/details UI** — Administrator Settings opens Import History with run list, source file/SHA-256/cutover/user/counts/status/replacement chain, currently linked movements, and persisted customer/container correction differences for replacement runs.
- [ ] Add a useful transactional failure report showing the row/customer/container that stopped execution.
- [x] Re-test exact re-import after provenance changes through the existing integration suite.

Already implemented:

- [x] workbook analysis and worksheet classification;
- [x] Source-only import planning;
- [x] normalized customer matching;
- [x] Create / Skip new-customer decisions;
- [x] existing-customer match confirmation/override;
- [x] default Blue, Yellow and Bulk legacy parsing;
- [x] unknown-container mapping;
- [x] authoritative B/Fwd reconciliation;
- [x] transactional customer creation/opening adjustments/OUT/IN;
- [x] ImportRun + SHA-256 exact re-import protection;
- [x] live-database revalidation immediately before commit;
- [x] workbook-changed-after-preflight protection;
- [x] transaction rollback implementation;
- [x] forced post-SaveChanges rollback regression test;
- [x] relational ImportRun movement provenance + legacy backfill migration;
- [x] production-workbook end-to-end import testing.

### 2. Protect unsaved customer edits — COMPLETE

- [x] Track dirty state for all editable customer fields.
- [x] Prompt **Save / Discard / Cancel** when switching customer, searching/filtering away, starting New Customer, navigating to another page, logging out or closing the application.
- [x] Container Types uses the same explicit **Save / Discard / Cancel** wording.

### Customer search/sorting/analytics

- [x] Type-ahead search by customer code/name exists.
- [ ] Add operator-useful sorting for customer code/name, outstanding position, credit position and last movement.
- [ ] Add customer lifetime totals for OUT/Taken and IN/Returned where operationally useful.
- [ ] Keep customer balances separated by configured Container Type.

## Priority 1 — finish the core operational product

### 3. Reports

- [x] **v1 report-hosting decision — Option B / hub-and-page:** Reports landing page remains the discovery hub. Detailed reports are to open inside the main BinTracker workspace and use a shell-level `Reports › <Report Name>` breadcrumb to return to the hub. Movement History is the first implemented reference page; migrate the other detailed reports as a controlled follow-up without changing accepted report business/export behavior. Market Floor remains an inline quick-report action on the hub.
- [x] **Migrate remaining detailed reports to the v1 workspace pattern — IMPLEMENTED, acceptance pending:** Outstanding Containers, Daily Movements, Weekly Movements, Customer Statement and Monthly Summary now open in the main workspace with the shared report breadcrumb. Existing report filters, sorting, PDF/CSV/audit behavior and accepted business logic are preserved; Windows visual/interaction smoke acceptance is required.
- [x] **Compact embedded report chrome** — integrated legacy report pages remove standalone outer padding, duplicate large report titles and Close-only footer rows while retaining the accepted explanatory sentence. The reclaimed space belongs to the report controls/grid; BT-RPT-019 permanently gates this.
- [x] **Integrated Reports smoke corrections — IMPLEMENTED, acceptance pending:** Weekly Movements keeps Source as one wrapping group and refreshes Notes availability after loads/tab changes; Customer Statement search width exposes its full keyboard cue; clicking the selected Reports sidebar item from a detailed report returns to the Reports hub. BT-RPT-020..021 permanently gate the functional behaviors.
- [x] **Option C tabled for post-WinForms discussion** — a persistent fully integrated Reports workspace with internal report navigation is not a v1 WinForms requirement. Revisit it when evaluating WinUI 3 or another replacement UI.

Implemented:

- [x] Customer Statement PDF with container-by-container running balances.
- [x] Market Floor Sheet, front + reverse.
- [x] Market Floor Account/Cash rules, Account-only separate credit section and special containers.
- [x] Blue implicit / Yellow explicit floor reporting.
- [x] import opening adjustments treated as B/Fwd rather than physical daily movement.
- [x] adaptive Market Floor sizing using the current real workbook.
- [x] Market Floor generation auditing.

Still required:

- [x] **Outstanding Containers report** — current/as-of-date on-screen query, customer/container filters, inactive/credit options, CSV export and audited landscape PDF generation are implemented. **Customer → Container grouping** keeps Blue/Yellow/Bulk/etc. adjacent for each customer.
- [x] **Historical Outstanding / As-of-Date foundation** — ledger-derived end-of-date customer/container positions are implemented and tested; future movements are excluded and containers remain separate.
- [x] **Weekly Movements report** — Monday-to-Sunday Daily Detail + Weekly Overview, This Week/Last Week shortcuts, authoritative Container Type filter, future-date guard/current-week activity-through-today semantics, PDF/Generate & Open and CSV export preserving selected-view sort.
- [x] **Customer Statement view/print workflow** — shared workflow supports Generate PDF and Generate & Open from Customers; Reports now has a Customer Statement launcher with customer search/selection. Opened PDFs are printable through the Windows PDF viewer.
- [x] **Daily Movements report** — integrated responsive report page with today/yesterday shortcuts, customer/container/direction/source filters, physical-movement default, optional opening adjustments, typed sorting, audited PDF and CSV preserving the current grid order.
- [x] **Movement History report** — integrated full-size main-application page with inclusive date range, customer/container/direction/source filters, opening-adjustment opt-in, future-date guards, quick range shortcuts, responsive readable columns, derived direction/reversal badges, typed sorting, audited PDF and CSV preserving current grid order, and stable customer-code filenames when a customer filter resolves to one customer.
- [x] **Movement History audit identity** — the grid and PDF/CSV exports expose the authoritative persisted Movement ID used by correction/reversal/audit workflows; ID sorting is numeric and participates in the existing multi-column sort.
- [x] **Monthly Summary** — selected-month OUT, IN and net movement totals with customer/container breakdown, This Month/Last Month shortcuts, customer/container/source filters, optional opening adjustments, typed numeric sorting, audited PDF and CSV preserving current grid order.
- [x] **Daily Print Pack** — selected-date Outstanding Summary + physical Movement Detail in one audited PDF; acceptance testing remains.
- [x] **Monthly Summary on-screen interaction** — integrated responsive main-workspace page with live dropdown/date/checkbox refresh, Customer-on-Enter search and sortable summary grid.
- [x] CSV export is implemented for Outstanding Containers, Daily Movements, Weekly Movements, Movement History and Monthly Summary, with audited export events.
- [ ] Decide whether native Excel export adds enough operational value beyond CSV before v1; do not add it merely for feature parity.
- [ ] Stress-test Market Floor with a genuinely high Yellow-bin day; adaptive sizing is accepted for now but remains a real-world validation item.

### Batch Entry — acceptance cleanup, not a redesign

Current operator-confirmed behaviour:

- [x] `Ctrl+Enter` saves the batch.
- [x] Tab / Shift+Tab keyboard flow works.
- [x] Enter from Quantity / Reference / Notes adds or updates the pending line.
- [x] Draft survives page navigation and logout/login while the application remains running.
- [x] Pending rows affect Current vs With Draft balance preview.
- [x] Dashboard refreshes after a successful batch save.

Remaining:

- [ ] **Acceptance pending:** Esc now cancels draft-line edit first, otherwise clears the current unsaved entry, otherwise returns to Dashboard while retaining the batch draft.
- [ ] **Acceptance pending:** Add to Batch clears Customer/Quantity/Reference/Notes and customer preview, returns focus to Customer, intentionally carries Movement Date / Batch Type / Container Type forward, and pending-grid rebinding does not reload the just-added row.
- [ ] **Acceptance pending:** crash/power-loss recovery persists the draft atomically and, after restart, asks the operator to Continue Batch / Save Batch / Discard Batch instead of silently resuming. Verify all three choices plus removal after successful Save Batch / Clear Batch.

This is acceptance/polish work only unless smoke testing exposes another real defect.

- [x] **Opening reconciliation provenance — IMPLEMENTED, acceptance pending:** future successful normal-cutover imports persist every non-zero opening adjustment as an immutable ImportRun snapshot (previous BinTracker position, Excel B/Fwd/target and adjustment). Import History distinguishes this from same-cutover Replace/Correct correction changes and labels pre-capture historical runs honestly. BT-IMP-022 gates the behavior.
- [ ] **Audit Trail Find/Search and filtered CSV export — BT-AUD-006 TRACKED / NOT IMPLEMENTED:** Provide Administrator-facing Find/Search across at minimum user, action, entity type/ID and description. Search combines with active filters rather than replacing them, has clear/reset plus next/previous-match or equivalent navigation, and preserves search/filter position when detail is opened and closed. CSV export uses the currently filtered/searched Audit Trail in its current sort order and includes authoritative UTC timestamp, user, action, entity type/ID, description, success/failure and review fields including reviewer/review time where applicable. It follows security/redaction rules and the export itself is audited. This remains a tracked enhancement/release decision and is not implemented.

### 4. Dashboard

Current dashboard is only the first operational summary: Returned Today, Taken Today, Outstanding and Requires Attention.

Required dashboard pass:

- [ ] Validate the meaning of each headline metric and make container/customer scope clear.
- [ ] Make **Requires Attention** useful, not only quantity-threshold based.
- [ ] Add actionable customer/container attention list with drill-through.
- [ ] Add recent movement/activity panel.
- [ ] Add useful by-container summary (Blue / Yellow / special types as appropriate).
- [ ] Add at least one genuinely useful operational chart/trend; use Container Type Dashboard Colour only as presentation metadata, not as physical-container meaning.
- [ ] Make dashboard cards/actions clickable where they naturally lead to Customers or Reports.
- [ ] Decide whether ageing / “days outstanding” belongs on the dashboard and define the business rule before implementing it.
- [ ] Review layout at production DPI and typical 4am floor/office use.

### 5. Movement correction / reversal

Task20C froze the user-approved R1–R4 activation decisions. Task20D/E/F/P1/FIX1 implemented the coordinator, R1 receipts, R2 preview/CAS/legacy closure, minimum R3 routing, EF membership and R7 snapshot prerequisites under explicit schema17 composition. BT-20-P2 activates those seams atomically in normal WinForms composition, BT-20-P3 completes full native Audit/History detail, and BT-20-P4 implements the separately sequenced Restore/RemainReversed WinForms workflow. Fresh alpha.9.6 exact-copy migration rehearsal, bounded FIX7-A/B/C and FIX6 Batch #30 regression acceptance, subsequent Items 2–7 and alpha.9.10 Item 8 passed; the bounded Task20 acceptance inventory is complete.

BT-20-FIX2 corrects the startup compatibility defect exposed before P4 UI smoke: schema16 preflight now recognizes the historically migrated V13 no-self-FK form as well as the exact fresh-schema16 form, and schema17 publication finalizes and requires the exact self-FK. This is source and synthetic-test evidence only; the retained database remains unmigrated, Batch #30 remains untouched and P4 Windows smoke must be rerun.

BT-20-FIX3 follows the alpha.9.1 canonical pass and disposable Windows smoke. That smoke reached the P4 dialog and passed interception, no-default, Cancel/Esc no-write and tested-scale visibility, but operator comprehension failed because raw IDs dominated the row. Alpha.9.2 makes authoritative customer/container/direction/quantity/date and reversal context primary while retaining lineage IDs as secondary evidence. The corrected dialog, disposition outcomes and continuation still require Windows retest; retained rehearsal and Batch #30 remain untouched.

BT-20-FIX4 follows the alpha.9.2 canonical pass and expanded disposable Windows smoke. Restore/RemainReversed, native detail, a subsequent whole-root correction, corrected current projection and Correct Selected controls passed. `Correct Entire Batch` from the resulting lineage-output physical batch failed because WinForms compared that batch with immutable original `RootMovementBatchId`; restoration history wording and the dialog's smaller default were bounded presentation defects. Alpha.9.3 uses typed authoritative original/output anchors without changing root identity, distinguishes persisted Restoration from Reversal and increases only the dialog's default size. Corrected Windows acceptance, retained rehearsal and protected Batch #30 remain pending.

BT-20-FIX5/alpha.9.4 makes both original and lineage-output physical batches navigation anchors only and builds the correction dialog from fresh logical current state. Its canonical gate passed 800/800, and the controlled exact-copy retained schema16-to17 migration/backup rehearsal passed with schema/count/integrity preservation and governed recovery evidence. Protected Batch #30 then passed dialog, Cancel, RemainReversed, Restore mutation and native audit/history/detail checks, but post-Restore continuation failed because the root-only disposition preview no longer carried the selected physical anchor.

BT-20-FIX6/alpha.9.5 retains root-only disposition semantics and separately sequenced Restore. Movement History reacquires the originally selected physical batch through the authoritative preview route and requires its typed anchor to resolve the same stable root and exact post-Restore generation before correction can continue. Its protected Batch #30 core Windows continuation passed.

BT-20-FIX7/alpha.9.6 corrects the three bounded Movement History defects exposed next: editing one date no longer moves the valid opposite boundary; one captured range/request and latest-request-wins application prevent stale report state; and a committed Restore refreshes authoritative persisted history before the separately sequenced correction dialog, so later Cancel cancels only that correction. Focused static/automated evidence, the operator-run canonical 811/811 gate, FIX7-A/B/C Windows re-smoke, successful original-anchor whole-root correction/immediate-refresh and the item-2 selected/whole both-orders, mixed-date/line-state, repeated and later-action sequence passed. The formerly open output-anchor, report/detail/review and DPI boundaries subsequently passed in Items 3–8.

Task 19 is complete at pushed commit `930f2c77b1df7cd4629be2faff662750431854fe`; independent review and its canonical BAT are complete. BT-20-P2 now composes that Daily Print Pack shared-snapshot authority in normal schema17 runtime. Bounded Windows/operator real preview/print and controlled exact-copy retained-data acceptance passed during Task20; packaging/deployment, actual production-database recovery and alternative-provider acceptance remain pending.

Retained Batch #30 stays protected from mutation/deletion in the retained source. A fresh alpha.9.6 exact-copy rehearsal proved hash identity, schema16-to17 migration, integrity/FKs, physical-count preservation, lineage backfill and exact-hash recovery evidence before the controlled Windows checks. FIX7-A/B/C and the FIX6 continuation regression passed; post-Cancel mechanics proved one Restore generation/movement and no later write, then isolated copies passed successful original-anchor correction/immediate refresh with complete generation 2/output Batch #31 evidence and the item-2 mutation sequence through generation 7. Items 3–8 subsequently completed the bounded detail, report, review, preview/print and display matrix recorded in Testing and TEST-CHECKLIST.

**Architecture frozen; normal schema17/native runtime activated and bounded Task20 acceptance complete:** BT-CORR-018..033, BT-HIST-008..009, BT-AUD-015..017 and BT-OPS-011..012 define stable roots/lines, full generations, restoration/RemainReversed, corrected activity/PositionAsOf, root concurrency, provider/client neutrality, migration preflight/backup and fail-closed health. BT-20-P2 routes normal startup through the Data coordinator, BT-20-P3 adds native Audit/History detail, and BT-20-P4 adds the explicit separately sequenced Restore/RemainReversed workflow. The production DbContext still does not map lineage entities; Data-owned native SQL remains the provider adapter. Explicit schema16 compatibility fixtures retain their legacy authority without becoming a second normal runtime. The complete bounded acceptance inventory passed through alpha.9.10 Item 8; broader permanent requirement rows retain their own statuses.

The bounded pre-IMP-07 lifecycle correction keeps actual migration publication strict to the frozen `MigrationBaseline` output while making schema-17 `AlreadyComplete` validation use separate structural/current-health checks that accept valid native `Initial` roots and reject malformed current lineage. It changes no lineage semantics, migration registration or runtime authority.

Unified Correct/Reverse/Restore execution, root-CAS/idempotency/provider translation, corrected activity/PositionAsOf, all prepared consumers, Import reconciliation, Task 19's shared Print Pack snapshot, native Audit/History detail and the separately sequenced Restore/RemainReversed UI are now normal post-readiness behavior. Broader activated-system automated gates, fresh exact-copy retained migration/backup rehearsal, bounded FIX7-A/B/C, the FIX6 Batch #30 regression check, successful original-anchor correction/refresh and the bounded Task20 acceptance inventory have passed. Alpha.9.10 closed Item 8 on Windows 11 at 1920×1080/150% and on a separate 1920×1080/100% PC after its canonical 827/827 gate; final read-only integrity/FKs and domain counts remained clean and unchanged. BT-ARCH-018 Stage A and alpha.10 P1 are complete and accepted without reopening Task20: independent review passed and the exact-candidate operator BAT passed 841/841. Security Hardening Batch A is complete at alpha.14 and Batch B is complete at the alpha.15 static/focused-automated-evidence level: BT-SH-006/030 now share one mutation/audit commit boundary, while BT-SH-040 retains only its Batch C operational-identifier scope. Packaging/deployment, actual production-database mutation/recovery and alternative-provider acceptance are not silently implied.

The whole-codebase layer delineation audit at execution step 5 and its four accepted alpha.10 remediations are complete and accepted. Independent review and the exact-candidate canonical BAT have passed; the architecture closure gate no longer blocks Security Hardening. No Security Hardening finding is fixed by that closure.

- [x] **Reversal foundation implemented:** Administrator can select a saved movement in Movement History and create an equal/opposite linked reversal; original ledger row is never edited/deleted.
- [x] Reversal requires a reason and preserves original/reversal linkage, actor/time and `MOVEMENT_REVERSED` audit in one database transaction.
- [x] Reversal permission is enforced at service layer: Administrator and Operator may reverse ordinary Manual/Batch movements; Viewer cannot. Opening Adjustment and Excel Import/provenance-linked movements are excluded from generic reversal and routed to Administrator-controlled workflows.
- [x] **Reversal engine smoke accepted:** OUT and IN reversal, immutable history, balance neutrality, audit creation, already-reversed protection and reversal-of-reversal protection passed on Windows. Later Task20 evidence accepted the refined Operator policy and Administrator review/security boundaries.
- [x] **Correction-by-replacement implemented/automated-tested:** date, customer, container, direction, quantity, reference and notes; original-period neutralisation; corrected replacement; immutable operation lineage; persisted whole-batch date/direction correction; database-backed race/idempotency handling; Operator review acknowledgement and MovementBatch audit detail.
- [x] **Bounded Task20 Windows/operator acceptance complete:** mutation, output-anchor/multi-window concurrency, forensic detail, Administrator review/authorization, corrected reports, real preview/print and the full display matrix passed through alpha.9.10. The final display pass used Windows 11 at 1920x1080/150% plus a separate 1920x1080/100% PC as the substantially larger effective workspace.

#### Pre-v1 Audit Trail / Administrator oversight acceptance

The alpha.8.6 core review workflow was manually accepted. Alpha.8.7 added acknowledgement drill-through, exact difference/readability improvements, Movement History width/wrapping correction and small-batch dialog cleanup; bounded Task20 Items 4, 5 and 8 subsequently accepted the applicable detail, review, authorization and Windows/DPI behavior:

- [x] **Review-state discoverability (BT-AUD-007) — IMPLEMENTED-ACCEPTED:** explicit state and All/Needs review/Reviewed filtering.
- [x] **Context-sensitive acknowledgement (BT-AUD-008) — IMPLEMENTED-ACCEPTED:** deterministic eligibility, contextual confirmation, immediate status/count feedback, next-pending selection, exact-event evidence and duplicate rejection.
- [x] **End-to-end review acceptance (BT-AUD-011):** Task20 Item 5 proved Operator correction/reversal -> persisted review-required state -> later Administrator reminder/`Needs review` -> authoritative detail -> successful acknowledgements -> `Reviewed` with persistent reviewer/time -> acknowledgement drill-through -> duplicate prevention.
- [x] **Persistent Administrator review reminder (BT-AUD-013) — IMPLEMENTED-ACCEPTED:** navigation-wide WinForms panel consumes presentation-independent service state/change contracts; future WinUI 3 maps this presentation to native `InfoBar` without changing review semantics.
- [x] **Service security boundary:** Task20 Item 5 proved Administrator-only review/detail service enforcement and exact Operator/Viewer denial under BT-SEC-005 and BT-CORR-001/004/011.
- [x] **Context-sensitive batch/detail routing (BT-AUD-009/010) — IMPLEMENTED-ACCEPTED for current routes:** action enablement follows authoritative identity; correction/reversal events open exact movement-change lineage, while ordinary MovementBatch events retain persisted batch detail. Missing/ambiguous lineage fails closed.

Future contextual drill-down is separately tracked by BT-AUD-010: supported events should route to authoritative existing detail surfaces (`MovementBatch`, ImportRun/Excel Import run detail, and correction/reversal lineage where implemented); events without meaningful detail have no enabled action. These additional routes are not implemented in this task.

Post-v1 policy/design requirements (recorded, not implemented):

- Define stronger controls for high-risk or historical corrections (potentially hundreds/thousands of containers, old/closed periods, sensitive changes, Administrator override/reopen and whether selected cases require Administrator authority/approval). Do not invent “large” or “old” thresholds until policy review.
- Investigate formal period closing/locking: an Administrator-controlled closed-through date, audited reopen/override and a configurable operational grace period. Do not automatically close yesterday because legitimate delayed entries occur.


### 5A. Security, Data Integrity & Code Quality Hardening — HARD GATE

This dedicated pre-v1 workstream begins immediately after the Movement Correction/Reversal workstream is complete, including its logical-lineage acceptance and protected layer-delineation closure gate, and before subsequent feature work, Business Information/Branding or Communications. `docs/SecurityHardeningRegister.md` is the authoritative finding ledger for the external code/security audit.

Implementation is deliberately split into controlled batches: security boundaries; data integrity/concurrency; hostile-input/filesystem resilience; supply-chain/release integrity; then maintainability/code reduction. Findings are not to be silently dropped merely because they are inconvenient or become less visible during feature work.

The repository audit must fail if the register disappears, loses an audit finding, contains an invalid disposition, or if this workstream is moved behind Branding/Communications. A v1.0 release must additionally fail while any accepted v1 finding remains unresolved.

BT-SH-001 is fixed at the alpha.11 static/focused-evidence checkpoint: all current report CSV exports use one structured cell serializer that neutralizes formula-leading text, including whitespace/control/format-prefix bypasses, while retaining typed numeric output and existing columns/order. BT-SH-002 is fixed at the alpha.12 static/focused-evidence checkpoint: the pending developer database-operation marker is fully validated at its sole Data reader before transition, replacement or cleanup, and only trusted configured/derived paths can influence database execution or staged-artifact deletion. BT-SH-003 is fixed at the alpha.13 static/focused-evidence checkpoint: the singleton desktop session atomically becomes anonymous before logout audit work and before any new login attempt, so stale user/role authority cannot survive failure or cancellation. The alpha.13 `BT-SH-004-P1` current-source reconciliation closed BT-SH-004, 011, 020 and 041 as already fixed, rejected BT-SH-023 and 046 as inapplicable to accepted current contracts, and organized the remaining work into eight authority-aligned batches. Alpha.14 `BT-SH-004-P2` completes Batch A by fixing BT-SH-008/017/018/019/022/028/032 through canonical bounded authentication identity, uniform Services authorization, persisted current-account revalidation and self-reset invalidation. Alpha.15 `BT-SH-004-P3` completes Batch B by fixing BT-SH-006/030 with one Data-owned commit/rollback boundary for each affected authoritative mutation plus required audit. BT-SH-040 retains only customer/container operational-identifier normalization in Batch C. Current totals are 16 `FIXED`, 20 `CONFIRMED-V1`, 1 `REVIEW-V1`, 11 `POST-V1` and 2 `NOT-APPLICABLE`; Batch C is next, and the Security register remains authoritative for exact membership, rationale and evidence.

### 5B. Email, SMS & Customer Communications — first post-hardening feature

This is the first user-facing feature milestone after the Security Hardening gate closes. Its complete scope remains Google Workspace email, Texto SMS, manual send from Customer, automatic/bulk reminders, customer preferences and opt-out, templates, delivery history, retry/duplicate protection, audit, the relevant outstanding snapshot and the statement attachment/link decision. No later feature milestone may begin first.

### 5C. User-scoped unsaved Batch Entry recovery/ownership — second post-hardening feature

BT-BATCH-012 is the second feature milestone after Security Hardening, immediately after Communications and before Container Type ordering. Every persisted unfinished draft must be owned by stable persisted user ID. Only that authenticated owner may Continue, Save or Discard it; another user, including an Administrator, must neither see it as their recoverable work nor operate on it. Missing, invalid or ambiguous ownership fails closed. Existing owner crash/restart recovery and independent role authorization for committing a batch remain unchanged.

This provider-neutral invariant is deliberately established in v1 before post-v1 multi-user/API/central-SQL work. Retrofitting workstation-scoped drafts after concurrent users exist would complicate authority, recovery, concurrency, takeover, audit, conflict handling and persisted-draft migration. This does not move API, PostgreSQL or another central-provider implementation into v1; any future Administrator takeover must be separately designed, explicit and audited.

### 5D. Deterministic configurable Container Type ordering

This is the third feature milestone after Security Hardening. Define duplicate-order handling and reorder UX without hard-coded container priorities.

### 5E. Business Information & Branding

This is a **pre-v1 milestone**, not a post-v1 idea.

- [ ] Business logo in Business Information.
- [x] Configurable textual **Default Report Header** already exists in Business Information; retain it as the custom textual header foundation.
- [ ] One authoritative branding model/service for PDFs, statements, email and other generated output.
- [ ] Design storage, file types, dimensions/aspect ratio, fallbacks, header/footer placement and per-output enable/disable behaviour before implementation.
- [ ] Decide how business name, trading name, logo and custom header coexist without duplicate-looking output.
- [ ] Leave room for richer HTML email header/signature treatment without creating a second branding system.

## Priority 2 — communications specification

### 6. Email, SMS & Customer Communications

Execution position is governed by the current order above and section 5B: this is the first feature after Security Hardening. This detailed section records scope and does not defer Communications behind Branding.

Groundwork already exists:

- customer `AllowEmailReminders`, `AllowSmsReminders` and `ReminderOptOut`;
- `ReminderDelivery` persistence model with channel, destination, status, provider response and outstanding snapshot.

Still required:

- [ ] Finalise reminder business rules around the previously agreed direction: automatically remind customers owing empty bins by **Friday or earlier**, while keeping thresholds/timing configurable where sensible.
- [ ] Email provider integration using the previously chosen **Google Workspace** direction.
- [ ] SMS provider integration using the previously chosen **Texto** direction.
- [ ] Administrator provider/credential configuration with secrets stored securely.
- [ ] Email and SMS templates.
- [ ] Manual send from Customer screen.
- [ ] Bulk/automatic reminder run.
- [ ] Respect per-customer Email/SMS/Opt-out settings.
- [ ] Delivery history UI.
- [ ] Pending / Sent / Failed / Skipped lifecycle.
- [ ] Retry/error handling without duplicate sends.
- [ ] Audit reminder runs and sends.
- [ ] Decide whether statements can be attached/linked in email reminders.


## Priority 3 — production hardening before v1.0

### 7. Production backup / restore / recovery

Developer Database tools are not the production solution.

- [ ] User-facing production backup.
- [ ] Restore workflow with confirmation and validation.
- [ ] Automatic backup before database/schema upgrade.
- [ ] Retention policy.
- [ ] Recovery documentation and restore test.
- [ ] Detect/report corrupt or inaccessible SQLite database cleanly.

### 8. Security and reliability review

- [ ] Full authorization review across every write/admin action.
- [ ] Secrets/credential storage review before Email/SMS.
- [ ] SQLite transaction/concurrency review.
- [ ] Database integrity constraints and migration audit.
- [ ] Error logging suitable for support without leaking passwords/secrets/customer-sensitive data.
- [ ] Crash/restart behaviour review.
- [ ] **Audit retention/archive release decision (BT-AUD-012):** document an explicit production policy without inventing a period; do not silently assume indefinite growth. Any cleanup/archive/deletion design must preserve audit integrity, remain auditable and retain required legal/business evidence.
- [ ] Release build (`Release`, not only `Debug`) acceptance testing.
- [ ] High-DPI regression pass at 100%, 125% and 150%.

### 9. Installer, upgrades and deployment

- [ ] Windows installer/package.
- [ ] Upgrade path that preserves database/configuration.
- [ ] Versioned upgrade/rollback guidance.
- [ ] Decide signing strategy for distributed builds.
- [ ] Production configuration location and permissions review.

### 10. Multi-computer architecture — post-v1 implementation

Current SQLite deployment is single-PC oriented.

The provider/client implementation is post-v1. Before simultaneous multi-computer production use:

- [ ] **PostgreSQL readiness audit** — inventory SQLite-specific SQL, PRAGMA/schema migration code, local-file assumptions, backup/reset tooling and provider-specific tests before introducing the central provider.
- [ ] Keep Services + `IDbContextFactory<BinTrackerDbContext>` as the business/data-access boundary; do not add a generic Repository layer merely for PostgreSQL migration.

- [ ] Decide whether PostgreSQL remains the target central provider.
- [ ] Implement/test central database provider.
- [ ] Concurrency and locking tests.
- [ ] Connection/configuration deployment.
- [ ] Backup/restore strategy for central database.

## Priority 4 — UI cleanup

Do after the functional/data-integrity items above unless a defect blocks use.

- [ ] Import Review action icons are still too small/cropped, especially container-related actions.
- [ ] Match approved Review mockup rounded metric tiles.
- [ ] Final password-eye/logout artwork polish.
- [ ] General consistency pass for spacing, button sizing and high DPI.
- [ ] Complete hands-on Container Type / Business Information UI validation.

## Before v1.0 acceptance

- [ ] Clean build with zero warnings.
- [ ] Full automated suite passes.
- [ ] Fresh install → first administrator → customer setup → movements → reports → backup/restore acceptance test.
- [ ] Fresh database → real Excel import → balances/reports validation.
- [ ] Forced import rollback test passes.
- [ ] Exact and changed-workbook re-import tests pass.
- [ ] Movement reversal/correction workflow validated.
- [ ] Core reports completed.
- [ ] Dashboard operational pass completed.
- [ ] Production backup/restore validated.
- [ ] Security/hardening review completed.
- [ ] Installer/upgrade tested.


## Post-v1.0 / commercial roadmap

### BinTracker Windows UI v2 / WinUI 3 evaluation

- [ ] After v1, evaluate whether the current WinForms UI is materially holding BinTracker back.
- [ ] Compare WinForms vs WinUI 3 for responsive/high-DPI layout, modern controls/styling, dashboard/chart interaction, drill-through/navigation, accessibility and long-term maintainability.
- [ ] Treat this as an evaluation gate, **not** a predetermined rewrite.
- [ ] If WinForms remains fit for purpose, keep it. If WinUI 3 provides enough concrete benefit to justify migration cost/risk, create a separate Windows UI v2 implementation roadmap.


- **Customer-list-only import mode**: support names-only, code + name, CSV/XLSX customer masters and custom workbook sources.
- **Explicit import intent**: Customers only; Customers + opening balances; Full migration (customers + balances + movements). Customer-only mode reuses matching/normalisation/merge preview but does not require container mapping, B/Fwd, OUT/IN or balance reconciliation.
- **Reusable Import Profiles**: legacy/custom workbook profiles, standard BinTracker import template, configurable mappings for other businesses, and future persistence of legacy token aliases.
- **Opt-in fuzzy customer-match suggestions**, never automatic fuzzy merge.
- Custom Report Designer.
- Legacy/custom report-template import.
- iPhone application.
- Android application.
- Hosted/cloud or centrally managed deployment option.
- Licensing/activation and commercial support tooling.
- Product onboarding/import wizard for new businesses.

### Additional post-v1 candidates recovered from original requirements

- [ ] Customer web portal.
- [ ] Barcode scanning.
- [ ] Multiple depots.



## Historical requirements audit — 16 August 2026

The roadmap was reconciled against the project history rather than only the most recent build notes. Items that must remain explicitly visible:

### Pre-v1 requirements recovered/confirmed

- [ ] **Batch Entry:** verify Esc semantics; clear/reset appropriate fields and return focus after successful entry; resolve crash/power-loss draft persistence.
- [ ] **Customer operations:** sort by code/name/outstanding/credit/last movement; lifetime OUT and IN totals where useful.
- [x] **Customer Statement:** operational save/open/print workflow available from both Customers and Reports.
- [x] **Movement History:** date-range/customer/container/source reporting implemented with PDF/CSV export and current-grid ordering.
- [x] **Monthly Summary:** selected month plus Last Month shortcut, OUT/IN/net and customer/container breakdown implemented and user-accepted.
- [x] **Daily Print Pack:** Outstanding Summary + physical Movement Detail implemented in one audited PDF; acceptance remains.
- [ ] **Movement Correction / Reversal:** linked original/correction records, reason, actor/time, permissions and audit; no destructive edit.
- [ ] **Business Information & Branding:** logo/custom header and reusable output branding.
- [ ] **Email/SMS communications:** Google Workspace email + Texto SMS direction; customer Email/SMS/Opt-out settings; Friday-or-earlier empty-bin reminder direction; manual/bulk/automatic sends; templates; delivery history; retry/idempotency; audit; statement attachment/link decision.
- [ ] **Dashboard:** design before code; charts, useful KPIs, attention states, drill-through, recent activity, by-container view, ageing rule discussion, forecasting/ML hooks and large-monitor layout.
- [ ] **Dashboard future-UI discussion:** explicitly compare what WinUI 3 v2 would improve in Dashboard, Reports launcher, individual report pages and import workflow, and avoid over-engineering WinForms v1 where appropriate.
- [ ] **Backup/recovery:** user backup/restore plus scheduled automatic backups, configurable destination/retention, pre-upgrade backup and recovery drill.
- [ ] **Audit coverage:** maintain an explicit audit-coverage matrix across security/admin/customer/movement/import/report/communication actions.
- [ ] **PostgreSQL/multi-user:** Services + `IDbContextFactory` remains the boundary; no generic Repository layer merely for migration.
- [ ] **Release discipline:** targeted smoke test for business-logic changes; full smoke test for UI changes; full documentation/audit reconciliation as development proceeds; clean Git/release state before milestone closure.

### Post-v1 requirements recovered/confirmed

- [ ] **Windows UI v2 / WinUI 3** evaluation/migration decision after v1 publication.
- [ ] **Customer portal**.
- [ ] **Barcode scanning**.
- [ ] **Multiple depots**.

These are not allowed to disappear merely because a later roadmap summary is shortened.

## Version milestone policy

Milestone numbering follows scope. Substantial workstreams may receive their own `0.x.0` milestone rather than being forced into a fixed number of phases. Dashboard is a likely candidate for its own milestone because it may involve experimentation and multiple iterations.


### Report interaction standard

Detailed report pages treat the on-screen dataset as the operator's printable view: supported column sorting is type-correct, and PDF generation preserves the current displayed order. This standard should be reused by subsequent report pages.


### Interactive report live-filter standard

Outstanding Containers, Daily Movements, Weekly Movements and Movement History use live date/dropdown/result-checkbox refresh with Customer-on-Enter search and no separate Run Report button. Reuse this interaction model for future interactive reports.

### Report output consistency

Detailed report pages treat the current grid order as the operator's chosen report view. PDF and CSV outputs preserve that displayed order. Reuse this rule for future report pages.


### Daily Movements print options

Daily Movements supports optional exported Notes. Notes stay out of PDF and CSV by default to preserve compact operational output, but can be included explicitly when investigation/detail is required.


### Daily Movements adjustment UX

Daily Movements uses a single explicit opening-adjustment control. Opening Adjustment is not duplicated in the Source selector.


### Daily Movements control layout standard

Daily Movements uses separate auto-sized Filter, Options and Actions rows. Reuse this structure when later report pages have enough controls that DPI wrapping could obscure actions.


### Weekly Movements views

Weekly Movements keeps detailed and aggregated use cases together: **Daily Detail** for individual transactions and **Weekly Overview** for Customer/Container OUT, IN and Net totals. PDF/CSV follow the selected view.


### Weekly actual-history semantics

Weekly Movements is explicitly historical/actual reporting: future dates are unavailable, current-week data stops at today, and configured Container Types drive filtering. Predictive future weeks remain reserved for later forecasting/analytics work.


### Mandatory per-build audit discipline

The full audit is part of **every** build, not a periodic cleanup task. No candidate is considered complete until code/state, all Markdown files, roadmap coverage, version references, specifications, known issues, tech debt, test requirements, changelog and current release notes have been reconciled.


### BinTracker product branding

The supplied fish/ice/yellow-bin artwork is the BinTracker **product** identity. v1 uses it for the Windows application icon and restrained in-app branding. This remains separate from the pre-v1 Business Information logo/custom-header system, which brands the operator's own business reports/emails. WinUI 3 v2 can revisit richer product-brand presentation.


### Application branding consistency

BinTracker product icon/logo is now treated as application-shell infrastructure: all Forms inherit the executable icon, while the sidebar displays the product logo. Future Business Information branding remains a separate customer/business-output system.


### Requirements register governance

`docs/RequirementsAcceptanceRegister.md` is the permanent requirements ledger. Roadmap sequencing may be shortened, but registered v1/post-v1/candidate items must not silently disappear. Material scope/status changes require an explicit changelog/decision record.

## Reporting launcher visual consistency

- [x] Reports landing page redesigned to the approved **Quick Reports + Explore Reports** card mock-up, with the approved icon artwork embedded directly from the mock-up.
- [x] Containers is a dedicated left-navigation destination immediately below Customers; Container Types is no longer duplicated in Settings. Non-admin access is view/search only and administrator-only mutation controls remain protected.
