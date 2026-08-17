# BinTracker Test Checklist

Current baseline: **v0.4.0-alpha.22.3.2**

Historical alpha checklists have been removed from this file. Defect history remains in `docs/CHANGELOG.md`.

## Build gate

- [ ] `Build-BinTracker.bat` reports v0.4.0-alpha.22.3.2.
- [ ] Restore succeeds.
- [ ] Full solution builds with zero warnings.
- [ ] All unit tests pass.
- [ ] All integration tests pass.

## Import acceptance

- [ ] Fresh test database → import latest real workbook succeeds.
- [ ] Existing populated database → Review correctly matches/merges existing customers.
- [ ] Blue / Yellow / Bulk balances reconcile correctly.
- [ ] Unknown explicit container token blocks until mapped.
- [ ] New customer Create/Skip decisions persist through wizard navigation.
- [ ] Existing match confirmation/override persists through wizard navigation.
- [ ] Excel B/Fwd is authoritative; current BinTracker test balances are not added blindly.
- [ ] Cutover OUT/IN remain real movements.
- [ ] Customer statements label opening adjustments distinctly.
- [ ] Exact same workbook cannot be imported twice.
- [ ] Workbook changed after Step 4 preflight is rejected.
- [x] **Automated forced failure after final SaveChanges rolls back customer, movements, ImportRun and completion audit; exact-source retry remains allowed.**
- [x] Every generated import movement links to the correct ImportRun; Manual movement regression remains NULL.
- [x] Changed workbook / same cutover is detected, compared and explicitly Replace/Corrected; prior linked movements are replaced while same-day and later Manual movements are preserved outside the corrected cutover baseline.
- [x] **Manual UI acceptance:** changed workbook on the same cutover shows **Replace / Correct** on Step 4 before execution, then displays the correction comparison.
- [x] **Manual correction accuracy:** real-workbook smoke test changed values for two customers; correction showed only genuine configured-container changes and completed successfully as run #2.
- [x] **Manual UI sizing:** Replace / Correct button text is fully visible.
- [x] Replacement smoke test verified run #2 movement references (`IMPORT-2`) and audit trail correctly records run #1 → run #2 replacement.

- [ ] Optional manual forced-failure acceptance before v1.0 release sign-off.

## Customer acceptance

- [ ] Search `zahos` returns Zahos in list and details.
- [ ] Search `big` returns BIG in list and details.
- [ ] No-result search clears stale detail pane.
- [ ] Customer code uniqueness remains case-insensitive.
- [ ] Customer balances are separate by container.
- [ ] Customer statement opening/movement/closing balances reconcile.
- [ ] Customer dirty-state smoke test: changing customer then selecting/searching/navigating away prompts **Save / Discard / Cancel** and all three choices behave correctly.

## Movement acceptance

- [ ] Batch Entry saves transactionally.
- [ ] Batch Entry supports multiple customers/container types per batch.
- [ ] Single Entry saves and resets correctly.
- [ ] Dashboard Today IN/OUT changes after saved movements.
- [ ] Audit contains saved batch/manual movement events.
- [ ] After correction workflow: reversing/correcting preserves original movement and audit linkage.

## Market Floor

- [x] Current real workbook produces exactly 2 pages (accepted for now).
- [x] Front page uses readable large text.
- [x] Blue is implicit.
- [x] Yellow appears separately/explicitly.
- [x] Bulk appears in Special Containers.
- [x] Cash/COD credits remain with Cash/COD.
- [x] Account credits use separate CREDIT section.
- [x] Opening adjustments contribute to B/Fwd rather than daily physical OUT/IN.
- [x] Reverse side is one page for current real workbook.
- [ ] Re-test adaptive sizing on a genuinely high Yellow-bin day.

## Reports

- [ ] Reports launcher keeps Market Floor Sheet first and inline.
- [ ] Outstanding Containers opens in a dedicated full report window.
- [ ] Clicking Open Report again while Outstanding is already open brings the same window forward rather than opening a duplicate.
- [ ] Outstanding dedicated window remains readable at production DPI and exposes Run Report / Today / Export CSV without clipping.
- [ ] Outstanding window uses responsive sizing on laptop and large monitor; results grid expands with available screen size.

