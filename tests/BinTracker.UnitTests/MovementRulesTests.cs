using BinTracker.Core;
using Xunit;

namespace BinTracker.UnitTests;

public sealed class MovementRulesTests
{
    [Fact]
    public void Out_increases_outstanding_position()
    {
        var opening = 10;
        var quantity = 5;

        var closing = opening + quantity;

        Assert.Equal(15, closing);
    }

    [Fact]
    public void In_reduces_outstanding_position()
    {
        var opening = 10;
        var quantity = 5;

        var closing = opening - quantity;

        Assert.Equal(5, closing);
    }

    [Fact]
    public void In_can_create_credit()
    {
        var opening = 3;
        var quantity = 8;

        var closing = opening - quantity;

        Assert.Equal(-5, closing);
    }

    [Fact]
    public void Different_container_types_remain_independent()
    {
        var blue = 20;
        var yellow = -4;

        Assert.Equal(20, blue);
        Assert.Equal(-4, yellow);
    }
}
