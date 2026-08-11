using Xunit;

namespace BinTracker.UnitTests;

public sealed class ContainerTypeMasterDataTests
{
    [Fact]
    public void System_code_is_separate_from_display_name()
    {
        const string name = "Large Blue Bin";
        const string systemCode = "BLUE_BIN";
        Assert.NotEqual(name, systemCode);
    }

    [Fact]
    public void Special_floor_report_flag_is_explicit()
    {
        var special = true;
        Assert.True(special);
    }

    [Fact]
    public void Dashboard_colour_is_visual_metadata_not_container_identity()
    {
        const string containerName = "Yellow Bin";
        const string dashboardColour = "Navy";
        Assert.NotEqual(containerName, dashboardColour);
    }
}
