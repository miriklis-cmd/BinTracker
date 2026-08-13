# Alpha 7.2 Test Checklist

- [ ] Build-BinTracker.bat passes
- [ ] Click a draft line: fields populate
- [ ] Right-side customer position changes to clicked customer
- [ ] Add to Batch changes to Update Line
- [ ] Change Blue Bin to Small Bin and Update Line
- [ ] Line count stays the same
- [ ] With Draft recalculates
- [ ] Esc exits edit mode
- [ ] Remove recalculates preview
- [ ] Add/Remove/Clear button captions align
- [ ] Administrator is fully visible in Users Role column
- [ ] Customer Recent Movement History fits better on laptop

## Alpha 7.2.3 Customer layout check
- [ ] Open Customers on the laptop
- [ ] Recent Movement History bottom border is visible above the status bar
- [ ] Movement rows can be viewed without the panel disappearing below the window
- [ ] Current Position remains usable
- [ ] Resizing the window gives extra height to Recent Movement History

## Alpha 7.2.4 Customer screen
- [ ] No large blank white band below Save Customer / Deactivate / Customer Statement
- [ ] Current Position is taller than before
- [ ] Recent Movement History is taller than before
- [ ] Bottom of Recent Movement History remains visible above the status bar
- [ ] Resizing the window distributes extra height between both grids

## Alpha 7.2.6
- [ ] Customer action buttons are followed immediately by Current Position; no large white band remains
- [ ] Current Position has usable height
- [ ] Recent Movement History remains large
- [ ] Full `09/08/2026` date is readable
- [ ] Full `IN (Returned)` and `OUT (Taken)` are readable
- [ ] `Lawrence` fits in Entered By
- [ ] Logout appears logically in the top-right signed-in area
- [ ] Logout -> Yes returns to Login screen
- [ ] Login again without restarting the application
- [ ] If a Batch Entry draft exists, it is still present after logout/login
- [ ] Closing the main window with X still exits normally

## Alpha 7.2.7
- [ ] No blank band between customer buttons and Current Position
- [ ] 09/08/2026 is fully readable
- [ ] IN (Returned) is fully readable
- [ ] OUT (Taken) is fully readable
- [ ] Lawrence fits in Entered By

## Alpha 7.2.8
- [ ] Dashboard title is fully visible with no top/bottom clipping
- [ ] Customers title is fully visible with no top/bottom clipping
- [ ] Batch Entry / Reports / Settings titles are also clean
- [ ] Dashboard nav icon displays
- [ ] Customers nav icon displays
- [ ] Batch Entry nav icon displays
- [ ] Single Entry nav icon displays
- [ ] Reports nav icon displays
- [ ] Settings nav icon displays
- [ ] Clicking either icon or nav text opens the correct page
- [ ] Navigation still looks correct on the laptop DPI/scaling setting

## Alpha 7.2.9
- [ ] All six left-menu icons appear consistently
- [ ] Logout button displays its icon
- [ ] Login password eye reveals and re-hides password
- [ ] Change Password eyes work on all password fields
- [ ] Add User temporary-password eye works
- [ ] Reset Password eyes work for password and confirmation
- [ ] First-run Administrator eyes work if tested on a fresh database
- [ ] Passwords are always masked initially

## Alpha 7.2.10
- [ ] Login eye looks integrated with the password field
- [ ] No separate box/button look around the eye
- [ ] Eye still toggles password visibility correctly
- [ ] Logout icon and full `Logout` text are visible
- [ ] Logout icon is vertically centred
- [ ] Settings icon reads clearly as a cog/gear

## Alpha 7.2.13
- [ ] Login displays a complete eye icon, not a small line
- [ ] Eye toggles visibility correctly
- [ ] Eye remains visually inside the password field
- [ ] Logout displays a complete door/arrow icon
- [ ] Full `Logout` caption is visible
- [ ] Logout icon and caption are vertically centred
- [ ] Logout returns to Login correctly
- [ ] Left navigation icons are unchanged

## Alpha 8.0 — Single Entry
- [ ] Open Single Entry: real screen appears, not placeholder text
- [ ] Customer code autocomplete works
- [ ] Invalid customer is rejected
- [ ] Quantity starts blank
- [ ] Returned preview changes `After Save` correctly
- [ ] Taken preview changes `After Save` correctly
- [ ] Save confirmation shows correct customer/direction/container/quantity/date
- [ ] Save movement succeeds
- [ ] Position refreshes immediately after save
- [ ] Quantity/reference/notes clear after save
- [ ] Dashboard totals update when Dashboard is reopened
- [ ] Customer recent movement history includes the manual movement
- [ ] Audit Trail contains MOVEMENT_RECORDED
- [ ] Viewer cannot save a Single Entry movement
- [ ] Ctrl+Enter saves

## Alpha 8.0.1 — Customer action buttons
- [ ] Save Customer is visible
- [ ] Deactivate / Reactivate is visible
- [ ] Customer Statement is visible
- [ ] Active/Inactive customer status is visible
- [ ] Current Position remains usable
- [ ] Recent Movement History remains visible and scrollable
- [ ] No large blank band returns between Customer details and Current Position

## Alpha 8.0.2 — Single Entry polish
- [ ] Resolved customer summary aligns with the input controls
- [ ] No `Ready: customer` message appears after customer lookup
- [ ] Successful save still shows meaningful confirmation/status feedback

## Alpha 8.0.3 — Single Entry reset
- [ ] Save a valid Single Entry movement
- [ ] Customer code clears
- [ ] Customer summary clears
- [ ] Container resets to first type
- [ ] Quantity returns to blank
- [ ] Reference clears
- [ ] Notes clears
- [ ] Direction resets to Returned (IN)
- [ ] Date resets to today
- [ ] Customer-position preview clears
- [ ] Focus returns to Customer code
- [ ] Previous save confirmation remains visible

