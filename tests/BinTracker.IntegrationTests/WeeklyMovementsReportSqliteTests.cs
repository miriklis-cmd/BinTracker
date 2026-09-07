using BinTracker.Core;
using BinTracker.Data;
using BinTracker.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BinTracker.IntegrationTests;

public sealed class WeeklyMovementsReportSqliteTests
{
    [Fact]
    public async Task Selected_week_is_monday_to_sunday_and_excludes_adjustments_by_default()
    {
        await using var connection=new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var services=new ServiceCollection();
        services.AddDbContextFactory<BinTrackerDbContext>(o=>o.UseSqlite(connection));
        services.AddBinTrackerServices();
        await using var provider=services.BuildServiceProvider();
        await using var scope=provider.CreateAsyncScope();
        var factory=scope.ServiceProvider.GetRequiredService<IDbContextFactory<BinTrackerDbContext>>();

        await using(var db=await factory.CreateDbContextAsync())
        {
            await db.Database.EnsureCreatedAsync();
            await DatabaseSetup.InitializeSqliteAsync(db);
            var c=new Customer{CustomerCode="CLAMMS",Name="Clamms",CustomerType=CustomerType.Account};
            db.Customers.Add(c); await db.SaveChangesAsync();
            db.BinMovements.AddRange(
                M(c.Id,1,new DateOnly(2026,8,10),MovementType.Out,MovementSource.Batch,9),
                M(c.Id,1,new DateOnly(2026,8,16),MovementType.In,MovementSource.Manual,4),
                M(c.Id,1,new DateOnly(2026,8,12),MovementType.Out,MovementSource.Adjustment,100),
                M(c.Id,1,new DateOnly(2026,8,17),MovementType.Out,MovementSource.Batch,50));
            await db.SaveChangesAsync();
        }

        var service=scope.ServiceProvider.GetRequiredService<IWeeklyMovementsReportService>();
        var result=await service.QueryAsync(new WeeklyMovementsReportQuery(new DateOnly(2026,8,13)));

        Assert.Equal(new DateOnly(2026,8,10),result.WeekStart);
        Assert.Equal(new DateOnly(2026,8,16),result.WeekEnd);
        Assert.Equal(2,result.Rows.Count);
        Assert.Equal(9,result.OutQuantity);
        Assert.Equal(4,result.InQuantity);
        Assert.Equal(5,result.NetQuantity);
        var summary=Assert.Single(result.Summary);
        Assert.Equal(9,summary.OutQuantity);
        Assert.Equal(4,summary.InQuantity);
        Assert.Equal(5,summary.NetQuantity);
    }

    [Fact]
    public async Task Filters_customer_container_and_source()
    {
        await using var connection=new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var services=new ServiceCollection();
        services.AddDbContextFactory<BinTrackerDbContext>(o=>o.UseSqlite(connection));
        services.AddBinTrackerServices();
        await using var provider=services.BuildServiceProvider();
        await using var scope=provider.CreateAsyncScope();
        var factory=scope.ServiceProvider.GetRequiredService<IDbContextFactory<BinTrackerDbContext>>();

        await using(var db=await factory.CreateDbContextAsync())
        {
            await db.Database.EnsureCreatedAsync();
            await DatabaseSetup.InitializeSqliteAsync(db);
            var a=new Customer{CustomerCode="AEGIR",Name="Aegir",CustomerType=CustomerType.Account};
            var c=new Customer{CustomerCode="CLAMMS",Name="Clamms",CustomerType=CustomerType.Account};
            db.Customers.AddRange(a,c); await db.SaveChangesAsync();
            db.BinMovements.AddRange(
                M(c.Id,3,new DateOnly(2026,8,11),MovementType.Out,MovementSource.Batch,43),
                M(c.Id,1,new DateOnly(2026,8,11),MovementType.In,MovementSource.Manual,3),
                M(a.Id,3,new DateOnly(2026,8,11),MovementType.Out,MovementSource.Batch,2));
            await db.SaveChangesAsync();
        }

        var service=scope.ServiceProvider.GetRequiredService<IWeeklyMovementsReportService>();
        var result=await service.QueryAsync(new WeeklyMovementsReportQuery(
            new DateOnly(2026,8,11),"clam",3,MovementSource.Batch));

        var row=Assert.Single(result.Rows);
        Assert.Equal("CLAMMS",row.CustomerCode);
        Assert.Equal(43,row.Quantity);
    }


