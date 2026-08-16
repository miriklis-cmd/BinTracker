using BinTracker.Core;
using BinTracker.Data;
using BinTracker.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BinTracker.IntegrationTests;

public sealed class DailyMovementsReportSqliteTests
{
    [Fact]
    public async Task Daily_query_filters_date_and_excludes_adjustments_by_default()
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
                Movement(customer.Id, 1, new DateOnly(2026, 8, 15),
                    MovementType.Out, MovementSource.Batch, 9),
                Movement(customer.Id, 3, new DateOnly(2026, 8, 15),
                    MovementType.In, MovementSource.Manual, 4),
                Movement(customer.Id, 1, new DateOnly(2026, 8, 15),
                    MovementType.Out, MovementSource.Adjustment, 100),
                Movement(customer.Id, 1, new DateOnly(2026, 8, 14),
                    MovementType.Out, MovementSource.Batch, 50));

            await db.SaveChangesAsync();
        }

        var service = scope.ServiceProvider
            .GetRequiredService<IDailyMovementsReportService>();

        var physical = await service.QueryAsync(
            new DailyMovementsReportQuery(
                new DateOnly(2026, 8, 15)));

        Assert.Equal(2, physical.Rows.Count);
        Assert.Equal(9, physical.OutQuantity);
        Assert.Equal(4, physical.InQuantity);
        Assert.DoesNotContain(
            physical.Rows,
            x => x.Source == MovementSource.Adjustment);

        var withAdjustments = await service.QueryAsync(
            new DailyMovementsReportQuery(
                new DateOnly(2026, 8, 15),
                IncludeAdjustments: true));

        Assert.Equal(3, withAdjustments.Rows.Count);
        Assert.Contains(
            withAdjustments.Rows,
            x => x.Source == MovementSource.Adjustment &&
                 x.Quantity == 100);
    }

    [Fact]
    public async Task Daily_query_applies_customer_container_direction_and_source_filters()
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
                Name = "AEGIR",
                CustomerType = CustomerType.Account
            };

            db.Customers.AddRange(clamms, aegir);
            await db.SaveChangesAsync();

            db.BinMovements.AddRange(
                Movement(clamms.Id, 3, new DateOnly(2026, 8, 15),
                    MovementType.Out, MovementSource.Batch, 43),
                Movement(clamms.Id, 1, new DateOnly(2026, 8, 15),
                    MovementType.In, MovementSource.Manual, 3),
                Movement(aegir.Id, 3, new DateOnly(2026, 8, 15),
                    MovementType.Out, MovementSource.Batch, 2));

            await db.SaveChangesAsync();
        }

        var service = scope.ServiceProvider
            .GetRequiredService<IDailyMovementsReportService>();

        var result = await service.QueryAsync(
            new DailyMovementsReportQuery(
                new DateOnly(2026, 8, 15),
                CustomerSearch: "clam",
                ContainerTypeId: 3,
                Direction: MovementType.Out,
                Source: MovementSource.Batch));

        var row = Assert.Single(result.Rows);
        Assert.Equal("CLAMMS", row.CustomerCode);
        Assert.Equal(43, row.Quantity);
        Assert.Equal(MovementType.Out, row.Direction);
        Assert.Equal(MovementSource.Batch, row.Source);
    }


    [Fact]
    public async Task Future_daily_date_is_clamped_to_today()
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

        var today = DateOnly.FromDateTime(DateTime.Today);
        var tomorrow = today.AddDays(1);

        await using (var db = await factory.CreateDbContextAsync())
        {
            await db.Database.EnsureCreatedAsync();
            await DatabaseSetup.InitializeSqliteAsync(db);

            var customer = new Customer
            {
                CustomerCode = "TODAY",
                Name = "Today Test",
                CustomerType = CustomerType.Account
            };

            db.Customers.Add(customer);
            await db.SaveChangesAsync();

            db.BinMovements.AddRange(
                Movement(customer.Id, 1, today,
                    MovementType.Out, MovementSource.Batch, 2),
                Movement(customer.Id, 1, tomorrow,
                    MovementType.Out, MovementSource.Batch, 99));

            await db.SaveChangesAsync();
        }

        var service = scope.ServiceProvider
            .GetRequiredService<IDailyMovementsReportService>();

        var result = await service.QueryAsync(
            new DailyMovementsReportQuery(today.AddDays(14)));

        Assert.Equal(today, result.ReportDate);

        var row = Assert.Single(result.Rows);
        Assert.Equal(2, row.Quantity);
        Assert.DoesNotContain(
            result.Rows,
            x => x.MovementDate > today);
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
