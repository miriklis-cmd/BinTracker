# BinTracker Current Release Notes

## v0.4.0-alpha.20.0.5

### Responsive report windows

Outstanding Containers no longer opens at a fixed 1320×760 size.

- Initial window size is calculated from the active monitor working area.
- The window uses approximately 90% of available width and 88% of available height, within sensible min/max bounds.
- Laptop displays remain compact.
- Larger desktop monitors provide substantially more result-grid space.
- Filter/action controls reserve enough vertical space to prevent button clipping.
- The result grid fills the remaining client area and resizes with the report window.
- Grid scrollbars remain available when data exceeds the visible area.

### Documentation audit

Roadmap, Technical Debt, Test Checklist, Functional Specification, Testing, README and versioning were reconciled.

### Test requirement

**Full smoke test** because report-window layout changed.
