using System.Text.Json;
using BinTracker.Core;
using BinTracker.Data;
using BinTracker.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BinTracker.IntegrationTests;

public sealed class AuditContractBoundaryTests
{
    [Fact]
    public void Public_audit_contract_has_no_generic_write_or_raw_entity_results()
    {
        var methods = typeof(IAuditService).GetMethods();

        Assert.DoesNotContain(methods, method => method.Name == "WriteAsync");
        Assert.DoesNotContain(
            typeof(CsvReportExportAuditIntent).GetProperties(),
            property => property.Name is "Action" or "EntityType" or "EntityId");
        Assert.All(methods, method =>
            Assert.DoesNotContain(
                typeof(AuditEvent),
                Flatten(method.ReturnType)));

        Assert.DoesNotContain(
            typeof(AuditEventSummary).GetProperties(),
            property => property.Name is "BeforeValues" or "AfterValues" or
                "ComputerName" or "SessionId");
        Assert.DoesNotContain(
            typeof(PendingMovementChangeAuditEvent).GetProperties(),
            property => property.Name is "BeforeValues" or "AfterValues" or
                "ComputerName" or "SessionId");
    }

    [Fact]
    public async Task Structured_csv_export_intents_fix_action_entity_and_payload_shape()
    {
        var appender = new RecordingAuditAppender();
        var service = new ReportExportAuditService(appender);

        CsvReportExportAuditIntent[] intents =
        [
            new DailyMovementsCsvExportAuditIntent(
                "daily description", "daily.csv", 3,
                new DateOnly(2026, 10, 1), "ACME", "Blue", "OUT", "Manual", true, false),
            new WeeklyMovementsCsvExportAuditIntent(
                "weekly description", "weekly.csv", 4,
                new DateOnly(2026, 9, 28), new DateOnly(2026, 10, 4),
                "Daily Detail", "", "All", "All", false, true),
            new MonthlySummaryCsvExportAuditIntent(
                "monthly description", "monthly.csv", 5,
                new DateOnly(2026, 10, 1), new DateOnly(2026, 10, 31),
                new DateOnly(2026, 10, 13), "", "All", "All", false),
            new OutstandingContainersCsvExportAuditIntent(
                "outstanding description", "outstanding.csv", 6,
                new DateOnly(2026, 10, 13), "", "All", "Credits", true),
            new MovementHistoryCsvExportAuditIntent(
                "history description", "history.csv", 7,
                new DateOnly(2026, 10, 1), new DateOnly(2026, 10, 13),
                "ACME", "Blue", "OUT", "Batch", false, true)
        ];

        foreach (var intent in intents)
            await service.RecordCsvExportAsync(intent);

        Assert.Equal(
            [
                "DAILY_MOVEMENTS_CSV_EXPORTED",
                "WEEKLY_MOVEMENTS_CSV_EXPORTED",
                "MONTHLY_SUMMARY_CSV_EXPORTED",
                "OUTSTANDING_CONTAINERS_CSV_EXPORTED",
                "MOVEMENT_HISTORY_CSV_EXPORTED"
            ],
            appender.Writes.Select(write => write.Action));
        Assert.Equal(
            [
                "2026-10-01",
                "2026-09-28:2026-10-04",
                "2026-10",
                "2026-10-13",
                "2026-10-01:2026-10-13"
            ],
            appender.Writes.Select(write => write.EntityId));
        Assert.All(appender.Writes, write => Assert.Equal("Report", write.EntityType));
        Assert.Equal(
            intents.Select(intent => intent.Description),
            appender.Writes.Select(write => write.Description));

        for (var index = 0; index < appender.Writes.Count; index++)
        {
            var payload = JsonSerializer.SerializeToElement(appender.Writes[index].After);
            Assert.Equal("CSV", payload.GetProperty("Format").GetString());
            Assert.Equal(intents[index].RowCount, payload.GetProperty("RowCount").GetInt32());
            Assert.Equal(intents[index].FileName, payload.GetProperty("FileName").GetString());
            Assert.Equal(JsonValueKind.Object, payload.GetProperty("Context").ValueKind);
        }

        var dailyContext = JsonSerializer.SerializeToElement(appender.Writes[0].After)
            .GetProperty("Context");
        Assert.Equal("2026-10-01", dailyContext.GetProperty("ReportDate").GetString());
        Assert.True(dailyContext.GetProperty("IncludeAdjustments").GetBoolean());
        Assert.False(dailyContext.GetProperty("IncludeNotes").GetBoolean());

        var weeklyContext = JsonSerializer.SerializeToElement(appender.Writes[1].After)
            .GetProperty("Context");
        Assert.Equal("Daily Detail", weeklyContext.GetProperty("View").GetString());
        Assert.True(weeklyContext.GetProperty("IncludeNotes").GetBoolean());

        var monthlyContext = JsonSerializer.SerializeToElement(appender.Writes[2].After)
            .GetProperty("Context");
        Assert.Equal("2026-10-31", monthlyContext.GetProperty("MonthEnd").GetString());
        Assert.Equal("2026-10-13", monthlyContext.GetProperty("DataThroughDate").GetString());

        var outstandingContext = JsonSerializer.SerializeToElement(appender.Writes[3].After)
            .GetProperty("Context");
        Assert.Equal("Credits", outstandingContext.GetProperty("BalanceFilter").GetString());
        Assert.True(outstandingContext.GetProperty("IncludeInactive").GetBoolean());

        var historyContext = JsonSerializer.SerializeToElement(appender.Writes[4].After)
            .GetProperty("Context");
        Assert.Equal("OUT", historyContext.GetProperty("Direction").GetString());
        Assert.Equal("Batch", historyContext.GetProperty("Source").GetString());
    }