- [ ] Customer Statement smoke test.
- [ ] Market Floor smoke test.
- [ ] Outstanding Containers on-screen/as-of-date UI acceptance.
- [ ] With **All containers**, each customer's configured container rows are adjacent (e.g. CLAMMS Blue / Yellow / Bulk together).
- [ ] Outstanding grid/card is fully visible and uses its own vertical scrollbar rather than being cut off by the main window.
- [ ] Outstanding Containers CSV export opens correctly and preserves container/customer separation.
- [ ] Outstanding Containers PDF/print acceptance after implementation.
- [ ] Daily Movements dedicated window opens single-instance and is responsive.
- [ ] Today shortcut displays today's physical movements.
- [ ] Yesterday shortcut displays yesterday's physical movements.
- [ ] Opening adjustments are excluded by default and appear only when Include adjustments is selected.
- [ ] Customer/container/direction/source filters work.
- [ ] Quantity sorts numerically.
- [ ] Daily PDF matches the current sorted grid order.
- [ ] Daily CSV matches the current sorted grid order.
- [ ] Daily PDF is readable/printable and DAILY_MOVEMENTS_REPORT_GENERATED is audited.
- [ ] Movement History report acceptance after implementation.
- [ ] Monthly Summary acceptance after implementation.
- [ ] Daily Print Pack acceptance after implementation.

## Import history

- [x] Settings → Import History opens and lists run #1 and run #2.
- [x] Run #1 shows **Replaced** and identifies run #2 as its replacement.
- [x] Run #2 shows **Completed**, identifies run #1 as the replaced run, and displays its linked `IMPORT-2` movements.
- [x] SHA-256, cutover date, workbook, user and counts are readable.
- [ ] New correction run created on alpha.19.12.1+ shows the persisted customer/container change list (previous → corrected → change).
- [ ] Non-Administrator cannot access Import History.

## Security / admin

- [ ] First-run Administrator creation.
- [ ] Login/logout.
- [ ] Failed login/lockout/unlock.
- [ ] Change/reset password.
- [ ] Role changes and active/inactive user controls.
- [ ] Audit Trail access restricted appropriately.
- [ ] Settings/admin actions respect roles.

## Backup / deployment

- [ ] Developer Database backup/load/fresh tools still work for testing.
- [ ] Production backup/restore acceptance after implementation.
- [ ] Release installer fresh-install test after implementation.
- [ ] Upgrade an existing database without data loss after installer/upgrader implementation.

## UI / DPI

- [ ] 100% scaling.
- [ ] 125% scaling.
- [ ] 150% scaling.
- [ ] No core action buttons inaccessible/clipped.
- [ ] Deferred Import Review icon/rounded-tile polish tracked separately.

## alpha.19.12.2 UI acceptance

- [ ] Import History title and explanatory text do not overlap.
- [ ] Import History run-list headings are readable (Customers / Movements / Replaces).
- [ ] Correction Changes table headings/data are readable.
- [ ] Linked movement grid has visible rows and readable headings/data.
- [ ] Existing-customer dialog clearly explains Accept match vs Override match.
- [x] Container Types: edit a field then select another container → unsaved-change prompt appears.
- [ ] Container Types prompt buttons are explicitly labelled **Save / Discard / Cancel**.
- [ ] Container Types: edit a field then close window → Save / Discard / Cancel prompt.
- [ ] Container Types: choose Save from prompt and confirm the change persists.
- [ ] Container Types: choose Cancel and confirm the editor remains on the current container with edits intact.

## alpha.19.12.3 customer protection acceptance

- [ ] Edit existing customer then select another customer → Save / Discard / Cancel.
- [ ] Edit existing customer then type in Search → Save / Discard / Cancel.
- [ ] Edit existing customer then toggle Inactive → Save / Discard / Cancel.
- [ ] Edit existing customer then click + New Customer → Save / Discard / Cancel.
- [ ] Edit existing customer then navigate to another BinTracker page → Save / Discard / Cancel.
- [ ] Edit existing customer then Logout → Save / Discard / Cancel before logout confirmation.
- [ ] Edit existing customer then close BinTracker → Save / Discard / Cancel.
- [ ] Save persists; Discard navigates without persisting; Cancel keeps the editor and typed changes.
- [ ] Import History Completed metadata stays on one line at the test display/DPI.

## Batch Entry acceptance cleanup

