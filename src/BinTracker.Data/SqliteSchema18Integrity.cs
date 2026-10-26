using System.Data.Common;
using System.Globalization;
using System.Security.Cryptography;
using System.Text.Json;
using BinTracker.Core;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Metadata;

namespace BinTracker.Data;

internal static class SqliteUtf16Length
{
    internal const string FunctionName = "bintracker_utf16_length";

    internal static SqliteConnection CreateConnection(string connectionString)
    {
        var connection = new SqliteConnection(connectionString);
        Register(connection);
        return connection;
    }

    internal static void Register(SqliteConnection connection) =>
        connection.CreateFunction<string?, int?>(FunctionName, value => value?.Length, isDeterministic: true);

    internal sealed class ConnectionInterceptor : DbConnectionInterceptor
    {
        internal static ConnectionInterceptor Instance { get; } = new();

        public override void ConnectionOpened(DbConnection connection, ConnectionEndEventData eventData)
        {
            if (connection is SqliteConnection sqlite) Register(sqlite);
        }

        public override Task ConnectionOpenedAsync(DbConnection connection, ConnectionEndEventData eventData,
            CancellationToken cancellationToken = default)
        {
            if (connection is SqliteConnection sqlite) Register(sqlite);
            return Task.CompletedTask;
        }
    }
}

internal enum Schema18MigrationCheckpoint
{
    BeforeSchemaMutation,
    AfterFirstConstraint,
    BeforePostflight,
    AfterPostflightBeforePublication
}

internal interface ISchema18FailureInjector
{
    void ThrowIfRequested(Schema18MigrationCheckpoint checkpoint);
}

internal sealed class NoSchema18FailureInjector : ISchema18FailureInjector
{
    internal static NoSchema18FailureInjector Instance { get; } = new();
    private NoSchema18FailureInjector() { }
    public void ThrowIfRequested(Schema18MigrationCheckpoint checkpoint) { }
}

internal sealed record Schema18PreflightResult(
    string DatabasePath,
    string FileIdentity,
    IReadOnlyDictionary<string, long> TableRowCounts);

internal sealed record VerifiedSchema18Backup(
    string BackupPath,
    string ManifestPath,
    string BackupSha256,
    string SourceFileIdentity);

internal sealed record Schema18BackupManifest(
    int FormatVersion,
    string Purpose,
    string Provider,
    string SourceDatabasePath,
    string SourcePathIdentityHash,
    string SourceFileIdentity,
    int SourceSchemaVersion,
    string BackupFileName,
    string BackupSha256,
    IReadOnlyDictionary<string, long> TableRowCounts,
    DateTime CreatedUtc);

internal sealed record SqliteStringLimit(
    string Table,
    string Column,
    int Maximum,
    bool Nullable);

internal static class SqliteSchema18Definition
{
    internal const int SourceSchemaVersion = 17;
    internal const int TargetSchemaVersion = 18;
    internal const string GuardColumn = "__BinTrackerSchema18Guard";

    internal static IReadOnlyList<SqliteStringLimit> StringLimits { get; } = BuildStringLimits();

    internal static IReadOnlyDictionary<string, IReadOnlyList<SqliteStringLimit>> LimitsByTable { get; } =
        StringLimits.GroupBy(x => x.Table, StringComparer.Ordinal)
            .ToDictionary(x => x.Key, x => (IReadOnlyList<SqliteStringLimit>)x.OrderBy(y => y.Column,
                StringComparer.Ordinal).ToArray(), StringComparer.Ordinal);

    internal static string ConstraintName(string table) => $"CK_{table}_Schema18Integrity";

    internal static string Predicate(string table)
    {
        var parts = LimitsByTable.TryGetValue(table, out var limits)
            ? limits.Select(x =>
                $"({Quote(x.Column)} IS NULL OR {SqliteUtf16Length.FunctionName}({Quote(x.Column)}) <= {x.Maximum.ToString(CultureInfo.InvariantCulture)})")
                .ToList()
            : [];
        if (table == "ApplicationSettings") parts.Add("(\"Id\" = 1)");
        return string.Join(" AND ", parts);
    }

