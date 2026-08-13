using System.Text;

namespace BinTracker.Services;

public enum CustomerMatchKind
{
    None = 0,
    ExactCode = 1,
    CaseInsensitiveCode = 2,
    NormalizedCode = 3,
    NormalizedName = 4
}

public sealed record CustomerMatchResult(
    CustomerListRow? Customer,
    CustomerMatchKind Kind,
    string Reason)
{
    public bool IsMatch => Customer is not null;
}

public static class CustomerNameNormalizer
{
    /// <summary>
    /// Produces a conservative comparison key for legacy customer identifiers.
    /// Case, spaces and punctuation are ignored, while letters and digits remain.
    /// Examples:
    /// "S & J", "S&J", "S  &  J" => "SJ"
    /// "A.E.G.I.R" => "AEGIR"
    /// </summary>
    public static string ComparisonKey(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        var builder = new StringBuilder(value.Length);

        foreach (var ch in value.Trim().ToUpperInvariant())
        {
            if (char.IsLetterOrDigit(ch))
                builder.Append(ch);
        }

        return builder.ToString();
    }

    public static CustomerMatchResult FindBestMatch(
        string importedCustomerCode,
        IReadOnlyCollection<CustomerListRow> existingCustomers)
    {
        var imported = importedCustomerCode.Trim();

        // 1. Preserve the strongest and safest match reason first.
        var exact = existingCustomers.FirstOrDefault(x =>
            x.CustomerCode.Equals(imported, StringComparison.Ordinal));

        if (exact is not null)
            return new CustomerMatchResult(
                exact,
                CustomerMatchKind.ExactCode,
                "Exact customer-code match");

        var caseInsensitive = existingCustomers.FirstOrDefault(x =>
            x.CustomerCode.Equals(imported, StringComparison.OrdinalIgnoreCase));

        if (caseInsensitive is not null)
            return new CustomerMatchResult(
                caseInsensitive,
                CustomerMatchKind.CaseInsensitiveCode,
                "Customer code matches ignoring case");

        var importedKey = ComparisonKey(imported);
        if (importedKey.Length == 0)
            return new CustomerMatchResult(null, CustomerMatchKind.None, "No match");

        // 2. Prefer customer CODE normalization over NAME normalization.
        var codeMatches = existingCustomers
            .Where(x => ComparisonKey(x.CustomerCode) == importedKey)
            .ToList();

        if (codeMatches.Count == 1)
        {
            return new CustomerMatchResult(
                codeMatches[0],
                CustomerMatchKind.NormalizedCode,
                "Customer code matches after spacing/punctuation normalization");
        }

        // 3. Name matching is useful for legacy sheets whose Buyer text was a
        // display name, but only auto-match if exactly one customer normalizes
        // to that value. Ambiguous normalized names remain unmatched.
        var nameMatches = existingCustomers
            .Where(x => ComparisonKey(x.Name) == importedKey)
            .ToList();

        if (nameMatches.Count == 1)
        {
            return new CustomerMatchResult(
                nameMatches[0],
                CustomerMatchKind.NormalizedName,
                "Customer name matches after spacing/punctuation normalization");
        }

        return new CustomerMatchResult(null, CustomerMatchKind.None, "No confident automatic match");
    }
}
