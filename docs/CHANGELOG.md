# Changelog

## 0.1.0-alpha.3

- Added local login and role-based access foundation.
- Added secure password hashing.
- Added first-run administrator creation.
- Added user administration.
- Added append-only audit trail and administrator audit viewer.
- Added auditing for login, logout, failed login, user creation and user status changes.
- Added backward-compatible SQLite schema upgrade from Alpha 2.

## 0.1.0-alpha.2

- Corrected .NET 8 package compatibility and build issues.

## 0.1.0-alpha.3.2

- Fixed first-run administrator form layout for DPI scaling and smaller displays.
- Added scrolling and a persistent bottom action bar so buttons remain visible.

## 0.1.0-alpha.3.5

- Applied DPI-aware, responsive layouts across the application.
- Rebuilt the dashboard cards to prevent clipped headings, values and subtitles.
- Removed the dashboard's unnecessary horizontal scrollbar.
- Made header text and signed-in details resize safely.
- Updated Users, Add User and Audit Trail windows for high-DPI and smaller displays.
- Added minimum sizes, wrapping and scroll-safe layouts to current application forms.


## 0.1.0-alpha.3.7
- Kept SQLite as the active database for simple development/install.
- Isolated database provider and connection configuration.
- Added provider-neutral database settings.
- Prepared the data layer for a later PostgreSQL multi-user migration.
- Prevented future connection settings from being committed to Git.


## 0.2.0-alpha.1
- Added customer management.
- Added customer reminder contact preferences.
- Added customer audit events.
- Added reminder delivery persistence groundwork.
- Renamed Standard Bin to Blue Bin while preserving Id/history.

## 0.2.0-alpha.2
- Made customer code mandatory and code-first throughout Customer Management.
- Expanded customer search fields and default sort order.
- Added PDF Customer Statements using QuestPDF.
- Added statement-period selection and report audit events.
- Fixed Audit Trail grid sizing at high DPI.

## 0.2.0-alpha.3
- Aligned login action buttons and placed Log in before Cancel.
- Main application now opens maximized.


## 0.2.0-alpha.4
- Fixed clipped Customer Statement Period actions at high DPI.
- Fixed SaveFileDialog/OLE crash by keeping WinForms startup on the STA thread.


## 0.2.0-alpha.5
- Enforced customer-code uniqueness ignoring case.
- Added Account / Cash-COD customer classification.
- Added customer type to the customer workspace/list.
- Aligned Settings action buttons.


## 0.2.0-alpha.6
- Added schema-versioned SQLite upgrades.
- Fixed Alpha 5 SQLite startup syntax error.
- Added unit and integration test projects.
- Added migration regression tests.
- Updated build script to run tests.
- Added Functional Specification, Business Rules, and Testing documentation.


## 0.2.0-alpha.6.1
- Fixed missing xUnit namespaces in new test projects.
- Fixed build script false-success reporting.
- Added batch build/test launcher.
- Removed EF1002 schema-upgrade warnings.
- Removed duplicate WinForms DPI manifest configuration.

## 0.2.0-alpha.6.2
- Added self-service password changes, administrator reset, forced change, lockout/unlock, session information, audit events, and security tests.


## 0.2.0-alpha.6.2.1
- Fixed blank Settings page caused by FlowLayoutPanel/AutoSize interaction.
- Rebuilt Settings using explicit TableLayoutPanel sections.

## 0.2.0-alpha.6.2.2
- UI polish for password/user/settings screens.
- Added administrator manual Lock / Unlock.

## 0.2.0-alpha.6.2.3
- Removed unnecessary Add User vertical scrolling.
- Simplified Users grid to a single Status column and improved sizing.

## 0.2.0-alpha.6.2.4
- Fixed Users toolbar/status clipping.
- Added administrator Change Role workflow with audit logging.

