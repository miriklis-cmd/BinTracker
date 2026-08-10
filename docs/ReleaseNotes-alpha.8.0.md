# BinTracker v0.2.0-alpha.8.0 — Single Entry

## New feature: Single Entry
- Replaced the Single Entry placeholder with a working manual movement screen.
- Record one Returned (IN) or Taken (OUT) movement.
- Customer code autocomplete/validation.
- Container type selection.
- Quantity is blank until explicitly entered.
- Optional reference and notes.
- Current customer position displayed on the right.
- `After Save` previews the selected movement before committing it.
- `Ctrl+Enter` saves the movement.
- Enter from quantity/reference/notes also saves.
- Save confirmation summarizes the movement before commit.
- Manual movements use `MovementSource.Manual`.
- Manual movements receive their own `MOVEMENT_RECORDED` audit event.
- Viewer accounts cannot save movements.

The Dashboard automatically reflects saved Single Entry movements when revisited.
