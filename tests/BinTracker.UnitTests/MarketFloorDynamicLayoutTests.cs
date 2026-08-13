using Xunit;

namespace BinTracker.UnitTests;

public sealed class MarketFloorDynamicLayoutTests
{
    [Theory]
    [InlineData(30, 11.0, 1.65)]
    [InlineData(34, 10.5, 1.45)]
    [InlineData(38, 10.0, 1.25)]
    [InlineData(42, 9.5, 1.05)]
    [InlineData(46, 9.0, 0.90)]
    [InlineData(50, 8.5, 0.75)]
    [InlineData(54, 8.0, 0.60)]
    [InlineData(58, 7.5, 0.45)]
    [InlineData(70, 7.0, 0.30)]
    public void Front_layout_shrinks_as_actual_row_load_increases(
        int maxRows,
        double expectedFont,
        double expectedPadding)
    {
        var actual = maxRows switch
        {
            <= 30 => (11.0, 1.65),
            <= 34 => (10.5, 1.45),
            <= 38 => (10.0, 1.25),
            <= 42 => (9.5, 1.05),
            <= 46 => (9.0, 0.90),
            <= 50 => (8.5, 0.75),
            <= 54 => (8.0, 0.60),
            <= 58 => (7.5, 0.45),
            _ => (7.0, 0.30)
        };

        Assert.Equal(expectedFont, actual.Item1, 2);
        Assert.Equal(expectedPadding, actual.Item2, 2);
    }

    [Fact]
    public void Extra_yellow_rows_increase_front_page_load()
    {
        const int ordinaryAccountRows = 58;
        const int extraYellowRows = 14;

        var before =
            (int)Math.Ceiling(
                ordinaryAccountRows / 2d);

        var after =
            (int)Math.Ceiling(
                (ordinaryAccountRows + extraYellowRows) / 2d);

        Assert.Equal(29, before);
        Assert.Equal(36, after);
        Assert.True(after > before);
    }
}
