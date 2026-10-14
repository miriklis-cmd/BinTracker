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

internal sealed record ValidatedPendingDatabaseOperation(
    PendingDatabaseOperation Operation,
    string? StagedDatabasePath)
{
    // Marker text is persisted control input, not filesystem authority. Keep its
    // legacy shape readable, but establish every operative path from trusted
    // coordinator configuration before a transition or database/file mutation.
    private static readonly HashSet<string> KnownProperties =
    [
        nameof(PendingDatabaseOperation.Type),
        nameof(PendingDatabaseOperation.ActiveDatabasePath),
        nameof(PendingDatabaseOperation.StagedDatabasePath),
        nameof(PendingDatabaseOperation.AutomaticBackupPath),
        nameof(PendingDatabaseOperation.CreatedUtc),
        nameof(PendingDatabaseOperation.State),
        nameof(PendingDatabaseOperation.PublishedDatabaseIdentity),
        nameof(PendingDatabaseOperation.DatabaseReplacementPublished)
    ];

    private static readonly HashSet<string> RequiredProperties =
    [
        nameof(PendingDatabaseOperation.Type),
        nameof(PendingDatabaseOperation.ActiveDatabasePath),
        nameof(PendingDatabaseOperation.StagedDatabasePath),
        nameof(PendingDatabaseOperation.AutomaticBackupPath),
        nameof(PendingDatabaseOperation.CreatedUtc)
    ];

    internal static ValidatedPendingDatabaseOperation Read(
        Stream stream,
        string markerPath,
        string activeDatabasePath)
    {
        using var document = JsonDocument.Parse(stream);
        if (document.RootElement.ValueKind != JsonValueKind.Object)
            throw new InvalidOperationException(
                "Pending developer database operation must be a JSON object.");

        var present = new HashSet<string>(StringComparer.Ordinal);
        foreach (var property in document.RootElement.EnumerateObject())
        {
            if (!KnownProperties.Contains(property.Name))
                throw new InvalidOperationException(
                    $"Pending developer database operation contains unexpected field '{property.Name}'.");
            if (!present.Add(property.Name))
                throw new InvalidOperationException(
                    $"Pending developer database operation contains duplicate field '{property.Name}'.");
        }

        if (!RequiredProperties.IsSubsetOf(present))
            throw new InvalidOperationException(
                "Pending developer database operation is missing required fields.");

        var operation = document.RootElement.Deserialize<PendingDatabaseOperation>()
            ?? throw new InvalidOperationException(
                "Pending developer database operation is empty.");
        return Validate(operation, markerPath, activeDatabasePath);
    }

    private static ValidatedPendingDatabaseOperation Validate(
        PendingDatabaseOperation operation,
        string markerPath,
        string activeDatabasePath)
    {
        if (!Enum.IsDefined(operation.Type))
            throw new InvalidOperationException(
                "Pending developer database operation type is unsupported.");
        if (!Enum.IsDefined(operation.State))
            throw new InvalidOperationException(
                "Pending developer database operation state is unsupported.");
        if (operation.CreatedUtc == default)
            throw new InvalidOperationException(
                "Pending developer database operation creation time is missing.");

        var expectedActivePath = Path.GetFullPath(activeDatabasePath);
        if (string.IsNullOrWhiteSpace(operation.ActiveDatabasePath) ||
            !Path.IsPathFullyQualified(operation.ActiveDatabasePath) ||
            !Path.GetFullPath(operation.ActiveDatabasePath).Equals(
                expectedActivePath, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "The pending developer database operation targets a different active database.");
        }

        var markerFolder = Path.GetDirectoryName(markerPath)
            ?? throw new InvalidOperationException("Pending operation folder is invalid.");
        var expectedStagedPath = Path.GetFullPath(
            Path.Combine(markerFolder, "pending-database-load.db"));
        string? trustedStagedPath = null;

        if (operation.Type == PendingDatabaseOperationType.Load)
        {
            if (string.IsNullOrWhiteSpace(operation.StagedDatabasePath) ||
                !Path.IsPathFullyQualified(operation.StagedDatabasePath) ||
                !Path.GetFullPath(operation.StagedDatabasePath).Equals(
                    expectedStagedPath, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    "The staged database path is missing or unexpected.");
            }

            trustedStagedPath = expectedStagedPath;
            if (operation.State == PendingDatabaseOperationState.ExecutionRequired &&
                !File.Exists(trustedStagedPath))
            {
                throw new InvalidOperationException("The staged database file is missing.");
            }
        }
        else if (operation.StagedDatabasePath is not null)
        {
            throw new InvalidOperationException(
                "A fresh-database operation cannot reference a staged database file.");
        }

        switch (operation.State)
        {
            case PendingDatabaseOperationState.ExecutionRequired:
            case PendingDatabaseOperationState.ExecutionInProgress:
                if (operation.PublishedDatabaseIdentity is not null ||
                    operation.DatabaseReplacementPublished)
                {
                    throw new InvalidOperationException(
                        "The pending operation publication state is inconsistent.");
                }
                break;
            case PendingDatabaseOperationState.PublicationReadyForCleanup:
                if (string.IsNullOrWhiteSpace(operation.PublishedDatabaseIdentity))
                    throw new InvalidOperationException(
                        "The pending operation has no published database identity.");
                break;
        }

        return new(operation, trustedStagedPath);
    }
}

internal sealed class PendingDatabaseOperationClaim : IDisposable
{
    private readonly string markerPath;
    private readonly Action release;
    private readonly Action<PendingDatabaseOperationCleanupStep>? beforeCleanup;
    private readonly Action<PendingDatabaseOperationState>? beforeStatePersist;
    private readonly string? stagedDatabasePath;
    private FileStream? stream;
    private FileStream? claimLock;
    private bool completed;

    internal PendingDatabaseOperationClaim(
        string markerPath,
        FileStream stream,
        FileStream claimLock,
        ValidatedPendingDatabaseOperation operation,
        Action release,
        Action<PendingDatabaseOperationCleanupStep>? beforeCleanup = null,
        Action<PendingDatabaseOperationState>? beforeStatePersist = null)
    {
        this.markerPath = markerPath;
        this.stream = stream;
        this.claimLock = claimLock;
        Operation = operation.Operation;
        stagedDatabasePath = operation.StagedDatabasePath;
        this.release = release;
        this.beforeCleanup = beforeCleanup;
        this.beforeStatePersist = beforeStatePersist;
    }

    internal PendingDatabaseOperation Operation { get; private set; }
    internal bool IsActive => claimLock is not null;

    internal string RequireStagedDatabasePath()
    {
        if (stagedDatabasePath is null)
            throw new InvalidOperationException("The staged database path is missing.");
        if (!File.Exists(stagedDatabasePath))
            throw new InvalidOperationException("The staged database file is missing.");
        return stagedDatabasePath;
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
        if (stagedDatabasePath is not null)
        {
            beforeCleanup?.Invoke(PendingDatabaseOperationCleanupStep.StagedDatabase);
            File.Delete(stagedDatabasePath);
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
