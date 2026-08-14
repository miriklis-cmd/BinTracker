# BinTracker Roadmap

Current planning baseline: **v0.4.0-alpha.19.11.3**

This roadmap tracks work that is still relevant. Completed alpha-by-alpha history belongs in `docs/CHANGELOG.md`, not here.

## Priority 0 — close data-integrity risks before more features

### 1. Finish Excel Import safety and provenance

The importer is functionally implemented and has been exercised against the current production Excel workbook. Analyse, Map, Review, customer decisions, container mapping, balance reconciliation and transactional Step 4 execution are working.

Remaining:

- [x] **Forced-failure rollback verification** — integration test now forces failure after the final `SaveChangesAsync` but before `CommitAsync` and verifies that customer, movements, ImportRun and completion audit all disappear; exact-source retry remains allowed.
- [x] **`BinMovement.ImportRunId` relational provenance** — nullable FK + index implemented; all new import Adjustment/ExcelImport movements link to their ImportRun, while Manual/Batch rows remain unlinked. Migration V10 safely backfills eligible alpha.19.x `IMPORT-<id>` rows.
- [x] **Correction comparison identity** — previous/proposed positions are now keyed by Customer identity + `ContainerTypeId`, not display labels, eliminating false `Blue` vs `Blue Bin` differences. One-value correction regression requires exactly one genuine changed position.
- [x] **Step 4 correction discovery/UI** — preflight now receives the cutover date, so changed-workbook same-cutover state is surfaced before execution as an explicit **Replace / Correct** action rather than dead-ending at the backend guard.
- [x] **Changed-workbook / same-cutover workflow** — structural CutoverDate detects a changed completed workbook for the same date; Step 4 shows a comparison and requires explicit Replace/Correct. Reconciliation reconstructs the pre-cutover baseline, replaces only prior ImportRun-linked movements, and preserves same-day/later Manual/Batch activity on top of the corrected workbook position.
- [ ] **Import Run history/details UI** — show source file, SHA-256, cutover date, user, counts, status and generated records.
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

### 2. Protect unsaved customer edits

- [ ] Track dirty state for all editable customer fields.
- [ ] Prompt **Save / Discard / Cancel** when switching customer, searching/filtering away, starting New Customer, navigating away, logging out or closing the application.
- [ ] Save button should clearly indicate when changes are pending.
- [ ] Add regression tests where practical.

This is a direct data-loss/usability issue and should be completed before further Customer features.

## Priority 1 — finish the core operational product

### 3. Reports

Implemented:

- [x] Customer Statement PDF with container-by-container running balances.
- [x] Market Floor Sheet, front + reverse.
- [x] Market Floor Account/Cash rules, Account-only separate credit section and special containers.
- [x] Blue implicit / Yellow explicit floor reporting.
- [x] import opening adjustments treated as B/Fwd rather than physical daily movement.
- [x] adaptive Market Floor sizing using the current real workbook.
- [x] Market Floor generation auditing.

Still required:

- [ ] **Outstanding Containers report** — current outstanding position by customer/container, filterable and exportable/printable.
- [ ] **Daily Movements report** — movement detail for a selected day, including direction, container, customer, reference, source and user.
- [ ] **Movement History report** — date range + customer/container/source filters.
- [ ] **Monthly Summary** — monthly OUT, IN, net movement and useful customer/container breakdowns.
- [ ] **Daily Print Pack** required by the Functional Specification: Outstanding Summary + Movement Detail.
- [ ] Review whether Daily/Weekly/Monthly query/reporting needs on-screen tables in addition to PDF output.
- [ ] Add CSV/Excel export where operationally useful.
- [ ] Stress-test Market Floor with a genuinely high Yellow-bin day; adaptive sizing is accepted for now but remains a real-world validation item.

### 4. Dashboard

Current dashboard is only the first operational summary: Returned Today, Taken Today, Outstanding and Requires Attention.

Required dashboard pass:

- [ ] Validate the meaning of each headline metric and make container/customer scope clear.
- [ ] Make **Requires Attention** useful, not only quantity-threshold based.
- [ ] Add actionable customer/container attention list with drill-through.
- [ ] Add recent movement/activity panel.
- [ ] Add useful by-container summary (Blue / Yellow / special types as appropriate).
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

- [ ] Define reminder business rules: who gets reminded, trigger/age/quantity thresholds and manual vs automatic sending.
- [ ] Email provider integration.
- [ ] SMS provider integration.
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
