using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using Xunit;

namespace BinTracker.Data;

public sealed class SecurityBatchFFilesystemSafetyTests
{
    [Fact]
    public void Backup_connection_builder_round_trips_delimiters_quotes_and_unicode()
    {
        const string path = "C:\\Temp\\semi;double\"-single'-snow-unicode-\u96EA=[value].db";

        var connectionString = DeveloperDatabaseService.BuildBackupConnectionString(path);
        var parsed = new SqliteConnectionStringBuilder(connectionString);

        Assert.Equal(path, parsed.DataSource);
        Assert.Equal(SqliteOpenMode.ReadWrite, parsed.Mode);
    }

    [Fact]
    public async Task Backup_with_hostile_valid_Windows_filename_targets_only_the_selected_file()
    {
        await using var fixture = await Fixture.CreateAsync();
        var destination = Path.Combine(
            fixture.BackupDirectory,
            "backup;Mode=Memory;Cache=Shared='quoted'-snow-unicode-\u96EA=[value],#.db");

        await fixture.Service.BackupAsync(destination);

        Assert.True(File.Exists(destination));
        await using var backup = new SqliteConnection(
            new SqliteConnectionStringBuilder { DataSource = destination, Pooling = false }.ConnectionString);
        await backup.OpenAsync();
        await using var command = backup.CreateCommand();
        command.CommandText = "SELECT Value FROM BackupProof WHERE Id=1";
        Assert.Equal("source-value", await command.ExecuteScalarAsync());
        Assert.DoesNotContain(
            Directory.GetFiles(fixture.BackupDirectory),
            path => !path.Equals(destination, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Existing_destination_is_never_overwritten()
    {
        await using var fixture = await Fixture.CreateAsync();
        var destination = Path.Combine(fixture.BackupDirectory, "existing.db");
        var prior = "unrelated retained evidence"u8.ToArray();
        await File.WriteAllBytesAsync(destination, prior);

        var error = await Assert.ThrowsAsync<IOException>(() =>
            fixture.Service.BackupAsync(destination));

        Assert.Contains("never overwrites", error.Message, StringComparison.Ordinal);
        Assert.Equal(prior, await File.ReadAllBytesAsync(destination));
    }

    [Fact]
    public async Task Direct_source_and_hard_link_alias_are_rejected_by_physical_identity()
    {
        await using var fixture = await Fixture.CreateAsync();
        var hardLink = Path.Combine(fixture.BackupDirectory, "hard-link.db");
        Assert.True(CreateHardLink(hardLink, fixture.SourcePath, IntPtr.Zero));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            fixture.Service.BackupAsync(fixture.SourcePath));
        var aliasError = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            fixture.Service.BackupAsync(hardLink));

        Assert.Contains("active BinTracker database", aliasError.Message, StringComparison.Ordinal);
        Assert.Equal(WindowsFileIdentity.Get(fixture.SourcePath), WindowsFileIdentity.Get(hardLink));
    }

    [Fact]
    public async Task Reparse_point_directory_alias_cannot_hide_the_active_database()
    {
        await using var fixture = await Fixture.CreateAsync();
        var aliasDirectory = Path.Combine(fixture.Root, "database-directory-alias");
        CreateDirectoryAlias(aliasDirectory, Path.GetDirectoryName(fixture.SourcePath)!);
        var aliasPath = Path.Combine(aliasDirectory, Path.GetFileName(fixture.SourcePath));

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            fixture.Service.BackupAsync(aliasPath));

        Assert.Contains("active BinTracker database", error.Message, StringComparison.Ordinal);
        Assert.Equal(WindowsFileIdentity.Get(fixture.SourcePath), WindowsFileIdentity.Get(aliasPath));
    }

