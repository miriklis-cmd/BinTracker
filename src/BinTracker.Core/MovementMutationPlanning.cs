using System.Collections.ObjectModel;

namespace BinTracker.Core;

public enum MovementMutationKind { Correct = 0, Reverse = 1, Restore = 2 }
public enum MovementMutationScope { Individual = 0, WholeRoot = 1 }
public enum MovementMutationPlanKind { NoOp = 0, Substantive = 1 }
public enum PlannedMovementPurpose { CorrectionNeutraliser = 0, CorrectionReplacement = 1, OrdinaryReversal = 2, Restoration = 3 }
public enum ReversedLineDisposition { RemainReversed = 0, Restore = 1 }

/// <summary>Preserves unselected, explicit clear and selected-value request intent.</summary>
public readonly struct MovementFieldIntent<T>
{
    private MovementFieldIntent(bool selected, T? value) => (IsSelected, Value) = (selected, value);
    public bool IsSelected { get; }
    public T? Value { get; }
    public static MovementFieldIntent<T> Unselected => default;
    public static MovementFieldIntent<T> Selected(T? value) => new(true, value);
}

public sealed class MovementMutationRequest
{
    private MovementMutationRequest(MovementMutationKind kind, MovementMutationScope scope,
        IReadOnlySet<LogicalMovementLineId> targetLineIds, string reason,
        MovementFieldIntent<DateOnly> movementDate, MovementFieldIntent<MovementType> direction,
        MovementFieldIntent<int> customer, MovementFieldIntent<int> containerType,
        MovementFieldIntent<int> quantity, MovementFieldIntent<string> reference,
        MovementFieldIntent<string> notes, IReadOnlyDictionary<LogicalMovementLineId, ReversedLineDecision> reversedLineDecisions)
    {
        Kind = kind; Scope = scope; TargetLineIds = targetLineIds; Reason = reason;
        MovementDate = movementDate; Direction = direction; Customer = customer;
        ContainerType = containerType; Quantity = quantity; Reference = reference; Notes = notes;
        AppliedFieldMask = Mask(this);
        ReversedLineDecisions = reversedLineDecisions;
    }

    public MovementMutationKind Kind { get; }
    public MovementMutationScope Scope { get; }
    public IReadOnlySet<LogicalMovementLineId> TargetLineIds { get; }
    public string Reason { get; }
    public MovementFieldIntent<DateOnly> MovementDate { get; }
    public MovementFieldIntent<MovementType> Direction { get; }
    public MovementFieldIntent<int> Customer { get; }
    public MovementFieldIntent<int> ContainerType { get; }
    public MovementFieldIntent<int> Quantity { get; }
    public MovementFieldIntent<string> Reference { get; }
    public MovementFieldIntent<string> Notes { get; }
    public MovementChangeField AppliedFieldMask { get; }
    public IReadOnlyDictionary<LogicalMovementLineId, ReversedLineDecision> ReversedLineDecisions { get; }

    public static MovementMutationRequest Correct(MovementMutationScope scope,
        IEnumerable<LogicalMovementLineId> targetLineIds, string reason,
        MovementFieldIntent<DateOnly> movementDate = default,
        MovementFieldIntent<MovementType> direction = default,
        MovementFieldIntent<int> customer = default,
        MovementFieldIntent<int> containerType = default,
        MovementFieldIntent<int> quantity = default,
        MovementFieldIntent<string> reference = default,
        MovementFieldIntent<string> notes = default,
        IEnumerable<ReversedLineDecision>? reversedLineDecisions = null) =>
        Create(MovementMutationKind.Correct, scope, targetLineIds, reason, movementDate,
            direction, customer, containerType, quantity, reference, notes, reversedLineDecisions);

    public static MovementMutationRequest Reverse(MovementMutationScope scope,
        IEnumerable<LogicalMovementLineId> targetLineIds, string reason) =>
        Create(MovementMutationKind.Reverse, scope, targetLineIds, reason);

    public static MovementMutationRequest Restore(MovementMutationScope scope,
        IEnumerable<LogicalMovementLineId> targetLineIds, string reason,
        MovementFieldIntent<DateOnly> movementDate = default,
        MovementFieldIntent<MovementType> direction = default,
        MovementFieldIntent<int> customer = default,
        MovementFieldIntent<int> containerType = default,
        MovementFieldIntent<int> quantity = default,
        MovementFieldIntent<string> reference = default,
        MovementFieldIntent<string> notes = default) =>
        Create(MovementMutationKind.Restore, scope, targetLineIds, reason, movementDate,
            direction, customer, containerType, quantity, reference, notes);

