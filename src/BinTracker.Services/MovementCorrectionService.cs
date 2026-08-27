using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using BinTracker.Core;
using BinTracker.Data;
using Microsoft.EntityFrameworkCore;

namespace BinTracker.Services;

public sealed record MovementCorrectionDetail(long MovementId, DateOnly MovementDate, int CustomerId,
    string CustomerCode, string CustomerName, int ContainerTypeId, string ContainerType,
    MovementType Direction, int Quantity, MovementSource Source, string Reference, string Notes,
    string EnteredBy, bool IsAlreadyReversed, int? MovementBatchId);
public sealed record ReverseMovementRequest(Guid ClientOperationId, long MovementId, string Reason);
public sealed record ReverseMovementResult(long OriginalMovementId, long ReversalMovementId);
public sealed record CorrectMovementRequest(Guid ClientOperationId, long MovementId, DateOnly MovementDate,
    int CustomerId, int ContainerTypeId, MovementType Direction, int Quantity, string? Reference,
    string? Notes, string Reason);
public sealed record CorrectBatchRequest(Guid ClientOperationId, int MovementBatchId,
    DateOnly? CorrectedDate, MovementType? CorrectedDirection, string Reason);
public sealed record MovementCorrectionLineResult(long OriginalMovementId, long NeutralisingMovementId,
    long ReplacementMovementId);
public sealed record MovementCorrectionResult(long CorrectionOperationId, int? ReplacementBatchId,
    IReadOnlyList<MovementCorrectionLineResult> Lines);
public sealed record MovementBatchCorrectionLineDetail(long MovementId, int MovementBatchId,
    int CustomerId, string CustomerCode, string CustomerName, int ContainerTypeId,
    string ContainerType, int Quantity);
public sealed record MovementBatchCorrectionDetail(int BatchId, int LineCount, int TotalContainers,
    DateOnly MovementDate, MovementType Direction, bool IsEligible,
    IReadOnlyList<MovementBatchCorrectionLineDetail> Lines);
public sealed record MovementCorrectionSelections(
    CustomerListRow Customer,
    ContainerTypeListRow ContainerType);

public static class MovementCorrectionSelection
{
    public static MovementCorrectionSelections Resolve(
        MovementCorrectionDetail movement,
        IReadOnlyList<CustomerListRow> customers,
        IReadOnlyList<ContainerTypeListRow> containerTypes)
    {
        var matchingCustomers = customers.Where(x => x.Id == movement.CustomerId).ToArray();
        if (matchingCustomers.Length != 1)
            throw new InvalidOperationException(
                $"Persisted movement #{movement.MovementId} references customer ID {movement.CustomerId}, " +
                $"but the correction customer list contains {matchingCustomers.Length} matching records. Reload master data and try again.");

        var matchingContainers = containerTypes.Where(x => x.Id == movement.ContainerTypeId).ToArray();
        if (matchingContainers.Length != 1)
            throw new InvalidOperationException(
                $"Persisted movement #{movement.MovementId} references container type ID {movement.ContainerTypeId}, " +
                $"but the correction container list contains {matchingContainers.Length} matching records. Reload master data and try again.");

        return new(matchingCustomers[0], matchingContainers[0]);
    }

    public static int ResolveBatchDirectionIndex(
        MovementBatchCorrectionDetail batch,
        IReadOnlyList<MovementType> directionValues)
    {
        var matches = directionValues
            .Select((value, index) => new { value, index })
            .Where(x => x.value == batch.Direction)
            .ToArray();
        if (matches.Length != 1)
            throw new InvalidOperationException(
                $"Persisted movement batch #{batch.BatchId} references direction {batch.Direction}, " +
                $"but the correction direction list contains {matches.Length} matching records. Reload the dialog and try again.");

        return matches[0].index;
    }
}

public interface IMovementCorrectionService
{
    Task<MovementCorrectionDetail?> GetAsync(long id, CancellationToken token = default);
    Task<MovementBatchCorrectionDetail?> GetBatchAsync(int id, CancellationToken token = default);
    Task<ReverseMovementResult> ReverseAsync(ReverseMovementRequest request, CancellationToken token = default);
    Task<MovementCorrectionResult> CorrectAsync(CorrectMovementRequest request, CancellationToken token = default);
    Task<MovementCorrectionResult> CorrectBatchAsync(CorrectBatchRequest request, CancellationToken token = default);
}

