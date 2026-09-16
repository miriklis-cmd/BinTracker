using System.Text.Json;
using Microsoft.Data.Sqlite;

namespace BinTracker.Data;

public enum PendingDatabaseOperationType
{
    Load = 0,
    Fresh = 1
}
internal enum PendingDatabaseOperationCleanupStep
{
    StagedDatabase,
    Marker
}
internal enum PendingDatabaseOperationState
{
    ExecutionRequired,
    ExecutionInProgress,
    PublicationReadyForCleanup
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
    DateTime CreatedUtc,
    PendingDatabaseOperationState State = PendingDatabaseOperationState.ExecutionRequired,
    string? PublishedDatabaseIdentity = null,
    bool DatabaseReplacementPublished = false);

internal sealed class PendingDatabaseOperationClaim : IDisposable
{
    private readonly string markerPath;
    private readonly Action release;
    private readonly Action<PendingDatabaseOperationCleanupStep>? beforeCleanup;
    private readonly Action<PendingDatabaseOperationState>? beforeStatePersist;
    private FileStream? stream;
    private FileStream? claimLock;
    private bool completed;

    internal PendingDatabaseOperationClaim(
        string markerPath,
        FileStream stream,
        FileStream claimLock,
        PendingDatabaseOperation operation,
        Action release,
        Action<PendingDatabaseOperationCleanupStep>? beforeCleanup = null,
        Action<PendingDatabaseOperationState>? beforeStatePersist = null)
    {
        this.markerPath = markerPath;
        this.stream = stream;
        this.claimLock = claimLock;
        Operation = operation;
        this.release = release;
        this.beforeCleanup = beforeCleanup;
        this.beforeStatePersist = beforeStatePersist;
    }

    internal PendingDatabaseOperation Operation { get; private set; }
    internal bool IsActive => claimLock is not null;

    internal string RequireStagedDatabasePath()
    {
        if (string.IsNullOrWhiteSpace(Operation.StagedDatabasePath))
            throw new InvalidOperationException("The staged database path is missing.");
        var staged = Path.GetFullPath(Operation.StagedDatabasePath);
        var expected = Path.Combine(
            Path.GetDirectoryName(markerPath)
                ?? throw new InvalidOperationException("Pending operation folder is invalid."),
            "pending-database-load.db");
        if (!staged.Equals(expected, StringComparison.OrdinalIgnoreCase) || !File.Exists(staged))
            throw new InvalidOperationException("The staged database file is missing or unexpected.");
        return staged;
    }

    internal void BeginExecution()
    {
        if (Operation.State != PendingDatabaseOperationState.ExecutionRequired)
            throw new InvalidOperationException("Pending operation cannot begin execution from its current state.");
        Persist(Operation with { State = PendingDatabaseOperationState.ExecutionInProgress });
    }

    internal void RestoreExecutionRequired()
    {
        if (Operation.State != PendingDatabaseOperationState.ExecutionInProgress)
            throw new InvalidOperationException("Pending operation cannot be restored from its current state.");
        Persist(Operation with { State = PendingDatabaseOperationState.ExecutionRequired });
    }

    internal string RequirePublishedDatabaseIdentity()
    {
        if (Operation.State != PendingDatabaseOperationState.PublicationReadyForCleanup ||
            string.IsNullOrWhiteSpace(Operation.PublishedDatabaseIdentity))
            throw new InvalidOperationException("The pending operation has no published database identity.");
        return Operation.PublishedDatabaseIdentity;
    }

    internal void Complete(StartupDatabaseSession session, bool databaseReplacementPublished)
    {
        if (completed) throw new InvalidOperationException("Pending operation is already complete.");
        ArgumentNullException.ThrowIfNull(session);
        if (!session.IsReadyForActivatedHost || session.SchemaVersion != 17)
            throw new InvalidOperationException("A pending operation can complete only after schema17 readiness.");

        if (Operation.State == PendingDatabaseOperationState.ExecutionInProgress)
        {
            Persist(Operation with
            {
                State = PendingDatabaseOperationState.PublicationReadyForCleanup,
                PublishedDatabaseIdentity = session.PhysicalIdentity,
                DatabaseReplacementPublished = databaseReplacementPublished
            });
        }
        else if (Operation.State != PendingDatabaseOperationState.PublicationReadyForCleanup)
        {
            throw new InvalidOperationException("Pending operation cannot complete from its current state.");
        }

        completed = true;
        if (!string.IsNullOrWhiteSpace(Operation.StagedDatabasePath))
        {
            beforeCleanup?.Invoke(PendingDatabaseOperationCleanupStep.StagedDatabase);
            File.Delete(Path.GetFullPath(Operation.StagedDatabasePath));
        }
        beforeCleanup?.Invoke(PendingDatabaseOperationCleanupStep.Marker);
        File.Delete(markerPath);
        Close();
    }

    private void Persist(PendingDatabaseOperation operation)
    {
        _ = stream ?? throw new ObjectDisposedException(nameof(PendingDatabaseOperationClaim));
        var temporaryPath = markerPath + $".{Guid.NewGuid():N}.tmp";
        FileStream? replacementStream = null;
        try
        {
            replacementStream = new FileStream(temporaryPath, FileMode.CreateNew, FileAccess.ReadWrite,
                FileShare.Delete, bufferSize: 4096, FileOptions.WriteThrough);
            JsonSerializer.Serialize(replacementStream, operation);
            replacementStream.Flush(flushToDisk: true);
            beforeStatePersist?.Invoke(operation.State);

            // The claim lock remains held while the marker stream is exchanged.
            // The previous marker stays complete until this same-volume replacement
            // publishes the fully flushed successor.
            replacementStream.Dispose();
            replacementStream = null;
            Interlocked.Exchange(ref stream, null)?.Dispose();
            File.Replace(temporaryPath, markerPath, destinationBackupFileName: null,
                ignoreMetadataErrors: false);
            Interlocked.Exchange(ref stream, new FileStream(markerPath, FileMode.Open,
                FileAccess.ReadWrite, FileShare.ReadWrite | FileShare.Delete));
            Operation = operation;
        }
        finally
        {
            replacementStream?.Dispose();
            if (File.Exists(temporaryPath)) File.Delete(temporaryPath);
        }
    }

    public void Dispose() => Close();

    private void Close()
    {
        Interlocked.Exchange(ref stream, null)?.Dispose();
        Interlocked.Exchange(ref claimLock, null)?.Dispose();
        release();
    }
}

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
