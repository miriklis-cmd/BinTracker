using System.Security.Cryptography;
using System.Text;
using Microsoft.Data.Sqlite;

namespace BinTracker.Data;

internal static class SqliteStartupInspection
{
    internal static string ConnectionString(string path, bool readOnly = true) => new SqliteConnectionStringBuilder
    {
        DataSource = path, Mode = readOnly ? SqliteOpenMode.ReadOnly : SqliteOpenMode.ReadWrite,
        Cache = SqliteCacheMode.Private, Pooling = false, ForeignKeys = true
    }.ConnectionString;

    internal static async Task<SqliteConnection> OpenAsync(string path, CancellationToken token)
    {
        var c = new SqliteConnection(ConnectionString(path));
        try { await c.OpenAsync(token); return c; }
        catch { await c.DisposeAsync(); throw; }
    }

    internal static async Task<StartupDatabaseObservation> InspectAsync(SqliteConnection c,
        SqliteTransaction tx, string identity, CancellationToken token)
    {
        try
        {
            var tables = await StringsAsync(c, tx, "SELECT name FROM sqlite_master WHERE type='table'", token);
            int? version = null;
            if (tables.Contains("SchemaVersion"))
            {
                await using var cmd = c.CreateCommand();
                cmd.Transaction = tx;
                cmd.CommandText = "SELECT Id,Version,typeof(Version) FROM SchemaVersion";
                await using var reader = await cmd.ExecuteReaderAsync(token);
                if (!await reader.ReadAsync(token) || reader.GetInt64(0) != 1 || reader.GetString(2) != "integer")
                    return Bad();
                var number = reader.GetInt64(1);
                if (number < 0 || number > int.MaxValue || await reader.ReadAsync(token)) return Bad();
                version = (int)number;
                if (version > 17) return new(StartupDatabaseState.UnsupportedFutureSchema, version, identity);
            }

            if (tables.Any(t => t.StartsWith("__", StringComparison.Ordinal))) return Bad(version);
            if (version != 17)
            {
                if (tables.Any(t => t.StartsWith("LogicalMovement", StringComparison.Ordinal))) return Bad(version);
                foreach (var (table, columns) in new[]
                {
                    ("MovementCorrectionOperations", new[] { "RequestJson", "RequestSchemaVersion", "LogicalMovementBatchId", "ExpectedGenerationNumber", "ResultGenerationNumber" }),
                    ("AuditEvents", new[] { "MovementCorrectionOperationId" })
                })
                {
                    if (!tables.Contains(table)) continue;
                    var actual = await StringsAsync(c, tx, $"SELECT name FROM pragma_table_info('{table}')", token);
                    if (columns.Any(actual.Contains)) return Bad(version);
                }
            }
            // A missing version table is supported only for the recognizable original
            // baseline; an empty or interrupted bootstrap is never silently recreated.
            var minimum = new[] { "Customers", "ContainerTypes", "BinMovements", "ApplicationSettings" };
            if (minimum.Any(t => !tables.Contains(t))) return Bad(version);
            if (version == 16 && new[] { "MovementBatches", "MovementCorrectionOperations", "MovementCorrectionLines",
                    "AuditEvents", "ImportRuns", "UserAccounts", "ReminderDeliveries" }.Any(t => !tables.Contains(t)))
                return Bad(version);
            return new(version switch
            {
                17 => StartupDatabaseState.Schema17ValidationRequired,
                16 => StartupDatabaseState.Schema16ActivationRequired,
                _ => StartupDatabaseState.Pre16UpgradeRequired
            }, version ?? 0, identity);
        }
        catch (SqliteException ex) when (ex.SqliteErrorCode is 1 or 11 or 26)
        {
            return Bad();
        }

        StartupDatabaseObservation Bad(int? version = null) => new(StartupDatabaseState.PartialOrCorrupt, version, identity);
    }

    internal static async Task<List<string>> StringsAsync(SqliteConnection c, SqliteTransaction? tx,
        string sql, CancellationToken token)
    {
        await using var cmd = c.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText = sql;
        var result = new List<string>();
        await using var reader = await cmd.ExecuteReaderAsync(token);
        while (await reader.ReadAsync(token)) result.Add(reader.GetString(0));
        return result;
    }

    internal static async Task RequireEquivalentAsync(string source, string backup, CancellationToken token)
    {
        var sourceHash = await FingerprintAsync(source, token);
        var backupHash = await FingerprintAsync(backup, token);
        if (!CryptographicOperations.FixedTimeEquals(sourceHash, backupHash))
            throw new StartupFault(StartupDatabaseFailure.SourceChanged);
    }

    internal static async Task RequireEquivalentAsync(SqliteConnection source, SqliteTransaction tx,
        string backup, CancellationToken token)
    {
        var sourceHash = await FingerprintAsync(source, tx, token);
        var backupHash = await FingerprintAsync(backup, token);
        if (!CryptographicOperations.FixedTimeEquals(sourceHash, backupHash))
            throw new StartupFault(StartupDatabaseFailure.SourceChanged);
    }

    private static async Task<byte[]> FingerprintAsync(string path, CancellationToken token)
    {
        await using var c = await OpenAsync(path, token);
        await using var tx = c.BeginTransaction(System.Data.IsolationLevel.Serializable, deferred: true);
        return await FingerprintAsync(c, tx, token);
    }

    private static async Task<byte[]> FingerprintAsync(SqliteConnection c, SqliteTransaction tx, CancellationToken token)
    {
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        var declarations = await StringsAsync(c, tx,
            "SELECT type || ':' || name || ':' || coalesce(sql,'') FROM sqlite_master ORDER BY type,name", token);
        foreach (var declaration in declarations) Append(Encoding.UTF8.GetBytes(declaration));
        var tables = await StringsAsync(c, tx, "SELECT name FROM sqlite_master WHERE type='table' ORDER BY name", token);
        foreach (var table in tables)
        {
            Append(Encoding.UTF8.GetBytes(table));
            await using var cmd = c.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText = $"SELECT * FROM \"{table.Replace("\"", "\"\"")}\" ORDER BY rowid";
            await using var reader = await cmd.ExecuteReaderAsync(token);
            while (await reader.ReadAsync(token))
            {
                for (var i = 0; i < reader.FieldCount; i++)
                {
                    var value = reader.GetValue(i);
                    Append(Encoding.UTF8.GetBytes(value.GetType().FullName ?? string.Empty));
                    Append(value is byte[] bytes ? bytes : Encoding.UTF8.GetBytes(
                        Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty));
                }
            }
        }
        return hash.GetHashAndReset();

        void Append(byte[] bytes)
        {
            hash.AppendData(BitConverter.GetBytes(bytes.Length));
            hash.AppendData(bytes);
        }
    }
}
