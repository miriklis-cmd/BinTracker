# BinTracker Test Checklist

Current baseline: **v0.4.0-alpha.19.12.3**

Historical alpha checklists have been removed from this file. Defect history remains in `docs/CHANGELOG.md`.

## Build gate

- [ ] `Build-BinTracker.bat` reports v0.4.0-alpha.19.12.3.
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

- [ ] Customer Statement smoke test.
- [ ] Market Floor smoke test.
- [ ] Outstanding Containers report acceptance after implementation.
- [ ] Daily Movements report acceptance after implementation.
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
