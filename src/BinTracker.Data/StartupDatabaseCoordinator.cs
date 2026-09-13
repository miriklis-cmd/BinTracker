using BinTracker.Core;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace BinTracker.Data;

public enum StartupDatabaseState
{
    Absent,
    Pre16UpgradeRequired,
    Schema16ActivationRequired,
    Schema17ValidationRequired,
    UnsupportedFutureSchema,
    PartialOrCorrupt,
    Unknown
}

public enum StartupDatabaseFailure
{
    UnsupportedSchema,
    PartialOrCorrupt,
    OwnershipUnavailable,
    IdentityChanged,
    PreflightRejected,
    BackupFailed,
    SourceChanged,
    MigrationFailed,
    StructuralCapabilityMissing,
    CurrentHealthInvalid,
    RuntimeParticipationFailed,
    Cancelled,
    InfrastructureFailure
}

public enum StartupDatabasePhase
{
    Classification,
    BootstrapReserved,
    PhysicalOwnershipAcquired,
    BaselineProgression,
    Preflight,
    Backup,
    BackupVerified,
    SourceVerification,
    Migration,
    Schema17Published,
    NativeStructure,
    NativeHealth,
    RuntimeTransition,
    RuntimeReacquired,
    FinalValidation,
    Ready,
    Replacement,
    ReplacementPublished,
    ReplacementRuntimeTransition,
    BootstrapWaiting
}

/// <summary>A fresh observation, never an inferred rollback or readiness claim.</summary>
public sealed record StartupDatabaseObservation(
    StartupDatabaseState State, int? SchemaVersion, string? PhysicalIdentity);

public sealed class StartupDatabaseException(
    StartupDatabaseFailure failure,
    StartupDatabasePhase phase,
    StartupDatabaseObservation persistedState,
    bool schemaMutationAttempted,
    Exception inner,
    bool databaseReplacementPublished = false) : InvalidOperationException($"Database startup failed: {failure} at {phase}.", inner)
{
    public StartupDatabaseFailure Failure { get; } = failure;
    public StartupDatabasePhase Phase { get; } = phase;
    public StartupDatabaseObservation PersistedState { get; } = persistedState;
    public bool SchemaMutationAttempted { get; } = schemaMutationAttempted;
    public bool DatabaseReplacementPublished { get; } = databaseReplacementPublished;
}

/// <summary>
/// Proof of validated schema17 readiness for an explicitly activated host. The host
/// must retain this session until all database users have stopped. It is not registered
/// by normal schema16 composition and does not install any application writer.
/// </summary>
public sealed class StartupDatabaseSession : IDisposable
{
    private IDisposable? ownership;
    internal StartupDatabaseSession(StartupDatabaseState initialState, string identity, IDisposable ownership)
    {
        InitialState = initialState;
        PhysicalIdentity = identity;
        this.ownership = ownership;
    }

    public StartupDatabaseState InitialState { get; }
    public string PhysicalIdentity { get; }
    public int SchemaVersion => 17;
    public bool IsReadyForActivatedHost => ownership is not null;
    public void Dispose() => Interlocked.Exchange(ref ownership, null)?.Dispose();
}

public interface IStartupDatabaseCoordinator
{
    Task<StartupDatabaseSession> StartAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// The Data-owned activation funnel. Companion leases coordinate participating
/// clients; they cannot police old binaries or arbitrary SQLite writers.
/// </summary>
public sealed partial class SqliteStartupDatabaseCoordinator : IStartupDatabaseCoordinator
{
    private readonly string databasePath;
    private readonly string backupDirectory;
    private readonly string lockDirectory;
    private readonly PendingDatabaseOperationConflictProbe pendingOperation;
    private readonly IDatabaseUpgradeGate gate;
    private readonly SqliteLineageMigrationPreflight preflight = new();
    private readonly SqliteLineageSchema17Migrator migrator;
    private readonly Func<StartupDatabasePhase, Task>? checkpoint;

    public SqliteStartupDatabaseCoordinator(string databasePath, string? backupDirectory = null,
        string? lockDirectory = null, string? pendingOperationPath = null)
        : this(databasePath, backupDirectory, lockDirectory, pendingOperationPath, null, null) { }

