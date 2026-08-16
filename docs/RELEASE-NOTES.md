# BinTracker Current Release Notes

## v0.4.0-alpha.22.1

### Live interactive report filtering

Removed the separate **Run Report** action from interactive report windows.

Outstanding Containers, Daily Movements, Weekly Movements and Movement History now use the same interaction standard:

- date changes refresh automatically;
- dropdown filters refresh automatically;
- result-affecting checkboxes refresh automatically;
- Customer text waits for Enter rather than querying on every keystroke;
- date/range shortcut buttons continue to refresh immediately.

Movement History's **This Month** action was widened so its full label is visible.

### Mandatory full audit

All Markdown files were enumerated/reviewed and current-state documentation, Roadmap Coverage, version references, specifications, business rules, testing, Known Issues, Tech Debt, changelog, release notes and Documentation Audit were reconciled.

### Test requirement

**Full smoke test** because multiple report UIs and interaction behaviour changed.
