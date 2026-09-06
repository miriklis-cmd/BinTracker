using BinTracker.Core;
using BinTracker.Data;
using BinTracker.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BinTracker.IntegrationTests;

public sealed class DashboardOperationalOutputCharacterizationTests
{
    [Fact]
    public async Task Dashboard_uses_requested_day_activity_and_raw_all_date_grouped_positions()
    {
        await using var h = await Harness.CreateAsync();
        var date = new DateOnly(2026, 9, 5);
        await using (var db = await h.Factory.CreateDbContextAsync())
        {
            var active = Customer("ACTIVE", "Active Customer");
            var inactive = Customer("INACTIVE", "Inactive Customer", isActive: false);
            var credit = Customer("CREDIT", "Credit Customer");
            db.Customers.AddRange(active, inactive, credit);
            await db.SaveChangesAsync();

            var inactiveContainer = await db.ContainerTypes.SingleAsync(x => x.Id == 2);
            inactiveContainer.IsActive = false;
            var settings = await db.ApplicationSettings.SingleAsync(x => x.Id == 1);
            settings.AttentionQuantityThreshold = 7;

            db.BinMovements.AddRange(
                Movement(active.Id, 1, date, MovementType.In, 3),
                Movement(active.Id, 1, date, MovementType.Out, 10),
                Movement(active.Id, 1, date, MovementType.In, 2, MovementSource.Adjustment),
                Movement(active.Id, 1, date, MovementType.Out, 4, MovementSource.ExcelImport),
                Movement(active.Id, 1, date.AddDays(1), MovementType.Out, 6),
                Movement(active.Id, 2, date.AddDays(-1), MovementType.Out, 8),
                Movement(inactive.Id, 1, date, MovementType.Out, 7),
                Movement(credit.Id, 1, date.AddDays(-1), MovementType.In, 5),
                Movement(credit.Id, 2, date.AddDays(-1), MovementType.Out, 5),
                Movement(credit.Id, 2, date.AddDays(-1), MovementType.In, 5));
            await db.SaveChangesAsync();
        }

        var summary = await h.Movements.GetDashboardSummaryAsync(date);

        Assert.Equal(
            new OperationalDashboardSummary(
                ReturnedToday: 5,
                TakenToday: 21,
                Outstanding: 30,
                RequiresAttention: 1),
            summary);
    }

    [Fact]
    public async Task Dashboard_defaults_attention_threshold_to_twenty_and_uses_strict_comparison()
    {
        await using var h = await Harness.CreateAsync();
        var date = new DateOnly(2026, 9, 5);
        await using (var db = await h.Factory.CreateDbContextAsync())
        {
            db.ApplicationSettings.Remove(await db.ApplicationSettings.SingleAsync(x => x.Id == 1));
            var exact = Customer("EXACT", "Exact Threshold Customer");
            var above = Customer("ABOVE", "Above Threshold Customer");
            db.Customers.AddRange(exact, above);
            await db.SaveChangesAsync();
            db.BinMovements.AddRange(
                Movement(exact.Id, 1, date, MovementType.Out, 20),
                Movement(above.Id, 1, date, MovementType.Out, 21));
            await db.SaveChangesAsync();
        }

        var summary = await h.Movements.GetDashboardSummaryAsync(date);

        Assert.Equal(41, summary.Outstanding);
        Assert.Equal(1, summary.RequiresAttention);
    }

    private static Customer Customer(string code, string name, bool isActive = true) => new()
    {
        CustomerCode = code,
        Name = name,
        CustomerType = CustomerType.Account,
        IsActive = isActive
    };

    private static BinMovement Movement(
        int customerId,
        int containerTypeId,
        DateOnly date,
        MovementType direction,
        int quantity,
        MovementSource source = MovementSource.Manual) => new()
        {
            CustomerId = customerId,
            ContainerTypeId = containerTypeId,
            MovementDate = date,
            MovementType = direction,
            Quantity = quantity,
            Source = source,
            CreatedBy = "dashboard-characterization"
        };

    private sealed class Harness : IAsyncDisposable
    {
        private readonly SqliteConnection connection;
        private readonly ServiceProvider provider;

        private Harness(
            SqliteConnection connection,
            ServiceProvider provider,
            IDbContextFactory<BinTrackerDbContext> factory)
        {
            this.connection = connection;
            this.provider = provider;
            Factory = factory;
            Movements = provider.GetRequiredService<IMovementService>();
        }

        public IDbContextFactory<BinTrackerDbContext> Factory { get; }
        public IMovementService Movements { get; }

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
            await provider.DisposeAsync();
            await connection.DisposeAsync();
        }
    }
}
