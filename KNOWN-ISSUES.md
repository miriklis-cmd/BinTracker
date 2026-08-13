# Known Issues

Current release: **v0.4.0-alpha.19.1**

This file tracks current defects, incomplete production-critical behaviour, and limitations that a tester/operator needs to know about. Planned enhancements belong in `docs/Roadmap.md`; engineering cleanup belongs in `TECH-DEBT.md`.

## High priority — before v1.0

### Excel Import Wizard transactional execution is newly enabled
**Status:** Alpha validation required  
**Area:** Import

Step 4 now performs the confirmed import inside one SQLite transaction:
- creates explicitly confirmed new customers;
- uses explicitly confirmed existing-customer targets;
- writes opening adjustments so BinTracker reaches Excel B/Fwd;
- writes the workbook OUT and IN as real cutover-day movements;
- records a completed ImportRun/source fingerprint;
- rolls the whole transaction back if any line fails.

Remaining work before v1.0:
- production-scale validation against the full legacy workbook;
- changed-workbook/same-cutover replacement workflow;
- polished import-run history/error reporting;
- explicit relational ImportRunId provenance on generated movements (currently traceable by import reference/notes).

### Sheet classification is implemented in Map but not yet persisted/executed
**Status:** In progress  
**Area:** Import

The Map page now classifies worksheets as Source, Validation, Report or Ignore and defaults the current legacy workbook sensibly (`Update Account` / `Update Cash` as Source; derived report sheets away from import). The classification currently exists only within the open wizard session.

Remaining work:
- validate/confirm classification rules against the real workbook;
- persist the selected mapping into the Review step;
- use only Source sheets during actual import;
- include Validation/Report sheets as reconciliation aids only.

### Changed-workbook re-import/replacement workflow is not implemented
**Status:** Exact re-import protection implemented; replacement workflow required before v1.0  
**Area:** Import / Data Integrity

Completed imports now record an ImportRun and SHA-256 source fingerprint, and exact completed-workbook re-imports are blocked both during preflight and again inside the write transaction. Generated movements are tagged with the ImportRun reference in `ReferenceNumber` / notes.

A changed workbook representing the same cutover date still needs an explicit difference/replacement workflow rather than simply inserting another copy.

See `docs/ReimportSafety.md`.

### Review decisions gate transactional Import
**Status:** Implemented / validation required  
**Area:** Import

Review compares Source-sheet customers against the current BinTracker database and blocks Step 4 until customer decisions, existing-match confirmations, container mappings and reconciliation are resolved. Step 4 revalidates these rules against the live database immediately before the transaction is committed.

### Legacy opening-position execution
**Status:** Implemented / validation required  
**Area:** Import

Review and Step 4 use:

`Opening adjustment = Excel B/Fwd - current BinTracker balance`

then:

`Projected = current + opening adjustment + OUT - IN`

Positive opening adjustments are written as Adjustment/OUT movements; negative adjustments as Adjustment/IN movements. The legacy daily OUT/IN values are then written as ExcelImport movements. All are committed atomically with the ImportRun.

### Market Floor Sheet needs final production validation
**Status:** Fixes implemented / validation required  
**Area:** Reports

Real imported data exposed three report rules that are now corrected:
- same-day import opening adjustments contribute to B/Fwd, not daily OUT/IN;
- Cash/COD credits remain in the Cash section; only Account credits use the separate CREDIT block;
- reverse-side Account customers are split across two columns with Cash/COD in a third so the report stays front + back rather than spilling to a third page.

The front page now uses adaptive typography based on row load for better page utilisation/readability.

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

### Import Review icon/tile polish remains
**Status:** Deferred to next UI cleanup pass  
**Area:** UI

The importer is functionally usable, but the approved Review mockup is not yet matched perfectly:
- Containers tile and Map container action icon can still appear too small/cropped;
- action icons are smaller than the approved mockup;
- Review metric tiles still use square rather than rounded corners.

This is intentionally deferred while transactional import execution is completed.

## Recently resolved

- Review warning copy no longer says Import is disabled; Step 4 is live and unresolved items now tell the operator to resolve blockers before continuing.
- Analyse duplicate warning triangle was still present because the dynamic warning text embedded its own icon; only the dedicated warning icon remains.
- First-run administrator Cancel/Create buttons now use a fixed footer grid so both buttons align to the same baseline and height.
- Market Floor same-day import opening adjustments now count as B/Fwd rather than physical daily OUT/IN.
- Market Floor Cash/COD credits remain in the Cash area; the separate CREDIT block is Account-only.
- Market Floor reverse side now uses two Account columns plus one Cash/COD column to keep the report to two physical pages.
- Customer movement history/statements now label import adjustment rows as Opening adjustment (OUT/IN), rather than OUT (Taken)/IN (Returned).

- Step 4 transactional execution is now enabled: confirmed customer creation, opening adjustments, cutover-day OUT/IN movements, completed ImportRun recording and atomic rollback are implemented.
- Step 4 now revalidates the workbook SHA-256 immediately before writing and re-checks exact re-import protection inside the transaction.

- Analyse warning showed two exclamation-triangle icons because both the dynamic text and warning layout supplied one; the text no longer embeds its own icon.
- Container summary/action icons could crop against their image bounds; icon loading now preserves transparent inset space and the Map container action has extra width/padding.
- Review-card secondary grey text was inconsistent and sometimes repeated the card subject awkwardly; all six cards now use explicit concise secondary metrics.

- Review cards were still overcrowded at normal DPI; they now use one strong primary metric plus one short secondary label.
- Review action buttons, especially Map container and the reconciliation viewer, could clip icon/text; widths, icon sizing, padding and button height are now explicit.
- Three xUnit2031 warnings in reconciliation tests were removed by using Assert.Single predicate overloads.
- Analyse warning layout now keeps warning text aligned beside the warning icon instead of allowing wrapped text to begin beneath the triangle.

- Review summary now uses the actual icon artwork extracted from the approved original mockup rather than recreated approximations.
- Review cards now match the mockup information hierarchy: primary values are bold/dark and secondary values are smaller grey text.
- Review action buttons now include pending counts and the full `View balance reconciliation larger...` label, with wider fixed sizing so text cannot clip.

- Review icons are now embedded raster PNG assets matching the approved mockup style; the runtime vector icon renderer has been removed.
- Review action buttons were too narrow for icon + label at normal DPI; widths/heights/padding are now explicit.
- Reconciliation applied customer-confirmation blocking before container resolution and cutover maths, causing pending rows (including CLAMMS Blue/Bulk/Yellow) to show blank containers and em-dashes for opening adjustment/projected. Container resolution and preview maths now run before the confirmation blocker is applied.

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
