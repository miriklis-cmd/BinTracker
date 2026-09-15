using System.Text.RegularExpressions;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace BinTracker.Data;

/// <summary>Provider shape inspection only; lineage semantics remain in existing validators.</summary>
internal static partial class SqliteSchema17Capabilities
{
    internal static async Task ValidateAsync(SqliteConnection actual, SqliteTransaction tx, CancellationToken token)
    {
        // This empty in-memory DDL reference describes required capabilities, not a
        // schema16 database or migration prerequisite. No preflight, backup, baseline
        // population or EnsureCreated runs on the native17 path.
        await using var reference = new SqliteConnection("Data Source=:memory:;Foreign Keys=False;Pooling=False");
        await reference.OpenAsync(token);
        await using (var create = reference.CreateCommand())
        {
            create.CommandText = SqliteLineageSchema17Definition.CorrectionOperations +
                SqliteLineageSchema17Definition.LogicalTables +
                SqliteLineageSchema17Definition.CorrectionOperationIndexes +
                "CREATE TABLE AuditEvents(Id INTEGER NOT NULL PRIMARY KEY," +
                SqliteLineageSchema17Definition.AuditOperationColumn + ");" +
                SqliteLineageSchema17Definition.AuditOperationIndex + """
                CREATE TABLE BinMovements(Id INTEGER NOT NULL PRIMARY KEY,
                    MovementBatchId INTEGER NULL REFERENCES MovementBatches(Id) ON DELETE RESTRICT);
                """;
            await create.ExecuteNonQueryAsync(token);
        }
        try
        {
            var expected = await ReadShapesAsync(reference, null, token);
            var observed = await ReadShapesAsync(actual, tx, token);
            foreach (var (name, required) in expected)
            {
                if (!observed.TryGetValue(name, out var shape) ||
                    required.Columns.Any(column => !shape.Columns.Contains(column)) ||
                    !required.PrimaryKey.SequenceEqual(shape.PrimaryKey) ||
                    required.ForeignKeys.Any(fk => !shape.ForeignKeys.Contains(fk)) ||
                    required.UniqueKeys.Any(key => !shape.UniqueKeys.Contains(key)) ||
                    required.Checks.Any(check => !shape.Checks.Contains(check)))
                    throw new StartupFault(StartupDatabaseFailure.StructuralCapabilityMissing);
            }
            foreach (var name in new[] { "Customers", "ContainerTypes", "MovementBatches", "ApplicationSettings",
                "ImportRuns", "MovementCorrectionLines", "UserAccounts", "ReminderDeliveries", "SchemaVersion" })
            {
                if (!observed.TryGetValue(name, out var shape) || !shape.PrimaryKey.SequenceEqual(["Id"]))
                    throw new StartupFault(StartupDatabaseFailure.StructuralCapabilityMissing);
            }
            ValidateBase(observed, immutableMembership: true);
            var integrity = await SqliteStartupInspection.StringsAsync(actual, tx, "PRAGMA integrity_check", token);
            if (!integrity.SequenceEqual(["ok"])) throw new StartupFault(StartupDatabaseFailure.PartialOrCorrupt);
        }
        catch (SqliteException ex) when (ex.SqliteErrorCode is 1 or 11 or 26)
        {
            throw new StartupFault(StartupDatabaseFailure.StructuralCapabilityMissing, ex);
        }
    }

    internal static async Task ValidateBaseAsync(SqliteConnection c, SqliteTransaction tx, CancellationToken token)
    {
        ValidateBase(await ReadShapesAsync(c, tx, token), immutableMembership: false);
    }

