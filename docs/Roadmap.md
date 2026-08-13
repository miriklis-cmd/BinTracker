# BinTracker Roadmap

## Sprint 12 — Master Data

- [x] Container Type master data
- [x] Future-proof migration version tests
- [x] Business Information
- [ ] Complete hands-on Container Type / Business Information testing
- [ ] Final Sprint 12 polish

## Sprint 13 — Excel Import Wizard

- Workbook analysis
- Fresh database / Merge / Replace modes
- Customer matching
- Container Type mapping
- Movement import and duplicate protection
- Preview and import log

## Sprint 14 — Reports

- Validate Market Floor Sheet against imported production-scale data
- Movement History
- Outstanding Containers
- Daily / Monthly summaries
- Customer Statement polish

## Sprint 15/16 — Operator polish and production preparation

- Batch Entry workflow polish
- Dashboard improvements
- Backup / Restore
- Installer and deployment
- Production acceptance testing

## Sprint 13 progress

- [x] Excel workbook read-only analysis
- [x] `.xlsm` / `.xlsx` support
- [x] worksheet/candidate preview
- [x] structural warnings and duplicate candidate warnings
- [ ] database comparison
- [ ] customer merge preview
- [ ] container mapping
- [ ] movement import
- [ ] duplicate movement protection
- [ ] Fresh / Merge / Replace execution modes

## Post-v1.0 ideas

### Custom Report Designer
Allow businesses to build database-backed reports by selecting data fields, filters, grouping, sorting, page orientation and layout.

### Legacy Excel report template / report-layout import
Explore allowing a user to nominate an Excel sheet as a legacy report layout. BinTracker would analyse the layout and help reproduce the familiar report from live BinTracker data. This must remain separate from data import: report/output sheets should not be treated as authoritative source data.

### Customer-list-only import mode
Allow a business to import **customer master data without balances or movements**.

Typical sources:
- a single-column list of customer names;
- customer code + name;
- CSV/XLSX customer master lists;
- a custom workbook where the business wants to establish customers first and begin container tracking from zero.

The wizard should explicitly ask for import intent:
- Customers only;
- Customers + opening balances;
- Full migration (customers + balances + movements).

Customer-only mode must reuse the same matching/normalisation/merge-preview rules but must not require container mapping, B/Fwd, OUT/IN or balance reconciliation.

### Import Profiles
Support multiple workbook adapters/profiles:
- legacy/custom workbook profiles;
- standard BinTracker import template;
- configurable custom mapping for other businesses.

### v0.4.0-alpha.19.1 progress
- [x] Separate Analyse page
- [x] Separate Map page
- [x] Source / Validation / Report / Ignore worksheet classification
- [x] Source-only customer candidate preview
- [ ] Review plan generation
- [ ] Customer/database matching
- [ ] Container mapping
- [ ] Transactional import execution

### v0.4.0-alpha.19.1 progress
- [x] Review page
- [x] Existing customer-code matching
- [x] New candidate detection
- [x] Customer type mismatch detection
- [x] Source-sheet conflict detection
- [x] Source snapshot reconciliation summary
- [ ] New-customer action/name confirmation
- [ ] Container mapping
- [ ] Transactional Import stage

### v0.4.0-alpha.19.1 progress
- [x] Conservative customer-code/name normalizer
- [x] Explainable automatic match reasons
- [x] `S & J` / `S&J` normalized matching
- [x] `(Y)` -> Yellow Bin legacy resolution
- [x] `(Bulk)` -> Bulk Bin legacy resolution
- [ ] Customer create/merge confirmation actions
- [ ] Full container mapping / unresolved-container handling
- [ ] Transactional Import

### v0.4.0-alpha.19.1 progress
- [x] Developer database backup
- [x] Stage/load an existing BinTracker test database safely on restart
- [x] Start fresh test database with automatic pre-reset backup
- [x] Re-import safety rules documented
- [ ] ImportRun / source fingerprint persistence
- [ ] Source-row provenance for import-generated positions/movements
- [ ] Exact re-import blocking
- [ ] Changed-workbook same-cutover difference workflow

