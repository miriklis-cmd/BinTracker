using BinTracker.Core;
using BinTracker.Data;
using BinTracker.Services;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BinTracker.IntegrationTests;

public sealed class ExcelImportAnalysisTests
{
    [Fact]
    public async Task Analysis_detects_buyer_columns_and_customer_types()
    {
        var file = Path.Combine(
            Path.GetTempPath(),
            $"bintracker-import-{Guid.NewGuid():N}.xlsx");

        try
        {
            using (var workbook = new XLWorkbook())
            {
                var account = workbook.AddWorksheet("Update Account");
                account.Cell("A1").Value = "Buyer";
                account.Cell("A2").Value = "ALBURY";
                account.Cell("A3").Value = "FISH PIER";

                var cash = workbook.AddWorksheet("Update Cash");
                cash.Cell("A1").Value = "Buyer";
                cash.Cell("A2").Value = "HAI PHU";

                workbook.AddWorksheet("Summary");
                workbook.AddWorksheet("CREDITS");
                workbook.AddWorksheet("Print This");
                workbook.AddWorksheet("Print this on reverse side");

                workbook.SaveAs(file);
            }

            await using var connection =
                new Microsoft.Data.Sqlite.SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();

            var options = new DbContextOptionsBuilder<BinTrackerDbContext>()
                .UseSqlite(connection)
                .Options;

            var services = new ServiceCollection();
            var clock = new TestBusinessClock();
            services.AddSingleton<IBusinessClock>(clock);
            services.AddSingleton(new UserSession(clock));
            services.AddSingleton<IDbContextFactory<BinTrackerDbContext>>(
                new TestFactory(options));

            var provider = services.BuildServiceProvider();
            var session = provider.GetRequiredService<UserSession>();

            // Sign in a synthetic administrator for the service permission check.
            session.SignIn(new UserAccount
            {
                Id = 1,
                Username = "admin",
                DisplayName = "Administrator",
                Role = UserRole.Administrator
            });

            await using var db = new BinTrackerDbContext(options);
            await db.Database.EnsureCreatedAsync();
            await DatabaseSetup.InitializeSqliteAsync(db);

            var audit = new TestAuditService();
            IExcelImportService importer =
                new ExcelImportServiceForTest(session, audit);

            var result = await importer.AnalyzeAsync(
                new ImportSourceDocument(
                    Path.GetFileName(file),
                    await File.ReadAllBytesAsync(file),
                    Path.GetFullPath(file),
                    File.GetLastWriteTimeUtc(file)));

            Assert.Equal(6, result.WorksheetCount);
            Assert.Equal(3, result.CustomerCandidateCount);
            Assert.Contains(result.CustomerCandidates,
                x => x.CustomerCode == "ALBURY" &&
                     x.CustomerType == CustomerType.Account);
            Assert.Contains(result.CustomerCandidates,
                x => x.CustomerCode == "HAI PHU" &&
                     x.CustomerType == CustomerType.CashCod);
        }
        finally
        {
            if (File.Exists(file))
                File.Delete(file);
        }
    }

    private sealed class TestFactory(
        DbContextOptions<BinTrackerDbContext> options)
        : IDbContextFactory<BinTrackerDbContext>
    {
        public BinTrackerDbContext CreateDbContext() => new(options);

        public Task<BinTrackerDbContext> CreateDbContextAsync(
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new BinTrackerDbContext(options));
    }

    private sealed class TestAuditService : IAuditService
    {
        public Task WriteAsync(
            string action,
            string entityType,
            string? entityId,
            string description,
            bool succeeded = true,
            object? before = null,
            object? after = null,
            int? userIdOverride = null,
            string? usernameOverride = null,
            CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task<IReadOnlyList<AuditEvent>> GetRecentAsync(
            int limit = 500,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<AuditEvent>>([]);
    }

    // The production implementation is internal, so this tiny test adapter
    // exercises the same public contract through the registered assembly.
    private sealed class ExcelImportServiceForTest(
        UserSession session,
        IAuditService audit) : IExcelImportService
    {
        private readonly IExcelImportService inner =
            (IExcelImportService)Activator.CreateInstance(
                typeof(IExcelImportService).Assembly
                    .GetType("BinTracker.Services.ExcelImportService")!,
                session,
                audit)!;

        public Task<ExcelImportAnalysis> AnalyzeAsync(
            ImportSourceDocument source,
            CancellationToken cancellationToken = default) =>
            inner.AnalyzeAsync(source, cancellationToken);
    }

    private sealed class TestBusinessClock : IBusinessClock
    {
        public DateTime UtcNow => new(2026, 8, 23, 0, 0, 0, DateTimeKind.Utc);
        public DateTime LocalNow => new(2026, 8, 23, 10, 0, 0, DateTimeKind.Unspecified);
        public DateOnly Today => new(2026, 8, 23);
        public string TimeZoneId => "Australia/Melbourne";
    }

}
