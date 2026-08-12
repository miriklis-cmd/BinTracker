using BinTracker.Core;
using ClosedXML.Excel;

namespace BinTracker.Services;

public sealed record ImportWorksheetAnalysis(
    string Name,
    int UsedRows,
    int UsedColumns,
    int BuyerColumns,
    int BuyerCandidates,
    string Status);

public sealed record ImportCustomerCandidate(
    string Worksheet,
    string CustomerCode,
    CustomerType CustomerType,
    string SourceCell);

public sealed record ImportSnapshotCandidate(
    string Worksheet,
    string CustomerCode,
    CustomerType CustomerType,
    string? ContainerHint,
    int? Out,
    int? In,
    int? BroughtForward,
    int? ExcelTotal,
    string SourceRow)
{
    public int? CalculatedTotal =>
        BroughtForward.HasValue || Out.HasValue || In.HasValue
            ? (BroughtForward ?? 0) + (Out ?? 0) - (In ?? 0)
            : null;

    public bool TotalMatches =>
        !ExcelTotal.HasValue ||
        !CalculatedTotal.HasValue ||
        ExcelTotal.Value == CalculatedTotal.Value;
}

public sealed record ExcelImportAnalysis(
    string FileName,
    string FullPath,
    IReadOnlyList<ImportWorksheetAnalysis> Worksheets,
    IReadOnlyList<ImportCustomerCandidate> CustomerCandidates,
    IReadOnlyList<ImportSnapshotCandidate> SnapshotCandidates,
    IReadOnlyList<string> Warnings)
{
    public int WorksheetCount => Worksheets.Count;
    public int CustomerCandidateCount => CustomerCandidates.Count;
    public int UniqueCustomerCount => CustomerCandidates
        .Where(x => !string.IsNullOrWhiteSpace(x.CustomerCode))
        .Select(x => x.CustomerCode.Trim())
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .Count();
    public int SnapshotCandidateCount => SnapshotCandidates.Count;
}

public interface IExcelImportService
{
    Task<ExcelImportAnalysis> AnalyzeAsync(
        string filePath,
        CancellationToken cancellationToken = default);
}

