using System.Collections.ObjectModel;
using System.Data;
using BinTracker.Core;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace BinTracker.Data;

/// <summary>
/// Dormant schema-17 materializer. It binds the validated CURRENT root and every
/// mutation fact under one SQLite snapshot and is intentionally not registered.
/// </summary>
internal sealed class SqliteMovementPlanningSnapshotMaterializer
{
    internal SqliteMovementPlanningSnapshotMaterializer(string connectionString) =>
        this.connectionString = string.IsNullOrWhiteSpace(connectionString)
            ? throw new ArgumentException("A SQLite connection string is required.", nameof(connectionString))
            : connectionString;

    private readonly string connectionString;
    public async Task<TrustedMovementPlanningSnapshot> MaterializeAsync(
        LogicalMovementBatchId logicalMovementBatchId, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqliteConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var transaction = (SqliteTransaction)await connection.BeginTransactionAsync(
            IsolationLevel.Serializable, cancellationToken);

        var snapshot = await MaterializeAsync(
            connection, transaction, logicalMovementBatchId, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return snapshot;
    }

    internal static Task<TrustedMovementPlanningSnapshot> MaterializeAsync(
        BinTrackerDbContext db, LogicalMovementBatchId logicalMovementBatchId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(db);
        if (db.Database.CurrentTransaction is null ||
            db.Database.GetDbConnection() is not SqliteConnection connection ||
            db.Database.CurrentTransaction.GetDbTransaction() is not SqliteTransaction transaction ||
            connection.State != ConnectionState.Open)
        {
            throw new InvalidOperationException(
                "The SQLite planning materializer requires the caller's active SQLite transaction.");
        }

        return MaterializeAsync(connection, transaction, logicalMovementBatchId, cancellationToken);
    }

    private static async Task<TrustedMovementPlanningSnapshot> MaterializeAsync(
        SqliteConnection connection, SqliteTransaction transaction,
        LogicalMovementBatchId logicalMovementBatchId, CancellationToken cancellationToken)
    {
        var resolution = await SqliteLogicalMovementCurrentRootResolver.ResolveInSnapshotAsync(
            connection, transaction, logicalMovementBatchId, cancellationToken);
        if (resolution.Kind != LogicalMovementCurrentRootResolutionKind.Resolved || resolution.Root is null)
            throw new InvalidOperationException($"Logical movement root is not plannable: {resolution.Kind}/{resolution.Failure}.");

        var root = resolution.Root;
        if (root.Status == LogicalMovementBatchStatus.ReadOnly)
            throw new InvalidOperationException("MOVEMENT_MUTATION_ROOT_READ_ONLY");
        var ids = root.Lines.SelectMany(x => x.TerminalReversalMovementId is { } terminal
                ? new[] { x.EffectiveMovementId!.Value, terminal }
                : new[] { x.EffectiveMovementId!.Value })
            .Distinct().ToArray();
        var facts = await ReadMovementsAsync(connection, transaction, ids, cancellationToken);
        if (facts.Count != ids.Length || !facts.Keys.ToHashSet().SetEquals(ids))
            throw new InvalidOperationException("Current movement facts are missing, duplicated or surplus.");

        var lines = new List<TrustedMovementPlanningLine>(root.Lines.Count);
        foreach (var line in root.Lines)
        {
            var effective = facts[line.EffectiveMovementId!.Value];
            MovementBusinessState? reversal = line.TerminalReversalMovementId is { } terminal ? facts[terminal] : null;
            if (effective.MovementId != line.EffectiveMovementId || reversal?.MovementId != line.TerminalReversalMovementId)
                throw new InvalidOperationException("Current movement facts do not match validated pointers.");
            lines.Add(new(line, effective, reversal));
        }

        var customers = await ReadActiveIdsAsync(connection, transaction, "Customers", cancellationToken);
        var containers = await ReadActiveIdsAsync(connection, transaction, "ContainerTypes", cancellationToken);
        var snapshot = new TrustedMovementPlanningSnapshot(root,
            new ReadOnlyCollection<TrustedMovementPlanningLine>(lines), customers, containers);
        // A planning snapshot is exposed as trusted only after the current movement pair has
        // been cross-proven against immutable business facts, not merely lineage role labels.
        MovementMutationPlanner.ValidateTrustedSnapshotFacts(snapshot);
        return snapshot;
    }

    private static async Task<IReadOnlyDictionary<long, MovementBusinessState>> ReadMovementsAsync(
        SqliteConnection connection, SqliteTransaction transaction, IReadOnlyList<long> ids,
        CancellationToken cancellationToken)
    {
        if (ids.Count == 0) throw new InvalidOperationException("A plannable root has no current movement identities.");
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        var names = new string[ids.Count];
        for (var i = 0; i < ids.Count; i++)
        {
            names[i] = $"$id{i}";
            command.Parameters.AddWithValue(names[i], ids[i]);
        }
        command.CommandText = $"""
            SELECT Id, MovementDate, MovementType, Source, CustomerId, ContainerTypeId, Quantity,
                   ReferenceNumber, Notes, MovementBatchId, ImportRunId, ReversesMovementId
            FROM BinMovements
            WHERE Id IN ({string.Join(",", names)});
            """;
        var result = new Dictionary<long, MovementBusinessState>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var id = reader.GetInt64(0);
            if (!DateOnly.TryParse(reader.GetString(1), out var date) ||
                !Enum.IsDefined(typeof(MovementType), reader.GetInt32(2)) ||
                !Enum.IsDefined(typeof(MovementSource), reader.GetInt32(3)) ||
                reader.GetInt32(4) <= 0 || reader.GetInt32(5) <= 0 || reader.GetInt32(6) <= 0 ||
                !result.TryAdd(id, new(id, date, (MovementType)reader.GetInt32(2),
                    (MovementSource)reader.GetInt32(3), reader.GetInt32(4), reader.GetInt32(5), reader.GetInt32(6),
                    reader.IsDBNull(7) ? null : reader.GetString(7), reader.IsDBNull(8) ? null : reader.GetString(8),
                    reader.IsDBNull(9) ? null : reader.GetInt32(9), reader.IsDBNull(10) ? null : reader.GetInt64(10),
                    reader.IsDBNull(11) ? null : reader.GetInt64(11))))
                throw new InvalidOperationException("A current movement fact is malformed or duplicated.");
        }
        return new ReadOnlyDictionary<long, MovementBusinessState>(result);
    }

