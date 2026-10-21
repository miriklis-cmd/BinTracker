using BinTracker.Core;
using BinTracker.Data;
using BinTracker.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BinTracker.IntegrationTests;

public sealed class SecurityBatchCValidationTests
{
    [Fact]
    public async Task Undefined_report_filters_are_rejected_instead_of_returning_empty_success()
    {
        await using var harness = await Harness.CreateAsync();
        var date = Harness.Today;

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            harness.Get<IDailyMovementsReportService>().QueryAsync(
                new(date, Direction: (MovementType)99)));
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            harness.Get<IDailyMovementsReportService>().QueryAsync(
                new(date, Source: (MovementSource)99)));
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            harness.Get<IWeeklyMovementsReportService>().QueryAsync(
                new(date, Source: (MovementSource)99)));
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            harness.Get<IMonthlySummaryReportService>().QueryAsync(
                new(date, Source: (MovementSource)99)));
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            harness.Get<IMovementHistoryReportService>().QueryAsync(
                new(date, date, Direction: (MovementType)99)));
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            harness.Get<IMovementHistoryReportService>().QueryAsync(
                new(date, date, Source: (MovementSource)99)));
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            harness.Get<IOutstandingReportService>().QueryAsync(
                new(date, BalanceFilter: (OutstandingBalanceFilter)99)));
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            harness.Get<IAuditService>().GetAuditTrailAsync((AuditReviewFilter)99));
    }

    [Fact]
    public async Task Undefined_mutation_enums_are_rejected_before_business_or_audit_writes()
    {
        await using var harness = await Harness.CreateAsync();

        await Assert.ThrowsAsync<ArgumentException>(() =>
            harness.Get<IUserService>().CreateUserAsync(
                "invalid-role", "Invalid Role", "CorrectHorse!42", (UserRole)99));
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            harness.Get<ICustomerService>().SaveAsync(new CustomerEditModel
            {
                CustomerCode = "INVALID-TYPE",
                Name = "Invalid Type",
                CustomerType = (CustomerType)99
            }));
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            harness.Get<IMovementService>().SaveSingleAsync(new(
                Guid.NewGuid(), Harness.Today, (MovementType)99,
                harness.CustomerId, 1, 1, null, null)));
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            harness.Get<IMovementService>().SaveBatchAsync(new(
                Guid.NewGuid(), Harness.Today, (MovementType)99, null,
                [new MovementBatchLine(harness.CustomerId, 1, 1, null, null)])));

        await using var verify = harness.CreateContext();
        Assert.False(await verify.Customers.AnyAsync(x => x.CustomerCode == "INVALID-TYPE"));
        Assert.Empty(await verify.BinMovements.ToListAsync());
        Assert.Empty(await verify.MovementBatches.ToListAsync());
        Assert.Empty(await verify.AuditEvents.ToListAsync());
        Assert.Equal(1, await verify.UserAccounts.CountAsync());
    }

    [Theory]
    [InlineData(nameof(CustomerEditModel.CustomerCode), 51)]
    [InlineData(nameof(CustomerEditModel.Name), 201)]
    [InlineData(nameof(CustomerEditModel.ContactName), 151)]
    [InlineData(nameof(CustomerEditModel.Phone), 51)]
    [InlineData(nameof(CustomerEditModel.MobileNumber), 51)]
    [InlineData(nameof(CustomerEditModel.Email), 201)]
    [InlineData(nameof(CustomerEditModel.Address), 501)]
    [InlineData(nameof(CustomerEditModel.Notes), 2001)]
    public async Task Customer_maxima_are_rejected_before_business_or_audit_writes(
        string propertyName, int length)
    {
        await using var harness = await Harness.CreateAsync();
        var model = new CustomerEditModel
        {
            CustomerCode = "BOUNDED",
            Name = "Bounded Customer"
        };
        typeof(CustomerEditModel).GetProperty(propertyName)!.SetValue(model, new string('X', length));

        await Assert.ThrowsAsync<ArgumentException>(() =>
            harness.Get<ICustomerService>().SaveAsync(model));

        await using var verify = harness.CreateContext();
        Assert.False(await verify.Customers.AnyAsync(x => x.CustomerCode == "BOUNDED"));
        Assert.Empty(await verify.AuditEvents.ToListAsync());
    }

    [Fact]
    public async Task Supported_unicode_operational_identifiers_retain_existing_normalization()
    {
        await using var harness = await Harness.CreateAsync();

        var customerId = await harness.Get<ICustomerService>().SaveAsync(new CustomerEditModel
        {
            CustomerCode = "  åβ-客户  ",
            Name = "Unicode Customer"
        });
        var containerId = await harness.Get<IContainerTypeService>().SaveAsync(
            new ContainerTypeEditModel(
                0, "  Café Bin  ", " ü箱 ", string.Empty, null, null, 10, true,
                false, null, new ContainerTypeUsage(0, 0, null, null), 0));

        await using var verify = harness.CreateContext();
        var customer = await verify.Customers.AsNoTracking().SingleAsync(x => x.Id == customerId);
        var container = await verify.ContainerTypes.AsNoTracking().SingleAsync(x => x.Id == containerId);
        Assert.Equal("ÅΒ-客户", customer.CustomerCode);
        Assert.Equal("Ü箱", container.ShortCode);
        Assert.Equal("Ü箱", container.SystemCode);
    }

    [Fact]
    public async Task Exact_customer_field_maxima_remain_valid()
    {
        await using var harness = await Harness.CreateAsync();
        var id = await harness.Get<ICustomerService>().SaveAsync(new CustomerEditModel
        {
            CustomerCode = new string('C', 50),
            Name = new string('N', 200),
            ContactName = new string('P', 150),
            Phone = new string('1', 50),
            MobileNumber = new string('2', 50),
            Email = new string('E', 200),
            Address = new string('A', 500),
            Notes = new string('Z', 2000)
        });

        await using var verify = harness.CreateContext();
        Assert.True(await verify.Customers.AnyAsync(x => x.Id == id));
        Assert.Single(await verify.AuditEvents.Where(x => x.Action == "CUSTOMER_CREATED").ToListAsync());
    }

    [Theory]
    [InlineData("name", 101)]
    [InlineData("short", 31)]
    [InlineData("description", 501)]
    [InlineData("notes", 2001)]
    [InlineData("colour", 51)]
    public async Task Container_maxima_are_rejected_before_business_or_audit_writes(
        string field, int length)
    {
        await using var harness = await Harness.CreateAsync();
        var value = new string('X', length);
        var model = new ContainerTypeEditModel(
            0,
            field == "name" ? value : "Bounded Container",
            field == "short" ? value : "BOUND",
            string.Empty,
            field == "description" ? value : null,
            field == "notes" ? value : null,
            10,
            true,
            false,
            field == "colour" ? value : null,
            new ContainerTypeUsage(0, 0, null, null),
            0);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            harness.Get<IContainerTypeService>().SaveAsync(model));

        await using var verify = harness.CreateContext();
        Assert.False(await verify.ContainerTypes.AnyAsync(x => x.ShortCode == "BOUND"));
        Assert.Empty(await verify.AuditEvents.ToListAsync());
    }

    private sealed class Harness : IAsyncDisposable
    {
        public static readonly DateOnly Today = new(2026, 10, 19);
        private readonly SqliteConnection connection;
        private readonly ServiceProvider provider;
        private readonly AsyncServiceScope scope;
        private readonly IDbContextFactory<BinTrackerDbContext> factory;

        private Harness(SqliteConnection connection, ServiceProvider provider,
            AsyncServiceScope scope, IDbContextFactory<BinTrackerDbContext> factory,
            int customerId)
        {
            this.connection = connection;
            this.provider = provider;
            this.scope = scope;
            this.factory = factory;
            CustomerId = customerId;
        }

        public int CustomerId { get; }

        public static async Task<Harness> CreateAsync()
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();
            var services = new ServiceCollection();
            services.AddSingleton<IBusinessClock>(new FixedClock());
            services.AddDbContextFactory<BinTrackerDbContext>(options => options.UseSqlite(connection));
            services.AddBinTrackerServices();
            var provider = services.BuildServiceProvider();
            var scope = provider.CreateAsyncScope();
            var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<BinTrackerDbContext>>();
            int customerId;
            await using (var db = await factory.CreateDbContextAsync())
            {
                await db.Database.EnsureCreatedAsync();
                await DatabaseSetup.InitializeSqliteAsync(db);
                var user = new UserAccount
                {
                    Username = "batch-c-admin",
                    DisplayName = "Batch C Administrator",
                    PasswordHash = "x",
                    PasswordSalt = "x",
                    Role = UserRole.Administrator,
                    IsActive = true
                };
                var customer = new Customer { CustomerCode = "BASE", Name = "Base Customer" };
                db.AddRange(user, customer);
                await db.SaveChangesAsync();
                customerId = customer.Id;
                scope.ServiceProvider.GetRequiredService<UserSession>().SignIn(user);
            }

            return new(connection, provider, scope, factory, customerId);
        }

        public T Get<T>() where T : notnull => scope.ServiceProvider.GetRequiredService<T>();
        public BinTrackerDbContext CreateContext() => factory.CreateDbContext();

        public async ValueTask DisposeAsync()
        {
            await scope.DisposeAsync();
            await provider.DisposeAsync();
            await connection.DisposeAsync();
        }

        private sealed class FixedClock : IBusinessClock
        {
            public DateTime UtcNow => Today.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            public DateTime LocalNow => UtcNow;
            public DateOnly Today => Harness.Today;
            public string TimeZoneId => "Test/Fixed";
        }
    }
}
