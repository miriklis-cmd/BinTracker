# BinTracker Active Test Checklist

Current baseline: **v0.5.0-alpha.8.6**

Historical defect/build chronology belongs in `docs/CHANGELOG.md` and `docs/DocumentationAudit.md`. Permanent behaviors recovered from old alpha checklists are retained by ID in `docs/RequirementsAcceptanceRegister.md` and in the active checks below.

Accepted baseline: Jack reported **v0.5.0-alpha.4 manual acceptance complete — 8/8 smoke tests passed**. Later candidates must preserve that accepted behaviour; this record does not imply acceptance of later UI/DPI changes.

## Release / audit / packaging gate

- [ ] `Audit-BinTracker.ps1` passes.
- [ ] `Build-BinTracker.bat` reports v0.5.0-alpha.8.6 and the actually resolved installed SDK.
- [ ] Restore succeeds; full solution builds with zero warnings.
- [ ] All unit tests pass; all integration tests pass.
- [ ] Failed restore/build/test cannot continue to `BUILD SUCCESSFUL`.
- [ ] `Package-BinTracker.ps1` produces ZIP filename/root folder/Version/InformationalVersion all exactly equal to the current `Directory.Build.props` Version.
- [ ] No unexpected `global.json` is packaged.

## Authentication / users / shell

- [ ] First-run Administrator creation works.
- [ ] Login/logout and login-again without restarting work.
- [ ] Failed login/lockout/unlock works.
- [ ] Change/reset password and role/active controls work.
- [ ] Password fields start masked; all supported eye controls reveal/re-hide correctly.
- [ ] Audit Trail / Settings admin actions respect roles.
- [ ] Login, Main, integrated report surfaces and dialogs use BinTracker icon; taskbar is branded before login. Current taskbar/icon rendering remains part of normal Windows visual smoke acceptance.
- [ ] Splash shows BinTracker branding/version during startup.
- [ ] Sidebar logo + full BinTracker wordmark remain aligned/unclipped.

## Customers / Container Types / Business Information

- [ ] Customer search by code/name works and no-result search clears stale details.
- [ ] Customer dirty edits prompt Save / Discard / Cancel on customer switch/search/navigation/logout/close.
- [ ] Customer balances remain separated by Container Type.
- [ ] Current Position and Recent History remain usable/scrollable with no large blank band.
- [ ] Container Type duplicate name/short code rejected; rename preserves history links.
- [ ] Container display order changes entry dropdown order; inactive types disappear from new entry but remain historical.
- [ ] Special Floor Report flag and usage statistics behave correctly; create/update/deactivate/reactivate are audited.
- [ ] Business Information saves/reloads and `BUSINESS_INFORMATION_UPDATED` is audited.
- [ ] Report header fallback: Default Report Header → Trading Name → Business Name → BinTracker.

## Single Entry

- [ ] Customer lookup/autocomplete works; invalid customer rejected; quantity starts blank.
- [ ] Returned/Taken preview calculates correct After Save position.
- [ ] Save writes movement, balance/history and `MOVEMENT_RECORDED`; Viewer cannot save.
- [ ] `Ctrl+Enter` saves.
- [ ] Successful save resets date=today, direction=Returned/IN, customer, container, quantity, reference, notes and preview, then focuses Customer.

## Batch Entry

