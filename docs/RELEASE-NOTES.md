# BinTracker Current Release Notes

## v0.4.0-alpha.20.0.6

### Outstanding column sizing fix

The previous dynamic Customer Code sizing was wired to `DataBindingComplete`, but Outstanding Containers manually adds grid rows, so that event never fired.

Fixed:

- Code width is recalculated immediately after each report result is populated.
- Code uses a wider 130 px minimum and up to 300 px based on the longest visible customer code.
- Type is now also content-aware, with a 130–220 px range.
- Filtering/rerunning recalculates both widths from the current visible result set.
- Customer remains the flexible fill column, so spare monitor width is still used efficiently.

### Documentation audit

Roadmap/current docs, Technical Debt, Test Checklist, Functional Specification, README and versioning were reconciled.

### Test requirement

**Targeted smoke test** — verify long Code values and Type labels are fully readable after running/filtering Outstanding Containers.
