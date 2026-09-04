namespace BinTracker.Core;

// These persistence contracts are deliberately not registered in the current
// production DbContext. Schema 17 remains an explicitly invoked migration
// target until the later coherent runtime activation slice.
public sealed class LogicalMovementBatch
{
    public long Id { get; set; }
    public int? RootMovementBatchId { get; set; }
    public LogicalMovementBatchStatus Status { get; set; }
    public int? CurrentGenerationNumber { get; set; }
    public int LineCount { get; set; }
    public string? StatusReasonCode { get; set; }
    public DateTime CreatedUtc { get; set; }
}

public sealed class LogicalMovementLine
{
    public long Id { get; set; }
    public long LogicalMovementBatchId { get; set; }
    public long RootMovementId { get; set; }
    public int OriginalDisplayOrdinal { get; set; }
    public DateTime CreatedUtc { get; set; }
}

public sealed class LogicalMovementGeneration
{
    public long Id { get; set; }
    public long LogicalMovementBatchId { get; set; }
    public int GenerationNumber { get; set; }
    public int? PreviousGenerationNumber { get; set; }
    public long? MovementCorrectionOperationId { get; set; }
    public LogicalMovementGenerationAction Kind { get; set; }
    public int LineCount { get; set; }
    public DateTime CreatedUtc { get; set; }
}

public sealed class LogicalMovementGenerationLine
{
    public long Id { get; set; }
    public long LogicalMovementBatchId { get; set; }
    public long LogicalMovementGenerationId { get; set; }
    public long LogicalMovementLineId { get; set; }
    public LogicalMovementLineState State { get; set; }
    public LogicalMovementGenerationAction Action { get; set; }
    public MovementChangeField AppliedFieldMask { get; set; }
    public long? PreviousGenerationLineId { get; set; }
    public long? ResultEffectiveMovementId { get; set; }
    public long? LastEffectiveMovementId { get; set; }
    public long? TerminalReversalMovementId { get; set; }
    public DateTime CreatedUtc { get; set; }
}

public sealed class LogicalMovementLedgerLink
{
    public long BinMovementId { get; set; }
    public long LogicalMovementBatchId { get; set; }
    public long LogicalMovementLineId { get; set; }
    public LogicalMovementTransformationRole Role { get; set; }
    public long? IntroducedByGenerationLineId { get; set; }
    public long? LegacyMovementCorrectionLineId { get; set; }
    public DateTime CreatedUtc { get; set; }
}

public sealed class LogicalMovementPhysicalOutput
{
    public int MovementBatchId { get; set; }
    public long LogicalMovementBatchId { get; set; }
    public long? LogicalMovementGenerationId { get; set; }
    public long? LegacyMovementCorrectionOperationId { get; set; }
    public DateTime CreatedUtc { get; set; }
}

/// <summary>
/// Validates the materialized, transaction-local construction state for one
/// native generation-zero root before that root may become current.
/// Committed-current projection remains the responsibility of
/// <see cref="LogicalMovementCurrentRootValidator"/>.
/// </summary>
internal static class LogicalMovementInitialConstructionValidator
{
    private const string InvalidConstruction = "INITIAL_MOVEMENT_LINEAGE_CONSTRUCTION_INVALID";

