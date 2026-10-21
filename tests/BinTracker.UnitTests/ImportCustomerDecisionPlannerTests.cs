using BinTracker.Core;
using BinTracker.Services;
using Xunit;

namespace BinTracker.UnitTests;

public sealed class ImportCustomerDecisionPlannerTests
{
    [Fact]
    public void New_customers_default_to_unconfirmed()
    {
        var result = ImportCustomerDecisionPlanner.MergeDefaults(Plan("NEWCO"), new Dictionary<string, ImportCustomerDecision>());
        var d = Assert.Single(result).Value;
        Assert.Equal("NEWCO", d.ProposedName);
        Assert.Equal(ImportCustomerDecisionAction.Unconfirmed, d.Action);
    }

    [Fact]
    public void Existing_decision_is_retained()
    {
        var existing = new Dictionary<string, ImportCustomerDecision>(StringComparer.OrdinalIgnoreCase) { ["NEWCO"] = new("NEWCO", "New Company Pty Ltd", ImportCustomerDecisionAction.Create) };
        var result = ImportCustomerDecisionPlanner.MergeDefaults(Plan("NEWCO"), existing);
        Assert.Equal("New Company Pty Ltd", result["NEWCO"].ProposedName);
        Assert.Equal(ImportCustomerDecisionAction.Create, result["NEWCO"].Action);
    }

    [Fact]
    public void Undefined_existing_decision_is_rejected()
    {
        var existing = new Dictionary<string, ImportCustomerDecision>
        {
            ["NEWCO"] = new("NEWCO", "New Company", (ImportCustomerDecisionAction)99)
        };

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            ImportCustomerDecisionPlanner.MergeDefaults(Plan("NEWCO"), existing));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            ImportCustomerDecisionPlanner.CreateCount(existing));
    }

    private static ImportReviewPlan Plan(string code) => new([
        new ImportCustomerReviewRow(code, CustomerType.Account, "Update Account", "", "", ImportCustomerReviewStatus.New, null, "", null, CustomerMatchKind.None, "No match")
    ], 1, 1, 0, 0);
}
