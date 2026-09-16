using System.Text.Json;
using BinTracker.Core;
using BinTracker.Data;
using BinTracker.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BinTracker.IntegrationTests;

public sealed class Task20RuntimeActivationTests
{
    [Fact]
    public async Task Normal_catalogue_and_composition_activate_schema17_native_authorities_atomically()
    {
        await using var f = await RuntimeFixture.CreateAsync();

        Assert.Equal(17, DatabaseSetup.LatestSchemaVersion);
        Assert.Equal(16, DatabaseSetup.LatestSchema16CompatibilityVersion);
        Assert.Equal(17, SqliteSchemaMigrations.All[^1].Version);
        Assert.Equal(16, SqliteSchemaMigrations.Schema16Baseline[^1].Version);

        using var session = await DatabaseSetup.InitializeAsync(f.Services);
        Assert.True(session.IsReadyForActivatedHost);
        Assert.Equal(17, await f.SchemaVersionAsync());

        await using var scope = f.Services.CreateAsyncScope();
        Assert.IsType<SqliteInitialMovementLineageWriter>(
            scope.ServiceProvider.GetRequiredService<IInitialMovementLineageWriter>());
        Assert.IsType<SqliteSingleMovementResponseReceiptStore>(
            scope.ServiceProvider.GetRequiredService<ISingleMovementResponseReceiptStore>());
        Assert.IsType<SqliteMovementMutationWriter>(
            scope.ServiceProvider.GetRequiredService<IMovementMutationWriter>());
        var projection = scope.ServiceProvider
            .GetRequiredService<IOperationalMovementProjectionAuthority>();
        Assert.IsType<SqliteOperationalMovementProjectionAuthority>(projection);
        Assert.Same(projection, scope.ServiceProvider
            .GetRequiredService<ITransactionalOperationalMovementProjectionAuthority>());
    }

    [Fact]
    public async Task Normal_schema16_startup_migrates_then_holds_runtime_lease_until_session_disposal()
    {
        await using var f = await RuntimeFixture.CreateSchema16Async();
        var coordinator = f.Services.GetRequiredService<IStartupDatabaseCoordinator>();

        var session = await coordinator.StartAsync();
        Assert.Equal(StartupDatabaseState.Schema16ActivationRequired, session.InitialState);
        Assert.Equal(17, await f.SchemaVersionAsync());
        Assert.Throws<DatabaseUpgradeUnavailableException>(() =>
            new WindowsFileDatabaseUpgradeGate(f.LockDirectory,
                    new PendingDatabaseOperationConflictProbe(f.MarkerPath))
                .AcquireUpgrade(f.DatabasePath));

        session.Dispose();
        using var upgrade = new WindowsFileDatabaseUpgradeGate(f.LockDirectory,
                new PendingDatabaseOperationConflictProbe(f.MarkerPath))
            .AcquireUpgrade(f.DatabasePath);
    }

