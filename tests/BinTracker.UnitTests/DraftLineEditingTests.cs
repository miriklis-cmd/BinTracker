using BinTracker.Services;
using Xunit;

namespace BinTracker.UnitTests;

public sealed class DraftLineEditingTests
{
    [Fact]
    public void Draft_line_can_be_replaced_in_place()
    {
        var draft = new DraftMovementBatch();
        draft.Lines.Add(new DraftMovementLine(
            1, "ALBURY", "Albury", 1, "Blue Bin", 20, null, null));

        draft.Lines[0] = new DraftMovementLine(
            1, "ALBURY", "Albury", 2, "Small Bin", 20, null, null);

        Assert.Single(draft.Lines);
        Assert.Equal("Small Bin", draft.Lines[0].ContainerType);
    }

    [Fact]
    public void Removing_line_updates_total_quantity()
    {
        var draft = new DraftMovementBatch();
        var a = new DraftMovementLine(1, "A", "A", 1, "Blue Bin", 20, null, null);
        var b = new DraftMovementLine(1, "A", "A", 3, "Yellow Bin", 5, null, null);
        draft.Lines.Add(a);
        draft.Lines.Add(b);

        draft.Lines.Remove(a);

        Assert.Equal(5, draft.TotalQuantity);
    }
}