## Alpha 9.0 — Market Floor Sheet
- [ ] Reports screen opens
- [ ] Market Floor Sheet can be generated for today
- [ ] PDF has exactly 2 pages
- [ ] Page 1: Account owing appears only in first two columns
- [ ] Page 1: Cash owing appears only in right column
- [ ] Page 1: credits appear below Cash owing
- [ ] Page 1: CHEP/LOSCAM/pallet balances appear in special block
- [ ] Page 1 fits one A4 landscape page
- [ ] Page 2: Account customers are left
- [ ] Page 2: Cash customers are right
- [ ] Page 2: Out equals selected day's OUT movements
- [ ] Page 2: In equals selected day's IN movements
- [ ] Page 2: B/Fwd equals position before selected day
- [ ] Page 2: Total = B/Fwd + Out - In
- [ ] Negative Total prints as `x CREDIT`
- [ ] Historic date can be regenerated
- [ ] Audit Trail records MARKET_FLOOR_REPORT_GENERATED

## Alpha 9.0.1 — Market Floor portrait print
- [ ] `Generate & Open` caption is fully visible
- [ ] PDF page 1 is A4 portrait
- [ ] PDF page 2 is A4 portrait
- [ ] Page 1 remains exactly one page
- [ ] Page 2 remains exactly one page
- [ ] Front account/cash/credit sections remain readable
- [ ] Reverse Account and Cash columns remain readable
- [ ] Duplex printing produces front + reverse on one physical A4 sheet

## v0.3.0-alpha.1 — Container Types
- [ ] Settings → Container Types opens for Administrator
- [ ] Existing Blue/Small/Yellow/Bulk/CHEP records remain present after migration
- [ ] Existing movements/balances are unchanged
- [ ] Add a new container type
- [ ] Duplicate name is rejected
- [ ] Duplicate short code is rejected
- [ ] Rename a container and confirm movement history remains linked
- [ ] Change display order and confirm Batch/Single Entry dropdown ordering changes
- [ ] Deactivate type and confirm it disappears from new-entry dropdowns
- [ ] Reactivate type and confirm it returns
- [ ] Mark/unmark Special Floor Report Container
- [ ] Usage statistics populate
- [ ] Audit Trail records create/update/deactivate/reactivate actions

## v0.3.0-alpha.2
- [ ] Build succeeds with zero warnings
- [ ] All unit tests pass
- [ ] All integration tests pass
- [ ] Existing database opens/upgrades normally
- [ ] Settings > Container Types still opens
- [ ] CHEP remains marked Special Floor Report Container

## v0.3.0-alpha.3 — Business Information
- [ ] Build succeeds with zero warnings
- [ ] All tests pass
- [ ] Settings > Business Information opens for Administrator
- [ ] Business Information saves and reloads
- [ ] Empty fields are accepted
- [ ] Audit Trail records BUSINESS_INFORMATION_UPDATED
- [ ] Customer Statement uses Default Report Header when configured
- [ ] Market Floor Sheet uses Default Report Header when configured
- [ ] Trading Name is used when Default Report Header is blank
- [ ] BinTracker fallback is used when all identity fields are blank

## v0.3.0-alpha.4 — Settings UI polish
- [ ] Settings administration buttons have matching vertical text alignment
- [ ] Business Information caption is fully visible
- [ ] Business Information opens in its own window
- [ ] Save and Close are visible at 100% Windows scaling
- [ ] Save and Close are visible/scrollable at 125% and 150% scaling
- [ ] Business Information saves successfully
- [ ] Visual Studio shows no CA1859 messages for MainForm.cs lines previously reported

## v0.4.0-alpha.1 — Excel Import Analysis
- [ ] Build succeeds with zero warnings
- [ ] All unit/integration tests pass
- [ ] Settings > Import Excel opens
- [ ] `.xlsm` workbook can be selected
- [ ] `.xlsx` workbook can be selected
- [ ] Analyse does not modify Customers
- [ ] Analyse does not modify Movements
- [ ] Worksheet list shows expected Excel sheets
- [ ] Buyer columns are detected
- [ ] Account candidates are identified from account source sheets
- [ ] Cash/COD candidates are identified from cash source sheets
- [ ] Duplicate candidate warning is understandable
- [ ] Audit Trail records IMPORT_WORKBOOK_ANALYSED
- [ ] Import execution button remains disabled in this analysis-only build

## v0.4.0-alpha.2 — Import build regression
- [ ] Build succeeds with zero warnings
- [ ] All automated tests pass
- [ ] Settings > Import Excel opens
- [ ] Workbook analysis still lists worksheet names
- [ ] Buyer source-cell addresses are populated
- [ ] No database data is changed by Analyse

## v0.4.0-alpha.3 — Import Wizard layout
- [ ] Build succeeds with zero warnings
- [ ] All automated tests pass
- [ ] Import Excel opens in its own resizable window
- [ ] Title and explanatory text are fully visible
- [ ] Browse button is visible
- [ ] Analyse button is visible
- [ ] Workbook path field is visible and selectable
- [ ] Analyse / Map / Review / Import step indicator is fully visible
- [ ] Workbook structure table headers are fully readable
- [ ] Customer candidate table headers are fully readable
- [ ] Read-only notice is fully visible
- [ ] Analyse button works after selecting an .xlsm workbook
- [ ] Analyse button works after selecting an .xlsx workbook
- [ ] Next remains disabled in this analysis-only build
- [ ] Cancel closes the wizard
- [ ] Controls remain usable at 100%, 125% and 150% Windows display scaling
- [ ] Window can be resized smaller and scrollbars appear instead of clipping controls
- [ ] No customer/movement data is modified by Analyse

## v0.4.0-alpha.4 — Import Wizard polish / snapshot analysis
- [ ] Build succeeds with zero warnings
- [ ] All automated tests pass
- [ ] Step numbers are circles, not squares
- [ ] Horizontal line connects steps 1-4
- [ ] Only one Analyse button is present
- [ ] View all worksheets button is visible
- [ ] View all worksheets shows every detected worksheet
- [ ] Next starts disabled
- [ ] Next enables after successful Analyse
- [ ] Analysis summary shows worksheet/customer/snapshot counts
- [ ] Reverse-side style Buyer/Out/In/B-Fwd/Total rows are detected
- [ ] B/Fwd + OUT - IN calculates expected Total
- [ ] CREDIT values remain negative internally
- [ ] Excel Total mismatch is detectable
- [ ] Analyse still performs no database import/write

