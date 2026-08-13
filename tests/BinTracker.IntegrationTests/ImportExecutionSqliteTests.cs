using BinTracker.Core;
using BinTracker.Data;
using BinTracker.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BinTracker.IntegrationTests;

public sealed class ImportExecutionSqliteTests
{
    [Fact]
    public async Task ExecuteAsync_creates_customer_and_atomic_cutover_movements()
    {
        var temp = Path.Combine(
            Path.GetTempPath(),
            $"bintracker-import-{Guid.NewGuid():N}.xlsx");

        await File.WriteAllBytesAsync(
            temp,
            "not-a-real-workbook-but-valid-fingerprint-input"u8.ToArray());

        try
        {
            await using var connection =
                new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();

            var services = new ServiceCollection();
            services.AddDbContextFactory<BinTrackerDbContext>(
                options => options.UseSqlite(connection));
            services.AddBinTrackerServices();

            await using var provider =
                services.BuildServiceProvider();

            await using var scope =
                provider.CreateAsyncScope();

            var factory = scope.ServiceProvider
                .GetRequiredService<IDbContextFactory<BinTrackerDbContext>>();

            await using (var db =
                await factory.CreateDbContextAsync())
            {
                await db.Database.EnsureCreatedAsync();

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
                await db.SaveChangesAsync();

                scope.ServiceProvider
                    .GetRequiredService<UserSession>()
                    .SignIn(admin);
            }

            var service = scope.ServiceProvider
                .GetRequiredService<IImportExecutionService>();

            var preflight = await service.PreflightAsync(temp);

            var analysis = Analysis(
                new ImportSnapshotCandidate(
                    "Update Account",
                    "NewCo",
                    CustomerType.Account,
                    null,
                    Out: 2,
                    In: 1,
                    BroughtForward: 10,
                    ExcelTotal: 11,
                    SourceRow: "12"));

            var result = await service.ExecuteAsync(
                new ImportExecutionRequest(
                    temp,
                    preflight.Source.Sha256,
                    analysis,
                    [
                        new ImportWorksheetMapping(
                            "Update Account",
                            ImportWorksheetRole.Source,
                            "")
                    ],
                    new Dictionary<string, int>(
                        StringComparer.OrdinalIgnoreCase),
                    new Dictionary<string, ImportCustomerDecision>(
                        StringComparer.OrdinalIgnoreCase)
                    {
                        ["NewCo"] = new(
                            "NewCo",
                            "New Company",
                            ImportCustomerDecisionAction.Create)
                    },
                    new Dictionary<string, ImportExistingCustomerDecision>(
                        StringComparer.OrdinalIgnoreCase),
                    new DateOnly(2026, 8, 13)));

            Assert.Equal(1, result.CreatedCustomers);
            Assert.Equal(1, result.OpeningAdjustmentMovements);
            Assert.Equal(1, result.OutMovements);
            Assert.Equal(1, result.InMovements);
            Assert.Equal(3, result.MovementCount);

            await using var verify =
                await factory.CreateDbContextAsync();

            var customer = await verify.Customers
                .SingleAsync(x => x.CustomerCode == "NEWCO");

            Assert.Equal("New Company", customer.Name);

            var movements = await verify.BinMovements
                .Where(x => x.CustomerId == customer.Id)
                .OrderBy(x => x.Id)
                .ToListAsync();

            Assert.Equal(3, movements.Count);
            Assert.Contains(
                movements,
                x =>
                    x.Source == MovementSource.Adjustment &&
                    x.MovementType == MovementType.Out &&
                    x.Quantity == 10);
            Assert.Contains(
                movements,
                x =>
                    x.Source == MovementSource.ExcelImport &&
                    x.MovementType == MovementType.Out &&
                    x.Quantity == 2);
            Assert.Contains(
                movements,
                x =>
                    x.Source == MovementSource.ExcelImport &&
                    x.MovementType == MovementType.In &&
                    x.Quantity == 1);

            var balance = movements.Sum(x =>
                x.MovementType == MovementType.Out
                    ? x.Quantity
                    : -x.Quantity);

            Assert.Equal(11, balance);

            var run = await verify.ImportRuns.SingleAsync();
            Assert.Equal("Completed", run.Status);
            Assert.Equal(1, run.CreatedCustomers);
            Assert.Equal(3, run.MovementCount);
            Assert.Equal(result.ImportRunId, run.Id);
        }
        finally
        {
            if (File.Exists(temp))
                File.Delete(temp);
        }
    }

    [Fact]
    public async Task Exact_completed_workbook_is_blocked_on_second_execute()
    {
        var temp = Path.Combine(
            Path.GetTempPath(),
            $"bintracker-import-{Guid.NewGuid():N}.xlsx");

        await File.WriteAllBytesAsync(
            temp,
            "same-source"u8.ToArray());

        try
        {
            await using var connection =
                new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();

            var services = new ServiceCollection();
            services.AddDbContextFactory<BinTrackerDbContext>(
                options => options.UseSqlite(connection));
            services.AddBinTrackerServices();

            await using var provider =
                services.BuildServiceProvider();
            await using var scope =
                provider.CreateAsyncScope();

            var factory = scope.ServiceProvider
                .GetRequiredService<IDbContextFactory<BinTrackerDbContext>>();

            await using (var db =
                await factory.CreateDbContextAsync())
            {
                await db.Database.EnsureCreatedAsync();

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
                await db.SaveChangesAsync();

                scope.ServiceProvider
                    .GetRequiredService<UserSession>()
                    .SignIn(admin);
            }

            var service = scope.ServiceProvider
                .GetRequiredService<IImportExecutionService>();

            var preflight = await service.PreflightAsync(temp);

            var request = new ImportExecutionRequest(
                temp,
                preflight.Source.Sha256,
                Analysis(
                    new ImportSnapshotCandidate(
                        "Update Account",
                        "NewCo",
                        CustomerType.Account,
                        null,
                        Out: 0,
                        In: 0,
                        BroughtForward: 1,
                        ExcelTotal: 1,
                        SourceRow: "12")),
                [
                    new ImportWorksheetMapping(
                        "Update Account",
                        ImportWorksheetRole.Source,
                        "")
                ],
                new Dictionary<string, int>(
                    StringComparer.OrdinalIgnoreCase),
                new Dictionary<string, ImportCustomerDecision>(
                    StringComparer.OrdinalIgnoreCase)
                {
                    ["NewCo"] = new(
                        "NewCo",
                        "New Company",
                        ImportCustomerDecisionAction.Create)
                },
                new Dictionary<string, ImportExistingCustomerDecision>(
                    StringComparer.OrdinalIgnoreCase),
                new DateOnly(2026, 8, 13));

            await service.ExecuteAsync(request);

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(
                () => service.ExecuteAsync(request));

            Assert.Contains(
                "already been imported",
                ex.Message,
                StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            if (File.Exists(temp))
                File.Delete(temp);
        }
    }

    private static ExcelImportAnalysis Analysis(
        params ImportSnapshotCandidate[] snapshots)
    {
        var customers = snapshots
            .Select(x => new ImportCustomerCandidate(
                x.Worksheet,
                x.CustomerCode,
                x.CustomerType,
                "A1"))
            .ToArray();

        return new ExcelImportAnalysis(
            "test.xlsx",
            "test.xlsx",
            [
                new ImportWorksheetAnalysis(
                    "Update Account",
                    10,
                    7,
                    1,
                    customers.Length,
                    "Detected")
            ],
            customers,
            snapshots,
            []);
    }
}
