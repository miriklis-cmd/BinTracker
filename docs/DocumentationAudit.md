# Documentation Audit Record

## v0.5.0-alpha.5.6.4.1 BT-RPT-021 audit correction

- [x] Alpha.5.6.4 Windows source audit failed before build on BT-RPT-021.
- [x] Actual Weekly source inspected against the exact audit expression.
- [x] Runtime code confirmed to contain the required Source grouping, row-aware Notes state, tab refresh and post-load refresh.
- [x] Brittle audit expression replaced with explicit tab-change and load-finally assertions without weakening BT-RPT-021.
- [ ] Windows audit/build/test and focused alpha.5.6.4 behavior smoke remain required.

## v0.5.0-alpha.5.6.4 integrated Reports smoke corrections

- [x] Report smoke pass completed across Outstanding Containers, Daily Movements, Weekly Movements, Customer Statement, Monthly Summary and Movement History.
- [x] Outstanding/Daily/Monthly/Movement History integrated layouts accepted for this pass.
- [x] Weekly Source wrapping correction implemented.
- [x] Weekly Notes enablement state unified for reload and tab-change paths.
- [x] Customer Statement search cue polish implemented.
- [x] Reports left-nav reselect returns from detailed report to overview.
- [x] BT-RPT-002 wording reconciled; BT-RPT-020..021 added and hard-gated.
- [ ] Windows source audit/build/test and focused smoke confirmation remain required.

## v0.5.0-alpha.5.6.3 embedded report dead-space correction

- [x] Hard-gate Roadmap, Requirements register and source audit reviewed before code change.
- [x] Operator-highlighted top and bottom dead bands traced to retained standalone root padding and Close-only footer allocation.
- [x] Embedded root padding/margin removed.
- [x] Close-only footer rows collapse; shared action rows retain non-Close actions.
- [x] Duplicate large title rows collapse to zero while explanatory text remains.
- [x] BT-RPT-019 added and permanent source audit strengthened.
- [ ] Windows source audit/build/test and report visual smoke acceptance remain required.

## v0.5.0-alpha.5.6.2 embedded report header cleanup

- [x] Operator screenshot identified duplicate `Outstanding Containers` / `Outstanding Containers — As of Date` headings and wasted vertical space.
- [x] Explanatory sentence retained in the shell subtitle.
- [x] Legacy standalone title/header block suppressed for all newly embedded detailed reports.
- [x] Permanent Option-B source gate strengthened for this presentation contract.
- [ ] Windows build/test/audit and visual smoke acceptance remain required.

## v0.5.0-alpha.5.6.1 BT-HIST audit reconciliation

- [x] Alpha.5.6 Windows source audit correctly exposed a stale Movement History host-pattern assertion.
- [x] Root cause identified: Movement History now uses the shared Option-B `OpenEmbeddedReport(...)` host, while the older gate still required the removed typed-page launch implementation.
- [x] Gate updated to verify shared host invariants without weakening Movement History-specific layout/export protections.
- [ ] Windows source audit/build/test and integrated-report visual smoke acceptance remain required.

## v0.5.0-alpha.5.6 integrated Reports implementation

- [x] Hard-gate review of Roadmap and RequirementsAcceptanceRegister completed before implementation.
- [x] v1 Option B applied to Outstanding Containers, Daily Movements, Weekly Movements, Movement History, Customer Statement and Monthly Summary.
- [x] Reports landing page remains the hub; Market Floor remains inline.
- [x] Shared shell breadcrumb is the report return navigation.
- [x] Existing report business/export/audit behavior preserved; integration changes are hosting/chrome changes.
- [x] BT-RPT-001/018 and ReportsArchitectureTests reconciled.
- [ ] Windows source audit/build/test and per-report visual/interaction smoke acceptance remain required.

## v0.5.0-alpha.5.5.3 report-breadcrumb decision

- [x] Operator accepted the alpha.5.5.2 Movement History compact maximised layout and chose not to pursue Date truncation or narrow-window WinForms redesign now.
- [x] Replaced standalone `← Reports` row with shell-level `Reports › Movement History` breadcrumb.
- [x] Recorded v1 Option B: Reports landing remains the hub; detailed reports integrate into the main workspace with the shared breadcrumb.
- [x] Recorded Option C persistent Reports workspace as post-WinForms / WinUI 3 discussion scope.
- [x] Added BT-RPT-018 and BT-UI-014; BT-HIST gate updated for the new header/navigation structure.
- [ ] Alpha.5.5.3 Windows source audit/build/test and breadcrumb smoke acceptance remain required.