    internal static string AddConstraintSql(string table) =>
        $"ALTER TABLE {Quote(table)} ADD COLUMN {Quote(GuardColumn)} INTEGER NULL " +
        $"CONSTRAINT {Quote(ConstraintName(table))} CHECK ({Predicate(table)});";

    internal static IEnumerable<string> ConstrainedTables() =>
        LimitsByTable.Keys.Append("ApplicationSettings").Distinct(StringComparer.Ordinal)
            .OrderBy(x => x, StringComparer.Ordinal);

    internal static string Quote(string identifier) => $"\"{identifier.Replace("\"", "\"\"")}\"";

    private static IReadOnlyList<SqliteStringLimit> BuildStringLimits()
    {
        using var db = new BinTrackerDbContext(new DbContextOptionsBuilder<BinTrackerDbContext>()
            .UseSqlite("Data Source=:memory:").Options);
        var result = new List<SqliteStringLimit>();
        foreach (var entity in db.Model.GetEntityTypes())
        {
            var table = entity.GetTableName();
            if (table is null) continue;
            var store = StoreObjectIdentifier.Table(table, entity.GetSchema());
            foreach (var property in entity.GetProperties().Where(x => x.ClrType == typeof(string)))
            {
                var maximum = property.GetMaxLength();
                var column = property.GetColumnName(store);
                if (maximum is null || column is null) continue;
                result.Add(new(table, column, maximum.Value, property.IsNullable));
            }
        }
        return result.OrderBy(x => x.Table, StringComparer.Ordinal)
            .ThenBy(x => x.Column, StringComparer.Ordinal).ToArray();
    }
}

internal sealed class SqliteSchema18Preflight
{
    internal async Task<Schema18PreflightResult> InspectAsync(string databasePath,
        CancellationToken cancellationToken = default)
    {
        var path = SqliteMigrationPath.NormalizeExistingDatabase(databasePath);
        await using var connection = SqliteUtf16Length.CreateConnection(
            SqliteStartupInspection.ConnectionString(path));
        await connection.OpenAsync(cancellationToken);
        await using var transaction = connection.BeginTransaction(
            System.Data.IsolationLevel.Serializable, deferred: true);
        await ValidateSourceAsync(connection, transaction, cancellationToken);
        var counts = await ReadTableCountsAsync(connection, transaction, cancellationToken);
        return new(path, WindowsFileIdentity.Get(path), counts);
    }

    internal static async Task ValidateSourceAsync(SqliteConnection connection, SqliteTransaction transaction,
        CancellationToken token)
    {
        var version = await ScalarInt64Async(connection, transaction,
            "SELECT Version FROM SchemaVersion WHERE Id=1;", token);
        if (version != SqliteSchema18Definition.SourceSchemaVersion)
            throw new InvalidOperationException("SCHEMA18_SOURCE_VERSION_UNSUPPORTED");

        foreach (var table in SqliteSchema18Definition.ConstrainedTables())
        {
            if (await ScalarInt64Async(connection, transaction,
                    $"SELECT COUNT(*) FROM pragma_table_info('{table}') WHERE name='{SqliteSchema18Definition.GuardColumn}';", token) != 0)
                throw new InvalidOperationException("SCHEMA18_PARTIAL_CONSTRAINT_STATE");
            var predicate = SqliteSchema18Definition.Predicate(table);
            if (await ScalarInt64Async(connection, transaction,
                    $"SELECT COUNT(*) FROM {SqliteSchema18Definition.Quote(table)} WHERE NOT ({predicate});", token) != 0)
                throw new InvalidOperationException($"SCHEMA18_RETAINED_STRING_LIMIT_VIOLATION:{table}");
        }

        if (await ScalarInt64Async(connection, transaction,
                "SELECT COUNT(*) FROM ApplicationSettings WHERE Id=1;", token) != 1 ||
            await ScalarInt64Async(connection, transaction,
                "SELECT COUNT(*) FROM ApplicationSettings;", token) != 1)
            throw new InvalidOperationException("SCHEMA18_APPLICATION_SETTINGS_SINGLETON_INVALID");

        var integrity = await SqliteStartupInspection.StringsAsync(connection, transaction,
            "PRAGMA integrity_check;", token);
        if (!integrity.SequenceEqual(["ok"]))
            throw new InvalidOperationException("SCHEMA18_SOURCE_INTEGRITY_INVALID");
        var foreignKeys = await ScalarInt64Async(connection, transaction,
            "SELECT COUNT(*) FROM pragma_foreign_key_check;", token);
        if (foreignKeys != 0)
            throw new InvalidOperationException("SCHEMA18_SOURCE_FOREIGN_KEYS_INVALID");
    }

