# BinTracker Roadmap

Current planning baseline: **v0.4.0-alpha.24.2.10**

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
- representative individual report windows as v1 reference screens, comparing WinForms vs WinUI 3 for filters, grids, sorting, summaries, exports and richer visualisation.

Only implement after a preferred direction is agreed. Dashboard is intentionally allowed its own milestone because experimentation may materially change scope.

2. **Dashboard operational pass** — validate metrics, add actionable drill-through and recent/container activity.
3. **Email/SMS reminder delivery** — business rules, providers, templates, retries and delivery history.
4. **Movement correction/reversal** — controlled audited reversal/correction workflow.
5. **Production operations** — Backup/Restore, hardening, installer/upgrade and production acceptance.

The remaining importer failure-detail message and deferred Review cosmetics can be handled alongside these phases without reopening the completed core import safety work.


## Current execution order — audited 16 August 2026

This order is the authoritative pre-v1 sequence. Milestone numbers after v0.4 remain flexible and follow genuine scope.

1. **Finish v0.4 Reporting** — Monthly Summary is user-accepted; validate Daily Print Pack, then complete the final report consistency/real-world print acceptance pass.
2. **Batch Entry acceptance cleanup** — verify Esc, post-entry field clearing/focus, and implement/decide crash/power-loss draft recovery before production.
3. **Movement Correction / Reversal** — controlled, linked, audited correction of saved movements; never silently edit/delete history.
4. **Business Information & Branding** — logo, custom header/branding text, and one reusable branding source for reports/statements/email and generated output.
5. **Email, SMS & Customer Communications** — Google Workspace email + Texto SMS direction, manual and automatic reminders, templates, delivery history/retries/audit, statement attachment/link decision.
6. **Dashboard** — mandatory design discussion before implementation: KPIs, charts, forecasting/ML hooks, drill-through, attention states, responsive/large-monitor behaviour, plus explicit WinForms-v1 vs WinUI-3-v2 comparison for Dashboard, Reports launcher, individual report screens and import workflow.
7. **Customer operational analytics/polish** — useful sorting by outstanding/credit/last movement, lifetime OUT/IN totals, and statement workflow integration.
8. **Production Backup / Restore / Recovery** — user backup, validated restore, scheduled automatic backups, retention, pre-upgrade backup and recovery drill.
9. **Security, audit & reliability hardening** — authorization matrix, audit-coverage matrix, secrets, logging, crash/restart, database integrity, Release build and DPI testing.
10. **PostgreSQL / multi-computer readiness** — preserve Services + `IDbContextFactory`; audit SQLite assumptions, central-provider migration, concurrency/configuration and central backup strategy.
11. **Installer / upgrade / deployment** — Windows package, safe upgrades, signing decision and production configuration.
12. **Full v1 acceptance / regression** — fresh install/database, real workbook import, balances, entries, corrections, reports, communications, branding, dashboard, backup/restore, restart/crash and upgrade testing.
13. **v1.0 production release** — BinTracker is accepted as the replacement for the daily Excel workflow.

Post-v1 remains separate: **Windows UI v2 / WinUI 3**, customer portal, barcode scanning, multiple depots and other commercial/expansion work.

## Priority 0 — close data-integrity risks before more features

### 1. Finish Excel Import safety and provenance

The importer is functionally implemented and has been exercised against the current production Excel workbook. Analyse, Map, Review, customer decisions, container mapping, balance reconciliation and transactional Step 4 execution are working.

Remaining:

- [x] **Forced-failure rollback verification** — integration test now forces failure after the final `SaveChangesAsync` but before `CommitAsync` and verifies that customer, movements, ImportRun and completion audit all disappear; exact-source retry remains allowed.
- [x] **`BinMovement.ImportRunId` relational provenance** — nullable FK + index implemented; all new import Adjustment/ExcelImport movements link to their ImportRun, while Manual/Batch rows remain unlinked. Migration V10 safely backfills eligible alpha.19.x `IMPORT-<id>` rows.
- [x] **Correction comparison identity** — previous/proposed positions are now keyed by Customer identity + `ContainerTypeId`, not display labels, eliminating false `Blue` vs `Blue Bin` differences. One-value correction regression requires exactly one genuine changed position.
- [x] **Step 4 correction discovery/UI** — preflight now receives the cutover date, so changed-workbook same-cutover state is surfaced before execution as an explicit **Replace / Correct** action rather than dead-ending at the backend guard.
- [x] **Changed-workbook / same-cutover workflow** — structural CutoverDate detects a changed completed workbook for the same date; Step 4 shows a comparison and requires explicit Replace/Correct. Reconciliation reconstructs the pre-cutover baseline, replaces only prior ImportRun-linked movements, and preserves same-day/later Manual/Batch activity on top of the corrected workbook position.
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