    private static void ValidateBase(Dictionary<string, Shape> observed, bool immutableMembership)
    {
        // The shared model remains the column authority for accepted base tables.
        // Membership delete action is catalogue-specific: schema16 preserves its
        // accepted persisted SET NULL behavior, while schema17 requires immutable
        // RESTRICT membership. EF's client behavior must not redefine either DDL.
        using var model = new BinTrackerDbContext(new DbContextOptionsBuilder<BinTrackerDbContext>()
            .UseSqlite("Data Source=:memory:").Options);
        foreach (var entity in model.Model.GetEntityTypes())
        {
            var table = entity.GetTableName() ?? throw new StartupFault(StartupDatabaseFailure.StructuralCapabilityMissing);
            if (!observed.TryGetValue(table, out var shape))
                throw new StartupFault(StartupDatabaseFailure.StructuralCapabilityMissing);
            var store = StoreObjectIdentifier.Table(table, entity.GetSchema());
            var primary = entity.FindPrimaryKey()?.Properties.Select(p => p.GetColumnName(store)).ToArray();
            if (primary is null || !shape.PrimaryKey.SequenceEqual(primary))
                throw new StartupFault(StartupDatabaseFailure.StructuralCapabilityMissing);
            foreach (var property in entity.GetProperties())
            {
                var column = property.GetColumnName(store);
                var requiredColumn = Canonical([column ?? string.Empty, property.GetRelationalTypeMapping().StoreType.ToUpperInvariant(),
                    property.IsNullable ? "0" : "1", "0"]);
                if (column is null || !shape.Columns.Contains(requiredColumn))
                    throw new StartupFault(StartupDatabaseFailure.StructuralCapabilityMissing);
            }
            foreach (var index in entity.GetIndexes().Where(i => i.IsUnique))
            {
                var columns = index.Properties.Select(p => p.GetColumnName(store)).ToArray();
                if (!shape.UniqueKeys.Any(key => CoversBaseUniqueKey(key, columns)))
                    throw new StartupFault(StartupDatabaseFailure.StructuralCapabilityMissing);
            }
            foreach (var fk in entity.GetForeignKeys())
            {
                var target = fk.PrincipalEntityType.GetTableName() ?? string.Empty;
                var targetStore = StoreObjectIdentifier.Table(target, fk.PrincipalEntityType.GetSchema());
                var batchMembership = table == "BinMovements" &&
                    fk.Properties.Select(p => p.Name).SequenceEqual(["MovementBatchId"]);
                var delete = batchMembership
                    ? immutableMembership ? "RESTRICT" : "SET NULL"
                    : fk.DeleteBehavior switch
                    {
                        DeleteBehavior.Restrict => "RESTRICT",
                        DeleteBehavior.Cascade => "CASCADE",
                        DeleteBehavior.SetNull => "SET NULL",
                        _ => "NO ACTION"
                    };
                var required = Canonical(fk.Properties.Select((p, i) => Canonical([
                    target, p.GetColumnName(store) ?? string.Empty,
                    fk.PrincipalKey.Properties[i].GetColumnName(targetStore) ?? string.Empty,
                    "NO ACTION", delete, "NONE"])));
                if (!shape.ForeignKeys.Contains(required))
                    throw new StartupFault(StartupDatabaseFailure.StructuralCapabilityMissing);
            }
        }
    }

    private sealed record Shape(HashSet<string> Columns, string[] PrimaryKey,
        HashSet<string> ForeignKeys, HashSet<string> UniqueKeys, HashSet<string> Checks);

    private static async Task<Dictionary<string, Shape>> ReadShapesAsync(SqliteConnection c,
        SqliteTransaction? tx, CancellationToken token)
    {
        var result = new Dictionary<string, Shape>(StringComparer.Ordinal);
        var names = await SqliteStartupInspection.StringsAsync(c, tx,
            "SELECT name FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%'", token);
        foreach (var name in names)
        {
            var columns = new HashSet<string>(StringComparer.Ordinal);
            var primary = new SortedDictionary<int, string>();
            await using var cmd = c.CreateCommand();
            cmd.Transaction = tx;
            cmd.Parameters.AddWithValue("$name", name);
            cmd.CommandText = "SELECT name,type,\"notnull\",pk,hidden FROM pragma_table_xinfo($name) ORDER BY cid";
            await using (var reader = await cmd.ExecuteReaderAsync(token))
            {
                while (await reader.ReadAsync(token))
                {
                    columns.Add(Canonical([reader.GetString(0), reader.GetString(1).ToUpperInvariant(),
                        reader.GetInt64(2).ToString(System.Globalization.CultureInfo.InvariantCulture),
                        reader.GetInt64(4).ToString(System.Globalization.CultureInfo.InvariantCulture)]));
                    if (reader.GetInt32(3) > 0) primary.Add(reader.GetInt32(3), reader.GetString(0));
                }
            }
            var fks = new Dictionary<long, List<string>>();
            cmd.CommandText = "SELECT id,seq,\"table\",\"from\",\"to\",on_update,on_delete,match FROM pragma_foreign_key_list($name) ORDER BY id,seq";
            await using (var reader = await cmd.ExecuteReaderAsync(token))
            {
                while (await reader.ReadAsync(token))
                {
                    var id = reader.GetInt64(0);
                    if (!fks.TryGetValue(id, out var parts)) fks.Add(id, parts = []);
                    parts.Add(Canonical(Enumerable.Range(2, 6).Select(i => reader.GetString(i))));
                }
            }
            var indexes = new List<(string Name, bool Partial)>();
            cmd.CommandText = "SELECT name,partial FROM pragma_index_list($name) WHERE \"unique\"=1";
            await using (var reader = await cmd.ExecuteReaderAsync(token))
                while (await reader.ReadAsync(token)) indexes.Add((reader.GetString(0), reader.GetBoolean(1)));
            var unique = new HashSet<string>(StringComparer.Ordinal);
            foreach (var index in indexes)
            {
                cmd.Parameters["$name"].Value = index.Name;
                cmd.CommandText = "SELECT name,coll,desc FROM pragma_index_xinfo($name) WHERE key=1 ORDER BY seqno";
                var keys = new List<string>();
                await using (var reader = await cmd.ExecuteReaderAsync(token))
                    while (await reader.ReadAsync(token))
                        keys.Add(Canonical([reader.IsDBNull(0) ? "<expression>" : reader.GetString(0), reader.GetString(1),
                            reader.GetInt64(2).ToString(System.Globalization.CultureInfo.InvariantCulture)]));
                cmd.CommandText = "SELECT coalesce(sql,'') FROM sqlite_master WHERE name=$name";
                var sql = Convert.ToString(await cmd.ExecuteScalarAsync(token)) ?? string.Empty;
                var tokens = Tokens(sql);
                var where = Array.IndexOf(tokens, "WHERE");
                var predicate = index.Partial && where >= 0 ? Canonical(tokens.Skip(where + 1).Where(t => t != ";")) : string.Empty;
                unique.Add(Canonical([Canonical(keys), predicate]));
            }
            cmd.Parameters["$name"].Value = name;
            cmd.CommandText = "SELECT sql FROM sqlite_master WHERE type='table' AND name=$name";
            var declaration = Convert.ToString(await cmd.ExecuteScalarAsync(token)) ?? string.Empty;
            result.Add(name, new(columns, primary.Values.ToArray(),
                fks.Values.Select(Canonical).ToHashSet(StringComparer.Ordinal), unique, Checks(declaration)));
        }
        return result;
    }

