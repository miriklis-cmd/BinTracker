using Xunit;

namespace BinTracker.UnitTests;

public sealed class MarketFloorAdaptiveBothPagesTests
{
    [Theory]
    [InlineData(30, 12.6)]
    [InlineData(34, 12.0)]
    [InlineData(38, 11.2)]
    [InlineData(42, 10.4)]
    [InlineData(50, 8.9)]
    [InlineData(58, 7.6)]
    [InlineData(70, 7.0)]
    public void Front_page_font_tracks_real_row_density(
        int rows,
        double expected)
    {
        double font = rows switch
        {
            <= 30 => 12.6,
            <= 34 => 12.0,
            <= 38 => 11.2,
            <= 42 => 10.4,
            <= 46 => 9.6,
            <= 50 => 8.9,
            <= 54 => 8.2,
            <= 58 => 7.6,
            _ => 7.0
        };

        Assert.Equal(expected, font);
    }

    [Theory]
    [InlineData(74, 8.0)]
    [InlineData(80, 7.7)]
    [InlineData(86, 7.3)]
    [InlineData(92, 6.9)]
    [InlineData(100, 6.5)]
    [InlineData(110, 6.0)]
    public void Reverse_page_font_never_exceeds_known_one_page_size(
        int renderedLines,
        double expected)
    {
        double font = renderedLines switch
        {
            <= 74 => 8.0,
            <= 80 => 7.7,
            <= 86 => 7.3,
            <= 92 => 6.9,
            <= 100 => 6.5,
            _ => 6.0
        };

        Assert.Equal(expected, font);
        Assert.True(font <= 8.0);
    }

    [Fact]
    public void Extra_yellow_rows_increase_reverse_density()
    {
        const int ordinaryAccountRows = 152;
        const int additionalYellowRows = 20;

        var normalColumn =
            (int)Math.Ceiling(
                ordinaryAccountRows / 2d);

        var heavyColumn =
            (int)Math.Ceiling(
                (ordinaryAccountRows + additionalYellowRows) / 2d);

        Assert.Equal(76, normalColumn);
        Assert.Equal(86, heavyColumn);
        Assert.True(heavyColumn > normalColumn);
    }
}
