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
}