## v0.4.0-alpha.5 — Import Wizard clipping fixes
- [ ] Build succeeds with zero warnings
- [ ] All automated tests pass
- [ ] Wizard progress indicator subtitles are fully visible
- [ ] Progress circles and connecting line are fully visible
- [ ] View all worksheets caption is fully visible
- [ ] View all worksheets opens correctly
- [ ] Workbook analysed success text is fully visible
- [ ] Analysis details show Worksheets / Unique customers / Occurrences / B/Fwd-daily rows
- [ ] Long warnings wrap/scroll instead of being cut off
- [ ] Candidate section is visible without excessive scrolling at 100% scaling
- [ ] Layout remains usable at 125% and 150% Windows scaling
- [ ] Unique customer count is case-insensitive
- [ ] Analyse still makes no database changes

## v0.4.0-alpha.6 — Centralised versioning
- [ ] Build-BinTracker.bat starts with `Version : v0.4.0-alpha.6`
- [ ] Build succeeds with zero warnings
- [ ] All automated tests pass
- [ ] Successful build banner shows `BinTracker v0.4.0-alpha.6`
- [ ] Failed build banner also shows the current version
- [ ] BinTracker status bar shows `v0.4.0-alpha.6`
- [ ] MainForm.cs contains no hard-coded release version
- [ ] Directory.Build.props is the release version source of truth

## v0.4.0-alpha.7 — Duplicate diagnostics / Business Information polish
- [ ] Build-BinTracker.bat reports v0.4.0-alpha.7
- [ ] Build succeeds with zero warnings
- [ ] All automated tests pass
- [ ] Import duplicate warning is concise and fully visible
- [ ] View duplicates... button is fully visible
- [ ] View duplicates dialog opens after analysis
- [ ] Duplicate dialog shows Customer / Occurrences / Worksheet / Cell / Type / Classification
- [ ] Duplicate dialog explains report/validation repetitions are handled in Map
- [ ] Business Information Save and Close buttons have identical vertical position/height
- [ ] Save and Close remain aligned at 100%, 125% and 150% scaling
- [ ] Address field comfortably fits a multi-line postal address
- [ ] Business Information still saves/reloads correctly
- [ ] KNOWN-ISSUES.md reflects current v0.4.0-alpha.7 status
- [ ] TECH-DEBT.md contains engineering cleanup rather than active defects

## v0.4.0-alpha.8 — Multi-page Import Wizard / Map
- [ ] Build-BinTracker.bat reports v0.4.0-alpha.8
- [ ] Build succeeds with zero warnings
- [ ] All automated tests pass
- [ ] Analyse page contains workbook selection and summary only
- [ ] Analyse page does not show the large worksheet/customer grids
- [ ] Next remains disabled until Analyse succeeds
- [ ] Next moves to Map after successful Analyse
- [ ] Wizard progress indicator highlights Map on page 2
- [ ] Map worksheet grid shows Classification dropdown
- [ ] Update Account defaults to Source
- [ ] Update Cash defaults to Source
- [ ] CREDITS defaults to Validation
- [ ] Print This defaults to Report
- [ ] Print this on reverse side defaults to Report
- [ ] Summary defaults to Ignore
- [ ] Changing classification refreshes Source customer preview immediately
- [ ] Customer preview excludes Validation/Report/Ignore sheets
- [ ] Back returns to Analyse without losing the analysis
- [ ] All Worksheets `Columns` header is fully visible
- [ ] All Worksheets `Candidates` header is fully visible
- [ ] Duplicate dialog `Occurrences` header is fully visible
- [ ] Review remains disabled/not implemented
- [ ] No customer, balance or movement data is written by Analyse or Map

## v0.4.0-alpha.9 — Import / Business Information UI regression
- [ ] Build-BinTracker.bat reports v0.4.0-alpha.9
- [ ] Build succeeds with zero warnings
- [ ] All automated tests pass
- [ ] Analyse result remains fully visible after workbook analysis
- [ ] No text is hidden behind the Analyse page footer
- [ ] `Preview changes` displays the full lowercase `g`
- [ ] Step 1-4 subtitles are fully readable at 100%, 125% and 150% scaling
- [ ] Map `Suggested reason` values are fully readable or wrap within the row
- [ ] Default mapping reasons do not rely on tooltips to be understood
- [ ] Business Information Save button is normal fixed width
- [ ] Business Information Close button is normal fixed width and matches Save
- [ ] Save and Close remain level at 100%, 125% and 150% scaling
- [ ] KNOWN-ISSUES.md and TECH-DEBT.md reviewed for v0.4.0-alpha.9

## v0.4.0-alpha.10 — Import Review
- [ ] Build-BinTracker.bat reports v0.4.0-alpha.10
- [ ] Build succeeds with zero warnings
- [ ] All automated tests pass
- [ ] Map `Candidates` header is fully visible
- [ ] Duplicate dialog `Occurrences` header is fully visible
- [ ] Review > opens step 3
- [ ] Wizard progress indicator highlights Review
- [ ] Review uses only Source-sheet customers
- [ ] Existing customer code match is case-insensitive
- [ ] Existing matching Account customer shows `Existing — match`
- [ ] Existing matching Cash/COD customer shows `Existing — match`
- [ ] Unknown customer code shows `New candidate`
- [ ] Existing code with different detected type shows `TYPE MISMATCH`
- [ ] Same code detected as both Account and Cash/COD Source data shows `SOURCE CONFLICT`
- [ ] Review summary reports Source snapshot rows
- [ ] Review summary reports B/Fwd/OUT/IN Total mismatches
- [ ] Back returns from Review to Map
- [ ] Import > remains disabled
- [ ] Analyse / Map / Review make no customer, movement or balance database changes
- [ ] KNOWN-ISSUES.md and TECH-DEBT.md reviewed for v0.4.0-alpha.10


## v0.4.0-alpha.10.1 — Review test build fix
- [ ] Build-BinTracker.bat reports v0.4.0-alpha.10.1
- [ ] BinTracker.UnitTests compiles
- [ ] All unit tests pass
- [ ] All integration tests pass
- [ ] Full solution builds with zero warnings
- [ ] Review page still opens and behaves as in v0.4.0-alpha.10
- [ ] KNOWN-ISSUES.md and TECH-DEBT.md reviewed