## 0.2.0-alpha.7.0
- Added operational Batch Entry.
- Added transactional MovementService batch save and audit record.
- Added customer-code autocomplete and live customer balances.
- Added movement unit/integration tests.
- Added release test checklist.

## 0.2.0-alpha.7.1
- Added persistent in-memory Batch Entry drafts across navigation.
- Added Current vs With Draft live balance preview.
- Added Enter-on-Quantity/Reference and Ctrl+Enter keyboard workflow.
- Dashboard now displays live saved movement totals.
- Polished User Management colours, status wording and context-sensitive actions.
- Improved Customer recent movement grid on smaller screens.
- Added application version display and Known Issues.

## 0.2.0-alpha.7.2
- Added editable pending batch lines and further responsive UI polish.

## 0.2.0-alpha.7.2.1
- Cleaned the remaining WinForms nullable-reference warnings.

## 0.2.0-alpha.7.2.2
- Fixed the three remaining CS8602 warnings in DataGridView cell-formatting handlers.

## 0.2.0-alpha.7.2.3
- Fixed Customer screen lower-right clipping by removing fixed panel minimum heights and giving Movement History the remaining vertical space.

## 0.2.0-alpha.7.2.4
- Removed unused Customer details height and redistributed it to Current Position and Recent Movement History.

## 0.2.0-alpha.7.2.5
- Tightened Customer layout and redistributed lower-grid space.
- Disabled automatic Batch Entry grid tooltips.
- Quantity now starts blank and requires positive input.

## 0.2.0-alpha.7.2.6
- Rebuilt Customer details layout to eliminate the phantom whitespace.
- Widened movement-history Date, Direction and Entered By columns.
- Added top-right Logout and return-to-login session flow.

## 0.2.0-alpha.7.2.7
- Tightened Customer editor height and widened movement-history columns.

## 0.2.0-alpha.7.2.8
- Fixed shared page-title clipping at scaled DPI settings.
- Added built-in Windows icons to the left navigation.

## 0.2.0-alpha.7.2.9
- Replaced navigation font glyphs with embedded PNG icons.
- Added logout icon.
- Added reusable show/hide password eye controls throughout the application.

## 0.2.0-alpha.7.2.10
- Integrated password eye controls into field styling.
- Fixed Logout button clipping/alignment.
- Replaced Settings navigation artwork with a standard cog icon.

## 0.2.0-alpha.7.2.11
- Approved login/header/navigation polish.

## 0.2.0-alpha.7.2.12
- Applied approved icon artwork and fixed Logout clipping.

## 0.2.0-alpha.7.2.13
- Replaced problematic image rendering for password eye and Logout with DPI-safe custom WinForms drawing.

## 0.2.0-alpha.8.0
- Implemented the full Single Entry manual movement workflow.
- Added manual-movement service persistence and audit logging.

## 0.2.0-alpha.8.0.1
- Fixed Customer action buttons being clipped below the visible details area.

## 0.2.0-alpha.8.0.2
- Cleaned up Single Entry text alignment and removed redundant Ready status.

## 0.2.0-alpha.8.0.3
- Single Entry now fully resets after a successful save.

## 0.2.0-alpha.9.0
- Added Reports hub and two-page duplex Market Floor Sheet.
- Added date-based B/Fwd / Out / In / Total calculation.
- Added Account/Cash front and reverse grouping plus special-container summary.

## 0.2.0-alpha.9.0.1
- Corrected Market Floor Sheet to A4 portrait on both sides.
- Fixed clipped Generate & Open button.

## 0.3.0-alpha.1
- Added configurable Container Type master data and management UI.
- Added SQLite schema migration v7 without changing existing container IDs.
- Market Floor Sheet now uses explicit special-container metadata.

## v0.3.0-alpha.2
- Future-proofed SQLite migration tests by deriving the expected latest schema version.
- Added Container Type master-data migration regression coverage.

## v0.3.0-alpha.3
- Added Business Information master data and report-header integration.
- Added SQLite schema migration v8 and related tests/documentation.
