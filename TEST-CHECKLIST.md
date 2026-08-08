# BinTracker Alpha 7.0 Test Checklist

## Build
- [ ] Run `Build-BinTracker.bat`
- [ ] Restore succeeds
- [ ] Build succeeds
- [ ] All automated tests pass

## Navigation
- [ ] Login still lands on Dashboard
- [ ] Batch Entry opens from the left navigation
- [ ] Customers, Settings and Audit still open normally

## Returned (IN) batch
- [ ] Batch type defaults to Returned (IN)
- [ ] Customer code autocomplete suggests active customer codes
- [ ] Entering a valid customer code shows customer name and Account/Cash-COD type
- [ ] Customer position table loads
- [ ] Add a Blue Bin return to the pending batch
- [ ] Add a Yellow/Bulk/CHEP return
- [ ] Pending line count and total quantity update
- [ ] Remove Selected works
- [ ] Save Batch asks for confirmation
- [ ] Saved return reduces outstanding / can create credit
- [ ] Audit Trail contains MOVEMENT_BATCH_RECORDED

## Taken (OUT) batch
- [ ] Switch batch type to Taken (OUT)
- [ ] Add movements for multiple customers
- [ ] Save Batch
- [ ] Taken movements increase outstanding balances
- [ ] Customer screen shows the new balances and movement history
- [ ] Customer statement includes the movements

## Validation
- [ ] Unknown customer code is rejected
- [ ] Inactive customer cannot be selected
- [ ] Empty batch cannot be saved
- [ ] Future movement date is rejected
- [ ] Viewer cannot save a batch

## Keyboard / usability
- [ ] Tab moves through fields naturally
- [ ] Enter after Customer Code resolves the customer
- [ ] Enter in Notes adds the movement to the pending batch
- [ ] After adding a line, focus returns to Customer Code
- [ ] No unnecessary scrollbars at 150% Windows scaling
- [ ] No text or buttons are clipped
