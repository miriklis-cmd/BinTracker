# BinTracker Current Release Notes

## v0.5.0-alpha.5.5.3

- Replaces Movement History's standalone `← Reports` content-row button with a shell-level `Reports › Movement History` breadcrumb in the main page header; Reports is clickable and returns to the Reports landing hub.
- Movement History content now starts directly with Filters / Options / Actions / Summary / Grid, preserving the compact alpha.5.5.2 layout.
- Records the v1 Option-B report-hosting decision: Reports remains the hub and detailed reports migrate into the main workspace using the same breadcrumb convention.
- Tables the persistent fully integrated Reports workspace (Option C) for discussion if/when BinTracker moves away from WinForms, including the planned WinUI 3 evaluation.
- Adds permanent BT-RPT-018 and BT-UI-014 requirements and updates the BT-HIST source gate to protect the shell breadcrumb instead of the removed content-level Back button.

## v0.5.0-alpha.5.5.2

- Removes the Movement History controls-card container entirely after alpha.5.5.1 still showed a large blank vertical band at runtime.
- Filters, Options and Actions are now direct AutoSize rows in the root page layout, followed immediately by Summary and the expanding Grid row.
- The BT-HIST source gate now requires this six-row direct layout and rejects reintroduction of the `controlsCard` structure.
- Preserves all accepted Movement History filters, buttons, grid sizing, badges/tooltips, reversal behavior, Status/Notes export semantics and customer-aware filenames.

## v0.5.0-alpha.5.5.1

- Corrects the BT-HIST-002..006 source audit after alpha.5.5 replaced the old calculated controls-row height with direct AutoSize content-height layout.
- The audit now protects the new structural AutoSize/GrowAndShrink layout rather than requiring the removed `controlsRowStyle` / `ResizeControlsCard` implementation that caused the alpha.5.4 blank band.
- No Movement History business/export behavior is changed from alpha.5.5.

## v0.5.0-alpha.5.5

- Removes the large empty vertical band introduced by alpha.5.4 beneath the Movement History action row.
- The integrated controls card now uses direct content-height AutoSize layout inside an AutoSize parent row; no separately calculated controls height is reserved.
- Preserves fully visible action buttons, `← Reports`, current grid widths/badges/tooltips, reversal semantics, customer-aware filenames, and Status/optional Notes in Movement History exports.
- Alpha.5.4 manual smoke confirmed the action buttons were visible and the notes-enabled PDF correctly contained both Status and Notes; only the excessive blank band remained a UI acceptance blocker.

## v0.5.0-alpha.5.4

- Fixes the remaining Movement History action-button clipping found during alpha.5.3 Windows smoke testing.
- Replaces the nested auto-sized Panel/docked-table measurement path with a direct auto-sized TableLayoutPanel card so Filter, Options and Actions rows contribute structurally to the card height.
- The Actions row now derives its required height from its 40-pixel buttons plus padding and may wrap as a whole row at genuinely narrow widths; the summary/grid cannot overlap it.
- Preserves the integrated page, `← Reports`, column allocation, badges/tooltips, reversal semantics, customer-aware export names and Status in Movement History PDF/CSV.
- Alpha.5.3 passed the Windows source audit, built with zero warnings/errors and passed 242/242 automated tests; manual UI acceptance remained blocked only by the clipped action row.

## v0.5.0-alpha.5.3

- Corrects the remaining alpha.5.2 Movement History manual-layout defects: protected action row, grouped filter wrapping, usable Date/Code/Direction widths and cleaner `← Reports` integrated-page navigation.
- Uses concise reversal Status text in the grid while retaining the complete derived reversal reason in the tooltip.
- Adds semantic Status to Movement History PDF and CSV so saved/exported history preserves reversal linkage. Other operational summary reports are unchanged because Status is specific to Movement History correction-ledger semantics.
- Requires fresh Windows build and Movement History smoke acceptance.

## v0.5.0-alpha.5.3

- Keeps Movement History integrated in the main workspace and adds an explicit `← Back to Reports` navigation action.
- Corrects the alpha.5.1 manual layout failure by structurally reserving the action row so report buttons are not clipped.
- Rebalances the grid from Windows smoke evidence: Date/Code/Source receive readable structured widths, Direction/Qty are compact, and Status/Notes receive greater responsive priority while narrow layouts still scroll at minimum widths.
- Preserves direction/reversal badges, tooltips, reversal policy and customer-code PDF/CSV filenames.
- Alpha.5.1 passed Windows source audit, zero-warning/zero-error build and 242/242 automated tests before this UI correction; alpha.5.2 requires fresh Windows build and UI/DPI acceptance.

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

## v0.4.0-alpha.24.2.19

