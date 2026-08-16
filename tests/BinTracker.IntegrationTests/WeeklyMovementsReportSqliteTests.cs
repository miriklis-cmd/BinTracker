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

    private static BinMovement M(int customerId,int containerId,DateOnly date,
        MovementType type,MovementSource source,int qty)=>new()
    {
        CustomerId=customerId,ContainerTypeId=containerId,MovementDate=date,
        MovementType=type,Source=source,Quantity=qty,CreatedBy="test",CreatedUtc=DateTime.UtcNow
    };
}
