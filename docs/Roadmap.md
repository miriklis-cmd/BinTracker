# BinTracker Roadmap

Current planning baseline: **v0.4.0-alpha.21.4.1.1**

This roadmap tracks work that is still relevant. Completed alpha-by-alpha history belongs in `docs/CHANGELOG.md`, not here.

## Current work order

With import integrity/provenance and unsaved Customer/Container-Type protection now in place, the implementation order is:

1. **Reports** — Outstanding Containers, Daily Movements, Movement History, Monthly Summary and Daily Print Pack.

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

## Current execution order

1. Finish **alpha.19.12.3/19.12.4 acceptance** and close remaining importer/customer/container UI defects.
2. **Reports** — historical/as-of-date, Daily, Weekly, Monthly, Outstanding, Statement view/print and Daily Print Pack.
3. **Batch Entry acceptance cleanup** — Esc behaviour, post-entry field reset/focus, crash/power-loss draft decision.
4. **Dashboard** — operational metrics, drill-through, by-container view and useful charts.
5. **Email/SMS** — Google Workspace email + Texto SMS direction, reminder rules/templates/delivery history.
6. **Normal movement correction/reversal**.
7. **Production backup/recovery**, including scheduled automatic backups.
8. **Security/audit hardening** and audit-coverage matrix.
9. **PostgreSQL/multi-computer readiness**, installer/update/deployment and production operations.

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
- [x] **Weekly Movements report** — first-class Monday-to-Sunday selected-week movement detail plus customer/container OUT, IN and net summary, This Week/Last Week shortcuts, filters and CSV export.
- [ ] **Customer Statement view/print workflow** — generate, open/view and print the statement directly from the operational workflow, not only save a PDF.
- [x] **Daily Movements report** — dedicated responsive report window with today/yesterday shortcuts, customer/container/direction/source filters, physical-movement default, optional opening adjustments, typed sorting, audited PDF and CSV preserving the current grid order.
- [ ] **Movement History report** — date range + customer/container/source filters.
- [ ] **Monthly Summary** — selected-month OUT, IN, net movement and useful customer/container breakdowns, including quick access to **last month**.
- [ ] **Daily Print Pack** required by the Functional Specification: Outstanding Summary + Movement Detail.
- [ ] Review whether Daily/Weekly/Monthly query/reporting needs on-screen tables in addition to PDF output.
- [ ] Add CSV/Excel export where operationally useful.
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

## Priority 2 — communications

### 6. Email and SMS reminders

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

### Production backup/recovery

- [ ] User-accessible production Backup.
- [ ] Validated Restore with explicit confirmation.
- [ ] **Scheduled automatic backups** with configurable destination/retention.
- [ ] Backup before schema/database upgrades.
- [ ] Restore verification / recovery drill before production sign-off.

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


### Business Information branding / generated-output identity

- [ ] Add ability to configure a **business logo** in Business Information.
- [ ] Add optional **custom header / branding text** independent of the legal/business name.
- [ ] Design how branding should be reused by **PDF reports, customer statements, emails, reminders and other generated output**.
- [ ] Discuss before implementation:
  - image storage format/location and database-vs-file trade-offs;
  - supported logo dimensions/aspect ratio/file types;
  - fallback when no logo is configured;
  - whether report/email branding can be enabled/disabled per output type;
  - header/footer placement rules;
  - how the business name, logo and custom header interact without duplication;
  - whether email branding should support a richer HTML header/signature later.
- [ ] Implement the branding layer only after the shared behaviour is agreed, so report/email generators consume one authoritative business-branding configuration.


## Post-v1.0 / commercial roadmap

### BinTracker Windows UI v2 / WinUI 3 evaluation

- [ ] After v1, evaluate whether the current WinForms UI is materially holding BinTracker back.
- [ ] Compare WinForms vs WinUI 3 for responsive/high-DPI layout, modern controls/styling, dashboard/chart interaction, drill-through/navigation, accessibility and long-term maintainability.
- [ ] Treat this as an evaluation gate, **not** a predetermined rewrite.
- [ ] If WinForms remains fit for purpose, keep it. If WinUI 3 provides enough concrete benefit to justify migration cost/risk, create a separate Windows UI v2 implementation roadmap.


- Customer-list-only import mode.
- Reusable Import Profiles and standard BinTracker import template.
- Opt-in fuzzy customer-match suggestions, never automatic fuzzy merge.
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


## Version milestone policy

Milestone numbering follows scope. Substantial workstreams may receive their own `0.x.0` milestone rather than being forced into a fixed number of phases. Dashboard is a likely candidate for its own milestone because it may involve experimentation and multiple iterations.


### Report interaction standard

Detailed report windows treat the on-screen dataset as the operator's printable view: supported column sorting is type-correct, and PDF generation preserves the current displayed order. This standard should be reused by subsequent report windows.


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