    internal static void Validate(
        LogicalMovementBatch root,
        IReadOnlyList<LogicalMovementLine> lines,
        IReadOnlyList<LogicalMovementGeneration> generations,
        IReadOnlyList<LogicalMovementGenerationLine> generationLines,
        IReadOnlyList<LogicalMovementLedgerLink> ledgerLinks,
        IReadOnlyList<long> expectedOrderedMovementIds,
        IReadOnlyDictionary<long, int?> physicalMovementBatchIds,
        IReadOnlySet<long> rootBatchMovementIds,
        int correctionOperationCount,
        int physicalOutputCount)
    {
        ArgumentNullException.ThrowIfNull(root);
        ArgumentNullException.ThrowIfNull(lines);
        ArgumentNullException.ThrowIfNull(generations);
        ArgumentNullException.ThrowIfNull(generationLines);
        ArgumentNullException.ThrowIfNull(ledgerLinks);
        ArgumentNullException.ThrowIfNull(expectedOrderedMovementIds);
        ArgumentNullException.ThrowIfNull(physicalMovementBatchIds);
        ArgumentNullException.ThrowIfNull(rootBatchMovementIds);

        if (root.Id <= 0 || root.Status != LogicalMovementBatchStatus.Initializing ||
            root.CurrentGenerationNumber is not null || root.StatusReasonCode is not null ||
            root.LineCount <= 0 || root.LineCount != expectedOrderedMovementIds.Count ||
            expectedOrderedMovementIds.Any(x => x <= 0) ||
            expectedOrderedMovementIds.Distinct().Count() != expectedOrderedMovementIds.Count ||
            correctionOperationCount != 0 || physicalOutputCount != 0)
        {
            Fail();
        }

        if (lines.Count != root.LineCount ||
            lines.Any(x => x.Id <= 0 || x.LogicalMovementBatchId != root.Id) ||
            lines.Select(x => x.Id).Distinct().Count() != lines.Count ||
            lines.Select(x => x.RootMovementId).Distinct().Count() != lines.Count ||
            lines.Select(x => x.OriginalDisplayOrdinal).Order().SequenceEqual(
                Enumerable.Range(0, root.LineCount)) is false)
        {
            Fail();
        }

        var orderedLines = lines.OrderBy(x => x.OriginalDisplayOrdinal).ToArray();
        if (!orderedLines.Select(x => x.RootMovementId).SequenceEqual(expectedOrderedMovementIds))
            Fail();

        if (generations.Count != 1)
            Fail();
        var generation = generations[0];
        if (generation.Id <= 0 || generation.LogicalMovementBatchId != root.Id ||
            generation.GenerationNumber != 0 || generation.PreviousGenerationNumber is not null ||
            generation.MovementCorrectionOperationId is not null ||
            generation.Kind != LogicalMovementGenerationAction.Initial ||
            generation.LineCount != root.LineCount)
        {
            Fail();
        }

        var lineIds = lines.Select(x => x.Id).ToHashSet();
        if (generationLines.Count != root.LineCount ||
            generationLines.Any(x => x.Id <= 0 || x.LogicalMovementBatchId != root.Id ||
                x.LogicalMovementGenerationId != generation.Id || !lineIds.Contains(x.LogicalMovementLineId)) ||
            generationLines.Select(x => x.Id).Distinct().Count() != generationLines.Count ||
            generationLines.Select(x => x.LogicalMovementLineId).Distinct().Count() != generationLines.Count)
        {
            Fail();
        }

        foreach (var line in lines)
        {
            var state = generationLines.Single(x => x.LogicalMovementLineId == line.Id);
            if (state.State != LogicalMovementLineState.Active ||
                state.Action != LogicalMovementGenerationAction.Initial ||
                state.AppliedFieldMask != MovementChangeField.None ||
                state.PreviousGenerationLineId is not null ||
                state.ResultEffectiveMovementId != line.RootMovementId ||
                state.LastEffectiveMovementId is not null ||
                state.TerminalReversalMovementId is not null)
            {
                Fail();
            }
        }

        if (ledgerLinks.Count != root.LineCount ||
            ledgerLinks.Any(x => x.LogicalMovementBatchId != root.Id || !lineIds.Contains(x.LogicalMovementLineId) ||
                x.Role != LogicalMovementTransformationRole.RootOriginal ||
                x.IntroducedByGenerationLineId is null || x.LegacyMovementCorrectionLineId is not null) ||
            ledgerLinks.Select(x => x.BinMovementId).Distinct().Count() != ledgerLinks.Count ||
            !ledgerLinks.Select(x => x.BinMovementId).ToHashSet().SetEquals(expectedOrderedMovementIds))
        {
            Fail();
        }

        foreach (var line in lines)
        {
            var state = generationLines.Single(x => x.LogicalMovementLineId == line.Id);
            if (ledgerLinks.Count(x => x.BinMovementId == line.RootMovementId &&
                    x.LogicalMovementLineId == line.Id &&
                    x.IntroducedByGenerationLineId == state.Id) != 1)
            {
                Fail();
            }
        }

        if (physicalMovementBatchIds.Count != expectedOrderedMovementIds.Count ||
            !physicalMovementBatchIds.Keys.ToHashSet().SetEquals(expectedOrderedMovementIds))
        {
            Fail();
        }

        if (root.RootMovementBatchId is { } rootBatchId)
        {
            if (rootBatchId <= 0 || physicalMovementBatchIds.Values.Any(x => x != rootBatchId) ||
                !rootBatchMovementIds.SetEquals(expectedOrderedMovementIds))
            {
                Fail();
            }
        }
        else if (expectedOrderedMovementIds.Count != 1 ||
                 physicalMovementBatchIds.Values.Single() is not null ||
                 rootBatchMovementIds.Count != 0)
        {
            Fail();
        }
    }

