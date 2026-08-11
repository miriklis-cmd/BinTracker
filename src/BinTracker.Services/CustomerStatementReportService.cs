using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace BinTracker.Services;

public interface ICustomerStatementReportService
{
    Task GeneratePdfAsync(int customerId, DateOnly fromDate, DateOnly toDate, string outputPath, CancellationToken cancellationToken = default);
}

internal sealed class CustomerStatementReportService(
    ICustomerService customers,
    IAuditService audit,
    IBusinessInformationService businessInformation) : ICustomerStatementReportService
{
    public async Task GeneratePdfAsync(int customerId, DateOnly fromDate, DateOnly toDate, string outputPath, CancellationToken cancellationToken = default)
    {
        var data = await customers.GetStatementAsync(customerId, fromDate, toDate, cancellationToken);
        var business = await businessInformation.GetAsync(cancellationToken);
        QuestPDF.Settings.License = LicenseType.Community;

        Document.Create(document =>
        {
            document.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(28);
                page.DefaultTextStyle(x => x.FontFamily("Arial").FontSize(9));

                page.Header().Column(header =>
                {
                    header.Item().Text($"{business.ReportHeader} - Customer Statement").FontSize(18).SemiBold();
                    header.Item().PaddingTop(4).Text($"{data.CustomerCode} - {data.CustomerName}").FontSize(13).SemiBold();
                    header.Item().Text($"Statement period: {data.FromDate:dd/MM/yyyy} to {data.ToDate:dd/MM/yyyy}");
                    header.Item().Text($"Generated: {DateTime.Now:dd/MM/yyyy HH:mm}").FontColor(Colors.Grey.Darken1);
                });

                page.Content().PaddingVertical(12).Column(content =>
                {
                    if (data.Containers.Count == 0)
                    {
                        content.Item().PaddingTop(20).Text("No container movements or balances were found for this statement period.");
                        return;
                    }

                    content.Item().Text("Current Position").FontSize(13).SemiBold();
                    content.Item().PaddingTop(5).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(3);
                            columns.RelativeColumn();
                            columns.RelativeColumn(2);
                        });
                        HeaderCell(table, "Container");
                        HeaderCell(table, "Balance");
                        HeaderCell(table, "Position");
                        foreach (var container in data.Containers)
                        {
                            BodyCell(table, container.ContainerType);
                            BodyCell(table, container.ClosingBalance.ToString());
                            BodyCell(table, Position(container.ClosingBalance));
                        }
                    });

                    foreach (var container in data.Containers)
                    {
                        content.Item().PaddingTop(18).Text(container.ContainerType).FontSize(12).SemiBold();
                        content.Item().Text($"Opening position: {Position(container.OpeningBalance)}").FontColor(Colors.Grey.Darken1);
                        content.Item().PaddingTop(5).Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.ConstantColumn(70);
                                columns.RelativeColumn(2);
                                columns.ConstantColumn(55);
                                columns.ConstantColumn(80);
                                columns.RelativeColumn(2);
                            });
                            HeaderCell(table, "Date");
                            HeaderCell(table, "Movement");
                            HeaderCell(table, "Qty");
                            HeaderCell(table, "Balance");
                            HeaderCell(table, "Reference");

                            if (container.Movements.Count == 0)
                            {
                                table.Cell().ColumnSpan(5).Padding(5).Text("No movements in this period.").Italic();
                            }
                            else
                            {
                                foreach (var movement in container.Movements)
                                {
                                    BodyCell(table, movement.Date.ToString("dd/MM/yyyy"));
                                    BodyCell(table, movement.Direction);
                                    BodyCell(table, movement.Quantity.ToString());
                                    BodyCell(table, Position(movement.RunningBalance));
                                    BodyCell(table, movement.Reference ?? string.Empty);
                                }
                            }
                        });
                        content.Item().PaddingTop(4).AlignRight().Text($"Closing position: {Position(container.ClosingBalance)}").SemiBold();
                    }
                });

                page.Footer().AlignCenter().Text(text =>
                {
                    text.Span("BinTracker customer statement  •  Page ");
                    text.CurrentPageNumber();
                    text.Span(" of ");
                    text.TotalPages();
                });
            });
        }).GeneratePdf(outputPath);

        await audit.WriteAsync(
            "CUSTOMER_STATEMENT_GENERATED",
            "Customer",
            data.CustomerId.ToString(),
            $"Customer statement generated for '{data.CustomerCode} - {data.CustomerName}' ({fromDate:dd/MM/yyyy} to {toDate:dd/MM/yyyy}).",
            after: new { data.CustomerCode, FromDate = fromDate, ToDate = toDate, FileName = Path.GetFileName(outputPath) },
            cancellationToken: cancellationToken);
    }

    private static string Position(int balance) => balance switch
    {
        > 0 => $"{balance} OUT",
        < 0 => $"{Math.Abs(balance)} CREDIT",
        _ => "Even"
    };

    private static void HeaderCell(TableDescriptor table, string text) =>
        table.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text(text).SemiBold();

    private static void BodyCell(TableDescriptor table, string text) =>
        table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(text);
}
