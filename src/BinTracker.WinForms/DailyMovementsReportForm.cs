using BinTracker.Core;
using BinTracker.Services;

namespace BinTracker.WinForms;

public sealed class DailyMovementsReportForm : Form
{
    private readonly IDailyMovementsReportService reports;
    private readonly IDailyMovementsReportPdfService pdfReports;
    private readonly IOutstandingReportService outstanding;

    private readonly DateTimePicker reportDate = new()
    {
        Format = DateTimePickerFormat.Short,
        Width = 140,
        Value = DateTime.Today
    };

    private readonly TextBox customerSearch = new()
    {
        Width = 220,
        PlaceholderText = "Customer code or name"
    };

    private readonly ComboBox containerFilter = ChoiceBox(175);
    private readonly ComboBox directionFilter = ChoiceBox(165);
    private readonly ComboBox sourceFilter = ChoiceBox(155);

    private readonly CheckBox includeAdjustments = new()
    {
        Text = "Include opening adjustments",
        AutoSize = true,
        Margin = new Padding(14, 9, 0, 0)
    };

    private readonly CheckBox includeNotesInPdf = new()
    {
        Text = "Include notes in exports",
        AutoSize = true,
        Margin = new Padding(18, 9, 0, 0)
    };

    private readonly Label summary = new()
    {
        AutoSize = true,
        ForeColor = Color.FromArgb(60, 75, 95),
        MaximumSize = new Size(1350, 0)
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

    private DailyMovementsReportResult? currentResult;

    public DailyMovementsReportForm(
        IDailyMovementsReportService reports,
        IDailyMovementsReportPdfService pdfReports,
        IOutstandingReportService outstanding)
    {
        this.reports = reports;
        this.pdfReports = pdfReports;
        this.outstanding = outstanding;

        Text = "Daily Movements";
        StartPosition = FormStartPosition.Manual;
        AutoScaleMode = AutoScaleMode.Dpi;
        MinimumSize = new Size(1050, 670);
        Font = new Font("Segoe UI", 10F);
        BackColor = Color.FromArgb(245, 247, 250);

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
            Text = "Daily Movements",
            AutoSize = true,
            Font = new Font("Segoe UI Semibold", 19F, FontStyle.Bold),
            Margin = new Padding(0, 0, 0, 5)
        }, 0, 0);

        header.Controls.Add(new Label
        {
            Text =
                "Physical IN/OUT activity for one day. Opening adjustments are excluded by default and can be included explicitly.",
            AutoSize = true,
            ForeColor = Color.FromArgb(80, 90, 105),
            MaximumSize = new Size(1250, 0)
        }, 0, 1);

        root.Controls.Add(header, 0, 0);

