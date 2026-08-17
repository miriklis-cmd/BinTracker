# BinTracker Roadmap Coverage Matrix

Audited: 17 August 2026

| Workstream | v1? | Roadmap status / gate |
|---|---|---|
| Excel Import safety/provenance | Yes | Core complete; transactional failure detail + cosmetic validation remain |
| Reports | Yes | Current v0.4 milestone; configured Container Type filters reconciled across detailed reports; Customer Statement + Monthly Summary + Daily Print Pack implemented; final consistency/print acceptance remains |
| Batch Entry acceptance/recovery | Yes | Esc, field reset/focus, crash/power-loss recovery remain |
| Movement Correction/Reversal | Yes | Explicit milestone before branding/communications/dashboard |
| Business Information & Branding | Yes | Textual Default Report Header exists; logo + shared report/email branding remain |
| Email/SMS Customer Communications | Yes | Google Workspace + Texto direction; reminder/send/history/audit workflow |
| Dashboard | Yes | Design discussion mandatory before coding |
| Forecasting/ML | Hook/design now; modelling later | Discuss hooks during Dashboard; do not fake prediction without useful history |
| Customer operational analytics | Yes | Sorting, lifetime OUT/IN, statement workflow |
| Backup/Restore | Yes | Manual + scheduled automatic + retention/recovery drill |
| Security/Audit hardening | Yes | Authorization, audit-coverage matrix, secrets/logging |
| PostgreSQL/Multi-computer | Readiness before v1 | Preserve Services + IDbContextFactory; central deployment follows readiness decision |
| Installer/Upgrade | Yes | Production package and safe upgrade path |
| Full per-build audit discipline | Always | Mandatory gate on every packaged build; includes all Markdown/current-state/version/roadmap reconciliation |
| BinTracker product branding | v1 | Supplied product icon/logo used by Windows shell and restrained in-app branding; separate from business branding |
| WinUI 3 Windows UI v2 | No — post-v1 | Evaluate/migrate after v1 publication |
| Customer portal | Post-v1 | Explicit candidate |
| Barcode scanning | Post-v1 | Explicit candidate |
| Multiple depots | Post-v1 | Explicit candidate |

## Development acceptance rules

- Business-logic change: targeted smoke test.
- UI change: full smoke test.
- Milestone closure: automated tests + relevant smoke tests + documentation/audit reconciliation.


## Permanent requirements ledger

`docs/RequirementsAcceptanceRegister.md` is the permanent ID/status ledger used to prevent roadmap shortening from dropping agreed work. `docs/ReconciliationReport.md` records the 23.5.2 historical reconciliation and provenance limits.
