
using BinTracker.Services;
using Xunit;

namespace BinTracker.UnitTests;

public sealed class ImportReviewReadinessTests
{
    [Fact]
    public void Fully_resolved_review_can_advance()
    {
        var reconciliation = new ImportBalanceReconciliationPlan([]);

        Assert.True(
            ImportReviewReadiness.CanAdvanceToImport(
                blockerCount: 0,
                reconciliation));
    }

    [Fact]
    public void Ui_blocker_prevents_advance()
    {
        var reconciliation = new ImportBalanceReconciliationPlan([]);

        Assert.False(
            ImportReviewReadiness.CanAdvanceToImport(
                blockerCount: 1,
                reconciliation));
    }
}
