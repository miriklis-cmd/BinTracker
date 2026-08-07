# BinTracker v0.2.0-alpha.1

## Customer Management

- Search customers as you type.
- Add and edit customer details.
- Deactivate/reactivate customers instead of deleting history.
- Optional customer code, contact, phone, mobile, email, address and notes.
- Email/SMS reminder preferences and automatic-reminder opt-out.
- Live balance by container type, including credit positions.
- Recent movement history.
- Viewer role is read-only; Operator and Administrator can edit.
- Customer create/update/deactivate/reactivate actions are written to the audit trail.

## Reminder groundwork

- Added `ReminderDeliveries` persistence for future Google Workspace email and Texto SMS integration.
- Records channel, destination, exact message, delivery state, provider response, sending user, and outstanding snapshot.
- No external messages are sent yet and no credentials are required.

## Bin naming

- The original `Standard Bin` seed is now `Blue Bin`.
- Existing Alpha databases are upgraded in-place by stable container type Id, preserving all history.