    private static MovementMutationRequest Create(MovementMutationKind kind, MovementMutationScope scope,
        IEnumerable<LogicalMovementLineId> targetLineIds, string? reason,
        MovementFieldIntent<DateOnly> movementDate = default,
        MovementFieldIntent<MovementType> direction = default,
        MovementFieldIntent<int> customer = default,
        MovementFieldIntent<int> containerType = default,
        MovementFieldIntent<int> quantity = default,
        MovementFieldIntent<string> reference = default,
        MovementFieldIntent<string> notes = default,
        IEnumerable<ReversedLineDecision>? reversedLineDecisions = null)
    {
        if (!Enum.IsDefined(kind) || !Enum.IsDefined(scope)) throw new ArgumentOutOfRangeException(nameof(kind));
        var targets = targetLineIds?.ToHashSet() ?? throw new ArgumentNullException(nameof(targetLineIds));
        if (targets.Count == 0 || targets.Any(x => x.Value <= 0)) throw new ArgumentException("At least one valid target line is required.");
        var cleanReason = (reason ?? string.Empty).Trim();
        if (cleanReason.Length is < 3 or > 500) throw new ArgumentException("Reason must contain 3 to 500 characters.");
        if (kind == MovementMutationKind.Reverse &&
            (movementDate.IsSelected || direction.IsSelected || customer.IsSelected || containerType.IsSelected ||
             quantity.IsSelected || reference.IsSelected || notes.IsSelected))
            throw new ArgumentException("A reversal cannot contain field overrides.");
        if (kind == MovementMutationKind.Correct && Mask(movementDate, direction, customer, containerType, quantity, reference, notes) == MovementChangeField.None)
            throw new ArgumentException("A correction must select at least one field.");
        if (movementDate.IsSelected && movementDate.Value == default) throw new ArgumentException("Movement date is invalid.");
        if (direction.IsSelected && !Enum.IsDefined(direction.Value)) throw new ArgumentException("Direction is invalid.");
        if (customer.IsSelected && customer.Value <= 0) throw new ArgumentException("Customer is invalid.");
        if (containerType.IsSelected && containerType.Value <= 0) throw new ArgumentException("Container type is invalid.");
        if (quantity.IsSelected && quantity.Value <= 0) throw new ArgumentException("Quantity must be positive.");
        reference = Normalize(reference); notes = Normalize(notes);
        var decisionList = (reversedLineDecisions ?? []).ToList();
        var decisions = decisionList.GroupBy(x => x.LineId).ToDictionary(x => x.Key, x => x.First());
        if (decisions.Count != decisionList.Count ||
            decisions.Any(x => x.Key.Value <= 0 || x.Value.LineId != x.Key || !Enum.IsDefined(x.Value.Disposition)))
            throw new ArgumentException("Reversed-line decisions must be unique and valid.");
        if ((kind != MovementMutationKind.Correct || scope != MovementMutationScope.WholeRoot) && decisions.Count != 0)
            throw new ArgumentException("Reversed-line decisions belong only to whole-root correction.");
        return new(kind, scope, new ReadOnlySet<LogicalMovementLineId>(targets), cleanReason,
            movementDate, direction, customer, containerType, quantity, reference, notes,
            new ReadOnlyDictionary<LogicalMovementLineId, ReversedLineDecision>(decisions));
    }

    private static MovementFieldIntent<string> Normalize(MovementFieldIntent<string> value) =>
        !value.IsSelected ? value : MovementFieldIntent<string>.Selected(
            string.IsNullOrWhiteSpace(value.Value) ? null : value.Value.Trim());

    private static MovementChangeField Mask(MovementMutationRequest request) => Mask(request.MovementDate,
        request.Direction, request.Customer, request.ContainerType, request.Quantity, request.Reference, request.Notes);
    private static MovementChangeField Mask(MovementFieldIntent<DateOnly> date, MovementFieldIntent<MovementType> direction,
        MovementFieldIntent<int> customer, MovementFieldIntent<int> container, MovementFieldIntent<int> quantity,
        MovementFieldIntent<string> reference, MovementFieldIntent<string> notes) =>
        (date.IsSelected ? MovementChangeField.MovementDate : 0) |
        (direction.IsSelected ? MovementChangeField.Direction : 0) |
        (customer.IsSelected ? MovementChangeField.Customer : 0) |
        (container.IsSelected ? MovementChangeField.ContainerType : 0) |
        (quantity.IsSelected ? MovementChangeField.Quantity : 0) |
        (reference.IsSelected ? MovementChangeField.Reference : 0) |
        (notes.IsSelected ? MovementChangeField.Notes : 0);
}

