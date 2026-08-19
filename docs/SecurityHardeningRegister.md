# Security Hardening Finding Register

Current baseline: **v0.4.0-alpha.24.2.24**

This is the permanent ledger for the 50 findings supplied by the external BinTracker code/security audit on 19 August 2026. It is a **hard-gated pre-v1 input**, not a claim that every external severity rating is accepted unchanged. Each item must be reconciled against current source before implementation.

Allowed dispositions: `CONFIRMED-V1`, `REVIEW-V1`, `POST-V1`, `NOT-APPLICABLE`, `FIXED`. `CONFIRMED-V1` and `REVIEW-V1` block v1.0 release until changed to `FIXED`, `POST-V1`, or `NOT-APPLICABLE` with rationale.

| Finding | Disposition | Audit finding | Required treatment |
|---|---|---|---|
| BT-SH-001 | CONFIRMED-V1 | CSV formula injection in exported spreadsheet cells | Remediate and add regression/security coverage. |
| BT-SH-002 | CONFIRMED-V1 | Pending database-operation marker trusts file paths | Remediate and add regression/security coverage. |
| BT-SH-003 | CONFIRMED-V1 | Logout does not clear authenticated UserSession | Remediate and add regression/security coverage. |
| BT-SH-004 | CONFIRMED-V1 | Failed-login lockout update is concurrency-prone | Remediate and add regression/security coverage. |
| BT-SH-005 | REVIEW-V1 | Excel workbook processing lacks hostile-input resource limits | Reconcile against current source; then fix, defer with rationale, or reject with evidence. |
| BT-SH-006 | REVIEW-V1 | Business writes and audit writes are not atomic | Reconcile against current source; then fix, defer with rationale, or reject with evidence. |
| BT-SH-007 | REVIEW-V1 | Future database credentials may be stored in plaintext configuration | Reconcile against current source; then fix, defer with rationale, or reject with evidence. |
| BT-SH-008 | REVIEW-V1 | Username case normalization/uniqueness is inconsistent | Reconcile against current source; then fix, defer with rationale, or reject with evidence. |
| BT-SH-009 | REVIEW-V1 | Password verification catches all exceptions | Reconcile against current source; then fix, defer with rationale, or reject with evidence. |
| BT-SH-010 | REVIEW-V1 | Blocking startup async calls need bounded timeout/cancellation review | Reconcile against current source; then fix, defer with rationale, or reject with evidence. |
| BT-SH-011 | CONFIRMED-V1 | User listing exposes password hashes and salts via domain entities | Remediate and add regression/security coverage. |
| BT-SH-012 | CONFIRMED-V1 | Persisted batch drafts are not isolated by BinTracker user | Remediate and add regression/security coverage. |
| BT-SH-013 | CONFIRMED-V1 | Malformed draft JSON object graph can crash recovery | Remediate and add regression/security coverage. |
| BT-SH-014 | CONFIRMED-V1 | Draft JSON read/deserialization is unbounded | Remediate and add regression/security coverage. |
| BT-SH-015 | CONFIRMED-V1 | Undefined enum values are accepted at service boundaries | Remediate and add regression/security coverage. |
| BT-SH-016 | CONFIRMED-V1 | int quantity/balance arithmetic can overflow | Remediate and add regression/security coverage. |
| BT-SH-017 | CONFIRMED-V1 | Audit-log reads lack service-layer authorization | Remediate and add regression/security coverage. |
| BT-SH-018 | CONFIRMED-V1 | Report/read services rely on UI for authentication | Remediate and add regression/security coverage. |
| BT-SH-019 | CONFIRMED-V1 | Session authorization is cached and not revalidated after account changes | Remediate and add regression/security coverage. |
| BT-SH-020 | REVIEW-V1 | Duplicate import prevention is check-then-insert | Reconcile against current source; then fix, defer with rationale, or reject with evidence. |
| BT-SH-021 | REVIEW-V1 | Container system-code generation is race-prone | Reconcile against current source; then fix, defer with rationale, or reject with evidence. |
| BT-SH-022 | REVIEW-V1 | Authentication checks are inconsistent across services | Reconcile against current source; then fix, defer with rationale, or reject with evidence. |
| BT-SH-023 | REVIEW-V1 | Future report dates are silently changed | Reconcile against current source; then fix, defer with rationale, or reject with evidence. |
| BT-SH-024 | REVIEW-V1 | Daily report search filters after materialization | Reconcile against current source; then fix, defer with rationale, or reject with evidence. |
| BT-SH-025 | REVIEW-V1 | Outstanding report loads full lookup entities/tables | Reconcile against current source; then fix, defer with rationale, or reject with evidence. |
| BT-SH-026 | REVIEW-V1 | Temporary BatchDraftTests.cs.tmp repository debris | Reconcile against current source; then fix, defer with rationale, or reject with evidence. |
| BT-SH-027 | CONFIRMED-V1 | SQLite backup connection string interpolates user-selected filename | Remediate and add regression/security coverage. |
| BT-SH-028 | CONFIRMED-V1 | Authentication inputs lack strict upper bounds | Remediate and add regression/security coverage. |
| BT-SH-029 | REVIEW-V1 | Failed-login auditing can amplify storage/DoS pressure | Reconcile against current source; then fix, defer with rationale, or reject with evidence. |
| BT-SH-030 | REVIEW-V1 | Authentication availability depends on separate audit writes | Reconcile against current source; then fix, defer with rationale, or reject with evidence. |
| BT-SH-031 | REVIEW-V1 | Administrator password reset does not revoke existing target session | Reconcile against current source; then fix, defer with rationale, or reject with evidence. |
| BT-SH-032 | REVIEW-V1 | Administrator self-reset can leave session/database state inconsistent | Reconcile against current source; then fix, defer with rationale, or reject with evidence. |
| BT-SH-033 | REVIEW-V1 | Backup source/destination same-file detection is path-string based | Reconcile against current source; then fix, defer with rationale, or reject with evidence. |
| BT-SH-034 | REVIEW-V1 | Backup destination overwrite policy is unsafe/ambiguous | Reconcile against current source; then fix, defer with rationale, or reject with evidence. |
| BT-SH-035 | REVIEW-V1 | Database settings writes are non-atomic | Reconcile against current source; then fix, defer with rationale, or reject with evidence. |
| BT-SH-036 | REVIEW-V1 | Batch-draft temporary filename is predictable | Reconcile against current source; then fix, defer with rationale, or reject with evidence. |
| BT-SH-037 | REVIEW-V1 | Corrupt-draft quarantine filenames can collide/overwrite evidence | Reconcile against current source; then fix, defer with rationale, or reject with evidence. |
| BT-SH-038 | REVIEW-V1 | EF string lengths are not consistently enforced by SQLite constraints | Reconcile against current source; then fix, defer with rationale, or reject with evidence. |
| BT-SH-039 | REVIEW-V1 | Customer input validation lacks consistent maxima/normalization | Reconcile against current source; then fix, defer with rationale, or reject with evidence. |
| BT-SH-040 | REVIEW-V1 | Security identifiers permit Unicode confusables | Reconcile against current source; then fix, defer with rationale, or reject with evidence. |
| BT-SH-041 | REVIEW-V1 | Import fingerprinting does not eliminate file-use races | Reconcile against current source; then fix, defer with rationale, or reject with evidence. |
| BT-SH-042 | REVIEW-V1 | Import history permanently stores full source paths | Reconcile against current source; then fix, defer with rationale, or reject with evidence. |
| BT-SH-043 | REVIEW-V1 | ApplicationSettings singleton invariant lacks database enforcement | Reconcile against current source; then fix, defer with rationale, or reject with evidence. |
| BT-SH-044 | REVIEW-V1 | Unknown report-filter enums silently return empty data | Reconcile against current source; then fix, defer with rationale, or reject with evidence. |
| BT-SH-045 | REVIEW-V1 | Synchronous PDF rendering can block UI inside async workflow | Reconcile against current source; then fix, defer with rationale, or reject with evidence. |
| BT-SH-046 | REVIEW-V1 | PDF output service paths lack explicit overwrite/path policy | Reconcile against current source; then fix, defer with rationale, or reject with evidence. |
| BT-SH-047 | CONFIRMED-V1 | Builds are not reproducible/pinned/locked | Remediate and add regression/security coverage. |
| BT-SH-048 | CONFIRMED-V1 | No automated dependency/security supply-chain gate | Remediate and add regression/security coverage. |
| BT-SH-049 | REVIEW-V1 | Packaged artifacts lack checksum/signing integrity | Reconcile against current source; then fix, defer with rationale, or reject with evidence. |
| BT-SH-050 | CONFIRMED-V1 | Audit-BinTracker is governance-heavy and lacks code-security checks | Remediate and add regression/security coverage. |

## Hard-gate rules

- No finding ID BT-SH-001 through BT-SH-050 may disappear from this file.
- New external findings are appended; existing IDs are never renumbered or reused.
- The dedicated hardening workstream remains immediately after Movement Correction/Reversal and before Business Information/Branding and Communications.
- Alpha builds may continue while findings are open so remediation can be developed and tested.
- A v1.0 release is prohibited while any finding remains `CONFIRMED-V1` or `REVIEW-V1`.
- Each `FIXED` finding must have source/test evidence before closure; severity can be adjusted during reconciliation but the finding itself remains in the ledger.
