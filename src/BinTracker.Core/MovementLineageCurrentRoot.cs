using System.Collections.ObjectModel;

namespace BinTracker.Core;

public enum LogicalMovementCurrentRootResolutionKind
{
    Resolved = 0,
    NotFound = 1,
    Unhealthy = 2
}

public enum LogicalMovementCurrentRootFailure
{
    None = 0,
    RootNotProjectable = 1,
    InvalidRoot = 2,
    InvalidPermanentMembership = 3,
    InvalidCurrentGeneration = 4,
    InvalidCurrentMembership = 5,
    InvalidCurrentState = 6,
    InvalidMovementOwnership = 7,
    InvalidTransformationRole = 8,
    InvalidRootOriginal = 9,
    InvalidIntroduction = 10
}

// These validated read models have no public construction or record-cloning surface.
// A successful instance can therefore originate only at the validator boundary.
public sealed class ValidatedLogicalMovementCurrentLine
{
    internal ValidatedLogicalMovementCurrentLine(LogicalMovementLineId id,
        LogicalMovementGenerationLineId currentGenerationLineId, long rootMovementId,
        int originalDisplayOrdinal, LogicalMovementLineState state, long? effectiveMovementId,
        long? terminalReversalMovementId)
    {
        Id = id;
        CurrentGenerationLineId = currentGenerationLineId;
        RootMovementId = rootMovementId;
        OriginalDisplayOrdinal = originalDisplayOrdinal;
        State = state;
        EffectiveMovementId = effectiveMovementId;
        TerminalReversalMovementId = terminalReversalMovementId;
    }

    public LogicalMovementLineId Id { get; }
    public LogicalMovementGenerationLineId CurrentGenerationLineId { get; }
    public long RootMovementId { get; }
    public int OriginalDisplayOrdinal { get; }
    public LogicalMovementLineState State { get; }
    public long? EffectiveMovementId { get; }
    public long? TerminalReversalMovementId { get; }
}

public sealed class ValidatedLogicalMovementCurrentRoot
{
    internal ValidatedLogicalMovementCurrentRoot(LogicalMovementBatchId id, int? rootMovementBatchId,
        LogicalMovementBatchStatus status, string? statusReasonCode,
        LogicalMovementGenerationNumber currentGenerationNumber,
        IReadOnlyList<ValidatedLogicalMovementCurrentLine> lines)
    {
        Id = id;
        RootMovementBatchId = rootMovementBatchId;
        Status = status;
        StatusReasonCode = statusReasonCode;
        CurrentGenerationNumber = currentGenerationNumber;
        Lines = lines;
    }

    public LogicalMovementBatchId Id { get; }
    public int? RootMovementBatchId { get; }
    public LogicalMovementBatchStatus Status { get; }
    public string? StatusReasonCode { get; }
    public LogicalMovementGenerationNumber CurrentGenerationNumber { get; }
    public IReadOnlyList<ValidatedLogicalMovementCurrentLine> Lines { get; }
}

public sealed class LogicalMovementCurrentRootResolution
{
    private LogicalMovementCurrentRootResolution(LogicalMovementCurrentRootResolutionKind kind,
        ValidatedLogicalMovementCurrentRoot? root, LogicalMovementCurrentRootFailure failure)
    {
        Kind = kind;
        Root = root;
        Failure = failure;
    }

    public LogicalMovementCurrentRootResolutionKind Kind { get; }
    public ValidatedLogicalMovementCurrentRoot? Root { get; }
    public LogicalMovementCurrentRootFailure Failure { get; }

    internal static LogicalMovementCurrentRootResolution NotFound() =>
        new(LogicalMovementCurrentRootResolutionKind.NotFound, null, LogicalMovementCurrentRootFailure.None);

    internal static LogicalMovementCurrentRootResolution Unhealthy(LogicalMovementCurrentRootFailure failure) =>
        new(LogicalMovementCurrentRootResolutionKind.Unhealthy, null, failure);

    internal static LogicalMovementCurrentRootResolution Resolved(ValidatedLogicalMovementCurrentRoot root) =>
        new(LogicalMovementCurrentRootResolutionKind.Resolved, root, LogicalMovementCurrentRootFailure.None);
}

public interface ILogicalMovementCurrentRootResolver
{
    Task<LogicalMovementCurrentRootResolution> ResolveAsync(
        LogicalMovementBatchId logicalMovementBatchId,
        CancellationToken cancellationToken = default);
}

