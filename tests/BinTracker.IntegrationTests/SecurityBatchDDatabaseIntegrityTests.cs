using System.Text.Json;
using BinTracker.Core;
using BinTracker.Data;
using Microsoft.Data.Sqlite;
using Xunit;

namespace BinTracker.IntegrationTests;

public sealed class SecurityBatchDDatabaseIntegrityTests
{
    public static IEnumerable<object[]> MasterDataStringLimits()
    {
        yield return ["Customers", "CustomerCode", MasterDataLimits.CustomerCode, true];
        yield return ["Customers", "Name", MasterDataLimits.CustomerName, false];
        yield return ["Customers", "ContactName", MasterDataLimits.CustomerContactName, true];
        yield return ["Customers", "Phone", MasterDataLimits.CustomerPhone, true];
        yield return ["Customers", "MobileNumber", MasterDataLimits.CustomerMobileNumber, true];
        yield return ["Customers", "Email", MasterDataLimits.CustomerEmail, true];
        yield return ["Customers", "Address", MasterDataLimits.CustomerAddress, true];
        yield return ["Customers", "Notes", MasterDataLimits.CustomerNotes, true];
        yield return ["ContainerTypes", "Name", MasterDataLimits.ContainerName, false];
        yield return ["ContainerTypes", "NameKey", MasterDataLimits.ContainerName, false];
        yield return ["ContainerTypes", "ShortCode", MasterDataLimits.ContainerShortCode, false];
        yield return ["ContainerTypes", "SystemCode", MasterDataLimits.ContainerSystemCode, false];
        yield return ["ContainerTypes", "Description", MasterDataLimits.ContainerDescription, true];
        yield return ["ContainerTypes", "Notes", MasterDataLimits.ContainerNotes, true];
        yield return ["ContainerTypes", "DashboardColour", MasterDataLimits.ContainerDashboardColour, true];
    }

    [Theory]
    [MemberData(nameof(MasterDataStringLimits))]
    public async Task Schema18_accepts_exact_master_data_limits_and_rejects_oversize_direct_writes(
        string table, string column, int maximum, bool nullable)
    {
        await using var fixture = await Task20Fixture.CreateAsync();
        using var ready = await fixture.Coordinator().StartAsync();
        await using var connection = await OpenAsync(fixture);
        var id = table == "Customers" ? fixture.CustomerId : 1;

        await ExecuteAsync(connection,
            $"UPDATE \"{table}\" SET \"{column}\"=$value WHERE Id=$id;",
            ("$value", new string('x', maximum)), ("$id", id));
        var failure = await Assert.ThrowsAsync<SqliteException>(() => ExecuteAsync(connection,
            $"UPDATE \"{table}\" SET \"{column}\"=$value WHERE Id=$id;",
            ("$value", new string('x', maximum + 1)), ("$id", id)));
        Assert.Equal(19, failure.SqliteErrorCode);

        if (nullable)
            await ExecuteAsync(connection,
                $"UPDATE \"{table}\" SET \"{column}\"=NULL WHERE Id=$id;", ("$id", id));
    }

    [Fact]
    public async Task Schema18_counts_supplementary_text_as_dotnet_utf16_code_units()
    {
        await using var fixture = await Task20Fixture.CreateAsync();
        using var ready = await fixture.Coordinator().StartAsync();
        await using var connection = await OpenAsync(fixture);
        var exact = string.Concat(Enumerable.Repeat("😀", MasterDataLimits.CustomerCode / 2));
        Assert.Equal(MasterDataLimits.CustomerCode, exact.Length);
        await ExecuteAsync(connection, "UPDATE Customers SET CustomerCode=$value WHERE Id=$id;",
            ("$value", exact), ("$id", fixture.CustomerId));

        var failure = await Assert.ThrowsAsync<SqliteException>(() => ExecuteAsync(connection,
            "UPDATE Customers SET CustomerCode=$value WHERE Id=$id;",
            ("$value", exact + "a"), ("$id", fixture.CustomerId)));
        Assert.Equal(19, failure.SqliteErrorCode);
    }

