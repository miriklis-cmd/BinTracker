# Known Issues

Current release: **v0.5.0-alpha.5.2**

This file contains current defects/limitations that affect testing or production readiness. Completed history is in `docs/CHANGELOG.md`; future features are in `docs/Roadmap.md`; engineering cleanup is in `TECH-DEBT.md`.

## Medium priority — production readiness

### Reports catalogue is incomplete
**Area:** Reports

Market Floor, Customer Statement, Outstanding Containers, Daily Movements, Weekly Movements, Movement History, Monthly Summary and Daily Print Pack are implemented. Monthly Summary and Daily Print Pack still require final operator acceptance/real-world print validation before the Reporting milestone closes.

### Movement History alpha.5.2 UI acceptance is pending
**Area:** Reports / Movement History

The alpha.5.1 Windows build gate passed with zero warnings/errors and 242/242 automated tests, but manual inspection found the integrated page's action row clipped and several structured columns too narrow. Alpha.5.2 keeps the integrated workspace and badge/export improvements, adds explicit Back to Reports navigation, reserves the action row structurally, and rebalances Date/Code/Direction/Source/Status/Notes/Customer widths. Maximized/narrow-window and DPI acceptance remain required.

### Dashboard is still the first-pass operational dashboard
**Area:** Dashboard

It shows Returned Today, Taken Today, Outstanding and Requires Attention. Requires Attention is quantity-focused and there is no drill-down/recent activity/operational attention list yet.

### Business branding is text-only
**Area:** Business Information / Generated Output

Business Information supports Business Name, Trading Name and Default Report Header, and current PDF reports use that identity. Logo storage/rendering and a shared branding layer for reports/email are not implemented yet.

### Email/SMS controls are preferences only
**Area:** Communications

Customer reminder preferences and the `ReminderDelivery` persistence model exist, but no real Email or SMS provider/send workflow has been implemented.

### Movement correction/reversal workflow is in active v0.5 implementation
- Operators can reverse ordinary Manual/Batch movements; Opening Adjustment and Excel Import generic reversal remain restricted to their controlled workflows.
- Append-only reversal, immutable original rows, reason/linkage/audit, already-reversed protection and reversal-of-reversal protection are implemented.
- Final correction-by-replacement workflow remains incomplete.
**Area:** Movements / Audit

### Production Backup / Restore is missing
**Area:** Operations

Developer Database Backup/Load/Fresh tools are for testing only. A production-safe user backup/restore/recovery workflow is still required.

### Batch Entry recovery/polish requires operator smoke acceptance
**Area:** Batch Entry

Batch Entry recovery is operator-confirmed for Continue/Save/Discard. v0.5.0-alpha.1.1 fixed the remaining stale asynchronous edit-state race, made Clear Batch fully reset the editor even with zero draft rows, and aligned recovery-dialog actions consistently. Focused Windows smoke acceptance remains pending where still listed in the active checklist.

### Multi-computer production use is not supported yet
**Area:** Deployment

SQLite is currently configured for local single-PC operation. A central provider/concurrency strategy is required for simultaneous multi-computer use.

## Validation watch items

### Market Floor high-Yellow-day stress test
The current real workbook produces the intended two-page front/reverse report and is accepted for now. Revisit adaptive sizing when a genuinely high Yellow-bin day occurs.

### Customer search regression
The Zahos/BIG search/list-detail synchronization bug is fixed but should remain in normal regression testing.

## Cosmetic / deferred

- Import Review action icons remain smaller/cropped compared with the approved mockup, particularly container-related icons.
- Review metric tiles do not yet have the approved rounded corners.
- Password eye / Logout artwork is functional but not final visual polish.

## Requirements reconciliation

- alpha.23.5.2 repaired stale/contradictory documentation and strengthened the mechanical audit gate. Windows build/tests and open manual acceptance items remain authoritative; static reconciliation is not a substitute for runtime acceptance.
