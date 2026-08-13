
namespace BinTracker.Services;

public static class ImportReviewReadiness
{
    public static bool CanAdvanceToImport(
        int blockerCount,
        ImportBalanceReconciliationPlan reconciliation) =>
        blockerCount == 0 && !reconciliation.HasBlockingIssues;
}
