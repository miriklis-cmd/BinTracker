using BinTracker.Core;
using BinTracker.Data;
using BinTracker.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BinTracker.IntegrationTests;

public sealed class Slice1OperationalOutputCharacterizationTests
{
    [Fact]
    public async Task Customer_search_returns_signed_net_balance_per_customer_in_code_order()
    {
        await using var harness = await Harness.CreateAsync();
        await using (var db = await harness.Factory.CreateDbContextAsync())
        {
            var alpha = Customer("ALPHA", "Alpha Customer");
            var beta = Customer("BETA", "Beta Customer");
            var none = Customer("NONE", "No Movement Customer");
            var zero = Customer("ZERO", "Zero Customer");
            db.Customers.AddRange(beta, zero, alpha, none);
            await db.SaveChangesAsync();

            db.BinMovements.AddRange(
                Movement(alpha.Id, 3, MovementType.In, 4),
                Movement(beta.Id, 1, MovementType.Out, 8),
                Movement(beta.Id, 2, MovementType.In, 3),
                Movement(zero.Id, 1, MovementType.Out, 2),
                Movement(zero.Id, 2, MovementType.In, 2));
            await db.SaveChangesAsync();
        }

        await using var scope = harness.Provider.CreateAsyncScope();
        var rows = await scope.ServiceProvider
            .GetRequiredService<ICustomerService>()
            .SearchAsync(null, includeInactive: true);

        Assert.Collection(
            rows,
            row => AssertCustomer(row, "ALPHA", -4),
            row => AssertCustomer(row, "BETA", 5),
            row => AssertCustomer(row, "NONE", 0),
            row => AssertCustomer(row, "ZERO", 0));
    }

    [Fact]
    public async Task Customer_balances_include_every_configured_container_in_display_order()
    {
        await using var harness = await Harness.CreateAsync();
        int customerId;
        await using (var db = await harness.Factory.CreateDbContextAsync())
        {
            var target = Customer("TARGET", "Target Customer");
            var other = Customer("OTHER", "Other Customer");
            db.Customers.AddRange(target, other);
            await db.SaveChangesAsync();
            customerId = target.Id;

            db.BinMovements.AddRange(
                Movement(target.Id, 1, MovementType.Out, 7),
                Movement(target.Id, 1, MovementType.In, 2),
                Movement(target.Id, 2, MovementType.In, 4),
                Movement(target.Id, 3, MovementType.Out, 3),
                Movement(target.Id, 3, MovementType.In, 3),
                Movement(other.Id, 4, MovementType.Out, 99));
            await db.SaveChangesAsync();
        }

        await using var scope = harness.Provider.CreateAsyncScope();
        var rows = await scope.ServiceProvider
            .GetRequiredService<ICustomerService>()
            .GetBalancesAsync(customerId);

        Assert.Collection(
            rows,
            row => AssertBalance(row, "Blue Bin", 5, "5 OUT"),
            row => AssertBalance(row, "Small Bin", -4, "4 CREDIT"),
            row => AssertBalance(row, "Yellow Bin", 0, "Even"),
            row => AssertBalance(row, "Bulk Bin", 0, "Even"),
            row => AssertBalance(row, "CHEP Pallet", 0, "Even"));
    }

