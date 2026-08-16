# BinTracker Current Release Notes

## v0.4.0-alpha.21.4.1

### Build/test fix

Fixed the integration-test compile failure introduced in alpha.21.4.

Cause:
- `WeeklyMovementsReportSqliteTests` directly called `WeeklyMovementsReportService.StartOfWeek(...)`;
- the concrete service is intentionally `internal`, so the test project could not access it.

Fix:
- the test now independently calculates the expected Monday week start and verifies the public report result;
- application/report behaviour is unchanged.

### Full audit

Testing, Technical Debt, Test Checklist, version references, changelog and release notes were reconciled.

### Test requirement

**Automated build/test gate only.** No runtime UI or business behaviour changed in this patch.
