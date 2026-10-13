using BinTracker.Core;
using BinTracker.Services;
using BinTracker.WinForms;
using System.Windows.Forms;
using Xunit;

namespace BinTracker.WinForms.Tests;

public sealed class ReversedLineDispositionDialogTests
{
    [Fact]
    public void Date_column_displays_full_explicit_date_without_changing_scroll_contract()
    {
        var preview = new LogicalMovementMutationPreview(
            new LogicalMovementBatchId(23),
            new LogicalMovementGenerationNumber(1),
            30,
            null,
            false,
            [new LogicalMovementMutationPreviewLine(
                new LogicalMovementLineId(176),
                1081,
                2,
                LogicalMovementLineState.Reversed,
                new LogicalMovementMutationPreviewState(
                    1081,
                    new DateOnly(2026, 8, 28),
                    MovementType.Out,
                    161,
                    "TEST",
                    "Test Customer",
                    3,
                    "Yellow Bin",
                    1,
                    null,
                    null,
                    30),
                1082)]);

        using var form = new ReversedLineDispositionDialog(30, preview);
        var grid = Descendants(form).OfType<DataGridView>().Single();
        var date = grid.Columns["Date"];

        Assert.NotNull(date);
        Assert.Equal("dd/MM/yyyy", date.DefaultCellStyle.Format);
        Assert.True(date.Width >= 125);
        Assert.True(date.MinimumWidth >= 125);
        Assert.Equal("28/08/2026", grid.Rows[0].Cells["Date"].FormattedValue);
        Assert.Equal(ScrollBars.Vertical, grid.ScrollBars);
        Assert.Equal(7, grid.Columns.Count);
    }

    private static IEnumerable<Control> Descendants(Control owner)
    {
        foreach (Control child in owner.Controls)
        {
            yield return child;
            foreach (var descendant in Descendants(child))
                yield return descendant;
        }
    }
}
