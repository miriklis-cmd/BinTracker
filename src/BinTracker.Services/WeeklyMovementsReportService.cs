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
    IDbContextFactory<BinTrackerDbContext> factory)
    : IWeeklyMovementsReportService
{
    public async Task<WeeklyMovementsReportResult> QueryAsync(
        WeeklyMovementsReportQuery query,
        CancellationToken cancellationToken = default)
    {
        var start = StartOfWeek(query.WeekStart);
        var end = start.AddDays(6);

        await using var db = await factory.CreateDbContextAsync(cancellationToken);

        var movements = db.BinMovements.AsNoTracking()
            .Where(x => x.MovementDate >= start && x.MovementDate <= end);

        if (!query.IncludeAdjustments)
            movements = movements.Where(x => x.Source != MovementSource.Adjustment);

        if (query.ContainerTypeId.HasValue)
            movements = movements.Where(x => x.ContainerTypeId == query.ContainerTypeId.Value);

        if (query.Source.HasValue)
            movements = movements.Where(x => x.Source == query.Source.Value);

        var raw = await movements.Select(x => new
        {
            x.Id, x.MovementDate, x.CustomerId,
            CustomerCode = x.Customer.CustomerCode ?? "",
            CustomerName = x.Customer.Name,
            x.Customer.CustomerType,
            x.ContainerTypeId,
            ContainerType = x.ContainerType.Name,
            ContainerDisplayOrder = x.ContainerType.DisplayOrder,
            Direction = x.MovementType,
            x.Quantity, x.Source,
            Reference = x.ReferenceNumber ?? "",
            Notes = x.Notes ?? "",
            EnteredBy = x.CreatedBy ?? ""
        }).ToListAsync(cancellationToken);

        var search = query.CustomerSearch?.Trim();

        var rows = raw
            .Where(x => string.IsNullOrWhiteSpace(search)
                || Contains(x.CustomerCode, search)
                || Contains(x.CustomerName, search))
            .Select(x => new WeeklyMovementReportRow(
                x.Id, x.MovementDate, x.CustomerId, x.CustomerCode, x.CustomerName,
                x.CustomerType, x.ContainerTypeId, x.ContainerType,
                x.ContainerDisplayOrder, x.Direction, x.Quantity, x.Source,
                x.Reference, x.Notes, x.EnteredBy))
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

        return new WeeklyMovementsReportResult(start, end, rows, summary);
    }

    public static DateOnly StartOfWeek(DateOnly date)
    {
        var offset = ((int)date.DayOfWeek + 6) % 7;
        return date.AddDays(-offset);
    }

    private static bool Contains(string? value, string term) =>
        !string.IsNullOrWhiteSpace(value) &&
        value.Contains(term, StringComparison.OrdinalIgnoreCase);
}