    [Fact]
    public async Task Normal_single_batch_mutation_and_numeric_consumers_use_native_authority()
    {
        await using var f = await RuntimeFixture.CreateAsync();
        using var session = await DatabaseSetup.InitializeAsync(f.Services);
        var (customerId, containerTypeId) = await f.SeedOperationalDataAsync();

        await using var scope = f.Services.CreateAsyncScope();
        var movements = scope.ServiceProvider.GetRequiredService<IMovementService>();
        var singleRequest = new SaveSingleMovementRequest(
            Guid.NewGuid(), f.Today, MovementType.Out, customerId, containerTypeId, 7,
            "normal-single", null);
        var first = await movements.SaveSingleAsync(singleRequest);
        await movements.SaveSingleAsync(new(
            Guid.NewGuid(), f.Today, MovementType.Out, customerId, containerTypeId, 2,
            "later", null));
        Assert.Equal(first, await movements.SaveSingleAsync(singleRequest));

        var batch = await movements.SaveBatchAsync(new(
            Guid.NewGuid(), f.Today, MovementType.Out, "normal-batch",
            [
                new(customerId, containerTypeId, 3, "batch-a", null),
                new(customerId, containerTypeId, 4, "batch-b", null)
            ]));

        Assert.Equal(3, await f.ScalarAsync("SELECT COUNT(*) FROM LogicalMovementBatches"));
        Assert.Equal(4, await f.ScalarAsync("SELECT COUNT(*) FROM LogicalMovementLines"));
        Assert.Equal(4, await f.ScalarAsync("SELECT COUNT(*) FROM LogicalMovementGenerationLines"));
        Assert.Equal(1, await f.ScalarAsync(
            $"SELECT COUNT(*) FROM LogicalMovementBatches WHERE RootMovementBatchId={batch.BatchId} AND LineCount=2 AND CurrentGenerationNumber=0"));
        Assert.Equal(1, await f.ScalarAsync(
            $"SELECT COUNT(*) FROM SingleMovementResponseReceipts WHERE MovementId={first.MovementId} AND ResultingPosition=7"));

        var corrections = scope.ServiceProvider.GetRequiredService<IMovementCorrectionService>();
        var legacy = await Assert.ThrowsAsync<LogicalMovementMutationException>(() =>
            corrections.ReverseAsync(new(Guid.NewGuid(), first.MovementId, "legacy route blocked")));
        Assert.Equal(LogicalMovementMutationFailure.LegacyRouteUnavailable, legacy.Failure);

        var preview = Assert.IsType<LogicalMovementMutationPreview>(
            await corrections.PreviewLogicalForMovementAsync(first.MovementId));
        var line = Assert.Single(preview.Lines);
        var corrected = await corrections.ExecuteLogicalAsync(new(
            Guid.NewGuid(), preview.LogicalMovementBatchId, preview.ExpectedGeneration,
            MovementMutationRequest.Correct(MovementMutationScope.Individual,
                [line.LogicalMovementLineId], "normal native correction",
                quantity: MovementFieldIntent<int>.Selected(9))));
        Assert.Equal(LogicalMovementMutationResultKind.Committed, corrected.Kind);

        var reversalPreview = Assert.IsType<LogicalMovementMutationPreview>(
            await corrections.PreviewLogicalForMovementAsync(first.MovementId));
        var reversalLine = Assert.Single(reversalPreview.Lines);
        var reversed = await corrections.ExecuteLogicalAsync(new(
            Guid.NewGuid(), reversalPreview.LogicalMovementBatchId,
            reversalPreview.ExpectedGeneration,
            MovementMutationRequest.Reverse(MovementMutationScope.Individual,
                [reversalLine.LogicalMovementLineId], "normal native reversal")));
        Assert.Equal(LogicalMovementMutationResultKind.Committed, reversed.Kind);
        Assert.Equal(corrected.ResultGeneration.Value + 1, reversed.ResultGeneration.Value);

        var balances = await scope.ServiceProvider.GetRequiredService<IBalanceService>()
            .GetBalancesAsync();
        Assert.Equal(9, Assert.Single(balances).Balance);
        var outstanding = await scope.ServiceProvider.GetRequiredService<IOutstandingReportService>()
            .QueryAsync(new(f.Today));
        Assert.Equal(9, Assert.Single(outstanding.Rows).Balance);
        var daily = await scope.ServiceProvider.GetRequiredService<IDailyMovementsReportService>()
            .QueryAsync(new(f.Today));
        Assert.Equal(9, daily.OutQuantity - daily.InQuantity);
        var customer = Assert.Single(await scope.ServiceProvider.GetRequiredService<ICustomerService>()
            .SearchAsync("NORMAL", includeInactive: false));
        Assert.Equal(9, customer.NetBalance);
        Assert.Equal(9, (await movements.GetDashboardSummaryAsync(f.Today)).Outstanding);
    }

    [Fact]
    public async Task Normal_numeric_failure_has_no_raw_fallback()
    {
        await using var f = await RuntimeFixture.CreateAsync();
        using var session = await DatabaseSetup.InitializeAsync(f.Services);
        var (customerId, containerTypeId) = await f.SeedOperationalDataAsync();
        await f.AddUnrootedMovementAsync(customerId, containerTypeId);

        await using var scope = f.Services.CreateAsyncScope();
        var error = await Assert.ThrowsAsync<OperationalMovementProjectionException>(() =>
            scope.ServiceProvider.GetRequiredService<IBalanceService>().GetBalancesAsync());
        Assert.Equal(OperationalMovementProjectionFailure.UnexpectedUnrootedOrdinary, error.Failure);
        await Assert.ThrowsAsync<OperationalMovementProjectionException>(() =>
            scope.ServiceProvider.GetRequiredService<IOutstandingReportService>()
                .QueryAsync(new(f.Today)));
        await Assert.ThrowsAsync<OperationalMovementProjectionException>(() =>
            scope.ServiceProvider.GetRequiredService<IDailyMovementsReportService>()
                .QueryAsync(new(f.Today)));
        await Assert.ThrowsAsync<OperationalMovementProjectionException>(() =>
            scope.ServiceProvider.GetRequiredService<ICustomerService>()
                .SearchAsync("NORMAL", includeInactive: false));
    }

