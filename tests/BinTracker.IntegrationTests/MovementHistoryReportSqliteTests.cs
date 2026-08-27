using BinTracker.Core;
using BinTracker.Data;
using BinTracker.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BinTracker.IntegrationTests;

public sealed class MovementHistoryReportSqliteTests
{
    [Fact]
    public async Task Persisted_movement_ids_survive_filtering_and_are_ordered_numerically()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var services = new ServiceCollection();
        services.AddDbContextFactory<BinTrackerDbContext>(
            options => options.UseSqlite(connection));
        services.AddBinTrackerServices();

        await using var provider = services.BuildServiceProvider();
        await using var scope = provider.CreateAsyncScope();
        var factory = scope.ServiceProvider
            .GetRequiredService<IDbContextFactory<BinTrackerDbContext>>();

        var date = new DateOnly(2026, 8, 25);
        await using (var db = await factory.CreateDbContextAsync())
        {
            await db.Database.EnsureCreatedAsync();
            await DatabaseSetup.InitializeSqliteAsync(db);
            var customer = new Customer
            {
                CustomerCode = "JMPL",
                Name = "JMPL",
                CustomerType = CustomerType.Account
            };
            db.Customers.Add(customer);
            await db.SaveChangesAsync();

            var movement10 = M(customer.Id, 1, date, MovementType.In, MovementSource.Batch, 2);
            movement10.Id = 10;
            var movement2 = M(customer.Id, 1, date, MovementType.In, MovementSource.Batch, 7);
            movement2.Id = 2;
            db.BinMovements.AddRange(movement10, movement2);
            await db.SaveChangesAsync();
        }

        var result = await scope.ServiceProvider
            .GetRequiredService<IMovementHistoryReportService>()
            .QueryAsync(new MovementHistoryReportQuery(date, date, CustomerSearch: "JMPL"));

        Assert.Equal([2L, 10L], result.Rows.Select(x => x.MovementId).ToArray());
        Assert.Equal(7, Assert.Single(result.Rows, x => x.MovementId == 2).Quantity);
        Assert.Equal(2, Assert.Single(result.Rows, x => x.MovementId == 10).Quantity);
    }

    [Fact]
    public async Task Date_range_is_inclusive_and_adjustments_are_excluded_by_default()
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
                M(customer.Id, 1, new DateOnly(2026, 8, 1),
                    MovementType.Out, MovementSource.Batch, 5),
                M(customer.Id, 1, new DateOnly(2026, 8, 10),
                    MovementType.In, MovementSource.Manual, 2),
                M(customer.Id, 1, new DateOnly(2026, 8, 15),
                    MovementType.Out, MovementSource.ExcelImport, 7),
                M(customer.Id, 1, new DateOnly(2026, 8, 12),
                    MovementType.Out, MovementSource.Adjustment, 100),
                M(customer.Id, 1, new DateOnly(2026, 7, 31),
                    MovementType.Out, MovementSource.Batch, 50));

            await db.SaveChangesAsync();
        }

        var service = scope.ServiceProvider
            .GetRequiredService<IMovementHistoryReportService>();

        var result = await service.QueryAsync(
            new MovementHistoryReportQuery(
                new DateOnly(2026, 8, 1),
                new DateOnly(2026, 8, 15)));

        Assert.Equal(3, result.Rows.Count);
        Assert.Equal(12, result.OutQuantity);
        Assert.Equal(2, result.InQuantity);
        Assert.Equal(10, result.NetQuantity);
        Assert.DoesNotContain(
            result.Rows,
            x => x.Source == MovementSource.Adjustment);
    }

    [Fact]
    public async Task Filters_customer_container_direction_and_source()
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
                M(clamms.Id, 3, new DateOnly(2026, 8, 11),
                    MovementType.Out, MovementSource.Batch, 43),
                M(clamms.Id, 1, new DateOnly(2026, 8, 11),
                    MovementType.In, MovementSource.Manual, 3),
                M(aegir.Id, 3, new DateOnly(2026, 8, 11),
                    MovementType.Out, MovementSource.Batch, 2));

            await db.SaveChangesAsync();
        }

        var service = scope.ServiceProvider
            .GetRequiredService<IMovementHistoryReportService>();

        var result = await service.QueryAsync(
            new MovementHistoryReportQuery(
                new DateOnly(2026, 8, 1),
                new DateOnly(2026, 8, 15),
                CustomerSearch: "clam",
                ContainerTypeId: 3,
                Direction: MovementType.Out,
                Source: MovementSource.Batch));

        var row = Assert.Single(result.Rows);
        Assert.Equal("CLAMMS", row.CustomerCode);
        Assert.Equal(43, row.Quantity);
    }

    [Fact]
    public async Task Future_dates_are_clamped_and_reversed_ranges_are_normalized()
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
        var yesterday = today.AddDays(-1);
        var tomorrow = today.AddDays(1);

        await using (var db = await factory.CreateDbContextAsync())
        {
            await db.Database.EnsureCreatedAsync();
            await DatabaseSetup.InitializeSqliteAsync(db);

            var customer = new Customer
            {
                CustomerCode = "RANGE",
                Name = "Range Test",
                CustomerType = CustomerType.Account
            };

            db.Customers.Add(customer);
            await db.SaveChangesAsync();

            db.BinMovements.AddRange(
                M(customer.Id, 1, yesterday,
                    MovementType.Out, MovementSource.Batch, 2),
                M(customer.Id, 1, today,
                    MovementType.In, MovementSource.Batch, 1),
                M(customer.Id, 1, tomorrow,
                    MovementType.Out, MovementSource.Batch, 99));

            await db.SaveChangesAsync();
        }

        var service = scope.ServiceProvider
            .GetRequiredService<IMovementHistoryReportService>();

        var result = await service.QueryAsync(
            new MovementHistoryReportQuery(
                today.AddDays(10),
                yesterday));

        Assert.Equal(yesterday, result.StartDate);
        Assert.Equal(today, result.EndDate);
        Assert.Equal(2, result.Rows.Count);
        Assert.DoesNotContain(
            result.Rows,
            x => x.MovementDate > today);
    }

    private static BinMovement M(
        int customerId,
        int containerId,
        DateOnly date,
        MovementType type,
        MovementSource source,
        int qty) =>
        new()
        {
            CustomerId = customerId,
            ContainerTypeId = containerId,
            MovementDate = date,
            MovementType = type,
            Source = source,
            Quantity = qty,
            CreatedBy = "test",
            CreatedUtc = DateTime.UtcNow
        };
}
