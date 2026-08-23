using BinTracker.Core;
using BinTracker.Data;
using BinTracker.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BinTracker.IntegrationTests;

public sealed class ImportRunHistorySqliteTests
{
    [Fact]
    public async Task History_shows_replacement_chain_and_only_current_linked_movements()
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

            var admin = new UserAccount
            {
                Username = "admin",
                DisplayName = "Administrator",
                PasswordHash = "x",
                PasswordSalt = "x",
                Role = UserRole.Administrator,
                IsActive = true
            };
            db.UserAccounts.Add(admin);

            var customer = new Customer
            {
                CustomerCode = "AEGIR",
                Name = "AEGIR",
                CustomerType = CustomerType.Account
            };
            db.Customers.Add(customer);
            await db.SaveChangesAsync();

            scope.ServiceProvider
                .GetRequiredService<UserSession>()
                .SignIn(admin);

            var first = new ImportRun
            {
                SourceFileName = "original.xlsm",
                SourceClientPath = @"C:\original.xlsm",
                SourceSha256 = new string('A', 64),
                SourceLength = 100,
                SourceLastWriteUtc = DateTime.UtcNow.AddMinutes(-20),
                CutoverDate = new DateOnly(2026, 8, 14),
                StartedUtc = DateTime.UtcNow.AddMinutes(-15),
                CompletedUtc = DateTime.UtcNow.AddMinutes(-14),
                Status = "Replaced",
                CreatedCustomers = 160,
                MovementCount = 151,
                UserId = admin.Id,
                Username = admin.Username,
                SessionId = "first"
            };
            db.ImportRuns.Add(first);
            await db.SaveChangesAsync();

            var second = new ImportRun
            {
                SourceFileName = "corrected.xlsm",
                SourceClientPath = @"C:\corrected.xlsm",
                SourceSha256 = new string('B', 64),
                SourceLength = 101,
                SourceLastWriteUtc = DateTime.UtcNow.AddMinutes(-5),
                CutoverDate = new DateOnly(2026, 8, 14),
                CurrentCutoverDate = new DateOnly(2026, 8, 14),
                ReplacesImportRunId = first.Id,
                CorrectionChangesJson =
                    """
                    [
                      {
                        "CustomerId": 1,
                        "CustomerCode": "AEGIR",
                        "CustomerName": "AEGIR",
                        "ContainerTypeId": 1,
                        "ContainerType": "Blue Bin",
                        "PreviousNetEffect": 11,
                        "CorrectedNetEffect": 12
                      }
                    ]
                    """,
                StartedUtc = DateTime.UtcNow.AddMinutes(-4),
                CompletedUtc = DateTime.UtcNow.AddMinutes(-3),
                Status = "Completed",
                CreatedCustomers = 0,
                MovementCount = 3,
                UserId = admin.Id,
                Username = admin.Username,
                SessionId = "second",
                Notes = "Corrected two customer values."
            };
            db.ImportRuns.Add(second);
            await db.SaveChangesAsync();

            db.BinMovements.AddRange(
                new BinMovement
                {
                    MovementDate = new DateOnly(2026, 8, 14),
                    MovementType = MovementType.Out,
                    Source = MovementSource.Adjustment,
                    CustomerId = customer.Id,
                    ContainerTypeId = 1,
                    ImportRunId = second.Id,
                    Quantity = 5,
                    ReferenceNumber = $"IMPORT-{second.Id}",
                    CreatedBy = "admin"
                },
                new BinMovement
                {
                    MovementDate = new DateOnly(2026, 8, 14),
                    MovementType = MovementType.Out,
                    Source = MovementSource.ExcelImport,
                    CustomerId = customer.Id,
                    ContainerTypeId = 1,
                    ImportRunId = second.Id,
                    Quantity = 2,
                    ReferenceNumber = $"IMPORT-{second.Id}",
                    CreatedBy = "admin"
                },
                new BinMovement
                {
                    MovementDate = new DateOnly(2026, 8, 14),
                    MovementType = MovementType.In,
                    Source = MovementSource.ExcelImport,
                    CustomerId = customer.Id,
                    ContainerTypeId = 1,
                    ImportRunId = second.Id,
                    Quantity = 2,
                    ReferenceNumber = $"IMPORT-{second.Id}",
                    CreatedBy = "admin"
                });

            await db.SaveChangesAsync();
        }

        var history = scope.ServiceProvider
            .GetRequiredService<IImportRunHistoryService>();

        var rows = await history.GetRunsAsync();
        Assert.Equal(2, rows.Count);
        Assert.Equal(2, rows[0].Id);
        Assert.Equal("Completed", rows[0].Status);
        Assert.Equal(1, rows[0].ReplacesImportRunId);
        Assert.Equal("Replaced", rows[1].Status);

        var firstDetail = await history.GetRunAsync(1);
        Assert.NotNull(firstDetail);
        Assert.Equal(2, firstDetail!.ReplacedByImportRunId);
        Assert.Empty(firstDetail.Movements);

        var secondDetail = await history.GetRunAsync(2);
        Assert.NotNull(secondDetail);
        Assert.Equal(1, secondDetail!.ReplacesImportRunId);
        var correction = Assert.Single(secondDetail.CorrectionChanges);
        Assert.Equal("AEGIR", correction.CustomerCode);
        Assert.Equal("Blue Bin", correction.ContainerType);
        Assert.Equal(11, correction.PreviousNetEffect);
        Assert.Equal(12, correction.CorrectedNetEffect);
        Assert.Equal(1, correction.Difference);
        Assert.Equal(3, secondDetail.Movements.Count);
        Assert.All(
            secondDetail.Movements,
            x => Assert.Equal("IMPORT-2", x.ReferenceNumber));
        Assert.Contains(
            secondDetail.Movements,
            x => x.Source == "Opening adjustment");
        Assert.Contains(
            secondDetail.Movements,
            x => x.Source == "Excel import");
    }

    [Fact]
    public async Task History_requires_administrator()
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

            var user = new UserAccount
            {
                Username = "operator",
                DisplayName = "Operator",
                PasswordHash = "x",
                PasswordSalt = "x",
                Role = UserRole.Operator,
                IsActive = true
            };
            db.UserAccounts.Add(user);
            await db.SaveChangesAsync();

            scope.ServiceProvider
                .GetRequiredService<UserSession>()
                .SignIn(user);
        }

        var history = scope.ServiceProvider
            .GetRequiredService<IImportRunHistoryService>();

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => history.GetRunsAsync());
    }
}
