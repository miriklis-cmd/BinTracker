using BinTracker.Core;
using BinTracker.Data;
using Microsoft.EntityFrameworkCore;

namespace BinTracker.Services;

public sealed record MovementCustomerOption(
    int Id,
    string Code,
    string Name,
    CustomerType CustomerType);

public sealed record MovementContainerOption(
    int Id,
    string Name);

public sealed record MovementBalanceRow(
    int ContainerTypeId,
    string ContainerType,
    int Balance)
{
    public string Position =>
        Balance > 0 ? $"{Balance} OUT" :
        Balance < 0 ? $"{Math.Abs(Balance)} CREDIT" :
        "Even";
}

public sealed record MovementCustomerSummary(
    int CustomerId,
    string Code,
    string Name,
    CustomerType CustomerType,
    IReadOnlyList<MovementBalanceRow> Balances);

public sealed record MovementBatchLine(
    int CustomerId,
    int ContainerTypeId,
    int Quantity,
    string? Reference,
    string? Notes);

public sealed record SaveMovementBatchRequest(
    DateOnly MovementDate,
    MovementType MovementType,
    string? Notes,
    IReadOnlyList<MovementBatchLine> Lines);

public sealed record SaveMovementBatchResult(
    int BatchId,
    int LineCount,
    int TotalQuantity);

public interface IMovementService
{
    Task<IReadOnlyList<MovementCustomerOption>> GetActiveCustomersAsync(
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MovementContainerOption>> GetActiveContainerTypesAsync(
        CancellationToken cancellationToken = default);

    Task<MovementCustomerSummary?> GetCustomerSummaryByCodeAsync(
        string code,
        CancellationToken cancellationToken = default);

    Task<SaveMovementBatchResult> SaveBatchAsync(
        SaveMovementBatchRequest request,
        CancellationToken cancellationToken = default);
}

internal sealed class MovementService(
    IDbContextFactory<BinTrackerDbContext> factory,
    UserSession session) : IMovementService
{
    public async Task<IReadOnlyList<MovementCustomerOption>> GetActiveCustomersAsync(
        CancellationToken cancellationToken = default)
    {
        await using var db = await factory.CreateDbContextAsync(cancellationToken);

        return await db.Customers
            .AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.CustomerCode)
            .ThenBy(x => x.Name)
            .Select(x => new MovementCustomerOption(
                x.Id,
                x.CustomerCode ?? string.Empty,
                x.Name,
                x.CustomerType))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<MovementContainerOption>> GetActiveContainerTypesAsync(
        CancellationToken cancellationToken = default)
    {
        await using var db = await factory.CreateDbContextAsync(cancellationToken);

        return await db.ContainerTypes
            .AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.DisplayOrder)
            .ThenBy(x => x.Name)
            .Select(x => new MovementContainerOption(x.Id, x.Name))
            .ToListAsync(cancellationToken);
    }