    internal SqliteStartupDatabaseCoordinator(string databasePath, string? backupDirectory,
        string? lockDirectory, string? pendingOperationPath,
        Func<StartupDatabasePhase, Task>? checkpoint, ILineageSchema17FailureInjector? migrationFailures,
        Func<SqliteConnection, SqliteTransaction, Task>? beforePublicationValidation = null, Action? afterCommit = null)
    {
        this.databasePath = Path.GetFullPath(databasePath);
        this.backupDirectory = Path.GetFullPath(backupDirectory ?? DatabaseConfiguration.LineageRecoveryFolder);
        this.lockDirectory = Path.GetFullPath(lockDirectory ?? DatabaseConfiguration.DatabaseAccessLockFolder);
        pendingOperation = new PendingDatabaseOperationConflictProbe(pendingOperationPath ?? DatabaseConfiguration.PendingDatabaseOperationPath);
        gate = new WindowsFileDatabaseUpgradeGate(this.lockDirectory, pendingOperation);
        this.checkpoint = checkpoint;
        migrator = new(failureInjector: migrationFailures)
        {
            BeforePublicationValidation = beforePublicationValidation,
            AfterCommit = afterCommit
        };
    }

    public Task<StartupDatabaseSession> StartAsync(CancellationToken cancellationToken = default) =>
        StartWithExpectedIdentityAsync(null, cancellationToken);

    private async Task<StartupDatabaseSession> StartWithExpectedIdentityAsync(
        string? expectedIdentity, CancellationToken token)
    {
        var phase = StartupDatabasePhase.Classification;
        try
        {
            // Mutex ownership is thread-affine. Keep the kernel wait/acquire/release
            // on one dedicated worker; async database work continues normally.
            // This completion rendezvous lets a bootstrap loser await the creator,
            // including abandonment, without polling or holding a physical lease.
            // It conveys no database ownership: after creation the physical upgrade
            // lease/pin governs mutation; the pre-identity reservation is released.
            return await Task.Factory.StartNew(() =>
            {
                token.ThrowIfCancellationRequested();
                pendingOperation.EnsureNoConflict(databasePath);
                using var completion = new Mutex(false,
                    @"Global\BinTracker.Startup." + SqliteMigrationPath.IdentityHash(CanonicalBootstrapPath(databasePath)));
                var acquired = false;
                try
                {
                    try { acquired = completion.WaitOne(0); }
                    catch (AbandonedMutexException) { acquired = true; }
                    if (!acquired)
                    {
                        phase = StartupDatabasePhase.BootstrapWaiting;
                        checkpoint?.Invoke(StartupDatabasePhase.BootstrapWaiting).GetAwaiter().GetResult();
                        try { acquired = WaitHandle.WaitAny([completion, token.WaitHandle]) == 0; }
                        catch (AbandonedMutexException) { acquired = true; }
                    }
                    token.ThrowIfCancellationRequested();
                    // Existing-file starts need no creation rendezvous once a prior
                    // creator has finished. Physical leases remain their authority.
                    if (File.Exists(databasePath))
                    {
                        completion.ReleaseMutex();
                        acquired = false;
                    }
                    return StartCoreAsync(expectedIdentity, token).GetAwaiter().GetResult();
                }
                finally { if (acquired) completion.ReleaseMutex(); }
            }, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);
        }
        catch (StartupDatabaseException) { throw; }
        catch (Exception ex)
        {
            throw new StartupDatabaseException(ClassifyFailure(ex, phase),
                phase, await ObserveFailureAsync(), false, ex);
        }
    }

