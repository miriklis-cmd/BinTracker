using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace BinTracker.Services;

public interface IWeeklyMovementsReportPdfService
{
    Task GeneratePdfAsync(
        WeeklyMovementsReportResult result,
        string outputPath,
        bool summaryView,
        bool includeNotes = false,
        CancellationToken cancellationToken = default);
}

internal sealed class WeeklyMovementsReportPdfService(
    IAuditService audit,
    IBusinessInformationService businessInformation)
    : IWeeklyMovementsReportPdfService
{
    public async Task GeneratePdfAsync(
        WeeklyMovementsReportResult result,
        string outputPath,
        bool summaryView,
        bool includeNotes = false,
        CancellationToken cancellationToken = default)
    {
        var business = await businessInformation.GetAsync(cancellationToken);
        QuestPDF.Settings.License = LicenseType.Community;

        Document.Create(document =>
        {
            document.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(18);
                page.DefaultTextStyle(x => x.FontFamily("Arial").FontSize(includeNotes ? 7.4f : 8.0f));

                page.Header().Column(header =>
                {
                    header.Item().Text($"{business.ReportHeader} - Weekly Movements").FontSize(17).SemiBold();
                    header.Item().PaddingTop(3)
                        .Text($"{result.WeekStart:dd/MM/yyyy} - {result.WeekEnd:dd/MM/yyyy} • {(summaryView ? "Customer / Container Summary" : "Movement Detail")}")
                        .FontSize(11).SemiBold();
                    header.Item().PaddingTop(2)
                        .Text($"{result.Rows.Count:N0} movement row(s) • {result.OutQuantity:N0} OUT • {result.InQuantity:N0} IN • Net {result.NetQuantity:+#;-#;0}")
                        .FontColor(Colors.Grey.Darken1);
                });

                page.Content().PaddingVertical(8).Table(table =>
                {
                    if (summaryView)
                        BuildSummary(table, result);
                    else
                        BuildDetail(table, result, includeNotes);
                });

                page.Footer().AlignCenter().Text(text =>
                {
                    text.Span("BinTracker Weekly Movements • Page ");
                    text.CurrentPageNumber();
                    text.Span(" of ");
                    text.TotalPages();
                });
            });
        }).GeneratePdf(outputPath);

        await audit.WriteAsync(
            "WEEKLY_MOVEMENTS_REPORT_GENERATED",
            "Report",
            $"{result.WeekStart:yyyy-MM-dd}:{result.WeekEnd:yyyy-MM-dd}",
            $"Weekly Movements PDF generated for {result.WeekStart:dd/MM/yyyy} - {result.WeekEnd:dd/MM/yyyy}: " +
            $"{result.Rows.Count:N0} row(s), {result.OutQuantity:N0} OUT, {result.InQuantity:N0} IN, view {(summaryView ? "summary" : "detail")}.",
            after: new
            {
                result.WeekStart,
                result.WeekEnd,
                RowCount = result.Rows.Count,
                SummaryRowCount = result.Summary.Count,
                result.OutQuantity,
                result.InQuantity,
                result.NetQuantity,
                View = summaryView ? "Summary" : "Detail",
                IncludeNotes = includeNotes,
                FileName = Path.GetFileName(outputPath)
            },
            cancellationToken: cancellationToken);
    }

    private static void BuildDetail(TableDescriptor table, WeeklyMovementsReportResult result, bool includeNotes)
    {
        table.ColumnsDefinition(columns =>
        {
            columns.RelativeColumn(1.05f); columns.RelativeColumn(1.15f); columns.RelativeColumn(includeNotes ? 1.70f : 2.05f);
            columns.RelativeColumn(1.00f); columns.RelativeColumn(1.20f); columns.RelativeColumn(0.75f); columns.RelativeColumn(0.60f);
            columns.RelativeColumn(1.15f); columns.RelativeColumn(1.20f);
            if (includeNotes) columns.RelativeColumn(1.75f);
            columns.RelativeColumn(1.00f);
        });
        foreach (var h in new[] { "Date", "Code", "Customer", "Type", "Container", "Direction", "Qty", "Source", "Reference" }) Header(table, h);
        if (includeNotes) Header(table, "Notes");
        Header(table, "Entered by");

        if (result.Rows.Count == 0)
        {
            table.Cell().ColumnSpan((uint)(includeNotes ? 11 : 10)).Padding(6).Text("No matching movements.").Italic();
            return;
        }

        foreach (var row in result.Rows)
        {
            Body(table, row.MovementDate.ToString("ddd dd/MM/yyyy")); Body(table, row.CustomerCode); Body(table, row.CustomerName);
            Body(table, row.CustomerType == BinTracker.Core.CustomerType.Account ? "Account" : "Cash / COD");
            Body(table, row.ContainerType); Body(table, row.DirectionText); Body(table, row.Quantity.ToString("N0"));
            Body(table, row.SourceText); Body(table, row.Reference); if (includeNotes) Body(table, row.Notes); Body(table, row.EnteredBy);
        }
    }

    private static void BuildSummary(TableDescriptor table, WeeklyMovementsReportResult result)
    {
        table.ColumnsDefinition(columns =>
        {
            columns.RelativeColumn(1.25f); columns.RelativeColumn(3.0f); columns.RelativeColumn(1.55f);
            columns.RelativeColumn(0.9f); columns.RelativeColumn(0.9f); columns.RelativeColumn(0.9f);
        });
        foreach (var h in new[] { "Code", "Customer", "Container", "OUT", "IN", "Net" }) Header(table, h);
        if (result.Summary.Count == 0)
        {
            table.Cell().ColumnSpan(6u).Padding(6).Text("No matching movements.").Italic();
            return;
        }
        foreach (var row in result.Summary)
        {
            Body(table, row.CustomerCode); Body(table, row.CustomerName); Body(table, row.ContainerType);
            Body(table, row.OutQuantity.ToString("N0")); Body(table, row.InQuantity.ToString("N0")); Body(table, row.NetQuantity.ToString("N0"));
        }
    }

    private static void Header(TableDescriptor table, string text) => table.Cell().Background(Colors.Grey.Lighten2).Padding(4).Text(text).SemiBold();
    private static void Body(TableDescriptor table, string text) => table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(2.2f).PaddingHorizontal(3).Text(text ?? "");
}
