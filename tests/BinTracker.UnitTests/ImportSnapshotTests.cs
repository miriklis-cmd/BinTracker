using BinTracker.Core;
using BinTracker.Services;
using Xunit;

namespace BinTracker.UnitTests;

public sealed class ImportSnapshotTests
{
    [Fact]
    public void Snapshot_total_is_bfwd_plus_out_minus_in()
    {
        var row = new ImportSnapshotCandidate(
            "Print this on reverse side",
            "ALBURY",
            CustomerType.Account,
            null,
            Out: 5,
            In: 3,
            BroughtForward: 12,
            ExcelTotal: 14,
            SourceRow: "20");

        Assert.Equal(14, row.CalculatedTotal);
        Assert.True(row.TotalMatches);
    }

    [Fact]
    public void Snapshot_flags_excel_total_mismatch()
    {
        var row = new ImportSnapshotCandidate(
            "Print this on reverse side",
            "ALBURY",
            CustomerType.Account,
            null,
            Out: 5,
            In: 3,
            BroughtForward: 12,
            ExcelTotal: 99,
            SourceRow: "20");

        Assert.False(row.TotalMatches);
    }

    [Fact]
    public void Credit_bfwd_is_preserved_as_negative_position()
    {
        var row = new ImportSnapshotCandidate(
            "Print this on reverse side",
            "TEST",
            CustomerType.Account,
            null,
            Out: 0,
            In: 2,
            BroughtForward: -5,
            ExcelTotal: -7,
            SourceRow: "5");

        Assert.Equal(-7, row.CalculatedTotal);
        Assert.True(row.TotalMatches);
    }
}
