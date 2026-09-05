using System.Collections.ObjectModel;

namespace BinTracker.Core;

/// <summary>Identifies which authoritative domain supplied one operational movement.</summary>
public enum OperationalMovementDomain
{
    LineageOrdinary = 0,
    Adjustment = 1,
    ExcelImport = 2
}

/// <summary>
/// Declares both the conservative validation scope and the final operational filter.
/// A null boundary or dimension means unbounded.
/// </summary>
public sealed class OperationalMovementProjectionScope
{
    private OperationalMovementProjectionScope(DateOnly? fromDateInclusive, DateOnly? throughDateInclusive,
        int? customerId, int? containerTypeId, bool isPositionAsOf)
    {
        if (fromDateInclusive is not null && throughDateInclusive is not null &&
            fromDateInclusive > throughDateInclusive)
            throw new ArgumentException("The movement date range is invalid.");
        if (customerId <= 0) throw new ArgumentOutOfRangeException(nameof(customerId));
        if (containerTypeId <= 0) throw new ArgumentOutOfRangeException(nameof(containerTypeId));

        FromDateInclusive = fromDateInclusive;
        ThroughDateInclusive = throughDateInclusive;
        CustomerId = customerId;
        ContainerTypeId = containerTypeId;
        IsPositionAsOf = isPositionAsOf;
    }

    public DateOnly? FromDateInclusive { get; }
    public DateOnly? ThroughDateInclusive { get; }
    public int? CustomerId { get; }
    public int? ContainerTypeId { get; }
    public bool IsPositionAsOf { get; }

    public static OperationalMovementProjectionScope All(
        int? customerId = null, int? containerTypeId = null) =>
        new(null, null, customerId, containerTypeId, false);

    public static OperationalMovementProjectionScope Activity(DateOnly fromDateInclusive,
        DateOnly throughDateInclusive, int? customerId = null, int? containerTypeId = null) =>
        new(fromDateInclusive, throughDateInclusive, customerId, containerTypeId, false);

    public static OperationalMovementProjectionScope PositionAsOf(DateOnly date,
        int? customerId = null, int? containerTypeId = null) =>
        new(null, date, customerId, containerTypeId, true);
}

/// <summary>One immutable operational contribution backed by persisted movement evidence.</summary>
public sealed class ProjectedOperationalMovement
{
    internal ProjectedOperationalMovement(long evidenceMovementId, OperationalMovementDomain domain,
        LogicalMovementBatchId? logicalRootId, LogicalMovementLineId? logicalLineId,
        LogicalMovementGenerationNumber? currentGeneration, DateOnly movementDate,
        int customerId, int containerTypeId, MovementType movementType, int quantity,
        MovementSource source, DateTime createdUtc, string? referenceNumber, string? notes,
        string? createdBy)
    {
        EvidenceMovementId = evidenceMovementId;
        Domain = domain;
        LogicalRootId = logicalRootId;
        LogicalLineId = logicalLineId;
        CurrentGeneration = currentGeneration;
        MovementDate = movementDate;
        CustomerId = customerId;
        ContainerTypeId = containerTypeId;
        MovementType = movementType;
        Quantity = quantity;
        Source = source;
        CreatedUtc = createdUtc;
        ReferenceNumber = referenceNumber;
        Notes = notes;
        CreatedBy = createdBy;
    }

    public long EvidenceMovementId { get; }
    public OperationalMovementDomain Domain { get; }
    public LogicalMovementBatchId? LogicalRootId { get; }
    public LogicalMovementLineId? LogicalLineId { get; }
    public LogicalMovementGenerationNumber? CurrentGeneration { get; }
    public DateOnly MovementDate { get; }
    public int CustomerId { get; }
    public int ContainerTypeId { get; }
    public MovementType MovementType { get; }
    public int Quantity { get; }
    public MovementSource Source { get; }
    public DateTime CreatedUtc { get; }
    public string? ReferenceNumber { get; }
    public string? Notes { get; }
    public string? CreatedBy { get; }
    public long SignedQuantity => MovementType == MovementType.Out ? Quantity : -Quantity;
}

public sealed record OperationalMovementPosition(int CustomerId, int ContainerTypeId, long Quantity);

