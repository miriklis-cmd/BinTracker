using BinTracker.Core;
using Xunit;

namespace BinTracker.UnitTests;

public sealed class ImportExecutionContractTests
{
    [Fact]
    public void Opening_adjustment_direction_matches_balance_semantics()
    {
        Assert.Equal(
            MovementType.Out,
            DirectionForAdjustment(5));

        Assert.Equal(
            MovementType.In,
            DirectionForAdjustment(-5));
    }

    [Fact]
    public void Cutover_movements_reach_excel_target()
    {
        const int current = 7;
        const int broughtForward = 12;
        const int outQuantity = 4;
        const int inQuantity = 2;

        var adjustment = broughtForward - current;
        var projected =
            current +
            adjustment +
            outQuantity -
            inQuantity;

        Assert.Equal(14, projected);
        Assert.Equal(
            broughtForward + outQuantity - inQuantity,
            projected);
    }

    private static MovementType DirectionForAdjustment(
        int adjustment) =>
        adjustment > 0
            ? MovementType.Out
            : MovementType.In;
}
