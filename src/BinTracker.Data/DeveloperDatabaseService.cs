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
    private FileStream? publicationLease;
    private bool completed;

    internal PendingDatabaseOperationClaim(
        string markerPath,
        FileStream stream,
        FileStream claimLock,
        FileStream publicationLease,
        ValidatedPendingDatabaseOperation operation,
        Action release,
        Action<PendingDatabaseOperationCleanupStep>? beforeCleanup = null,
        Action<PendingDatabaseOperationState>? beforeStatePersist = null)
    {
        this.markerPath = markerPath;
        this.stream = stream;
        this.claimLock = claimLock;
        this.publicationLease = publicationLease;
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
        if (!session.IsReadyForActivatedHost || session.SchemaVersion != SqliteSchema18Definition.TargetSchemaVersion)
            throw new InvalidOperationException("A pending operation can complete only after schema18 readiness.");

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
        _ = publicationLease ?? throw new ObjectDisposedException(nameof(PendingDatabaseOperationClaim));

        try
        {
            AtomicFilePublication.Write(
                markerPath,
                replacement => JsonSerializer.Serialize(replacement, operation),
                AtomicFilePublicationMode.ReplaceExisting,
                publicationLease,
                () =>
                {
                    beforeStatePersist?.Invoke(operation.State);
                    Interlocked.Exchange(ref stream, null)?.Dispose();
                });
            Interlocked.Exchange(ref stream, new FileStream(markerPath, FileMode.Open,
                FileAccess.ReadWrite, FileShare.ReadWrite | FileShare.Delete));
            Operation = operation;
        }
        catch
        {
            if (stream is null)
            {
                Interlocked.Exchange(ref stream, new FileStream(markerPath, FileMode.Open,
                    FileAccess.ReadWrite, FileShare.ReadWrite | FileShare.Delete));
            }
            throw;
        }
    }

    public void Dispose() => Close();

    private void Close()
    {
        Interlocked.Exchange(ref stream, null)?.Dispose();
        Interlocked.Exchange(ref claimLock, null)?.Dispose();
        Interlocked.Exchange(ref publicationLease, null)?.Dispose();
        release();
    }
}

internal sealed class DeveloperDatabaseFailureHooks
{
    internal Action? BeforeBackupPublication { get; init; }
    internal Action? BeforeSettingsOrMarkerPublication { get; init; }
}

internal sealed class DeveloperDatabaseService : IDeveloperDatabaseService
{
    private readonly DatabaseProvider provider;
    private readonly string activeDatabasePath;
    private readonly string connectionString;
    private readonly string developerBackupFolder;
    private readonly string pendingOperationPath;
    private readonly string pendingDatabasePath;
    private readonly DeveloperDatabaseFailureHooks? failureHooks;

    public DeveloperDatabaseService()
        : this(
            DatabaseSetup.Settings,
            DatabaseConfiguration.DeveloperBackupFolder,
            DatabaseConfiguration.PendingDatabaseOperationPath,
            DatabaseConfiguration.PendingDatabaseFilePath)
    {
    }

    internal DeveloperDatabaseService(
        DatabaseSettings settings,
        string developerBackupFolder,
        string pendingOperationPath,
        string pendingDatabasePath,
        DeveloperDatabaseFailureHooks? failureHooks = null)
    {
        ArgumentNullException.ThrowIfNull(settings);
        provider = settings.Provider;
        connectionString = settings.ConnectionString ??
            DatabaseConfiguration.CreateSqliteConnectionString(DatabaseConfiguration.DefaultSqlitePath);
        activeDatabasePath = DatabaseConfiguration.GetSqlitePath(connectionString)
            ?? DatabaseConfiguration.DefaultSqlitePath;
        this.developerBackupFolder = Path.GetFullPath(developerBackupFolder);
        this.pendingOperationPath = Path.GetFullPath(pendingOperationPath);
        this.pendingDatabasePath = Path.GetFullPath(pendingDatabasePath);
        this.failureHooks = failureHooks;
    }

