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
        var operationId = Guid.NewGuid();
        var request = new ReverseMovementRequest(operationId, movementId, "Incorrect dispatch");
        var result=await service.ReverseAsync(request);
        var retry = await service.ReverseAsync(request);
        Assert.Equal(result, retry);

        var payloadConflict = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.ReverseAsync(
                new ReverseMovementRequest(operationId, movementId, "Different reason")));
        Assert.Contains("different reversal request", payloadConflict.Message);

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

        var history = scope.ServiceProvider.GetRequiredService<IMovementHistoryReportService>();
        var displayed = await history.QueryAsync(new MovementHistoryReportQuery(
            new DateOnly(2026, 8, 1), new DateOnly(2026, 8, 31)));
        var originalRow = Assert.Single(displayed.Rows, x => x.MovementId == movementId);
        var reversalRow = Assert.Single(displayed.Rows, x => x.MovementId == reversal.Id);
        Assert.Equal($"Reversed — see REV-{movementId}", originalRow.Status);
        Assert.False(originalRow.CanReverse);
        Assert.Equal($"Reversal of #{movementId} — Incorrect dispatch", reversalRow.Status);
        Assert.Equal("Reversal", reversalRow.SourceText);
        Assert.False(reversalRow.CanReverse);
    }

    [Theory]
    [InlineData(MovementSource.Manual)]
    [InlineData(MovementSource.Batch)]
    public async Task Operator_can_reverse_ordinary_operational_movement(MovementSource source)
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

        long id;
        await using (var db = await factory.CreateDbContextAsync())
        {
            await db.Database.EnsureCreatedAsync();
            await DatabaseSetup.InitializeSqliteAsync(db);
            var user = new UserAccount
            {
                Username = "op", DisplayName = "Operator", PasswordHash = "x", PasswordSalt = "x",
                Role = UserRole.Operator, IsActive = true
            };
            var customer = new Customer { CustomerCode = "OP", Name = "Operator Test" };
            db.AddRange(user, customer);
            await db.SaveChangesAsync();
            session.SignIn(user);

            var movement = new BinMovement
            {
                MovementDate = DateOnly.FromDateTime(DateTime.Today),
                MovementType = MovementType.Out,
                Source = source,
                CustomerId = customer.Id,
                ContainerTypeId = 1,
                Quantity = 2,
                CreatedBy = "someone-else"
            };
            db.Add(movement);
            await db.SaveChangesAsync();
            id = movement.Id;
        }

        var service = scope.ServiceProvider.GetRequiredService<IMovementCorrectionService>();
        var result = await service.ReverseAsync(new ReverseMovementRequest(Guid.NewGuid(), id, "Operator correction"));

        await using var verify = await factory.CreateDbContextAsync();
        var original = await verify.BinMovements.AsNoTracking().SingleAsync(x => x.Id == id);
        var reversal = await verify.BinMovements.AsNoTracking().SingleAsync(x => x.Id == result.ReversalMovementId);
        Assert.Equal(id, reversal.ReversesMovementId);
        Assert.Equal(MovementType.In, reversal.MovementType);
        Assert.Equal(2, reversal.Quantity);
        Assert.Equal("op", reversal.CreatedBy);
        Assert.Equal(reversal.Id, original.CorrectedByMovementId);
    }

    [Theory]
    [InlineData(MovementSource.ExcelImport)]
    [InlineData(MovementSource.Adjustment)]
    public async Task Sensitive_sources_cannot_be_reversed_by_generic_workflow(MovementSource source)
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

        long id;
        await using (var db = await factory.CreateDbContextAsync())
        {
            await db.Database.EnsureCreatedAsync();
            await DatabaseSetup.InitializeSqliteAsync(db);
            var user = new UserAccount
            {
                Username = "admin", DisplayName = "Admin", PasswordHash = "x", PasswordSalt = "x",
                Role = UserRole.Administrator, IsActive = true
            };
            var customer = new Customer { CustomerCode = "SENSITIVE", Name = "Sensitive Test" };
            db.AddRange(user, customer);
            await db.SaveChangesAsync();
            session.SignIn(user);

            var movement = new BinMovement
            {
                MovementDate = DateOnly.FromDateTime(DateTime.Today),
                MovementType = MovementType.Out,
                Source = source,
                CustomerId = customer.Id,
                ContainerTypeId = 1,
                Quantity = 1,
                CreatedBy = "admin"
            };
            db.Add(movement);
            await db.SaveChangesAsync();
            id = movement.Id;
        }

        var service = scope.ServiceProvider.GetRequiredService<IMovementCorrectionService>();
        var error = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.ReverseAsync(new ReverseMovementRequest(Guid.NewGuid(), id, "Sensitive correction")));

        Assert.Contains(
            source == MovementSource.ExcelImport ? "Replace / Correct" : "Administrator-controlled",
            error.Message);
    }

    [Fact]
    public async Task Viewer_cannot_reverse_saved_movement()
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

        long id;
        await using (var db = await factory.CreateDbContextAsync())
        {
            await db.Database.EnsureCreatedAsync();
            await DatabaseSetup.InitializeSqliteAsync(db);
            var user = new UserAccount
            {
                Username = "viewer", DisplayName = "Viewer", PasswordHash = "x", PasswordSalt = "x",
                Role = UserRole.Viewer, IsActive = true
            };
            var customer = new Customer { CustomerCode = "VIEW", Name = "Viewer Test" };
            db.AddRange(user, customer);
            await db.SaveChangesAsync();
            session.SignIn(user);
            var movement = new BinMovement
            {
                MovementDate = DateOnly.FromDateTime(DateTime.Today),
                MovementType = MovementType.Out,
                Source = MovementSource.Manual,
                CustomerId = customer.Id,
                ContainerTypeId = 1,
                Quantity = 1
            };
            db.Add(movement);
            await db.SaveChangesAsync();
            id = movement.Id;
        }

        var service = scope.ServiceProvider.GetRequiredService<IMovementCorrectionService>();
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => service.ReverseAsync(new ReverseMovementRequest(Guid.NewGuid(), id, "Should fail")));
    }
}
