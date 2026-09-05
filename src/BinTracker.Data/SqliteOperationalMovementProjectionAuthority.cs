using System.Data;
using System.Globalization;
using BinTracker.Core;
using Microsoft.Data.Sqlite;

namespace BinTracker.Data;

/// <summary>
/// Explicit dormant schema-17 projection implementation. Normal application composition does
/// not register it; callers must opt into this SQLite adapter deliberately.
/// </summary>
public sealed class SqliteOperationalMovementProjectionAuthority(string connectionString)
    : IOperationalMovementProjectionAuthority
{
    private readonly string connectionString = string.IsNullOrWhiteSpace(connectionString)
        ? throw new ArgumentException("A SQLite connection string is required.", nameof(connectionString))
        : connectionString;

    public async Task<OperationalMovementProjectionResult> QueryAsync(
        OperationalMovementProjectionScope scope,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(scope);
        await using var connection = new SqliteConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var transaction = (SqliteTransaction)await connection.BeginTransactionAsync(
            IsolationLevel.Serializable, cancellationToken);

        if (await ScalarInt64Async(connection, transaction,
                "SELECT Version FROM SchemaVersion WHERE Id=1;", cancellationToken) !=
            SqliteLineageSchema17Migrator.TargetSchemaVersion)
            throw Failure(OperationalMovementProjectionFailure.SchemaUnavailable,
                "The corrected operational projection requires explicit schema 17 composition.");

        // Candidate relevance, full validation, projection and aggregation all consume this one
        // transaction snapshot. No raw-ledger fallback is available after any integrity failure.
        var roots = await ReadRootIdsAsync(connection, transaction, cancellationToken);
        var lines = await ReadLinesAsync(connection, transaction, cancellationToken);
        var generationLines = await ReadGenerationLinesAsync(connection, transaction, cancellationToken);
        var links = await ReadLinksAsync(connection, transaction, cancellationToken);
        var movements = await ReadMovementsAsync(connection, transaction, cancellationToken);
        var importRuns = await ReadImportRunIdsAsync(connection, transaction, cancellationToken);

        var rootSet = roots.ToHashSet();
        var structuralRootIds = lines.Select(x => x.RootId)
            .Concat(generationLines.Select(x => x.RootId))
            .Concat(links.Select(x => x.RootId)).Distinct().ToArray();
        foreach (var orphanRootId in structuralRootIds.Where(x => !rootSet.Contains(x)))
        {
            var orphanInfluences = InfluenceMovementIds(orphanRootId, lines, generationLines, links)
                .Select(id => Influence(movements.GetValueOrDefault(id))).ToArray();
            if (OperationalMovementProjectionSemantics.IsRelevant(scope, orphanInfluences))
                throw Failure(OperationalMovementProjectionFailure.RelevantLineageInvalid,
                    "Relevant lineage structure refers to a missing logical root.");
        }

        var projected = new List<ProjectedOperationalMovement>();
        foreach (var rootId in roots)
        {
            var influenceIds = InfluenceMovementIds(rootId, lines, generationLines, links).ToArray();
            var influences = influenceIds.Select(id => Influence(movements.GetValueOrDefault(id))).ToArray();
            if (!OperationalMovementProjectionSemantics.IsRelevant(scope, influences)) continue;

            ValidateRelevantRootEvidence(rootId, influenceIds, lines, generationLines, links, movements);
            var resolution = await SqliteLogicalMovementCurrentRootResolver.ResolveInSnapshotAsync(
                connection, transaction, new(rootId), cancellationToken);
            if (resolution.Kind != LogicalMovementCurrentRootResolutionKind.Resolved || resolution.Root is null)
                throw Failure(OperationalMovementProjectionFailure.RelevantLineageInvalid,
                    $"Relevant logical root {rootId} is not projectable: {resolution.Kind}/{resolution.Failure}.");

            var facts = influenceIds.Distinct().ToDictionary(id => id,
                id => movements[id].Fact ?? throw Failure(
                    OperationalMovementProjectionFailure.RelevantLineageInvalid,
                    $"Relevant logical root {rootId} contains a malformed movement fact."));
            projected.AddRange(OperationalMovementProjectionSemantics.ProjectLineageRoot(
                resolution.Root, facts));
        }

        var linksByMovement = links.GroupBy(x => x.MovementId)
            .ToDictionary(x => x.Key, x => x.Count());
        foreach (var raw in movements.Values)
        {
            if (!OperationalMovementProjectionSemantics.IsRelevant(scope, [Influence(raw)])) continue;
            if (raw.Fact is not { } fact)
                throw Failure(OperationalMovementProjectionFailure.UnknownRelevance,
                    $"Movement {raw.Id} has malformed facts and cannot be proven irrelevant.");

            var ownershipCount = linksByMovement.GetValueOrDefault(raw.Id);
            if (fact.Source is MovementSource.Manual or MovementSource.Batch)
            {
                if (fact.ImportRunId is not null || ownershipCount != 1)
                    throw Failure(OperationalMovementProjectionFailure.UnexpectedUnrootedOrdinary,
                        $"Ordinary movement {raw.Id} is not owned exactly once by generic lineage.");
                continue;
            }

            if (fact.Source is not (MovementSource.Adjustment or MovementSource.ExcelImport) ||
                ownershipCount != 0 || fact.ReversesMovementId is not null ||
                fact.ImportRunId is { } importRunId && !importRuns.Contains(importRunId))
                throw Failure(OperationalMovementProjectionFailure.InvalidExcludedDomain,
                    $"Excluded-domain movement {raw.Id} has invalid provenance or lineage ownership.");
            projected.Add(OperationalMovementProjectionSemantics.ProjectExcluded(fact));
        }

        var result = OperationalMovementProjectionSemantics.Complete(scope, projected);
        await transaction.CommitAsync(cancellationToken);
        return result;
    }

    private static void ValidateRelevantRootEvidence(long rootId, IReadOnlyCollection<long> influenceIds,
        IReadOnlyCollection<RawLine> lines, IReadOnlyCollection<RawGenerationLine> generationLines,
        IReadOnlyCollection<RawLink> links, IReadOnlyDictionary<long, RawMovement> movements)
    {
        if (influenceIds.Count == 0 || influenceIds.Any(id => !movements.TryGetValue(id, out var row) || row.Fact is null))
            throw Failure(OperationalMovementProjectionFailure.RelevantLineageInvalid,
                $"Relevant logical root {rootId} has missing or malformed influence evidence.");

        var lineIds = lines.Where(x => x.RootId == rootId).Select(x => x.Id).ToHashSet();
        var introductions = generationLines.ToDictionary(x => x.Id);
        var rootLinks = links.Where(x => x.RootId == rootId).ToArray();
        foreach (var influenceId in influenceIds)
        {
            if (movements[influenceId].Fact is not { } influenceFact ||
                influenceFact.Source is not (MovementSource.Manual or MovementSource.Batch) ||
                influenceFact.ImportRunId is not null ||
                rootLinks.Count(x => x.MovementId == influenceId) != 1)
                throw Failure(OperationalMovementProjectionFailure.RelevantLineageInvalid,
                    $"Relevant logical root {rootId} does not own every influence fact exactly once.");
        }

        foreach (var link in rootLinks)
        {
            if (!lineIds.Contains(link.LineId) ||
                !Enum.IsDefined(typeof(LogicalMovementTransformationRole), link.Role) ||
                link.IntroducedByGenerationLineId is not { } introductionId ||
                !introductions.TryGetValue(introductionId, out var introduction) ||
                introduction.RootId != rootId || introduction.LineId != link.LineId ||
                movements[link.MovementId].Fact is not { } fact ||
                fact.Source is not (MovementSource.Manual or MovementSource.Batch) ||
                fact.ImportRunId is not null)
                throw Failure(OperationalMovementProjectionFailure.RelevantLineageInvalid,
                    $"Relevant logical root {rootId} has invalid evidence ownership.");

            var role = (LogicalMovementTransformationRole)link.Role;
            if ((role is LogicalMovementTransformationRole.CorrectionNeutraliser or
                    LogicalMovementTransformationRole.OrdinaryReversal) &&
                (fact.Source != MovementSource.Manual || fact.MovementBatchId is not null ||
                 fact.ReversesMovementId is null))
                throw Failure(OperationalMovementProjectionFailure.RelevantLineageInvalid,
                    $"Relevant logical root {rootId} has malformed neutralising evidence.");
        }
    }

    private static IEnumerable<long> InfluenceMovementIds(long rootId,
        IEnumerable<RawLine> lines, IEnumerable<RawGenerationLine> generationLines,
        IEnumerable<RawLink> links) =>
        lines.Where(x => x.RootId == rootId).Select(x => x.RootMovementId)
            .Concat(links.Where(x => x.RootId == rootId).Select(x => x.MovementId))
            .Concat(generationLines.Where(x => x.RootId == rootId).SelectMany(x =>
                new long?[] { x.ResultEffectiveMovementId, x.LastEffectiveMovementId,
                    x.TerminalReversalMovementId }.Where(id => id is not null).Select(id => id!.Value)))
            .Distinct();

    private static OperationalMovementInfluence Influence(RawMovement? row) => row is null
        ? new(null, null, null)
        : new(row.MovementDate, row.CustomerId > 0 ? row.CustomerId : null,
            row.ContainerTypeId > 0 ? row.ContainerTypeId : null);

    private static async Task<List<long>> ReadRootIdsAsync(SqliteConnection connection,
        SqliteTransaction transaction, CancellationToken token)
    {
        await using var command = Command(connection, transaction,
            "SELECT Id FROM LogicalMovementBatches ORDER BY Id;");
        await using var reader = await command.ExecuteReaderAsync(token);
        var result = new List<long>();
        while (await reader.ReadAsync(token)) result.Add(reader.GetInt64(0));
        return result;
    }

    private static async Task<List<RawLine>> ReadLinesAsync(SqliteConnection connection,
        SqliteTransaction transaction, CancellationToken token)
    {
        await using var command = Command(connection, transaction,
            "SELECT Id,LogicalMovementBatchId,RootMovementId FROM LogicalMovementLines;");
        await using var reader = await command.ExecuteReaderAsync(token);
        var result = new List<RawLine>();
        while (await reader.ReadAsync(token))
            result.Add(new(reader.GetInt64(0), reader.GetInt64(1), reader.GetInt64(2)));
        return result;
    }

    private static async Task<List<RawGenerationLine>> ReadGenerationLinesAsync(
        SqliteConnection connection, SqliteTransaction transaction, CancellationToken token)
    {
        await using var command = Command(connection, transaction, """
            SELECT Id,LogicalMovementBatchId,LogicalMovementLineId,
                   ResultEffectiveMovementId,LastEffectiveMovementId,TerminalReversalMovementId
            FROM LogicalMovementGenerationLines;
            """);
        await using var reader = await command.ExecuteReaderAsync(token);
        var result = new List<RawGenerationLine>();
        while (await reader.ReadAsync(token))
            result.Add(new(reader.GetInt64(0), reader.GetInt64(1), reader.GetInt64(2),
                NullableInt64(reader, 3), NullableInt64(reader, 4), NullableInt64(reader, 5)));
        return result;
    }

    private static async Task<List<RawLink>> ReadLinksAsync(SqliteConnection connection,
        SqliteTransaction transaction, CancellationToken token)
    {
        await using var command = Command(connection, transaction, """
            SELECT BinMovementId,LogicalMovementBatchId,LogicalMovementLineId,Role,IntroducedByGenerationLineId
            FROM LogicalMovementLedgerLinks;
            """);
        await using var reader = await command.ExecuteReaderAsync(token);
        var result = new List<RawLink>();
        while (await reader.ReadAsync(token))
            result.Add(new(reader.GetInt64(0), reader.GetInt64(1), reader.GetInt64(2),
                reader.GetInt32(3), NullableInt64(reader, 4)));
        return result;
    }

    private static async Task<Dictionary<long, RawMovement>> ReadMovementsAsync(
        SqliteConnection connection, SqliteTransaction transaction, CancellationToken token)
    {
        await using var command = Command(connection, transaction, """
            SELECT Id,MovementDate,MovementType,Source,CustomerId,ContainerTypeId,Quantity,
                   ReferenceNumber,Notes,CreatedBy,CreatedUtc,MovementBatchId,ImportRunId,ReversesMovementId
            FROM BinMovements;
            """);
        await using var reader = await command.ExecuteReaderAsync(token);
        var result = new Dictionary<long, RawMovement>();
        while (await reader.ReadAsync(token))
        {
            var id = reader.GetInt64(0);
            var dateText = Convert.ToString(reader.GetValue(1), CultureInfo.InvariantCulture);
            DateOnly? date = DateOnly.TryParse(dateText, CultureInfo.InvariantCulture,
                DateTimeStyles.None, out var parsedDate) ? parsedDate : null;
            var createdText = Convert.ToString(reader.GetValue(10), CultureInfo.InvariantCulture);
            DateTime? created = DateTime.TryParse(createdText, CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind, out var parsedCreated) ? parsedCreated : null;
            var direction = reader.GetInt32(2);
            var source = reader.GetInt32(3);
            var customer = reader.GetInt32(4);
            var container = reader.GetInt32(5);
            var quantity = reader.GetInt32(6);
            OperationalMovementFact? fact = null;
            if (id > 0 && date is not null && created is not null &&
                Enum.IsDefined(typeof(MovementType), direction) &&
                Enum.IsDefined(typeof(MovementSource), source) &&
                customer > 0 && container > 0 && quantity > 0)
                fact = new(id, date.Value, (MovementType)direction, (MovementSource)source,
                    customer, container, quantity, NullableString(reader, 7), NullableString(reader, 8),
                    NullableString(reader, 9), created.Value, NullableInt32(reader, 11),
                    NullableInt64(reader, 12), NullableInt64(reader, 13));
            if (!result.TryAdd(id, new(id, date, customer, container, fact)))
                throw Failure(OperationalMovementProjectionFailure.UnknownRelevance,
                    "Duplicate movement evidence identity encountered.");
        }
        return result;
    }

    private static async Task<HashSet<long>> ReadImportRunIdsAsync(SqliteConnection connection,
        SqliteTransaction transaction, CancellationToken token)
    {
        await using var command = Command(connection, transaction, "SELECT Id FROM ImportRuns;");
        await using var reader = await command.ExecuteReaderAsync(token);
        var result = new HashSet<long>();
        while (await reader.ReadAsync(token)) result.Add(reader.GetInt64(0));
        return result;
    }

    private static async Task<long> ScalarInt64Async(SqliteConnection connection,
        SqliteTransaction transaction, string sql, CancellationToken token)
    {
        await using var command = Command(connection, transaction, sql);
        return Convert.ToInt64(await command.ExecuteScalarAsync(token), CultureInfo.InvariantCulture);
    }

    private static SqliteCommand Command(SqliteConnection connection,
        SqliteTransaction transaction, string sql)
    {
        var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = sql;
        return command;
    }

    private static OperationalMovementProjectionException Failure(
        OperationalMovementProjectionFailure failure, string message) => new(failure, message);
    private static int? NullableInt32(SqliteDataReader reader, int ordinal) =>
        reader.IsDBNull(ordinal) ? null : reader.GetInt32(ordinal);
    private static long? NullableInt64(SqliteDataReader reader, int ordinal) =>
        reader.IsDBNull(ordinal) ? null : reader.GetInt64(ordinal);
    private static string? NullableString(SqliteDataReader reader, int ordinal) =>
        reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);

    private sealed record RawLine(long Id, long RootId, long RootMovementId);
    private sealed record RawGenerationLine(long Id, long RootId, long LineId,
        long? ResultEffectiveMovementId, long? LastEffectiveMovementId,
        long? TerminalReversalMovementId);
    private sealed record RawLink(long MovementId, long RootId, long LineId, int Role,
        long? IntroducedByGenerationLineId);
    private sealed record RawMovement(long Id, DateOnly? MovementDate, int CustomerId,
        int ContainerTypeId, OperationalMovementFact? Fact);
}
