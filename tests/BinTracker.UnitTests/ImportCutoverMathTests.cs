
using Xunit;

namespace BinTracker.UnitTests;

public sealed class ImportCutoverMathTests
{
    [Theory]
    [InlineData(0, 5, 10, 15, 5, 0)]
    [InlineData(20, 12, 4, 1, -8, 15)]
    [InlineData(3, 8, 2, 6, 5, 4)]
    public void Cutover_formula_is_current_plus_adjustment_plus_out_minus_in(
        int current,
        int broughtForward,
        int outQuantity,
        int inQuantity,
        int expectedAdjustment,
        int expectedProjected)
    {
        var adjustment = broughtForward - current;
        var projected = current + adjustment + outQuantity - inQuantity;

        Assert.Equal(expectedAdjustment, adjustment);
        Assert.Equal(expectedProjected, projected);
        Assert.Equal(
            broughtForward + outQuantity - inQuantity,
            projected);
    }
}