- [x] Ctrl+Enter saves the batch.
- [x] Tab / Shift+Tab keyboard flow works.
- [x] Enter on Quantity / Reference / Notes adds or updates the pending line.
- [x] Draft survives page navigation and logout/login while the app remains running.
- [x] Pending rows affect Current vs With Draft balance preview.
- [x] Dashboard refreshes after successful save.
- [ ] Verify/document Esc behaviour in each state.
- [ ] After successful line entry, non-carry-forward fields clear and focus returns to Customer entry.
- [ ] Decide/implement crash/power-loss draft recovery before production if required.

## Recovered reporting acceptance

- [x] Automated Historical Outstanding / As-of-Date regression excludes future movements and keeps Blue/Yellow (all configured containers) separate.
- [ ] Manual Historical Outstanding / As-of-Date query matches a known past balance.
- [ ] Weekly Movements report works for a selected week.
- [ ] Daily report supports fast today/yesterday use.
- [ ] Monthly report supports fast current/previous month use.
- [ ] Customer Statement can be generated, opened/viewed and printed.
- [ ] Customer sorting supports code/name, outstanding, credit and last movement where implemented.
- [ ] Customer lifetime OUT/Taken and IN/Returned totals reconcile where exposed.

## Project-process acceptance

- [ ] Every implementation pass states TEST REQUIRED: None / Targeted / Full.
- [ ] Full docs audit is performed every meaningful pass.
- [ ] Build-BinTracker.bat truthfully fails on restore/build/test failure.

- [ ] Outstanding Containers customer Code column expands to fit the longest visible customer code, subject to its maximum width.
- [ ] Code column remains sensible with short codes and does not consume excessive width with an unusually long code.

- [ ] Outstanding Containers Code column shows long customer codes such as BEST OCIANA / CAMBERWELL without truncation where available width permits.
- [ ] Outstanding Containers Type column expands enough to show Account and Cash / COD clearly.
- [ ] Rerunning/filtering the report recalculates Code/Type widths from currently visible rows.

- [ ] Outstanding Containers Generate PDF saves a readable landscape PDF matching the current filtered/as-of-date result.
- [ ] Outstanding Containers Generate & Open saves the PDF and opens it in the Windows default PDF viewer.
- [ ] Outstanding PDF preserves Customer → Container ordering, credit/inactive choices already reflected in the current result, totals and long customer codes.
- [ ] OUTSTANDING_REPORT_GENERATED audit event records date, row count and output filename.


## alpha.20.0.7.1 constructor wiring

- [ ] Build succeeds with `OutstandingContainersReportForm(IOutstandingReportService, IOutstandingReportPdfService)`.
- [ ] Full automated unit/integration suite passes.


## alpha.20.0.7.2 Outstanding action layout

- [ ] Outstanding filters remain fully visible on the first control row.
- [ ] Run Report and Today are fully visible on the action row.
- [ ] Generate PDF button is fully visible.
- [ ] Generate & Open button is fully visible.
- [ ] Export CSV button is fully visible.
- [ ] No report action is partially hidden at production laptop DPI.


## alpha.20.0.7.3 report sorting / printable view

- [ ] Sort Position descending: e.g. `72 OUT` sorts ahead of `9 OUT`, `8 OUT`, `7 OUT`.
- [ ] Sort Position ascending and verify numeric order.
- [ ] Sort Type and confirm Cash / COD and Account group correctly.
- [ ] After sorting the grid, Generate PDF preserves that exact visible row order.
- [ ] Generate & Open preserves the current grid sort order as well.
- [ ] Sorting does not alter the underlying balances or report totals.


## alpha.20.0.7.4 CSV visible-order export

- [ ] Sort Position descending, Export CSV, confirm numeric-descending row order matches the grid.
- [ ] Sort Type, Export CSV, confirm Type grouping matches the grid.
- [ ] Sort Customer/Code/Container and confirm CSV preserves that exact visible row order.
- [ ] PDF and CSV from the same sorted grid use the same row order.


## alpha.20.0.8.1 Daily report polish

- [ ] Daily action button visibly reads **Generate & Open** including the ampersand.
- [ ] Direction selector fully displays **All directions** at production laptop DPI.
- [ ] Include notes in exports unchecked: generated PDF omits Notes.
- [ ] Include notes in exports checked: generated PDF contains a Notes column and note text.
- [ ] Default no-notes PDF remains readable and uses page space efficiently.


## alpha.20.0.8.2 Daily adjustment-control cleanup

- [ ] Source dropdown offers All sources, Single Entry, Batch Entry and Excel Import.
- [ ] Source dropdown does not offer Opening Adjustment.
- [ ] **Include opening adjustments** unchecked excludes adjustment rows.
- [ ] **Include opening adjustments** checked allows adjustment rows to appear when other filters permit them.


