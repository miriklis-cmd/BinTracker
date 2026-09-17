using System.Globalization;
using System.Text.Json;
using BinTracker.Core;
using Microsoft.Data.Sqlite;

namespace BinTracker.Data;

public enum LineageSchema17MigrationOutcome
{
    Migrated = 0,
    AlreadyComplete = 1
}

public enum LineageSchema17MigrationCheckpoint
{
    BeforeSchemaMutation = 0,
    AfterFirstSchemaChange = 1,
    DuringRootCreation = 2,
    DuringLineCreation = 3,
    DuringBaselineGenerationCreation = 4,
    DuringGenerationLineCreation = 5,
    DuringLedgerLinkCreation = 6,
    DuringLegacyOperationMapping = 7,
    DuringLegacyAuditMapping = 8,
    DuringMovementBatchForeignKeyRebuild = 9,
    BeforePostflight = 10,
    AfterPostflightBeforePublication = 11
}

public interface ILineageSchema17FailureInjector
{
    void ThrowIfRequested(LineageSchema17MigrationCheckpoint checkpoint);
}

public sealed class NoLineageSchema17FailureInjector : ILineageSchema17FailureInjector
{
    public static NoLineageSchema17FailureInjector Instance { get; } = new();
    private NoLineageSchema17FailureInjector() { }
    public void ThrowIfRequested(LineageSchema17MigrationCheckpoint checkpoint) { }
}

public sealed record LineageSchema17MigrationPrerequisites(
    IDatabaseUpgradeLease UpgradeLease,
    LineageMigrationPreflightResult Preflight,
    VerifiedLineageMigrationBackup VerifiedBackup,
    ILineageMigrationBackupVerifier BackupVerifier);

public sealed record LineageSchema17PostflightResult(
    int Roots,
    int Lines,
    int Generations,
    int GenerationLines,
    int LedgerLinks,
    int HistoricalPhysicalOutputs,
    int StrongLegacyAuditLinks);

public sealed record LineageSchema17MigrationResult(
    LineageSchema17MigrationOutcome Outcome,
    LineageSchema17PostflightResult Postflight);

