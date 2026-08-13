# Re-import Safety

## Current status

Exact completed-workbook re-import protection is implemented.

Every Import Run records source metadata including SHA-256 fingerprint. Step 4 checks the fingerprint during preflight and again inside the write transaction.

A blind exact duplicate import is therefore blocked.

## Remaining problem: changed workbook, same cutover

A workbook can change while still representing the same business cutover date. Its SHA-256 will be different, so fingerprint-only duplicate protection is not enough.

Before v1.0 BinTracker must detect prior Import Runs for the same import profile/cutover date and require an explicit correction workflow.

Allowed choices should be:

- **Cancel**
- **Review differences**
- **Replace/correct previous import**

There must never be a generic **Import again anyway** action.

## Provenance now implemented

Import-generated `BinMovement` rows now have a nullable relational `ImportRunId` FK.

Rules:

- Adjustment/ExcelImport rows created by Step 4 link to the ImportRun that created them.
- Manual/Batch movements remain `ImportRunId = NULL`.
- migration V10 backfills eligible alpha.19.x import rows only when they have `Adjustment`/`ExcelImport` source, a strict `IMPORT-<numeric id>` reference and a matching ImportRun.
- legitimate operator movements are never inferred into an ImportRun merely because their free-text reference resembles an import.

This gives the changed-workbook replacement workflow a safe record boundary to build on.

## Replacement design requirement

A replacement transaction should:

1. load prior ImportRun-generated records;
2. calculate proposed differences;
3. show the operator what will change;
4. require explicit confirmation;
5. reverse/remove/replace only the prior import-generated records in a controlled transaction;
6. write a new ImportRun/audit trail linking the correction to the prior run;
7. roll back everything on failure.

## Rollback verification

Rollback is covered by deterministic SQLite integration testing. The test injects a failure after the final `SaveChangesAsync` and before `CommitAsync`, after the pending/completed ImportRun state, created customer, generated movements and completion audit have all been flushed into the open transaction.

After the forced failure the test verifies:

- no imported customer survives;
- no imported movement survives;
- no ImportRun survives;
- no `EXCEL_IMPORT_COMPLETED` audit survives;
- the exact same workbook fingerprint remains eligible for retry.

## Developer testing

`Settings → Developer Tools → Developer Database` supports:

- fresh database first-import testing;
- restored populated database matching/re-import testing;
- repeated controlled scenarios.

It is development tooling, not the production Backup/Restore feature.
