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
    public string Position => MovementPositionMath.Format(Balance);
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
    Guid ClientOperationId,
    DateOnly MovementDate,
    MovementType MovementType,
    string? Notes,
    IReadOnlyList<MovementBatchLine> Lines);

public sealed record SaveMovementBatchResult(
    int BatchId,
    int LineCount,
    int TotalQuantity);

public sealed record SaveSingleMovementRequest(
    Guid ClientOperationId,
    DateOnly MovementDate,
    MovementType MovementType,
    int CustomerId,
    int ContainerTypeId,
    int Quantity,
    string? Reference,
    string? Notes);

public sealed record SaveSingleMovementResult(
    long MovementId,
    int NewBalance);

public sealed record OperationalDashboardSummary(
    int ReturnedToday,
    int TakenToday,
    int Outstanding,
    int RequiresAttention);

public sealed record DraftMovementLine(
    int CustomerId,
    string CustomerCode,
    string CustomerName,
    int ContainerTypeId,
    string ContainerType,
    int Quantity,
    string? Reference,
    string? Notes);

public sealed class DraftMovementBatch(IBusinessClock clock)
{
    public DateOnly MovementDate { get; set; } = clock.Today;
    public MovementType MovementType { get; set; } = MovementType.In;
    public List<DraftMovementLine> Lines { get; } = [];

    public int TotalQuantity => Lines.Sum(x => x.Quantity);
    public bool HasLines => Lines.Count > 0;

    public void Clear()
    {
        Lines.Clear();
        MovementDate = clock.Today;
        MovementType = MovementType.In;
    }

    internal void Restore(
        DateOnly movementDate,
        MovementType movementType,
        IEnumerable<DraftMovementLine> lines)
    {
        Lines.Clear();
        Lines.AddRange(lines);
        MovementDate = movementDate;
        MovementType = movementType;
    }
}

public interface IBatchDraftStore
{
    DraftMovementBatchSnapshot? Load();
    void Save(DraftMovementBatch draft);
    void Clear();
}

public sealed record DraftMovementBatchSnapshot(
    DateOnly MovementDate,
    MovementType MovementType,
    IReadOnlyList<DraftMovementLine> Lines,
    DateTimeOffset LastSavedAtUtc);

public sealed class ApplicationState
{
    private readonly IBatchDraftStore? draftStore;

    public ApplicationState(IBusinessClock clock)
    {
        DraftBatch = new DraftMovementBatch(clock);
    }

    public ApplicationState(
        IBatchDraftStore draftStore,
        IBusinessClock clock)
    {
        this.draftStore = draftStore;
        DraftBatch = new DraftMovementBatch(clock);

        var restored = draftStore.Load();
        if (restored is not null)
        {
            DraftBatch.Restore(
                restored.MovementDate,
                restored.MovementType,
                restored.Lines);
            RecoveryDraftLastSavedAtUtc = restored.LastSavedAtUtc;
            RecoveryPromptPending = true;
        }
    }

    public DraftMovementBatch DraftBatch { get; }

    public DateTimeOffset? RecoveryDraftLastSavedAtUtc { get; private set; }

    /// <summary>
    /// True only when this application process started with a persisted draft
    /// loaded from disk. Drafts created later in the same process (including
    /// logout/login) are not presented as crash/power-loss recovery.
    /// </summary>
    public bool RecoveryPromptPending { get; private set; }

    public void MarkRecoveryPromptHandled() => RecoveryPromptPending = false;

    public void PersistDraft()
    {
        if (draftStore is null)
            return;

        if (DraftBatch.HasLines)
            draftStore.Save(DraftBatch);
        else
            draftStore.Clear();
    }

    public void ClearDraft()
    {
        DraftBatch.Clear();
        RecoveryPromptPending = false;
        RecoveryDraftLastSavedAtUtc = null;
        draftStore?.Clear();
    }
}

