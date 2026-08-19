# BinTracker Requirements & Acceptance Register

Current baseline: **v0.4.0-alpha.24.2.25**

This is the permanent requirements ledger for BinTracker. A requirement may change status or scope, but it must not silently disappear. `docs/Roadmap.md` provides sequencing; this register provides requirement identity and acceptance state.

## Status / provenance legend

Allowed scopes: `v1`, `post-v1`, `candidate`.

Allowed statuses:
- `IMPLEMENTED-STATIC` — implementation is present in the audited source; Windows/manual acceptance may still be required.
- `IMPLEMENTED-ACCEPTED` — implementation is present and the operator has explicitly reported it working.
- `PLANNED-V1` — required before v1 but not yet implemented/accepted.
- `POST-V1` — explicitly tabled until after v1.
- `NEEDS-CONFIRMATION` — recovered historical idea/detail that is not strong enough to silently promote into v1 scope.

Provenance tags:
- `CHAT-SURFACED` — explicitly visible in the current BinTracker conversation history/context.
- `CURRENT-DOC` — present in current source documentation.
- `HIST-BUILD` — recovered from one or more archived BinTracker builds.
- `CODE` — verified in current source code.

## Product / data model / architecture

| ID | Scope | Status | Requirement | Provenance |
|---|---|---|---|---|
| BT-ARCH-001 | v1 | IMPLEMENTED-STATIC | Windows desktop application replaces the daily-saved Excel bin-tracking workflow. | CURRENT-DOC,HIST-BUILD,CODE |
| BT-ARCH-002 | v1 | IMPLEMENTED-STATIC | Track customer/container movements daily and query historical activity by date/range. | CHAT-SURFACED,CURRENT-DOC,CODE |
| BT-ARCH-003 | v1 | IMPLEMENTED-STATIC | Customer balances remain separated by configured Container Type. | CURRENT-DOC,HIST-BUILD,CODE |
| BT-ARCH-004 | v1 | IMPLEMENTED-STATIC | Services + `IDbContextFactory<BinTrackerDbContext>` remain the business/data-access boundary. | CHAT-SURFACED,CURRENT-DOC,CODE |
| BT-ARCH-005 | post-v1 | POST-V1 | PostgreSQL/central-provider migration must preserve service boundaries; do not add a generic Repository merely for migration. | CHAT-SURFACED,CURRENT-DOC |
| BT-ARCH-006 | post-v1 | POST-V1 | Multi-computer readiness includes provider audit, concurrency, configuration and central backup strategy. | CURRENT-DOC |
| BT-ARCH-007 | v1 | IMPLEMENTED-STATIC | BinTracker targets `net8.0`/`net8.0-windows`; a compatible newer installed SDK may build it. | CHAT-SURFACED,CURRENT-DOC,CODE |

## Build, audit, versioning and release discipline

| ID | Scope | Status | Requirement | Provenance |
|---|---|---|---|---|
| BT-REL-001 | v1 | PLANNED-V1 | Every candidate build receives a full source/current-state/documentation/roadmap/version audit; audit is a release gate, not a prose claim. | CHAT-SURFACED,CURRENT-DOC |
| BT-REL-002 | v1 | IMPLEMENTED-STATIC | `Audit-BinTracker.ps1` runs before restore/build/test and must fail the build on reconciliation defects. | CURRENT-DOC,CODE |
| BT-REL-003 | v1 | IMPLEMENTED-STATIC | ZIP filename, sole root folder, `Version`, `InformationalVersion` and current release metadata must match exactly. | CHAT-SURFACED,CURRENT-DOC |
| BT-REL-004 | v1 | IMPLEMENTED-STATIC | Packaging must mechanically reopen and verify the produced ZIP before delivery. | CHAT-SURFACED,CURRENT-DOC |
| BT-REL-005 | v1 | IMPLEMENTED-STATIC | Build script must stop on restore/build/test failure and must never print BUILD SUCCESSFUL after a failed command. | CHAT-SURFACED,CURRENT-DOC,CODE |
| BT-REL-006 | v1 | IMPLEMENTED-STATIC | Build tooling cleans stale MSBuild build servers and disables server/node reuse to reduce intermittent MSB4242 worker-node failures. | CHAT-SURFACED,CURRENT-DOC,CODE |
| BT-REL-007 | v1 | IMPLEMENTED-STATIC | Build tooling self-heals only the exact obsolete alpha.23.3 `global.json`; unrelated SDK configuration must not be deleted. | CHAT-SURFACED,CURRENT-DOC,CODE |
| BT-REL-008 | v1 | PLANNED-V1 | Release build acceptance requires clean build, zero warnings and all automated tests passing. | CURRENT-DOC,HIST-BUILD |
| BT-REL-009 | v1 | IMPLEMENTED-STATIC | Genuine feature milestones increment the milestone; bug/polish fixes use one suffix where practical; avoid deeply nested new-version schemes. | CHAT-SURFACED,CURRENT-DOC |
| BT-REL-010 | v1 | PLANNED-V1 | UI changes require appropriate Windows smoke/DPI testing; unverified manual behavior stays explicitly unverified. | CHAT-SURFACED,CURRENT-DOC |

## Authentication / users / security / audit

