# BinTracker Operational Workflow

## Login

Users authenticate with a BinTracker account. Passwords are masked by default and can be temporarily revealed with the eye control.

## Batch Entry

1. Select movement date.
2. Select Returned (IN) or Taken (OUT).
3. Enter customer code.
4. Choose container type.
5. Enter quantity.
6. Add the line.
7. Review/edit/remove pending lines.
8. Check the `With Draft` customer position.
9. Save the batch.

Pending lines are not committed to the database until Save Batch is confirmed.

## Customer review

The Customers screen shows customer details, current positions by type and recent movement history.

## Logout

Logout ends the authenticated session and returns to the login screen. During the same application run, the current in-memory Batch Entry draft is preserved.

This document should be expanded with production SOPs before deployment.
