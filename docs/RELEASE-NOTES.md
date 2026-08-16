# BinTracker Current Release Notes

## v0.4.0-alpha.21.4

### Daily Movements future-date guard

Daily Movements now follows the same actual-history rule as Weekly Movements:

- date picker cannot go later than today;
- service logic defensively clamps future date requests to today;
- future-dated movement rows cannot leak into a Daily Movements report.

### Roadmap: Business Information branding

Added a formal design/roadmap item for:

- business logo;
- optional custom branding/header text;
- shared use across reports, customer statements, emails, reminders and other generated output;
- discussion of storage, sizing, fallbacks, placement and per-output behaviour before implementation.

This is intentionally a design item first rather than a rushed partial implementation.

### Full audit

Roadmap, Business Rules, Functional Specification, Testing, Test Checklist, Technical Debt, README, Known Issues, Versioning, changelog and release notes were reconciled.

### Test requirement

**Targeted smoke test** for Daily Movements date selection. Automated coverage was added for future-date clamping.