public sealed class ReversedLineDecision
{
    private ReversedLineDecision(LogicalMovementLineId lineId, ReversedLineDisposition disposition,
        MovementFieldIntent<DateOnly> movementDate, MovementFieldIntent<MovementType> direction,
        MovementFieldIntent<int> customer, MovementFieldIntent<int> containerType,
        MovementFieldIntent<int> quantity, MovementFieldIntent<string> reference, MovementFieldIntent<string> notes)
    {
        LineId = lineId; Disposition = disposition; MovementDate = movementDate; Direction = direction;
        Customer = customer; ContainerType = containerType; Quantity = quantity;
        Reference = Normalize(reference); Notes = Normalize(notes); AppliedFieldMask = Mask();
        if (lineId.Value <= 0 || !Enum.IsDefined(disposition)) throw new ArgumentException("Reversed-line decision is invalid.");
        if (disposition == ReversedLineDisposition.RemainReversed && AppliedFieldMask != MovementChangeField.None)
            throw new ArgumentException("RemainReversed cannot contain restoration overrides.");
        if (movementDate.IsSelected && movementDate.Value == default || direction.IsSelected && !Enum.IsDefined(direction.Value) ||
            customer.IsSelected && customer.Value <= 0 || containerType.IsSelected && containerType.Value <= 0 ||
            quantity.IsSelected && quantity.Value <= 0) throw new ArgumentException("Restoration override is invalid.");
    }
    public LogicalMovementLineId LineId { get; }
    public ReversedLineDisposition Disposition { get; }
    public MovementFieldIntent<DateOnly> MovementDate { get; }
    public MovementFieldIntent<MovementType> Direction { get; }
    public MovementFieldIntent<int> Customer { get; }
    public MovementFieldIntent<int> ContainerType { get; }
    public MovementFieldIntent<int> Quantity { get; }
    public MovementFieldIntent<string> Reference { get; }
    public MovementFieldIntent<string> Notes { get; }
    public MovementChangeField AppliedFieldMask { get; }
    public static ReversedLineDecision RemainReversed(LogicalMovementLineId lineId) => new(lineId,
        ReversedLineDisposition.RemainReversed, default, default, default, default, default, default, default);
    public static ReversedLineDecision Restore(LogicalMovementLineId lineId,
        MovementFieldIntent<DateOnly> movementDate = default, MovementFieldIntent<MovementType> direction = default,
        MovementFieldIntent<int> customer = default, MovementFieldIntent<int> containerType = default,
        MovementFieldIntent<int> quantity = default, MovementFieldIntent<string> reference = default,
        MovementFieldIntent<string> notes = default) => new(lineId, ReversedLineDisposition.Restore, movementDate,
            direction, customer, containerType, quantity, reference, notes);
    private MovementChangeField Mask() =>
        (MovementDate.IsSelected ? MovementChangeField.MovementDate : 0) | (Direction.IsSelected ? MovementChangeField.Direction : 0) |
        (Customer.IsSelected ? MovementChangeField.Customer : 0) | (ContainerType.IsSelected ? MovementChangeField.ContainerType : 0) |
        (Quantity.IsSelected ? MovementChangeField.Quantity : 0) | (Reference.IsSelected ? MovementChangeField.Reference : 0) |
        (Notes.IsSelected ? MovementChangeField.Notes : 0);
    private static MovementFieldIntent<string> Normalize(MovementFieldIntent<string> value) => !value.IsSelected ? value :
        MovementFieldIntent<string>.Selected(string.IsNullOrWhiteSpace(value.Value) ? null : value.Value.Trim());
}

public sealed class TrustedMovementPlanningLine
{
    internal TrustedMovementPlanningLine(ValidatedLogicalMovementCurrentLine current, MovementBusinessState lastEffective,
        MovementBusinessState? terminalReversal) => (Current, LastEffective, TerminalReversal) = (current, lastEffective, terminalReversal);
    public ValidatedLogicalMovementCurrentLine Current { get; }
    public MovementBusinessState LastEffective { get; }
    public MovementBusinessState? TerminalReversal { get; }
}

public sealed class TrustedMovementPlanningSnapshot
{
    internal TrustedMovementPlanningSnapshot(ValidatedLogicalMovementCurrentRoot root,
        IReadOnlyList<TrustedMovementPlanningLine> lines, IReadOnlySet<int> activeCustomerIds,
        IReadOnlySet<int> activeContainerTypeIds)
    { Root = root; Lines = lines; ActiveCustomerIds = activeCustomerIds; ActiveContainerTypeIds = activeContainerTypeIds; }
    public ValidatedLogicalMovementCurrentRoot Root { get; }
    public IReadOnlyList<TrustedMovementPlanningLine> Lines { get; }
    internal IReadOnlySet<int> ActiveCustomerIds { get; }
    internal IReadOnlySet<int> ActiveContainerTypeIds { get; }
}

public sealed class MovementBusinessState
{
    internal MovementBusinessState(long movementId, DateOnly movementDate, MovementType direction,
        MovementSource source, int customerId, int containerTypeId, int quantity, string? reference,
        string? notes, int? movementBatchId, long? importRunId, long? reversesMovementId) =>
        (MovementId, MovementDate, Direction, Source, CustomerId, ContainerTypeId, Quantity, Reference,
            Notes, MovementBatchId, ImportRunId, ReversesMovementId) = (movementId, movementDate, direction,
            source, customerId, containerTypeId, quantity, reference, notes, movementBatchId, importRunId,
            reversesMovementId);
    public long MovementId { get; }
    public DateOnly MovementDate { get; }
    public MovementType Direction { get; }
    public MovementSource Source { get; }
    public int CustomerId { get; }
    public int ContainerTypeId { get; }
    public int Quantity { get; }
    public string? Reference { get; }
    public string? Notes { get; }
    public int? MovementBatchId { get; }
    public long? ImportRunId { get; }
    public long? ReversesMovementId { get; }
}

