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

- Report grids populated with `Rows.Add(...)` must trigger any content-based column sizing explicitly after population; `DataBindingComplete` does not run for manually added rows.

- Dedicated report windows must be responsive to monitor working area rather than fixed desktop dimensions. Filters/actions keep their required height; result grids consume the remaining client area.

- Reports page is a launcher, not a scrolling host for every report. Keep Market Floor inline. Most detailed/filter-heavy reports use dedicated windows, while Movement History is deliberately integrated into the main workspace because it is also the operational reversal surface. MainForm owns each active report surface and prevents duplicate windows/pages.

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


## Dependency wiring

- Constructor dependency changes must be propagated to every manual WinForms construction site. Prefer consistent DI/factory patterns where they reduce the risk of stale constructor calls without obscuring ownership/lifetime.


## Report filter/action layout

- Do not pack all filters and report actions into one FlowLayoutPanel. Use separate filter/action rows in detailed report windows so DPI wrapping cannot hide action buttons.


## Report sorting

- Formatted numeric grid cells (for example `72 OUT` / `3 CREDIT`) must never rely on default string sorting. Keep an authoritative typed row model attached to the grid row and compare the underlying numeric value.


## Report output consistency

- PDF and CSV should use the same displayed-result snapshot helper rather than independently re-reading the service result, otherwise on-screen sorting can diverge from exported output.


## Daily report master-data choices

- Daily Movements currently reuses the Outstanding reporting service to discover configured Container Types for its filter list. A shared report-filter/master-data provider may be worthwhile once more report windows need the same choices; do not duplicate direct DbContext UI queries.


## WinForms button labels

- WinForms treats `&` as a mnemonic marker by default. Literal ampersands in button labels must be escaped as `&&` (or mnemonics explicitly disabled) so labels such as `Generate & Open` render correctly.


## Report filter semantics

- Avoid two controls that appear to select the same semantic concept. Daily Movements separates normal entry Source from the explicit Opening Adjustment inclusion toggle.


## Detailed report control bands

- When detailed reports have many controls, use separate auto-sized semantic rows (filters / options / actions) rather than relying on one or two wrapping FlowLayoutPanels. This prevents DPI-dependent action clipping.


## Report export option semantics

- If one option affects multiple export formats, name it by the shared concept rather than a specific format. Daily Movements uses `Include notes in exports` for both PDF and CSV.


## Report filter master data

- Detailed report Container Type selectors now use `IContainerTypeService` as the authoritative configured master-data source rather than deriving choices from current outstanding balances.
- Include inactive configured types in historical report selectors; deactivation must not make historical activity unfilterable.


## Multi-view report export consistency

- Reports with multiple grids/views must export the **selected report view**, not silently fall back to the detail dataset. Weekly Movements PDF and CSV both follow the selected Daily Detail / Weekly Overview tab and its displayed ordering.


## Business branding assets

- Business Information currently stores textual business details only. Future branding support should avoid scattering logo/header handling across PDF/email code.
- Prefer one reusable branding model/service that supplies logo, custom header text, fallback behaviour and placement rules to reports, emails and other generated output.


## Test/service visibility

- Tests must not depend on internal concrete report services solely to call implementation helpers. Prefer public interfaces/results and independent expected-value calculations.


## Documentation consistency

- Current-state docs must not retain superseded UI semantics after a later patch changes them. Historical behaviour belongs only in `docs/CHANGELOG.md`.
- `docs/RoadmapCoverageMatrix.md` is the cross-check for major pre-v1/post-v1 workstreams; update it whenever roadmap scope moves.


## Documentation/process drift risk

- Past development allowed current-state documentation to drift from implemented behaviour. Treat the mandatory full-build audit as a permanent control, not a temporary cleanup measure.
- If a build changes behaviour without reconciling current-state docs, that build is incomplete even if it compiles/tests successfully.


## Report movement-row duplication

- Daily, Weekly and Movement History currently have closely related movement-row DTO/query projection code. Do not prematurely introduce a generic report abstraction, but consider extracting shared typed movement-report primitives if Monthly/other reports create meaningful maintenance duplication.