/// <summary>
/// Governed schema-17 migration entry point. The normal catalogue advertises
/// version 17, but only the Data-owned startup coordinator may invoke this
/// migration because it supplies the required ownership, preflight and backup.
/// </summary>
public sealed class SqliteLineageSchema17Migrator(
    TimeProvider? timeProvider = null,
    ILineageSchema17FailureInjector? failureInjector = null)
{
    public const int SourceSchemaVersion = 16;
    public const int TargetSchemaVersion = 17;

    private readonly TimeProvider timeProvider = timeProvider ?? TimeProvider.System;
    private readonly ILineageSchema17FailureInjector failureInjector =
        failureInjector ?? NoLineageSchema17FailureInjector.Instance;

    internal Func<SqliteConnection, SqliteTransaction, Task>? BeforePublicationValidation { get; init; }
    internal Action? AfterCommit { get; init; }

    public Task<LineageSchema17MigrationResult> MigrateAsync(
        LineageSchema17MigrationPrerequisites prerequisites,
        CancellationToken cancellationToken = default) =>
        MigrateForStartupAsync(prerequisites, null, null, cancellationToken);

    internal async Task<LineageSchema17MigrationResult> MigrateForStartupAsync(
        LineageSchema17MigrationPrerequisites prerequisites,
        Func<SqliteConnection, SqliteTransaction, CancellationToken, Task>? verifySource,
        Action? schemaMutationStarting,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(prerequisites);
        await ValidatePrerequisitesAsync(prerequisites, cancellationToken);

        var path = SqliteMigrationPath.NormalizeExistingDatabase(
            prerequisites.UpgradeLease.DatabasePath);
        var connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = path,
            Mode = SqliteOpenMode.ReadWrite,
            Cache = SqliteCacheMode.Private,
            ForeignKeys = false,
            Pooling = false
        }.ConnectionString;

        await using var connection = new SqliteConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        var existingVersion = Convert.ToInt32(await ScalarAsync(
            connection, null, "SELECT Version FROM SchemaVersion WHERE Id=1;", cancellationToken));
        if (existingVersion == TargetSchemaVersion)
        {
            var complete = await ValidateAlreadyCompleteSchema17Async(
                connection,
                cancellationToken);
            return new(LineageSchema17MigrationOutcome.AlreadyComplete, complete);
        }
        if (existingVersion != SourceSchemaVersion)
            throw new InvalidOperationException("LINEAGE_MIGRATION_SOURCE_SCHEMA_UNSUPPORTED");

        await EnsureNoPartialLineageSchemaAsync(connection, cancellationToken);
        await NonQueryAsync(connection, null, "PRAGMA foreign_keys=OFF;", cancellationToken);
        await NonQueryAsync(connection, null, "PRAGMA legacy_alter_table=ON;", cancellationToken);

        await using var transaction = (SqliteTransaction)await connection.BeginTransactionAsync(cancellationToken);
        var committed = false;
        try
        {
            failureInjector.ThrowIfRequested(LineageSchema17MigrationCheckpoint.BeforeSchemaMutation);
            if (verifySource is not null) await verifySource(connection, transaction, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            schemaMutationStarting?.Invoke();
            await CreateSchemaAsync(connection, transaction, cancellationToken);

            var graph = await LegacyGraph.LoadAsync(connection, transaction, cancellationToken);
            var createdUtc = timeProvider.GetUtcNow().UtcDateTime;
            await BackfillAsync(connection, transaction, graph, prerequisites.Preflight,
                createdUtc, cancellationToken);

            failureInjector.ThrowIfRequested(LineageSchema17MigrationCheckpoint.BeforePostflight);
            if (BeforePublicationValidation is not null)
                await BeforePublicationValidation(connection, transaction);
            var postflight = await ValidatePostflightAsync(connection, transaction, cancellationToken);
            failureInjector.ThrowIfRequested(LineageSchema17MigrationCheckpoint.AfterPostflightBeforePublication);

            await NonQueryAsync(connection, transaction,
                "UPDATE SchemaVersion SET Version=17, UpdatedUtc=$utc WHERE Id=1;",
                cancellationToken, ("$utc", createdUtc.ToString("O", CultureInfo.InvariantCulture)));
            await transaction.CommitAsync(cancellationToken);
            committed = true;
            AfterCommit?.Invoke();

            await NonQueryAsync(connection, null, "PRAGMA foreign_keys=ON;", cancellationToken);
            if (await ForeignKeyViolationCountAsync(connection, null, cancellationToken) != 0)
                throw new InvalidOperationException("LINEAGE_POSTCOMMIT_FOREIGN_KEY_FAILURE");
            return new(LineageSchema17MigrationOutcome.Migrated, postflight);
        }
        catch
        {
            // Publication cannot be undone by rolling back a completed transaction.
            // Preserve the original post-COMMIT diagnosis for startup recovery.
            if (!committed)
                await transaction.RollbackAsync(CancellationToken.None);
            throw;
        }
    }

    private static async Task ValidatePrerequisitesAsync(
        LineageSchema17MigrationPrerequisites prerequisites,
        CancellationToken cancellationToken)
    {
        if (!prerequisites.UpgradeLease.PendingOperationCheckPassed)
            throw new InvalidOperationException("LINEAGE_UPGRADE_LEASE_NOT_EXCLUSIVE");
        if (prerequisites.Preflight.SchemaVersion != SourceSchemaVersion ||
            prerequisites.Preflight.Classification is not (
                LineagePreflightClassification.Migratable or LineagePreflightClassification.ReadOnly) ||
            prerequisites.Preflight.Issues.Any(x =>
                x.ReasonCode == LineagePreflightReasonCode.UnsupportedCorrectionKind) ||
            !prerequisites.Preflight.IntegrityCheckPassed ||
            !prerequisites.Preflight.ForeignKeyCheckPassed)
        {
            throw new InvalidOperationException("LINEAGE_PREFLIGHT_NOT_MIGRATABLE");
        }

        var manifest = prerequisites.VerifiedBackup.Manifest;
        if (manifest.SourceSchemaVersion != SourceSchemaVersion ||
            manifest.SourcePathIdentityHash != prerequisites.Preflight.SourceIdentityHash ||
            manifest.StructuralFingerprint != prerequisites.Preflight.StructuralFingerprint ||
            manifest.PreflightClassification != prerequisites.Preflight.Classification ||
            !File.Exists(prerequisites.VerifiedBackup.BackupPath) ||
            !File.Exists(prerequisites.VerifiedBackup.ManifestPath) ||
            !File.Exists(prerequisites.VerifiedBackup.ChecksumPath))
        {
            throw new InvalidOperationException("LINEAGE_BACKUP_NOT_VERIFIED_FOR_SOURCE");
        }

        var leaseIdentity = WindowsFileIdentity.Get(prerequisites.UpgradeLease.DatabasePath);
        if (!leaseIdentity.Equals(prerequisites.UpgradeLease.DatabaseFileIdentity, StringComparison.Ordinal) ||
            !manifest.SourceFileIdentity.Equals(leaseIdentity, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("LINEAGE_PREREQUISITE_SOURCE_IDENTITY_MISMATCH");
        }

        var verification = await prerequisites.BackupVerifier.VerifyForSourceAsync(
            prerequisites.VerifiedBackup.ManifestPath,
            prerequisites.UpgradeLease.DatabasePath,
            SourceSchemaVersion,
            cancellationToken);
        if (!verification.IsValidForExpectedSource ||
            verification.Manifest?.ArtifactId != manifest.ArtifactId ||
            verification.Manifest.BackupSha256 != manifest.BackupSha256 ||
            verification.Manifest.StructuralFingerprint != manifest.StructuralFingerprint)
            throw new InvalidOperationException("LINEAGE_BACKUP_REVERIFICATION_FAILED");
    }

    private async Task CreateSchemaAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        CancellationToken token)
    {
        await RebuildCorrectionOperationsAsync(connection, transaction, token);
        failureInjector.ThrowIfRequested(LineageSchema17MigrationCheckpoint.AfterFirstSchemaChange);
        await CreateLogicalTablesAsync(connection, transaction, token);
        await AddAuditOperationColumnAsync(connection, transaction, token);
        await RebuildMovementBatchForeignKeyAsync(connection, transaction, token);
        failureInjector.ThrowIfRequested(LineageSchema17MigrationCheckpoint.DuringMovementBatchForeignKeyRebuild);
    }

    private static async Task RebuildCorrectionOperationsAsync(
        SqliteConnection c, SqliteTransaction tx, CancellationToken token)
    {
        await NonQueryAsync(c, tx, "ALTER TABLE MovementCorrectionOperations RENAME TO __MovementCorrectionOperations_v16;", token);
        await NonQueryAsync(c, tx, SqliteLineageSchema17Definition.CorrectionOperations, token);
        await NonQueryAsync(c, tx, """

            INSERT INTO MovementCorrectionOperations
                (Id, ClientOperationId, RequestFingerprint, Kind, OriginalBatchId, ReplacementBatchId,
                 Reason, ActorUserId, ActorUsername, CreatedUtc)
            SELECT Id, ClientOperationId, RequestFingerprint, Kind, OriginalBatchId, ReplacementBatchId,
                   Reason, ActorUserId, ActorUsername, CreatedUtc
            FROM __MovementCorrectionOperations_v16;
            DROP TABLE __MovementCorrectionOperations_v16;
            """, token);
        await NonQueryAsync(c, tx, SqliteLineageSchema17Definition.CorrectionOperationIndexes, token);
    }

    private static Task CreateLogicalTablesAsync(
        SqliteConnection c, SqliteTransaction tx, CancellationToken token) =>
        NonQueryAsync(c, tx, SqliteLineageSchema17Definition.LogicalTables, token);

    private static async Task AddAuditOperationColumnAsync(
        SqliteConnection c, SqliteTransaction tx, CancellationToken token)
    {
        await NonQueryAsync(c, tx,
            "ALTER TABLE AuditEvents ADD COLUMN " + SqliteLineageSchema17Definition.AuditOperationColumn + ";",
            token);
        await NonQueryAsync(c, tx,
            SqliteLineageSchema17Definition.AuditOperationIndex,
            token);
    }

    private static async Task RebuildMovementBatchForeignKeyAsync(
        SqliteConnection c, SqliteTransaction tx, CancellationToken token)
    {
        var createSql = Convert.ToString(await ScalarAsync(c, tx,
            "SELECT sql FROM sqlite_master WHERE type='table' AND name='BinMovements';", token));
        if (string.IsNullOrWhiteSpace(createSql) ||
            !createSql.Contains("ON DELETE SET NULL", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("LINEAGE_BINMOVEMENT_BATCH_FK_SHAPE_UNEXPECTED");
        }

        var indexSql = await ReadStringsAsync(c, tx,
            "SELECT sql FROM sqlite_master WHERE type='index' AND tbl_name='BinMovements' AND sql IS NOT NULL ORDER BY name;", token);
        var columns = await ReadStringsAsync(c, tx,
            "SELECT name FROM pragma_table_info('BinMovements') ORDER BY cid;", token);
        if (columns.Count == 0 || columns.Any(x => !IsIdentifier(x)))
            throw new InvalidOperationException("LINEAGE_BINMOVEMENT_COLUMNS_UNSAFE");

        var reversalForeignKeys = Convert.ToInt32(await ScalarAsync(c, tx, """
            SELECT COUNT(*)
            FROM pragma_foreign_key_list('BinMovements')
            WHERE "from"='ReversesMovementId';
            """, token), CultureInfo.InvariantCulture);
        var exactReversalForeignKeys = Convert.ToInt32(await ScalarAsync(c, tx, """
            SELECT COUNT(*)
            FROM pragma_foreign_key_list('BinMovements')
            WHERE "from"='ReversesMovementId'
              AND "table"='BinMovements'
              AND "to"='Id'
              AND on_update='NO ACTION'
              AND on_delete='RESTRICT'
              AND match='NONE';
            """, token), CultureInfo.InvariantCulture);
        if (reversalForeignKeys is not (0 or 1) || exactReversalForeignKeys != reversalForeignKeys)
            throw new InvalidOperationException("LINEAGE_BINMOVEMENT_REVERSAL_FK_SHAPE_UNEXPECTED");

        await NonQueryAsync(c, tx, "ALTER TABLE BinMovements RENAME TO __BinMovements_v16;", token);
        var rebuiltSql = createSql.Replace("ON DELETE SET NULL", "ON DELETE RESTRICT", StringComparison.OrdinalIgnoreCase);
        if (reversalForeignKeys == 0)
        {
            var close = rebuiltSql.LastIndexOf(')');
            if (close < 0 || rebuiltSql[(close + 1)..].Trim().TrimEnd(';').Length != 0)
                throw new InvalidOperationException("LINEAGE_BINMOVEMENT_TABLE_SHAPE_UNEXPECTED");
            rebuiltSql = rebuiltSql.Insert(close, """
                , CONSTRAINT FK_BinMovements_BinMovements_ReversesMovementId
                    FOREIGN KEY (ReversesMovementId) REFERENCES BinMovements (Id) ON DELETE RESTRICT
                """);
        }
        await NonQueryAsync(c, tx, rebuiltSql, token);
        var quoted = string.Join(", ", columns.Select(x => $"\"{x}\""));
        await NonQueryAsync(c, tx,
            $"INSERT INTO BinMovements ({quoted}) SELECT {quoted} FROM __BinMovements_v16;", token);
        await NonQueryAsync(c, tx, "DROP TABLE __BinMovements_v16;", token);
        foreach (var sql in indexSql)
            await NonQueryAsync(c, tx, sql, token);
    }

    private async Task BackfillAsync(
        SqliteConnection c,
        SqliteTransaction tx,
        LegacyGraph graph,
        LineageMigrationPreflightResult preflight,
        DateTime createdUtc,
        CancellationToken token)
    {
        var utc = createdUtc.ToString("O", CultureInfo.InvariantCulture);
        var roots = graph.BuildRoots();
        var readOnlyIssues = preflight.Issues
            .Where(x => x.Classification == LineagePreflightClassification.ReadOnly)
            .ToArray();
        if (readOnlyIssues.Any(x => x.EntityType != "MovementCorrectionOperation" ||
                !x.EntityId.HasValue || roots.All(root => !root.OperationIds.Contains(x.EntityId.Value))))
        {
            throw new InvalidOperationException("LINEAGE_READONLY_REASON_NOT_ROOT_SCOPED");
        }
        foreach (var root in roots)
        {
            failureInjector.ThrowIfRequested(LineageSchema17MigrationCheckpoint.DuringRootCreation);
            var rootReadOnlyIssues = readOnlyIssues.Where(x =>
                    x.Classification == LineagePreflightClassification.ReadOnly &&
                    x.EntityType == "MovementCorrectionOperation" &&
                    x.EntityId.HasValue && root.OperationIds.Contains(x.EntityId.Value))
                .ToArray();
            var status = rootReadOnlyIssues.Length == 0 ? 1 : 2;
            var statusReason = rootReadOnlyIssues.Length == 0
                ? null
                : string.Join(',', rootReadOnlyIssues.Select(x => x.ReasonCode.ToString()).Distinct().Order());
            var rootId = await InsertReturningIdAsync(c, tx, """
                INSERT INTO LogicalMovementBatches
                    (RootMovementBatchId, Status, CurrentGenerationNumber, LineCount, StatusReasonCode, CreatedUtc)
                VALUES ($batch, $status, 0, $count, $reason, $utc)
                RETURNING Id;
                """, token, ("$batch", root.RootBatchId), ("$status", status),
                ("$count", root.Lines.Count), ("$reason", statusReason), ("$utc", utc));

            var lineRows = new List<(LegacyLine Line, long Id)>();
            for (var ordinal = 0; ordinal < root.Lines.Count; ordinal++)
            {
                failureInjector.ThrowIfRequested(LineageSchema17MigrationCheckpoint.DuringLineCreation);
                var line = root.Lines[ordinal];
                var lineId = await InsertReturningIdAsync(c, tx, """
                    INSERT INTO LogicalMovementLines
                        (LogicalMovementBatchId, RootMovementId, OriginalDisplayOrdinal, CreatedUtc)
                    VALUES ($root, $movement, $ordinal, $utc)
                    RETURNING Id;
                    """, token, ("$root", rootId), ("$movement", line.RootMovementId),
                    ("$ordinal", ordinal), ("$utc", utc));
                lineRows.Add((line, lineId));
            }

            failureInjector.ThrowIfRequested(LineageSchema17MigrationCheckpoint.DuringBaselineGenerationCreation);
            var generationId = await InsertReturningIdAsync(c, tx, """
                INSERT INTO LogicalMovementGenerations
                    (LogicalMovementBatchId, GenerationNumber, PreviousGenerationNumber,
                     MovementCorrectionOperationId, Kind, LineCount, CreatedUtc)
                VALUES ($root, 0, NULL, NULL, 1, $count, $utc)
                RETURNING Id;
                """, token, ("$root", rootId), ("$count", root.Lines.Count), ("$utc", utc));

            foreach (var (line, lineId) in lineRows)
            {
                failureInjector.ThrowIfRequested(LineageSchema17MigrationCheckpoint.DuringGenerationLineCreation);
                var state = line.TerminalReversalId.HasValue ? 1 : 0;
                var generationLineId = await InsertReturningIdAsync(c, tx, """
                    INSERT INTO LogicalMovementGenerationLines
                        (LogicalMovementBatchId, LogicalMovementGenerationId, LogicalMovementLineId,
                         State, Action, AppliedFieldMask, PreviousGenerationLineId,
                         ResultEffectiveMovementId, LastEffectiveMovementId,
                         TerminalReversalMovementId, CreatedUtc)
                    VALUES ($root, $generation, $line, $state, 1, 0, NULL,
                            $result, $last, $reversal, $utc)
                    RETURNING Id;
                    """, token, ("$root", rootId), ("$generation", generationId), ("$line", lineId),
                    ("$state", state), ("$result", state == 0 ? line.LastEffectiveMovementId : null),
                    ("$last", state == 1 ? line.LastEffectiveMovementId : null),
                    ("$reversal", line.TerminalReversalId), ("$utc", utc));

                foreach (var movement in line.Movements)
                {
                    failureInjector.ThrowIfRequested(LineageSchema17MigrationCheckpoint.DuringLedgerLinkCreation);
                    await NonQueryAsync(c, tx, """
                        INSERT INTO LogicalMovementLedgerLinks
                            (BinMovementId, LogicalMovementBatchId, LogicalMovementLineId, Role,
                             IntroducedByGenerationLineId, LegacyMovementCorrectionLineId, CreatedUtc)
                        VALUES ($movement, $root, $line, $role, $introduced, $legacy, $utc);
                        """, token, ("$movement", movement.MovementId), ("$root", rootId), ("$line", lineId),
                        ("$role", movement.Role), ("$introduced", generationLineId),
                        ("$legacy", movement.LegacyCorrectionLineId), ("$utc", utc));
                }
            }

            foreach (var operationId in root.OperationIds)
            {
                failureInjector.ThrowIfRequested(LineageSchema17MigrationCheckpoint.DuringLegacyOperationMapping);
                await NonQueryAsync(c, tx,
                    "UPDATE MovementCorrectionOperations SET LogicalMovementBatchId=$root WHERE Id=$operation;",
                    token, ("$root", rootId), ("$operation", operationId));
            }
        }

        failureInjector.ThrowIfRequested(LineageSchema17MigrationCheckpoint.DuringLegacyAuditMapping);
        await LinkStrongLegacyAuditsAsync(c, tx, graph, token);
    }

    private static async Task LinkStrongLegacyAuditsAsync(
        SqliteConnection c, SqliteTransaction tx, LegacyGraph graph, CancellationToken token)
    {
        var audits = await ReadAuditFactsAsync(c, tx, token);
        foreach (var operation in graph.Operations.Values)
        {
            var expected = graph.CorrectionLines.Where(x => x.OperationId == operation.Id)
                .Select(x => (x.OriginalMovementId, x.NeutralisingMovementId, x.ReplacementMovementId))
                .OrderBy(x => x.OriginalMovementId).ToArray();
            if (expected.Length == 0) continue;
            var entityType = operation.Kind == 1 ? "MovementBatch" : "BinMovement";
            var entityId = Convert.ToString(operation.Kind == 1
                ? operation.OriginalBatchId
                : expected[0].OriginalMovementId, CultureInfo.InvariantCulture);
            var action = operation.Kind == 1 ? "MOVEMENT_BATCH_CORRECTED" : "MOVEMENT_CORRECTED";
            var matches = audits.Where(x => x.Action == action && x.EntityType == entityType && x.EntityId == entityId)
                .Where(x => ParseCorrectionTriples(x.AfterValues).SequenceEqual(expected)).ToArray();
            if (matches.Length == 1)
            {
                await NonQueryAsync(c, tx,
                    "UPDATE AuditEvents SET MovementCorrectionOperationId=$operation WHERE Id=$audit;",
                    token, ("$operation", operation.Id), ("$audit", matches[0].Id));
            }
        }
    }

    private static (long OriginalMovementId, long NeutralisingMovementId, long ReplacementMovementId)[]
        ParseCorrectionTriples(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return [];
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.ValueKind != JsonValueKind.Array) return [];
            return doc.RootElement.EnumerateArray().Select(x => (
                    x.TryGetProperty("Id", out var original) ? original.GetInt64() : 0,
                    x.TryGetProperty("NeutralisingMovementId", out var neutral) ? neutral.GetInt64() : 0,
                    x.TryGetProperty("ReplacementMovementId", out var replacement) ? replacement.GetInt64() : 0))
                .Where(x => x.Item1 > 0 && x.Item2 > 0 && x.Item3 > 0)
                .OrderBy(x => x.Item1).ToArray();
        }
        catch (JsonException)
        {
            return [];
        }
    }

    public static async Task<LineageSchema17PostflightResult> ValidatePostflightAsync(
        SqliteConnection c, SqliteTransaction? tx, CancellationToken token = default)
    {
        var physicalOutputs = await ValidateStructuralAndCurrentHealthAsync(
            c,
            tx,
            "LINEAGE_POSTFLIGHT_TABLE_MISSING",
            "LINEAGE_POSTFLIGHT_INVARIANT_FAILURE",
            token);

        // These predicates prove what the schema-16 -> 17 migration itself published.
        // They deliberately remain stricter than ongoing schema-17 health validation:
        // native Initial roots and later native generations cannot exist in this
        // migration transaction and must never weaken MigrationBaseline proof.
        var badBaselineShape = await CountAsync(c, tx, """
            SELECT COUNT(*) FROM LogicalMovementGenerations g
            LEFT JOIN LogicalMovementBatches b ON b.Id=g.LogicalMovementBatchId
            WHERE g.GenerationNumber<>0 OR g.PreviousGenerationNumber IS NOT NULL
               OR g.MovementCorrectionOperationId IS NOT NULL OR g.Kind<>1
               OR g.LineCount<>b.LineCount;
            """, token) + await CountAsync(c, tx, """
            SELECT COUNT(*) FROM LogicalMovementGenerationLines
            WHERE Action<>1 OR AppliedFieldMask<>0 OR PreviousGenerationLineId IS NOT NULL;
            """, token);
        var badLedgerRoles = await CountAsync(c, tx, """
            SELECT COUNT(*) FROM LogicalMovementLedgerLinks ll
            JOIN BinMovements m ON m.Id=ll.BinMovementId
            WHERE (ll.Role=0 AND (m.ReversesMovementId IS NOT NULL OR ll.LegacyMovementCorrectionLineId IS NOT NULL))
               OR (ll.Role=1 AND NOT EXISTS (
                     SELECT 1 FROM MovementCorrectionLines cl
                     WHERE cl.Id=ll.LegacyMovementCorrectionLineId AND cl.NeutralisingMovementId=ll.BinMovementId))
               OR (ll.Role=2 AND NOT EXISTS (
                     SELECT 1 FROM MovementCorrectionLines cl
                     WHERE cl.Id=ll.LegacyMovementCorrectionLineId AND cl.ReplacementMovementId=ll.BinMovementId))
               OR (ll.Role=3 AND (m.ReversesMovementId IS NULL OR ll.LegacyMovementCorrectionLineId IS NOT NULL))
               OR ll.Role=4;
            """, token);
        var legacyNewFields = await CountAsync(c, tx, """
            SELECT COUNT(*) FROM MovementCorrectionOperations
            WHERE RequestJson IS NOT NULL OR RequestSchemaVersion IS NOT NULL
               OR ExpectedGenerationNumber IS NOT NULL OR ResultGenerationNumber IS NOT NULL;
            """, token);
        var inventedReceipts = await CountAsync(c, tx,
            "SELECT COUNT(*) FROM SingleMovementResponseReceipts;", token);
        if (badBaselineShape != 0 || badLedgerRoles != 0 ||
            legacyNewFields != 0 || physicalOutputs != 0 || inventedReceipts != 0)
        {
            throw new InvalidOperationException("LINEAGE_POSTFLIGHT_INVARIANT_FAILURE");
        }

        return await ReadPostflightResultAsync(c, tx, physicalOutputs, token);
    }

    private static async Task<LineageSchema17PostflightResult> ValidateAlreadyCompleteSchema17Async(
        SqliteConnection connection,
        CancellationToken cancellationToken)
    {
        var physicalOutputs = await ValidateStructuralAndCurrentHealthAsync(
            connection,
            null,
            "LINEAGE_SCHEMA17_TABLE_MISSING",
            "LINEAGE_SCHEMA17_HEALTH_INVARIANT_FAILURE",
            cancellationToken);

        return await ReadPostflightResultAsync(
            connection,
            null,
            physicalOutputs,
            cancellationToken);
    }

    internal static async Task<int> ValidateStructuralAndCurrentHealthAsync(
        SqliteConnection c,
        SqliteTransaction? tx,
        string missingTableError,
        string invariantError,
        CancellationToken token,
        Func<Exception>? healthFailure = null)
    {
        var required = new[] { "LogicalMovementBatches", "LogicalMovementLines", "LogicalMovementGenerations",
            "LogicalMovementGenerationLines", "LogicalMovementLedgerLinks", "LogicalMovementPhysicalOutputs",
            "SingleMovementResponseReceipts" };
        foreach (var table in required)
        {
            if (Convert.ToInt64(await ScalarAsync(c, tx,
                    "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name=$name;", token, ("$name", table))) != 1)
                throw healthFailure?.Invoke() ?? new InvalidOperationException(missingTableError);
        }

        // Reuse the provider-neutral committed-current authority for every projectable root.
        // Migration-publication-only checks remain in ValidatePostflightAsync and do not
        // leak into ordinary schema-17 health or current-state reads.
        await using (var roots = c.CreateCommand())
        {
            roots.Transaction = tx;
            roots.CommandText = "SELECT Id FROM LogicalMovementBatches WHERE Status IN (1,2);";
            var rootIds = new List<long>();
            await using (var reader = await roots.ExecuteReaderAsync(token))
                while (await reader.ReadAsync(token)) rootIds.Add(reader.GetInt64(0));
            foreach (var rootId in rootIds)
            {
                var current = await SqliteLogicalMovementCurrentRootResolver.ResolveInSnapshotAsync(
                    c, tx,
                    new LogicalMovementBatchId(rootId), token);
                if (current.Kind != LogicalMovementCurrentRootResolutionKind.Resolved)
                    throw healthFailure?.Invoke() ?? new InvalidOperationException(invariantError);
            }
        }

        var initializing = await CountAsync(c, tx, "SELECT COUNT(*) FROM LogicalMovementBatches WHERE Status=0;", token);
        var incompleteRoots = await CountAsync(c, tx, """
            SELECT COUNT(*) FROM LogicalMovementBatches b
            WHERE b.Status IN (1,2) AND (
                b.CurrentGenerationNumber IS NULL OR
                NOT EXISTS (SELECT 1 FROM LogicalMovementGenerations g WHERE g.LogicalMovementBatchId=b.Id AND g.GenerationNumber=b.CurrentGenerationNumber) OR
                b.LineCount <> (SELECT COUNT(*) FROM LogicalMovementLines l WHERE l.LogicalMovementBatchId=b.Id) OR
                b.LineCount <> (SELECT COUNT(*) FROM LogicalMovementGenerationLines gl JOIN LogicalMovementGenerations g ON g.Id=gl.LogicalMovementGenerationId WHERE g.LogicalMovementBatchId=b.Id AND g.GenerationNumber=b.CurrentGenerationNumber)
            );
            """, token);
        var duplicateOrForeignLines = await CountAsync(c, tx, """
            SELECT COUNT(*) FROM LogicalMovementGenerationLines gl
            LEFT JOIN LogicalMovementLines l ON l.Id=gl.LogicalMovementLineId AND l.LogicalMovementBatchId=gl.LogicalMovementBatchId
            LEFT JOIN LogicalMovementGenerations g ON g.Id=gl.LogicalMovementGenerationId AND g.LogicalMovementBatchId=gl.LogicalMovementBatchId
            WHERE l.Id IS NULL OR g.Id IS NULL;
            """, token);
        var badPointers = await CountAsync(c, tx, """
            SELECT COUNT(*) FROM LogicalMovementGenerationLines
            WHERE NOT ((State=0 AND ResultEffectiveMovementId IS NOT NULL AND LastEffectiveMovementId IS NULL AND TerminalReversalMovementId IS NULL)
                    OR (State=1 AND ResultEffectiveMovementId IS NULL AND LastEffectiveMovementId IS NOT NULL AND TerminalReversalMovementId IS NOT NULL));
            """, token);
        var badPointerOwnership = await CountAsync(c, tx, """
            SELECT COUNT(*) FROM LogicalMovementGenerationLines gl
            WHERE (gl.State=0 AND NOT EXISTS (
                       SELECT 1 FROM LogicalMovementLedgerLinks ll
                       WHERE ll.BinMovementId=gl.ResultEffectiveMovementId
                         AND ll.LogicalMovementBatchId=gl.LogicalMovementBatchId
                         AND ll.LogicalMovementLineId=gl.LogicalMovementLineId
                         AND ll.Role IN (0,2,4)))
               OR (gl.State=1 AND (
                       NOT EXISTS (SELECT 1 FROM LogicalMovementLedgerLinks ll
                                   WHERE ll.BinMovementId=gl.LastEffectiveMovementId
                                     AND ll.LogicalMovementBatchId=gl.LogicalMovementBatchId
                                     AND ll.LogicalMovementLineId=gl.LogicalMovementLineId
                                     AND ll.Role IN (0,2,4))
                    OR NOT EXISTS (SELECT 1 FROM LogicalMovementLedgerLinks ll
                                   WHERE ll.BinMovementId=gl.TerminalReversalMovementId
                                     AND ll.LogicalMovementBatchId=gl.LogicalMovementBatchId
                                     AND ll.LogicalMovementLineId=gl.LogicalMovementLineId
                                     AND ll.Role=3)));
            """, token);
        var missingIntroductions = await CountAsync(c, tx, """
            SELECT COUNT(*) FROM LogicalMovementLedgerLinks ll
            LEFT JOIN LogicalMovementGenerationLines gl ON gl.Id=ll.IntroducedByGenerationLineId
                AND gl.LogicalMovementBatchId=ll.LogicalMovementBatchId
                AND gl.LogicalMovementLineId=ll.LogicalMovementLineId
            WHERE gl.Id IS NULL;
            """, token);
        var unownedOrdinary = await CountAsync(c, tx, """
            SELECT COUNT(*) FROM BinMovements m
            WHERE m.Source IN (0,1) AND m.ImportRunId IS NULL
              AND NOT EXISTS (SELECT 1 FROM LogicalMovementLedgerLinks ll WHERE ll.BinMovementId=m.Id);
            """, token);
        var physicalOutputs = await CountAsync(c, tx, "SELECT COUNT(*) FROM LogicalMovementPhysicalOutputs;", token);
        var fkViolations = await ForeignKeyViolationCountAsync(c, tx, token);
        if (initializing != 0 || incompleteRoots != 0 || duplicateOrForeignLines != 0 ||
            badPointers != 0 || badPointerOwnership != 0 ||
            missingIntroductions != 0 || unownedOrdinary != 0 || fkViolations != 0)
        {
            throw healthFailure?.Invoke() ?? new InvalidOperationException(invariantError);
        }

        return physicalOutputs;
    }

    private static async Task<LineageSchema17PostflightResult> ReadPostflightResultAsync(
        SqliteConnection c,
        SqliteTransaction? tx,
        int physicalOutputs,
        CancellationToken token) =>
        new(
            await CountAsync(c, tx, "SELECT COUNT(*) FROM LogicalMovementBatches;", token),
            await CountAsync(c, tx, "SELECT COUNT(*) FROM LogicalMovementLines;", token),
            await CountAsync(c, tx, "SELECT COUNT(*) FROM LogicalMovementGenerations;", token),
            await CountAsync(c, tx, "SELECT COUNT(*) FROM LogicalMovementGenerationLines;", token),
            await CountAsync(c, tx, "SELECT COUNT(*) FROM LogicalMovementLedgerLinks;", token),
            physicalOutputs,
            await CountAsync(c, tx, "SELECT COUNT(*) FROM AuditEvents WHERE MovementCorrectionOperationId IS NOT NULL;", token));

    private static async Task EnsureNoPartialLineageSchemaAsync(SqliteConnection c, CancellationToken token)
    {
        var count = await CountAsync(c, null, """
            SELECT COUNT(*) FROM sqlite_master
            WHERE type='table' AND name IN ('LogicalMovementBatches','LogicalMovementLines',
                'LogicalMovementGenerations','LogicalMovementGenerationLines',
                'LogicalMovementLedgerLinks','LogicalMovementPhysicalOutputs',
                'SingleMovementResponseReceipts');
            """, token);
        if (count != 0)
            throw new InvalidOperationException("LINEAGE_PARTIAL_SCHEMA_PRESENT");
    }

    private static bool IsIdentifier(string value) =>
        value.Length > 0 && value.All(x => char.IsLetterOrDigit(x) || x == '_');

    private static async Task<int> ForeignKeyViolationCountAsync(
        SqliteConnection c, SqliteTransaction? tx, CancellationToken token)
    {
        await using var cmd = c.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText = "PRAGMA foreign_key_check;";
        await using var reader = await cmd.ExecuteReaderAsync(token);
        var count = 0;
        while (await reader.ReadAsync(token)) count++;
        return count;
    }

    private static async Task<int> CountAsync(
        SqliteConnection c, SqliteTransaction? tx, string sql, CancellationToken token) =>
        Convert.ToInt32(await ScalarAsync(c, tx, sql, token));

    private static async Task<long> InsertReturningIdAsync(
        SqliteConnection c, SqliteTransaction tx, string sql, CancellationToken token,
        params (string Name, object? Value)[] parameters) =>
        Convert.ToInt64(await ScalarAsync(c, tx, sql, token, parameters));

    private static async Task<object?> ScalarAsync(
        SqliteConnection c, SqliteTransaction? tx, string sql, CancellationToken token,
        params (string Name, object? Value)[] parameters)
    {
        await using var cmd = c.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText = sql;
        AddParameters(cmd, parameters);
        return await cmd.ExecuteScalarAsync(token);
    }

    private static async Task NonQueryAsync(
        SqliteConnection c, SqliteTransaction? tx, string sql, CancellationToken token,
        params (string Name, object? Value)[] parameters)
    {
        await using var cmd = c.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText = sql;
        AddParameters(cmd, parameters);
        await cmd.ExecuteNonQueryAsync(token);
    }

    private static void AddParameters(SqliteCommand cmd, IEnumerable<(string Name, object? Value)> parameters)
    {
        foreach (var (name, value) in parameters)
            cmd.Parameters.AddWithValue(name, value ?? DBNull.Value);
    }

    private static async Task<List<string>> ReadStringsAsync(
        SqliteConnection c, SqliteTransaction? tx, string sql, CancellationToken token)
    {
        await using var cmd = c.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText = sql;
        await using var reader = await cmd.ExecuteReaderAsync(token);
        var result = new List<string>();
        while (await reader.ReadAsync(token)) result.Add(reader.GetString(0));
        return result;
    }

    private static async Task<List<AuditFact>> ReadAuditFactsAsync(
        SqliteConnection c, SqliteTransaction tx, CancellationToken token)
    {
        await using var cmd = c.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText = "SELECT Id, Action, EntityType, EntityId, AfterValues FROM AuditEvents ORDER BY Id;";
        await using var reader = await cmd.ExecuteReaderAsync(token);
        var result = new List<AuditFact>();
        while (await reader.ReadAsync(token))
            result.Add(new(reader.GetInt64(0), reader.GetString(1), reader.GetString(2),
                reader.IsDBNull(3) ? null : reader.GetString(3), reader.IsDBNull(4) ? null : reader.GetString(4)));
        return result;
    }

    private sealed record AuditFact(long Id, string Action, string EntityType, string? EntityId, string? AfterValues);

    private sealed class LegacyGraph
    {
        public Dictionary<long, MovementFact> Movements { get; } = [];
        public Dictionary<long, OperationFact> Operations { get; } = [];
        public List<CorrectionFact> CorrectionLines { get; } = [];

        public static async Task<LegacyGraph> LoadAsync(
            SqliteConnection c, SqliteTransaction tx, CancellationToken token)
        {
            var graph = new LegacyGraph();
            await using (var cmd = c.CreateCommand())
            {
                cmd.Transaction = tx;
                cmd.CommandText = "SELECT Id, Source, MovementBatchId, ImportRunId, ReversesMovementId, CorrectedByMovementId FROM BinMovements ORDER BY Id;";
                await using var reader = await cmd.ExecuteReaderAsync(token);
                while (await reader.ReadAsync(token))
                {
                    var fact = new MovementFact(reader.GetInt64(0), reader.GetInt32(1), NullableInt64(reader, 2),
                        NullableInt64(reader, 3), NullableInt64(reader, 4), NullableInt64(reader, 5));
                    graph.Movements.Add(fact.Id, fact);
                }
            }
            await using (var cmd = c.CreateCommand())
            {
                cmd.Transaction = tx;
                cmd.CommandText = "SELECT Id, Kind, OriginalBatchId, ReplacementBatchId FROM MovementCorrectionOperations ORDER BY Id;";
                await using var reader = await cmd.ExecuteReaderAsync(token);
                while (await reader.ReadAsync(token))
                {
                    var fact = new OperationFact(reader.GetInt64(0), reader.GetInt32(1),
                        NullableInt64(reader, 2), NullableInt64(reader, 3));
                    graph.Operations.Add(fact.Id, fact);
                }
            }
            await using (var cmd = c.CreateCommand())
            {
                cmd.Transaction = tx;
                cmd.CommandText = "SELECT Id, CorrectionOperationId, OriginalMovementId, NeutralisingMovementId, ReplacementMovementId FROM MovementCorrectionLines ORDER BY Id;";
                await using var reader = await cmd.ExecuteReaderAsync(token);
                while (await reader.ReadAsync(token))
                    graph.CorrectionLines.Add(new(reader.GetInt64(0), reader.GetInt64(1), reader.GetInt64(2), reader.GetInt64(3), reader.GetInt64(4)));
            }
            return graph;
        }

        public List<LegacyRoot> BuildRoots()
        {
            var neutralisers = CorrectionLines.Select(x => x.NeutralisingMovementId).ToHashSet();
            var replacements = CorrectionLines.Select(x => x.ReplacementMovementId).ToHashSet();
            var reversalIds = Movements.Values.Where(x => x.ReversesMovementId.HasValue && !neutralisers.Contains(x.Id))
                .Select(x => x.Id).ToHashSet();
            var roots = Movements.Values.Where(IsOrdinary)
                .Where(x => !neutralisers.Contains(x.Id) && !replacements.Contains(x.Id) && !reversalIds.Contains(x.Id))
                .OrderBy(x => x.MovementBatchId ?? long.MaxValue).ThenBy(x => x.Id)
                .GroupBy(x => x.MovementBatchId.HasValue ? $"b:{x.MovementBatchId}" : $"m:{x.Id}");

            var successor = CorrectionLines.ToDictionary(x => x.OriginalMovementId);
            var reversalsByTarget = Movements.Values.Where(x => x.ReversesMovementId.HasValue && reversalIds.Contains(x.Id))
                .ToDictionary(x => x.ReversesMovementId!.Value);
            var result = new List<LegacyRoot>();
            foreach (var group in roots)
            {
                var lines = new List<LegacyLine>();
                var operations = new HashSet<long>();
                foreach (var original in group.OrderBy(x => x.Id))
                {
                    var movements = new List<LegacyOwnedMovement> { new(original.Id, 0, null) };
                    var effective = original.Id;
                    while (successor.TryGetValue(effective, out var correction))
                    {
                        operations.Add(correction.OperationId);
                        movements.Add(new(correction.NeutralisingMovementId, 1, correction.Id));
                        movements.Add(new(correction.ReplacementMovementId, 2, correction.Id));
                        effective = correction.ReplacementMovementId;
                    }
                    long? reversal = null;
                    if (reversalsByTarget.TryGetValue(effective, out var terminal))
                    {
                        reversal = terminal.Id;
                        movements.Add(new(terminal.Id, 3, null));
                    }
                    lines.Add(new(original.Id, effective, reversal, movements));
                }
                result.Add(new(group.First().MovementBatchId, lines, operations));
            }

            var owned = result.SelectMany(x => x.Lines).SelectMany(x => x.Movements)
                .Select(x => x.MovementId).ToHashSet();
            var expected = Movements.Values.Where(IsOrdinary).Select(x => x.Id).ToHashSet();
            if (!owned.SetEquals(expected))
                throw new InvalidOperationException("LINEAGE_GRAPH_OWNERSHIP_INCOMPLETE");
            foreach (var operation in Operations.Keys)
            {
                if (result.Count(x => x.OperationIds.Contains(operation)) != 1)
                    throw new InvalidOperationException("LINEAGE_OPERATION_ROOT_AMBIGUOUS");
            }
            return result;
        }

        private static bool IsOrdinary(MovementFact x) =>
            x is { ImportRunId: null } && x.Source is 0 or 1;

        private static long? NullableInt64(SqliteDataReader reader, int ordinal) =>
            reader.IsDBNull(ordinal) ? null : reader.GetInt64(ordinal);
    }

    private static long? NullableInt64(SqliteDataReader reader, int ordinal) =>
        reader.IsDBNull(ordinal) ? null : reader.GetInt64(ordinal);

    private sealed record MovementFact(long Id, int Source, long? MovementBatchId, long? ImportRunId,
        long? ReversesMovementId, long? CorrectedByMovementId);
    private sealed record OperationFact(long Id, int Kind, long? OriginalBatchId, long? ReplacementBatchId);
    private sealed record CorrectionFact(long Id, long OperationId, long OriginalMovementId,
        long NeutralisingMovementId, long ReplacementMovementId);
    private sealed record LegacyOwnedMovement(long MovementId, int Role, long? LegacyCorrectionLineId);
    private sealed record LegacyLine(long RootMovementId, long LastEffectiveMovementId,
        long? TerminalReversalId, IReadOnlyList<LegacyOwnedMovement> Movements);
    private sealed record LegacyRoot(long? RootBatchId, IReadOnlyList<LegacyLine> Lines,
        IReadOnlySet<long> OperationIds);
}
