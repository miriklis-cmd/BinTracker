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

    [Fact]
    public async Task Daily_query_preserves_historical_metadata_order_totals_and_source_distinctions()
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
        var date = new DateOnly(2026, 8, 15);
        var recordedUtc = new DateTime(2026, 8, 15, 4, 5, 6, DateTimeKind.Utc);

        int inactiveCustomerId;
        int activeCustomerId;
        await using (var db = await factory.CreateDbContextAsync())
        {
            await db.Database.EnsureCreatedAsync();
            await DatabaseSetup.InitializeSqliteAsync(db);

            var inactive = new Customer
            {
                CustomerCode = "ALPHA",
                Name = "Inactive Alpha",
                CustomerType = CustomerType.CashCod,
                IsActive = false
            };
            var active = new Customer
            {
                CustomerCode = "BETA",
                Name = "Active Beta",
                CustomerType = CustomerType.Account
            };
            db.Customers.AddRange(inactive, active);
            (await db.ContainerTypes.SingleAsync(x => x.Id == 2)).IsActive = false;
            await db.SaveChangesAsync();
            inactiveCustomerId = inactive.Id;
            activeCustomerId = active.Id;

            db.BinMovements.AddRange(
                new BinMovement
                {
                    CustomerId = inactive.Id,
                    ContainerTypeId = 2,
                    MovementDate = date,
                    MovementType = MovementType.In,
                    Source = MovementSource.ExcelImport,
                    Quantity = 2,
                    ReferenceNumber = "excel-ref",
                    Notes = "excel-notes",
                    CreatedBy = "import-user",
                    CreatedUtc = recordedUtc
                },
                new BinMovement
                {
                    CustomerId = inactive.Id,
                    ContainerTypeId = 2,
                    MovementDate = date,
                    MovementType = MovementType.Out,
                    Source = MovementSource.Adjustment,
                    Quantity = 4,
                    CreatedBy = "opening-user",
                    CreatedUtc = recordedUtc.AddMinutes(1)
                },
                new BinMovement
                {
                    CustomerId = active.Id,
                    ContainerTypeId = 3,
                    MovementDate = date,
                    MovementType = MovementType.Out,
                    Source = MovementSource.Manual,
                    Quantity = 5,
                    ReferenceNumber = null,
                    Notes = null,
                    CreatedBy = null,
                    CreatedUtc = recordedUtc.AddMinutes(2)
                });
            await db.SaveChangesAsync();
        }

        var service = scope.ServiceProvider
            .GetRequiredService<IDailyMovementsReportService>();

        var physical = await service.QueryAsync(new(date));
        var imported = Assert.Single(physical.Rows,
            x => x.Source == MovementSource.ExcelImport);
        Assert.Equal((inactiveCustomerId, "ALPHA", "Inactive Alpha", CustomerType.CashCod),
            (imported.CustomerId, imported.CustomerCode, imported.CustomerName,
                imported.CustomerType));
        Assert.Equal((2, "Small Bin", 2, "excel-ref", "excel-notes", "import-user", recordedUtc),
            (imported.ContainerTypeId, imported.ContainerType, imported.ContainerDisplayOrder,
                imported.Reference, imported.Notes, imported.EnteredBy, imported.RecordedUtc));
        Assert.DoesNotContain(physical.Rows,
            x => x.Source == MovementSource.Adjustment);
        Assert.Equal(new[] { MovementSource.ExcelImport, MovementSource.Manual },
            physical.Rows.Select(x => x.Source));

        var withAdjustments = await service.QueryAsync(new(date, IncludeAdjustments: true));
        Assert.Equal(
            new[]
            {
                (inactiveCustomerId, MovementType.In, MovementSource.ExcelImport),
                (inactiveCustomerId, MovementType.Out, MovementSource.Adjustment),
                (activeCustomerId, MovementType.Out, MovementSource.Manual)
            },
            withAdjustments.Rows.Select(x => (x.CustomerId, x.Direction, x.Source)));
        Assert.Equal((9, 2),
            (withAdjustments.OutQuantity, withAdjustments.InQuantity));
        Assert.Equal(
            new[]
            {
                (2, "Small Bin", 2, 4, 2),
                (3, "Yellow Bin", 3, 5, 0)
            },
            withAdjustments.ContainerTotals.Select(x =>
                (x.ContainerTypeId, x.ContainerType, x.DisplayOrder,
                    x.OutQuantity, x.InQuantity)));

        Assert.Empty((await service.QueryAsync(new(date,
            Source: MovementSource.Adjustment))).Rows);
        Assert.Single((await service.QueryAsync(new(date,
            Source: MovementSource.Adjustment,
            IncludeAdjustments: true))).Rows);
        Assert.Single((await service.QueryAsync(new(date,
            Source: MovementSource.ExcelImport))).Rows);
    }

    [Fact]
    public async Task Daily_query_fails_closed_when_integer_totals_overflow()
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
        var date = new DateOnly(2026, 8, 15);

        await using (var db = await factory.CreateDbContextAsync())
        {
            await db.Database.EnsureCreatedAsync();
            await DatabaseSetup.InitializeSqliteAsync(db);
            var customer = new Customer
            {
                CustomerCode = "OVERFLOW",
                Name = "Overflow Customer"
            };
            db.Customers.Add(customer);
            await db.SaveChangesAsync();
            db.BinMovements.AddRange(
                Movement(customer.Id, 1, date, MovementType.Out,
                    MovementSource.Manual, int.MaxValue),
                Movement(customer.Id, 1, date, MovementType.Out,
                    MovementSource.Manual, 1));
            await db.SaveChangesAsync();
        }

        var service = scope.ServiceProvider
            .GetRequiredService<IDailyMovementsReportService>();

        await Assert.ThrowsAsync<OverflowException>(() =>
            service.QueryAsync(new(date)));
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