    internal static async Task ValidateTargetAsync(SqliteConnection connection, SqliteTransaction transaction,
        CancellationToken token)
    {
        var version = await ScalarInt64Async(connection, transaction,
            "SELECT Version FROM SchemaVersion WHERE Id=1;", token);
        if (version != SqliteSchema18Definition.TargetSchemaVersion)
            throw new InvalidOperationException("SCHEMA18_TARGET_VERSION_INVALID");

        foreach (var table in SqliteSchema18Definition.ConstrainedTables())
        {
            if (await ScalarInt64Async(connection, transaction,
                    $"SELECT COUNT(*) FROM pragma_table_info('{table}') WHERE name='{SqliteSchema18Definition.GuardColumn}';", token) != 1)
                throw new InvalidOperationException($"SCHEMA18_CONSTRAINT_COLUMN_MISSING:{table}");
            var sql = await ScalarStringAsync(connection, transaction,
                "SELECT sql FROM sqlite_master WHERE type='table' AND name=$table;", token,
                ("$table", table));
            var expectedConstraint =
                $"{SqliteSchema18Definition.Quote(SqliteSchema18Definition.GuardColumn)} INTEGER NULL " +
                $"CONSTRAINT {SqliteSchema18Definition.Quote(SqliteSchema18Definition.ConstraintName(table))} " +
                $"CHECK ({SqliteSchema18Definition.Predicate(table)})";
            if (!sql.Contains(expectedConstraint, StringComparison.Ordinal))
                throw new InvalidOperationException($"SCHEMA18_CONSTRAINT_MISSING:{table}");
            var predicate = SqliteSchema18Definition.Predicate(table);
            if (await ScalarInt64Async(connection, transaction,
                    $"SELECT COUNT(*) FROM {SqliteSchema18Definition.Quote(table)} WHERE NOT ({predicate});", token) != 0)
                throw new InvalidOperationException($"SCHEMA18_TARGET_DATA_INVALID:{table}");
        }
    }

    internal static async Task<IReadOnlyDictionary<string, long>> ReadTableCountsAsync(
        SqliteConnection connection, SqliteTransaction transaction, CancellationToken token)
    {
        var tables = await SqliteStartupInspection.StringsAsync(connection, transaction,
            "SELECT name FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%' ORDER BY name;", token);
        var result = new Dictionary<string, long>(StringComparer.Ordinal);
        foreach (var table in tables)
            result.Add(table, await ScalarInt64Async(connection, transaction,
                $"SELECT COUNT(*) FROM {SqliteSchema18Definition.Quote(table)};", token));
        return result;
    }

    internal static async Task<long> ScalarInt64Async(SqliteConnection connection, SqliteTransaction? transaction,
        string sql, CancellationToken token, params (string Name, object Value)[] parameters)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = sql;
        foreach (var (name, value) in parameters) command.Parameters.AddWithValue(name, value);
        return Convert.ToInt64(await command.ExecuteScalarAsync(token), CultureInfo.InvariantCulture);
    }

    private static async Task<string> ScalarStringAsync(SqliteConnection connection, SqliteTransaction transaction,
        string sql, CancellationToken token, params (string Name, object Value)[] parameters)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = sql;
        foreach (var (name, value) in parameters) command.Parameters.AddWithValue(name, value);
        return Convert.ToString(await command.ExecuteScalarAsync(token), CultureInfo.InvariantCulture)
            ?? throw new InvalidOperationException("SCHEMA18_SCHEMA_SQL_MISSING");
    }
}

internal sealed class SqliteSchema18BackupService(TimeProvider? timeProvider = null)
{
    private const string Purpose = "PreSchema18IntegrityUpgradeRecovery";
    private readonly TimeProvider timeProvider = timeProvider ?? TimeProvider.System;

