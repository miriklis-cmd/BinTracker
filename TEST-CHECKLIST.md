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