public sealed class PlannedMovementSpec
{
    internal PlannedMovementSpec(PlannedMovementPurpose purpose, LogicalMovementLineId lineId,
        DateOnly movementDate, MovementType direction, MovementSource source, int customerId,
        int containerTypeId, int quantity, string? reference, string? notes, string reason,
        long? reversesMovementId) => (Purpose, LineId, MovementDate, Direction, Source, CustomerId,
            ContainerTypeId, Quantity, Reference, Notes, Reason, ReversesMovementId) = (purpose, lineId,
            movementDate, direction, source, customerId, containerTypeId, quantity, reference, notes,
            reason, reversesMovementId);
    public PlannedMovementPurpose Purpose { get; }
    public LogicalMovementLineId LineId { get; }
    public DateOnly MovementDate { get; }
    public MovementType Direction { get; }
    public MovementSource Source { get; }
    public int CustomerId { get; }
    public int ContainerTypeId { get; }
    public int Quantity { get; }
    public string? Reference { get; }
    public string? Notes { get; }
    public string Reason { get; }
    public long? ReversesMovementId { get; }
}

public sealed class PlannedMovementLine
{
    internal PlannedMovementLine(LogicalMovementLineId lineId, LogicalMovementGenerationAction action,
        LogicalMovementLineState state, MovementChangeField mask, PlannedMovementReference effectiveMovement,
        PlannedMovementReference? terminalReversalMovement,
        IReadOnlyList<PlannedMovementSpec> movements)
    { LineId = lineId; Action = action; State = state; AppliedFieldMask = mask;
      EffectiveMovement = effectiveMovement; TerminalReversalMovement = terminalReversalMovement; Movements = movements; }
    public LogicalMovementLineId LineId { get; }
    public LogicalMovementGenerationAction Action { get; }
    public LogicalMovementLineState State { get; }
    public MovementChangeField AppliedFieldMask { get; }
    public PlannedMovementReference EffectiveMovement { get; }
    public PlannedMovementReference? TerminalReversalMovement { get; }
    public IReadOnlyList<PlannedMovementSpec> Movements { get; }
}

public sealed class PlannedMovementReference
{
    private PlannedMovementReference(long? existingMovementId, PlannedMovementPurpose? plannedPurpose) =>
        (ExistingMovementId, PlannedPurpose) = (existingMovementId, plannedPurpose);
    public long? ExistingMovementId { get; }
    public PlannedMovementPurpose? PlannedPurpose { get; }
    internal static PlannedMovementReference Existing(long movementId) => movementId > 0
        ? new(movementId, null) : throw new ArgumentOutOfRangeException(nameof(movementId));
    internal static PlannedMovementReference Planned(PlannedMovementPurpose purpose) => Enum.IsDefined(purpose)
        ? new(null, purpose) : throw new ArgumentOutOfRangeException(nameof(purpose));
}

public sealed class PlannedPhysicalOutputMember
{
    internal PlannedPhysicalOutputMember(LogicalMovementLineId lineId, PlannedMovementPurpose purpose) =>
        (LineId, Purpose) = (lineId, purpose);
    public LogicalMovementLineId LineId { get; }
    public PlannedMovementPurpose Purpose { get; }
}
public sealed class PlannedPhysicalOutput
{
    internal PlannedPhysicalOutput(DateOnly movementDate, MovementType direction, MovementSource source,
        IReadOnlyList<PlannedPhysicalOutputMember> members) =>
        (MovementDate, Direction, Source, Members) = (movementDate, direction, source, members);
    public DateOnly MovementDate { get; }
    public MovementType Direction { get; }
    public MovementSource Source { get; }
    public IReadOnlyList<PlannedPhysicalOutputMember> Members { get; }
}

public sealed class MovementMutationPlan
{
    private MovementMutationPlan(MovementMutationPlanKind kind, LogicalMovementBatchId rootId,
        LogicalMovementGenerationNumber generation, IReadOnlyList<PlannedMovementLine> lines,
        PlannedPhysicalOutput? physicalOutput)
    { Kind = kind; RootId = rootId; PlannedFromGeneration = generation; Lines = lines; PhysicalOutput = physicalOutput; }
    public MovementMutationPlanKind Kind { get; }
    public LogicalMovementBatchId RootId { get; }
    public LogicalMovementGenerationNumber PlannedFromGeneration { get; }
    public IReadOnlyList<PlannedMovementLine> Lines { get; }
    public PlannedPhysicalOutput? PhysicalOutput { get; }
    internal static MovementMutationPlan NoOp(TrustedMovementPlanningSnapshot s) =>
        new(MovementMutationPlanKind.NoOp, s.Root.Id, s.Root.CurrentGenerationNumber, Array.Empty<PlannedMovementLine>(), null);
    internal static MovementMutationPlan Substantive(TrustedMovementPlanningSnapshot s,
        IReadOnlyList<PlannedMovementLine> lines, PlannedPhysicalOutput? output) =>
        new(MovementMutationPlanKind.Substantive, s.Root.Id, s.Root.CurrentGenerationNumber, lines, output);
}

