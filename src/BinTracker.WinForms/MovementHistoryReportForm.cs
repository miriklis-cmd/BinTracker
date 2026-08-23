using BinTracker.Core;
using BinTracker.Services;

namespace BinTracker.WinForms;

public sealed class MovementHistoryReportForm : BinTrackerForm
{
    private DateTime BusinessToday => clock.Today.ToDateTime(TimeOnly.MinValue);

    private readonly IMovementHistoryReportService reports;
    private readonly IMovementHistoryReportPdfService pdfReports;
    private readonly IContainerTypeService containerTypes;

    private readonly DateTimePicker startDate = new()
    {
        Format = DateTimePickerFormat.Short,
        Width = 140
    };

    private readonly DateTimePicker endDate = new()
    {
        Format = DateTimePickerFormat.Short,
        Width = 140
    };

    private readonly TextBox customerSearch = new()
    {
        Width = 220,
        PlaceholderText = "Type, then press Enter"
    };

    private readonly ComboBox containerFilter = ChoiceBox(175);
    private readonly ComboBox directionFilter = ChoiceBox(165);
    private readonly ComboBox sourceFilter = ChoiceBox(155);

    private readonly CheckBox includeAdjustments = new()
    {
        Text = "Include opening adjustments",
        AutoSize = true
    };

    private readonly CheckBox includeNotesInExports = new()
    {
        Text = "Include notes in exports",
        AutoSize = true,
        Margin = new Padding(22, 0, 0, 0)
    };

