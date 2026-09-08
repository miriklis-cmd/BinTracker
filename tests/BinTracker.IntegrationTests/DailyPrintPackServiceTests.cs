using System.Text;
using System.Text.Json;
using BinTracker.Core;
using BinTracker.Data;
using BinTracker.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BinTracker.IntegrationTests;

public sealed class DailyPrintPackServiceTests
{
    [Fact]
    public async Task Future_date_is_clamped_for_both_sections_and_one_success_audit()
    {
        var today = new DateOnly(2026, 9, 8);
        var calls = new List<string>();
        var outstanding = new RecordingOutstandingService(calls, OutstandingResult(today));
        var daily = new RecordingDailyService(calls, DailyResult(today));
        var audit = new RecordingAuditService(calls);
        var business = new RecordingBusinessInformationService(calls);
        using var cancellation = new CancellationTokenSource();
        await using var provider = BuildProvider(
            new FixedClock(today), outstanding, daily, audit, business);

        var pdf = await provider.GetRequiredService<IDailyPrintPackService>()
            .BuildPdfAsync(today.AddDays(3), cancellation.Token);

        Assert.Equal("%PDF", Encoding.ASCII.GetString(pdf, 0, 4));
        var outstandingQuery = Assert.Single(outstanding.Queries);
        Assert.Equal(today, outstandingQuery.Query.AsOfDate);
        Assert.Equal(OutstandingBalanceFilter.OutstandingOnly,
            outstandingQuery.Query.BalanceFilter);
        Assert.False(outstandingQuery.Query.IncludeInactiveCustomers);
        Assert.Null(outstandingQuery.Query.CustomerSearch);
        Assert.Null(outstandingQuery.Query.ContainerTypeId);
        Assert.Equal(cancellation.Token, outstandingQuery.Token);

        var dailyQuery = Assert.Single(daily.Queries);
        Assert.Equal(today, dailyQuery.Query.ReportDate);
        Assert.False(dailyQuery.Query.IncludeAdjustments);
        Assert.Null(dailyQuery.Query.CustomerSearch);
        Assert.Null(dailyQuery.Query.ContainerTypeId);
        Assert.Null(dailyQuery.Query.Direction);
        Assert.Null(dailyQuery.Query.Source);
        Assert.Equal(cancellation.Token, dailyQuery.Token);
        Assert.Equal(cancellation.Token, Assert.Single(business.Tokens));

        var write = Assert.Single(audit.Writes);
        Assert.Equal("DAILY_PRINT_PACK_GENERATED", write.Action);
        Assert.Equal("Report", write.EntityType);
        Assert.Equal("2026-09-08", write.EntityId);
        Assert.Contains("2 outstanding row(s)", write.Description);
        Assert.Contains("3 movement row(s)", write.Description);
        Assert.Contains("7 OUT, 2 IN", write.Description);
        Assert.Equal(cancellation.Token, write.Token);
        using var after = JsonDocument.Parse(JsonSerializer.Serialize(write.After));
        Assert.Equal("2026-09-08", after.RootElement.GetProperty("ReportDate").GetString());
        Assert.Equal(2, after.RootElement.GetProperty("OutstandingRows").GetInt32());
        Assert.Equal(3, after.RootElement.GetProperty("MovementRows").GetInt32());
        Assert.Equal(7, after.RootElement.GetProperty("OutQuantity").GetInt32());
        Assert.Equal(2, after.RootElement.GetProperty("InQuantity").GetInt32());
        Assert.Equal(new[] { "outstanding", "daily", "business", "audit" }, calls);
    }

    [Fact]
    public async Task Delegated_failure_propagates_without_business_lookup_or_audit()
    {
        var today = new DateOnly(2026, 9, 8);
        var calls = new List<string>();
        var expected = new InvalidOperationException("delegated report failed");
        var outstanding = new RecordingOutstandingService(
            calls, _ => Task.FromException<OutstandingReportResult>(expected));
        var daily = new RecordingDailyService(calls, DailyResult(today));
        var audit = new RecordingAuditService(calls);
        var business = new RecordingBusinessInformationService(calls);
        await using var provider = BuildProvider(
            new FixedClock(today), outstanding, daily, audit, business);

        var actual = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            provider.GetRequiredService<IDailyPrintPackService>().BuildPdfAsync(today));

