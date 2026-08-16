using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace BinTracker.Services;

public interface IDailyMovementsReportPdfService
{
    Task GeneratePdfAsync(
        DailyMovementsReportResult result,
        string outputPath,
        bool includeNotes = false,
        CancellationToken cancellationToken = default);
}

internal sealed class DailyMovementsReportPdfService(
    IAuditService audit,
    IBusinessInformationService businessInformation)
    : IDailyMovementsReportPdfService
{
    public async Task GeneratePdfAsync(
        DailyMovementsReportResult result,
        string outputPath,
        bool includeNotes = false,
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
                page.Margin(18);
                page.DefaultTextStyle(
                    x => x.FontFamily("Arial").FontSize(includeNotes ? 7.6f : 8.0f));

                page.Header().Column(header =>
                {
                    header.Item()
                        .Text($"{business.ReportHeader} - Daily Movements")
                        .FontSize(17)
                        .SemiBold();

                    header.Item()
                        .PaddingTop(3)
                        .Text($"{result.ReportDate:dddd, dd/MM/yyyy}")
                        .FontSize(11)
                        .SemiBold();

                    var totals = result.ContainerTotals.Select(x =>
                        $"{x.ContainerType}: {x.OutQuantity:N0} OUT / {x.InQuantity:N0} IN");

                    header.Item()
                        .PaddingTop(2)
                        .Text(
                            $"{result.OutQuantity:N0} OUT • {result.InQuantity:N0} IN" +
                            (result.ContainerTotals.Count > 0
                                ? "   •   " + string.Join("   •   ", totals)
                                : ""))
                        .FontColor(Colors.Grey.Darken1);
                });

                page.Content()
                    .PaddingVertical(8)
                    .Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(1.20f); // code
                            columns.RelativeColumn(includeNotes ? 1.85f : 2.20f); // customer
                            columns.RelativeColumn(1.10f); // type
                            columns.RelativeColumn(1.30f); // container
                            columns.RelativeColumn(0.80f); // direction
                            columns.RelativeColumn(0.70f); // qty
                            columns.RelativeColumn(1.25f); // source
                            columns.RelativeColumn(includeNotes ? 1.20f : 1.50f); // reference

                            if (includeNotes)
                                columns.RelativeColumn(2.00f); // notes

                            columns.RelativeColumn(1.10f); // entered
                        });

                        Header(table, "Code");
                        Header(table, "Customer");
                        Header(table, "Type");
                        Header(table, "Container");
                        Header(table, "Direction");
                        Header(table, "Qty");
                        Header(table, "Source");
                        Header(table, "Reference");

                        if (includeNotes)
                            Header(table, "Notes");

                        Header(table, "Entered by");

                        if (result.Rows.Count == 0)
                        {
                            table.Cell()
                                .ColumnSpan((uint)(includeNotes ? 10 : 9))
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
                                Body(
                                    table,
                                    row.CustomerType ==
                                        BinTracker.Core.CustomerType.Account
                                            ? "Account"
                                            : "Cash / COD");
                                Body(table, row.ContainerType);
                                Body(table, row.DirectionText);
                                Body(table, row.Quantity.ToString("N0"));
                                Body(table, row.SourceText);
                                Body(table, row.Reference);

                                if (includeNotes)
                                    Body(table, row.Notes);

                                Body(table, row.EnteredBy);
                            }
                        }
                    });

                page.Footer()
                    .AlignCenter()
                    .Text(text =>
                    {
                        text.Span("BinTracker Daily Movements • Page ");
                        text.CurrentPageNumber();
                        text.Span(" of ");
                        text.TotalPages();
                    });
            });
        }).GeneratePdf(outputPath);

        await audit.WriteAsync(
            "DAILY_MOVEMENTS_REPORT_GENERATED",
            "Report",
            result.ReportDate.ToString("yyyy-MM-dd"),
            $"Daily Movements PDF generated for {result.ReportDate:dd/MM/yyyy}: " +
            $"{result.Rows.Count:N0} row(s), " +
            $"{result.OutQuantity:N0} OUT, {result.InQuantity:N0} IN.",
            after: new
            {
                result.ReportDate,
                RowCount = result.Rows.Count,
                result.OutQuantity,
                result.InQuantity,
                IncludeNotes = includeNotes,
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
            .Text(text);
}