    private static async Task<IReadOnlySet<int>> ReadActiveIdsAsync(SqliteConnection connection,
        SqliteTransaction transaction, string table, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = $"SELECT Id FROM {table} WHERE IsActive=1;";
        var result = new HashSet<int>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
            if (!result.Add(reader.GetInt32(0))) throw new InvalidOperationException($"Duplicate active identity in {table}.");
        return new ImmutableIntSet(result);
    }

    private sealed class ImmutableIntSet(HashSet<int> values) : IReadOnlySet<int>
    {
        public int Count => values.Count;
        public bool Contains(int item) => values.Contains(item);
        public IEnumerator<int> GetEnumerator() => values.GetEnumerator();
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
        public bool IsProperSubsetOf(IEnumerable<int> other) => values.IsProperSubsetOf(other);
        public bool IsProperSupersetOf(IEnumerable<int> other) => values.IsProperSupersetOf(other);
        public bool IsSubsetOf(IEnumerable<int> other) => values.IsSubsetOf(other);
        public bool IsSupersetOf(IEnumerable<int> other) => values.IsSupersetOf(other);
        public bool Overlaps(IEnumerable<int> other) => values.Overlaps(other);
        public bool SetEquals(IEnumerable<int> other) => values.SetEquals(other);
    }
}