| ID | Scope | Status | Requirement | Provenance |
|---|---|---|---|---|
| BT-SEC-001 | v1 | IMPLEMENTED-STATIC | First-run Administrator creation and role-based user management. | CURRENT-DOC,HIST-BUILD,CODE |
| BT-SEC-002 | v1 | IMPLEMENTED-STATIC | Login/logout, failed-login lockout/unlock and password change/reset flows. | CURRENT-DOC,HIST-BUILD,CODE |
| BT-SEC-003 | v1 | IMPLEMENTED-STATIC | Password fields start masked and eye controls reveal/re-hide passwords consistently. | HIST-BUILD,CODE |
| BT-SEC-004 | v1 | IMPLEMENTED-STATIC | Viewer/read-only roles cannot perform prohibited movement/admin writes. | CURRENT-DOC,HIST-BUILD,CODE |
| BT-SEC-005 | v1 | PLANNED-V1 | Full authorization review across every write/admin action before v1. | CURRENT-DOC |
| BT-SEC-006 | v1 | PLANNED-V1 | Secrets/credential storage review before Email/SMS provider configuration. | CURRENT-DOC |
| BT-SEC-007 | v1 | PLANNED-V1 | Support-safe error logging must not leak passwords, secrets or customer-sensitive data. | CURRENT-DOC |
| BT-AUD-001 | v1 | IMPLEMENTED-STATIC | Maintain explicit audit-coverage matrix across security, admin, customer, movement, import, report, communications and production actions. | CHAT-SURFACED,CURRENT-DOC |
| BT-AUD-002 | v1 | IMPLEMENTED-STATIC | PDF report generation is audited for all implemented report types. | CURRENT-DOC,CODE |
| BT-AUD-003 | v1 | IMPLEMENTED-ACCEPTED | CSV export is audited for Outstanding, Daily, Weekly, Movement History and Monthly Summary. | CHAT-SURFACED,CURRENT-DOC,CODE |
| BT-AUD-004 | v1 | IMPLEMENTED-STATIC | CSV audit records filename, row count and relevant report/filter context without storing report contents. | CHAT-SURFACED,CURRENT-DOC,CODE |
| BT-AUD-005 | v1 | IMPLEMENTED-STATIC | If CSV file creation succeeds but audit persistence fails, operator is warned rather than audit loss being silent. | CHAT-SURFACED,CODE |
| BT-AUD-006 | candidate | NEEDS-CONFIRMATION | Audit Trail screen may need additional filtering/export polish before production. Recovered from early project README; not silently promoted to v1. | HIST-BUILD |

## Navigation / branding / general WinForms behavior

| ID | Scope | Status | Requirement | Provenance |
|---|---|---|---|---|
| BT-UI-001 | v1 | IMPLEMENTED-ACCEPTED | Left navigation product logo and full `BinTracker` wordmark are visible, aligned and unclipped. | CHAT-SURFACED,CODE |
| BT-UI-002 | v1 | IMPLEMENTED-STATIC | Login, Main, report breakouts and dialogs inherit the BinTracker application icon; pre-login taskbar uses BinTracker icon. | CHAT-SURFACED,CURRENT-DOC,CODE |
| BT-UI-003 | v1 | IMPLEMENTED-STATIC | Startup splash displays BinTracker product branding/version and disappears as startup completes without artificial delay. | CHAT-SURFACED,CURRENT-DOC,CODE |
| BT-UI-004 | v1 | IMPLEMENTED-STATIC | Product branding is separate from the operator/business branding used on generated outputs. | CHAT-SURFACED,CURRENT-DOC |
| BT-UI-005 | v1 | PLANNED-V1 | General high-DPI pass at 100%, 125% and 150%; buttons, labels and actions must not clip. | CHAT-SURFACED,CURRENT-DOC,HIST-BUILD |
| BT-UI-006 | v1 | IMPLEMENTED-STATIC | Report/data windows size responsively from current monitor working area; grids grow on larger monitors. | CHAT-SURFACED,CURRENT-DOC,CODE |
| BT-UI-007 | v1 | IMPLEMENTED-STATIC | Customer-code and similar report columns have enough width/dynamic sizing to avoid truncating real codes; Type remains readable. | CHAT-SURFACED,CODE |
| BT-UI-008 | v1 | IMPLEMENTED-STATIC | Navigation icon/text controls remain fully visible and clickable at laptop production DPI. | HIST-BUILD,CODE |
| BT-UI-009 | post-v1 | POST-V1 | WinUI 3 / Windows UI v2 discussion is tabled until after v1 publication. | CHAT-SURFACED,CURRENT-DOC |
| BT-UI-010 | post-v1 | POST-V1 | WinUI 3 evaluation explicitly compares Dashboard, Reports launcher, individual reports and import workflow; rewrite is not predetermined. | CHAT-SURFACED,CURRENT-DOC |
| BT-UI-011 | v1 | IMPLEMENTED-STATIC | Reports landing page uses the approved Quick Reports + Explore Reports card hierarchy, exact approved report-icon artwork, compact 3x2 explorer grid and no unnecessary vertical scrolling at normal 1080p desktop size. | CHAT-SURFACED,CODE |
| BT-UI-012 | v1 | IMPLEMENTED-STATIC | Containers is a dedicated left-navigation destination immediately below Customers; Container Types are no longer buried in Settings. | CHAT-SURFACED,CODE |
| BT-UI-013 | v1 | IMPLEMENTED-STATIC | Reports landing-page cards, descriptions, action rows and page subtitle must remain fully visible at supported Windows DPI scaling; fixed layout rows must not collapse around AutoSize content. | CHAT-SURFACED,CODE |

## Customers / master data / business information

