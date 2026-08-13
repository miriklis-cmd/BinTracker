namespace BinTracker.Services;

public enum ImportCustomerDecisionAction
{
    Unconfirmed = 0,
    Create = 1,
    Skip = 2
}

public sealed record ImportCustomerDecision(
    string CustomerCode,
    string ProposedName,
    ImportCustomerDecisionAction Action);

public static class ImportCustomerDecisionPlanner
{
    public static IReadOnlyDictionary<string, ImportCustomerDecision> MergeDefaults(
        ImportReviewPlan review,
        IReadOnlyDictionary<string, ImportCustomerDecision> existing)
    {
        var result = new Dictionary<string, ImportCustomerDecision>(existing, StringComparer.OrdinalIgnoreCase);
        var newCodes = review.Customers
            .Where(x => x.Status == ImportCustomerReviewStatus.New)
            .Select(x => x.CustomerCode)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var row in review.Customers.Where(x => x.Status == ImportCustomerReviewStatus.New))
        {
            if (!result.ContainsKey(row.CustomerCode))
                result[row.CustomerCode] = new ImportCustomerDecision(row.CustomerCode, row.CustomerCode, ImportCustomerDecisionAction.Unconfirmed);
        }

        foreach (var key in result.Keys.ToList())
            if (!newCodes.Contains(key)) result.Remove(key);

        return result;
    }

    public static int UnconfirmedCount(IReadOnlyDictionary<string, ImportCustomerDecision> decisions) =>
        decisions.Values.Count(x => x.Action == ImportCustomerDecisionAction.Unconfirmed);

    public static int CreateCount(IReadOnlyDictionary<string, ImportCustomerDecision> decisions) =>
        decisions.Values.Count(x => x.Action == ImportCustomerDecisionAction.Create);

    public static int SkipCount(IReadOnlyDictionary<string, ImportCustomerDecision> decisions) =>
        decisions.Values.Count(x => x.Action == ImportCustomerDecisionAction.Skip);
}
