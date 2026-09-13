using BinTracker.Core;
using BinTracker.Data;
using Microsoft.Data.Sqlite;
using Xunit;

namespace BinTracker.IntegrationTests;

public sealed class Task20StartupCoordinatorTests
{
    [Fact]
    public async Task A2_schema16_activation_then_native_reentry_needs_no_retained_backup()
    {
        await using var f = await Task20Fixture.CreateAsync(schema17: false, enabled: false, projection: false);
        await f.Movements.SaveSingleAsync(f.Single());
        var phases = new List<StartupDatabasePhase>();
        using (var ready = await f.Coordinator(p => { phases.Add(p); return Task.CompletedTask; }).StartAsync())
        {
            Assert.True(ready.IsReadyForActivatedHost);
            Assert.Equal(StartupDatabaseState.Schema16ActivationRequired, ready.InitialState);
            Assert.Equal(WindowsFileIdentity.Get(PathOf(f)), ready.PhysicalIdentity);
            Assert.Equal(1, await f.ScalarAsync("SELECT COUNT(*) FROM LogicalMovementGenerations WHERE Kind=1"));
            Assert.Throws<DatabaseUpgradeUnavailableException>(() => Gate(f).AcquireUpgrade(PathOf(f)));
        }
        Assert.True(phases.IndexOf(StartupDatabasePhase.BackupVerified) < phases.IndexOf(StartupDatabasePhase.Migration));
        var backup = Assert.Single(Directory.GetFiles(Backups(f), "*.manifest.json"));
        File.Move(backup, backup + ".retained-offline");
        phases.Clear();
        var before = await f.StateAsync();
        using var later = await f.Coordinator(p => { phases.Add(p); return Task.CompletedTask; }).StartAsync();
        Assert.Equal(StartupDatabaseState.Schema17ValidationRequired, later.InitialState);
        Assert.DoesNotContain(StartupDatabasePhase.Preflight, phases);
        Assert.DoesNotContain(StartupDatabasePhase.Backup, phases);
        Assert.DoesNotContain(StartupDatabasePhase.Migration, phases);
        Assert.Equal(before, await f.StateAsync());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task A3_native_initial_and_later_generations_use_only_native_validation(bool later)
    {
        await using var f = await Task20Fixture.CreateAsync();
        var root = await f.Database.CreateSingleAsync(Task20Fixture.Today, f.CustomerId, 1, 7);
        if (later)
        {
            var line = Assert.Single(await f.Database.LineIdsAsync(root.RootId));
            await f.Database.MutateAsync(root.RootId, 0, MovementMutationRequest.Correct(
                MovementMutationScope.Individual, [new(line)], "native startup", quantity: MovementFieldIntent<int>.Selected(8)));
        }
        var before = await f.StateAsync();
        var phases = new List<StartupDatabasePhase>();
        using var ready = await f.Coordinator(p => { phases.Add(p); return Task.CompletedTask; }).StartAsync();
        Assert.True(ready.IsReadyForActivatedHost);
        Assert.Equal(before, await f.StateAsync());
        Assert.DoesNotContain(StartupDatabasePhase.Preflight, phases);
        Assert.DoesNotContain(StartupDatabasePhase.Migration, phases);
        Assert.False(Directory.Exists(Backups(f)));
    }

    [Theory]
    [InlineData(LineageSchema17MigrationCheckpoint.BeforeSchemaMutation)]
    [InlineData(LineageSchema17MigrationCheckpoint.AfterFirstSchemaChange)]
    [InlineData(LineageSchema17MigrationCheckpoint.DuringBaselineGenerationCreation)]
    [InlineData(LineageSchema17MigrationCheckpoint.AfterPostflightBeforePublication)]
    public async Task A8_A9_migration_failure_leaves_exact16_and_verified_backup_precedes_first_mutation(
        LineageSchema17MigrationCheckpoint failAt)
    {
        await using var f = await Task20Fixture.CreateAsync(schema17: false, enabled: false, projection: false);
        await f.Movements.SaveSingleAsync(f.Single());
        var before = await f.StateAsync();
        var injector = new MigrationFailure(failAt, () =>
        {
            Assert.Single(Directory.GetFiles(Backups(f), "*.db"));
            Assert.Single(Directory.GetFiles(Backups(f), "*.manifest.json"));
            Assert.Single(Directory.GetFiles(Backups(f), "*.checksums.json"));
        });
        var failure = await Assert.ThrowsAsync<StartupDatabaseException>(() => f.Coordinator(migrationFailures: injector).StartAsync());
        Assert.Equal(StartupDatabaseFailure.MigrationFailed, failure.Failure);
        Assert.Same(injector.Error, failure.InnerException);
        Assert.True(injector.SawFirstMutationBoundary);
        Assert.Equal(16, failure.PersistedState.SchemaVersion);
        Assert.Equal(before, await f.StateAsync());
        await AssertBackupValidAsync(f);
        using var released = Gate(f).AcquireUpgrade(PathOf(f));
    }

    [Fact]
    public async Task A8_strict_publication_rejects_initial_in_migration_transaction_and_rolls_back()
    {
        await using var f = await Task20Fixture.CreateAsync(schema17: false, enabled: false, projection: false);
        await f.Movements.SaveSingleAsync(f.Single());
        var before = await f.StateAsync();
        var failure = await Assert.ThrowsAsync<StartupDatabaseException>(() => f.Coordinator(
            beforePublicationValidation: async (c, tx) =>
            {
                await using var cmd = c.CreateCommand();
                cmd.Transaction = tx;
                cmd.CommandText = "UPDATE LogicalMovementGenerations SET Kind=0; UPDATE LogicalMovementGenerationLines SET Action=0";
                await cmd.ExecuteNonQueryAsync();
            }).StartAsync());
        Assert.Equal(StartupDatabaseFailure.MigrationFailed, failure.Failure);
        Assert.Equal("LINEAGE_POSTFLIGHT_INVARIANT_FAILURE", failure.InnerException?.Message);
        Assert.Equal(before, await f.StateAsync());
        Assert.Equal(16, failure.PersistedState.SchemaVersion);
        await AssertBackupValidAsync(f);
    }

    [Theory]
    [InlineData("structure")]
    [InlineData("health")]
    public async Task A8_A10_postpublication_validation_failure_reports_persisted17(string damage)
    {
        await using var f = await Task20Fixture.CreateAsync(schema17: false, enabled: false, projection: false);
        await f.Movements.SaveSingleAsync(f.Single());
        var identity = WindowsFileIdentity.Get(PathOf(f));
        var failure = await Assert.ThrowsAsync<StartupDatabaseException>(() => f.Coordinator(async p =>
        {
            if (p == StartupDatabasePhase.Schema17Published)
                await f.ExecuteAsync(damage == "structure" ? "DROP INDEX IX_LogicalMovementLines_RootMovementId" :
                    "UPDATE LogicalMovementBatches SET CurrentGenerationNumber=99");
        }).StartAsync());
        Assert.Equal(damage == "structure" ? StartupDatabaseFailure.StructuralCapabilityMissing :
            StartupDatabaseFailure.CurrentHealthInvalid, failure.Failure);
        Assert.Equal(17, failure.PersistedState.SchemaVersion);
        Assert.Equal(identity, failure.PersistedState.PhysicalIdentity);
        Assert.Equal(17, await f.ScalarAsync("SELECT Version FROM SchemaVersion"));
        Assert.Equal(1, await f.ScalarAsync("SELECT COUNT(*) FROM LogicalMovementBatches"));
        Assert.Single(Directory.GetFiles(Backups(f), "*.manifest.json"));
        using var released = Gate(f).AcquireUpgrade(PathOf(f));
    }

    [Fact]
    public async Task A10_immediate_postcommit_exception_is_not_masked_by_rollback()
    {
        await using var f = await Task20Fixture.CreateAsync(schema17: false, enabled: false, projection: false);
        var original = new InjectedFailure();
        var failure = await Assert.ThrowsAsync<StartupDatabaseException>(() =>
            f.Coordinator(afterCommit: () => throw original).StartAsync());
        Assert.Same(original, failure.InnerException);
        Assert.Equal(17, failure.PersistedState.SchemaVersion);
        Assert.Equal(17, await f.ScalarAsync("SELECT Version FROM SchemaVersion"));
        using var later = await f.Coordinator().StartAsync();
        Assert.Equal(StartupDatabaseState.Schema17ValidationRequired, later.InitialState);
    }

    [Theory]
    [InlineData("lease")]
    [InlineData("identity")]
    [InlineData("schema")]
    [InlineData("health")]
    [InlineData("structure")]
    public async Task A8_A11_transition_revalidates_ownership_physical_identity_schema_and_health(string change)
    {
        await using var f = await Task20Fixture.CreateAsync(schema17: false, enabled: false, projection: false);
        await f.Movements.SaveSingleAsync(f.Single());
        var identity = WindowsFileIdentity.Get(PathOf(f));
        IDatabaseUpgradeLease? competitor = null;
        try
        {
            var failure = await Assert.ThrowsAsync<StartupDatabaseException>(() => f.Coordinator(async phase =>
            {
                if (phase != StartupDatabasePhase.RuntimeTransition) return;
                switch (change)
                {
                    case "lease": competitor = Gate(f).AcquireUpgrade(PathOf(f)); break;
                    case "identity":
                        File.Copy(PathOf(f), PathOf(f) + ".swap");
                        File.Replace(PathOf(f) + ".swap", PathOf(f), PathOf(f) + ".old");
                        Assert.NotEqual(identity, WindowsFileIdentity.Get(PathOf(f)));
                        break;
                    case "schema": await f.ExecuteAsync("UPDATE SchemaVersion SET Version=18"); break;
                    case "health": await f.ExecuteAsync("UPDATE LogicalMovementBatches SET CurrentGenerationNumber=99"); break;
                    case "structure": await f.ExecuteAsync("DROP INDEX IX_LogicalMovementLines_RootMovementId"); break;
                }
            }).StartAsync());
            Assert.Equal(change switch
            {
                "lease" => StartupDatabaseFailure.RuntimeParticipationFailed,
                "identity" => StartupDatabaseFailure.IdentityChanged,
                "schema" => StartupDatabaseFailure.UnsupportedSchema,
                "health" => StartupDatabaseFailure.CurrentHealthInvalid,
                _ => StartupDatabaseFailure.StructuralCapabilityMissing
            }, failure.Failure);
            Assert.Equal(change == "schema" ? 18 : 17, failure.PersistedState.SchemaVersion);
            Assert.Equal(WindowsFileIdentity.Get(PathOf(f)), failure.PersistedState.PhysicalIdentity);
            Assert.Equal(1, await f.ScalarAsync("SELECT COUNT(*) FROM LogicalMovementBatches"));
        }
        finally { competitor?.Dispose(); }
        using var released = Gate(f).AcquireUpgrade(PathOf(f));
    }

    [Fact]
    public async Task A11_runtime_session_pins_identity_until_disposed_and_allows_shared_participation()
    {
        await using var f = await Task20Fixture.CreateAsync();
        using var first = await f.Coordinator().StartAsync();
        using var second = await f.Coordinator().StartAsync();
        Assert.Equal(first.PhysicalIdentity, second.PhysicalIdentity);
        Assert.Throws<IOException>(() => File.Move(PathOf(f), PathOf(f) + ".moved"));
        first.Dispose();
        Assert.False(first.IsReadyForActivatedHost);
        Assert.Throws<DatabaseUpgradeUnavailableException>(() => Gate(f).AcquireUpgrade(PathOf(f)));
        second.Dispose();
        using var released = Gate(f).AcquireUpgrade(PathOf(f));
    }

    [Fact]
    public async Task A13_concurrent_bootstrap_has_one_creator_and_loser_reclassifies()
    {
        using var sandbox = new BootstrapSandbox();
        var reserved = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var proceed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var waiting = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var creators = 0;
        var owner = sandbox.Coordinator(async p =>
        {
            if (p != StartupDatabasePhase.BootstrapReserved) return;
            Interlocked.Increment(ref creators);
            reserved.SetResult();
            await proceed.Task;
        });
        var first = owner.StartAsync();
        await reserved.Task.WaitAsync(TimeSpan.FromSeconds(20));
        Assert.False(File.Exists(sandbox.Path));
        var second = sandbox.Coordinator(p =>
        {
            if (p == StartupDatabasePhase.BootstrapWaiting) waiting.TrySetResult();
            if (p == StartupDatabasePhase.BootstrapReserved) Interlocked.Increment(ref creators);
            return Task.CompletedTask;
        }).StartAsync();
        try
        {
            await waiting.Task.WaitAsync(TimeSpan.FromSeconds(20));
            Assert.False(second.IsCompleted);
            Assert.False(File.Exists(sandbox.Path));
        }
        finally { proceed.TrySetResult(); }
        using var ready = await first;
        using var loserReady = await second;
        Assert.Equal(1, creators);
        Assert.Equal(StartupDatabaseState.Absent, ready.InitialState);
        Assert.Equal(StartupDatabaseState.Schema17ValidationRequired, loserReady.InitialState);
        Assert.Equal(ready.PhysicalIdentity, loserReady.PhysicalIdentity);
        Assert.True(loserReady.IsReadyForActivatedHost);
        Assert.Single(Directory.GetFiles(sandbox.Backups, "*.manifest.json"));
        Assert.Equal(17, ready.SchemaVersion);
    }

    [Fact]
    public async Task A13_waiting_participant_cancels_without_creating_or_disturbing_creator()
    {
        using var sandbox = new BootstrapSandbox();
        var reserved = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var proceed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var waiting = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var cancelled = new CancellationTokenSource();
        var first = sandbox.Coordinator(async p =>
        {
            if (p == StartupDatabasePhase.BootstrapReserved) { reserved.SetResult(); await proceed.Task; }
        }).StartAsync();
        await reserved.Task.WaitAsync(TimeSpan.FromSeconds(20));
        try
        {
            var second = sandbox.Coordinator(p =>
            {
                if (p == StartupDatabasePhase.BootstrapWaiting) waiting.SetResult();
                return Task.CompletedTask;
            }).StartAsync(cancelled.Token);
            await waiting.Task.WaitAsync(TimeSpan.FromSeconds(20));
            cancelled.Cancel();
            var failure = await Assert.ThrowsAsync<StartupDatabaseException>(() => second);
            Assert.Equal(StartupDatabaseFailure.Cancelled, failure.Failure);
            Assert.Equal(StartupDatabasePhase.BootstrapWaiting, failure.Phase);
            Assert.Equal(StartupDatabaseState.Absent, failure.PersistedState.State);
            Assert.False(failure.SchemaMutationAttempted);
            Assert.False(File.Exists(sandbox.Path));
            Assert.False(first.IsCompleted);
        }
        finally { proceed.TrySetResult(); }
        using var ready = await first;
        Assert.True(ready.IsReadyForActivatedHost);
        Assert.Single(Directory.GetFiles(sandbox.Backups, "*.manifest.json"));
    }

    [Fact]
    public async Task A13_waiter_reclassifies_interrupted_physical_bootstrap_without_recreation()
    {
        using var sandbox = new BootstrapSandbox();
        var reserved = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var proceed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var waiting = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var original = new InjectedFailure();
        var first = sandbox.Coordinator(async p =>
        {
            if (p == StartupDatabasePhase.BootstrapReserved) { reserved.SetResult(); await proceed.Task; }
            if (p == StartupDatabasePhase.PhysicalOwnershipAcquired) throw original;
        }).StartAsync();
        await reserved.Task.WaitAsync(TimeSpan.FromSeconds(20));
        var second = sandbox.Coordinator(p =>
        {
            if (p == StartupDatabasePhase.BootstrapWaiting) waiting.SetResult();
            return Task.CompletedTask;
        }).StartAsync();
        try { await waiting.Task.WaitAsync(TimeSpan.FromSeconds(20)); }
        finally { proceed.TrySetResult(); }
        var creator = await Assert.ThrowsAsync<StartupDatabaseException>(() => first);
        var loser = await Assert.ThrowsAsync<StartupDatabaseException>(() => second);
        Assert.Same(original, creator.InnerException);
        Assert.Equal(StartupDatabaseFailure.PartialOrCorrupt, loser.Failure);
        Assert.Equal(creator.PersistedState.PhysicalIdentity, loser.PersistedState.PhysicalIdentity);
        Assert.Equal(WindowsFileIdentity.Get(sandbox.Path), loser.PersistedState.PhysicalIdentity);
        Assert.Equal(0, new FileInfo(sandbox.Path).Length);
        Assert.False(loser.SchemaMutationAttempted);
        Assert.False(Directory.Exists(sandbox.Backups));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task A15_published_replacement_identity_survives_competing_handoff_interposition(bool load)
    {
        await using var target = await Task20Fixture.CreateAsync();
        await using var source = await Task20Fixture.CreateAsync();
        await using var competingSource = await Task20Fixture.CreateAsync();
        await source.Database.CreateSingleAsync(Task20Fixture.Today, source.CustomerId, 1, 7);
        await competingSource.Database.CreateSingleAsync(Task20Fixture.Today, competingSource.CustomerId, 1, 11);
        var released = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var proceed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        string? publishedIdentity = null;
        var owner = target.Coordinator(async p =>
        {
            if (p != StartupDatabasePhase.ReplacementRuntimeTransition) return;
            publishedIdentity = WindowsFileIdentity.Get(PathOf(target));
            released.SetResult();
            await proceed.Task;
        });
        var first = load ? owner.LoadAsync(PathOf(source)) : owner.FreshAsync();
        await released.Task.WaitAsync(TimeSpan.FromSeconds(20));
        StartupDatabaseSession? competitor = null;
        try
        {
            // This is a participating replacement using real maintenance ownership,
            // after the first operation relinquished BOTH publication leases.
            competitor = await target.Coordinator().LoadAsync(PathOf(competingSource));
            Assert.NotEqual(publishedIdentity, competitor.PhysicalIdentity);
            Assert.False(first.IsCompleted);
        }
        finally { proceed.TrySetResult(); }
        Assert.NotNull(publishedIdentity);
        Assert.NotNull(competitor);
        using (competitor)
        {
            var failure = await Assert.ThrowsAsync<StartupDatabaseException>(() => first);
            Assert.Equal(StartupDatabaseFailure.IdentityChanged, failure.Failure);
            Assert.True(failure.DatabaseReplacementPublished);
            Assert.False(failure.SchemaMutationAttempted);
            Assert.Equal(competitor?.PhysicalIdentity, failure.PersistedState.PhysicalIdentity);
            Assert.Equal(WindowsFileIdentity.Get(PathOf(target)), failure.PersistedState.PhysicalIdentity);
            Assert.Equal(17, failure.PersistedState.SchemaVersion);
            Assert.Equal(11, await target.ScalarAsync("SELECT Quantity FROM BinMovements"));
            Assert.Equal(await competingSource.StateAsync(), await target.StateAsync());
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task A15_cancellation_after_replacement_ownership_release_reports_published_state(bool load)
    {
        await using var target = await Task20Fixture.CreateAsync();
        await using var source = await Task20Fixture.CreateAsync();
        using var cancelled = new CancellationTokenSource();
        var coordinator = target.Coordinator(p =>
        {
            if (p == StartupDatabasePhase.ReplacementRuntimeTransition) cancelled.Cancel();
            return Task.CompletedTask;
        });
        var failure = await Assert.ThrowsAsync<StartupDatabaseException>(() =>
            load ? coordinator.LoadAsync(PathOf(source), cancelled.Token) : coordinator.FreshAsync(cancelled.Token));
        Assert.Equal(StartupDatabaseFailure.Cancelled, failure.Failure);
        Assert.True(failure.DatabaseReplacementPublished);
        Assert.False(failure.SchemaMutationAttempted);
        Assert.Equal(load ? 17 : 16, failure.PersistedState.SchemaVersion);
        Assert.Equal(WindowsFileIdentity.Get(PathOf(target)), failure.PersistedState.PhysicalIdentity);
        using var released = Gate(target).AcquireUpgrade(PathOf(target));
    }

    [Fact]
    public async Task A14_pre16_progression_and_activation_share_exclusive_physical_ownership()
    {
        await using var f = await Task20Fixture.CreateAsync(schema17: false, enabled: false, projection: false);
        await f.ExecuteAsync("DROP TABLE MovementCorrectionLines; DROP TABLE MovementCorrectionOperations; UPDATE SchemaVersion SET Version=15");
        var identity = WindowsFileIdentity.Get(PathOf(f));
        var visited = new List<StartupDatabasePhase>();
        using var ready = await f.Coordinator(p =>
        {
            visited.Add(p);
            if (p is StartupDatabasePhase.BaselineProgression or StartupDatabasePhase.Preflight or StartupDatabasePhase.BackupVerified)
                Assert.Throws<DatabaseUpgradeUnavailableException>(() => Gate(f).AcquireRuntime(PathOf(f)));
            return Task.CompletedTask;
        }).StartAsync();
        Assert.Equal(StartupDatabaseState.Pre16UpgradeRequired, ready.InitialState);
        Assert.Equal(identity, ready.PhysicalIdentity);
        Assert.True(visited.IndexOf(StartupDatabasePhase.BaselineProgression) < visited.IndexOf(StartupDatabasePhase.Preflight));
        Assert.Equal(17, await f.ScalarAsync("SELECT Version FROM SchemaVersion"));
        Assert.Single(Directory.GetFiles(Backups(f), "*.manifest.json"));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task A15_developer_load_and_fresh_replace_under_ownership_and_restart_classification(bool load)
    {
        await using var f = await Task20Fixture.CreateAsync();
        await f.Database.CreateSingleAsync(Task20Fixture.Today, f.CustomerId, 1, 7);
        await using var source = await Task20Fixture.CreateAsync(schema17: !load, enabled: false, projection: false);
        var oldIdentity = WindowsFileIdentity.Get(PathOf(f));
        var replacementObserved = false;
        var coordinator = f.Coordinator(p =>
        {
            if (p == StartupDatabasePhase.Replacement)
            {
                replacementObserved = true;
                Assert.Throws<DatabaseUpgradeUnavailableException>(() => Gate(f).AcquireRuntime(PathOf(f)));
            }
            return Task.CompletedTask;
        });
        using (var active = await f.Coordinator().StartAsync())
        {
            var denied = await Assert.ThrowsAsync<StartupDatabaseException>(() => load ? coordinator.LoadAsync(PathOf(source)) : coordinator.FreshAsync());
            Assert.Equal(StartupDatabaseFailure.OwnershipUnavailable, denied.Failure);
            Assert.Equal(oldIdentity, WindowsFileIdentity.Get(PathOf(f)));
            Assert.Equal(1, await f.ScalarAsync("SELECT COUNT(*) FROM BinMovements"));
        }
        using var ready = await (load ? coordinator.LoadAsync(PathOf(source)) : coordinator.FreshAsync());
        Assert.True(replacementObserved);
        Assert.NotEqual(oldIdentity, ready.PhysicalIdentity);
        Assert.Equal(WindowsFileIdentity.Get(PathOf(f)), ready.PhysicalIdentity);
        Assert.Equal(StartupDatabaseState.Schema16ActivationRequired, ready.InitialState);
        Assert.Equal(0, await f.ScalarAsync("SELECT COUNT(*) FROM BinMovements"));
        Assert.Single(Directory.GetFiles(Path.GetDirectoryName(PathOf(f)) ?? string.Empty, "*.before-replacement-*.db"));
        Assert.Single(Directory.GetFiles(Backups(f), "*.manifest.json"));
    }

    [Fact]
    public async Task Source_value_change_after_backup_is_rejected_before_first_schema17_mutation()
    {
        await using var f = await Task20Fixture.CreateAsync(schema17: false, enabled: false, projection: false);
        await f.Movements.SaveSingleAsync(f.Single());
        var failure = await Assert.ThrowsAsync<StartupDatabaseException>(() => f.Coordinator(async phase =>
        {
            if (phase == StartupDatabasePhase.BackupVerified)
                await f.ExecuteAsync("UPDATE BinMovements SET Quantity=8");
        }).StartAsync());
        Assert.Equal(StartupDatabaseFailure.SourceChanged, failure.Failure);
        Assert.False(failure.SchemaMutationAttempted);
        Assert.Equal(16, failure.PersistedState.SchemaVersion);
        Assert.Equal(0, await f.ScalarAsync("SELECT COUNT(*) FROM sqlite_master WHERE name LIKE 'LogicalMovement%'"));
        Assert.Equal(8, await f.ScalarAsync("SELECT Quantity FROM BinMovements"));
        await AssertBackupValidAsync(f);
    }

    [Fact]
    public async Task Pending_operation_is_a_real_conflict_and_cancellation_returns_no_readiness()
    {
        await using var f = await Task20Fixture.CreateAsync(schema17: false, enabled: false, projection: false);
        var before = await f.StateAsync();
        await File.WriteAllTextAsync(System.IO.Path.Combine(Root(f), "pending.json"), "pending operation");
        var denied = await Assert.ThrowsAsync<StartupDatabaseException>(() => f.Coordinator().StartAsync());
        Assert.Equal(StartupDatabaseFailure.OwnershipUnavailable, denied.Failure);
        Assert.Equal(DatabaseUpgradeUnavailableReason.PendingDatabaseOperation,
            Assert.IsType<DatabaseUpgradeUnavailableException>(denied.InnerException).Reason);
        Assert.Equal(before, await f.StateAsync());
        using var cancelled = new CancellationTokenSource();
        cancelled.Cancel();
        var failure = await Assert.ThrowsAsync<StartupDatabaseException>(() => f.Coordinator().StartAsync(cancelled.Token));
        Assert.Equal(StartupDatabaseFailure.Cancelled, failure.Failure);
        Assert.False(failure.SchemaMutationAttempted);
        Assert.Equal(before, await f.StateAsync());
    }

    [Theory]
    [InlineData("column")]
    [InlineData("primary-key")]
    [InlineData("foreign-key")]
    [InlineData("composite-foreign-key")]
    [InlineData("membership")]
    [InlineData("filtered-unique")]
    [InlineData("composite-unique")]
    [InlineData("check-or-true")]
    [InlineData("missing-table")]
    [InlineData("base-column")]
    [InlineData("base-type")]
    [InlineData("base-nullability")]
    [InlineData("base-unique")]
    [InlineData("base-foreign-key")]
    [InlineData("quoted-null-check")]
    public async Task Native_readiness_requires_actual_schema_capabilities_even_with_no_rows(string damage)
    {
        await using var f = await Task20Fixture.CreateAsync();
        var sql = damage switch
        {
            "column" => "ALTER TABLE LogicalMovementBatches RENAME COLUMN StatusReasonCode TO MissingReason",
            "primary-key" => "UPDATE sqlite_master SET sql=replace(sql,'CONSTRAINT PK_LogicalMovementBatches PRIMARY KEY AUTOINCREMENT','') WHERE name='LogicalMovementBatches'",
            "foreign-key" => "UPDATE sqlite_master SET sql=replace(sql,'REFERENCES MovementBatches (Id) ON DELETE RESTRICT','REFERENCES Customers (Id) ON DELETE RESTRICT') WHERE name='LogicalMovementBatches'",
            "composite-foreign-key" => "UPDATE sqlite_master SET sql=replace(sql,'REFERENCES LogicalMovementLines (LogicalMovementBatchId, Id)','REFERENCES LogicalMovementLines (Id, LogicalMovementBatchId)') WHERE name='LogicalMovementGenerationLines'",
            "membership" => "UPDATE sqlite_master SET sql=replace(sql,'ON DELETE RESTRICT','ON DELETE SET NULL') WHERE name='BinMovements'",
            "filtered-unique" => "DROP INDEX IX_LogicalMovementBatches_RootMovementBatchId; CREATE UNIQUE INDEX weakened ON LogicalMovementBatches(RootMovementBatchId) WHERE RootMovementBatchId > 99",
            "composite-unique" => "DROP INDEX IX_LogicalMovementLines_Root_Ordinal; CREATE UNIQUE INDEX weakened ON LogicalMovementLines(LogicalMovementBatchId,OriginalDisplayOrdinal,RootMovementId)",
            "check-or-true" => "UPDATE sqlite_master SET sql=replace(sql,'CHECK (LineCount > 0)','CHECK (LineCount > 0 OR 1)') WHERE name='LogicalMovementBatches'",
            "base-column" => "ALTER TABLE BinMovements RENAME COLUMN Quantity TO MissingQuantity",
            "base-type" => "UPDATE sqlite_master SET sql=replace(sql,'\"Quantity\" INTEGER','\"Quantity\" TEXT') WHERE name='BinMovements'",
            "base-nullability" => "UPDATE sqlite_master SET sql=replace(sql,'\"Quantity\" INTEGER NOT NULL','\"Quantity\" INTEGER NULL') WHERE name='BinMovements'",
            "base-unique" => "DROP INDEX IX_Customers_CustomerCode; DROP INDEX IX_Customers_CustomerCode_NoCase",
            "base-foreign-key" => "UPDATE sqlite_master SET sql=replace(sql,'REFERENCES \"Customers\" (\"Id\") ON DELETE RESTRICT','REFERENCES \"Customers\" (\"Id\") ON DELETE CASCADE') WHERE name='BinMovements'",
            "quoted-null-check" => "UPDATE sqlite_master SET sql=replace(sql,'IS NOT NULL','IS NOT \"NULL\"') WHERE name='LogicalMovementGenerationLines'",
            _ => "DROP TABLE LogicalMovementPhysicalOutputs"
        };
        await f.ExecuteAsync("PRAGMA writable_schema=ON; " + sql + "; PRAGMA writable_schema=OFF;");
        var before = await f.StateAsync();
        var failure = await Assert.ThrowsAsync<StartupDatabaseException>(f.StartCoordinatedAsync);
        Assert.Equal(StartupDatabaseFailure.StructuralCapabilityMissing, failure.Failure);
        Assert.Equal(17, failure.PersistedState.SchemaVersion);
        Assert.False(failure.SchemaMutationAttempted);
        Assert.Equal(before, await f.StateAsync());
    }

    [Theory]
    [InlineData(StartupDatabasePhase.RuntimeTransition)]
    [InlineData(StartupDatabasePhase.RuntimeReacquired)]
    [InlineData(StartupDatabasePhase.FinalValidation)]
    public async Task Cancellation_during_runtime_handoff_reports_committed17_and_releases_ownership(StartupDatabasePhase at)
    {
        await using var f = await Task20Fixture.CreateAsync(schema17: false, enabled: false, projection: false);
        using var cancelled = new CancellationTokenSource();
        var failure = await Assert.ThrowsAsync<StartupDatabaseException>(() => f.Coordinator(p =>
        {
            if (p == at) cancelled.Cancel();
            return Task.CompletedTask;
        }).StartAsync(cancelled.Token));
        Assert.Equal(StartupDatabaseFailure.Cancelled, failure.Failure);
        Assert.Equal(17, failure.PersistedState.SchemaVersion);
        Assert.True(failure.SchemaMutationAttempted);
        Assert.Equal(17, await f.ScalarAsync("SELECT Version FROM SchemaVersion"));
        using var released = Gate(f).AcquireUpgrade(PathOf(f));
    }

    [Theory]
    [InlineData("duplicate-version")]
    [InlineData("missing-version-row")]
    [InlineData("text-version")]
    [InlineData("partial17-column")]
    [InlineData("missing16-table")]
    public async Task Partial_schema16_is_rejected_without_creating_or_repairing_objects(string damage)
    {
        await using var f = await Task20Fixture.CreateAsync(schema17: false, enabled: false, projection: false);
        await f.ExecuteAsync(damage switch
        {
            "duplicate-version" => "INSERT INTO SchemaVersion VALUES(2,16,'2026-09-13')",
            "missing-version-row" => "DELETE FROM SchemaVersion",
            "text-version" => "UPDATE SchemaVersion SET Version='bad'",
            "partial17-column" => "ALTER TABLE AuditEvents ADD COLUMN MovementCorrectionOperationId INTEGER",
            _ => "DROP TABLE MovementCorrectionLines"
        });
        var before = await f.StateAsync();
        var failure = await Assert.ThrowsAsync<StartupDatabaseException>(f.StartCoordinatedAsync);
        Assert.Equal(StartupDatabaseFailure.PartialOrCorrupt, failure.Failure);
        Assert.False(failure.SchemaMutationAttempted);
        Assert.Equal(before, await f.StateAsync());
        Assert.False(Directory.Exists(Backups(f)));
    }

    [Fact]
    public async Task A8_final_validation_rejects_health_change_after_runtime_participation()
    {
        await using var f = await Task20Fixture.CreateAsync();
        await f.Database.CreateSingleAsync(Task20Fixture.Today, f.CustomerId, 1, 7);
        var failure = await Assert.ThrowsAsync<StartupDatabaseException>(() => f.Coordinator(async p =>
        {
            if (p == StartupDatabasePhase.FinalValidation)
                await f.ExecuteAsync("UPDATE LogicalMovementBatches SET CurrentGenerationNumber=99");
        }).StartAsync());
        Assert.Equal(StartupDatabaseFailure.CurrentHealthInvalid, failure.Failure);
        Assert.Equal(StartupDatabasePhase.FinalValidation, failure.Phase);
        Assert.Equal(17, failure.PersistedState.SchemaVersion);
        Assert.Equal(99, await f.ScalarAsync("SELECT CurrentGenerationNumber FROM LogicalMovementBatches"));
        using var released = Gate(f).AcquireUpgrade(PathOf(f));
    }

    [Fact]
    public async Task A15_load_native17_reclassifies_native_without_migration_backup()
    {
        await using var target = await Task20Fixture.CreateAsync();
        await using var source = await Task20Fixture.CreateAsync();
        await source.Database.CreateSingleAsync(Task20Fixture.Today, source.CustomerId, 1, 11);
        var oldIdentity = WindowsFileIdentity.Get(PathOf(target));
        var sourceState = await source.StateAsync();
        using var ready = await target.Coordinator().LoadAsync(PathOf(source));
        Assert.Equal(StartupDatabaseState.Schema17ValidationRequired, ready.InitialState);
        Assert.NotEqual(oldIdentity, ready.PhysicalIdentity);
        Assert.NotEqual(WindowsFileIdentity.Get(PathOf(source)), ready.PhysicalIdentity);
        Assert.Equal(sourceState, await source.StateAsync());
        Assert.Equal(sourceState, await target.StateAsync());
        Assert.False(Directory.Exists(Backups(target)));
    }

    [Fact]
    public async Task A15_load_into_absent_path_uses_bootstrap_then_native_classification()
    {
        using var target = new BootstrapSandbox();
        await using var source = await Task20Fixture.CreateAsync();
        await source.Database.CreateSingleAsync(Task20Fixture.Today, source.CustomerId, 1, 11);
        using var ready = await target.Coordinator().LoadAsync(PathOf(source));
        Assert.Equal(StartupDatabaseState.Schema17ValidationRequired, ready.InitialState);
        Assert.Equal(WindowsFileIdentity.Get(target.Path), ready.PhysicalIdentity);
        Assert.NotEqual(WindowsFileIdentity.Get(PathOf(source)), ready.PhysicalIdentity);
        Assert.False(Directory.Exists(target.Backups));
    }

    [Fact]
    public async Task A15_replacement_failure_before_swap_preserves_old_identity_and_evidence()
    {
        await using var f = await Task20Fixture.CreateAsync();
        await f.Database.CreateSingleAsync(Task20Fixture.Today, f.CustomerId, 1, 7);
        var oldIdentity = WindowsFileIdentity.Get(PathOf(f));
        var oldState = await f.StateAsync();
        var original = new InjectedFailure();
        var failure = await Assert.ThrowsAsync<StartupDatabaseException>(() => f.Coordinator(p =>
            p == StartupDatabasePhase.Replacement ? throw original : Task.CompletedTask).FreshAsync());
        Assert.Same(original, failure.InnerException);
        Assert.Equal(StartupDatabasePhase.Replacement, failure.Phase);
        Assert.Equal(oldIdentity, failure.PersistedState.PhysicalIdentity);
        Assert.Equal(oldState, await f.StateAsync());
        Assert.Single(Directory.GetFiles(Path.GetDirectoryName(PathOf(f)) ?? string.Empty, "*.replacement-*.db"));
        using var released = Gate(f).AcquireUpgrade(PathOf(f));
    }

    [Fact]
    public async Task A15_loaded_invalid_native_structure_withholds_readiness_and_preserves_old_file()
    {
        await using var target = await Task20Fixture.CreateAsync();
        await using var source = await Task20Fixture.CreateAsync();
        await source.ExecuteAsync("DROP INDEX IX_LogicalMovementLines_RootMovementId");
        var oldIdentity = WindowsFileIdentity.Get(PathOf(target));
        var failure = await Assert.ThrowsAsync<StartupDatabaseException>(() => target.Coordinator().LoadAsync(PathOf(source)));
        Assert.Equal(StartupDatabaseFailure.StructuralCapabilityMissing, failure.Failure);
        Assert.Equal(17, failure.PersistedState.SchemaVersion);
        Assert.Equal(oldIdentity, failure.PersistedState.PhysicalIdentity);
        Assert.False(failure.DatabaseReplacementPublished);
        Assert.Empty(Directory.GetFiles(Path.GetDirectoryName(PathOf(target)) ?? string.Empty, "*.before-replacement-*.db"));
        Assert.Equal(1, await target.ScalarAsync("SELECT COUNT(*) FROM sqlite_master WHERE name='IX_LogicalMovementLines_RootMovementId'"));
        using var ready = await target.Coordinator().StartAsync();
        Assert.True(ready.IsReadyForActivatedHost);
    }

    [Fact]
    public async Task A15_failure_after_swap_reports_new_selected_identity_without_restoring_old()
    {
        await using var target = await Task20Fixture.CreateAsync();
        await using var source = await Task20Fixture.CreateAsync();
        await source.Database.CreateSingleAsync(Task20Fixture.Today, source.CustomerId, 1, 11);
        var oldIdentity = WindowsFileIdentity.Get(PathOf(target));
        var expected = await source.StateAsync();
        var original = new InjectedFailure();
        var failure = await Assert.ThrowsAsync<StartupDatabaseException>(() => target.Coordinator(p =>
            p == StartupDatabasePhase.ReplacementPublished ? throw original : Task.CompletedTask).LoadAsync(PathOf(source)));
        Assert.Same(original, failure.InnerException);
        Assert.True(failure.DatabaseReplacementPublished);
        Assert.False(failure.SchemaMutationAttempted);
        Assert.Equal(StartupDatabasePhase.ReplacementPublished, failure.Phase);
        Assert.NotEqual(oldIdentity, failure.PersistedState.PhysicalIdentity);
        Assert.Equal(WindowsFileIdentity.Get(PathOf(target)), failure.PersistedState.PhysicalIdentity);
        Assert.Equal(expected, await target.StateAsync());
        var preserved = Assert.Single(Directory.GetFiles(Path.GetDirectoryName(PathOf(target)) ?? string.Empty, "*.before-replacement-*.db"));
        Assert.Equal(oldIdentity, WindowsFileIdentity.Get(preserved));
        using var ready = await target.Coordinator().StartAsync();
        Assert.True(ready.IsReadyForActivatedHost);
    }

    [Fact]
    public async Task Quoted_partial_index_predicate_cannot_impersonate_required_token_sequence()
    {
        await using var f = await Task20Fixture.CreateAsync();
        await f.ExecuteAsync("""
            DROP INDEX IX_LogicalMovementBatches_RootMovementBatchId;
            CREATE UNIQUE INDEX IX_LogicalMovementBatches_RootMovementBatchId
            ON LogicalMovementBatches(RootMovementBatchId) WHERE "RootMovementBatchId IS NOT NULL";
            """);
        var before = await f.StateAsync();
        var failure = await Assert.ThrowsAsync<StartupDatabaseException>(() => f.Coordinator().StartAsync());
        Assert.Equal(StartupDatabaseFailure.StructuralCapabilityMissing, failure.Failure);
        Assert.False(failure.SchemaMutationAttempted);
        Assert.Equal(before, await f.StateAsync());
    }

    [Fact]
    public async Task Future_load_source_is_rejected_without_replacing_active_database()
    {
        await using var target = await Task20Fixture.CreateAsync();
        await using var source = await Task20Fixture.CreateAsync();
        await source.ExecuteAsync("UPDATE SchemaVersion SET Version=18");
        var oldIdentity = WindowsFileIdentity.Get(PathOf(target));
        var before = await target.StateAsync();
        var failure = await Assert.ThrowsAsync<StartupDatabaseException>(() => target.Coordinator().LoadAsync(PathOf(source)));
        Assert.Equal(StartupDatabaseFailure.UnsupportedSchema, failure.Failure);
        Assert.Equal(oldIdentity, failure.PersistedState.PhysicalIdentity);
        Assert.Equal(before, await target.StateAsync());
        Assert.Equal(18, await source.ScalarAsync("SELECT Version FROM SchemaVersion"));
    }

    [Fact]
    public async Task Cancellation_after_publication_retains_committed17_and_no_runtime_session()
    {
        await using var f = await Task20Fixture.CreateAsync(schema17: false, enabled: false, projection: false);
        using var cancelled = new CancellationTokenSource();
        var failure = await Assert.ThrowsAsync<StartupDatabaseException>(() => f.Coordinator(p =>
        {
            if (p == StartupDatabasePhase.Schema17Published) cancelled.Cancel();
            return Task.CompletedTask;
        }).StartAsync(cancelled.Token));
        Assert.Equal(StartupDatabaseFailure.Cancelled, failure.Failure);
        Assert.Equal(17, failure.PersistedState.SchemaVersion);
        Assert.Equal(17, await f.ScalarAsync("SELECT Version FROM SchemaVersion"));
        using var ready = await f.Coordinator().StartAsync();
        Assert.True(ready.IsReadyForActivatedHost);
    }

    [Fact]
    public async Task Interrupted_bootstrap_is_classified_partial_instead_of_silently_recreated()
    {
        using var sandbox = new BootstrapSandbox();
        var original = new InjectedFailure();
        var failed = await Assert.ThrowsAsync<StartupDatabaseException>(() => sandbox.Coordinator(p =>
            p == StartupDatabasePhase.PhysicalOwnershipAcquired ? throw original : Task.CompletedTask).StartAsync());
        Assert.Same(original, failed.InnerException);
        var identity = WindowsFileIdentity.Get(sandbox.Path);
        Assert.Equal(0, new FileInfo(sandbox.Path).Length);
        var retry = await Assert.ThrowsAsync<StartupDatabaseException>(() => sandbox.Coordinator().StartAsync());
        Assert.Equal(StartupDatabaseFailure.PartialOrCorrupt, retry.Failure);
        Assert.Equal(identity, retry.PersistedState.PhysicalIdentity);
        Assert.Equal(0, new FileInfo(sandbox.Path).Length);
        Assert.False(retry.SchemaMutationAttempted);
    }

    private static string PathOf(Task20Fixture f) => new SqliteConnectionStringBuilder(f.Database.ConnectionString).DataSource;
    private static string Root(Task20Fixture f) => Directory.GetParent(Path.GetDirectoryName(PathOf(f)) ?? throw new InvalidOperationException())?.FullName
        ?? throw new InvalidOperationException();
    private static string Backups(Task20Fixture f) => System.IO.Path.Combine(Root(f), "startup-backups");
    private static WindowsFileDatabaseUpgradeGate Gate(Task20Fixture f) => new(System.IO.Path.Combine(Root(f), "startup-locks"),
        new PendingDatabaseOperationConflictProbe(System.IO.Path.Combine(Root(f), "pending.json")));

    private static async Task AssertBackupValidAsync(Task20Fixture f)
    {
        var manifest = Assert.Single(Directory.GetFiles(Backups(f), "*.manifest.json"));
        var result = await new SqliteLineageMigrationBackupService(Gate(f), new SqliteLineageMigrationPreflight())
            .VerifyForSourceAsync(manifest, PathOf(f));
        Assert.True(result.IsValidForExpectedSource, result.FailureCode);
        Assert.Equal(WindowsFileIdentity.Get(PathOf(f)), result.Manifest?.SourceFileIdentity);
    }

    private sealed class InjectedFailure : Exception;
    private sealed class LifecycleFailure(Action<LineageSchema17MigrationCheckpoint> action) : ILineageSchema17FailureInjector
    {
        public void ThrowIfRequested(LineageSchema17MigrationCheckpoint checkpoint) => action(checkpoint);
    }

    [Fact]
    public async Task Source_equivalence_and_first_mutation_share_migrator_write_ownership()
    {
        await using var f = await Task20Fixture.CreateAsync(schema17: false, enabled: false, projection: false);
        await f.Movements.SaveSingleAsync(f.Single());
        var checkedOwnership = false;
        var injector = new LifecycleFailure(p =>
        {
            if (p != LineageSchema17MigrationCheckpoint.BeforeSchemaMutation) return;
            using var c = new SqliteConnection(f.Database.ConnectionString);
            c.Open();
            using var cmd = c.CreateCommand();
            cmd.CommandTimeout = 1;
            cmd.CommandText = "UPDATE BinMovements SET Quantity=8";
            var blocked = Assert.Throws<SqliteException>(() => cmd.ExecuteNonQuery());
            Assert.Equal(5, blocked.SqliteErrorCode);
            checkedOwnership = true;
        });
        using var ready = await f.Coordinator(migrationFailures: injector).StartAsync();
        Assert.True(checkedOwnership);
        Assert.Equal(7, await f.ScalarAsync("SELECT Quantity FROM BinMovements"));
        Assert.Equal(17, await f.ScalarAsync("SELECT Version FROM SchemaVersion"));
    }

    [Theory]
    [InlineData(LineageSchema17MigrationCheckpoint.BeforeSchemaMutation)]
    [InlineData(LineageSchema17MigrationCheckpoint.AfterFirstSchemaChange)]
    public async Task Cancellation_at_migration_boundaries_preserves_exact16(LineageSchema17MigrationCheckpoint at)
    {
        await using var f = await Task20Fixture.CreateAsync(schema17: false, enabled: false, projection: false);
        var before = await f.StateAsync();
        using var cancelled = new CancellationTokenSource();
        var injector = new LifecycleFailure(p => { if (p == at) cancelled.Cancel(); });
        var failure = await Assert.ThrowsAsync<StartupDatabaseException>(() =>
            f.Coordinator(migrationFailures: injector).StartAsync(cancelled.Token));
        Assert.Equal(StartupDatabaseFailure.Cancelled, failure.Failure);
        Assert.Equal(at != LineageSchema17MigrationCheckpoint.BeforeSchemaMutation, failure.SchemaMutationAttempted);
        Assert.Equal(16, failure.PersistedState.SchemaVersion);
        Assert.Equal(before, await f.StateAsync());
        await AssertBackupValidAsync(f);
        using var released = Gate(f).AcquireUpgrade(PathOf(f));
    }

    [Fact]
    public async Task Invalid_schema16_load_cannot_replace_valid_target()
    {
        await using var target = await Task20Fixture.CreateAsync();
        await using var source = await Task20Fixture.CreateAsync(schema17: false, enabled: false, projection: false);
        await source.ExecuteAsync("ALTER TABLE BinMovements RENAME COLUMN Quantity TO MissingQuantity");
        var before = await target.StateAsync();
        var identity = WindowsFileIdentity.Get(PathOf(target));
        var failure = await Assert.ThrowsAsync<StartupDatabaseException>(() => target.Coordinator().LoadAsync(PathOf(source)));
        Assert.Equal(StartupDatabaseFailure.StructuralCapabilityMissing, failure.Failure);
        Assert.False(failure.DatabaseReplacementPublished);
        Assert.Equal(identity, failure.PersistedState.PhysicalIdentity);
        Assert.Equal(before, await target.StateAsync());
    }

    [Fact]
    public async Task Unexpected_health_phase_exception_retains_infrastructure_classification()
    {
        await using var f = await Task20Fixture.CreateAsync();
        var before = await f.StateAsync();
        var original = new InvalidOperationException("unexpected implementation failure");
        var failure = await Assert.ThrowsAsync<StartupDatabaseException>(() => f.Coordinator(p =>
            p == StartupDatabasePhase.NativeHealth ? throw original : Task.CompletedTask).StartAsync());
        Assert.Equal(StartupDatabaseFailure.InfrastructureFailure, failure.Failure);
        Assert.Same(original, failure.InnerException);
        Assert.Equal(before, await f.StateAsync());
    }
    private sealed class MigrationFailure(LineageSchema17MigrationCheckpoint requested, Action verifyBackup) : ILineageSchema17FailureInjector
    {
        internal InjectedFailure Error { get; } = new();
        internal bool SawFirstMutationBoundary { get; private set; }
        public void ThrowIfRequested(LineageSchema17MigrationCheckpoint checkpoint)
        {
            if (checkpoint == LineageSchema17MigrationCheckpoint.BeforeSchemaMutation)
            {
                verifyBackup();
                SawFirstMutationBoundary = true;
            }
            if (checkpoint == requested) throw Error;
        }
    }

    private sealed class BootstrapSandbox : IDisposable
    {
        private readonly string root = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "BinTracker-20D-" + Guid.NewGuid().ToString("N"));
        internal string Path => System.IO.Path.Combine(root, "active", "fresh.db");
        internal string Backups => System.IO.Path.Combine(root, "backups");
        internal SqliteStartupDatabaseCoordinator Coordinator(Func<StartupDatabasePhase, Task>? hook = null) =>
            new(Path, Backups, System.IO.Path.Combine(root, "locks"), System.IO.Path.Combine(root, "pending.json"), hook, null);
        public void Dispose() { if (Directory.Exists(root)) Directory.Delete(root, recursive: true); }
    }
}
