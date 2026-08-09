# BinTracker Database

BinTracker currently uses SQLite with Entity Framework Core.

## Main domain concepts

- Customer
- ContainerType
- BinMovement
- MovementBatch
- UserAccount
- AuditEvent
- ApplicationSetting

## Balance convention

- OUT increases a customer's outstanding position.
- IN decreases it.
- Positive balance = OUT.
- Negative balance = CREDIT.
- Zero = Even.

## Operational principle

Committed movements are append-oriented operational records. UI previews should not write movements until the user explicitly saves the batch.

This document should be expanded before beta with the complete schema, keys, indexes and migration strategy.