    [Fact]
    public async Task Destination_created_during_backup_is_retained_and_publication_fails()
    {
        await using var fixture = await Fixture.CreateAsync();
        var destination = Path.Combine(fixture.BackupDirectory, "raced.db");
        var competing = "competing writer"u8.ToArray();
        var service = fixture.CreateService(new DeveloperDatabaseFailureHooks
        {
            BeforeBackupPublication = () => File.WriteAllBytes(destination, competing)
        });

        await Assert.ThrowsAsync<IOException>(() => service.BackupAsync(destination));

        Assert.Equal(competing, await File.ReadAllBytesAsync(destination));
        Assert.DoesNotContain(
            Directory.GetFiles(fixture.BackupDirectory),
            path => Path.GetFileName(path).StartsWith(".bintracker-backup-", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Concurrent_backups_to_one_destination_publish_one_complete_snapshot()
    {
        await using var fixture = await Fixture.CreateAsync();
        var destination = Path.Combine(fixture.BackupDirectory, "one-winner.db");

        var outcomes = await Task.WhenAll(Enumerable.Range(0, 6).Select(async _ =>
        {
            try
            {
                await fixture.CreateService().BackupAsync(destination);
                return true;
            }
            catch (IOException)
            {
                return false;
            }
        }));

        Assert.Equal(1, outcomes.Count(x => x));
        await using var backup = new SqliteConnection(
            new SqliteConnectionStringBuilder { DataSource = destination, Pooling = false }.ConnectionString);
        await backup.OpenAsync();
        await using var command = backup.CreateCommand();
        command.CommandText = "PRAGMA integrity_check";
        Assert.Equal("ok", await command.ExecuteScalarAsync());
    }

    [Fact]
    public async Task Interrupted_settings_publication_retains_prior_valid_configuration()
    {
        await using var fixture = await Fixture.CreateAsync();
        var settingsPath = Path.Combine(fixture.Root, "settings", "database.json");
        DatabaseConfiguration.Save(Settings("first.db"), settingsPath);
        var prior = await File.ReadAllBytesAsync(settingsPath);

        Assert.Throws<IOException>(() => DatabaseConfiguration.Save(
            Settings("second.db"),
            settingsPath,
            () => throw new IOException("Injected interruption before publication.")));

        Assert.Equal(prior, await File.ReadAllBytesAsync(settingsPath));
        var recovered = DatabaseConfiguration.Load(settingsPath, fixture.SourcePath);
        Assert.EndsWith("first.db", DatabaseConfiguration.GetSqlitePath(recovered.ConnectionString),
            StringComparison.OrdinalIgnoreCase);
        Assert.Empty(TemporarySiblings(settingsPath));
    }

    [Fact]
    public async Task Locked_or_read_only_settings_retain_prior_valid_configuration()
    {
        await using var fixture = await Fixture.CreateAsync();
        var settingsPath = Path.Combine(fixture.Root, "settings", "database.json");
        DatabaseConfiguration.Save(Settings("first.db"), settingsPath);
        var prior = await File.ReadAllBytesAsync(settingsPath);

        using (new FileStream(settingsPath, FileMode.Open, FileAccess.Read, FileShare.Read))
        {
            Assert.ThrowsAny<IOException>(() =>
                DatabaseConfiguration.Save(Settings("locked.db"), settingsPath));
        }
        Assert.Equal(prior, await File.ReadAllBytesAsync(settingsPath));

        File.SetAttributes(settingsPath, File.GetAttributes(settingsPath) | FileAttributes.ReadOnly);
        try
        {
            var error = Record.Exception(() =>
                DatabaseConfiguration.Save(Settings("read-only.db"), settingsPath));
            Assert.True(error is IOException or UnauthorizedAccessException,
                $"Expected a filesystem permission failure, received {error?.GetType().Name ?? "no exception"}.");
            Assert.Equal(prior, await File.ReadAllBytesAsync(settingsPath));
        }
        finally
        {
            File.SetAttributes(settingsPath, FileAttributes.Normal);
        }
    }

    [Fact]
    public async Task Concurrent_settings_writers_publish_one_whole_valid_document_without_temp_collisions()
    {
        await using var fixture = await Fixture.CreateAsync();
        var settingsPath = Path.Combine(fixture.Root, "settings", "database.json");
        var expected = Enumerable.Range(0, 16)
            .Select(index => Path.GetFullPath(Path.Combine(fixture.Root, $"db-{index}.db")))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        await Task.WhenAll(expected.Select(path => Task.Run(() =>
            DatabaseConfiguration.Save(Settings(path), settingsPath))));

        await using var stream = File.OpenRead(settingsPath);
        var persisted = await JsonSerializer.DeserializeAsync<DatabaseSettings>(stream);
        Assert.NotNull(persisted);
        Assert.Contains(DatabaseConfiguration.GetSqlitePath(persisted.ConnectionString)!, expected);
        var loaded = DatabaseConfiguration.Load(settingsPath, fixture.SourcePath);
        Assert.Contains(DatabaseConfiguration.GetSqlitePath(loaded.ConnectionString)!, expected);
        Assert.Empty(TemporarySiblings(settingsPath));
    }

    [Fact]
    public async Task Failed_initial_marker_publication_never_becomes_startup_control_input()
    {
        await using var fixture = await Fixture.CreateAsync();
        var service = fixture.CreateService(new DeveloperDatabaseFailureHooks
        {
            BeforeSettingsOrMarkerPublication = () =>
                throw new IOException("Injected interruption before marker publication.")
        });

        await Assert.ThrowsAsync<IOException>(() => service.StageFreshAsync());

        Assert.False(File.Exists(fixture.MarkerPath));
        Assert.Empty(TemporarySiblings(fixture.MarkerPath));
        Assert.Null(new PendingDatabaseOperationConflictProbe(fixture.MarkerPath)
            .TryClaim(fixture.SourcePath));
        Assert.Single(Directory.GetFiles(fixture.BackupDirectory, "*.db"));
    }

    [Fact]
    public async Task Concurrent_pending_operation_writers_publish_exactly_one_valid_marker()
    {
        await using var fixture = await Fixture.CreateAsync();

        var outcomes = await Task.WhenAll(Enumerable.Range(0, 4).Select(async _ =>
        {
            try
            {
                await fixture.CreateService().StageFreshAsync();
                return true;
            }
            catch (InvalidOperationException)
            {
                return false;
            }
        }));

        Assert.Equal(1, outcomes.Count(x => x));
        await using var marker = File.OpenRead(fixture.MarkerPath);
        var operation = await JsonSerializer.DeserializeAsync<PendingDatabaseOperation>(marker);
        Assert.NotNull(operation);
        Assert.Equal(PendingDatabaseOperationType.Fresh, operation.Type);
        Assert.Equal(PendingDatabaseOperationState.ExecutionRequired, operation.State);
        Assert.Single(Directory.GetFiles(fixture.BackupDirectory, "*.db"));
        Assert.Empty(TemporarySiblings(fixture.MarkerPath));
    }

    [Fact]
    public async Task Normal_stage_load_publishes_complete_staged_database_before_marker()
    {
        await using var fixture = await Fixture.CreateAsync();
        var selected = Path.Combine(fixture.Root, "selected.db");
        await Fixture.CreateDatabaseAsync(selected, "selected-value", includeProductTables: true);

        var automaticBackup = await fixture.Service.StageLoadAsync(selected);

        Assert.True(File.Exists(automaticBackup));
        Assert.True(File.Exists(fixture.StagedPath));
        Assert.True(File.Exists(fixture.MarkerPath));
        await using var staged = new SqliteConnection(
            new SqliteConnectionStringBuilder { DataSource = fixture.StagedPath, Pooling = false }.ConnectionString);
        await staged.OpenAsync();
        await using var command = staged.CreateCommand();
        command.CommandText = "SELECT Value FROM BackupProof WHERE Id=1";
        Assert.Equal("selected-value", await command.ExecuteScalarAsync());
    }

    private static DatabaseSettings Settings(string databasePath) => new()
    {
        Provider = DatabaseProvider.Sqlite,
        ConnectionString = DatabaseConfiguration.CreateSqliteConnectionString(databasePath)
    };

    private static string[] TemporarySiblings(string path)
    {
        var directory = Path.GetDirectoryName(path)!;
        if (!Directory.Exists(directory)) return [];
        return Directory.GetFiles(directory, $".{Path.GetFileName(path)}.*.tmp");
    }

    private static void CreateDirectoryAlias(string alias, string target)
    {
        try
        {
            Directory.CreateSymbolicLink(alias, target);
            return;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            var start = new ProcessStartInfo("cmd.exe")
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden
            };
            start.ArgumentList.Add("/c");
            start.ArgumentList.Add("mklink");
            start.ArgumentList.Add("/J");
            start.ArgumentList.Add(alias);
            start.ArgumentList.Add(target);
            using var process = Process.Start(start)
                ?? throw new InvalidOperationException("Could not start junction creation.");
            process.WaitForExit();
            Assert.True(process.ExitCode == 0 && Directory.Exists(alias),
                $"This Windows filesystem did not permit a test reparse-point alias (exit {process.ExitCode}).");
        }
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CreateHardLink(
        string fileName,
        string existingFileName,
        IntPtr securityAttributes);

    private sealed class Fixture : IAsyncDisposable
    {
        private Fixture(string root)
        {
            Root = root;
            SourcePath = Path.Combine(root, "database", "active.db");
            BackupDirectory = Path.Combine(root, "backups");
            MarkerPath = Path.Combine(root, "control", "pending-database-operation.json");
            StagedPath = Path.Combine(root, "control", "pending-database-load.db");
            Directory.CreateDirectory(BackupDirectory);
            Service = CreateService();
        }

        internal string Root { get; }
        internal string SourcePath { get; }
        internal string BackupDirectory { get; }
        internal string MarkerPath { get; }
        internal string StagedPath { get; }
        internal DeveloperDatabaseService Service { get; }

        internal static async Task<Fixture> CreateAsync()
        {
            var root = Path.Combine(
                Path.GetTempPath(),
                $"BinTracker-SecurityBatchF-{Guid.NewGuid():N}");
            Directory.CreateDirectory(root);
            var fixture = new Fixture(root);
            await CreateDatabaseAsync(fixture.SourcePath, "source-value", includeProductTables: true);
            return fixture;
        }

        internal DeveloperDatabaseService CreateService(
            DeveloperDatabaseFailureHooks? hooks = null) =>
            new(
                Settings(SourcePath),
                BackupDirectory,
                MarkerPath,
                StagedPath,
                hooks);

        internal static async Task CreateDatabaseAsync(
            string path,
            string value,
            bool includeProductTables)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            await using var connection = new SqliteConnection(
                new SqliteConnectionStringBuilder { DataSource = path, Pooling = false }.ConnectionString);
            await connection.OpenAsync();
            await using var command = connection.CreateCommand();
            command.CommandText = includeProductTables
                ? """
                    CREATE TABLE BackupProof (Id INTEGER PRIMARY KEY, Value TEXT NOT NULL);
                    INSERT INTO BackupProof (Id, Value) VALUES (1, $value);
                    CREATE TABLE Customers (Id INTEGER PRIMARY KEY);
                    CREATE TABLE UserAccounts (Id INTEGER PRIMARY KEY);
                    CREATE TABLE ApplicationSettings (Id INTEGER PRIMARY KEY);
                    """
                : """
                    CREATE TABLE BackupProof (Id INTEGER PRIMARY KEY, Value TEXT NOT NULL);
                    INSERT INTO BackupProof (Id, Value) VALUES (1, $value);
                    """;
            command.Parameters.AddWithValue("$value", value);
            await command.ExecuteNonQueryAsync();
        }

        public ValueTask DisposeAsync()
        {
            if (Directory.Exists(Root)) Directory.Delete(Root, recursive: true);
            return ValueTask.CompletedTask;
        }
    }
}