internal sealed record LogicalMovementCurrentRootCandidate(
    long RequestedRootId,
    RawLogicalMovementRoot? Root,
    IReadOnlyList<RawLogicalMovementLine> PermanentLines,
    IReadOnlyList<RawLogicalMovementGeneration> SelectedGenerations,
    IReadOnlyList<RawLogicalMovementGenerationLine> CurrentLines,
    IReadOnlyList<RawLogicalMovementLedgerLink> LedgerLinks,
    IReadOnlyList<RawLogicalMovementIntroduction> Introductions,
    IReadOnlyDictionary<long, RawLogicalMovementFact> Movements,
    IReadOnlySet<int> ExistingMovementBatchIds);

internal sealed record RawLogicalMovementRoot(long Id, int? RootMovementBatchId, int Status,
    string? StatusReasonCode, int? CurrentGenerationNumber, int LineCount);
internal sealed record RawLogicalMovementLine(long Id, long RootId, long RootMovementId, int OriginalDisplayOrdinal);
internal sealed record RawLogicalMovementGeneration(long Id, long RootId, int Number, int LineCount);
internal sealed record RawLogicalMovementGenerationLine(long Id, long RootId, long GenerationId, long LineId, int State,
    long? ResultEffectiveMovementId, long? LastEffectiveMovementId, long? TerminalReversalMovementId);
internal sealed record RawLogicalMovementLedgerLink(long MovementId, long RootId, long LineId, int Role, long? IntroducedByGenerationLineId);
internal sealed record RawLogicalMovementIntroduction(long GenerationLineId, long RootId, long LineId);
internal sealed record RawLogicalMovementFact(long Id, int? MovementBatchId);

internal static class LogicalMovementCurrentRootValidator
{
    private static readonly HashSet<int> EffectiveRoles =
        [(int)LogicalMovementTransformationRole.RootOriginal,
         (int)LogicalMovementTransformationRole.CorrectionReplacement,
         (int)LogicalMovementTransformationRole.Restoration];

