using BinTracker.Core;
using BinTracker.Data;
using BinTracker.Services;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.IO.Compression;
using System.Text;
using System.Xml.Linq;
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

    [Fact]
    public void Resource_guard_accepts_the_maximum_worksheet_boundary()
    {
        var content = CreateWorkbookBytes(workbook =>
        {
            for (var index = 1; index <= ExcelWorkbookResourceBudget.Default.MaximumWorksheets; index++)
                workbook.AddWorksheet($"Sheet {index}").Cell("A1").Value = index;
        });

        ExcelWorkbookResourceGuard.ValidateForAnalysis(
            new ImportSourceDocument("boundary.xlsx", content));
    }

    [Theory]
    [InlineData("ordinary.xlsx")]
    [InlineData("ordinary.xlsm")]
    public async Task Analysis_preserves_supported_workbook_formats(string fileName)
    {
        var content = CreateWorkbookBytes(workbook =>
            workbook.AddWorksheet("Update Account").Cell("A1").Value = "Buyer");
        var audit = new TestAuditService();

        var result = await CreateImporter(audit).AnalyzeAsync(
            new ImportSourceDocument(fileName, content));

        Assert.Single(result.Worksheets);
        Assert.Equal(1, audit.AppendCount);
    }

    [Fact]
    public async Task Analysis_accepts_binary_printer_settings_and_vml_xml()
    {
        var content = CreateWorkbookPackage(
            printerSettings: [0x00, 0xff, 0x01, 0xfe, 0x02],
            vml: "<xml xmlns:v=\"urn:schemas-microsoft-com:vml\"><v:shape id=\"synthetic\" /></xml>");
        var audit = new TestAuditService();

        var result = await CreateImporter(audit).AnalyzeAsync(
            new ImportSourceDocument("synthetic-printer-settings.xlsm", content));

        Assert.Single(result.Worksheets);
        Assert.Equal(1, audit.AppendCount);
    }

    [Fact]
    public void Resource_guard_rejects_dtd_in_vml_xml_part()
    {
        var content = CreateWorkbookPackage(
            printerSettings: [0x00, 0xff, 0x01],
            vml: "<!DOCTYPE xml [<!ENTITY hostile \"expanded\">]>" +
                 "<xml xmlns:v=\"urn:schemas-microsoft-com:vml\"><v:shape id=\"&hostile;\" /></xml>");

        var exception = Assert.Throws<InvalidOperationException>(() =>
            ExcelWorkbookResourceGuard.ValidateForAnalysis(
                new ImportSourceDocument("synthetic-hostile-vml.xlsm", content)));

        Assert.IsNotType<WorkbookResourceLimitException>(exception);
        Assert.Contains("not a valid", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Resource_guard_enforces_xml_part_budget_for_vml()
    {
        var content = CreateWorkbookPackage(
            printerSettings: [0x00, 0xff, 0x01],
            vml: $"<xml>{new string('x', 128 * 1024)}</xml>");
        var budget = ExcelWorkbookResourceBudget.Default with
        {
            MaximumXmlEntryBytes = 64 * 1024
        };

        var exception = Assert.Throws<WorkbookResourceLimitException>(() =>
            ExcelWorkbookResourceGuard.ValidateForAnalysis(
                new ImportSourceDocument("synthetic-oversized-vml.xlsm", content), budget));

        Assert.Contains("xl/drawings/synthetic.vml", exception.Message, StringComparison.Ordinal);
        Assert.Contains("XML-part limit", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Resource_guard_enforces_binary_entry_budget_for_printer_settings()
    {
        var content = CreateWorkbookPackage(
            printerSettings: new byte[128 * 1024],
            vml: "<xml xmlns:v=\"urn:schemas-microsoft-com:vml\"><v:shape id=\"synthetic\" /></xml>");
        var budget = ExcelWorkbookResourceBudget.Default with
        {
            MaximumEntryBytes = 64 * 1024
        };

        var exception = Assert.Throws<WorkbookResourceLimitException>(() =>
            ExcelWorkbookResourceGuard.ValidateForAnalysis(
                new ImportSourceDocument("synthetic-oversized-printer-settings.xlsm", content), budget));

        Assert.Contains(
            "xl/printerSettings/printerSettings1.bin",
            exception.Message,
            StringComparison.Ordinal);
        Assert.Contains("entry limit", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Resource_guard_rejects_file_bytes_before_archive_processing()
    {
        var content = CreateWorkbookBytes(workbook =>
            workbook.AddWorksheet("Bytes").Cell("A1").Value = 1);
        var budget = ExcelWorkbookResourceBudget.Default with
        {
            MaximumFileBytes = content.Length - 1
        };

        Assert.Throws<WorkbookResourceLimitException>(() =>
            ExcelWorkbookResourceGuard.ValidateForAnalysis(
                new ImportSourceDocument("bytes.xlsx", content), budget));
    }

    [Fact]
    public async Task Analysis_rejects_excess_worksheets_before_success_audit()
    {
        var content = CreateWorkbookBytes(workbook =>
        {
            for (var index = 1;
                 index <= ExcelWorkbookResourceBudget.Default.MaximumWorksheets + 1;
                 index++)
            {
                workbook.AddWorksheet($"Sheet {index}").Cell("A1").Value = index;
            }
        });
        var audit = new TestAuditService();
        var importer = CreateImporter(audit);

        var exception = await Assert.ThrowsAsync<WorkbookResourceLimitException>(() =>
            importer.AnalyzeAsync(new ImportSourceDocument("too-many-sheets.xlsx", content)));

        Assert.Contains("worksheets", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(0, audit.AppendCount);
    }

    [Fact]
    public void Resource_guard_rejects_sparse_used_range_before_closedxml_load()
    {
        var content = CreateWorkbookBytes(workbook =>
        {
            var sheet = workbook.AddWorksheet("Sparse");
            sheet.Cell("A1").Value = "Buyer";
            sheet.Cell("IW1").Value = "hostile extent";
        });

        var exception = Assert.Throws<WorkbookResourceLimitException>(() =>
            ExcelWorkbookResourceGuard.ValidateForAnalysis(
                new ImportSourceDocument("sparse.xlsx", content)));

        Assert.Contains("columns", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Resource_guard_enforces_row_and_cell_budgets_on_real_workbook()
    {
        var content = CreateWorkbookBytes(workbook =>
        {
            var sheet = workbook.AddWorksheet("Rows");
            sheet.Cell("A1").Value = 1;
            sheet.Cell("A2").Value = 2;
            sheet.Cell("A3").Value = 3;
        });
        var rowBudget = ExcelWorkbookResourceBudget.Default with
        {
            MaximumRowsPerWorksheet = 2
        };
        var cellBudget = ExcelWorkbookResourceBudget.Default with
        {
            MaximumCellsPerWorksheet = 2
        };

        Assert.Throws<WorkbookResourceLimitException>(() =>
            ExcelWorkbookResourceGuard.ValidateForAnalysis(
                new ImportSourceDocument("rows.xlsx", content), rowBudget));
        Assert.Throws<WorkbookResourceLimitException>(() =>
            ExcelWorkbookResourceGuard.ValidateForAnalysis(
                new ImportSourceDocument("cells.xlsx", content), cellBudget));
    }

    [Fact]
    public void Resource_guard_rejects_compressed_expansion_with_bounded_fixture()
    {
        var content = CreateWorkbookBytes(workbook =>
        {
            workbook.AddWorksheet("Compressed")
                .Cell("A1").Value = new string('A', 16_000);
        });
        var budget = ExcelWorkbookResourceBudget.Default with
        {
            MaximumExpandedBytes = 4_096
        };

        var exception = Assert.Throws<WorkbookResourceLimitException>(() =>
            ExcelWorkbookResourceGuard.ValidateForAnalysis(
                new ImportSourceDocument("compressed.xlsx", content), budget));

        Assert.Contains("expands beyond", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Resource_guard_reports_corrupt_input_as_controlled_failure()
    {
        var exception = Assert.Throws<InvalidOperationException>(() =>
            ExcelWorkbookResourceGuard.ValidateForAnalysis(
                new ImportSourceDocument("corrupt.xlsx", [0x50, 0x4b, 0x03, 0x04])));

        Assert.IsNotType<WorkbookResourceLimitException>(exception);
        Assert.Contains("not a valid", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    private static byte[] CreateWorkbookBytes(Action<XLWorkbook> configure)
    {
        using var workbook = new XLWorkbook();
        configure(workbook);
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private static byte[] CreateWorkbookPackage(
        byte[] printerSettings,
        string vml)
    {
        var workbookBytes = CreateWorkbookBytes(workbook =>
            workbook.AddWorksheet("Synthetic").Cell("A1").Value = "Fixture");
        using var package = new MemoryStream();
        package.Write(workbookBytes);
        package.Position = 0;

        using (var archive = new ZipArchive(
                   package,
                   ZipArchiveMode.Update,
                   leaveOpen: true))
        {
            var contentTypesEntry = archive.GetEntry("[Content_Types].xml")!;
            XDocument contentTypes;
            using (var stream = contentTypesEntry.Open())
                contentTypes = XDocument.Load(stream);

            var packageNamespace = contentTypes.Root!.Name.Namespace;
            contentTypes.Root.Add(
                new XElement(
                    packageNamespace + "Override",
                    new XAttribute("PartName", "/xl/printerSettings/printerSettings1.bin"),
                    new XAttribute(
                        "ContentType",
                        "application/vnd.openxmlformats-officedocument.spreadsheetml.printerSettings")),
                new XElement(
                    packageNamespace + "Default",
                    new XAttribute("Extension", "vml"),
                    new XAttribute(
                        "ContentType",
                        "application/vnd.openxmlformats-officedocument.vmlDrawing")));

            contentTypesEntry.Delete();
            var replacementContentTypes = archive.CreateEntry("[Content_Types].xml");
            using (var stream = replacementContentTypes.Open())
                contentTypes.Save(stream);

            var printerSettingsEntry = archive.CreateEntry(
                "xl/printerSettings/printerSettings1.bin");
            using (var stream = printerSettingsEntry.Open())
                stream.Write(printerSettings);

            var vmlEntry = archive.CreateEntry("xl/drawings/synthetic.vml");
            using (var stream = vmlEntry.Open())
                stream.Write(Encoding.UTF8.GetBytes(vml));
        }

        return package.ToArray();
    }

    private static IExcelImportService CreateImporter(TestAuditService audit)
    {
        var clock = new TestBusinessClock();
        var session = new UserSession(clock);
        session.SignIn(new UserAccount
        {
            Id = 1,
            Username = "admin",
            DisplayName = "Administrator",
            Role = UserRole.Administrator
        });
        return new ExcelImportServiceForTest(session, audit);
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

    private sealed class TestAuditService : IAuditService, IAuditAppender
    {
        public int AppendCount { get; private set; }

        public event EventHandler<AdministratorReviewState>? AdministratorReviewStateChanged { add { } remove { } }
        public Task AppendAsync(
            string action,
            string entityType,
            string? entityId,
            string description,
            bool succeeded = true,
            object? before = null,
            object? after = null,
            CancellationToken cancellationToken = default)
        {
            AppendCount++;
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<AuditEventSummary>> GetRecentAsync(
            int limit = 500,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<AuditEventSummary>>([]);

        public Task<IReadOnlyList<AuditTrailRow>> GetAuditTrailAsync(AuditReviewFilter filter = AuditReviewFilter.All,
            int limit = 500, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<AuditTrailRow>>([]);

        public Task<AdministratorReviewState> GetAdministratorReviewStateAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(new AdministratorReviewState(0));

        public Task<IReadOnlyList<PendingMovementChangeAuditEvent>> GetUnreviewedMovementChangesAsync(
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<PendingMovementChangeAuditEvent>>([]);

        public Task MarkMovementChangesReviewedAsync(
            IReadOnlyCollection<long> auditEventIds,
            CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task<IReadOnlyList<MovementBatchAuditLine>> GetMovementBatchDetailAsync(
            int batchId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<MovementBatchAuditLine>>([]);

        public Task<MovementChangeAuditDetail?> GetMovementChangeDetailAsync(long auditEventId,
            CancellationToken cancellationToken = default) => Task.FromResult<MovementChangeAuditDetail?>(null);
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
                audit,
                new TestAuthorization(session))!;

        public Task<ExcelImportAnalysis> AnalyzeAsync(
            ImportSourceDocument source,
            CancellationToken cancellationToken = default) =>
            inner.AnalyzeAsync(source, cancellationToken);

        private sealed class TestAuthorization(UserSession session) :
            ICurrentAccountAuthorization
        {
            public Task<AuthorizedUser> RequireAuthenticatedAsync(
                CancellationToken cancellationToken = default) =>
                RequireAnyRoleAsync(Enum.GetValues<UserRole>(), cancellationToken);

            public Task<AuthorizedUser> RequireCredentialChangeAsync(
                CancellationToken cancellationToken = default) =>
                RequireAuthenticatedAsync(cancellationToken);

            public Task<AuthorizedUser> RequireAnyRoleAsync(
                IReadOnlyCollection<UserRole> roles,
                CancellationToken cancellationToken = default)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (!session.IsAuthenticated || !session.UserId.HasValue ||
                    !roles.Contains(session.Role))
                {
                    throw new UnauthorizedAccessException();
                }

                return Task.FromResult(new AuthorizedUser(
                    session.UserId.Value,
                    session.Username,
                    session.DisplayName,
                    session.Role));
            }
        }
    }

    private sealed class TestBusinessClock : IBusinessClock
    {
        public DateTime UtcNow => new(2026, 8, 23, 0, 0, 0, DateTimeKind.Utc);
        public DateTime LocalNow => new(2026, 8, 23, 10, 0, 0, DateTimeKind.Unspecified);
        public DateOnly Today => new(2026, 8, 23);
        public string TimeZoneId => "Australia/Melbourne";
    }

}
