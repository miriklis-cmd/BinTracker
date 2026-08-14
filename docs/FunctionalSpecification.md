# BinTracker Functional Specification

## Customer Management

- BT-CUST-001: Every customer must have a customer code.
- BT-CUST-002: Customer code is the primary visible customer identifier.
- BT-CUST-003: Customer codes must be unique without regard to case.
- BT-CUST-004: Customer codes are normalised to uppercase.
- BT-CUST-005: Customers are classified as Account or Cash / COD.
- BT-CUST-006: Customers can be deactivated/reactivated without destroying history.
- BT-CUST-007: Customer balances are maintained separately per Container Type.
- BT-CUST-008: Unsaved customer edits must not be silently discarded; navigation away from dirty data requires Save / Discard / Cancel.

## Containers

- BT-CONT-001: Blue Bin is the default normal bin type.
- BT-CONT-002: Container types are data-driven and administrator-manageable.
- BT-CONT-003: Balances are maintained independently per container type.
- BT-CONT-004: Container types can be marked as Special Floor Report Containers.

## Movements

- BT-MOVE-001: IN means Returned.
- BT-MOVE-002: OUT means Taken.
- BT-MOVE-003: A customer may have a credit balance.
- BT-MOVE-004: Batch Entry supports separate IN and OUT workflows.
- BT-MOVE-005: A batch contains one movement direction.
- BT-MOVE-006: Batch Entry can contain multiple customers and container types.
- BT-MOVE-007: Saving a batch is transactional.
- BT-MOVE-008: Saved batches are audited.
- BT-MOVE-009: Corrections/reversals must preserve the original movement and audit trail.

## Dashboard

- BT-DASH-001: Dashboard shows today’s Returned and Taken quantities.
- BT-DASH-002: Dashboard shows current outstanding positions.
- BT-DASH-003: Dashboard identifies customers/positions requiring attention.
- BT-DASH-004: Attention items should be actionable/drillable rather than only a headline count.
- BT-DASH-005: Dashboard rules must be based on explicit business thresholds/ageing rules.

## Security and Audit

- BT-SEC-001: Login events are audited.
- BT-SEC-002: User administration is audited.
- BT-SEC-003: Customer creation/change/status events are audited.
- BT-SEC-004: Report generation is audited.
- BT-SEC-005: Saved movement history is not silently deleted or overwritten.
- BT-SEC-006: Administrative actions are role-restricted.
- BT-SEC-007: Credentials/secrets for external providers must not be stored in plain text.

## Reporting

- BT-PRINT-001: Daily Print Pack contains Outstanding Summary and Movement Detail.
- BT-PRINT-002: Customer statements show opening position, movements, running position and closing position.
- BT-PRINT-003: Outstanding Containers report shows current position by customer/container.
- BT-PRINT-004: Daily Movements report shows selected-day movement detail.
- BT-PRINT-005: Movement History supports date-range/customer/container/source filters.
- BT-PRINT-006: Monthly Summary provides monthly OUT, IN and net movement reporting.
- BT-PRINT-007: Market Floor Sheet is a two-page front/reverse operational report.
- BT-PRINT-008: Market Floor Blue is implicit; non-standard regular containers are explicit.
- BT-PRINT-009: Special Floor Report Containers use the dedicated special section.
- BT-PRINT-010: Import opening adjustments contribute to opening/B/Fwd reporting and are not physical daily OUT/IN.

## Reminders / Communications

- BT-COMM-001: Customers can independently allow Email reminders and SMS reminders.
- BT-COMM-002: Customer opt-out overrides automatic reminder sending.
- BT-COMM-003: Reminder delivery attempts record channel, destination, status, provider response and relevant outstanding snapshot.
- BT-COMM-004: Failed sends can be retried safely without accidental duplicate sends.
- BT-COMM-005: Reminder sends/runs are auditable.
- BT-COMM-006: Provider credentials are administrator-configured and securely stored.

## Migration

- BT-IMPORT-001: Excel brought-forward positions establish authoritative cutover opening position.
- BT-IMPORT-002: `(Y)` maps to Yellow Bin for the legacy profile.
- BT-IMPORT-003: `(Bulk)` maps to Bulk Bin for the legacy profile.
- BT-IMPORT-004: `(Chep)` maps to CHEP Pallet where configured/resolved.
- BT-IMPORT-005: Unprefixed legacy customer rows map to Blue Bin.
- BT-IMPORT-006: Unknown explicit container tokens must be resolved, not guessed.
- BT-IMPORT-007: Import execution is transactional.
- BT-IMPORT-008: Exact completed-workbook re-import is blocked.
- BT-IMPORT-009: Changed-workbook/same-cutover correction must be explicit and must not duplicate prior imported movements.
- BT-IMPORT-010: Import-generated movements must link relationally to the Import Run that created them; non-import movements remain unlinked.
- BT-IMPORT-011: Same-cutover correction must calculate the corrected import from pre-cutover legitimate history and preserve same-day/later non-import activity on top.
- BT-IMPORT-012: Step 4 must surface changed-workbook/same-cutover correction before execution with an explicit Replace / Correct action; execution-time rejection alone is not an acceptable workflow.
- BT-IMPORT-013: Correction comparisons must use resolved configured container identity, not legacy/display container strings.
- BT-IMPORT-014: Administrators must be able to inspect Import Run provenance, replacement relationships and generated movement records through a read-only history UI.
- BT-IMPORT-015: A corrected ImportRun must persist the exact resolved customer/container difference snapshot before the prior generated rows are removed, so replacement intent remains auditable later.

## Backup / Recovery

- BT-OPS-001: Production data can be backed up safely.
- BT-OPS-002: Restore requires explicit confirmation and database validation.
- BT-OPS-003: Upgrades protect existing data and include recovery guidance.


- BT-UI-006: Editable Container Type master data must warn before navigation/close discards unsaved changes and offer Save / Discard / Cancel.
- BT-IMPORT-016: Import History must keep provenance, correction changes and linked movement data readable at supported desktop sizes/DPI.


- BT-CUSTOMER-009: Customer editor changes must never be silently discarded. Selection, filtering, New Customer, page navigation, logout and application close must offer explicit Save / Discard / Cancel.
- BT-UI-007: Unsaved-change prompts use explicit action labels rather than Yes / No where the actions are Save / Discard / Cancel.