| ID | Scope | Status | Requirement | Provenance |
|---|---|---|---|---|
| BT-CUST-001 | v1 | IMPLEMENTED-STATIC | Customer search supports code/name and clears stale detail on no result. | CURRENT-DOC,HIST-BUILD,CODE |
| BT-CUST-002 | v1 | IMPLEMENTED-STATIC | Customer code uniqueness is case-insensitive. | CURRENT-DOC,CODE |
| BT-CUST-003 | v1 | IMPLEMENTED-STATIC | Dirty customer edits prompt Save / Discard / Cancel when changing/searching/navigating/logout/close. | CURRENT-DOC,CODE |
| BT-CUST-004 | v1 | PLANNED-V1 | Add operator-useful customer sorting by code/name/outstanding/credit/last movement. | CHAT-SURFACED,CURRENT-DOC |
| BT-CUST-005 | v1 | PLANNED-V1 | Add lifetime OUT/Taken and IN/Returned totals where operationally useful. | CHAT-SURFACED,CURRENT-DOC |
| BT-CUST-006 | v1 | IMPLEMENTED-STATIC | Current Position and Recent Movement History remain usable/scrollable without large blank bands. | HIST-BUILD,CODE |
| BT-CT-001 | v1 | IMPLEMENTED-STATIC | Container Type create/update/deactivate/reactivate with duplicate name/short-code rejection. | CURRENT-DOC,HIST-BUILD,CODE |
| BT-CT-002 | v1 | IMPLEMENTED-STATIC | Container rename preserves movement/history links; display order controls entry dropdown order. | HIST-BUILD,CODE |
| BT-CT-003 | v1 | IMPLEMENTED-STATIC | Inactive types disappear from new entry while historical reporting remains valid/selectable where needed. | CURRENT-DOC,HIST-BUILD,CODE |
| BT-CT-004 | v1 | IMPLEMENTED-STATIC | Special Floor Report Container flag controls special-container treatment. | CURRENT-DOC,HIST-BUILD,CODE |
| BT-CT-005 | v1 | IMPLEMENTED-STATIC | All signed-in users may view configured Container Types from Containers; add/rename/reorder/deactivate/reactivate controls are restricted to Administrators. | CHAT-SURFACED,CODE |
| BT-BIZ-001 | v1 | IMPLEMENTED-STATIC | Business Information persists business/trading identity and Default Report Header; update is audited. | CURRENT-DOC,HIST-BUILD,CODE |
| BT-BIZ-002 | v1 | IMPLEMENTED-STATIC | Report header fallback: Default Report Header → Trading Name → Business Name → BinTracker. | CURRENT-DOC,HIST-BUILD,CODE |
| BT-BIZ-003 | v1 | PLANNED-V1 | Business Information supports operator/business logo. | CHAT-SURFACED,CURRENT-DOC |
| BT-BIZ-004 | v1 | PLANNED-V1 | One authoritative branding model/service feeds PDFs, statements, emails and other generated output. | CHAT-SURFACED,CURRENT-DOC |
| BT-BIZ-005 | v1 | PLANNED-V1 | Define logo storage, formats, dimensions/aspect ratio, fallbacks, placement and per-output enable/disable. | CURRENT-DOC |
| BT-BIZ-006 | v1 | PLANNED-V1 | Decide coexistence of business name, trading name, logo and custom header without duplicate-looking output. | CURRENT-DOC |

## Single Entry

| ID | Scope | Status | Requirement | Provenance |
|---|---|---|---|---|
| BT-SE-001 | v1 | IMPLEMENTED-STATIC | Keyboard-first customer lookup/autocomplete and invalid-customer rejection. | HIST-BUILD,CODE |
| BT-SE-002 | v1 | IMPLEMENTED-STATIC | Quantity starts blank and Current/After Save position preview reflects selected direction/container/quantity. | HIST-BUILD,CODE |
| BT-SE-003 | v1 | IMPLEMENTED-STATIC | Save confirmation identifies customer, direction, container, quantity and date; save updates balance/history/audit immediately. | HIST-BUILD,CODE |
| BT-SE-004 | v1 | IMPLEMENTED-STATIC | `Ctrl+Enter` saves Single Entry. | HIST-BUILD,CODE |
| BT-SE-005 | v1 | IMPLEMENTED-STATIC | Successful save resets date=today, direction=IN/Returned, customer, container, quantity, reference, notes and preview, then focuses Customer. | HIST-BUILD,CODE |
| BT-SE-006 | v1 | IMPLEMENTED-STATIC | Viewer cannot save Single Entry movement. | HIST-BUILD,CODE |

## Batch Entry

