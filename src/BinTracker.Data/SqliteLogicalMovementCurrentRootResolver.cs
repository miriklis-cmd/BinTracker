using System.Data;
using BinTracker.Core;
using Microsoft.Data.Sqlite;

namespace BinTracker.Data;

/// <summary>
/// Dormant schema-17 reader. It is intentionally not registered by production startup.
/// </summary>
public sealed class SqliteLogicalMovementCurrentRootResolver(string connectionString)
    : ILogicalMovementCurrentRootResolver
{
    public async Task<LogicalMovementCurrentRootResolution> ResolveAsync(
        LogicalMovementBatchId logicalMovementBatchId,
        CancellationToken cancellationToken = default)
    {
        await using var connection = new SqliteConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        // All raw structured state is captured under one SQLite snapshot. Validation below performs no lazy reads.
        await using var transaction = (SqliteTransaction)await connection.BeginTransactionAsync(
            IsolationLevel.Serializable, cancellationToken);

        var result = await ResolveInSnapshotAsync(connection, transaction, logicalMovementBatchId, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return result;
    }

    internal static async Task<LogicalMovementCurrentRootResolution> ResolveInSnapshotAsync(
        SqliteConnection connection,
        SqliteTransaction? transaction,
        LogicalMovementBatchId logicalMovementBatchId,
        CancellationToken cancellationToken)
    {
        var root = await ReadRootAsync(connection, transaction, logicalMovementBatchId.Value, cancellationToken);
        if (root is null)
            return LogicalMovementCurrentRootResolution.NotFound();

        var lines = await ReadLinesAsync(connection, transaction, root.Id, cancellationToken);
        // CurrentGenerationNumber is the sole current selection authority. Ordinary reads deliberately do not scan history.
        var generations = root.CurrentGenerationNumber is null
            ? []
            : await ReadGenerationAsync(connection, transaction, root.Id, root.CurrentGenerationNumber.Value, cancellationToken);
        var currentLines = generations.Count == 1
            ? await ReadCurrentLinesAsync(connection, transaction, root.Id, generations[0].Id, cancellationToken)
            : [];
        var proofMovementIds = lines.Select(x => x.RootMovementId)
            .Concat(currentLines.SelectMany(CurrentPointers)).Distinct().ToArray();
        var links = await ReadLinksAsync(connection, transaction, root.Id, proofMovementIds, cancellationToken);
        var introductions = await ReadIntroductionsAsync(connection, transaction, links, cancellationToken);
        var movements = await ReadMovementsAsync(connection, transaction, proofMovementIds, cancellationToken);
        var batches = await ReadExistingMovementBatchesAsync(connection, transaction,
            root.RootMovementBatchId is { } batchId ? [batchId] : [], cancellationToken);
        // Raw persistence rows never escape this validation boundary as operational truth.
        return LogicalMovementCurrentRootValidator.Validate(new(logicalMovementBatchId.Value, root, lines,
            generations, currentLines, links, introductions, movements, batches));
    }

    private static IEnumerable<long> CurrentPointers(RawLogicalMovementGenerationLine line)
    {
        if (line.ResultEffectiveMovementId is { } result) yield return result;
        if (line.LastEffectiveMovementId is { } last) yield return last;
        if (line.TerminalReversalMovementId is { } reversal) yield return reversal;
    }

    private static async Task<RawLogicalMovementRoot?> ReadRootAsync(SqliteConnection c, SqliteTransaction? tx, long id, CancellationToken token)
    {
        await using var cmd = Command(c, tx, "SELECT Id,RootMovementBatchId,Status,StatusReasonCode,CurrentGenerationNumber,LineCount FROM LogicalMovementBatches WHERE Id=$id;", ("$id", id));
        await using var r = await cmd.ExecuteReaderAsync(token);
        return await r.ReadAsync(token) ? new(r.GetInt64(0), NullableInt32(r, 1), r.GetInt32(2),
            r.IsDBNull(3) ? null : r.GetString(3), NullableInt32(r, 4), r.GetInt32(5)) : null;
    }

    private static async Task<List<RawLogicalMovementLine>> ReadLinesAsync(SqliteConnection c, SqliteTransaction? tx, long rootId, CancellationToken token)
    {
        await using var cmd = Command(c, tx, "SELECT Id,LogicalMovementBatchId,RootMovementId,OriginalDisplayOrdinal FROM LogicalMovementLines WHERE LogicalMovementBatchId=$id;", ("$id", rootId));
        await using var r = await cmd.ExecuteReaderAsync(token);var rows=new List<RawLogicalMovementLine>();
        while(await r.ReadAsync(token)) rows.Add(new(r.GetInt64(0),r.GetInt64(1),r.GetInt64(2),r.GetInt32(3)));return rows;
    }

    private static async Task<List<RawLogicalMovementGeneration>> ReadGenerationAsync(SqliteConnection c, SqliteTransaction? tx, long rootId, int number, CancellationToken token)
    {
        await using var cmd=Command(c,tx,"SELECT Id,LogicalMovementBatchId,GenerationNumber,LineCount FROM LogicalMovementGenerations WHERE LogicalMovementBatchId=$id AND GenerationNumber=$number;",("$id",rootId),("$number",number));
        await using var r=await cmd.ExecuteReaderAsync(token);var rows=new List<RawLogicalMovementGeneration>();while(await r.ReadAsync(token))rows.Add(new(r.GetInt64(0),r.GetInt64(1),r.GetInt32(2),r.GetInt32(3)));return rows;
    }

    private static async Task<List<RawLogicalMovementGenerationLine>> ReadCurrentLinesAsync(SqliteConnection c, SqliteTransaction? tx, long rootId, long generationId, CancellationToken token)
    {
        await using var cmd=Command(c,tx,"SELECT Id,LogicalMovementBatchId,LogicalMovementGenerationId,LogicalMovementLineId,State,ResultEffectiveMovementId,LastEffectiveMovementId,TerminalReversalMovementId FROM LogicalMovementGenerationLines WHERE LogicalMovementGenerationId=$generation;",("$generation",generationId));
        await using var r=await cmd.ExecuteReaderAsync(token);var rows=new List<RawLogicalMovementGenerationLine>();while(await r.ReadAsync(token))rows.Add(new(r.GetInt64(0),r.GetInt64(1),r.GetInt64(2),r.GetInt64(3),r.GetInt32(4),NullableInt64(r,5),NullableInt64(r,6),NullableInt64(r,7)));return rows;
    }

    private static async Task<List<RawLogicalMovementLedgerLink>> ReadLinksAsync(SqliteConnection c,
        SqliteTransaction? tx, long rootId, IReadOnlyList<long> movementIds, CancellationToken token)
    {
        if (movementIds.Count == 0) return [];
        await using var cmd=InCommand(c,tx,"SELECT BinMovementId,LogicalMovementBatchId,LogicalMovementLineId,Role,IntroducedByGenerationLineId FROM LogicalMovementLedgerLinks WHERE LogicalMovementBatchId=$root AND BinMovementId IN (",movementIds, ("$root", rootId));
        await using var r=await cmd.ExecuteReaderAsync(token);var rows=new List<RawLogicalMovementLedgerLink>();while(await r.ReadAsync(token))rows.Add(new(r.GetInt64(0),r.GetInt64(1),r.GetInt64(2),r.GetInt32(3),NullableInt64(r,4)));return rows;
    }

    private static async Task<List<RawLogicalMovementIntroduction>> ReadIntroductionsAsync(SqliteConnection c, SqliteTransaction? tx, IReadOnlyList<RawLogicalMovementLedgerLink> links, CancellationToken token)
    {
        var ids=links.Where(x=>x.IntroducedByGenerationLineId is not null).Select(x=>x.IntroducedByGenerationLineId!.Value).Distinct().ToArray();
        if(ids.Length==0)return [];
        await using var cmd=InCommand(c,tx,"SELECT Id,LogicalMovementBatchId,LogicalMovementLineId FROM LogicalMovementGenerationLines WHERE Id IN (",ids);
        await using var r=await cmd.ExecuteReaderAsync(token);var rows=new List<RawLogicalMovementIntroduction>();while(await r.ReadAsync(token))rows.Add(new(r.GetInt64(0),r.GetInt64(1),r.GetInt64(2)));return rows;
    }

    private static async Task<Dictionary<long, RawLogicalMovementFact>> ReadMovementsAsync(SqliteConnection c,
        SqliteTransaction? tx, IReadOnlyList<long> ids, CancellationToken token)
    {
        if(ids.Count==0)return [];
        await using var cmd=InCommand(c,tx,"SELECT Id,MovementBatchId FROM BinMovements WHERE Id IN (",ids);await using var r=await cmd.ExecuteReaderAsync(token);var rows=new Dictionary<long,RawLogicalMovementFact>();while(await r.ReadAsync(token)){var fact=new RawLogicalMovementFact(r.GetInt64(0),NullableInt32(r,1));rows.Add(fact.Id,fact);}return rows;
    }

    private static async Task<HashSet<int>> ReadExistingMovementBatchesAsync(SqliteConnection c,
        SqliteTransaction? tx, IReadOnlyList<int> ids, CancellationToken token)
    {
        if(ids.Count==0)return [];
        await using var cmd=InCommand(c,tx,"SELECT Id FROM MovementBatches WHERE Id IN (",ids.Select(x=>(long)x).ToArray());await using var r=await cmd.ExecuteReaderAsync(token);var rows=new HashSet<int>();while(await r.ReadAsync(token))rows.Add(r.GetInt32(0));return rows;
    }

    private static SqliteCommand InCommand(SqliteConnection c, SqliteTransaction? tx, string prefix,
        IReadOnlyList<long> ids, params (string Name, object Value)[] args)
    {
        var cmd=c.CreateCommand();cmd.Transaction=tx;foreach(var arg in args)cmd.Parameters.AddWithValue(arg.Name,arg.Value);var names=new string[ids.Count];for(var i=0;i<ids.Count;i++){names[i]="$p"+i;cmd.Parameters.AddWithValue(names[i],ids[i]);}cmd.CommandText=prefix+string.Join(',',names)+");";return cmd;
    }

    private static SqliteCommand Command(SqliteConnection c, SqliteTransaction? tx, string sql, params (string Name, object Value)[] args)
    {var cmd=c.CreateCommand();cmd.Transaction=tx;cmd.CommandText=sql;foreach(var arg in args)cmd.Parameters.AddWithValue(arg.Name,arg.Value);return cmd;}
    private static int? NullableInt32(SqliteDataReader r,int ordinal)=>r.IsDBNull(ordinal)?null:r.GetInt32(ordinal);
    private static long? NullableInt64(SqliteDataReader r,int ordinal)=>r.IsDBNull(ordinal)?null:r.GetInt64(ordinal);
}
