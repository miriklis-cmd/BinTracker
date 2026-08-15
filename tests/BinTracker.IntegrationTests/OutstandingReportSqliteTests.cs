using BinTracker.Core;
using BinTracker.Data;
using BinTracker.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BinTracker.IntegrationTests;

public sealed class OutstandingReportSqliteTests
{
    [Fact]
    public async Task As_of_date_excludes_future_movements_and_keeps_containers_separate()
    {
        await using var scope =
            await CreateScopeAsync();

        var factory = scope.ServiceProvider
            .GetRequiredService<
                IDbContextFactory<BinTrackerDbContext>>();

        await using (var db =
                     await factory.CreateDbContextAsync())
        {
            var customer = new Customer
            {
                CustomerCode = "AEGIR",
                Name = "AEGIR",
                CustomerType = CustomerType.Account,
                IsActive = true
            };

            db.Customers.Add(customer);
            await db.SaveChangesAsync();

            db.BinMovements.AddRange(
                // Blue: 10 OUT - 3 IN = 7 as at 10 Aug.
                new BinMovement
                {
                    MovementDate = new DateOnly(2026, 8, 5),
                    MovementType = MovementType.Out,
                    Source = MovementSource.Manual,
                    CustomerId = customer.Id,
                    ContainerTypeId = 1,
                    Quantity = 10
                },
                new BinMovement
                {
                    MovementDate = new DateOnly(2026, 8, 10),
                    MovementType = MovementType.In,
                    Source = MovementSource.Manual,
                    CustomerId = customer.Id,
                    ContainerTypeId = 1,
                    Quantity = 3
                },
                // Yellow is separate.
                new BinMovement
                {
                    MovementDate = new DateOnly(2026, 8, 9),
                    MovementType = MovementType.Out,
                    Source = MovementSource.Manual,
                    CustomerId = customer.Id,
                    ContainerTypeId = 3,
                    Quantity = 4
                },
                // Future relative to the report date; must not appear.
                new BinMovement
                {
                    MovementDate = new DateOnly(2026, 8, 11),
                    MovementType = MovementType.Out,
                    Source = MovementSource.Manual,
                    CustomerId = customer.Id,
                    ContainerTypeId = 1,
                    Quantity = 100
                });

            await db.SaveChangesAsync();
        }

        var service = scope.ServiceProvider
            .GetRequiredService<IOutstandingReportService>();

        var result = await service.QueryAsync(
            new OutstandingReportQuery(
                new DateOnly(2026, 8, 10)));

        Assert.Equal(2, result.Rows.Count);

        var blue = Assert.Single(
            result.Rows,
            x => x.ContainerTypeId == 1);
        Assert.Equal(7, blue.Balance);
        Assert.Equal(
            new DateOnly(2026, 8, 10),
            blue.LastMovementDate);

        var yellow = Assert.Single(
            result.Rows,
            x => x.ContainerTypeId == 3);
        Assert.Equal(4, yellow.Balance);

        Assert.Equal(
            new[] { 1, 3 },
            result.Rows
                .Where(x => x.CustomerCode == "AEGIR")
                .Select(x => x.ContainerTypeId)
                .ToArray());

        Assert.DoesNotContain(
            result.Rows,
            x => x.Balance == 107);
    }

