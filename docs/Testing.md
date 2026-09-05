# BinTracker Automated Testing

## Characterization-before-change

Before changing accepted behaviour or placing new authority beside it, identify existing tests that precisely characterize the affected path. Where coverage is inadequate, add characterization coverage and run it before the change, then rerun it afterward. If the existing behaviour is intentionally defective, characterize it where useful and add a separate expected-behaviour regression test; do not preserve a known defect merely because it was characterized.

## Structured-input fail-closed stress testing

Parsers, deserializers, importers, migration readers, recovery-manifest readers and structured persisted-input boundaries require adversarial malformed-input coverage appropriate to their actual format. Input must be fully accepted and validated or fail with a controlled/stable outcome and no partially accepted or persisted state. Relevant cases include truncation, missing/extra fields, duplicate keys or records, wrong types, unsupported enum values, invalid IDs/FKs/dates, numeric overflow/extremes, empty/null/whitespace edges, malformed JSON or worksheet/database graphs, inconsistent manifest/checksum metadata, and cancellation/failure during an operation. NaN/Infinity and other format-specific cases apply only where the boundary actually accepts those representations.

For schema 16 -> 17, the structured input is the persisted legacy database graph. Migration coverage therefore includes malformed correction/reversal ownership, cross-domain ImportRun relationships, invalid operation kinds/schema state, partial lineage artifacts, prerequisite tampering and transaction-stage failure injection. An undefined persisted enum value in the source schema is hostile/invalid input: it must not acquire meaning merely because a later schema allocates the same number. Every case must classify deterministically or fail closed without a partially committed schema 17.

## Logical-lineage implementation and acceptance gate

BT-CORR-018..033 is documentation-frozen. Dormant Core contracts, migration-safety infrastructure, schema-17 migration/postflight and a validation-gated CURRENT-root resolver exist under isolated tests. Resolver coverage exercises non-forgeable success construction, current membership/pointers, required current ledger ownership/roles/introductions, exact root-batch or null-single original membership, preserved ReadOnly reason, unique non-negative ordinals, nonprojectable statuses and undefined relevant enums. An unrelated historical-only link does not determine ordinary current projectability, while migration postflight remains globally strict. Ordinary resolution selects only `CurrentGenerationNumber` under one consistent read boundary; it does not scan full history. Normal startup remains schema 16 and production lineage authority is not activated or accepted.

Corrected dormant IMP-05 automated coverage exercises authoritative fact materialization, an infrastructure-internal materializer and non-forgeable trusted snapshots/plans, complete existing/plan-local result pointer shapes, explicit whole-root decisions for every Reversed line, mixed Corrected/Restored/RemainReversed results, no-op, every later-generation action, absent/clear/value normalization, exact AppliedFieldMask selection including selected-equal and override-free Restore, generic ImportRun/ExcelImport/Adjustment exclusion, separate authoritative business-date input, master-data activity, physical-output eligibility and predicate failures. IMP-05C adversarial unit and disposable-schema integration coverage corrupts each current reversal-pair fact independently: wrong/null `ReversesMovementId`, same direction, customer/container/quantity mismatch, non-Manual source, physical-batch or ImportRun membership, and future terminal/LastEffective/Active-effective dates. Positive RemainReversed, standalone Restore, whole-root Restore and mixed Corrected+Restored cases remain. Integration tests prove the materializer uses the validated current root and exact current persisted pair under one SQLite read transaction, ignores unrelated historical-only rows and performs no writes. The original IMP-05 pre-edit execution ordering was missed; IMP-05B recovery remains recorded truthfully, while IMP-05C ran the 37-test alpha.8 characterization before correction editing. Independent review approved the IMP-05 boundary.

