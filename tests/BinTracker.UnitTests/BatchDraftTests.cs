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
    [Fact]
    public void File_store_restores_draft_after_new_application_state_is_created()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"BinTrackerTests-{Guid.NewGuid():N}");
        var path = Path.Combine(directory, "batch-entry-draft.json");

        try
        {
            var firstStore = new FileBatchDraftStore(path);
            var firstState = new ApplicationState(firstStore);
            firstState.DraftBatch.MovementDate = new DateOnly(2026, 8, 18);
            firstState.DraftBatch.MovementType = MovementType.Out;
            firstState.DraftBatch.Lines.Add(new DraftMovementLine(
                1, "ALBURY", "Albury", 1, "Blue Bin", 20, "REF", "Notes"));
            firstState.PersistDraft();

            var restoredState = new ApplicationState(new FileBatchDraftStore(path));

            Assert.True(restoredState.DraftBatch.HasLines);
            Assert.Equal(new DateOnly(2026, 8, 18), restoredState.DraftBatch.MovementDate);
            Assert.Equal(MovementType.Out, restoredState.DraftBatch.MovementType);
            var line = Assert.Single(restoredState.DraftBatch.Lines);
            Assert.Equal("ALBURY", line.CustomerCode);
            Assert.Equal(20, line.Quantity);
            Assert.Equal("REF", line.Reference);
        }
        finally
        {
            if (Directory.Exists(directory))
                Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void Clearing_persisted_draft_removes_recovery_file()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"BinTrackerTests-{Guid.NewGuid():N}");
        var path = Path.Combine(directory, "batch-entry-draft.json");

        try
        {
            var state = new ApplicationState(new FileBatchDraftStore(path));
            state.DraftBatch.Lines.Add(new DraftMovementLine(
                1, "ALBURY", "Albury", 1, "Blue Bin", 20, null, null));
            state.PersistDraft();
            Assert.True(File.Exists(path));

            state.ClearDraft();

            Assert.False(File.Exists(path));
            Assert.False(state.DraftBatch.HasLines);
        }
        finally
        {
            if (Directory.Exists(directory))
                Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void Persisted_draft_sets_recovery_prompt_pending_until_handled()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"BinTrackerTests-{Guid.NewGuid():N}");
        var path = Path.Combine(directory, "batch-entry-draft.json");

        try
        {
            var first = new ApplicationState(new FileBatchDraftStore(path));
            first.DraftBatch.Lines.Add(new DraftMovementLine(
                1, "ALBURY", "Albury", 1, "Blue Bin", 3, null, null));
            first.PersistDraft();

            var restored = new ApplicationState(new FileBatchDraftStore(path));

            Assert.True(restored.RecoveryPromptPending);
            Assert.NotNull(restored.RecoveryDraftLastSavedAtUtc);
            restored.MarkRecoveryPromptHandled();
            Assert.False(restored.RecoveryPromptPending);
            Assert.True(restored.DraftBatch.HasLines);
        }
        finally
        {
            if (Directory.Exists(directory))
                Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void Draft_created_in_current_process_does_not_set_recovery_prompt_pending()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"BinTrackerTests-{Guid.NewGuid():N}");
        var path = Path.Combine(directory, "batch-entry-draft.json");

        try
        {
            var state = new ApplicationState(new FileBatchDraftStore(path));
            state.DraftBatch.Lines.Add(new DraftMovementLine(
                1, "ALBURY", "Albury", 1, "Blue Bin", 3, null, null));
            state.PersistDraft();

            Assert.False(state.RecoveryPromptPending);
        }
        finally
        {
            if (Directory.Exists(directory))
                Directory.Delete(directory, recursive: true);
        }
    }

}
