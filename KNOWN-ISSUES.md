# Known Issues

Current release: **v0.4.0-alpha.19.8.1**

This file contains current defects/limitations that affect testing or production readiness. Completed history is in `docs/CHANGELOG.md`; future features are in `docs/Roadmap.md`; engineering cleanup is in `TECH-DEBT.md`.

## High priority

### Import rollback is implemented but still needs deliberate failure verification
**Area:** Import / Data integrity

Step 4 runs inside a SQLite transaction and failure paths are intended to roll back the entire import. We have not yet completed the explicit acceptance test that forces a mid-import failure and proves no partial customers, movements or completed ImportRun survive.

### Changed-workbook / same-cutover re-import is not implemented
**Area:** Import / Data integrity

Exact identical workbooks are blocked using SHA-256. A modified workbook representing the same cutover date can have a different fingerprint. BinTracker still needs a controlled Review Differences / Replace-Correct workflow. It must never offer a blind duplicate import.

### Import-generated movements do not yet have a relational `ImportRunId`
**Area:** Import / Database

Generated movements are currently traceable using `IMPORT-<run id>` references and notes. Add a nullable ImportRun FK before the replacement/correction workflow so provenance is reliable and queryable.

### Customer edits can be lost when navigating away
**Area:** Customers

Editing a customer and then selecting/searching/navigating away without pressing Save discards the changes. Add dirty-state tracking with **Save / Discard / Cancel** protection.

## Medium priority — production readiness

### Reports catalogue is incomplete
**Area:** Reports

Only Market Floor and Customer Statement are implemented. Outstanding Containers, Daily Movements, Movement History, Monthly Summary and the Daily Print Pack remain to be built.

### Dashboard is still the first-pass operational dashboard
**Area:** Dashboard

It shows Returned Today, Taken Today, Outstanding and Requires Attention. Requires Attention is quantity-focused and there is no drill-down/recent activity/operational attention list yet.

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
