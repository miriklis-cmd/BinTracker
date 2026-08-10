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
