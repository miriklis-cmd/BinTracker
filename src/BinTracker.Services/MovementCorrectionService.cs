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

public sealed record ReverseMovementRequest(long MovementId, string Reason);

public sealed record ReverseMovementResult(long OriginalMovementId, long ReversalMovementId);

public interface IMovementCorrectionService
{
    Task<MovementCorrectionDetail?> GetAsync(long movementId, CancellationToken cancellationToken = default);
    Task<ReverseMovementResult> ReverseAsync(ReverseMovementRequest request, CancellationToken cancellationToken = default);
}

internal sealed class MovementCorrectionService(
    IDbContextFactory<BinTrackerDbContext> factory,
    UserSession session) : IMovementCorrectionService
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
        if (session.Role != UserRole.Administrator)
            throw new UnauthorizedAccessException("Administrator access is required to reverse saved movements.");

        var reason = (request.Reason ?? string.Empty).Trim();
        if (reason.Length < 3)
            throw new InvalidOperationException("Enter a reason for the reversal.");
        if (reason.Length > 500)
            throw new InvalidOperationException("Reversal reason cannot exceed 500 characters.");

        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        await using var tx = await db.Database.BeginTransactionAsync(cancellationToken);

        var original = await db.BinMovements
            .SingleOrDefaultAsync(x => x.Id == request.MovementId, cancellationToken)
            ?? throw new InvalidOperationException("The selected movement no longer exists.");

        if (original.ReversesMovementId.HasValue)
            throw new InvalidOperationException("A reversal movement cannot itself be reversed.");
        if (original.CorrectedByMovementId.HasValue ||
            await db.BinMovements.AnyAsync(x => x.ReversesMovementId == original.Id, cancellationToken))
            throw new InvalidOperationException("This movement has already been reversed.");

        var reversal = new BinMovement
        {
            MovementDate = DateOnly.FromDateTime(DateTime.Today),
            MovementType = original.MovementType == MovementType.Out ? MovementType.In : MovementType.Out,
            Source = MovementSource.Manual,
            CustomerId = original.CustomerId,
            ContainerTypeId = original.ContainerTypeId,
            Quantity = original.Quantity,
            ReferenceNumber = string.IsNullOrWhiteSpace(original.ReferenceNumber)
                ? $"REV-{original.Id}"
                : $"REV-{original.Id} / {original.ReferenceNumber}",
            Notes = $"Reversal of movement #{original.Id}. Reason: {reason}",
            CreatedBy = session.Username,
            CreatedUtc = DateTime.UtcNow,
            ReversesMovementId = original.Id,
            CorrectionReason = reason
        };

        db.BinMovements.Add(reversal);
        await db.SaveChangesAsync(cancellationToken);
        original.CorrectedByMovementId = reversal.Id;

        db.AuditEvents.Add(new AuditEvent
        {
            TimestampUtc = DateTime.UtcNow,
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
            ComputerName = Environment.MachineName,
            SessionId = session.SessionId,
            Succeeded = true
        });

        await db.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);
        return new ReverseMovementResult(original.Id, reversal.Id);
    }
}
