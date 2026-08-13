
using BinTracker.Core;
using BinTracker.Services;
using Xunit;

namespace BinTracker.UnitTests;

public sealed class ImportExistingCustomerDecisionPlannerTests
{
    [Fact]
    public void Existing_match_defaults_to_unconfirmed()
    {
        var result = ImportExistingCustomerDecisionPlanner.MergeDefaults(
            Plan(),
            new Dictionary<string, ImportExistingCustomerDecision>());

        var decision = Assert.Single(result).Value;
        Assert.Equal(ImportExistingCustomerDecisionAction.Unconfirmed, decision.Action);
        Assert.Equal(7, decision.CustomerId);
    }

    [Fact]
    public void Confirmed_decision_is_retained()
    {
        var existing = new Dictionary<string, ImportExistingCustomerDecision>(
            StringComparer.OrdinalIgnoreCase)
        {
            ["CLAMMS"] = new(
                "CLAMMS",
                ImportExistingCustomerDecisionAction.AcceptMatch,
                7,
                "CLAMMS",
                "Clamms Seafood")
        };

        var result = ImportExistingCustomerDecisionPlanner.MergeDefaults(Plan(), existing);

        Assert.Equal(
            ImportExistingCustomerDecisionAction.AcceptMatch,
            result["CLAMMS"].Action);
    }

    private static ImportReviewPlan Plan() =>
        new(
            [
                new ImportCustomerReviewRow(
                    "CLAMMS",
                    CustomerType.Account,
                    "Update Account",
                    "",
                    "",
                    ImportCustomerReviewStatus.Existing,
                    7,
                    "Clamms Seafood",
                    CustomerType.Account,
                    CustomerMatchKind.ExactCode,
                    "Exact code")
            ],
            1, 1, 1, 0);
}