## v0.4.0-alpha.10.2 — Map classification state
- [ ] Build-BinTracker.bat reports v0.4.0-alpha.10.2
- [ ] Build succeeds with zero warnings
- [ ] All automated tests pass
- [ ] Analyse workbook and continue to Map
- [ ] Default worksheet classifications display visibly in every row
- [ ] Change one or more classifications
- [ ] Changed classification remains visibly selected after clicking another cell
- [ ] Go Map → Review → Back
- [ ] All prior classification selections are still visible
- [ ] Go Map → Analyse → Next
- [ ] All prior classification selections are still visible
- [ ] Source sheet/customer counts still match selected Source mappings
- [ ] No customer/movement/balance database data is written
- [ ] KNOWN-ISSUES.md and TECH-DEBT.md reviewed


## v0.4.0-alpha.10.3 — Legacy Buyer prefix handling
- [ ] Build-BinTracker.bat reports v0.4.0-alpha.10.3
- [ ] Build succeeds with zero warnings
- [ ] All automated tests pass
- [ ] `Clamms` and `(Bulk) Clamms` appear as one Review customer
- [ ] Existing `CLAMMS` customer matches both variants case-insensitively
- [ ] Review shows container hint `Bulk`
- [ ] Review shows legacy variant `(Bulk) Clamms`
- [ ] `(Y) Barwon` normalises to customer `Barwon` with hint `Y`
- [ ] Review status `Existing — match` is fully visible
- [ ] Snapshot candidates preserve container hints
- [ ] No database writes occur
- [ ] KNOWN-ISSUES.md and TECH-DEBT.md reviewed

## v0.4.0-alpha.11 — Normalized customer matching
- [ ] Build-BinTracker.bat reports v0.4.0-alpha.11
- [ ] Build succeeds with zero warnings
- [ ] All automated tests pass
- [ ] `S & J` and `(Bulk) S&J` appear under one Review customer
- [ ] Existing `S & J` customer is matched from imported `S&J`
- [ ] Match reason says `Normalized code` where appropriate
- [ ] Exact customer-code matches still take priority
- [ ] Ambiguous normalized names are not auto-matched
- [ ] `(Y)` displays as `Yellow Bin` in Review
- [ ] `(Bulk)` displays as `Bulk Bin` in Review
- [ ] Container short-code hints resolve where unambiguous
- [ ] Review shows `Existing customer` header in full
- [ ] Review shows `Existing type` header in full
- [ ] Review status is fully readable
- [ ] Review grid has no horizontal scrollbar at normal wizard width / 100% DPI
- [ ] Legacy variant text remains inspectable via row tooltip
- [ ] No import/database writes occur
- [ ] KNOWN-ISSUES.md and TECH-DEBT.md reviewed

## v0.4.0-alpha.12 — Developer Database Tools / re-import safety
- [ ] Build-BinTracker.bat reports v0.4.0-alpha.12
- [ ] Build succeeds with zero warnings
- [ ] All automated tests pass
- [ ] Settings shows Developer Tools for Administrator
- [ ] Developer Database window shows active database path
- [ ] Backup Database creates a usable .db copy
- [ ] Load Database validates the selected file
- [ ] Loading a non-BinTracker SQLite file is rejected
- [ ] Load Database automatically creates a current-state backup
- [ ] Load Database restarts BinTracker
- [ ] After restart the selected database is active
- [ ] Start Fresh Test Database warns before proceeding
- [ ] Start Fresh automatically backs up the current database
- [ ] Start Fresh restarts BinTracker
- [ ] Fresh restart presents first-run Administrator setup
- [ ] A saved developer backup can subsequently be loaded again
- [ ] No live SQLite file replacement occurs while the app is running
- [ ] `docs/ReimportSafety.md` documents exact and changed-workbook re-import handling
- [ ] Re-import protection remains a blocker before Import is enabled
- [ ] KNOWN-ISSUES.md and TECH-DEBT.md reviewed

## v0.4.0-alpha.13 — Balance reconciliation
- [ ] Build-BinTracker.bat reports v0.4.0-alpha.13
- [ ] Build succeeds with zero warnings
- [ ] All automated tests pass
- [ ] Review has Customer matches and Balance reconciliation tabs
- [ ] Current 12 / B/Fwd 20 / OUT 5 / IN 3 plans +8 opening adjustment
- [ ] Same row projects to 22, not 34
- [ ] Fresh current 0 plans opening adjustment equal to B/Fwd
- [ ] OUT and IN remain separately visible
- [ ] Missing B/Fwd / unresolved container / Excel mismatch block the row
- [ ] No database writes occur
- [ ] KNOWN-ISSUES.md and TECH-DEBT.md reviewed

## v0.4.0-alpha.13.1 — WinForms tooltip build fix
- [ ] Build-BinTracker.bat reports v0.4.0-alpha.13.1
- [ ] BinTracker.WinForms compiles
- [ ] Full solution builds with zero warnings
- [ ] All unit tests pass
- [ ] All integration tests pass
- [ ] Review page still opens
- [ ] Hovering Review row cells can show legacy variant tooltip
- [ ] Balance reconciliation tab behaves as in v0.4.0-alpha.13
- [ ] KNOWN-ISSUES.md and TECH-DEBT.md reviewed


## v0.4.0-alpha.13.2 — Normalized Review grouping
- [ ] Build-BinTracker.bat reports v0.4.0-alpha.13.2
- [ ] Full solution builds with zero warnings
- [ ] All automated tests pass
- [ ] `S & J` and `(Bulk) S&J` produce one Review customer row
- [ ] `S&J`, `S & J` and `S  &  J` produce one Review customer row
- [ ] Existing customer `S & J` is preferred as consolidated display code
- [ ] Bulk container hint remains attached to the consolidated row
- [ ] Legacy variant `(Bulk) S&J` remains visible in tooltip data
- [ ] Existing count is not doubled by normalized variants
- [ ] No database writes occur
- [ ] KNOWN-ISSUES.md and TECH-DEBT.md reviewed

