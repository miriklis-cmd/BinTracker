# BinTracker v0.4.0-alpha.6 — Centralised Versioning

- Added a single release-version source in `Directory.Build.props`.
- Removed the hard-coded application version from `MainForm.cs`.
- Added `AppVersion` to read assembly informational version metadata.
- `Build-BinTracker.bat` now displays the version before restore/build/test.
- Build success and failure banners also show the version.
- Build script now changes to its own repository directory before running commands.
- Added versioning documentation and regression checklist.
