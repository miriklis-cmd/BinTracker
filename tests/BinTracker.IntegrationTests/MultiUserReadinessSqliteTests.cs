using BinTracker.Core;
using BinTracker.Data;
using BinTracker.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BinTracker.IntegrationTests;

public sealed class MultiUserReadinessSqliteTests
{
    [Fact]
    public async Task Retried_single_movement_operation_does_not_duplicate_ledger_entry()
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
        var session = scope.ServiceProvider.GetRequiredService<UserSession>();

        int customerId;
        await using (var db = await factory.CreateDbContextAsync())
        {
            await db.Database.EnsureCreatedAsync();
            await DatabaseSetup.InitializeSqliteAsync(db);

            var user = new UserAccount
            {
                Username = "operator",
                DisplayName = "Operator",
                PasswordHash = "x",
                PasswordSalt = "x",
                Role = UserRole.Operator,
                IsActive = true
            };
            var customer = new Customer
            {
                CustomerCode = "RETRY",
                Name = "Retry Test"
            };

            db.AddRange(user, customer);
            await db.SaveChangesAsync();

            session.SignIn(user);
            customerId = customer.Id;
        }

        var operationId = Guid.NewGuid();
        var request = new SaveSingleMovementRequest(
            operationId,
            new DateOnly(2026, 8, 23),
            MovementType.Out,
            customerId,
            1,
            3,
            "retry-test",
            null);

        var service = scope.ServiceProvider.GetRequiredService<IMovementService>();

        var first = await service.SaveSingleAsync(request);
        var retry = await service.SaveSingleAsync(request);

        Assert.Equal(first.MovementId, retry.MovementId);

        await using var verify = await factory.CreateDbContextAsync();
        Assert.Equal(
            1,
            await verify.BinMovements.CountAsync(
                x => x.ClientOperationId == operationId));
    }

    [Fact]
    public async Task Stale_customer_edit_is_rejected_instead_of_overwriting_other_users_change()
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
        var session = scope.ServiceProvider.GetRequiredService<UserSession>();

        int customerId;
        await using (var db = await factory.CreateDbContextAsync())
        {
            await db.Database.EnsureCreatedAsync();
            await DatabaseSetup.InitializeSqliteAsync(db);

            var user = new UserAccount
            {
                Username = "operator",
                DisplayName = "Operator",
                PasswordHash = "x",
                PasswordSalt = "x",
                Role = UserRole.Operator,
                IsActive = true
            };
            var customer = new Customer
            {
                CustomerCode = "CONCUR",
                Name = "Concurrent Test"
            };

            db.AddRange(user, customer);
            await db.SaveChangesAsync();

            session.SignIn(user);
            customerId = customer.Id;
        }

        var service = scope.ServiceProvider.GetRequiredService<ICustomerService>();
        var firstEditor = await service.GetAsync(customerId);
        var secondEditor = await service.GetAsync(customerId);

        Assert.NotNull(firstEditor);
        Assert.NotNull(secondEditor);

        firstEditor!.Name = "Changed by first user";
        await service.SaveAsync(firstEditor);

        secondEditor!.Phone = "03 9999 9999";

        var error = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.SaveAsync(secondEditor));

        Assert.Contains("changed by another user", error.Message);

        var current = await service.GetAsync(customerId);
        Assert.Equal("Changed by first user", current!.Name);
        Assert.Null(current.Phone);
    }

    [Fact]
    public async Task Reused_single_movement_operation_id_with_different_payload_is_rejected()
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
        var session = scope.ServiceProvider.GetRequiredService<UserSession>();

        int customerId;
        await using (var db = await factory.CreateDbContextAsync())
        {
            await db.Database.EnsureCreatedAsync();
            await DatabaseSetup.InitializeSqliteAsync(db);

            var user = new UserAccount
            {
                Username = "operator-conflict",
                DisplayName = "Operator Conflict",
                PasswordHash = "x",
                PasswordSalt = "x",
                Role = UserRole.Operator,
                IsActive = true
            };
            var customer = new Customer
            {
                CustomerCode = "RETRY-CONFLICT",
                Name = "Retry Conflict Test"
            };

            db.AddRange(user, customer);
            await db.SaveChangesAsync();

            session.SignIn(user);
            customerId = customer.Id;
        }

        var operationId = Guid.NewGuid();
        var service = scope.ServiceProvider.GetRequiredService<IMovementService>();

        await service.SaveSingleAsync(
            new SaveSingleMovementRequest(
                operationId,
                new DateOnly(2026, 8, 23),
                MovementType.Out,
                customerId,
                1,
                3,
                "retry-conflict",
                null));

        var error = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.SaveSingleAsync(
                new SaveSingleMovementRequest(
                    operationId,
                    new DateOnly(2026, 8, 23),
                    MovementType.Out,
                    customerId,
                    1,
                    4,
                    "retry-conflict",
                    null)));

        Assert.Contains("different movement request", error.Message);
    }

    [Fact]
    public async Task Batch_retry_requires_the_same_payload()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var services = new ServiceCollection();
        services.AddDbContextFactory<BinTrackerDbContext>(
            options => options.UseSqlite(connection));
        services.AddBinTrackerServices();

        await using var provider = services.BuildServiceProvider();
        await using var scope = provider.CreateAsyncScope();
        var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<BinTrackerDbContext>>();
        var session = scope.ServiceProvider.GetRequiredService<UserSession>();

        int customerId;
        await using (var db = await factory.CreateDbContextAsync())
        {
            await db.Database.EnsureCreatedAsync();
            await DatabaseSetup.InitializeSqliteAsync(db);
            var user = new UserAccount
            {
                Username = "batch-operator", DisplayName = "Batch Operator",
                PasswordHash = "x", PasswordSalt = "x",
                Role = UserRole.Operator, IsActive = true
            };
            var customer = new Customer { CustomerCode = "BATCH-RETRY", Name = "Batch Retry" };
            db.AddRange(user, customer);
            await db.SaveChangesAsync();
            session.SignIn(user);
            customerId = customer.Id;
        }

        var operationId = Guid.NewGuid();
        var request = new SaveMovementBatchRequest(
            operationId, new DateOnly(2026, 8, 23), MovementType.Out, "batch",
            [new MovementBatchLine(customerId, 1, 3, "ref", "line")]);
        var service = scope.ServiceProvider.GetRequiredService<IMovementService>();

        var first = await service.SaveBatchAsync(request);
        var retry = await service.SaveBatchAsync(request);
        Assert.Equal(first, retry);

        var changed = request with
        {
            Lines = [new MovementBatchLine(customerId, 1, 4, "ref", "line")]
        };
        var conflict = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.SaveBatchAsync(changed));
        Assert.Contains("different batch request", conflict.Message);
    }


}