internal sealed class MovementCorrectionService(IDbContextFactory<BinTrackerDbContext> factory,
    IUserContext session, IBusinessClock clock, IClientContext client) : IMovementCorrectionService
{
    public async Task<MovementCorrectionDetail?> GetAsync(long id, CancellationToken token = default)
    {
        await using var db = await factory.CreateDbContextAsync(token);
        return await db.BinMovements.AsNoTracking().Where(x => x.Id == id).Select(x =>
            new MovementCorrectionDetail(x.Id, x.MovementDate, x.CustomerId,
                x.Customer.CustomerCode ?? "", x.Customer.Name, x.ContainerTypeId, x.ContainerType.Name,
                x.MovementType, x.Quantity, x.Source, x.ReferenceNumber ?? "", x.Notes ?? "",
                x.CreatedBy ?? "", x.CorrectedByMovementId != null || x.ReversesMovementId != null,
                x.MovementBatchId)).SingleOrDefaultAsync(token);
    }

    public async Task<MovementBatchCorrectionDetail?> GetBatchAsync(int id, CancellationToken token = default)
    {
        await using var db = await factory.CreateDbContextAsync(token);
        var row = await db.MovementBatches.AsNoTracking().Where(x => x.Id == id).Select(x => new
        {
            x.Id, x.MovementDate, x.MovementType, Count = x.Movements.Count,
            Total = x.Movements.Sum(m => m.Quantity),
            Eligible = x.Movements.All(m => (m.Source == MovementSource.Manual || m.Source == MovementSource.Batch) &&
                m.ImportRunId == null && m.ReversesMovementId == null && m.CorrectedByMovementId == null)
        }).SingleOrDefaultAsync(token);
        if (row is null) return null;
        var lines = await db.BinMovements.AsNoTracking()
            .Where(x => x.MovementBatchId == row.Id)
            .OrderBy(x => x.Id)
            .Select(x => new MovementBatchCorrectionLineDetail(x.Id, x.MovementBatchId!.Value,
                x.CustomerId, x.Customer.CustomerCode ?? "", x.Customer.Name,
                x.ContainerTypeId, x.ContainerType.Name, x.Quantity))
            .ToArrayAsync(token);
        if (lines.Length != row.Count || lines.Any(x => x.MovementBatchId != row.Id))
            throw new InvalidOperationException(
                $"Persisted movement batch #{row.Id} changed while its correction detail was loading. Reload and try again.");
        return new(row.Id, row.Count, row.Total, row.MovementDate,
            row.MovementType, row.Count > 0 && row.Eligible, lines);
    }

    public async Task<ReverseMovementResult> ReverseAsync(ReverseMovementRequest request, CancellationToken token = default)
    {
        Authorize("reverse");
        var reason = Reason(request.Reason, "reversal");
        OperationId(request.ClientOperationId);
        await using var db = await factory.CreateDbContextAsync(token);
        var retry = await ReversalRetry(db, request, reason, token);
        if (retry is not null) return retry;
        if (await db.MovementCorrectionOperations.AnyAsync(x => x.ClientOperationId == request.ClientOperationId, token))
            throw new InvalidOperationException("This client operation ID was already used for a correction request.");

        await using var tx = await db.Database.BeginTransactionAsync(token);
        var original = await Eligible(db, request.MovementId, token);
        var reversal = Neutraliser(original, request.ClientOperationId, reason, clock.Today);
        db.Add(reversal);
        try { await db.SaveChangesAsync(token); }
        catch (DbUpdateException)
        {
            await tx.RollbackAsync(token);
            // The database uniqueness constraint is the authoritative guard.
            // A fresh context translates the losing race into retry success or
            // the stable "already been reversed" business outcome.
            await using var verify = await factory.CreateDbContextAsync(token);
            retry = await ReversalRetry(verify, request, reason, token);
            if (retry is not null) return retry;
            if (await Consumed(verify, request.MovementId, token))
                throw new InvalidOperationException("This movement has already been reversed or corrected.");
            throw;
        }
        original.CorrectedByMovementId = reversal.Id;
        db.Add(Audit("MOVEMENT_REVERSED", "BinMovement", original.Id.ToString(),
            $"Movement #{original.Id} reversed by movement #{reversal.Id}. Reason: {reason}",
            Snap(original), new { ReversalMovementId = reversal.Id, reversal.MovementDate,
                reversal.MovementType, reversal.Quantity, reason }));
        await db.SaveChangesAsync(token);
        await tx.CommitAsync(token);
        return new(original.Id, reversal.Id);
    }

    public Task<MovementCorrectionResult> CorrectAsync(CorrectMovementRequest request, CancellationToken token = default)
    {
        if (request.MovementDate == default || request.CustomerId <= 0 || request.ContainerTypeId <= 0 || request.Quantity <= 0)
            throw new InvalidOperationException("Corrected date, customer, container type and a positive quantity are required.");
        if (request.MovementDate > clock.Today)
            throw new ArgumentException("Corrected movement date cannot be in the future.");
        var reason = Reason(request.Reason, "correction");
        var fp = Hash(new { Type = "single", request.MovementId, request.MovementDate, request.CustomerId,
            request.ContainerTypeId, request.Direction, request.Quantity, Reference = Clean(request.Reference),
            Notes = Clean(request.Notes), Reason = reason });
        return CorrectCore(request.ClientOperationId, MovementCorrectionKind.Single, null, [request.MovementId],
            request.MovementDate, request.Direction, request.CustomerId, request.ContainerTypeId, request.Quantity,
            Clean(request.Reference), Clean(request.Notes), reason, fp, token);
    }

    public async Task<MovementCorrectionResult> CorrectBatchAsync(CorrectBatchRequest request, CancellationToken token = default)
    {
        Authorize("correct"); OperationId(request.ClientOperationId);
        var reason = Reason(request.Reason, "correction");
        if (request.CorrectedDate is null && request.CorrectedDirection is null)
            throw new InvalidOperationException("Change the batch date, direction, or both.");
        if (request.CorrectedDate > clock.Today)
            throw new ArgumentException("Corrected batch date cannot be in the future.");
        await using var db = await factory.CreateDbContextAsync(token);
        var ids = await db.BinMovements.AsNoTracking().Where(x => x.MovementBatchId == request.MovementBatchId)
            .OrderBy(x => x.Id).Select(x => x.Id).ToArrayAsync(token);
        if (ids.Length == 0) throw new InvalidOperationException("The persisted batch no longer exists or is empty.");
        var fp = Hash(new { Type = "batch", request.MovementBatchId, request.CorrectedDate,
            request.CorrectedDirection, Reason = reason, MovementIds = ids });
        return await CorrectCore(request.ClientOperationId, MovementCorrectionKind.WholeBatch,
            request.MovementBatchId, ids, request.CorrectedDate, request.CorrectedDirection,
            null, null, null, null, null, reason, fp, token);
    }

    private async Task<MovementCorrectionResult> CorrectCore(Guid id, MovementCorrectionKind kind,
        int? originalBatchId, IReadOnlyList<long> ids, DateOnly? date, MovementType? direction,
        int? customerId, int? containerId, int? quantity, string? reference, string? notes,
        string reason, string fingerprint, CancellationToken token)
    {
        Authorize("correct"); OperationId(id);
        var actorUserId = session.UserId ?? throw new InvalidOperationException("You must be signed in to correct a movement.");
        await using var db = await factory.CreateDbContextAsync(token);
        var retry = await CorrectionRetry(db, id, fingerprint, token);
        if (retry is not null) return retry;
        if (await db.BinMovements.AnyAsync(x => x.ClientOperationId == id, token))
            throw new InvalidOperationException("This client operation ID was already used for a reversal request.");
        await using var tx = await db.Database.BeginTransactionAsync(token);
        var originals = await db.BinMovements.Where(x => ids.Contains(x.Id)).OrderBy(x => x.Id).ToListAsync(token);
        if (originals.Count != ids.Count) throw new InvalidOperationException("One or more movements no longer exist. Nothing was corrected.");
        foreach (var item in originals) ValidateEligible(item);
        if (kind == MovementCorrectionKind.Single)
        {
            var original = originals[0];
            if (original.MovementDate == date && original.MovementType == direction &&
                original.CustomerId == customerId && original.ContainerTypeId == containerId &&
                original.Quantity == quantity && Clean(original.ReferenceNumber) == reference &&
                Clean(original.Notes) == notes)
                throw new InvalidOperationException("Change at least one saved movement value. Nothing was corrected.");
        }
        if (kind == MovementCorrectionKind.WholeBatch &&
            (!date.HasValue || originals.All(x => x.MovementDate == date.Value)) &&
            (!direction.HasValue || originals.All(x => x.MovementType == direction.Value)))
            throw new InvalidOperationException("The corrected batch date/direction must differ from the saved batch. Nothing was corrected.");
        if (await db.BinMovements.AnyAsync(x => ids.Contains(x.ReversesMovementId ?? 0), token))
            throw new InvalidOperationException("One or more movements have already been reversed or corrected. Nothing was corrected.");

        MovementBatch? batch = null;
        if (kind == MovementCorrectionKind.WholeBatch)
        {
            batch = new MovementBatch { MovementDate = date ?? originals[0].MovementDate,
                MovementType = direction ?? originals[0].MovementType, Source = MovementSource.Batch,
                Notes = $"Corrected replacement for batch #{originalBatchId}. Reason: {reason}",
                CreatedBy = session.Username, CreatedUtc = clock.UtcNow };
            db.Add(batch);
        }
        var operation = new MovementCorrectionOperation { ClientOperationId = id,
            RequestFingerprint = fingerprint, Kind = kind, OriginalBatchId = originalBatchId,
            ReplacementBatchId = null,
            Reason = reason, ActorUserId = actorUserId, ActorUsername = session.Username,
            CreatedUtc = clock.UtcNow };
        db.Add(operation);
        var triples = originals.Select(o => (Original: o,
            Neutral: Neutraliser(o, null, reason, o.MovementDate),
            Replacement: new BinMovement { MovementDate = date ?? o.MovementDate,
                MovementType = direction ?? o.MovementType, Source = o.Source,
                CustomerId = customerId ?? o.CustomerId, ContainerTypeId = containerId ?? o.ContainerTypeId,
                Quantity = quantity ?? o.Quantity, ReferenceNumber = kind == MovementCorrectionKind.Single ? reference : o.ReferenceNumber,
                Notes = kind == MovementCorrectionKind.Single ? notes : o.Notes, MovementBatch = batch,
                CreatedBy = session.Username, CreatedUtc = clock.UtcNow, CorrectionReason = reason })).ToList();
        foreach (var t in triples) db.AddRange(t.Neutral, t.Replacement);
        try
        {
            await db.SaveChangesAsync(token);
            operation.ReplacementBatchId = batch?.Id;
            foreach (var t in triples)
            {
                t.Original.CorrectedByMovementId = t.Neutral.Id;
                operation.Lines.Add(new MovementCorrectionLine { OriginalMovementId = t.Original.Id,
                    NeutralisingMovementId = t.Neutral.Id, ReplacementMovementId = t.Replacement.Id });
            }
            db.Add(Audit(kind == MovementCorrectionKind.WholeBatch ? "MOVEMENT_BATCH_CORRECTED" : "MOVEMENT_CORRECTED",
                kind == MovementCorrectionKind.WholeBatch ? "MovementBatch" : "BinMovement",
                (originalBatchId ?? originals[0].Id).ToString(),
                kind == MovementCorrectionKind.WholeBatch
                    ? $"Batch #{originalBatchId} corrected in full: {triples.Count} lines, {originals.Sum(x => x.Quantity)} containers. Reason: {reason}"
                    : $"Movement #{originals[0].Id} corrected by replacement. Reason: {reason}",
                originals.Select(Snap).ToArray(), triples.Select(t => new { t.Original.Id,
                    NeutralisingMovementId = t.Neutral.Id, ReplacementMovementId = t.Replacement.Id,
                    t.Replacement.MovementDate, t.Replacement.CustomerId, t.Replacement.ContainerTypeId,
                    t.Replacement.MovementType, t.Replacement.Quantity, t.Replacement.ReferenceNumber,
                    t.Replacement.Notes }).ToArray()));
            await db.SaveChangesAsync(token);
            await tx.CommitAsync(token);
        }
        catch (DbUpdateException)
        {
            await tx.RollbackAsync(token);
            await using var verify = await factory.CreateDbContextAsync(token);
            retry = await CorrectionRetry(verify, id, fingerprint, token);
            if (retry is not null) return retry;
            if (await verify.BinMovements.AnyAsync(x => ids.Contains(x.ReversesMovementId ?? 0), token))
                throw new InvalidOperationException("One or more movements were concurrently reversed or corrected. Nothing was corrected.");
            throw;
        }
        return Result(operation);
    }

    private void Authorize(string verb)
    {
        if (!session.IsAuthenticated) throw new InvalidOperationException($"You must be signed in to {verb} a movement.");
        if (session.Role is not (UserRole.Administrator or UserRole.Operator))
            throw new UnauthorizedAccessException($"Only Administrators and Operators can {verb} ordinary operational movements.");
    }
    private static string Reason(string? text, string noun)
    {
        var value = (text ?? "").Trim();
        if (value.Length < 3) throw new InvalidOperationException($"Enter a reason for the {noun}.");
        if (value.Length > 500) throw new InvalidOperationException("Correction/reversal reason cannot exceed 500 characters.");
        return value;
    }
    private static void OperationId(Guid id) { if (id == Guid.Empty) throw new ArgumentException("Client operation ID is required."); }
    private static void ValidateEligible(BinMovement original)
    {
        if (original.Source == MovementSource.Adjustment) throw new InvalidOperationException("Opening adjustments require an Administrator-controlled adjustment workflow.");
        if (original.Source == MovementSource.ExcelImport || original.ImportRunId.HasValue) throw new InvalidOperationException("Excel Import movements require the Administrator Replace / Correct import workflow.");
        if (original.ReversesMovementId.HasValue) throw new InvalidOperationException("A reversal movement cannot itself be reversed or corrected.");
        if (original.CorrectedByMovementId.HasValue) throw new InvalidOperationException("This movement has already been reversed or corrected.");
    }
    private static async Task<BinMovement> Eligible(BinTrackerDbContext db, long id, CancellationToken token)
    {
        var row = await db.BinMovements.SingleOrDefaultAsync(x => x.Id == id, token) ?? throw new InvalidOperationException("The selected movement no longer exists.");
        ValidateEligible(row);
        if (await db.BinMovements.AnyAsync(x => x.ReversesMovementId == id, token)) throw new InvalidOperationException("This movement has already been reversed or corrected.");
        return row;
    }
    private BinMovement Neutraliser(BinMovement original, Guid? id, string reason, DateOnly date) => new()
    {
        MovementDate = date, MovementType = original.MovementType == MovementType.Out ? MovementType.In : MovementType.Out,
        Source = MovementSource.Manual, ClientOperationId = id, CustomerId = original.CustomerId,
        ContainerTypeId = original.ContainerTypeId, Quantity = original.Quantity,
        ReferenceNumber = string.IsNullOrWhiteSpace(original.ReferenceNumber) ? $"REV-{original.Id}" : $"REV-{original.Id} / {original.ReferenceNumber}",
        Notes = $"Neutralises movement #{original.Id}. Reason: {reason}", CreatedBy = session.Username,
        CreatedUtc = clock.UtcNow, ReversesMovementId = original.Id, CorrectionReason = reason
    };
    private AuditEvent Audit(string action, string type, string entityId, string description, object before, object after) => new()
    {
        TimestampUtc = clock.UtcNow, UserId = session.UserId, Username = session.Username, Action = action,
        EntityType = type, EntityId = entityId, Description = description,
        BeforeValues = JsonSerializer.Serialize(before), AfterValues = JsonSerializer.Serialize(after),
        ComputerName = client.DeviceName, SessionId = session.SessionId, Succeeded = true,
        RequiresAdministratorReview = session.Role == UserRole.Operator
    };
    private static object Snap(BinMovement x) => new { x.Id, x.MovementDate, x.MovementType, x.CustomerId, x.ContainerTypeId, x.Quantity, x.ReferenceNumber, x.Notes, x.MovementBatchId };
    private static string? Clean(string? x) => string.IsNullOrWhiteSpace(x) ? null : x.Trim();
    private static string Hash(object x) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(x))));
    private static async Task<bool> Consumed(BinTrackerDbContext db, long id, CancellationToken token) =>
        await db.BinMovements.AnyAsync(x => x.Id == id && x.CorrectedByMovementId != null, token) || await db.BinMovements.AnyAsync(x => x.ReversesMovementId == id, token);
    private static async Task<ReverseMovementResult?> ReversalRetry(BinTrackerDbContext db, ReverseMovementRequest r, string reason, CancellationToken token)
    {
        var row = await db.BinMovements.AsNoTracking().Where(x => x.ClientOperationId == r.ClientOperationId)
            .Select(x => new { x.Id, x.ReversesMovementId, x.CorrectionReason }).SingleOrDefaultAsync(token);
        if (row is null) return null;
        if (row.ReversesMovementId == r.MovementId && row.CorrectionReason == reason) return new(r.MovementId, row.Id);
        throw new InvalidOperationException("This client operation ID was already used for a different reversal request.");
    }
    private static async Task<MovementCorrectionResult?> CorrectionRetry(BinTrackerDbContext db, Guid id, string fp, CancellationToken token)
    {
        var op = await db.MovementCorrectionOperations.AsNoTracking().Include(x => x.Lines).SingleOrDefaultAsync(x => x.ClientOperationId == id, token);
        if (op is null) return null;
        if (op.RequestFingerprint != fp) throw new InvalidOperationException("This client operation ID was already used for a different correction request.");
        return Result(op);
    }
    private static MovementCorrectionResult Result(MovementCorrectionOperation op) => new(op.Id, op.ReplacementBatchId,
        op.Lines.OrderBy(x => x.OriginalMovementId).Select(x => new MovementCorrectionLineResult(x.OriginalMovementId, x.NeutralisingMovementId, x.ReplacementMovementId)).ToArray());
}
