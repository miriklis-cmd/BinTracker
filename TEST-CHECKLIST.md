# BinTracker Active Test Checklist

Current baseline: **v0.4.0-alpha.24.2.4**

Historical defect/build chronology belongs in `docs/CHANGELOG.md` and `docs/DocumentationAudit.md`. Permanent behaviors recovered from old alpha checklists are retained by ID in `docs/RequirementsAcceptanceRegister.md` and in the active checks below.

## Release / audit / packaging gate

- [ ] `Audit-BinTracker.ps1` passes.
- [ ] `Build-BinTracker.bat` reports v0.4.0-alpha.24.2.4 and the actually resolved installed SDK.
- [ ] Restore succeeds; full solution builds with zero warnings.
- [ ] All unit tests pass; all integration tests pass.
- [ ] Failed restore/build/test cannot continue to `BUILD SUCCESSFUL`.
- [ ] `Package-BinTracker.ps1` produces ZIP filename/root folder/Version/InformationalVersion all exactly `0.4.0-alpha.24.2.4`.
- [ ] No unexpected `global.json` is packaged.

## Authentication / users / shell

- [ ] First-run Administrator creation works.
- [ ] Login/logout and login-again without restarting work.
- [ ] Failed login/lockout/unlock works.
- [ ] Change/reset password and role/active controls work.
- [ ] Password fields start masked; all supported eye controls reveal/re-hide correctly.
- [ ] Audit Trail / Settings admin actions respect roles.
- [ ] Login, Main, breakout reports and dialogs use BinTracker icon; taskbar is branded before login.
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
- [ ] Enter from Quantity/Reference/Notes adds or updates pending line.
- [ ] Pending rows affect Current vs With Draft preview.
- [ ] Clicking draft line loads edit fields; update/remove/container change recalculates preview.
- [ ] Esc exits draft-line edit mode.
- [ ] Draft survives navigation and logout/login while application process remains running.
- [ ] **Before v1:** verify/document Esc behavior in every Batch state.
- [ ] **Before v1:** decide/finalize post-add clearing/carry-forward and return focus behavior.
- [ ] **Before v1:** decide/implement crash/power-loss draft recovery.

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
- [ ] Import History shows run/source/SHA/cutover/user/counts/status/replacement chain/linked movements/correction differences.
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

- [ ] Detailed reports open in dedicated single-instance responsive windows and reuse existing window on repeated Open Report.
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
- [ ] Movement History inclusive range/filters/quick ranges/adjustment opt-in work; PDF/CSV are ordered/audited.

## Customer Statement

- [ ] Customer Statement works from Customers and Reports and uses the same workflow.
- [ ] Reports customer selection/search/inactive option and double-click work.
- [ ] Statement date range cannot extend past today.
- [ ] Generate PDF saves to chosen location; Generate & Open opens without requiring a chosen save path.
- [ ] Statement opening/movement/closing balances reconcile by container and opened PDF prints normally.

## Monthly Summary

- [ ] Month/year picker shows full year and cannot select future month.
- [ ] This Month / Last Month select correct calendar month.
- [ ] Customer Enter search; Container/Source live filters; adjustment opt-in work.
- [ ] OUT/IN/Net totals and customer/container rows are correct and sort numerically.
- [ ] Current month states activity through today.
- [ ] PDF/Generate & Open/CSV preserve grid order and audit correctly.

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

- [ ] Movement Correction/Reversal: linked, reasoned, permissioned, audited; original never destructively edited/deleted.
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
- [ ] Daily Movements opens.
- [ ] Weekly Movements opens.
- [ ] Movement History opens.
- [ ] Monthly Summary opens.
- [ ] Customer Statement opens.
- [ ] No report controls or card text are clipped at 100% DPI.
- [ ] Repeat visual pass at 125% and 150% DPI.
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
