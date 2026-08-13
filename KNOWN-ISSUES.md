# Known Issues

Current release: **v0.4.0-alpha.18.6**

This file tracks current defects, incomplete production-critical behaviour, and limitations that a tester/operator needs to know about. Planned enhancements belong in `docs/Roadmap.md`; engineering cleanup belongs in `TECH-DEBT.md`.

## High priority — before v1.0

### Excel Import Wizard is analysis-only
**Status:** In progress  
**Area:** Import

The wizard can read `.xlsm` / `.xlsx`, analyse workbook structure, detect Buyer/customer occurrences, identify snapshot-style B/Fwd / OUT / IN / Total rows, and show duplicate diagnostics. It does **not** yet write customers, opening positions or movements to the database.

Remaining work:
- customer merge/create decisions for new candidates;
- container mapping;
- opening-position execution model;
- transactional Import step;
- rollback/error report.

### Sheet classification is implemented in Map but not yet persisted/executed
**Status:** In progress  
**Area:** Import

The Map page now classifies worksheets as Source, Validation, Report or Ignore and defaults the current legacy workbook sensibly (`Update Account` / `Update Cash` as Source; derived report sheets away from import). The classification currently exists only within the open wizard session.

Remaining work:
- validate/confirm classification rules against the real workbook;
- persist the selected mapping into the Review step;
- use only Source sheets during actual import;
- include Validation/Report sheets as reconciliation aids only.

### Re-import duplicate protection is not implemented
**Status:** Required before Import can be enabled  
**Area:** Import / Data Integrity

BinTracker must never silently apply the same legacy import twice. Before Step 4 is enabled, successful imports need Import Run/source-fingerprint tracking and import-generated movements/opening positions must be traceable to that run.

Exact re-imports must be blocked by default. Changed workbooks representing the same cutover date must go through an explicit difference/replacement workflow rather than simply inserting another copy.

See `docs/ReimportSafety.md`.

### Review is read-only; Import execution remains disabled
**Status:** In progress  
**Area:** Import

Review now compares Source-sheet customers against the current BinTracker database and flags Existing, New, Type mismatch and Source conflict states. Matching ignores case, spacing and punctuation when an exact code match is unavailable. Legacy Buyer prefixes such as `(Bulk)` and `(Y)` are separated from customer identity; `(Bulk)` resolves to Bulk Bin and `(Y)` resolves to Yellow Bin when those container types exist.

Import remains intentionally disabled until:
- new-customer names/actions are confirmed;
- container types are mapped;
- opening positions and cutover movements can be applied transactionally.

### Legacy opening-position execution is not yet committed
**Status:** Reconciliation planner implemented / database execution pending  
**Area:** Import

Review now calculates:

`Opening adjustment = Excel B/Fwd - current BinTracker balance`

then:

`Projected = current + opening adjustment + OUT - IN`

The projected result must reconcile to Excel. This prevents existing test/live balances from being blindly added on top of the workbook balance. Database writes remain disabled until re-import provenance, customer confirmation and remaining container mappings are complete.

### Market Floor Sheet needs production-scale validation
**Status:** Pending real-data validation  
**Area:** Reports

The two-page A4 portrait Market Floor Sheet exists, including Account/Cash grouping, credits, reverse-side B/Fwd and special containers. It still needs validation against the full legacy workbook dataset after import mapping is complete.

## Medium priority — before production acceptance

### Batch Entry draft is not crash/power-loss persistent
**Status:** Known limitation  
**Area:** Batch Entry

Unsaved draft lines survive navigation and logout/login within the running application, but they are not persisted to disk. A crash, power loss or forced process termination loses the draft.

### Movement correction/reversal workflow is not implemented
**Status:** Planned before/around production acceptance  
**Area:** Movements / Audit

Saved movements are auditable, but there is not yet a dedicated operator/admin workflow for correcting an erroneous movement while preserving the original entry and reversal trail.

### Dashboard attention logic is quantity-focused
**Status:** Partial implementation  
**Area:** Dashboard

`Requires Attention` currently focuses on configured outstanding quantity. More nuanced age-based attention logic remains to be completed/validated.

### Production Backup / Restore and deployment are not complete
**Status:** Planned before v1.0  
**Area:** Operations

Developer-only SQLite backup/load/fresh-database tools now exist for import testing. A polished production backup/restore workflow, retention policy, installer/deployment packaging and production upgrade guidance are still not complete.

## Low priority / cosmetic

### Password eye and Logout artwork is functional but not final
**Status:** Accepted for now  
**Area:** UI

The controls work correctly, but the current custom-drawn artwork was accepted as functional rather than final visual polish.

## Recently resolved

- Balance Reconciliation headers repeated the formula already shown above the grid, making headers excessively tall; headers are now concise again.
- Step 3 metric cards could clip at normal DPI because the summary ribbon was too short; the ribbon is taller and has more internal icon/text space.
- Alpha.18.5 used Unicode stand-ins for Review icons instead of the approved mockup icon set; Review now uses custom-drawn database, people, check-circle, person-plus, container, scales and expand icons matching the mockup semantics.

- Step 3 Review summary was information-dense and consumed too much scanning effort; it now uses six compact visual metric cards and a simplified action row.
- The Balance Reconciliation larger-view action was easy to miss at the bottom of the tab; it is now a persistent top-level Review action and the normal reconciliation grid gets more height.
- Password visibility toggle artwork has been updated to the requested filled eye / eye-slash convention.

