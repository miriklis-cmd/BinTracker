using BinTracker.Core;
using BinTracker.Data;
using Microsoft.EntityFrameworkCore;

namespace BinTracker.Services;

public sealed record MonthlySummaryReportQuery(
    DateOnly Month,
    string? CustomerSearch = null,
    int? ContainerTypeId = null,
    MovementSource? Source = null,
    bool IncludeAdjustments = false);

public sealed record MonthlySummaryReportRow(
    int CustomerId,
    string CustomerCode,
    string CustomerName,
    int ContainerTypeId,
    string ContainerType,
    int ContainerDisplayOrder,
    int OutQuantity,
    int InQuantity)
{
    public int NetQuantity => OutQuantity - InQuantity;
}

public sealed record MonthlySummaryContainerTotal(
    int ContainerTypeId,
    string ContainerType,
    int DisplayOrder,
    int OutQuantity,
    int InQuantity)
{
    public int NetQuantity => OutQuantity - InQuantity;
}

public sealed record MonthlySummaryReportResult(
    DateOnly MonthStart,
    DateOnly MonthEnd,
    DateOnly DataThroughDate,
    IReadOnlyList<MonthlySummaryReportRow> Rows,
    IReadOnlyList<MonthlySummaryContainerTotal> ContainerTotals)
{
    public int OutQuantity => Rows.Sum(x => x.OutQuantity);
    public int InQuantity => Rows.Sum(x => x.InQuantity);
    public int NetQuantity => OutQuantity - InQuantity;
}

public interface IMonthlySummaryReportService
{
    Task<MonthlySummaryReportResult> QueryAsync(
        MonthlySummaryReportQuery query,
        CancellationToken cancellationToken = default);
}

internal sealed class MonthlySummaryReportService(
    IDbContextFactory<BinTrackerDbContext> factory,
    IBusinessClock clock)
    : IMonthlySummaryReportService
{
    public async Task<MonthlySummaryReportResult> QueryAsync(
        MonthlySummaryReportQuery query,
        CancellationToken cancellationToken = default)
    {
        var today = clock.Today;
        var requested = query.Month > today ? today : query.Month;

        var start = new DateOnly(requested.Year, requested.Month, 1);
        var end = start.AddMonths(1).AddDays(-1);
        var dataThrough = end > today ? today : end;

        await using var db =
            await factory.CreateDbContextAsync(cancellationToken);

        var movements = db.BinMovements
            .AsNoTracking()
            .Where(x =>
                x.MovementDate >= start &&
                x.MovementDate <= dataThrough);

        if (!query.IncludeAdjustments)
            movements = movements.Where(
                x => x.Source != MovementSource.Adjustment);

        if (query.ContainerTypeId.HasValue)
            movements = movements.Where(
                x => x.ContainerTypeId == query.ContainerTypeId.Value);

        if (query.Source.HasValue)
            movements = movements.Where(
                x => x.Source == query.Source.Value);

        var raw = await movements
            .Select(x => new
            {
                x.CustomerId,
                CustomerCode = x.Customer.CustomerCode ?? "",
                CustomerName = x.Customer.Name,
                x.ContainerTypeId,
                ContainerType = x.ContainerType.Name,
                ContainerDisplayOrder = x.ContainerType.DisplayOrder,
                Direction = x.MovementType,
                x.Quantity
            })
            .ToListAsync(cancellationToken);

        var search = query.CustomerSearch?.Trim();

        var matching = raw
            .Where(x =>
                string.IsNullOrWhiteSpace(search) ||
                Contains(x.CustomerCode, search) ||
                Contains(x.CustomerName, search))
            .ToList();

        var rows = matching
            .GroupBy(x => new
            {
                x.CustomerId,
                x.CustomerCode,
                x.CustomerName,
                x.ContainerTypeId,
                x.ContainerType,
                x.ContainerDisplayOrder
            })
            .Select(g => new MonthlySummaryReportRow(
                g.Key.CustomerId,
                g.Key.CustomerCode,
                g.Key.CustomerName,
                g.Key.ContainerTypeId,
                g.Key.ContainerType,
                g.Key.ContainerDisplayOrder,
                g.Where(x => x.Direction == MovementType.Out)
                    .Sum(x => x.Quantity),
                g.Where(x => x.Direction == MovementType.In)
                    .Sum(x => x.Quantity)))
            .OrderBy(x => x.CustomerCode, StringComparer.OrdinalIgnoreCase)
            .ThenBy(x => x.ContainerDisplayOrder)
            .ToList();

        var containerTotals = rows
            .GroupBy(x => new
            {
                x.ContainerTypeId,
                x.ContainerType,
                x.ContainerDisplayOrder
            })
            .Select(g => new MonthlySummaryContainerTotal(
                g.Key.ContainerTypeId,
                g.Key.ContainerType,
                g.Key.ContainerDisplayOrder,
                g.Sum(x => x.OutQuantity),
                g.Sum(x => x.InQuantity)))
            .OrderBy(x => x.DisplayOrder)
            .ThenBy(x => x.ContainerType, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return new MonthlySummaryReportResult(
            start,
            end,
            dataThrough,
            rows,
            containerTotals);
    }

    private static bool Contains(string? value, string term) =>
        !string.IsNullOrWhiteSpace(value) &&
        value.Contains(term, StringComparison.OrdinalIgnoreCase);
}
