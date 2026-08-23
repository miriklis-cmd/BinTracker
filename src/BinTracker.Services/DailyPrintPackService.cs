using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace BinTracker.Services;

public interface IDailyPrintPackService
{
    Task<byte[]> BuildPdfAsync(
        DateOnly date,
        CancellationToken cancellationToken = default);
}

internal sealed class DailyPrintPackService(
    IOutstandingReportService outstandingReports,
    IDailyMovementsReportService dailyMovementsReports,
    IAuditService audit,
    IBusinessInformationService businessInformation,
    IBusinessClock clock) : IDailyPrintPackService
{
    public async Task<byte[]> BuildPdfAsync(
        DateOnly date,
        CancellationToken cancellationToken = default)
    {
        var today = clock.Today;
        var reportDate = date > today ? today : date;

        var outstandingTask = outstandingReports.QueryAsync(
            new OutstandingReportQuery(
                reportDate,
                BalanceFilter: OutstandingBalanceFilter.OutstandingOnly,
                IncludeInactiveCustomers: false),
            cancellationToken);

        var movementsTask = dailyMovementsReports.QueryAsync(
            new DailyMovementsReportQuery(
                reportDate,
                IncludeAdjustments: false),
            cancellationToken);

        await Task.WhenAll(outstandingTask, movementsTask);

        var outstanding = await outstandingTask;
        var movements = await movementsTask;
        var business = await businessInformation.GetAsync(cancellationToken);

        QuestPDF.Settings.License = LicenseType.Community;
        using var output = new MemoryStream();

        Document.Create(document =>
        {
            document.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(22);
                page.DefaultTextStyle(x => x.FontFamily("Arial").FontSize(8.2f));

                page.Header().Column(header =>
                {
                    header.Item().Text($"{business.ReportHeader} - Daily Print Pack")
                        .FontSize(17).SemiBold();
                    header.Item().PaddingTop(3).Text("Outstanding Summary")
                        .FontSize(12).SemiBold();
                    header.Item().Text($"As at {reportDate:dddd, dd/MM/yyyy}")
                        .FontColor(Colors.Grey.Darken1);
                });

                page.Content().PaddingVertical(8).Column(content =>
                {
                    var totals = outstanding.ContainerTotals
                        .Where(x => x.OutstandingQuantity > 0)
                        .Select(x => $"{x.ContainerType}: {x.OutstandingQuantity:N0} OUT")
                        .ToList();

                    content.Item().Text(
                        $"{outstanding.OutstandingPositionCount:N0} outstanding position(s)" +
                        (totals.Count > 0 ? "   •   " + string.Join("   •   ", totals) : string.Empty))
                        .SemiBold();

                    content.Item().PaddingTop(6).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(1.3f);
                            columns.RelativeColumn(3.2f);
                            columns.RelativeColumn(1.2f);
                            columns.RelativeColumn(1.5f);
                            columns.RelativeColumn(1.2f);
                            columns.RelativeColumn(1.3f);
                        });

                        Header(table, "Code");
                        Header(table, "Customer");
                        Header(table, "Type");
                        Header(table, "Container");
                        Header(table, "Position");
                        Header(table, "Last movement");

                        if (outstanding.Rows.Count == 0)
                        {
                            table.Cell().ColumnSpan(6).Padding(6)
                                .Text("No outstanding positions.").Italic();
                        }
                        else
                        {
                            foreach (var row in outstanding.Rows)
                            {
                                Body(table, row.CustomerCode);
                                Body(table, row.CustomerName);
                                Body(table, row.CustomerType == BinTracker.Core.CustomerType.Account
                                    ? "Account" : "Cash / COD");
                                Body(table, row.ContainerType);
                                Body(table, row.PositionText);
                                Body(table, row.LastMovementDate?.ToString("dd/MM/yyyy") ?? "—");
                            }
                        }
                    });
                });

                Footer(page, "Outstanding Summary");
            });

            document.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(18);
                page.DefaultTextStyle(x => x.FontFamily("Arial").FontSize(8.0f));

                page.Header().Column(header =>
                {
                    header.Item().Text($"{business.ReportHeader} - Daily Print Pack")
                        .FontSize(17).SemiBold();
                    header.Item().PaddingTop(3).Text("Movement Detail")
                        .FontSize(12).SemiBold();
                    header.Item().Text($"{reportDate:dddd, dd/MM/yyyy}")
                        .FontColor(Colors.Grey.Darken1);

                    var totals = movements.ContainerTotals.Select(x =>
                        $"{x.ContainerType}: {x.OutQuantity:N0} OUT / {x.InQuantity:N0} IN");
                    header.Item().PaddingTop(2).Text(
                        $"{movements.OutQuantity:N0} OUT • {movements.InQuantity:N0} IN" +
                        (movements.ContainerTotals.Count > 0
                            ? "   •   " + string.Join("   •   ", totals)
                            : string.Empty));
                });

                page.Content().PaddingVertical(8).Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(1.15f);
                        columns.RelativeColumn(2.35f);
                        columns.RelativeColumn(1.05f);
                        columns.RelativeColumn(1.25f);
                        columns.RelativeColumn(0.75f);
                        columns.RelativeColumn(0.65f);
                        columns.RelativeColumn(1.25f);
                        columns.RelativeColumn(1.55f);
                        columns.RelativeColumn(1.10f);
                    });

                    Header(table, "Code");
                    Header(table, "Customer");
                    Header(table, "Type");
                    Header(table, "Container");
                    Header(table, "Direction");
                    Header(table, "Qty");
                    Header(table, "Source");
                    Header(table, "Reference");
                    Header(table, "Entered by");

                    if (movements.Rows.Count == 0)
                    {
                        table.Cell().ColumnSpan(9).Padding(6)
                            .Text("No physical movements for this date.").Italic();
                    }
                    else
                    {
                        foreach (var row in movements.Rows)
                        {
                            Body(table, row.CustomerCode);
                            Body(table, row.CustomerName);
                            Body(table, row.CustomerType == BinTracker.Core.CustomerType.Account
                                ? "Account" : "Cash / COD");
                            Body(table, row.ContainerType);
                            Body(table, row.DirectionText);
                            Body(table, row.Quantity.ToString("N0"));
                            Body(table, row.SourceText);
                            Body(table, row.Reference);
                            Body(table, row.EnteredBy);
                        }
                    }
                });

                Footer(page, "Movement Detail");
            });
        }).GeneratePdf(output);

        await audit.WriteAsync(
            "DAILY_PRINT_PACK_GENERATED",
            "Report",
            reportDate.ToString("yyyy-MM-dd"),
            $"Daily Print Pack generated for {reportDate:dd/MM/yyyy}: " +
            $"{outstanding.Rows.Count:N0} outstanding row(s), " +
            $"{movements.Rows.Count:N0} movement row(s), " +
            $"{movements.OutQuantity:N0} OUT, {movements.InQuantity:N0} IN.",
            after: new
            {
                ReportDate = reportDate,
                OutstandingRows = outstanding.Rows.Count,
                MovementRows = movements.Rows.Count,
                movements.OutQuantity,
                movements.InQuantity,
            },
            cancellationToken: cancellationToken);

        return output.ToArray();
    }

    private static void Header(TableDescriptor table, string text) =>
        table.Cell().Background(Colors.Grey.Lighten2).Padding(4).Text(text).SemiBold();

    private static void Body(TableDescriptor table, string text) =>
        table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2)
            .PaddingVertical(2.4f).PaddingHorizontal(3).Text(text);

    private static void Footer(PageDescriptor page, string section) =>
        page.Footer().AlignCenter().Text(text =>
        {
            text.Span($"BinTracker Daily Print Pack - {section} • Page ");
            text.CurrentPageNumber();
            text.Span(" of ");
            text.TotalPages();
        });
}
