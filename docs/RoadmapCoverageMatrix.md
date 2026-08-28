# BinTracker Roadmap Coverage Matrix

Audited: 27 August 2026

| Workstream | v1? | Roadmap status / gate |
|---|---|---|
| Excel Import safety/provenance | Yes | Core complete; transactional failure detail + cosmetic validation remain |
| Reports | Yes | v0.4 milestone completed/acceptance-cleaned before the v0.5 correction/reversal milestone; Movement History is now an integrated responsive main-workspace page with authoritative persisted Movement ID, reversal badges and customer-code export naming; configured Container Type filters remain reconciled; Daily Print Pack still awaits real preview/print acceptance |
| Batch Entry acceptance/recovery | Yes | post-add reset and recovery choice are operator-confirmed; edit/remove/Enter/Esc-navigation cleanup is implemented and pending focused smoke acceptance |
| Movement Correction/Reversal | Yes | Correction semantics and Administrator review workflow/infobar/exact lineage detail implemented; full Windows/DPI acceptance pending before milestone closure |
| Security, Data Integrity & Code Quality Hardening | Yes — HARD GATE | Dedicated post-correction/pre-branding workstream; 50 external audit findings permanently tracked in SecurityHardeningRegister; per-build completeness/order gate and v1.0 unresolved-finding release block |
| Business Information & Branding | Yes | Textual Default Report Header exists; logo + shared report/email branding remain |
| Email/SMS Customer Communications | Yes | Google Workspace + Texto direction; reminder/send/history/audit workflow |
| Dashboard | Yes | Design discussion mandatory before coding |
| Forecasting/ML | Hook/design now; modelling later | Discuss hooks during Dashboard; do not fake prediction without useful history |
| Customer operational analytics | Yes | Sorting, lifetime OUT/IN, statement workflow |
| Backup/Restore | Yes | Manual + scheduled automatic + retention/recovery drill |
| Security/Audit hardening | Yes | Authorization, audit-coverage matrix, secrets/logging |
| Logical movement lineage | Required before v1 | Frozen roots/lines/full generations/restoration/projection/migration; implement and accept before later major work |
| Whole-codebase layer audit | Protected pre-v1 gate | After lineage acceptance, remove authoritative WinForms business/persistence logic before subsequent major milestones |
| PostgreSQL/API/Multi-computer | Post-v1 implementation | v1 preserves client/provider-neutral services, concurrency and idempotency; central host/provider/client delivery is post-v1 |
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