    [Fact]
    public async Task Existing_schema17_revalidates_without_replaying_migration_or_backup()
    {
        await using var f = await RuntimeFixture.CreateAsync();
        using (var first = await DatabaseSetup.InitializeAsync(f.Services))
            Assert.Equal(StartupDatabaseState.Absent, first.InitialState);

        var backupFiles = Directory.GetFiles(
            f.BackupDirectory, "*", SearchOption.AllDirectories).Order().ToArray();
        var roots = await f.ScalarAsync("SELECT COUNT(*) FROM LogicalMovementBatches");

        using var second = await DatabaseSetup.InitializeAsync(f.Services);

        Assert.Equal(StartupDatabaseState.Schema17ValidationRequired, second.InitialState);
        Assert.Equal(17, await f.SchemaVersionAsync());
        Assert.Equal(roots, await f.ScalarAsync("SELECT COUNT(*) FROM LogicalMovementBatches"));
        Assert.Equal(backupFiles, Directory.GetFiles(
            f.BackupDirectory, "*", SearchOption.AllDirectories).Order().ToArray());
    }

    [Fact]
    public async Task Future_schema_is_rejected_without_mutation_or_backup()
    {
        await using var f = await RuntimeFixture.CreateSchema16Async();
        await f.SetSchemaVersionAsync(18);

        var error = await Assert.ThrowsAsync<StartupDatabaseException>(() =>
            DatabaseSetup.InitializeAsync(f.Services));

        Assert.Equal(StartupDatabaseFailure.UnsupportedSchema, error.Failure);
        Assert.Equal(StartupDatabaseState.UnsupportedFutureSchema, error.PersistedState.State);
        Assert.False(error.SchemaMutationAttempted);
        Assert.Equal(18, await f.SchemaVersionAsync());
        Assert.False(Directory.Exists(f.BackupDirectory));
    }

    [Theory]
    [InlineData(PendingDatabaseOperationType.Load)]
    [InlineData(PendingDatabaseOperationType.Fresh)]
    public async Task Staged_developer_operation_is_claimed_and_revalidated_by_normal_coordinator(
        PendingDatabaseOperationType operationType)
    {
        await using var f = await RuntimeFixture.CreateAsync();
        using (await DatabaseSetup.InitializeAsync(f.Services))
            await f.SeedOperationalDataAsync();

        string? staged = null;
        if (operationType == PendingDatabaseOperationType.Load)
        {
            var source = Path.Combine(f.Root, "source", "source.db");
            Directory.CreateDirectory(Path.GetDirectoryName(source)!);
            await RuntimeFixture.CreateSchema16DatabaseAsync(source, "LOADED");
            staged = Path.Combine(Path.GetDirectoryName(f.MarkerPath)!, "pending-database-load.db");
            Directory.CreateDirectory(Path.GetDirectoryName(staged)!);
            File.Copy(source, staged);
        }
        Directory.CreateDirectory(Path.GetDirectoryName(f.MarkerPath)!);
        File.WriteAllText(f.MarkerPath, JsonSerializer.Serialize(new PendingDatabaseOperation(
            operationType, f.DatabasePath, staged, null, DateTime.UtcNow)));

        using var session = await f.Services.GetRequiredService<IStartupDatabaseCoordinator>().StartAsync();

        Assert.Equal(17, await f.SchemaVersionAsync());
        Assert.False(File.Exists(f.MarkerPath));
        if (staged is not null) Assert.False(File.Exists(staged));
        Assert.Equal(operationType == PendingDatabaseOperationType.Load ? 1 : 0,
            await f.ScalarAsync("SELECT COUNT(*) FROM Customers WHERE CustomerCode='LOADED'"));
        Assert.Equal(0, await f.ScalarAsync("SELECT COUNT(*) FROM Customers WHERE CustomerCode='NORMAL'"));
    }