- [ ] `Ctrl+Enter` saves transactionally; Tab/Shift+Tab flow works.
- [ ] Enter from Quantity/Reference/Notes adds a new line in Add mode and updates the selected line in Edit mode; it never appends a duplicate while editing.
- [ ] Pending rows affect Current vs With Draft preview.
- [ ] Clicking a draft line loads edit fields; Update Line replaces that row, then clears Customer/Quantity/Reference/Notes/preview and returns to Add to Batch mode.
- [ ] Remove clears the editor and exits edit mode; removing the final row leaves Add to Batch ready for the next new line with no ghost edit state.
- [ ] Esc while editing cancels edit mode, clears the editor/preview, retains every draft line, and shows the retained-draft status beneath the line/container summary.
- [ ] After Esc cancels an edit, the pending grid has no live CurrentCell/CurrentRow selection capable of asynchronously restoring `Update Line`; button remains `Add to Batch`.
- [ ] Draft survives navigation and logout/login while application process remains running.
- [ ] Esc with current unsaved entry data clears Customer/Quantity/Reference/Notes/customer preview only, retains the draft, and focuses Customer.
- [ ] Esc with no edit/current entry leaves Batch Entry for Dashboard, highlights Dashboard in left navigation, and retains the draft; Batch Entry can immediately be clicked to return.
- [ ] Add to Batch clears Customer/Quantity/Reference/Notes/customer preview, focuses Customer, carries Movement Date / Batch Type / Container Type forward, leaves no draft row selected, and does not reload the just-added row into the editor.
- [ ] Add at least two draft lines, then close/terminate BinTracker without saving (normal close, Stop Debugging, crash/power loss equivalent), relaunch/login, and confirm the Unsaved Batch Recovered prompt appears with date/direction/line count/total/last-saved details.
- [ ] Recovery prompt buttons appear left-to-right as Continue Batch, Save Batch, Discard Batch.
- [ ] Continue Batch restores the draft without saving it.
- [ ] Save Batch saves the recovered movements and removes recovery state: restart and confirm no draft returns.
- [ ] Discard Batch requires confirmation and removes recovery state: restart and confirm no draft returns.
- [ ] Clear Batch also removes recovery state: restart and confirm no draft returns. If Clear Batch is pressed while editing (including when no draft rows remain), it clears the editor/preview and returns immediately to Add to Batch mode with no stale Update Line state.
- [ ] Cancelling edit with Esc cannot be undone by a late asynchronous customer/row load: after Esc the editor stays cleared and Add to Batch remains active.

## Excel import / re-import

- [ ] Fresh test database → latest real workbook import succeeds and Blue/Yellow/Bulk balances reconcile.
- [ ] Analyse/Map/Review perform no database writes before Import.
- [ ] Existing populated database matches/merges existing customers correctly.
- [ ] Unknown explicit container token blocks until mapped; mappings/decisions persist through wizard navigation.
- [ ] New customer Create/Skip and existing match confirmation/override are explicit and persistent.
- [ ] Excel B/Fwd is authoritative; cutover OUT/IN remain physical movements.
- [ ] Exact successful workbook cannot import twice.
- [ ] Workbook changed after preflight is rejected.
- [ ] Changed workbook/same cutover presents Replace/Correct before execution and preserves legitimate same-day/later Manual/Batch activity.
- [ ] Import History shows run/source/SHA/cutover/user/counts/status/replacement chain/linked movements and same-cutover correction differences.
- [ ] A normal new-cutover import that creates opening adjustments shows `Opening reconciliation changes` with previous BinTracker position, Excel B/Fwd and adjustment.
- [ ] A historical normal-cutover run created before reconciliation snapshots existed says the detail was not captured by that build; it must not say `Correction changes: not applicable`.
- [ ] Replace/Correct continues to show `Correction changes` and the replacement chain independently of normal-cutover opening reconciliation.
- [ ] Forced post-SaveChanges failure fully rolls back and exact-source retry remains possible.
- [ ] Non-Administrator cannot access Import History.
- [ ] **Before v1:** execution failure report identifies useful row/customer/container context.

## Market Floor

- [ ] Market Floor remains first and inline on Reports.
- [ ] Selected date cannot exceed today; historical date regenerates correctly.
- [ ] Current real workbook produces exactly two printable A4 pages for duplex use.
- [ ] Front: Account owing / Cash owing / Account CREDIT / special-container rules remain correct.
- [ ] Blue implicit; Yellow explicit; Bulk/special configured containers treated correctly.
- [ ] Reverse: OUT/IN/B-Fwd/Total correct; Opening Adjustments affect B/Fwd, not physical daily OUT/IN.
- [ ] High-Yellow-day adaptive-layout stress test completed before v1.

## Report launcher / common behavior

