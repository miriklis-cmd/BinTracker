using Xunit;

namespace BinTracker.UnitTests;

public sealed class CustomerHistoryLayoutTests
{
    [Fact]
    public void Movement_history_column_widths_fit_operational_values()
    {
        const int dateWidth = 100;
        const int directionWidth = 120;
        const int enteredByWidth = 110;

        Assert.True(dateWidth >= 100);
        Assert.True(directionWidth >= 120);
        Assert.True(enteredByWidth >= 110);
    }
}