    [Fact]
    public async Task Load_cleanup_failure_after_readiness_preserves_publication_truth_and_does_not_replay()
    {
        await using var f = await RuntimeFixture.CreateAsync();
        using (await DatabaseSetup.InitializeAsync(f.Services))
            await f.SeedOperationalDataAsync();
        var staged = await WritePendingOperationAsync(f, PendingDatabaseOperationType.Load);

        var failure = await Assert.ThrowsAsync<StartupDatabaseException>(() => f.Coordinator(step =>
        {
            if (step == PendingDatabaseOperationCleanupStep.StagedDatabase)
                throw new IOException("Injected staged cleanup failure.");
        }).StartAsync());

        Assert.True(failure.DatabaseReplacementPublished);
        Assert.Equal(StartupDatabasePhase.PendingOperationCleanup, failure.Phase);
        Assert.Equal(17, await f.SchemaVersionAsync());
        var publishedIdentity = WindowsFileIdentity.Get(f.DatabasePath);
        var marker = ReadPendingOperation(f.MarkerPath);
        Assert.Equal(PendingDatabaseOperationState.PublicationReadyForCleanup, marker.State);
        Assert.Equal(publishedIdentity, marker.PublishedDatabaseIdentity);
        using var retry = await f.Coordinator().StartAsync();
        Assert.Equal(publishedIdentity, WindowsFileIdentity.Get(f.DatabasePath));
        Assert.False(File.Exists(f.MarkerPath));
        Assert.False(File.Exists(staged));
        Assert.Equal(1, await f.ScalarAsync("SELECT COUNT(*) FROM Customers WHERE CustomerCode='LOADED'"));
    }

    [Fact]
    public async Task Fresh_cleanup_failure_after_readiness_preserves_publication_truth_and_does_not_replay()
    {
        await using var f = await RuntimeFixture.CreateAsync();
        using (await DatabaseSetup.InitializeAsync(f.Services))
            await f.SeedOperationalDataAsync();
        await WritePendingOperationAsync(f, PendingDatabaseOperationType.Fresh);

        var failure = await Assert.ThrowsAsync<StartupDatabaseException>(() => f.Coordinator(step =>
        {
            if (step == PendingDatabaseOperationCleanupStep.Marker)
                throw new IOException("Injected marker cleanup failure.");
        }).StartAsync());

        Assert.True(failure.DatabaseReplacementPublished);
        Assert.Equal(StartupDatabasePhase.PendingOperationCleanup, failure.Phase);
        Assert.Equal(17, await f.SchemaVersionAsync());
        var publishedIdentity = WindowsFileIdentity.Get(f.DatabasePath);
        var marker = ReadPendingOperation(f.MarkerPath);
        Assert.Equal(PendingDatabaseOperationState.PublicationReadyForCleanup, marker.State);
        Assert.Equal(publishedIdentity, marker.PublishedDatabaseIdentity);
        using var retry = await f.Coordinator().StartAsync();
        Assert.Equal(publishedIdentity, WindowsFileIdentity.Get(f.DatabasePath));
        Assert.False(File.Exists(f.MarkerPath));
        Assert.Equal(0, await f.ScalarAsync("SELECT COUNT(*) FROM Customers WHERE CustomerCode='NORMAL'"));
    }

    [Fact]
    public async Task Load_partial_cleanup_failure_reenters_completion_without_requiring_removed_stage()
    {
        await using var f = await RuntimeFixture.CreateAsync();
        using (await DatabaseSetup.InitializeAsync(f.Services))
            await f.SeedOperationalDataAsync();
        var staged = await WritePendingOperationAsync(f, PendingDatabaseOperationType.Load);

        var failure = await Assert.ThrowsAsync<StartupDatabaseException>(() => f.Coordinator(step =>
        {
            if (step == PendingDatabaseOperationCleanupStep.Marker)
                throw new IOException("Injected marker cleanup failure.");
        }).StartAsync());

        Assert.True(failure.DatabaseReplacementPublished);
        Assert.Equal(StartupDatabasePhase.PendingOperationCleanup, failure.Phase);
        Assert.False(File.Exists(staged));
        Assert.Equal(PendingDatabaseOperationState.PublicationReadyForCleanup,
            ReadPendingOperation(f.MarkerPath).State);
        using var retry = await f.Coordinator().StartAsync();
        Assert.True(retry.IsReadyForActivatedHost);
        Assert.False(File.Exists(f.MarkerPath));
        Assert.Equal(1, await f.ScalarAsync("SELECT COUNT(*) FROM Customers WHERE CustomerCode='LOADED'"));
    }