    public DeveloperDatabaseStatus GetStatus() => new(
        activeDatabasePath,
        developerBackupFolder,
        File.Exists(pendingOperationPath));

    public async Task BackupAsync(
        string destinationPath,
        CancellationToken cancellationToken = default)
    {
        EnsureSqlite();

        if (string.IsNullOrWhiteSpace(destinationPath))
            throw new ArgumentException("Choose a backup destination.");

        var requestedDestination = Path.GetFullPath(destinationPath);
        var destinationName = Path.GetFileName(requestedDestination);
        if (string.IsNullOrWhiteSpace(destinationName))
            throw new InvalidOperationException("Backup destination filename is invalid.");

        var requestedDirectory = Path.GetDirectoryName(requestedDestination)
            ?? throw new InvalidOperationException("Backup destination folder is invalid.");
        Directory.CreateDirectory(requestedDirectory);
        var destinationDirectory = SqliteMigrationPath.NormalizeExistingDirectory(requestedDirectory);
        var fullDestination = Path.Combine(destinationDirectory, destinationName);
        var sourcePath = SqliteMigrationPath.NormalizeExistingDatabase(activeDatabasePath);
        var sourceIdentity = WindowsFileIdentity.Get(sourcePath);

        RejectExistingBackupDestination(fullDestination, sourceIdentity);

        var temporaryPath = Path.Combine(
            destinationDirectory,
            $".bintracker-backup-{Guid.NewGuid():N}.tmp");
        var published = false;
        Exception? failure = null;
        try
        {
            string temporaryIdentity;
            using (var pin = new FileStream(
                temporaryPath,
                FileMode.CreateNew,
                FileAccess.ReadWrite,
                FileShare.ReadWrite,
                bufferSize: 4096,
                FileOptions.WriteThrough))
            {
                temporaryIdentity = WindowsFileIdentity.Get(temporaryPath);
                if (temporaryIdentity == sourceIdentity)
                    throw new InvalidOperationException(
                        "Backup temporary storage resolves to the active BinTracker database.");

                await using (var source = SqliteUtf16Length.CreateConnection(
                    BuildUnpooledSourceConnectionString(connectionString)))
                await using (var destination = SqliteUtf16Length.CreateConnection(
                    BuildBackupConnectionString(temporaryPath)))
                {
                    await source.OpenAsync(cancellationToken);
                    await destination.OpenAsync(cancellationToken);
                    if (WindowsFileIdentity.Get(sourcePath) != sourceIdentity ||
                        WindowsFileIdentity.Get(temporaryPath) != temporaryIdentity)
                    {
                        throw new IOException("Backup source or temporary destination identity changed.");
                    }

                    source.BackupDatabase(destination);
                }

                pin.Flush(flushToDisk: true);
                if (WindowsFileIdentity.Get(sourcePath) != sourceIdentity ||
                    WindowsFileIdentity.Get(temporaryPath) != temporaryIdentity)
                {
                    throw new IOException("Backup source or temporary destination identity changed.");
                }
            }

            cancellationToken.ThrowIfCancellationRequested();
            failureHooks?.BeforeBackupPublication?.Invoke();
            RejectExistingBackupDestination(fullDestination, sourceIdentity);
            if (WindowsFileIdentity.Get(temporaryPath) != temporaryIdentity)
                throw new IOException("Backup temporary destination identity changed before publication.");

            File.Move(temporaryPath, fullDestination, overwrite: false);
            published = true;
            var publishedIdentity = WindowsFileIdentity.Get(fullDestination);
            if (publishedIdentity != temporaryIdentity || publishedIdentity == sourceIdentity)
                throw new IOException("The published backup identity could not be proven.");
        }
        catch (Exception ex)
        {
            failure = ex;
            throw;
        }
        finally
        {
            try
            {
                if (!published && File.Exists(temporaryPath)) File.Delete(temporaryPath);
            }
            catch when (failure is not null)
            {
                // Do not hide the operation failure. This unique unpublished file
                // is never authoritative and a later maintenance pass may remove it.
            }
        }
    }

