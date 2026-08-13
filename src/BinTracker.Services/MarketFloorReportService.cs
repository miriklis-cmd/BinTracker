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
    string Container,
    int Total);

public sealed record MarketFloorSpecialRow(
    string Buyer,
    string Container,
    int Balance);

public sealed record MarketFloorReverseRow(
    int CustomerId,
    string Buyer,
    CustomerType CustomerType,
    string Container,
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

internal sealed record MarketFloorFrontLayout(
    float FontSize,
    float CellVerticalPadding,
    float SectionPadding,
    float ContentTopPadding);

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
        // Special-container configuration is authoritative.
        // Bulk is a special container in the production setup and must stay
        // in the Special Containers block rather than the normal floor rows.
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

        var blueContainer = containerTypes
            .FirstOrDefault(x =>
                string.Equals(
                    x.Name,
                    "Blue Bin",
                    StringComparison.OrdinalIgnoreCase));

        var reverse = new List<MarketFloorReverseRow>();

        foreach (var customer in customers)
        {
            var customerRows = regular
                .Where(x => x.CustomerId == customer.Id)
                .GroupBy(x => x.ContainerTypeId)
                .OrderBy(g =>
                    FloorContainerSortKey(
                        containerById.GetValueOrDefault(
                            g.Key,
                            "Unknown")))
                .ThenBy(g =>
                    containerById.GetValueOrDefault(
                        g.Key,
                        "Unknown"),
                    StringComparer.OrdinalIgnoreCase)
                .ToList();

            // Keep customers with no history visible on the reverse sheet as
            // a normal Blue row, matching the legacy daily worksheet.
            if (customerRows.Count == 0)
            {
                reverse.Add(new MarketFloorReverseRow(
                    customer.Id,
                    customer.Buyer,
                    customer.CustomerType,
                    "Blue",
                    0,
                    0,
                    0,
                    0));

                continue;
            }

            foreach (var group in customerRows)
            {
                var containerName =
                    containerById.GetValueOrDefault(
                        group.Key,
                        "Unknown");

                var rows = group.ToList();

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

                reverse.Add(new MarketFloorReverseRow(
                    customer.Id,
                    customer.Buyer,
                    customer.CustomerType,
                    FloorContainerName(containerName),
                    outs,
                    ins,
                    bfwd,
                    total));
            }
        }

        reverse = reverse
            .OrderBy(x => x.Buyer, StringComparer.OrdinalIgnoreCase)
            .ThenBy(x => FloorContainerSortKey(x.Container))
            .ThenBy(x => x.Container, StringComparer.OrdinalIgnoreCase)
            .ToList();

        // Front sheet is also container-specific. Never aggregate Blue,
        // Yellow, Bulk, etc. into one number because floor staff must know
        // which physical container to collect.
        var front = reverse
            .Where(x => x.Total != 0)
            .Select(x => new MarketFloorFrontRow(
                x.CustomerId,
                x.Buyer,
                x.CustomerType,
                x.Container,
                x.Total))
            .ToList();

        var accountOwing = front
            .Where(x =>
                x.CustomerType == CustomerType.Account &&
                x.Total > 0)
            .OrderBy(x => x.Buyer, StringComparer.OrdinalIgnoreCase)
            .ThenBy(x => FloorContainerSortKey(x.Container))
            .ToList();

        // Cash/COD customers stay together in the Cash area whether they
        // owe bins or are in credit. Each container remains a separate row.
        var cashOwing = front
            .Where(x =>
                x.CustomerType == CustomerType.CashCod)
            .OrderBy(x => x.Buyer, StringComparer.OrdinalIgnoreCase)
            .ThenBy(x => FloorContainerSortKey(x.Container))
            .ToList();

        // Only Account-customer credits belong in the separate CREDIT block,
        // again separated by container.
        var credits = front
            .Where(x =>
                x.CustomerType == CustomerType.Account &&
                x.Total < 0)
            .OrderBy(x => x.Buyer, StringComparer.OrdinalIgnoreCase)
            .ThenBy(x => FloorContainerSortKey(x.Container))
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
                var frontLayout = FrontPageLayout(data);
                var frontFontSize = frontLayout.FontSize;

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

                page.Content()
                    .PaddingTop(frontLayout.ContentTopPadding)
                    .Row(row =>
                {
                    var split = (int)Math.Ceiling(data.AccountOwing.Count / 2d);
                    var accountLeft = data.AccountOwing.Take(split).ToList();
                    var accountRight = data.AccountOwing.Skip(split).ToList();

                    row.RelativeItem().PaddingRight(2).Element(c =>
                        FrontBuyerTable(c, "ACCOUNT - OWING", accountLeft, frontLayout));

                    row.RelativeItem().PaddingHorizontal(1).Element(c =>
                        FrontBuyerTable(c, "ACCOUNT - OWING", accountRight, frontLayout));

                    row.RelativeItem().PaddingLeft(2).Column(right =>
                    {
                        right.Item().Element(c =>
                            FrontBuyerTable(c, "CASH - OWING", data.CashOwing, frontLayout));

                        right.Item()
                            .PaddingTop(frontLayout.SectionPadding)
                            .Background(Colors.Yellow.Lighten3)
                            .Padding(frontLayout.CellVerticalPadding)
                            .Text("CREDIT")
                            .SemiBold();

                        right.Item().Element(c =>
                            CreditTable(c, data.Credits, frontLayout));

                        right.Item()
                            .PaddingTop(frontLayout.SectionPadding)
                            .Element(c =>
                            SpecialTable(c, data.SpecialContainers, frontLayout));
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
                page.DefaultTextStyle(x => x.FontFamily("Arial").FontSize(8.0f));

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
        IReadOnlyList<MarketFloorFrontRow> rows,
        MarketFloorFrontLayout layout)
    {
        container.Column(column =>
        {
            column.Item()
                .Background(Colors.Grey.Lighten2)
                .Padding(layout.CellVerticalPadding)
                .Text(heading)
                .SemiBold();

            column.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(5);
                    columns.RelativeColumn(2);
                });

                FrontHeader(table, "Buyer", layout);
                FrontHeader(table, "Total", layout);

                foreach (var item in rows)
                {
                    FrontCell(
                        table,
                        FloorBuyerLabel(
                            item.Buyer,
                            item.Container),
                        layout);

                    FrontCell(
                        table,
                        FormatTotal(item.Total),
                        layout);
                }
            });
        });
    }

    private static void CreditTable(
        IContainer container,
        IReadOnlyList<MarketFloorFrontRow> rows,
        MarketFloorFrontLayout layout)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.RelativeColumn(5);
                columns.RelativeColumn(2);
            });

            foreach (var item in rows)
            {
                FrontCell(
                    table,
                    FloorBuyerLabel(
                        item.Buyer,
                        item.Container),
                    layout);

                FrontCell(
                    table,
                    $"{Math.Abs(item.Total)}\u00A0CREDIT",
                    layout);
            }
        });
    }

    private static void SpecialTable(
        IContainer container,
        IReadOnlyList<MarketFloorSpecialRow> rows,
        MarketFloorFrontLayout layout)
    {
        if (rows.Count == 0)
            return;

        container.Column(column =>
        {
            column.Item()
                .Background(Colors.Grey.Lighten2)
                .Padding(layout.CellVerticalPadding)
                .Text("SPECIAL CONTAINERS")
                .SemiBold();

            column.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(3);
                    columns.RelativeColumn(2);
                });

                foreach (var item in rows)
                {
                    FrontCell(table, item.Buyer, layout);

                    var text = item.Balance > 0
                        ? $"{item.Balance} {ShortContainerName(item.Container)}"
                        : $"{Math.Abs(item.Balance)} {ShortContainerName(item.Container)}\u00A0CREDIT";

                    FrontCell(table, text, layout);
                }
            });
        });
    }

    private static MarketFloorFrontLayout FrontPageLayout(
        MarketFloorReportData data)
    {
        // The front page is generated from the actual rows for that day.
        // Extra Yellow rows therefore increase the measured load immediately.
        var accountColumnLoad =
            (int)Math.Ceiling(data.AccountOwing.Count / 2d);

        var rightColumnLoad =
            data.CashOwing.Count +
            data.Credits.Count +
            data.SpecialContainers.Count +
            5; // section headings / visual spacing

        var maxRows =
            Math.Max(
                accountColumnLoad,
                rightColumnLoad);

        // Use large type on light days, then progressively reduce font,
        // cell padding and section spacing as row count rises. This keeps
        // the front sheet on one A4 page without permanently sacrificing
        // readability on normal days.
        return maxRows switch
        {
            <= 30 => new(11.0f, 1.65f, 5.0f, 5.0f),
            <= 34 => new(10.5f, 1.45f, 4.5f, 4.5f),
            <= 38 => new(10.0f, 1.25f, 4.0f, 4.0f),
            <= 42 => new(9.5f, 1.05f, 3.5f, 3.5f),
            <= 46 => new(9.0f, 0.90f, 3.0f, 3.0f),
            <= 50 => new(8.5f, 0.75f, 2.5f, 2.5f),
            <= 54 => new(8.0f, 0.60f, 2.0f, 2.0f),
            <= 58 => new(7.5f, 0.45f, 1.5f, 1.5f),
            _ => new(7.0f, 0.30f, 1.0f, 1.0f)
        };
    }

    private static void FrontHeader(
        TableDescriptor table,
        string text,
        MarketFloorFrontLayout layout) =>
        table.Cell()
            .Background(Colors.Grey.Lighten3)
            .BorderBottom(0.8f)
            .PaddingVertical(layout.CellVerticalPadding)
            .PaddingHorizontal(2)
            .Text(text)
            .SemiBold();

    private static void FrontCell(
        TableDescriptor table,
        string text,
        MarketFloorFrontLayout layout) =>
        table.Cell()
            .BorderBottom(0.45f)
            .BorderColor(Colors.Grey.Lighten1)
            .PaddingVertical(layout.CellVerticalPadding)
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
                    columns.RelativeColumn(4.8f);
                    columns.RelativeColumn(0.75f);
                    columns.RelativeColumn(0.75f);
                    columns.RelativeColumn(1.35f);
                    columns.RelativeColumn(2.35f);
                });

                CompactHeader(table, "Buyer");
                CompactHeader(table, "Out");
                CompactHeader(table, "In");
                CompactHeader(table, "B/Fwd");
                CompactHeader(table, "Total");

                foreach (var item in rows)
                {
                    CompactCell(
                        table,
                        FloorBuyerLabel(
                            item.Buyer,
                            item.Container));

                    CompactCell(table, item.Out.ToString());
                    CompactCell(table, item.In.ToString());
                    CompactCell(table, item.BroughtForward.ToString());
                    CompactCell(table, FormatTotal(item.Total));
                }
            });
        });
    }

    private static string FloorBuyerLabel(
        string buyer,
        string container)
    {
        // Blue is the standard floor bin and is intentionally implicit.
        if (container.Equals(
                "Blue",
                StringComparison.OrdinalIgnoreCase))
        {
            return buyer;
        }

        return $"{buyer} ({container})";
    }

    private static string FloorContainerName(string name)
    {
        if (name.Equals(
                "Blue Bin",
                StringComparison.OrdinalIgnoreCase))
            return "Blue";

        if (name.Equals(
                "Yellow Bin",
                StringComparison.OrdinalIgnoreCase))
            return "Yellow";

        if (name.Equals(
                "Bulk Bin",
                StringComparison.OrdinalIgnoreCase))
            return "Bulk";

        if (name.EndsWith(
                " Bin",
                StringComparison.OrdinalIgnoreCase))
        {
            return name[..^4].Trim();
        }

        return name;
    }

    private static int FloorContainerSortKey(string name)
    {
        if (name.Contains(
                "Blue",
                StringComparison.OrdinalIgnoreCase))
            return 0;

        if (name.Contains(
                "Yellow",
                StringComparison.OrdinalIgnoreCase))
            return 1;

        if (name.Contains(
                "Bulk",
                StringComparison.OrdinalIgnoreCase))
            return 2;

        return 10;
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
