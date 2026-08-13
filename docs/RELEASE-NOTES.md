# BinTracker Current Release Notes

## v0.4.0-alpha.17

### Import Review
- Added **Confirm existing matches...**.
- Automatic existing-customer matches now require explicit confirmation.
- Operators can accept the proposed match or override it to another active BinTracker customer.
- Unconfirmed existing matches block reconciliation / Import readiness.
- Existing-match decisions persist across wizard navigation.

### UI fixes
- Developer Database Tools messages now render real line breaks rather than literal `\n`.
- Confirm New Customers bulk-action buttons have been widened.

### Remaining Import blockers
- ImportRun/source provenance.
- Exact re-import protection.
- Transactional Step 4 execution and rollback.
