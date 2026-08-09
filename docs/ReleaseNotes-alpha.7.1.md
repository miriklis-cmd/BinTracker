# BinTracker v0.2.0-alpha.7.1 — Workflow & Polish

## What changed because of testing

The following changes came directly from hands-on Alpha 7.0 testing:

- Enter on Quantity now adds a line when Reference and Notes are not required.
- Enter on Reference adds a line when Notes are not required.
- Enter on Notes still adds a line.
- Ctrl+Enter saves the current batch.
- Draft batches survive navigating away from Batch Entry and back.
- Customer position now has Current and With Draft columns so unsaved movements are visible before the batch is committed.
- Removing a pending line immediately recalculates the preview.
- Batch action buttons now use a consistent size.
- Dashboard cards now read real movement data when the Dashboard is opened.
- Customer Recent Movement History has tighter responsive column sizing for smaller screens.
- User Management now uses short colour-coded status wording and context-sensitive action buttons.

## Batch Entry
- Added application-level in-memory draft state.
- Movement date, direction and pending lines are retained while navigating.
- IN/OUT direction cannot be changed while a draft contains lines.
- Live preview uses database balance plus the pending draft.
- CREDIT preview is green; outstanding preview is red.
- Ctrl+Enter is displayed next to Save Batch.

## Dashboard
- Returned Today and Taken Today now use saved movement data.
- Outstanding totals positive customer/container positions without allowing credits to cancel other outstanding positions.
- Requires Attention currently counts customers above the configured quantity threshold.
- Dashboard shows when an unsaved Batch Entry draft exists.

## User Management
- Status wording: Active, Password Reset Required, Locked, Inactive.
- Status text is colour-coded.
- Role text is subtly colour-coded.
- Display Name is narrower; Status receives more space.
- Activate/Deactivate and Lock/Unlock buttons now show only the action that will occur.

## General
- Status bar shows `BinTracker v0.2.0-alpha.7.1`.
- Added `KNOWN-ISSUES.md`.
- Expanded automated tests for draft persistence and preview balance calculations.
