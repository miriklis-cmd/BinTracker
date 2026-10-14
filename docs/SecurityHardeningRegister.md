# Security Hardening Finding Register

External finding-capture baseline: **v0.5.0-alpha.6.5**. Current repository/application baseline: **v0.5.0-alpha.13**.

This is the permanent ledger for the 50 findings supplied by the external BinTracker code/security audit on 19 August 2026. It is a **hard-gated pre-v1 input**, not a claim that every external severity rating is accepted unchanged. Each item must be reconciled against current source before implementation.

Allowed dispositions: `CONFIRMED-V1`, `REVIEW-V1`, `POST-V1`, `NOT-APPLICABLE`, `FIXED`. `CONFIRMED-V1` and `REVIEW-V1` block v1.0 release until changed to `FIXED`, `POST-V1`, or `NOT-APPLICABLE` with rationale.

| Finding | Disposition | Audit finding | Required treatment |
|---|---|---|---|
| BT-SH-001 | FIXED | CSV formula injection in exported spreadsheet cells | Alpha.11 routes every current report CSV path through one protected structured serializer; focused regression/security coverage is recorded below. |
| BT-SH-002 | FIXED | Pending database-operation marker trusts file paths | Alpha.12 validates the complete marker at its sole Data reader before transition and derives all operative/cleanup locations from trusted configuration; focused hostile-marker and recovery coverage is recorded below. |
| BT-SH-003 | FIXED | Logout does not clear authenticated UserSession | Alpha.13 atomically clears the desktop session before logout audit work and before every new login attempt; focused lifecycle/authorization coverage is recorded below. |
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

## Fixed-finding evidence

### BT-SH-001 — CSV formula injection

- **Enforcement:** `ReportCsvExportContract.WriteRows` is the single cell-serialization boundary for Daily, Weekly Detail/Overview, Monthly, Outstanding and Movement History CSV exports. It apostrophe-neutralizes formula-leading text after leading whitespace/control/Unicode-format characters, then performs ordinary CSV quote escaping. Typed numeric primitives bypass text neutralization and retain invariant numeric output.
- **Coverage:** pre-fix characterization proved all five duplicated form helpers preserved comma/quote/CR/LF/empty behavior but emitted `=`, `+`, `-` and `@` prefixes unchanged. Post-fix focused tests cover ordinary/empty/structural escaping, all four formula markers, whitespace/control/format-prefix bypasses, formula text containing commas/quotes/newlines, unchanged negative/decimal/date output, displayed row/column order and absence of form-local text serializers.
- **Scope:** PDF and on-screen reports, filters, columns, row ordering, filenames and export auditing are unchanged. That alpha.11 pass left BT-SH-002..050 at their prior dispositions.

### BT-SH-002 — pending database-operation marker path trust

