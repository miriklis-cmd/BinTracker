using System.Data;
using BinTracker.Core;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace BinTracker.Data;

/// <summary>
/// Provider-neutral intent boundary for attaching generation-zero lineage to
/// physical movements created by the existing movement-entry authority.
/// </summary>
public interface IInitialMovementLineageWriter
{
    bool IsEnabled { get; }

    Task EnsureReadyAsync(
        BinTrackerDbContext db,
        CancellationToken cancellationToken = default);

    Task ValidateExistingSingleAsync(
        BinTrackerDbContext db,
        long movementId,
        CancellationToken cancellationToken = default);

    Task ValidateExistingBatchAsync(
        BinTrackerDbContext db,
        int movementBatchId,
        CancellationToken cancellationToken = default);

    Task WriteInitialAsync(
        BinTrackerDbContext db,
        int? rootMovementBatchId,
        IReadOnlyList<long> orderedRootMovementIds,
        DateTime createdUtc,
        CancellationToken cancellationToken = default);
}

/// <summary>Preserves the current schema-16 runtime without probing lineage capability.</summary>
public sealed class DormantInitialMovementLineageWriter : IInitialMovementLineageWriter
{
    public bool IsEnabled => false;

    public Task EnsureReadyAsync(BinTrackerDbContext db, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task ValidateExistingSingleAsync(
        BinTrackerDbContext db, long movementId, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task ValidateExistingBatchAsync(
        BinTrackerDbContext db, int movementBatchId, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task WriteInitialAsync(
        BinTrackerDbContext db, int? rootMovementBatchId,
        IReadOnlyList<long> orderedRootMovementIds, DateTime createdUtc,
        CancellationToken cancellationToken = default) => Task.CompletedTask;
}

internal enum InitialMovementLineageWriteCheckpoint
{
    AfterRootInserted = 0,
    AfterRootOriginalLinksInsertedBeforeIntroductionUpdate = 1,
    AfterFinalValidation = 2
}

internal interface IInitialMovementLineageFailureInjector
{
    void ThrowIfRequested(InitialMovementLineageWriteCheckpoint checkpoint);
}

internal sealed class NoInitialMovementLineageFailureInjector : IInitialMovementLineageFailureInjector
{
    internal static NoInitialMovementLineageFailureInjector Instance { get; } = new();
    private NoInitialMovementLineageFailureInjector() { }
    public void ThrowIfRequested(InitialMovementLineageWriteCheckpoint checkpoint) { }
}

/// <summary>
/// Dormant SQLite schema-17 implementation. The caller owns the supplied
/// context, transaction, SaveChanges boundaries and transaction completion.
/// </summary>
internal sealed class SqliteInitialMovementLineageWriter(
    IInitialMovementLineageFailureInjector failureInjector) : IInitialMovementLineageWriter
{
    private const string SchemaRequired = "INITIAL_MOVEMENT_LINEAGE_SCHEMA17_REQUIRED";
    private const string InvalidExisting = "INITIAL_MOVEMENT_LINEAGE_EXISTING_ROOT_INVALID";
    private const string PersistenceFailure = "INITIAL_MOVEMENT_LINEAGE_PERSISTENCE_FAILURE";

    public bool IsEnabled => true;

    public async Task EnsureReadyAsync(
        BinTrackerDbContext db,
        CancellationToken cancellationToken = default)
    {
        var (connection, transaction) = RequireTransaction(db);
        try
        {
            await using var version = Command(
                connection, transaction, "SELECT Version FROM SchemaVersion WHERE Id=1;");
            if (Convert.ToInt32(await version.ExecuteScalarAsync(cancellationToken)) !=
                SqliteLineageSchema17Migrator.TargetSchemaVersion)
            {
                throw new InvalidOperationException(SchemaRequired);
            }

            await SqliteLineageSchema17Migrator.ValidateStructuralAndCurrentHealthAsync(
                connection,
                transaction,
                "INITIAL_MOVEMENT_LINEAGE_SCHEMA17_TABLE_MISSING",
                "INITIAL_MOVEMENT_LINEAGE_SCHEMA17_HEALTH_INVALID",
                cancellationToken);
        }
        catch (SqliteException ex)
        {
            throw new InvalidOperationException(SchemaRequired, ex);
        }
    }

    public async Task ValidateExistingSingleAsync(
        BinTrackerDbContext db,
        long movementId,
        CancellationToken cancellationToken = default)
    {
        var (connection, transaction) = RequireTransaction(db);
        try
        {
            var roots = await ReadInt64sAsync(connection, transaction, """
                SELECT LogicalMovementBatchId
                FROM LogicalMovementLines
                WHERE RootMovementId=$movement;
                """, cancellationToken, ("$movement", movementId));
            if (roots.Count != 1)
                throw new InvalidOperationException(InvalidExisting);

            var resolved = await ResolveAsync(connection, transaction, roots[0], cancellationToken);
            if (resolved.RootMovementBatchId is not null || resolved.Lines.Count != 1 ||
                resolved.Lines[0].RootMovementId != movementId)
            {
                throw new InvalidOperationException(InvalidExisting);
            }
        }
        catch (SqliteException ex)
        {
            throw new InvalidOperationException(PersistenceFailure, ex);
        }
    }

    public async Task ValidateExistingBatchAsync(
        BinTrackerDbContext db,
        int movementBatchId,
        CancellationToken cancellationToken = default)
    {
        var (connection, transaction) = RequireTransaction(db);
        try
        {
            var roots = await ReadInt64sAsync(connection, transaction, """
                SELECT Id
                FROM LogicalMovementBatches
                WHERE RootMovementBatchId=$batch;
                """, cancellationToken, ("$batch", movementBatchId));
            if (roots.Count != 1)
                throw new InvalidOperationException(InvalidExisting);

            var resolved = await ResolveAsync(connection, transaction, roots[0], cancellationToken);
            var physical = await ReadInt64sAsync(connection, transaction, """
                SELECT Id
                FROM BinMovements
                WHERE MovementBatchId=$batch;
                """, cancellationToken, ("$batch", movementBatchId));
            if (resolved.RootMovementBatchId != movementBatchId ||
                !resolved.Lines.Select(x => x.RootMovementId).ToHashSet().SetEquals(physical))
            {
                throw new InvalidOperationException(InvalidExisting);
            }
        }
        catch (SqliteException ex)
        {
            throw new InvalidOperationException(PersistenceFailure, ex);
        }
    }

    public async Task WriteInitialAsync(
        BinTrackerDbContext db,
        int? rootMovementBatchId,
        IReadOnlyList<long> orderedRootMovementIds,
        DateTime createdUtc,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(orderedRootMovementIds);
        if (orderedRootMovementIds.Count == 0 || orderedRootMovementIds.Any(x => x <= 0) ||
            orderedRootMovementIds.Distinct().Count() != orderedRootMovementIds.Count)
        {
            throw new ArgumentException("Authoritative physical movement IDs are required.",
                nameof(orderedRootMovementIds));
        }

        var (connection, transaction) = RequireTransaction(db);
        try
        {
            var rootId = await ScalarInt64Async(connection, transaction, """
                INSERT INTO LogicalMovementBatches
                    (RootMovementBatchId,Status,CurrentGenerationNumber,LineCount,StatusReasonCode,CreatedUtc)
                VALUES ($batch,0,NULL,$count,NULL,$utc)
                RETURNING Id;
                """, cancellationToken,
                ("$batch", rootMovementBatchId), ("$count", orderedRootMovementIds.Count), ("$utc", createdUtc));
            failureInjector.ThrowIfRequested(InitialMovementLineageWriteCheckpoint.AfterRootInserted);

            var lineIds = new long[orderedRootMovementIds.Count];
            for (var ordinal = 0; ordinal < orderedRootMovementIds.Count; ordinal++)
            {
                lineIds[ordinal] = await ScalarInt64Async(connection, transaction, """
                    INSERT INTO LogicalMovementLines
                        (LogicalMovementBatchId,RootMovementId,OriginalDisplayOrdinal,CreatedUtc)
                    VALUES ($root,$movement,$ordinal,$utc)
                    RETURNING Id;
                    """, cancellationToken,
                    ("$root", rootId), ("$movement", orderedRootMovementIds[ordinal]),
                    ("$ordinal", ordinal), ("$utc", createdUtc));
            }

            var generationId = await ScalarInt64Async(connection, transaction, """
                INSERT INTO LogicalMovementGenerations
                    (LogicalMovementBatchId,GenerationNumber,PreviousGenerationNumber,
                     MovementCorrectionOperationId,Kind,LineCount,CreatedUtc)
                VALUES ($root,0,NULL,NULL,$kind,$count,$utc)
                RETURNING Id;
                """, cancellationToken,
                ("$root", rootId), ("$kind", (int)LogicalMovementGenerationAction.Initial),
                ("$count", orderedRootMovementIds.Count), ("$utc", createdUtc));

            var generationLineIds = new long[lineIds.Length];
            for (var ordinal = 0; ordinal < lineIds.Length; ordinal++)
            {
                generationLineIds[ordinal] = await ScalarInt64Async(connection, transaction, """
                    INSERT INTO LogicalMovementGenerationLines
                        (LogicalMovementBatchId,LogicalMovementGenerationId,LogicalMovementLineId,
                         State,Action,AppliedFieldMask,PreviousGenerationLineId,
                         ResultEffectiveMovementId,LastEffectiveMovementId,TerminalReversalMovementId,CreatedUtc)
                    VALUES ($root,$generation,$line,$state,$action,$mask,NULL,$movement,NULL,NULL,$utc)
                    RETURNING Id;
                    """, cancellationToken,
                    ("$root", rootId), ("$generation", generationId), ("$line", lineIds[ordinal]),
                    ("$state", (int)LogicalMovementLineState.Active),
                    ("$action", (int)LogicalMovementGenerationAction.Initial),
                    ("$mask", (int)MovementChangeField.None),
                    ("$movement", orderedRootMovementIds[ordinal]), ("$utc", createdUtc));
            }

            for (var ordinal = 0; ordinal < lineIds.Length; ordinal++)
            {
                await NonQueryExactlyOneAsync(connection, transaction, """
                    INSERT INTO LogicalMovementLedgerLinks
                        (BinMovementId,LogicalMovementBatchId,LogicalMovementLineId,Role,
                         IntroducedByGenerationLineId,LegacyMovementCorrectionLineId,CreatedUtc)
                    VALUES ($movement,$root,$line,$role,NULL,NULL,$utc);
                    """, cancellationToken,
                    ("$movement", orderedRootMovementIds[ordinal]), ("$root", rootId),
                    ("$line", lineIds[ordinal]),
                    ("$role", (int)LogicalMovementTransformationRole.RootOriginal), ("$utc", createdUtc));
            }

            failureInjector.ThrowIfRequested(
                InitialMovementLineageWriteCheckpoint.AfterRootOriginalLinksInsertedBeforeIntroductionUpdate);

            for (var ordinal = 0; ordinal < lineIds.Length; ordinal++)
            {
                await NonQueryExactlyOneAsync(connection, transaction, """
                    UPDATE LogicalMovementLedgerLinks
                    SET IntroducedByGenerationLineId=$generationLine
                    WHERE BinMovementId=$movement AND LogicalMovementBatchId=$root
                      AND LogicalMovementLineId=$line AND IntroducedByGenerationLineId IS NULL;
                    """, cancellationToken,
                    ("$generationLine", generationLineIds[ordinal]),
                    ("$movement", orderedRootMovementIds[ordinal]), ("$root", rootId),
                    ("$line", lineIds[ordinal]));
            }

            await ValidateConstructionAsync(connection, transaction, rootId,
                orderedRootMovementIds, cancellationToken);

            await NonQueryExactlyOneAsync(connection, transaction, """
                UPDATE LogicalMovementBatches
                SET Status=$active,CurrentGenerationNumber=0
                WHERE Id=$root AND Status=$initializing AND CurrentGenerationNumber IS NULL;
                """, cancellationToken,
                ("$active", (int)LogicalMovementBatchStatus.Active), ("$root", rootId),
                ("$initializing", (int)LogicalMovementBatchStatus.Initializing));

            var resolved = await ResolveAsync(connection, transaction, rootId, cancellationToken);
            if (resolved.RootMovementBatchId != rootMovementBatchId ||
                !resolved.Lines.Select(x => x.RootMovementId).SequenceEqual(orderedRootMovementIds))
            {
                throw new InvalidOperationException(InvalidExisting);
            }

            await SqliteLineageSchema17Migrator.ValidateStructuralAndCurrentHealthAsync(
                connection,
                transaction,
                "INITIAL_MOVEMENT_LINEAGE_SCHEMA17_TABLE_MISSING",
                "INITIAL_MOVEMENT_LINEAGE_SCHEMA17_HEALTH_INVALID",
                cancellationToken);
            failureInjector.ThrowIfRequested(InitialMovementLineageWriteCheckpoint.AfterFinalValidation);
        }
        catch (SqliteException ex)
        {
            throw new InvalidOperationException(PersistenceFailure, ex);
        }
    }

    private static async Task<ValidatedLogicalMovementCurrentRoot> ResolveAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        long rootId,
        CancellationToken cancellationToken)
    {
        var resolution = await SqliteLogicalMovementCurrentRootResolver.ResolveInSnapshotAsync(
            connection, transaction, new LogicalMovementBatchId(rootId), cancellationToken);
        if (resolution.Kind != LogicalMovementCurrentRootResolutionKind.Resolved || resolution.Root is null)
            throw new InvalidOperationException(InvalidExisting);
        return resolution.Root;
    }

    private static async Task ValidateConstructionAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        long rootId,
        IReadOnlyList<long> expectedOrderedMovementIds,
        CancellationToken cancellationToken)
    {
        LogicalMovementBatch root;
        await using (var command = Command(connection, transaction, """
            SELECT Id,RootMovementBatchId,Status,CurrentGenerationNumber,LineCount,StatusReasonCode,CreatedUtc
            FROM LogicalMovementBatches WHERE Id=$root;
            """, ("$root", rootId)))
        await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
        {
            if (!await reader.ReadAsync(cancellationToken))
                throw new InvalidOperationException(PersistenceFailure);
            root = new LogicalMovementBatch
            {
                Id = reader.GetInt64(0),
                RootMovementBatchId = NullableInt32(reader, 1),
                Status = (LogicalMovementBatchStatus)reader.GetInt32(2),
                CurrentGenerationNumber = NullableInt32(reader, 3),
                LineCount = reader.GetInt32(4),
                StatusReasonCode = reader.IsDBNull(5) ? null : reader.GetString(5),
                CreatedUtc = reader.GetDateTime(6)
            };
        }

        var lines = new List<LogicalMovementLine>();
        await using (var command = Command(connection, transaction, """
            SELECT Id,LogicalMovementBatchId,RootMovementId,OriginalDisplayOrdinal,CreatedUtc
            FROM LogicalMovementLines WHERE LogicalMovementBatchId=$root;
            """, ("$root", rootId)))
        await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
                lines.Add(new()
                {
                    Id = reader.GetInt64(0), LogicalMovementBatchId = reader.GetInt64(1),
                    RootMovementId = reader.GetInt64(2), OriginalDisplayOrdinal = reader.GetInt32(3),
                    CreatedUtc = reader.GetDateTime(4)
                });
        }

        var generations = new List<LogicalMovementGeneration>();
        await using (var command = Command(connection, transaction, """
            SELECT Id,LogicalMovementBatchId,GenerationNumber,PreviousGenerationNumber,
                   MovementCorrectionOperationId,Kind,LineCount,CreatedUtc
            FROM LogicalMovementGenerations WHERE LogicalMovementBatchId=$root;
            """, ("$root", rootId)))
        await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
                generations.Add(new()
                {
                    Id = reader.GetInt64(0), LogicalMovementBatchId = reader.GetInt64(1),
                    GenerationNumber = reader.GetInt32(2), PreviousGenerationNumber = NullableInt32(reader, 3),
                    MovementCorrectionOperationId = NullableInt64(reader, 4),
                    Kind = (LogicalMovementGenerationAction)reader.GetInt32(5),
                    LineCount = reader.GetInt32(6), CreatedUtc = reader.GetDateTime(7)
                });
        }

