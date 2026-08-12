# BinTracker v0.4.0-alpha.2 — Excel Import Build Fix

- Fixed ClosedXML cell coordinate access for version 0.105.0.
- Replaced unsupported `IXLCell.RowNumber()` / `ColumnNumber()` calls with `cell.Address.RowNumber` / `ColumnNumber`.
- Removed nullable warning for imported source-cell address.
- No import behaviour or database-write behaviour changed.