## Report refresh concurrency

- Live filter refresh uses asynchronous report queries. If future reports become slow or network-backed, consider cancellation/debouncing so rapidly changed filters cannot waste work or allow an older query to overwrite a newer result.
- Customer free-text deliberately refreshes on Enter rather than every keystroke to avoid unnecessary queries.


## Product-vs-business branding separation

- Keep the BinTracker product logo/icon separate from Business Information branding assets. Report/email identity must not accidentally substitute the BinTracker logo for the operator's own business logo.
- WinForms v1 uses restrained product branding; richer visual treatment belongs in the later WinUI 3 evaluation.


## WinForms docked-Panel autosize

- Avoid using an AutoSize `Panel` as a sizing boundary around docked/wrapping report controls. Weekly demonstrated that the parent can under-measure a docked AutoSize child after FlowLayout wrapping. Prefer AutoSize `TableLayoutPanel` containers when child preferred height must drive following rows.


## Form branding inheritance

- All WinForms windows now inherit `BinTrackerForm` so application icon behaviour is centralized. New forms should inherit `BinTrackerForm`; do not reintroduce per-form icon file loading.


## Sidebar branding width

- Keep product-branding dimensions tied to the real sidebar width rather than relying on a large fixed logo column plus oversized wordmark font. Recheck at non-100% DPI when sidebar branding changes.


## Customer Statement workflow ownership

- `CustomerStatementWorkflow` is the single WinForms orchestration point for statement period selection, save/open path and PDF launching. Keep Customers and Reports entry points thin so future Email Statement support can be added once rather than twice.


## IWin32Window owner typing

- Shared WinForms workflows should accept `IWin32Window` owners, but callers must avoid null-coalescing different concrete control types without first normalising to the interface type.


## Monthly report query scaling

- Monthly Summary currently filters the movement set in EF Core and performs the final customer/container grouping in application memory. This stays provider-neutral for SQLite/PostgreSQL. If movement volume becomes large after central PostgreSQL deployment, benchmark and move grouping into a provider-neutral SQL-translatable LINQ projection where useful.


## Local build SDK selection

- BinTracker targets .NET 8 but currently uses a compatible installed SDK as the build host (10.0.400 on the development PC). The invalid alpha.23.3 restrictive `global.json` pin was removed. If deterministic pinning is introduced later, standardise/install the selected SDK on developer/CI machines first and test the failure path.


## Build SDK policy correction

- alpha.23.3 incorrectly pinned the repository to an uninstalled .NET 8 SDK. The pin was removed in alpha.23.4.
- BinTracker continues targeting .NET 8 while using a compatible installed SDK as the build host.
- If deterministic SDK pinning is introduced later, first install/standardise that SDK on development/CI machines and test the failure path.


## Full-ZIP overlay semantics

- Full release ZIPs cannot delete stale files when extracted over an older folder. Build tooling now self-heals the one known dangerous obsolete file (`global.json` from alpha.23.3), but development instructions should continue to prefer extracting full builds into a clean folder.


## Packaging/version drift prevention

- Source/current-document version consistency is now mechanically checked by `Audit-BinTracker.ps1` before Windows restore/build/test.
- ZIP filename/root-folder identity still requires the packaging step to validate the archive itself before delivery.
- Do not rely on a prose claim that the audit was completed when these checks have not actually run.


## Reports launcher mock-up assets

- alpha.24 embeds the approved report icon artwork directly from the approved Reports mock-up to prevent later icon substitution/drift.
- If the Reports hub is reimplemented in WinUI 3 post-v1, treat these visual assets and hierarchy as design reference unless a new design is explicitly approved.

## Movement correction/reversal workflow placement
- Dedicated movement correction/reversal operational surface is deferred until the reversal engine passes acceptance.
- Movement History may retain a contextual Correct/Reverse action, but Reports should not become the primary transaction-management surface by accident.
- Revisit navigation when correction-by-replacement is implemented; prefer a coherent Movements operational area over a reversal-only top-level item.