- Step 4 preflight could terminate BinTracker with an unhandled `IOException` when Excel/another process held the workbook open; it now returns safely to Review with a retry message.
- Balance Reconciliation remained too cramped for practical review even after the collapse fix; Review summary/header were compacted and a full-size reconciliation viewer was added.

- Step 3 Customer Matches / Balance Reconciliation card inherited `AutoSize=true` from the generic Card helper, collapsing the tab/grid area to roughly one visible row despite `Dock=Fill`.

- Alpha.18.1 evaluated Step 3 readiness before local `blockers` and `reconciliation` variables were declared, causing two CS0841 WinForms build errors.

- Step 3 Balance Reconciliation was vertically clipped at the bottom of the Review page.
- Step 3 could remain unable to advance to Step 4 even when all customer/container decisions were resolved and reconciliation was fully ready.

- Existing-customer match dialog could show a blank proposed customer when the matched record was inactive; inactive matches are now visible and labelled.
- Existing-match decision values displayed raw enum names such as `AcceptMatch`; UI now uses `Accept match` / `Override match`.
- Import had no durable source provenance or exact-file re-import detection; ImportRuns schema and SHA-256 preflight are now in place.

- Developer database staging/restart dialog rendered literal `\\n` sequences instead of line breaks.
- Confirm New Customers bulk-action button text was clipped.
- Automatic existing-customer matches had no explicit confirmation/override workflow; Review now requires confirmation before Import readiness.

- Fresh-database reconciliation test still expected `NewCustomerPendingConfirmation` after an explicit Create decision; expected status is now `Ready`.

- Fresh-database reconciliation unit test still assumed pre-alpha.16 automatic new-customer inclusion; it now supplies an explicit Create decision.

- Alpha.16 customer-decision reconciliation used a conditionally assigned local variable and failed Services compilation with CS0165.

- New customers had no explicit Create/Skip confirmation workflow; Review now supports editable names, Create/Skip decisions, bulk actions and retained wizard state.

- Alpha.15 packaged the manual container-mapping calls without the `containerTokenMappings` wizard-state field, causing nine WinForms CS0103 build errors.

- Unknown legacy container tokens could be detected but not resolved inside the Import Wizard; Review now supports manual token-to-Container-Type mapping.

- Unprefixed legacy rows were treated as unresolved containers; they now default to standard Blue Bin.
- Unknown explicit bracket/container tokens could not be distinguished from missing hints; they are now hard blockers requiring mapping.

- Step 3 Review still required horizontal scrolling after alpha.13.5; wizard widened and Review/Reconciliation grids now use fill sizing with wrapped values.
- Developer Database Tools still clipped actions at normal DPI; dialog widened/tallened and action column expanded.

- Step 3 Review columns and row values were clipped at normal window size.
- Developer Database Tools content was clipped vertically and hid actions.
- BalanceService integration tests emitted xUnit2031 analyzer warnings.


- `BalanceService` follow-up ID filtering triggered a `ReadOnlySpan<int>` EF parameter-extraction failure; small customer/container lookup tables are now loaded directly after balance aggregation.

- Import Review crashed when `BalanceService` used an EF Core/SQLite-untranslatable navigation `GroupBy` projection; aggregation now uses scalar IDs in SQL and resolves display names after materialisation.

- Review matched normalized customer variants but still emitted separate rows (`S & J` and `(Bulk) S&J`); grouping now uses the normalized customer key before matching.

- Review legacy-variant tooltip used invalid `DataGridViewRow` API and failed the WinForms build.

- Existing BinTracker balances could have been interpreted as additive during migration; Review now plans target reconciliation to Excel B/Fwd/Total.

- Developer import testing required manual database-file management; Settings now provides Backup / Load / Fresh test database tools.

- Legacy spacing/punctuation variants could create duplicate customer candidates (`S & J` vs `S&J`).
- Review grid required horizontal scrolling and clipped Existing customer/type details.
- Legacy `(Y)` container hint displayed as raw `Y` instead of Yellow Bin.


- Legacy Buyer prefixes were incorrectly treated as part of the customer identity (for example `(Bulk) Clamms`).
- Review status `Existing — match` text clipping.

- Map classification combo display/state loss after Back navigation.

- Review unit-test target-typed `new()` compile failure.

The following older issues are no longer active and should not be treated as current defects:

- Customer lower-panel whitespace/clipping.
- Customer action buttons disappearing.
- Recent Movement History date/direction width.
- Page title text clipping.
- Logout caption clipping/functionality.
- Single Entry alignment/reset-after-save.
- Business Information bottom button clipping.
- Excel Import Wizard missing Browse/Analyse controls.
- Duplicate Analyse button in Import Wizard.
- Import Wizard stepper square-number styling.
- Build migration tests hard-coded to schema version 6.
- ClosedXML row/column compile errors.
- Hard-coded app/build release version.
- Import Wizard Analyse footer clipping after analysis.
- Import Wizard progress subtitle descender clipping.
- Map Suggested reason truncation for default legacy mappings.
- Oversized Business Information Close button.

Resolved details remain available in `docs/CHANGELOG.md` and release notes.