/// <summary>
/// A complete immutable result whose activity and signed positions were derived from the same
/// validated provider snapshot. PositionAsOf requests use the same Activity stream through D.
/// </summary>
public sealed class OperationalMovementProjectionResult
{
    internal OperationalMovementProjectionResult(OperationalMovementProjectionScope scope,
        IReadOnlyList<ProjectedOperationalMovement> activity,
        IReadOnlyList<OperationalMovementPosition> positions)
    {
        Scope = scope;
        Activity = activity;
        Positions = positions;
    }

    public OperationalMovementProjectionScope Scope { get; }
    public IReadOnlyList<ProjectedOperationalMovement> Activity { get; }
    /// <summary>Populated only for a <see cref="OperationalMovementProjectionScope.PositionAsOf"/> request.</summary>
    public IReadOnlyList<OperationalMovementPosition> Positions { get; }
}

public enum OperationalMovementProjectionFailure
{
    SchemaUnavailable = 0,
    RelevantLineageInvalid = 1,
    UnknownRelevance = 2,
    UnexpectedUnrootedOrdinary = 3,
    InvalidExcludedDomain = 4,
    DuplicateCurrentContribution = 5
}

public sealed class OperationalMovementProjectionException(
    OperationalMovementProjectionFailure failure, string message, Exception? innerException = null)
    : InvalidOperationException(message, innerException)
{
    public OperationalMovementProjectionFailure Failure { get; } = failure;
}

public interface IOperationalMovementProjectionAuthority
{
    Task<OperationalMovementProjectionResult> QueryAsync(
        OperationalMovementProjectionScope scope,
        CancellationToken cancellationToken = default);
}

internal sealed record OperationalMovementFact(long EvidenceMovementId, DateOnly MovementDate,
    MovementType MovementType, MovementSource Source, int CustomerId, int ContainerTypeId,
    int Quantity, string? ReferenceNumber, string? Notes, string? CreatedBy, DateTime CreatedUtc,
    int? MovementBatchId, long? ImportRunId, long? ReversesMovementId);

internal readonly record struct OperationalMovementInfluence(
    DateOnly? MovementDate, int? CustomerId, int? ContainerTypeId);

internal static class OperationalMovementProjectionSemantics
{
    internal static bool IsRelevant(OperationalMovementProjectionScope scope,
        IReadOnlyCollection<OperationalMovementInfluence> influences)
    {
        if (influences.Count == 0) return true;
        if (scope.CustomerId is { } customer &&
            influences.All(x => x.CustomerId is not null && x.CustomerId != customer))
            return false;
        if (scope.ContainerTypeId is { } container &&
            influences.All(x => x.ContainerTypeId is not null && x.ContainerTypeId != container))
            return false;
        if ((scope.FromDateInclusive is not null || scope.ThroughDateInclusive is not null) &&
            influences.All(x => x.MovementDate is not null && !IncludesDate(scope, x.MovementDate.Value)))
            return false;
        return true;
    }

    internal static IReadOnlyList<ProjectedOperationalMovement> ProjectLineageRoot(
        ValidatedLogicalMovementCurrentRoot root,
        IReadOnlyDictionary<long, OperationalMovementFact> movementFacts)
    {
        var planningLines = new List<TrustedMovementPlanningLine>(root.Lines.Count);
        foreach (var line in root.Lines)
        {
            if (line.EffectiveMovementId is not { } effectiveId ||
                !movementFacts.TryGetValue(effectiveId, out var effective) ||
                line.TerminalReversalMovementId is { } terminalId &&
                !movementFacts.TryGetValue(terminalId, out _))
                throw InvalidLineage("A current lineage movement fact is missing.");

            var terminal = line.TerminalReversalMovementId is { } reversalId
                ? movementFacts[reversalId]
                : null;
            planningLines.Add(new(line, BusinessState(effective),
                terminal is null ? null : BusinessState(terminal)));
        }

        try
        {
            MovementMutationPlanner.ValidateCurrentProjectionFacts(root, planningLines);
        }
        catch (InvalidOperationException exception)
        {
            throw InvalidLineage(exception.Message, exception);
        }

        var result = new List<ProjectedOperationalMovement>();
        foreach (var line in root.Lines)
        {
            result.Add(Project(movementFacts[line.EffectiveMovementId!.Value],
                OperationalMovementDomain.LineageOrdinary, root, line.Id));
            if (line.State == LogicalMovementLineState.Reversed)
                result.Add(Project(movementFacts[line.TerminalReversalMovementId!.Value],
                    OperationalMovementDomain.LineageOrdinary, root, line.Id));
        }
        return result;
    }

