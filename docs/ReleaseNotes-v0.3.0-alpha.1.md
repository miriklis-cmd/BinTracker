# BinTracker v0.3.0-alpha.1 — Container Type Master Data

## Container Types
- Added Settings → Container Types for administrators.
- Add, rename, reorder, activate/deactivate container types.
- Short Code is user-editable; System Code is stable/immutable after creation.
- Added explicit Special Floor Report Container flag.
- Added Dashboard Colour metadata (reserved for dashboard/chart styling).
- Added Description and Notes.
- Added usage statistics: movement count, customers with non-zero balance, first/last use.
- Existing container IDs and movement history are preserved by schema migration.

## Reports
- Market Floor Sheet now uses the explicit special-container flag instead of guessing from CHEP/LOSCAM/PALLET text.

## Audit
- Container type create/update/activate/deactivate actions are audited.
