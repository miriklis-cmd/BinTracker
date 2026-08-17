using BinTracker.Core;
using BinTracker.Data;
using BinTracker.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BinTracker.IntegrationTests;

public sealed class MonthlySummaryReportSqliteTests
{
    [Fact]
    public async Task Month_is_inclusive_and_adjustments_are_excluded_by_default()
    {
        await using var connection =
            new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var services = new ServiceCollection();
        services.AddDbContextFactory<BinTrackerDbContext>(
            options => options.UseSqlite(connection));
        services.AddBinTrackerServices();

        await using var provider = services.BuildServiceProvider();
        await using var scope = provider.CreateAsyncScope();

        var factory = scope.ServiceProvider
            .GetRequiredService<IDbContextFactory<BinTrackerDbContext>>();

        await using (var db = await factory.CreateDbContextAsync())
        {
            await db.Database.EnsureCreatedAsync();
            await DatabaseSetup.InitializeSqliteAsync(db);

            var customer = new Customer
            {
                CustomerCode = "CLAMMS",
                Name = "Clamms",
                CustomerType = CustomerType.Account
            };

            db.Customers.Add(customer);
            await db.SaveChangesAsync();

            db.BinMovements.AddRange(
                Movement(customer.Id, 1, new DateOnly(2026, 7, 1),
                    MovementType.Out, MovementSource.Batch, 10),
                Movement(customer.Id, 1, new DateOnly(2026, 7, 31),
                    MovementType.In, MovementSource.Manual, 4),
                Movement(customer.Id, 1, new DateOnly(2026, 7, 15),
                    MovementType.Out, MovementSource.Adjustment, 100),
                Movement(customer.Id, 1, new DateOnly(2026, 6, 30),
                    MovementType.Out, MovementSource.Batch, 50));

            await db.SaveChangesAsync();
        }

        var service = scope.ServiceProvider
            .GetRequiredService<IMonthlySummaryReportService>();

        var result = await service.QueryAsync(
            new MonthlySummaryReportQuery(
                new DateOnly(2026, 7, 12)));

        var row = Assert.Single(result.Rows);

        Assert.Equal(10, row.OutQuantity);
        Assert.Equal(4, row.InQuantity);
        Assert.Equal(6, row.NetQuantity);
        Assert.Equal(10, result.OutQuantity);
        Assert.Equal(4, result.InQuantity);
    }

    [Fact]
    public async Task Filters_customer_container_and_source()
    {
        await using var connection =
            new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var services = new ServiceCollection();
        services.AddDbContextFactory<BinTrackerDbContext>(
            options => options.UseSqlite(connection));
        services.AddBinTrackerServices();

        await using var provider = services.BuildServiceProvider();
        await using var scope = provider.CreateAsyncScope();

        var factory = scope.ServiceProvider
            .GetRequiredService<IDbContextFactory<BinTrackerDbContext>>();

        await using (var db = await factory.CreateDbContextAsync())
        {
            await db.Database.EnsureCreatedAsync();
            await DatabaseSetup.InitializeSqliteAsync(db);

            var clamms = new Customer
            {
                CustomerCode = "CLAMMS",
                Name = "Clamms",
                CustomerType = CustomerType.Account
            };

            var aegir = new Customer
            {
                CustomerCode = "AEGIR",
                Name = "Aegir",
                CustomerType = CustomerType.Account
            };

            db.Customers.AddRange(clamms, aegir);
            await db.SaveChangesAsync();

            db.BinMovements.AddRange(
                Movement(clamms.Id, 3, new DateOnly(2026, 8, 3),
                    MovementType.Out, MovementSource.Batch, 45),
                Movement(clamms.Id, 1, new DateOnly(2026, 8, 4),
                    MovementType.In, MovementSource.Manual, 3),
                Movement(aegir.Id, 3, new DateOnly(2026, 8, 5),
                    MovementType.Out, MovementSource.Batch, 2));

            await db.SaveChangesAsync();
        }

        var service = scope.ServiceProvider
            .GetRequiredService<IMonthlySummaryReportService>();

        var result = await service.QueryAsync(
            new MonthlySummaryReportQuery(
                new DateOnly(2026, 8, 1),
                CustomerSearch: "clam",
                ContainerTypeId: 3,
                Source: MovementSource.Batch));

        var row = Assert.Single(result.Rows);
        Assert.Equal("CLAMMS", row.CustomerCode);
        Assert.Equal(45, row.OutQuantity);
        Assert.Equal(0, row.InQuantity);
    }

    [Fact]
    public async Task Future_month_is_clamped_to_current_month()
    {
        await using var connection =
            new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var services = new ServiceCollection();
        services.AddDbContextFactory<BinTrackerDbContext>(
            options => options.UseSqlite(connection));
        services.AddBinTrackerServices();

        await using var provider = services.BuildServiceProvider();
        await using var scope = provider.CreateAsyncScope();

        var factory = scope.ServiceProvider
            .GetRequiredService<IDbContextFactory<BinTrackerDbContext>>();

        await using (var db = await factory.CreateDbContextAsync())
        {
            await db.Database.EnsureCreatedAsync();
            await DatabaseSetup.InitializeSqliteAsync(db);
        }

        var service = scope.ServiceProvider
            .GetRequiredService<IMonthlySummaryReportService>();

        var today = DateOnly.FromDateTime(DateTime.Today);

        var result = await service.QueryAsync(
            new MonthlySummaryReportQuery(
                today.AddMonths(4)));

        Assert.Equal(
            new DateOnly(today.Year, today.Month, 1),
            result.MonthStart);

        Assert.Equal(today, result.DataThroughDate);
    }

    private static BinMovement Movement(
        int customerId,
        int containerTypeId,
        DateOnly date,
        MovementType type,
        MovementSource source,
        int quantity) =>
        new()
        {
            CustomerId = customerId,
            ContainerTypeId = containerTypeId,
            MovementDate = date,
            MovementType = type,
            Source = source,
            Quantity = quantity,
            CreatedBy = "test",
            CreatedUtc = DateTime.UtcNow
        };
}