    [Fact]
    public async Task Schema18_enforces_application_settings_singleton_and_preserves_updates()
    {
        await using var fixture = await Task20Fixture.CreateAsync();
        using var ready = await fixture.Coordinator().StartAsync();
        await using var connection = await OpenAsync(fixture);
        await ExecuteAsync(connection,
            "UPDATE ApplicationSettings SET AttentionAgeDays=9, Revision=Revision+1 WHERE Id=1;");
        Assert.Equal(9, await ScalarAsync(connection,
            "SELECT AttentionAgeDays FROM ApplicationSettings WHERE Id=1;"));

        var invalidUpdate = await Assert.ThrowsAsync<SqliteException>(() => ExecuteAsync(connection,
            "UPDATE ApplicationSettings SET Id=2 WHERE Id=1;"));
        Assert.Equal(19, invalidUpdate.SqliteErrorCode);

        var duplicate = await Assert.ThrowsAsync<SqliteException>(() => ExecuteAsync(connection, """
            INSERT INTO ApplicationSettings
                (Id, AttentionQuantityThreshold, AttentionAgeDays, BackupRetentionCount,
                 MaxFailedLoginAttempts, Revision)
            VALUES (2, 20, 7, 30, 5, 1);
            """));
        Assert.Equal(19, duplicate.SqliteErrorCode);
        Assert.Equal(1, await ScalarAsync(connection, "SELECT COUNT(*) FROM ApplicationSettings;"));
    }

    [Fact]
    public async Task Fresh_startup_publishes_schema18_and_all_modelled_string_checks()
    {
        await using var fixture = await Task20Fixture.CreateAsync(schema17: false,
            enabled: false, projection: false);
        using var ready = await fixture.Coordinator().StartAsync();
        Assert.Equal(18, ready.SchemaVersion);
        Assert.Equal(18, await fixture.ScalarAsync("SELECT Version FROM SchemaVersion WHERE Id=1;"));
        await using var connection = await OpenAsync(fixture);
        Assert.Equal(SqliteSchema18Definition.ConstrainedTables().Count(), await ScalarAsync(connection,
            $"SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND sql LIKE '%{SqliteSchema18Definition.GuardColumn}%';"));
    }

    [Fact]
    public async Task Oversize_schema17_retained_data_fails_preflight_without_mutation_or_backup()
    {
        await using var fixture = await Task20Fixture.CreateAsync();
        await using (var connection = await OpenAsync(fixture))
            await ExecuteAsync(connection, "UPDATE Customers SET Notes=$value WHERE Id=$id;",
                ("$value", new string('x', MasterDataLimits.CustomerNotes + 1)), ("$id", fixture.CustomerId));
        var before = await fixture.StateAsync();
        var failure = await Assert.ThrowsAsync<StartupDatabaseException>(() => fixture.Coordinator().StartAsync());
        Assert.Equal(StartupDatabaseFailure.PreflightRejected, failure.Failure);
        Assert.Equal(StartupDatabasePhase.Schema18Preflight, failure.Phase);
        Assert.False(failure.SchemaMutationAttempted);
        Assert.Equal(17, failure.PersistedState.SchemaVersion);
        Assert.Equal(before, await fixture.StateAsync());
        Assert.False(Directory.Exists(BackupDirectory(fixture)));
    }

    [Fact]
    public async Task Ambiguous_schema17_settings_fail_preflight_without_silent_repair()
    {
        await using var fixture = await Task20Fixture.CreateAsync();
        await using (var connection = await OpenAsync(fixture))
            await ExecuteAsync(connection, """
                INSERT INTO ApplicationSettings
                    (Id, AttentionQuantityThreshold, AttentionAgeDays, BackupRetentionCount,
                     MaxFailedLoginAttempts, Revision)
                VALUES (2, 20, 7, 30, 5, 1);
                """);
        var before = await fixture.StateAsync();
        var failure = await Assert.ThrowsAsync<StartupDatabaseException>(() => fixture.Coordinator().StartAsync());
        Assert.Equal(StartupDatabaseFailure.PreflightRejected, failure.Failure);
        Assert.False(failure.SchemaMutationAttempted);
        Assert.Equal(before, await fixture.StateAsync());
        Assert.Equal(2, await fixture.ScalarAsync("SELECT COUNT(*) FROM ApplicationSettings;"));
    }

    [Fact]
    public async Task Invalid_single_schema17_settings_id_fails_preflight_without_silent_repair()
    {
        await using var fixture = await Task20Fixture.CreateAsync();
        await using (var connection = await OpenAsync(fixture))
            await ExecuteAsync(connection, "UPDATE ApplicationSettings SET Id=2 WHERE Id=1;");
        var before = await fixture.StateAsync();

        var failure = await Assert.ThrowsAsync<StartupDatabaseException>(() => fixture.Coordinator().StartAsync());

        Assert.Equal(StartupDatabaseFailure.PreflightRejected, failure.Failure);
        Assert.Equal(StartupDatabasePhase.Schema18Preflight, failure.Phase);
        Assert.False(failure.SchemaMutationAttempted);
        Assert.Equal(before, await fixture.StateAsync());
        Assert.Equal(0, await fixture.ScalarAsync("SELECT COUNT(*) FROM ApplicationSettings WHERE Id=1;"));
        Assert.Equal(1, await fixture.ScalarAsync("SELECT COUNT(*) FROM ApplicationSettings WHERE Id=2;"));
    }

