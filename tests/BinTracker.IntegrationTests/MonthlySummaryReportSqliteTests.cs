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

    [Fact]
    public async Task Grouping_order_totals_sources_and_inactive_historical_metadata_are_preserved()
    {
        await using var connection =
            new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var services = new ServiceCollection();
        services.AddSingleton<IBusinessClock>(
            new FixedClock(new DateOnly(2026, 9, 8)));
        services.AddDbContextFactory<BinTrackerDbContext>(
            options => options.UseSqlite(connection));
        services.AddBinTrackerServices();

        await using var provider = services.BuildServiceProvider();
        await using var scope = provider.CreateAsyncScope();

        var factory = scope.ServiceProvider
            .GetRequiredService<IDbContextFactory<BinTrackerDbContext>>();

        int alphaId;
        int zuluId;
        await using (var db = await factory.CreateDbContextAsync())
        {
            await db.Database.EnsureCreatedAsync();
            await DatabaseSetup.InitializeSqliteAsync(db);

            var alpha = new Customer
            {
                CustomerCode = "alpha",
                Name = "Historical Alpha",
                CustomerType = CustomerType.Account,
                IsActive = false
            };
            var zulu = new Customer
            {
                CustomerCode = "ZULU",
                Name = "Zulu",
                CustomerType = CustomerType.Account
            };
            db.Customers.AddRange(zulu, alpha);
            await db.SaveChangesAsync();
            alphaId = alpha.Id;
            zuluId = zulu.Id;

            (await db.ContainerTypes.SingleAsync(x => x.Id == 2)).IsActive = false;
            db.BinMovements.AddRange(
                Movement(zulu.Id, 1, new DateOnly(2026, 9, 2),
                    MovementType.Out, MovementSource.Batch, 3),
                Movement(alpha.Id, 2, new DateOnly(2026, 9, 3),
                    MovementType.Out, MovementSource.Manual, 7),
                Movement(alpha.Id, 2, new DateOnly(2026, 9, 4),
                    MovementType.In, MovementSource.Manual, 7),
                Movement(alpha.Id, 1, new DateOnly(2026, 9, 5),
                    MovementType.In, MovementSource.ExcelImport, 2),
                Movement(alpha.Id, 1, new DateOnly(2026, 9, 6),
                    MovementType.Out, MovementSource.Adjustment, 11));
            await db.SaveChangesAsync();
        }

        var service = scope.ServiceProvider
            .GetRequiredService<IMonthlySummaryReportService>();

        var result = await service.QueryAsync(
            new MonthlySummaryReportQuery(new DateOnly(2026, 9, 1)));

        Assert.Equal(
            new[]
            {
                (alphaId, "alpha", "Historical Alpha", 1, "Blue Bin", 1, 0, 2, -2),
                (alphaId, "alpha", "Historical Alpha", 2, "Small Bin", 2, 7, 7, 0),
                (zuluId, "ZULU", "Zulu", 1, "Blue Bin", 1, 3, 0, 3)
            },
            result.Rows.Select(x =>
                (x.CustomerId, x.CustomerCode, x.CustomerName,
                    x.ContainerTypeId, x.ContainerType, x.ContainerDisplayOrder,
                    x.OutQuantity, x.InQuantity, x.NetQuantity)));
        Assert.Equal(
            new[]
            {
                (1, "Blue Bin", 1, 3, 2, 1),
                (2, "Small Bin", 2, 7, 7, 0)
            },
            result.ContainerTotals.Select(x =>
                (x.ContainerTypeId, x.ContainerType, x.DisplayOrder,
                    x.OutQuantity, x.InQuantity, x.NetQuantity)));
        Assert.Equal((10, 9, 1),
            (result.OutQuantity, result.InQuantity, result.NetQuantity));

        var excel = await service.QueryAsync(new MonthlySummaryReportQuery(
            new DateOnly(2026, 9, 1), Source: MovementSource.ExcelImport));
        Assert.Equal((alphaId, 0, 2),
            (Assert.Single(excel.Rows).CustomerId,
                excel.OutQuantity, excel.InQuantity));

        var adjustment = await service.QueryAsync(new MonthlySummaryReportQuery(
            new DateOnly(2026, 9, 1),
            Source: MovementSource.Adjustment,
            IncludeAdjustments: true));
        Assert.Equal((alphaId, 11, 0),
            (Assert.Single(adjustment.Rows).CustomerId,
                adjustment.OutQuantity, adjustment.InQuantity));
    }

    [Fact]
    public async Task Current_month_excludes_activity_after_today_and_future_requests_use_same_interval()
    {
        var today = new DateOnly(2026, 9, 8);
        await using var connection =
            new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var services = new ServiceCollection();
        services.AddSingleton<IBusinessClock>(new FixedClock(today));
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
                CustomerCode = "CLOCK",
                Name = "Clock Customer",
                CustomerType = CustomerType.Account
            };
            db.Customers.Add(customer);
            await db.SaveChangesAsync();
            db.BinMovements.AddRange(
                Movement(customer.Id, 1, today,
                    MovementType.Out, MovementSource.Manual, 4),
                Movement(customer.Id, 1, today.AddDays(1),
                    MovementType.Out, MovementSource.Manual, 100));
            await db.SaveChangesAsync();
        }

        var service = scope.ServiceProvider
            .GetRequiredService<IMonthlySummaryReportService>();
        var current = await service.QueryAsync(
            new MonthlySummaryReportQuery(new DateOnly(2026, 9, 20)));
        var future = await service.QueryAsync(
            new MonthlySummaryReportQuery(new DateOnly(2026, 10, 1)));

        foreach (var result in new[] { current, future })
        {
            Assert.Equal(new DateOnly(2026, 9, 1), result.MonthStart);
            Assert.Equal(new DateOnly(2026, 9, 30), result.MonthEnd);
            Assert.Equal(today, result.DataThroughDate);
            Assert.Equal(4, Assert.Single(result.Rows).OutQuantity);
        }
    }

    [Fact]
    public async Task Integer_group_totals_overflow_fails_closed()
    {
        await using var connection =
            new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var services = new ServiceCollection();
        services.AddSingleton<IBusinessClock>(
            new FixedClock(new DateOnly(2026, 9, 8)));
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
                CustomerCode = "OVERFLOW",
                Name = "Overflow Customer",
                CustomerType = CustomerType.Account
            };
            db.Customers.Add(customer);
            await db.SaveChangesAsync();
            db.BinMovements.AddRange(
                Movement(customer.Id, 1, new DateOnly(2026, 9, 1),
                    MovementType.Out, MovementSource.Manual, int.MaxValue),
                Movement(customer.Id, 1, new DateOnly(2026, 9, 2),
                    MovementType.Out, MovementSource.Manual, 1));
            await db.SaveChangesAsync();
        }

        var service = scope.ServiceProvider
            .GetRequiredService<IMonthlySummaryReportService>();

        await Assert.ThrowsAsync<OverflowException>(() =>
            service.QueryAsync(new MonthlySummaryReportQuery(
                new DateOnly(2026, 9, 1))));
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

    private sealed class FixedClock(DateOnly today) : IBusinessClock
    {
        public DateTime UtcNow => today.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        public DateTime LocalNow => UtcNow;
        public DateOnly Today => today;
        public string TimeZoneId => "UTC";
    }
}