    private async Task<StartupDatabaseSession> StartCoreAsync(string? expectedIdentity, CancellationToken cancellationToken)
    {
        var phase = StartupDatabasePhase.Classification;
        var mutationAttempted = false;
        Ownership? ownership = null;
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            pendingOperation.EnsureNoConflict(databasePath);
            // A replacement handoff must never recreate a vanished publication or
            // activate a later replacement selected at the same configured path.
            if (expectedIdentity is not null &&
                (!File.Exists(databasePath) || WindowsFileIdentity.Get(databasePath) != expectedIdentity))
                throw new StartupFault(StartupDatabaseFailure.IdentityChanged);
            var initial = StartupDatabaseState.Absent;
            var created = false;
            if (!File.Exists(databasePath))
            {
                phase = StartupDatabasePhase.BootstrapReserved;
                using var reservation = ReserveBootstrap(databasePath);
                await ReachAsync(StartupDatabasePhase.BootstrapReserved);
                // A losing creator retries classification; it never truncates a file.
                if (!File.Exists(databasePath))
                {
                    using (new FileStream(databasePath, FileMode.CreateNew, FileAccess.Write, FileShare.None)) { }
                    created = true;
                    ownership = Acquire(upgrade: true);
                }
            }

            ownership ??= Acquire(upgrade: false);
            if (expectedIdentity is not null) RequireIdentity(ownership, expectedIdentity);
            if (!created)
            {
                var observed = await InspectAsync(databasePath, cancellationToken);
                initial = observed.State;
                RejectUnstartable(observed);
                if (initial != StartupDatabaseState.Schema17ValidationRequired)
                {
                    var identity = ownership.Identity;
                    ownership.Dispose();
                    ownership = Acquire(upgrade: true);
                    RequireIdentity(ownership, identity);
                    // Another participating starter may have completed activation in the gap.
                    observed = await InspectAsync(databasePath, cancellationToken);
                    RejectUnstartable(observed);
                }
            }

            phase = StartupDatabasePhase.PhysicalOwnershipAcquired;
            await ReachAsync(phase);
            ownership.VerifyIdentity();
            var state = created ? StartupDatabaseState.Absent :
                (await InspectAsync(databasePath, cancellationToken)).State;
            if (state is StartupDatabaseState.Absent or StartupDatabaseState.Pre16UpgradeRequired)
            {
                phase = StartupDatabasePhase.BaselineProgression;
                await ReachAsync(phase);
                ownership.VerifyIdentity();
                mutationAttempted = true;
                await BuildBaselineAsync(databasePath, cancellationToken);
                state = (await InspectAsync(databasePath, cancellationToken)).State;
            }

            if (state == StartupDatabaseState.Schema16ActivationRequired)
            {
                var upgrade = ownership.UpgradeLease ?? throw new StartupFault(StartupDatabaseFailure.OwnershipUnavailable);
                phase = StartupDatabasePhase.Preflight;
                await ReachAsync(phase);
                ownership.VerifyIdentity();
                await ValidateBaselineCapabilitiesAsync(databasePath, cancellationToken);
                var source = await preflight.InspectAsync(databasePath, cancellationToken);
                if (source.Classification is not (LineagePreflightClassification.Migratable or LineagePreflightClassification.ReadOnly))
                    throw new StartupFault(StartupDatabaseFailure.PreflightRejected);

                phase = StartupDatabasePhase.Backup;
                await ReachAsync(phase);
                var backups = new SqliteLineageMigrationBackupService(gate, preflight);
                var backup = await backups.CreateVerifiedAsync(upgrade, backupDirectory, cancellationToken);
                phase = StartupDatabasePhase.BackupVerified;
                await ReachAsync(phase);

                phase = StartupDatabasePhase.SourceVerification;
                ownership.VerifyIdentity();
                await ReachAsync(phase);
                // The preflight fingerprint describes lineage relationships, not every value.
                // Compare all persisted source values to the verified provider snapshot too.
                await SqliteStartupInspection.RequireEquivalentAsync(databasePath, backup.BackupPath, cancellationToken);
                var current = await preflight.InspectAsync(databasePath, cancellationToken);
                if (current.StructuralFingerprint != source.StructuralFingerprint || current.Counts != source.Counts ||
                    current.Classification != source.Classification)
                    throw new StartupFault(StartupDatabaseFailure.SourceChanged);
                ownership.VerifyIdentity();
                phase = StartupDatabasePhase.Migration;
                await ReachAsync(phase);
                ownership.VerifyIdentity();
                // Check again after the lifecycle boundary, immediately before the migrator.
                await SqliteStartupInspection.RequireEquivalentAsync(databasePath, backup.BackupPath, cancellationToken);
                await migrator.MigrateForStartupAsync(new(upgrade, current, backup, backups),
                    async (c, tx, token) =>
                    {
                        ownership.VerifyIdentity();
                        await SqliteStartupInspection.RequireEquivalentAsync(c, tx, backup.BackupPath, token);
                    }, () => mutationAttempted = true, cancellationToken);
                phase = StartupDatabasePhase.Schema17Published;
                await ReachAsync(phase);
            }
            else if (state != StartupDatabaseState.Schema17ValidationRequired)
                throw new StartupFault(StartupDatabaseFailure.PartialOrCorrupt);

            phase = StartupDatabasePhase.NativeStructure;
            await ReachAsync(phase);
            await ValidateNativeAsync(ownership, async () =>
            {
                phase = StartupDatabasePhase.NativeHealth;
                await ReachAsync(phase);
            }, cancellationToken);

            if (ownership.UpgradeLease is not null)
            {
                var identity = ownership.Identity;
                phase = StartupDatabasePhase.RuntimeTransition;
                ownership.Dispose();
                ownership = null;
                await ReachAsync(phase);
                ownership = Acquire(upgrade: false);
                RequireIdentity(ownership, identity);
            }
            phase = StartupDatabasePhase.RuntimeReacquired;
            await ReachAsync(phase);
            phase = StartupDatabasePhase.FinalValidation;
            await ReachAsync(phase);
            await ValidateNativeAsync(ownership, null, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            if (expectedIdentity is not null) RequireIdentity(ownership, expectedIdentity);
            phase = StartupDatabasePhase.Ready;
            // There is deliberately no callback between final proof and handoff.
            var session = new StartupDatabaseSession(initial, ownership.Identity, ownership);
            ownership = null;
            return session;

            Task ReachAsync(StartupDatabasePhase next) => checkpoint?.Invoke(next) ?? Task.CompletedTask;
        }
        catch (Exception ex)
        {
            var observed = await ObserveFailureAsync();
            throw new StartupDatabaseException(ClassifyFailure(ex, phase), phase, observed, mutationAttempted, ex);
        }
        finally { ownership?.Dispose(); }
    }