    [Theory]
    [InlineData((int)Schema18MigrationCheckpoint.BeforeSchemaMutation)]
    [InlineData((int)Schema18MigrationCheckpoint.AfterFirstConstraint)]
    [InlineData((int)Schema18MigrationCheckpoint.BeforePostflight)]
    [InlineData((int)Schema18MigrationCheckpoint.AfterPostflightBeforePublication)]
    public async Task Schema18_failure_rolls_back_to_exact_schema17_and_verified_backup_supports_retry(
        int checkpointValue)
    {
        var checkpoint = (Schema18MigrationCheckpoint)checkpointValue;
        await using var fixture = await Task20Fixture.CreateAsync();
        await fixture.Database.CreateSingleAsync(Task20Fixture.Today, fixture.CustomerId, 1, 7);
        var before = await fixture.StateAsync();
        var injector = new FailureInjector(checkpoint);
        var failure = await Assert.ThrowsAsync<StartupDatabaseException>(() =>
            fixture.Coordinator(integrityFailures: injector).StartAsync());
        Assert.Equal(StartupDatabaseFailure.MigrationFailed, failure.Failure);
        Assert.Same(injector.Error, failure.InnerException);
        Assert.Equal(17, failure.PersistedState.SchemaVersion);
        Assert.Equal(before, await fixture.StateAsync());
        await AssertIntegrityBackupValidAsync(fixture);

        using var retry = await fixture.Coordinator().StartAsync();
        Assert.Equal(18, retry.SchemaVersion);
    }

    [Fact]
    public async Task Schema18_postcommit_failure_reports_committed_state_and_retry_only_revalidates()
    {
        await using var fixture = await Task20Fixture.CreateAsync();
        var original = new InvalidOperationException("schema18 postcommit failure");
        var failure = await Assert.ThrowsAsync<StartupDatabaseException>(() =>
            fixture.Coordinator(integrityAfterCommit: () => throw original).StartAsync());

        Assert.Equal(StartupDatabaseFailure.MigrationFailed, failure.Failure);
        Assert.Same(original, failure.InnerException);
        Assert.True(failure.SchemaMutationAttempted);
        Assert.Equal(18, failure.PersistedState.SchemaVersion);
        Assert.Equal(18, await fixture.ScalarAsync("SELECT Version FROM SchemaVersion WHERE Id=1;"));
        var evidence = Directory.GetFiles(BackupDirectory(fixture), "*", SearchOption.AllDirectories)
            .Order().ToArray();

        using var retry = await fixture.Coordinator().StartAsync();
        Assert.Equal(StartupDatabaseState.Schema18ValidationRequired, retry.InitialState);
        Assert.Equal(evidence, Directory.GetFiles(
            BackupDirectory(fixture), "*", SearchOption.AllDirectories).Order().ToArray());
    }

    [Fact]
    public async Task Schema18_native_reentry_rejects_a_weakened_constraint_definition()
    {
        await using var fixture = await Task20Fixture.CreateAsync();
        using (var ready = await fixture.Coordinator().StartAsync())
            Assert.Equal(18, ready.SchemaVersion);
        await fixture.ExecuteAsync("""
            PRAGMA writable_schema=ON;
            UPDATE sqlite_master
            SET sql=replace(sql, '<= 50', '<= 51')
            WHERE type='table' AND name='Customers';
            PRAGMA writable_schema=OFF;
            PRAGMA schema_version=9001;
            """);
        var before = await fixture.StateAsync();

        var failure = await Assert.ThrowsAsync<StartupDatabaseException>(() => fixture.Coordinator().StartAsync());

        Assert.False(failure.SchemaMutationAttempted);
        Assert.Equal(before, await fixture.StateAsync());
    }

