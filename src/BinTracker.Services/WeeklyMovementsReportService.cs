using BinTracker.Core;
using BinTracker.Data;
using Microsoft.EntityFrameworkCore;

namespace BinTracker.Services;

public sealed record WeeklyMovementsReportQuery(
    DateOnly WeekStart,
    string? CustomerSearch = null,
    int? ContainerTypeId = null,
    MovementSource? Source = null,
    bool IncludeAdjustments = false);

public sealed record WeeklyMovementReportRow(
    long MovementId,
    DateOnly MovementDate,
    int CustomerId,
    string CustomerCode,
    string CustomerName,
    CustomerType CustomerType,
    int ContainerTypeId,
    string ContainerType,
    int ContainerDisplayOrder,
    MovementType Direction,
    int Quantity,
    MovementSource Source,
    string Reference,
    string Notes,
    string EnteredBy)
{
    public string DirectionText => Direction == MovementType.Out ? "OUT" : "IN";
    public string SourceText => Source switch
    {
        MovementSource.Manual => "Single Entry",
        MovementSource.Batch => "Batch Entry",
        MovementSource.ExcelImport => "Excel Import",
        MovementSource.Adjustment => "Opening Adjustment",
        _ => Source.ToString()
    };
}

public sealed record WeeklyMovementSummaryRow(
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

public sealed record WeeklyMovementsReportResult(
    DateOnly WeekStart,
    DateOnly WeekEnd,
    DateOnly DataThroughDate,
    IReadOnlyList<WeeklyMovementReportRow> Rows,
    IReadOnlyList<WeeklyMovementSummaryRow> Summary)
{
    public int OutQuantity => Rows.Where(x => x.Direction == MovementType.Out).Sum(x => x.Quantity);
    public int InQuantity => Rows.Where(x => x.Direction == MovementType.In).Sum(x => x.Quantity);
    public int NetQuantity => OutQuantity - InQuantity;
}

public interface IWeeklyMovementsReportService
{
    Task<WeeklyMovementsReportResult> QueryAsync(
        WeeklyMovementsReportQuery query,
        CancellationToken cancellationToken = default);
}

internal sealed class WeeklyMovementsReportService(
    IDbContextFactory<BinTrackerDbContext> factory,
    IBusinessClock clock,
    IOperationalMovementProjectionAuthority? operationalProjection = null)
    : IWeeklyMovementsReportService
{
    public async Task<WeeklyMovementsReportResult> QueryAsync(
        WeeklyMovementsReportQuery query,
        CancellationToken cancellationToken = default)
    {
        var today = clock.Today;
        var selectedDate = query.WeekStart > today
            ? today
            : query.WeekStart;

        var start = StartOfWeek(selectedDate);
        var end = start.AddDays(6);
        var dataThrough = end > today ? today : end;

        await using var db = await factory.CreateDbContextAsync(cancellationToken);

        List<WeeklyMovementActivity> movements;
        if (operationalProjection is null)
        {
            movements = await db.EffectiveOperationalMovements()
                .Where(x =>
                    x.MovementDate >= start &&
                    x.MovementDate <= dataThrough)
                .Select(x => new WeeklyMovementActivity(
                    x.Id,
                    x.MovementDate,
                    x.CustomerId,
                    x.ContainerTypeId,
                    x.MovementType,
                    x.Quantity,
                    x.Source,
                    x.ReferenceNumber,
                    x.Notes,
                    x.CreatedBy))
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
                .Select(x => new WeeklyMovementActivity(
                    x.EvidenceMovementId,
                    x.MovementDate,
                    x.CustomerId,
                    x.ContainerTypeId,
                    x.MovementType,
                    x.Quantity,
                    x.Source,
                    x.ReferenceNumber,
                    x.Notes,
                    x.CreatedBy))
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

        var rows = movements
            .Where(x => customers.ContainsKey(x.CustomerId) &&
                        containers.ContainsKey(x.ContainerTypeId))
            .Where(x =>
                string.IsNullOrWhiteSpace(search) ||
                Contains(customers[x.CustomerId].CustomerCode, search) ||
                Contains(customers[x.CustomerId].Name, search))
            .Select(x =>
            {
                var customer = customers[x.CustomerId];
                var container = containers[x.ContainerTypeId];
                return new WeeklyMovementReportRow(
                    x.MovementId,
                    x.MovementDate,
                    x.CustomerId,
                    customer.CustomerCode ?? "",
                    customer.Name,
                    customer.CustomerType,
                    x.ContainerTypeId,
                    container.Name,
                    container.DisplayOrder,
                    x.Direction,
                    x.Quantity,
                    x.Source,
                    x.Reference ?? "",
                    x.Notes ?? "",
                    x.EnteredBy ?? "");
            })
            .OrderBy(x => x.MovementDate)
            .ThenBy(x => x.CustomerCode, StringComparer.OrdinalIgnoreCase)
            .ThenBy(x => x.ContainerDisplayOrder)
            .ThenBy(x => x.MovementId)
            .ToList();

        var summary = rows
            .GroupBy(x => new
            {
                x.CustomerId, x.CustomerCode, x.CustomerName,
                x.ContainerTypeId, x.ContainerType, x.ContainerDisplayOrder
            })
            .Select(g => new WeeklyMovementSummaryRow(
                g.Key.CustomerId, g.Key.CustomerCode, g.Key.CustomerName,
                g.Key.ContainerTypeId, g.Key.ContainerType, g.Key.ContainerDisplayOrder,
                g.Where(x => x.Direction == MovementType.Out).Sum(x => x.Quantity),
                g.Where(x => x.Direction == MovementType.In).Sum(x => x.Quantity)))
            .OrderBy(x => x.CustomerCode, StringComparer.OrdinalIgnoreCase)
            .ThenBy(x => x.ContainerDisplayOrder)
            .ToList();

        return new WeeklyMovementsReportResult(start, end, end > clock.Today ? clock.Today : end, rows, summary);
    }

    public static DateOnly StartOfWeek(DateOnly date)
    {
        var offset = ((int)date.DayOfWeek + 6) % 7;
        return date.AddDays(-offset);
    }

    private static bool Contains(string? value, string term) =>
        !string.IsNullOrWhiteSpace(value) &&
        value.Contains(term, StringComparison.OrdinalIgnoreCase);

    private sealed record WeeklyMovementActivity(
        long MovementId,
        DateOnly MovementDate,
        int CustomerId,
        int ContainerTypeId,
        MovementType Direction,
        int Quantity,
        MovementSource Source,
        string? Reference,
        string? Notes,
        string? EnteredBy);
}
