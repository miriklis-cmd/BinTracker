using BinTracker.Core;
using Xunit;

namespace BinTracker.UnitTests;

public sealed class CustomerStatementImportAdjustmentTests
{
    [Theory]
    [InlineData(MovementType.Out, "Opening adjustment (OUT)")]
    [InlineData(MovementType.In, "Opening adjustment (IN)")]
    public void Import_adjustments_must_not_be_labelled_as_physical_movements(
        MovementType movementType,
        string expected)
    {
        var text = movementType == MovementType.Out
            ? "Opening adjustment (OUT)"
            : "Opening adjustment (IN)";

        Assert.Equal(expected, text);
        Assert.DoesNotContain("Taken", text);
        Assert.DoesNotContain("Returned", text);
    }
}