    [Fact]
    public async Task Schema17_upgrade_preserves_lineage_audit_business_rows_foreign_keys_and_indexes()
    {
        await using var fixture = await Task20Fixture.CreateAsync();
        await fixture.Database.CreateSingleAsync(Task20Fixture.Today, fixture.CustomerId, 1, 7);
        var counts = await fixture.CountsAsync();
        var audit = await fixture.ScalarAsync("SELECT COUNT(*) FROM AuditEvents;");
        var fk = await fixture.ScalarAsync("SELECT COUNT(*) FROM pragma_foreign_key_list('LogicalMovementLines');");
        var indexes = await fixture.ScalarAsync("SELECT COUNT(*) FROM sqlite_master WHERE type='index' AND sql IS NOT NULL;");
        var customerCode = await ScalarTextAsync(fixture, "SELECT CustomerCode FROM Customers WHERE Id=1;");
        var auditDescription = await ScalarTextAsync(fixture, "SELECT Description FROM AuditEvents ORDER BY Id LIMIT 1;");

        using var ready = await fixture.Coordinator().StartAsync();
        Assert.Equal(18, ready.SchemaVersion);
        Assert.Equal(counts, await fixture.CountsAsync());
        Assert.Equal(audit, await fixture.ScalarAsync("SELECT COUNT(*) FROM AuditEvents;"));
        Assert.Equal(fk, await fixture.ScalarAsync("SELECT COUNT(*) FROM pragma_foreign_key_list('LogicalMovementLines');"));
        Assert.Equal(indexes, await fixture.ScalarAsync("SELECT COUNT(*) FROM sqlite_master WHERE type='index' AND sql IS NOT NULL;"));
        Assert.Equal(0, await fixture.ScalarAsync("SELECT COUNT(*) FROM pragma_foreign_key_check;"));
        Assert.Equal(1, await fixture.ScalarAsync("SELECT COUNT(*) FROM LogicalMovementBatches WHERE Status=1;"));
        Assert.Equal(customerCode, await ScalarTextAsync(fixture, "SELECT CustomerCode FROM Customers WHERE Id=1;"));
        Assert.Equal(auditDescription, await ScalarTextAsync(fixture, "SELECT Description FROM AuditEvents ORDER BY Id LIMIT 1;"));
        Assert.Equal(7, await fixture.ScalarAsync("SELECT Quantity FROM BinMovements ORDER BY Id LIMIT 1;"));
    }

    private static async Task<SqliteConnection> OpenAsync(Task20Fixture fixture)
    {
        var connection = SqliteUtf16Length.CreateConnection(fixture.Database.ConnectionString);
        await connection.OpenAsync();
        return connection;
    }

    private static async Task ExecuteAsync(SqliteConnection connection, string sql,
        params (string Name, object Value)[] parameters)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        foreach (var (name, value) in parameters) command.Parameters.AddWithValue(name, value);
        await command.ExecuteNonQueryAsync();
    }

    private static async Task<long> ScalarAsync(SqliteConnection connection, string sql)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        return Convert.ToInt64(await command.ExecuteScalarAsync());
    }

    private static async Task<string> ScalarTextAsync(Task20Fixture fixture, string sql)
    {
        await using var connection = await OpenAsync(fixture);
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        return Convert.ToString(await command.ExecuteScalarAsync())
            ?? throw new InvalidOperationException("Expected retained text was missing.");
    }

    private static async Task AssertIntegrityBackupValidAsync(Task20Fixture fixture)
    {
        var directory = BackupDirectory(fixture);
        var backupPath = Assert.Single(Directory.GetFiles(directory, "*.db"));
        var manifestPath = Assert.Single(Directory.GetFiles(directory, "*.manifest.json"));
        await using var stream = File.OpenRead(manifestPath);
        var manifest = await JsonSerializer.DeserializeAsync<Schema18BackupManifest>(stream)
            ?? throw new InvalidOperationException("Schema18 backup manifest was unreadable.");
        await SqliteSchema18BackupService.VerifyAsync(new(
            backupPath, manifestPath, manifest.BackupSha256, manifest.SourceFileIdentity),
            new SqliteConnectionStringBuilder(fixture.Database.ConnectionString).DataSource,
            CancellationToken.None);
    }

    private static string BackupDirectory(Task20Fixture fixture)
    {
        var path = new SqliteConnectionStringBuilder(fixture.Database.ConnectionString).DataSource;
        var root = Directory.GetParent(Path.GetDirectoryName(path)!)!;
        return Path.Combine(root.FullName, "startup-backups");
    }

    private sealed class FailureInjector(Schema18MigrationCheckpoint checkpoint) : ISchema18FailureInjector
    {
        internal Exception Error { get; } = new InvalidOperationException("schema18 injected failure");
        public void ThrowIfRequested(Schema18MigrationCheckpoint current)
        {
            if (current == checkpoint) throw Error;
        }
    }
}