    public async Task<string> StageLoadAsync(
        string sourcePath,
        CancellationToken cancellationToken = default)
    {
        EnsureSqlite();

        if (string.IsNullOrWhiteSpace(sourcePath) || !File.Exists(sourcePath))
            throw new FileNotFoundException("Choose an existing BinTracker SQLite database.", sourcePath);

        await ValidateDatabaseAsync(sourcePath, cancellationToken);

        using var publicationLease = AtomicFilePublication.AcquireLease(pendingOperationPath);
        RejectPendingOperation();

        var automaticBackup = await CreateAutomaticBackupAsync("before-load", cancellationToken);

        AtomicFilePublication.Write(
            pendingDatabasePath,
            stream =>
            {
                using var source = new FileStream(sourcePath, FileMode.Open, FileAccess.Read,
                    FileShare.Read, bufferSize: 81920, FileOptions.SequentialScan);
                source.CopyTo(stream);
            },
            AtomicFilePublicationMode.CreateOrReplace);

        WritePending(new PendingDatabaseOperation(
            PendingDatabaseOperationType.Load,
            activeDatabasePath,
            pendingDatabasePath,
            automaticBackup,
            DateTime.UtcNow), publicationLease);

        return automaticBackup;
    }

    public async Task<string> StageFreshAsync(
        CancellationToken cancellationToken = default)
    {
        EnsureSqlite();

        using var publicationLease = AtomicFilePublication.AcquireLease(pendingOperationPath);
        RejectPendingOperation();

        var automaticBackup = await CreateAutomaticBackupAsync("before-fresh", cancellationToken);

        WritePending(new PendingDatabaseOperation(
            PendingDatabaseOperationType.Fresh,
            activeDatabasePath,
            null,
            automaticBackup,
            DateTime.UtcNow), publicationLease);

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
                Mode = SqliteOpenMode.ReadOnly,
                Pooling = false
            };

            await using var connection = SqliteUtf16Length.CreateConnection(builder.ConnectionString);
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
        Directory.CreateDirectory(developerBackupFolder);

        var file = Path.Combine(
            developerBackupFolder,
            $"BinTracker-{reason}-{DateTime.UtcNow:yyyyMMddTHHmmssfffZ}-{Guid.NewGuid():N}.db");

        await BackupAsync(file, cancellationToken);
        return file;
    }

    private void WritePending(PendingDatabaseOperation operation, FileStream publicationLease)
    {
        AtomicFilePublication.Write(
            pendingOperationPath,
            stream => JsonSerializer.Serialize(
                stream,
                operation,
                new JsonSerializerOptions { WriteIndented = true }),
            AtomicFilePublicationMode.CreateNew,
            publicationLease,
            failureHooks?.BeforeSettingsOrMarkerPublication);
    }

    private void RejectPendingOperation()
    {
        if (File.Exists(pendingOperationPath))
            throw new InvalidOperationException(
                "A developer database operation is already pending restart.");
    }

    private static void RejectExistingBackupDestination(string path, string sourceIdentity)
    {
        if (!File.Exists(path)) return;

        if (WindowsFileIdentity.Get(path) == sourceIdentity)
            throw new InvalidOperationException(
                "Backup destination resolves to the active BinTracker database.");

        throw new IOException(
            "Backup destination already exists. BinTracker never overwrites a backup or unrelated file.");
    }

    internal static string BuildBackupConnectionString(string path) =>
        new SqliteConnectionStringBuilder
        {
            DataSource = path,
            Mode = SqliteOpenMode.ReadWrite,
            Pooling = false
        }.ConnectionString;

    private static string BuildUnpooledSourceConnectionString(string configuredConnectionString)
    {
        var builder = new SqliteConnectionStringBuilder(configuredConnectionString)
        {
            Pooling = false
        };
        return builder.ConnectionString;
    }

    private void EnsureSqlite()
    {
        if (provider != DatabaseProvider.Sqlite)
        {
            throw new NotSupportedException(
                "Developer database backup/load currently supports SQLite only.");
        }
    }
}