    internal async Task<VerifiedSchema18Backup> CreateVerifiedAsync(IDatabaseUpgradeLease lease,
        string backupDirectory, Schema18PreflightResult expected, CancellationToken token)
    {
        if (!lease.PendingOperationCheckPassed || lease.DatabaseFileIdentity != expected.FileIdentity)
            throw new InvalidOperationException("SCHEMA18_BACKUP_LEASE_INVALID");
        var source = SqliteMigrationPath.NormalizeExistingDatabase(lease.DatabasePath);
        var directory = Path.GetFullPath(backupDirectory);
        Directory.CreateDirectory(directory);
        directory = SqliteMigrationPath.NormalizeExistingDirectory(directory);
        if (SqliteMigrationPath.IsSameOrChildDirectory(source, directory))
            throw new InvalidOperationException("SCHEMA18_BACKUP_LOCATION_INVALID");

        var stamp = timeProvider.GetUtcNow().UtcDateTime;
        var name = $"BinTracker-pre-integrity-v17-{stamp:yyyyMMddTHHmmssfffZ}-{Guid.NewGuid():N}.db";
        var backup = Path.Combine(directory, name);
        using (new FileStream(backup, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None)) { }
        await using (var sourceConnection = SqliteUtf16Length.CreateConnection(
                         SqliteStartupInspection.ConnectionString(source)))
        await using (var backupConnection = SqliteUtf16Length.CreateConnection(
                         SqliteStartupInspection.ConnectionString(backup, readOnly: false)))
        {
            await sourceConnection.OpenAsync(token);
            await backupConnection.OpenAsync(token);
            sourceConnection.BackupDatabase(backupConnection);
        }
        if (!File.Exists(backup) || new FileInfo(backup).Length == 0)
            throw new InvalidOperationException("SCHEMA18_BACKUP_EMPTY");

        await SqliteStartupInspection.RequireEquivalentAsync(source, backup, token);
        var backupPreflight = await new SqliteSchema18Preflight().InspectAsync(backup, token);
        if (!SameCounts(expected.TableRowCounts, backupPreflight.TableRowCounts))
            throw new InvalidOperationException("SCHEMA18_BACKUP_COUNTS_MISMATCH");
        var hash = await SqliteMigrationPath.Sha256FileAsync(backup, token);
        var manifestPath = backup + ".manifest.json";
        var manifest = new Schema18BackupManifest(1, Purpose, "SQLite", source,
            SqliteMigrationPath.IdentityHash(source), expected.FileIdentity,
            SqliteSchema18Definition.SourceSchemaVersion, name, hash,
            backupPreflight.TableRowCounts, stamp);
        await using (var stream = new FileStream(manifestPath, FileMode.CreateNew, FileAccess.Write,
                         FileShare.None, 4096, FileOptions.Asynchronous | FileOptions.WriteThrough))
            await JsonSerializer.SerializeAsync(stream, manifest, cancellationToken: token);
        await VerifyAsync(new(backup, manifestPath, hash, expected.FileIdentity), source, token);
        return new(backup, manifestPath, hash, expected.FileIdentity);
    }

