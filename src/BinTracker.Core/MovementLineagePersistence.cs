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

    private static void Fail() => throw new InvalidOperationException(InvalidConstruction);
}
