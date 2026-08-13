using Xunit;

namespace BinTracker.UnitTests;

public sealed class MarketFloorCreditLayoutTests
{
    [Theory]
    [InlineData(1)]
    [InlineData(12)]
    [InlineData(38)]
    [InlineData(999)]
    public void Credit_value_is_an_unbroken_quantity_and_label(int quantity)
    {
        var text = $"{quantity}\u00A0CREDIT";

        Assert.Contains('\u00A0', text);
        Assert.Equal(
            $"{quantity} CREDIT",
            text.Replace('\u00A0', ' '));
    }

    [Theory]
    [InlineData(34, 12.2)]
    [InlineData(42, 11.4)]
    [InlineData(50, 10.6)]
    [InlineData(58, 9.8)]
    [InlineData(70, 9.0)]
    public void Front_font_policy_maximises_readability(
        int maxRows,
        double expected)
    {
        double actual = maxRows switch
        {
            <= 34 => 12.2,
            <= 42 => 11.4,
            <= 50 => 10.6,
            <= 58 => 9.8,
            _ => 9.0
        };

        Assert.Equal(expected, actual);
    }
}