    private static HashSet<string> Checks(string sql)
    {
        var tokens = Tokens(sql);
        var result = new HashSet<string>(StringComparer.Ordinal);
        for (var i = 0; i + 1 < tokens.Length; i++)
        {
            if (tokens[i] != "CHECK" || tokens[i + 1] != "(") continue;
            var start = i + 2;
            var end = start;
            var depth = 1;
            for (; end < tokens.Length; end++)
            {
                if (tokens[end] == "(") depth++;
                if (tokens[end] == ")") depth--;
                if (depth == 0) break;
            }
            if (depth != 0) throw new StartupFault(StartupDatabaseFailure.StructuralCapabilityMissing);
            result.Add(Canonical(tokens[start..end]));
            i = end;
        }
        return result;
    }

    private static string[] Tokens(string sql) => SqlTokens().Matches(sql).Select(m => m.Value)
        .Where(t => !t.StartsWith("--", StringComparison.Ordinal) && !t.StartsWith("/*", StringComparison.Ordinal))
        .Select(t => t[0] is '\'' or '"' or '`' or '[' ? t : t.ToUpperInvariant()).ToArray();

    // Encoding the token sequence preserves boundaries: a quoted constant containing
    // spaces must never equal several SQL operators/identifiers after normalization.
    private static string Canonical(IEnumerable<string> tokens) => JsonSerializer.Serialize(tokens.ToArray());

    private static bool CoversBaseUniqueKey(string key, string?[] required)
    {
        using var encoded = JsonDocument.Parse(key);
        var parts = encoded.RootElement;
        using var encodedColumns = JsonDocument.Parse(parts[0].GetString() ?? "[]");
        var actual = encodedColumns.RootElement.EnumerateArray().Select(c =>
            JsonSerializer.Deserialize<string[]>(c.GetString() ?? "[]")?[0]);
        if (!actual.SequenceEqual(required)) return false;
        // Historical nullable single-column indexes sometimes exclude NULL; this
        // preserves SQLite's ordinary unique-key semantics. Other filters cannot.
        var predicate = parts[1].GetString();
        return predicate == string.Empty || (required.Length == 1 &&
            predicate == Canonical([required[0]?.ToUpperInvariant() ?? string.Empty, "IS", "NOT", "NULL"]));
    }

    [GeneratedRegex("--[^\\r\\n]*|/\\*[\\s\\S]*?\\*/|'(?:''|[^'])*'|\"(?:\"\"|[^\"])*\"|`(?:``|[^`])*`|\\[[^\\]]*\\]|[A-Za-z_][A-Za-z_0-9]*|[0-9]+|<>|>=|<=|\\S")]
    private static partial Regex SqlTokens();
}
