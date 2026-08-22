using BinTracker.Core;
using BinTracker.Data;
using BinTracker.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BinTracker.IntegrationTests;

public sealed class MovementCorrectionSqliteTests
{
    [Fact]
    public async Task Administrator_reversal_preserves_original_and_creates_linked_opposite_movement_and_audit()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var services = new ServiceCollection();
        services.AddDbContextFactory<BinTrackerDbContext>(o => o.UseSqlite(connection));
        services.AddBinTrackerServices();
        await using var provider = services.BuildServiceProvider();
        await using var scope = provider.CreateAsyncScope();
        var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<BinTrackerDbContext>>();
        var session = scope.ServiceProvider.GetRequiredService<UserSession>();

        long movementId;
        await using (var db = await factory.CreateDbContextAsync())
        {
            await db.Database.EnsureCreatedAsync();
            await DatabaseSetup.InitializeSqliteAsync(db);
            var user = new UserAccount { Username="admin", DisplayName="Admin", PasswordHash="x", PasswordSalt="x", Role=UserRole.Administrator, IsActive=true };
            db.UserAccounts.Add(user);
            var customer = new Customer { CustomerCode="TEST", Name="Test", CustomerType=CustomerType.Account };
            db.Customers.Add(customer);
            await db.SaveChangesAsync();
            session.SignIn(user);
            var movement = new BinMovement { MovementDate=new DateOnly(2026,8,20), MovementType=MovementType.Out, Source=MovementSource.Manual, CustomerId=customer.Id, ContainerTypeId=1, Quantity=7, CreatedBy="admin" };
            db.BinMovements.Add(movement);
            await db.SaveChangesAsync();
            movementId=movement.Id;
        }

        var service=scope.ServiceProvider.GetRequiredService<IMovementCorrectionService>();
        var result=await service.ReverseAsync(new ReverseMovementRequest(movementId,"Incorrect dispatch"));

        await using var verify=await factory.CreateDbContextAsync();
        var original=await verify.BinMovements.AsNoTracking().SingleAsync(x=>x.Id==movementId);
        var reversal=await verify.BinMovements.AsNoTracking().SingleAsync(x=>x.Id==result.ReversalMovementId);
        Assert.Equal(MovementType.Out, original.MovementType);
        Assert.Equal(7, original.Quantity);
        Assert.Equal(reversal.Id, original.CorrectedByMovementId);
        Assert.Equal(movementId, reversal.ReversesMovementId);
        Assert.Equal(MovementType.In, reversal.MovementType);
        Assert.Equal(7, reversal.Quantity);
        Assert.Contains("Incorrect dispatch", reversal.CorrectionReason);
        Assert.True(await verify.AuditEvents.AnyAsync(x=>x.Action=="MOVEMENT_REVERSED" && x.EntityId==movementId.ToString()));
    }

    [Fact]
    public async Task Operator_cannot_reverse_saved_movement()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var services=new ServiceCollection();
        services.AddDbContextFactory<BinTrackerDbContext>(o=>o.UseSqlite(connection));
        services.AddBinTrackerServices();
        await using var provider=services.BuildServiceProvider();
        await using var scope=provider.CreateAsyncScope();
        var factory=scope.ServiceProvider.GetRequiredService<IDbContextFactory<BinTrackerDbContext>>();
        var session=scope.ServiceProvider.GetRequiredService<UserSession>();
        long id;
        await using(var db=await factory.CreateDbContextAsync()){
            await db.Database.EnsureCreatedAsync(); await DatabaseSetup.InitializeSqliteAsync(db);
            var user=new UserAccount{Username="op",DisplayName="Operator",PasswordHash="x",PasswordSalt="x",Role=UserRole.Operator,IsActive=true};
            var c=new Customer{CustomerCode="OP",Name="Operator Test"}; db.AddRange(user,c); await db.SaveChangesAsync(); session.SignIn(user);
            var m=new BinMovement{MovementDate=DateOnly.FromDateTime(DateTime.Today),MovementType=MovementType.Out,Source=MovementSource.Manual,CustomerId=c.Id,ContainerTypeId=1,Quantity=1};
            db.Add(m); await db.SaveChangesAsync(); id=m.Id;
        }
        var service=scope.ServiceProvider.GetRequiredService<IMovementCorrectionService>();
        await Assert.ThrowsAsync<UnauthorizedAccessException>(()=>service.ReverseAsync(new ReverseMovementRequest(id,"Should fail")));
    }
}
