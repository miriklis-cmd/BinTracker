# BinTracker v0.2.0-alpha.2

## Customer workflow improvements

- Customer Code now appears before Customer Name in the customer list.
- Customers are sorted by code by default.
- Customer Code is required for all new/edited customers and is normalised to uppercase.
- Search now covers code, customer name, contact, phone, mobile and email.
- Added Customer Statement PDF generation with a selectable date range.
- Customer statements show current position, opening position, chronological IN/OUT movements and running position by container type.
- Every generated customer statement is recorded in the audit trail.

## Audit screen

- Reworked audit-grid sizing so the Description column fills available space.
- Success column has a fixed DPI-safe width.
- Horizontal/vertical scrolling remains available on smaller displays.
- Added an audit-event count footer.
