# Audit Coverage

This is the pre-v1 audit-coverage checklist. Audit history is append-only evidence, not an editable operational ledger.

## Must be audited

- Login success/failure/lockout/logout.
- User create/activate/deactivate/role/password administration.
- Customer create/change/activate/deactivate.
- Container Type create/change/activate/deactivate.
- Saved Single Entry and Batch Entry movements.
- Movement correction/reversal: actor, timestamp, mandatory reason, original/reversal linkage, source class and success/failure where appropriate.
- Correction operations additionally preserve before/after evidence and original/neutraliser/replacement lineage; Operator changes create persistent Administrator-review state and review acknowledgement is itself audited.
- Operational report/export regression coverage must prove correction bookkeeping is suppressed by persisted lineage, corrected replacements drive displayed/exported totals, and ordinary reversal rows are not suppressed by correction-only rules.
- MovementBatch audit events expose authoritative persisted line detail.
- Authorization coverage: Operator/Admin ordinary Manual/Batch reversal; Viewer denial; generic reversal denial for Opening Adjustment and Excel Import/provenance-linked rows.
- Administrator review UX acceptance: explicit Needs review/Reviewed/not-applicable state, All/Needs review/Reviewed filtering, eligibility-aware acknowledgement action, persisted reviewer/time, audited acknowledgement and duplicate prevention.
- Persistent Administrator review-reminder acceptance: navigation-wide non-blocking visibility, live outstanding count, pending-set action, refresh after review changes, disappearance at zero, Administrator-only role behavior and presentation-independent state/navigation contracts (BT-AUD-013). The login popup remains separate.
- Audit detail action acceptance: View Batch Detail is enabled only for a selected event with persisted MovementBatch identity/detail. Future contextual entity routes remain tracked, not claimed implemented.
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

## Audit Trail release decisions

- Broader search, general multi-field filtering and Audit Trail CSV export remain a tracked enhancement/release decision, separate from mandatory pre-v1 review discoverability. CSV export is not implemented; if released it should export the filtered view where practical, include authoritative UTC timestamp/user/action/entity/ID/description/success/review fields, apply defined security/redaction rules and audit the export action.
- Audit retention/archive policy requires an explicit pre-production/release decision. No period is assumed. Cleanup must not weaken integrity; any archival/deletion policy must remain auditable and preserve required legal/business evidence.


## Requirements/register governance

The product audit-coverage matrix and the Requirements & Acceptance Register are both release-gated documents. Report PDF/CSV generation, imports, corrections, communications, backup/restore and security/admin actions must remain represented as their implementation status changes.
