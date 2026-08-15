using BinTracker.Core;
using BinTracker.Data;
using Microsoft.EntityFrameworkCore;

namespace BinTracker.Services;

public sealed record OutstandingReportQuery(
    DateOnly AsOfDate,
    string? CustomerSearch = null,
    int? ContainerTypeId = null,
    bool IncludeCredits = false,
    bool IncludeInactiveCustomers = true);

public sealed record OutstandingReportRow(
    int CustomerId,
    string CustomerCode,
    string CustomerName,
    CustomerType CustomerType,
    bool IsActive,
    int ContainerTypeId,
    string ContainerType,
    int ContainerDisplayOrder,
    int Balance,
    DateOnly? LastMovementDate)
{
    public string PositionText =>
        Balance > 0
            ? $"{Balance:N0} OUT"
            : Balance < 0
                ? $"{Math.Abs(Balance):N0} CREDIT"
                : "Even";
}

public sealed record OutstandingContainerTotal(
    int ContainerTypeId,
    string ContainerType,
    int DisplayOrder,
    int OutstandingQuantity,
    int CreditQuantity,
    int PositionCount);

public sealed record OutstandingReportResult(
    DateOnly AsOfDate,
    IReadOnlyList<OutstandingReportRow> Rows,
    IReadOnlyList<OutstandingContainerTotal> ContainerTotals)
{
    public int OutstandingPositionCount =>
        Rows.Count(x => x.Balance > 0);

    public int CreditPositionCount =>
        Rows.Count(x => x.Balance < 0);
}

public interface IOutstandingReportService
{
    Task<OutstandingReportResult> QueryAsync(
        OutstandingReportQuery query,
        CancellationToken cancellationToken = default);
}

internal sealed class OutstandingReportService(
    IDbContextFactory<BinTrackerDbContext> factory)
    : IOutstandingReportService
{
    public async Task<OutstandingReportResult> QueryAsync(
        OutstandingReportQuery query,
        CancellationToken cancellationToken = default)
    {
        await using var db =
            await factory.CreateDbContextAsync(cancellationToken);

        // Historical position is derived directly from the immutable movement
        // ledger. A movement dated after AsOfDate must never affect the result.
        var movementQuery = db.BinMovements
            .AsNoTracking()
            .Where(x => x.MovementDate <= query.AsOfDate);

        if (query.ContainerTypeId.HasValue)
        {
            movementQuery = movementQuery.Where(
                x => x.ContainerTypeId == query.ContainerTypeId.Value);
        }

        var totals = await movementQuery
            .GroupBy(x => new
            {
                x.CustomerId,
                x.ContainerTypeId
            })
            .Select(g => new
            {
                g.Key.CustomerId,
                g.Key.ContainerTypeId,
                Balance = g.Sum(x =>
                    x.MovementType == MovementType.Out
                        ? x.Quantity
                        : -x.Quantity),
                LastMovementDate = g.Max(x => x.MovementDate)
            })
            .ToListAsync(cancellationToken);

        if (totals.Count == 0)
        {
            return new OutstandingReportResult(
                query.AsOfDate,
                [],
                []);
        }

        var customers = await db.Customers
            .AsNoTracking()
            .ToDictionaryAsync(
                x => x.Id,
                cancellationToken);

        var containers = await db.ContainerTypes
            .AsNoTracking()
            .ToDictionaryAsync(
                x => x.Id,
                cancellationToken);

        var search = query.CustomerSearch?.Trim();
        var rows = new List<OutstandingReportRow>();

        foreach (var total in totals)
        {
            if (!customers.TryGetValue(
                    total.CustomerId,
                    out var customer) ||
                !containers.TryGetValue(
                    total.ContainerTypeId,
                    out var container))
            {
                continue;
            }

            if (!query.IncludeInactiveCustomers &&
                !customer.IsActive)
            {
                continue;
            }

            if (!string.IsNullOrWhiteSpace(search) &&
                !ContainsIgnoreCase(customer.CustomerCode, search) &&
                !ContainsIgnoreCase(customer.Name, search))
            {
                continue;
            }

            if (query.IncludeCredits)
            {
                if (total.Balance == 0)
                    continue;
            }
            else if (total.Balance <= 0)
            {
                continue;
            }

            rows.Add(new OutstandingReportRow(
                customer.Id,
                customer.CustomerCode ?? string.Empty,
                customer.Name,
                customer.CustomerType,
                customer.IsActive,
                container.Id,
                container.Name,
                container.DisplayOrder,
                total.Balance,
                total.LastMovementDate));
        }

        rows = rows
            .OrderBy(x => x.CustomerCode, StringComparer.OrdinalIgnoreCase)
            .ThenBy(x => x.CustomerName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(x => x.ContainerDisplayOrder)
            .ThenBy(x => x.ContainerType, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var containerTotals = rows
            .GroupBy(x => new
            {
                x.ContainerTypeId,
                x.ContainerType,
                x.ContainerDisplayOrder
            })
            .Select(g => new OutstandingContainerTotal(
                g.Key.ContainerTypeId,
                g.Key.ContainerType,
                g.Key.ContainerDisplayOrder,
                g.Where(x => x.Balance > 0)
                    .Sum(x => x.Balance),
                g.Where(x => x.Balance < 0)
                    .Sum(x => Math.Abs(x.Balance)),
                g.Count()))
            .OrderBy(x => x.DisplayOrder)
            .ThenBy(x => x.ContainerType, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return new OutstandingReportResult(
            query.AsOfDate,
            rows,
            containerTotals);
    }

    private static bool ContainsIgnoreCase(
        string? value,
        string term) =>
        !string.IsNullOrWhiteSpace(value) &&
        value.Contains(
            term,
            StringComparison.OrdinalIgnoreCase);
}