## alpha.20.0.8.3 Daily control layout

- [ ] Core filters are fully visible.
- [ ] Include opening adjustments and Include notes in exports are fully visible on their own options row.
- [ ] Run Report, Today, Yesterday, Generate PDF, Generate & Open and Export CSV are fully visible.
- [ ] No action button is hidden behind the summary panel at production laptop DPI.
- [ ] Resizing the window does not partially clip the action row.


## alpha.20.0.8.4 Daily export notes consistency

- [ ] Include notes in exports unchecked: PDF omits Notes.
- [ ] Include notes in exports unchecked: CSV omits Notes column/data.
- [ ] Include notes in exports checked: PDF includes Notes.
- [ ] Include notes in exports checked: CSV includes Notes column/data.
- [ ] On-screen Notes column remains visible regardless of export setting.


## alpha.21 Weekly Movements

- [ ] Weekly Movements dedicated window opens single-instance and is responsive.
- [ ] Selected week resolves Monday through Sunday.
- [ ] This Week and Last Week shortcuts select the correct week.
- [ ] Customer/container/source filters work.
- [ ] Opening adjustments are excluded by default and included only explicitly.
- [ ] Movement Detail and Customer / Container Summary agree.
- [ ] OUT, IN and Net values are numerically correct.
- [ ] Quantity/summary numeric sorting is numeric rather than textual.
- [ ] CSV preserves current detail order.
- [ ] **Include notes in exports** correctly includes/omits Notes in Daily Detail CSV and PDF.
- [ ] Select date clearly displays the resolved Monday-Sunday Week range.
- [ ] Generate PDF and Generate & Open work from both report tabs.
- [ ] Weekly PDF preserves the selected tab and current grid sort order.
- [ ] Weekly PDF respects active customer/container/source/opening-adjustment filters.
- [ ] **Include notes in exports** is the single Notes control for both PDF and CSV.
- [ ] Detail Date values are not truncated; detail/summary Code widths adapt to visible codes.
- [ ] Weekly PDF generation appears in the audit trail with week, view, totals and output filename.


## alpha.21.2 Weekly overview / export polish

- [ ] Weekly button visibly reads **Generate & Open** including the ampersand.
- [ ] Weekly shows one **Include notes in exports** control; it is disabled on Weekly Overview.
- [ ] Daily Detail shows individual movement rows.
- [ ] Weekly Overview shows one row per customer/container with OUT, IN and Net.
- [ ] Known equal activity example displays equal OUT/IN and Net 0.
- [ ] Weekly Overview numeric OUT/IN/Net sorting remains numeric.
- [ ] Generate PDF from Daily Detail exports current Daily Detail ordering.
- [ ] Generate PDF from Weekly Overview exports current Weekly Overview ordering.
- [ ] Export CSV from Daily Detail exports current Daily Detail ordering and respects CSV Notes option.
- [ ] Export CSV from Weekly Overview exports overview columns/order.
- [ ] Notes controls are disabled while Weekly Overview is selected.


## alpha.21.3 Weekly semantics / filter fixes

- [ ] Weekly has one **Include notes in exports** checkbox, not separate PDF/CSV Notes controls.
- [ ] Notes checkbox affects both Daily Detail PDF and CSV consistently.
- [ ] Date picker does not allow a date after today.
- [ ] Current-week report does not include future-dated movements.
- [ ] Current-week label/PDF clearly states activity is only through today when the week has not finished.
- [ ] Container filter includes all configured types relevant to this database (e.g. Blue, Yellow, Bulk) regardless of current outstanding balance.
- [ ] Inactive configured container types appear as `(inactive)` and remain usable for historical filtering.
- [ ] Selecting Yellow/Bulk correctly filters both Daily Detail and Weekly Overview.


## alpha.21.4 Daily date guard

- [ ] Daily Movements date picker cannot select a date after today.
- [ ] Today still runs correctly.
- [ ] Yesterday still runs correctly.
- [ ] Automated integration coverage confirms future service requests clamp to today.


## alpha.21.4.1 build/test wiring

- [ ] Integration tests compile without referencing internal `WeeklyMovementsReportService`.
- [ ] Full automated unit/integration suite passes.


## alpha.21.5.1 documentation consistency audit

