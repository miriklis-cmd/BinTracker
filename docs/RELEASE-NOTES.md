## v0.4.0-alpha.24.2.25

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

# BinTracker Current Release Notes

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
