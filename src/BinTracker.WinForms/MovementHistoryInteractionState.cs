using BinTracker.Services;

namespace BinTracker.WinForms;

internal enum MovementHistoryDateBoundary
{
    From,
    To
}

internal readonly record struct MovementHistoryDateRange(DateOnly From, DateOnly To)
{
    public static MovementHistoryDateRange FromPickerValues(DateTime from, DateTime to) =>
        new(DateOnly.FromDateTime(from.Date), DateOnly.FromDateTime(to.Date));

    public MovementHistoryDateRange NormalizeAfterChange(MovementHistoryDateBoundary changedBoundary)
    {
        if (From <= To)
            return this;

        return changedBoundary == MovementHistoryDateBoundary.From
            ? new(To, To)
            : new(From, From);
    }
}

internal readonly record struct MovementHistoryRefreshRequest(
    long Sequence,
    MovementHistoryReportQuery Query);

internal sealed class MovementHistoryRefreshCoordinator
{
    private long latestSequence;

    public MovementHistoryRefreshRequest Begin(MovementHistoryReportQuery query) =>
        new(++latestSequence, query);

    public bool IsLatest(MovementHistoryRefreshRequest request) =>
        request.Sequence == latestSequence;

    public bool MatchesAppliedRange(
        MovementHistoryRefreshRequest request,
        MovementHistoryReportResult result) =>
        request.Query.StartDate == result.StartDate &&
        request.Query.EndDate == result.EndDate;
}

internal sealed class MovementHistoryMutationRefreshState
{
    private bool refreshRequired;

    public void RecordDisposition(LogicalMovementMutationResultKind kind)
    {
        if (kind is LogicalMovementMutationResultKind.Committed or
            LogicalMovementMutationResultKind.Replayed)
        {
            refreshRequired = true;
        }
    }

    public void RecordCorrectionCommitted() => refreshRequired = true;

    public bool TakeRefreshRequirement()
    {
        var required = refreshRequired;
        refreshRequired = false;
        return required;
    }
}