## v0.4.0-alpha.13.3 — BalanceService SQLite crash
- [ ] Build-BinTracker.bat reports v0.4.0-alpha.13.3
- [ ] Full solution builds with zero warnings
- [ ] All unit tests pass
- [ ] All integration tests pass
- [ ] BalanceService SQLite regression test passes
- [ ] Open Import Wizard and Analyse workbook
- [ ] Continue Map -> Review without an EF Core translation exception
- [ ] Review Customer matches tab loads
- [ ] Review Balance reconciliation tab loads
- [ ] Current BinTracker balances shown in reconciliation are correct
- [ ] Empty movement database returns an empty balance list without error
- [ ] No import/database write occurs in Review
- [ ] KNOWN-ISSUES.md and TECH-DEBT.md reviewed

## v0.4.0-alpha.13.4 — BalanceService lookup regression
- [ ] Build-BinTracker.bat reports v0.4.0-alpha.13.4
- [ ] Full solution builds with zero warnings
- [ ] All unit tests pass
- [ ] All integration tests pass
- [ ] BalanceService SQLite aggregate regression test passes
- [ ] Unrelated customers with no movements are excluded from balance results
- [ ] Import Wizard reaches Review without crashing
- [ ] Balance reconciliation tab loads current balances
- [ ] No database writes occur in Review
- [ ] KNOWN-ISSUES.md and TECH-DEBT.md reviewed

## v0.4.0-alpha.13.5 — Review / Developer Tools layout
- [ ] Build-BinTracker.bat reports v0.4.0-alpha.13.5
- [ ] Full solution builds with zero warnings
- [ ] All automated tests pass
- [ ] Review Customer / Code header is fully visible
- [ ] Review Container(s) header and values are fully visible/wrap
- [ ] Review Existing customer header and values are fully visible/wrap
- [ ] Review Existing type header and values are fully visible/wrap
- [ ] Review status header and values are fully visible/wrap
- [ ] Match reason header and values are fully visible/wrap
- [ ] Source worksheet header and values are fully visible/wrap
- [ ] Review row height grows for wrapped values
- [ ] Developer Database Tools opens at a usable size
- [ ] Backup Database action is fully visible
- [ ] Load Database action is fully visible
- [ ] Start Fresh Test Database action is fully visible
- [ ] Developer Database Tools can scroll vertically if DPI/content requires it
- [ ] Close button is visible and normal sized
- [ ] KNOWN-ISSUES.md and TECH-DEBT.md reviewed

## v0.4.0-alpha.13.6 — width/layout pass
- [ ] Build-BinTracker.bat reports v0.4.0-alpha.13.6
- [ ] Full solution builds with zero warnings
- [ ] All automated tests pass
- [ ] Customer Review has no horizontal scrollbar at 100% DPI
- [ ] Container(s), Existing customer/type, Review status, Match reason and Source values show fully or wrap
- [ ] Balance Reconciliation has no normal horizontal scrollbar at 100% DPI
- [ ] Developer Database Tools opens at larger size
- [ ] Backup, Load and Start Fresh actions are fully visible
- [ ] Close remains visible
- [ ] KNOWN-ISSUES.md and TECH-DEBT.md reviewed

## v0.4.0-alpha.14 — legacy container inference
- [ ] Build-BinTracker.bat reports v0.4.0-alpha.14
- [ ] Full solution builds with zero warnings
- [ ] All automated tests pass
- [ ] Customer with no brackets resolves to Blue Bin
- [ ] `(Y)` resolves to Yellow Bin
- [ ] `(Bulk)` resolves to Bulk Bin
- [ ] Known Container Type short code resolves correctly
- [ ] `(Tub)` remains unresolved when Tub is not configured
- [ ] Unknown explicit token is shown in Review
- [ ] Unknown explicit token blocks Balance Reconciliation / Import readiness
- [ ] Missing token is not treated as an error
- [ ] Balance Reconciliation explains the container rule used
- [ ] No database writes occur
- [ ] KNOWN-ISSUES.md and TECH-DEBT.md reviewed

## v0.4.0-alpha.15 — manual container mapping
- [ ] Build-BinTracker.bat reports v0.4.0-alpha.15
- [ ] Full solution builds with zero warnings
- [ ] All automated tests pass
- [ ] `(Tub)` remains blocked before mapping
- [ ] Review exposes `Map container tokens...`
- [ ] Mapping dialog lists each unresolved token once
- [ ] Token can be mapped to an existing Container Type
- [ ] `Manage Container Types...` opens Container Type master data
- [ ] Newly created Container Type appears after returning
- [ ] Applied mapping survives Review refresh / Back-Forward within the same wizard
- [ ] Mapping can unblock Balance Reconciliation
- [ ] Unknown unmapped tokens remain blocked
- [ ] New workbook analysis clears session token mappings
- [ ] No movement/import writes occur
- [ ] KNOWN-ISSUES.md and TECH-DEBT.md reviewed

## v0.4.0-alpha.15.1 — manual mapping build fix
- [ ] Build-BinTracker.bat reports v0.4.0-alpha.15.1
- [ ] BinTracker.WinForms compiles
- [ ] Full solution builds with zero warnings
- [ ] All automated tests pass
- [ ] Import Wizard opens
- [ ] Review opens
- [ ] `Map container tokens...` opens
- [ ] Manual token mappings survive Review refresh / Back-Forward navigation
- [ ] Starting a new workbook analysis clears session token mappings
- [ ] KNOWN-ISSUES.md and TECH-DEBT.md reviewed

## v0.4.0-alpha.16 — customer decisions
- [ ] Build reports v0.4.0-alpha.16
- [ ] Full solution builds with zero warnings
- [ ] All tests pass
- [ ] Confirm new customers opens
- [ ] Proposed name editable
- [ ] Create / Skip work
- [ ] Bulk selected/all actions work
- [ ] Decisions survive Review and Back/Forward
- [ ] Unconfirmed blocks reconciliation
- [ ] Skip excludes customer without blocking
- [ ] Create includes customer in reconciliation
- [ ] New workbook clears decisions
- [ ] No database writes occur

