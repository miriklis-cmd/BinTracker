using BinTracker.Services;
using BinTracker.WinForms;
using Xunit;

namespace BinTracker.WinForms.Tests;

public sealed class MovementHistoryInteractionStateTests
{
    [Fact]
    public void Changing_to_preserves_a_valid_from_date()
    {
        var range = new MovementHistoryDateRange(
            new DateOnly(2026, 8, 28), new DateOnly(2026, 9, 4));

        var normalized = range.NormalizeAfterChange(MovementHistoryDateBoundary.To);

        Assert.Equal(new DateOnly(2026, 8, 28), normalized.From);
        Assert.Equal(new DateOnly(2026, 9, 4), normalized.To);
    }

    [Fact]
    public void Changing_from_preserves_a_valid_to_date()
    {
        var range = new MovementHistoryDateRange(
            new DateOnly(2026, 8, 20), new DateOnly(2026, 8, 31));

        var normalized = range.NormalizeAfterChange(MovementHistoryDateBoundary.From);

        Assert.Equal(new DateOnly(2026, 8, 20), normalized.From);
        Assert.Equal(new DateOnly(2026, 8, 31), normalized.To);
    }

    [Fact]
    public void Invalid_from_change_clamps_from_without_moving_to()
    {
        var range = new MovementHistoryDateRange(
            new DateOnly(2026, 8, 31), new DateOnly(2026, 8, 30));

        var normalized = range.NormalizeAfterChange(MovementHistoryDateBoundary.From);

        Assert.Equal(new DateOnly(2026, 8, 30), normalized.From);
        Assert.Equal(new DateOnly(2026, 8, 30), normalized.To);
    }

    [Fact]
    public void Invalid_to_change_clamps_to_without_moving_from()
    {
        var range = new MovementHistoryDateRange(
            new DateOnly(2026, 8, 30), new DateOnly(2026, 8, 29));

        var normalized = range.NormalizeAfterChange(MovementHistoryDateBoundary.To);

        Assert.Equal(new DateOnly(2026, 8, 30), normalized.From);
        Assert.Equal(new DateOnly(2026, 8, 30), normalized.To);
    }

    [Fact]
    public void Applied_report_range_must_match_the_final_captured_picker_range()
    {
        var coordinator = new MovementHistoryRefreshCoordinator();
        var query = Query(new DateOnly(2026, 8, 28), new DateOnly(2026, 9, 4));
        var request = coordinator.Begin(query);
        var result = Result(query.StartDate, query.EndDate);

        Assert.True(coordinator.IsLatest(request));
        Assert.True(coordinator.MatchesAppliedRange(request, result));
        Assert.False(coordinator.MatchesAppliedRange(
            request, Result(new DateOnly(2026, 8, 1), query.EndDate)));
    }

    [Fact]
    public void Stale_refresh_cannot_overwrite_a_newer_date_selection()
    {
        var coordinator = new MovementHistoryRefreshCoordinator();
        var stale = coordinator.Begin(Query(
            new DateOnly(2026, 8, 1), new DateOnly(2026, 8, 31)));
        var current = coordinator.Begin(Query(
            new DateOnly(2026, 8, 28), new DateOnly(2026, 9, 4)));

        Assert.False(coordinator.IsLatest(stale));
        Assert.True(coordinator.IsLatest(current));
    }

    [Fact]
    public void Committed_restore_requires_refresh_before_a_later_cancel()
    {
        var state = new MovementHistoryMutationRefreshState();

        state.RecordDisposition(LogicalMovementMutationResultKind.Committed);

        Assert.True(state.TakeRefreshRequirement());
        Assert.False(state.TakeRefreshRequirement());
    }

    [Fact]
    public void Cancel_before_restore_has_no_write_and_no_refresh_requirement()
    {
        var state = new MovementHistoryMutationRefreshState();

        Assert.False(state.TakeRefreshRequirement());
    }

    [Fact]
    public void Successful_later_correction_requires_its_own_refresh()
    {
        var state = new MovementHistoryMutationRefreshState();
        state.RecordDisposition(LogicalMovementMutationResultKind.Committed);
        Assert.True(state.TakeRefreshRequirement());

        state.RecordCorrectionCommitted();

        Assert.True(state.TakeRefreshRequirement());
    }

    private static MovementHistoryReportQuery Query(DateOnly from, DateOnly to) =>
        new(from, to);

    private static MovementHistoryReportResult Result(DateOnly from, DateOnly to) =>
        new(from, to, [], []);
}
