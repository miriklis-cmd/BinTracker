# BinTracker v0.2.0-alpha.9.0 — Market Floor Sheet

## Reports screen
The Reports navigation item now opens a real report hub.

## Market Floor Sheet
Generates a fixed two-page A4 landscape PDF intended for duplex printing.

### Page 1 — floor reference
- Account customers owing are alphabetical and split across two columns.
- Cash/COD customers owing occupy the third column.
- All credit customers (Account and Cash) appear below the Cash column.
- CHEP / LOSCAM / pallet-style container balances appear in the special-container block.
- Normal totals exclude special/pallet container types.

### Page 2 — reverse-side daily worksheet
- Account customers are listed on the left.
- Cash/COD customers are listed on the right.
- Each side has Buyer / Out / In / B/Fwd / Total.
- B/Fwd is calculated from all movements before the selected report date.
- Total = B/Fwd + OUT - IN.
- Negative totals are printed as `x CREDIT`.

The report can be regenerated for any historic date because it is calculated from movement history.
