namespace BinTracker.WinForms;

internal sealed record MovementHistoryColumnWidth(
    string Name,
    int PreferredWidth,
    int MinimumWidth,
    double ExtraWeight = 0);

internal static class MovementHistoryColumnLayout
{
    internal const string MovementIdHeader = "Movement\nID";
    internal const string EnteredByHeader = "Entered\nby";

    internal static void ConfigureWrappedHeaders(DataGridView grid)
    {
        ArgumentNullException.ThrowIfNull(grid);
        grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
        grid.ColumnHeadersDefaultCellStyle.WrapMode = DataGridViewTriState.True;
        grid.ColumnHeadersHeight = Math.Max(
            grid.ColumnHeadersHeight,
            (grid.ColumnHeadersDefaultCellStyle.Font ?? grid.Font).Height * 2 + 12);
    }

    internal static IReadOnlyList<MovementHistoryColumnWidth> CreateSpecifications(
        int dateContentWidth,
        Func<string, int> headerMinimum)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(dateContentWidth);
        ArgumentNullException.ThrowIfNull(headerMinimum);

        int Preferred(string name, int contentWidth) =>
            Math.Max(headerMinimum(name), contentWidth);

        return
        [
            new("Date", Preferred("Date", dateContentWidth), dateContentWidth),
            new("MovementId", Preferred("MovementId", 82), headerMinimum("MovementId")),
            new("Code", Preferred("Code", 72), headerMinimum("Code")),
            new("Customer", Preferred("Customer", 145), Math.Max(headerMinimum("Customer"), 105), 0.15),
            new("Type", Preferred("Type", 72), headerMinimum("Type")),
            new("Container", Preferred("Container", 105), Math.Max(headerMinimum("Container"), 88)),
            new("Direction", Preferred("Direction", 84), headerMinimum("Direction")),
            new("Quantity", Preferred("Quantity", 52), headerMinimum("Quantity")),
            new("Source", Preferred("Source", 88), headerMinimum("Source")),
            new("Reference", Preferred("Reference", 105), Math.Max(headerMinimum("Reference"), 82), 0.12),
            new("Status", Preferred("Status", 190), Math.Max(headerMinimum("Status"), 150), 0.28),
            new("Notes", Preferred("Notes", 240), Math.Max(headerMinimum("Notes"), 180), 0.45),
            new("EnteredBy", Preferred("EnteredBy", 86), headerMinimum("EnteredBy"))
        ];
    }

    public static IReadOnlyDictionary<string, int> Allocate(
        int availableWidth,
        IReadOnlyList<MovementHistoryColumnWidth> columns)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(availableWidth);
        ArgumentNullException.ThrowIfNull(columns);

        if (columns.Count == 0)
            throw new ArgumentException("At least one column is required.", nameof(columns));
        if (columns.Select(column => column.Name).Distinct(StringComparer.Ordinal).Count() != columns.Count)
            throw new ArgumentException("Column names must be unique.", nameof(columns));
        if (columns.Any(column => column.MinimumWidth <= 0 || column.PreferredWidth < column.MinimumWidth))
            throw new ArgumentException("Each preferred width must be at least its positive minimum width.", nameof(columns));

        var widths = columns.ToDictionary(
            column => column.Name,
            column => column.PreferredWidth,
            StringComparer.Ordinal);
        var overflow = widths.Values.Sum() - availableWidth;

        // Compact structural columns yield before the semantic fields. Among the
        // semantic fields, lower growth weights yield first so Notes and Status
        // retain their useful wrapping width for as long as possible.
        foreach (var column in columns.OrderBy(column => column.ExtraWeight))
        {
            if (overflow <= 0)
                break;

            var reduction = Math.Min(
                overflow,
                widths[column.Name] - column.MinimumWidth);
            widths[column.Name] -= reduction;
            overflow -= reduction;
        }

        if (overflow > 0)
        {
            // The host is narrower than the measured safe minimum. Retain readable
            // headers rather than silently hiding evidence; the caller may then scroll.
            foreach (var column in columns)
                widths[column.Name] = column.MinimumWidth;
            return widths;
        }

        var remaining = availableWidth - widths.Values.Sum();
        var weighted = columns.Where(column => column.ExtraWeight > 0).ToArray();
        var totalWeight = weighted.Sum(column => column.ExtraWeight);
        var assigned = 0;
        for (var index = 0; index < weighted.Length; index++)
        {
            var column = weighted[index];
            var extra = index == weighted.Length - 1
                ? remaining - assigned
                : (int)Math.Floor(remaining * column.ExtraWeight / totalWeight);
            widths[column.Name] += extra;
            assigned += extra;
        }

        return widths;
    }
}