    [Fact]
    public async Task Authentication_and_bootstrap_audits_keep_trusted_actor_and_context_stamping()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var services = new ServiceCollection();
        services.AddSingleton<IBusinessClock>(new FixedClock());
        services.AddDbContextFactory<BinTrackerDbContext>(options => options.UseSqlite(connection));
        services.AddBinTrackerServices();
        services.AddSingleton<IClientContext>(new FixedClientContext());

        await using var provider = services.BuildServiceProvider();
        await using var scope = provider.CreateAsyncScope();
        var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<BinTrackerDbContext>>();
        await using (var db = await factory.CreateDbContextAsync())
        {
            await db.Database.EnsureCreatedAsync();
            await DatabaseSetup.InitializeSqliteAsync(db);
        }

        var authentication = scope.ServiceProvider.GetRequiredService<IAuthenticationService>();
        await authentication.CreateInitialAdministratorAsync(
            "admin", "Administrator", "CorrectHorse!42");
        Assert.True(await authentication.LoginAsync("admin", "CorrectHorse!42"));

        for (var attempt = 0; attempt < 5; attempt++)
            Assert.False(await authentication.LoginAsync("admin", "wrong-password"));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            authentication.LoginAsync("admin", "CorrectHorse!42"));

        await using var verify = await factory.CreateDbContextAsync();
        var audit = await verify.AuditEvents.AsNoTracking()
            .OrderBy(item => item.Id)
            .ToListAsync();
        var administrator = await verify.UserAccounts.AsNoTracking().SingleAsync();

        Assert.Contains(audit, item =>
            item.Action == "USER_CREATED" &&
            item.UserId == administrator.Id &&
            item.Username == "admin");
        Assert.Contains(audit, item =>
            item.Action == "LOGIN_SUCCEEDED" &&
            item.UserId == administrator.Id &&
            item.Username == "admin");
        Assert.Equal(5, audit.Count(item =>
            item.Action == "LOGIN_FAILED" &&
            item.UserId == administrator.Id &&
            item.Username == "admin"));
        Assert.Contains(audit, item =>
            item.Action == "ACCOUNT_LOCKED" &&
            item.UserId == administrator.Id &&
            item.Username == "admin");
        Assert.Contains(audit, item =>
            item.Action == "LOGIN_BLOCKED_LOCKED" &&
            item.UserId == administrator.Id &&
            item.Username == "admin");
        Assert.All(audit, item =>
        {
            Assert.Equal(FixedClock.Utc, item.TimestampUtc);
            Assert.Equal("test-device", item.ComputerName);
            Assert.False(string.IsNullOrWhiteSpace(item.SessionId));
        });
    }

    private static IReadOnlyList<Type> Flatten(Type type)
    {
        var result = new List<Type> { type };
        foreach (var argument in type.GetGenericArguments())
            result.AddRange(Flatten(argument));
        return result;
    }

    private sealed class RecordingAuditAppender : IAuditAppender
    {
        public List<AuditWrite> Writes { get; } = [];

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
            Writes.Add(new(action, entityType, entityId, description, after));
            return Task.CompletedTask;
        }
    }

    private sealed record AuditWrite(
        string Action,
        string EntityType,
        string? EntityId,
        string Description,
        object? After);

    private sealed class FixedClock : IBusinessClock
    {
        public static readonly DateTime Utc =
            new(2026, 10, 13, 1, 2, 3, DateTimeKind.Utc);
        public DateTime UtcNow => Utc;
        public DateTime LocalNow => Utc;
        public DateOnly Today => DateOnly.FromDateTime(Utc);
        public string TimeZoneId => "UTC";
    }

    private sealed class FixedClientContext : IClientContext
    {
        public string ClientInstanceId => "test-client";
        public string DeviceName => "test-device";
    }
}
