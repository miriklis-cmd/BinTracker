using BinTracker.Core;
using BinTracker.Data;
using BinTracker.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BinTracker.IntegrationTests;

public sealed class TransactionAuditAppenderTests
{
    [Fact]
    public async Task Caller_commit_persists_exactly_one_primary_audit_event()
    {
        await using var harness = await Harness.CreateAsync();
        await using var db = harness.CreateContext();
        await using var transaction = await db.Database.BeginTransactionAsync();
        var auditEvent = PrimaryAudit("commit");

        var appended = new TransactionAuditAppender().AppendPrimary(db, auditEvent);

        Assert.Same(auditEvent, appended);
        Assert.Equal(EntityState.Added, db.Entry(auditEvent).State);
        Assert.Single(db.ChangeTracker.Entries<AuditEvent>());
        Assert.Empty(await db.AuditEvents.AsNoTracking().ToListAsync());

        await db.SaveChangesAsync();
        await transaction.CommitAsync();
        db.ChangeTracker.Clear();

        var persisted = Assert.Single(await db.AuditEvents.AsNoTracking().ToListAsync());
        Assert.Equal("MOVEMENT_CHANGE_PLANNED", persisted.Action);
        Assert.Equal("commit", persisted.Description);
        Assert.True(persisted.RequiresAdministratorReview);
    }

    [Fact]
    public async Task Caller_rollback_removes_primary_audit_and_sibling_state_from_same_transaction()
    {
        await using var harness = await Harness.CreateAsync();
        await using var db = harness.CreateContext();
        await using var transaction = await db.Database.BeginTransactionAsync();
        var sibling = new Customer
        {
            CustomerCode = "ROLLBACK-SIBLING",
            Name = "Rollback sibling customer"
        };

        new TransactionAuditAppender().AppendPrimary(db, PrimaryAudit("rollback"));
        db.Customers.Add(sibling);
        await db.SaveChangesAsync();
        Assert.Single(db.ChangeTracker.Entries<AuditEvent>());
        Assert.Single(await db.AuditEvents.AsNoTracking()
            .Where(x => x.Description == "rollback").ToListAsync());
        Assert.Single(await db.Customers.AsNoTracking()
            .Where(x => x.CustomerCode == "ROLLBACK-SIBLING").ToListAsync());

        await transaction.RollbackAsync();

        await using var verify = harness.CreateContext();
        Assert.Empty(await verify.AuditEvents.AsNoTracking()
            .Where(x => x.Description == "rollback").ToListAsync());
        Assert.Empty(await verify.Customers.AsNoTracking()
            .Where(x => x.CustomerCode == "ROLLBACK-SIBLING").ToListAsync());
    }

    [Fact]
    public async Task Append_requires_the_callers_active_transaction_and_tracks_nothing_on_rejection()
    {
        await using var harness = await Harness.CreateAsync();
        await using var db = harness.CreateContext();

        var error = Assert.Throws<InvalidOperationException>(() =>
            new TransactionAuditAppender().AppendPrimary(db, PrimaryAudit("rejected")));

        Assert.Contains("caller-owned transaction", error.Message);
        Assert.Empty(db.ChangeTracker.Entries<AuditEvent>());
        Assert.Empty(await db.AuditEvents.AsNoTracking().ToListAsync());
    }

    [Fact]
    public async Task Existing_independent_audit_service_still_saves_its_event()
    {
        await using var harness = await Harness.CreateAsync();
        using var provider = harness.CreateServices();
        var audit = provider.GetRequiredService<IAuditService>();

        await audit.WriteAsync(
            "EXISTING_AUDIT_BEHAVIOR",
            "Characterization",
            "42",
            "Independent audit write",
            before: new { Value = "before" },
            after: new { Value = "after" });

        await using var verify = harness.CreateContext();
        var persisted = Assert.Single(await verify.AuditEvents.AsNoTracking().ToListAsync());
        Assert.Equal(Harness.AuditUtc, persisted.TimestampUtc);
        Assert.Equal("anonymous", persisted.Username);
        Assert.Equal("EXISTING_AUDIT_BEHAVIOR", persisted.Action);
        Assert.Equal("Characterization", persisted.EntityType);
        Assert.Equal("42", persisted.EntityId);
        Assert.Equal("Independent audit write", persisted.Description);
        Assert.Contains("before", persisted.BeforeValues);
        Assert.Contains("after", persisted.AfterValues);
        Assert.True(persisted.Succeeded);
    }

    private static AuditEvent PrimaryAudit(string description) => new()
    {
        TimestampUtc = Harness.AuditUtc,
        UserId = 7,
        Username = "operator",
        Action = "MOVEMENT_CHANGE_PLANNED",
        EntityType = "MovementCorrectionOperation",
        EntityId = "future",
        Description = description,
        ComputerName = "test-device",
        SessionId = "test-session",
        Succeeded = true,
        RequiresAdministratorReview = true
    };

    private sealed class Harness : IAsyncDisposable
    {
        public static readonly DateTime AuditUtc = new(2026, 9, 2, 1, 2, 3, DateTimeKind.Utc);
        private readonly SqliteConnection connection;
        private readonly DbContextOptions<BinTrackerDbContext> options;

        private Harness(SqliteConnection connection)
        {
            this.connection = connection;
            options = new DbContextOptionsBuilder<BinTrackerDbContext>()
                .UseSqlite(connection)
                .Options;
        }

        public static async Task<Harness> CreateAsync()
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();
            var harness = new Harness(connection);
            await using var db = harness.CreateContext();
            await db.Database.EnsureCreatedAsync();
            await DatabaseSetup.InitializeSqliteAsync(db);
            return harness;
        }

        public BinTrackerDbContext CreateContext() => new(options);

        public ServiceProvider CreateServices()
        {
            var services = new ServiceCollection();
            services.AddSingleton<IBusinessClock>(new FixedClock());
            services.AddDbContextFactory<BinTrackerDbContext>(builder => builder.UseSqlite(connection));
            services.AddBinTrackerServices();
            return services.BuildServiceProvider();
        }

        public ValueTask DisposeAsync() => connection.DisposeAsync();

        private sealed class FixedClock : IBusinessClock
        {
            public DateTime UtcNow => AuditUtc;
            public DateTime LocalNow => AuditUtc;
            public DateOnly Today => DateOnly.FromDateTime(AuditUtc);
            public string TimeZoneId => "UTC";
        }
    }
}
