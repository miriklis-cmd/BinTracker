using BinTracker.Core;
using BinTracker.Data;
using Microsoft.EntityFrameworkCore;

namespace BinTracker.Services;

public sealed record MovementCorrectionDetail(
    long MovementId,
    DateOnly MovementDate,
    string CustomerCode,
    string CustomerName,
    string ContainerType,
    MovementType Direction,
    int Quantity,
    MovementSource Source,
    string Reference,
    string Notes,
    string EnteredBy,
    bool IsAlreadyReversed);

public sealed record ReverseMovementRequest(Guid ClientOperationId, long MovementId, string Reason);

public sealed record ReverseMovementResult(long OriginalMovementId, long ReversalMovementId);

public interface IMovementCorrectionService
{
    Task<MovementCorrectionDetail?> GetAsync(long movementId, CancellationToken cancellationToken = default);
    Task<ReverseMovementResult> ReverseAsync(ReverseMovementRequest request, CancellationToken cancellationToken = default);
}

internal sealed class MovementCorrectionService(
    IDbContextFactory<BinTrackerDbContext> factory,
    IUserContext session,
    IBusinessClock clock,
    IClientContext client) : IMovementCorrectionService
{
    public async Task<MovementCorrectionDetail?> GetAsync(long movementId, CancellationToken cancellationToken = default)
    {
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        return await db.BinMovements.AsNoTracking()
            .Where(x => x.Id == movementId)
            .Select(x => new MovementCorrectionDetail(
                x.Id, x.MovementDate, x.Customer.CustomerCode ?? "", x.Customer.Name,
                x.ContainerType.Name, x.MovementType, x.Quantity, x.Source,
                x.ReferenceNumber ?? "", x.Notes ?? "", x.CreatedBy ?? "",
                x.CorrectedByMovementId != null))
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<ReverseMovementResult> ReverseAsync(
        ReverseMovementRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!session.IsAuthenticated)
            throw new InvalidOperationException("You must be signed in to reverse a movement.");
        if (session.Role is not (UserRole.Administrator or UserRole.Operator))
            throw new UnauthorizedAccessException(
                "Only Administrators and Operators can reverse ordinary operational movements.");

        var reason = (request.Reason ?? string.Empty).Trim();
        if (reason.Length < 3)
            throw new InvalidOperationException("Enter a reason for the reversal.");
        if (reason.Length > 500)
            throw new InvalidOperationException("Reversal reason cannot exceed 500 characters.");

        if (request.ClientOperationId == Guid.Empty)
            throw new ArgumentException("Client operation ID is required.");

        await using var db = await factory.CreateDbContextAsync(cancellationToken);

        var priorReversal = await db.BinMovements
            .AsNoTracking()
            .Where(x => x.ClientOperationId == request.ClientOperationId)
            .Select(x => new { x.Id, x.ReversesMovementId, x.CorrectionReason })
            .SingleOrDefaultAsync(cancellationToken);

        if (priorReversal is not null)
        {
            if (priorReversal.ReversesMovementId == request.MovementId &&
                string.Equals(priorReversal.CorrectionReason, reason, StringComparison.Ordinal))
            {
                return new ReverseMovementResult(
                    request.MovementId,
                    priorReversal.Id);
            }

            throw new InvalidOperationException(
                "This client operation ID was already used for a different reversal request.");
        }
        await using var tx = await db.Database.BeginTransactionAsync(cancellationToken);

        var original = await db.BinMovements
            .SingleOrDefaultAsync(x => x.Id == request.MovementId, cancellationToken)
            ?? throw new InvalidOperationException("The selected movement no longer exists.");

        if (original.Source == MovementSource.Adjustment)
            throw new InvalidOperationException(
                "Opening adjustments cannot be reversed from Movement History. " +
                "They affect brought-forward position and require an Administrator-controlled adjustment workflow.");

        if (original.Source == MovementSource.ExcelImport || original.ImportRunId.HasValue)
            throw new InvalidOperationException(
                "Excel Import movements cannot be reversed individually from Movement History. " +
                "Use the Administrator Replace / Correct import workflow so Import Run provenance remains intact.");

        if (original.ReversesMovementId.HasValue)
            throw new InvalidOperationException("A reversal movement cannot itself be reversed.");
        if (original.CorrectedByMovementId.HasValue ||
            await db.BinMovements.AnyAsync(x => x.ReversesMovementId == original.Id, cancellationToken))
            throw new InvalidOperationException("This movement has already been reversed.");

        var reversal = new BinMovement
        {
            MovementDate = clock.Today,
            MovementType = original.MovementType == MovementType.Out ? MovementType.In : MovementType.Out,
            Source = MovementSource.Manual,
            ClientOperationId = request.ClientOperationId,
            CustomerId = original.CustomerId,
            ContainerTypeId = original.ContainerTypeId,
            Quantity = original.Quantity,
            ReferenceNumber = string.IsNullOrWhiteSpace(original.ReferenceNumber)
                ? $"REV-{original.Id}"
                : $"REV-{original.Id} / {original.ReferenceNumber}",
            Notes = $"Reversal of movement #{original.Id}. Reason: {reason}",
            CreatedBy = session.Username,
            CreatedUtc = clock.UtcNow,
            ReversesMovementId = original.Id,
            CorrectionReason = reason
        };

        db.BinMovements.Add(reversal);
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            await tx.RollbackAsync(cancellationToken);

            // A database uniqueness constraint is the authoritative guard
            // against two remote users reversing the same movement at once.
            // Re-check after rollback using a fresh context so this works for
            // current and future relational providers without provider-specific
            // exception parsing.
            await using var verify =
                await factory.CreateDbContextAsync(cancellationToken);

            var duplicateOperation = await verify.BinMovements
                .AsNoTracking()
                .Where(x => x.ClientOperationId == request.ClientOperationId)
                .Select(x => new { x.Id, x.ReversesMovementId, x.CorrectionReason })
                .SingleOrDefaultAsync(cancellationToken);

            if (duplicateOperation is not null)
            {
                if (duplicateOperation.ReversesMovementId == request.MovementId &&
                    string.Equals(duplicateOperation.CorrectionReason, reason, StringComparison.Ordinal))
                {
                    return new ReverseMovementResult(
                        request.MovementId,
                        duplicateOperation.Id);
                }

                throw new InvalidOperationException(
                    "This client operation ID was already used for a different reversal request.");
            }

            var alreadyReversed = await verify.BinMovements
                .AsNoTracking()
                .AnyAsync(
                    x => x.Id == request.MovementId &&
                         x.CorrectedByMovementId != null,
                    cancellationToken)
                || await verify.BinMovements
                    .AsNoTracking()
                    .AnyAsync(
                        x => x.ReversesMovementId == request.MovementId,
                        cancellationToken);

            if (alreadyReversed)
                throw new InvalidOperationException(
                    "This movement has already been reversed.");

            throw;
        }

        original.CorrectedByMovementId = reversal.Id;

        db.AuditEvents.Add(new AuditEvent
        {
            TimestampUtc = clock.UtcNow,
            UserId = session.UserId,
            Username = session.Username,
            Action = "MOVEMENT_REVERSED",
            EntityType = "BinMovement",
            EntityId = original.Id.ToString(),
            Description = $"Movement #{original.Id} reversed by movement #{reversal.Id}. Reason: {reason}",
            BeforeValues = System.Text.Json.JsonSerializer.Serialize(new {
                original.Id, original.MovementDate, original.MovementType, original.CustomerId,
                original.ContainerTypeId, original.Quantity, original.ReferenceNumber, original.Notes
            }),
            AfterValues = System.Text.Json.JsonSerializer.Serialize(new {
                ReversalMovementId = reversal.Id, reversal.MovementDate, reversal.MovementType,
                reversal.Quantity, reversal.ReversesMovementId, reason
            }),
            ComputerName = client.DeviceName,
            SessionId = session.SessionId,
            Succeeded = true
        });

        await db.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);
        return new ReverseMovementResult(original.Id, reversal.Id);
    }
}
