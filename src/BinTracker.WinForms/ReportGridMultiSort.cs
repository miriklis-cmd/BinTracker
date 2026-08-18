using System.Collections;
using System.Globalization;
using System.Text.RegularExpressions;

namespace BinTracker.WinForms;

internal static class ReportGridMultiSort
{
    private sealed record SortCriterion(string ColumnName, SortOrder Direction);
    private static readonly Dictionary<DataGridView, List<SortCriterion>> States = new();
    private static readonly Dictionary<DataGridView, Dictionary<string, Func<DataGridViewRow, object?>>> TypedValueSelectors = new();
    private static readonly Regex LeadingNumber = new(
        @"^\s*([+-]?(?:\d{1,3}(?:,\d{3})*|\d+)(?:\.\d+)?)\b",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public static Control Wrap(DataGridView grid)
    {
        Attach(grid);
        var host = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        host.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        host.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        host.Controls.Add(new Label
        {
            Text = "Sort: click a column heading • Shift+click to add another column",
            AutoSize = true,
            ForeColor = Color.DimGray,
            Font = new Font("Segoe UI", 8.5F),
            Margin = new Padding(2, 0, 0, 6)
        }, 0, 0);
        grid.Dock = DockStyle.Fill;
        host.Controls.Add(grid, 0, 1);
        return host;
    }

    public static void Attach(DataGridView grid)
    {
        if (States.ContainsKey(grid)) return;
        States[grid] = [];
        TypedValueSelectors[grid] = new(StringComparer.Ordinal);
        foreach (DataGridViewColumn column in grid.Columns)
            ConfigureColumn(column);

        grid.ColumnAdded += (_, e) => ConfigureColumn(e.Column);
        grid.ColumnHeaderMouseClick += OnColumnHeaderMouseClick;
    }


    public static void SetTypedSortValue(
        DataGridView grid,
        string columnName,
        Func<DataGridViewRow, object?> selector)
    {
        Attach(grid);
        TypedValueSelectors[grid][columnName] = selector;
    }

    public static void Reapply(DataGridView grid)
    {
        if (!States.TryGetValue(grid, out var state) || state.Count == 0 || grid.Rows.Count <= 1)
            return;

        ApplySort(grid, state);
    }

    private static void ConfigureColumn(DataGridViewColumn column)
    {
        column.SortMode = DataGridViewColumnSortMode.Programmatic;
        column.HeaderCell.ToolTipText = "Click to sort. Shift+click adds another sort level.";
    }

    private static void OnColumnHeaderMouseClick(object? sender, DataGridViewCellMouseEventArgs e)
    {
        if (sender is not DataGridView grid || e.ColumnIndex < 0 || grid.Rows.Count <= 1) return;
        var state = States[grid];
        var column = grid.Columns[e.ColumnIndex];
        var shift = (Control.ModifierKeys & Keys.Shift) == Keys.Shift;
        var existing = state.FindIndex(x => x.ColumnName == column.Name);

        if (!shift)
        {
            var direction = existing == 0 && state.Count == 1 && state[0].Direction == SortOrder.Ascending
                ? SortOrder.Descending : SortOrder.Ascending;
            state.Clear();
            state.Add(new SortCriterion(column.Name, direction));
        }
        else if (existing >= 0)
        {
            var item = state[existing];
            state[existing] = item with { Direction = item.Direction == SortOrder.Ascending ? SortOrder.Descending : SortOrder.Ascending };
        }
        else
        {
            state.Add(new SortCriterion(column.Name, SortOrder.Ascending));
        }

        ApplySort(grid, state);
    }

    private static void ApplySort(DataGridView grid, IReadOnlyList<SortCriterion> state)
    {
        foreach (DataGridViewColumn c in grid.Columns)
        {
            c.HeaderCell.SortGlyphDirection = SortOrder.None;
            c.HeaderCell.ToolTipText = "Click to sort. Shift+click adds another sort level.";
        }

        for (var i = 0; i < state.Count; i++)
        {
            var item = state[i];
            if (grid.Columns[item.ColumnName] is not DataGridViewColumn c) continue;
            c.HeaderCell.SortGlyphDirection = item.Direction;
            c.HeaderCell.ToolTipText =
                $"Sort {i + 1}: {item.Direction}. Shift+click another column to add the next level.";
        }

        var originalOrder = grid.Rows.Cast<DataGridViewRow>()
            .Where(row => !row.IsNewRow)
            .Select((row, index) => (row, index))
            .ToDictionary(x => x.row, x => x.index);
        var typedSelectors = TypedValueSelectors.TryGetValue(grid, out var selectors)
            ? selectors
            : new Dictionary<string, Func<DataGridViewRow, object?>>(StringComparer.Ordinal);
        grid.Sort(new GridComparer(state.ToArray(), originalOrder, typedSelectors));
    }

    private sealed class GridComparer(
        IReadOnlyList<SortCriterion> criteria,
        IReadOnlyDictionary<DataGridViewRow, int> originalOrder,
        IReadOnlyDictionary<string, Func<DataGridViewRow, object?>> typedSelectors) : IComparer
    {
        public int Compare(object? x, object? y)
        {
            if (x is not DataGridViewRow left || y is not DataGridViewRow right) return 0;
            foreach (var criterion in criteria)
            {
                var a = typedSelectors.TryGetValue(criterion.ColumnName, out var selector)
                    ? selector(left)
                    : left.Cells[criterion.ColumnName].Value;
                var b = typedSelectors.TryGetValue(criterion.ColumnName, out selector)
                    ? selector(right)
                    : right.Cells[criterion.ColumnName].Value;
                var result = CompareValues(a, b);
                if (result != 0) return criterion.Direction == SortOrder.Descending ? -result : result;
            }

            var leftOrder = originalOrder.TryGetValue(left, out var li) ? li : int.MaxValue;
            var rightOrder = originalOrder.TryGetValue(right, out var ri) ? ri : int.MaxValue;
            return leftOrder.CompareTo(rightOrder);
        }
    }

    internal static int CompareValues(object? a, object? b)
    {
        if (ReferenceEquals(a, b)) return 0;
        if (a is null || a is DBNull) return -1;
        if (b is null || b is DBNull) return 1;

        // Date-looking strings must be tested before the leading-number fallback;
        // otherwise a value such as 18/08/2026 would be compared as the number 18.
        if (TryDate(a, out var dateA) && TryDate(b, out var dateB))
            return dateA.CompareTo(dateB);

        if (TryDecimal(a, out var da) && TryDecimal(b, out var db))
            return da.CompareTo(db);

        if (a is IComparable ca && a.GetType() == b.GetType())
            return ca.CompareTo(b);

        return string.Compare(
            Convert.ToString(a, CultureInfo.CurrentCulture),
            Convert.ToString(b, CultureInfo.CurrentCulture),
            StringComparison.CurrentCultureIgnoreCase);
    }

    private static bool TryDecimal(object value, out decimal number)
    {
        switch (value)
        {
            case byte v: number = v; return true;
            case sbyte v: number = v; return true;
            case short v: number = v; return true;
            case ushort v: number = v; return true;
            case int v: number = v; return true;
            case uint v: number = v; return true;
            case long v: number = v; return true;
            case ulong v: number = v; return true;
            case decimal v: number = v; return true;
            case float v when !float.IsNaN(v) && !float.IsInfinity(v): number = (decimal)v; return true;
            case double v when !double.IsNaN(v) && !double.IsInfinity(v): number = (decimal)v; return true;
        }

        var text = Convert.ToString(value, CultureInfo.CurrentCulture)?.Trim();
        if (string.IsNullOrEmpty(text))
        {
            number = default;
            return false;
        }

        if (decimal.TryParse(text, NumberStyles.Number | NumberStyles.AllowLeadingSign,
                CultureInfo.CurrentCulture, out number))
            return true;

        var match = LeadingNumber.Match(text);
        if (match.Success && decimal.TryParse(
                match.Groups[1].Value,
                NumberStyles.Number | NumberStyles.AllowLeadingSign,
                CultureInfo.InvariantCulture,
                out number))
        {
            // Formatted container positions display credits as an absolute
            // magnitude plus the word CREDIT. Their business value is signed:
            // CREDIT is negative, OUT is positive. Keep this generic fallback
            // for report grids that expose formatted position text.
            if (text.Contains("CREDIT", StringComparison.OrdinalIgnoreCase))
                number = -Math.Abs(number);
            return true;
        }

        number = default;
        return false;
    }

    private static bool TryDate(object value, out DateTime date)
    {
        if (value is DateTime dt)
        {
            date = dt;
            return true;
        }

        var text = Convert.ToString(value, CultureInfo.CurrentCulture)?.Trim();
        if (string.IsNullOrEmpty(text) || text == "—")
        {
            date = default;
            return false;
        }

        var formats = new[]
        {
            "dd/MM/yyyy", "d/M/yyyy", "dd/MM/yy", "d/M/yy",
            "ddd dd/MM/yyyy", "ddd d/M/yyyy", "ddd dd/MM", "ddd d/M"
        };
        return DateTime.TryParseExact(
            text,
            formats,
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out date);
    }
}
