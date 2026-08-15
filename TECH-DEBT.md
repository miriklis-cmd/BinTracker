# Technical Debt

Engineering improvements that are not currently user-facing defects. Product work belongs in `docs/Roadmap.md`; active defects/limitations belong in `KNOWN-ISSUES.md`.

## Architecture

- Continue separating WinForms presentation from reusable Services/domain policy so future mobile/web/CLI clients can reuse business logic.
- Extract large views/forms (especially Import Review) into smaller components.
- Centralise typography, spacing, buttons, cards and DPI-safe UI primitives.
- Keep database provider configuration isolated; avoid SQLite-specific assumptions in core business rules where practical.

## Database / data integrity

- Add source-row/import-profile metadata needed for changed-workbook correction tooling.
- Review useful database-level constraints/indexes rather than relying only on service validation.
- Establish a production backup-before-migration policy.
- Review SQLite concurrency assumptions before production and before any network/shared deployment.
- PostgreSQL remains a candidate for future simultaneous multi-computer deployment; do not enable it until concurrency, migration and deployment are designed/tested.

## Import

- Older replacement runs created before alpha.19.12.1 cannot have exact correction-difference detail reconstructed if their replaced movement rows are already gone. History explicitly labels these as “not captured by the build that created this run”; do not fabricate/backfill differences.
- Import Run history is intentionally read-only. Do not add deletion/edit controls; corrections flow through Replace/Correct so provenance remains intact.
- Import/replacement comparisons must use stable database identity (`ContainerTypeId`) for configured containers; display names/tokens are presentation only and must never become reconciliation keys.
- Preflight context must include cutover/profile identity whenever UI decisions depend on same-cutover history. Do not rely on execution-time guards to discover state the operator needed to see earlier.
- Same-cutover replacement baseline is historical, not current-state: use legitimate movements strictly before the cutover date while excluding the prior ImportRun. Same-day/later Manual/Batch activity must remain outside the corrected workbook reconciliation and survive on top.
- Replacement comparison currently summarizes changed net customer/container positions and movement counts. A future Import Run details UI can add line/source-row differences without widening the replacement safety boundary.
- Historical alpha.19.x provenance backfill intentionally links only `Adjustment`/`ExcelImport` rows with a strict `IMPORT-<numeric id>` reference that resolves to an existing ImportRun; do not broaden inference to Manual/Batch rows.
- Keep Review/reconciliation planning pure/read-only and reusable.
- Transaction execution must always rebuild/validate against the live database immediately before writes.
- Exact re-import identity uses SHA-256; future Import Profiles should add profile/parser version to provenance.
- Changed-workbook same-cutover correction must operate only on records linked to the prior ImportRun and must never touch legitimate operator movements.
- Manual legacy token aliases are session-scoped today; future Import Profiles should persist reusable aliases per profile.
- Keep no-token Blue default distinct from unknown explicit token. Unknown explicit tokens must never silently become Blue.
- Keep legacy Buyer-prefix parsing inside the legacy profile rather than generic importer behaviour.
- Keep normalization conservative. Fuzzy matching may be offered post-v1.0 as a suggestion only.
- Customer-list-only import should be a post-v1.0 explicit import intent that reuses matching/confirmation but bypasses balances.
- Add broader non-production Excel fixtures covering strange/custom workbook layouts without committing private business workbooks.

## Reports

- Dedicated report windows must be responsive to monitor working area rather than fixed desktop dimensions. Filters/actions keep their required height; result grids consume the remaining client area.

- Reports page is a launcher, not a scrolling host for every report. Keep Market Floor inline; detailed/filter-heavy reports belong in dedicated windows. MainForm owns one live instance per report window to prevent duplicates.

- Market Floor is an operational ~4am document: maximise readable type while guaranteeing front + reverse pagination.
- Both Market Floor pages must derive density from actual rendered row load, including extra non-standard container rows and likely wraps.
- Blue is implicit on Market Floor; non-standard regular containers such as Yellow must be explicit.
- `IsSpecialFloorReportContainer` is authoritative for the Special Containers block; Bulk is special in the production configuration.
- Cash/COD credits stay with Cash/COD; only Account credits belong in the separate CREDIT block.
- Adjustment movements change book/opening position and must not be presented as physical Taken/Returned movements.
- Extract reusable report layout/export primitives as the catalogue grows.
- Keep report-specific legacy presentation rules out of core balance semantics.

## Customer UI

- Customer master/detail filtering must suppress async selection events during reload and only display a currently visible result.
- Dirty-state snapshots remain per editor, but the operator-facing **Save / Discard / Cancel** dialog is shared. Consider extracting the remaining navigation/snapshot mechanics if more editable master-data screens are added.
- Consider common validation/error-display helpers for edit forms.
- Save button could visually indicate pending editor changes; this is polish, not a data-loss blocker now that navigation guards are implemented.

## Communications

- Provider credentials must never be stored in plain text or committed configuration.
- Make reminder sends idempotent/retry-safe to avoid duplicate Email/SMS after timeouts.
- Store enough provider response metadata for support while avoiding sensitive payload leakage.
- Keep reminder eligibility/business rules separate from provider adapters.

## Security / operations

- Add structured application error/support logging with redaction.
- Formalise secret storage before Email/SMS or hosted services.
- Review authorization at both UI and service layer.
- Add production backup/restore abstractions separate from Developer Database tooling.
- Define installer/update signing and upgrade rollback strategy.

## Testing

- Keep the Import failure-injection seam test-only/no-op in production; use it for deterministic transaction-boundary regression tests rather than exposing runtime failure controls.
- Continue the regression rule: when a real defect is found, add a reproducing automated test where practical.
- Add Release-build CI/validation in addition to Debug.
- Add high-DPI automated/manual acceptance coverage.
- Add stress fixtures for Market Floor row-density extremes.

## Database provider readiness

- Perform a PostgreSQL readiness audit before central multi-user deployment.
- Inventory SQLite-specific PRAGMA/raw SQL/schema upgrade paths, file-path assumptions and developer backup/reset tooling.
- Avoid adding a generic Repository abstraction solely for provider portability; keep provider-neutral business behaviour in Services and isolate provider-specific infrastructure.

## Reporting foundation

- Historical reporting deliberately derives as-of-date positions from the movement ledger. Do not introduce daily snapshot persistence unless a measured performance/recovery need justifies it.
- Outstanding CSV export is implemented; PDF/print presentation remains part of the Reports phase.