### v0.4.0-alpha.19.1 progress
- [x] Authoritative Excel B/Fwd reconciliation planner
- [x] Existing-balance vs Excel-target preview
- [x] Preserve cutover-day OUT/IN in projected result
- [ ] Default/unprefixed legacy container mapping
- [ ] Customer create/merge confirmation
- [ ] ImportRun/source provenance
- [ ] Transactional execution

### v0.4.0-alpha.19.1 progress
- [x] Fix BalanceService SQLite translation crash
- [x] Add real SQLite balance-service regression test
- [ ] Customer create/merge confirmation
- [ ] Default/unprefixed legacy container mapping
- [ ] ImportRun/source provenance and re-import protection
- [ ] Transactional Import execution

### v0.4.0-alpha.19.1 progress
- [x] Unprefixed legacy customer -> Blue Bin default
- [x] Known explicit token resolution
- [x] Unknown explicit token hard blocker
- [x] Container inference reason visible in Review
- [ ] UI workflow to map unknown tokens to existing/new Container Types
- [ ] Customer create/merge confirmation
- [ ] ImportRun/source provenance
- [ ] Transactional Import

### v0.4.0-alpha.19.1 progress
- [x] UI workflow to map unknown legacy container tokens
- [x] Map token to existing Container Type
- [x] Open Container Type management and refresh choices
- [x] Recalculate Review after mappings
- [ ] Persist aliases inside future Import Profiles
- [ ] Customer create/merge confirmation
- [ ] ImportRun/source provenance
- [ ] Transactional Import

### v0.4.0-alpha.19.1 progress
- [x] Editable proposed names for new customers
- [x] Explicit Create / Skip decisions
- [x] Selected / all bulk actions
- [x] Decisions retained across wizard navigation
- [x] Unconfirmed customers block reconciliation
- [x] Skipped customers excluded without blocking
- [x] Unit and reconciliation tests
- [ ] Existing-customer match override/confirmation
- [ ] ImportRun/source provenance
- [ ] Transactional Import

### v0.4.0-alpha.19.1 progress
- [x] Existing-customer match confirmation
- [x] Existing-customer match override
- [x] Existing-match decisions retained across wizard navigation
- [x] Unconfirmed existing matches block readiness
- [x] Developer dialog newline fix
- [x] Customer bulk-button clipping fix
- [ ] ImportRun/source provenance
- [ ] Exact re-import blocking
- [ ] Transactional Step 4 Import

### v0.4.0-alpha.19.1 progress
- [x] ImportRun/source provenance schema
- [x] SHA-256 source workbook fingerprint
- [x] Exact completed-workbook re-import detection
- [x] Step 4 preflight screen
- [x] Existing match first-row/inactive display fix
- [x] Human-readable match decision labels
- [ ] Transactional customer creation
- [ ] Transactional opening adjustments
- [ ] Transactional daily OUT/IN movements
- [ ] Commit completed ImportRun
- [ ] Full rollback on failure

### v0.4.0-alpha.19.1 progress
- [x] Review balance grid vertical layout fix
- [x] Step 3 → Step 4 readiness gate fix
- [x] Centralised Review readiness policy
- [ ] Transactional customer creation
- [ ] Transactional opening adjustments
- [ ] Transactional daily OUT/IN movements
- [ ] Commit completed ImportRun
- [ ] Full rollback on failure

### v0.4.0-alpha.19.1 progress
- [x] Root-cause fix for collapsed Review tab region
- [x] Customer Matches fills remaining Step 3 height
- [x] Balance Reconciliation fills remaining Step 3 height
- [ ] Transactional customer creation
- [ ] Transactional opening adjustments
- [ ] Transactional daily OUT/IN movements
- [ ] Commit completed ImportRun
- [ ] Full rollback on failure

### v0.4.0-alpha.19.1 progress
- [x] Practical Balance Reconciliation viewing area
- [x] Full-size reconciliation viewer
- [x] Cutover math regression coverage
- [x] Workbook-lock crash hardening
- [ ] Transactional customer creation
- [ ] Transactional opening adjustments
- [ ] Transactional daily OUT/IN movements
- [ ] Commit completed ImportRun
- [ ] Full rollback on failure

