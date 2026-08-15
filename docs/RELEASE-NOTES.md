# BinTracker Current Release Notes

## v0.4.0-alpha.20.0.7.2

### Outstanding action-row layout fix

The Outstanding report had enough overall window space, but filters and actions shared one wrapping FlowLayoutPanel. At production DPI the action buttons wrapped into a second line that was partially hidden.

Fixed structurally:

- first row contains date/customer/container filters and credit/inactive options;
- second dedicated row contains Run Report, Today, Generate PDF, Generate & Open and Export CSV;
- action buttons have explicit DPI-safe widths/heights;
- the controls card reserves enough vertical space for both rows.

This avoids relying on further window enlargement.

### Dashboard design gate recorded

Before the future Dashboard milestone begins, implementation must stop for a joint design/evaluation discussion covering charts, forecasting hooks, drill-through, exception/attention ideas, trends, comparison views and laptop/large-monitor layouts.

### Full audit

Roadmap, Technical Debt, Test Checklist, Functional Specification, Testing, README, Known Issues, Versioning, changelog and release notes were reconciled.

### Test requirement

**Full smoke test** because report-window UI layout changed.