- Fixed the Batch Entry post-add reset regression: rebinding the pending grid no longer auto-selects the first draft row and reloads its Customer/Quantity into the editor. Customer, Quantity, Reference, Notes and customer preview now remain cleared after Add to Batch while Date / Batch Type / Container Type carry forward.
- Crash/power-loss recovery now requires an explicit operator decision instead of silently restoring into the UI: Continue Batch, Save Batch, or Discard Batch. The prompt shows movement date, direction, pending line count, total quantity and the draft's last-saved time. Discard requires confirmation; failed recovered-batch save leaves the draft intact and opens Batch Entry.
- Added recovery-state tracking so only a draft loaded from disk at process startup triggers the recovery prompt; normal same-process navigation/logout drafts are not mislabeled as crash recovery.
- Installed the newly supplied latest hybrid application icon as the next visual trial; branding approval remains pending.
- Batch Entry recovery/reset behaviour remains pending operator smoke acceptance on Windows.

## v0.4.0-alpha.24.2.17

- Implemented the remaining Batch Entry cleanup in code, pending operator smoke acceptance: explicit Esc edit/clear/exit semantics, post-add field reset/focus, and crash/power-loss draft recovery.
- Add to Batch now clears Customer, Quantity, Reference, Notes and customer preview, returns focus to Customer, and deliberately carries Movement Date / Batch Type / Container Type forward.
- Batch drafts are atomically persisted to `%LOCALAPPDATA%\BinTracker\batch-entry-draft.json` whenever pending lines change and restored in a new application process; Save Batch and Clear Batch remove the recovery file.
- Added unit coverage for persisted draft restoration and recovery-file removal.
- Installed the newly supplied `latest BinTracker-hybrid.zip` icon as the next visual trial; branding approval remains pending user comparison.

## v0.4.0-alpha.24.2.16

- Fixed the remaining report sort-layout shift: sort-indicator space is now reserved when each report grid is configured, so sorting no longer widens the active column or pushes neighbouring columns sideways.
- Preserved the accepted filled-triangle + priority indicators (`▲1`, `▼1`, `▲2`, etc.) and the existing typed comparator behaviour.
- Strengthened BT-RPT-017 acceptance/audit wording to require stable column widths as well as stable header height.
- The newly supplied application icon remains a visual trial; no icon asset change was made in this revision.

## v0.4.0-alpha.24.2.15

- Fixed the remaining report sort-header layout issue: active triangle/priority indicators now stay on one line (`Direction ▲1`, `Type ▲2`, etc.) rather than wrapping and visually changing the grid header height. The accepted comparator/sort semantics are unchanged.
- Installed the newly supplied hybrid application icon as another visual trial. It is not yet a permanent branding decision and can be reverted after user comparison.
- Strengthened BT-RPT-017/audit coverage for single-line, DPI-visible sort indicators.

## v0.4.0-alpha.24.2.14
- Replaced the application/executable/window icon asset with the user-supplied `BinTracker-hybrid.ico`; the existing common `BinTrackerForm` branding path continues to propagate the executable icon to login, report breakout, and other application windows.

- Replaced thin report sort arrows with filled triangle indicators (`▲1`, `▼1`, `▲2`, etc.) so direction is easier to see at normal Windows scaling. Sort priority numbering and sorting behaviour are unchanged.

## v0.5.0-alpha.2

### Operational reversal authorization

- Administrator and Operator can reverse ordinary Single Entry (`Manual`) and Batch Entry (`Batch`) movements, including movements entered by another operator.
- Viewer remains unable to reverse movements.
- Generic reversal rejects Opening Adjustments because they establish brought-forward position and require an Administrator-controlled adjustment workflow.
- Generic reversal rejects Excel Import and ImportRun-linked movements and directs the user to Administrator Replace / Correct so import provenance/reconciliation remains intact.
- Existing append-only linkage, mandatory reason, audit event, double-reversal protection and reversal-of-reversal protection are preserved.
- TEST REQUIRED: Targeted — full automated build/test gate; Operator ordinary Manual/Batch reversal; Viewer visibility/denial; Opening Adjustment and Excel Import generic-reversal denial.



## v0.5.0-alpha.1.3

### Reversal dialog full-layout correction + release-gate repair

- Carries forward the alpha.1.2 reversal-dialog full-layout correction: guaranteed Reason editor and action-row space at supported runtime/DPI.
- Corrects the Release Notes candidate heading to the exact standalone format required by `Audit-BinTracker.ps1`.
- No reversal ledger, permission or audit semantics changed.
- TEST REQUIRED: Targeted — full build/test gate, then verify Reason editor and Cancel/Create Reversal buttons are simultaneously visible and usable.


## v0.5.0-alpha.1.2