public static class MovementMutationPlanner
{
    public static MovementMutationPlan Plan(TrustedMovementPlanningSnapshot snapshot, MovementMutationRequest request,
        DateOnly authoritativeBusinessDate)
    {
        ArgumentNullException.ThrowIfNull(snapshot); ArgumentNullException.ThrowIfNull(request);
        if (authoritativeBusinessDate == default) throw new ArgumentException("Authoritative business date is required.");
        ValidateTrustedSnapshotFacts(snapshot);
        ValidateCurrentMovementDates(snapshot, authoritativeBusinessDate);
        var rootIds = snapshot.Lines.Select(x => x.Current.Id).ToHashSet();
        if (!request.TargetLineIds.IsSubsetOf(rootIds) ||
            (request.Scope == MovementMutationScope.WholeRoot && !request.TargetLineIds.SetEquals(rootIds)))
            throw new InvalidOperationException("Request targets do not match the trusted root membership.");
        ValidateDecisions(snapshot, request, authoritativeBusinessDate);
        ValidateDate(request.MovementDate, authoritativeBusinessDate);
        if (request.Customer.IsSelected && !snapshot.ActiveCustomerIds.Contains(request.Customer.Value) ||
            request.ContainerType.IsSelected && !snapshot.ActiveContainerTypeIds.Contains(request.ContainerType.Value))
            throw new InvalidOperationException("Selected master data does not exist or is inactive.");

        var planned = new List<PlannedMovementLine>(snapshot.Lines.Count);
        var substantive = false;
        foreach (var line in snapshot.Lines)
        {
            var targeted = request.TargetLineIds.Contains(line.Current.Id);
            var item = request.Kind switch
            {
                MovementMutationKind.Correct => Correct(line, targeted, request),
                MovementMutationKind.Reverse => Reverse(line, targeted, request, authoritativeBusinessDate),
                MovementMutationKind.Restore => Restore(line, targeted, request),
                _ => throw new InvalidOperationException("Unsupported mutation kind.")
            };
            planned.Add(item);
            substantive |= item.Movements.Count != 0 || item.Action == LogicalMovementGenerationAction.Restored;
        }
        if (!substantive) return MovementMutationPlan.NoOp(snapshot);
        var frozen = new ReadOnlyCollection<PlannedMovementLine>(planned);
        ValidateLineSemantics(request, frozen);
        ValidateResultShape(frozen);
        return MovementMutationPlan.Substantive(snapshot, frozen, PhysicalOutput(snapshot, request, frozen));
    }

    private static PlannedMovementLine Correct(TrustedMovementPlanningLine line, bool targeted, MovementMutationRequest request)
    {
        if (!targeted) return Carry(line);
        if (line.Current.State == LogicalMovementLineState.Reversed)
        {
            if (request.Scope != MovementMutationScope.WholeRoot)
                throw new InvalidOperationException("A reversed line cannot be corrected; restore it instead.");
            var decision = request.ReversedLineDecisions[line.Current.Id];
            return decision.Disposition == ReversedLineDisposition.RemainReversed
                ? Line(line, LogicalMovementGenerationAction.RemainReversed, LogicalMovementLineState.Reversed,
                    MovementChangeField.None, [])
                : Restore(line, decision, request.Reason);
        }
        var matches = Matches(line.LastEffective, request);
        var result = Apply(line.LastEffective, request);
        if (matches)
            return Line(line, LogicalMovementGenerationAction.AlreadyMatches, LogicalMovementLineState.Active, request.AppliedFieldMask, []);
        var neutral = Neutralise(line, request.Reason, PlannedMovementPurpose.CorrectionNeutraliser, line.LastEffective.MovementDate);
        var replacement = Spec(line, result, request.Reason, PlannedMovementPurpose.CorrectionReplacement, null);
        return Line(line, LogicalMovementGenerationAction.Corrected, LogicalMovementLineState.Active,
            request.AppliedFieldMask, [neutral, replacement]);
    }

    private static PlannedMovementLine Reverse(TrustedMovementPlanningLine line, bool targeted,
        MovementMutationRequest request, DateOnly authoritativeBusinessDate)
    {
        if (!targeted) return Carry(line);
        if (line.Current.State == LogicalMovementLineState.Reversed)
        {
            if (request.Scope != MovementMutationScope.WholeRoot)
                throw new InvalidOperationException("The selected line is already reversed.");
            return Line(line, LogicalMovementGenerationAction.RemainReversed,
                LogicalMovementLineState.Reversed, MovementChangeField.None, []);
        }
        var reversal = Neutralise(line, request.Reason, PlannedMovementPurpose.OrdinaryReversal, authoritativeBusinessDate);
        return Line(line, LogicalMovementGenerationAction.Reversed, LogicalMovementLineState.Reversed,
            MovementChangeField.None, [reversal]);
    }

    private static PlannedMovementLine Restore(TrustedMovementPlanningLine line, bool targeted, MovementMutationRequest request)
    {
        if (!targeted) return Carry(line);
        if (line.Current.State != LogicalMovementLineState.Reversed)
            throw new InvalidOperationException("Only a reversed line can be restored.");
        var result = Apply(line.LastEffective, request);
        return Line(line, LogicalMovementGenerationAction.Restored, LogicalMovementLineState.Active,
            request.AppliedFieldMask, [Spec(line, result, request.Reason, PlannedMovementPurpose.Restoration,
                line.TerminalReversal?.MovementId)]);
    }

    private static PlannedMovementLine Restore(TrustedMovementPlanningLine line, ReversedLineDecision decision, string reason)
    {
        var result = Apply(line.LastEffective, decision);
        return Line(line, LogicalMovementGenerationAction.Restored, LogicalMovementLineState.Active,
            decision.AppliedFieldMask, [Spec(line, result, reason, PlannedMovementPurpose.Restoration,
                line.TerminalReversal?.MovementId)]);
    }