- [x] **Report launcher architecture** — Market Floor remains inline as the primary operational report; detailed reports open in dedicated single-instance, responsive report windows sized from the current monitor working area so data grids grow on larger displays.

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
- [x] **Daily Movements report** — dedicated responsive report window with today/yesterday shortcuts, customer/container/direction/source filters, physical-movement default, optional opening adjustments, typed sorting, audited PDF and CSV preserving the current grid order.
- [x] **Movement History report** — inclusive date range, customer/container/direction/source filters, opening-adjustment opt-in, future-date guards, quick range shortcuts, typed sorting, audited PDF and CSV preserving current grid order.
- [x] **Monthly Summary** — selected-month OUT, IN and net movement totals with customer/container breakdown, This Month/Last Month shortcuts, customer/container/source filters, optional opening adjustments, typed numeric sorting, audited PDF and CSV preserving current grid order.
- [x] **Daily Print Pack** — selected-date Outstanding Summary + physical Movement Detail in one audited PDF; acceptance testing remains.
- [x] **Monthly Summary on-screen interaction** — dedicated responsive window with live dropdown/date/checkbox refresh, Customer-on-Enter search and sortable summary grid.
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

- [ ] Verify and document **Esc** behaviour in each Batch Entry state.
- [ ] After a line is successfully added/committed, clear all entry fields that should not carry forward and return focus to the Customer field/code entry.
- [ ] Decide/implement crash or power-loss draft recovery if required for production. Current in-memory draft does not survive process termination.

This is acceptance/polish work only unless smoke testing exposes another real defect.

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

- [ ] Add controlled correction/reversal workflow for saved movements.
- [ ] Never silently edit/delete the original movement.
- [ ] Preserve original + reversal/correction linkage and audit trail.
- [ ] Decide roles permitted to reverse/correct.

### 5A. Business Information & Branding

This is a **pre-v1 milestone**, not a post-v1 idea.

- [ ] Business logo in Business Information.
- [x] Configurable textual **Default Report Header** already exists in Business Information; retain it as the custom textual header foundation.
- [ ] One authoritative branding model/service for PDFs, statements, email and other generated output.
- [ ] Design storage, file types, dimensions/aspect ratio, fallbacks, header/footer placement and per-output enable/disable behaviour before implementation.
- [ ] Decide how business name, trading name, logo and custom header coexist without duplicate-looking output.
- [ ] Leave room for richer HTML email header/signature treatment without creating a second branding system.

## Priority 2 — communications

### 6. Email, SMS & Customer Communications

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
- [ ] Release build (`Release`, not only `Debug`) acceptance testing.
- [ ] High-DPI regression pass at 100%, 125% and 150%.

### 9. Installer, upgrades and deployment

- [ ] Windows installer/package.
- [ ] Upgrade path that preserves database/configuration.
- [ ] Versioned upgrade/rollback guidance.
- [ ] Decide signing strategy for distributed builds.
- [ ] Production configuration location and permissions review.

### 10. Multi-computer architecture

Current SQLite deployment is single-PC oriented.

Before simultaneous multi-computer production use:

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
- [ ] **Dashboard future-UI discussion:** explicitly compare what WinUI 3 v2 would improve in Dashboard, Reports launcher, individual report windows and import workflow, and avoid over-engineering WinForms v1 where appropriate.
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

Detailed report windows treat the on-screen dataset as the operator's printable view: supported column sorting is type-correct, and PDF generation preserves the current displayed order. This standard should be reused by subsequent report windows.


### Interactive report live-filter standard

Outstanding Containers, Daily Movements, Weekly Movements and Movement History use live date/dropdown/result-checkbox refresh with Customer-on-Enter search and no separate Run Report button. Reuse this interaction model for future interactive reports.

### Report output consistency

Detailed report windows treat the current grid order as the operator's chosen report view. PDF and CSV outputs preserve that displayed order. Reuse this rule for future report windows.


### Daily Movements print options

Daily Movements supports optional exported Notes. Notes stay out of PDF and CSV by default to preserve compact operational output, but can be included explicitly when investigation/detail is required.


### Daily Movements adjustment UX

Daily Movements uses a single explicit opening-adjustment control. Opening Adjustment is not duplicated in the Source selector.


### Daily Movements control layout standard

Daily Movements uses separate auto-sized Filter, Options and Actions rows. Reuse this structure when later report windows have enough controls that DPI wrapping could obscure actions.


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
