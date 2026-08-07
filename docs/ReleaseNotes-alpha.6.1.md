# BinTracker v0.2.0-alpha.6.1 — Test Harness Fix

## Fixed

- Added the missing `using Xunit;` directives to unit and integration test source files.
- Fixed `Build-BinTracker.ps1` so it stops immediately when restore, build, or tests fail.
- Added `Build-BinTracker.bat` as the recommended Windows build/test launcher.
- Removed the two EF1002 warnings from schema-upgrade code by parameterising lookups and restricting DDL to explicit allow-listed statements.
- Removed duplicate high-DPI manifest configuration; BinTracker continues to use `Application.SetHighDpiMode(PerMonitorV2)`.

## Expected result

Running `Build-BinTracker.bat` should finish with:

`Build and tests succeeded.`

That message is now only printed after all three stages actually succeed.
