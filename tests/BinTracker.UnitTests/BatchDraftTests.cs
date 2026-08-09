using BinTracker.Core;
using BinTracker.Services;
using Xunit;

namespace BinTracker.UnitTests;

public sealed class BatchDraftTests
{
    [Fact]
    public void Draft_lines_survive_when_the_same_application_state_is_reused()
    {
        var state = new ApplicationState();

        state.DraftBatch.Lines.Add(new DraftMovementLine(
            1,
            "ALBURY",
            "Albury",
            1,
            "Blue Bin",
            20,
            null,
            null));

        var laterViewOfSameState = state.DraftBatch;

        Assert.Single(laterViewOfSameState.Lines);
        Assert.Equal(20, laterViewOfSameState.TotalQuantity);
    }

    [Fact]
    public void Clearing_draft_removes_unsaved_lines()
    {
        var state = new ApplicationState();

        state.DraftBatch.Lines.Add(new DraftMovementLine(
            1,
            "ALBURY",
            "Albury",
            1,
            "Blue Bin",
            20,
            null,
            null));

        state.DraftBatch.Clear();

        Assert.Empty(state.DraftBatch.Lines);
        Assert.False(state.DraftBatch.HasLines);
    }

    [Fact]
    public void Pending_in_is_reflected_in_preview_without_changing_database_balance()
    {
        var databaseBalance = 5;

        var preview = MovementPositionMath.Apply(
            databaseBalance,
            MovementType.In,
            20);

        Assert.Equal(5, databaseBalance);
        Assert.Equal(-15, preview);
        Assert.Equal("15 CREDIT", MovementPositionMath.Format(preview));
    }

    [Fact]
    public void Pending_out_is_reflected_in_preview()
    {
        var preview = MovementPositionMath.Apply(
            5,
            MovementType.Out,
            20);

        Assert.Equal(25, preview);
        Assert.Equal("25 OUT", MovementPositionMath.Format(preview));
    }
}