    private Ownership Acquire(bool upgrade) => new(upgrade ? gate.AcquireUpgrade(databasePath) : gate.AcquireRuntime(databasePath), databasePath);

    private static void RequireIdentity(Ownership ownership, string expected)
    {
        ownership.VerifyIdentity();
        if (ownership.Identity != expected) throw new StartupFault(StartupDatabaseFailure.IdentityChanged);
    }

    private async Task ValidateNativeAsync(Ownership ownership, Func<Task>? beforeHealth, CancellationToken token,
        string? selectedPath = null)
    {
        ownership.VerifyIdentity();
        await using var c = await SqliteStartupInspection.OpenAsync(selectedPath ?? databasePath, token);
        await using var tx = c.BeginTransaction(System.Data.IsolationLevel.Serializable, deferred: true);
        var state = await SqliteStartupInspection.InspectAsync(c, tx, ownership.Identity, token);
        RejectUnstartable(state);
        if (state.State != StartupDatabaseState.Schema17ValidationRequired)
            throw new StartupFault(StartupDatabaseFailure.PartialOrCorrupt);
        await SqliteSchema17Capabilities.ValidateAsync(c, tx, token);
        if (beforeHealth is not null) await beforeHealth();
        try
        {
            await SqliteLineageSchema17Migrator.ValidateStructuralAndCurrentHealthAsync(c, tx,
                "STARTUP_LINEAGE_TABLE_MISSING", "STARTUP_LINEAGE_HEALTH_INVALID", token,
                () => new StartupFault(StartupDatabaseFailure.CurrentHealthInvalid));
            await SqliteOperationalMovementProjectionAuthority.QueryInSnapshotAsync(c, tx,
                OperationalMovementProjectionScope.All(), token);
        }
        catch (Exception ex) when (ex is OperationalMovementProjectionException or OverflowException)
        {
            throw new StartupFault(StartupDatabaseFailure.CurrentHealthInvalid, ex);
        }
        ownership.VerifyIdentity();
    }

    private static async Task BuildBaselineAsync(string path, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        await using var db = new BinTrackerDbContext(new DbContextOptionsBuilder<BinTrackerDbContext>()
            .UseSqlite(SqliteStartupInspection.ConnectionString(path, readOnly: false)).Options);
        // The legacy numbered catalogue is reused only after classification/ownership.
        // It remains schema16 and is not another schema17 startup authority.
        await DatabaseSetup.InitializeSqliteAsync(db);
        token.ThrowIfCancellationRequested();
    }

    private static async Task ValidateBaselineCapabilitiesAsync(string path, CancellationToken token)
    {
        await using var c = await SqliteStartupInspection.OpenAsync(path, token);
        await using var tx = c.BeginTransaction(System.Data.IsolationLevel.Serializable, deferred: true);
        await SqliteSchema17Capabilities.ValidateBaseAsync(c, tx, token);
    }

