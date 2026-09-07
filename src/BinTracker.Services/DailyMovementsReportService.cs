using BinTracker.Core;
using BinTracker.Data;
using Microsoft.EntityFrameworkCore;

namespace BinTracker.Services;

public sealed record DailyMovementsReportQuery(
    DateOnly ReportDate,
    string? CustomerSearch = null,
    int? ContainerTypeId = null,
    MovementType? Direction = null,
    MovementSource? Source = null,
    bool IncludeAdjustments = false);

public sealed record DailyMovementReportRow(
    long MovementId,
    DateOnly MovementDate,
    DateTime RecordedUtc,
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
    public string DirectionText =>
        Direction == MovementType.Out ? "OUT" : "IN";

    public string SourceText => Source switch
    {
        MovementSource.Manual => "Single Entry",
        MovementSource.Batch => "Batch Entry",
        MovementSource.ExcelImport => "Excel Import",
        MovementSource.Adjustment => "Opening Adjustment",
        _ => Source.ToString()
    };
}

public sealed record DailyContainerMovementTotal(
    int ContainerTypeId,
    string ContainerType,
    int DisplayOrder,
    int OutQuantity,
    int InQuantity);

public sealed record DailyMovementsReportResult(
    DateOnly ReportDate,
    IReadOnlyList<DailyMovementReportRow> Rows,
    IReadOnlyList<DailyContainerMovementTotal> ContainerTotals)
{
    public int OutQuantity =>
        Rows.Where(x => x.Direction == MovementType.Out)
            .Sum(x => x.Quantity);

    public int InQuantity =>
        Rows.Where(x => x.Direction == MovementType.In)
            .Sum(x => x.Quantity);
}

public interface IDailyMovementsReportService
{
    Task<DailyMovementsReportResult> QueryAsync(
        DailyMovementsReportQuery query,
        CancellationToken cancellationToken = default);
}

internal sealed class DailyMovementsReportService(
    IDbContextFactory<BinTrackerDbContext> factory,
    IBusinessClock clock,
    IOperationalMovementProjectionAuthority? operationalProjection = null)
    : IDailyMovementsReportService
{
    public async Task<DailyMovementsReportResult> QueryAsync(
        DailyMovementsReportQuery query,
        CancellationToken cancellationToken = default)
    {
        var today = clock.Today;
        var reportDate = query.ReportDate > today
            ? today
            : query.ReportDate;

        await using var db =
            await factory.CreateDbContextAsync(cancellationToken);

        List<DailyMovementActivity> movements;
        if (operationalProjection is null)
        {
            movements = await db.EffectiveOperationalMovements()
                .Where(x => x.MovementDate == reportDate)
                .Select(x => new DailyMovementActivity(
                    x.Id,
                    x.MovementDate,
                    x.CreatedUtc,
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
                    reportDate,
                    reportDate,
                    containerTypeId: query.ContainerTypeId),
                cancellationToken);
            movements = projected.Activity
                .Select(x => new DailyMovementActivity(
                    x.EvidenceMovementId,
                    x.MovementDate,
                    x.CreatedUtc,
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

        if (query.Direction.HasValue)
            movements = movements
                .Where(x => x.Direction == query.Direction.Value)
                .ToList();

        if (query.Source.HasValue)
            movements = movements
                .Where(x => x.Source == query.Source.Value)
                .ToList();

        if (movements.Count == 0)
            return new DailyMovementsReportResult(reportDate, [], []);

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
                return new DailyMovementReportRow(
                    x.MovementId,
                    x.MovementDate,
                    x.RecordedUtc,
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
            .OrderBy(x => x.CustomerCode, StringComparer.OrdinalIgnoreCase)
            .ThenBy(x => x.CustomerName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(x => x.ContainerDisplayOrder)
            .ThenBy(x => x.Direction)
            .ThenBy(x => x.MovementId)
            .ToList();

        var totals = rows
            .GroupBy(x => new
            {
                x.ContainerTypeId,
                x.ContainerType,
                x.ContainerDisplayOrder
            })
            .Select(g => new DailyContainerMovementTotal(
                g.Key.ContainerTypeId,
                g.Key.ContainerType,
                g.Key.ContainerDisplayOrder,
                g.Where(x => x.Direction == MovementType.Out)
                    .Sum(x => x.Quantity),
                g.Where(x => x.Direction == MovementType.In)
                    .Sum(x => x.Quantity)))
            .OrderBy(x => x.DisplayOrder)
            .ThenBy(x => x.ContainerType, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return new DailyMovementsReportResult(
            reportDate,
            rows,
            totals);
    }

    private static bool Contains(string? value, string term) =>
        !string.IsNullOrWhiteSpace(value) &&
        value.Contains(term, StringComparison.OrdinalIgnoreCase);

    private sealed record DailyMovementActivity(
        long MovementId,
        DateOnly MovementDate,
        DateTime RecordedUtc,
        int CustomerId,
        int ContainerTypeId,
        MovementType Direction,
        int Quantity,
        MovementSource Source,
        string? Reference,
        string? Notes,
        string? EnteredBy);
}
