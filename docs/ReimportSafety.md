# Re-import Safety

Re-import protection is a mandatory requirement before the Import step is enabled.

## Problem

A legacy workbook may be imported more than once:

- the same exact file is selected twice;
- the workbook is changed and imported again;
- the operator restores an older BinTracker database and imports again;
- a same-day workbook contains the same B/Fwd and IN/OUT snapshot as a previous import.

Blindly inserting the same data again would duplicate opening positions and movements.

## Required behaviour before Step 4 is enabled

### Import Run identity

Every successful import must be recorded as an Import Run containing at least:

- import profile;
- source filename;
- SHA-256 source file fingerprint;
- cutover/effective date;
- importing user;
- imported timestamp;
- result/status.

### Exact re-import

If the exact same source fingerprint has already been successfully imported into the current database, BinTracker must block normal import and show the prior Import Run.

The user may inspect it but must not silently duplicate it.

### Changed workbook / same cutover date

A changed workbook may have a different file fingerprint while still representing the same cutover day.

Before applying it, BinTracker must compare the proposed opening positions and movements to prior Import-generated records for that profile/date.

The safe choices should be explicit, for example:

- Cancel;
- Review differences;
- Replace/correct the previous import (future controlled workflow).

There must be no generic "import again anyway" button that simply duplicates movements.

### Import-generated records

Opening positions and cutover movements need enough source metadata to trace them to the Import Run that created them. This is required for later replacement/correction without affecting legitimate operator-entered movements.

## Developer database testing

`Settings > Developer Tools > Developer Database` exists specifically to make these scenarios easy to test:

- clean database -> first import behaviour;
- populated database -> merge/match behaviour;
- restore a known state -> repeated-import behaviour.