    [System.Diagnostics.CodeAnalysis.DoesNotReturn]
    private static void Fail() => throw new InvalidOperationException(InvalidConstruction);
}

/// <summary>
/// Provider-neutral immutable identity and canonical-intent envelope supplied
/// by the business service to a provider persistence implementation.
/// </summary>
public sealed record MovementMutationOperationIntent(
    Guid ClientOperationId,
    LogicalMovementBatchId RootId,
    LogicalMovementGenerationNumber ExpectedGeneration,
    MovementCorrectionKind OperationKind,
    LogicalMovementGenerationAction GenerationKind,
    int RequestSchemaVersion,
    string RequestJson,
    string RequestFingerprint);

internal sealed record PersistedMovementMutationOperation(
    long Id, Guid ClientOperationId, string RequestFingerprint, MovementCorrectionKind Kind,
    int? OriginalBatchId, int? ReplacementBatchId, string Reason,
    int ActorUserId, string ActorUsername, DateTime CreatedUtc,
    string? RequestJson, int? RequestSchemaVersion, long? RootId,
    int? ExpectedGenerationNumber, int? ResultGenerationNumber);

internal sealed record PersistedMovementMutationGeneration(
    long Id, long RootId, int GenerationNumber, int? PreviousGenerationNumber,
    long? OperationId, LogicalMovementGenerationAction Kind, int LineCount);

internal sealed record PersistedMovementMutationLine(
    long Id, long RootId, long GenerationId, long LineId, LogicalMovementLineState State,
    LogicalMovementGenerationAction Action, MovementChangeField AppliedFieldMask,
    long? PreviousGenerationLineId, long? ResultEffectiveMovementId,
    long? LastEffectiveMovementId, long? TerminalReversalMovementId);

internal sealed record PersistedPlannedMovement(
    long Id, LogicalMovementLineId LineId, PlannedMovementPurpose Purpose,
    DateOnly MovementDate, MovementType Direction, MovementSource Source,
    int CustomerId, int ContainerTypeId, int Quantity, string? Reference,
    string? Notes, string? Reason, long? ReversesMovementId, int? MovementBatchId,
    long? ImportRunId, Guid? ClientOperationId);

internal sealed record PersistedMovementMutationLedgerLink(
    long MovementId, long RootId, long LineId, LogicalMovementTransformationRole Role,
    long? IntroducedByGenerationLineId, long? LegacyMovementCorrectionLineId);

internal sealed record PersistedMovementMutationPhysicalOutput(
    int MovementBatchId, long RootId, long? GenerationId, long? LegacyOperationId,
    DateOnly MovementDate, MovementType Direction, MovementSource Source,
    IReadOnlySet<long> MemberMovementIds);

internal sealed record LogicalMovementMutationConstruction(
    PersistedMovementMutationOperation Operation,
    PersistedMovementMutationGeneration Generation,
    IReadOnlyList<PersistedMovementMutationLine> Lines,
    IReadOnlyList<PersistedPlannedMovement> Movements,
    IReadOnlyList<PersistedMovementMutationLedgerLink> NewLedgerLinks,
    PersistedMovementMutationPhysicalOutput? PhysicalOutput);

internal static class LogicalMovementMutationConstructionValidator
{
    private const string InvalidConstruction = "MOVEMENT_MUTATION_CONSTRUCTION_INVALID";