| ID | Scope | Status | Requirement | Provenance |
|---|---|---|---|---|
| BT-BATCH-001 | v1 | IMPLEMENTED-ACCEPTED | `Ctrl+Enter` saves batch. | CHAT-SURFACED,CURRENT-DOC,CODE |
| BT-BATCH-002 | v1 | IMPLEMENTED-ACCEPTED | Tab/Shift+Tab keyboard flow works. | CURRENT-DOC,HIST-BUILD |
| BT-BATCH-003 | v1 | IMPLEMENTED-STATIC | Enter from Quantity / Reference / Notes adds in Add mode and updates the selected row in Edit mode; Enter while editing must never append a duplicate or leave Update Line active afterward. | CHAT-SURFACED,CURRENT-DOC,CODE |
| BT-BATCH-004 | v1 | IMPLEMENTED-STATIC | Pending lines affect Current vs With Draft preview. | CURRENT-DOC,CODE |
| BT-BATCH-005 | v1 | IMPLEMENTED-STATIC | Clicking a draft line loads edit fields; Update/Remove/Clear recalculates or clears draft preview as applicable, clears the editor afterward, and returns to Add to Batch mode. Removing or clearing the final line must not leave a ghost edit state; stale asynchronous row/customer loads must never resurrect Update Line after Esc/Clear. | CHAT-SURFACED,CURRENT-DOC,CODE |
| BT-BATCH-006 | v1 | IMPLEMENTED-STATIC | Esc while editing cancels edit mode, clears the current editor/preview, retains the draft, and reports the retained-draft status beneath the line/container summary. | CHAT-SURFACED,CURRENT-DOC,CODE |
| BT-BATCH-007 | v1 | IMPLEMENTED-ACCEPTED | Draft survives page navigation and logout/login while process remains running. | CHAT-SURFACED,CURRENT-DOC,CODE |
| BT-BATCH-008 | v1 | IMPLEMENTED-STATIC | Esc has explicit state semantics: cancel+clear draft-line edit first; otherwise clear only current unsaved entry fields; otherwise leave for Dashboard while retaining the draft. Programmatic Esc navigation must synchronise the left-nav highlight so Batch Entry can immediately be reopened. | CHAT-SURFACED,CURRENT-DOC,CODE |
| BT-BATCH-009 | v1 | IMPLEMENTED-ACCEPTED | Successful Add to Batch clears customer/quantity/reference/notes/customer preview and returns focus to Customer; pending-grid rebinding must not auto-select/reload the just-added row; movement date, batch direction and container type intentionally carry forward for rapid entry. | CHAT-SURFACED,CURRENT-DOC,CODE |
| BT-BATCH-010 | v1 | IMPLEMENTED-STATIC | Unsaved Batch Entry draft lines/date/direction are persisted atomically under LocalApplicationData after changes; successful Save Batch/Clear Batch removes the recovery file. | CHAT-SURFACED,CURRENT-DOC,CODE,TEST |
| BT-BATCH-011 | v1 | IMPLEMENTED-STATIC | A persisted draft loaded after process restart/normal close/crash/power loss is not silently resumed: BinTracker presents Continue Batch / Save Batch / Discard Batch, in that visual order, with draft date, direction, line count, total quantity and last-saved time. Discard requires confirmation; failed recovery-save retains the draft. Same-process navigation/logout drafts do not trigger the recovery prompt. | CHAT-SURFACED,CURRENT-DOC,CODE,TEST |

## Excel import / migration

| ID | Scope | Status | Requirement | Provenance |
|---|---|---|---|---|
| BT-IMP-001 | v1 | IMPLEMENTED-STATIC | Read-only `.xlsm`/`.xlsx` analysis with worksheet classification and no database writes before Import. | CURRENT-DOC,HIST-BUILD,CODE |
| BT-IMP-002 | v1 | IMPLEMENTED-STATIC | Import planning uses authoritative Source sheets; Validation/Report/Ignore sheets are not treated as source data. | CURRENT-DOC,CODE |
| BT-IMP-003 | v1 | IMPLEMENTED-STATIC | Conservative normalized customer matching with explicit match reasons; no automatic fuzzy merge. | CURRENT-DOC,HIST-BUILD,CODE |
| BT-IMP-004 | v1 | IMPLEMENTED-STATIC | New customers require explicit Create/Skip; existing automatic matches require confirmation/override; decisions persist across wizard navigation. | CURRENT-DOC,HIST-BUILD,CODE |
| BT-IMP-005 | v1 | IMPLEMENTED-STATIC | Legacy unprefixed customer defaults to Blue; known `(Y)` and `(Bulk)` tokens resolve to configured Yellow/Bulk; unknown explicit tokens block until mapped. | CURRENT-DOC,HIST-BUILD,CODE |
| BT-IMP-006 | v1 | IMPLEMENTED-STATIC | Manual unknown-token mapping can target existing/new Container Type and survives Review refresh/back-forward in the same wizard. | CURRENT-DOC,HIST-BUILD,CODE |
| BT-IMP-007 | v1 | IMPLEMENTED-STATIC | Excel B/Fwd is authoritative; importer reconstructs opening position and preserves real cutover OUT/IN movement. | CURRENT-DOC,CODE |
| BT-IMP-008 | v1 | IMPLEMENTED-STATIC | Transactional execution, live-database revalidation and workbook-changed-after-preflight protection. | CURRENT-DOC,CODE |
| BT-IMP-009 | v1 | IMPLEMENTED-STATIC | ImportRun + SHA-256 blocks exact successful re-import and links generated movements to provenance. | CURRENT-DOC,CODE |
| BT-IMP-010 | v1 | IMPLEMENTED-ACCEPTED | Changed workbook/same cutover is detected before execution and requires explicit Replace/Correct comparison. | CURRENT-DOC,CHAT-SURFACED,CODE |
| BT-IMP-011 | v1 | IMPLEMENTED-STATIC | Correction baseline preserves same-day/later Manual/Batch activity outside replaced import-generated movements. | CURRENT-DOC,CODE |
| BT-IMP-012 | v1 | IMPLEMENTED-STATIC | Import History shows run status, source/SHA, cutover/user/counts, replacement chain, linked movements and persisted correction differences. | CURRENT-DOC,CODE |
| BT-IMP-013 | v1 | IMPLEMENTED-STATIC | Forced failure after final SaveChanges/before Commit rolls back customer, movements, ImportRun and completion audit. | CURRENT-DOC,CODE |
| BT-IMP-014 | v1 | PLANNED-V1 | Add useful transactional execution failure report identifying row/customer/container that stopped execution. | CURRENT-DOC |
| BT-IMP-015 | v1 | PLANNED-V1 | Deferred Review cosmetics (small/cropped action icons, rounded metric tiles) remain visible tech-debt/UI work. | CURRENT-DOC,HIST-BUILD |
| BT-IMP-016 | post-v1 | POST-V1 | Customer-list-only import supports names-only, code+name, CSV/XLSX master lists and custom workbook sources. | HIST-BUILD,CURRENT-DOC |
| BT-IMP-017 | post-v1 | POST-V1 | Import intent options: Customers only; Customers + opening balances; Full migration (customers + balances + movements). | HIST-BUILD |
| BT-IMP-018 | post-v1 | POST-V1 | Customer-only mode reuses matching/normalisation/merge preview but does not require container mapping/B-Fwd/OUT/IN/balance reconciliation. | HIST-BUILD |
| BT-IMP-019 | post-v1 | POST-V1 | Import Profiles support legacy/custom workbook profiles, standard BinTracker template and configurable mapping for other businesses. | HIST-BUILD,CURRENT-DOC |
| BT-IMP-020 | post-v1 | POST-V1 | Legacy token aliases can persist inside future Import Profiles rather than remaining session-only. | HIST-BUILD |
| BT-IMP-021 | post-v1 | POST-V1 | Optional fuzzy-match suggestions require explicit operator approval and never auto-merge. | HIST-BUILD,CURRENT-DOC |