## v0.4.0-alpha.16.1 — customer decision compile fix
- [ ] Build-BinTracker.bat reports v0.4.0-alpha.16.1
- [ ] BinTracker.Services compiles
- [ ] Full solution builds with zero warnings
- [ ] All automated tests pass
- [ ] Missing decision remains blocked
- [ ] Unconfirmed decision remains blocked
- [ ] Skip excludes customer without blocking
- [ ] Create remains eligible for reconciliation
- [ ] KNOWN-ISSUES.md and TECH-DEBT.md reviewed

## v0.4.0-alpha.16.2 — fresh database decision test
- [ ] Build-BinTracker.bat reports v0.4.0-alpha.16.2
- [ ] Full solution builds with zero warnings
- [ ] All automated tests pass
- [ ] Fresh new customer + Create decision calculates B/Fwd opening adjustment
- [ ] Fresh new customer without decision remains blocked
- [ ] Skip remains excluded
- [ ] Existing-customer reconciliation is unchanged
- [ ] KNOWN-ISSUES.md and TECH-DEBT.md reviewed

## v0.4.0-alpha.16.3 — decision status / docs consolidation
- [ ] Build-BinTracker.bat reports v0.4.0-alpha.16.3
- [ ] Full solution builds with zero warnings
- [ ] All automated tests pass
- [ ] Fresh new customer + Create decision status is Ready
- [ ] Fresh new customer without decision remains blocked
- [ ] Unconfirmed remains blocked
- [ ] Skip remains excluded
- [ ] `docs/RELEASE-NOTES.md` exists
- [ ] Old per-alpha `ReleaseNotes-v*.md` files are removed
- [ ] `docs/CHANGELOG.md` retains version history
- [ ] KNOWN-ISSUES.md and TECH-DEBT.md reviewed

## v0.4.0-alpha.17 — existing matches / UI fixes
- [ ] Build-BinTracker.bat reports v0.4.0-alpha.17
- [ ] Full solution builds with zero warnings
- [ ] All automated tests pass
- [ ] Developer restart dialog shows real line breaks, not literal `\n`
- [ ] Selected → Create is fully visible
- [ ] Selected → Skip is fully visible
- [ ] Review exposes Confirm existing matches...
- [ ] Existing match starts Unconfirmed
- [ ] Selected → Accept works
- [ ] All → Accept works
- [ ] Match can be overridden to another active customer
- [ ] Override survives Review refresh / Back-Forward
- [ ] Unconfirmed existing match blocks reconciliation
- [ ] Confirmed existing match can become Ready
- [ ] Container mapping does not erase customer decisions
- [ ] KNOWN-ISSUES.md and TECH-DEBT.md reviewed

## v0.4.0-alpha.18 — Step 4 provenance/preflight
- [ ] Build-BinTracker.bat reports v0.4.0-alpha.18
- [ ] Full solution builds with zero warnings
- [ ] All automated tests pass
- [ ] Existing matched customer appears in first row even if inactive
- [ ] Inactive customer is labelled `(inactive)`
- [ ] Decision shows `Accept match`, not `AcceptMatch`
- [ ] Decision shows `Override match`, not `OverrideMatch`
- [ ] Review → Import opens Step 4
- [ ] Step 4 shows workbook SHA-256
- [ ] Step 4 shows file size and modified timestamp
- [ ] ImportRuns schema migration v9 applies to existing database
- [ ] Exact previously completed workbook is detected from SHA-256
- [ ] No customer or movement writes occur yet
- [ ] KNOWN-ISSUES.md and TECH-DEBT.md reviewed

## v0.4.0-alpha.18.1 — Review readiness/layout
- [ ] Build-BinTracker.bat reports v0.4.0-alpha.18.1
- [ ] Full solution builds with zero warnings
- [ ] All automated tests pass
- [ ] Balance Reconciliation grid is fully visible at 100% DPI
- [ ] Bottom rows/status/container rule are not clipped
- [ ] Review with 0 new-customer unconfirmed, 0 existing-match unconfirmed, 0 container mappings, and no reconciliation blockers enables Import
- [ ] Import button advances to Step 4
- [ ] Any blocker disables Import
- [ ] Step 4 still performs SHA-256 provenance preflight
- [ ] No database writes occur yet
- [ ] KNOWN-ISSUES.md and TECH-DEBT.md reviewed

## v0.4.0-alpha.18.2 — Review readiness compile fix
- [ ] Build-BinTracker.bat reports v0.4.0-alpha.18.2
- [ ] BinTracker.WinForms compiles
- [ ] Full solution builds with zero warnings
- [ ] All automated tests pass
- [ ] Fully resolved Review enables Import
- [ ] Import advances to Step 4
- [ ] Any Review blocker disables Import
- [ ] Balance Reconciliation remains fully visible
- [ ] KNOWN-ISSUES.md and TECH-DEBT.md reviewed

## v0.4.0-alpha.18.3 — Review fill-layout fix
- [ ] Build-BinTracker.bat reports v0.4.0-alpha.18.3
- [ ] Full solution builds with zero warnings
- [ ] All automated tests pass
- [ ] Step 3 Customer Matches shows multiple visible data rows
- [ ] Step 3 Balance Reconciliation shows multiple visible data rows
- [ ] Balance Reconciliation grid uses the available height down to the wizard footer
- [ ] Switching between Customer Matches and Balance Reconciliation does not collapse either grid
- [ ] Resize wizard taller: Review grid grows
- [ ] Resize wizard shorter to minimum: Review grid remains usable with vertical scrolling
- [ ] Import remains enabled when Review is fully resolved
- [ ] Import advances to Step 4
- [ ] KNOWN-ISSUES.md and TECH-DEBT.md reviewed

## v0.4.0-alpha.18.4 — reconciliation/math/file-lock hardening
- [ ] Build-BinTracker.bat reports v0.4.0-alpha.18.4
- [ ] Full solution builds with zero warnings
- [ ] All automated tests pass
- [ ] Balance Reconciliation shows several rows in normal wizard size
- [ ] View balance reconciliation larger opens a large read-only grid
- [ ] Zahos example: Current 0, B/Fwd 5, OUT 10, IN 15 => Adjustment +5, Projected 0
- [ ] Existing Current 20, B/Fwd 12, OUT 4, IN 1 => Adjustment -8, Projected 15
- [ ] Formula remains B/Fwd + OUT - IN for Excel target
- [ ] Open workbook in Excel before Step 4: BinTracker shows warning and does not crash
- [ ] After closing workbook, clicking Import again reaches Step 4
- [ ] No data is written on locked-file preflight failure
- [ ] KNOWN-ISSUES.md and TECH-DEBT.md reviewed

