# Audit Coverage

This is the pre-v1 audit-coverage checklist. Audit history is append-only evidence, not an editable operational ledger.

## Must be audited

- Login success/failure/lockout/logout.
- User create/activate/deactivate/role/password administration.
- Customer create/change/activate/deactivate.
- Container Type create/change/activate/deactivate.
- Saved Single Entry and Batch Entry movements.
- Movement correction/reversal: actor, timestamp, mandatory reason, original/reversal linkage, source class and success/failure where appropriate.
- Authorization coverage: Operator/Admin ordinary Manual/Batch reversal; Viewer denial; generic reversal denial for Opening Adjustment and Excel Import/provenance-linked rows.
- Excel Import completion, failure where appropriate, and Replace/Correct.
- Import Run provenance/replacement relationship.
- Report generation, including Customer Statement, Market Floor, Outstanding Containers, Daily Movements, Weekly Movements, Movement History, Monthly Summary and Daily Print Pack PDF output.
- CSV export for Outstanding Containers, Daily Movements, Weekly Movements, Movement History and Monthly Summary.
- Business Information/settings changes, including future logo/branding changes when implemented.
- Reminder runs and individual Email/SMS delivery attempts.
- Production Backup/Restore and database upgrade operations when implemented.

## Evidence expectations

Where relevant, events should preserve:

- timestamp;
- authenticated user;
- action/entity;
- success/failure;
- session/computer identity;
- meaningful before/after values or a concise description.

Before v1.0, test this matrix end-to-end and explicitly document any intentionally unaudited operation.


## Requirements/register governance

The product audit-coverage matrix and the Requirements & Acceptance Register are both release-gated documents. Report PDF/CSV generation, imports, corrections, communications, backup/restore and security/admin actions must remain represented as their implementation status changes.
