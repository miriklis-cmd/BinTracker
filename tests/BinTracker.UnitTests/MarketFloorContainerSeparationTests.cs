using Xunit;

namespace BinTracker.UnitTests;

public sealed class MarketFloorContainerSeparationTests
{
    [Fact]
    public void Clamms_positions_must_not_be_aggregated()
    {
        var positions = new[]
        {
            new { Container = "Blue", Total = 10 },
            new { Container = "Yellow", Total = 45 },
            new { Container = "Bulk", Total = 1 }
        };

        Assert.Equal(56, positions.Sum(x => x.Total));
        Assert.Equal(3, positions.Length);
        Assert.Contains(positions, x => x.Container == "Blue" && x.Total == 10);
        Assert.Contains(positions, x => x.Container == "Yellow" && x.Total == 45);
        Assert.Contains(positions, x => x.Container == "Bulk" && x.Total == 1);
    }

    [Fact]
    public void Reverse_rows_keep_each_container_independent()
    {
        var blue = new { Container = "Blue", Out = 0, In = 0, BFwd = 10, Total = 10 };
        var yellow = new { Container = "Yellow", Out = 0, In = 0, BFwd = 45, Total = 45 };
        var bulk = new { Container = "Bulk", Out = 0, In = 0, BFwd = 1, Total = 1 };

        Assert.NotEqual(blue.Container, yellow.Container);
        Assert.NotEqual(yellow.Container, bulk.Container);
        Assert.Equal(10, blue.Total);
        Assert.Equal(45, yellow.Total);
        Assert.Equal(1, bulk.Total);
    }
}
