using System.Globalization;

namespace BinTracker.WinForms;

internal sealed record ReportCsvAuditEntry(
    string Description,
    string FileName);

internal static class ReportCsvExportContract
{
    public static ReportCsvAuditEntry Daily(
        string outputPath,
        int rowCount,
        DateOnly reportDate,
        string customerSearch,
        string container,
        string direction,
        string source,
        bool includeAdjustments,
        bool includeNotes) =>
        Create(
            "Daily Movements",
            outputPath,
            rowCount,
            $"Date={reportDate:dd/MM/yyyy}",
            $"Customer search={FilterText(customerSearch)}",
            $"Container={FilterText(container)}",
            $"Direction={FilterText(direction)}",
            $"Source={FilterText(source)}",
            $"Opening adjustments={Included(includeAdjustments)}",
            $"Notes={Included(includeNotes)}");

    public static ReportCsvAuditEntry Weekly(
        string outputPath,
        int rowCount,
        DateOnly weekStart,
        DateOnly weekEnd,
        string view,
        string customerSearch,
        string container,
        string source,
        bool includeAdjustments,
        bool? includeNotes) =>
        Create(
            "Weekly Movements",
            outputPath,
            rowCount,
            $"Range={weekStart:dd/MM/yyyy} - {weekEnd:dd/MM/yyyy}",
            $"View={view}",
            $"Customer search={FilterText(customerSearch)}",
            $"Container={FilterText(container)}",
            $"Source={FilterText(source)}",
            $"Opening adjustments={Included(includeAdjustments)}",
            includeNotes.HasValue
                ? $"Notes={Included(includeNotes.Value)}"
                : null);

    public static ReportCsvAuditEntry Monthly(
        string outputPath,
        int rowCount,
        DateOnly monthStart,
        DateOnly dataThroughDate,
        int outQuantity,
        int inQuantity,
        int netQuantity,
        string customerSearch,
        string container,
        string source,
        bool includeAdjustments) =>
        Create(
            "Monthly Summary",
            outputPath,
            rowCount,
            $"Month={monthStart:MMMM yyyy}",
            $"Data through={dataThroughDate:dd/MM/yyyy}",
            $"OUT={outQuantity.ToString(CultureInfo.InvariantCulture)}",
            $"IN={inQuantity.ToString(CultureInfo.InvariantCulture)}",
            $"Net={netQuantity.ToString(CultureInfo.InvariantCulture)}",
            $"Customer search={FilterText(customerSearch)}",
            $"Container={FilterText(container)}",
            $"Source={FilterText(source)}",
            $"Opening adjustments={Included(includeAdjustments)}");

    public static ReportCsvAuditEntry Outstanding(
        string outputPath,
        int rowCount,
        DateOnly asOfDate,
        string customerSearch,
        string container,
        string balanceMode,
        bool includeInactive) =>
        Create(
            "Outstanding Containers",
            outputPath,
            rowCount,
            $"As of={asOfDate:dd/MM/yyyy}",
            $"Customer search={FilterText(customerSearch)}",
            $"Container={FilterText(container)}",
            $"Balance mode={FilterText(balanceMode)}",
            $"Inactive customers={Included(includeInactive)}");

    public static ReportCsvAuditEntry MovementHistory(
        string outputPath,
        int rowCount,
        DateOnly startDate,
        DateOnly endDate,
        string customerSearch,
        string container,
        string direction,
        string source,
        bool includeAdjustments,
        bool includeNotes) =>
        Create(
            "Movement History",
            outputPath,
            rowCount,
            $"Range={startDate:dd/MM/yyyy} - {endDate:dd/MM/yyyy}",
            $"Customer search={FilterText(customerSearch)}",
            $"Container={FilterText(container)}",
            $"Direction={FilterText(direction)}",
            $"Source={FilterText(source)}",
            $"Opening adjustments={Included(includeAdjustments)}",
            $"Notes={Included(includeNotes)}");

    public static IReadOnlyList<T> PreserveDisplayedOrder<T>(
        IEnumerable<T?> displayedRows)
        where T : class =>
        displayedRows.Where(row => row is not null).Cast<T>().ToList();

    public static void WriteRows(
        TextWriter writer,
        IReadOnlyList<string> header,
        IEnumerable<IReadOnlyList<object?>> rows)
    {
        writer.WriteLine(SerializeRow(header));
        foreach (var row in rows)
            writer.WriteLine(SerializeRow(row));
    }

    public static string SerializeRow(params object?[] fields) =>
        SerializeRow((IReadOnlyList<object?>)fields);

    private static string SerializeRow<T>(IReadOnlyList<T> fields) =>
        string.Join(",", fields.Select(field => SerializeCell(field)));

    private static string SerializeCell(object? value)
    {
        if (value is null)
            return string.Empty;

        if (value is string text)
            return SerializeTextCell(text);

        if (value is sbyte or byte or short or ushort or int or uint or
            long or ulong or float or double or decimal)
        {
            return Convert.ToString(value, CultureInfo.InvariantCulture)
                ?? string.Empty;
        }

        throw new ArgumentException(
            $"Unsupported CSV cell type '{value.GetType().FullName}'.",
            nameof(value));
    }

    private static string SerializeTextCell(string value)
    {
        var protectedValue = StartsWithFormula(value)
            ? $"'{value}"
            : value;
        var escaped = protectedValue.Replace("\"", "\"\"");

        return protectedValue.Contains(',') ||
               protectedValue.Contains('"') ||
               protectedValue.Contains('\r') ||
               protectedValue.Contains('\n')
            ? $"\"{escaped}\""
            : escaped;
    }

    private static bool StartsWithFormula(string value)
    {
        foreach (var character in value)
        {
            if (character is '=' or '+' or '-' or '@')
                return true;

            if (!char.IsWhiteSpace(character) &&
                !char.IsControl(character) &&
                char.GetUnicodeCategory(character) != UnicodeCategory.Format)
                return false;
        }

        return false;
    }

    private static ReportCsvAuditEntry Create(
        string reportName,
        string outputPath,
        int rowCount,
        params string?[] context)
    {
        var fileName = Path.GetFileName(outputPath);
        var count = rowCount.ToString(CultureInfo.InvariantCulture);
        var activeContext = string.Join(
            "; ",
            context.Where(value => !string.IsNullOrWhiteSpace(value)));

        return new ReportCsvAuditEntry(
            $"{reportName} CSV '{fileName}' exported: {count} row(s); {activeContext}.",
            fileName);
    }

    private static string FilterText(string value) =>
        string.IsNullOrWhiteSpace(value) ? "All" : value.Trim();

    private static string Included(bool included) =>
        included ? "Included" : "Excluded";
}
