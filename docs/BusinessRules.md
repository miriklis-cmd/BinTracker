# BinTracker Known Business Rules

- IN means Returned.
- OUT means Taken.
- Customers can return more containers than they have taken; the excess is credit.
- Balances are maintained separately for each container type.
- Blue Bin is the normal/default bin type.
- Initial container types are Blue Bin, Small Bin, Yellow Bin, Bulk Bin, and CHEP Pallet.
- Container types are configurable and may be added later.
- Customer code is the primary business identifier.
- Customer codes are unique without regard to case.
- Customer codes are displayed in uppercase.
- Customers are classified as Account or Cash / COD.
- Daily movement entry is commonly performed in separate IN and OUT batches.
- Friday is the normal collections/reminder day, but reminders may be run earlier when required.
- Excel migration must recognise `(Y)`, `(Bulk)`, and `(Chep)` as container-type markers.
- Excel brought-forward balances become opening positions during cut-over.
- Daily downstairs paperwork includes an outstanding summary and movement/balance detail.
- Customer statements must explain how a current balance was reached.

- Passwords do not expire periodically.
- Five failed login attempts lock an account by default; an administrator unlocks it.
- Administrator password resets force the user to choose a new password at next login.
