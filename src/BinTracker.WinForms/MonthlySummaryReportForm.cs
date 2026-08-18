using BinTracker.Core;
using BinTracker.Services;

namespace BinTracker.WinForms;

public sealed class MonthlySummaryReportForm : BinTrackerForm
{
    private readonly IMonthlySummaryReportService reports;
    private readonly IMonthlySummaryReportPdfService pdfReports;
    private readonly IContainerTypeService containerTypes;

    private readonly DateTimePicker monthPicker = new()
    {
        Format = DateTimePickerFormat.Custom,
        CustomFormat = "MMMM yyyy",
        ShowUpDown = true,
        Width = 205,
        Value = DateTime.Today
    };

    private readonly TextBox customerSearch = new()
    {
        Width = 220,
        PlaceholderText = "Type, then press Enter"
    };

    private readonly ComboBox containerFilter = ChoiceBox(180);
    private readonly ComboBox sourceFilter = ChoiceBox(160);

    private readonly CheckBox includeAdjustments = new()
    {
        Text = "Include opening adjustments",
        AutoSize = true
    };

    private readonly Label summary = new()
    {
        AutoSize = true,
        ForeColor = Color.FromArgb(60, 75, 95),
        MaximumSize = new Size(1450, 0)
    };

    private readonly DataGridView grid = new()
    {
        Dock = DockStyle.Fill,
        ReadOnly = true,
        AllowUserToAddRows = false,
        AllowUserToDeleteRows = false,
        AllowUserToResizeRows = false,
        MultiSelect = false,
        SelectionMode = DataGridViewSelectionMode.FullRowSelect,
        RowHeadersVisible = false,
        AutoGenerateColumns = false,
        BackgroundColor = Color.White,
        BorderStyle = BorderStyle.FixedSingle,
        ScrollBars = ScrollBars.Both
    };

    private readonly IAuditService audit;
    private MonthlySummaryReportResult? current;
    private bool autoRefreshReady;

    public MonthlySummaryReportForm(
        IMonthlySummaryReportService reports,
        IMonthlySummaryReportPdfService pdfReports,
        IContainerTypeService containerTypes,
        IAuditService audit)
    {
        this.reports = reports;
        this.pdfReports = pdfReports;
        this.containerTypes = containerTypes;

        this.audit = audit;

        Text = "Monthly Summary";
        StartPosition = FormStartPosition.Manual;
        AutoScaleMode = AutoScaleMode.Dpi;
        MinimumSize = new Size(1050, 700);
        Font = new Font("Segoe UI", 10F);
        BackColor = Color.FromArgb(245, 247, 250);

        monthPicker.MaxDate = DateTime.Today;

        monthPicker.ValueChanged += async (_, _) =>
        {
            if (autoRefreshReady)
                await LoadReportAsync();
        };

        customerSearch.KeyDown += async (_, e) =>
        {
            if (e.KeyCode != Keys.Enter)
                return;

            e.SuppressKeyPress = true;

            if (autoRefreshReady)
                await LoadReportAsync();
        };

        containerFilter.SelectedIndexChanged += async (_, _) =>
        {
            if (autoRefreshReady)
                await LoadReportAsync();
        };

        sourceFilter.SelectedIndexChanged += async (_, _) =>
        {
            if (autoRefreshReady)
                await LoadReportAsync();
        };

        includeAdjustments.CheckedChanged += async (_, _) =>
        {
            if (autoRefreshReady)
                await LoadReportAsync();
        };

        Build();

        Load += async (_, _) =>
        {
            ApplyResponsiveBounds();
            await InitialiseAsync();
        };
    }

