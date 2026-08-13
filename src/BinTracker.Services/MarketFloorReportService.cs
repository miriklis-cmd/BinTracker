using BinTracker.Core;
using BinTracker.Data;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace BinTracker.Services;

public sealed record MarketFloorFrontRow(
    int CustomerId,
    string Buyer,
    CustomerType CustomerType,
    int Total);

public sealed record MarketFloorSpecialRow(
    string Buyer,
    string Container,
    int Balance);

public sealed record MarketFloorReverseRow(
    int CustomerId,
    string Buyer,
    CustomerType CustomerType,
    int Out,
    int In,
    int BroughtForward,
    int Total);

public sealed record MarketFloorReportData(
    DateOnly Date,
    IReadOnlyList<MarketFloorFrontRow> AccountOwing,
    IReadOnlyList<MarketFloorFrontRow> CashOwing,
    IReadOnlyList<MarketFloorFrontRow> Credits,
    IReadOnlyList<MarketFloorSpecialRow> SpecialContainers,
    IReadOnlyList<MarketFloorReverseRow> AccountDaily,
    IReadOnlyList<MarketFloorReverseRow> CashDaily);

public interface IMarketFloorReportService
{
    Task<MarketFloorReportData> GetAsync(
        DateOnly date,
        CancellationToken cancellationToken = default);

    Task GeneratePdfAsync(
        DateOnly date,
        string outputPath,
        CancellationToken cancellationToken = default);
}

