namespace BinTracker.Core;

/// <summary>
/// Stable identity of one logical movement root. A root continues across all
/// corrections, reversals and restorations of its permanent logical lines.
/// </summary>
public readonly record struct LogicalMovementBatchId(long Value);

/// <summary>
/// Stable identity of one permanent business line within a logical root.
/// </summary>
public readonly record struct LogicalMovementLineId(long Value);

/// <summary>
/// Persisted identity of one complete logical-root generation.
/// </summary>
public readonly record struct LogicalMovementGenerationId(long Value);

/// <summary>
/// Persisted identity of one permanent line's state in a specific generation.
/// Later generations use this exact identity for predecessor lineage.
/// </summary>
public readonly record struct LogicalMovementGenerationLineId(long Value);

/// <summary>
/// Root-scoped semantic mutation order and optimistic-concurrency value.
/// This is independent of movement business dates and forensic timestamps.
/// </summary>
public readonly record struct LogicalMovementGenerationNumber(int Value);

/// <summary>
/// Operational health of a persisted logical movement root.
/// </summary>
public enum LogicalMovementBatchStatus
{
    Initializing = 0,
    Active = 1,
    ReadOnly = 2,
    Invalid = 3
}

/// <summary>
/// Authoritative state represented by a complete generation-line row.
/// </summary>
public enum LogicalMovementLineState
{
    Active = 0,
    Reversed = 1
}

/// <summary>
/// Why a permanent line has its state in a particular root generation.
/// CarriedForward and AlreadyMatches intentionally remain distinct.
/// </summary>
public enum LogicalMovementGenerationAction
{
    Initial = 0,
    MigrationBaseline = 1,
    CarriedForward = 2,
    AlreadyMatches = 3,
    Corrected = 4,
    Reversed = 5,
    Restored = 6,
    RemainReversed = 7
}

/// <summary>
/// Immutable transformation role of a ledger movement within one logical line.
/// This is separate from <see cref="MovementSource"/> provenance.
/// </summary>
public enum LogicalMovementTransformationRole
{
    RootOriginal = 0,
    CorrectionNeutraliser = 1,
    CorrectionReplacement = 2,
    OrdinaryReversal = 3,
    Restoration = 4
}

/// <summary>
/// Explicit movement fields selected by an operator request. Request contracts
/// must separately distinguish an absent field, an explicit clear and a value.
/// </summary>
[Flags]
public enum MovementChangeField
{
    None = 0,
    MovementDate = 1 << 0,
    Direction = 1 << 1,
    Customer = 1 << 2,
    ContainerType = 1 << 3,
    Quantity = 1 << 4,
    Reference = 1 << 5,
    Notes = 1 << 6
}