    [Fact]
    public async Task Future_selected_date_is_clamped_to_current_week_and_future_movements_are_excluded()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var services = new ServiceCollection();
        services.AddDbContextFactory<BinTrackerDbContext>(
            o => o.UseSqlite(connection));
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
                CustomerCode = "FUTURE",
                Name = "Future Test",
                CustomerType = CustomerType.Account
            };

            db.Customers.Add(customer);
            await db.SaveChangesAsync();

            db.BinMovements.AddRange(
                M(customer.Id, 1, today,
                    MovementType.Out, MovementSource.Batch, 2),
                M(customer.Id, 1, tomorrow,
                    MovementType.Out, MovementSource.Batch, 99));

            await db.SaveChangesAsync();
        }

        var service = scope.ServiceProvider
            .GetRequiredService<IWeeklyMovementsReportService>();

        var result = await service.QueryAsync(
            new WeeklyMovementsReportQuery(today.AddDays(14)));

        var expectedWeekStart =
            today.AddDays(-(((int)today.DayOfWeek + 6) % 7));

        Assert.Equal(
            expectedWeekStart,
            result.WeekStart);

        Assert.Equal(today, result.DataThroughDate);

        var row = Assert.Single(result.Rows);
        Assert.Equal(2, row.Quantity);
        Assert.DoesNotContain(result.Rows, x => x.MovementDate > today);
    }

    [Fact]
    public async Task Weekly_query_preserves_historical_metadata_order_summary_and_source_distinctions()
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
        var monday = new DateOnly(2026, 8, 10);

        int inactiveCustomerId;
        int activeCustomerId;
        long firstInactiveMovementId;
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

            var firstInactive = Movement(inactive.Id, 2, monday,
                MovementType.In, MovementSource.ExcelImport, 2);
            firstInactive.ReferenceNumber = "excel-ref";
            firstInactive.Notes = "excel-notes";
            firstInactive.CreatedBy = "import-user";
            db.BinMovements.AddRange(
                firstInactive,
                Movement(inactive.Id, 2, monday.AddDays(1),
                    MovementType.Out, MovementSource.Adjustment, 4),
                Movement(inactive.Id, 2, monday.AddDays(6),
                    MovementType.Out, MovementSource.Batch, 3),
                Movement(active.Id, 3, monday.AddDays(1),
                    MovementType.Out, MovementSource.Manual, 5));
            await db.SaveChangesAsync();
            firstInactiveMovementId = firstInactive.Id;
        }

        var service = scope.ServiceProvider
            .GetRequiredService<IWeeklyMovementsReportService>();

        var physical = await service.QueryAsync(new(monday));
        Assert.Equal(
            new[]
            {
                (inactiveCustomerId, MovementSource.ExcelImport),
                (activeCustomerId, MovementSource.Manual),
                (inactiveCustomerId, MovementSource.Batch)
            },
            physical.Rows.Select(x => (x.CustomerId, x.Source)));
        var imported = Assert.Single(physical.Rows,
            x => x.MovementId == firstInactiveMovementId);
        Assert.Equal(
            (monday, inactiveCustomerId, "ALPHA", "Inactive Alpha",
                CustomerType.CashCod, 2, "Small Bin", 2, MovementType.In,
                2, MovementSource.ExcelImport, "excel-ref", "excel-notes", "import-user"),
            (imported.MovementDate, imported.CustomerId, imported.CustomerCode,
                imported.CustomerName, imported.CustomerType, imported.ContainerTypeId,
                imported.ContainerType, imported.ContainerDisplayOrder, imported.Direction,
                imported.Quantity, imported.Source, imported.Reference, imported.Notes,
                imported.EnteredBy));

        var withAdjustments = await service.QueryAsync(new(monday,
            IncludeAdjustments: true));
        Assert.Equal((12, 2, 10),
            (withAdjustments.OutQuantity, withAdjustments.InQuantity,
                withAdjustments.NetQuantity));
        Assert.Equal(
            new[]
            {
                (inactiveCustomerId, "ALPHA", 2, "Small Bin", 2, 7, 2, 5),
                (activeCustomerId, "BETA", 3, "Yellow Bin", 3, 5, 0, 5)
            },
            withAdjustments.Summary.Select(x =>
                (x.CustomerId, x.CustomerCode, x.ContainerTypeId, x.ContainerType,
                    x.ContainerDisplayOrder, x.OutQuantity, x.InQuantity, x.NetQuantity)));

        Assert.Empty((await service.QueryAsync(new(monday,
            Source: MovementSource.Adjustment))).Rows);
        Assert.Single((await service.QueryAsync(new(monday,
            Source: MovementSource.Adjustment,
            IncludeAdjustments: true))).Rows);
        Assert.Single((await service.QueryAsync(new(monday,
            Source: MovementSource.ExcelImport))).Rows);
        Assert.Equal(2, (await service.QueryAsync(new(monday,
            CustomerSearch: "  inactive alpha  ",
            ContainerTypeId: 2,
            Source: MovementSource.ExcelImport))).InQuantity);
    }

    [Fact]
    public async Task Weekly_query_fails_closed_when_integer_summary_totals_overflow()
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
        var monday = new DateOnly(2026, 8, 10);

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
                Movement(customer.Id, 1, monday, MovementType.Out,
                    MovementSource.Manual, int.MaxValue),
                Movement(customer.Id, 1, monday.AddDays(1), MovementType.Out,
                    MovementSource.Manual, 1));
            await db.SaveChangesAsync();
        }

        var service = scope.ServiceProvider
            .GetRequiredService<IWeeklyMovementsReportService>();

        await Assert.ThrowsAsync<OverflowException>(() =>
            service.QueryAsync(new(monday)));
    }

    private static BinMovement M(int customerId,int containerId,DateOnly date,
        MovementType type,MovementSource source,int qty)=>new()
    {
        CustomerId=customerId,ContainerTypeId=containerId,MovementDate=date,
        MovementType=type,Source=source,Quantity=qty,CreatedBy="test",CreatedUtc=DateTime.UtcNow
    };

    private static BinMovement Movement(
        int customerId,
        int containerTypeId,
        DateOnly date,
        MovementType type,
        MovementSource source,
        int quantity) =>
        M(customerId, containerTypeId, date, type, source, quantity);
}
