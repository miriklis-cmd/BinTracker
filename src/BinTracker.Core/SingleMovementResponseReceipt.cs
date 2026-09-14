namespace BinTracker.Core;

/// <summary>
/// Immutable evidence of the exact response returned by a successful native
/// Single Entry command. Operational projection remains the balance authority.
/// </summary>
public sealed class SingleMovementResponseReceipt
{
    public SingleMovementResponseReceipt(
        Guid clientOperationId,
        long movementId,
        DateOnly businessDate,
        int resultingPosition)
    {
        if (clientOperationId == Guid.Empty)
            throw new ArgumentException("Client operation ID is required.", nameof(clientOperationId));
        if (movementId <= 0)
            throw new ArgumentOutOfRangeException(nameof(movementId));

        ClientOperationId = clientOperationId;
        MovementId = movementId;
        BusinessDate = businessDate;
        ResultingPosition = resultingPosition;
    }

    public Guid ClientOperationId { get; }
    public long MovementId { get; }
    public DateOnly BusinessDate { get; }
    public int ResultingPosition { get; }
}