## v0.5.0-alpha.5.5.2 direct-row layout correction

- [x] Alpha.5.5.1 runtime screenshot proved the controls card still consumed excessive vertical space.
- [x] Removed the controls card entirely; Filters / Options / Actions / Summary are direct AutoSize root rows and Grid is the only Percent row.
- [x] BT-HIST audit updated to reject reintroduction of the failed controls-card structure.
- [ ] Alpha.5.5.2 Windows audit/build/test and compact-layout smoke acceptance remain required.

## v0.5.0-alpha.5.5.1 audit-gate correction

- [x] Alpha.5.5 Windows build stopped correctly because the BT-HIST source audit still required the removed alpha.5.4 calculated-height implementation.
- [x] Audit gate updated to enforce alpha.5.5 direct AutoSize/GrowAndShrink content-height layout instead of `controlsRowStyle` / `ResizeControlsCard`.
- [ ] Alpha.5.5.1 Windows source audit/build/test and compact-layout smoke acceptance remain required.

## v0.5.0-alpha.5.5 Movement History compact-height correction

- [x] Alpha.5.4 manual Windows smoke confirmed the action buttons are fully visible.
- [x] Alpha.5.4 notes-enabled Movement History PDF confirmed Status and optional Notes export together.
- [x] Alpha.5.4 manual UI smoke exposed an excessive blank band between the actions and summary/grid.
- [x] Alpha.5.5 removes the separately reserved controls-row height and uses direct AutoSize content height.
- [ ] Alpha.5.5 Windows source audit/build/test and compact-layout smoke acceptance remain required.

## v0.5.0-alpha.5.4 Movement History action-row correction

- [x] Alpha.5.3 Windows source audit passed with 204 permanent requirement IDs / 25 Markdown files.
- [x] Alpha.5.3 Windows build passed with zero warnings/errors and 242/242 automated tests.
- [x] Alpha.5.3 PDF smoke confirmed the new Status column; Notes was intentionally absent because Include notes in exports was unticked.
- [x] Alpha.5.3 manual UI smoke still showed the entire action row vertically clipped.
- [x] Alpha.5.4 replaces the nested auto-size measurement path with a direct structural TableLayoutPanel controls card; no broader report redesign is included.
- [ ] Alpha.5.4 Windows audit/build/test and manual action-row/DPI acceptance remain required.
- [ ] Re-test Movement History PDF with Include notes in exports ticked.

## v0.5.0-alpha.5.3 Movement History Windows-smoke correction

- [x] Preserved the integrated Movement History design after operator review.
- [x] Added explicit Back to Reports navigation instead of relying only on left navigation.
- [x] Corrected clipped action-row layout and rebalanced structured/flexible columns from the alpha.5.1 Windows screenshot.
- [x] Preserved BT-HIST-002..005, BT-ARCH-008..015 and security-register identities.
- [ ] Alpha.5.2 Windows source audit, zero-warning build, full tests, package audit and DPI/UI acceptance remain required.

## v0.5.0-alpha.5.1 warning-gate correction

- [x] Recorded alpha.5 as built with 242/242 automated tests passing but blocked from release by two `CS8602` warnings.
- [x] Issued a corrective alpha.5.1 candidate rather than overwriting the delivered alpha.5 artifact.
- [x] Preserved BT-HIST-002..005, BT-ARCH-008..015 and all security/requirements register identities.
- [x] Alpha.5.1 Windows source audit/build/test gate passed with zero warnings, zero errors and 242/242 automated tests; manual UI smoke then found the action-row/column-allocation defects corrected in alpha.5.2.

## v0.5.0-alpha.5 Movement History integration

- [x] Recorded v0.5.0-alpha.4 manual acceptance as 8/8 smoke tests passed before follow-up source changes.
- [x] Reconciled Movement History's intentional full-size main-workspace exception across Roadmap, Requirements, Business Rules, Functional Specification, Testing, Technical Debt, Release Notes and active checklist.
- [x] Added permanent BT-HIST-002..005 requirements plus source-audit protection for integrated hosting, responsive columns/minimums, badges/tooltips and customer-code PDF/CSV filename rules.
- [x] Preserved BT-ARCH-008..015 architecture requirements and the Security Hardening register without renumbering or dropping IDs.
- [ ] Windows restore/build/test/package audit and manual maximized-width/DPI acceptance remain outstanding until executed on a suitable Windows/.NET host.

