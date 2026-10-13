using BinTracker.Core;
using BinTracker.Services;
using BinTracker.WinForms;
using System.Windows.Forms;
using Xunit;

namespace BinTracker.WinForms.Tests;

public sealed class MovementHistoryColumnLayoutTests
{
    [Fact]
    public void Floor_width_fits_compact_structure_and_protects_semantic_columns()
    {
        var columns = Columns();

        var result = MovementHistoryColumnLayout.Allocate(1450, columns);

        Assert.Equal(1450, result.Values.Sum());
        Assert.Equal(columns.Length, result.Count);
        Assert.InRange(result["MovementId"], 78, 100);
        Assert.InRange(result["Direction"], 74, 90);
        Assert.InRange(result["EnteredBy"], 78, 95);
        Assert.True(result["Status"] >= 190);
        Assert.True(result["Notes"] >= 240);
        Assert.True(result["Notes"] > result["Status"]);
    }

    [Fact]
    public void Larger_workspace_expands_semantic_columns_without_hiding_structure()
    {
        var columns = Columns();
        var floor = MovementHistoryColumnLayout.Allocate(1450, columns);

        var large = MovementHistoryColumnLayout.Allocate(2200, columns);

        Assert.Equal(2200, large.Values.Sum());
        Assert.All(columns, column => Assert.True(large.ContainsKey(column.Name)));
        Assert.True(large["Customer"] > floor["Customer"]);
        Assert.True(large["Reference"] > floor["Reference"]);
        Assert.True(large["Status"] > floor["Status"]);
        Assert.True(large["Notes"] > floor["Notes"]);
        Assert.Equal(floor["MovementId"], large["MovementId"]);
        Assert.Equal(floor["Direction"], large["Direction"]);
        Assert.Equal(floor["EnteredBy"], large["EnteredBy"]);
        Assert.True(large["Notes"] > large["Status"]);
    }

    [Fact]
    public void Wrapped_identifier_headers_remain_readable_without_oversized_columns()
    {
        using var grid = new DataGridView();
        grid.RowTemplate.Height = 30;

        MovementHistoryColumnLayout.ConfigureWrappedHeaders(grid);

        var movementId = MovementHistoryReportForm.CreateColumn(
            MovementHistoryColumnLayout.MovementIdHeader,
            92,
            "MovementId");
        var enteredBy = MovementHistoryReportForm.CreateColumn(
            MovementHistoryColumnLayout.EnteredByHeader,
            86,
            "EnteredBy");

        Assert.Equal("Movement\nID", movementId.HeaderText);
        Assert.Equal("Entered\nby", enteredBy.HeaderText);
        Assert.Equal(DataGridViewTriState.True, movementId.HeaderCell.Style.WrapMode);
        Assert.Equal(DataGridViewTriState.True, enteredBy.HeaderCell.Style.WrapMode);
        Assert.Equal(DataGridViewTriState.True,
            grid.ColumnHeadersDefaultCellStyle.WrapMode);
        Assert.Equal(DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
            grid.ColumnHeadersHeightSizeMode);
        Assert.True(grid.ColumnHeadersHeight > grid.RowTemplate.Height);
    }

    [Fact]
    public void Multi_sort_attachment_does_not_reserve_synthetic_header_text_width()
    {
        using var grid = new DataGridView();
        grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "MovementId",
            HeaderText = MovementHistoryColumnLayout.MovementIdHeader,
            Width = 82,
            AutoSizeMode = DataGridViewAutoSizeColumnMode.None
        });

        ReportGridMultiSort.Attach(grid);

        Assert.Equal(82, grid.Columns["MovementId"].Width);
    }

    [Fact]
    public void Report_grid_navigation_cleanup_dismisses_tooltips_and_releases_sort_state()
    {
        using var owner = new Panel();
        using var grid = new DataGridView();
        grid.Columns.Add("Date", "Date");
        owner.Controls.Add(grid);
        ReportGridMultiSort.Attach(grid);

        ReportGridMultiSort.PrepareForOwnerDisposal(owner);

        Assert.False(grid.ShowCellToolTips);
        Assert.False(ReportGridMultiSort.IsAttached(grid));
    }

    [Fact]
    public void Batch_detail_uses_full_explicit_date_format()
    {
        var rows = new[]
        {
            new MovementBatchAuditLine(1080, 30, new DateOnly(2026, 8, 28),
                "TEST", "Test Customer", "Blue Bin", MovementType.In, 4, "ref", "notes")
        };
        using var form = new MovementBatchDetailForm(30, rows);
        var grid = Assert.Single(form.Controls.OfType<DataGridView>());
        var date = grid.Columns["Date"];

        Assert.NotNull(date);
        Assert.Equal("dd/MM/yyyy", date.DefaultCellStyle.Format);
        Assert.Equal(DataGridViewAutoSizeColumnMode.AllCells, date.AutoSizeMode);
        Assert.True(date.MinimumWidth >= 120);
    }

    private static MovementHistoryColumnWidth[] Columns()
    {
        var headerMinimums = new Dictionary<string, int>(StringComparer.Ordinal)
        {
            ["Date"] = 70,
            ["MovementId"] = 78,
            ["Code"] = 64,
            ["Customer"] = 90,
            ["Type"] = 62,
            ["Container"] = 82,
            ["Direction"] = 74,
            ["Quantity"] = 48,
            ["Source"] = 74,
            ["Reference"] = 82,
            ["Status"] = 65,
            ["Notes"] = 60,
            ["EnteredBy"] = 78
        };

        return MovementHistoryColumnLayout.CreateSpecifications(
                112,
                name => headerMinimums[name])
            .ToArray();
    }
}
