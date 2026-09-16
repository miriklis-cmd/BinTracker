# BinTracker Roadmap Coverage Matrix

Audited: 15 September 2026

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
| Logical movement lineage | Required before v1 | Frozen roots/lines/full generations/restoration/projection/migration are normal schema17 runtime authority after BT-20-P2. Startup, native entry/mutation writers, typed Single receipts, validated projection consumers and developer replacement share one Data-owned readiness boundary. Full audit/history detail, Restore UI, broader activated-system gates, retained-DB rehearsal, Windows/operator acceptance, packaging and full acceptance remain pending |
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

Task20C froze R1–R4 and R5–R7 constraints; Task20D/E/F/P1/FIX1 supplied the reviewed activation components. BT-20-P2 connects them to normal WinForms startup and composition, and BT-20-P3 completes native Audit/History detail without altering correction/reversal semantics. The frozen later order is Restore/RemainReversed UI, broader activated-system gates, retained-database rehearsal, Windows/operator acceptance, protected layer audit, then Security Hardening/subsequent roadmap work.

Status coverage is promoted only after criterion-level reconciliation from authoritative requirement through actual enforcement and evidence to the claimed status. Agreement among this matrix, Roadmap, checklist, Testing or continuation is not independent proof and cannot redefine Architecture/Business Rules. The default implementation unit is the largest coherent safe roadmap pass; split only at a genuine semantic, data/migration, transaction/authority, release-risk, manual-acceptance or independent-verification boundary.

- Business-logic change: targeted smoke test.
- UI change: full smoke test.
- Milestone closure: automated tests + relevant smoke tests + documentation/audit reconciliation.


## Permanent requirements ledger

`docs/RequirementsAcceptanceRegister.md` is the permanent ID/status ledger used to prevent roadmap shortening from dropping agreed work. `docs/ReconciliationReport.md` records the 23.5.2 historical reconciliation and provenance limits.