## Reports — common interaction/output rules

| ID | Scope | Status | Requirement | Provenance |
|---|---|---|---|---|
| BT-RPT-001 | v1 | IMPLEMENTED-ACCEPTED | Reports launcher keeps Market Floor first/inline; detailed reports use dedicated breakout windows. | CHAT-SURFACED,CURRENT-DOC,CODE |
| BT-RPT-002 | v1 | IMPLEMENTED-STATIC | Detailed breakout reports are single-instance and responsive to laptop/large monitor working area. | CHAT-SURFACED,CURRENT-DOC,CODE |
| BT-RPT-003 | v1 | IMPLEMENTED-STATIC | Interactive reports remove separate Run Report button: date/dropdown/checkbox changes refresh live; Customer text applies on Enter with visible cue. | CHAT-SURFACED,CURRENT-DOC,CODE |
| BT-RPT-004 | v1 | IMPLEMENTED-STATIC | Numeric report columns sort numerically rather than lexicographically. | CHAT-SURFACED,CURRENT-DOC,CODE |
| BT-RPT-005 | v1 | IMPLEMENTED-STATIC | PDF/CSV exports preserve the currently displayed grid sort/order. | CHAT-SURFACED,CURRENT-DOC,CODE |
| BT-RPT-006 | v1 | IMPLEMENTED-STATIC | Historical report date/month controls do not allow future periods; current week/month stops at today. | CHAT-SURFACED,CURRENT-DOC,CODE |
| BT-RPT-007 | v1 | IMPLEMENTED-STATIC | Notes are optional export detail where supported and remain off by default for compact output; Daily/Weekly use one notes option for PDF+CSV. | CHAT-SURFACED,CURRENT-DOC,CODE |
| BT-RPT-008 | v1 | IMPLEMENTED-STATIC | Report filter/options/actions use auto-sized layout so wrapped controls cannot clip action buttons. | CHAT-SURFACED,CURRENT-DOC,CODE |
| BT-RPT-009 | v1 | PLANNED-V1 | Decide whether native Excel export adds sufficient value beyond CSV before v1. | CURRENT-DOC |
| BT-RPT-010 | v1 | PLANNED-V1 | Final cross-report consistency/print/DPI/real-world acceptance pass before reporting milestone closes. | CURRENT-DOC,CHAT-SURFACED |
| BT-RPT-011 | v1 | IMPLEMENTED-STATIC | Report container selectors use configured Container Types master data, including inactive types for historical filtering; choices must not be inferred from current outstanding balances. | CHAT-SURFACED,CURRENT-DOC,CODE |
| BT-RPT-012 | v1 | IMPLEMENTED-STATIC | Reports landing page must fit the normal maximised viewport without page scrollbars at supported Windows scaling, and report action buttons must render their document/external-link icon plus full single-line caption without wrapping or clipping. | CHAT-SURFACED,CURRENT-DOC,CODE |
| BT-RPT-013 | v1 | IMPLEMENTED-STATIC | Reports landing page omits the redundant bottom PDF/CSV/date information bar so available vertical space is reserved for fully visible report-card content. | CHAT-SURFACED,CURRENT-DOC,CODE |
| BT-RPT-014 | v1 | IMPLEMENTED-STATIC | Outstanding Containers provides an explicit balance filter with Outstanding only (default), Credits only, and All non-zero modes; the selected mode applies consistently to on-screen results and exported PDF/CSV output. | USER-REQUEST,CODE,TEST |
| BT-RPT-015 | v1 | IMPLEMENTED-ACCEPTED | Outstanding Containers keeps the Balance selector fully readable at supported DPI and provides trial multi-column grid sorting: click sets the primary sort and Shift+click adds/toggles secondary or later sort columns while preserving the displayed order in PDF/CSV snapshots. | USER-REQUEST,CODE,USER-ACCEPTED |
| BT-RPT-016 | v1 | IMPLEMENTED-STATIC | All applicable report grids provide consistent type-aware multi-column sorting: click sets a primary sort, Shift+click adds/toggles later sort levels, numeric quantities/positions sort by their true business value rather than display text (including CREDIT as negative and OUT as positive), report dates sort chronologically, active sorts persist across report refreshes, and each grid displays an on-screen usage hint; Outstanding Containers keeps its filter/action controls fully visible at supported DPI. | USER-APPROVED,CODE |
| BT-RPT-017 | v1 | IMPLEMENTED-STATIC | Every active sort on an applicable report grid visibly identifies both direction and sort priority in the column header (for example ▲1/▼1 and ▲2/▼2). The indication must remain visible at supported DPI, including narrow text columns such as Direction, must stay on a single header line without sort-driven header/grid height or column-width changes, and must not depend solely on the WinForms native sort glyph. | CODE |