    [Fact]
    public async Task Recent_movements_are_customer_scoped_labelled_and_limited_after_date_and_id_ordering()
    {
        await using var harness = await Harness.CreateAsync();
        int customerId;
        await using (var db = await harness.Factory.CreateDbContextAsync())
        {
            var target = Customer("TARGET", "Target Customer");
            var other = Customer("OTHER", "Other Customer");
            db.Customers.AddRange(target, other);
            await db.SaveChangesAsync();
            customerId = target.Id;

            db.BinMovements.Add(Movement(
                target.Id, 1, MovementType.Out, 2,
                new DateOnly(2026, 9, 1), MovementSource.Manual, "OLD", "operator-a"));
            await db.SaveChangesAsync();
            db.BinMovements.Add(Movement(
                target.Id, 2, MovementType.In, 3,
                new DateOnly(2026, 9, 2), MovementSource.Manual, "RETURN", "operator-b"));
            await db.SaveChangesAsync();
            db.BinMovements.Add(Movement(
                target.Id, 3, MovementType.Out, 4,
                new DateOnly(2026, 9, 2), MovementSource.Adjustment, "ADJ-OUT", "import"));
            await db.SaveChangesAsync();
            db.BinMovements.Add(Movement(
                target.Id, 4, MovementType.In, 5,
                new DateOnly(2026, 9, 2), MovementSource.Adjustment, "ADJ-IN", "import"));
            await db.SaveChangesAsync();
            db.BinMovements.Add(Movement(
                other.Id, 1, MovementType.Out, 100,
                new DateOnly(2026, 9, 3), MovementSource.Manual, "OTHER", "operator-c"));
            await db.SaveChangesAsync();
        }

        await using var scope = harness.Provider.CreateAsyncScope();
        var service = scope.ServiceProvider.GetRequiredService<ICustomerService>();
        var rows = await service.GetRecentMovementsAsync(customerId, limit: 10);

        Assert.Collection(
            rows,
            row => AssertMovement(row, new DateOnly(2026, 9, 2), "Opening adjustment (IN)", "Bulk Bin", 5, "ADJ-IN", "import"),
            row => AssertMovement(row, new DateOnly(2026, 9, 2), "Opening adjustment (OUT)", "Yellow Bin", 4, "ADJ-OUT", "import"),
            row => AssertMovement(row, new DateOnly(2026, 9, 2), "IN (Returned)", "Small Bin", 3, "RETURN", "operator-b"),
            row => AssertMovement(row, new DateOnly(2026, 9, 1), "OUT (Taken)", "Blue Bin", 2, "OLD", "operator-a"));

        var limited = await service.GetRecentMovementsAsync(customerId, limit: 2);
        Assert.Equal(rows.Take(2), limited);
    }

    [Fact]
    public async Task Movement_customer_summary_returns_active_configured_containers_with_signed_balances_in_display_order()
    {
        await using var harness = await Harness.CreateAsync();
        await using (var db = await harness.Factory.CreateDbContextAsync())
        {
            var target = Customer("MIXED", "Mixed Position Customer");
            var other = Customer("OTHER", "Other Customer");
            db.Customers.AddRange(target, other);
            db.ContainerTypes.Add(new ContainerType
            {
                Name = "Archived Bin",
                NameKey = "ARCHIVED BIN",
                ShortCode = "ARCH",
                SystemCode = "ARCHIVED_BIN",
                DisplayOrder = 0,
                IsActive = false
            });
            await db.SaveChangesAsync();
            var archivedId = await db.ContainerTypes
                .Where(x => x.SystemCode == "ARCHIVED_BIN")
                .Select(x => x.Id)
                .SingleAsync();

            db.BinMovements.AddRange(
                Movement(target.Id, 1, MovementType.Out, 10),
                Movement(target.Id, 1, MovementType.In, 3),
                Movement(target.Id, 2, MovementType.In, 5),
                Movement(target.Id, archivedId, MovementType.Out, 99),
                Movement(other.Id, 1, MovementType.Out, 50));
            await db.SaveChangesAsync();
        }

        await using var scope = harness.Provider.CreateAsyncScope();
        var summary = await scope.ServiceProvider
            .GetRequiredService<IMovementService>()
            .GetCustomerSummaryByCodeAsync("  mixed  ");

        Assert.NotNull(summary);
        Assert.Equal("MIXED", summary.Code);
        Assert.Equal("Mixed Position Customer", summary.Name);
        Assert.Equal(CustomerType.Account, summary.CustomerType);
        Assert.Collection(
            summary.Balances,
            row => AssertMovementBalance(row, 1, "Blue Bin", 7, "7 OUT"),
            row => AssertMovementBalance(row, 2, "Small Bin", -5, "5 CREDIT"),
            row => AssertMovementBalance(row, 3, "Yellow Bin", 0, "Even"),
            row => AssertMovementBalance(row, 4, "Bulk Bin", 0, "Even"),
            row => AssertMovementBalance(row, 5, "CHEP Pallet", 0, "Even"));
    }

