# BinTracker Alpha 7.1 Test Checklist

## Build
- [ ] Run `Build-BinTracker.bat`
- [ ] Build succeeds
- [ ] All automated tests pass

## Batch Entry keyboard workflow
- [ ] Enter on Customer Code resolves customer
- [ ] Enter on Quantity adds to batch when Reference/Notes are not needed
- [ ] Enter on Reference adds to batch when Notes are not needed
- [ ] Enter on Notes adds to batch
- [ ] Ctrl+Enter opens Save Batch confirmation
- [ ] Add / Remove / Clear / Save buttons are aligned

## Live draft balances
- [ ] Select a customer and note Blue Bin current position
- [ ] Add an unsaved Blue Bin line
- [ ] Re-select that customer
- [ ] "With Draft" reflects the unsaved Blue Bin line
- [ ] Add another container type for that customer
- [ ] Blue preview remains altered while entering the second type
- [ ] Remove the Blue line
- [ ] Blue preview returns to the database position
- [ ] CREDIT preview is green
- [ ] OUT preview is red

## Draft navigation
- [ ] Add several unsaved lines
- [ ] Click Customers
- [ ] Add/edit a customer if desired
- [ ] Return to Batch Entry
- [ ] Unsaved draft lines are still present
- [ ] Movement date and IN/OUT direction are retained
- [ ] Changing IN/OUT is blocked while a draft has lines
- [ ] Clear Batch removes the draft
- [ ] Saving removes the draft only after successful save

## Dashboard
- [ ] Save a Returned (IN) batch
- [ ] Open Dashboard
- [ ] Returned Today reflects saved quantity
- [ ] Save a Taken (OUT) batch
- [ ] Open Dashboard
- [ ] Taken Today reflects saved quantity
- [ ] Outstanding reflects current positive customer/container positions
- [ ] Dashboard mentions an unsaved draft when one exists

## User management
- [ ] Display Name column is narrower
- [ ] Status is fully visible
- [ ] Active is green
- [ ] Password Reset Required is orange
- [ ] Locked is red
- [ ] Inactive is grey
- [ ] Administrator/Operator/Viewer roles have subtle colours
- [ ] Active user shows Deactivate button
- [ ] Inactive user shows Activate button
- [ ] Unlocked user shows Lock button
- [ ] Locked user shows Unlock button

## Customer screen
- [ ] On the laptop/smaller screen, Recent Movement History fits better
- [ ] Date, Direction, Container Type, Qty, Reference and Entered By are usable
- [ ] No right-side content is unexpectedly clipped

## General
- [ ] Status bar shows BinTracker v0.2.0-alpha.7.1
- [ ] Login still lands on Dashboard
- [ ] Customer Statement still generates
- [ ] Audit Trail still opens
