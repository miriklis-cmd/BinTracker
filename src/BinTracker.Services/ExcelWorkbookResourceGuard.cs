using System.Buffers.Binary;
using System.IO.Compression;
using System.Xml;

namespace BinTracker.Services;

/// <summary>
/// Stable workbook input limits shared by the desktop file boundary and the
/// provider-neutral import services.
/// </summary>
public static class ExcelWorkbookResourceLimits
{
    public const long MaximumFileBytes = 32L * 1024 * 1024;

    public static void EnsureFileLength(long length)
    {
        if (length > MaximumFileBytes)
        {
            throw new WorkbookResourceLimitException(
                $"The workbook is too large. BinTracker accepts workbooks up to " +
                $"{MaximumFileBytes / (1024 * 1024)} MiB.");
        }
    }
}

public sealed class WorkbookResourceLimitException(string message)
    : InvalidOperationException(message);

internal sealed record ExcelWorkbookResourceBudget(
    long MaximumFileBytes,
    int MaximumArchiveEntries,
    long MaximumCentralDirectoryBytes,
    long MaximumExpandedBytes,
    long MaximumEntryBytes,
    int MaximumXmlEntries,
    long MaximumXmlEntryBytes,
    long MaximumXmlElements,
    int MaximumWorksheets,
    int MaximumRowsPerWorksheet,
    int MaximumRowsTotal,
    int MaximumCellsPerWorksheet,
    int MaximumCellsTotal,
    int MaximumUsedColumnsPerWorksheet,
    long MaximumUsedRangeCellsPerWorksheet,
    long MaximumUsedRangeCellsTotal)
{
    public static ExcelWorkbookResourceBudget Default { get; } = new(
        ExcelWorkbookResourceLimits.MaximumFileBytes,
        MaximumArchiveEntries: 2_048,
        MaximumCentralDirectoryBytes: 8L * 1024 * 1024,
        MaximumExpandedBytes: 256L * 1024 * 1024,
        MaximumEntryBytes: 64L * 1024 * 1024,
        MaximumXmlEntries: 1_024,
        MaximumXmlEntryBytes: 32L * 1024 * 1024,
        MaximumXmlElements: 2_000_000,
        MaximumWorksheets: 64,
        MaximumRowsPerWorksheet: 100_000,
        MaximumRowsTotal: 200_000,
        MaximumCellsPerWorksheet: 250_000,
        MaximumCellsTotal: 500_000,
        MaximumUsedColumnsPerWorksheet: 256,
        MaximumUsedRangeCellsPerWorksheet: 500_000,
        MaximumUsedRangeCellsTotal: 1_000_000);
}

internal static class ExcelWorkbookResourceGuard
{
    private const uint EndOfCentralDirectorySignature = 0x06054b50;
    private const int EndOfCentralDirectoryLength = 22;
    private const int MaximumZipCommentLength = ushort.MaxValue;

    public static void ValidateForAnalysis(
        ImportSourceDocument source,
        CancellationToken cancellationToken = default) =>
        ValidateForAnalysis(source, ExcelWorkbookResourceBudget.Default, cancellationToken);

    internal static void ValidateForAnalysis(
        ImportSourceDocument source,
        ExcelWorkbookResourceBudget budget,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(source.Content);

        ValidateFileLength(source.Content.LongLength, budget.MaximumFileBytes);

        try
        {
            ValidateArchive(source.Content, budget, cancellationToken);
        }
        catch (WorkbookResourceLimitException)
        {
            throw;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex) when (ex is InvalidDataException or IOException or
                                   XmlException or NotSupportedException or
                                   OverflowException)
        {
            throw new InvalidOperationException(
                "The selected file is not a valid, readable Excel workbook.", ex);
        }
    }

