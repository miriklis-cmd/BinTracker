using Microsoft.Data.Sqlite;

namespace BinTracker.Data;

public sealed partial class SqliteStartupDatabaseCoordinator
{
    /// <summary>
    /// Explicit activated-host developer Load. The caller must first stop its own
    /// database users/session. The old database is preserved, and the replacement
    /// enters StartAsync from its new physical identity. Normal UI is not wired here.
    /// </summary>
    public Task<StartupDatabaseSession> LoadAsync(string sourcePath, CancellationToken cancellationToken = default) =>
        ReplaceAsync(Path.GetFullPath(sourcePath), cancellationToken);

    /// <summary>Creates a fresh baseline under maintenance ownership, then reclassifies it.</summary>
    public Task<StartupDatabaseSession> FreshAsync(CancellationToken cancellationToken = default) =>
        ReplaceAsync(null, cancellationToken);

    private async Task<StartupDatabaseSession> ReplaceAsync(string? sourcePath, CancellationToken token)
    {
        var stage = databasePath + $".replacement-{Guid.NewGuid():N}.db";
        var published = false;
        string replacementIdentity;
        try
        {
            token.ThrowIfCancellationRequested();
            pendingOperation.EnsureNoConflict(databasePath);
            if (!File.Exists(databasePath))
            {
                // Fresh creation already belongs to the ordinary bootstrap funnel.
                if (sourcePath is null) return await StartAsync(token);
            }
            using var bootstrap = File.Exists(databasePath) ? null : ReserveBootstrap(databasePath);
            using (var oldLease = File.Exists(databasePath) ? gate.AcquireUpgrade(databasePath) : null)
            {
                var oldIdentity = oldLease?.DatabaseFileIdentity;
                using var pin = oldLease is null ? null : new FileStream(databasePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                if (oldIdentity is not null)
                {
                    if (WindowsFileIdentity.Get(databasePath) != oldIdentity)
                        throw new StartupFault(StartupDatabaseFailure.IdentityChanged);
                    RejectUnstartable(await InspectAsync(databasePath, token));
                }
                using var reservation = ReserveBootstrap(stage);
                using (new FileStream(stage, FileMode.CreateNew, FileAccess.Write, FileShare.None)) { }
                using var stagedOwnership = new Ownership(gate.AcquireUpgrade(stage));
                reservation.Dispose();
                if (sourcePath is null)
                    await BuildBaselineAsync(stage, token);
                else
                {
                    using var sourceOwnership = new Ownership(gate.AcquireRuntime(sourcePath), sourcePath);
                    RejectUnstartable(await InspectAsync(sourcePath, token));
                    await using var source = await SqliteStartupInspection.OpenAsync(sourcePath, token);
                    await using var target = new SqliteConnection(SqliteStartupInspection.ConnectionString(stage, readOnly: false));
                    await target.OpenAsync(token);
                    source.BackupDatabase(target);
                    sourceOwnership.VerifyIdentity();
                }
                stagedOwnership.VerifyIdentity();
                replacementIdentity = stagedOwnership.Identity;
                var stagedState = await InspectAsync(stage, token);
                RejectUnstartable(stagedState);
                if (stagedState.State == StartupDatabaseState.Pre16UpgradeRequired)
                {
                    await BuildBaselineAsync(stage, token);
                    stagedState = await InspectAsync(stage, token);
                    RejectUnstartable(stagedState);
                }
                // Validate the stable provider snapshot before selecting it. Live-source
                // validation alone could race another allowed writer on that source.
                if (stagedState.State == StartupDatabaseState.Schema17ValidationRequired)
                    await ValidateNativeAsync(stagedOwnership, null, token, stage);
                else
                {
                    await ValidateBaselineCapabilitiesAsync(stage, token);
                    var sourceCheck = await preflight.InspectAsync(stage, token);
                    if (sourceCheck.Classification is not (LineagePreflightClassification.Migratable or LineagePreflightClassification.ReadOnly))
                        throw new StartupFault(StartupDatabaseFailure.PreflightRejected);
                }

                // Own both physical identities across atomic replacement. The pins
                // are intentionally released for File.Replace; companion ownership
                // remains exclusive. SQLite handles are closed and WAL is checkpointed.
                if (oldIdentity is not null) await CheckpointForReplacementAsync(databasePath, token);
                await CheckpointForReplacementAsync(stage, token);
                if (checkpoint is not null) await checkpoint(StartupDatabasePhase.Replacement);
                if ((oldIdentity is null ? File.Exists(databasePath) : WindowsFileIdentity.Get(databasePath) != oldIdentity) ||
                    WindowsFileIdentity.Get(stage) != replacementIdentity)
                    throw new StartupFault(StartupDatabaseFailure.IdentityChanged);
                token.ThrowIfCancellationRequested();
                pin?.Dispose();
                stagedOwnership.ReleasePinForReplacement();
                if (oldIdentity is null)
                    File.Move(stage, databasePath, overwrite: false);
                else
                {
                    var preserved = databasePath + $".before-replacement-{Guid.NewGuid():N}.db";
                    File.Replace(stage, databasePath, preserved, ignoreMetadataErrors: false);
                }
                published = true;
                bootstrap?.Dispose();
                if (WindowsFileIdentity.Get(databasePath) != replacementIdentity)
                    throw new StartupFault(StartupDatabaseFailure.IdentityChanged);
                if (checkpoint is not null) await checkpoint(StartupDatabasePhase.ReplacementPublished);
            }
            // All replacement ownership has been relinquished. Tests can interpose
            // a real competing operation here; readiness must still belong to OUR
            // newly published identity, never merely whatever now occupies the path.
            if (checkpoint is not null) await checkpoint(StartupDatabasePhase.ReplacementRuntimeTransition);
        }
        catch (StartupDatabaseException) { throw; }
        catch (Exception ex)
        {
            // A failed replacement retains its staged/old evidence. Never restore
            // automatically or assume which physical file is now selected.
            throw new StartupDatabaseException(ClassifyFailure(ex, StartupDatabasePhase.Replacement),
                published ? StartupDatabasePhase.ReplacementPublished : StartupDatabasePhase.Replacement,
                await ObserveFailureAsync(), false, ex, published);
        }
        // Only the NEW published identity crosses this boundary. All schema and
        // health facts are reacquired by the same startup authority.
        try { return await StartWithExpectedIdentityAsync(replacementIdentity, token); }
        catch (StartupDatabaseException ex)
        {
            throw new StartupDatabaseException(ex.Failure, ex.Phase, ex.PersistedState,
                ex.SchemaMutationAttempted, ex.InnerException ?? ex, databaseReplacementPublished: true);
        }
    }

    private static async Task CheckpointForReplacementAsync(string path, CancellationToken token)
    {
        await using var c = new SqliteConnection(SqliteStartupInspection.ConnectionString(path, readOnly: false));
        await c.OpenAsync(token);
        await using var cmd = c.CreateCommand();
        cmd.CommandText = "PRAGMA wal_checkpoint(TRUNCATE)";
        await using var reader = await cmd.ExecuteReaderAsync(token);
        if (!await reader.ReadAsync(token) || reader.GetInt32(0) != 0)
            throw new StartupFault(StartupDatabaseFailure.OwnershipUnavailable);
    }
}