    private void Build()
    {
        ConfigureGrid();

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 5,
            Padding = new Padding(18)
        };
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var header = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            ColumnCount = 1,
            RowCount = 2,
            BackColor = Color.White,
            Padding = new Padding(18, 12, 18, 12),
            Margin = new Padding(0, 0, 0, 10)
        };

        header.Controls.Add(new Label
        {
            Text = "Monthly Summary",
            AutoSize = true,
            Font = new Font("Segoe UI Semibold", 19F, FontStyle.Bold),
            Margin = new Padding(0, 0, 0, 5)
        }, 0, 0);

        header.Controls.Add(new Label
        {
            Text =
                "Monthly OUT, IN and net movement totals by customer and container. Opening adjustments are excluded by default.",
            AutoSize = true,
            ForeColor = Color.FromArgb(80, 90, 105),
            MaximumSize = new Size(1250, 0)
        }, 0, 1);

        root.Controls.Add(header, 0, 0);

        var controlsCard = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 1,
            RowCount = 3,
            BackColor = Color.White,
            Padding = new Padding(16, 12, 16, 12),
            Margin = new Padding(0, 0, 0, 10)
        };
        controlsCard.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        controlsCard.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        controlsCard.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var filters = Flow();
        filters.Controls.Add(ControlLabel("Month"));
        filters.Controls.Add(monthPicker);
        filters.Controls.Add(ControlLabel("Customer", 16));
        filters.Controls.Add(customerSearch);
        filters.Controls.Add(new Label
        {
            Text = "Press Enter to search",
            AutoSize = true,
            ForeColor = Color.DimGray,
            Margin = new Padding(8, 9, 0, 0)
        });
        filters.Controls.Add(ControlLabel("Container", 16));
        filters.Controls.Add(containerFilter);
        filters.Controls.Add(ControlLabel("Source", 16));
        filters.Controls.Add(sourceFilter);

        var options = Flow();
        options.Padding = new Padding(0, 8, 0, 2);
        options.Controls.Add(includeAdjustments);

        var actions = Flow();
        actions.Padding = new Padding(0, 8, 0, 4);

        actions.Controls.Add(ActionButton(
            "This Month",
            125,
            async () => await SetMonthAndRefreshAsync(DateTime.Today)));

        actions.Controls.Add(ActionButton(
            "Last Month",
            125,
            async () => await SetMonthAndRefreshAsync(
                DateTime.Today.AddMonths(-1))));

        actions.Controls.Add(ActionButton(
            "Generate PDF",
            145,
            async () => await GeneratePdfAsync(false)));

        actions.Controls.Add(ActionButton(
            "Generate && Open",
            175,
            async () => await GeneratePdfAsync(true)));

        var csv = new Button
        {
            Text = "Export CSV",
            Size = new Size(135, 40),
            Margin = new Padding(8, 0, 0, 0)
        };
        csv.Click += async (_, _) => await ExportCsvAsync();
        actions.Controls.Add(csv);

        controlsCard.Controls.Add(filters, 0, 0);
        controlsCard.Controls.Add(options, 0, 1);
        controlsCard.Controls.Add(actions, 0, 2);

        root.Controls.Add(controlsCard, 0, 1);

        var summaryCard = new Panel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            BackColor = Color.White,
            Padding = new Padding(16, 10, 16, 10),
            Margin = new Padding(0, 0, 0, 10)
        };
        summaryCard.Controls.Add(summary);
        root.Controls.Add(summaryCard, 0, 2);

        var gridCard = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.White,
            Padding = new Padding(10)
        };
        gridCard.Controls.Add(ReportGridMultiSort.Wrap(grid));
        root.Controls.Add(gridCard, 0, 3);

        var close = new Button
        {
            Text = "Close",
            Size = new Size(110, 38),
            Margin = new Padding(0, 10, 0, 0)
        };
        close.Click += (_, _) => Close();

        var footer = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            FlowDirection = FlowDirection.RightToLeft
        };
        footer.Controls.Add(close);
        root.Controls.Add(footer, 0, 4);

        Controls.Add(root);
    }

    private async Task InitialiseAsync()
    {
        containerFilter.Items.Add(
            new Choice<int?>(null, "All containers"));

        var configured = await containerTypes.SearchAsync(
            search: null,
            includeInactive: true);

        foreach (var container in configured)
        {
            var label = container.IsActive
                ? container.Name
                : $"{container.Name} (inactive)";

            containerFilter.Items.Add(
                new Choice<int?>(container.Id, label));
        }

        containerFilter.SelectedIndex = 0;

        sourceFilter.Items.Add(
            new Choice<MovementSource?>(null, "All sources"));
        sourceFilter.Items.Add(
            new Choice<MovementSource?>(
                MovementSource.Manual,
                "Single Entry"));
        sourceFilter.Items.Add(
            new Choice<MovementSource?>(
                MovementSource.Batch,
                "Batch Entry"));
        sourceFilter.Items.Add(
            new Choice<MovementSource?>(
                MovementSource.ExcelImport,
                "Excel Import"));
        sourceFilter.SelectedIndex = 0;

        autoRefreshReady = true;
        await LoadReportAsync();
    }

    private async Task SetMonthAndRefreshAsync(DateTime month)
    {
        autoRefreshReady = false;
        monthPicker.Value = month;
        autoRefreshReady = true;
        await LoadReportAsync();
    }

    private async Task LoadReportAsync()
    {
        try
        {
            Enabled = false;
            UseWaitCursor = true;

            current = await reports.QueryAsync(
                new MonthlySummaryReportQuery(
                    DateOnly.FromDateTime(monthPicker.Value.Date),
                    customerSearch.Text,
                    (containerFilter.SelectedItem as Choice<int?>)?.Value,
                    (sourceFilter.SelectedItem as Choice<MovementSource?>)?.Value,
                    includeAdjustments.Checked));

            grid.Rows.Clear();

            foreach (var row in current.Rows)
            {
                var index = grid.Rows.Add(
                    row.CustomerCode,
                    row.CustomerName,
                    row.ContainerType,
                    row.OutQuantity.ToString("N0"),
                    row.InQuantity.ToString("N0"),
                    row.NetQuantity.ToString("N0"));

                grid.Rows[index].Tag = row;
            }

            AdjustGridWidths();
            ReportGridMultiSort.Reapply(grid);

            var period =
                current.DataThroughDate < current.MonthEnd
                    ? $"{current.MonthStart:MMMM yyyy} • activity through " +
                      $"{current.DataThroughDate:dd/MM/yyyy}"
                    : $"{current.MonthStart:MMMM yyyy}";

            var containerText = current.ContainerTotals.Count == 0
                ? "No matching movements."
                : string.Join(
                    "   •   ",
                    current.ContainerTotals.Select(x =>
                        $"{x.ContainerType}: " +
                        $"{x.OutQuantity:N0} OUT / " +
                        $"{x.InQuantity:N0} IN / " +
                        $"Net {x.NetQuantity:+#;-#;0}"));

            summary.Text =
                $"{period} — {current.OutQuantity:N0} OUT • " +
                $"{current.InQuantity:N0} IN • " +
                $"Net {current.NetQuantity:+#;-#;0}" +
                Environment.NewLine +
                containerText;
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                this,
                ex.Message,
                "Monthly Summary",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
        finally
        {
            Enabled = true;
            UseWaitCursor = false;
        }
    }

    private MonthlySummaryReportResult BuildDisplayedResult()
    {
        var source = current ??
            throw new InvalidOperationException("Open the report first.");

        var rows = grid.Rows
            .Cast<DataGridViewRow>()
            .Where(x => !x.IsNewRow)
            .Select(x => x.Tag as MonthlySummaryReportRow)
            .Where(x => x is not null)
            .Cast<MonthlySummaryReportRow>()
            .ToList();

        var totals = rows
            .GroupBy(x => new
            {
                x.ContainerTypeId,
                x.ContainerType,
                x.ContainerDisplayOrder
            })
            .Select(g => new MonthlySummaryContainerTotal(
                g.Key.ContainerTypeId,
                g.Key.ContainerType,
                g.Key.ContainerDisplayOrder,
                g.Sum(x => x.OutQuantity),
                g.Sum(x => x.InQuantity)))
            .OrderBy(x => x.DisplayOrder)
            .ThenBy(x => x.ContainerType)
            .ToList();

        return new MonthlySummaryReportResult(
            source.MonthStart,
            source.MonthEnd,
            source.DataThroughDate,
            rows,
            totals);
    }

    private async Task GeneratePdfAsync(bool openAfter)
    {
        if (current is null)
        {
            MessageBox.Show(this, "Open the report first.");
            return;
        }

        var result = BuildDisplayedResult();

        using var dialog = new SaveFileDialog
        {
            Title = "Generate Monthly Summary PDF",
            Filter = "PDF file (*.pdf)|*.pdf",
            FileName =
                $"BinTracker_Monthly_Summary_{result.MonthStart:yyyyMM}.pdf",
            AddExtension = true,
            DefaultExt = "pdf"
        };

        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;

        try
        {
            Enabled = false;
            UseWaitCursor = true;

            await pdfReports.GeneratePdfAsync(
                result,
                dialog.FileName);

            if (openAfter)
            {
                System.Diagnostics.Process.Start(
                    new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = dialog.FileName,
                        UseShellExecute = true
                    });
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                this,
                ex.Message,
                "Monthly Summary PDF",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
        finally
        {
            Enabled = true;
            UseWaitCursor = false;
        }
    }

    private async Task ExportCsvAsync()
    {
        if (current is null)
        {
            MessageBox.Show(this, "Open the report first.");
            return;
        }

        var result = BuildDisplayedResult();

        using var dialog = new SaveFileDialog
        {
            Title = "Export Monthly Summary",
            Filter = "CSV file (*.csv)|*.csv",
            FileName =
                $"BinTracker_Monthly_Summary_{result.MonthStart:yyyyMM}.csv",
            AddExtension = true,
            DefaultExt = "csv"
        };

        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;

        using var writer = new StreamWriter(
            dialog.FileName,
            false,
            new System.Text.UTF8Encoding(true));

        writer.WriteLine(
            "Customer Code,Customer Name,Container,OUT,IN,Net");

        foreach (var row in result.Rows)
        {
            writer.WriteLine(string.Join(",",
                Csv(row.CustomerCode),
                Csv(row.CustomerName),
                Csv(row.ContainerType),
                row.OutQuantity.ToString(
                    System.Globalization.CultureInfo.InvariantCulture),
                row.InQuantity.ToString(
                    System.Globalization.CultureInfo.InvariantCulture),
                row.NetQuantity.ToString(
                    System.Globalization.CultureInfo.InvariantCulture)));
        }

        writer.Flush();

        await ReportCsvAudit.WriteAsync(
            this, audit,
            "MONTHLY_SUMMARY_CSV_EXPORTED",
            result.MonthStart.ToString("yyyy-MM"),
            $"Monthly Summary CSV exported for {result.MonthStart:MMMM yyyy}: {result.Rows.Count:N0} row(s), {result.OutQuantity:N0} OUT, {result.InQuantity:N0} IN, Net {result.NetQuantity:+#;-#;0}.",
            dialog.FileName,
            result.Rows.Count,
            new
            {
                result.MonthStart,
                result.MonthEnd,
                result.DataThroughDate,
                CustomerSearch = customerSearch.Text.Trim(),
                Container = containerFilter.Text,
                Source = sourceFilter.Text,
                IncludeAdjustments = includeAdjustments.Checked
            });
    }

    private void ConfigureGrid()
    {
        grid.Columns.Add(Column("Code", 155, "Code"));
        grid.Columns.Add(Column(
            "Customer",
            270,
            "Customer",
            DataGridViewAutoSizeColumnMode.Fill));
        grid.Columns.Add(Column("Container", 160, "Container"));
        grid.Columns.Add(Column("OUT", 110, "Out"));
        grid.Columns.Add(Column("IN", 110, "In"));
        grid.Columns.Add(Column("Net", 110, "Net"));

        grid.SortCompare += (_, e) =>
        {
            if (grid.Rows[e.RowIndex1].Tag is not MonthlySummaryReportRow left ||
                grid.Rows[e.RowIndex2].Tag is not MonthlySummaryReportRow right)
                return;

            e.SortResult = grid.Columns[e.Column.Index].Name switch
            {
                "Out" => left.OutQuantity.CompareTo(right.OutQuantity),
                "In" => left.InQuantity.CompareTo(right.InQuantity),
                "Net" => left.NetQuantity.CompareTo(right.NetQuantity),
                _ => 0
            };

            if (grid.Columns[e.Column.Index].Name is "Out" or "In" or "Net")
                e.Handled = true;
        };
    }

    private void AdjustGridWidths()
    {
        if (current is null)
            return;

        grid.Columns["Code"].Width = WidthFor(
            "Code",
            current.Rows.Select(x => x.CustomerCode),
            155,
            300);

        grid.Columns["Container"].Width = WidthFor(
            "Container",
            current.Rows.Select(x => x.ContainerType),
            160,
            260);
    }

    private int WidthFor(
        string columnName,
        IEnumerable<string> values,
        int minimum,
        int maximum)
    {
        var header = grid.Columns[columnName].HeaderText;
        var longest = values
            .Append(header)
            .OrderByDescending(x => x?.Length ?? 0)
            .FirstOrDefault() ?? header;

        return Math.Clamp(
            TextRenderer.MeasureText(longest, grid.Font).Width + 34,
            minimum,
            maximum);
    }

    private void ApplyResponsiveBounds()
    {
        var screen = Owner is not null
            ? Screen.FromControl(Owner)
            : Screen.FromPoint(Cursor.Position);

        var area = screen.WorkingArea;

        Width = Math.Clamp(
            (int)Math.Round(area.Width * 0.90),
            MinimumSize.Width,
            1800);

        Height = Math.Clamp(
            (int)Math.Round(area.Height * 0.86),
            MinimumSize.Height,
            1050);

        Left = area.Left + Math.Max(0, (area.Width - Width) / 2);
        Top = area.Top + Math.Max(0, (area.Height - Height) / 2);
    }

    private static FlowLayoutPanel Flow() =>
        new()
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true,
            Margin = Padding.Empty
        };

    private static Button ActionButton(
        string text,
        int width,
        Func<Task> action)
    {
        var button = new Button
        {
            Text = text,
            Size = new Size(width, 40),
            Margin = new Padding(8, 0, 0, 0)
        };

        button.Click += async (_, _) => await action();
        return button;
    }

    private static Label ControlLabel(
        string text,
        int left = 0) =>
        new()
        {
            Text = text,
            AutoSize = true,
            Margin = new Padding(left, 9, 8, 0)
        };

    private static ComboBox ChoiceBox(int width) =>
        new()
        {
            Width = width,
            DropDownStyle = ComboBoxStyle.DropDownList
        };

    private static DataGridViewTextBoxColumn Column(
        string header,
        int width,
        string name,
        DataGridViewAutoSizeColumnMode autoSize =
            DataGridViewAutoSizeColumnMode.None) =>
        new()
        {
            Name = name,
            HeaderText = header,
            Width = width,
            MinimumWidth = Math.Min(width, 90),
            AutoSizeMode = autoSize,
            SortMode = DataGridViewColumnSortMode.Automatic
        };

    private static string Csv(string value)
    {
        var escaped = value.Replace("\"", "\"\"");

        return value.Contains(',') ||
               value.Contains('"') ||
               value.Contains('\r') ||
               value.Contains('\n')
            ? $"\"{escaped}\""
            : escaped;
    }

    private sealed record Choice<T>(
        T Value,
        string Text)
    {
        public override string ToString() => Text;
    }
}
