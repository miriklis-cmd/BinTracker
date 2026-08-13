
using BinTracker.Core;
using BinTracker.Data;
using BinTracker.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BinTracker.IntegrationTests;

public sealed class BalanceServiceSqliteTests
{
    [Fact]
    public async Task GetBalancesAsync_aggregates_real_sqlite_rows_without_translation_failure()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var services = new ServiceCollection();

        services.AddDbContextFactory<BinTrackerDbContext>(options =>
            options.UseSqlite(connection));

        services.AddBinTrackerServices();

        await using var provider = services.BuildServiceProvider();

        await using (var scope = provider.CreateAsyncScope())
        {
            var factory = scope.ServiceProvider
                .GetRequiredService<IDbContextFactory<BinTrackerDbContext>>();

            await using var db = await factory.CreateDbContextAsync();
            await db.Database.EnsureCreatedAsync();

            var customerA = new Customer
            {
                Name = "Clamms Seafood",
                CustomerCode = "CLAMMS",
                CustomerType = CustomerType.Account
            };

            var customerB = new Customer
            {
                Name = "S & J",
                CustomerCode = "S & J",
                CustomerType = CustomerType.Account
            };

            var unrelatedCustomer = new Customer
            {
                Name = "No Movements Customer",
                CustomerCode = "NO-MOVE",
                CustomerType = CustomerType.Account
            };

            db.Customers.AddRange(customerA, customerB, unrelatedCustomer);
            await db.SaveChangesAsync();

            db.BinMovements.AddRange(
                new BinMovement
                {
                    MovementDate = new DateOnly(2026, 8, 12),
                    MovementType = MovementType.Out,
                    Source = MovementSource.Manual,
                    CustomerId = customerA.Id,
                    ContainerTypeId = 3, // Yellow Bin seed
                    Quantity = 20
                },
                new BinMovement
                {
                    MovementDate = new DateOnly(2026, 8, 12),
                    MovementType = MovementType.In,
                    Source = MovementSource.Manual,
                    CustomerId = customerA.Id,
                    ContainerTypeId = 3,
                    Quantity = 3
                },
                new BinMovement
                {
                    MovementDate = new DateOnly(2026, 8, 12),
                    MovementType = MovementType.Out,
                    Source = MovementSource.Manual,
                    CustomerId = customerB.Id,
                    ContainerTypeId = 4, // Bulk Bin seed
                    Quantity = 7
                });

            await db.SaveChangesAsync();
        }

        await using (var scope = provider.CreateAsyncScope())
        {
            var balances = scope.ServiceProvider
                .GetRequiredService<IBalanceService>();

            var rows = await balances.GetBalancesAsync();

            Assert.Equal(2, rows.Count);

            var clamms = Assert.Single(
                rows,
                x =>
                    x.CustomerName == "Clamms Seafood" &&
                    x.ContainerTypeName == "Yellow Bin");

            Assert.Equal(17, clamms.Balance);

            var sj = Assert.Single(
                rows,
                x =>
                    x.CustomerName == "S & J" &&
                    x.ContainerTypeName == "Bulk Bin");

            Assert.Equal(7, sj.Balance);

            Assert.DoesNotContain(rows, x =>
                x.CustomerName == "No Movements Customer");
        }
    }

    [Fact]
    public async Task GetBalancesAsync_returns_empty_list_when_there_are_no_movements()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var services = new ServiceCollection();

        services.AddDbContextFactory<BinTrackerDbContext>(options =>
            options.UseSqlite(connection));

        services.AddBinTrackerServices();

        await using var provider = services.BuildServiceProvider();

        await using (var scope = provider.CreateAsyncScope())
        {
            var factory = scope.ServiceProvider
                .GetRequiredService<IDbContextFactory<BinTrackerDbContext>>();

            await using var db = await factory.CreateDbContextAsync();
            await db.Database.EnsureCreatedAsync();
        }

        await using (var scope = provider.CreateAsyncScope())
        {
            var balances = scope.ServiceProvider
                .GetRequiredService<IBalanceService>();

            var rows = await balances.GetBalancesAsync();

            Assert.Empty(rows);
        }
    }
}
