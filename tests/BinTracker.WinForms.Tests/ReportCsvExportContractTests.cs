using BinTracker.WinForms;
using Xunit;

namespace BinTracker.WinForms.Tests;

public sealed class ReportCsvExportContractTests
{
    [Fact]
    public void Daily_description_identifies_file_rows_and_active_filters()
    {
        var entry = ReportCsvExportContract.Daily(
            @"C:\Exports\daily.csv", 3, new DateOnly(2026, 10, 4),
            "ACME", "Blue", "IN", "Manual", true, false);

        Assert.Equal("daily.csv", entry.FileName);
        Assert.Contains("'daily.csv'", entry.Description);
        Assert.Contains("3 row(s)", entry.Description);
        Assert.Contains("Date=04/10/2026", entry.Description);
        Assert.Contains("Customer search=ACME", entry.Description);
        Assert.Contains("Container=Blue", entry.Description);
        Assert.Contains("Direction=IN", entry.Description);
        Assert.Contains("Source=Manual", entry.Description);
        Assert.Contains("Opening adjustments=Included", entry.Description);
        Assert.Contains("Notes=Excluded", entry.Description);
        Assert.DoesNotContain(@"C:\Exports", entry.Description);
    }

    [Theory]
    [InlineData("Daily Detail", true)]
    [InlineData("Weekly Overview", false)]
    public void Weekly_views_are_distinguishable_and_complete(
        string view,
        bool includeNotes)
    {
        var entry = ReportCsvExportContract.Weekly(
            $@"C:\Exports\weekly-{view}.csv", 7,
            new DateOnly(2026, 9, 28), new DateOnly(2026, 10, 4),
            view, "", "All containers", "All sources", false,
            includeNotes ? true : null);

        Assert.Equal($"weekly-{view}.csv", entry.FileName);
        Assert.Contains($"'weekly-{view}.csv'", entry.Description);
        Assert.Contains("7 row(s)", entry.Description);
        Assert.Contains("Range=28/09/2026 - 04/10/2026", entry.Description);
        Assert.Contains($"View={view}", entry.Description);
        Assert.Contains("Customer search=All", entry.Description);
        Assert.Contains("Container=All containers", entry.Description);
        Assert.Contains("Source=All sources", entry.Description);
        Assert.Contains("Opening adjustments=Excluded", entry.Description);
        Assert.Equal(includeNotes, entry.Description.Contains("Notes=Included"));
    }

    [Fact]
    public void Monthly_description_identifies_file_rows_and_report_context()
    {
        var entry = ReportCsvExportContract.Monthly(
            @"C:\Exports\monthly.csv", 1,
            new DateOnly(2026, 10, 1), new DateOnly(2026, 10, 4),
            1, 3, -2,
            "", "Yellow", "Import", true);

        Assert.Equal("monthly.csv", entry.FileName);
        Assert.Contains("'monthly.csv'", entry.Description);
        Assert.Contains("1 row(s)", entry.Description);
        Assert.Contains("Month=October 2026", entry.Description);
        Assert.Contains("Data through=04/10/2026", entry.Description);
        Assert.Contains("OUT=1", entry.Description);
        Assert.Contains("IN=3", entry.Description);
        Assert.Contains("Net=-2", entry.Description);
        Assert.Contains("Container=Yellow", entry.Description);
        Assert.Contains("Source=Import", entry.Description);
    }

    [Fact]
    public void Outstanding_description_identifies_file_rows_and_balance_context()
    {
        var entry = ReportCsvExportContract.Outstanding(
            @"C:\Exports\outstanding.csv", 3,
            new DateOnly(2026, 10, 4), "COD", "All containers",
            "Credits only", true);

        Assert.Equal("outstanding.csv", entry.FileName);
        Assert.Contains("'outstanding.csv'", entry.Description);
        Assert.Contains("3 row(s)", entry.Description);
        Assert.Contains("As of=04/10/2026", entry.Description);
        Assert.Contains("Customer search=COD", entry.Description);
        Assert.Contains("Balance mode=Credits only", entry.Description);
        Assert.Contains("Inactive customers=Included", entry.Description);
    }

    [Fact]
    public void Movement_history_uses_the_same_complete_description_convention()
    {
        var entry = ReportCsvExportContract.MovementHistory(
            @"C:\Exports\history.csv", 12,
            new DateOnly(2026, 10, 1), new DateOnly(2026, 10, 4),
            "ACME", "Blue", "OUT", "Batch", false, true);

        Assert.Equal("history.csv", entry.FileName);
        Assert.Contains("'history.csv'", entry.Description);
        Assert.Contains("12 row(s)", entry.Description);
        Assert.Contains("Range=01/10/2026 - 04/10/2026", entry.Description);
        Assert.Contains("Customer search=ACME", entry.Description);
        Assert.Contains("Direction=OUT", entry.Description);
        Assert.Contains("Notes=Included", entry.Description);
    }

    [Fact]
    public void Csv_rows_preserve_the_current_display_order_and_skip_non_rows()
    {
        var rows = ReportCsvExportContract.PreserveDisplayedOrder<string>(
            ["third", null, "first", "second"]);

        Assert.Equal(["third", "first", "second"], rows);
    }

    [Fact]
    public void Csv_writer_preserves_header_content_row_content_and_order()
    {
        using var writer = new StringWriter();

        ReportCsvExportContract.WriteRows(
            writer,
            "Header A,Header B",
            ["third,3", "first,1", "second,2"]);

        Assert.Equal(
            $"Header A,Header B{Environment.NewLine}" +
            $"third,3{Environment.NewLine}" +
            $"first,1{Environment.NewLine}" +
            $"second,2{Environment.NewLine}",
            writer.ToString());
    }
}
