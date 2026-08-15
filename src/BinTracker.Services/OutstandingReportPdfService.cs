using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace BinTracker.Services;

public interface IOutstandingReportPdfService
{
    Task GeneratePdfAsync(
        OutstandingReportResult result,
        string outputPath,
        CancellationToken cancellationToken = default);
}

internal sealed class OutstandingReportPdfService(
    IAuditService audit,
    IBusinessInformationService businessInformation)
    : IOutstandingReportPdfService
{
    public async Task GeneratePdfAsync(
        OutstandingReportResult result,
        string outputPath,
        CancellationToken cancellationToken = default)
    {
        var business = await businessInformation.GetAsync(cancellationToken);
        QuestPDF.Settings.License = LicenseType.Community;

        Document.Create(document =>
        {
            document.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(24);
                page.DefaultTextStyle(x => x.FontFamily("Arial").FontSize(8.5f));

                page.Header().Column(header =>
                {
                    header.Item().Text($"{business.ReportHeader} - Outstanding Containers")
                        .FontSize(17).SemiBold();
                    header.Item().PaddingTop(3)
                        .Text($"As at {result.AsOfDate:dd/MM/yyyy}")
                        .FontSize(11).SemiBold();

                    var totals = result.ContainerTotals.Select(x =>
                        $"{x.ContainerType}: {x.OutstandingQuantity:N0} OUT" +
                        (x.CreditQuantity > 0 ? $" / {x.CreditQuantity:N0} CREDIT" : ""));
                    header.Item().PaddingTop(2)
                        .Text(string.Join("   •   ", totals))
                        .FontColor(Colors.Grey.Darken1);
                });

                page.Content().PaddingVertical(10).Column(content =>
                {
                    content.Item().Text(
                        $"{result.OutstandingPositionCount:N0} outstanding position(s)" +
                        (result.CreditPositionCount > 0
                            ? $" • {result.CreditPositionCount:N0} credit position(s)"
                            : ""));

                    content.Item().PaddingTop(6).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(1.35f);
                            columns.RelativeColumn(3.1f);
                            columns.RelativeColumn(1.25f);
                            columns.RelativeColumn(1.55f);
                            columns.RelativeColumn(1.35f);
                            columns.RelativeColumn(1.35f);
                            columns.RelativeColumn(1.0f);
                        });

                        Header(table, "Code");
                        Header(table, "Customer");
                        Header(table, "Type");
                        Header(table, "Container");
                        Header(table, "Position");
                        Header(table, "Last movement");
                        Header(table, "Status");

                        if (result.Rows.Count == 0)
                        {
                            table.Cell().ColumnSpan(7).Padding(6)
                                .Text("No matching outstanding positions.").Italic();
                        }
                        else
                        {
                            foreach (var row in result.Rows)
                            {
                                Body(table, row.CustomerCode);
                                Body(table, row.CustomerName);
                                Body(table, row.CustomerType == BinTracker.Core.CustomerType.Account
                                    ? "Account" : "Cash / COD");
                                Body(table, row.ContainerType);
                                Body(table, row.PositionText);
                                Body(table, row.LastMovementDate?.ToString("dd/MM/yyyy") ?? "—");
                                Body(table, row.IsActive ? "Active" : "Inactive");
                            }
                        }
                    });
                });

                page.Footer().AlignCenter().Text(text =>
                {
                    text.Span("BinTracker outstanding containers  •  Page ");
                    text.CurrentPageNumber();
                    text.Span(" of ");
                    text.TotalPages();
                });
            });
        }).GeneratePdf(outputPath);

        await audit.WriteAsync(
            "OUTSTANDING_REPORT_GENERATED",
            "Report",
            result.AsOfDate.ToString("yyyy-MM-dd"),
            $"Outstanding Containers PDF generated as at {result.AsOfDate:dd/MM/yyyy}: " +
            $"{result.Rows.Count:N0} position row(s).",
            after: new
            {
                AsOfDate = result.AsOfDate,
                RowCount = result.Rows.Count,
                FileName = Path.GetFileName(outputPath)
            },
            cancellationToken: cancellationToken);
    }

    private static void Header(TableDescriptor table, string text) =>
        table.Cell().Background(Colors.Grey.Lighten2).Padding(4).Text(text).SemiBold();

    private static void Body(TableDescriptor table, string text) =>
        table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2)
            .PaddingVertical(3).PaddingHorizontal(4).Text(text);
}
