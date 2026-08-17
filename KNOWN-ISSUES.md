# Known Issues

Current release: **v0.4.0-alpha.23.5.2**

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

### Movement correction/reversal workflow is missing
**Area:** Movements / Audit

Saved movements are auditable but there is no controlled workflow to reverse/correct an entry while preserving the original.

### Production Backup / Restore is missing
**Area:** Operations

Developer Database Backup/Load/Fresh tools are for testing only. A production-safe user backup/restore/recovery workflow is still required.

### Batch Entry draft does not survive crash/power loss
**Area:** Batch Entry

Drafts survive in-app navigation and logout/login within the running process, but not process termination or power loss.

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
