using BinTracker.Core;
using BinTracker.Services;
using System.Drawing.Drawing2D;

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
        AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None,
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
    private bool allocatingColumns;

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

        AutoScaleMode = AutoScaleMode.Dpi;
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
            Padding = Padding.Empty
        };
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // filters
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // options
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // actions
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // summary
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F)); // grid

        var filters = Flow();
        filters.BackColor = Color.White;
        filters.Padding = new Padding(16, 12, 16, 0);
        filters.Controls.Add(FilterGroup("From", startDate));
        filters.Controls.Add(FilterGroup("To", endDate, 14));
        filters.Controls.Add(FilterGroup("Customer", customerSearch, 14));
        filters.Controls.Add(FilterGroup("Container", containerFilter, 14));
        filters.Controls.Add(FilterGroup("Direction", directionFilter, 14));
        filters.Controls.Add(FilterGroup("Source", sourceFilter, 14));

        var options = Flow();
        options.BackColor = Color.White;
        options.Padding = new Padding(16, 6, 16, 4);
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
        actions.BackColor = Color.White;
        actions.Padding = new Padding(16, 8, 16, 12);
        actions.MinimumSize = new Size(0, 52);
        actions.WrapContents = true;
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

        // Filters, options and actions are direct AutoSize rows in the root layout.
        // There is deliberately no intermediate controls card: this removes the
        // WinForms preferred-height feedback loop that previously either clipped
        // the action buttons or created a large blank band beneath them.
        root.Controls.Add(filters, 0, 0);
        root.Controls.Add(options, 0, 1);
        root.Controls.Add(actions, 0, 2);

        var summaryCard = new Panel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            BackColor = Color.White,
            Padding = new Padding(16, 10, 16, 10),
            Margin = new Padding(0, 0, 0, 10)
        };
        summaryCard.Controls.Add(summary);
        root.Controls.Add(summaryCard, 0, 3);

        var gridCard = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.White,
            Padding = new Padding(10)
        };
        gridCard.Controls.Add(ReportGridMultiSort.Wrap(grid));
        root.Controls.Add(gridCard, 0, 4);

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
                grid.Rows[index].Cells["Status"].ToolTipText = row.Status;
                grid.Rows[index].Cells["Notes"].ToolTipText = row.Notes;
            }

            UpdateReverseAvailability();

            ReportGridMultiSort.Reapply(grid);
            AllocateResponsiveColumns();

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
        grid.RowTemplate.Height = 30;
        grid.Columns.Add(Column("Date", 150, 146, "Date"));
        grid.Columns.Add(Column("Code", 130, 120, "Code"));
        grid.Columns.Add(Column("Customer", 210, 175, "Customer"));
        grid.Columns.Add(Column("Type", 86, 80, "Type"));
        grid.Columns.Add(Column("Container", 105, 96, "Container"));
        grid.Columns.Add(Column("Direction", 112, 106, "Direction"));
        grid.Columns.Add(Column("Qty", 62, 56, "Quantity"));
        grid.Columns.Add(Column("Source", 125, 116, "Source"));
        grid.Columns.Add(Column("Reference", 108, 96, "Reference"));
        grid.Columns.Add(Column("Status", 220, 210, "Status"));
        grid.Columns.Add(Column("Notes", 175, 155, "Notes"));
        grid.Columns.Add(Column("Entered by", 105, 90, "EnteredBy"));

        grid.SortCompare += Grid_SortCompare;
        grid.CellPainting += Grid_CellPainting;
        grid.Resize += (_, _) => AllocateResponsiveColumns();
    }

    private void Grid_CellPainting(
        object? sender,
        DataGridViewCellPaintingEventArgs e)
    {
        if (e.RowIndex < 0 || e.ColumnIndex < 0 ||
            grid.Rows[e.RowIndex].Tag is not MovementHistoryReportRow row)
            return;

        var columnName = grid.Columns[e.ColumnIndex].Name;
        var isDirection = columnName == "Direction";
        var isReversalStatus = columnName == "Status" &&
            (row.ReversesMovementId.HasValue || row.CorrectedByMovementId.HasValue);
        if (!isDirection && !isReversalStatus)
            return;

        var text = isDirection ? row.DirectionText : DisplayStatus(row);
        if (string.IsNullOrWhiteSpace(text))
            return;

        // WinForms annotates both values as nullable because custom painting
        // can be raised without a resolved style or graphics surface. A badge
        // cannot be rendered safely in that state, so retain the grid's normal
        // painting instead of dereferencing either value.
        var cellStyle = e.CellStyle;
        var graphics = e.Graphics;
        if (cellStyle is null || graphics is null)
            return;

        e.PaintBackground(e.ClipBounds, true);
        e.Paint(e.ClipBounds, DataGridViewPaintParts.Border);

        var fill = isReversalStatus
            ? Color.FromArgb(255, 232, 194)
            : row.Direction == MovementType.In
                ? Color.FromArgb(218, 242, 226)
                : Color.FromArgb(250, 222, 222);
        var foreground = isReversalStatus
            ? Color.FromArgb(132, 74, 0)
            : row.Direction == MovementType.In
                ? Color.FromArgb(28, 102, 55)
                : Color.FromArgb(151, 43, 43);

        var cellFont = cellStyle.Font ?? grid.Font;
        var textWidth = TextRenderer.MeasureText(text, cellFont).Width;
        var badgeWidth = isDirection
            ? Math.Min(e.CellBounds.Width - 10, textWidth + 22)
            : e.CellBounds.Width - 10;
        var badgeBounds = new Rectangle(
            e.CellBounds.Left + 5,
            e.CellBounds.Top + 4,
            Math.Max(1, badgeWidth),
            Math.Max(1, e.CellBounds.Height - 8));

        using (var path = RoundedRectangle(badgeBounds, 7))
        using (var brush = new SolidBrush(fill))
            graphics.FillPath(brush, path);

        TextRenderer.DrawText(
            graphics,
            text,
            cellFont,
            Rectangle.Inflate(badgeBounds, -8, 0),
            foreground,
            TextFormatFlags.VerticalCenter |
            TextFormatFlags.Left |
            TextFormatFlags.EndEllipsis |
            TextFormatFlags.SingleLine |
            TextFormatFlags.NoPrefix);

        e.Handled = true;
    }

    private static string DisplayStatus(MovementHistoryReportRow row)
    {
        if (row.ReversesMovementId.HasValue)
            return $"Reversal — #{row.ReversesMovementId.Value}";

        if (row.CorrectedByMovementId.HasValue)
        {
            var reference = row.LinkedReversalReference;
            return string.IsNullOrWhiteSpace(reference)
                ? "Reversed"
                : $"Reversed — {reference}";
        }

        return row.Status;
    }

    private static GraphicsPath RoundedRectangle(Rectangle bounds, int radius)
    {
        var diameter = radius * 2;
        var path = new GraphicsPath();
        path.AddArc(bounds.Left, bounds.Top, diameter, diameter, 180, 90);
        path.AddArc(bounds.Right - diameter, bounds.Top, diameter, diameter, 270, 90);
        path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
        path.AddArc(bounds.Left, bounds.Bottom - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();
        return path;
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
            FileName = MovementHistoryExportFileName.Build(
                result,
                !string.IsNullOrWhiteSpace(customerSearch.Text),
                "pdf"),
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
            FileName = MovementHistoryExportFileName.Build(
                result,
                !string.IsNullOrWhiteSpace(customerSearch.Text),
                "csv"),
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
            ? "Date,Customer Code,Customer Name,Customer Type,Container,Direction,Quantity,Source,Reference,Status,Notes,Entered By"
            : "Date,Customer Code,Customer Name,Customer Type,Container,Direction,Quantity,Source,Reference,Status,Entered By");

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
                Csv(row.Reference),
                Csv(DisplayStatus(row))
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

    private void AllocateResponsiveColumns()
    {
        if (allocatingColumns || grid.ClientSize.Width <= 0)
            return;

        allocatingColumns = true;
        try
        {
            var fixedWidths = new Dictionary<string, int>(StringComparer.Ordinal)
            {
                ["Date"] = 150,
                ["Code"] = 130,
                ["Type"] = 86,
                ["Container"] = 105,
                ["Direction"] = 112,
                ["Quantity"] = 62,
                ["Source"] = 125,
                ["Reference"] = 108,
                ["EnteredBy"] = 105
            };

            foreach (var pair in fixedWidths)
                grid.Columns[pair.Key].Width = pair.Value;

            var available = grid.ClientSize.Width - 4;
            var verticalScrollBar = grid.Controls
                .OfType<VScrollBar>()
                .FirstOrDefault(scrollBar => scrollBar.Visible);
            if (verticalScrollBar is not null)
                available -= verticalScrollBar.Width;

            var fixedTotal = fixedWidths.Values.Sum();
            var flexible = new[]
            {
                (Name: "Customer", Minimum: 175, Weight: 0.40),
                (Name: "Status", Minimum: 210, Weight: 0.28),
                (Name: "Notes", Minimum: 155, Weight: 0.32)
            };
            var minimumFlexibleTotal = flexible.Sum(item => item.Minimum);
            var minimumTotal = fixedTotal + minimumFlexibleTotal;

            if (available < minimumTotal)
            {
                foreach (var item in flexible)
                    grid.Columns[item.Name].Width = item.Minimum;
                return;
            }

            var distributable = available - fixedTotal - minimumFlexibleTotal;
            var assignedExtra = 0;
            for (var index = 0; index < flexible.Length; index++)
            {
                var item = flexible[index];
                var extra = index == flexible.Length - 1
                    ? distributable - assignedExtra
                    : (int)Math.Floor(distributable * item.Weight);
                assignedExtra += extra;
                grid.Columns[item.Name].Width = item.Minimum + extra;
            }
        }
        finally
        {
            allocatingColumns = false;
        }
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

    private static FlowLayoutPanel FilterGroup(string label, Control control, int left = 0)
    {
        var group = new FlowLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Margin = new Padding(left, 0, 0, 0),
            Padding = Padding.Empty
        };
        group.Controls.Add(ControlLabel(label));
        group.Controls.Add(control);
        return group;
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
        int minimumWidth,
        string name) =>
        new()
        {
            Name = name,
            HeaderText = header,
            Width = width,
            MinimumWidth = minimumWidth,
            AutoSizeMode = DataGridViewAutoSizeColumnMode.None,
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