    private readonly Label summary = new()
    {
        AutoSize = true,
        ForeColor = Color.FromArgb(60, 75, 95),
        MaximumSize = new Size(1400, 0)
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
    private readonly IMovementCorrectionService corrections;
    private readonly UserSession session;
    private Button? reverseButton;
    private MovementHistoryReportResult? currentResult;
    private bool autoRefreshReady;

    private readonly IBusinessClock clock;

    public MovementHistoryReportForm(
        IMovementHistoryReportService reports,
        IMovementHistoryReportPdfService pdfReports,
        IContainerTypeService containerTypes,
        IAuditService audit,
        IMovementCorrectionService corrections,
        UserSession session,
        IBusinessClock clock)
    {
        this.reports = reports;
        this.pdfReports = pdfReports;
        this.containerTypes = containerTypes;

        this.audit = audit;
        this.clock = clock;
        this.corrections = corrections;
        this.session = session;

        Text = "Movement History";
        StartPosition = FormStartPosition.Manual;
        AutoScaleMode = AutoScaleMode.Dpi;
        MinimumSize = new Size(1120, 720);
        Font = new Font("Segoe UI", 10F);
        BackColor = Color.FromArgb(245, 247, 250);

        startDate.MaxDate = BusinessToday;
        endDate.MaxDate = BusinessToday;
        startDate.Value = BusinessToday.AddDays(-29);
        endDate.Value = BusinessToday;

        startDate.ValueChanged += async (_, _) =>
        {
            if (startDate.Value.Date > endDate.Value.Date)
                endDate.Value = startDate.Value.Date;

            if (autoRefreshReady)
                await LoadReportAsync();
        };

        endDate.ValueChanged += async (_, _) =>
        {
            if (endDate.Value.Date < startDate.Value.Date)
                startDate.Value = endDate.Value.Date;

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

        directionFilter.SelectedIndexChanged += async (_, _) =>
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
            Text = "Movement History",
            AutoSize = true,
            Font = new Font("Segoe UI Semibold", 19F, FontStyle.Bold),
            Margin = new Padding(0, 0, 0, 5)
        }, 0, 0);

        header.Controls.Add(new Label
        {
            Text =
                "Search actual IN/OUT movement history across a selected date range. Future dates are not permitted.",
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

        var rows = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 1,
            RowCount = 3,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };

        var filters = Flow();
        filters.Controls.Add(ControlLabel("From"));
        filters.Controls.Add(startDate);
        filters.Controls.Add(ControlLabel("To", 14));
        filters.Controls.Add(endDate);
        filters.Controls.Add(ControlLabel("Customer", 14));
        filters.Controls.Add(customerSearch);
        filters.Controls.Add(ControlLabel("Container", 14));
        filters.Controls.Add(containerFilter);
        filters.Controls.Add(ControlLabel("Direction", 14));
        filters.Controls.Add(directionFilter);
        filters.Controls.Add(ControlLabel("Source", 14));
        filters.Controls.Add(sourceFilter);

        var options = Flow();
        options.Padding = new Padding(0, 6, 0, 4);
        options.Controls.Add(includeAdjustments);
        options.Controls.Add(includeNotesInExports);
        options.Controls.Add(new Label
        {
            Text = "Customer search: type code/name and press Enter",
            AutoSize = true,
            ForeColor = Color.DimGray,
            Margin = new Padding(22, 3, 0, 0)
        });

        var actions = Flow();
        actions.Padding = new Padding(0, 8, 0, 0);
        actions.Controls.Add(ActionButton(
            "Last 7 Days",
            115,
            async () => await SetRangeAndRefreshAsync(
                BusinessToday.AddDays(-6),
                BusinessToday)));

        actions.Controls.Add(ActionButton(
            "Last 30 Days",
            125,
            async () => await SetRangeAndRefreshAsync(
                BusinessToday.AddDays(-29),
                BusinessToday)));

        actions.Controls.Add(ActionButton(
            "This Month",
            135,
            async () => await SetRangeAndRefreshAsync(
                new DateTime(BusinessToday.Year, BusinessToday.Month, 1),
                BusinessToday)));

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

        if (session.Role is UserRole.Administrator or UserRole.Operator)
        {
            reverseButton = new Button
            {
                Text = "Reverse Selected",
                Size = new Size(155, 40),
                Margin = new Padding(8, 0, 0, 0)
            };
            reverseButton.Click += async (_, _) => await ReverseSelectedAsync();
            actions.Controls.Add(reverseButton);
            grid.SelectionChanged += (_, _) => UpdateReverseAvailability();
        }

        rows.Controls.Add(filters, 0, 0);
        rows.Controls.Add(options, 0, 1);
        rows.Controls.Add(actions, 0, 2);
        controlsCard.Controls.Add(rows);
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

        var configured =
            await containerTypes.SearchAsync(
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

        directionFilter.Items.Add(
            new Choice<MovementType?>(null, "All directions"));
        directionFilter.Items.Add(
            new Choice<MovementType?>(MovementType.Out, "OUT"));
        directionFilter.Items.Add(
            new Choice<MovementType?>(MovementType.In, "IN"));
        directionFilter.SelectedIndex = 0;

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

    private async Task SetRangeAndRefreshAsync(DateTime from, DateTime to)
    {
        autoRefreshReady = false;
        startDate.Value = from;
        endDate.Value = to;
        autoRefreshReady = true;
        await LoadReportAsync();
    }

    private async Task LoadReportAsync()
    {
        try
        {
            Enabled = false;
            UseWaitCursor = true;

            var result = await reports.QueryAsync(
                new MovementHistoryReportQuery(
                    DateOnly.FromDateTime(startDate.Value.Date),
                    DateOnly.FromDateTime(endDate.Value.Date),
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
                    row.MovementDate.ToString("ddd dd/MM/yyyy"),
                    row.CustomerCode,
                    row.CustomerName,
                    CustomerTypeText(row.CustomerType),
                    row.ContainerType,
                    row.DirectionText,
                    row.Quantity.ToString("N0"),
                    row.SourceText,
                    row.Reference,
                    row.Status,
                    row.Notes,
                    row.EnteredBy);

                grid.Rows[index].Tag = row;
            }

            UpdateReverseAvailability();

            ResizeContentColumns();
            ReportGridMultiSort.Reapply(grid);

            var totals = result.ContainerTotals.Select(x =>
                $"{x.ContainerType}: " +
                $"{x.OutQuantity:N0} OUT / " +
                $"{x.InQuantity:N0} IN / " +
                $"Net {x.NetQuantity:+#;-#;0}");

            summary.Text =
                $"{result.StartDate:dd/MM/yyyy} – {result.EndDate:dd/MM/yyyy} — " +
                $"{result.Rows.Count:N0} movement row(s) • " +
                $"{result.OutQuantity:N0} OUT • " +
                $"{result.InQuantity:N0} IN • " +
                $"Net {result.NetQuantity:+#;-#;0}" +
                (result.ContainerTotals.Count > 0
                    ? Environment.NewLine +
                      string.Join("   •   ", totals)
                    : Environment.NewLine + "No matching movements.");
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                this,
                ex.Message,
                "Movement History",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
        finally
        {
            Enabled = true;
            UseWaitCursor = false;
        }
    }

    private async Task ReverseSelectedAsync()
    {
        if (grid.CurrentRow?.Tag is not MovementHistoryReportRow selected)
        {
            MessageBox.Show(this, "Select a movement row first.", "Reverse Movement",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var detail = await corrections.GetAsync(selected.MovementId);
        if (detail is null)
        {
            MessageBox.Show(this, "The selected movement no longer exists.", "Reverse Movement",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            await LoadReportAsync();
            return;
        }

        if (detail.IsAlreadyReversed)
        {
            MessageBox.Show(this, "This movement has already been reversed.", "Reverse Movement",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        if (detail.Source == MovementSource.Adjustment)
        {
            MessageBox.Show(this,
                "Opening adjustments cannot be reversed here because they change the brought-forward position. " +
                "Use the Administrator-controlled adjustment workflow.",
                "Reverse Movement", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        if (detail.Source == MovementSource.ExcelImport)
        {
            MessageBox.Show(this,
                "Excel Import movements cannot be reversed individually. " +
                "Use the Administrator Replace / Correct import workflow so the Import Run remains auditable.",
                "Reverse Movement", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        using var dialog = new MovementReversalDialog(detail);
        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;

        try
        {
            Enabled = false;
            UseWaitCursor = true;
            var result = await corrections.ReverseAsync(
                new ReverseMovementRequest(Guid.NewGuid(), selected.MovementId, dialog.Reason));

            MessageBox.Show(this,
                $"Movement #{result.OriginalMovementId} was preserved and reversal movement #{result.ReversalMovementId} was created.",
                "Movement Reversed", MessageBoxButtons.OK, MessageBoxIcon.Information);
            await LoadReportAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Reverse Movement",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            Enabled = true;
            UseWaitCursor = false;
        }
    }

    private void UpdateReverseAvailability()
    {
        if (reverseButton is null)
            return;

        reverseButton.Enabled = grid.CurrentRow?.Tag is MovementHistoryReportRow row &&
                                row.CanReverse &&
                                row.Source is MovementSource.Manual or MovementSource.Batch;
    }

    private void ConfigureGrid()
    {
        grid.Columns.Add(Column("Date", 135, "Date"));
        grid.Columns.Add(Column("Code", 145, "Code"));
        grid.Columns.Add(Column(
            "Customer",
            220,
            "Customer",
            DataGridViewAutoSizeColumnMode.Fill));
        grid.Columns.Add(Column("Type", 115, "Type"));
        grid.Columns.Add(Column("Container", 135, "Container"));
        grid.Columns.Add(Column("Direction", 100, "Direction"));
        grid.Columns.Add(Column("Qty", 80, "Quantity"));
        grid.Columns.Add(Column("Source", 140, "Source"));
        grid.Columns.Add(Column("Reference", 145, "Reference"));
        grid.Columns.Add(Column("Status", 230, "Status"));
        grid.Columns.Add(Column("Notes", 180, "Notes"));
        grid.Columns.Add(Column("Entered by", 110, "EnteredBy"));

        grid.SortCompare += Grid_SortCompare;
    }

    private void Grid_SortCompare(
        object? sender,
        DataGridViewSortCompareEventArgs e)
    {
        if (grid.Rows[e.RowIndex1].Tag is not MovementHistoryReportRow left ||
            grid.Rows[e.RowIndex2].Tag is not MovementHistoryReportRow right)
            return;

        var column = grid.Columns[e.Column.Index].Name;

        e.SortResult = column switch
        {
            "Date" => left.MovementDate.CompareTo(right.MovementDate),
            "Quantity" => left.Quantity.CompareTo(right.Quantity),
            _ => 0
        };

        if (column is not ("Date" or "Quantity"))
            return;

        if (e.SortResult == 0)
            e.SortResult = left.MovementId.CompareTo(right.MovementId);

        e.Handled = true;
    }

    private MovementHistoryReportResult BuildDisplayedResult()
    {
        var source = currentResult ??
            throw new InvalidOperationException("Run the report first.");

        var rows = grid.Rows
            .Cast<DataGridViewRow>()
            .Where(x => !x.IsNewRow)
            .Select(x => x.Tag as MovementHistoryReportRow)
            .Where(x => x is not null)
            .Cast<MovementHistoryReportRow>()
            .ToList();

        var totals = rows
            .GroupBy(x => new
            {
                x.ContainerTypeId,
                x.ContainerType,
                x.ContainerDisplayOrder
            })
            .Select(g => new MovementHistoryContainerTotal(
                g.Key.ContainerTypeId,
                g.Key.ContainerType,
                g.Key.ContainerDisplayOrder,
                g.Where(x => x.Direction == MovementType.Out)
                    .Sum(x => x.Quantity),
                g.Where(x => x.Direction == MovementType.In)
                    .Sum(x => x.Quantity)))
            .OrderBy(x => x.DisplayOrder)
            .ThenBy(x => x.ContainerType)
            .ToList();

        return new MovementHistoryReportResult(
            source.StartDate,
            source.EndDate,
            rows,
            totals);
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
            Title = "Generate Movement History PDF",
            Filter = "PDF file (*.pdf)|*.pdf",
            FileName =
                $"BinTracker_Movement_History_{result.StartDate:yyyyMMdd}_{result.EndDate:yyyyMMdd}.pdf",
            AddExtension = true,
            DefaultExt = "pdf"
        };

        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;

        try
        {
            Enabled = false;
            UseWaitCursor = true;

            var pdf = await pdfReports.BuildPdfAsync(
                result,
                includeNotesInExports.Checked);
            await File.WriteAllBytesAsync(dialog.FileName, pdf);

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
                "Movement History PDF",
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
        if (currentResult is null)
        {
            MessageBox.Show(this, "Run the report first.");
            return;
        }

        var result = BuildDisplayedResult();

        using var dialog = new SaveFileDialog
        {
            Title = "Export Movement History",
            Filter = "CSV file (*.csv)|*.csv",
            FileName =
                $"BinTracker_Movement_History_{result.StartDate:yyyyMMdd}_{result.EndDate:yyyyMMdd}.csv",
            AddExtension = true,
            DefaultExt = "csv"
        };

        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;

        using var writer = new StreamWriter(
            dialog.FileName,
            false,
            new System.Text.UTF8Encoding(true));

        writer.WriteLine(includeNotesInExports.Checked
            ? "Date,Customer Code,Customer Name,Customer Type,Container,Direction,Quantity,Source,Reference,Notes,Entered By"
            : "Date,Customer Code,Customer Name,Customer Type,Container,Direction,Quantity,Source,Reference,Entered By");

        foreach (var row in result.Rows)
        {
            var fields = new List<string>
            {
                Csv(row.MovementDate.ToString("dd/MM/yyyy")),
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

            if (includeNotesInExports.Checked)
                fields.Add(Csv(row.Notes));

            fields.Add(Csv(row.EnteredBy));
            writer.WriteLine(string.Join(",", fields));
        }

        writer.Flush();

        await ReportCsvAudit.WriteAsync(
            this, audit,
            "MOVEMENT_HISTORY_CSV_EXPORTED",
            $"{result.StartDate:yyyy-MM-dd}:{result.EndDate:yyyy-MM-dd}",
            $"Movement History CSV exported for {result.StartDate:dd/MM/yyyy} - {result.EndDate:dd/MM/yyyy}: {result.Rows.Count:N0} row(s).",
            dialog.FileName,
            result.Rows.Count,
            new
            {
                result.StartDate,
                result.EndDate,
                CustomerSearch = customerSearch.Text.Trim(),
                Container = containerFilter.Text,
                Direction = directionFilter.Text,
                Source = sourceFilter.Text,
                IncludeAdjustments = includeAdjustments.Checked,
                IncludeNotes = includeNotesInExports.Checked
            });
    }

    private void ResizeContentColumns()
    {
        ResizeColumn(
            "Date",
            grid.Rows.Cast<DataGridViewRow>()
                .Where(x => x.Tag is MovementHistoryReportRow)
                .Select(x =>
                    ((MovementHistoryReportRow)x.Tag!).MovementDate
                        .ToString("ddd dd/MM/yyyy")),
            135,
            180);

        ResizeColumn(
            "Code",
            grid.Rows.Cast<DataGridViewRow>()
                .Where(x => x.Tag is MovementHistoryReportRow)
                .Select(x =>
                    ((MovementHistoryReportRow)x.Tag!).CustomerCode),
            145,
            300);

        ResizeColumn(
            "Container",
            grid.Rows.Cast<DataGridViewRow>()
                .Where(x => x.Tag is MovementHistoryReportRow)
                .Select(x =>
                    ((MovementHistoryReportRow)x.Tag!).ContainerType),
            135,
            240);
    }

    private void ResizeColumn(
        string columnName,
        IEnumerable<string> values,
        int minimum,
        int maximum)
    {
        var column = grid.Columns[columnName];
        var font = grid.DefaultCellStyle.Font ?? Font;

        var longest = values
            .Append(column.HeaderText)
            .Select(x => TextRenderer.MeasureText(x ?? "", font).Width)
            .DefaultIfEmpty(minimum)
            .Max();

        column.AutoSizeMode =
            DataGridViewAutoSizeColumnMode.None;
        column.MinimumWidth = minimum;
        column.Width = Math.Clamp(
            longest + 34,
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
            (int)Math.Round(area.Width * 0.92),
            MinimumSize.Width,
            1900);

        Height = Math.Clamp(
            (int)Math.Round(area.Height * 0.88),
            MinimumSize.Height,
            1100);

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