    public static LogicalMovementCurrentRootResolution Validate(LogicalMovementCurrentRootCandidate candidate)
    {
        var root = candidate.Root;
        if (root is null) return LogicalMovementCurrentRootResolution.NotFound();
        if (root.Id != candidate.RequestedRootId || !Enum.IsDefined(typeof(LogicalMovementBatchStatus), root.Status) || root.LineCount <= 0)
            return Bad(LogicalMovementCurrentRootFailure.InvalidRoot);
        var status = (LogicalMovementBatchStatus)root.Status;
        // ReadOnly is mutation-restricted, not corrupt; it must prove the same current mathematics as Active.
        if (status is LogicalMovementBatchStatus.Initializing or LogicalMovementBatchStatus.Invalid)
            return Bad(LogicalMovementCurrentRootFailure.RootNotProjectable);
        if (status is not (LogicalMovementBatchStatus.Active or LogicalMovementBatchStatus.ReadOnly))
            return Bad(LogicalMovementCurrentRootFailure.InvalidRoot);

        var permanent = candidate.PermanentLines;
        if (permanent.Count != root.LineCount || permanent.Any(x => x.RootId != root.Id) ||
            permanent.Select(x => x.Id).Distinct().Count() != permanent.Count ||
            permanent.Select(x => x.RootMovementId).Distinct().Count() != permanent.Count ||
            permanent.Any(x => x.OriginalDisplayOrdinal < 0) ||
            permanent.Select(x => x.OriginalDisplayOrdinal).Distinct().Count() != permanent.Count)
            return Bad(LogicalMovementCurrentRootFailure.InvalidPermanentMembership);
        if (root.CurrentGenerationNumber is null || root.CurrentGenerationNumber < 0 || candidate.SelectedGenerations.Count != 1)
            return Bad(LogicalMovementCurrentRootFailure.InvalidCurrentGeneration);
        var generation = candidate.SelectedGenerations[0];
        if (generation.RootId != root.Id || generation.Number != root.CurrentGenerationNumber || generation.LineCount != root.LineCount)
            return Bad(LogicalMovementCurrentRootFailure.InvalidCurrentGeneration);

        // Matching counts are insufficient: the permanent and current sets must be exactly equal.
        var permanentIds = permanent.Select(x => x.Id).ToHashSet();
        var currentIds = candidate.CurrentLines.Select(x => x.LineId).ToList();
        if (candidate.CurrentLines.Count != root.LineCount || currentIds.Distinct().Count() != currentIds.Count ||
            !permanentIds.SetEquals(currentIds) || candidate.CurrentLines.Any(x => x.RootId != root.Id || x.GenerationId != generation.Id))
            return Bad(LogicalMovementCurrentRootFailure.InvalidCurrentMembership);

        // Only RootOriginal and current pointer links participate in ordinary current proof.
        // Superseded historical links remain migration/diagnostic responsibility.
        var links = candidate.LedgerLinks;
        if (links.Any(x => x.RootId != root.Id || !permanentIds.Contains(x.LineId) ||
                          !Enum.IsDefined(typeof(LogicalMovementTransformationRole), x.Role)))
            return Bad(LogicalMovementCurrentRootFailure.InvalidMovementOwnership);
        foreach (var line in permanent)
        {
            var rootLinks = links.Where(x => x.LineId == line.Id && x.MovementId == line.RootMovementId &&
                                             x.Role == (int)LogicalMovementTransformationRole.RootOriginal).ToList();
            if (rootLinks.Count != 1 || !candidate.Movements.TryGetValue(line.RootMovementId, out var rootMovement))
                return Bad(LogicalMovementCurrentRootFailure.InvalidRootOriginal);
            // RootMovementBatchId is authority only after exact original physical membership is proven.
            if (root.RootMovementBatchId is { } batchId)
            {
                if (!candidate.ExistingMovementBatchIds.Contains(batchId) || rootMovement.MovementBatchId != batchId)
                    return Bad(LogicalMovementCurrentRootFailure.InvalidRootOriginal);
            }
            else if (rootMovement.MovementBatchId is not null)
                return Bad(LogicalMovementCurrentRootFailure.InvalidRootOriginal);
        }

        var projected = new List<ValidatedLogicalMovementCurrentLine>(root.LineCount);
        foreach (var state in candidate.CurrentLines)
        {
            if (!Enum.IsDefined(typeof(LogicalMovementLineState), state.State))
                return Bad(LogicalMovementCurrentRootFailure.InvalidCurrentState);
            var lineState = (LogicalMovementLineState)state.State;
            long effective;
            long? terminal = null;
            if (lineState == LogicalMovementLineState.Active)
            {
                if (state.ResultEffectiveMovementId is null || state.LastEffectiveMovementId is not null || state.TerminalReversalMovementId is not null)
                    return Bad(LogicalMovementCurrentRootFailure.InvalidCurrentState);
                effective = state.ResultEffectiveMovementId.Value;
            }
            else
            {
                if (state.ResultEffectiveMovementId is not null || state.LastEffectiveMovementId is null || state.TerminalReversalMovementId is null)
                    return Bad(LogicalMovementCurrentRootFailure.InvalidCurrentState);
                effective = state.LastEffectiveMovementId.Value;
                terminal = state.TerminalReversalMovementId.Value;
            }
            if (!candidate.Movements.ContainsKey(effective) || (terminal is not null && !candidate.Movements.ContainsKey(terminal.Value)))
                return Bad(LogicalMovementCurrentRootFailure.InvalidMovementOwnership);
            if (links.Count(x => x.MovementId == effective && x.LineId == state.LineId && EffectiveRoles.Contains(x.Role)) != 1)
                return Bad(LogicalMovementCurrentRootFailure.InvalidTransformationRole);
            if (terminal is not null && links.Count(x => x.MovementId == terminal && x.LineId == state.LineId &&
                x.Role == (int)LogicalMovementTransformationRole.OrdinaryReversal) != 1)
                return Bad(LogicalMovementCurrentRootFailure.InvalidTransformationRole);
            if (state.Id <= 0)
                return Bad(LogicalMovementCurrentRootFailure.InvalidCurrentMembership);
            projected.Add(new(new(state.LineId), new(state.Id), permanent.Single(x => x.Id == state.LineId).RootMovementId,
                permanent.Single(x => x.Id == state.LineId).OriginalDisplayOrdinal, lineState, effective, terminal));
        }

        foreach (var link in links)
        {
            if (link.IntroducedByGenerationLineId is null || candidate.Introductions.Count(x =>
                    x.GenerationLineId == link.IntroducedByGenerationLineId && x.RootId == root.Id && x.LineId == link.LineId) != 1)
                return Bad(LogicalMovementCurrentRootFailure.InvalidIntroduction);
        }

        var ordered = new ReadOnlyCollection<ValidatedLogicalMovementCurrentLine>(projected.OrderBy(x => x.OriginalDisplayOrdinal).ToList());
        // StatusReasonCode is preserved, not interpreted into a new status or stronger reason taxonomy.
        return LogicalMovementCurrentRootResolution.Resolved(new(new(root.Id), root.RootMovementBatchId, status,
            root.StatusReasonCode, new(root.CurrentGenerationNumber.Value), ordered));
    }

    private static LogicalMovementCurrentRootResolution Bad(LogicalMovementCurrentRootFailure failure) =>
        LogicalMovementCurrentRootResolution.Unhealthy(failure);
}
