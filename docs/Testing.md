# BinTracker Automated Testing

## Multi-user readiness regression

SQLite integration tests exercise retry identity, different-payload rejection, stale-edit rejection, current-cutover ownership and schema migration. They protect provider-neutral semantics but are not evidence of PostgreSQL/API execution; that requires a real fixture.

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

Normal-cutover reconciliation coverage verifies `OpeningReconciliationChangesJson` persists every non-zero approved opening adjustment and that Import History exposes previous BinTracker position, Excel B/Fwd/target and adjustment. NULL means the historical build did not capture this provenance; `[]` means capture occurred and there were no non-zero opening adjustments.


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

Detailed report pages use a two-row control layout when necessary: filters on the first row and report actions on a dedicated second row. At supported DPI/resolutions, button labels must remain fully visible; wrapping may move whole controls but must never hide part of the action row.


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

- Open Movement History from Reports and confirm it replaces the main content area at full size rather than opening a floating window. Navigate away and back; confirm one clean page instance and all filters/actions remain functional.
- At the normal maximized production width, confirm predictable columns are compact, Customer/Status/Notes use the available remainder, ordinary values are readable and no unnecessary horizontal scrollbar appears.
- Resize narrower and confirm useful minimum widths are retained before horizontal scrolling appears. Confirm rows stay single-height at supported DPI settings.
- Confirm IN uses a restrained green badge, OUT a restrained red badge, and both reversed originals and reversal rows use amber/orange Status badges. Select each row type and confirm badge and row text remain readable.
- Hover long/truncated Status and Notes values and confirm the full text tooltip. Confirm derived Status wording and persisted ledger Notes are unchanged.
- Apply a customer filter resolving to exactly one customer and confirm PDF and CSV suggested filenames use its sanitized stable code. Confirm empty/unfiltered and multi-customer results use the generic filename.

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
- Verify Main, integrated report surfaces, Import/Admin and other dialog Forms use the same icon.
- Verify the left navigation shows the BinTracker product logo beside the BinTracker wordmark.
- Verify no form fails to open if icon extraction unexpectedly fails.


## Sidebar wordmark clipping regression

- Verify the full `BinTracker` wordmark is visible at the standard laptop resolution/DPI.
- Verify logo and wordmark remain vertically aligned.
- Verify the wider sidebar does not cause navigation labels or main content to clip.
- Verify logo still reads cleanly against the navy background.


## Customer Statement generate/open workflow

- Select a customer and open Customer Statement.
- Confirm From/To cannot be moved beyond today.
- Confirm an invalid From/To range is rejected.
- Confirm **Generate PDF** prompts for a PDF location and saves a valid statement.
- Confirm **Generate & Open** does not prompt for a save location and opens the generated PDF in the Windows default PDF application.
- From the opened PDF viewer, confirm the statement can be printed normally.
- Confirm the generated statement still uses the selected customer and selected period.


## Customer Statement Reports launcher

- Open Reports → Customer Statement.
- Confirm typing Customer text does not search on each keystroke; Enter applies the search.
- Confirm Include inactive refreshes the customer list.
- Confirm double-click and Customer Statement button launch the same period/generation workflow used from Customers.
- Confirm Generate PDF / Generate & Open results are identical regardless of whether the workflow was entered from Customers or Reports.


## Customer Statement owner compile boundary

The shared Customer Statement workflow accepts `IWin32Window` as its owner. Callers must explicitly provide a compatible `IWin32Window`; do not use `FindForm() ?? this` where the operands have different concrete types (`Form` and `UserControl`).


## Monthly Summary coverage

`MonthlySummaryReportSqliteTests` verifies:

- inclusive calendar-month boundaries;
- default exclusion of Opening Adjustments;
- OUT / IN / Net calculations;
- customer/container/source filtering;
- future-month clamping to the current month and activity-through-today semantics.

Manual acceptance verifies This Month / Last Month shortcuts, authoritative Container Type choices, live filter behaviour, Customer-on-Enter search, numeric sorting and PDF/CSV visible-order consistency.


## CSV export audit coverage

Manually export CSV from Outstanding Containers, Daily Movements, Weekly Movements (both views), Movement History and Monthly Summary. Confirm each successful export creates its report-specific `*_CSV_EXPORTED` AuditEvent with filename, row count and relevant report/filter context.


