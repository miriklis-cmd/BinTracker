using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace BinTracker.Services;

public interface IMovementHistoryReportPdfService
{
    Task<byte[]> BuildPdfAsync(
        MovementHistoryReportResult result,
        bool includeNotes = false,
        CancellationToken cancellationToken = default);
}

internal sealed class MovementHistoryReportPdfService(
    IAuditService audit,
    IBusinessInformationService businessInformation)
    : IMovementHistoryReportPdfService
{
    public async Task<byte[]> BuildPdfAsync(
        MovementHistoryReportResult result,
        bool includeNotes = false,
        CancellationToken cancellationToken = default)
    {
        var business =
            await businessInformation.GetAsync(cancellationToken);

        QuestPDF.Settings.License = LicenseType.Community;
        using var output = new MemoryStream();

        Document.Create(document =>
        {
            document.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(18);
                page.DefaultTextStyle(x =>
                    x.FontFamily("Arial")
                        .FontSize(includeNotes ? 7.4f : 8.0f));

                page.Header().Column(header =>
                {
                    header.Item()
                        .Text($"{business.ReportHeader} - Movement History")
                        .FontSize(17)
                        .SemiBold();

                    header.Item()
                        .PaddingTop(3)
                        .Text(
                            $"{result.StartDate:dd/MM/yyyy} - {result.EndDate:dd/MM/yyyy}")
                        .FontSize(11)
                        .SemiBold();

                    var totals = result.ContainerTotals.Select(x =>
                        $"{x.ContainerType}: {x.OutQuantity:N0} OUT / " +
                        $"{x.InQuantity:N0} IN / Net {x.NetQuantity:+#;-#;0}");

                    header.Item()
                        .PaddingTop(2)
                        .Text(
                            $"{result.Rows.Count:N0} row(s) • " +
                            $"{result.OutQuantity:N0} OUT • " +
                            $"{result.InQuantity:N0} IN • " +
                            $"Net {result.NetQuantity:+#;-#;0}" +
                            (result.ContainerTotals.Count > 0
                                ? "   •   " +
                                  string.Join("   •   ", totals)
                                : ""))
                        .FontColor(Colors.Grey.Darken1);
                });

                page.Content()
                    .PaddingVertical(8)
                    .Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(1.05f); // date
                            columns.RelativeColumn(1.10f); // code
                            columns.RelativeColumn(includeNotes ? 1.65f : 2.05f); // customer
                            columns.RelativeColumn(1.00f); // type
                            columns.RelativeColumn(1.20f); // container
                            columns.RelativeColumn(0.78f); // dir
                            columns.RelativeColumn(0.65f); // qty
                            columns.RelativeColumn(1.15f); // source
                            columns.RelativeColumn(includeNotes ? 1.00f : 1.30f); // ref
                            if (includeNotes)
                                columns.RelativeColumn(1.75f);
                            columns.RelativeColumn(1.00f); // entered by
                        });

                        Header(table, "Date");
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
                                .ColumnSpan((uint)(includeNotes ? 11 : 10))
                                .Padding(6)
                                .Text("No matching movements.")
                                .Italic();
                        }
                        else
                        {
                            foreach (var row in result.Rows)
                            {
                                Body(table, row.MovementDate.ToString("dd/MM/yyyy"));
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
                        text.Span("BinTracker Movement History • Page ");
                        text.CurrentPageNumber();
                        text.Span(" of ");
                        text.TotalPages();
                    });
            });
        }).GeneratePdf(output);

        await audit.WriteAsync(
            "MOVEMENT_HISTORY_REPORT_GENERATED",
            "Report",
            $"{result.StartDate:yyyy-MM-dd}:{result.EndDate:yyyy-MM-dd}",
            $"Movement History PDF generated for " +
            $"{result.StartDate:dd/MM/yyyy} - {result.EndDate:dd/MM/yyyy}: " +
            $"{result.Rows.Count:N0} row(s), " +
            $"{result.OutQuantity:N0} OUT, " +
            $"{result.InQuantity:N0} IN.",
            after: new
            {
                result.StartDate,
                result.EndDate,
                RowCount = result.Rows.Count,
                result.OutQuantity,
                result.InQuantity,
                result.NetQuantity,
                IncludeNotes = includeNotes,
            },
            cancellationToken: cancellationToken);

        return output.ToArray();
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