internal sealed class MarketFloorReportService(
    IDbContextFactory<BinTrackerDbContext> factory,
    IAuditService audit,
    IBusinessInformationService businessInformation) : IMarketFloorReportService
{
    public async Task<MarketFloorReportData> GetAsync(
        DateOnly date,
        CancellationToken cancellationToken = default)
    {
        await using var db = await factory.CreateDbContextAsync(cancellationToken);

        var customers = await db.Customers
            .AsNoTracking()
            .Where(x => x.IsActive)
            .Select(x => new
            {
                x.Id,
                Buyer = x.CustomerCode == null || x.CustomerCode == string.Empty
                    ? x.Name
                    : x.CustomerCode,
                x.Name,
                x.CustomerType
            })
            .OrderBy(x => x.Buyer)
            .ToListAsync(cancellationToken);

        var containerTypes = await db.ContainerTypes
            .AsNoTracking()
            .Where(x => x.IsActive)
            .Select(x => new { x.Id, x.Name, x.IsSpecialFloorReportContainer })
            .ToListAsync(cancellationToken);

        // CHEP, LOSCAM and similar pool/pallet types belong in the special
        // bottom-right block rather than the ordinary floor-bin total.
        var specialContainerIds = containerTypes
            .Where(x => x.IsSpecialFloorReportContainer)
            .Select(x => x.Id)
            .ToHashSet();

        var movements = await db.BinMovements
            .AsNoTracking()
            .Where(x => x.MovementDate <= date)
            .Select(x => new
            {
                x.CustomerId,
                x.ContainerTypeId,
                x.MovementDate,
                x.MovementType,
                x.Source,
                x.Quantity
            })
            .ToListAsync(cancellationToken);

        var customerById = customers.ToDictionary(x => x.Id);
        var containerById = containerTypes.ToDictionary(x => x.Id, x => x.Name);

        var regular = movements
            .Where(x => !specialContainerIds.Contains(x.ContainerTypeId))
            .ToList();

        var reverse = customers
            .Select(customer =>
            {
                var rows = regular.Where(x => x.CustomerId == customer.Id);

                // Opening adjustments are bookkeeping/cutover position,
                // not physical movements for the market-floor daily columns.
                var bfwd = rows
                    .Where(x =>
                        x.MovementDate < date ||
                        (x.MovementDate == date &&
                         x.Source == MovementSource.Adjustment))
                    .Sum(x =>
                        x.MovementType == MovementType.Out
                            ? x.Quantity
                            : -x.Quantity);

                var day = rows
                    .Where(x =>
                        x.MovementDate == date &&
                        x.Source != MovementSource.Adjustment)
                    .ToList();

                var outs = day
                    .Where(x => x.MovementType == MovementType.Out)
                    .Sum(x => x.Quantity);

                var ins = day
                    .Where(x => x.MovementType == MovementType.In)
                    .Sum(x => x.Quantity);

                var total = bfwd + outs - ins;

                return new MarketFloorReverseRow(
                    customer.Id,
                    customer.Buyer,
                    customer.CustomerType,
                    outs,
                    ins,
                    bfwd,
                    total);
            })
            .OrderBy(x => x.Buyer, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var front = reverse
            .Select(x => new MarketFloorFrontRow(
                x.CustomerId,
                x.Buyer,
                x.CustomerType,
                x.Total))
            .ToList();

        var accountOwing = front
            .Where(x => x.CustomerType == CustomerType.Account && x.Total > 0)
            .OrderBy(x => x.Buyer, StringComparer.OrdinalIgnoreCase)
            .ToList();

        // Cash/COD customers stay together in the Cash area whether they
        // owe bins or are in credit. This mirrors the market-floor workflow.
        var cashOwing = front
            .Where(x =>
                x.CustomerType == CustomerType.CashCod &&
                x.Total != 0)
            .OrderBy(x => x.Buyer, StringComparer.OrdinalIgnoreCase)
            .ToList();

        // Only Account-customer credits belong in the separate CREDIT block.
        var credits = front
            .Where(x =>
                x.CustomerType == CustomerType.Account &&
                x.Total < 0)
            .OrderBy(x => x.Buyer, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var special = movements
            .Where(x =>
                specialContainerIds.Contains(x.ContainerTypeId) &&
                customerById.ContainsKey(x.CustomerId))
            .GroupBy(x => new { x.CustomerId, x.ContainerTypeId })
            .Select(g => new
            {
                g.Key.CustomerId,
                g.Key.ContainerTypeId,
                Balance = g.Sum(x => x.MovementType == MovementType.Out ? x.Quantity : -x.Quantity)
            })
            .Where(x => x.Balance != 0)
            .Select(x => new MarketFloorSpecialRow(
                customerById[x.CustomerId].Buyer,
                containerById.GetValueOrDefault(x.ContainerTypeId, "Special"),
                x.Balance))
            .OrderBy(x => x.Buyer, StringComparer.OrdinalIgnoreCase)
            .ThenBy(x => x.Container, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return new MarketFloorReportData(
            date,
            accountOwing,
            cashOwing,
            credits,
            special,
            reverse.Where(x => x.CustomerType == CustomerType.Account).ToList(),
            reverse.Where(x => x.CustomerType == CustomerType.CashCod).ToList());
    }

    public async Task GeneratePdfAsync(
        DateOnly date,
        string outputPath,
        CancellationToken cancellationToken = default)
    {
        var data = await GetAsync(date, cancellationToken);
        var business = await businessInformation.GetAsync(cancellationToken);

        QuestPDF.Settings.License = LicenseType.Community;

        Document.Create(document =>
        {
            // PAGE 1: front market-floor position sheet.
            document.Page(page =>
            {
                var frontFontSize = FrontPageFontSize(data);

                page.Size(PageSizes.A4);
                page.Margin(7);
                page.DefaultTextStyle(x =>
                    x.FontFamily("Arial").FontSize(frontFontSize));

                page.Header().Column(header =>
                {
                    header.Item().Text(business.ReportHeader)
                        .FontSize(frontFontSize + 1.2f).SemiBold();
                    header.Item().Row(row =>
                    {
                    row.RelativeItem().Text("MARKET FLOOR BIN POSITION")
                        .FontSize(frontFontSize + 4.2f).SemiBold();
                    row.ConstantItem(180).AlignRight()
                        .Text(data.Date.ToString("dddd dd/MM/yyyy"))
                        .FontSize(frontFontSize + 0.5f);
                    });
                });

                page.Content().PaddingTop(6).Row(row =>
                {
                    var split = (int)Math.Ceiling(data.AccountOwing.Count / 2d);
                    var accountLeft = data.AccountOwing.Take(split).ToList();
                    var accountRight = data.AccountOwing.Skip(split).ToList();

                    row.RelativeItem().PaddingRight(2).Element(c =>
                        FrontBuyerTable(c, "ACCOUNT - OWING", accountLeft));

                    row.RelativeItem().PaddingHorizontal(1).Element(c =>
                        FrontBuyerTable(c, "ACCOUNT - OWING", accountRight));

                    row.RelativeItem().PaddingLeft(2).Column(right =>
                    {
                        right.Item().Element(c =>
                            FrontBuyerTable(c, "CASH - OWING", data.CashOwing));

                        right.Item().PaddingTop(8).Background(Colors.Yellow.Lighten3)
                            .Padding(3).Text("CREDIT").SemiBold();

                        right.Item().Element(c =>
                            CreditTable(c, data.Credits));

                        right.Item().PaddingTop(10).Element(c =>
                            SpecialTable(c, data.SpecialContainers));
                    });
                });

                page.Footer().Row(row =>
                {
                    row.RelativeItem().Text("BinTracker Market Floor Sheet")
                        .FontColor(Colors.Grey.Darken1);
                    row.ConstantItem(220).AlignRight()
                        .Text($"Generated {DateTime.Now:dd/MM/yyyy HH:mm}")
                        .FontColor(Colors.Grey.Darken1);
                });
            });

            // PAGE 2: existing reverse-side daily worksheet.
            document.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(12);
                page.DefaultTextStyle(x => x.FontFamily("Arial").FontSize(7.8f));

                page.Header().Column(header =>
                {
                    header.Item().Text(business.ReportHeader)
                        .FontSize(8).SemiBold();
                    header.Item().Row(row =>
                    {
                        row.RelativeItem().Text("DAILY BIN MOVEMENT - REVERSE SIDE")
                            .FontSize(11).SemiBold();
                        row.ConstantItem(180).AlignRight()
                            .Text(data.Date.ToString("dddd dd/MM/yyyy"))
                            .FontSize(7.5f);
                    });
                });

                page.Content().PaddingTop(6).Row(row =>
                {
                    var accountSplit =
                        (int)Math.Ceiling(data.AccountDaily.Count / 2d);

                    var accountLeft =
                        data.AccountDaily.Take(accountSplit).ToList();
                    var accountRight =
                        data.AccountDaily.Skip(accountSplit).ToList();

                    row.RelativeItem().PaddingRight(2).Element(c =>
                        ReverseTable(c, "ACCOUNT CUSTOMERS", accountLeft));

                    row.RelativeItem().PaddingHorizontal(1).Element(c =>
                        ReverseTable(c, "ACCOUNT CUSTOMERS", accountRight));

                    row.RelativeItem().PaddingLeft(2).Element(c =>
                        ReverseTable(c, "CASH CUSTOMERS", data.CashDaily));
                });

                page.Footer().Row(row =>
                {
                    row.RelativeItem().Text("B/Fwd = position before today's movements")
                        .FontColor(Colors.Grey.Darken1);
                    row.ConstantItem(220).AlignRight()
                        .Text($"Generated {DateTime.Now:dd/MM/yyyy HH:mm}")
                        .FontColor(Colors.Grey.Darken1);
                });
            });
        }).GeneratePdf(outputPath);

        await audit.WriteAsync(
            "MARKET_FLOOR_REPORT_GENERATED",
            "Report",
            data.Date.ToString("yyyy-MM-dd"),
            $"Market Floor Sheet generated for {data.Date:dd/MM/yyyy}.",
            after: new
            {
                Date = data.Date,
                AccountOwing = data.AccountOwing.Count,
                CashOwing = data.CashOwing.Count,
                Credits = data.Credits.Count,
                Special = data.SpecialContainers.Count,
                FileName = Path.GetFileName(outputPath)
            },
            cancellationToken: cancellationToken);
    }



    private static void FrontBuyerTable(
        IContainer container,
        string heading,
        IReadOnlyList<MarketFloorFrontRow> rows)
    {
        container.Column(column =>
        {
            column.Item().Background(Colors.Grey.Lighten2).Padding(3)
                .Text(heading).SemiBold();

            column.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(4);
                    columns.RelativeColumn(2);
                });

                FrontHeader(table, "Buyer");
                FrontHeader(table, "Total");

                foreach (var item in rows)
                {
                    FrontCell(table, item.Buyer);
                    FrontCell(table, FormatTotal(item.Total));
                }
            });
        });
    }

    private static void CreditTable(
        IContainer container,
        IReadOnlyList<MarketFloorFrontRow> rows)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.RelativeColumn(4);
                columns.RelativeColumn(2);
            });

            foreach (var item in rows)
            {
                FrontCell(table, item.Buyer);
                FrontCell(table, $"{Math.Abs(item.Total)}\u00A0CREDIT");
            }
        });
    }

    private static void SpecialTable(
        IContainer container,
        IReadOnlyList<MarketFloorSpecialRow> rows)
    {
        if (rows.Count == 0)
            return;

        container.Column(column =>
        {
            column.Item().Background(Colors.Grey.Lighten2).Padding(3)
                .Text("SPECIAL CONTAINERS").SemiBold();

            column.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(3);
                    columns.RelativeColumn(2);
                });

                foreach (var item in rows)
                {
                    FrontCell(table, item.Buyer);

                    var text = item.Balance > 0
                        ? $"{item.Balance} {ShortContainerName(item.Container)}"
                        : $"{Math.Abs(item.Balance)} {ShortContainerName(item.Container)}\u00A0CREDIT";

                    FrontCell(table, text);
                }
            });
        });
    }

    private static float FrontPageFontSize(
        MarketFloorReportData data)
    {
        var accountColumns =
            (int)Math.Ceiling(data.AccountOwing.Count / 2d);

        var rightColumnLoad =
            data.CashOwing.Count +
            data.Credits.Count +
            data.SpecialContainers.Count +
            8; // section headings / spacing allowance

        var maxRows = Math.Max(accountColumns, rightColumnLoad);

        // This report is used from around 4am. Prefer the largest
        // readable text that still fits a single front page.
        return maxRows switch
        {
            <= 34 => 12.2f,
            <= 42 => 11.4f,
            <= 50 => 10.6f,
            <= 58 => 9.8f,
            _ => 9.0f
        };
    }

    private static void FrontHeader(
        TableDescriptor table,
        string text) =>
        table.Cell()
            .Background(Colors.Grey.Lighten3)
            .BorderBottom(0.8f)
            .PaddingVertical(2.1f)
            .PaddingHorizontal(2)
            .Text(text)
            .SemiBold();

    private static void FrontCell(
        TableDescriptor table,
        string text) =>
        table.Cell()
            .BorderBottom(0.45f)
            .BorderColor(Colors.Grey.Lighten1)
            .PaddingVertical(1.8f)
            .PaddingHorizontal(2)
            .Text(text);

    private static void ReverseTable(
        IContainer container,
        string heading,
        IReadOnlyList<MarketFloorReverseRow> rows)
    {
        container.Column(column =>
        {
            column.Item().Background(Colors.Grey.Lighten2).Padding(3)
                .Text(heading).SemiBold();

            column.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(4.2f);
                    columns.RelativeColumn(0.85f);
                    columns.RelativeColumn(0.85f);
                    columns.RelativeColumn(1.35f);
                    columns.RelativeColumn(2.75f);
                });

                CompactHeader(table, "Buyer");
                CompactHeader(table, "Out");
                CompactHeader(table, "In");
                CompactHeader(table, "B/Fwd");
                CompactHeader(table, "Total");

                foreach (var item in rows)
                {
                    CompactCell(table, item.Buyer);
                    CompactCell(table, item.Out.ToString());
                    CompactCell(table, item.In.ToString());
                    CompactCell(table, item.BroughtForward.ToString());
                    CompactCell(table, FormatTotal(item.Total));
                }
            });
        });
    }

    private static string FormatTotal(int value) =>
        value < 0 ? $"{Math.Abs(value)}\u00A0CREDIT" : value.ToString();

    private static string ShortContainerName(string name) =>
        name.Replace(" Pallet", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Trim()
            .ToUpperInvariant();

    private static void CompactHeader(TableDescriptor table, string text) =>
        table.Cell()
            .Background(Colors.Grey.Lighten3)
            .BorderBottom(0.7f)
            .PaddingVertical(0.7f)
            .PaddingHorizontal(1)
            .Text(text)
            .SemiBold();

    private static void CompactCell(TableDescriptor table, string text) =>
        table.Cell()
            .BorderBottom(0.35f)
            .BorderColor(Colors.Grey.Lighten1)
            .PaddingVertical(0.55f)
            .PaddingHorizontal(1)
            .Text(text);
}
