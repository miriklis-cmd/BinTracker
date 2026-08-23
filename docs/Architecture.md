# BinTracker Architecture

Current baseline: **v0.5.0-alpha.5.1**

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
- Single Entry, Batch Entry, reversal and import persist client operation IDs. A retry returns the existing result only when the canonical payload identity matches; reuse with a different payload is rejected.
- Reversal and import uniqueness constraints are authoritative under races.
- Authentication counters and account/credential mutations use conditional or atomic database updates.

Moving to PostgreSQL still requires an API host, provider/schema migrations, authentication/authorization deployment, central backup/monitoring and real PostgreSQL integration tests. It must not require rewriting accepted business, reversal, import, report or audit semantics.
