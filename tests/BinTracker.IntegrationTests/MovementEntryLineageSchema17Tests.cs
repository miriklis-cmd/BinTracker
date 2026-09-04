using BinTracker.Core;
using BinTracker.Data;
using BinTracker.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BinTracker.IntegrationTests;

public sealed class MovementEntryLineageSchema17Tests
{
    [Fact]
    public async Task Native_single_creates_exact_generation_zero_resolves_and_is_already_complete()
    {
        await using var harness = await Harness.CreateAsync();
        var request = SingleRequest(harness);
        var result = await harness.Movements.SaveSingleAsync(request);

        await using var connection = await harness.OpenAsync();
        var rootId = await ScalarAsync(connection,
            "SELECT Id FROM LogicalMovementBatches WHERE RootMovementBatchId IS NULL;");
        Assert.Equal(1, await ScalarAsync(connection, "SELECT COUNT(*) FROM LogicalMovementBatches;"));
        Assert.Equal(1, await ScalarAsync(connection,
            $"SELECT COUNT(*) FROM LogicalMovementBatches WHERE Id={rootId} AND Status=1 AND CurrentGenerationNumber=0 AND LineCount=1;"));
        Assert.Equal(1, await ScalarAsync(connection,
            $"SELECT COUNT(*) FROM LogicalMovementLines WHERE LogicalMovementBatchId={rootId} AND RootMovementId={result.MovementId} AND OriginalDisplayOrdinal=0;"));
        Assert.Equal(1, await ScalarAsync(connection,
            $"SELECT COUNT(*) FROM LogicalMovementGenerations WHERE LogicalMovementBatchId={rootId} AND GenerationNumber=0 AND PreviousGenerationNumber IS NULL AND MovementCorrectionOperationId IS NULL AND Kind=0 AND LineCount=1;"));
        Assert.Equal(1, await ScalarAsync(connection, $"""
            SELECT COUNT(*) FROM LogicalMovementGenerationLines
            WHERE LogicalMovementBatchId={rootId} AND State=0 AND Action=0 AND AppliedFieldMask=0
              AND PreviousGenerationLineId IS NULL AND ResultEffectiveMovementId={result.MovementId}
              AND LastEffectiveMovementId IS NULL AND TerminalReversalMovementId IS NULL;
            """));
        Assert.Equal(1, await ScalarAsync(connection, $"""
            SELECT COUNT(*) FROM LogicalMovementLedgerLinks ll
            JOIN LogicalMovementGenerationLines gl ON gl.Id=ll.IntroducedByGenerationLineId
            WHERE ll.LogicalMovementBatchId={rootId} AND ll.BinMovementId={result.MovementId}
              AND ll.Role=0 AND ll.LegacyMovementCorrectionLineId IS NULL
              AND gl.LogicalMovementBatchId=ll.LogicalMovementBatchId
              AND gl.LogicalMovementLineId=ll.LogicalMovementLineId;
            """));
        Assert.Equal(0, await ScalarAsync(connection,
            $"SELECT COUNT(*) FROM MovementCorrectionOperations WHERE LogicalMovementBatchId={rootId};"));
        Assert.Equal(0, await ScalarAsync(connection,
            $"SELECT COUNT(*) FROM LogicalMovementPhysicalOutputs WHERE LogicalMovementBatchId={rootId};"));
        Assert.Equal(1, await ScalarAsync(connection, "SELECT COUNT(*) FROM AuditEvents WHERE Action='MOVEMENT_RECORDED';"));

        var beforeRetry = await LineageSnapshotAsync(connection);
        Assert.Equal(result, await harness.Movements.SaveSingleAsync(request));
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            harness.Movements.SaveSingleAsync(request with { Quantity = request.Quantity + 1 }));
        Assert.Equal(beforeRetry, await LineageSnapshotAsync(connection));
        Assert.Equal(1, await ScalarAsync(connection, "SELECT COUNT(*) FROM BinMovements;"));
        Assert.Equal(1, await ScalarAsync(connection, "SELECT COUNT(*) FROM AuditEvents;"));

