
# Legacy Container Rules

For the Jack Miriklis legacy workbook profile:

- No bracket/container token means **Blue Bin**.
- `(Y)` means **Yellow Bin**.
- `(Bulk)` means **Bulk Bin**.
- Other tokens may resolve by configured Container Type name or short code.
- Any explicit token that cannot be resolved is a **hard blocker**.

Example:

`Clamms` -> Blue Bin

`(Y) Clamms` -> Yellow Bin

`(Bulk) Clamms` -> Bulk Bin

`(Tub) Clamms` -> customer Clamms, unresolved container token `Tub`

Unknown explicit tokens are never silently changed to Blue Bin. They must be mapped to an existing Container Type or the relevant Container Type must be created before Import can proceed.
