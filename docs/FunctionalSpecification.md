# BinTracker Functional Specification

## Customer Management

- BT-CUST-001: Every customer must have a customer code.
- BT-CUST-002: Customer code is the primary visible customer identifier.
- BT-CUST-003: Customer codes must be unique without regard to case.
- BT-CUST-004: Customer codes are normalised to uppercase.
- BT-CUST-005: Customers are classified as Account or Cash / COD.
- BT-CUST-006: Customers can be deactivated and reactivated without destroying history.

## Containers

- BT-CONT-001: Blue Bin is the default normal bin type.
- BT-CONT-002: Container types are data-driven and administrator-manageable.
- BT-CONT-003: Balances are maintained independently per container type.

## Movements

- BT-MOVE-001: IN means Returned.
- BT-MOVE-002: OUT means Taken.
- BT-MOVE-003: A customer may have a credit balance.
- BT-MOVE-004: Batch entry must support separate IN and OUT workflows.

## Security and Audit

- BT-SEC-001: Login events are audited.
- BT-SEC-002: User administration is audited.
- BT-SEC-003: Customer creation/change/status events are audited.
- BT-SEC-004: Report generation is audited.
- BT-SEC-005: Saved movement history is not silently deleted or overwritten.

## Reporting

- BT-PRINT-001: Daily Print Pack contains Outstanding Summary and Movement Detail.
- BT-PRINT-002: Customer statements show opening position, movements, running position, and closing position.

## Migration

- BT-IMPORT-001: Excel brought-forward positions are imported as opening positions.
- BT-IMPORT-002: `(Y)` maps to Yellow Bin.
- BT-IMPORT-003: `(Bulk)` maps to Bulk Bin.
- BT-IMPORT-004: `(Chep)` maps to CHEP Pallet.
- BT-IMPORT-005: Unprefixed Excel customer rows map to Blue Bin.

- BT-MOVE-005: A batch contains one movement direction.
- BT-MOVE-006: Batch Entry must allow multiple customers and container types in one batch.
- BT-MOVE-007: Saving a batch must be transactional.
- BT-MOVE-008: Saved batches must be recorded in the audit trail.
