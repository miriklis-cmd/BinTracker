# Known Issues

Current release: **v0.5.0-alpha.8.7**

This file contains current defects/limitations that affect testing or production readiness. Completed history is in `docs/CHANGELOG.md`; future features are in `docs/Roadmap.md`; engineering cleanup is in `TECH-DEBT.md`.

## Medium priority — production readiness

### Reports catalogue is incomplete
**Area:** Reports

Market Floor, Customer Statement, Outstanding Containers, Daily Movements, Weekly Movements, Movement History, Monthly Summary and Daily Print Pack are implemented. Monthly Summary and Daily Print Pack still require final operator acceptance/real-world print validation before the Reporting milestone closes.

### Dashboard is still the first-pass operational dashboard
**Area:** Dashboard

It shows Returned Today, Taken Today, Outstanding and Requires Attention. Requires Attention is quantity-focused and there is no drill-down/recent activity/operational attention list yet.

### Business branding is text-only
**Area:** Business Information / Generated Output

Business Information supports Business Name, Trading Name and Default Report Header, and current PDF reports use that identity. Logo storage/rendering and a shared branding layer for reports/email are not implemented yet.

### Email/SMS controls are preferences only
**Area:** Communications

Customer reminder preferences and the `ReminderDelivery` persistence model exist, but no real Email or SMS provider/send workflow has been implemented.

### Movement correction/reversal requires Windows/operator acceptance
- alpha.8.7 follow-up Movement History initial/changed-row action synchronization, whole-batch auto-tick/auto-clear/no-op handling, and focus-stable `Confirm Every Line` rendering passed focused Windows acceptance. The canonical gate also passed with 0 warnings/errors and 310/310 tests.
- alpha.8.5 exposes the authoritative persisted Movement ID in Movement History and PDF/CSV exports, and restructures Correct Entire Batch so its scrollable content cannot displace the fixed Cancel/confirmation action band at the required laptop DPI.
- alpha.8.2 makes the correction dialog labels/values DPI-safe, preserves effective operational reporting, and distinguishes correction lineage from ordinary reversal wording in immutable Movement History.
- alpha.8.1 corrects the release-blocking alpha.8 dialog-initialisation crash and clipped Movement History correction captions; the corrected UI requires renewed Windows smoke testing.
- alpha.8 implements append-only single-movement and whole-persisted-batch correction, transactional lineage, database-enforced cross-command exclusion, Operator/Admin authority and Administrator acknowledgement of Operator changes.
- Opening Adjustment and Excel Import generic correction/reversal remain blocked as required.
- Correction dialogs, the redesigned Administrator review workflow/infobar/detail drill-down, and real concurrent multi-window behaviour require Windows/operator smoke acceptance. Automated tests do not prove 1920x1080/150% visual behaviour.
**Area:** Movements / Audit

### Container display order permits duplicate priorities
**Area:** Container Types

Multiple active Container Types can currently be saved with the same configurable Display order, making their relative order depend on a secondary name sort. Preserve manual reordering and do not hard-code Blue/Yellow priorities; a dedicated follow-up must define deterministic conflict handling and reorder UX.

### Production Backup / Restore is missing
**Area:** Operations

Developer Database Backup/Load/Fresh tools are for testing only. A production-safe user backup/restore/recovery workflow is still required.

### Batch Entry recovery/polish requires operator smoke acceptance
**Area:** Batch Entry

Batch Entry recovery is operator-confirmed for Continue/Save/Discard. v0.5.0-alpha.1.1 fixes the remaining stale asynchronous edit-state race (Esc/Clear could be followed by a late row-load that resurrected Update Line), makes Clear Batch fully reset the editor even with zero draft rows, and aligns recovery-dialog actions consistently. Focused Windows smoke acceptance remains pending.

### Multi-computer production use is not supported yet
**Area:** Deployment

SQLite is currently configured for local single-PC operation. A central provider/concurrency strategy is required for simultaneous multi-computer use.

## Validation watch items

### Market Floor high-Yellow-day stress test
The current real workbook produces the intended two-page front/reverse report and is accepted for now. Revisit adaptive sizing when a genuinely high Yellow-bin day occurs.

### Customer search regression
The Zahos/BIG search/list-detail synchronization bug is fixed but should remain in normal regression testing.

### Audit Trail search/filter/export is not implemented
**Area:** Administration / Audit

Audit persistence is available and remains authoritative, but the Administrator Audit Trail screen does not yet provide practical search/filter/export. This is logged as lower-priority post-v1 usability work (BT-AUD-006).

## Cosmetic / deferred

- alpha.8.7 rebalances Movement History to retain the full authoritative Movement ID header/Entered by column and selectively wrap Status/Notes without normal-case horizontal overflow; Windows 11 1920x1080/150% confirmation remains pending.
- alpha.8.7 makes only the long batch-line list scroll in Correct Entire Batch and titles successful completion `Batch Corrected`; small/long-batch Windows/DPI confirmation remains pending.
- alpha.8.7 expands Movement Change Detail/Audit Trail, adds exact field differences, clarifies Action succeeded, and makes review acknowledgements navigate to exact lineage; Windows/DPI readability acceptance remains pending.
- Import Review action icons remain smaller/cropped compared with the approved mockup, particularly container-related icons.
- Review metric tiles do not yet have the approved rounded corners.
- Password eye / Logout artwork is functional but not final visual polish.


## Requirements reconciliation

- alpha.23.5.2 repaired stale/contradictory documentation and strengthened the mechanical audit gate. Windows build/tests and open manual acceptance items remain authoritative; static reconciliation is not a substitute for runtime acceptance.

- v0.5.0-alpha.1.1: Remaining Batch Entry Esc/Clear edit-state race and recovery-button alignment require focused operator smoke-test; current larger icon trial remains pending final approval.