### v0.4.0-alpha.19.1 progress
- [x] Review summary metric-card redesign
- [x] Review action-row redesign
- [x] Persistent large reconciliation viewer action
- [x] Larger normal reconciliation area
- [x] Reconciliation formula context in headers
- [x] Password eye / eye-slash artwork
- [ ] Transactional customer creation
- [ ] Transactional opening adjustments
- [ ] Transactional daily OUT/IN movements
- [ ] Commit completed ImportRun
- [ ] Full rollback on failure

### v0.4.0-alpha.19.1 progress
- [x] Concise Balance Reconciliation headers
- [x] Summary-card clipping fix
- [x] Mockup icon set implemented as DPI-safe vectors
- [x] Review action icons aligned with metric cards
- [ ] Transactional customer creation
- [ ] Transactional opening adjustments
- [ ] Transactional daily OUT/IN movements
- [ ] Commit completed ImportRun
- [ ] Full rollback on failure

### v0.4.0-alpha.19.1 progress
- [x] Approved Review raster icon assets
- [x] Review action button clipping fix
- [x] Resolve container before customer confirmation blocker
- [x] Calculate opening/projected preview before customer confirmation blocker
- [x] CLAMMS Blue/Bulk/Yellow regression coverage
- [ ] Transactional customer creation
- [ ] Transactional opening adjustments
- [ ] Transactional daily OUT/IN movements
- [ ] Commit completed ImportRun
- [ ] Full rollback on failure

### v0.4.0-alpha.19.1 progress
- [x] Approved original-mockup icon artwork used directly
- [x] Bold primary / grey secondary metric typography
- [x] Review button pending counts
- [x] Full View balance reconciliation larger label
- [x] Wider Review wizard
- [ ] Transactional customer creation
- [ ] Transactional opening adjustments
- [ ] Transactional daily OUT/IN movements
- [ ] Commit completed ImportRun
- [ ] Full rollback on failure

### v0.4.0-alpha.19.1 progress
- [x] Simplified Review metric cards
- [x] Map container button/icon clipping fix
- [x] Reconciliation action clipping fix
- [x] xUnit warning cleanup
- [x] Analyse warning alignment cleanup
- [ ] Transactional customer creation
- [ ] Transactional opening adjustments
- [ ] Transactional daily OUT/IN movements
- [ ] Commit completed ImportRun
- [ ] Full rollback on failure

### v0.4.0-alpha.19.1 progress
- [x] Duplicate Analyse warning icon fix
- [x] Container icon inset/cropping fix
- [x] Map container button spacing fix
- [x] Consistent Review secondary metrics
- [ ] Transactional customer creation
- [ ] Transactional opening adjustments
- [ ] Transactional daily OUT/IN movements
- [ ] Commit completed ImportRun
- [ ] Full rollback on failure

### v0.4.0-alpha.19.1 progress
- [x] Transactional confirmed customer creation
- [x] Transactional opening adjustments
- [x] Transactional cutover-day OUT movements
- [x] Transactional cutover-day IN movements
- [x] Completed ImportRun written atomically
- [x] Exact SHA re-import re-check inside transaction
- [x] Workbook-change-after-preflight guard
- [x] Import audit event
- [x] Full transaction rollback on execution failure
- [ ] Production-scale validation against full legacy workbook
- [ ] Changed-workbook same-cutover replacement workflow
- [ ] Import-run history/details UI
- [ ] Explicit BinMovement.ImportRunId FK

### v0.4.0-alpha.19.1 progress
- [x] Market Floor opening-adjustment semantics
- [x] Cash/COD credit placement
- [x] Two-page Market Floor pagination
- [x] Adaptive front-page readability sizing
- [x] Statement adjustment labelling
- [x] Stale Import-disabled Review wording removed
- [x] Analyse duplicate warning icon removed
- [x] First-run administrator button alignment
- [ ] Revalidate generated Market Floor PDF against imported production-like data
- [ ] Revalidate Zahos statement after adjustment labelling
- [ ] Continue alpha.19 importer end-to-end verification
