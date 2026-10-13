using BinTracker.Core;
using BinTracker.Services;
using BinTracker.WinForms;
using Xunit;

namespace BinTracker.WinForms.Tests;

public sealed class ReportDisplayedTotalsTests
{
    [Fact]
    public void Monthly_display_order_is_preserved_without_recomputing_service_totals()
    {
        var first = new MonthlySummaryReportRow(1, "A", "Alpha", 1, "Blue", 1, 3, 1);
        var second = new MonthlySummaryReportRow(2, "B", "Bravo", 1, "Blue", 1, 5, 2);
        IReadOnlyList<MonthlySummaryContainerTotal> authoritativeTotals =
            [new(1, "Blue", 1, 99, 44)];
        var source = new MonthlySummaryReportResult(
            new DateOnly(2026, 10, 1),
            new DateOnly(2026, 10, 31),
            new DateOnly(2026, 10, 13),
            [first, second],
            authoritativeTotals);

        var displayed = MonthlySummaryReportForm.BuildDisplayedResult(
            source,
            [second, first]);

        Assert.Equal([second, first], displayed.Rows);
        Assert.Same(authoritativeTotals, displayed.ContainerTotals);
    }

    [Fact]
    public void Movement_history_display_order_is_preserved_without_recomputing_service_totals()
    {
        var first = HistoryRow(1, MovementType.Out, 3);
        var second = HistoryRow(2, MovementType.In, 1);
        IReadOnlyList<MovementHistoryContainerTotal> authoritativeTotals =
            [new(1, "Blue", 1, 77, 22)];
        var source = new MovementHistoryReportResult(
            new DateOnly(2026, 10, 1),
            new DateOnly(2026, 10, 13),
            [first, second],
            authoritativeTotals);

        var displayed = MovementHistoryReportForm.BuildDisplayedResult(
            source,
            [second, first]);

        Assert.Equal([second, first], displayed.Rows);
        Assert.Same(authoritativeTotals, displayed.ContainerTotals);
    }

    private static MovementHistoryReportRow HistoryRow(
        long id,
        MovementType direction,
        int quantity) =>
        new(
            id,
            new DateOnly(2026, 10, 2),
            new DateTime(2026, 10, 2, 1, 2, 3, DateTimeKind.Utc),
            1,
            "A",
            "Alpha",
            CustomerType.Account,
            1,
            "Blue",
            1,
            direction,
            quantity,
            MovementSource.Manual,
            "reference",
            "notes",
            "operator",
            null,
            null,
            string.Empty,
            string.Empty);
}
