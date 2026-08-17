# BinTracker Automated Testing

BinTracker uses separate unit and SQLite integration test projects.

## Unit tests

`tests/BinTracker.UnitTests`

Business-rule and regression coverage includes customer identity, balances, import planning/reconciliation, Market Floor rules/layout policy and other non-database logic.

## Integration tests

`tests/BinTracker.IntegrationTests`

Database-backed coverage includes schema upgrades, movement/balance behaviour and transactional importer behaviour, including transaction-boundary failure injection, relational ImportRunId provenance, and same-cutover replacement preserving same-day/later Manual activity outside the corrected baseline.

## Regression rule

When a real defect is found:

1. reproduce it with an automated test where practical;
2. fix the defect;
3. keep the regression test permanently.

## Current high-value gaps

- broader production-scale/custom workbook fixtures without private business data;
- Release-build acceptance;
- stress coverage for high-density Market Floor days.

## Local validation

Run:

```powershell
.\Build-BinTracker.bat
```

A local candidate is valid only when restore, build and automated tests all succeed with zero warnings.


## UI/business workflow acceptance

Automated integration coverage verifies that a changed workbook preflight supplied with the cutover date returns `RequiresReplacement = true`. Manual UI acceptance must additionally prove that Step 4 uses that state to show **Replace / Correct** before execution.


Correction regression coverage includes the real smoke-test shape: changing one Blue OUT quantity from 1 to 2 must produce exactly one changed customer/container position with a +1 difference.


Import Run history service integration coverage verifies replacement-chain lookup, linked movement detail and Administrator-only access. The WinForms history screen requires manual UI acceptance because this release changes UI.


Replacement integration coverage verifies `CorrectionChangesJson` is persisted by execution. Import History integration coverage verifies the stored snapshot is parsed and exposes previous/corrected/difference values.


The current UI smoke pass includes Import History readability at the operator's DPI plus Customer and Container Types unsaved-change **Save / Discard / Cancel** behaviour. These are manual acceptance checks because they depend on WinForms focus/navigation and layout.


Customer dirty-state protection requires full manual UI acceptance because selection, search/filter events, main-page navigation, logout and FormClosing are WinForms event-order behaviours. Container Types prompt wording and Import History no-wrap metadata are included in the same UI smoke pass.

## Manual testing policy

Every candidate build must be classified explicitly:

- **UI changed → Full smoke test.**
- **Business logic changed → Targeted smoke test** covering the affected workflow/calculation.
- **UI + business logic changed → Full smoke test**, with special attention to the affected logic.
- **Pure internal/refactor → Automated tests** unless a specific operational risk warrants manual verification.
- **Reports/printing changed → Real preview/print test** of the affected report.
- **Importer changed → Real-workbook test** when the behaviour depends on the production workbook/operator flow.

The release response should state `TEST REQUIRED: None / Targeted / Full` and list the exact checks.

## Documentation/audit policy

Every meaningful implementation pass must reconcile the current implementation against:

- `docs/Roadmap.md`;
- `docs/RoadmapCoverageMatrix.md`;
- `KNOWN-ISSUES.md`;
- `TECH-DEBT.md`;
- `TEST-CHECKLIST.md`;
- `docs/FunctionalSpecification.md`;
- `docs/BusinessRules.md`;
- relevant feature docs;
- `docs/RELEASE-NOTES.md`;
- `docs/CHANGELOG.md`.

Completed work is closed/removed from active lists; superseded statements are deleted rather than allowed to contradict current state. Historical release detail belongs in the changelog.

## Conversation-to-requirements reconciliation

Periodically compare the repository plan against the original product requirements and accepted operator decisions so requirements raised during bug-fixing do not disappear merely because they were not initially added to the roadmap.

## Historical reporting coverage

`OutstandingReportSqliteTests` verifies:

- future movements do not leak into an earlier As-of-Date result;
- OUT/IN produce the correct signed position;
- configured Container Types remain separate;
- credits are hidden by default and optionally included;
- customer/container/inactive filters work.

Because alpha.20.0 adds Reports UI, manual acceptance is a **Full smoke test** under the project testing policy.


Outstanding reporting regression coverage also checks customer/container adjacency so a multi-container customer is not visually split into separate large container blocks.


## Report launcher UI acceptance

The report launcher architecture is manually verified because single-instance WinForms window ownership/activation is UI behaviour. Outstanding report calculation remains covered independently by SQLite integration tests.


Report-window UI acceptance includes a laptop-sized display and a larger desktop/27-inch monitor resolution. Verify filters/actions remain visible and the dataset expands with available client space.


## Compile-time dependency wiring

When a WinForms form/service constructor gains a required dependency, audit **all construction sites** in the solution. A build failure caused by a stale constructor call is a regression in dependency wiring and should be fixed before any runtime smoke test.


## Report action layout

Detailed report windows use a two-row control layout when necessary: filters on the first row and report actions on a dedicated second row. At supported DPI/resolutions, button labels must remain fully visible; wrapping may move whole controls but must never hide part of the action row.


## Interactive report sort/print acceptance

For Outstanding Containers:

- Position numeric sort must order by signed numeric balance, not formatted display text (`9`, `8`, `72`, `7` is invalid numeric descending order).
- Sort by Type/Code/Customer/Container should continue to use ordinary text ordering.
- Generate PDF / Generate & Open must preserve the grid's current displayed row order.


CSV preserves the grid's current displayed row order in the same way as PDF. After sorting Position, Type, Customer, Code or Container, exported CSV rows must appear in that order.


## Daily Movements coverage