## v0.4.0-alpha.18.5 — Review redesign / password icons
- [ ] Build-BinTracker.bat reports v0.4.0-alpha.18.5
- [ ] Full solution builds with zero warnings
- [ ] All automated tests pass
- [ ] Review summary displays six metric cards in one row
- [ ] Metric values correctly reflect Review state after Confirm/Map actions
- [ ] Confirm new / Confirm existing / Map container actions remain usable
- [ ] View reconciliation larger is visible without scrolling
- [ ] Balance Reconciliation receives most of the remaining Step 3 height
- [ ] Multiple reconciliation rows are visible at normal wizard size
- [ ] Large reconciliation viewer still opens and shows all current rows
- [ ] Reconciliation headers explain Current/B/Fwd/OUT/IN/target/adjustment/projected context
- [ ] Hidden password uses normal eye
- [ ] Visible password uses eye-with-slash
- [ ] Password toggle remains keyboard/accessibility safe
- [ ] KNOWN-ISSUES.md and TECH-DEBT.md reviewed

## v0.4.0-alpha.18.6 — Review icons/header/card polish
- [ ] Build-BinTracker.bat reports v0.4.0-alpha.18.6
- [ ] Full solution builds with zero warnings
- [ ] All automated tests pass
- [ ] Metric cards are not vertically clipped at 100% DPI
- [ ] Metric cards are not vertically clipped at 125% DPI
- [ ] Source uses database-stack icon
- [ ] Customers uses people icon
- [ ] Existing matches uses check-circle icon
- [ ] New candidates uses person-plus icon
- [ ] Containers uses container/bin icon
- [ ] Reconciliation uses scales icon
- [ ] Review action buttons use corresponding vector icons
- [ ] Balance headers are concise: Customer, Container, Current, B/Fwd, OUT, IN, Excel target, Opening adjustment, Projected, Status, Container rule
- [ ] Full cutover formula remains visible above the reconciliation grid
- [ ] KNOWN-ISSUES.md and TECH-DEBT.md reviewed

## v0.4.0-alpha.18.7 — raster icons / reconciliation preview fix
- [ ] Build-BinTracker.bat reports v0.4.0-alpha.18.7
- [ ] Full solution builds with zero warnings
- [ ] All automated tests pass
- [ ] Review summary icons match approved mockup style and are raster PNG assets
- [ ] Confirm new button icon and full text are visible
- [ ] Confirm existing button icon and full text are visible
- [ ] Map container button icon and full text are visible
- [ ] View reconciliation larger icon and full text are visible
- [ ] Before customer confirmation, CLAMMS has separate Blue Bin / Bulk Bin / Yellow Bin reconciliation rows
- [ ] Blue CLAMMS row says no legacy token -> standard Blue Bin
- [ ] Bulk CLAMMS row shows legacy token Bulk -> Bulk Bin
- [ ] Yellow CLAMMS row shows legacy token Y -> Yellow Bin
- [ ] Pending confirmation rows still show Opening adjustment
- [ ] Pending confirmation rows still show Projected
- [ ] Pending confirmation status still blocks Step 4
- [ ] Confirming customer changes status to Ready without changing the preview maths
- [ ] KNOWN-ISSUES.md and TECH-DEBT.md reviewed

## v0.4.0-alpha.18.8 — approved mockup fidelity
- [ ] Build-BinTracker.bat reports v0.4.0-alpha.18.8
- [ ] Full solution builds with zero warnings
- [ ] All automated tests pass
- [ ] Six Review icons visually match the ORIGINAL MOCKUP comparison image
- [ ] Icons are the extracted approved raster assets, not runtime vectors or Unicode
- [ ] Primary metric value is bold/dark
- [ ] Secondary metric value is smaller grey text
- [ ] Confirm new button shows count and full label
- [ ] Confirm existing button shows count and full label
- [ ] Map container button shows count and full label
- [ ] View balance reconciliation larger... shows the entire label
- [ ] Buttons are not clipped at 100% DPI
- [ ] Buttons are not clipped at 125% DPI
- [ ] Six cards fit without clipping at the wizard minimum width
- [ ] KNOWN-ISSUES.md and TECH-DEBT.md reviewed

## v0.4.0-alpha.18.9 — Review simplification/button cleanup
- [ ] Build-BinTracker.bat reports v0.4.0-alpha.18.9
- [ ] Full solution builds with zero warnings
- [ ] All automated tests pass
- [ ] New card title/value do not wrap or clip at 100% DPI
- [ ] Container card icon and values are fully visible
- [ ] Confirm new button icon and text are fully visible
- [ ] Confirm existing button icon and text are fully visible
- [ ] Map container button icon and text are fully visible
- [ ] Open reconciliation button icon and text are fully visible
- [ ] Approved original mockup icon assets remain in use
- [ ] Analyse warning wraps beside the triangle, not beneath it
- [ ] KNOWN-ISSUES.md and TECH-DEBT.md reviewed

## v0.4.0-alpha.18.10 — warning/icon/secondary-text cleanup
- [ ] Build-BinTracker.bat reports v0.4.0-alpha.18.10
- [ ] Full solution builds with zero warnings
- [ ] All automated tests pass
- [ ] Analyse warning shows exactly one exclamation-triangle icon
- [ ] Container summary icon is fully visible
- [ ] Map container button icon is fully visible
- [ ] Map container button label is fully visible
- [ ] Source secondary reads balance rows
- [ ] Customers secondary reads formula issues
- [ ] Existing matches secondary reads unconfirmed count
- [ ] New candidates secondary reads skipped count
- [ ] Containers secondary reads manual mappings
- [ ] Reconciliation secondary reads issues
- [ ] KNOWN-ISSUES.md and TECH-DEBT.md reviewed

