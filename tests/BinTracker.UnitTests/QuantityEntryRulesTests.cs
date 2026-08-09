using Xunit;

namespace BinTracker.UnitTests;

public sealed class QuantityEntryRulesTests
{
    [Fact]
    public void Zero_quantity_is_invalid()
    {
        Assert.False(IsValid(0));
    }

    [Fact]
    public void Positive_quantity_is_valid()
    {
        Assert.True(IsValid(1));
        Assert.True(IsValid(20));
    }

    private static bool IsValid(int quantity) => quantity > 0;
}
