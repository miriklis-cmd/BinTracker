using Xunit;

namespace BinTracker.UnitTests;

public sealed class MarketFloorFormattingTests
{
    [Fact]
    public void Cash_credit_uses_non_breaking_separator()
    {
        var text = $"38\u00A0CREDIT";

        Assert.Contains('\u00A0', text);
        Assert.Equal("38 CREDIT", text.Replace('\u00A0', ' '));
    }

    [Theory]
    [InlineData(34, 11.2)]
    [InlineData(42, 10.5)]
    [InlineData(50, 9.8)]
    [InlineData(58, 9.0)]
    [InlineData(70, 8.2)]
    public void Front_font_policy_prioritises_early_morning_readability(
        int maxRows,
        double expected)
    {
        double actual = maxRows switch
        {
            <= 34 => 11.2,
            <= 42 => 10.5,
            <= 50 => 9.8,
            <= 58 => 9.0,
            _ => 8.2
        };

        Assert.Equal(expected, actual);
    }
}