- Fixes the second reversal-dialog acceptance blocker: after the Reason field became visible, the action row could still be pushed below the client area at runtime/DPI.
- Uses an explicit vertical contract: 620x540 client area, 130px Reason row, 54px action row, non-autosizing button panel, and a minimum multiline Reason editor height.
- Preserves the existing reversal business logic and authorization/audit behavior.
- Corrects accidental historical-version wording introduced by the previous blanket documentation version replacement.
- TEST REQUIRED: Targeted — build/test gate, then verify Reason editor and Cancel/Create Reversal buttons are simultaneously visible and usable.


## v0.5.0-alpha.1.1 — Reversal acceptance blocker fix

- Fixes the required Reason editor collapsing to effectively zero height in the Reverse Saved Movement dialog.
- Gives the reason region explicit DPI-safe vertical space and a minimum multiline editor height.
- Records the dedicated correction/reversal operational-surface decision as deferred until the reversal engine passes acceptance; Movement History remains the current contextual entry point.
- No reversal ledger/business-rule semantics changed from alpha.1.



## v0.5.0-alpha.1 — Movement Correction/Reversal milestone

- Starts the documented clean milestone/versioning scheme.
- Adds the first append-only saved-movement reversal workflow from Movement History.
- Original ledger rows are preserved; reversal rows are equal/opposite and linked to the original.
- Reversal requires an Administrator, a reason, and records `MOVEMENT_REVERSED` audit data in the same transaction.
- Adds SQLite schema migration V13 for movement reversal linkage/reason provenance.
- Fixes V13 to use a dedicated strict allow-listed `BinMovements` schema helper rather than the Customers-only migration helper.
- Adds integration coverage for reversal authorization/linkage/audit and V13 schema/index migration.
- TEST REQUIRED: Full automated build/test gate, then targeted real-app reversal acceptance.


## v0.4.0-alpha.24.2.12

- Fixed report sort-state visibility without changing the accepted comparator logic. Active sort columns now show explicit `↑1` / `↓1`, `↑2` / `↓2`, etc. indicators in the header caption, so direction and multi-sort priority remain visible even where WinForms suppresses its native glyph.
- Added header-width protection for explicit sort indicators and widened Direction in Daily Movements and Weekly Movements so the indicator is not clipped at supported DPI.
- Added permanent requirement BT-RPT-017 to gate visible sort direction/priority across applicable report grids.

## v0.4.0-alpha.24.2.11

- Corrected Outstanding Containers Position sorting to use the underlying signed `Balance` value from each report row rather than the formatted `OUT`/`CREDIT` caption. Credits therefore sort as negative positions and outstanding balances as positive positions.
- Added a shared typed-sort-value hook so report grids can sort formatted display columns by their actual business value instead of reverse-engineering screen text.
- The generic numeric fallback now also treats formatted `CREDIT` values as negative, protecting other report grids that display signed container positions as text.
- Outstanding Containers Last movement sorting now also uses the underlying typed report date.
- Strengthened BT-RPT-016 to require signed credit/outstanding semantics for position sorting.

## v0.4.0-alpha.24.2.7

- Outstanding Containers now has a Balance filter: Outstanding only (default), Credits only, or All non-zero. Credits-only results flow through the screen, PDF and CSV exports.

## v0.4.0-alpha.24.2.6

### Reports landing-page vertical-fit correction

- Removed the informational PDF/CSV/date notice bar from the bottom of the Reports landing page.
- Reallocated the reclaimed vertical space to the report-card regions, with a larger share reserved for Explore Reports so descriptions and action buttons remain fully visible at the display scaling that exposed alpha.24.2.5 clipping.
- The existing report export/date rules remain enforced by the report workflows; only the redundant landing-page notice was removed.

### Acceptance requirement

Run `Build-BinTracker.bat`, then verify the Reports landing page at the same Windows scaling: there is no bottom information bar, both lines of every Explore description are visible, all Open captions are complete, and the page remains free of horizontal scrolling.

## v0.4.0-alpha.24.2.5

### Reports landing-page layout/button correction

- Removed the Reports landing-page scroll host entirely at normal application size and changed the page to a viewport-filling TableLayout layout. Fixed header/footer rows remain fixed while Quick Reports and Explore Reports share the available height, eliminating the scrollbar instead of trying to compensate for it after WinForms AutoScroll calculations.
- Reworked Generate PDF, Generate & Open and Explore Open button rendering so icon + caption are painted as one centred single-line unit. This avoids WinForms ImageBeforeText wrapping/clipping at DPI-scaled sizes and preserves descenders such as the `p` in Open.
- Increased Explore action-button height and footer allowance so the complete caption remains visible.

### Acceptance requirement

