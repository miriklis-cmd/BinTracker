# Re-import Safety

## Current status

Exact duplicate protection and changed-workbook/same-cutover correction are implemented.

An exact completed SHA-256 remains blocked. A different workbook fingerprint for a cutover date with a completed ImportRun enters correction mode instead of normal import.

## Correction review

Step 4 shows the previous run, previous/proposed movement counts and changed customer/container net effects. The operator must explicitly continue to **Replace/Correct**. There is no blind “import again anyway” action.

## Replacement safety boundary

`BinMovement.ImportRunId` is the authoritative boundary.

The replacement transaction:

1. validates the changed workbook and previous completed run;
2. rebuilds reconciliation from legitimate movement history **strictly before the cutover date**, excluding the prior ImportRun;
3. preserves same-day/later Manual, Batch and all other non-prior-import movements outside the workbook correction;
4. removes only movements whose `ImportRunId` equals the previous run;
5. keeps customer records, including customers originally created by the old import;
6. marks the old run `Replaced`;
7. creates a new run with `ReplacesImportRunId` pointing to the old run;
8. writes corrected movements linked to the new run;
9. commits all changes atomically.

Keeping customer records is intentional because legitimate activity may already reference them.

## Structural metadata

Migration V11 adds `CutoverDate` and `ReplacesImportRunId` to ImportRun plus lookup indexes. Earlier alpha.19 cutover dates are conservatively backfilled from the stable `Cutover date yyyy-MM-dd` Notes prefix.

## Rollback

Correction uses the same verified SQLite transaction boundary as normal Import. The existing forced-failure regression continues to protect atomicity.


## Why the baseline is pre-cutover

A correction can happen after operators have already entered legitimate activity on the cutover date or later. Those movements are subsequent real activity and must not change the corrected Excel opening adjustment.

The corrected workbook therefore rebuilds from history strictly before the cutover. After the old ImportRun movements are replaced, legitimate same-day/later activity remains on top of the corrected imported position.


## Step 4 discovery

The Import UI must call preflight with the active cutover date. A changed fingerprint with a prior completed run for that date is surfaced on Step 4 **before execution**:

- amber same-cutover warning;
- explicit **Replace / Correct** action;
- difference review before final confirmation.

The execution-time same-cutover guard remains as defence in depth, not as the primary operator workflow.


## Container identity in comparisons

Correction comparison keys use normalized customer identity plus the resolved `ContainerTypeId`. Legacy tokens such as `Blue`, configured names such as `Blue Bin`, and other display wording must not create separate correction positions for the same configured container.


## Import Run history UI

Administrators can inspect the immutable provenance chain from Settings → Import History. The history shows original/replacement status, source fingerprint and the movements currently linked to each run. A replaced run can legitimately show zero currently linked movements because those generated records were atomically replaced by the corrected run; its historical run metadata and replacement relationship remain intact.


## Correction difference snapshot

Before a prior run's generated movements are removed, the corrected ImportRun persists an immutable JSON snapshot of every changed resolved Customer + ContainerType position, including previous import effect, corrected import effect and numeric difference. Import History renders this snapshot so the reason for a replacement remains visible after the old generated rows are gone.

Runs corrected before this feature existed cannot be reconstructed reliably and are explicitly shown as “not captured by the build that created this run.”