        var generationLines = new List<LogicalMovementGenerationLine>();
        await using (var command = Command(connection, transaction, """
            SELECT Id,LogicalMovementBatchId,LogicalMovementGenerationId,LogicalMovementLineId,
                   State,Action,AppliedFieldMask,PreviousGenerationLineId,ResultEffectiveMovementId,
                   LastEffectiveMovementId,TerminalReversalMovementId,CreatedUtc
            FROM LogicalMovementGenerationLines WHERE LogicalMovementBatchId=$root;
            """, ("$root", rootId)))
        await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
                generationLines.Add(new()
                {
                    Id = reader.GetInt64(0), LogicalMovementBatchId = reader.GetInt64(1),
                    LogicalMovementGenerationId = reader.GetInt64(2), LogicalMovementLineId = reader.GetInt64(3),
                    State = (LogicalMovementLineState)reader.GetInt32(4),
                    Action = (LogicalMovementGenerationAction)reader.GetInt32(5),
                    AppliedFieldMask = (MovementChangeField)reader.GetInt32(6),
                    PreviousGenerationLineId = NullableInt64(reader, 7),
                    ResultEffectiveMovementId = NullableInt64(reader, 8),
                    LastEffectiveMovementId = NullableInt64(reader, 9),
                    TerminalReversalMovementId = NullableInt64(reader, 10), CreatedUtc = reader.GetDateTime(11)
                });
        }

        var ledgerLinks = new List<LogicalMovementLedgerLink>();
        await using (var command = Command(connection, transaction, """
            SELECT BinMovementId,LogicalMovementBatchId,LogicalMovementLineId,Role,
                   IntroducedByGenerationLineId,LegacyMovementCorrectionLineId,CreatedUtc
            FROM LogicalMovementLedgerLinks WHERE LogicalMovementBatchId=$root;
            """, ("$root", rootId)))
        await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
                ledgerLinks.Add(new()
                {
                    BinMovementId = reader.GetInt64(0), LogicalMovementBatchId = reader.GetInt64(1),
                    LogicalMovementLineId = reader.GetInt64(2),
                    Role = (LogicalMovementTransformationRole)reader.GetInt32(3),
                    IntroducedByGenerationLineId = NullableInt64(reader, 4),
                    LegacyMovementCorrectionLineId = NullableInt64(reader, 5), CreatedUtc = reader.GetDateTime(6)
                });
        }

        var physical = await ReadPhysicalMovementBatchIdsAsync(
            connection, transaction, expectedOrderedMovementIds, cancellationToken);
        var batchMembers = root.RootMovementBatchId is { } batchId
            ? (await ReadInt64sAsync(connection, transaction,
                "SELECT Id FROM BinMovements WHERE MovementBatchId=$batch;",
                cancellationToken, ("$batch", batchId))).ToHashSet()
            : [];
        var operations = await CountAsync(connection, transaction,
            "SELECT COUNT(*) FROM MovementCorrectionOperations WHERE LogicalMovementBatchId=$root;",
            cancellationToken, ("$root", rootId));
        var outputs = await CountAsync(connection, transaction,
            "SELECT COUNT(*) FROM LogicalMovementPhysicalOutputs WHERE LogicalMovementBatchId=$root;",
            cancellationToken, ("$root", rootId));

        LogicalMovementInitialConstructionValidator.Validate(
            root, lines, generations, generationLines, ledgerLinks, expectedOrderedMovementIds,
            physical, batchMembers, operations, outputs);
    }

    private static async Task<IReadOnlyDictionary<long, int?>> ReadPhysicalMovementBatchIdsAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        IReadOnlyList<long> movementIds,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        var parameters = new string[movementIds.Count];
        for (var index = 0; index < movementIds.Count; index++)
        {
            parameters[index] = $"$movement{index}";
            command.Parameters.AddWithValue(parameters[index], movementIds[index]);
        }
        command.CommandText = $"SELECT Id,MovementBatchId FROM BinMovements WHERE Id IN ({string.Join(',', parameters)});";
        var result = new Dictionary<long, int?>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
            result.Add(reader.GetInt64(0), NullableInt32(reader, 1));
        return result;
    }

    private static (SqliteConnection Connection, SqliteTransaction Transaction) RequireTransaction(
        BinTrackerDbContext db)
    {
        ArgumentNullException.ThrowIfNull(db);
        if (db.Database.CurrentTransaction is null ||
            db.Database.GetDbConnection() is not SqliteConnection connection ||
            db.Database.CurrentTransaction.GetDbTransaction() is not SqliteTransaction transaction ||
            connection.State != ConnectionState.Open)
        {
            throw new InvalidOperationException(
                "The SQLite initial-lineage writer requires the caller's active SQLite transaction.");
        }
        return (connection, transaction);
    }

    private static async Task<long> ScalarInt64Async(
        SqliteConnection connection, SqliteTransaction transaction, string sql,
        CancellationToken cancellationToken, params (string Name, object? Value)[] parameters)
    {
        await using var command = Command(connection, transaction, sql, parameters);
        return Convert.ToInt64(await command.ExecuteScalarAsync(cancellationToken));
    }

    private static async Task<int> CountAsync(
        SqliteConnection connection, SqliteTransaction transaction, string sql,
        CancellationToken cancellationToken, params (string Name, object? Value)[] parameters) =>
        Convert.ToInt32(await ScalarInt64Async(connection, transaction, sql, cancellationToken, parameters));

    private static async Task NonQueryExactlyOneAsync(
        SqliteConnection connection, SqliteTransaction transaction, string sql,
        CancellationToken cancellationToken, params (string Name, object? Value)[] parameters)
    {
        await using var command = Command(connection, transaction, sql, parameters);
        if (await command.ExecuteNonQueryAsync(cancellationToken) != 1)
            throw new InvalidOperationException(PersistenceFailure);
    }

    private static async Task<List<long>> ReadInt64sAsync(
        SqliteConnection connection, SqliteTransaction transaction, string sql,
        CancellationToken cancellationToken, params (string Name, object? Value)[] parameters)
    {
        await using var command = Command(connection, transaction, sql, parameters);
        var result = new List<long>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
            result.Add(reader.GetInt64(0));
        return result;
    }

    private static SqliteCommand Command(
        SqliteConnection connection, SqliteTransaction transaction, string sql,
        params (string Name, object? Value)[] parameters)
    {
        var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = sql;
        foreach (var (name, value) in parameters)
            command.Parameters.AddWithValue(name, value ?? DBNull.Value);
        return command;
    }

    private static int? NullableInt32(SqliteDataReader reader, int ordinal) =>
        reader.IsDBNull(ordinal) ? null : reader.GetInt32(ordinal);

    private static long? NullableInt64(SqliteDataReader reader, int ordinal) =>
        reader.IsDBNull(ordinal) ? null : reader.GetInt64(ordinal);
}