        await AssertResolvedAsync(harness.ConnectionString, rootId, [result.MovementId]);
        Assert.Equal(LineageSchema17MigrationOutcome.AlreadyComplete,
            (await harness.ValidateAlreadyCompleteAsync()).Outcome);
    }

    [Fact]
    public async Task Native_batch_preserves_first_successful_request_order_and_retries_never_rewrite_lineage()
    {
        await using var harness = await Harness.CreateAsync();
        var request = BatchRequest(harness);
        var first = await harness.Movements.SaveBatchAsync(request);

        await using var connection = await harness.OpenAsync();
        var rootId = await ScalarAsync(connection,
            $"SELECT Id FROM LogicalMovementBatches WHERE RootMovementBatchId={first.BatchId};");
        var ordered = await ReadInt64sAsync(connection, $"""
            SELECT m.CustomerId
            FROM LogicalMovementLines l
            JOIN BinMovements m ON m.Id=l.RootMovementId
            WHERE l.LogicalMovementBatchId={rootId}
            ORDER BY l.OriginalDisplayOrdinal;
            """);
        Assert.Equal(
            request.Lines.Select(x => (long)x.CustomerId),
            ordered);
        Assert.Equal(request.Lines.Count, await ScalarAsync(connection,
            $"SELECT COUNT(*) FROM LogicalMovementLines WHERE LogicalMovementBatchId={rootId};"));
        Assert.Equal(request.Lines.Count, await ScalarAsync(connection,
            $"SELECT COUNT(*) FROM LogicalMovementGenerationLines WHERE LogicalMovementBatchId={rootId};"));
        Assert.Equal(request.Lines.Count, await ScalarAsync(connection,
            $"SELECT COUNT(*) FROM LogicalMovementLedgerLinks WHERE LogicalMovementBatchId={rootId};"));
        Assert.Equal(1, await ScalarAsync(connection,
            $"SELECT COUNT(*) FROM LogicalMovementBatches WHERE Id={rootId} AND RootMovementBatchId={first.BatchId} AND Status=1 AND CurrentGenerationNumber=0 AND LineCount={request.Lines.Count};"));
        Assert.Equal(1, await ScalarAsync(connection,
            $"SELECT COUNT(*) FROM LogicalMovementGenerations WHERE LogicalMovementBatchId={rootId} AND GenerationNumber=0 AND PreviousGenerationNumber IS NULL AND MovementCorrectionOperationId IS NULL AND Kind=0 AND LineCount={request.Lines.Count};"));
        Assert.Equal(request.Lines.Count, await ScalarAsync(connection, $"""
            SELECT COUNT(*) FROM LogicalMovementGenerationLines
            WHERE LogicalMovementBatchId={rootId} AND State=0 AND Action=0 AND AppliedFieldMask=0
              AND PreviousGenerationLineId IS NULL AND ResultEffectiveMovementId IS NOT NULL
              AND LastEffectiveMovementId IS NULL AND TerminalReversalMovementId IS NULL;
            """));
        Assert.Equal(request.Lines.Count, await ScalarAsync(connection, $"""
            SELECT COUNT(*) FROM LogicalMovementLedgerLinks ll
            JOIN LogicalMovementGenerationLines gl ON gl.Id=ll.IntroducedByGenerationLineId
            WHERE ll.LogicalMovementBatchId={rootId} AND ll.Role=0
              AND ll.LegacyMovementCorrectionLineId IS NULL
              AND gl.LogicalMovementBatchId=ll.LogicalMovementBatchId
              AND gl.LogicalMovementLineId=ll.LogicalMovementLineId
              AND gl.ResultEffectiveMovementId=ll.BinMovementId;
            """));
        Assert.Equal(0, await ScalarAsync(connection,
            $"SELECT COUNT(*) FROM MovementCorrectionOperations WHERE LogicalMovementBatchId={rootId};"));
        Assert.Equal(0, await ScalarAsync(connection,
            $"SELECT COUNT(*) FROM LogicalMovementPhysicalOutputs WHERE LogicalMovementBatchId={rootId};"));

        var before = await LineageSnapshotAsync(connection);
        var identical = await harness.Movements.SaveBatchAsync(request);
        var reordered = await harness.Movements.SaveBatchAsync(
            request with { Lines = request.Lines.Reverse().ToArray() });
        Assert.Equal(first, identical);
        Assert.Equal(first, reordered);
        Assert.Equal(before, await LineageSnapshotAsync(connection));

        var changed = request.Lines.ToArray();
        changed[0] = changed[0] with { Quantity = changed[0].Quantity + 1 };
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            harness.Movements.SaveBatchAsync(request with { Lines = changed }));
        Assert.Equal(before, await LineageSnapshotAsync(connection));
        Assert.Equal(1, await ScalarAsync(connection, "SELECT COUNT(*) FROM MovementBatches;"));
        Assert.Equal(request.Lines.Count, await ScalarAsync(connection, "SELECT COUNT(*) FROM BinMovements;"));
        Assert.Equal(1, await ScalarAsync(connection,
            "SELECT COUNT(*) FROM AuditEvents WHERE Action='MOVEMENT_BATCH_RECORDED';"));

        var movementIds = await ReadInt64sAsync(connection, $"""
            SELECT RootMovementId FROM LogicalMovementLines
            WHERE LogicalMovementBatchId={rootId} ORDER BY OriginalDisplayOrdinal;
            """);
        await AssertResolvedAsync(harness.ConnectionString, rootId, movementIds);
        Assert.Equal(LineageSchema17MigrationOutcome.AlreadyComplete,
            (await harness.ValidateAlreadyCompleteAsync()).Outcome);
    }

    [Fact]
    public async Task Migrated_single_retry_returns_existing_result_without_rewriting_migration_baseline()
    {
        await using var harness = await Harness.CreateAsync(
            migrateToSchema17:false, enableLineageWriter:false);
        var request = SingleRequest(harness);
        var original = await harness.Movements.SaveSingleAsync(request);

        await using (var schema16 = await harness.OpenAsync())
        {
            Assert.Equal(1, await ScalarAsync(schema16, "SELECT COUNT(*) FROM BinMovements;"));
            Assert.Equal(1, await ScalarAsync(schema16, "SELECT COUNT(*) FROM AuditEvents;"));
        }

        await harness.MigrateAndEnableAsync();
        await using var connection = await harness.OpenAsync();
        var rootId = await ScalarAsync(connection,
            $"SELECT LogicalMovementBatchId FROM LogicalMovementLines WHERE RootMovementId={original.MovementId};");
        Assert.Equal(1, await ScalarAsync(connection,
            $"SELECT COUNT(*) FROM LogicalMovementGenerations WHERE LogicalMovementBatchId={rootId} AND GenerationNumber=0 AND Kind=1;"));
        Assert.Equal(1, await ScalarAsync(connection,
            $"SELECT COUNT(*) FROM LogicalMovementGenerationLines WHERE LogicalMovementBatchId={rootId} AND Action=1;"));
        var before = await LineageSnapshotAsync(connection);

        Assert.Equal(original, await harness.Movements.SaveSingleAsync(request));

        Assert.Equal(1, await ScalarAsync(connection, "SELECT COUNT(*) FROM BinMovements;"));
        Assert.Equal(1, await ScalarAsync(connection, "SELECT COUNT(*) FROM AuditEvents;"));
        Assert.Equal(before, await LineageSnapshotAsync(connection));
        Assert.Equal(1, await ScalarAsync(connection,
            $"SELECT COUNT(*) FROM LogicalMovementGenerations WHERE LogicalMovementBatchId={rootId} AND GenerationNumber=0 AND Kind=1;"));
        await AssertResolvedAsync(harness.ConnectionString, rootId, [original.MovementId]);
    }

    [Fact]
    public async Task Migrated_batch_exact_and_reordered_retries_do_not_rewrite_migration_baseline_or_ordinals()
    {
        await using var harness = await Harness.CreateAsync(
            migrateToSchema17:false, enableLineageWriter:false);
        var request = BatchRequest(harness);
        var original = await harness.Movements.SaveBatchAsync(request);

        await using (var schema16 = await harness.OpenAsync())
        {
            Assert.Equal(1, await ScalarAsync(schema16, "SELECT COUNT(*) FROM MovementBatches;"));
            Assert.Equal(request.Lines.Count, await ScalarAsync(schema16, "SELECT COUNT(*) FROM BinMovements;"));
            Assert.Equal(1, await ScalarAsync(schema16, "SELECT COUNT(*) FROM AuditEvents;"));
        }

        await harness.MigrateAndEnableAsync();
        await using var connection = await harness.OpenAsync();
        var rootId = await ScalarAsync(connection,
            $"SELECT Id FROM LogicalMovementBatches WHERE RootMovementBatchId={original.BatchId};");
        Assert.Equal(1, await ScalarAsync(connection,
            $"SELECT COUNT(*) FROM LogicalMovementGenerations WHERE LogicalMovementBatchId={rootId} AND GenerationNumber=0 AND Kind=1;"));
        Assert.Equal(request.Lines.Count, await ScalarAsync(connection,
            $"SELECT COUNT(*) FROM LogicalMovementGenerationLines WHERE LogicalMovementBatchId={rootId} AND Action=1;"));
        var originalOrder = await ReadInt64sAsync(connection, $"""
            SELECT RootMovementId FROM LogicalMovementLines
            WHERE LogicalMovementBatchId={rootId} ORDER BY OriginalDisplayOrdinal;
            """);
        var before = await LineageSnapshotAsync(connection);

        Assert.Equal(original, await harness.Movements.SaveBatchAsync(request));
        Assert.Equal(original, await harness.Movements.SaveBatchAsync(
            request with { Lines=request.Lines.Reverse().ToArray() }));

        Assert.Equal(1, await ScalarAsync(connection, "SELECT COUNT(*) FROM MovementBatches;"));
        Assert.Equal(request.Lines.Count, await ScalarAsync(connection, "SELECT COUNT(*) FROM BinMovements;"));
        Assert.Equal(1, await ScalarAsync(connection, "SELECT COUNT(*) FROM AuditEvents;"));
        Assert.Equal(before, await LineageSnapshotAsync(connection));
        Assert.Equal(originalOrder, await ReadInt64sAsync(connection, $"""
            SELECT RootMovementId FROM LogicalMovementLines
            WHERE LogicalMovementBatchId={rootId} ORDER BY OriginalDisplayOrdinal;
            """));
        Assert.Equal(1, await ScalarAsync(connection,
            $"SELECT COUNT(*) FROM LogicalMovementGenerations WHERE LogicalMovementBatchId={rootId} AND GenerationNumber=0 AND Kind=1;"));
        await AssertResolvedAsync(harness.ConnectionString, rootId, originalOrder);
    }

    [Theory]
    [InlineData(false, UserRole.Operator)]
    [InlineData(true, UserRole.Viewer)]
    public async Task Authorization_rejection_creates_no_artifact(bool authenticated, UserRole role)
    {
        await using var harness = await Harness.CreateAsync();
        harness.User.SetAccess(authenticated, role);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            harness.Movements.SaveSingleAsync(SingleRequest(harness)));

        await harness.AssertNoOperationArtifactsAsync();
    }

    [Theory]
    [InlineData("missing-customer")]
    [InlineData("inactive-customer")]
    [InlineData("missing-container")]
    [InlineData("inactive-container")]
    public async Task Master_data_rejection_creates_no_artifact(string scenario)
    {
        await using var harness = await Harness.CreateAsync();
        var request = SingleRequest(harness);
        request = scenario switch
        {
            "missing-customer" => request with { CustomerId = int.MaxValue },
            "inactive-customer" => request with { CustomerId = harness.InactiveCustomerId },
            "missing-container" => request with { ContainerTypeId = int.MaxValue },
            "inactive-container" => request with { ContainerTypeId = harness.InactiveContainerId },
            _ => throw new InvalidOperationException("Unknown scenario.")
        };

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            harness.Movements.SaveSingleAsync(request));

        await harness.AssertNoOperationArtifactsAsync();
    }

    [Fact]
    public async Task Explicit_schema17_writer_rejects_schema16_before_physical_write()
    {
        await using var harness = await Harness.CreateAsync(migrateToSchema17: false);

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            harness.Movements.SaveSingleAsync(SingleRequest(harness)));
        Assert.Equal("INITIAL_MOVEMENT_LINEAGE_SCHEMA17_REQUIRED", error.Message);

        await harness.AssertNoOperationArtifactsAsync(expectLineageTables: false);
    }

    [Fact]
    public async Task Preexisting_unowned_ordinary_movement_fails_health_before_new_physical_write()
    {
        await using var harness = await Harness.CreateAsync();
        await using (var db = harness.CreateContext())
        {
            db.BinMovements.Add(new BinMovement
            {
                ClientOperationId=Guid.NewGuid(), MovementDate=new DateOnly(2026,9,1),
                MovementType=MovementType.Out, Source=MovementSource.Manual,
                CustomerId=harness.ActiveCustomerId, ContainerTypeId=1, Quantity=2,
                CreatedBy="corrupt-fixture", CreatedUtc=Harness.UtcNow
            });
            await db.SaveChangesAsync();
        }

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            harness.Movements.SaveSingleAsync(SingleRequest(harness)));
        Assert.Equal("INITIAL_MOVEMENT_LINEAGE_SCHEMA17_HEALTH_INVALID", error.Message);

        await using var connection = await harness.OpenAsync();
        Assert.Equal(1, await ScalarAsync(connection, "SELECT COUNT(*) FROM BinMovements;"));
        Assert.Equal(0, await ScalarAsync(connection, "SELECT COUNT(*) FROM LogicalMovementBatches;"));
        Assert.Equal(0, await ScalarAsync(connection, "SELECT COUNT(*) FROM AuditEvents;"));
    }

    [Theory]
    [InlineData("physical")]
    [InlineData("mid-lineage")]
    [InlineData("post-lineage")]
    [InlineData("audit")]
    public async Task Failure_at_each_transaction_phase_rolls_back_every_new_artifact(string phase)
    {
        await using var harness = await Harness.CreateAsync();
        switch (phase)
        {
            case "physical": harness.FailSaveChangesOn(1); break;
            case "mid-lineage": harness.FailLineageAt(InitialMovementLineageWriteCheckpoint.AfterRootInserted); break;
            case "post-lineage": harness.FailLineageAt(InitialMovementLineageWriteCheckpoint.AfterFinalValidation); break;
            case "audit": harness.FailSaveChangesOn(2); break;
        }

        await Assert.ThrowsAnyAsync<Exception>(() =>
            harness.Movements.SaveSingleAsync(SingleRequest(harness)));

        await harness.AssertNoOperationArtifactsAsync();
    }

    [Fact]
    public async Task Batch_mid_lineage_failure_rolls_back_header_members_lineage_and_audit()
    {
        await using var harness = await Harness.CreateAsync();
        harness.FailLineageAt(InitialMovementLineageWriteCheckpoint.AfterRootInserted);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            harness.Movements.SaveBatchAsync(BatchRequest(harness)));

        await harness.AssertNoOperationArtifactsAsync();
    }

    [Fact]
    public async Task Failure_with_root_original_links_still_null_rolls_back_every_artifact()
    {
        await using var harness = await Harness.CreateAsync();
        var checkpoint =
            InitialMovementLineageWriteCheckpoint.AfterRootOriginalLinksInsertedBeforeIntroductionUpdate;
        harness.FailLineageAt(checkpoint);

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            harness.Movements.SaveSingleAsync(SingleRequest(harness)));

        Assert.Equal("ENTRY_V17_LINEAGE_FAILURE", error.Message);
        Assert.True(harness.WasLineageCheckpointObserved(checkpoint));
        await harness.AssertNoOperationArtifactsAsync();
    }

    private static SaveSingleMovementRequest SingleRequest(Harness harness) => new(
        Guid.NewGuid(), new DateOnly(2026,9,1), MovementType.Out,
        harness.ActiveCustomerId, 1, 7, "single-ref", "single-note");

    private static SaveMovementBatchRequest BatchRequest(Harness harness) => new(
        Guid.NewGuid(), new DateOnly(2026,9,1), MovementType.In, "batch-note",
        [
            new(harness.SecondCustomerId, 3, 4, "second", "second-note"),
            new(harness.ActiveCustomerId, 1, 2, "first", "first-note"),
            new(harness.ThirdCustomerId, 4, 9, "third", "third-note")
        ]);

    private static async Task AssertResolvedAsync(
        string connectionString, long rootId, IReadOnlyList<long> orderedMovementIds)
    {
        var resolution = await new SqliteLogicalMovementCurrentRootResolver(connectionString)
            .ResolveAsync(new(rootId));
        Assert.Equal(LogicalMovementCurrentRootResolutionKind.Resolved, resolution.Kind);
        Assert.Equal(0, resolution.Root!.CurrentGenerationNumber.Value);
        Assert.Equal(orderedMovementIds,
            resolution.Root.Lines.Select(x => x.RootMovementId).ToArray());
    }

    private static async Task<string> LineageSnapshotAsync(SqliteConnection connection)
    {
        var parts = new List<string>();
        foreach (var (table, columns) in new[]
        {
            ("LogicalMovementBatches", "Id,RootMovementBatchId,Status,CurrentGenerationNumber,LineCount,StatusReasonCode,CreatedUtc"),
            ("LogicalMovementLines", "Id,LogicalMovementBatchId,RootMovementId,OriginalDisplayOrdinal,CreatedUtc"),
            ("LogicalMovementGenerations", "Id,LogicalMovementBatchId,GenerationNumber,PreviousGenerationNumber,MovementCorrectionOperationId,Kind,LineCount,CreatedUtc"),
            ("LogicalMovementGenerationLines", "Id,LogicalMovementBatchId,LogicalMovementGenerationId,LogicalMovementLineId,State,Action,AppliedFieldMask,PreviousGenerationLineId,ResultEffectiveMovementId,LastEffectiveMovementId,TerminalReversalMovementId,CreatedUtc"),
            ("LogicalMovementLedgerLinks", "BinMovementId,LogicalMovementBatchId,LogicalMovementLineId,Role,IntroducedByGenerationLineId,LegacyMovementCorrectionLineId,CreatedUtc"),
            ("LogicalMovementPhysicalOutputs", "MovementBatchId,LogicalMovementBatchId,LogicalMovementGenerationId,LegacyMovementCorrectionOperationId,CreatedUtc")
        })
        {
            await using var command = connection.CreateCommand();
            command.CommandText = $"SELECT {columns} FROM {table} ORDER BY 1;";
            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
                parts.Add(table + ":" + string.Join('|', Enumerable.Range(0, reader.FieldCount)
                    .Select(i => reader.IsDBNull(i) ? "NULL" : Convert.ToString(reader.GetValue(i)))));
        }
        return string.Join(Environment.NewLine, parts);
    }

    private static async Task<long> ScalarAsync(SqliteConnection connection, string sql)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        return Convert.ToInt64(await command.ExecuteScalarAsync());
    }

    private static async Task<List<long>> ReadInt64sAsync(SqliteConnection connection, string sql)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        var result = new List<long>();
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync()) result.Add(reader.GetInt64(0));
        return result;
    }

    private sealed class Harness : IAsyncDisposable
    {
        public static readonly DateTime UtcNow = new(2026,9,2,1,2,3,DateTimeKind.Utc);
        private readonly string root;
        private ServiceProvider services;
        private LineageSchema17MigrationPrerequisites? prerequisites;
        private readonly TestSaveChangesInterceptor saveFailure;
        private readonly TestLineageFailureInjector lineageFailure;

        private Harness(
            string root,
            string connectionString,
            ServiceProvider services,
            LineageSchema17MigrationPrerequisites? prerequisites,
            TestUserContext user,
            TestSaveChangesInterceptor saveFailure,
            TestLineageFailureInjector lineageFailure)
        {
            this.root=root; ConnectionString=connectionString; this.services=services;
            this.prerequisites=prerequisites; User=user; this.saveFailure=saveFailure;
            this.lineageFailure=lineageFailure;
            Movements=services.GetRequiredService<IMovementService>();
        }

        public string ConnectionString { get; }
        public IMovementService Movements { get; private set; }
        public TestUserContext User { get; }
        public int ActiveCustomerId { get; private set; }
        public int SecondCustomerId { get; private set; }
        public int ThirdCustomerId { get; private set; }
        public int InactiveCustomerId { get; private set; }
        public int InactiveContainerId { get; private set; }

        public static async Task<Harness> CreateAsync(
            bool migrateToSchema17 = true,
            bool enableLineageWriter = true)
        {
            var root = Path.Combine(Path.GetTempPath(), $"BinTracker-entry-v17-{Guid.NewGuid():N}");
            Directory.CreateDirectory(root);
            var databasePath = Path.Combine(root, "db", "BinTracker.db");
            Directory.CreateDirectory(Path.GetDirectoryName(databasePath)!);
            var connectionString = $"Data Source={databasePath};Foreign Keys=True;Pooling=False";

            int activeId, secondId, thirdId, inactiveId, inactiveContainerId;
            await using (var db = new BinTrackerDbContext(
                new DbContextOptionsBuilder<BinTrackerDbContext>().UseSqlite(connectionString).Options))
            {
                await DatabaseSetup.InitializeSqliteAsync(db);
                var active = new Customer { CustomerCode="ACTIVE", Name="Active", IsActive=true };
                var second = new Customer { CustomerCode="SECOND", Name="Second", IsActive=true };
                var third = new Customer { CustomerCode="THIRD", Name="Third", IsActive=true };
                var inactive = new Customer { CustomerCode="INACTIVE", Name="Inactive", IsActive=false };
                var inactiveContainer = new ContainerType
                {
                    Name="Inactive Test Bin", NameKey="INACTIVE TEST BIN", ShortCode="INACTIVE",
                    SystemCode="INACTIVE_TEST_BIN", IsActive=false, DisplayOrder=99
                };
                db.AddRange(active, second, third, inactive, inactiveContainer);
                await db.SaveChangesAsync();
                activeId=active.Id; secondId=second.Id; thirdId=third.Id;
                inactiveId=inactive.Id; inactiveContainerId=inactiveContainer.Id;
            }

            LineageSchema17MigrationPrerequisites? prerequisites = null;
            if (migrateToSchema17)
                prerequisites = await MigrateAsync(root, databasePath);

            var saveFailure = new TestSaveChangesInterceptor();
            var lineageFailure = new TestLineageFailureInjector();
            var user = new TestUserContext();
            var services = BuildServices(
                connectionString, user, saveFailure, lineageFailure, enableLineageWriter);
            var harness = new Harness(root, connectionString, services, prerequisites,
                user, saveFailure, lineageFailure)
            {
                ActiveCustomerId=activeId, SecondCustomerId=secondId, ThirdCustomerId=thirdId,
                InactiveCustomerId=inactiveId, InactiveContainerId=inactiveContainerId
            };
            return harness;
        }

        public async Task MigrateAndEnableAsync()
        {
            if (prerequisites is not null)
                throw new InvalidOperationException("The harness has already migrated to schema 17.");

            await services.DisposeAsync();
            var databasePath = new SqliteConnectionStringBuilder(ConnectionString).DataSource;
            prerequisites = await MigrateAsync(root, databasePath);
            services = BuildServices(ConnectionString, User, saveFailure, lineageFailure,
                enableLineageWriter:true);
            Movements = services.GetRequiredService<IMovementService>();
        }

        private static ServiceProvider BuildServices(
            string connectionString,
            TestUserContext user,
            TestSaveChangesInterceptor saveFailure,
            TestLineageFailureInjector lineageFailure,
            bool enableLineageWriter)
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton<IBusinessClock>(new FixedClock());
            serviceCollection.AddSingleton<IUserContext>(user);
            serviceCollection.AddSingleton<IClientContext>(new TestClientContext());
            serviceCollection.AddDbContextFactory<BinTrackerDbContext>(builder =>
                builder.UseSqlite(connectionString).AddInterceptors(saveFailure));
            if (enableLineageWriter)
            {
                serviceCollection.AddScoped<IInitialMovementLineageWriter>(_ =>
                    new SqliteInitialMovementLineageWriter(lineageFailure));
            }
            serviceCollection.AddBinTrackerBusinessServices();
            return serviceCollection.BuildServiceProvider();
        }

        private static async Task<LineageSchema17MigrationPrerequisites> MigrateAsync(
            string root,
            string databasePath)
        {
            var gate = new WindowsFileDatabaseUpgradeGate(
                Path.Combine(root, "locks"), new NoConflictProbe());
            var lease = gate.AcquireUpgrade(databasePath);
            try
            {
                var preflightService = new SqliteLineageMigrationPreflight();
                var preflight = await preflightService.InspectAsync(databasePath);
                var backupService = new SqliteLineageMigrationBackupService(gate, preflightService);
                var backup = await backupService.CreateVerifiedAsync(
                    lease, Path.Combine(root, "recovery"));
                var prerequisites = new LineageSchema17MigrationPrerequisites(
                    lease, preflight, backup, backupService);
                await new SqliteLineageSchema17Migrator().MigrateAsync(prerequisites);
                return prerequisites;
            }
            catch
            {
                lease.Dispose();
                throw;
            }
        }

        public BinTrackerDbContext CreateContext() => new(
            new DbContextOptionsBuilder<BinTrackerDbContext>().UseSqlite(ConnectionString).Options);

        public async Task<SqliteConnection> OpenAsync()
        {
            var connection = new SqliteConnection(ConnectionString);
            await connection.OpenAsync();
            return connection;
        }

        public Task<LineageSchema17MigrationResult> ValidateAlreadyCompleteAsync() =>
            new SqliteLineageSchema17Migrator().MigrateAsync(
                prerequisites ?? throw new InvalidOperationException("Schema 17 was not prepared."));

        public void FailSaveChangesOn(int call) => saveFailure.Arm(call);
        public void FailLineageAt(InitialMovementLineageWriteCheckpoint checkpoint) =>
            lineageFailure.Arm(checkpoint);
        public bool WasLineageCheckpointObserved(InitialMovementLineageWriteCheckpoint checkpoint) =>
            lineageFailure.Observed.Contains(checkpoint);

        public async Task AssertNoOperationArtifactsAsync(bool expectLineageTables = true)
        {
            await using var connection = await OpenAsync();
            Assert.Equal(0, await ScalarAsync(connection, "SELECT COUNT(*) FROM MovementBatches;"));
            Assert.Equal(0, await ScalarAsync(connection, "SELECT COUNT(*) FROM BinMovements;"));
            Assert.Equal(0, await ScalarAsync(connection, "SELECT COUNT(*) FROM AuditEvents;"));
            if (expectLineageTables)
            {
                Assert.Equal(0, await ScalarAsync(connection, "SELECT COUNT(*) FROM LogicalMovementBatches;"));
                Assert.Equal(0, await ScalarAsync(connection, "SELECT COUNT(*) FROM LogicalMovementLines;"));
                Assert.Equal(0, await ScalarAsync(connection, "SELECT COUNT(*) FROM LogicalMovementGenerations;"));
                Assert.Equal(0, await ScalarAsync(connection, "SELECT COUNT(*) FROM LogicalMovementGenerationLines;"));
                Assert.Equal(0, await ScalarAsync(connection, "SELECT COUNT(*) FROM LogicalMovementLedgerLinks;"));
            }
        }

        public async ValueTask DisposeAsync()
        {
            await services.DisposeAsync();
            prerequisites?.UpgradeLease.Dispose();
            SqliteConnection.ClearAllPools();
            if (Directory.Exists(root)) Directory.Delete(root, recursive:true);
        }

        public sealed class TestUserContext : IUserContext
        {
            public string SessionId => "entry-v17-session";
            public int? UserId => IsAuthenticated ? 41 : null;
            public string Username => IsAuthenticated ? "entry-operator" : "anonymous";
            public string DisplayName => IsAuthenticated ? "Entry Operator" : "Not signed in";
            public UserRole Role { get; private set; } = UserRole.Operator;
            public bool MustChangePassword => false;
            public bool IsAuthenticated { get; private set; } = true;
            public void SetAccess(bool authenticated, UserRole role)
            { IsAuthenticated=authenticated; Role=role; }
        }

        private sealed class FixedClock : IBusinessClock
        {
            public DateTime UtcNow => Harness.UtcNow;
            public DateTime LocalNow => UtcNow;
            public DateOnly Today => new(2026,9,2);
            public string TimeZoneId => "UTC";
        }

        private sealed class TestClientContext : IClientContext
        {
            public string ClientInstanceId => "entry-v17-client";
            public string DeviceName => "entry-v17-device";
        }

        private sealed class TestSaveChangesInterceptor : SaveChangesInterceptor
        {
            private int? failOn;
            private int calls;
            public void Arm(int call) { calls=0; failOn=call; }
            public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
                DbContextEventData eventData, InterceptionResult<int> result,
                CancellationToken cancellationToken=default)
            {
                if (failOn.HasValue && ++calls == failOn.Value)
                {
                    failOn=null;
                    throw new InvalidOperationException("ENTRY_V17_SAVE_FAILURE");
                }
                return base.SavingChangesAsync(eventData, result, cancellationToken);
            }
        }

        private sealed class TestLineageFailureInjector : IInitialMovementLineageFailureInjector
        {
            private InitialMovementLineageWriteCheckpoint? requested;
            public HashSet<InitialMovementLineageWriteCheckpoint> Observed { get; } = [];
            public void Arm(InitialMovementLineageWriteCheckpoint checkpoint) => requested=checkpoint;
            public void ThrowIfRequested(InitialMovementLineageWriteCheckpoint checkpoint)
            {
                Observed.Add(checkpoint);
                if (requested == checkpoint)
                {
                    requested=null;
                    throw new InvalidOperationException("ENTRY_V17_LINEAGE_FAILURE");
                }
            }
        }

        private sealed class NoConflictProbe : IDatabaseOperationConflictProbe
        {
            public void EnsureNoConflict(string databasePath) { }
        }
    }
}
