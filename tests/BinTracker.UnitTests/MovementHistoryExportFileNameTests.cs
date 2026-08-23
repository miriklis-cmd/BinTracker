using BinTracker.Core;
using BinTracker.Services;
using Xunit;

namespace BinTracker.UnitTests;

public sealed class MovementHistoryExportFileNameTests
{
    [Fact]
    public void Filter_resolving_one_customer_adds_stable_sanitized_code()
    {
        var result = Result(Row(7, "TE:ST/01"));

        var name = MovementHistoryExportFileName.Build(result, true, "pdf");

        Assert.Equal(
            "BinTracker_Movement_History_TE_ST_01_20260725_20260823.pdf",
            name);
    }

    [Fact]
    public void Unfiltered_result_keeps_generic_name_even_with_one_customer()
    {
        var name = MovementHistoryExportFileName.Build(
            Result(Row(7, "TEST")),
            false,
            ".csv");

        Assert.Equal(
            "BinTracker_Movement_History_20260725_20260823.csv",
            name);
    }

    [Fact]
    public void Filter_resolving_multiple_customers_keeps_generic_name()
    {
        var name = MovementHistoryExportFileName.Build(
            Result(Row(7, "TEST"), Row(8, "SECOND")),
            true,
            "pdf");

        Assert.Equal(
            "BinTracker_Movement_History_20260725_20260823.pdf",
            name);
    }

    [Fact]
    public void Reserved_windows_device_name_is_made_safe()
    {
        Assert.Equal("_CON", MovementHistoryExportFileName.SanitizeWindowsSegment("CON"));
    }

    private static MovementHistoryReportResult Result(
        params MovementHistoryReportRow[] rows) =>
        new(
            new DateOnly(2026, 7, 25),
            new DateOnly(2026, 8, 23),
            rows,
            Array.Empty<MovementHistoryContainerTotal>());

    private static MovementHistoryReportRow Row(int customerId, string code) =>
        new(
            1,
            new DateOnly(2026, 8, 1),
            new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc),
            customerId,
            code,
            "Customer",
            CustomerType.Account,
            1,
            "Blue",
            1,
            MovementType.Out,
            1,
            MovementSource.Manual,
            "",
            "",
            "operator",
            null,
            null,
            "",
            "");
}
