using BinTracker.Services;
using Xunit;

namespace BinTracker.UnitTests;

public sealed class BusinessInformationTests
{
    [Fact]
    public void Trading_name_is_preferred_for_display()
    {
        var value = new BusinessInformation(
            "Legal Pty Ltd",
            "Trading Name",
            "",
            "",
            "",
            "",
            "");

        Assert.Equal("Trading Name", value.DisplayName);
        Assert.Equal("Trading Name", value.ReportHeader);
    }

    [Fact]
    public void Explicit_report_header_overrides_display_name()
    {
        var value = new BusinessInformation(
            "Legal Pty Ltd",
            "Trading Name",
            "",
            "",
            "",
            "",
            "Warehouse Operations");

        Assert.Equal("Warehouse Operations", value.ReportHeader);
    }

    [Fact]
    public void Empty_business_information_falls_back_to_bintracker()
    {
        var value = new BusinessInformation("", "", "", "", "", "", "");

        Assert.Equal("BinTracker", value.DisplayName);
        Assert.Equal("BinTracker", value.ReportHeader);
    }
}