- [ ] Detailed reports open as one active integrated main-workspace page with `Reports › <Report Name>` navigation; clicking the selected Reports sidebar item returns to the Reports overview.
- [ ] Laptop and large-monitor sizing gives available space to data grid; no controls/buttons clipped.
- [ ] Interactive reports have **no separate Run Report button**.
- [ ] Date/dropdown/checkbox filters refresh live; Customer filter runs on Enter with visible cue.
- [ ] Customer Code/Type and similar columns remain readable for real data.
- [ ] Numeric columns sort numerically.
- [ ] PDF/CSV preserve current displayed grid order.
- [ ] Future historical periods cannot be selected; current week/month stops at today.
- [ ] Notes export controls behave consistently where supported.

## Outstanding / Daily / Weekly / History

- [ ] Outstanding current/as-of-date filters work; All Containers keeps each customer's container rows adjacent.
- [ ] Outstanding PDF/CSV are readable, correctly ordered and audited.
- [ ] Daily Today/Yesterday shortcuts, customer/container/direction/source filters and adjustment opt-in work.
- [ ] Daily PDF/CSV preserve sort and are audited; optional Notes work.
- [ ] Weekly resolves Monday–Sunday and This Week/Last Week correctly.
- [ ] Weekly Daily Detail + Weekly Overview totals are correct; wrapping never clips actions.
- [ ] Weekly PDF/CSV match selected view/order and are audited.
- [ ] Movement History opens as a full-size integrated main-app page; inclusive range/filters/quick ranges/adjustment opt-in work; PDF/CSV are ordered/audited.
- [ ] On Windows 11 at 1920x1080 and 150% scaling, Movement History remains usable and shows the real persisted Movement ID after Date; verify IDs such as 2 and 10 sort numerically, Shift+click multi-sort retains priority, filtering preserves each row's ID, and PDF/CSV contain the same IDs in displayed order.
- [ ] At normal maximized desktop width Movement History has no unnecessary horizontal scrollbar; Customer/Status/Notes receive remaining width. Narrow the app and confirm minimum readable columns are preserved before horizontal scrolling appears. Rows remain single-height.
- [ ] IN/OUT cells show restrained green/red badges; reversed originals and reversal rows show amber/orange Status badges. Select each kind of row and confirm text remains readable. Hover truncated Status/Notes and confirm full-text tooltips.
- [ ] A customer filter resolving to one customer suggests PDF and CSV names containing the sanitized stable customer code; unfiltered and multi-customer results keep generic filenames.

## Customer Statement

- [ ] Customer Statement works from Customers and Reports and uses the same workflow.
- [ ] Reports customer selection/search/inactive option and double-click work.
- [ ] Statement date range cannot extend past today.
- [ ] Generate PDF saves to chosen location; Generate & Open opens without requiring a chosen save path.
- [ ] Statement opening/movement/closing balances reconcile by container and opened PDF prints normally.

## Outstanding Containers multi-sort trial

- [ ] Balance selector displays `Outstanding only`, `Credits only` and `All non-zero` without clipping.
- [ ] Click **Type** to sort by customer type, then Shift+click **Code**; Account/Cash-COD grouping remains primary and Code is alphabetical within each group.
- [ ] Outstanding Containers: Position ascending treats CREDIT as negative and OUT as positive; descending reverses that signed order.
- [ ] Shift+click an existing sort column toggles its direction without discarding the other sort levels.
- [ ] A plain click on a column returns to a single-column sort.
- [ ] Generated PDF/CSV follows the current multi-column grid order.

## Monthly Summary

- [x] Month/year picker shows full year and cannot select future month.
- [x] This Month / Last Month select correct calendar month.
- [x] Customer Enter search; Container/Source live filters; adjustment opt-in work.
- [x] OUT/IN/Net totals and customer/container rows are correct and sort numerically.
- [x] Current month states activity through today.
- [x] PDF/Generate & Open/CSV preserve grid order and audit correctly.
- [x] User acceptance/sign-off recorded during v0.4.0-alpha.24.2.7 review.

## Daily Print Pack

- [ ] Date cannot exceed today.
- [ ] One PDF contains Outstanding Summary first and physical Movement Detail second.
- [ ] Opening Adjustments are excluded from physical Movement Detail.
- [ ] Generate & Open opens generated pack.
- [ ] Exactly one `DAILY_PRINT_PACK_GENERATED` event is written per generated pack.
- [ ] Real PDF preview/print is readable at production DPI.