    [Theory]
    [InlineData(PendingDatabaseOperationType.Load)]
    [InlineData(PendingDatabaseOperationType.Fresh)]
    public async Task Publication_ready_marker_persistence_failure_preserves_publication_truth_and_requires_recovery(
        PendingDatabaseOperationType operationType)
    {
        await using var f = await RuntimeFixture.CreateAsync();
        using (await DatabaseSetup.InitializeAsync(f.Services))
            await f.SeedOperationalDataAsync();
        await WritePendingOperationAsync(f, operationType);
        var publicationReadyPersistAttempted = false;

        var failure = await Assert.ThrowsAsync<StartupDatabaseException>(() => f.Coordinator(
            pendingStatePersist: state =>
            {
                if (state != PendingDatabaseOperationState.PublicationReadyForCleanup) return;
                publicationReadyPersistAttempted = true;
                throw new IOException("Injected publication-ready marker persistence failure.");
            }).StartAsync());

        Assert.True(publicationReadyPersistAttempted);
        Assert.True(failure.DatabaseReplacementPublished);
        Assert.Equal(17, await f.SchemaVersionAsync());
        var publishedIdentity = WindowsFileIdentity.Get(f.DatabasePath);
        Assert.Equal(PendingDatabaseOperationState.ExecutionInProgress,
            ReadPendingOperation(f.MarkerPath).State);

        var retry = await Assert.ThrowsAsync<StartupDatabaseException>(() => f.Coordinator().StartAsync());
        Assert.Equal(StartupDatabaseFailure.PendingOperationRecoveryRequired, retry.Failure);
        Assert.Equal(publishedIdentity, WindowsFileIdentity.Get(f.DatabasePath));
    }

    private static async Task<string?> WritePendingOperationAsync(
        RuntimeFixture f, PendingDatabaseOperationType operationType)
    {
        string? staged = null;
        if (operationType == PendingDatabaseOperationType.Load)
        {
            var source = Path.Combine(f.Root, "source", "source.db");
            Directory.CreateDirectory(Path.GetDirectoryName(source)!);
            await RuntimeFixture.CreateSchema16DatabaseAsync(source, "LOADED");
            staged = Path.Combine(Path.GetDirectoryName(f.MarkerPath)!, "pending-database-load.db");
            Directory.CreateDirectory(Path.GetDirectoryName(staged)!);
            File.Copy(source, staged);
        }
        Directory.CreateDirectory(Path.GetDirectoryName(f.MarkerPath)!);
        File.WriteAllText(f.MarkerPath, JsonSerializer.Serialize(new PendingDatabaseOperation(
            operationType, f.DatabasePath, staged, null, DateTime.UtcNow)));
        return staged;
    }

    private static PendingDatabaseOperation ReadPendingOperation(string markerPath) =>
        JsonSerializer.Deserialize<PendingDatabaseOperation>(File.ReadAllText(markerPath))
        ?? throw new InvalidOperationException("The pending-operation marker is empty.");

    private sealed class RuntimeFixture : IAsyncDisposable
    {
        private RuntimeFixture(string root, ServiceProvider services, DateOnly today)
        {
            Root = root;
            DatabasePath = Path.Combine(root, "active", "BinTracker.db");
            BackupDirectory = Path.Combine(root, "backups");
            LockDirectory = Path.Combine(root, "locks");
            MarkerPath = Path.Combine(root, "control", "pending-database-operation.json");
            Services = services;
            Today = today;
        }

        internal string Root { get; }
        internal string DatabasePath { get; }
        internal string BackupDirectory { get; }
        internal string LockDirectory { get; }
        internal string MarkerPath { get; }
        internal ServiceProvider Services { get; }
        internal DateOnly Today { get; }

        internal static Task<RuntimeFixture> CreateAsync() => CreateAsync(schema16: false);
        internal static Task<RuntimeFixture> CreateSchema16Async() => CreateAsync(schema16: true);

        private static async Task<RuntimeFixture> CreateAsync(bool schema16)
        {
            var root = Path.Combine(Path.GetTempPath(), $"BinTracker-20P2-{Guid.NewGuid():N}");
            var databasePath = Path.Combine(root, "active", "BinTracker.db");
            var backupDirectory = Path.Combine(root, "backups");
            var lockDirectory = Path.Combine(root, "locks");
            var markerPath = Path.Combine(root, "control", "pending-database-operation.json");
            Directory.CreateDirectory(Path.GetDirectoryName(databasePath)!);
            if (schema16) await CreateSchema16DatabaseAsync(databasePath);

            var today = new DateOnly(2026, 9, 16);
            var services = new ServiceCollection();
            services.AddBinTrackerData(new DatabaseSettings
            {
                Provider = DatabaseProvider.Sqlite,
                ConnectionString = $"Data Source={databasePath};Cache=Shared;Pooling=False"
            }, backupDirectory, lockDirectory, markerPath);
            services.AddSingleton<IBusinessClock>(new Clock(today));
            services.AddSingleton<IUserContext>(new User());
            services.AddSingleton<IClientContext>(new Client());
            services.AddBinTrackerBusinessServices();
            return new(root, services.BuildServiceProvider(), today);
        }

