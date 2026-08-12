# Known Issues

Current release: **v0.4.0-alpha.7**

This file tracks current defects, incomplete production-critical behaviour, and limitations that a tester/operator needs to know about. Planned enhancements belong in `docs/Roadmap.md`; engineering cleanup belongs in `TECH-DEBT.md`.

## High priority — before v1.0

### Excel Import Wizard is analysis-only
**Status:** In progress  
**Area:** Import

The wizard can read `.xlsm` / `.xlsx`, analyse workbook structure, detect Buyer/customer occurrences, identify snapshot-style B/Fwd / OUT / IN / Total rows, and show duplicate diagnostics. It does **not** yet write customers, opening positions or movements to the database.

Remaining work:
- Map step;
- sheet classification;
- customer matching/merge decisions;
- container mapping;
- Review step;
- transactional Import step;
- rollback/error report.

### Sheet classification is not implemented
**Status:** In progress  
**Area:** Import

Custom workbooks may repeat data across source, validation and report/output sheets. For the current legacy workbook, sheets such as `CREDITS`, `Print This` and `Print this on reverse side` are derived views and must not create duplicate imported records.

The upcoming Map step must classify each sheet as:
- Source;
- Validation only;
- Report/output only;
- Ignore.

### Legacy opening-position import is not yet committed
**Status:** Designed / not implemented  
**Area:** Import

Snapshot analysis understands the rule:

`Total = B/Fwd + OUT - IN`

but database execution is still pending. B/Fwd must become an opening position at cutover rather than a fake physical movement on the cutover day. The selected day's OUT/IN will remain real movements. Excel Total is validation only.

### Market Floor Sheet needs production-scale validation
**Status:** Pending real-data validation  
**Area:** Reports

The two-page A4 portrait Market Floor Sheet exists, including Account/Cash grouping, credits, reverse-side B/Fwd and special containers. It still needs validation against the full legacy workbook dataset after import mapping is complete.

## Medium priority — before production acceptance

### Batch Entry draft is not crash/power-loss persistent
**Status:** Known limitation  
**Area:** Batch Entry

Unsaved draft lines survive navigation and logout/login within the running application, but they are not persisted to disk. A crash, power loss or forced process termination loses the draft.

### Movement correction/reversal workflow is not implemented
**Status:** Planned before/around production acceptance  
**Area:** Movements / Audit

Saved movements are auditable, but there is not yet a dedicated operator/admin workflow for correcting an erroneous movement while preserving the original entry and reversal trail.

### Dashboard attention logic is quantity-focused
**Status:** Partial implementation  
**Area:** Dashboard

`Requires Attention` currently focuses on configured outstanding quantity. More nuanced age-based attention logic remains to be completed/validated.

### Backup / Restore and production deployment are not complete
**Status:** Planned before v1.0  
**Area:** Operations

Database backup/restore workflow, installer/deployment packaging and production upgrade guidance are not yet complete.

## Low priority / cosmetic

### Password eye and Logout artwork is functional but not final
**Status:** Accepted for now  
**Area:** UI

The controls work correctly, but the current custom-drawn artwork was accepted as functional rather than final visual polish.

## Recently resolved

The following older issues are no longer active and should not be treated as current defects:

- Customer lower-panel whitespace/clipping.
- Customer action buttons disappearing.
- Recent Movement History date/direction width.
- Page title text clipping.
- Logout caption clipping/functionality.
- Single Entry alignment/reset-after-save.
- Business Information bottom button clipping.
- Excel Import Wizard missing Browse/Analyse controls.
- Duplicate Analyse button in Import Wizard.
- Import Wizard stepper square-number styling.
- Build migration tests hard-coded to schema version 6.
- ClosedXML row/column compile errors.
- Hard-coded app/build release version.

Resolved details remain available in `docs/CHANGELOG.md` and release notes.
