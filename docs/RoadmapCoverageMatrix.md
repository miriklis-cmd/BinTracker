# BinTracker Roadmap Coverage Matrix

Audited: 16 October 2026

| Workstream | v1? | Roadmap status / gate |
|---|---|---|
| Excel Import safety/provenance | Yes | Core complete; transactional failure detail + cosmetic validation remain |
| Reports | Yes | v0.4 milestone completed/acceptance-cleaned before the v0.5 correction/reversal milestone; Movement History is now an integrated responsive main-workspace page with authoritative persisted Movement ID, reversal badges and customer-code export naming; configured Container Type filters remain reconciled; bounded Item-7 real preview/print and the corrected Item-8 display/DPI matrix passed |
| Movement Correction/Reversal | Yes | Correction semantics and Administrator review workflow/infobar/exact lineage detail implemented; bounded Task20 Windows/DPI acceptance complete; protected layer-delineation Stage A and P1 accepted after independent review and an exact-candidate 841/841 operator BAT |
| Security, Data Integrity & Code Quality Hardening | Yes — HARD GATE | All 50 findings remain permanently tracked. Alpha.14 Batch A fixes BT-SH-008/017/018/019/022/028/032 and closes BT-SH-040's authentication scope; alpha.15 Batch B fixes BT-SH-006/030 with transactional state/audit consistency. Customer/container operational identifiers remain with Batch C. Current totals are 16 FIXED, 20 CONFIRMED-V1, 1 REVIEW-V1, 11 POST-V1 and 2 NOT-APPLICABLE. The 21 unresolved v1 blockers remain across six batches; Batch C is next. The per-build completeness/order gate and v1.0 unresolved-finding block remain. |
| Email/SMS Customer Communications | Yes — first post-hardening feature | Google Workspace + Texto direction; manual Customer send, bulk/automatic reminders, preferences/opt-out, templates, delivery history, retries/duplicate protection, audit, outstanding snapshot and statement attachment/link decision |
| User-scoped unsaved Batch Entry ownership | Yes — second post-hardening feature | BT-BATCH-012 is a v1 blocker: stable persisted user-ID ownership; owner-only Continue/Save/Discard; Administrator does not bypass; invalid ownership fails closed below WinForms. Existing owner recovery and separate commit-role authorization remain |
| Batch Entry acceptance/recovery | Yes | Existing post-add reset and owner recovery choices are operator-confirmed; edit/remove/Enter/Esc-navigation cleanup is implemented and pending focused smoke acceptance. Current persisted recovery remains cross-user until BT-BATCH-012 is implemented |
| Deterministic configurable Container Type ordering | Yes — third post-hardening feature | Define duplicate-order handling and reorder UX without hard-coded container priorities |
| Business Information & Branding | Yes — fourth post-hardening feature | Textual Default Report Header exists; logo + shared report/email branding remain |
| Dashboard | Yes | Design discussion mandatory before coding |
| Forecasting/ML | Hook/design now; modelling later | Discuss hooks during Dashboard; do not fake prediction without useful history |
| Customer operational analytics | Yes | Sorting, lifetime OUT/IN, statement workflow |
| Backup/Restore | Yes | Manual + scheduled automatic + retention/recovery drill |
| Security/Audit hardening | Yes | Authorization, audit-coverage matrix, secrets/logging |
| Logical movement lineage | Required before v1 | Frozen roots/lines/full generations/restoration/projection/migration are normal schema17 runtime authority after BT-20-P2. BT-20-P3 supplies full native audit/history detail, BT-20-P4 supplies the separately sequenced Restore/RemainReversed UI, FIX2–FIX5 close startup/understanding/anchor/current-state defects, FIX6 reacquires the selected authoritative physical anchor after Restore, and FIX7 synchronizes Movement History date/report state plus immediate post-Restore and successful-correction history visibility. Alpha.9.6 passed a fresh exact-copy schema16-to17 rehearsal, canonical 811/811 gate, bounded FIX7-A/B/C/FIX6 acceptance, successful original-anchor correction/refresh and the item-2 mixed/ordering/repeated/later-action sequence through generation 7; the exact Batch #31 output-anchor/multi-window, DPI/report/detail/review remainder, packaging and full acceptance remain pending |
| Whole-codebase layer audit | Protected pre-v1 gate | CLOSED — Stage A and alpha.10 P1 completed accepted F01–F04 with credential-free DTOs, Services authorization/audit authority and Services-supplied report totals; independent review and exact-candidate canonical BAT passed, and accepted commit `cd53f9ca19de1a10ea9ec375b63e294fdddfad23` unblocks Security Hardening |
| PostgreSQL/API/Multi-computer | Post-v1 implementation | v1 preserves client/provider-neutral services, concurrency and idempotency; central host/provider/client delivery is post-v1 |
| Installer/Upgrade | Yes | Production package and safe upgrade path |
| Full per-build audit discipline | Always | Mandatory gate on every packaged build; includes all Markdown/current-state/version/roadmap reconciliation |
| BinTracker product branding | v1 | Supplied product icon/logo used by Windows shell and restrained in-app branding; separate from business branding |
| WinUI 3 Windows UI v2 | No — post-v1 | Evaluate/migrate after v1 publication |
| Customer portal | Post-v1 | Explicit candidate |
| Barcode scanning | Post-v1 | Explicit candidate |
| Multiple depots | Post-v1 | Explicit candidate |
| Licensing/activation | Post-v1 | Permanently tracked as BT-PV1-009; not a v1 execution milestone |

After Security Hardening closes, the authoritative v1 feature order is: Communications; BT-BATCH-012 user-scoped draft ownership; Container Type ordering; Business Information & Branding; Dashboard; customer operational analytics/polish; Production Backup/Restore/Recovery. Final security/audit/reliability review, installer/upgrade/deployment, full v1 acceptance and v1.0 release then follow. Post-v1 API/central-provider implementation remains post-v1.

## Development acceptance rules

Task20C froze R1–R4 and R5–R7 constraints; Task20D/E/F/P1/FIX1 supplied the reviewed activation components. BT-20-P2 connects them to normal WinForms startup and composition, BT-20-P3 completes native Audit/History detail, and BT-20-P4 implements the separately sequenced Restore/RemainReversed UI without weakening the direct whole-root correction block. The bounded Task20 matrix and protected layer audit are complete. Security Hardening retains BT-SH-001..003 evidence and now follows the eight BT-SH-004-P1 implementation batches rather than obsolete audit order; the register controls exact membership and disposition.

Status coverage is promoted only after criterion-level reconciliation from authoritative requirement through actual enforcement and evidence to the claimed status. Agreement among this matrix, Roadmap, checklist, Testing or continuation is not independent proof and cannot redefine Architecture/Business Rules. The default implementation unit is the largest coherent safe roadmap pass; split only at a genuine semantic, data/migration, transaction/authority, release-risk, manual-acceptance or independent-verification boundary.

- Business-logic change: targeted smoke test.
- UI change: full smoke test.
- Milestone closure: automated tests + relevant smoke tests + documentation/audit reconciliation.


## Permanent requirements ledger

`docs/RequirementsAcceptanceRegister.md` is the permanent ID/status ledger used to prevent roadmap shortening from dropping agreed work. `docs/ReconciliationReport.md` records the 23.5.2 historical reconciliation and provenance limits.
