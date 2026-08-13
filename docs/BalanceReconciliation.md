# Import Balance Reconciliation

Excel is authoritative for the migration/cutover snapshot.

BinTracker must not add the Excel total on top of an existing BinTracker balance.

`Opening adjustment = Excel B/Fwd - current BinTracker balance`

Then:

`Projected balance = current BinTracker balance + opening adjustment + OUT - IN`

The projected balance must equal the Excel target.

Example: current 12, B/Fwd 20, OUT 5, IN 3, Total 22.

Opening adjustment is +8 and projected is 22 — not 34.

No reconciliation writes are enabled in v0.4.0-alpha.13.