- **Threat/failure model:** the configured active database path was already supplied independently and normal Load execution compared its marker stage with the fixed marker-directory artifact. Fresh nevertheless accepted a non-null marker `StagedDatabasePath`, and completion canonicalized then deleted any nonblank marker path. Publication-ready Load re-entry reached the same deletion without the earlier execution-time stage check. A forged Fresh or publication-ready marker could therefore delete an arbitrary absolute or relative file after readiness; an undefined operation also transitioned the marker before being rejected.
- **Enforcement:** `ValidatedPendingDatabaseOperation.Read` is the only marker deserialization/validation boundary. Before the coordinator can persist a state transition, replace/create a database or clean an artifact, it validates the structured shape, enum/state consistency, configured active identity and operation-specific path rules. The only Load stage is derived as `pending-database-load.db` beside the trusted configured marker and that derived path alone is used for Load and cleanup. Fresh cannot carry a stage. Publication-ready re-entry passes the same validation. Invalid evidence remains in place for diagnosis. Relative/traversal, alternate absolute, short-name and marker-supplied symlink/reparse aliases cannot satisfy the exact fully-qualified path rule. Replacing the trusted configured app-data directory or its fixed artifact through independent OS filesystem authority is not a capability conferred by marker text and remains outside this marker-input finding; the staged database still undergoes the existing structural/current-health validation before publication.
- **Compatibility/recovery:** the JSON field shape is unchanged; the original five fields remain required and later state/publication fields keep their established defaults, so legitimate pre-state-field markers remain readable. A valid publication-ready Load may have already removed the fixed stage during partial cleanup, and re-entry still completes without replay. Execution-in-progress still requires controlled recovery. Marker age is not treated as corruption because legitimate interrupted recovery has no safe arbitrary expiry.
- **Coverage:** pre-fix security regression reproduced arbitrary cleanup redirection for Fresh and publication-ready Load and proved an undefined operation mutated the marker before failure. Post-fix coverage rejects hostile absolute and relative/traversal paths, wrong active identity, missing execution-required Load evidence, undefined enums and malformed/duplicate/extended JSON with marker bytes, protected file and active database unchanged. Existing valid Load/Fresh, publication-ready cleanup retry, partial cleanup and recovery-required behavior remain green.
- **Scope:** Data/infrastructure retains SQLite/filesystem ownership. Backup contents, migration manifests/hashes, Load/Fresh publication sequencing, Services authorization, WinForms behavior, Task20 lineage and BT-ARCH-018 boundaries are unchanged. BT-SH-003..050 retain their prior dispositions.

### BT-SH-003 — authenticated desktop session survives logout

- **Threat/failure model:** desktop composition registers one singleton `UserSession`, and `Program.RunApplication` retains one DI scope across every login/logout/login loop. `AuthenticationService.LogoutAsync` previously wrote `LOGOUT` only; it never cleared user ID, username, display name, role, password-change state or login time. The login screen therefore reused an authenticated object, Administrator-only Services continued to authorize the prior user after visual logout, repeated logout wrote repeated events and failed/cancelled login could retain stale authority. A later successful login overwrote fields, but only after the stale interval.
- **Enforcement:** `UserSession` now owns one immutable authentication snapshot replaced atomically by its central sign-in/sign-out operations. `LogoutAsync` captures and clears the prior snapshot before awaiting audit persistence; the existing internal authentication-audit seam records the captured actor, so cancellation/audit failure cannot preserve authentication. `LoginAsync` clears any prior snapshot before database/password work that may fail or be cancelled. Anonymous state has no user ID, the established anonymous labels, Viewer role, no password-change requirement and no login time. Repeated logout is a no-op, while successful login installs only the authenticated new snapshot.
- **Coverage:** the pre-fix lifecycle run passed only normal login and failed 6/7 expected security cases because logout/failed-login/cancel paths retained `IsAuthenticated=true`. Post-fix focused coverage passes 8/8 for normal login, complete logout clearing, stale Administrator denial, logout-audit actor attribution, failed/cancelled login, successful different-user re-login, repeated logout and audit-cancelled logout. The adjacent authentication/audit, user-administration, developer-database authorization and movement-entry authorization set passes 37/37 with zero failed/skipped.
- **Scope:** the desktop singleton remains a bounded local adapter; future hosts still supply request-scoped `IUserContext` and do not inherit `UserSession`. Authorization remains in Services, the UI still requests logout through `IAuthenticationService`, and the existing process/client session identifier and accepted user administration behavior are unchanged. This pass does not revalidate account mutations during an existing session and does not disposition BT-SH-004 or BT-SH-019/022/030/031/032. BT-SH-004..050 retain their prior dispositions.

## Hard-gate rules

- No finding ID BT-SH-001 through BT-SH-050 may disappear from this file.
- New external findings are appended; existing IDs are never renumbered or reused.
- The dedicated hardening workstream remains immediately after Movement Correction/Reversal and before Business Information/Branding and Communications.
- Alpha builds may continue while findings are open so remediation can be developed and tested.
- A v1.0 release is prohibited while any finding remains `CONFIRMED-V1` or `REVIEW-V1`.
- Each `FIXED` finding must have source/test evidence before closure; severity can be adjusted during reconciliation but the finding itself remains in the ledger.
