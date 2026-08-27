# BinTracker Architecture

Current baseline: **v0.5.0-alpha.8.6**

## Display and DPI boundary

The required frequently-used laptop acceptance configuration is Windows 11 at **1920x1080 with 150% Windows scaling**. Every ordinary workflow and modal must remain accessible: action bands stay visible, content fits or scrolls within the working area, text does not clip, controls do not overlap, report grids remain usable, and normal WinForms DPI scaling is preserved.

The primary production environment uses a substantially larger display. The laptop configuration is an acceptance floor for usability, not a direction to globally reduce information density or optimise every screen specifically for a 14-inch panel. Layout concepts should remain presentation-neutral where practical so a future WinUI 3 client can replace WinForms presentation without moving business rules into UI controls; no WinUI dependency is introduced in v1.

## Permanent target and hard gate

BinTracker's target architecture is **Windows desktop / remote clients → authenticated BinTracker API/service → central PostgreSQL**. Clients never receive database credentials or connect directly. The current SQLite application remains the supported local deployment adapter until the central host exists.

All production business code must assume multiple authenticated remote users can execute the same operation concurrently. Core, Services, UI contracts and the shared EF model remain provider-neutral; provider migrations and local database tooling stay in `BinTracker.Data`.

## Host composition and request context

`AddBinTrackerBusinessServices()` registers provider-neutral business services. A future API must provide request-scoped `IUserContext` and `IClientContext`, and may supply its business-clock configuration. It must not inherit the desktop singleton `UserSession`.

`AddBinTrackerServices()` is the current desktop composition and adds the local session, client identity, authentication adapter and crash-draft storage before registering business services.

- `IUserContext` carries authenticated user identity and role.
- `IBusinessClock` supplies UTC and configured business-local time/date.
- `IClientContext` carries client provenance; an API must not record its own host as the operator workstation.

## Transport boundary

Imports cross the service boundary as `ImportSourceDocument` content plus safe metadata. `SourceClientPath` is provenance only and is never treated by business/server logic as a readable path. PDF/report services return bytes; the desktop client chooses where to save or open them. Local crash-draft recovery and developer database tools remain explicitly desktop-local.

## Concurrency, retries and invariants

- Customer, Container Type and Application Settings use optimistic `Revision` tokens.
- Container Type `NameKey` uses provider-neutral Unicode/case normalization and a unique database index.
- A nullable unique `ImportRun.CurrentCutoverDate` gives one current run ownership of a cutover; replaced runs retain provenance with null ownership.
- ImportRun provenance has two distinct immutable snapshots: `CorrectionChangesJson` for same-cutover Replace/Correct comparisons, and `OpeningReconciliationChangesJson` for non-zero opening adjustments generated from normal authoritative cutovers. NULL means the historical build did not capture that snapshot; `[]` means capture occurred with no changes.
- Single Entry, Batch Entry, reversal and import persist client operation IDs. A retry returns the existing result only when the canonical payload identity matches; reuse with a different payload is rejected.
- Reversal and import uniqueness constraints are authoritative under races.
- Correction extends the same invariant: the unique neutraliser FK (`ReversesMovementId`) arbitrates Reverse-vs-Reverse, Reverse-vs-Correct and Correct-vs-Correct; correction-operation identity/fingerprint makes identical retries return persisted lineage and rejects changed payload reuse.
- A single correction transaction writes original-period neutraliser(s), corrected replacement(s), operation/line lineage, original consumed links and audit. Whole-batch correction uses persisted `MovementBatchId` and rolls back every line on any conflict/failure.
- Effective operational report queries omit correction-consumed originals and correction-only neutralisers while Movement History retains all ledger evidence; balances remain ledger sums, where each correction pair nets to zero.
- Administrator acknowledgement is a review record, not an effectiveness gate: Operator corrections/reversals remain operationally effective immediately. Review authorization and duplicate prevention remain service/database concerns even when the Audit Trail disables ineligible UI actions.
- The outstanding-review count/state and navigation action must be exposed through a presentation-independent service/state/navigation contract. Current WinForms may render that contract with a reusable infobar-style `UserControl` or panel; a future WinUI 3 client replaces only that presentation with native `InfoBar`, retaining the underlying contract. Review business logic must not live in a disposable UI control.
- Audit detail navigation is keyed by authoritative persisted entity identity. MovementBatch detail is never inferred from description text; future contextual routes may use authoritative ImportRun or correction-lineage identity when available.
- Authentication counters and account/credential mutations use conditional or atomic database updates.

Moving to PostgreSQL still requires an API host, provider/schema migrations, authentication/authorization deployment, central backup/monitoring and real PostgreSQL integration tests. It must not require rewriting accepted business, reversal, import, report or audit semantics.