IMP-06 integration coverage proves the persistence-boundary audit appender requires an active caller-owned transaction, leaves one new AuditEvent Added and unsaved, persists exactly once when the caller saves/commits, rolls back together with legitimate sibling state saved in the same transaction and rejects a missing transaction without tracking. A separate regression proves existing independent `AuditService.WriteAsync` still creates and saves its own event. Dormant schema-17 coverage proves unique structured legacy association (`Legacy_audit_mapping_requires_one_unique_complete_structured_match`), required audit column/index presence (`Schema_17_contains_required_tables_indexes_and_restrict_membership_fk`) and the RESTRICT operation FK plus duplicate-primary-audit rejection (`Audit_operation_association_is_restrict_foreign_key_and_unique`). The unified mutation writer now consumes this primitive only through explicit schema-17 composition; normal runtime and operator acceptance remain pending.

IMP-07 satisfied BT-REL-011 immediately before its first production edit with 17/17 `MovementEntryCharacterizationTests` passing, 0 failed/skipped. Its reviewed schema-17 integration class passes 18/18 and proves exact native Single/Batch generation zero; first-successful request-order ordinals; identical and reordered retries without rewrite/duplication; changed-payload conflicts; zero artifacts after authorization/master-data rejection; schema-16 and malformed-schema-17 fail-closed behavior before new physical persistence; rollback after physical, mid-lineage, post-lineage, null-introduction-link and audit failures; unchanged migrated Single/Batch `MigrationBaseline` retries; current-root resolution; and AlreadyComplete acceptance of native `Initial` roots. The complete migration class passes 55/55, the provider-neutral contract/current-root unit filter passes 31/31, and the adjacent integration filter passes 59/59, all with 0 failed/skipped. A release `dotnet build BinTracker.sln --no-restore` passed with 0 warnings/errors, and the mechanical audit plus `git diff --check` passed. This is targeted/adjacent automated and release-build evidence after independent source review, not the canonical BAT, retained-production-database rehearsal, runtime activation or Windows/operator acceptance.

Unified schema-17 mutation coverage exercises Correct, Reverse and Restore persistence; whole-root mixed decisions; exact persisted generation-line identity; canonical replay/idempotency and expected-generation CAS; operation/audit uniqueness; rollback/failure injection; optional physical output; target-root native audit-health isolation; and primary audit before/after business-state completeness. Deterministic injected SQLite busy/locked failures prove exhausted contention becomes PersistenceFailure rather than stale-generation or integrity failure. The canonical `Build-BinTracker.bat` passed source audit, restore and Debug build with 0 warnings/errors; 279/279 UnitTests and 259/259 IntegrationTests passed, totaling 538/538 with 0 failed/skipped. This does not prove retained-production-database rehearsal, runtime schema activation, projection/UI behavior, packaging or Windows/operator acceptance.

The dormant corrected-projection authority has 10 focused schema-17 integration tests covering Active, Reversed, repeated correction/current generation, mixed Active/Reversed, Restore, RemainReversed, ReadOnly projection with mutation still prohibited, Adjustment/ExcelImport union, PositionAsOf boundaries and signs, corrected-dimension/date relevance, relevant Invalid/incomplete failure, unexpected unrooted ordinary failure, provably disjoint corrupt-root isolation, unknown relevance and non-registration/schema-16 rejection. The adjacent schema-17 integration set passed 126/126, provider-neutral lineage/planner unit set 63/63 and unchanged alpha.8 correction/balance/report characterization set 68/68. Canonical `Build-BinTracker.bat` then passed audit/restore/build with 0 warnings/errors and 279/279 UnitTests plus 274/274 IntegrationTests, totaling 553/553 with 0 failed/skipped. This remains static/automated evidence only: no consumer cutover, retained-database rehearsal, package, UI/manual or operator acceptance occurred.

Normal composition still supplies no-op initial-lineage and mutation writers and remains schema 16. Explicit isolated schema-17 composition alone exercises the SQLite writers; tests must continue proving that default writers perform no schema probe/query/write. Core construction validation remains provider-neutral, Services remains client-neutral authority, and Data owns SQLite mechanics. No PostgreSQL/API/web/mobile/handheld execution evidence is implied.