    internal static ProjectedOperationalMovement ProjectExcluded(OperationalMovementFact fact)
    {
        var domain = fact.Source switch
        {
            MovementSource.Adjustment => OperationalMovementDomain.Adjustment,
            MovementSource.ExcelImport => OperationalMovementDomain.ExcelImport,
            _ => throw new OperationalMovementProjectionException(
                OperationalMovementProjectionFailure.InvalidExcludedDomain,
                "Only Adjustment and ExcelImport movements may bypass generic lineage.")
        };
        return Project(fact, domain, null, null);
    }

    internal static OperationalMovementProjectionResult Complete(
        OperationalMovementProjectionScope scope,
        IEnumerable<ProjectedOperationalMovement> projected)
    {
        var candidates = projected.ToList();
        if (candidates.Select(x => x.EvidenceMovementId).Distinct().Count() != candidates.Count)
            throw new OperationalMovementProjectionException(
                OperationalMovementProjectionFailure.DuplicateCurrentContribution,
                "A movement evidence identity would contribute more than once.");

        var filtered = candidates.Where(x => Includes(scope, x))
            .OrderBy(x => x.MovementDate).ThenBy(x => x.EvidenceMovementId).ToList();
        var positions = scope.IsPositionAsOf
            ? filtered.GroupBy(x => new { x.CustomerId, x.ContainerTypeId })
                .Select(x => new OperationalMovementPosition(x.Key.CustomerId, x.Key.ContainerTypeId,
                    x.Sum(movement => movement.SignedQuantity)))
                .OrderBy(x => x.CustomerId).ThenBy(x => x.ContainerTypeId).ToList()
            : [];
        return new(scope, new ReadOnlyCollection<ProjectedOperationalMovement>(filtered),
            new ReadOnlyCollection<OperationalMovementPosition>(positions));
    }

    internal static bool Includes(OperationalMovementProjectionScope scope,
        OperationalMovementFact fact) =>
        (scope.CustomerId is null || fact.CustomerId == scope.CustomerId) &&
        (scope.ContainerTypeId is null || fact.ContainerTypeId == scope.ContainerTypeId) &&
        IncludesDate(scope, fact.MovementDate);

    private static bool Includes(OperationalMovementProjectionScope scope,
        ProjectedOperationalMovement fact) =>
        (scope.CustomerId is null || fact.CustomerId == scope.CustomerId) &&
        (scope.ContainerTypeId is null || fact.ContainerTypeId == scope.ContainerTypeId) &&
        IncludesDate(scope, fact.MovementDate);

    private static bool IncludesDate(OperationalMovementProjectionScope scope, DateOnly date) =>
        (scope.FromDateInclusive is null || date >= scope.FromDateInclusive) &&
        (scope.ThroughDateInclusive is null || date <= scope.ThroughDateInclusive);

    private static MovementBusinessState BusinessState(OperationalMovementFact fact) => new(
        fact.EvidenceMovementId, fact.MovementDate, fact.MovementType, fact.Source,
        fact.CustomerId, fact.ContainerTypeId, fact.Quantity, fact.ReferenceNumber, fact.Notes,
        fact.MovementBatchId, fact.ImportRunId, fact.ReversesMovementId);

    private static ProjectedOperationalMovement Project(OperationalMovementFact fact,
        OperationalMovementDomain domain, ValidatedLogicalMovementCurrentRoot? root,
        LogicalMovementLineId? lineId) => new(fact.EvidenceMovementId, domain, root?.Id,
            lineId, root?.CurrentGenerationNumber, fact.MovementDate, fact.CustomerId,
            fact.ContainerTypeId, fact.MovementType, fact.Quantity, fact.Source,
            fact.CreatedUtc, fact.ReferenceNumber, fact.Notes, fact.CreatedBy);

    private static OperationalMovementProjectionException InvalidLineage(string message,
        Exception? inner = null) => new(OperationalMovementProjectionFailure.RelevantLineageInvalid,
            message, inner);
}
