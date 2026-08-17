using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace BinTracker.Services;

public interface IMonthlySummaryReportPdfService
{
    Task GeneratePdfAsync(
        MonthlySummaryReportResult result,
        string outputPath,
        CancellationToken cancellationToken = default);
}

internal sealed class MonthlySummaryReportPdfService(
    IAuditService audit,
    IBusinessInformationService businessInformation)
    : IMonthlySummaryReportPdfService
{
    public async Task GeneratePdfAsync(
        MonthlySummaryReportResult result,
        string outputPath,
        CancellationToken cancellationToken = default)
    {
        var business =
            await businessInformation.GetAsync(cancellationToken);

        QuestPDF.Settings.License = LicenseType.Community;

        Document.Create(document =>
        {
            document.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(20);
                page.DefaultTextStyle(x =>
                    x.FontFamily("Arial").FontSize(8.5f));

                page.Header().Column(header =>
                {
                    header.Item()
                        .Text($"{business.ReportHeader} - Monthly Summary")
                        .FontSize(17)
                        .SemiBold();

                    header.Item()
                        .PaddingTop(3)
                        .Text(result.MonthStart.ToString("MMMM yyyy"))
                        .FontSize(11)
                        .SemiBold();

                    var periodText =
                        result.DataThroughDate < result.MonthEnd
                            ? $"{result.MonthStart:dd/MM/yyyy} - {result.MonthEnd:dd/MM/yyyy} " +
                              $"(activity through {result.DataThroughDate:dd/MM/yyyy})"
                            : $"{result.MonthStart:dd/MM/yyyy} - {result.MonthEnd:dd/MM/yyyy}";

                    header.Item()
                        .PaddingTop(2)
                        .Text(
                            $"{periodText} • " +
                            $"{result.OutQuantity:N0} OUT • " +
                            $"{result.InQuantity:N0} IN • " +
                            $"Net {result.NetQuantity:+#;-#;0}")
                        .FontColor(Colors.Grey.Darken1);

                    if (result.ContainerTotals.Count > 0)
                    {
                        header.Item()
                            .PaddingTop(2)
                            .Text(string.Join(
                                "   •   ",
                                result.ContainerTotals.Select(x =>
                                    $"{x.ContainerType}: " +
                                    $"{x.OutQuantity:N0} OUT / " +
                                    $"{x.InQuantity:N0} IN / " +
                                    $"Net {x.NetQuantity:+#;-#;0}")))
                            .FontColor(Colors.Grey.Darken1);
                    }
                });

                page.Content()
                    .PaddingVertical(8)
                    .Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(1.25f);
                            columns.RelativeColumn(3.0f);
                            columns.RelativeColumn(1.55f);
                            columns.RelativeColumn(0.9f);
                            columns.RelativeColumn(0.9f);
                            columns.RelativeColumn(0.9f);
                        });

                        foreach (var heading in new[]
                                 {
                                     "Code", "Customer", "Container",
                                     "OUT", "IN", "Net"
                                 })
                        {
                            Header(table, heading);
                        }

                        if (result.Rows.Count == 0)
                        {
                            table.Cell()
                                .ColumnSpan(6u)
                                .Padding(6)
                                .Text("No matching movements.")
                                .Italic();
                        }
                        else
                        {
                            foreach (var row in result.Rows)
                            {
                                Body(table, row.CustomerCode);
                                Body(table, row.CustomerName);
                                Body(table, row.ContainerType);
                                Body(table, row.OutQuantity.ToString("N0"));
                                Body(table, row.InQuantity.ToString("N0"));
                                Body(table, row.NetQuantity.ToString("N0"));
                            }
                        }
                    });

                page.Footer()
                    .AlignCenter()
                    .Text(text =>
                    {
                        text.Span("BinTracker Monthly Summary • Page ");
                        text.CurrentPageNumber();
                        text.Span(" of ");
                        text.TotalPages();
                    });
            });
        }).GeneratePdf(outputPath);

        await audit.WriteAsync(
            "MONTHLY_SUMMARY_REPORT_GENERATED",
            "Report",
            $"{result.MonthStart:yyyy-MM}",
            $"Monthly Summary PDF generated for {result.MonthStart:MMMM yyyy}: " +
            $"{result.Rows.Count:N0} row(s), " +
            $"{result.OutQuantity:N0} OUT, " +
            $"{result.InQuantity:N0} IN, " +
            $"Net {result.NetQuantity:+#;-#;0}.",
            after: new
            {
                result.MonthStart,
                result.MonthEnd,
                result.DataThroughDate,
                RowCount = result.Rows.Count,
                result.OutQuantity,
                result.InQuantity,
                result.NetQuantity,
                FileName = Path.GetFileName(outputPath)
            },
            cancellationToken: cancellationToken);
    }

    private static void Header(
        TableDescriptor table,
        string text) =>
        table.Cell()
            .Background(Colors.Grey.Lighten2)
            .Padding(4)
            .Text(text)
            .SemiBold();

    private static void Body(
        TableDescriptor table,
        string text) =>
        table.Cell()
            .BorderBottom(0.5f)
            .BorderColor(Colors.Grey.Lighten2)
            .PaddingVertical(2.2f)
            .PaddingHorizontal(3)
            .Text(text ?? "");
}