    private static PlannedMovementLine Carry(TrustedMovementPlanningLine line) => Line(line,
        LogicalMovementGenerationAction.CarriedForward, line.Current.State, MovementChangeField.None, []);
    private static PlannedMovementLine Line(TrustedMovementPlanningLine line, LogicalMovementGenerationAction action,
        LogicalMovementLineState state, MovementChangeField mask, IReadOnlyList<PlannedMovementSpec> specs)
    {
        var effective = action switch
        {
            LogicalMovementGenerationAction.Corrected => PlannedMovementReference.Planned(PlannedMovementPurpose.CorrectionReplacement),
            LogicalMovementGenerationAction.Restored => PlannedMovementReference.Planned(PlannedMovementPurpose.Restoration),
            _ => PlannedMovementReference.Existing(line.LastEffective.MovementId)
        };
        var terminal = action switch
        {
            LogicalMovementGenerationAction.Reversed => PlannedMovementReference.Planned(PlannedMovementPurpose.OrdinaryReversal),
            LogicalMovementGenerationAction.RemainReversed or LogicalMovementGenerationAction.CarriedForward
                when state == LogicalMovementLineState.Reversed => PlannedMovementReference.Existing(line.TerminalReversal!.MovementId),
            _ => null
        };
        return new(line.Current.Id, action, state, mask, effective, terminal,
            new ReadOnlyCollection<PlannedMovementSpec>(specs.ToList()));
    }

    private static MovementBusinessState Apply(MovementBusinessState x, MovementMutationRequest r) => new(
        x.MovementId, r.MovementDate.IsSelected ? r.MovementDate.Value : x.MovementDate,
        r.Direction.IsSelected ? r.Direction.Value : x.Direction, x.Source,
        r.Customer.IsSelected ? r.Customer.Value : x.CustomerId,
        r.ContainerType.IsSelected ? r.ContainerType.Value : x.ContainerTypeId,
        r.Quantity.IsSelected ? r.Quantity.Value : x.Quantity,
        r.Reference.IsSelected ? r.Reference.Value : x.Reference,
        r.Notes.IsSelected ? r.Notes.Value : x.Notes, x.MovementBatchId, x.ImportRunId,
        x.ReversesMovementId);

    private static MovementBusinessState Apply(MovementBusinessState x, ReversedLineDecision r) => new(
        x.MovementId, r.MovementDate.IsSelected ? r.MovementDate.Value : x.MovementDate,
        r.Direction.IsSelected ? r.Direction.Value : x.Direction, x.Source,
        r.Customer.IsSelected ? r.Customer.Value : x.CustomerId,
        r.ContainerType.IsSelected ? r.ContainerType.Value : x.ContainerTypeId,
        r.Quantity.IsSelected ? r.Quantity.Value : x.Quantity,
        r.Reference.IsSelected ? r.Reference.Value : x.Reference,
        r.Notes.IsSelected ? r.Notes.Value : x.Notes, x.MovementBatchId, x.ImportRunId,
        x.ReversesMovementId);

    private static bool Matches(MovementBusinessState x, MovementMutationRequest r) =>
        (!r.MovementDate.IsSelected || r.MovementDate.Value == x.MovementDate) &&
        (!r.Direction.IsSelected || r.Direction.Value == x.Direction) &&
        (!r.Customer.IsSelected || r.Customer.Value == x.CustomerId) &&
        (!r.ContainerType.IsSelected || r.ContainerType.Value == x.ContainerTypeId) &&
        (!r.Quantity.IsSelected || r.Quantity.Value == x.Quantity) &&
        (!r.Reference.IsSelected || r.Reference.Value == Clean(x.Reference)) &&
        (!r.Notes.IsSelected || r.Notes.Value == Clean(x.Notes));

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static PlannedMovementSpec Neutralise(TrustedMovementPlanningLine line, string reason,
        PlannedMovementPurpose purpose, DateOnly date) => new(purpose, line.Current.Id, date,
        Opposite(line.LastEffective.Direction), MovementSource.Manual, line.LastEffective.CustomerId,
        line.LastEffective.ContainerTypeId, line.LastEffective.Quantity,
        string.IsNullOrWhiteSpace(line.LastEffective.Reference) ? $"REV-{line.LastEffective.MovementId}" :
            $"REV-{line.LastEffective.MovementId} / {line.LastEffective.Reference}",
        $"Neutralises movement #{line.LastEffective.MovementId}. Reason: {reason}", reason,
        line.LastEffective.MovementId);
    private static PlannedMovementSpec Spec(TrustedMovementPlanningLine line, MovementBusinessState x, string reason,
        PlannedMovementPurpose purpose, long? reverses) => new(purpose, line.Current.Id, x.MovementDate,
        x.Direction, x.Source, x.CustomerId, x.ContainerTypeId, x.Quantity, x.Reference, x.Notes, reason, reverses);
    private static MovementType Opposite(MovementType x) => x == MovementType.In ? MovementType.Out : MovementType.In;