    internal static void Validate(
        MovementMutationOperationIntent intent,
        TrustedMovementPlanningSnapshot snapshot,
        MovementMutationPlan plan,
        LogicalMovementMutationConstruction construction)
    {
        ArgumentNullException.ThrowIfNull(intent);
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(construction);

        if (intent.ClientOperationId == Guid.Empty || intent.RootId != snapshot.Root.Id ||
            intent.RootId != plan.RootId || intent.ExpectedGeneration != snapshot.Root.CurrentGenerationNumber ||
            intent.ExpectedGeneration != plan.PlannedFromGeneration || intent.RequestSchemaVersion != 1 ||
            string.IsNullOrEmpty(intent.RequestJson) || intent.RequestFingerprint.Length != 64 ||
            plan.Kind != MovementMutationPlanKind.Substantive)
        {
            Fail();
        }

        var operation = construction.Operation;
        var resultGeneration = checked(intent.ExpectedGeneration.Value + 1);
        if (operation.Id <= 0 || operation.ClientOperationId != intent.ClientOperationId ||
            operation.RequestFingerprint != intent.RequestFingerprint || operation.Kind != intent.OperationKind ||
            operation.OriginalBatchId is not null || operation.ReplacementBatchId is not null ||
            operation.RequestJson != intent.RequestJson || operation.RequestSchemaVersion != intent.RequestSchemaVersion ||
            operation.RootId != intent.RootId.Value || operation.ExpectedGenerationNumber != intent.ExpectedGeneration.Value ||
            operation.ResultGenerationNumber != resultGeneration || operation.Reason != plan.Lines.SelectMany(x => x.Movements)
                .Select(x => x.Reason).DefaultIfEmpty(operation.Reason).First())
        {
            Fail();
        }

        var generation = construction.Generation;
        if (generation.Id <= 0 || generation.RootId != intent.RootId.Value ||
            generation.GenerationNumber != resultGeneration ||
            generation.PreviousGenerationNumber != intent.ExpectedGeneration.Value ||
            generation.OperationId != operation.Id || generation.Kind != intent.GenerationKind ||
            generation.LineCount != snapshot.Root.Lines.Count)
        {
            Fail();
        }

        var plannedByLine = plan.Lines.ToDictionary(x => x.LineId);
        var currentByLine = snapshot.Lines.ToDictionary(x => x.Current.Id);
        if (construction.Lines.Count != snapshot.Root.Lines.Count ||
            construction.Lines.Select(x => x.LineId).Distinct().Count() != construction.Lines.Count ||
            !construction.Lines.Select(x => new LogicalMovementLineId(x.LineId)).ToHashSet()
                .SetEquals(plannedByLine.Keys))
        {
            Fail();
        }

        var movements = construction.Movements.ToDictionary(x => (x.LineId, x.Purpose));
        var plannedSpecs = plan.Lines.SelectMany(x => x.Movements).ToArray();
        if (movements.Count != plannedSpecs.Length ||
            !movements.Keys.ToHashSet().SetEquals(plannedSpecs.Select(x => (x.LineId, x.Purpose))))
        {
            Fail();
        }

        foreach (var spec in plannedSpecs)
        {
            var movement = movements[(spec.LineId, spec.Purpose)];
            if (movement.Id <= 0 || movement.MovementDate != spec.MovementDate ||
                movement.Direction != spec.Direction || movement.Source != spec.Source ||
                movement.CustomerId != spec.CustomerId || movement.ContainerTypeId != spec.ContainerTypeId ||
                movement.Quantity != spec.Quantity || movement.Reference != spec.Reference ||
                movement.Notes != spec.Notes || movement.Reason != spec.Reason ||
                movement.ReversesMovementId != spec.ReversesMovementId || movement.ImportRunId is not null ||
                movement.ClientOperationId is not null)
            {
                Fail();
            }
        }

        foreach (var line in construction.Lines)
        {
            if (line.Id <= 0 || line.RootId != intent.RootId.Value || line.GenerationId != generation.Id)
                Fail();
            var lineId = new LogicalMovementLineId(line.LineId);
            var planned = plannedByLine[lineId];
            var current = currentByLine[lineId].Current;
            if (line.State != planned.State || line.Action != planned.Action ||
                line.AppliedFieldMask != planned.AppliedFieldMask ||
                line.PreviousGenerationLineId != current.CurrentGenerationLineId.Value)
            {
                Fail();
            }

            var effective = Resolve(planned.EffectiveMovement, lineId, movements);
            var terminal = planned.TerminalReversalMovement is null
                ? (long?)null
                : Resolve(planned.TerminalReversalMovement, lineId, movements);
            if (line.State == LogicalMovementLineState.Active)
            {
                if (line.ResultEffectiveMovementId != effective || line.LastEffectiveMovementId is not null ||
                    line.TerminalReversalMovementId is not null)
                    Fail();
            }
            else if (line.ResultEffectiveMovementId is not null || line.LastEffectiveMovementId != effective ||
                     line.TerminalReversalMovementId != terminal)
            {
                Fail();
            }
        }

        if (construction.NewLedgerLinks.Count != movements.Count ||
            !construction.NewLedgerLinks.Select(x => x.MovementId).ToHashSet()
                .SetEquals(movements.Values.Select(x => x.Id)))
        {
            Fail();
        }
        foreach (var movement in movements.Values)
        {
            var line = construction.Lines.Single(x => x.LineId == movement.LineId.Value);
            var expectedRole = movement.Purpose switch
            {
                PlannedMovementPurpose.CorrectionNeutraliser => LogicalMovementTransformationRole.CorrectionNeutraliser,
                PlannedMovementPurpose.CorrectionReplacement => LogicalMovementTransformationRole.CorrectionReplacement,
                PlannedMovementPurpose.OrdinaryReversal => LogicalMovementTransformationRole.OrdinaryReversal,
                PlannedMovementPurpose.Restoration => LogicalMovementTransformationRole.Restoration,
                _ => throw new InvalidOperationException(InvalidConstruction)
            };
            if (construction.NewLedgerLinks.Count(x => x.MovementId == movement.Id &&
                    x.RootId == intent.RootId.Value && x.LineId == movement.LineId.Value &&
                    x.Role == expectedRole && x.IntroducedByGenerationLineId == line.Id &&
                    x.LegacyMovementCorrectionLineId is null) != 1)
            {
                Fail();
            }
        }

        ValidateOutput(plan, construction, movements);
    }

