# BinTracker v0.4.0-alpha.10.2 — Map State Fix

- Fixed worksheet Classification cells appearing blank after navigating Review → Back.
- Mapping selections are now stored as wizard state rather than relying on transient DataGridView state.
- Map selections survive Analyse / Map / Review navigation while the wizard remains open.
- Classification combo now uses explicit enum items and `ValueType` instead of an enum-array DataSource.
- Source-customer preview continues to refresh immediately after classification changes.
- No database import logic changed.
- Reviewed `KNOWN-ISSUES.md` and `TECH-DEBT.md`.