- [x] Current-version references agree with `Directory.Build.props`.
- [x] Weekly Movements is documented as implemented in current-state docs.
- [x] Weekly Notes semantics use one Include notes in exports control.
- [x] Branding docs distinguish existing Default Report Header from future logo/shared branding.
- [x] Roadmap has one authoritative execution order and no duplicate production-backup section.
- [x] Changelog remains historical and is not rewritten to erase superseded behaviour.


## Mandatory checklist for every packaged build

- [ ] Automated build/test gate passes, or any failure is explicitly documented before another candidate is issued.
- [ ] Changed implementation and affected call sites/services/UI/persistence/tests audited.
- [ ] Every Markdown file enumerated and reviewed.
- [ ] Roadmap reconciled with Roadmap Coverage Matrix.
- [ ] Current-version references match `Directory.Build.props`.
- [ ] Known Issues reflects current limitations only.
- [ ] Tech Debt reflects unresolved debt only.
- [ ] Functional Specification and Business Rules match current behaviour.
- [ ] Testing/Test Checklist updated for the candidate.
- [ ] Changelog updated.
- [ ] Current Release Notes replaced for the candidate.
- [ ] DocumentationAudit updated.
- [ ] Operator testing requirement explicitly classified as automated-only, targeted smoke, or full smoke.


## alpha.22 Movement History

- [ ] Movement History opens as a single-instance responsive report window.
- [ ] Start/end date pickers cannot select later than today.
- [ ] Last 7 Days, Last 30 Days and This Month select correct inclusive ranges.
- [ ] Customer/container/direction/source filters work.
- [ ] Container selector includes configured Blue/Yellow/Bulk/etc. and inactive historical types where applicable.
- [ ] Opening adjustments are excluded by default and included only explicitly.
- [ ] Date sorts chronologically and Quantity sorts numerically.
- [ ] PDF matches current grid sorting/filter result.
- [ ] CSV matches current grid sorting/filter result.
- [ ] Include notes in exports consistently affects both PDF and CSV.
- [ ] MOVEMENT_HISTORY_REPORT_GENERATED audit event is written for PDF generation.


## alpha.22.1 live report refresh

- [ ] Outstanding Containers has no Run Report button.
- [ ] Daily Movements has no Run Report button.
- [ ] Weekly Movements has no Run Report button.
- [ ] Movement History has no Run Report button.
- [ ] Changing date/dropdown/result-affecting checkbox filters automatically refreshes each report.
- [ ] Customer typing does not refresh on every character.
- [ ] Pressing Enter in Customer refreshes each report.
- [ ] Report shortcut buttons still refresh immediately.
- [ ] Movement History button reads **This Month** in full.
- [ ] Existing PDF/CSV exports still reflect the current displayed dataset/order.


## alpha.22.2 report layout/search cue/product branding

- [ ] Weekly Movements filter wrapping does not clip This Week / Last Week / PDF / Generate & Open / CSV buttons.
- [ ] Weekly works at laptop resolution and scales normally on a larger monitor.
- [ ] Outstanding Customer field/cue clearly says Enter is required.
- [ ] Daily Customer field/cue clearly says Enter is required.
- [ ] Weekly Customer field/cue clearly says Enter is required.
- [ ] Movement History Customer field/cue clearly says Enter is required.
- [ ] Customer Enter refresh works on all four reports.
- [ ] Supplied BinTracker icon appears as the Windows application icon.
- [ ] Sidebar shows the supplied BinTracker product logo at a restrained size.
- [ ] Existing report PDF/CSV behaviour remains unchanged.


## alpha.22.3.1 Weekly wrapped layout

- [ ] At the laptop width/DPI that previously wrapped Source to a second line, all Weekly action buttons are completely visible.
- [ ] This Week / Last Week / Generate PDF / Generate & Open / Export CSV are not overlapped by the summary panel.
- [ ] Resizing narrower/wider does not cause the summary to cover the controls.
- [ ] Daily Detail / Weekly Overview grid still fills the remaining window space.


## alpha.22.3.2 application branding

- [ ] Login title bar uses BinTracker icon.
- [ ] Taskbar uses BinTracker icon while Login is the only visible BinTracker window.
- [ ] Main shell uses BinTracker icon after login.
- [ ] Outstanding/Daily/Weekly/Movement History breakout forms use BinTracker icon.
- [ ] Import/admin/settings dialogs use BinTracker icon.
- [ ] Left navigation visibly shows BinTracker logo + BinTracker wordmark without overlap.