## CSV audit trail

- [ ] Outstanding CSV → `OUTSTANDING_CONTAINERS_CSV_EXPORTED`.
- [ ] Daily CSV → `DAILY_MOVEMENTS_CSV_EXPORTED`.
- [ ] Weekly Detail/Overview CSV → `WEEKLY_MOVEMENTS_CSV_EXPORTED`.
- [ ] Movement History CSV → `MOVEMENT_HISTORY_CSV_EXPORTED`.
- [ ] Monthly Summary CSV → `MONTHLY_SUMMARY_CSV_EXPORTED`.
- [ ] CSV audit contains filename, row count and relevant date/filter/view context.

## Remaining pre-v1 milestones

- [ ] Movement Correction/Reversal: linked, reasoned and audited; original never destructively edited/deleted.
- [ ] Administrator and Operator both see Reverse for ordinary Single Entry/Batch Entry movements and can complete a reversal.
- [ ] Viewer does not receive reversal capability.
- [ ] Opening Adjustment generic reversal is blocked and directs to Administrator-controlled adjustment handling.
- [ ] Excel Import / ImportRun-linked generic reversal is blocked and directs to Administrator Replace / Correct.
- [ ] Correct Selected shows original/current and corrected date/customer/container/direction/quantity/reference/notes, requires a reason, and states that saved history remains preserved.
- [ ] Correct Entire Batch shows persisted Batch ID, line/container totals, old/new date/direction, says EVERY line is affected, and requires explicit confirmation.
- [ ] On Windows 11 at 1920x1080 and 150% scaling, Correct Entire Batch remains within the usable working area; inspect a batch long enough to scroll, enter a reason, and confirm Cancel and Confirm Every Line remain fully visible/clickable with no overlap or clipped text. Recheck on the larger production display.
- [ ] Wrong-date correction removes the operational effect from the wrong day/week/month and applies it to the corrected day/week/month.
- [ ] Daily/Weekly/Monthly, customer recent, statements and Market Floor show each correction exactly once as its effective replacement; correction-consumed originals and correction neutralisers remain visible only in immutable history/audit evidence. Ordinary reversal rows retain established operational visibility.
- [ ] Movement History uses compact Source `Correction`, a blue Status pill, and full status/tooltips for original, neutraliser, replacement, and chained replacement-then-corrected roles; ordinary reversals remain amber/orange.
- [ ] A replacement corrected again is ineligible for Correct/Reverse, its final replacement is eligible, and a fresh Movement History load plus PDF/CSV export succeeds.
- [ ] Administrator receives one consolidated notification for unreviewed Operator reversals/corrections; Audit Trail can acknowledge them and the acknowledgement is audited.
- [ ] Administrator-only persistent, non-blocking review infobar remains visible across navigation while Operator correction/reversal reviews are outstanding, shows the current count, opens the pending Audit Trail set, refreshes after review changes and disappears at zero; the existing login popup remains.
- [ ] MovementBatch Audit Trail event supports authoritative persisted-line drill-down.
- [ ] Audit Trail visibly distinguishes Needs review / Reviewed / not applicable and filters All / Needs review / Reviewed.
- [ ] Mark Selected Reviewed enables only for a selected unreviewed, review-required Operator correction/reversal; it stays disabled for no selection, Administrator changes, unrelated events and already-reviewed events.
- [ ] Review acknowledgement persists reviewer/time, writes a visible acknowledgement audit event and cannot be repeated; Viewer/Operator remain denied at the service boundary.
- [ ] View Batch Detail is disabled for no selection/non-batch events and enabled only for authoritative persisted MovementBatch detail.
- [ ] Double-click a reviewable movement-change event and confirm Movement Change Detail shows actor/time/reason, exact original/neutraliser/replacement IDs and original-vs-corrected date/direction without unrelated same-date/customer rows.
- [ ] Press Esc in Movement Change Detail/Batch Detail to return to Audit Trail, then Esc in Audit Trail to return to the underlying BinTracker screen without exiting the application.
- [ ] Business logo + shared generated-output branding.
- [ ] Google Workspace email + Texto SMS communications, templates, manual/automatic sends, opt-out, delivery history, retries/idempotency and audit.
- [ ] Dashboard design discussion **before code**, covering KPIs/charts/drill-through/attention/recent activity/ageing/forecasting-ML/large-monitor behavior.
- [ ] Production backup/restore/scheduled retention/pre-upgrade recovery drill.
- [ ] Security/reliability/Release/DPI hardening.
- [ ] Installer/upgrade/deployment acceptance.
- [ ] Full v1 regression/production acceptance.


