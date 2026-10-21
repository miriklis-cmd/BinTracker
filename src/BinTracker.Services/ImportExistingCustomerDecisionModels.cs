
using BinTracker.Core;

namespace BinTracker.Services;

public enum ImportExistingCustomerDecisionAction
{
    Unconfirmed = 0,
    AcceptMatch = 1,
    OverrideMatch = 2
}

public sealed record ImportExistingCustomerDecision(
    string ImportedCustomerCode,
    ImportExistingCustomerDecisionAction Action,
    int? CustomerId,
    string CustomerCode,
    string CustomerName);

public static class ImportExistingCustomerDecisionPlanner
{
    public static IReadOnlyDictionary<string, ImportExistingCustomerDecision> MergeDefaults(
        ImportReviewPlan review,
        IReadOnlyDictionary<string, ImportExistingCustomerDecision> existing)
    {
        ArgumentNullException.ThrowIfNull(review);
        ArgumentNullException.ThrowIfNull(existing);
        foreach (var decision in existing.Values)
            ProviderNeutralValidation.RequireDefined(decision.Action, nameof(decision.Action));

        var result = new Dictionary<string, ImportExistingCustomerDecision>(
            existing,
            StringComparer.OrdinalIgnoreCase);

        var matched = review.Customers
            .Where(x =>
                x.Status == ImportCustomerReviewStatus.Existing &&
                x.ExistingCustomerId.HasValue)
            .ToList();

        foreach (var row in matched)
        {
            if (result.ContainsKey(row.CustomerCode))
                continue;

            result[row.CustomerCode] = new ImportExistingCustomerDecision(
                row.CustomerCode,
                ImportExistingCustomerDecisionAction.Unconfirmed,
                row.ExistingCustomerId,
                row.ExistingCustomerName,
                row.ExistingCustomerName);
        }

        var valid = matched
            .Select(x => x.CustomerCode)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var key in result.Keys.ToList())
        {
            if (!valid.Contains(key))
                result.Remove(key);
        }

        return result;
    }

    public static int UnconfirmedCount(
        IReadOnlyDictionary<string, ImportExistingCustomerDecision> decisions) =>
        Validated(decisions).Count(x =>
            x.Action == ImportExistingCustomerDecisionAction.Unconfirmed);

    public static int ConfirmedCount(
        IReadOnlyDictionary<string, ImportExistingCustomerDecision> decisions) =>
        Validated(decisions).Count(x =>
            x.Action != ImportExistingCustomerDecisionAction.Unconfirmed);

    private static IEnumerable<ImportExistingCustomerDecision> Validated(
        IReadOnlyDictionary<string, ImportExistingCustomerDecision> decisions)
    {
        ArgumentNullException.ThrowIfNull(decisions);
        foreach (var decision in decisions.Values)
        {
            ProviderNeutralValidation.RequireDefined(decision.Action, nameof(decision.Action));
            yield return decision;
        }
    }
}