`DailyMovementsReportSqliteTests` verifies:

- only the selected MovementDate is returned;
- opening adjustments are excluded by default and included only explicitly;
- OUT/IN totals are correct;
- customer/container/direction/source filters work.

Manual UI acceptance verifies responsive layout, Today/Yesterday, numeric Quantity sorting, and PDF/CSV visible-order consistency.


Daily Movements UI acceptance also verifies:

- the literal `Generate & Open` label is visible (ampersand is not consumed as a mnemonic);
- `All directions` is fully readable in the Direction selector;
- Include notes in exports off omits Notes from both PDF and CSV;
- Include notes in exports on adds the Notes column to both PDF and CSV;
- the denser default PDF layout does not reduce operational readability.


Daily Movements source-control acceptance verifies that Opening Adjustment is not present in the Source selector and is controlled solely by **Include opening adjustments**.


The three-row Daily Movements control layout is manually verified at production DPI: filter wrapping and option text must not push the action row beneath the summary/results area.


## Weekly Movements coverage

`WeeklyMovementsReportSqliteTests` verifies Monday-Sunday boundaries, exclusion of opening adjustments, OUT/IN/net totals, summary aggregation and customer/container/source filters.

Manual acceptance verifies This Week/Last Week, responsive layout, Detail/Summary tabs, numeric sorting and CSV visible-order behaviour.


## Weekly Overview export acceptance

- Daily Detail lists each movement row.
- Weekly Overview aggregates Customer + Container Type across the full Monday-Sunday week.
- Example validation: 45 OUT and 45 IN for the same customer/container appears as `OUT 45 / IN 45 / Net 0`.
- PDF and CSV export whichever tab is selected and preserve the current sort of that tab.
- One **Include notes in exports** option controls both PDF and CSV for Daily Detail and is disabled on Weekly Overview.
- `Generate & Open` visibly includes the literal ampersand.


## Weekly future-date/container-filter acceptance

- Date picker cannot select later than today.
- Service queried with a future date clamps to the current week.
- Future-dated movement rows are excluded.
- If the current week has future calendar days, the UI/PDF says activity is through today rather than implying future days had zero movements.
- Container filter is populated from configured Container Types and therefore includes Yellow/Bulk/etc. even when a type has no current outstanding balance.
- Inactive historical container types remain filterable and display `(inactive)`.
- One Include notes in exports option controls both PDF and CSV.


## Daily future-date acceptance

- Daily Movements date picker cannot select later than today.
- A future date supplied directly to the service is clamped to today.
- Future-dated movement rows are not returned by Daily Movements.


## Test visibility boundary

Integration tests should verify public behaviour through registered interfaces and returned results. They should not depend on internal concrete service classes merely to reuse helper methods; duplicate a tiny expected-value calculation in the test when that better preserves the implementation boundary.


## Build audit acceptance gate

Before packaging every candidate:

- enumerate all Markdown files;
- reconcile current-state docs against actual implementation;
- compare Roadmap against Roadmap Coverage Matrix;
- reconcile version references with `Directory.Build.props`;
- confirm Known Issues contains real current limitations only;
- confirm Tech Debt contains unresolved engineering debt rather than already-fixed defects;
- confirm Functional Specification / Business Rules describe current behaviour;
- confirm Test Checklist includes the new candidate's acceptance requirement;
- preserve superseded historical behaviour only in CHANGELOG;
- record the audit in `docs/DocumentationAudit.md`.

Failure of this audit blocks packaging in the same way as a failed automated test.


## Movement History coverage

`MovementHistoryReportSqliteTests` verifies:

- inclusive date-range boundaries;
- default exclusion of Opening Adjustments;
- OUT/IN/net totals;
- customer/container/direction/source filtering;
- future-date clamping;
- reversed-range normalization.

Manual acceptance verifies responsive layout, Last 7 / Last 30 / This Month shortcuts, authoritative Container Type choices, typed Date/Quantity sorting and PDF/CSV visible-order consistency.


## Interactive report refresh acceptance

For Outstanding Containers, Daily Movements, Weekly Movements and Movement History:

- Run Report button is absent.
- Changing date/dropdown/result-affecting checkbox filters refreshes results.
- Typing a Customer does not query on each character.
- Pressing Enter in Customer applies the search.
- Shortcut buttons still refresh immediately.
- Export/PDF continues to use the resulting on-screen dataset/order.


## Report wrapped-layout/customer-cue acceptance

- Weekly Movements at laptop width may wrap filters, but all shortcut/PDF/CSV buttons remain fully visible.
- Outstanding, Daily, Weekly and Movement History visibly explain that Customer search applies on Enter.
- Customer Enter continues to refresh exactly once.
- Live dropdown/date/checkbox behaviour remains unchanged.
- BinTracker executable/window icon uses the supplied product icon and the sidebar displays the supplied product logo without dominating the navigation.


## Weekly wrapped-control layout regression

Weekly Movements controls must remain fully visible when the filter row wraps at laptop width/DPI scaling. The filter/options/action area must contribute its true preferred height before the summary/grid rows are laid out. Test with the Source filter wrapped to a second line and verify all action buttons are fully visible.


## Application icon/branding acceptance

- Launch BinTracker and verify Login shows the BinTracker icon in its title bar and taskbar before authentication.
- Verify Main, Outstanding, Daily, Weekly, Movement History, Import/Admin and other breakout/dialog Forms use the same icon.
- Verify the left navigation shows the BinTracker product logo beside the BinTracker wordmark.
- Verify no form fails to open if icon extraction unexpectedly fails.