## Market Floor / Outstanding / movements / statements / summaries

| ID | Scope | Status | Requirement | Provenance |
|---|---|---|---|---|
| BT-MF-001 | v1 | IMPLEMENTED-ACCEPTED | Market Floor generates exactly two pages for current real workbook and is designed for duplex front/reverse use. | CURRENT-DOC,HIST-BUILD,CODE |
| BT-MF-002 | v1 | IMPLEMENTED-STATIC | Front: Account owing first two columns; Cash/COD owing separate; Account credits separate CREDIT section; Cash credits stay with Cash. | CURRENT-DOC,HIST-BUILD,CODE |
| BT-MF-003 | v1 | IMPLEMENTED-STATIC | Blue is implicit; Yellow explicit; Bulk/special configured containers appear in special-container treatment. | CHAT-SURFACED,CURRENT-DOC,CODE |
| BT-MF-004 | v1 | IMPLEMENTED-STATIC | Reverse: Account/Cash daily OUT/IN/B-Fwd/Total; opening adjustments contribute to B/Fwd, not physical daily movement. | CURRENT-DOC,HIST-BUILD,CODE |
| BT-MF-005 | v1 | IMPLEMENTED-STATIC | Historic dates can be regenerated; future dates are blocked. | HIST-BUILD,CURRENT-DOC,CODE |
| BT-MF-006 | v1 | PLANNED-V1 | Stress-test adaptive layout with a genuinely high Yellow-bin day. | CURRENT-DOC |
| BT-OUT-001 | v1 | IMPLEMENTED-STATIC | Outstanding supports current/as-of-date positions with customer/container filters and inactive/credit options. | CURRENT-DOC,CODE |
| BT-OUT-002 | v1 | IMPLEMENTED-STATIC | All-container result groups each customer's configured container rows adjacently. | CHAT-SURFACED,CURRENT-DOC,CODE |
| BT-OUT-003 | v1 | IMPLEMENTED-STATIC | Outstanding has audited landscape PDF and CSV export preserving displayed order. | CURRENT-DOC,CODE |
| BT-DAY-001 | v1 | IMPLEMENTED-STATIC | Daily Movements has Today/Yesterday shortcuts; physical movements by default; optional opening adjustments. | CURRENT-DOC,CODE |
| BT-DAY-002 | v1 | IMPLEMENTED-STATIC | Daily filters customer/container/direction/source and exports audited PDF/CSV in visible order. | CURRENT-DOC,CODE |
| BT-WEEK-001 | v1 | IMPLEMENTED-STATIC | Weekly period is Monday–Sunday; selected date resolves to that calendar week. | CHAT-SURFACED,CURRENT-DOC,CODE |
| BT-WEEK-002 | v1 | IMPLEMENTED-STATIC | Weekly contains Daily Detail and Weekly Overview (customer/container OUT, IN, Net) in one report. | CHAT-SURFACED,CURRENT-DOC,CODE |
| BT-WEEK-003 | v1 | IMPLEMENTED-STATIC | This Week/Last Week shortcuts, configured Container Type filter and audited PDF/CSV follow selected view/order. | CURRENT-DOC,CODE |
| BT-HIST-001 | v1 | IMPLEMENTED-STATIC | Movement History supports inclusive date range, customer/container/direction/source filters, adjustment opt-in and quick ranges. | CURRENT-DOC,CODE |
| BT-STMT-001 | v1 | IMPLEMENTED-ACCEPTED | Customer Statement workflow available from both Customers and Reports using one shared implementation. | CHAT-SURFACED,CURRENT-DOC,CODE |
| BT-STMT-002 | v1 | IMPLEMENTED-ACCEPTED | Statement supports Generate PDF and Generate & Open; opened PDF is printable via Windows viewer; dates cannot exceed today. | CHAT-SURFACED,CURRENT-DOC,CODE |
| BT-STMT-003 | v1 | IMPLEMENTED-STATIC | Statement running balances reconcile opening, movement and closing positions by container. | CURRENT-DOC,HIST-BUILD,CODE |
| BT-MON-001 | v1 | IMPLEMENTED-ACCEPTED | Monthly Summary has selected month, This Month/Last Month, OUT/IN/Net and customer/container breakdown; user acceptance completed on v0.4.0-alpha.24.2.7. | CHAT-SURFACED,CURRENT-DOC,CODE,USER-ACCEPTED |
| BT-MON-002 | v1 | IMPLEMENTED-ACCEPTED | Monthly filters customer/container/source, optional adjustments, numeric sorting, audited PDF/CSV and activity-through-today semantics; user acceptance completed on v0.4.0-alpha.24.2.7. | CURRENT-DOC,CODE,USER-ACCEPTED |
| BT-PACK-001 | v1 | IMPLEMENTED-STATIC | Daily Print Pack generates one selected-date PDF: Outstanding Summary first, physical Movement Detail second. | CURRENT-DOC,CODE |
| BT-PACK-002 | v1 | IMPLEMENTED-STATIC | Daily Print Pack excludes Opening Adjustments from physical movement detail and blocks future dates. | CURRENT-DOC,CODE |
| BT-PACK-003 | v1 | IMPLEMENTED-STATIC | Daily Print Pack supports Generate PDF / Generate & Open and writes one `DAILY_PRINT_PACK_GENERATED` audit event. | CURRENT-DOC,CODE |
| BT-PACK-004 | v1 | PLANNED-V1 | Daily Print Pack requires real preview/print acceptance before reporting milestone closure. | CURRENT-DOC |