    internal static async Task VerifyAsync(VerifiedSchema18Backup backup, string source, CancellationToken token)
    {
        if (!File.Exists(backup.BackupPath) || !File.Exists(backup.ManifestPath) ||
            WindowsFileIdentity.Get(source) != backup.SourceFileIdentity)
            throw new InvalidOperationException("SCHEMA18_BACKUP_EVIDENCE_MISSING");
        await using var stream = new FileStream(backup.ManifestPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        var manifest = await JsonSerializer.DeserializeAsync<Schema18BackupManifest>(stream,
            cancellationToken: token) ?? throw new InvalidOperationException("SCHEMA18_BACKUP_MANIFEST_INVALID");
        var actualHash = await SqliteMigrationPath.Sha256FileAsync(backup.BackupPath, token);
        if (manifest.FormatVersion != 1 || manifest.Purpose != Purpose || manifest.Provider != "SQLite" ||
            manifest.SourceSchemaVersion != SqliteSchema18Definition.SourceSchemaVersion ||
            manifest.SourceDatabasePath != source ||
            manifest.SourcePathIdentityHash != SqliteMigrationPath.IdentityHash(source) ||
            manifest.SourceFileIdentity != backup.SourceFileIdentity ||
            manifest.BackupFileName != Path.GetFileName(backup.BackupPath) ||
            !FixedHashEquals(manifest.BackupSha256, actualHash) ||
            !FixedHashEquals(backup.BackupSha256, actualHash))
            throw new InvalidOperationException("SCHEMA18_BACKUP_MANIFEST_INVALID");
        var verified = await new SqliteSchema18Preflight().InspectAsync(backup.BackupPath, token);
        if (!SameCounts(manifest.TableRowCounts, verified.TableRowCounts))
            throw new InvalidOperationException("SCHEMA18_BACKUP_MANIFEST_INVALID");
    }

    private static bool SameCounts(IReadOnlyDictionary<string, long> left,
        IReadOnlyDictionary<string, long> right) => left.Count == right.Count &&
        left.OrderBy(x => x.Key, StringComparer.Ordinal).SequenceEqual(
            right.OrderBy(x => x.Key, StringComparer.Ordinal));

    private static bool FixedHashEquals(string left, string right) =>
        CryptographicOperations.FixedTimeEquals(Convert.FromHexString(left), Convert.FromHexString(right));
}

internal sealed class SqliteSchema18Migrator(ISchema18FailureInjector? failureInjector = null,
    TimeProvider? timeProvider = null)
{
    private readonly ISchema18FailureInjector failureInjector =
        failureInjector ?? NoSchema18FailureInjector.Instance;
    private readonly TimeProvider timeProvider = timeProvider ?? TimeProvider.System;

    internal Action? AfterCommit { get; init; }

    internal async Task MigrateAsync(IDatabaseUpgradeLease lease, Schema18PreflightResult preflight,
        VerifiedSchema18Backup backup, CancellationToken token)
    {
        if (!lease.PendingOperationCheckPassed || lease.DatabaseFileIdentity != preflight.FileIdentity)
            throw new InvalidOperationException("SCHEMA18_MIGRATION_PREREQUISITES_INVALID");
        await SqliteSchema18BackupService.VerifyAsync(backup, lease.DatabasePath, token);
        await using var connection = SqliteUtf16Length.CreateConnection(
            SqliteStartupInspection.ConnectionString(lease.DatabasePath, readOnly: false));
        await connection.OpenAsync(token);
        await using var transaction = (SqliteTransaction)await connection.BeginTransactionAsync(token);
        var committed = false;
        try
        {
            await SqliteStartupInspection.RequireEquivalentAsync(connection, transaction, backup.BackupPath, token);
            await SqliteSchema18Preflight.ValidateSourceAsync(connection, transaction, token);
            failureInjector.ThrowIfRequested(Schema18MigrationCheckpoint.BeforeSchemaMutation);
            var first = true;
            foreach (var table in SqliteSchema18Definition.ConstrainedTables())
            {
                await NonQueryAsync(connection, transaction, SqliteSchema18Definition.AddConstraintSql(table), token);
                if (first)
                {
                    first = false;
                    failureInjector.ThrowIfRequested(Schema18MigrationCheckpoint.AfterFirstConstraint);
                }
            }
            failureInjector.ThrowIfRequested(Schema18MigrationCheckpoint.BeforePostflight);
            await NonQueryAsync(connection, transaction,
                "UPDATE SchemaVersion SET Version=18, UpdatedUtc=$utc WHERE Id=1;", token,
                ("$utc", timeProvider.GetUtcNow().UtcDateTime.ToString("O", CultureInfo.InvariantCulture)));
            await SqliteSchema18Preflight.ValidateTargetAsync(connection, transaction, token);
            failureInjector.ThrowIfRequested(Schema18MigrationCheckpoint.AfterPostflightBeforePublication);
            await transaction.CommitAsync(token);
            committed = true;
            AfterCommit?.Invoke();
        }
        catch
        {
            if (!committed) await transaction.RollbackAsync(CancellationToken.None);
            throw;
        }
    }

    private static async Task NonQueryAsync(SqliteConnection connection, SqliteTransaction transaction,
        string sql, CancellationToken token, params (string Name, object Value)[] parameters)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = sql;
        foreach (var (name, value) in parameters) command.Parameters.AddWithValue(name, value);
        await command.ExecuteNonQueryAsync(token);
    }
}
