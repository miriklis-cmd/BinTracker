using System.Text.Json;
using BinTracker.Core;
using BinTracker.Data;
using BinTracker.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BinTracker.IntegrationTests;

public sealed class MovementEntryCharacterizationTests
{
    [Fact]
    public async Task Single_save_retry_and_conflict_preserve_one_truthful_movement_and_audit()
    {
        await using var harness = await Harness.CreateAsync();
        var operationId = Guid.NewGuid();
        var request = new SaveSingleMovementRequest(
            operationId,
            new DateOnly(2026, 9, 1),
            MovementType.Out,
            harness.ActiveCustomerId,
            1,
            7,
            "  single-ref  ",
            "  single-note  ");

        var first = await harness.Movements.SaveSingleAsync(request);
        var retry = await harness.Movements.SaveSingleAsync(request);

        Assert.Equal(first, retry);
        Assert.Equal(7, first.NewBalance);

        var conflict = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            harness.Movements.SaveSingleAsync(request with { Quantity = 8 }));
        Assert.Contains("different movement request", conflict.Message);

        await using var db = harness.CreateContext();
        var movement = Assert.Single(await db.BinMovements.AsNoTracking().ToListAsync());
        Assert.Equal(first.MovementId, movement.Id);
        Assert.Equal(operationId, movement.ClientOperationId);
        Assert.Equal(request.MovementDate, movement.MovementDate);
        Assert.Equal(request.MovementType, movement.MovementType);
        Assert.Equal(MovementSource.Manual, movement.Source);
        Assert.Equal(harness.ActiveCustomerId, movement.CustomerId);
        Assert.Equal(1, movement.ContainerTypeId);
        Assert.Equal(7, movement.Quantity);
        Assert.Equal("single-ref", movement.ReferenceNumber);
        Assert.Equal("single-note", movement.Notes);
        Assert.Equal(Harness.Username, movement.CreatedBy);
        Assert.Equal(Harness.UtcNow, movement.CreatedUtc);
        Assert.Null(movement.MovementBatchId);
        Assert.Null(movement.ImportRunId);
        Assert.Null(movement.ReversesMovementId);
        Assert.Null(movement.CorrectedByMovementId);

        var audit = Assert.Single(await db.AuditEvents.AsNoTracking().ToListAsync());
        Assert.Equal(Harness.UtcNow, audit.TimestampUtc);
        Assert.Equal(Harness.UserId, audit.UserId);
        Assert.Equal(Harness.Username, audit.Username);
        Assert.Equal("MOVEMENT_RECORDED", audit.Action);
        Assert.Equal("BinMovement", audit.EntityType);
        Assert.Equal(movement.Id.ToString(), audit.EntityId);
        Assert.Equal(
            "OUT (Taken) manual movement recorded: 7 Blue Bin for ACTIVE.",
            audit.Description);
        Assert.Equal(Harness.DeviceName, audit.ComputerName);
        Assert.Equal(Harness.SessionId, audit.SessionId);
        Assert.True(audit.Succeeded);
        Assert.False(audit.RequiresAdministratorReview);
        Assert.Null(audit.BeforeValues);

        using var after = JsonDocument.Parse(Assert.IsType<string>(audit.AfterValues));
        Assert.Equal("2026-09-01", after.RootElement.GetProperty("MovementDate").GetString());
        Assert.Equal("OUT (Taken)", after.RootElement.GetProperty("Direction").GetString());
        Assert.Equal("ACTIVE", after.RootElement.GetProperty("Customer").GetString());
        Assert.Equal("Blue Bin", after.RootElement.GetProperty("Container").GetString());
        Assert.Equal(7, after.RootElement.GetProperty("Quantity").GetInt32());
        Assert.Equal("single-ref", after.RootElement.GetProperty("ReferenceNumber").GetString());
        Assert.Equal("7 OUT", after.RootElement.GetProperty("NewPosition").GetString());