Windows acceptance retains Batch #30 and covers RemainReversed, Restore, mixed dates, repeated whole-root correction, selected/whole correction in both orders, later reversal/restoration, descendant navigation, optional physical output, reports/balances, Audit Detail and Administrator Review at the DPI floor and larger display. Automated success never claims this acceptance.

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

The report launcher architecture is manually verified because integrated main-workspace page ownership/navigation is WinForms UI behaviour. Outstanding report calculation remains covered independently by SQLite integration tests.


Integrated-report UI acceptance includes a laptop-sized display and a larger desktop/27-inch monitor resolution. Verify filters/actions remain visible and the dataset expands with available client space.


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

Manual acceptance verifies responsive layout, Last 7 Days / Last 30 Days / This Month shortcuts, authoritative Container Type choices, typed Date/Quantity sorting and PDF/CSV visible-order consistency.

Movement History identity acceptance additionally verifies that the displayed/exported Movement ID is the persisted identifier used by correction/reversal, remains paired with the correct row after filtering and sorting, sorts numerically (for example 2 before 10), and participates in Shift+click multi-column sorting without changing selection semantics.

Required DPI smoke configuration: **Windows 11, 1920x1080, 150% Windows scaling**. Movement History must show the full Movement ID header and Entered by column without normal-case horizontal scrolling; Status/Notes alone may wrap with auto-height. Correct Entire Batch must keep heading/context/fields/reason/actions visible, show no form/content scrollbar for a two-line batch, and scroll only the movement list for a genuinely long batch. Also confirm the substantially larger primary production display is not degraded. This is a manual visual gate; build/unit success does not prove it.


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

Correction coverage additionally verifies every editable field, wrong-date day/week/month semantics, persisted whole-batch date/direction replacement, all-or-nothing conflict behavior, database-enforced reverse/correct exclusion, payload-aware retries, Operator review state, audited acknowledgement, migration backfill and authoritative MovementBatch detail. UI/dialog/DPI and real multi-window interaction remain Windows/operator acceptance.
- Correction reporting regression must cover quantity, wrong-date, direction, customer and container changes across effective Daily/Weekly/Monthly totals, customer recent movements, statements and Market Floor; immutable Movement History must retain and distinguish original/neutraliser/replacement, and ordinary reversal visibility must remain unchanged.

Audit Trail review Windows/regression acceptance must prove the complete sequence: Operator correction/reversal; persisted review requirement; later Administrator login and one consolidated notification; visibly discoverable `Needs review` row through the review-state display/filter; selection enabling **Mark Selected Reviewed**; successful acknowledgement; transition to `Reviewed`; persisted reviewer/UTC time; visible acknowledgement audit event; and rejected/disabled duplicate acknowledgement.

Persistent-reminder acceptance (BT-AUD-013) must additionally prove Administrator-only visibility across main navigation, accurate initial and refreshed outstanding counts, direct routing to the pending Audit Trail set, persistence until the last review is acknowledged, automatic disappearance at zero, retention of the existing login popup and no approval/blocking effect. Contract tests should cover presentation-independent review state/count/navigation separately from WinForms rendering; future WinUI 3 uses the same contract.

Movement-change detail acceptance must verify exact original/neutraliser/replacement lineage, actor/time/reason, field-accurate before/after differences and persisted batch IDs, including exclusion of unchanged/unrelated rows and fail-closed identity. An exact single-event review acknowledgement must open the same referenced lineage with reviewer/time, remain non-reviewable, and reject missing/invalid references. Esc hierarchy remains BT-AUD-010/014.

Selection-state acceptance must verify **Mark Selected Reviewed** is disabled for no selection, Administrator corrections/reversals, login/logout, report generation, customer/container/import events, already-reviewed rows and every other non-reviewable event. Viewer and Operator must fail unauthorized Administrator review attempts at the service boundary regardless of UI visibility.

Audit detail acceptance must verify **View Batch Detail** is disabled for no selection and non-batch events and enabled for an event with authoritative persisted MovementBatch detail. Existing detail contents remain authoritative; contextual ImportRun and correction-lineage routes are future tracked behavior and are not part of the current implementation claim.
