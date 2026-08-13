using System.Text.Json;
using Microsoft.Data.Sqlite;

namespace BinTracker.Data;

public enum PendingDatabaseOperationType
{
    Load = 0,
    Fresh = 1
}

public sealed record DeveloperDatabaseStatus(
    string ActiveDatabasePath,
    string BackupFolder,
    bool PendingRestart);

public interface IDeveloperDatabaseService
{
    DeveloperDatabaseStatus GetStatus();
    Task BackupAsync(string destinationPath, CancellationToken cancellationToken = default);
    Task<string> StageLoadAsync(string sourcePath, CancellationToken cancellationToken = default);
    Task<string> StageFreshAsync(CancellationToken cancellationToken = default);
}

internal sealed record PendingDatabaseOperation(
    PendingDatabaseOperationType Type,
    string ActiveDatabasePath,
    string? StagedDatabasePath,
    string? AutomaticBackupPath,
    DateTime CreatedUtc);

internal sealed class DeveloperDatabaseService : IDeveloperDatabaseService
{
    public DeveloperDatabaseStatus GetStatus() => new(
        DatabaseSetup.ActiveSqlitePath,
        DatabaseConfiguration.DeveloperBackupFolder,
        File.Exists(DatabaseConfiguration.PendingDatabaseOperationPath));

    public async Task BackupAsync(
        string destinationPath,
        CancellationToken cancellationToken = default)
    {
        EnsureSqlite();

        if (string.IsNullOrWhiteSpace(destinationPath))
            throw new ArgumentException("Choose a backup destination.");

        var activePath = DatabaseSetup.ActiveSqlitePath;
        var fullDestination = Path.GetFullPath(destinationPath);

        if (Path.GetFullPath(activePath).Equals(
                fullDestination,
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "Backup destination cannot be the active BinTracker database.");
        }

        Directory.CreateDirectory(
            Path.GetDirectoryName(fullDestination)
            ?? throw new InvalidOperationException("Backup destination folder is invalid."));

        await using var source = new SqliteConnection(DatabaseSetup.ConnectionString);
        await using var destination =
            new SqliteConnection($"Data Source={fullDestination}");

        await source.OpenAsync(cancellationToken);
        await destination.OpenAsync(cancellationToken);

        source.BackupDatabase(destination);
    }

    public async Task<string> StageLoadAsync(
        string sourcePath,
        CancellationToken cancellationToken = default)
    {
        EnsureSqlite();

        if (string.IsNullOrWhiteSpace(sourcePath) || !File.Exists(sourcePath))
            throw new FileNotFoundException("Choose an existing BinTracker SQLite database.", sourcePath);

        await ValidateDatabaseAsync(sourcePath, cancellationToken);

        var automaticBackup = await CreateAutomaticBackupAsync(
            "before-load",
            cancellationToken);

        var staged = DatabaseConfiguration.PendingDatabaseFilePath;
        Directory.CreateDirectory(DatabaseConfiguration.AppFolder);
        File.Copy(sourcePath, staged, overwrite: true);

        WritePending(new PendingDatabaseOperation(
            PendingDatabaseOperationType.Load,
            DatabaseSetup.ActiveSqlitePath,
            staged,
            automaticBackup,
            DateTime.UtcNow));

        return automaticBackup;
    }

    public async Task<string> StageFreshAsync(
        CancellationToken cancellationToken = default)
    {
        EnsureSqlite();

        var automaticBackup = await CreateAutomaticBackupAsync(
            "before-fresh",
            cancellationToken);

        WritePending(new PendingDatabaseOperation(
            PendingDatabaseOperationType.Fresh,
            DatabaseSetup.ActiveSqlitePath,
            null,
            automaticBackup,
            DateTime.UtcNow));

        return automaticBackup;
    }