## alpha.24 Reports landing page

- [ ] Reports header/subtitle matches approved mock-up.
- [ ] Quick Reports contains Market Floor Sheet and Daily Print Pack side-by-side.
- [ ] Quick Reports uses exact approved icon artwork.
- [ ] Market Floor date / Generate PDF / Generate & Open still work.
- [ ] Daily Print Pack date / Generate PDF / Generate & Open still work.
- [ ] Explore Reports is 3×2 at normal desktop width.
- [ ] All six Explore report icons match the approved mock-up.
- [ ] Outstanding Containers opens.
- [ ] Outstanding Containers Balance filter defaults to Outstanding only; Credits only shows only negative/credit positions; All non-zero shows both, and PDF/CSV match the selected mode.
- [ ] Daily Movements opens.
- [ ] Weekly Movements opens.
- [ ] Movement History opens.
- [ ] Monthly Summary opens.
- [ ] Customer Statement opens.
- [ ] No report controls or card text are clipped at 100% DPI.
- [ ] Repeat visual pass at 125% and 150% DPI.
- [ ] Reports landing page shows no page scrollbar when maximized at the supported test scales.
- [ ] Generate PDF shows the small document icon; Generate & Open stays on one line; every Explore Open button shows the full word `Open` without clipping.
- [ ] Containers is a dedicated left-navigation destination immediately below Customers; no duplicate Container Types administration entry remains in Settings.


## alpha.24.1 Containers navigation
- [ ] Containers appears immediately below Customers in the left navigation.
- [ ] Administrator can add, rename, reorder, deactivate/reactivate and save Container Types.
- [ ] Operator can open Containers and search/view active/inactive Container Types but cannot modify them.
- [ ] Viewer can open Containers and search/view active/inactive Container Types but cannot modify them.
- [ ] Unsaved Administrator edits prompt before navigating away from Containers.
- [ ] Settings no longer contains a duplicate Container Types button.


## alpha.24.2 Reports layout
- [ ] Reports page subtitle is fully visible beneath the Reports heading.
- [ ] Both Quick Reports cards show title, description, date and both buttons without clipping.
- [ ] All six Explore Reports cards show the complete two-line description.
- [ ] All six Explore Reports cards show the complete Open footer.
- [ ] No report card overlaps or clips at the Windows display scaling used for the acceptance workstation.


## alpha.24.2.1 audit gate
- [ ] Build-BinTracker.bat passes the source/package-state audit.
- [ ] Audit reports the permanent requirement count rather than a false BT-CT-005 missing error.
- [ ] Continue the alpha.24.2 Reports visual smoke test after build/tests pass.

- [ ] Reports landing page has no bottom PDF/CSV/date information bar and all Explore report descriptions/Open captions are fully visible at the affected display scaling.

### Report multi-column sorting
- [ ] Each applicable report grid shows the sort hint.
- [ ] Click a column for primary sort; Shift+click a second column and confirm grouping is preserved with the second sort inside each group.
- [ ] Numeric report columns sort numerically (for example 2, 3, 21, 26), not lexically (2, 21, 26, 3).
- [ ] Date columns sort chronologically, including weekday-prefixed dates.
- [ ] Active multi-column sort order is preserved after changing report filters/reloading results.
- [ ] Outstanding Containers shows Today, Generate PDF, Generate & Open and Export CSV without clipping at normal Windows scaling.

- [ ] Report sort-state indicator: every active sort column visibly shows direction and priority (for example `▲1`, `▼1`, `▲2`), including Daily Movements Direction and both Weekly Movements tabs at 100%, 125% and 150% scaling; active indicators stay on one line and sorting does not change the grid/header height or any column widths.
