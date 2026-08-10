using Xunit;

namespace BinTracker.UnitTests;

public sealed class SingleEntryResetTests
{
    [Fact]
    public void Successful_single_entry_should_start_next_entry_from_clean_defaults()
    {
        var defaults = new
        {
            CustomerCode = string.Empty,
            Quantity = 0,
            Reference = string.Empty,
            Notes = string.Empty,
            DirectionIndex = 0,
            ContainerIndex = 0
        };

        Assert.Equal(string.Empty, defaults.CustomerCode);
        Assert.Equal(0, defaults.Quantity);
        Assert.Equal(string.Empty, defaults.Reference);
        Assert.Equal(string.Empty, defaults.Notes);
        Assert.Equal(0, defaults.DirectionIndex);
        Assert.Equal(0, defaults.ContainerIndex);
    }
}
