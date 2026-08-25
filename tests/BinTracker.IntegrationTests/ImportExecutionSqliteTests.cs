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

            var preflight = await service.PreflightAsync(await SourceAsync(temp));

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
                    await SourceAsync(temp),
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
                    new DateOnly(2026, 8, 13),
                    ClientOperationId: Guid.NewGuid()));

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
            Assert.All(
                movements,
                movement =>
                    Assert.Equal(
                        result.ImportRunId,
                        movement.ImportRunId));

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
            var reconciliationJson = Assert.IsType<string>(
                run.OpeningReconciliationChangesJson);
            Assert.False(string.IsNullOrWhiteSpace(reconciliationJson));
            Assert.Contains(
                "\"PreviousBinTrackerBalance\":0",
                reconciliationJson);
            Assert.Contains(
                "\"ExcelBroughtForward\":10",
                reconciliationJson);
            Assert.Contains(
                "\"ExcelTarget\":11",
                reconciliationJson);
            Assert.Contains(
                "\"OpeningAdjustment\":10",
                reconciliationJson);
        }
        finally
        {
            if (File.Exists(temp))
                File.Delete(temp);
        }
    }

    [Fact]
    public async Task Forced_failure_after_database_save_rolls_back_entire_import()
    {
        var temp = Path.Combine(
            Path.GetTempPath(),
            $"bintracker-import-rollback-{Guid.NewGuid():N}.xlsx");

        await File.WriteAllBytesAsync(
            temp,
            "rollback-fingerprint-input"u8.ToArray());

        try
        {
            await using var connection =
                new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();

            var services = new ServiceCollection();

            services.AddDbContextFactory<BinTrackerDbContext>(
                options => options.UseSqlite(connection));

            services.AddBinTrackerServices();

            // Last registration wins for single-service resolution.
            services.AddSingleton<IImportExecutionFailureInjector>(
                new ThrowingImportExecutionFailureInjector(
                    ImportExecutionFailurePoint.AfterDatabaseSaveBeforeCommit));

            await using var provider =
                services.BuildServiceProvider();

            await using var scope =
                provider.CreateAsyncScope();

            var factory = scope.ServiceProvider
                .GetRequiredService<
                    IDbContextFactory<BinTrackerDbContext>>();

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

            await using (var baseline =
                await factory.CreateDbContextAsync())
            {
                Assert.Equal(0, await baseline.Customers.CountAsync());
                Assert.Equal(0, await baseline.BinMovements.CountAsync());
                Assert.Equal(0, await baseline.ImportRuns.CountAsync());
                Assert.DoesNotContain(
                    await baseline.AuditEvents.ToListAsync(),
                    x => x.Action == "EXCEL_IMPORT_COMPLETED");
            }

            var service = scope.ServiceProvider
                .GetRequiredService<IImportExecutionService>();

            var preflight =
                await service.PreflightAsync(await SourceAsync(temp));

            var request = new ImportExecutionRequest(
                await SourceAsync(temp),
                preflight.Source.Sha256,
                Analysis(
                    new ImportSnapshotCandidate(
                        "Update Account",
                        "RollbackCo",
                        CustomerType.Account,
                        null,
                        Out: 2,
                        In: 1,
                        BroughtForward: 10,
                        ExcelTotal: 11,
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
                    ["RollbackCo"] = new(
                        "RollbackCo",
                        "Rollback Company",
                        ImportCustomerDecisionAction.Create)
                },
                new Dictionary<
                    string,
                    ImportExistingCustomerDecision>(
                    StringComparer.OrdinalIgnoreCase),
                new DateOnly(2026, 8, 13),
                ClientOperationId: Guid.NewGuid());

            var ex =
                await Assert.ThrowsAsync<
                    ImportExecutionInjectedFailureException>(
                    () => service.ExecuteAsync(request));

            Assert.Equal(
                ImportExecutionFailurePoint
                    .AfterDatabaseSaveBeforeCommit,
                ex.Point);

            // The injected failure happens only after ImportRun, new customer,
            // movements and completion audit have all been SaveChanges'd into
            // the transaction. None may survive transaction disposal.
            await using var verify =
                await factory.CreateDbContextAsync();

            Assert.Equal(
                0,
                await verify.Customers.CountAsync());

            Assert.Equal(
                0,
                await verify.BinMovements.CountAsync());

            Assert.Equal(
                0,
                await verify.ImportRuns.CountAsync());

            Assert.DoesNotContain(
                await verify.AuditEvents.ToListAsync(),
                x => x.Action == "EXCEL_IMPORT_COMPLETED");

            // Because the failed run left no completed fingerprint behind,
            // preflight must still permit a retry of the exact same source.
            var retryPreflight =
                await service.PreflightAsync(await SourceAsync(temp));

            Assert.True(retryPreflight.CanProceed);
            Assert.False(
                retryPreflight.ExactWorkbookPreviouslyImported);
        }
        finally
        {
            if (File.Exists(temp))
                File.Delete(temp);
        }
    }

    [Fact]
    public async Task Import_provenance_is_relational_and_non_import_movements_remain_unlinked()
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
            .GetRequiredService<
                IDbContextFactory<BinTrackerDbContext>>();

        await using var db =
            await factory.CreateDbContextAsync();

        await db.Database.EnsureCreatedAsync();
        await DatabaseSetup.InitializeSqliteAsync(db);

        var customer = new Customer
        {
            CustomerCode = "MANUAL",
            Name = "Manual Customer",
            CustomerType = CustomerType.Account
        };

        db.Customers.Add(customer);
        await db.SaveChangesAsync();

        db.BinMovements.Add(new BinMovement
        {
            MovementDate = new DateOnly(2026, 8, 13),
            MovementType = MovementType.Out,
            Source = MovementSource.Manual,
            CustomerId = customer.Id,
            ContainerTypeId = 1,
            Quantity = 1,
            CreatedBy = "tester"
        });

        await db.SaveChangesAsync();

        var movement =
            await db.BinMovements
                .AsNoTracking()
                .SingleAsync();

        Assert.Null(movement.ImportRunId);
    }

    [Fact]
    public async Task Changed_workbook_same_cutover_replaces_only_prior_import_movements()
    {
        var firstFile = Path.Combine(
            Path.GetTempPath(),
            $"bintracker-replace-a-{Guid.NewGuid():N}.xlsx");
        var correctedFile = Path.Combine(
            Path.GetTempPath(),
            $"bintracker-replace-b-{Guid.NewGuid():N}.xlsx");

        await File.WriteAllBytesAsync(firstFile, "first-workbook"u8.ToArray());
        await File.WriteAllBytesAsync(correctedFile, "corrected-workbook"u8.ToArray());

        try
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
                await db.SaveChangesAsync();
                scope.ServiceProvider.GetRequiredService<UserSession>().SignIn(admin);
            }

            var service = scope.ServiceProvider.GetRequiredService<IImportExecutionService>();
            var cutover = new DateOnly(2026, 8, 14);

            var firstPreflight = await service.PreflightAsync(await SourceAsync(firstFile), cutover);
            var firstResult = await service.ExecuteAsync(
                new ImportExecutionRequest(
                    await SourceAsync(firstFile),
                    firstPreflight.Source.Sha256,
                    Analysis(new ImportSnapshotCandidate(
                        "Update Account", "ReplaceCo", CustomerType.Account, null,
                        Out: 1, In: 0, BroughtForward: 10, ExcelTotal: 11, SourceRow: "12")),
                    [new ImportWorksheetMapping("Update Account", ImportWorksheetRole.Source, "")],
                    new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase),
                    new Dictionary<string, ImportCustomerDecision>(StringComparer.OrdinalIgnoreCase)
                    {
                        ["ReplaceCo"] = new(
                            "ReplaceCo", "Replace Company",
                            ImportCustomerDecisionAction.Create)
                    },
                    new Dictionary<string, ImportExistingCustomerDecision>(StringComparer.OrdinalIgnoreCase),
                    cutover,
                    ClientOperationId: Guid.NewGuid()));

            int customerId;
            await using (var db = await factory.CreateDbContextAsync())
            {
                var customer = await db.Customers.SingleAsync(x => x.CustomerCode == "REPLACECO");
                customerId = customer.Id;

                db.BinMovements.AddRange(
                    new BinMovement
                    {
                        MovementDate = cutover,
                        MovementType = MovementType.Out,
                        Source = MovementSource.Manual,
                        CustomerId = customer.Id,
                        ContainerTypeId = 1,
                        Quantity = 2,
                        CreatedBy = "admin"
                    },
                    new BinMovement
                    {
                        MovementDate = cutover.AddDays(1),
                        MovementType = MovementType.Out,
                        Source = MovementSource.Manual,
                        CustomerId = customer.Id,
                        ContainerTypeId = 1,
                        Quantity = 4,
                        CreatedBy = "admin"
                    });
                await db.SaveChangesAsync();
            }

            // Step 4 UI must supply the cutover date to preflight. Without
            // it, the backend execution guard would reject the import later
            // but the UI could not surface Replace/Correct beforehand.
            var correctedPreflight =
                await service.PreflightAsync(
                    await SourceAsync(correctedFile),
                    cutover);

            Assert.True(correctedPreflight.CanProceed);
            Assert.True(correctedPreflight.RequiresReplacement);
            Assert.False(
                correctedPreflight.ExactWorkbookPreviouslyImported);
            Assert.Equal(
                firstResult.ImportRunId,
                correctedPreflight.PreviousCutoverRun!.ImportRunId);

            var correctedRequest = new ImportExecutionRequest(
                await SourceAsync(correctedFile),
                correctedPreflight.Source.Sha256,
                Analysis(new ImportSnapshotCandidate(
                    "Update Account", "ReplaceCo", CustomerType.Account, null,
                    Out: 2, In: 0, BroughtForward: 10, ExcelTotal: 12, SourceRow: "12")),
                [new ImportWorksheetMapping("Update Account", ImportWorksheetRole.Source, "")],
                new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase),
                new Dictionary<string, ImportCustomerDecision>(StringComparer.OrdinalIgnoreCase),
                new Dictionary<string, ImportExistingCustomerDecision>(StringComparer.OrdinalIgnoreCase)
                {
                    ["ReplaceCo"] = new ImportExistingCustomerDecision(
                        "ReplaceCo",
                        ImportExistingCustomerDecisionAction.AcceptMatch,
                        customerId,
                        "REPLACECO",
                        "Replace Company")
                },
                cutover,
                ImportExecutionMode.ReplacePreviousCutover,
                firstResult.ImportRunId,
                Guid.NewGuid());

            var comparison = await service.CompareReplacementAsync(correctedRequest);

            var difference = Assert.Single(comparison.Differences);
            Assert.Equal(1, comparison.ChangedPositionCount);
            Assert.Equal("REPLACECO", difference.CustomerCode);
            Assert.Equal("Blue Bin", difference.Container);
            Assert.Equal(11, difference.PreviousNetEffect);
            Assert.Equal(12, difference.ProposedNetEffect);
            Assert.Equal(1, difference.Difference);

            var correctedResult = await service.ExecuteAsync(correctedRequest);

            await using var verify = await factory.CreateDbContextAsync();

            var oldRun = await verify.ImportRuns.SingleAsync(x => x.Id == firstResult.ImportRunId);
            var newRun = await verify.ImportRuns.SingleAsync(x => x.Id == correctedResult.ImportRunId);

            Assert.Equal("Replaced", oldRun.Status);
            Assert.Null(oldRun.CurrentCutoverDate);
            Assert.Equal(cutover, newRun.CurrentCutoverDate);
            Assert.Equal(firstResult.ImportRunId, newRun.ReplacesImportRunId);
            Assert.False(string.IsNullOrWhiteSpace(newRun.CorrectionChangesJson));
            Assert.Contains(
                "\"Difference\":1",
                newRun.CorrectionChangesJson);

            Assert.Equal(
                0,
                await verify.BinMovements.CountAsync(
                    x => x.ImportRunId == firstResult.ImportRunId));

            Assert.True(
                await verify.BinMovements.AnyAsync(
                    x => x.ImportRunId == correctedResult.ImportRunId));

            var manualMovements = await verify.BinMovements
                .Where(x => x.Source == MovementSource.Manual)
                .OrderBy(x => x.MovementDate)
                .ToListAsync();

            Assert.Equal(2, manualMovements.Count);
            Assert.All(
                manualMovements,
                x => Assert.Null(x.ImportRunId));
            Assert.Equal(2, manualMovements[0].Quantity);
            Assert.Equal(4, manualMovements[1].Quantity);

            var finalBalance = await verify.BinMovements
                .Where(x =>
                    x.CustomerId == customerId &&
                    x.ContainerTypeId == 1)
                .SumAsync(x =>
                    x.MovementType == MovementType.Out
                        ? x.Quantity
                        : -x.Quantity);

            // Corrected workbook position 12 + same-day Manual 2 +
            // next-day Manual 4.
            Assert.Equal(18, finalBalance);
        }
        finally
        {
            if (File.Exists(firstFile)) File.Delete(firstFile);
            if (File.Exists(correctedFile)) File.Delete(correctedFile);
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

            var preflight = await service.PreflightAsync(await SourceAsync(temp));

            var request = new ImportExecutionRequest(
                await SourceAsync(temp),
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
                new DateOnly(2026, 8, 13),
                ClientOperationId: Guid.NewGuid());

            var first = await service.ExecuteAsync(request);

            // A network/client retry of the same command must be idempotent.
            var retry = await service.ExecuteAsync(request);
            Assert.Equal(first.ImportRunId, retry.ImportRunId);

            var changedPayload = request with
            {
                Mappings =
                [
                    new ImportWorksheetMapping(
                        "Update Account",
                        ImportWorksheetRole.Source,
                        "Different exclusion reason")
                ]
            };

            var payloadConflict = await Assert.ThrowsAsync<InvalidOperationException>(
                () => service.ExecuteAsync(changedPayload));
            Assert.Contains("different import request", payloadConflict.Message);

            // A genuinely new command using the same exact source is not a retry
            // and must remain blocked by the database-backed source-hash invariant.
            var newOperation = request with
            {
                ClientOperationId = Guid.NewGuid()
            };

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(
                () => service.ExecuteAsync(newOperation));

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

    private sealed class ThrowingImportExecutionFailureInjector(
        ImportExecutionFailurePoint failurePoint)
        : IImportExecutionFailureInjector
    {
        public Task ReachAsync(
            ImportExecutionFailurePoint point,
            CancellationToken cancellationToken = default)
        {
            if (point == failurePoint)
            {
                throw new ImportExecutionInjectedFailureException(
                    point);
            }

            return Task.CompletedTask;
        }
    }

    private sealed class ImportExecutionInjectedFailureException(
        ImportExecutionFailurePoint point)
        : Exception(
            $"Injected import failure at {point}.")
    {
        public ImportExecutionFailurePoint Point { get; } =
            point;
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
            new ImportSourceDocument(
                "test.xlsx",
                new byte[] { 1 },
                "test.xlsx"),
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

    private static async Task<ImportSourceDocument> SourceAsync(string path)
    {
        var info = new FileInfo(path);
        return new ImportSourceDocument(
            info.Name,
            await File.ReadAllBytesAsync(path),
            info.FullName,
            info.LastWriteTimeUtc);
    }

}
