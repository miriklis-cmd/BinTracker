# BinTracker v0.2.0-alpha.7.2.6 — Customer Layout & Logout

## Customer screen
- Removed the Customer details wrapper panel entirely.
- The details table now sizes only to its actual controls, eliminating the phantom blank row below the buttons.
- Current Position and Recent Movement History begin immediately below Customer details.
- Remaining lower space is split 38% Current Position / 62% Recent Movement History.
- Recent movement Date widened to 100 px.
- Direction widened to 120 px so `IN (Returned)` / `OUT (Taken)` are readable.
- Entered By widened to 110 px for longer usernames such as `Lawrence`.
- Automatic DataGridView cell tooltips are disabled on Customer grids.

## Logout
- Added Logout to the top-right signed-in area.
- Logout asks for confirmation.
- Explicit Logout returns to the Login screen without exiting BinTracker.
- An in-memory unsaved Batch Entry draft is retained across logout/login during the same application run.
- Closing the main window normally still exits BinTracker.