    private static async Task ValidateDatabaseAsync(
        string path,
        CancellationToken cancellationToken)
    {
        try
        {
            var builder = new SqliteConnectionStringBuilder
            {
                DataSource = Path.GetFullPath(path),
                Mode = SqliteOpenMode.ReadOnly
            };

            await using var connection = new SqliteConnection(builder.ConnectionString);
            await connection.OpenAsync(cancellationToken);

            await using var command = connection.CreateCommand();
            command.CommandText = """
                SELECT COUNT(*)
                FROM sqlite_master
                WHERE type='table'
                  AND name IN ('Customers', 'UserAccounts', 'ApplicationSettings');
                """;

            var count = Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken));

            if (count < 3)
            {
                throw new InvalidOperationException(
                    "The selected file does not look like a BinTracker database.");
            }
        }
        catch (InvalidOperationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                "The selected database could not be opened or validated.",
                ex);
        }
    }

    private async Task<string> CreateAutomaticBackupAsync(
        string reason,
        CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(DatabaseConfiguration.DeveloperBackupFolder);

        var file = Path.Combine(
            DatabaseConfiguration.DeveloperBackupFolder,
            $"BinTracker-{reason}-{DateTime.Now:yyyyMMdd-HHmmss}.db");

        await BackupAsync(file, cancellationToken);
        return file;
    }

    private static void WritePending(PendingDatabaseOperation operation)
    {
        Directory.CreateDirectory(DatabaseConfiguration.AppFolder);

        File.WriteAllText(
            DatabaseConfiguration.PendingDatabaseOperationPath,
            JsonSerializer.Serialize(
                operation,
                new JsonSerializerOptions { WriteIndented = true }));
    }

    private static void EnsureSqlite()
    {
        if (DatabaseSetup.Settings.Provider != DatabaseProvider.Sqlite)
        {
            throw new NotSupportedException(
                "Developer database backup/load currently supports SQLite only.");
        }
    }
}

public static class DeveloperDatabaseStartup
{
    /// <summary>
    /// Applies a staged developer database operation before EF/DI loads the
    /// active database. This is intentionally restart-based: replacing an
    /// SQLite file while DbContexts may still be alive is unsafe.
    /// </summary>
    public static void ApplyPendingOperation()
    {
        var marker = DatabaseConfiguration.PendingDatabaseOperationPath;

        if (!File.Exists(marker))
            return;

        PendingDatabaseOperation operation;

        try
        {
            operation = JsonSerializer.Deserialize<PendingDatabaseOperation>(
                File.ReadAllText(marker))
                ?? throw new InvalidOperationException(
                    "Pending developer database operation is empty.");
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                "Could not read the pending developer database operation.",
                ex);
        }

        var activePath = Path.GetFullPath(operation.ActiveDatabasePath);
        Directory.CreateDirectory(
            Path.GetDirectoryName(activePath)
            ?? throw new InvalidOperationException("Active database folder is invalid."));

        DeleteSqliteSidecars(activePath);

        switch (operation.Type)
        {
            case PendingDatabaseOperationType.Load:
                if (string.IsNullOrWhiteSpace(operation.StagedDatabasePath) ||
                    !File.Exists(operation.StagedDatabasePath))
                {
                    throw new InvalidOperationException(
                        "The staged database file is missing.");
                }

                File.Copy(
                    operation.StagedDatabasePath,
                    activePath,
                    overwrite: true);

                File.Delete(operation.StagedDatabasePath);
                break;

            case PendingDatabaseOperationType.Fresh:
                if (File.Exists(activePath))
                    File.Delete(activePath);
                break;

            default:
                throw new InvalidOperationException(
                    $"Unsupported developer database operation '{operation.Type}'.");
        }

        File.Delete(marker);
    }

    private static void DeleteSqliteSidecars(string databasePath)
    {
        foreach (var suffix in new[] { "-wal", "-shm", "-journal" })
        {
            var sidecar = databasePath + suffix;
            if (File.Exists(sidecar))
                File.Delete(sidecar);
        }
    }
}