## v0.5.0-alpha.4 selective branch comparison

- Inspected the actual attached alternate alpha.3 source rather than relying on its summary.
- Reconciled the selective concurrency/portability merge against the controlled Markdown set, requirement register and BT-ARCH-008..015 hard gate.
- Preserved Work's reversal Status/Source UX and packaging protections; documented schema V14, content transport, host composition, payload identity and PostgreSQL verification limits.
- Promoted the merged candidate to alpha.4 because source, schema and requirements changed.

Audit date: 16 August 2026

Baseline audited: v0.4.0-alpha.21.5.1

Resulting documentation baseline: v0.4.0-alpha.21.5.2

All Markdown files in the package were reviewed. Historical changelog entries are preserved as historical truth; current-state documents were corrected where stale or contradictory.

## Files reviewed

- [x] `KNOWN-ISSUES.md`
- [x] `README.md`
- [x] `TECH-DEBT.md`
- [x] `TEST-CHECKLIST.md`
- [x] `docs/AuditCoverage.md`
- [x] `docs/BalanceReconciliation.md`
- [x] `docs/BusinessRules.md`
- [x] `docs/CHANGELOG.md`
- [x] `docs/Database.md`
- [x] `docs/DevelopmentWorkflow.md`
- [x] `docs/FunctionalSpecification.md`
- [x] `docs/ImportWizard.md`
- [x] `docs/LegacyContainerRules.md`
- [x] `docs/MasterData.md`
- [x] `docs/RELEASE-NOTES.md`
- [x] `docs/ReimportSafety.md`
- [x] `docs/Roadmap.md`
- [x] `docs/RoadmapCoverageMatrix.md`
- [x] `docs/Testing.md`
- [x] `docs/Versioning.md`

## Material corrections

- Weekly Movements status/behaviour reconciled across current-state docs.
- One `Include notes in exports` control is the only current Weekly Notes semantic documented.
- Current version references reconciled with `Directory.Build.props`.
- Roadmap execution order made authoritative and duplicate backup planning removed.
- Existing textual Default Report Header distinguished from future logo/shared branding.
- Import Replace/Correct/provenance/history status explicitly recorded.
- Roadmap coverage matrix retained as the major-workstream completeness guard.


## Ongoing requirement

This file is not a one-off audit artifact. Every packaged build must append or refresh an audit record confirming that all Markdown/current-state files were reviewed for that candidate. The audit is part of the definition of done.


## Candidate: v0.4.0-alpha.21.5.2

- [x] 21 Markdown files enumerated/reviewed.
- [x] Mandatory audit-gate documentation reconciled.
- [x] Roadmap Coverage Matrix updated.
- [x] Version references reconciled.
- [x] Release Notes and Changelog updated.
- [x] Runtime behaviour unchanged.


## Candidate: v0.4.0-alpha.22

- [x] 21 Markdown files enumerated/reviewed.
- [x] Movement History implementation/status reconciled across README, Known Issues, Roadmap, Coverage Matrix, specification, business rules and testing.
- [x] Current version references reconciled with `Directory.Build.props`.
- [x] Changelog and Release Notes updated.
- [x] Test requirement classified as **full smoke test**.


## Candidate: v0.4.0-alpha.22.1

- [x] 21 Markdown files enumerated/reviewed.
- [x] Live-filter report interaction standard reconciled across current-state documentation.
- [x] Roadmap Coverage Matrix reviewed; no agreed workstream removed.
- [x] Version references reconciled with `Directory.Build.props`.
- [x] Changelog and Release Notes updated.
- [x] Test requirement classified as **full smoke test**.


## Candidate: v0.4.0-alpha.22.2

- [x] 21 Markdown files enumerated/reviewed.
- [x] Weekly wrapped-layout fix and Customer Enter cue reconciled across UI standard documentation.
- [x] BinTracker product icon/logo documented separately from business branding.
- [x] Roadmap Coverage Matrix reviewed; no agreed workstream removed.
- [x] Version references reconciled with `Directory.Build.props`.
- [x] Changelog and Release Notes updated.
- [x] Test requirement classified as **full smoke test**.


## Candidate: v0.4.0-alpha.22.3.1

