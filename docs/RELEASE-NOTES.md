# BinTracker Current Release Notes

## v0.4.0-alpha.24.2.4

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