        await AssertSchema16DormantAsync(db);
    }

    [Theory]
    [InlineData(false, UserRole.Operator)]
    [InlineData(true, UserRole.Viewer)]
    public async Task Single_authorization_rejection_leaves_zero_artifacts(
        bool authenticated,
        UserRole role)
    {
        await using var harness = await Harness.CreateAsync();
        harness.User.SetAccess(authenticated, role);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            harness.Movements.SaveSingleAsync(SingleRequest(harness)));

        await harness.AssertNoArtifactsAsync();
    }

    [Theory]
    [InlineData("missing-customer", "customer")]
    [InlineData("inactive-customer", "customer")]
    [InlineData("missing-container", "container")]
    [InlineData("inactive-container", "container")]
    public async Task Single_master_data_rejection_leaves_zero_artifacts(
        string scenario,
        string expectedMessage)
    {
        await using var harness = await Harness.CreateAsync();
        var request = SingleRequest(harness);
        request = scenario switch
        {
            "missing-customer" => request with { CustomerId = int.MaxValue },
            "inactive-customer" => request with { CustomerId = harness.InactiveCustomerId },
            "missing-container" => request with { ContainerTypeId = int.MaxValue },
            "inactive-container" => request with { ContainerTypeId = harness.InactiveContainerId },
            _ => throw new InvalidOperationException("Unknown characterization scenario.")
        };

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            harness.Movements.SaveSingleAsync(request));
        Assert.Contains(expectedMessage, error.Message, StringComparison.OrdinalIgnoreCase);

        await harness.AssertNoArtifactsAsync();
    }

    [Fact]
    public async Task Single_audit_save_failure_rolls_back_the_already_saved_movement()
    {
        await using var harness = await Harness.CreateAsync();
        harness.FailSaveChangesOn(2);

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            harness.Movements.SaveSingleAsync(SingleRequest(harness)));
        Assert.Equal(Harness.InjectedFailure, error.Message);

        await harness.AssertNoArtifactsAsync();
    }

    [Fact]
    public async Task Batch_save_retry_and_conflict_preserve_truthful_membership_and_one_audit()
    {
        await using var harness = await Harness.CreateAsync();
        var operationId = Guid.NewGuid();
        var request = new SaveMovementBatchRequest(
            operationId,
            new DateOnly(2026, 9, 1),
            MovementType.In,
            "  batch-note  ",
            [
                new(harness.SecondCustomerId, 3, 4, "  second-ref  ", "  second-note  "),
                new(harness.ActiveCustomerId, 1, 2, "  first-ref  ", "  first-note  "),
                new(harness.ActiveCustomerId, 4, 9, null, "  bulk-note  ")
            ]);

        var first = await harness.Movements.SaveBatchAsync(request);
        var retry = await harness.Movements.SaveBatchAsync(request);
        var reorderedRetry = await harness.Movements.SaveBatchAsync(
            request with { Lines = request.Lines.Reverse().ToArray() });

        Assert.Equal(first, retry);
        Assert.Equal(first, reorderedRetry);
        Assert.Equal(3, first.LineCount);
        Assert.Equal(15, first.TotalQuantity);

        var changedLines = request.Lines.ToArray();
        changedLines[0] = changedLines[0] with { Quantity = 5 };
        var conflict = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            harness.Movements.SaveBatchAsync(request with { Lines = changedLines }));
        Assert.Contains("different batch request", conflict.Message);

        await using var db = harness.CreateContext();
        var batch = Assert.Single(await db.MovementBatches.AsNoTracking().ToListAsync());
        Assert.Equal(first.BatchId, batch.Id);
        Assert.Equal(operationId, batch.ClientOperationId);
        Assert.Equal(request.MovementDate, batch.MovementDate);
        Assert.Equal(request.MovementType, batch.MovementType);
        Assert.Equal(MovementSource.Batch, batch.Source);
        Assert.Equal("batch-note", batch.Notes);
        Assert.Equal(Harness.Username, batch.CreatedBy);
        Assert.Equal(Harness.UtcNow, batch.CreatedUtc);
        Assert.False(batch.IsReversed);

        var members = await db.BinMovements.AsNoTracking()
            .Where(x => x.MovementBatchId == batch.Id)
            .ToListAsync();
        Assert.Equal(3, members.Count);
        var expectedMemberMultiset = request.Lines
            .Select(x => new
            {
                x.CustomerId,
                x.ContainerTypeId,
                x.Quantity,
                ReferenceNumber = x.Reference?.Trim(),
                Notes = x.Notes?.Trim()
            })
            .OrderBy(x => x.CustomerId)
            .ThenBy(x => x.ContainerTypeId)
            .ThenBy(x => x.Quantity)
            .ThenBy(x => x.ReferenceNumber)
            .ThenBy(x => x.Notes)
            .ToList();
        var actualMemberMultiset = members
            .Select(x => new
            {
                x.CustomerId,
                x.ContainerTypeId,
                x.Quantity,
                x.ReferenceNumber,
                x.Notes
            })
            .OrderBy(x => x.CustomerId)
            .ThenBy(x => x.ContainerTypeId)
            .ThenBy(x => x.Quantity)
            .ThenBy(x => x.ReferenceNumber)
            .ThenBy(x => x.Notes)
            .ToList();
        Assert.Equal(expectedMemberMultiset, actualMemberMultiset);
        Assert.All(members, movement => AssertBatchMember(movement, batch));

        var audit = Assert.Single(await db.AuditEvents.AsNoTracking().ToListAsync());
        Assert.Equal(Harness.UtcNow, audit.TimestampUtc);
        Assert.Equal(Harness.UserId, audit.UserId);
        Assert.Equal(Harness.Username, audit.Username);
        Assert.Equal("MOVEMENT_BATCH_RECORDED", audit.Action);
        Assert.Equal("MovementBatch", audit.EntityType);
        Assert.Equal(batch.Id.ToString(), audit.EntityId);
        Assert.Equal(
            $"IN (Returned) batch #{batch.Id} recorded with 3 line(s) and 15 total containers.",
            audit.Description);
        Assert.Equal(Harness.DeviceName, audit.ComputerName);
        Assert.Equal(Harness.SessionId, audit.SessionId);
        Assert.True(audit.Succeeded);
        Assert.False(audit.RequiresAdministratorReview);
        Assert.Null(audit.BeforeValues);

        using var after = JsonDocument.Parse(Assert.IsType<string>(audit.AfterValues));
        Assert.Equal("2026-09-01", after.RootElement.GetProperty("MovementDate").GetString());
        Assert.Equal("IN (Returned)", after.RootElement.GetProperty("Direction").GetString());
        Assert.Equal(3, after.RootElement.GetProperty("LineCount").GetInt32());
        Assert.Equal(15, after.RootElement.GetProperty("TotalQuantity").GetInt32());

        Assert.Equal(3, await db.BinMovements.CountAsync());
        await AssertSchema16DormantAsync(db);
    }

    [Theory]
    [InlineData(false, UserRole.Operator)]
    [InlineData(true, UserRole.Viewer)]
    public async Task Batch_authorization_rejection_leaves_zero_artifacts(
        bool authenticated,
        UserRole role)
    {
        await using var harness = await Harness.CreateAsync();
        harness.User.SetAccess(authenticated, role);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            harness.Movements.SaveBatchAsync(BatchRequest(harness)));

        await harness.AssertNoArtifactsAsync();
    }

    [Theory]
    [InlineData("missing-customer", "customers")]
    [InlineData("inactive-customer", "customers")]
    [InlineData("missing-container", "container types")]
    [InlineData("inactive-container", "container types")]
    public async Task Batch_master_data_rejection_leaves_zero_artifacts(
        string scenario,
        string expectedMessage)
    {
        await using var harness = await Harness.CreateAsync();
        var request = BatchRequest(harness);
        var line = request.Lines[0];
        line = scenario switch
        {
            "missing-customer" => line with { CustomerId = int.MaxValue },
            "inactive-customer" => line with { CustomerId = harness.InactiveCustomerId },
            "missing-container" => line with { ContainerTypeId = int.MaxValue },
            "inactive-container" => line with { ContainerTypeId = harness.InactiveContainerId },
            _ => throw new InvalidOperationException("Unknown characterization scenario.")
        };
        request = request with { Lines = [line] };

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            harness.Movements.SaveBatchAsync(request));
        Assert.Contains(expectedMessage, error.Message, StringComparison.OrdinalIgnoreCase);

        await harness.AssertNoArtifactsAsync();
    }

    [Theory]
    [InlineData(2)]
    [InlineData(3)]
    public async Task Batch_intermediate_save_failure_rolls_back_batch_members_and_audit(
        int failedSaveChangesCall)
    {
        await using var harness = await Harness.CreateAsync();
        harness.FailSaveChangesOn(failedSaveChangesCall);

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            harness.Movements.SaveBatchAsync(BatchRequest(harness)));
        Assert.Equal(Harness.InjectedFailure, error.Message);

        await harness.AssertNoArtifactsAsync();
    }

    private static SaveSingleMovementRequest SingleRequest(Harness harness) => new(
        Guid.NewGuid(),
        new DateOnly(2026, 9, 1),
        MovementType.Out,
        harness.ActiveCustomerId,
        1,
        3,
        "single-ref",
        "single-note");

    private static SaveMovementBatchRequest BatchRequest(Harness harness) => new(
        Guid.NewGuid(),
        new DateOnly(2026, 9, 1),
        MovementType.Out,
        "batch-note",
        [new(harness.ActiveCustomerId, 1, 3, "batch-ref", "line-note")]);

    private static void AssertBatchMember(
        BinMovement movement,
        MovementBatch batch)
    {
        Assert.Null(movement.ClientOperationId);
        Assert.Equal(batch.Id, movement.MovementBatchId);
        Assert.Equal(batch.MovementDate, movement.MovementDate);
        Assert.Equal(batch.MovementType, movement.MovementType);
        Assert.Equal(MovementSource.Batch, movement.Source);
        Assert.Equal(Harness.Username, movement.CreatedBy);
        Assert.Equal(Harness.UtcNow, movement.CreatedUtc);
        Assert.Null(movement.ImportRunId);
        Assert.Null(movement.ReversesMovementId);
        Assert.Null(movement.CorrectedByMovementId);
    }

    private static async Task AssertSchema16DormantAsync(BinTrackerDbContext db)
    {
        var version = await db.Database
            .SqlQueryRaw<int>("SELECT Version AS Value FROM SchemaVersion WHERE Id = 1")
            .SingleAsync();
        var lineageTableCount = await db.Database
            .SqlQueryRaw<int>(
                "SELECT COUNT(*) AS Value FROM sqlite_master WHERE type='table' AND name LIKE 'LogicalMovement%'")
            .SingleAsync();

        Assert.Equal(16, version);
        Assert.Equal(0, lineageTableCount);
    }

    private sealed class Harness : IAsyncDisposable
    {
        public const int UserId = 41;
        public const string Username = "entry-operator";
        public const string SessionId = "entry-session";
        public const string DeviceName = "entry-device";
        public const string InjectedFailure = "CHARACTERIZATION_INJECTED_SAVE_FAILURE";
        public static readonly DateTime UtcNow =
            new(2026, 9, 2, 1, 2, 3, DateTimeKind.Utc);

        private readonly SqliteConnection connection;
        private readonly DbContextOptions<BinTrackerDbContext> options;
        private readonly ServiceProvider services;
        private readonly FailingSaveChangesInterceptor failureInterceptor;

        private Harness(
            SqliteConnection connection,
            DbContextOptions<BinTrackerDbContext> options,
            ServiceProvider services,
            TestUserContext user,
            FailingSaveChangesInterceptor failureInterceptor)
        {
            this.connection = connection;
            this.options = options;
            this.services = services;
            User = user;
            this.failureInterceptor = failureInterceptor;
            Movements = services.GetRequiredService<IMovementService>();
        }

        public int ActiveCustomerId { get; private set; }
        public int SecondCustomerId { get; private set; }
        public int InactiveCustomerId { get; private set; }
        public int InactiveContainerId { get; private set; }
        public TestUserContext User { get; }
        public IMovementService Movements { get; }

        public static async Task<Harness> CreateAsync()
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();
            var failureInterceptor = new FailingSaveChangesInterceptor();
            var options = new DbContextOptionsBuilder<BinTrackerDbContext>()
                .UseSqlite(connection)
                .AddInterceptors(failureInterceptor)
                .Options;
            var user = new TestUserContext();
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton<IBusinessClock>(new FixedClock());
            serviceCollection.AddSingleton<IUserContext>(user);
            serviceCollection.AddSingleton<IClientContext>(new TestClientContext());
            serviceCollection.AddDbContextFactory<BinTrackerDbContext>(builder =>
                builder.UseSqlite(connection).AddInterceptors(failureInterceptor));
            serviceCollection.AddBinTrackerBusinessServices();
            var services = serviceCollection.BuildServiceProvider();
            var harness = new Harness(connection, options, services, user, failureInterceptor);

            await using var db = harness.CreateContext();
            await db.Database.EnsureCreatedAsync();
            await DatabaseSetup.InitializeSqliteAsync(db);

            var active = new Customer
            {
                CustomerCode = "ACTIVE",
                Name = "Active Customer",
                IsActive = true
            };
            var second = new Customer
            {
                CustomerCode = "SECOND",
                Name = "Second Customer",
                IsActive = true
            };
            var inactive = new Customer
            {
                CustomerCode = "INACTIVE",
                Name = "Inactive Customer",
                IsActive = false
            };
            var inactiveContainer = new ContainerType
            {
                Name = "Inactive Test Bin",
                NameKey = "INACTIVE TEST BIN",
                ShortCode = "INACTIVE",
                SystemCode = "INACTIVE_TEST_BIN",
                IsActive = false,
                DisplayOrder = 99
            };
            db.AddRange(active, second, inactive, inactiveContainer);
            await db.SaveChangesAsync();

            harness.ActiveCustomerId = active.Id;
            harness.SecondCustomerId = second.Id;
            harness.InactiveCustomerId = inactive.Id;
            harness.InactiveContainerId = inactiveContainer.Id;
            return harness;
        }

        public BinTrackerDbContext CreateContext() => new(options);

        public void FailSaveChangesOn(int invocation) =>
            failureInterceptor.Arm(invocation);

        public async Task AssertNoArtifactsAsync()
        {
            await using var db = CreateContext();
            Assert.Empty(await db.MovementBatches.AsNoTracking().ToListAsync());
            Assert.Empty(await db.BinMovements.AsNoTracking().ToListAsync());
            Assert.Empty(await db.AuditEvents.AsNoTracking().ToListAsync());
            await AssertSchema16DormantAsync(db);
        }

        public async ValueTask DisposeAsync()
        {
            await services.DisposeAsync();
            await connection.DisposeAsync();
        }

        public sealed class TestUserContext : IUserContext
        {
            public string SessionId => MovementEntryCharacterizationTests.Harness.SessionId;
            public int? UserId => IsAuthenticated
                ? MovementEntryCharacterizationTests.Harness.UserId
                : null;
            public string Username => IsAuthenticated
                ? MovementEntryCharacterizationTests.Harness.Username
                : "anonymous";
            public string DisplayName => IsAuthenticated ? "Entry Operator" : "Not signed in";
            public UserRole Role { get; private set; } = UserRole.Operator;
            public bool MustChangePassword => false;
            public bool IsAuthenticated { get; private set; } = true;

            public void SetAccess(bool authenticated, UserRole role)
            {
                IsAuthenticated = authenticated;
                Role = role;
            }
        }

        private sealed class FixedClock : IBusinessClock
        {
            public DateTime UtcNow => MovementEntryCharacterizationTests.Harness.UtcNow;
            public DateTime LocalNow => UtcNow;
            public DateOnly Today => new(2026, 9, 2);
            public string TimeZoneId => "UTC";
        }

        private sealed class TestClientContext : IClientContext
        {
            public string ClientInstanceId => "entry-client";
            public string DeviceName => MovementEntryCharacterizationTests.Harness.DeviceName;
        }

        private sealed class FailingSaveChangesInterceptor : SaveChangesInterceptor
        {
            private int? failOnInvocation;
            private int invocation;

            public void Arm(int failOn)
            {
                invocation = 0;
                failOnInvocation = failOn;
            }

            public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
                DbContextEventData eventData,
                InterceptionResult<int> result,
                CancellationToken cancellationToken = default)
            {
                if (failOnInvocation.HasValue && ++invocation == failOnInvocation.Value)
                {
                    failOnInvocation = null;
                    throw new InvalidOperationException(InjectedFailure);
                }

                return base.SavingChangesAsync(eventData, result, cancellationToken);
            }
        }
    }
}