- [x] 21 Markdown files enumerated/reviewed.
- [x] Weekly wrapped-layout defect and structural fix documented.
- [x] Roadmap Coverage Matrix reviewed; no agreed workstream removed.
- [x] Version references reconciled.
- [x] Changelog / Release Notes / Testing / Test Checklist / Tech Debt reconciled.
- [x] Test requirement classified as **full smoke test**.


## Candidate: v0.4.0-alpha.22.3.2

- [x] 21 Markdown files enumerated/reviewed.
- [x] Application icon/sidebar branding behaviour reconciled across specs/business rules/testing/roadmap/tech debt.
- [x] Roadmap Coverage Matrix reviewed; no agreed workstream removed.
- [x] Version references reconciled.
- [x] Changelog and Release Notes updated.
- [x] Test requirement classified as **full smoke test**.


## Candidate: v0.4.0-alpha.22.3.3

- [x] Source tree and Markdown/current-state documentation reviewed.
- [x] Weekly Movements reclaimed-grid-space change documented.
- [x] Sidebar circular transparent product-logo treatment documented.
- [x] Startup splash implementation documented.
- [x] Roadmap/current workstreams reviewed; no agreed workstream removed.
- [x] Version references reconciled with `Directory.Build.props`.
- [x] Changelog and Release Notes updated.
- [x] Test requirement classified as **full smoke test**.


## Candidate: v0.4.0-alpha.22.3.4

- [x] 21 Markdown files enumerated/reviewed.
- [x] Sidebar branding regression documented.
- [x] Roadmap Coverage Matrix reviewed; no agreed workstream removed.
- [x] Version references reconciled.
- [x] Changelog / Release Notes / Testing / Test Checklist / Tech Debt reconciled.
- [x] Test requirement classified as **targeted UI smoke test**.


## Candidate: v0.4.0-alpha.22.5

- [x] 21 Markdown files enumerated/reviewed.
- [x] Customer Statement generate/save/open workflow documented.
- [x] Statement future-date restriction documented.
- [x] Roadmap/current-state workstreams checked: Movement Correction, SMS, Email, Dashboard, WinUI 3, branding.
- [x] Changelog / Release Notes / Testing / Test Checklist reconciled.
- [x] Test requirement classified as **targeted functional/UI test**.


## Candidate: v0.4.0-alpha.22.6

- [x] 21 Markdown files enumerated/reviewed.
- [x] Customer Statement Reports entry point and shared workflow reconciled.
- [x] Roadmap/coverage updated to mark Customer Statement workflow complete.
- [x] Major workstreams retained: Movement Correction, SMS, Email, Dashboard, WinUI 3, branding, Monthly Summary, Daily Print Pack.
- [x] Changelog / Release Notes / Testing / Test Checklist / Tech Debt reconciled.
- [x] Test requirement classified as **full smoke test**.


## Candidate: v0.4.0-alpha.22.6.1

- [x] 21 Markdown files enumerated/reviewed.
- [x] Compile defect and owner-interface fix documented.
- [x] Roadmap Coverage Matrix reviewed; no agreed workstream removed.
- [x] Version references reconciled.
- [x] Changelog / Release Notes / Testing / Test Checklist / Tech Debt reconciled.
- [x] Test requirement classified as **automated build gate + existing Customer Statement smoke test**.


## Candidate: v0.4.0-alpha.23

- [x] 21 Markdown files enumerated/reviewed.
- [x] Monthly Summary implementation/status reconciled across README, Known Issues, Roadmap, Coverage Matrix, Functional Specification, Business Rules and Testing.
- [x] PostgreSQL/provider-neutral report boundary documented.
- [x] Agreed workstreams retained: Movement Correction, SMS, Email, Dashboard, WinUI 3, branding, Daily Print Pack, PostgreSQL.
- [x] Changelog and Release Notes updated.
- [x] Test requirement classified as **full smoke test**.


## Candidate: v0.4.0-alpha.23.1
- [x] 21 Markdown files enumerated/reviewed.
- [x] Both Windows failures diagnosed as new integration-test fixture/setup defects.
- [x] Production date behaviour retained.
- [x] Changelog / Release Notes / Test Checklist reconciled.
- [x] Test requirement remains full smoke test after automated suite passes.


## Candidate: v0.4.0-alpha.23.2
- [x] 21 Markdown files enumerated/reviewed.
- [x] CSV audit gap audited across all current CSV-capable reports.
- [x] Export auditing rules added to Functional Specification and Business Rules.
- [x] Monthly Summary year-picker regression included in acceptance checks.
- [x] Roadmap workstreams retained.
- [x] Test requirement classified as full report-export smoke test.


