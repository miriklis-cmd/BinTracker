# BinTracker Database

## Schema V16 movement corrections and review

V16 adds `MovementCorrectionOperations` and `MovementCorrectionLines` with unique client-operation and original/neutraliser/replacement lineage indexes. The existing unique `BinMovements.ReversesMovementId` remains the cross-command arbiter. Audit review columns default to false/null so historical reversal events do not become newly pending after upgrade.

## Schema V14 concurrency foundation

V14 adds optimistic `Revision` tokens, normalized unique Container Type `NameKey`, persisted operation IDs, provenance-only `SourceClientPath`, nullable unique `ImportRun.CurrentCutoverDate`, and import `ClientRequestFingerprint`. SQLite migration SQL remains isolated in Data; PostgreSQL requires its own provider migrations and real integration fixture.

## ApplicationSettings business information

The singleton `ApplicationSettings` row also stores configurable Business Information:

- BusinessName
- TradingName
- Abn
- Address
- Phone
- Email
- DefaultReportHeader

Schema migration v8 adds these columns to existing SQLite databases without replacing the settings row.


## Branding schema status

The current schema stores textual Business Information only. There is no logo/blob/path field yet. Logo persistence and migration design are part of the pre-v1 Business Information & Branding milestone and must be decided before schema implementation.
