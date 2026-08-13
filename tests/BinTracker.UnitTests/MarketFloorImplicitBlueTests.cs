using Xunit;

namespace BinTracker.UnitTests;

public sealed class MarketFloorImplicitBlueTests
{
    [Theory]
    [InlineData("CLAMMS", "Blue", "CLAMMS")]
    [InlineData("CLAMMS", "Yellow", "CLAMMS (Yellow)")]
    [InlineData("CLAMMS", "Bulk", "CLAMMS (Bulk)")]
    public void Blue_is_implicit_nonstandard_bins_are_explicit(
        string buyer,
        string container,
        string expected)
    {
        var label =
            container.Equals(
                "Blue",
                StringComparison.OrdinalIgnoreCase)
                ? buyer
                : $"{buyer} ({container})";

        Assert.Equal(expected, label);
    }

    [Theory]
    [InlineData("Blue Bin", true)]
    [InlineData("Yellow Bin", true)]
    [InlineData("Bulk Bin", true)]
    [InlineData("CHEP", false)]
    [InlineData("LOSCAM", false)]
    public void Operational_floor_bins_are_not_special_pool_containers(
        string name,
        bool expected)
    {
        var operational =
            name.Equals("Blue Bin", StringComparison.OrdinalIgnoreCase) ||
            name.Equals("Yellow Bin", StringComparison.OrdinalIgnoreCase) ||
            name.Equals("Bulk Bin", StringComparison.OrdinalIgnoreCase);

        Assert.Equal(expected, operational);
    }
}
