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
    IBusinessClock clock,
    IOperationalMovementProjectionAuthority? operationalProjection = null)
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

        List<MonthlyMovementActivity> movements;
        if (operationalProjection is null)
        {
            movements = await db.EffectiveOperationalMovements()
                .Where(x =>
                    x.MovementDate >= start &&
                    x.MovementDate <= dataThrough)
                .Select(x => new MonthlyMovementActivity(
                    x.CustomerId,
                    x.ContainerTypeId,
                    x.MovementType,
                    x.Quantity,
                    x.Source))
                .ToListAsync(cancellationToken);
        }
        else
        {
            var projected = await operationalProjection.QueryAsync(
                OperationalMovementProjectionScope.Activity(
                    start,
                    dataThrough,
                    containerTypeId: query.ContainerTypeId),
                cancellationToken);
            movements = projected.Activity
                .Select(x => new MonthlyMovementActivity(
                    x.CustomerId,
                    x.ContainerTypeId,
                    x.MovementType,
                    x.Quantity,
                    x.Source))
                .ToList();
        }

        if (!query.IncludeAdjustments)
            movements = movements
                .Where(x => x.Source != MovementSource.Adjustment)
                .ToList();

        if (query.ContainerTypeId.HasValue)
            movements = movements
                .Where(x => x.ContainerTypeId == query.ContainerTypeId.Value)
                .ToList();

        if (query.Source.HasValue)
            movements = movements
                .Where(x => x.Source == query.Source.Value)
                .ToList();

        var customers = await db.Customers
            .AsNoTracking()
            .ToDictionaryAsync(x => x.Id, cancellationToken);
        var containers = await db.ContainerTypes
            .AsNoTracking()
            .ToDictionaryAsync(x => x.Id, cancellationToken);

        var search = query.CustomerSearch?.Trim();

        var matching = movements
            .Where(x => customers.ContainsKey(x.CustomerId) &&
                        containers.ContainsKey(x.ContainerTypeId))
            .Where(x =>
                string.IsNullOrWhiteSpace(search) ||
                Contains(customers[x.CustomerId].CustomerCode, search) ||
                Contains(customers[x.CustomerId].Name, search))
            .ToList();

        var rows = matching
            .GroupBy(x => new
            {
                x.CustomerId,
                CustomerCode = customers[x.CustomerId].CustomerCode ?? "",
                CustomerName = customers[x.CustomerId].Name,
                x.ContainerTypeId,
                ContainerType = containers[x.ContainerTypeId].Name,
                ContainerDisplayOrder = containers[x.ContainerTypeId].DisplayOrder
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

    private sealed record MonthlyMovementActivity(
        int CustomerId,
        int ContainerTypeId,
        MovementType Direction,
        int Quantity,
        MovementSource Source);
}