    [Fact]
    public async Task Credits_are_hidden_by_default_and_optional_when_requested()
    {
        await using var scope =
            await CreateScopeAsync();

        var factory = scope.ServiceProvider
            .GetRequiredService<
                IDbContextFactory<BinTrackerDbContext>>();

        await using (var db =
                     await factory.CreateDbContextAsync())
        {
            var customer = new Customer
            {
                CustomerCode = "CREDIT",
                Name = "Credit Customer",
                CustomerType = CustomerType.CashCod,
                IsActive = true
            };

            db.Customers.Add(customer);
            await db.SaveChangesAsync();

            db.BinMovements.Add(new BinMovement
            {
                MovementDate = new DateOnly(2026, 8, 10),
                MovementType = MovementType.In,
                Source = MovementSource.Manual,
                CustomerId = customer.Id,
                ContainerTypeId = 1,
                Quantity = 5
            });
            await db.SaveChangesAsync();
        }

        var service = scope.ServiceProvider
            .GetRequiredService<IOutstandingReportService>();

        var defaultResult = await service.QueryAsync(
            new OutstandingReportQuery(
                new DateOnly(2026, 8, 10)));

        Assert.Empty(defaultResult.Rows);

        var withCredits = await service.QueryAsync(
            new OutstandingReportQuery(
                new DateOnly(2026, 8, 10),
                IncludeCredits: true));

        var row = Assert.Single(withCredits.Rows);
        Assert.Equal(-5, row.Balance);
        Assert.Equal("5 CREDIT", row.PositionText);
    }

    [Fact]
    public async Task Search_container_and_inactive_filters_are_applied()
    {
        await using var scope =
            await CreateScopeAsync();

        var factory = scope.ServiceProvider
            .GetRequiredService<
                IDbContextFactory<BinTrackerDbContext>>();

        await using (var db =
                     await factory.CreateDbContextAsync())
        {
            var active = new Customer
            {
                CustomerCode = "ALPHA",
                Name = "Alpha Customer",
                CustomerType = CustomerType.Account,
                IsActive = true
            };
            var inactive = new Customer
            {
                CustomerCode = "BETA",
                Name = "Beta Customer",
                CustomerType = CustomerType.Account,
                IsActive = false
            };

            db.Customers.AddRange(active, inactive);
            await db.SaveChangesAsync();

            db.BinMovements.AddRange(
                new BinMovement
                {
                    MovementDate = new DateOnly(2026, 8, 1),
                    MovementType = MovementType.Out,
                    Source = MovementSource.Manual,
                    CustomerId = active.Id,
                    ContainerTypeId = 1,
                    Quantity = 2
                },
                new BinMovement
                {
                    MovementDate = new DateOnly(2026, 8, 1),
                    MovementType = MovementType.Out,
                    Source = MovementSource.Manual,
                    CustomerId = active.Id,
                    ContainerTypeId = 3,
                    Quantity = 3
                },
                new BinMovement
                {
                    MovementDate = new DateOnly(2026, 8, 1),
                    MovementType = MovementType.Out,
                    Source = MovementSource.Manual,
                    CustomerId = inactive.Id,
                    ContainerTypeId = 1,
                    Quantity = 4
                });

            await db.SaveChangesAsync();
        }

        var service = scope.ServiceProvider
            .GetRequiredService<IOutstandingReportService>();

        var result = await service.QueryAsync(
            new OutstandingReportQuery(
                new DateOnly(2026, 8, 2),
                CustomerSearch: "alp",
                ContainerTypeId: 1,
                IncludeInactiveCustomers: false));

        var row = Assert.Single(result.Rows);
        Assert.Equal("ALPHA", row.CustomerCode);
        Assert.Equal(1, row.ContainerTypeId);
        Assert.Equal(2, row.Balance);
    }

    private static async Task<AsyncServiceScope> CreateScopeAsync()
    {
        var connection =
            new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var services = new ServiceCollection();
        services.AddDbContextFactory<BinTrackerDbContext>(
            options => options.UseSqlite(connection));
        services.AddBinTrackerServices();

        var provider = services.BuildServiceProvider();
        var scope = provider.CreateAsyncScope();

        var factory = scope.ServiceProvider
            .GetRequiredService<
                IDbContextFactory<BinTrackerDbContext>>();

        await using var db =
            await factory.CreateDbContextAsync();

        await db.Database.EnsureCreatedAsync();
        await DatabaseSetup.InitializeSqliteAsync(db);

        // Keep the connection/provider alive for the lifetime of the scope.
        scope.ServiceProvider.GetRequiredService<UserSession>();

        return scope;
    }
}