## Candidate: v0.4.0-alpha.23.4

- [x] 21 Markdown files enumerated/reviewed.
- [x] alpha.23.3 SDK-pinning mistake documented and corrected.
- [x] BAT false-success failure path documented and corrected.
- [x] Useful MSBuild worker-resilience changes retained.
- [x] Agreed roadmap workstreams retained: Movement Correction, SMS, Email, Dashboard, WinUI 3, branding, Daily Print Pack, PostgreSQL.
- [x] Changelog / Release Notes / Development Workflow / Testing / Test Checklist / Tech Debt reconciled.


## Candidate: v0.4.0-alpha.23.5.1

Full audit performed after alpha.23.5 packaging drift was reported.

Findings corrected:
- [x] alpha.23.5 ZIP/root/Version/InformationalVersion identity was checked directly.
- [x] `docs/RELEASE-NOTES.md` was stale at alpha.23.4.1 and replaced.
- [x] `docs/Roadmap.md` planning baseline was stale at alpha.22.2 and reconciled.
- [x] `KNOWN-ISSUES.md` current release was stale at alpha.22.2 and reconciled.
- [x] Historical-requirements section incorrectly still marked Movement History, Monthly Summary and Daily Print Pack pending; reconciled.
- [x] Audit Coverage omitted Movement History, Monthly Summary, Daily Print Pack and CSV exports; reconciled.
- [x] Development Workflow described obsolete BAT subroutine handling; reconciled to direct command guards.
- [x] Inline Market Floor / Daily Print Pack selectors lacked the future-date UI guard used by breakout reports; fixed.
- [x] 21 Markdown files enumerated and reviewed as current-state/historical documents.
- [x] Major pre-v1/post-v1 workstreams retained: Batch Entry cleanup, Movement Correction, Branding, Email/SMS, Dashboard design gate, Backup/Restore, PostgreSQL readiness, installer/acceptance, post-v1 WinUI 3.
- [x] Mechanical source audit added to stop current-version-document drift before build.
- [x] Package identity gate documented as mandatory.

Test classification: **full report smoke/print test** after Windows automated build/test gate.


## Historical audit-log repair — alpha.23.5.2

The reconciliation found candidate headings that had been corrupted by prior broad version-string replacement. Headings were repaired only where the section content and archived build chronology identified the candidate unambiguously: sidebar branding → alpha.22.3.4; Customer Statement generate/open → alpha.22.5; compile owner fix → alpha.22.6.1; CSV audit pass → alpha.23.2; SDK/build correction → alpha.23.4. The Customer Statement Reports-entry section remains alpha.22.6. No unverified historical claim was invented.

## Candidate: v0.4.0-alpha.24.2.1

- [x] Reconciliation inspected current source/docs plus 166 archived BinTracker ZIPs and all BinTracker history surfaced in the current conversation context.
- [x] Personal-context history query returned no additional BinTracker entries; raw complete chat transcript access was not claimed.
- [x] Permanent Requirements & Acceptance Register created with stable IDs/scope/status/provenance.
- [x] Active Test Checklist rebuilt; historical alpha-specific blocks removed after permanent behaviors were migrated.
- [x] Stale SDK, importer and tech-debt contradictions corrected.
- [x] Historical DocumentationAudit headings repaired where archived chronology/content made identity unambiguous.
- [x] Audit gate strengthened and packaging identity verifier added.
- [x] Lost detailed post-v1 import requirements restored.
- [x] Current package identity mechanically verified before delivery.


## Candidate: v0.4.0-alpha.24.2.1
- [x] Containers navigation decision reconciled into the permanent Requirements & Acceptance Register.
- [x] Permission compromise implemented: view for all roles; mutations Administrator-only.
- [x] Settings duplication removed.
- [x] Unsaved-change navigation protection preserved.
- [x] 176 unique permanent requirement IDs verified.
- [x] Package/version metadata gate required before handoff.


## Candidate: v0.4.0-alpha.24.2.1
- [x] False BT-CT-005 audit failure root cause identified as undefined `$requirementsText`.
- [x] Requirement presence checks now use parsed `$reqRows.Id`.
- [x] BT-UI-013 is also explicitly guarded by the mechanical gate.
- [x] Version/package metadata reconciled.