internal sealed class ExcelImportService(
    UserSession session,
    IAuditService audit) : IExcelImportService
{
    private static readonly string[] ExpectedOperationalSheets =
    [
        "Update Account",
        "Update Cash",
        "Summary",
        "CREDITS",
        "Print This",
        "Print this on reverse side"
    ];

    public async Task<ExcelImportAnalysis> AnalyzeAsync(
        string filePath,
        CancellationToken cancellationToken = default)
    {
        RequireAdmin();

        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("Choose an Excel workbook first.");

        if (!File.Exists(filePath))
            throw new FileNotFoundException("The selected workbook no longer exists.", filePath);

        var extension = Path.GetExtension(filePath);
        if (!extension.Equals(".xlsx", StringComparison.OrdinalIgnoreCase) &&
            !extension.Equals(".xlsm", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "BinTracker import currently supports .xlsx and .xlsm workbooks.");
        }

        var worksheets = new List<ImportWorksheetAnalysis>();
        var candidates = new List<ImportCustomerCandidate>();
        var snapshots = new List<ImportSnapshotCandidate>();
        var warnings = new List<string>();

        using var workbook = new XLWorkbook(filePath);

        foreach (var sheet in workbook.Worksheets)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var range = sheet.RangeUsed();
            if (range is null)
            {
                worksheets.Add(new ImportWorksheetAnalysis(
                    sheet.Name, 0, 0, 0, 0, "Empty"));
                continue;
            }

            var usedRows = range.RowCount();
            var usedColumns = range.ColumnCount();

            var buyerHeaders = FindBuyerHeaders(sheet, range);
            var sheetCandidates = new List<ImportCustomerCandidate>();

            foreach (var header in buyerHeaders)
            {
                var customerType = GuessCustomerType(sheet.Name);
                sheetCandidates.AddRange(ReadBuyerColumn(
                    sheet,
                    header.Address.RowNumber,
                    header.Address.ColumnNumber,
                    customerType));
            }

            candidates.AddRange(sheetCandidates);

            // Snapshot-style legacy sheets commonly expose today's Out / In,
            // a carried-forward B/Fwd and a calculated Total. Detect that shape
            // now so Mapping can import opening position + today's real movements
            // without pretending the workbook contains full historical detail.
            snapshots.AddRange(ReadSnapshotRows(sheet, range));

            var status = buyerHeaders.Count > 0
                ? $"Detected {buyerHeaders.Count} Buyer column(s)"
                : "No Buyer header detected";

            worksheets.Add(new ImportWorksheetAnalysis(
                sheet.Name,
                usedRows,
                usedColumns,
                buyerHeaders.Count,
                sheetCandidates.Count,
                status));
        }

        foreach (var expected in ExpectedOperationalSheets)
        {
            if (!workbook.Worksheets.Any(x =>
                    x.Name.Equals(expected, StringComparison.OrdinalIgnoreCase)))
            {
                warnings.Add($"Expected worksheet not found: {expected}");
            }
        }

        // We deliberately de-duplicate only for the analysis summary. Source
        // cells remain visible so conflicts can be reviewed before a real import.
        var duplicateCodes = candidates
            .Where(x => !string.IsNullOrWhiteSpace(x.CustomerCode))
            .GroupBy(
                x => x.CustomerCode.Trim(),
                StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .Take(25)
            .ToList();

        if (duplicateCodes.Count > 0)
        {
            warnings.Add(
                $"Potential duplicate customer codes detected ({duplicateCodes.Count} shown): " +
                string.Join(", ", duplicateCodes));
        }

        var analysis = new ExcelImportAnalysis(
            Path.GetFileName(filePath),
            Path.GetFullPath(filePath),
            worksheets,
            candidates,
            snapshots,
            warnings);

        await audit.WriteAsync(
            "IMPORT_WORKBOOK_ANALYSED",
            "Import",
            null,
            $"Excel workbook '{analysis.FileName}' analysed. " +
            $"{analysis.WorksheetCount} worksheet(s), " +
            $"{analysis.UniqueCustomerCount} unique customer(s), " +
            $"{analysis.CustomerCandidateCount} occurrence(s), " +
            $"{analysis.SnapshotCandidateCount} snapshot row(s).",
            after: new
            {
                analysis.FileName,
                analysis.WorksheetCount,
                analysis.UniqueCustomerCount,
                analysis.CustomerCandidateCount,
                analysis.SnapshotCandidateCount,
                WarningCount = analysis.Warnings.Count
            },
            cancellationToken: cancellationToken);

        return analysis;
    }

    private static List<IXLCell> FindBuyerHeaders(
        IXLWorksheet sheet,
        IXLRange range)
    {
        var result = new List<IXLCell>();

        // Search only the used range. "Buyer" is the workbook's established
        // heading for customer lists on the operational print/update sheets.
        foreach (var cell in range.Cells())
        {
            var value = cell.GetFormattedString().Trim();
            if (value.Equals("Buyer", StringComparison.OrdinalIgnoreCase))
                result.Add(cell);
        }

        return result;
    }

    private static IEnumerable<ImportCustomerCandidate> ReadBuyerColumn(
        IXLWorksheet sheet,
        int headerRow,
        int column,
        CustomerType customerType)
    {
        var lastUsedRow = sheet.LastRowUsed()?.RowNumber() ?? headerRow;

        for (var row = headerRow + 1; row <= lastUsedRow; row++)
        {
            var cell = sheet.Cell(row, column);
            var value = cell.GetFormattedString().Trim();

            if (string.IsNullOrWhiteSpace(value))
                continue;

            if (LooksLikeSectionHeading(value))
                continue;

            yield return new ImportCustomerCandidate(
                sheet.Name,
                value,
                customerType,
                cell.Address?.ToString() ?? string.Empty);
        }
    }


    private static IEnumerable<ImportSnapshotCandidate> ReadSnapshotRows(
        IXLWorksheet sheet,
        IXLRange range)
    {
        // Find one row containing Buyer plus at least two of Out/In/B-Fwd/Total.
        // This is deliberately conservative: custom workbook mapping will be
        // confirmed by the user before any database write is enabled.
        for (var row = range.FirstRow().RowNumber();
             row <= range.LastRow().RowNumber();
             row++)
        {
            var headers = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            for (var col = range.FirstColumn().ColumnNumber();
                 col <= range.LastColumn().ColumnNumber();
                 col++)
            {
                var value = sheet.Cell(row, col).GetFormattedString().Trim();
                var normalized = NormalizeHeader(value);

                if (normalized is not null && !headers.ContainsKey(normalized))
                    headers[normalized] = col;
            }

            if (!headers.TryGetValue("BUYER", out var buyerCol))
                continue;

            var movementHeaderCount = new[] { "OUT", "IN", "BFWD", "TOTAL" }
                .Count(headers.ContainsKey);

            if (movementHeaderCount < 2)
                continue;

            var lastUsedRow = sheet.LastRowUsed()?.RowNumber() ?? row;

            for (var dataRow = row + 1; dataRow <= lastUsedRow; dataRow++)
            {
                var buyer = sheet.Cell(dataRow, buyerCol).GetFormattedString().Trim();
                if (string.IsNullOrWhiteSpace(buyer) || LooksLikeSectionHeading(buyer))
                    continue;

                yield return new ImportSnapshotCandidate(
                    sheet.Name,
                    buyer,
                    GuessCustomerType(sheet.Name),
                    ContainerHint: null,
                    Out: ReadInt(sheet, dataRow, headers, "OUT"),
                    In: ReadInt(sheet, dataRow, headers, "IN"),
                    BroughtForward: ReadInt(sheet, dataRow, headers, "BFWD"),
                    ExcelTotal: ReadInt(sheet, dataRow, headers, "TOTAL"),
                    SourceRow: dataRow.ToString());
            }

            // One detected header row per worksheet is enough for this pass.
            yield break;
        }
    }

    private static string? NormalizeHeader(string value)
    {
        var normalized = value
            .Trim()
            .Replace("/", string.Empty)
            .Replace(".", string.Empty)
            .Replace(" ", string.Empty)
            .ToUpperInvariant();

        return normalized switch
        {
            "BUYER" => "BUYER",
            "OUT" => "OUT",
            "IN" => "IN",
            "BFWD" or "BFORWARD" or "BROUGHTFORWARD" => "BFWD",
            "TOTAL" => "TOTAL",
            _ => null
        };
    }

    private static int? ReadInt(
        IXLWorksheet sheet,
        int row,
        IReadOnlyDictionary<string, int> headers,
        string key)
    {
        if (!headers.TryGetValue(key, out var column))
            return null;

        var cell = sheet.Cell(row, column);

        if (cell.TryGetValue<double>(out var numeric))
            return Convert.ToInt32(Math.Round(numeric, MidpointRounding.AwayFromZero));

        var text = cell.GetFormattedString().Trim();

        if (string.IsNullOrWhiteSpace(text))
            return null;

        // Legacy sheets often display credits as "12 CREDIT".
        var credit = text.Contains("CREDIT", StringComparison.OrdinalIgnoreCase);
        var digits = new string(text
            .Where(ch => char.IsDigit(ch) || ch == '-' || ch == '+')
            .ToArray());

        if (!int.TryParse(digits, out var parsed))
            return null;

        return credit ? -Math.Abs(parsed) : parsed;
    }

    private static bool LooksLikeSectionHeading(string value)
    {
        var normalized = value.Trim().Trim(':').ToUpperInvariant();

        return normalized is
            "BUYER" or
            "CREDIT" or
            "CREDITS" or
            "TOTAL" or
            "(BLANK)" or
            "BLANK";
    }

    private static CustomerType GuessCustomerType(string worksheetName)
    {
        return worksheetName.Contains("cash", StringComparison.OrdinalIgnoreCase)
            ? CustomerType.CashCod
            : CustomerType.Account;
    }

    private void RequireAdmin()
    {
        if (session.Role != UserRole.Administrator)
            throw new UnauthorizedAccessException(
                "Administrator access is required to analyse or import Excel data.");
    }
}
