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
