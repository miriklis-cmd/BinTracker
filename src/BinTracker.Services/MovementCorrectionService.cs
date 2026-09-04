using System.Security.Cryptography;
using System.Globalization;
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

public sealed record BatchCorrectionProposal(DateOnly? CorrectedDate, MovementType? CorrectedDirection)
{
    public bool HasActualChange => CorrectedDate.HasValue || CorrectedDirection.HasValue;
}

public sealed record LogicalMovementMutationCommand(
    Guid ClientOperationId,
    LogicalMovementBatchId LogicalMovementBatchId,
    LogicalMovementGenerationNumber ExpectedGeneration,
    MovementMutationRequest Mutation);

public enum LogicalMovementMutationResultKind
{
    Committed = 0,
    Replayed = 1,
    NoChange = 2
}

public sealed record LogicalMovementMutationResult(
    LogicalMovementMutationResultKind Kind,
    LogicalMovementBatchId LogicalMovementBatchId,
    LogicalMovementGenerationNumber ResultGeneration,
    long? OperationId,
    int? PhysicalOutputBatchId);

public enum LogicalMovementMutationFailure
{
    SchemaUnavailable = 0,
    OperationIdConflict = 1,
    StaleGeneration = 2,
    NotFound = 3,
    ReadOnly = 4,
    Unhealthy = 5,
    IntegrityFailure = 6,
    PersistenceFailure = 7
}

public sealed class LogicalMovementMutationException(
    LogicalMovementMutationFailure failure, string message, Exception? innerException = null)
    : InvalidOperationException(message, innerException)
{
    public LogicalMovementMutationFailure Failure { get; } = failure;
}

public static class MovementCorrectionSelection
{
    public static BatchCorrectionProposal ResolveBatchProposal(
        DateOnly persistedDate,
        MovementType persistedDirection,
        bool changeDate,
        DateOnly proposedDate,
        bool changeDirection,
        MovementType proposedDirection) => new(
            changeDate && proposedDate != persistedDate ? proposedDate : null,
            changeDirection && proposedDirection != persistedDirection ? proposedDirection : null);

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
    Task<LogicalMovementMutationResult> ExecuteLogicalAsync(
        LogicalMovementMutationCommand command, CancellationToken token = default);
}