public static class MovementPositionMath
{
    public static int Apply(int openingBalance, MovementType movementType, int quantity) =>
        movementType == MovementType.Out
            ? openingBalance + quantity
            : openingBalance - quantity;

    public static string Format(int balance) =>
        balance > 0 ? $"{balance} OUT" :
        balance < 0 ? $"{Math.Abs(balance)} CREDIT" :
        "Even";
}

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

    Task<SaveSingleMovementResult> SaveSingleAsync(
        SaveSingleMovementRequest request,
        CancellationToken cancellationToken = default);

    Task<OperationalDashboardSummary> GetDashboardSummaryAsync(
        DateOnly date,
        CancellationToken cancellationToken = default);
}

internal sealed class MovementService(
    IDbContextFactory<BinTrackerDbContext> factory,
    IUserContext session,
    IBusinessClock clock,
    IClientContext client) : IMovementService
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

        if (request.MovementDate > clock.Today)
            throw new ArgumentException("Movement date cannot be in the future.");

        if (request.Lines.Any(x => x.Quantity <= 0))
            throw new ArgumentException("Movement quantities must be greater than zero.");

        if (request.ClientOperationId == Guid.Empty)
            throw new ArgumentException("Client operation ID is required.");

        await using var db = await factory.CreateDbContextAsync(cancellationToken);

        var existingBatch = await GetMatchingBatchRetryAsync(
            db,
            request,
            cancellationToken);

        if (existingBatch is not null)
            return existingBatch;
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
            ClientOperationId = request.ClientOperationId,
            MovementDate = request.MovementDate,
            MovementType = request.MovementType,
            Source = MovementSource.Batch,
            Notes = Clean(request.Notes),
            CreatedBy = session.Username,
            CreatedUtc = clock.UtcNow
        };

        db.MovementBatches.Add(batch);
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            await transaction.RollbackAsync(cancellationToken);

            await using var verify =
                await factory.CreateDbContextAsync(cancellationToken);

            var duplicate = await GetMatchingBatchRetryAsync(
                verify,
                request,
                cancellationToken);

            if (duplicate is not null)
                return duplicate;

            throw;
        }

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
                CreatedUtc = clock.UtcNow
            });
        }

        await db.SaveChangesAsync(cancellationToken);

        var totalQuantity = request.Lines.Sum(x => x.Quantity);
        var direction = request.MovementType == MovementType.In
            ? "IN (Returned)"
            : "OUT (Taken)";

        db.AuditEvents.Add(new AuditEvent
        {
            TimestampUtc = clock.UtcNow,
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
            ComputerName = client.DeviceName,
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

    public async Task<SaveSingleMovementResult> SaveSingleAsync(
        SaveSingleMovementRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!session.IsAuthenticated)
            throw new UnauthorizedAccessException("You must be logged in to record movements.");

        if (session.Role == UserRole.Viewer)
            throw new UnauthorizedAccessException("Viewer accounts cannot record movements.");

        if (request.MovementDate > clock.Today)
            throw new ArgumentException("Movement date cannot be in the future.");

        if (request.Quantity <= 0)
            throw new ArgumentException("Movement quantity must be greater than zero.");

        if (request.ClientOperationId == Guid.Empty)
            throw new ArgumentException("Client operation ID is required.");

        await using var db = await factory.CreateDbContextAsync(cancellationToken);

        var existingMovement = await GetMatchingSingleRetryAsync(
            db,
            request,
            cancellationToken);

        if (existingMovement is not null)
            return existingMovement;
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        var customer = await db.Customers
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == request.CustomerId && x.IsActive, cancellationToken)
            ?? throw new InvalidOperationException("The customer is missing or inactive.");

        var container = await db.ContainerTypes
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == request.ContainerTypeId && x.IsActive, cancellationToken)
            ?? throw new InvalidOperationException("The container type is missing or inactive.");

        var openingBalance = await db.BinMovements
            .AsNoTracking()
            .Where(x =>
                x.CustomerId == request.CustomerId &&
                x.ContainerTypeId == request.ContainerTypeId)
            .SumAsync(
                x => x.MovementType == MovementType.Out
                    ? x.Quantity
                    : -x.Quantity,
                cancellationToken);

        var movement = new BinMovement
        {
            MovementDate = request.MovementDate,
            MovementType = request.MovementType,
            Source = MovementSource.Manual,
            ClientOperationId = request.ClientOperationId,
            CustomerId = request.CustomerId,
            ContainerTypeId = request.ContainerTypeId,
            Quantity = request.Quantity,
            ReferenceNumber = Clean(request.Reference),
            Notes = Clean(request.Notes),
            CreatedBy = session.Username,
            CreatedUtc = clock.UtcNow
        };

        db.BinMovements.Add(movement);
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            await transaction.RollbackAsync(cancellationToken);

            await using var verify =
                await factory.CreateDbContextAsync(cancellationToken);

            var duplicate = await GetMatchingSingleRetryAsync(
                verify,
                request,
                cancellationToken);

            if (duplicate is not null)
                return duplicate;

            throw;
        }

        var newBalance = MovementPositionMath.Apply(
            openingBalance,
            request.MovementType,
            request.Quantity);

        var direction = request.MovementType == MovementType.In
            ? "IN (Returned)"
            : "OUT (Taken)";

        db.AuditEvents.Add(new AuditEvent
        {
            TimestampUtc = clock.UtcNow,
            UserId = session.UserId,
            Username = session.Username,
            Action = "MOVEMENT_RECORDED",
            EntityType = "BinMovement",
            EntityId = movement.Id.ToString(),
            Description =
                $"{direction} manual movement recorded: {request.Quantity} {container.Name} for {customer.CustomerCode ?? customer.Name}.",
            AfterValues = System.Text.Json.JsonSerializer.Serialize(new
            {
                movement.MovementDate,
                Direction = direction,
                Customer = customer.CustomerCode ?? customer.Name,
                Container = container.Name,
                movement.Quantity,
                movement.ReferenceNumber,
                NewPosition = MovementPositionMath.Format(newBalance)
            }),
            ComputerName = client.DeviceName,
            SessionId = session.SessionId,
            Succeeded = true
        });

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return new SaveSingleMovementResult(movement.Id, newBalance);
    }

    public async Task<OperationalDashboardSummary> GetDashboardSummaryAsync(
        DateOnly date,
        CancellationToken cancellationToken = default)
    {
        await using var db = await factory.CreateDbContextAsync(cancellationToken);

        var today = await db.BinMovements
            .AsNoTracking()
            .Where(x => x.MovementDate == date)
            .GroupBy(x => x.MovementType)
            .Select(g => new { Type = g.Key, Quantity = g.Sum(x => x.Quantity) })
            .ToListAsync(cancellationToken);

        var returned = today
            .Where(x => x.Type == MovementType.In)
            .Sum(x => x.Quantity);

        var taken = today
            .Where(x => x.Type == MovementType.Out)
            .Sum(x => x.Quantity);

        var positions = await db.BinMovements
            .AsNoTracking()
            .GroupBy(x => new { x.CustomerId, x.ContainerTypeId })
            .Select(g => new
            {
                g.Key.CustomerId,
                Balance = g.Sum(x =>
                    x.MovementType == MovementType.Out
                        ? x.Quantity
                        : -x.Quantity)
            })
            .ToListAsync(cancellationToken);

        var outstanding = positions
            .Where(x => x.Balance > 0)
            .Sum(x => x.Balance);

        var settings = await db.ApplicationSettings
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == 1, cancellationToken);

        var threshold = settings?.AttentionQuantityThreshold ?? 20;

        var requiresAttention = positions
            .Where(x => x.Balance > threshold)
            .Select(x => x.CustomerId)
            .Distinct()
            .Count();

        return new OperationalDashboardSummary(
            returned,
            taken,
            outstanding,
            requiresAttention);
    }

    private sealed record BatchRetryLine(
        int CustomerId,
        int ContainerTypeId,
        int Quantity,
        string? Reference,
        string? Notes);

    private static async Task<SaveMovementBatchResult?> GetMatchingBatchRetryAsync(
        BinTrackerDbContext db,
        SaveMovementBatchRequest request,
        CancellationToken cancellationToken)
    {
        var existing = await db.MovementBatches
            .AsNoTracking()
            .Where(x => x.ClientOperationId == request.ClientOperationId)
            .Select(x => new
            {
                x.Id,
                x.MovementDate,
                x.MovementType,
                x.Notes
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (existing is null)
            return null;

        var persistedLines = await db.BinMovements
            .AsNoTracking()
            .Where(x => x.MovementBatchId == existing.Id)
            .Select(x => new BatchRetryLine(
                x.CustomerId,
                x.ContainerTypeId,
                x.Quantity,
                x.ReferenceNumber,
                x.Notes))
            .ToListAsync(cancellationToken);

        var requestedLines = request.Lines
            .Select(x => new BatchRetryLine(
                x.CustomerId,
                x.ContainerTypeId,
                x.Quantity,
                Clean(x.Reference),
                Clean(x.Notes)))
            .ToList();

        static IEnumerable<BatchRetryLine> Canonical(
            IEnumerable<BatchRetryLine> lines) =>
            lines.OrderBy(x => x.CustomerId)
                .ThenBy(x => x.ContainerTypeId)
                .ThenBy(x => x.Quantity)
                .ThenBy(x => x.Reference, StringComparer.Ordinal)
                .ThenBy(x => x.Notes, StringComparer.Ordinal);

        if (existing.MovementDate != request.MovementDate ||
            existing.MovementType != request.MovementType ||
            !string.Equals(existing.Notes, Clean(request.Notes), StringComparison.Ordinal) ||
            !Canonical(persistedLines).SequenceEqual(Canonical(requestedLines)))
        {
            throw new InvalidOperationException(
                "This client operation ID was already used for a different batch request.");
        }

        return new SaveMovementBatchResult(
            existing.Id,
            persistedLines.Count,
            persistedLines.Sum(x => x.Quantity));
    }

    private static async Task<SaveSingleMovementResult?> GetMatchingSingleRetryAsync(
        BinTrackerDbContext db,
        SaveSingleMovementRequest request,
        CancellationToken cancellationToken)
    {
        var existing = await db.BinMovements
            .AsNoTracking()
            .Where(x => x.ClientOperationId == request.ClientOperationId)
            .Select(x => new
            {
                x.Id,
                x.MovementDate,
                x.MovementType,
                x.Source,
                x.CustomerId,
                x.ContainerTypeId,
                x.Quantity,
                x.ReferenceNumber,
                x.Notes
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (existing is null)
            return null;

        if (existing.Source != MovementSource.Manual ||
            existing.MovementDate != request.MovementDate ||
            existing.MovementType != request.MovementType ||
            existing.CustomerId != request.CustomerId ||
            existing.ContainerTypeId != request.ContainerTypeId ||
            existing.Quantity != request.Quantity ||
            !string.Equals(existing.ReferenceNumber, Clean(request.Reference), StringComparison.Ordinal) ||
            !string.Equals(existing.Notes, Clean(request.Notes), StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "This client operation ID was already used for a different movement request.");
        }

        var balance = await db.BinMovements
            .AsNoTracking()
            .Where(x =>
                x.CustomerId == existing.CustomerId &&
                x.ContainerTypeId == existing.ContainerTypeId)
            .SumAsync(
                x => x.MovementType == MovementType.Out
                    ? x.Quantity
                    : -x.Quantity,
                cancellationToken);

        return new SaveSingleMovementResult(existing.Id, balance);
    }

    private static string? Clean(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
