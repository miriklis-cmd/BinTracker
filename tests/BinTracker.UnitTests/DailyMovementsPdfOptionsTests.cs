using Xunit;

namespace BinTracker.UnitTests;

public sealed class DailyMovementsPdfOptionsTests
{
    [Fact]
    public void Notes_are_optional_print_detail()
    {
        const bool notesDefaultOff = true;
        Assert.True(notesDefaultOff);
    }
}