## Movement correction / reversal

| ID | Scope | Status | Requirement | Provenance |
|---|---|---|---|---|
| BT-CORR-001 | v1 | PLANNED-V1 | Controlled correction/reversal workflow for saved movements. | CHAT-SURFACED,CURRENT-DOC |
| BT-CORR-002 | v1 | PLANNED-V1 | Never silently edit/delete original saved movement. | CHAT-SURFACED,CURRENT-DOC |
| BT-CORR-003 | v1 | PLANNED-V1 | Preserve original + reversal/correction linkage, reason, actor/time and audit trail. | CURRENT-DOC |
| BT-CORR-004 | v1 | PLANNED-V1 | Define and enforce roles permitted to correct/reverse. | CURRENT-DOC |

## Email / SMS / customer communications

| ID | Scope | Status | Requirement | Provenance |
|---|---|---|---|---|
| BT-COMM-001 | v1 | IMPLEMENTED-STATIC | Customer model contains Email, Mobile, AllowEmailReminders, AllowSmsReminders and ReminderOptOut groundwork. | CURRENT-DOC,CODE |
| BT-COMM-002 | v1 | IMPLEMENTED-STATIC | ReminderDelivery persistence groundwork records channel/destination/status/provider response/outstanding snapshot. | CURRENT-DOC,CODE |
| BT-COMM-003 | v1 | PLANNED-V1 | Email provider direction is Google Workspace. | CHAT-SURFACED,CURRENT-DOC |
| BT-COMM-004 | v1 | PLANNED-V1 | SMS provider direction is Texto. | CHAT-SURFACED,CURRENT-DOC |
| BT-COMM-005 | v1 | PLANNED-V1 | Reminder rule direction: customers owing empty bins are automatically reminded by Friday or earlier, configurable where sensible. | CHAT-SURFACED,CURRENT-DOC |
| BT-COMM-006 | v1 | PLANNED-V1 | Manual send from Customer screen plus bulk/automatic reminder run. | CHAT-SURFACED,CURRENT-DOC |
| BT-COMM-007 | v1 | PLANNED-V1 | Email/SMS templates and secure Administrator provider/credential configuration. | CURRENT-DOC |
| BT-COMM-008 | v1 | PLANNED-V1 | Respect per-customer Email/SMS/Opt-out settings. | CHAT-SURFACED,CURRENT-DOC |
| BT-COMM-009 | v1 | PLANNED-V1 | Delivery history UI with Pending/Sent/Failed/Skipped lifecycle. | CURRENT-DOC |
| BT-COMM-010 | v1 | PLANNED-V1 | Retry/error handling must avoid duplicate sends and audit runs/sends. | CHAT-SURFACED,CURRENT-DOC |
| BT-COMM-011 | v1 | PLANNED-V1 | Decide whether statements are attached or linked in email reminders. | CHAT-SURFACED,CURRENT-DOC |

## Dashboard

| ID | Scope | Status | Requirement | Provenance |
|---|---|---|---|---|
| BT-DASH-001 | v1 | PLANNED-V1 | Mandatory design discussion before Dashboard implementation; do not jump directly to code. | CHAT-SURFACED,CURRENT-DOC |
| BT-DASH-002 | v1 | PLANNED-V1 | Validate headline KPIs and customer/container scope. | CURRENT-DOC |
| BT-DASH-003 | v1 | PLANNED-V1 | Useful attention/exception list with drill-through, not quantity threshold alone. | CHAT-SURFACED,CURRENT-DOC |
| BT-DASH-004 | v1 | PLANNED-V1 | Recent activity and by-container operational summaries. | CHAT-SURFACED,CURRENT-DOC |
| BT-DASH-005 | v1 | PLANNED-V1 | At least one useful chart/trend and customer/container comparisons. | CHAT-SURFACED,CURRENT-DOC |
| BT-DASH-006 | v1 | PLANNED-V1 | Discuss ageing/days-outstanding business rule before implementation. | CURRENT-DOC |
| BT-DASH-007 | v1 | PLANNED-V1 | Discuss forecasting hooks, predictive/ML readiness, anomaly/risk ideas and drill-through behavior. | CHAT-SURFACED,CURRENT-DOC |
| BT-DASH-008 | v1 | PLANNED-V1 | Design for laptop and large-monitor layouts/typical operational use. | CHAT-SURFACED,CURRENT-DOC |
| BT-DASH-009 | v1 | PLANNED-V1 | Dashboard milestone may experiment with alternative concepts before choosing implementation. | CHAT-SURFACED,CURRENT-DOC |