Run `Build-BinTracker.bat`, then verify the Reports page at the same display scale that exposed alpha.24.2.4: no page scrollbar, both Generate & Open captions remain on one line, every Explore Open caption is fully visible, and Generate PDF retains the small document icon.


### Reports landing-page viewport and action-icon correction

- Reworked Reports scrolling so a dedicated vertical scroll host owns scrolling while the report content is explicitly fitted to the current viewport width after DPI/layout changes. This prevents the small horizontal scrollbar caused when the vertical scrollbar reduces the WinForms client width.
- Removed fixed minimum card widths that could force the Reports grid wider than the available viewport.
- Restored a proper small document icon beside **Generate PDF** using vector-drawn GDI artwork rather than a font/symbol glyph.
- Replaced the font-dependent diagonal-arrow text on **Generate & Open** and Explore **Open** buttons with a consistent vector-drawn external-link icon.
- Added permanent requirement/audit coverage for the viewport and action-icon behaviour.

### Acceptance requirement

Run `Build-BinTracker.bat`, then verify the Reports landing page at the normal display scale plus 125% and 150% where available: no horizontal scrollbar, Generate PDF shows the document icon, and every Open/Generate & Open button shows a clean external-link icon with no stray symbol text.

## v0.4.0-alpha.24.2.3

### Report container-filter master-data correction

- Fixed Outstanding Containers and Daily Movements container selectors so they are populated from configured Container Types rather than from today's non-zero Outstanding balances.
- Active and inactive configured container types remain selectable for historical reporting. A type with a zero balance today no longer disappears from report filtering.
- Weekly Movements, Movement History and Monthly Summary already used configured Container Types and retain that behaviour.
- Added a permanent common-report requirement and source audit markers to prevent this regression.

### Reports landing-page interaction correction

- Replaced the clipped `Open →` footer text with actual blue `Open` buttons on all Explore Report cards.
- Removed the font-dependent pseudo-PDF glyph that rendered as a broken square on some Windows systems.
- Removed the duplicate host scrolling layer on the Reports page so the Reports view owns scrolling and the stray horizontal scrollbar is not introduced by the host panel.

### Acceptance requirement

Run `Build-BinTracker.bat`, then smoke-test the Reports landing page and confirm every configured Container Type appears in the report container selectors, including inactive types labelled `(inactive)`.

## v0.4.0-alpha.24.2.2

### Audit/documentation reconciliation

- Reconciled `BT-CT-005` with the implemented Containers navigation/permission compromise.
- Removed stale current-state documentation saying Containers remained in Settings or was still pending a navigation decision.
- Strengthened `Audit-BinTracker.ps1` so those regressions fail the release gate.
- Retained the corrected requirements-register ID validation using parsed `$reqRows`.

### Reports landing page redesign

Implemented the approved Reports landing-page mock-up without changing the report workflows themselves.

- **Quick Reports** now presents Market Floor Sheet and Daily Print Pack side-by-side as prominent operational cards.
- **Explore Reports** is a compact 3×2 grid: Outstanding Containers, Daily Movements, Weekly Movements, Movement History, Monthly Summary and Customer Statement.
- The exact approved report-icon artwork from the mock-up is embedded as application assets.
- Cards use the approved rounded white-card treatment, blue action language, compact descriptions and Open → footer actions.
- Reports header now carries the approved subtitle: “Generate operational sheets and explore detailed reports.”
- Existing report date guards and report generation actions are preserved.

### Test requirement

Windows UI acceptance at 100%, 125% and 150% scaling, including normal 1080p display and laptop display.


## Containers navigation compromise

- Promoted **Containers** to a first-class left-navigation destination directly below Customers.
- All signed-in roles can view configured Container Types.
- Administrator permissions remain required to add, rename, reorder, deactivate or reactivate Container Types.
- Removed the duplicate Container Types administration button from Settings.
- Preserved unsaved-change protection when navigating away from Containers.


## Reports DPI/layout correction
- Corrected the Reports landing page so Explore Report descriptions and Open rows are not clipped.
- Corrected Quick Report card sizing so controls remain fully visible.
- Increased the main page header height so the Reports subtitle is fully visible.
- Replaced fragile AutoSize/fixed-Height combinations with explicit TableLayout row sizing for the approved Reports mock-up.


## alpha.24.2.1 audit-gate correction

Corrected the source/package audit gate. `BT-CT-005` was present in the Requirements & Acceptance Register, but the previous audit script checked an undefined `$requirementsText` variable and therefore falsely reported the requirement as missing. The gate now checks the already-parsed `$reqRows` requirement IDs directly.

This build includes the alpha.24.2 Reports DPI/layout correction and the alpha.24.1 Containers navigation/permissions change.
