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

### Import Profiles
Support multiple workbook adapters/profiles:
- legacy/custom workbook profiles;
- standard BinTracker import template;
- configurable custom mapping for other businesses.
