namespace BinTracker.Services;

public abstract record CsvReportExportAuditIntent(
    string Description,
    string FileName,
    int RowCount);

public sealed record DailyMovementsCsvExportAuditIntent(
    string Description,
    string FileName,
    int RowCount,
    DateOnly ReportDate,
    string CustomerSearch,
    string Container,
    string Direction,
    string Source,
    bool IncludeAdjustments,
    bool IncludeNotes) :
    CsvReportExportAuditIntent(Description, FileName, RowCount);

public sealed record WeeklyMovementsCsvExportAuditIntent(
    string Description,
    string FileName,
    int RowCount,
    DateOnly WeekStart,
    DateOnly WeekEnd,
    string View,
    string CustomerSearch,
    string Container,
    string Source,
    bool IncludeAdjustments,
    bool? IncludeNotes) :
    CsvReportExportAuditIntent(Description, FileName, RowCount);

public sealed record MonthlySummaryCsvExportAuditIntent(
    string Description,
    string FileName,
    int RowCount,
    DateOnly MonthStart,
    DateOnly MonthEnd,
    DateOnly DataThroughDate,
    string CustomerSearch,
    string Container,
    string Source,
    bool IncludeAdjustments) :
    CsvReportExportAuditIntent(Description, FileName, RowCount);

public sealed record OutstandingContainersCsvExportAuditIntent(
    string Description,
    string FileName,
    int RowCount,
    DateOnly AsOfDate,
    string CustomerSearch,
    string Container,
    string BalanceFilter,
    bool IncludeInactive) :
    CsvReportExportAuditIntent(Description, FileName, RowCount);

public sealed record MovementHistoryCsvExportAuditIntent(
    string Description,
    string FileName,
    int RowCount,
    DateOnly StartDate,
    DateOnly EndDate,
    string CustomerSearch,
    string Container,
    string Direction,
    string Source,
    bool IncludeAdjustments,
    bool IncludeNotes) :
    CsvReportExportAuditIntent(Description, FileName, RowCount);

public interface IReportExportAuditService
{
    Task RecordCsvExportAsync(
        CsvReportExportAuditIntent intent,
        CancellationToken cancellationToken = default);
}

internal sealed class ReportExportAuditService(
    IAuditAppender audit,
    ICurrentAccountAuthorization authorization) : IReportExportAuditService
{
    public async Task RecordCsvExportAsync(
        CsvReportExportAuditIntent intent,
        CancellationToken cancellationToken = default)
    {
        await authorization.RequireAuthenticatedAsync(cancellationToken);
        ArgumentNullException.ThrowIfNull(intent);
        if (intent.RowCount < 0)
            throw new ArgumentOutOfRangeException(nameof(intent), "CSV row count cannot be negative.");
        if (string.IsNullOrWhiteSpace(intent.FileName) ||
            intent.FileName.IndexOfAny(['/', '\\']) >= 0)
        {
            throw new ArgumentException(
                "CSV audit metadata requires a leaf file name.",
                nameof(intent));
        }

        await (intent switch
        {
            DailyMovementsCsvExportAuditIntent daily => audit.AppendAsync(
                "DAILY_MOVEMENTS_CSV_EXPORTED",
                "Report",
                daily.ReportDate.ToString("yyyy-MM-dd"),
                daily.Description,
                after: Envelope(daily, new
                {
                    daily.ReportDate,
                    daily.CustomerSearch,
                    daily.Container,
                    daily.Direction,
                    daily.Source,
                    daily.IncludeAdjustments,
                    daily.IncludeNotes
                }),
                cancellationToken: cancellationToken),

            WeeklyMovementsCsvExportAuditIntent weekly => RecordWeeklyAsync(
                weekly, cancellationToken),

            MonthlySummaryCsvExportAuditIntent monthly => audit.AppendAsync(
                "MONTHLY_SUMMARY_CSV_EXPORTED",
                "Report",
                monthly.MonthStart.ToString("yyyy-MM"),
                monthly.Description,
                after: Envelope(monthly, new
                {
                    monthly.MonthStart,
                    monthly.MonthEnd,
                    monthly.DataThroughDate,
                    monthly.CustomerSearch,
                    monthly.Container,
                    monthly.Source,
                    monthly.IncludeAdjustments
                }),
                cancellationToken: cancellationToken),

            OutstandingContainersCsvExportAuditIntent outstanding => audit.AppendAsync(
                "OUTSTANDING_CONTAINERS_CSV_EXPORTED",
                "Report",
                outstanding.AsOfDate.ToString("yyyy-MM-dd"),
                outstanding.Description,
                after: Envelope(outstanding, new
                {
                    outstanding.AsOfDate,
                    outstanding.CustomerSearch,
                    outstanding.Container,
                    outstanding.BalanceFilter,
                    outstanding.IncludeInactive
                }),
                cancellationToken: cancellationToken),

            MovementHistoryCsvExportAuditIntent history => audit.AppendAsync(
                "MOVEMENT_HISTORY_CSV_EXPORTED",
                "Report",
                $"{history.StartDate:yyyy-MM-dd}:{history.EndDate:yyyy-MM-dd}",
                history.Description,
                after: Envelope(history, new
                {
                    history.StartDate,
                    history.EndDate,
                    history.CustomerSearch,
                    history.Container,
                    history.Direction,
                    history.Source,
                    history.IncludeAdjustments,
                    history.IncludeNotes
                }),
                cancellationToken: cancellationToken),

            _ => throw new ArgumentException(
                "Unsupported CSV report export audit intent.",
                nameof(intent))
        });
    }

    private Task RecordWeeklyAsync(
        WeeklyMovementsCsvExportAuditIntent weekly,
        CancellationToken cancellationToken)
    {
        var entityId = $"{weekly.WeekStart:yyyy-MM-dd}:{weekly.WeekEnd:yyyy-MM-dd}";
        if (weekly.IncludeNotes.HasValue)
        {
            return audit.AppendAsync(
                "WEEKLY_MOVEMENTS_CSV_EXPORTED",
                "Report",
                entityId,
                weekly.Description,
                after: Envelope(weekly, new
                {
                    weekly.WeekStart,
                    weekly.WeekEnd,
                    weekly.View,
                    weekly.CustomerSearch,
                    weekly.Container,
                    weekly.Source,
                    weekly.IncludeAdjustments,
                    IncludeNotes = weekly.IncludeNotes.Value
                }),
                cancellationToken: cancellationToken);
        }

        return audit.AppendAsync(
            "WEEKLY_MOVEMENTS_CSV_EXPORTED",
            "Report",
            entityId,
            weekly.Description,
            after: Envelope(weekly, new
            {
                weekly.WeekStart,
                weekly.WeekEnd,
                weekly.View,
                weekly.CustomerSearch,
                weekly.Container,
                weekly.Source,
                weekly.IncludeAdjustments
            }),
            cancellationToken: cancellationToken);
    }

    private static object Envelope(
        CsvReportExportAuditIntent intent,
        object context) =>
        new
        {
            Format = "CSV",
            intent.RowCount,
            intent.FileName,
            Context = context
        };
}
