# BinTracker v0.2.0-alpha.7.0 — Batch Entry

## Batch Entry
- Added the first operational Batch Entry screen.
- A batch has one direction: Returned (IN) or Taken (OUT).
- This matches the existing workflow of entering all returns together and dispatches separately.
- Supports movement date, customer code, container type, quantity, reference and notes.
- Customer codes autocomplete from active customers.
- Selecting a customer immediately shows Account/Cash-COD classification and current position by container type.
- Operators can build a pending batch, remove rows, clear it, review totals and save the whole batch at once.
- Batch save is transactional.
- Every saved batch creates one MovementBatch plus its BinMovement records.
- Saved batches are audited as MOVEMENT_BATCH_RECORDED.

## Keyboard workflow
- Tab / Shift+Tab use normal Windows field navigation.
- Enter on Customer Code resolves the customer and advances to container type.
- Enter in Notes adds the current movement to the pending batch.
- After adding a row, focus returns to Customer Code.

## Permissions
- Viewer accounts can inspect the screen but cannot save movements.
- Operator and Administrator accounts can save batches.

## Tests
- Added movement balance rules.
- Added persistence tests for IN/OUT balance calculation.
- Added multi-customer MovementBatch persistence test.

## Testing
- Added `TEST-CHECKLIST.md` to the root of the ZIP.
