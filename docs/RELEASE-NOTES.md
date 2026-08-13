# BinTracker Current Release Notes

## v0.4.0-alpha.19.6

### Bulk classification corrected
Bulk Bin remains a special container. Market Floor now follows the configured `IsSpecialFloorReportContainer` flag without a Bulk exception.

### Dynamic Market Floor front-page layout
The front page now sizes itself from the actual row count for that report date.

The algorithm considers:
- Account owing rows after Blue/Yellow separation;
- Cash/COD rows;
- Account credit rows;
- Special-container rows.

As the busiest column grows, BinTracker progressively reduces:
- font size;
- row padding;
- section spacing;
- content-top spacing.

On light days it automatically uses larger text. On unusually busy days, including days with many Yellow-bin rows, it becomes progressively more compact to preserve a single front A4 page.
