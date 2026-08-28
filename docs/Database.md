# BinTracker Database

## Planned logical movement lineage (documentation-frozen; not implemented)

The next lineage schema is governed by BT-CORR-018..033, BT-AUD-015..017 and BT-OPS-011..012. No migration exists yet.

- `LogicalMovementBatch`: stable root, nullable unique RootMovementBatchId as sole original physical-batch link, CurrentGenerationNumber, status/reason and line count.
- `LogicalMovementLine`: one root, immutable RootMovementId and immutable OriginalDisplayOrdinal; no current pointer.
- `LogicalMovementGeneration`: unique root/generation number, predecessor, operation, kind/count/time.
- `LogicalMovementGenerationLine`: one row per permanent line per generation, explicit action/state/AppliedFieldMask, predecessor and Active or Reversed movement pointers; no authoritative before/result JSON.
- `LogicalMovementLedgerLink`: one movement owner/line and transformation role. IntroducedByGenerationLineId may be null only during the transaction and must be filled before Active commit.
- correction-output-only physical batch link: never duplicates RootMovementBatchId; at most one truthful output batch per eligible generation.
- existing `MovementCorrectionOperations` remains the storage-compatible operation envelope and gains logical root/expected/result generation plus canonical versioned request/fingerprint. Existing `MovementCorrectionLines` remains legacy evidence only.
- new operations have one unique primary AuditEvent link. Audit health remains separate from operational state.

Portable PK/FK/RESTRICT/UNIQUE/CHECK/index constraints and root CAS combine with transactional service/validator cardinality checks; no SQLite triggers/deferred-FK dependency. Persisted batches/movements/evidence use RESTRICT/NO ACTION rather than ordinary detach/delete.

Migration creates Generation 0 MigrationBaseline only from deterministic IDs/FKs/correction/reversal relationships. Status is Initializing, Active, ReadOnly or Invalid. Import-owned data is not grouped into generic roots. Read-only preflight and verified recoverable backup are blocking before schema mutation.

Numeric projection uses one connection/context and read transaction in SQLite WAL; validation and aggregation share that snapshot. Future PostgreSQL uses equivalent read-only repeatable snapshot semantics behind infrastructure.

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