internal sealed class MovementCorrectionService(IDbContextFactory<BinTrackerDbContext> factory,
    IUserContext session, IBusinessClock clock, IClientContext client,
    IMovementMutationWriter mutationWriter,
    TransactionAuditAppender transactionAuditAppender) : IMovementCorrectionService
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
        if (request.CorrectedDate > clock.Today)
            throw new ArgumentException("Corrected batch date cannot be in the future.");
        await using var db = await factory.CreateDbContextAsync(token);
        var persisted = await db.MovementBatches.AsNoTracking()
            .Where(x => x.Id == request.MovementBatchId)
            .Select(x => new { x.MovementDate, x.MovementType })
            .SingleOrDefaultAsync(token);
        if (persisted is null)
            throw new InvalidOperationException("The persisted batch no longer exists or is empty.");
        var proposal = MovementCorrectionSelection.ResolveBatchProposal(
            persisted.MovementDate, persisted.MovementType,
            request.CorrectedDate.HasValue, request.CorrectedDate ?? persisted.MovementDate,
            request.CorrectedDirection.HasValue, request.CorrectedDirection ?? persisted.MovementType);
        if (!proposal.HasActualChange)
            throw new InvalidOperationException("The corrected batch date and/or direction must actually differ from the saved batch. Nothing was corrected.");
        var ids = await db.BinMovements.AsNoTracking().Where(x => x.MovementBatchId == request.MovementBatchId)
            .OrderBy(x => x.Id).Select(x => x.Id).ToArrayAsync(token);
        if (ids.Length == 0) throw new InvalidOperationException("The persisted batch no longer exists or is empty.");
        var fp = Hash(new { Type = "batch", request.MovementBatchId, proposal.CorrectedDate,
            proposal.CorrectedDirection, Reason = reason, MovementIds = ids });
        return await CorrectCore(request.ClientOperationId, MovementCorrectionKind.WholeBatch,
            request.MovementBatchId, ids, proposal.CorrectedDate, proposal.CorrectedDirection,
            null, null, null, null, null, reason, fp, token);
    }

    public async Task<LogicalMovementMutationResult> ExecuteLogicalAsync(
        LogicalMovementMutationCommand command, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(command.Mutation);
        Authorize(command.Mutation.Kind switch
        {
            MovementMutationKind.Correct => "correct",
            MovementMutationKind.Reverse => "reverse",
            MovementMutationKind.Restore => "restore",
            _ => throw new ArgumentOutOfRangeException(nameof(command))
        });
        if (!mutationWriter.IsEnabled)
            throw new LogicalMovementMutationException(LogicalMovementMutationFailure.SchemaUnavailable,
                "Logical movement mutation execution is dormant in normal runtime composition.");
        OperationId(command.ClientOperationId);
        if (command.LogicalMovementBatchId.Value <= 0 || command.ExpectedGeneration.Value < 0)
            throw new ArgumentException("A valid logical root and expected generation are required.", nameof(command));

        var actorUserId = session.UserId ?? throw new InvalidOperationException(
            "You must be signed in to change a logical movement.");
        var requestJson = CanonicalLogicalRequest(command);
        var intent = new MovementMutationOperationIntent(command.ClientOperationId,
            command.LogicalMovementBatchId, command.ExpectedGeneration,
            OperationKind(command.Mutation), GenerationKind(command.Mutation.Kind), 1,
            requestJson, HashUtf8(requestJson));

        await using var db = await factory.CreateDbContextAsync(token);
        await using var transaction = await db.Database.BeginTransactionAsync(token);
        try
        {
            try
            {
                await mutationWriter.EnsureReadyAsync(db, command.LogicalMovementBatchId, token);
            }
            catch (InvalidOperationException ex) when (
                ex.Message.Contains("SCHEMA17_REQUIRED", StringComparison.Ordinal))
            {
                throw new LogicalMovementMutationException(LogicalMovementMutationFailure.SchemaUnavailable,
                    "Exact schema 17 is required for dormant logical movement mutation execution.", ex);
            }
            catch (InvalidOperationException ex)
            {
                throw new LogicalMovementMutationException(LogicalMovementMutationFailure.Unhealthy,
                    "Schema-17 structural, current, or operation/audit health validation failed.", ex);
            }
            var replay = await mutationWriter.FindCommittedAsync(db, intent, token);
            if (replay is not null)
            {
                await transaction.RollbackAsync(token);
                return ReplayResult(replay);
            }

            TrustedMovementPlanningSnapshot snapshot;
            try
            {
                snapshot = await mutationWriter.MaterializeAsync(db, command.LogicalMovementBatchId, token);
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("NotFound", StringComparison.Ordinal))
            {
                throw new LogicalMovementMutationException(LogicalMovementMutationFailure.NotFound,
                    "The logical movement root was not found.", ex);
            }
            catch (InvalidOperationException ex) when (ex.Message == "MOVEMENT_MUTATION_ROOT_READ_ONLY")
            {
                throw new LogicalMovementMutationException(LogicalMovementMutationFailure.ReadOnly,
                    "The logical movement root is read-only.", ex);
            }
            catch (InvalidOperationException ex)
            {
                throw new LogicalMovementMutationException(LogicalMovementMutationFailure.Unhealthy,
                    "The logical movement root is not healthy enough to mutate.", ex);
            }

            if (snapshot.Root.CurrentGenerationNumber != command.ExpectedGeneration)
                throw new LogicalMovementMutationException(LogicalMovementMutationFailure.StaleGeneration,
                    "The logical movement root changed; reload and preview the current generation.");

            var plan = MovementMutationPlanner.Plan(snapshot, command.Mutation, clock.Today);
            if (plan.Kind == MovementMutationPlanKind.NoOp)
            {
                await transaction.RollbackAsync(token);
                return new(LogicalMovementMutationResultKind.NoChange, snapshot.Root.Id,
                    snapshot.Root.CurrentGenerationNumber, null, null);
            }

            PendingMovementMutation pending;
            try
            {
                pending = await mutationWriter.PersistAsync(db, intent, snapshot, plan,
                    actorUserId, session.Username, clock.UtcNow, token);
            }
            catch (MovementMutationWriteConflictException ex)
            {
                await transaction.RollbackAsync(token);
                return await ClassifyFreshAsync(intent, token, retryHealthyExpected: true,
                    healthyExpectedFailure: ex.Kind == MovementMutationWriteConflictKind.TransientContention
                        ? LogicalMovementMutationFailure.PersistenceFailure
                        : LogicalMovementMutationFailure.IntegrityFailure);
            }

            var audit = transactionAuditAppender.AppendPrimary(db,
                LogicalMutationAudit(command.Mutation, snapshot, plan, pending));
            await db.SaveChangesAsync(token);
            await mutationWriter.AssociatePrimaryAuditAsync(db, pending, audit.Id, token);
            await mutationWriter.ValidateOperationAuditHealthAsync(db, pending.RootId, token);

            bool published;
            try
            {
                published = await mutationWriter.TryPublishAsync(
                    db, pending, command.ExpectedGeneration, token);
            }
            catch (MovementMutationWriteConflictException ex)
            {
                await transaction.RollbackAsync(token);
                return await ClassifyFreshAsync(intent, token, retryHealthyExpected: true,
                    healthyExpectedFailure: ex.Kind == MovementMutationWriteConflictKind.TransientContention
                        ? LogicalMovementMutationFailure.PersistenceFailure
                        : LogicalMovementMutationFailure.IntegrityFailure);
            }
            if (!published)
            {
                await transaction.RollbackAsync(token);
                return await ClassifyFreshAsync(intent, token);
            }

            await mutationWriter.ValidatePublishedAsync(db, pending, token);
            await transaction.CommitAsync(token);
            return new(LogicalMovementMutationResultKind.Committed, pending.RootId,
                pending.ResultGeneration, pending.OperationId, pending.PhysicalOutputBatchId);
        }
        catch (LogicalMovementMutationException)
        {
            await RollbackIfActiveAsync(transaction);
            throw;
        }
        catch (InvalidOperationException ex) when (ex.Message == "MOVEMENT_MUTATION_OPERATION_ID_CONFLICT")
        {
            await RollbackIfActiveAsync(transaction);
            throw new LogicalMovementMutationException(LogicalMovementMutationFailure.OperationIdConflict,
                "This client operation ID was already used for a different logical movement request.", ex);
        }
        catch
        {
            await RollbackIfActiveAsync(transaction);
            throw;
        }
    }

    private async Task<LogicalMovementMutationResult> ClassifyFreshAsync(
        MovementMutationOperationIntent intent, CancellationToken token,
        bool retryHealthyExpected = false,
        LogicalMovementMutationFailure healthyExpectedFailure = LogicalMovementMutationFailure.IntegrityFailure)
    {
        for (var attempt = 0; ; attempt++)
        {
            await using var verify = await factory.CreateDbContextAsync(token);
            await using var verifyTransaction = await verify.Database.BeginTransactionAsync(token);
            MovementMutationFreshState state;
            try
            {
                state = await mutationWriter.ClassifyFreshAsync(verify, intent, token);
                await verifyTransaction.RollbackAsync(token);
            }
            catch
            {
                await RollbackIfActiveAsync(verifyTransaction);
                throw;
            }
            if (retryHealthyExpected && state.Kind == MovementMutationFreshStateKind.IntegrityFailure && attempt < 4)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(25 * (attempt + 1)), token);
                continue;
            }
            return FreshResult(state, healthyExpectedFailure);
        }
    }

    private static LogicalMovementMutationResult FreshResult(MovementMutationFreshState state,
        LogicalMovementMutationFailure healthyExpectedFailure = LogicalMovementMutationFailure.IntegrityFailure) =>
        state.Kind switch
        {
            MovementMutationFreshStateKind.Replay when state.Replay is not null => ReplayResult(state.Replay),
            MovementMutationFreshStateKind.OperationIdConflict => throw new LogicalMovementMutationException(
                LogicalMovementMutationFailure.OperationIdConflict,
                "This client operation ID was already used for a different logical movement request."),
            MovementMutationFreshStateKind.StaleGeneration => throw new LogicalMovementMutationException(
                LogicalMovementMutationFailure.StaleGeneration,
                "The logical movement root changed; reload and preview the current generation."),
            MovementMutationFreshStateKind.NotFound => throw new LogicalMovementMutationException(
                LogicalMovementMutationFailure.NotFound, "The logical movement root was not found."),
            MovementMutationFreshStateKind.Unhealthy => throw new LogicalMovementMutationException(
                LogicalMovementMutationFailure.Unhealthy,
                "The logical movement root or its operation/audit evidence is unhealthy."),
            _ => throw new LogicalMovementMutationException(healthyExpectedFailure,
                healthyExpectedFailure == LogicalMovementMutationFailure.PersistenceFailure
                    ? "The mutation could not complete because database contention did not resolve."
                    : "The mutation could not publish although the expected healthy generation remains current.")
        };

    private AuditEvent LogicalMutationAudit(MovementMutationRequest request,
        TrustedMovementPlanningSnapshot snapshot, MovementMutationPlan plan,
        PendingMovementMutation pending)
    {
        var action = request.Kind switch
        {
            MovementMutationKind.Correct when request.Scope == MovementMutationScope.WholeRoot => "MOVEMENT_BATCH_CORRECTED",
            MovementMutationKind.Correct => "MOVEMENT_CORRECTED",
            MovementMutationKind.Reverse => "MOVEMENT_REVERSED",
            MovementMutationKind.Restore => "MOVEMENT_RESTORED",
            _ => throw new InvalidOperationException("Unsupported logical mutation audit action.")
        };
        return Audit(action, "LogicalMovementBatch", pending.RootId.Value.ToString(CultureInfo.InvariantCulture),
            $"Logical movement root #{pending.RootId.Value} advanced to generation {pending.ResultGeneration.Value}. Reason: {request.Reason}",
            new
            {
                CurrentGeneration = snapshot.Root.CurrentGenerationNumber.Value,
                Lines = snapshot.Lines.Select(x => new
                {
                    LineId = x.Current.Id.Value,
                    GenerationLineId = x.Current.CurrentGenerationLineId.Value,
                    x.Current.State,
                    EffectiveMovementId = x.Current.EffectiveMovementId,
                    x.Current.TerminalReversalMovementId,
                    LastEffective = AuditBusinessState(x.LastEffective),
                    TerminalReversal = x.TerminalReversal is null
                        ? null
                        : AuditBusinessState(x.TerminalReversal)
                }).ToArray()
            },
            new
            {
                ResultGeneration = pending.ResultGeneration.Value,
                pending.PhysicalOutputBatchId,
                Lines = pending.Lines.Select(x => new
                {
                    LineId = x.LineId.Value,
                    x.Action,
                    x.State,
                    x.AppliedFieldMask,
                    x.EffectiveMovementId,
                    x.TerminalReversalMovementId,
                    LastEffective = ResultAuditBusinessState(
                        x.LineId, x.EffectiveMovementId, snapshot, plan, pending),
                    TerminalReversal = x.TerminalReversalMovementId is null
                        ? null
                        : ResultAuditBusinessState(x.LineId, x.TerminalReversalMovementId.Value,
                            snapshot, plan, pending),
                    NewMovements = pending.Movements.Where(m => m.LineId == x.LineId)
                        .Select(m => new { m.MovementId, m.Purpose }).ToArray()
                }).ToArray()
            });
    }

    private static object AuditBusinessState(MovementBusinessState state) => AuditBusinessState(
        state.MovementId, state.MovementDate, state.Direction, state.Source, state.CustomerId,
        state.ContainerTypeId, state.Quantity, state.Reference, state.Notes, state.MovementBatchId,
        state.ImportRunId, state.ReversesMovementId);

    private static object ResultAuditBusinessState(LogicalMovementLineId lineId, long movementId,
        TrustedMovementPlanningSnapshot snapshot, MovementMutationPlan plan,
        PendingMovementMutation pending)
    {
        var current = snapshot.Lines.Single(x => x.Current.Id == lineId);
        if (current.LastEffective.MovementId == movementId)
            return AuditBusinessState(current.LastEffective);
        if (current.TerminalReversal?.MovementId == movementId)
            return AuditBusinessState(current.TerminalReversal);

        var persisted = pending.Movements.Single(x => x.LineId == lineId && x.MovementId == movementId);
        var specification = plan.Lines.Single(x => x.LineId == lineId).Movements
            .Single(x => x.Purpose == persisted.Purpose);
        var outputMember = plan.PhysicalOutput?.Members.Any(
            x => x.LineId == lineId && x.Purpose == persisted.Purpose) == true;
        return AuditBusinessState(
            persisted.MovementId, specification.MovementDate, specification.Direction,
            specification.Source, specification.CustomerId, specification.ContainerTypeId,
            specification.Quantity, specification.Reference, specification.Notes,
            outputMember ? pending.PhysicalOutputBatchId : null, null,
            specification.ReversesMovementId);
    }

    private static object AuditBusinessState(long movementId, DateOnly movementDate,
        MovementType direction, MovementSource source, int customerId, int containerTypeId,
        int quantity, string? reference, string? notes, int? movementBatchId,
        long? importRunId, long? reversesMovementId) => new
        {
            MovementId = movementId,
            MovementDate = movementDate,
            Direction = direction,
            Source = source,
            CustomerId = customerId,
            ContainerTypeId = containerTypeId,
            Quantity = quantity,
            Reference = reference,
            Notes = notes,
            MovementBatchId = movementBatchId,
            ImportRunId = importRunId,
            ReversesMovementId = reversesMovementId
        };

    private static LogicalMovementMutationResult ReplayResult(MovementMutationReplay replay) =>
        new(LogicalMovementMutationResultKind.Replayed, replay.RootId, replay.ResultGeneration,
            replay.OperationId, replay.PhysicalOutputBatchId);

    private static MovementCorrectionKind OperationKind(MovementMutationRequest request) => request.Kind switch
    {
        MovementMutationKind.Correct when request.Scope == MovementMutationScope.Individual => MovementCorrectionKind.Single,
        MovementMutationKind.Correct => MovementCorrectionKind.WholeBatch,
        MovementMutationKind.Reverse => MovementCorrectionKind.Reverse,
        MovementMutationKind.Restore => MovementCorrectionKind.Restore,
        _ => throw new ArgumentOutOfRangeException(nameof(request))
    };

    private static LogicalMovementGenerationAction GenerationKind(MovementMutationKind kind) => kind switch
    {
        MovementMutationKind.Correct => LogicalMovementGenerationAction.Corrected,
        MovementMutationKind.Reverse => LogicalMovementGenerationAction.Reversed,
        MovementMutationKind.Restore => LogicalMovementGenerationAction.Restored,
        _ => throw new ArgumentOutOfRangeException(nameof(kind))
    };

    private static async Task RollbackIfActiveAsync(Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction transaction)
    {
        try { await transaction.RollbackAsync(CancellationToken.None); }
        catch (InvalidOperationException) { }
    }

    private static string CanonicalLogicalRequest(LogicalMovementMutationCommand command)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = false }))
        {
            writer.WriteStartObject();
            writer.WriteNumber("schemaVersion", 1);
            writer.WriteNumber("logicalMovementBatchId", command.LogicalMovementBatchId.Value);
            writer.WriteNumber("expectedGeneration", command.ExpectedGeneration.Value);
            writer.WriteNumber("mutationKind", (int)command.Mutation.Kind);
            writer.WriteNumber("scope", (int)command.Mutation.Scope);
            writer.WritePropertyName("targetLineIds");
            writer.WriteStartArray();
            foreach (var id in command.Mutation.TargetLineIds.OrderBy(x => x.Value))
                writer.WriteNumberValue(id.Value);
            writer.WriteEndArray();
            writer.WriteString("reason", command.Mutation.Reason);
            writer.WritePropertyName("fields");
            WriteFields(writer, command.Mutation.MovementDate, command.Mutation.Direction,
                command.Mutation.Customer, command.Mutation.ContainerType, command.Mutation.Quantity,
                command.Mutation.Reference, command.Mutation.Notes);
            writer.WritePropertyName("reversedLineDecisions");
            writer.WriteStartArray();
            foreach (var decision in command.Mutation.ReversedLineDecisions.Values.OrderBy(x => x.LineId.Value))
            {
                writer.WriteStartObject();
                writer.WriteNumber("logicalMovementLineId", decision.LineId.Value);
                writer.WriteNumber("disposition", (int)decision.Disposition);
                writer.WritePropertyName("fields");
                WriteFields(writer, decision.MovementDate, decision.Direction, decision.Customer,
                    decision.ContainerType, decision.Quantity, decision.Reference, decision.Notes);
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
            writer.WriteEndObject();
        }
        return Encoding.UTF8.GetString(stream.ToArray());
    }

    private static void WriteFields(Utf8JsonWriter writer,
        MovementFieldIntent<DateOnly> date, MovementFieldIntent<MovementType> direction,
        MovementFieldIntent<int> customer, MovementFieldIntent<int> container,
        MovementFieldIntent<int> quantity, MovementFieldIntent<string> reference,
        MovementFieldIntent<string> notes)
    {
        writer.WriteStartObject();
        WriteIntent(writer, "movementDate", date, x => x.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
        WriteIntent(writer, "direction", direction, x => (int)x);
        WriteIntent(writer, "customerId", customer, x => x);
        WriteIntent(writer, "containerTypeId", container, x => x);
        WriteIntent(writer, "quantity", quantity, x => x);
        WriteTextIntent(writer, "reference", reference);
        WriteTextIntent(writer, "notes", notes);
        writer.WriteEndObject();
    }

    private static void WriteIntent<T, TValue>(Utf8JsonWriter writer, string name,
        MovementFieldIntent<T> intent, Func<T, TValue> value) where T : struct
    {
        writer.WritePropertyName(name);
        writer.WriteStartObject();
        if (!intent.IsSelected)
            writer.WriteString("selection", "unselected");
        else
        {
            writer.WriteString("selection", "value");
            writer.WritePropertyName("value");
            JsonSerializer.Serialize(writer, value(intent.Value));
        }
        writer.WriteEndObject();
    }

    private static void WriteTextIntent(Utf8JsonWriter writer, string name,
        MovementFieldIntent<string> intent)
    {
        writer.WritePropertyName(name);
        writer.WriteStartObject();
        if (!intent.IsSelected)
            writer.WriteString("selection", "unselected");
        else if (intent.Value is null)
            writer.WriteString("selection", "clear");
        else
        {
            writer.WriteString("selection", "value");
            writer.WriteString("value", intent.Value);
        }
        writer.WriteEndObject();
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
    private static string HashUtf8(string value) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
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