        internal static async Task CreateSchema16DatabaseAsync(string path, string? customerCode = null)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            await using var db = new BinTrackerDbContext(new DbContextOptionsBuilder<BinTrackerDbContext>()
                .UseSqlite($"Data Source={path};Pooling=False").Options);
            await DatabaseSetup.InitializeSchema16CompatibilityAsync(db);
            if (customerCode is not null)
            {
                db.Customers.Add(new Customer { CustomerCode = customerCode, Name = customerCode });
                await db.SaveChangesAsync();
            }
        }

        internal async Task<(int CustomerId, int ContainerTypeId)> SeedOperationalDataAsync()
        {
            await using var db = await Services.GetRequiredService<IDbContextFactory<BinTrackerDbContext>>()
                .CreateDbContextAsync();
            var customer = new Customer { CustomerCode = "NORMAL", Name = "Normal Customer" };
            db.Customers.Add(customer);
            await db.SaveChangesAsync();
            var container = await db.ContainerTypes.Where(x => x.IsActive).OrderBy(x => x.Id).FirstAsync();
            return (customer.Id, container.Id);
        }

        internal async Task AddUnrootedMovementAsync(int customerId, int containerTypeId)
        {
            await using var db = await Services.GetRequiredService<IDbContextFactory<BinTrackerDbContext>>()
                .CreateDbContextAsync();
            db.BinMovements.Add(new BinMovement
            {
                ClientOperationId = Guid.NewGuid(),
                MovementDate = Today,
                MovementType = MovementType.Out,
                Source = MovementSource.Manual,
                CustomerId = customerId,
                ContainerTypeId = containerTypeId,
                Quantity = 99,
                CreatedBy = "corruption-test",
                CreatedUtc = DateTime.UtcNow
            });
            await db.SaveChangesAsync();
        }

        internal async Task<int> SchemaVersionAsync() =>
            await ScalarAsync("SELECT Version FROM SchemaVersion WHERE Id=1");

        internal SqliteStartupDatabaseCoordinator Coordinator(
            Action<PendingDatabaseOperationCleanupStep>? pendingCleanup = null,
            Action<PendingDatabaseOperationState>? pendingStatePersist = null) =>
            new(DatabasePath, BackupDirectory, LockDirectory, MarkerPath, null, null, null, null,
                pendingCleanup, pendingStatePersist);

        internal async Task SetSchemaVersionAsync(int version)
        {
            await using var db = await Services.GetRequiredService<IDbContextFactory<BinTrackerDbContext>>()
                .CreateDbContextAsync();
            await db.Database.ExecuteSqlRawAsync(
                "UPDATE SchemaVersion SET Version={0} WHERE Id=1", version);
        }

        internal async Task<int> ScalarAsync(string sql)
        {
            await using var db = await Services.GetRequiredService<IDbContextFactory<BinTrackerDbContext>>()
                .CreateDbContextAsync();
            await db.Database.OpenConnectionAsync();
            await using var command = db.Database.GetDbConnection().CreateCommand();
            command.CommandText = sql;
            return Convert.ToInt32(await command.ExecuteScalarAsync());
        }

        public async ValueTask DisposeAsync()
        {
            await Services.DisposeAsync();
            if (Directory.Exists(Root)) Directory.Delete(Root, recursive: true);
        }

        private sealed class Clock(DateOnly today) : IBusinessClock
        {
            public DateTime UtcNow => today.ToDateTime(new TimeOnly(1, 2), DateTimeKind.Utc);
            public DateTime LocalNow => UtcNow;
            public DateOnly Today => today;
            public string TimeZoneId => "UTC";
        }

        private sealed class User : IUserContext
        {
            public string SessionId => "task20-p2-session";
            public int? UserId => 1;
            public string Username => "task20-p2-operator";
            public string DisplayName => Username;
            public UserRole Role => UserRole.Operator;
            public bool MustChangePassword => false;
            public bool IsAuthenticated => true;
        }

        private sealed class Client : IClientContext
        {
            public string ClientInstanceId => "task20-p2-client";
            public string DeviceName => "task20-p2-device";
        }
    }
}