    public async Task<MovementCustomerSummary?> GetCustomerSummaryByCodeAsync(
        string code,
        CancellationToken cancellationToken = default)
    {
        code = (code ?? string.Empty).Trim().ToUpperInvariant();

        if (code.Length == 0)
            return null;

        await using var db = await factory.CreateDbContextAsync(cancellationToken);

        var customer = await db.Customers
            .AsNoTracking()
            .Where(x => x.IsActive &&
                        x.CustomerCode != null &&
                        x.CustomerCode.ToUpper() == code)
            .Select(x => new
            {
                x.Id,
                Code = x.CustomerCode!,
                x.Name,
                x.CustomerType
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (customer is null)
            return null;

        var containerTypes = await db.ContainerTypes
            .AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.DisplayOrder)
            .ThenBy(x => x.Name)
            .Select(x => new { x.Id, x.Name })
            .ToListAsync(cancellationToken);

        var balances = await db.BinMovements
            .AsNoTracking()
            .Where(x => x.CustomerId == customer.Id)
            .GroupBy(x => x.ContainerTypeId)
            .Select(g => new
            {
                ContainerTypeId = g.Key,
                Balance = g.Sum(x =>
                    x.MovementType == MovementType.Out
                        ? x.Quantity
                        : -x.Quantity)
            })
            .ToDictionaryAsync(
                x => x.ContainerTypeId,
                x => x.Balance,
                cancellationToken);

        return new MovementCustomerSummary(
            customer.Id,
            customer.Code,
            customer.Name,
            customer.CustomerType,
            containerTypes
                .Select(x => new MovementBalanceRow(
                    x.Id,
                    x.Name,
                    balances.GetValueOrDefault(x.Id)))
                .ToList());
    }

    public async Task<SaveMovementBatchResult> SaveBatchAsync(
        SaveMovementBatchRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!session.IsAuthenticated)
            throw new UnauthorizedAccessException("You must be logged in to record movements.");

        if (session.Role == UserRole.Viewer)
            throw new UnauthorizedAccessException("Viewer accounts cannot record movements.");

        if (request.Lines is null || request.Lines.Count == 0)
            throw new ArgumentException("Add at least one movement before saving the batch.");

        if (request.MovementDate > DateOnly.FromDateTime(DateTime.Today))
            throw new ArgumentException("Movement date cannot be in the future.");

        if (request.Lines.Any(x => x.Quantity <= 0))
            throw new ArgumentException("Movement quantities must be greater than zero.");

        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        var customerIds = request.Lines.Select(x => x.CustomerId).Distinct().ToList();
        var containerIds = request.Lines.Select(x => x.ContainerTypeId).Distinct().ToList();

        var validCustomers = await db.Customers
            .Where(x => customerIds.Contains(x.Id) && x.IsActive)
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);

        if (validCustomers.Count != customerIds.Count)
            throw new InvalidOperationException(
                "One or more customers are missing or inactive. Refresh the batch and try again.");

        var validContainers = await db.ContainerTypes
            .Where(x => containerIds.Contains(x.Id) && x.IsActive)
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);

        if (validContainers.Count != containerIds.Count)
            throw new InvalidOperationException(
                "One or more container types are missing or inactive. Refresh the batch and try again.");

        var batch = new MovementBatch
        {
            MovementDate = request.MovementDate,
            MovementType = request.MovementType,
            Source = MovementSource.Batch,
            Notes = Clean(request.Notes),
            CreatedBy = session.Username,
            CreatedUtc = DateTime.UtcNow
        };

        db.MovementBatches.Add(batch);
        await db.SaveChangesAsync(cancellationToken);

        foreach (var line in request.Lines)
        {
            db.BinMovements.Add(new BinMovement
            {
                MovementDate = request.MovementDate,
                MovementType = request.MovementType,
                Source = MovementSource.Batch,
                CustomerId = line.CustomerId,
                ContainerTypeId = line.ContainerTypeId,
                MovementBatchId = batch.Id,
                Quantity = line.Quantity,
                ReferenceNumber = Clean(line.Reference),
                Notes = Clean(line.Notes),
                CreatedBy = session.Username,
                CreatedUtc = DateTime.UtcNow
            });
        }

        await db.SaveChangesAsync(cancellationToken);

        var totalQuantity = request.Lines.Sum(x => x.Quantity);
        var direction = request.MovementType == MovementType.In
            ? "IN (Returned)"
            : "OUT (Taken)";

        db.AuditEvents.Add(new AuditEvent
        {
            TimestampUtc = DateTime.UtcNow,
            UserId = session.UserId,
            Username = session.Username,
            Action = "MOVEMENT_BATCH_RECORDED",
            EntityType = "MovementBatch",
            EntityId = batch.Id.ToString(),
            Description =
                $"{direction} batch #{batch.Id} recorded with {request.Lines.Count} line(s) and {totalQuantity} total containers.",
            AfterValues = System.Text.Json.JsonSerializer.Serialize(new
            {
                batch.MovementDate,
                Direction = direction,
                LineCount = request.Lines.Count,
                TotalQuantity = totalQuantity
            }),
            ComputerName = Environment.MachineName,
            SessionId = session.SessionId,
            Succeeded = true
        });

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return new SaveMovementBatchResult(
            batch.Id,
            request.Lines.Count,
            totalQuantity);
    }

    private static string? Clean(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