    private static long Resolve(PlannedMovementReference reference, LogicalMovementLineId lineId,
        IReadOnlyDictionary<(LogicalMovementLineId, PlannedMovementPurpose), PersistedPlannedMovement> movements) =>
        reference.ExistingMovementId ?? movements[(lineId,
            reference.PlannedPurpose ?? throw new InvalidOperationException(InvalidConstruction))].Id;

    private static void ValidateOutput(MovementMutationPlan plan,
        LogicalMovementMutationConstruction construction,
        IReadOnlyDictionary<(LogicalMovementLineId, PlannedMovementPurpose), PersistedPlannedMovement> movements)
    {
        if (plan.PhysicalOutput is null)
        {
            if (construction.PhysicalOutput is not null || movements.Values.Any(x => x.MovementBatchId is not null))
                Fail();
            return;
        }

        var output = construction.PhysicalOutput;
        if (output is null || output.MovementBatchId <= 0 || output.RootId != plan.RootId.Value ||
            output.GenerationId != construction.Generation.Id || output.LegacyOperationId is not null ||
            output.MovementDate != plan.PhysicalOutput.MovementDate ||
            output.Direction != plan.PhysicalOutput.Direction || output.Source != plan.PhysicalOutput.Source)
        {
            Fail();
        }
        var expectedMembers = plan.PhysicalOutput.Members
            .Select(x => movements[(x.LineId, x.Purpose)].Id).ToHashSet();
        if (!output.MemberMovementIds.SetEquals(expectedMembers) ||
            movements.Values.Any(x => expectedMembers.Contains(x.Id)
                ? x.MovementBatchId != output.MovementBatchId
                : x.MovementBatchId is not null))
        {
            Fail();
        }
    }

    [System.Diagnostics.CodeAnalysis.DoesNotReturn]
    private static void Fail() => throw new InvalidOperationException(InvalidConstruction);
}