    private static async Task<StartupDatabaseObservation> InspectAsync(string path, CancellationToken token)
    {
        if (!File.Exists(path)) return new(StartupDatabaseState.Absent, null, null);
        var identity = WindowsFileIdentity.Get(path);
        await using var c = await SqliteStartupInspection.OpenAsync(path, token);
        await using var tx = c.BeginTransaction(System.Data.IsolationLevel.Serializable, deferred: true);
        var result = await SqliteStartupInspection.InspectAsync(c, tx, identity, token);
        if (WindowsFileIdentity.Get(path) != identity)
            return new(StartupDatabaseState.Unknown, null, null);
        return result;
    }

    private async Task<StartupDatabaseObservation> ObserveFailureAsync()
    {
        try { return await InspectAsync(databasePath, CancellationToken.None); }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or SqliteException or InvalidOperationException)
        {
            return new(StartupDatabaseState.Unknown, null, null);
        }
    }

    private static void RejectUnstartable(StartupDatabaseObservation observation)
    {
        if (observation.State == StartupDatabaseState.UnsupportedFutureSchema)
            throw new StartupFault(StartupDatabaseFailure.UnsupportedSchema);
        if (observation.State is StartupDatabaseState.PartialOrCorrupt or StartupDatabaseState.Absent or StartupDatabaseState.Unknown)
            throw new StartupFault(StartupDatabaseFailure.PartialOrCorrupt);
    }

    private static StartupDatabaseFailure ClassifyFailure(Exception ex, StartupDatabasePhase phase) => ex switch
    {
        StartupFault fault => fault.Failure,
        OperationCanceledException => StartupDatabaseFailure.Cancelled,
        DatabaseUpgradeUnavailableException => phase == StartupDatabasePhase.RuntimeTransition
            ? StartupDatabaseFailure.RuntimeParticipationFailed : StartupDatabaseFailure.OwnershipUnavailable,
        _ when phase is StartupDatabasePhase.Backup or StartupDatabasePhase.BackupVerified => StartupDatabaseFailure.BackupFailed,
        _ when phase == StartupDatabasePhase.Migration => StartupDatabaseFailure.MigrationFailed,
        _ => StartupDatabaseFailure.InfrastructureFailure
    };

    private IDisposable ReserveBootstrap(string path)
    {
        var canonical = CanonicalBootstrapPath(path);
        Directory.CreateDirectory(lockDirectory);
        var reservationPath = Path.Combine(lockDirectory, SqliteMigrationPath.IdentityHash(canonical) + ".bootstrap.lock");
        try { return new FileStream(reservationPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None); }
        catch (IOException ex) { throw new StartupFault(StartupDatabaseFailure.OwnershipUnavailable, ex); }
    }

    private static string CanonicalBootstrapPath(string path)
    {
        var parent = Path.GetDirectoryName(path) ?? throw new ArgumentException("Database parent is required.");
        Directory.CreateDirectory(parent);
        return Path.Combine(SqliteMigrationPath.NormalizeExistingDirectory(parent), Path.GetFileName(path));
    }

    private sealed class Ownership : IDisposable
    {
        private readonly IDatabaseAccessLease lease;
        private readonly FileStream pin;
        private readonly string selectedPath;
        internal Ownership(IDatabaseAccessLease lease, string? selectedPath = null)
        {
            this.lease = lease;
            this.selectedPath = selectedPath ?? lease.DatabasePath;
            try
            {
                // Keep the physical object present while owned, including the runtime lifetime.
                pin = new(lease.DatabasePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                VerifyIdentity();
            }
            catch { pin?.Dispose(); lease.Dispose(); throw; }
        }
        internal string Identity => lease.DatabaseFileIdentity;
        internal IDatabaseUpgradeLease? UpgradeLease => lease as IDatabaseUpgradeLease;
        internal void ReleasePinForReplacement() => pin.Dispose();
        internal void VerifyIdentity()
        {
            if (WindowsFileIdentity.Get(lease.DatabasePath) != Identity || WindowsFileIdentity.Get(selectedPath) != Identity)
                throw new StartupFault(StartupDatabaseFailure.IdentityChanged);
        }
        public void Dispose() { pin.Dispose(); lease.Dispose(); }
    }
}

internal sealed class StartupFault(StartupDatabaseFailure failure, Exception? inner = null)
    : InvalidOperationException(failure.ToString(), inner)
{
    internal StartupDatabaseFailure Failure { get; } = failure;
}
