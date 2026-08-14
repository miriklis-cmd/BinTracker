# Excel Import Wizard

## Goal

Import legacy/customer workbook data without silently damaging current BinTracker data.

## Current workflow

### 1. Analyse

Administrators select `.xlsx` or `.xlsm`.

BinTracker reads the workbook without requiring Excel, discovers worksheets/used ranges, identifies candidate customer/snapshot rows and reports structural/duplicate warnings.

No database writes occur during Analyse.

### 2. Map

Every worksheet is classified as:

- **Source** — authoritative rows eligible for Import;
- **Validation** — reconciliation/checking information only;
- **Report** — workbook output/report layout, never imported;
- **Ignore** — unrelated/unsupported sheet.

The current legacy workbook defaults its operational Update Account / Update Cash sheets to Source.

### 3. Review

Review compares Source customers with the live BinTracker database and resolves:

- existing matches;
- existing match override;
- new customer Create / Skip;
- normalized spacing/punctuation variants;
- customer type conflicts;
- legacy container aliases;
- unknown container-token mapping;
- authoritative balance reconciliation.

Decisions remain wizard-session state while navigating Back/Next.

### 4. Import

Step 4 is implemented.

Immediately before writing, BinTracker:

- re-fingerprints the workbook;
- rejects a workbook changed after preflight;
- re-checks exact completed-file import protection;
- rebuilds/validates customer matching and reconciliation against the live database.

Then one SQLite transaction:

- creates confirmed new customers;
- uses confirmed existing-customer targets;
- writes opening adjustments;
- writes cutover-day OUT/IN movements;
- links every generated movement to the ImportRun through `BinMovement.ImportRunId`;
- records a completed ImportRun and audit event.

A failure rolls the whole transaction back. This is now covered by an integration test that injects a failure after the final database SaveChanges and before CommitAsync, then proves that all import writes disappear.

## Legacy snapshot rule

For a workbook containing:

- B/Fwd;
- today's OUT;
- today's IN;
- Total;

the model is:

`Opening adjustment = Excel B/Fwd - current BinTracker balance`

then:

`Projected = current + opening adjustment + OUT - IN`

Excel Total is validation data and is not imported as another balance.

Opening adjustments are bookkeeping/cutover position. They must not appear in reports as physical Taken/Returned movements.

## Customer matching

Current matching progressively uses:

1. exact customer code;
2. case-insensitive customer code;
3. code ignoring spacing/punctuation;
4. name ignoring spacing/punctuation only when exactly one existing customer matches.

No fuzzy/edit-distance auto-merge is used.

Examples such as `S & J` / `S&J` are normalized to the same identity.

## Legacy Buyer/container rules

For the current legacy profile:

- no explicit token → **Blue Bin**;
- `(Y)` → **Yellow Bin**;
- `(Bulk)` → **Bulk Bin**.

An unknown explicit token such as `(Tub)` is never guessed as Blue. It blocks until mapped to a Container Type.

These rules belong to this legacy import profile and must not become assumptions for unrelated future spreadsheets.

## New customers

Unmatched customers require explicit:

- **Create** with editable proposed name; or
- **Skip**.

Unconfirmed customers block readiness. Skipped customer balance rows are excluded.

## Existing customers

Automatic matches require operator confirmation or override before Import readiness.

## Re-import

Exact successful workbook fingerprints are blocked.

A different workbook for the same cutover date still needs the controlled difference/replacement workflow described in `ReimportSafety.md`.

## Remaining importer work before v1.0

- Import Run history/details UI;
- improved execution failure report;
- deferred Review icon/rounded-card polish.

## Post-v1.0 import ideas

- Customers-only import intent.
- Customers + opening balances intent.
- Reusable Import Profiles.
- Standard BinTracker import template.
- Optional fuzzy-match suggestions requiring explicit operator approval.

## Changed-workbook correction

A changed fingerprint for an already-completed cutover date enters correction mode. Step 4 reviews the net differences and requires explicit Replace/Correct confirmation. The previous import is removed from the reconciliation baseline. The correction baseline uses legitimate history strictly before the cutover date, so same-day/later Manual/Batch activity remains on top rather than being absorbed into the corrected Excel opening position.


## Correction UI

Step 4 preflight includes the active cutover date. When a changed workbook corresponds to a previously completed cutover, the normal Import action becomes **Replace / Correct** and an amber warning identifies the previous run. Clicking it opens the correction comparison before any database write.


Correction differences are grouped by stable configured container identity (`ContainerTypeId`), not by legacy/display wording. The review shows the configured container name to the operator.


## Import history

Settings → Import History provides the post-import provenance view. It is read-only and shows every Import Run, replacement relationships, source fingerprint and generated movement records.


## Correction provenance snapshot

Successful Replace/Correct execution persists the approved resolved customer/container differences on the corrected ImportRun before deleting the previous run's generated movement rows. This is displayed later in Import History.