internal sealed record NativeMovementGenerationAuditFact(
    long GenerationId, long RootId, int GenerationNumber, int? PreviousGenerationNumber,
    long? OperationId, LogicalMovementGenerationAction Kind);

internal sealed record NativeMovementOperationAuditFact(
    long OperationId, long? RootId, int? ExpectedGenerationNumber, int? ResultGenerationNumber,
    int? RequestSchemaVersion, MovementCorrectionKind Kind);

internal sealed record PrimaryMovementAuditFact(long AuditEventId, long OperationId,
    string Action, string EntityType, string? EntityId, bool Succeeded);

internal static class LogicalMovementOperationAuditHealthValidator
{
    internal static void Validate(
        IReadOnlyList<NativeMovementGenerationAuditFact> generations,
        IReadOnlyList<NativeMovementOperationAuditFact> operations,
        IReadOnlyList<PrimaryMovementAuditFact> audits)
    {
        ArgumentNullException.ThrowIfNull(generations);
        ArgumentNullException.ThrowIfNull(operations);
        ArgumentNullException.ThrowIfNull(audits);
        if (operations.Select(x => x.OperationId).Distinct().Count() != operations.Count ||
            audits.Select(x => x.AuditEventId).Distinct().Count() != audits.Count)
            Fail();

        foreach (var generation in generations.Where(x => x.GenerationNumber > 0))
        {
            if (generation.PreviousGenerationNumber != generation.GenerationNumber - 1 ||
                generation.OperationId is null)
                Fail();
            var matches = operations.Where(x => x.OperationId == generation.OperationId).ToArray();
            if (matches.Length != 1)
                Fail();
            var operation = matches[0];
            if (operation.RequestSchemaVersion != 1 || operation.RootId != generation.RootId ||
                operation.ExpectedGenerationNumber != generation.PreviousGenerationNumber ||
                operation.ResultGenerationNumber != generation.GenerationNumber ||
                GenerationKind(operation.Kind) != generation.Kind ||
                audits.Count(x => x.OperationId == operation.OperationId) != 1)
                Fail();
            var audit = audits.Single(x => x.OperationId == operation.OperationId);
            if (!audit.Succeeded || audit.Action != AuditAction(operation.Kind) ||
                audit.EntityType != "LogicalMovementBatch" ||
                audit.EntityId != operation.RootId.Value.ToString(System.Globalization.CultureInfo.InvariantCulture))
                Fail();
        }

        foreach (var operation in operations.Where(x => x.RequestSchemaVersion is not null))
        {
            if (generations.Count(x => x.GenerationNumber > 0 && x.OperationId == operation.OperationId) != 1 ||
                audits.Count(x => x.OperationId == operation.OperationId) != 1)
                Fail();
        }
    }

    private static LogicalMovementGenerationAction GenerationKind(MovementCorrectionKind kind) => kind switch
    {
        MovementCorrectionKind.Single or MovementCorrectionKind.WholeBatch => LogicalMovementGenerationAction.Corrected,
        MovementCorrectionKind.Reverse => LogicalMovementGenerationAction.Reversed,
        MovementCorrectionKind.Restore => LogicalMovementGenerationAction.Restored,
        _ => throw new InvalidOperationException("MOVEMENT_MUTATION_AUDIT_HEALTH_INVALID")
    };

    private static string AuditAction(MovementCorrectionKind kind) => kind switch
    {
        MovementCorrectionKind.Single => "MOVEMENT_CORRECTED",
        MovementCorrectionKind.WholeBatch => "MOVEMENT_BATCH_CORRECTED",
        MovementCorrectionKind.Reverse => "MOVEMENT_REVERSED",
        MovementCorrectionKind.Restore => "MOVEMENT_RESTORED",
        _ => throw new InvalidOperationException("MOVEMENT_MUTATION_AUDIT_HEALTH_INVALID")
    };

    [System.Diagnostics.CodeAnalysis.DoesNotReturn]
    private static void Fail() =>
        throw new InvalidOperationException("MOVEMENT_MUTATION_AUDIT_HEALTH_INVALID");
}