    public static void ValidateFileLength(ImportSourceDocument source)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(source.Content);
        ExcelWorkbookResourceLimits.EnsureFileLength(source.Content.LongLength);
    }

    private static void ValidateFileLength(long length, long maximum)
    {
        if (length > maximum)
        {
            throw new WorkbookResourceLimitException(
                $"The workbook is too large. BinTracker accepts workbooks up to " +
                $"{maximum / (1024 * 1024)} MiB.");
        }
    }

    private static void ValidateArchive(
        byte[] content,
        ExcelWorkbookResourceBudget budget,
        CancellationToken cancellationToken)
    {
        var directory = ReadCentralDirectory(content, budget);
        using var stream = new MemoryStream(content, writable: false);
        using var archive = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: false);

        if (archive.Entries.Count != directory.EntryCount)
            throw new InvalidDataException("The ZIP directory entry count is inconsistent.");

        var entries = new Dictionary<string, ZipArchiveEntry>(StringComparer.OrdinalIgnoreCase);
        long declaredExpandedBytes = 0;

        foreach (var entry in archive.Entries)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!entries.TryAdd(entry.FullName, entry))
                throw new InvalidDataException("The workbook contains duplicate package entries.");

            if (entry.Length > budget.MaximumEntryBytes)
            {
                throw new WorkbookResourceLimitException(
                    $"Workbook package entry '{entry.FullName}' exceeds the " +
                    $"{ToMiB(budget.MaximumEntryBytes)} MiB entry limit.");
            }

            declaredExpandedBytes = checked(declaredExpandedBytes + entry.Length);
            if (declaredExpandedBytes > budget.MaximumExpandedBytes)
            {
                throw new WorkbookResourceLimitException(
                    $"The workbook expands beyond the {ToMiB(budget.MaximumExpandedBytes)} MiB limit.");
            }
        }

        if (!entries.TryGetValue("[Content_Types].xml", out var contentTypesEntry))
            throw new InvalidDataException("The workbook package has no content-type manifest.");

        var state = new ValidationState(budget);
        var contentTypes = ReadContentTypes(contentTypesEntry, state, cancellationToken);

        foreach (var entry in archive.Entries)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (ReferenceEquals(entry, contentTypesEntry) || IsDirectory(entry))
                continue;

            var contentType = contentTypes.GetContentType(entry.FullName);
            if (IsXmlContent(contentType, entry.FullName))
                ReadXmlPart(entry, state, cancellationToken);
            else
                DrainEntry(entry, state, cancellationToken);
        }

        state.Complete();
    }

    private static CentralDirectoryInfo ReadCentralDirectory(
        ReadOnlySpan<byte> content,
        ExcelWorkbookResourceBudget budget)
    {
        var minimumOffset = Math.Max(
            0,
            content.Length - EndOfCentralDirectoryLength - MaximumZipCommentLength);

        for (var offset = content.Length - EndOfCentralDirectoryLength;
             offset >= minimumOffset;
             offset--)
        {
            if (BinaryPrimitives.ReadUInt32LittleEndian(content[offset..]) !=
                EndOfCentralDirectorySignature)
            {
                continue;
            }

            var commentLength = BinaryPrimitives.ReadUInt16LittleEndian(content[(offset + 20)..]);
            if (offset + EndOfCentralDirectoryLength + commentLength != content.Length)
                continue;

            var diskNumber = BinaryPrimitives.ReadUInt16LittleEndian(content[(offset + 4)..]);
            var directoryDisk = BinaryPrimitives.ReadUInt16LittleEndian(content[(offset + 6)..]);
            var entriesOnDisk = BinaryPrimitives.ReadUInt16LittleEndian(content[(offset + 8)..]);
            var entryCount = BinaryPrimitives.ReadUInt16LittleEndian(content[(offset + 10)..]);
            var directorySize = BinaryPrimitives.ReadUInt32LittleEndian(content[(offset + 12)..]);
            var directoryOffset = BinaryPrimitives.ReadUInt32LittleEndian(content[(offset + 16)..]);

            if (diskNumber != 0 || directoryDisk != 0 || entriesOnDisk != entryCount)
                throw new InvalidDataException("Multi-disk workbook packages are not supported.");

            if (entryCount == ushort.MaxValue || directorySize == uint.MaxValue ||
                directoryOffset == uint.MaxValue)
            {
                throw new WorkbookResourceLimitException(
                    "ZIP64 workbook packages exceed BinTracker's package-entry budget.");
            }

            if (entryCount > budget.MaximumArchiveEntries)
            {
                throw new WorkbookResourceLimitException(
                    $"The workbook contains more than {budget.MaximumArchiveEntries:N0} package entries.");
            }

            if (directorySize > budget.MaximumCentralDirectoryBytes)
            {
                throw new WorkbookResourceLimitException(
                    $"The workbook package directory exceeds {ToMiB(budget.MaximumCentralDirectoryBytes)} MiB.");
            }

            if ((ulong)directoryOffset + directorySize > (ulong)offset)
                throw new InvalidDataException("The workbook ZIP directory is outside the package.");

            return new CentralDirectoryInfo(entryCount);
        }

        throw new InvalidDataException("The workbook ZIP directory is missing.");
    }

    private static ContentTypeCatalog ReadContentTypes(
        ZipArchiveEntry entry,
        ValidationState state,
        CancellationToken cancellationToken)
    {
        var catalog = new ContentTypeCatalog();
        ReadXml(entry, state, cancellationToken, reader =>
        {
            if (reader.LocalName.Equals("Default", StringComparison.Ordinal))
            {
                var extension = reader.GetAttribute("Extension");
                var contentType = reader.GetAttribute("ContentType");
                if (!string.IsNullOrWhiteSpace(extension) && !string.IsNullOrWhiteSpace(contentType))
                    catalog.Defaults[extension] = contentType;
            }
            else if (reader.LocalName.Equals("Override", StringComparison.Ordinal))
            {
                var partName = reader.GetAttribute("PartName");
                var contentType = reader.GetAttribute("ContentType");
                if (!string.IsNullOrWhiteSpace(partName) && !string.IsNullOrWhiteSpace(contentType))
                    catalog.Overrides[partName.TrimStart('/')] = contentType;
            }
        });
        return catalog;
    }

    private static void ReadXmlPart(
        ZipArchiveEntry entry,
        ValidationState state,
        CancellationToken cancellationToken)
    {
        WorksheetStatistics? worksheet = null;
        var rootSeen = false;
        var workbookPart = false;

        ReadXml(entry, state, cancellationToken, reader =>
        {
            if (!rootSeen)
            {
                rootSeen = true;
                workbookPart = reader.LocalName.Equals("workbook", StringComparison.Ordinal);
                if (reader.LocalName.Equals("worksheet", StringComparison.Ordinal))
                    worksheet = new WorksheetStatistics(state.Budget);
            }

            if (workbookPart && reader.LocalName.Equals("sheet", StringComparison.Ordinal))
                state.AddWorkbookSheet();

            worksheet?.ReadElement(reader);
        });

        if (worksheet is not null)
            state.AddWorksheet(worksheet);
    }

    private static void ReadXml(
        ZipArchiveEntry entry,
        ValidationState state,
        CancellationToken cancellationToken,
        Action<XmlReader> visitElement)
    {
        if (entry.Length > state.Budget.MaximumXmlEntryBytes)
        {
            throw new WorkbookResourceLimitException(
                $"Workbook XML part '{entry.FullName}' exceeds the " +
                $"{ToMiB(state.Budget.MaximumXmlEntryBytes)} MiB XML-part limit.");
        }

        state.AddXmlEntry();
        using var source = entry.Open();
        using var bounded = new BoundedReadStream(
            source,
            state,
            state.Budget.MaximumXmlEntryBytes,
            cancellationToken);
        using var reader = XmlReader.Create(bounded, new XmlReaderSettings
        {
            DtdProcessing = DtdProcessing.Prohibit,
            XmlResolver = null,
            MaxCharactersInDocument = state.Budget.MaximumXmlEntryBytes
        });

        while (reader.Read())
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (reader.NodeType != XmlNodeType.Element)
                continue;

            state.AddXmlElement();
            visitElement(reader);
        }
    }

    private static void DrainEntry(
        ZipArchiveEntry entry,
        ValidationState state,
        CancellationToken cancellationToken)
    {
        using var source = entry.Open();
        using var bounded = new BoundedReadStream(
            source,
            state,
            state.Budget.MaximumEntryBytes,
            cancellationToken);
        Span<byte> buffer = stackalloc byte[16 * 1024];
        while (bounded.Read(buffer) != 0)
        {
        }
    }

    private static bool IsXmlContent(string? contentType, string entryName)
    {
        var extension = Path.GetExtension(entryName);
        if (extension.Equals(".xml", StringComparison.OrdinalIgnoreCase) ||
            extension.Equals(".rels", StringComparison.OrdinalIgnoreCase) ||
            extension.Equals(".vml", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (string.IsNullOrWhiteSpace(contentType))
            return false;

        var parameterSeparator = contentType.IndexOf(';', StringComparison.Ordinal);
        var mediaType = (parameterSeparator >= 0
                ? contentType[..parameterSeparator]
                : contentType)
            .Trim();

        return mediaType.Equals("application/xml", StringComparison.OrdinalIgnoreCase) ||
               mediaType.Equals("text/xml", StringComparison.OrdinalIgnoreCase) ||
               mediaType.EndsWith("+xml", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsDirectory(ZipArchiveEntry entry) =>
        entry.FullName.EndsWith("/", StringComparison.Ordinal) && entry.Length == 0;

    private static long ToMiB(long bytes) => bytes / (1024 * 1024);

    private sealed record CentralDirectoryInfo(int EntryCount);

    private sealed class ContentTypeCatalog
    {
        public Dictionary<string, string> Defaults { get; } =
            new(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, string> Overrides { get; } =
            new(StringComparer.OrdinalIgnoreCase);

        public string? GetContentType(string entryName)
        {
            if (Overrides.TryGetValue(entryName, out var overridden))
                return overridden;

            var extension = Path.GetExtension(entryName).TrimStart('.');
            return Defaults.TryGetValue(extension, out var defaultType)
                ? defaultType
                : null;
        }
    }

    private sealed class ValidationState(ExcelWorkbookResourceBudget budget)
    {
        private long expandedBytes;
        private int xmlEntries;
        private long xmlElements;
        private int workbookSheets;
        private int worksheetParts;
        private int rows;
        private int cells;
        private long usedRangeCells;

        public ExcelWorkbookResourceBudget Budget { get; } = budget;

        public void AddExpandedBytes(int count)
        {
            expandedBytes = checked(expandedBytes + count);
            if (expandedBytes > Budget.MaximumExpandedBytes)
            {
                throw new WorkbookResourceLimitException(
                    $"The workbook expands beyond the {ToMiB(Budget.MaximumExpandedBytes)} MiB limit.");
            }
        }

        public void AddXmlEntry()
        {
            xmlEntries++;
            if (xmlEntries > Budget.MaximumXmlEntries)
            {
                throw new WorkbookResourceLimitException(
                    $"The workbook contains more than {Budget.MaximumXmlEntries:N0} XML parts.");
            }
        }

        public void AddXmlElement()
        {
            xmlElements++;
            if (xmlElements > Budget.MaximumXmlElements)
            {
                throw new WorkbookResourceLimitException(
                    $"The workbook XML contains more than {Budget.MaximumXmlElements:N0} elements.");
            }
        }

        public void AddWorkbookSheet()
        {
            workbookSheets++;
            if (workbookSheets > Budget.MaximumWorksheets)
            {
                throw new WorkbookResourceLimitException(
                    $"The workbook contains more than {Budget.MaximumWorksheets:N0} worksheets.");
            }
        }

        public void AddWorksheet(WorksheetStatistics worksheet)
        {
            worksheetParts++;
            if (worksheetParts > Budget.MaximumWorksheets)
            {
                throw new WorkbookResourceLimitException(
                    $"The workbook contains more than {Budget.MaximumWorksheets:N0} worksheet parts.");
            }

            worksheet.Validate();
            rows = checked(rows + worksheet.RowCount);
            cells = checked(cells + worksheet.CellCount);
            usedRangeCells = checked(usedRangeCells + worksheet.UsedRangeCells);

            if (rows > Budget.MaximumRowsTotal)
                throw new WorkbookResourceLimitException(
                    $"The workbook contains more than {Budget.MaximumRowsTotal:N0} worksheet rows.");
            if (cells > Budget.MaximumCellsTotal)
                throw new WorkbookResourceLimitException(
                    $"The workbook contains more than {Budget.MaximumCellsTotal:N0} cells.");
            if (usedRangeCells > Budget.MaximumUsedRangeCellsTotal)
            {
                throw new WorkbookResourceLimitException(
                    $"The workbook used ranges exceed {Budget.MaximumUsedRangeCellsTotal:N0} cells.");
            }
        }

        public void Complete()
        {
            if (workbookSheets == 0 || worksheetParts == 0)
                throw new InvalidDataException("The workbook contains no worksheets.");
        }
    }

    private sealed class WorksheetStatistics(ExcelWorkbookResourceBudget budget)
    {
        private int inferredRow;
        private int inferredColumn;
        private int minimumRow = int.MaxValue;
        private int maximumRow;
        private int minimumColumn = int.MaxValue;
        private int maximumColumn;

        public int RowCount { get; private set; }
        public int CellCount { get; private set; }
        public long UsedRangeCells { get; private set; }

        public void ReadElement(XmlReader reader)
        {
            if (reader.LocalName.Equals("row", StringComparison.Ordinal))
            {
                RowCount++;
                inferredColumn = 0;
                inferredRow = ParsePositiveInt(reader.GetAttribute("r")) ?? inferredRow + 1;
                return;
            }

            if (reader.LocalName.Equals("c", StringComparison.Ordinal))
            {
                CellCount++;
                var reference = reader.GetAttribute("r");
                if (TryParseCell(reference, out var row, out var column))
                {
                    inferredRow = row;
                    inferredColumn = column;
                }
                else
                {
                    inferredColumn++;
                }

                AddPoint(inferredRow, inferredColumn);
                return;
            }

            if (reader.LocalName.Equals("dimension", StringComparison.Ordinal) ||
                reader.LocalName.Equals("mergeCell", StringComparison.Ordinal))
            {
                AddRange(reader.GetAttribute("ref"));
            }
        }

        public void Validate()
        {
            if (RowCount > budget.MaximumRowsPerWorksheet)
            {
                throw new WorkbookResourceLimitException(
                    $"A worksheet contains more than {budget.MaximumRowsPerWorksheet:N0} rows.");
            }

            if (CellCount > budget.MaximumCellsPerWorksheet)
            {
                throw new WorkbookResourceLimitException(
                    $"A worksheet contains more than {budget.MaximumCellsPerWorksheet:N0} cells.");
            }

            if (maximumRow == 0 || maximumColumn == 0)
                return;

            var rowSpan = checked(maximumRow - minimumRow + 1);
            var columnSpan = checked(maximumColumn - minimumColumn + 1);
            if (rowSpan > budget.MaximumRowsPerWorksheet)
            {
                throw new WorkbookResourceLimitException(
                    $"A worksheet used range spans more than {budget.MaximumRowsPerWorksheet:N0} rows.");
            }

            if (columnSpan > budget.MaximumUsedColumnsPerWorksheet)
            {
                throw new WorkbookResourceLimitException(
                    $"A worksheet used range spans more than {budget.MaximumUsedColumnsPerWorksheet:N0} columns.");
            }

            UsedRangeCells = checked((long)rowSpan * columnSpan);
            if (UsedRangeCells > budget.MaximumUsedRangeCellsPerWorksheet)
            {
                throw new WorkbookResourceLimitException(
                    $"A worksheet used range exceeds {budget.MaximumUsedRangeCellsPerWorksheet:N0} cells.");
            }
        }

        private void AddRange(string? reference)
        {
            if (string.IsNullOrWhiteSpace(reference))
                return;

            var separator = reference.IndexOf(':');
            var first = separator < 0 ? reference : reference[..separator];
            var last = separator < 0 ? reference : reference[(separator + 1)..];
            if (TryParseCell(first, out var firstRow, out var firstColumn))
                AddPoint(firstRow, firstColumn);
            if (TryParseCell(last, out var lastRow, out var lastColumn))
                AddPoint(lastRow, lastColumn);
        }

        private void AddPoint(int row, int column)
        {
            if (row <= 0 || column <= 0)
                throw new InvalidDataException("A worksheet contains an invalid cell reference.");

            minimumRow = Math.Min(minimumRow, row);
            maximumRow = Math.Max(maximumRow, row);
            minimumColumn = Math.Min(minimumColumn, column);
            maximumColumn = Math.Max(maximumColumn, column);
        }

        private static int? ParsePositiveInt(string? value) =>
            int.TryParse(value, out var parsed) && parsed > 0 ? parsed : null;

        private static bool TryParseCell(string? reference, out int row, out int column)
        {
            row = 0;
            column = 0;
            if (string.IsNullOrWhiteSpace(reference))
                return false;

            var value = reference.AsSpan();
            var separator = value.LastIndexOf('!');
            if (separator >= 0)
                value = value[(separator + 1)..];

            var index = 0;
            while (index < value.Length && value[index] == '$')
                index++;

            var letterCount = 0;
            while (index < value.Length && char.IsAsciiLetter(value[index]))
            {
                column = checked(column * 26 + (char.ToUpperInvariant(value[index]) - 'A' + 1));
                index++;
                letterCount++;
            }

            while (index < value.Length && value[index] == '$')
                index++;

            var rowStart = index;
            while (index < value.Length && char.IsAsciiDigit(value[index]))
            {
                row = checked(row * 10 + (value[index] - '0'));
                index++;
            }

            return letterCount > 0 && index > rowStart && index == value.Length &&
                   row > 0 && column > 0;
        }
    }

    private sealed class BoundedReadStream(
        Stream inner,
        ValidationState state,
        long maximumEntryBytes,
        CancellationToken cancellationToken) : Stream
    {
        private long bytesRead;

        public override int Read(byte[] buffer, int offset, int count) =>
            Count(inner.Read(buffer, offset, count));

        public override int Read(Span<byte> buffer) => Count(inner.Read(buffer));

        private int Count(int count)
        {
            cancellationToken.ThrowIfCancellationRequested();
            bytesRead = checked(bytesRead + count);
            if (bytesRead > maximumEntryBytes)
                throw new WorkbookResourceLimitException("A workbook package entry exceeds its expansion limit.");
            state.AddExpandedBytes(count);
            return count;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                inner.Dispose();
            base.Dispose(disposing);
        }

        public override bool CanRead => inner.CanRead;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
        public override void Flush() => throw new NotSupportedException();
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }
}
