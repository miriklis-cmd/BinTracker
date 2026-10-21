using BinTracker.Core;
using BinTracker.Services;
using Xunit;

namespace BinTracker.UnitTests;

public sealed class SingleEntryMovementTests
{
    [Fact]
    public void Returned_movement_reduces_position()
    {
        Assert.Equal(7, MovementPositionMath.Apply(10, MovementType.In, 3));
    }

    [Fact]
    public void Taken_movement_increases_position()
    {
        Assert.Equal(13, MovementPositionMath.Apply(10, MovementType.Out, 3));
    }

    [Fact]
    public void Credit_preview_formats_consistently()
    {
        Assert.Equal("2 CREDIT", MovementPositionMath.Format(-2));
    }

    [Fact]
    public void Boundary_position_is_exact_and_overflow_fails_closed()
    {
        Assert.Equal(int.MaxValue,
            MovementPositionMath.Apply(int.MaxValue - 1, MovementType.Out, 1));
        Assert.Equal(int.MinValue,
            MovementPositionMath.Apply(int.MinValue + 1, MovementType.In, 1));
        Assert.Throws<OverflowException>(() =>
            MovementPositionMath.Apply(int.MaxValue, MovementType.Out, 1));
        Assert.Throws<OverflowException>(() =>
            MovementPositionMath.Apply(int.MinValue, MovementType.In, 1));
        Assert.Throws<OverflowException>(() => MovementPositionMath.Format(int.MinValue));
    }

    [Fact]
    public void Undefined_direction_is_rejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            MovementPositionMath.Apply(0, (MovementType)99, 1));
    }

    [Fact]
    public void Signed_sum_allows_both_int_boundary_cancellations_and_rejects_final_overflow()
    {
        Assert.Equal(int.MaxValue,
            QuantityMath.SumToInt32(int.MaxValue, 1L, -1L));
        Assert.Equal(int.MinValue,
            QuantityMath.SumToInt32(int.MinValue, -1L, 1L));
        Assert.Throws<OverflowException>(() =>
            QuantityMath.SumToInt32(int.MaxValue, 1L));
    }
}