## Build tooling resilience

Build acceptance includes running `Build-BinTracker.bat` repeatedly from a normal Windows developer shell.

Verify:
- the header reports the actually resolved compatible installed SDK (currently 10.0.400 on the development PC);
- stale build-server shutdown does not block the build;
- restore/build/test complete with MSBuild server/node reuse disabled;
- the build no longer intermittently reports MSB4242 SDK Resolver Failure / worker node shutdown under normal use;
- the project continues targeting net8.0/net8.0-windows without requiring an uninstalled restrictive SDK pin.


## Build script failure-path regression

For build-tooling changes, verify both success and failure paths:

- normal build reports the installed/resolved SDK and completes successfully;
- a deliberately invalid solution/project argument returns `BUILD FAILED`;
- restore failure stops immediately and does not continue to build/test;
- build failure stops immediately and does not continue to tests;
- test failure returns `BUILD FAILED`;
- the script must never print `BUILD SUCCESSFUL` after any failed dotnet command.


## Stale global.json self-heal regression

- Place the exact alpha.23.3 generated `global.json` beside `Build-BinTracker.bat`.
- Run the BAT and confirm it deletes that obsolete file, resolves the installed SDK and continues.
- Place a different/user-managed `global.json` beside the BAT and confirm BinTracker refuses to delete it and reports BUILD FAILED.
- Force a restore/build/test failure and confirm the `command || goto :fail` path prints BUILD FAILED and never BUILD SUCCESSFUL.


## Daily Print Pack acceptance

- Reports inline Market Floor and Daily Print Pack date pickers cannot select a future date.
- Daily Print Pack Outstanding Summary is calculated as at the selected date.
- Movement Detail contains physical movements only; Opening Adjustments are excluded.
- Generate PDF and Generate & Open produce one readable two-section PDF.
- `DAILY_PRINT_PACK_GENERATED` is written exactly once per generated pack.
- Real preview/print validation is required before the Reporting milestone closes.

## Mechanical audit regression

Run `Audit-BinTracker.ps1` and confirm it rejects a deliberately stale README/current Release Notes/Roadmap baseline, unexpected `global.json`, or mismatched Version/InformationalVersion, and passes after reconciliation.


## Requirements register audit regression

- Confirm every table requirement ID in `docs/RequirementsAcceptanceRegister.md` is unique.
- Confirm Scope/Status values are from the approved register enums.
- Remove or duplicate a required ID in a temporary copy and confirm `Audit-BinTracker.ps1` fails.
- Introduce a known stale phrase (old SDK pin, stale importer remaining item, separate Run Report expectation) and confirm the audit fails.


## Reports landing-page redesign acceptance

Verify the approved alpha.24 Reports mock-up implementation:

- Quick Reports shows exactly two prominent side-by-side cards: Market Floor Sheet and Daily Print Pack.
- Quick Report cards use the approved report icons, Date selector, Generate PDF and blue Generate & Open action.
- Explore Reports is 3 columns × 2 rows at normal desktop width and contains the six approved report cards.
- Every Explore Reports card uses the approved icon artwork and Open → footer action.
- Report cards open the accepted integrated main-workspace report pages; no report business/export behavior is duplicated or replaced.
- Reports header subtitle reads `Generate operational sheets and explore detailed reports.`
- Normal maximized 1080p desktop must not show a Reports landing-page scrollbar.
- Generate PDF must show its document icon; Generate & Open must stay on one line; every Explore Open button must render the full word `Open` without clipping.
- Test at 100%, 125% and 150% Windows display scaling.
- Verify Containers appears immediately below Customers in the left navigation for every signed-in role; non-admin users receive a clearly read-only view and Settings does not duplicate Container Types administration.

## Movement correction/reversal authorization regression

Integration coverage must verify Operator/Admin ordinary-reversal authorization for Manual and Batch movements, Viewer denial, generic-workflow denial for Opening Adjustment and Excel Import rows, immutable original/reversal linkage, double-reversal protection and reversal-of-reversal protection. Manual Windows acceptance verifies role-based action visibility and the sensitive-source operator messaging.