        var controlsCard = new Panel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            BackColor = Color.White,
            Padding = new Padding(16, 12, 16, 12),
            Margin = new Padding(0, 0, 0, 10)
        };

        var controlRows = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 1,
            RowCount = 3,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        controlRows.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        controlRows.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        controlRows.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var filters = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true,
            Margin = Padding.Empty,
            Padding = new Padding(0, 2, 0, 4)
        };

        filters.Controls.Add(ControlLabel("Date"));
        filters.Controls.Add(reportDate);
        filters.Controls.Add(ControlLabel("Customer", 14));
        filters.Controls.Add(customerSearch);
        filters.Controls.Add(ControlLabel("Container", 14));
        filters.Controls.Add(containerFilter);
        filters.Controls.Add(ControlLabel("Direction", 14));
        filters.Controls.Add(directionFilter);
        filters.Controls.Add(ControlLabel("Source", 14));
        filters.Controls.Add(sourceFilter);

        var options = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true,
            Margin = Padding.Empty,
            Padding = new Padding(0, 6, 0, 4)
        };

        includeAdjustments.Margin = Padding.Empty;
        includeNotesInPdf.Margin = new Padding(22, 0, 0, 0);
        options.Controls.Add(includeAdjustments);
        options.Controls.Add(includeNotesInPdf);

        var actions = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true,
            Margin = Padding.Empty,
            Padding = new Padding(0, 8, 0, 0)
        };

        actions.Controls.Add(ActionButton(
            "Run Report", 125,
            async () => await LoadReportAsync()));

        actions.Controls.Add(ActionButton(
            "Today", 90,
            async () =>
            {
                reportDate.Value = DateTime.Today;
                await LoadReportAsync();
            }));

        actions.Controls.Add(ActionButton(
            "Yesterday", 105,
            async () =>
            {
                reportDate.Value = DateTime.Today.AddDays(-1);
                await LoadReportAsync();
            }));

        actions.Controls.Add(ActionButton(
            "Generate PDF", 145,
            async () => await GeneratePdfAsync(false)));

        actions.Controls.Add(ActionButton(
            "Generate && Open", 175,
            async () => await GeneratePdfAsync(true)));

        var csv = new Button
        {
            Text = "Export CSV",
            Size = new Size(135, 40),
            Margin = new Padding(8, 0, 0, 0)
        };
        csv.Click += (_, _) => ExportCsv();
        actions.Controls.Add(csv);

        controlRows.Controls.Add(filters, 0, 0);
        controlRows.Controls.Add(options, 0, 1);
        controlRows.Controls.Add(actions, 0, 2);

        controlsCard.Controls.Add(controlRows);
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
        gridCard.Controls.Add(grid);
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

    private void ConfigureGrid()
    {
        grid.ColumnHeadersHeightSizeMode =
            DataGridViewColumnHeadersHeightSizeMode.AutoSize;

        grid.Columns.Add(Column("Code", 125, "Code"));
        grid.Columns.Add(Column(
            "Customer", 235, "Customer",
            DataGridViewAutoSizeColumnMode.Fill));
        grid.Columns.Add(Column("Type", 125, "Type"));
        grid.Columns.Add(Column("Container", 135, "Container"));
        grid.Columns.Add(Column("Direction", 100, "Direction"));
        grid.Columns.Add(Column("Qty", 80, "Quantity"));
        grid.Columns.Add(Column("Source", 145, "Source"));
        grid.Columns.Add(Column("Reference", 145, "Reference"));
        grid.Columns.Add(Column("Notes", 180, "Notes"));
        grid.Columns.Add(Column("Entered by", 110, "EnteredBy"));

        grid.SortCompare += Grid_SortCompare;
    }

    private async Task InitialiseAsync()
    {
        containerFilter.Items.Add(new Choice<int?>(null, "All containers"));

        var containerSource = await outstanding.QueryAsync(
            new OutstandingReportQuery(
                DateOnly.FromDateTime(DateTime.Today),
                IncludeCredits: true,
                IncludeInactiveCustomers: true));

        foreach (var container in containerSource.ContainerTotals)
        {
            containerFilter.Items.Add(
                new Choice<int?>(
                    container.ContainerTypeId,
                    container.ContainerType));
        }
        containerFilter.SelectedIndex = 0;

        directionFilter.Items.Add(new Choice<MovementType?>(null, "All directions"));
        directionFilter.Items.Add(new Choice<MovementType?>(MovementType.Out, "OUT"));
        directionFilter.Items.Add(new Choice<MovementType?>(MovementType.In, "IN"));
        directionFilter.SelectedIndex = 0;

        sourceFilter.Items.Add(new Choice<MovementSource?>(null, "All sources"));
        sourceFilter.Items.Add(new Choice<MovementSource?>(MovementSource.Manual, "Single Entry"));
        sourceFilter.Items.Add(new Choice<MovementSource?>(MovementSource.Batch, "Batch Entry"));
        sourceFilter.Items.Add(new Choice<MovementSource?>(MovementSource.ExcelImport, "Excel Import"));
        sourceFilter.SelectedIndex = 0;

        await LoadReportAsync();
    }

    private async Task LoadReportAsync()
    {
        try
        {
            Enabled = false;
            UseWaitCursor = true;

            var result = await reports.QueryAsync(
                new DailyMovementsReportQuery(
                    DateOnly.FromDateTime(reportDate.Value.Date),
                    customerSearch.Text,
                    (containerFilter.SelectedItem as Choice<int?>)?.Value,
                    (directionFilter.SelectedItem as Choice<MovementType?>)?.Value,
                    (sourceFilter.SelectedItem as Choice<MovementSource?>)?.Value,
                    includeAdjustments.Checked));

            currentResult = result;
            grid.Rows.Clear();

            foreach (var row in result.Rows)
            {
                var index = grid.Rows.Add(
                    row.CustomerCode,
                    row.CustomerName,
                    CustomerTypeText(row.CustomerType),
                    row.ContainerType,
                    row.DirectionText,
                    row.Quantity.ToString("N0"),
                    row.SourceText,
                    row.Reference,
                    row.Notes,
                    row.EnteredBy);

                grid.Rows[index].Tag = row;
            }

            ResizeContentColumns();

            var totals = result.ContainerTotals.Select(x =>
                $"{x.ContainerType}: {x.OutQuantity:N0} OUT / {x.InQuantity:N0} IN");

            summary.Text =
                $"{result.ReportDate:dddd, dd/MM/yyyy} — " +
                $"{result.Rows.Count:N0} movement row(s) • " +
                $"{result.OutQuantity:N0} OUT • {result.InQuantity:N0} IN" +
                (result.ContainerTotals.Count > 0
                    ? Environment.NewLine +
                      string.Join("   •   ", totals)
                    : Environment.NewLine + "No matching movements.");
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                this, ex.Message, "Daily Movements",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            Enabled = true;
            UseWaitCursor = false;
        }
    }

    private void Grid_SortCompare(
        object? sender,
        DataGridViewSortCompareEventArgs e)
    {
        var left = grid.Rows[e.RowIndex1].Tag as DailyMovementReportRow;
        var right = grid.Rows[e.RowIndex2].Tag as DailyMovementReportRow;

        if (left is null || right is null)
            return;

        var name = grid.Columns[e.Column.Index].Name;

        e.SortResult = name switch
        {
            "Quantity" => left.Quantity.CompareTo(right.Quantity),
            _ => 0
        };

        if (name != "Quantity")
            return;

        if (e.SortResult == 0)
            e.SortResult = left.MovementId.CompareTo(right.MovementId);

        e.Handled = true;
    }

    private DailyMovementsReportResult BuildDisplayedResult()
    {
        var source = currentResult
            ?? throw new InvalidOperationException("Run the report first.");

        var rows = grid.Rows
            .Cast<DataGridViewRow>()
            .Where(x => !x.IsNewRow)
            .Select(x => x.Tag as DailyMovementReportRow)
            .Where(x => x is not null)
            .Cast<DailyMovementReportRow>()
            .ToList();

        return new DailyMovementsReportResult(
            source.ReportDate,
            rows,
            source.ContainerTotals);
    }

    private async Task GeneratePdfAsync(bool openAfter)
    {
        if (currentResult is null)
        {
            MessageBox.Show(this, "Run the report first.");
            return;
        }

        var result = BuildDisplayedResult();

        using var dialog = new SaveFileDialog
        {
            Title = "Generate Daily Movements PDF",
            Filter = "PDF file (*.pdf)|*.pdf",
            FileName =
                $"BinTracker_Daily_Movements_{result.ReportDate:yyyyMMdd}.pdf",
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
                dialog.FileName,
                includeNotesInPdf.Checked);

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
                this, ex.Message, "Daily Movements PDF",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            Enabled = true;
            UseWaitCursor = false;
        }
    }

    private void ExportCsv()
    {
        if (currentResult is null)
        {
            MessageBox.Show(this, "Run the report first.");
            return;
        }

        var result = BuildDisplayedResult();

        using var dialog = new SaveFileDialog
        {
            Title = "Export Daily Movements",
            Filter = "CSV file (*.csv)|*.csv",
            FileName =
                $"BinTracker_Daily_Movements_{result.ReportDate:yyyyMMdd}.csv",
            AddExtension = true,
            DefaultExt = "csv"
        };

        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;

        using var writer = new StreamWriter(
            dialog.FileName, false,
            new System.Text.UTF8Encoding(true));

        if (includeNotesInPdf.Checked)
        {
            writer.WriteLine(
                "Date,Customer Code,Customer Name,Customer Type,Container,Direction,Quantity,Source,Reference,Notes,Entered By");
        }
        else
        {
            writer.WriteLine(
                "Date,Customer Code,Customer Name,Customer Type,Container,Direction,Quantity,Source,Reference,Entered By");
        }

        foreach (var row in result.Rows)
        {
            var fields = new List<string>
            {
                Csv(result.ReportDate.ToString("dd/MM/yyyy")),
                Csv(row.CustomerCode),
                Csv(row.CustomerName),
                Csv(CustomerTypeText(row.CustomerType)),
                Csv(row.ContainerType),
                Csv(row.DirectionText),
                row.Quantity.ToString(
                    System.Globalization.CultureInfo.InvariantCulture),
                Csv(row.SourceText),
                Csv(row.Reference)
            };

            if (includeNotesInPdf.Checked)
                fields.Add(Csv(row.Notes));

            fields.Add(Csv(row.EnteredBy));

            writer.WriteLine(string.Join(",", fields));
        }
    }

    private void ResizeContentColumns()
    {
        ResizeColumn("Code", 125, 300);
        ResizeColumn("Type", 125, 220);
        ResizeColumn("Container", 135, 240);
    }

    private void ResizeColumn(
        string name,
        int minimum,
        int maximum)
    {
        var column = grid.Columns[name];
        var font = grid.DefaultCellStyle.Font ?? Font;
        var longest =
            TextRenderer.MeasureText(column.HeaderText, font).Width;

        foreach (DataGridViewRow row in grid.Rows)
        {
            var text =
                Convert.ToString(row.Cells[column.Index].Value) ?? "";
            longest = Math.Max(
                longest,
                TextRenderer.MeasureText(text, font).Width);
        }

        column.AutoSizeMode =
            DataGridViewAutoSizeColumnMode.None;
        column.MinimumWidth = minimum;
        column.Width =
            Math.Clamp(longest + 34, minimum, maximum);
    }

    private void ApplyResponsiveBounds()
    {
        var screen = Owner is not null
            ? Screen.FromControl(Owner)
            : Screen.FromPoint(Cursor.Position);

        var area = screen.WorkingArea;

        Width = Math.Clamp(
            (int)Math.Round(area.Width * 0.92),
            MinimumSize.Width, 1900);

        Height = Math.Clamp(
            (int)Math.Round(area.Height * 0.88),
            MinimumSize.Height, 1100);

        Left = area.Left + Math.Max(0, (area.Width - Width) / 2);
        Top = area.Top + Math.Max(0, (area.Height - Height) / 2);
    }

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

    private static string CustomerTypeText(CustomerType type) =>
        type == CustomerType.Account
            ? "Account"
            : "Cash / COD";

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
