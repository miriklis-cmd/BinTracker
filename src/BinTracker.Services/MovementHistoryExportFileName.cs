namespace BinTracker.Services;

public static class MovementHistoryExportFileName
{
    private static readonly HashSet<string> ReservedWindowsNames = new(
        new[] { "CON", "PRN", "AUX", "NUL", "CLOCK$" }
            .Concat(Enumerable.Range(1, 9).Select(x => $"COM{x}"))
            .Concat(Enumerable.Range(1, 9).Select(x => $"LPT{x}")),
        StringComparer.OrdinalIgnoreCase);

    public static string Build(
        MovementHistoryReportResult result,
        bool customerFilterApplied,
        string extension)
    {
        ArgumentNullException.ThrowIfNull(result);

        var customerCode = customerFilterApplied
            ? ResolveSingleCustomerCode(result.Rows)
            : null;
        var customerSegment = string.IsNullOrWhiteSpace(customerCode)
            ? string.Empty
            : $"_{SanitizeWindowsSegment(customerCode)}";
        var safeExtension = extension.Trim().TrimStart('.');

        return $"BinTracker_Movement_History{customerSegment}_" +
               $"{result.StartDate:yyyyMMdd}_{result.EndDate:yyyyMMdd}." +
               safeExtension;
    }

    public static string SanitizeWindowsSegment(string value)
    {
        var invalid = new HashSet<char>("<>:\"/\\|?*");
        var sanitized = new string(value.Trim()
            .Select(character => character < 32 || invalid.Contains(character)
                ? '_'
                : character)
            .ToArray())
            .TrimEnd(' ', '.');

        if (string.IsNullOrWhiteSpace(sanitized))
            return "CUSTOMER";

        return ReservedWindowsNames.Contains(sanitized)
            ? $"_{sanitized}"
            : sanitized;
    }

    private static string? ResolveSingleCustomerCode(
        IReadOnlyList<MovementHistoryReportRow> rows)
    {
        var customers = rows
            .GroupBy(row => row.CustomerId)
            .Take(2)
            .ToList();

        return customers.Count == 1
            ? customers[0].Select(row => row.CustomerCode)
                .FirstOrDefault(code => !string.IsNullOrWhiteSpace(code))
            : null;
    }
}