        Assert.Same(expected, actual);
        Assert.Single(outstanding.Queries);
        Assert.Single(daily.Queries);
        Assert.Empty(business.Tokens);
        Assert.Empty(audit.Writes);
    }

    [Fact]
    public async Task Delegated_cancellation_propagates_without_business_lookup_or_audit()
    {
        var today = new DateOnly(2026, 9, 8);
        var calls = new List<string>();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var outstanding = new RecordingOutstandingService(
            calls, token => Task.FromCanceled<OutstandingReportResult>(token));
        var daily = new RecordingDailyService(
            calls, token => Task.FromCanceled<DailyMovementsReportResult>(token));
        var audit = new RecordingAuditService(calls);
        var business = new RecordingBusinessInformationService(calls);
        await using var provider = BuildProvider(
            new FixedClock(today), outstanding, daily, audit, business);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            provider.GetRequiredService<IDailyPrintPackService>()
                .BuildPdfAsync(today, cancellation.Token));

        Assert.Equal(cancellation.Token, Assert.Single(outstanding.Queries).Token);
        Assert.Equal(cancellation.Token, Assert.Single(daily.Queries).Token);
        Assert.Empty(business.Tokens);
        Assert.Empty(audit.Writes);
    }

    [Fact]
    public async Task Normal_composition_preserves_alpha8_delegated_source_date_and_total_semantics()
    {
        var today = new DateOnly(2026, 9, 8);
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var services = new ServiceCollection();
        services.AddSingleton<IBusinessClock>(new FixedClock(today));
        services.AddDbContextFactory<BinTrackerDbContext>(
            options => options.UseSqlite(connection));
        services.AddBinTrackerServices();
        await using var provider = services.BuildServiceProvider();
        var factory = provider.GetRequiredService<IDbContextFactory<BinTrackerDbContext>>();

        await using (var db = await factory.CreateDbContextAsync())
        {
            await db.Database.EnsureCreatedAsync();
            await DatabaseSetup.InitializeSqliteAsync(db);
            var customer = new Customer
            {
                CustomerCode = "PACK",
                Name = "Print Pack",
                CustomerType = CustomerType.Account,
                IsActive = true
            };
            db.Add(customer);
            await db.SaveChangesAsync();
            db.BinMovements.AddRange(
                Movement(customer.Id, today.AddDays(-1), MovementType.Out,
                    MovementSource.Manual, 5),
                Movement(customer.Id, today, MovementType.In,
                    MovementSource.Batch, 2),
                Movement(customer.Id, today, MovementType.Out,
                    MovementSource.Adjustment, 99),
                Movement(customer.Id, today, MovementType.Out,
                    MovementSource.ExcelImport, 3),
                Movement(customer.Id, today.AddDays(1), MovementType.Out,
                    MovementSource.Manual, 100));
            await db.SaveChangesAsync();
        }

        var pdf = await provider.GetRequiredService<IDailyPrintPackService>()
            .BuildPdfAsync(today.AddDays(1));

        Assert.Equal("%PDF", Encoding.ASCII.GetString(pdf, 0, 4));
        await using var verification = await factory.CreateDbContextAsync();
        var audit = Assert.Single(await verification.AuditEvents.AsNoTracking()
            .Where(x => x.Action == "DAILY_PRINT_PACK_GENERATED")
            .ToListAsync());
        Assert.Equal("2026-09-08", audit.EntityId);
        Assert.Contains("1 outstanding row(s)", audit.Description);
        Assert.Contains("2 movement row(s)", audit.Description);
        Assert.Contains("3 OUT, 2 IN", audit.Description);
    }

    private static ServiceProvider BuildProvider(
        IBusinessClock clock,
        IOutstandingReportService outstanding,
        IDailyMovementsReportService daily,
        IAuditService audit,
        IBusinessInformationService business)
    {
        var services = new ServiceCollection();
        services.AddSingleton(clock);
        services.AddBinTrackerBusinessServices();
        services.AddSingleton(outstanding);
        services.AddSingleton(daily);
        services.AddSingleton(audit);
        services.AddSingleton(business);
        return services.BuildServiceProvider();
    }

    private static OutstandingReportResult OutstandingResult(DateOnly date) =>
        new(date,
            [
                new(2, "ZULU", "Zulu", CustomerType.Account, true,
                    3, "Yellow Bin", 3, 4, date),
                new(1, "ALPHA", "Alpha", CustomerType.CashCod, true,
                    1, "Blue Bin", 1, 2, date.AddDays(-1))
            ],
            [
                new(1, "Blue Bin", 1, 2, 0, 1),
                new(3, "Yellow Bin", 3, 4, 0, 1)
            ]);

    private static DailyMovementsReportResult DailyResult(DateOnly date) =>
        new(date,
            [
                DailyRow(31, date, "ZULU", MovementType.Out, 4,
                    MovementSource.Manual, "manual-ref", "manual-user"),
                DailyRow(32, date, "ALPHA", MovementType.In, 2,
                    MovementSource.Batch, "batch-ref", "batch-user"),
                DailyRow(33, date, "BRAVO", MovementType.Out, 3,
                    MovementSource.ExcelImport, "excel-ref", "import-user")
            ],
            [
                new(1, "Blue Bin", 1, 3, 2),
                new(3, "Yellow Bin", 3, 4, 0)
            ]);

    private static DailyMovementReportRow DailyRow(
        long id,
        DateOnly date,
        string code,
        MovementType direction,
        int quantity,
        MovementSource source,
        string reference,
        string enteredBy) =>
        new(id, date, date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc),
            checked((int)id), code, $"{code} customer", CustomerType.Account,
            source == MovementSource.Manual ? 3 : 1,
            source == MovementSource.Manual ? "Yellow Bin" : "Blue Bin",
            source == MovementSource.Manual ? 3 : 1,
            direction, quantity, source, reference, $"{code} notes", enteredBy);

    private static BinMovement Movement(
        int customerId,
        DateOnly date,
        MovementType direction,
        MovementSource source,
        int quantity) =>
        new()
        {
            ClientOperationId = Guid.NewGuid(),
            CustomerId = customerId,
            ContainerTypeId = 1,
            MovementDate = date,
            MovementType = direction,
            Source = source,
            Quantity = quantity,
            ReferenceNumber = $"{source}-reference",
            CreatedBy = $"{source}-user",
            CreatedUtc = date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc)
        };

    private sealed class FixedClock(DateOnly today) : IBusinessClock
    {
        public DateTime UtcNow => today.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        public DateTime LocalNow => UtcNow;
        public DateOnly Today => today;
        public string TimeZoneId => "UTC";
    }

    private sealed class RecordingOutstandingService : IOutstandingReportService
    {
        private readonly List<string> calls;
        private readonly Func<CancellationToken, Task<OutstandingReportResult>> result;

        public RecordingOutstandingService(
            List<string> calls,
            OutstandingReportResult result)
            : this(calls, _ => Task.FromResult(result))
        {
        }

        public RecordingOutstandingService(
            List<string> calls,
            Func<CancellationToken, Task<OutstandingReportResult>> result)
        {
            this.calls = calls;
            this.result = result;
        }

        public List<(OutstandingReportQuery Query, CancellationToken Token)> Queries { get; } = [];

        public Task<OutstandingReportResult> QueryAsync(
            OutstandingReportQuery query,
            CancellationToken cancellationToken = default)
        {
            calls.Add("outstanding");
            Queries.Add((query, cancellationToken));
            return result(cancellationToken);
        }
    }

    private sealed class RecordingDailyService : IDailyMovementsReportService
    {
        private readonly List<string> calls;
        private readonly Func<CancellationToken, Task<DailyMovementsReportResult>> result;

        public RecordingDailyService(
            List<string> calls,
            DailyMovementsReportResult result)
            : this(calls, _ => Task.FromResult(result))
        {
        }

        public RecordingDailyService(
            List<string> calls,
            Func<CancellationToken, Task<DailyMovementsReportResult>> result)
        {
            this.calls = calls;
            this.result = result;
        }

        public List<(DailyMovementsReportQuery Query, CancellationToken Token)> Queries { get; } = [];

        public Task<DailyMovementsReportResult> QueryAsync(
            DailyMovementsReportQuery query,
            CancellationToken cancellationToken = default)
        {
            calls.Add("daily");
            Queries.Add((query, cancellationToken));
            return result(cancellationToken);
        }
    }

    private sealed class RecordingBusinessInformationService(List<string> calls)
        : IBusinessInformationService
    {
        public List<CancellationToken> Tokens { get; } = [];

        public Task<BusinessInformation> GetAsync(
            CancellationToken cancellationToken = default)
        {
            calls.Add("business");
            Tokens.Add(cancellationToken);
            return Task.FromResult(new BusinessInformation(
                "BinTracker", string.Empty, string.Empty, string.Empty,
                string.Empty, string.Empty, "BinTracker"));
        }

        public Task SaveAsync(
            BusinessInformation information,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }

    private sealed class RecordingAuditService(List<string> calls) : IAuditService
    {
        public List<AuditWrite> Writes { get; } = [];

        public event EventHandler<AdministratorReviewState>? AdministratorReviewStateChanged
        {
            add { }
            remove { }
        }

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
            CancellationToken cancellationToken = default)
        {
            calls.Add("audit");
            Writes.Add(new(action, entityType, entityId, description,
                succeeded, before, after, cancellationToken));
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<AuditEvent>> GetRecentAsync(
            int limit = 500,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<AuditTrailRow>> GetAuditTrailAsync(
            AuditReviewFilter filter = AuditReviewFilter.All,
            int limit = 500,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<AdministratorReviewState> GetAdministratorReviewStateAsync(
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<AuditEvent>> GetUnreviewedMovementChangesAsync(
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task MarkMovementChangesReviewedAsync(
            IReadOnlyCollection<long> auditEventIds,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<MovementBatchAuditLine>> GetMovementBatchDetailAsync(
            int batchId,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<MovementChangeAuditDetail?> GetMovementChangeDetailAsync(
            long auditEventId,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }

    private sealed record AuditWrite(
        string Action,
        string EntityType,
        string? EntityId,
        string Description,
        bool Succeeded,
        object? Before,
        object? After,
        CancellationToken Token);
}
