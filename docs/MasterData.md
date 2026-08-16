# Master Data

BinTracker keeps changeable business configuration in master data rather than hard-coding it in screens.

## Container Types

Container Types define the reusable assets tracked by movements.

Important fields include:

- Name
- Short Code
- immutable System Code
- Display Order
- Active/Inactive
- Special Floor Report Container
- Dashboard Colour
- Notes

Existing types with movement history should be deactivated rather than deleted.

## Business Information

Business Information is stored in the singleton Application Settings record and is editable by Administrators.

Fields:

- Business Name
- Trading Name
- ABN
- Address
- Phone
- Email
- Default Report Header

Business-specific values are configuration data and are not stored in README/project documentation.

### Report identity

When a Default Report Header is supplied, reports use it. Otherwise Trading Name is preferred, then Business Name, then `BinTracker`.

All changes are audited.

### Branding roadmap status

`Default Report Header` already provides the configurable/custom textual report header. The planned pre-v1 branding expansion adds:

- business logo configuration;
- shared logo/header placement rules for reports/statements;
- reuse of the same authoritative identity in future email/generated output;
- agreed image storage, sizing, fallback and per-output behaviour.

Logo support is **not implemented** in the current schema/UI.


## Unsaved changes

Customer and Container Type master-data editors protect modified fields with explicit **Save / Discard / Cancel** choices before navigation or close can discard edits.
