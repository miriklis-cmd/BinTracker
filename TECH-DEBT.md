# Technical Debt

These are engineering improvements, not current user-facing defects.

## UI
- Do not use Unicode glyphs as substitutes for designed UI icons. Use BinTracker-owned vector drawing helpers so icon appearance is DPI-safe, predictable and independent of installed fonts.
- Keep high-density wizard summaries scannable with compact metric cards rather than multi-line diagnostic prose; reserve vertical space for the primary review grid.
- Do not use the generic AutoSize `Card()` behaviour for fill/remaining-space regions such as Step 3 Review tabs. Explicitly set `AutoSize=false` before `Dock=Fill` for those containers.
- When adding wizard state used across multiple event handlers/pages, declare and initialize the state in the form before wiring callers; packaging sanity checks should confirm both references and backing state exist.
- Keep Review-grid column widths explicit and row wrapping enabled; the Review dataset is information-dense and should not rely on Fill sizing that silently truncates values.
- Prefer cell-level DataGridView tooltips for row-wide diagnostics; `DataGridViewRow` has no ToolTipText property.
- Prefer fixed-width action strips or shared dialog-button helpers for modal Save/Close actions; avoid Dock=Top tables when button widths must remain fixed.
- Consolidate repeated WinForms card/button/grid construction into shared UI helpers.
- Continue high-DPI regression testing across 100%, 125% and 150% scaling.
- Consider centralising typography/spacing constants.

## Import
- Workbook lock/access failures are recoverable preflight conditions, not fatal exceptions. Keep Step 4 state unchanged until fingerprint preflight succeeds and never write anything before that point.
- In `ShowReviewPageAsync`, compute reconciliation and the complete blocker list before assigning `nextButton.Enabled`; keep readiness assignment at the end of Review-state calculation.
- Keep Step 3 readiness in `ImportReviewReadiness` rather than duplicating button-enable logic in WinForms; all blockers and reconciliation state must flow through one policy.
- Exact re-import protection uses SHA-256 workbook fingerprints recorded in ImportRuns. Future import profiles/transform versions should add a parser/profile version to provenance so semantic reprocessing can be distinguished from accidental duplicate import.
- Existing-customer match decisions are wizard-session state in alpha.17. Transactional Import must consume the reviewed target CustomerId and must not recompute matching at execution time.
- Tests that create new customers must provide the same explicit decision state required by the wizard; avoid fixtures that bypass Create/Skip confirmation semantics.
- Keep reconciliation decision lookup branches explicit; avoid compact `TryGetValue` expressions that obscure definite-assignment and null/unconfirmed handling.
- Customer decision state is wizard-session state in alpha.16. Transactional Import must consume the exact reviewed decision snapshot rather than recomputing it.
- Manual legacy container-token mappings are session-scoped in alpha.15. Persist reusable aliases later inside Import Profiles, not as global assumptions.
- Legacy container inference must distinguish absent token (safe Blue Bin default) from unknown explicit token (hard blocker). Never default an unknown explicit token such as `(Tub)` to Blue.
- Post-v1.0 customer-only import should be an explicit import intent/profile capability, not a special case hidden inside balance-import logic. It should bypass container/balance requirements while reusing customer matching and merge review.
- Normalize legacy customer identity before grouping and matching; matching alone is not enough if variants have already been split into separate review rows.
- Keep balance reconciliation planning pure/read-only and separate from execution so the exact proposed adjustments can be tested before transactional writes are introduced.
- Introduce an `ImportRun`/source-row provenance model before import execution so re-import correction can be idempotent and auditable.
- Developer Database Tools are intentionally restart-based; do not evolve them into live SQLite file replacement while DbContexts may be active.
- Keep `CustomerNameNormalizer` conservative and reusable; commercial import profiles may need different normalization rules.
- Post-v1.0: consider opt-in fuzzy customer matching (edit distance/similarity), but never auto-merge fuzzy matches without user approval.
- Keep legacy Buyer-prefix parsing inside the legacy import profile/parser; generic import profiles must not assume parentheses always mean a container type.
- Keep wizard state separate from page controls so Back/Next navigation never depends on transient DataGridView cell state.
- Keep review-planning logic in the Services layer (not WinForms) so future CLI/import profiles can reuse the same safety checks.
- The Import Wizard now uses separate Analyse/Map pages; continue extracting Review/Import into dedicated view components as those stages are implemented.
- Introduce reusable import-profile abstractions so legacy/custom workbooks do not leak rules into the generic import engine.
- Add a standard BinTracker import template/profile for future customers.

## Reports
- Extract more reusable report layout primitives as the report catalogue grows.
- Keep legacy report-layout inference separate from core report generation.

## Testing
- Add broader fixture coverage for complex/custom Excel workbooks without storing private production data in the repository.