    private static PlannedPhysicalOutput? PhysicalOutput(TrustedMovementPlanningSnapshot snapshot,
        MovementMutationRequest request, IReadOnlyList<PlannedMovementLine> lines)
    {
        if (snapshot.Root.RootMovementBatchId is null || request.Scope != MovementMutationScope.WholeRoot ||
            lines.Any(x => x.Action is not (LogicalMovementGenerationAction.Corrected or LogicalMovementGenerationAction.Restored))) return null;
        var members = lines.SelectMany(x => x.Movements.Where(m => m.Purpose is PlannedMovementPurpose.CorrectionReplacement or PlannedMovementPurpose.Restoration)).ToArray();
        if (members.Length != lines.Count || members.Any(x => x.Source != MovementSource.Batch) ||
            members.Any(x => snapshot.Lines.Single(l => l.Current.Id == x.LineId).LastEffective.ImportRunId is not null) ||
            members.Select(x => x.MovementDate).Distinct().Count() != 1 || members.Select(x => x.Direction).Distinct().Count() != 1)
            return null;
        return new(members[0].MovementDate, members[0].Direction, MovementSource.Batch,
            new ReadOnlyCollection<PlannedPhysicalOutputMember>(members.Select(x =>
                new PlannedPhysicalOutputMember(x.LineId, x.Purpose)).ToList()));
    }

    internal static void ValidateTrustedSnapshotFacts(TrustedMovementPlanningSnapshot s)
    {
        if (s.Root.Status != LogicalMovementBatchStatus.Active)
            throw new InvalidOperationException("Trusted planning snapshot is incomplete or not mutable.");
        ValidateCurrentProjectionFacts(s.Root, s.Lines);
    }

    internal static void ValidateCurrentProjectionFacts(ValidatedLogicalMovementCurrentRoot root,
        IReadOnlyList<TrustedMovementPlanningLine> lines)
    {
        if (root.Status is not (LogicalMovementBatchStatus.Active or LogicalMovementBatchStatus.ReadOnly) ||
            lines.Count != root.Lines.Count ||
            lines.Select(x => x.Current.Id).Distinct().Count() != lines.Count ||
            !lines.Select(x => x.Current.Id).ToHashSet().SetEquals(root.Lines.Select(x => x.Id)))
            throw new InvalidOperationException("Trusted current projection snapshot is incomplete or unhealthy.");
        foreach (var line in lines)
        {
            var reversal = line.TerminalReversal;
            if (line.LastEffective.MovementId != line.Current.EffectiveMovementId ||
                (line.Current.State == LogicalMovementLineState.Reversed) != (reversal is not null) ||
                reversal?.MovementId != line.Current.TerminalReversalMovementId ||
                !Enum.IsDefined(line.LastEffective.Direction) || !Enum.IsDefined(line.LastEffective.Source) ||
                line.LastEffective.Source is MovementSource.ExcelImport or MovementSource.Adjustment ||
                line.LastEffective.ImportRunId is not null ||
                line.LastEffective.CustomerId <= 0 || line.LastEffective.ContainerTypeId <= 0 ||
                line.LastEffective.Quantity <= 0 ||
                reversal is not null &&
                    (reversal.ReversesMovementId != line.LastEffective.MovementId ||
                     reversal.Direction != Opposite(line.LastEffective.Direction) ||
                     reversal.CustomerId != line.LastEffective.CustomerId ||
                     reversal.ContainerTypeId != line.LastEffective.ContainerTypeId ||
                     reversal.Quantity != line.LastEffective.Quantity ||
                     reversal.Source != MovementSource.Manual ||
                     reversal.ImportRunId is not null ||
                     reversal.MovementBatchId is not null))
                throw new InvalidOperationException("Trusted planning facts do not match validated current pointers.");
        }
    }

    private static void ValidateCurrentMovementDates(TrustedMovementPlanningSnapshot snapshot,
        DateOnly authoritativeBusinessDate)
    {
        if (snapshot.Lines.Any(line => line.LastEffective.MovementDate > authoritativeBusinessDate ||
            line.TerminalReversal?.MovementDate > authoritativeBusinessDate))
            throw new InvalidOperationException(
                "A current effective or terminal reversal movement is in the future relative to the authoritative business date.");
    }

    private static void ValidateDecisions(TrustedMovementPlanningSnapshot snapshot, MovementMutationRequest request,
        DateOnly authoritativeBusinessDate)
    {
        var reversed = snapshot.Lines.Where(x => x.Current.State == LogicalMovementLineState.Reversed)
            .Select(x => x.Current.Id).ToHashSet();
        if (request.Kind == MovementMutationKind.Correct && request.Scope == MovementMutationScope.WholeRoot)
        {
            if (!request.ReversedLineDecisions.Keys.ToHashSet().SetEquals(reversed))
                throw new InvalidOperationException("Every and only reversed root line requires an explicit decision.");
            foreach (var decision in request.ReversedLineDecisions.Values)
            {
                ValidateDate(decision.MovementDate, authoritativeBusinessDate);
                if (decision.Customer.IsSelected && !snapshot.ActiveCustomerIds.Contains(decision.Customer.Value) ||
                    decision.ContainerType.IsSelected && !snapshot.ActiveContainerTypeIds.Contains(decision.ContainerType.Value))
                    throw new InvalidOperationException("Selected restoration master data does not exist or is inactive.");
            }
        }
        else if (request.ReversedLineDecisions.Count != 0)
            throw new InvalidOperationException("Reversed-line decisions are invalid for this mutation.");
    }