    [Fact]
    public async Task Container_usage_keeps_raw_activity_fields_distinct_from_customers_with_nonzero_balance()
    {
        await using var harness = await Harness.CreateAsync();
        await using (var db = await harness.Factory.CreateDbContextAsync())
        {
            var zero = Customer("ZERO", "Zero Customer");
            var positive = Customer("POSITIVE", "Positive Customer");
            var negative = Customer("NEGATIVE", "Negative Customer");
            db.Customers.AddRange(zero, positive, negative);
            await db.SaveChangesAsync();

            db.BinMovements.AddRange(
                Movement(zero.Id, 1, MovementType.Out, 5, new DateOnly(2026, 1, 1)),
                Movement(positive.Id, 1, MovementType.Out, 3, new DateOnly(2026, 1, 2)),
                Movement(negative.Id, 1, MovementType.In, 2, new DateOnly(2026, 1, 3)),
                Movement(zero.Id, 1, MovementType.In, 5, new DateOnly(2026, 1, 4)),
                Movement(zero.Id, 2, MovementType.Out, 20, new DateOnly(2025, 12, 1)));
            await db.SaveChangesAsync();
        }

        await using var scope = harness.Provider.CreateAsyncScope();
        var item = await scope.ServiceProvider
            .GetRequiredService<IContainerTypeService>()
            .GetAsync(1);

        Assert.NotNull(item);
        Assert.Equal("Blue Bin", item.Name);
        Assert.Equal(4, item.Usage.MovementCount);
        Assert.Equal(2, item.Usage.CustomersWithBalance);
        Assert.Equal(new DateOnly(2026, 1, 1), item.Usage.FirstUsed);
        Assert.Equal(new DateOnly(2026, 1, 4), item.Usage.LastUsed);
    }

    private static Customer Customer(string code, string name) => new()
    {
        CustomerCode = code,
        Name = name,
        CustomerType = CustomerType.Account
    };

    private static BinMovement Movement(
        int customerId,
        int containerTypeId,
        MovementType movementType,
        int quantity,
        DateOnly? date = null,
        MovementSource source = MovementSource.Manual,
        string? reference = null,
        string? createdBy = null) => new()
        {
            CustomerId = customerId,
            ContainerTypeId = containerTypeId,
            MovementDate = date ?? new DateOnly(2026, 9, 1),
            MovementType = movementType,
            Source = source,
            Quantity = quantity,
            ReferenceNumber = reference,
            CreatedBy = createdBy
        };

    private static void AssertCustomer(CustomerListRow row, string code, int balance)
    {
        Assert.Equal(code, row.CustomerCode);
        Assert.Equal(balance, row.NetBalance);
    }

    private static void AssertBalance(
        CustomerBalanceRow row,
        string container,
        int balance,
        string position)
    {
        Assert.Equal(container, row.ContainerType);
        Assert.Equal(balance, row.Balance);
        Assert.Equal(position, row.Position);
    }

    private static void AssertMovement(
        CustomerMovementRow row,
        DateOnly date,
        string direction,
        string container,
        int quantity,
        string reference,
        string createdBy)
    {
        Assert.Equal(date, row.Date);
        Assert.Equal(direction, row.Direction);
        Assert.Equal(container, row.ContainerType);
        Assert.Equal(quantity, row.Quantity);
        Assert.Equal(reference, row.Reference);
        Assert.Equal(createdBy, row.CreatedBy);
    }

    private static void AssertMovementBalance(
        MovementBalanceRow row,
        int containerId,
        string container,
        int balance,
        string position)
    {
        Assert.Equal(containerId, row.ContainerTypeId);
        Assert.Equal(container, row.ContainerType);
        Assert.Equal(balance, row.Balance);
        Assert.Equal(position, row.Position);
    }

    private sealed class Harness : IAsyncDisposable
    {
        private readonly SqliteConnection connection;

        private Harness(
            SqliteConnection connection,
            ServiceProvider provider,
            IDbContextFactory<BinTrackerDbContext> factory)
        {
            this.connection = connection;
            Provider = provider;
            Factory = factory;
        }

        public ServiceProvider Provider { get; }

        public IDbContextFactory<BinTrackerDbContext> Factory { get; }

        public static async Task<Harness> CreateAsync()
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();

            var services = new ServiceCollection();
            services.AddDbContextFactory<BinTrackerDbContext>(options =>
                options.UseSqlite(connection));
            services.AddBinTrackerServices();

            var provider = services.BuildServiceProvider();
            var factory = provider.GetRequiredService<IDbContextFactory<BinTrackerDbContext>>();
            await using var db = await factory.CreateDbContextAsync();
            await db.Database.EnsureCreatedAsync();

            return new Harness(connection, provider, factory);
        }

        public async ValueTask DisposeAsync()
        {
            await Provider.DisposeAsync();
            await connection.DisposeAsync();
        }
    }
}