## v0.4.0-alpha.19 — transactional import execution
- [ ] Build-BinTracker.bat reports v0.4.0-alpha.19
- [ ] Full solution builds with zero warnings
- [ ] All automated tests pass
- [ ] Use Developer -> Start Fresh Test Database before first real importer execution test
- [ ] Complete Review decisions and mappings
- [ ] Step 4 Import now button is enabled only when exact source was not previously completed
- [ ] Import confirmation explains cutover date and atomic rollback
- [ ] New confirmed customers are created once
- [ ] Skipped new customers are not created
- [ ] Confirmed existing matches use the selected BinTracker customer
- [ ] Positive Opening adjustment produces Adjustment/OUT movement
- [ ] Negative Opening adjustment produces Adjustment/IN movement
- [ ] Excel OUT produces ExcelImport/OUT movement
- [ ] Excel IN produces ExcelImport/IN movement
- [ ] Final balances equal Excel target after import
- [ ] ImportRun status is Completed
- [ ] ImportRun CreatedCustomers and MovementCount are correct
- [ ] Generated movement ReferenceNumber is IMPORT-<run id>
- [ ] Exact same workbook is blocked on a second import
- [ ] Modify workbook after Step 4 preflight: execution refuses and requests re-analysis
- [ ] Force/observe an execution error: no partial customers/movements are retained
- [ ] Audit trail contains EXCEL_IMPORT_COMPLETED
- [ ] KNOWN-ISSUES.md and TECH-DEBT.md reviewed
- [ ] Deferred UI note remains: container/action icon sizing and rounded Review tiles

## v0.4.0-alpha.19.1 — report semantics and UI cleanup
- [ ] Build-BinTracker.bat reports v0.4.0-alpha.19.1
- [ ] Full solution builds with zero warnings
- [ ] All automated tests pass
- [ ] Analyse warning shows exactly one triangle
- [ ] Review blocker message does not say Import is disabled in this alpha
- [ ] First Run Administrator Cancel/Create buttons align on same baseline
- [ ] Market Floor PDF is exactly 2 pages with current imported dataset
- [ ] Front page uses larger/adaptive readable text without overflow
- [ ] Cash/COD owing and credit customers appear together in Cash section
- [ ] Account credits appear in separate CREDIT section
- [ ] Same-day import Adjustment is included in B/Fwd
- [ ] Same-day import Adjustment is excluded from daily OUT/IN
- [ ] Zahos reverse row reads B/Fwd 5, OUT 10, IN 15, Total 0 for the imported example
- [ ] Zahos statement labels the imported 5 as Opening adjustment (OUT)
- [ ] Zahos statement labels physical 10 as OUT (Taken)
- [ ] Zahos statement labels physical 15 as IN (Returned)
- [ ] KNOWN-ISSUES.md and TECH-DEBT.md reviewed

## v0.4.0-alpha.19.2
- [ ] Build reports v0.4.0-alpha.19.2
- [ ] Zero warnings
- [ ] All tests pass
- [ ] Search `zahos` shows Zahos in list and details
- [ ] Search `big` shows BIG in list and details
- [ ] Search nonsense clears list and details
- [ ] Clear search restores normal list
- [ ] Market Floor is exactly 2 pages
- [ ] Page 1 text is clearly larger than alpha.19.1
- [ ] `38 CREDIT` remains on one line
- [ ] Cash/COD credits remain in Cash area
- [ ] KNOWN-ISSUES.md and TECH-DEBT.md reviewed

## v0.4.0-alpha.19.3
- [ ] Build reports v0.4.0-alpha.19.3
- [ ] Zero warnings
- [ ] All tests pass
- [ ] Market Floor remains exactly 2 pages
- [ ] Page 1 text is larger than alpha.19.2
- [ ] Page 2 text is larger than alpha.19.2
- [ ] KHALID 12 CREDIT is one line
- [ ] HO 38 CREDIT is one line
- [ ] HP 18 CREDIT is one line
- [ ] JUST 11 CREDIT is one line
- [ ] KEVIN 30 CREDIT is one line
- [ ] No Cash/COD CREDIT value wraps
- [ ] No reverse-side CREDIT value wraps

## v0.4.0-alpha.19.4
- [ ] Build reports v0.4.0-alpha.19.4
- [ ] Zero warnings
- [ ] All tests pass
- [ ] Market Floor remains exactly 2 pages
- [ ] Page 1 has Buyer / Bin / Total
- [ ] Page 2 has Buyer / Bin / Out / In / B/Fwd / Total
- [ ] CLAMMS page 1 shows Blue 10
- [ ] CLAMMS page 1 shows Yellow 45
- [ ] CLAMMS page 1 shows Bulk 1
- [ ] CLAMMS is not shown as aggregate 56
- [ ] CLAMMS reverse rows remain separate by container
- [ ] Other multi-container customers remain separated
- [ ] CREDIT values remain single-line
- [ ] Special containers remain in the Special Containers block

## v0.4.0-alpha.19.5
- [ ] Build reports v0.4.0-alpha.19.5
- [ ] Zero warnings
- [ ] All tests pass
- [ ] Market Floor PDF is exactly 2 pages
- [ ] No standard Blue row prints the word Blue
- [ ] CLAMMS Blue row displays simply as CLAMMS
- [ ] CLAMMS Yellow row displays as CLAMMS (Yellow)
- [ ] CLAMMS Bulk row displays as CLAMMS (Bulk)
- [ ] Bulk is not in Special Containers
- [ ] Reverse side has no dedicated Bin column
- [ ] Reverse CLAMMS Blue/Yellow/Bulk rows remain separate
- [ ] Buyer names do not wrap unnecessarily
- [ ] Out / In / B/Fwd headers do not fragment
- [ ] CREDIT values remain one line

## v0.4.0-alpha.19.6
- [ ] Build reports v0.4.0-alpha.19.6
- [ ] Zero warnings
- [ ] All tests pass
- [ ] Market Floor is exactly 2 pages with latest real workbook
- [ ] Bulk appears only in Special Containers
- [ ] Blue remains implicit
- [ ] Yellow remains explicit inline with buyer
- [ ] Normal current-day report uses largest practical readable font
- [ ] Extra Yellow rows cause font/padding/spacing to shrink automatically
- [ ] Front page does not spill onto a second front page
- [ ] CREDIT values remain on one line
- [ ] Reverse side remains one page