## Backup / recovery / deployment / production hardening

| ID | Scope | Status | Requirement | Provenance |
|---|---|---|---|---|
| BT-OPS-001 | v1 | PLANNED-V1 | User-facing production backup; developer database tools are not the production solution. | CURRENT-DOC |
| BT-OPS-002 | v1 | PLANNED-V1 | Validated Restore workflow with confirmation and recovery drill. | CURRENT-DOC |
| BT-OPS-003 | v1 | PLANNED-V1 | Scheduled automatic backups, configurable destination/retention and pre-upgrade backup. | CHAT-SURFACED,CURRENT-DOC |
| BT-OPS-004 | v1 | PLANNED-V1 | Detect/report corrupt or inaccessible SQLite database cleanly. | CURRENT-DOC |
| BT-OPS-005 | v1 | PLANNED-V1 | SQLite transaction/concurrency and integrity/migration audit. | CURRENT-DOC |
| BT-OPS-006 | v1 | PLANNED-V1 | Windows installer/package and upgrade path preserving database/configuration. | CURRENT-DOC |
| BT-OPS-007 | v1 | PLANNED-V1 | Versioned upgrade/rollback guidance and signing decision. | CURRENT-DOC |
| BT-OPS-008 | v1 | PLANNED-V1 | Full v1 fresh-install/database/import/balance/movement/correction/report/communications/branding/dashboard/backup/restart/upgrade acceptance. | CURRENT-DOC |
| BT-OPS-009 | v1 | PLANNED-V1 | v1.0 production release replaces daily Excel workflow after acceptance. | CURRENT-DOC |
| BT-OPS-010 | candidate | NEEDS-CONFIRMATION | Automatic application updates were mentioned in an early README but are not confirmed current v1 scope. | HIST-BUILD |

## Post-v1 product / commercial roadmap

| ID | Scope | Status | Requirement | Provenance |
|---|---|---|---|---|
| BT-PV1-001 | post-v1 | POST-V1 | Custom Report Designer: fields, filters, grouping, sorting, orientation/layout. | HIST-BUILD,CURRENT-DOC |
| BT-PV1-002 | post-v1 | POST-V1 | Legacy Excel report-template/layout import is separate from authoritative data import. | HIST-BUILD,CURRENT-DOC |
| BT-PV1-003 | post-v1 | POST-V1 | Customer web portal. | CHAT-SURFACED,CURRENT-DOC |
| BT-PV1-004 | post-v1 | POST-V1 | Barcode scanning. | CHAT-SURFACED,CURRENT-DOC |
| BT-PV1-005 | post-v1 | POST-V1 | Multiple depots. | CHAT-SURFACED,CURRENT-DOC |
| BT-PV1-006 | post-v1 | POST-V1 | iPhone application. | CURRENT-DOC |
| BT-PV1-007 | post-v1 | POST-V1 | Android application. | CURRENT-DOC |
| BT-PV1-008 | post-v1 | POST-V1 | Hosted/cloud or centrally managed deployment option. | CURRENT-DOC |
| BT-PV1-009 | post-v1 | POST-V1 | Licensing/activation and commercial support tooling. | CURRENT-DOC |
| BT-PV1-010 | post-v1 | POST-V1 | Product onboarding/import workflow for new businesses. | CURRENT-DOC |
| BT-PV1-011 | candidate | NEEDS-CONFIRMATION | Repository README screenshots before beta were an early maintenance/documentation idea; not confirmed current release scope. | HIST-BUILD |
| BT-PV1-012 | candidate | NEEDS-CONFIRMATION | Expanded developer documentation set (Architecture/DeveloperGuide/extension guidance) was an early maintenance idea. | HIST-BUILD |

## Register governance

- New requirements receive a permanent ID before implementation or roadmap scheduling.
- Removing or materially changing an ID requires an explicit reason in `docs/CHANGELOG.md` and `docs/ReconciliationReport.md` or equivalent decision record.
- `NEEDS-CONFIRMATION` items are **not** automatically v1 requirements; they exist specifically so recovered historical ideas cannot silently vanish or silently become commitments.
- `IMPLEMENTED-STATIC` means source presence was verified, not that Windows rendering/printing/operator behavior was personally exercised in the reconciliation environment.
- Human acceptance in `TEST-CHECKLIST.md` remains required where the source alone cannot establish behavior.

## Security hardening governance

| ID | Scope | Status | Requirement | Provenance |
|---|---|---|---|---|
| BT-SEC-008 | v1 | PLANNED-V1 | Dedicated Security, Data Integrity & Code Quality Hardening workstream executes immediately after Movement Correction/Reversal and before Branding/Communications. | CHAT-SURFACED,EXTERNAL-AUDIT |
| BT-SEC-009 | v1 | IMPLEMENTED-STATIC | `docs/SecurityHardeningRegister.md` permanently tracks every external audit finding BT-SH-001..BT-SH-050; findings cannot silently disappear or be renumbered. | CHAT-SURFACED,EXTERNAL-AUDIT |
| BT-SEC-010 | v1 | IMPLEMENTED-STATIC | Per-build audit hard-gates presence/completeness/dispositions of the security finding register and protects roadmap ordering. | CHAT-SURFACED,CODE |
| BT-SEC-011 | v1 | PLANNED-V1 | v1.0 release is prohibited while any accepted/review-v1 security finding remains unresolved; fixed findings require source/test evidence. | CHAT-SURFACED,EXTERNAL-AUDIT |