    private static void ValidateDate(MovementFieldIntent<DateOnly> date, DateOnly authoritativeBusinessDate)
    {
        if (date.IsSelected && date.Value > authoritativeBusinessDate)
            throw new InvalidOperationException("Movement date is in the future relative to the authoritative business date.");
    }

    private static void ValidateResultShape(IReadOnlyList<PlannedMovementLine> lines)
    {
        foreach (var line in lines)
        {
            var purposes = line.Movements.Select(x => x.Purpose).ToArray();
            var expected = line.Action switch
            {
                LogicalMovementGenerationAction.Corrected => new[] { PlannedMovementPurpose.CorrectionNeutraliser, PlannedMovementPurpose.CorrectionReplacement },
                LogicalMovementGenerationAction.Reversed => new[] { PlannedMovementPurpose.OrdinaryReversal },
                LogicalMovementGenerationAction.Restored => new[] { PlannedMovementPurpose.Restoration },
                _ => Array.Empty<PlannedMovementPurpose>()
            };
            var expectedEffectivePurpose = line.Action switch
            {
                LogicalMovementGenerationAction.Corrected => PlannedMovementPurpose.CorrectionReplacement,
                LogicalMovementGenerationAction.Restored => PlannedMovementPurpose.Restoration,
                _ => (PlannedMovementPurpose?)null
            };
            var expectedTerminalPurpose = line.Action == LogicalMovementGenerationAction.Reversed
                ? PlannedMovementPurpose.OrdinaryReversal : (PlannedMovementPurpose?)null;
            if (!purposes.SequenceEqual(expected) || !IsValidReference(line.EffectiveMovement) ||
                line.EffectiveMovement.PlannedPurpose != expectedEffectivePurpose ||
                (line.State == LogicalMovementLineState.Reversed) != (line.TerminalReversalMovement is not null) ||
                line.TerminalReversalMovement is { } terminal && (!IsValidReference(terminal) ||
                    terminal.PlannedPurpose != expectedTerminalPurpose))
                throw new InvalidOperationException("Generation action, movement specifications and result pointers are inconsistent.");
        }
    }

    private static bool IsValidReference(PlannedMovementReference reference) =>
        (reference.ExistingMovementId is not null) != (reference.PlannedPurpose is not null);

    internal static void ValidateLineSemantics(MovementMutationRequest request, IReadOnlyList<PlannedMovementLine> lines)
    {
        const MovementChangeField all = MovementChangeField.MovementDate | MovementChangeField.Direction |
            MovementChangeField.Customer | MovementChangeField.ContainerType | MovementChangeField.Quantity |
            MovementChangeField.Reference | MovementChangeField.Notes;
        foreach (var line in lines)
        {
            if ((line.AppliedFieldMask & ~all) != 0) throw new InvalidOperationException("Applied field mask contains undefined bits.");
            var expected = line.Action switch
            {
                LogicalMovementGenerationAction.Corrected or LogicalMovementGenerationAction.AlreadyMatches
                    when request.Kind == MovementMutationKind.Correct => request.AppliedFieldMask,
                LogicalMovementGenerationAction.Restored when request.Kind == MovementMutationKind.Restore => request.AppliedFieldMask,
                LogicalMovementGenerationAction.Restored when request.Kind == MovementMutationKind.Correct &&
                    request.ReversedLineDecisions.TryGetValue(line.LineId, out var decision) => decision.AppliedFieldMask,
                LogicalMovementGenerationAction.CarriedForward or LogicalMovementGenerationAction.Reversed or
                    LogicalMovementGenerationAction.RemainReversed => MovementChangeField.None,
                _ => throw new InvalidOperationException("Generation action is irreconcilable with the request kind.")
            };
            if (line.AppliedFieldMask != expected ||
                line.Action is LogicalMovementGenerationAction.Corrected or LogicalMovementGenerationAction.AlreadyMatches &&
                    (line.State != LogicalMovementLineState.Active || line.AppliedFieldMask == MovementChangeField.None) ||
                line.Action is LogicalMovementGenerationAction.Reversed or LogicalMovementGenerationAction.RemainReversed &&
                    line.State != LogicalMovementLineState.Reversed ||
                line.Action == LogicalMovementGenerationAction.Restored && line.State != LogicalMovementLineState.Active)
                throw new InvalidOperationException("Generation action, state and applied field mask are inconsistent.");
        }
    }
}

internal sealed class ReadOnlySet<T>(ISet<T> values) : ReadOnlyCollection<T>(values.ToList()), IReadOnlySet<T>
{
    public new bool Contains(T item) => values.Contains(item);
    public bool IsProperSubsetOf(IEnumerable<T> other) => values.IsProperSubsetOf(other);
    public bool IsProperSupersetOf(IEnumerable<T> other) => values.IsProperSupersetOf(other);
    public bool IsSubsetOf(IEnumerable<T> other) => values.IsSubsetOf(other);
    public bool IsSupersetOf(IEnumerable<T> other) => values.IsSupersetOf(other);
    public bool Overlaps(IEnumerable<T> other) => values.Overlaps(other);
    public bool SetEquals(IEnumerable<T> other) => values.SetEquals(other);
}
