using BinTracker.WinForms;
using System.Reflection;
using Xunit;

namespace BinTracker.WinForms.Tests;

public sealed class ReportCsvExportContractTests
{
    private static readonly Type[] CurrentCsvExporters =
    [
        typeof(DailyMovementsReportForm),
        typeof(WeeklyMovementsReportForm),
        typeof(MonthlySummaryReportForm),
        typeof(OutstandingContainersReportForm),
        typeof(MovementHistoryReportForm)
    ];

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
            ["Header A", "Header B"],
            [
                new object?[] { "third", 3 },
                new object?[] { "first", 1 },
                new object?[] { "second", 2 }
            ]);

        Assert.Equal(
            $"Header A,Header B{Environment.NewLine}" +
            $"third,3{Environment.NewLine}" +
            $"first,1{Environment.NewLine}" +
            $"second,2{Environment.NewLine}",
            writer.ToString());
    }

    [Fact]
    public void Csv_serializer_preserves_ordinary_structural_and_empty_text()
    {
        Assert.Equal("ordinary", ReportCsvExportContract.SerializeRow("ordinary"));
        Assert.Equal("\"two, fields\"", ReportCsvExportContract.SerializeRow("two, fields"));
        Assert.Equal("\"a \"\"quote\"\"\"", ReportCsvExportContract.SerializeRow("a \"quote\""));
        Assert.Equal("\"line one\r\nline two\"", ReportCsvExportContract.SerializeRow("line one\r\nline two"));
        Assert.Equal(",", ReportCsvExportContract.SerializeRow(string.Empty, null));
    }

    [Theory]
    [InlineData("=1+1", "'=1+1")]
    [InlineData("+1+1", "'+1+1")]
    [InlineData("-1+1", "'-1+1")]
    [InlineData("@SUM(A1:A2)", "'@SUM(A1:A2)")]
    public void Csv_serializer_neutralizes_formula_leading_text(
        string value,
        string expected)
    {
        Assert.Equal(expected, ReportCsvExportContract.SerializeRow(value));
    }

    [Theory]
    [InlineData(" =1+1", "' =1+1")]
    [InlineData("\t@SUM(A1:A2)", "'\t@SUM(A1:A2)")]
    [InlineData("\r\n-1+1", "\"'\r\n-1+1\"")]
    [InlineData("\u200B+1+1", "'\u200B+1+1")]
    [InlineData(" ACME", " ACME")]
    public void Csv_serializer_handles_formula_bypass_prefixes_without_changing_safe_whitespace(
        string value,
        string expected)
    {
        Assert.Equal(expected, ReportCsvExportContract.SerializeRow(value));
    }

    [Fact]
    public void Csv_serializer_composes_formula_protection_with_csv_escaping()
    {
        Assert.Equal(
            "\"'=SUM(A1:A2), \"\"quoted\"\"\r\nnext\"",
            ReportCsvExportContract.SerializeRow(
                "=SUM(A1:A2), \"quoted\"\r\nnext"));
    }

    [Fact]
    public void Csv_serializer_preserves_numeric_and_date_semantics()
    {
        Assert.Equal(
            "-42,12.5,14/10/2026,'-42",
            ReportCsvExportContract.SerializeRow(
                -42,
                12.5m,
                "14/10/2026",
                "-42"));
    }

    [Fact]
    public void Csv_writer_applies_security_to_every_text_cell()
    {
        using var writer = new StringWriter();

        ReportCsvExportContract.WriteRows(
            writer,
            ["Text", "Number", "Date"],
            [new object?[] { "=1+1", -42, "14/10/2026" }]);

        Assert.Equal(
            $"Text,Number,Date{Environment.NewLine}" +
            $"'=1+1,-42,14/10/2026{Environment.NewLine}",
            writer.ToString());
    }

    [Fact]
    public void Current_report_exporters_do_not_own_bypassable_csv_serializers()
    {
        foreach (var exporter in CurrentCsvExporters)
        {
            var localCsvMethods = exporter.GetMethods(
                    BindingFlags.NonPublic |
                    BindingFlags.Public |
                    BindingFlags.Static |
                    BindingFlags.Instance)
                .Where(method => method.Name.Contains(
                    "Csv",
                    StringComparison.OrdinalIgnoreCase) &&
                    method.ReturnType == typeof(string) &&
                    method.GetParameters() is [{ ParameterType: var parameterType }] &&
                    parameterType == typeof(string));

            Assert.Empty(localCsvMethods);
        }
    }
}
