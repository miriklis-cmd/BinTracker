# BinTracker Current Release Notes

## v0.4.0-alpha.19.1

### Market Floor report corrections
- Same-day import opening adjustments now contribute to B/Fwd and are excluded from physical daily OUT/IN.
- Cash/COD credits remain in the Cash section.
- Only Account-customer credits appear in the separate CREDIT section.
- Reverse side now splits Account customers across two columns and puts Cash/COD in a third column, keeping the report to a front and back page.
- Front-page font size adapts to row load for improved readability and page utilisation.

### Customer statement / movement history
- Adjustment-source movements now display as `Opening adjustment (OUT)` / `Opening adjustment (IN)`.
- They are no longer described as `OUT (Taken)` / `IN (Returned)`.

### UI fixes
- Review no longer says Import is disabled in this alpha.
- Analyse warning now has exactly one triangle icon.
- First-run Administrator footer buttons are aligned with fixed widths/heights.
