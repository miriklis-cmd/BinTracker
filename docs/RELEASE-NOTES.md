# BinTracker Current Release Notes

## v0.5.0-alpha.5.2

- Keeps Movement History integrated in the main BinTracker workspace and adds an explicit `← Back to Reports` action.
- Corrects the alpha.5.1 manual UI failure where the action row was vertically clipped: filter/options/actions now have explicit AutoSize row styles and the action surface reserves its button height.
- Rebalances the grid from the real Windows screenshot: Direction and Qty are compact; Date/Code/Source retain useful structured widths; Status receives the largest share of surplus width, followed by Notes and Customer; narrow layouts retain readable minimums and then scroll.
- Preserves green IN, red OUT and amber reversal badges, Status/Notes tooltips, customer-code PDF/CSV filename rules, sorting, reversal permissions and all BT-ARCH-008..015 behaviour.
- Alpha.5.1 passed the Windows source/build/test gate with 0 warnings, 0 errors and 242/242 automated tests before this corrective UI candidate. Alpha.5.2 requires a fresh Windows build and manual maximized/narrow/DPI acceptance.

## v0.5.0-alpha.5.1

- Fixes the two `CS8602` compiler warnings reported by the Windows alpha.5 release-gate build in Movement History custom badge painting.
- Explicitly verifies that WinForms supplied a non-null cell style and graphics surface before badge rendering; when either is unavailable, the grid retains its normal painting path.
- No Movement History functionality or presentation decision was removed or redesigned.

## v0.5.0-alpha.5

- Movement History now uses the full main-application workspace rather than a floating report window, preserving its filters, date shortcuts, sorting, PDF/CSV actions, Include Notes, reversal linkage/actions, permissions, sensitive-source restrictions and audit behaviour.
- Its grid now keeps predictable fields compact and dynamically distributes remaining width across Customer, Status and Notes. Readable minimums are retained when the app is narrow, at which point horizontal scrolling is allowed.
- IN and OUT use restrained green/red cell badges; reversed originals and reversal rows use amber/orange Status badges. Ledger Notes and derived status text remain unchanged, single-line rows are retained, and full Status/Notes text remains available through tooltips.
- When an applied customer filter resolves the displayed report to exactly one customer, both PDF and CSV suggested filenames include that customer's Windows-sanitized stable code. Generic filenames remain for unfiltered or multi-customer results.
- v0.5.0-alpha.4 was manually accepted at 8/8 smoke tests before this follow-up work. Alpha.5 still requires fresh Windows maximized-width and DPI smoke acceptance.

## v0.5.0-alpha.4

- Compared the actual alternate alpha.3 source against tested Work alpha.3 and selectively merged only stronger concurrency/portability foundations.
- Added payload-aware idempotency for Single Entry, Batch Entry, reversal and import: identical retries return the existing result; a different payload under the same operation ID is rejected.
- Added normalized Container Type `NameKey`, unique current-cutover ownership, optimistic revisions, concurrency-safe account mutations and split desktop/business host composition.
- Replaced server-readable path semantics with content transport plus provenance-only `SourceClientPath`; report services return PDF bytes.
- Added migration V14 and hardened BT-ARCH-008..015 audit/test coverage. SQLite remains current; PostgreSQL/API execution is not claimed.

## v0.5.0-alpha.3

### Portability and multi-user concurrency hard gate

- Establishes BT-ARCH-008..015 as permanent release gates for authenticated remote clients, central PostgreSQL, request identity, business time, client provenance, concurrent integrity, idempotency, provider isolation and transport-safe file workflows.
- Business movement/correction services now depend on user, clock and client abstractions. The current SQLite desktop deployment supplies local adapters without changing accepted functionality.
- The reversal unique constraint remains authoritative and a losing concurrent reversal is converted to the stable “already reversed” business result.
- Movement History now shows immutable derived reversal status (`Reversed — see REV-…` / `Reversal of #… — reason`) and disables Reverse for already-reversed originals and reversal rows.
- Removes provider-specific filtered-index and check-constraint SQL fragments from the shared EF model; SQLite schema migrations remain isolated in the Data provider layer.
- Automated build/test/audit/package verification is required for this candidate; PostgreSQL execution is not claimed until a central host/provider fixture exists.

## v0.5.0-alpha.1.1

- Fixed the final Batch Entry edit-mode persistence defect found in operator smoke testing: asynchronous row/customer resolution is generation-guarded, so Esc or Clear cannot be followed by a stale continuation that puts the UI back into Update Line mode.
- Clear Batch is now also a complete editor reset when the draft is already empty or the user is mid-edit.
- Recovery-dialog buttons are laid out in one fixed-height row with consistent centred text while preserving Continue Batch / Save Batch / Discard Batch order.
- Current larger icon trial is unchanged pending final branding approval.

## v0.4.0-alpha.24.2.20

- Fixed Batch Entry edit-state regressions found in operator smoke testing: Esc while editing now clears the editor, Enter in Edit mode updates rather than duplicates, Update/Remove clear the editor and return to Add to Batch mode, and removing the final row cannot leave a ghost Update state.
- Esc-to-Dashboard now synchronises the left-navigation highlight, so Batch Entry can be clicked immediately to return to the retained draft.
- Moved Batch Entry status feedback beneath the pending line/container summary.
- Unfinished-batch recovery is explicitly documented for accidental normal close as well as restart/crash/power loss; recovery actions are ordered Continue Batch / Save Batch / Discard Batch.
- Reverted the application icon to the previous larger visual trial after the newest Win32 trial appeared smaller on the taskbar.
- Operator smoke acceptance remains required for the editor-state fixes.
