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
- Unified schema-17 logical changes create one primary AuditEvent atomically with the operation/generation, including Restore and partial whole-root decisions. BeforeValues contains trusted current lineage pointers and business state; AfterValues contains the result generation, per-line action/state/mask/pointers/resulting business state, relevant new movement identities and optional physical output. Structured lineage remains authoritative. Review remains after-the-fact acknowledgement and references operation/audit identity, not an optional physical output batch.
- The unified mutation writer uses the IMP-06 caller-owned DbContext/transaction appender without changing the independent AuditService. Normal composition registers the dormant mutation writer; the active SQLite writer and this operation/audit execution are available only through explicit schema-17 composition.
- Legacy association requires one unique complete structured persisted-ID match; unmatched evidence remains independently readable and is never linked by prose/time/business values.
- Operational corruption and audit corruption are distinct: Invalid operational state fails affected numbers; audit-only corruption preserves proven numbers but blocks mutation/review/evidence-completeness output with critical health for the affected target root. Unrelated healthy roots remain mutable.
- Operational report/export regression coverage must prove correction bookkeeping is suppressed by persisted lineage, corrected replacements drive displayed/exported totals, and ordinary reversal rows are not suppressed by correction-only rules.
- MovementBatch audit events expose authoritative persisted line detail.
- Authorization coverage: Operator/Admin ordinary Manual/Batch reversal; Viewer denial; generic reversal denial for Opening Adjustment and Excel Import/provenance-linked rows.
- Administrator review UX acceptance: explicit Needs review/Reviewed/not-applicable state, All/Needs review/Reviewed filtering, eligibility-aware acknowledgement action, persisted reviewer/time, audited acknowledgement and duplicate prevention.
- Persistent Administrator review-reminder acceptance: navigation-wide non-blocking visibility, live outstanding count, pending-set action, refresh after review changes, disappearance at zero, Administrator-only role behavior and presentation-independent state/navigation contracts (BT-AUD-013). The login popup remains separate.
- Audit detail action acceptance: persisted MovementBatch events use authoritative batch identity; correction/reversal events and their exact single-event review acknowledgements use the referenced audit event's authoritative lineage. Invalid/multi-event references fail closed and unrelated events remain non-navigable.
- Excel Import completion, failure where appropriate, and Replace/Correct.
- Import Run provenance/replacement relationship.
- Report generation, including Customer Statement, Market Floor, Outstanding Containers, Daily Movements, Weekly Movements, Movement History, Monthly Summary and Daily Print Pack PDF output.
- CSV export for Outstanding Containers, Daily Movements, Weekly Movements, Movement History and Monthly Summary.
- Business Information/settings changes, including future logo/branding changes when implemented.
- Reminder runs and individual Email/SMS delivery attempts.
- Production Backup/Restore operations when implemented, plus database upgrade attempts when the dormant lineage coordinator is activated.
- Lineage preflight, verified pre-upgrade backup/manifest, migration/postflight and recovery attempts when activated; the dormant infrastructure and isolated migration tests do not yet constitute production audit wiring. Recovery evidence must survive restoration to an older database snapshot.

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
