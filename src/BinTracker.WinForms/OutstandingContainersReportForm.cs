using BinTracker.Services;

namespace BinTracker.WinForms;

public sealed class OutstandingContainersReportForm : BinTrackerForm
{
    private readonly IOutstandingReportService outstanding;
    private readonly IOutstandingReportPdfService pdfReports;
    private readonly IContainerTypeService containerTypes;

    private readonly DateTimePicker reportDate = new()
    {
        Format = DateTimePickerFormat.Short,
        Width = 145,
        Value = DateTime.Today
    };

    private readonly TextBox customerSearch = new()
    {
        Width = 220,
        PlaceholderText = "Type, then press Enter"
    };

    private readonly ComboBox containerFilter = new()
    {
        Width = 185,
        DropDownStyle = ComboBoxStyle.DropDownList
    };

    private readonly ComboBox balanceFilter = new()
    {
        Width = 215,
        DropDownStyle = ComboBoxStyle.DropDownList
    };

    private readonly CheckBox includeInactive = new()
    {
        Text = "Include inactive",
        AutoSize = true,
        Checked = true,
        Margin = new Padding(14, 9, 0, 0)
    };

    private readonly Label summary = new()
    {
        AutoSize = true,
        ForeColor = Color.FromArgb(60, 75, 95),
        MaximumSize = new Size(1200, 0)
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
        BorderStyle = BorderStyle.FixedSingle
    };

    private readonly IAuditService audit;
    private OutstandingReportResult? currentResult;
    private bool autoRefreshReady;

    public OutstandingContainersReportForm(
        IOutstandingReportService outstanding,
        IOutstandingReportPdfService pdfReports,
        IContainerTypeService containerTypes,
        IAuditService audit)
    {
        this.outstanding = outstanding;
        this.pdfReports = pdfReports;
        this.containerTypes = containerTypes;

        this.audit = audit;

        Text = "Outstanding Containers";
        StartPosition = FormStartPosition.Manual;
        AutoScaleMode = AutoScaleMode.Dpi;
        MinimumSize = new Size(1000, 650);
        Font = new Font("Segoe UI", 10F);
        BackColor = Color.FromArgb(245, 247, 250);

        reportDate.MaxDate = DateTime.Today;

        reportDate.ValueChanged += async (_, _) =>
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

        balanceFilter.SelectedIndexChanged += async (_, _) =>
        {
            if (autoRefreshReady)
                await LoadReportAsync();
        };

        includeInactive.CheckedChanged += async (_, _) =>
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
            Padding = new Padding(18),
            Margin = Padding.Empty
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
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            BackColor = Color.White,
            ColumnCount = 1,
            RowCount = 2,
            Padding = new Padding(18, 12, 18, 12),
            Margin = new Padding(0, 0, 0, 10)
        };

        header.Controls.Add(new Label
        {
            Text = "Outstanding Containers — As of Date",
            AutoSize = true,
            Font = new Font(
                "Segoe UI Semibold",
                19F,
                FontStyle.Bold),
            Margin = new Padding(0, 0, 0, 5)
        }, 0, 0);

        header.Controls.Add(new Label
        {
            Text =
                "Shows customer/container position at the end of the selected date. " +
                "Container types for the same customer stay together. Future movements do not affect a historical result.",
            AutoSize = true,
            ForeColor = Color.FromArgb(80, 90, 105),
            MaximumSize = new Size(1180, 0),
            Margin = Padding.Empty
        }, 0, 1);

        root.Controls.Add(header, 0, 0);

        var controlsCard = new Panel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            MinimumSize = new Size(0, 168),
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
            RowCount = 2,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        controlRows.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        controlRows.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var filters = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true,
            Padding = new Padding(0, 2, 0, 4),
            Margin = Padding.Empty
        };

        filters.Controls.Add(ControlLabel("As of"));
        filters.Controls.Add(reportDate);
        filters.Controls.Add(ControlLabel("Customer", 14));
        filters.Controls.Add(customerSearch);
        filters.Controls.Add(ControlLabel("Container", 14));
        filters.Controls.Add(containerFilter);
        filters.Controls.Add(ControlLabel("Balance", 14));
        filters.Controls.Add(balanceFilter);
        filters.Controls.Add(includeInactive);

        var actions = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true,
            Padding = new Padding(0, 8, 0, 0),
            Margin = Padding.Empty
        };

        var today = new Button
        {
            Text = "Today",
            AutoSize = false,
            Size = new Size(90, 40),
            Margin = new Padding(8, 0, 0, 0)
        };
        today.Click += async (_, _) =>
        {
            autoRefreshReady = false;
            reportDate.Value = DateTime.Today;
            autoRefreshReady = true;
            await LoadReportAsync();
        };

        var pdf = new Button
        {
            Text = "Generate PDF",
            AutoSize = false,
            Size = new Size(145, 40),
            Margin = new Padding(18, 0, 0, 0)
        };
        pdf.Click += async (_, _) => await GeneratePdfAsync(openAfter: false);

        var pdfOpen = new Button
        {
            Text = "Generate && Open",
            AutoSize = false,
            Size = new Size(175, 40),
            Margin = new Padding(8, 0, 0, 0)
        };
        pdfOpen.Click += async (_, _) => await GeneratePdfAsync(openAfter: true);

        var export = new Button
        {
            Text = "Export CSV",
            AutoSize = false,
            Size = new Size(135, 40),
            Margin = new Padding(8, 0, 0, 0)
        };
        export.Click += async (_, _) => await ExportCsvAsync();

        actions.Controls.Add(today);
        actions.Controls.Add(pdf);
        actions.Controls.Add(pdfOpen);
        actions.Controls.Add(export);

        var customerHint = new Label
        {
            Text = "Customer: press Enter to search",
            AutoSize = true,
            ForeColor = Color.DimGray,
            Margin = new Padding(0, 2, 0, 4)
        };

        controlRows.RowCount = 3;
        controlRows.RowStyles.Clear();
        controlRows.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        controlRows.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        controlRows.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        controlRows.Controls.Add(filters, 0, 0);
        controlRows.Controls.Add(customerHint, 0, 1);
        controlRows.Controls.Add(actions, 0, 2);

        controlsCard.Controls.Add(controlRows);
        root.Controls.Add(controlsCard, 0, 1);

        var summaryCard = new Panel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
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
            Padding = new Padding(10),
            Margin = Padding.Empty
        };
        gridCard.Controls.Add(ReportGridMultiSort.Wrap(grid));
        root.Controls.Add(gridCard, 0, 3);

        var close = new Button
        {
            Text = "Close",
            AutoSize = false,
            Size = new Size(110, 38),
            Margin = new Padding(0, 10, 0, 0)
        };
        close.Click += (_, _) => Close();

        var footer = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            Margin = Padding.Empty
        };
        footer.Controls.Add(close);
        root.Controls.Add(footer, 0, 4);

        Controls.Add(root);
    }

    private void ApplyResponsiveBounds()
    {
        var ownerScreen = Owner is not null
            ? Screen.FromControl(Owner)
            : Screen.FromPoint(Cursor.Position);

        var working = ownerScreen.WorkingArea;

        var targetWidth = Math.Clamp(
            (int)Math.Round(working.Width * 0.90),
            MinimumSize.Width,
            1800);

        var targetHeight = Math.Clamp(
            (int)Math.Round(working.Height * 0.88),
            MinimumSize.Height,
            1100);

        Size = new Size(targetWidth, targetHeight);

        Left = working.Left +
            Math.Max(0, (working.Width - Width) / 2);
        Top = working.Top +
            Math.Max(0, (working.Height - Height) / 2);
    }

    private void ResizeContentColumns()
    {
        ResizeColumnToContent(
            "Code",
            minimumWidth: 130,
            maximumWidth: 300);

        ResizeColumnToContent(
            "Type",
            minimumWidth: 130,
            maximumWidth: 220);
    }

    private void ResizeColumnToContent(
        string headerText,
        int minimumWidth,
        int maximumWidth)
    {
        var column = grid.Columns
            .Cast<DataGridViewColumn>()
            .FirstOrDefault(x =>
                string.Equals(
                    x.HeaderText,
                    headerText,
                    StringComparison.OrdinalIgnoreCase));

        if (column is null)
        {
            return;
        }

        var font = grid.DefaultCellStyle.Font ?? Font;
        var longestWidth =
            TextRenderer.MeasureText(
                headerText,
                grid.ColumnHeadersDefaultCellStyle.Font ?? font).Width;

        foreach (DataGridViewRow row in grid.Rows)
        {
            if (row.IsNewRow)
            {
                continue;
            }

            var text =
                Convert.ToString(
                    row.Cells[column.Index].Value) ??
                string.Empty;

            longestWidth = Math.Max(
                longestWidth,
                TextRenderer.MeasureText(
                    text,
                    font).Width);
        }

        column.AutoSizeMode =
            DataGridViewAutoSizeColumnMode.None;
        column.MinimumWidth = minimumWidth;
        column.Width = Math.Clamp(
            longestWidth + 34,
            minimumWidth,
            maximumWidth);
    }

    private void ConfigureGrid()
    {
        grid.ColumnHeadersHeightSizeMode =
            DataGridViewColumnHeadersHeightSizeMode.AutoSize;
        grid.AutoSizeRowsMode =
            DataGridViewAutoSizeRowsMode.None;
        grid.ScrollBars = ScrollBars.Both;

        grid.Columns.Add(Column("Code", 115, name: "Code"));
        grid.Columns.Add(Column(
            "Customer",
            260,
            DataGridViewAutoSizeColumnMode.Fill,
            name: "Customer"));
        grid.Columns.Add(Column("Type", 130, name: "Type"));
        grid.Columns.Add(Column("Container", 150, name: "Container"));
        grid.Columns.Add(Column("Position", 120, name: "Position"));
        grid.Columns.Add(Column("Last movement", 130, name: "LastMovement"));
        grid.Columns.Add(Column("Status", 95, name: "Status"));

        // Position is rendered as e.g. "5 OUT" / "5 CREDIT", but sorting must
        // use the signed business balance. Credits are negative. Using the
        // report-row model also avoids reverse-engineering display text.
        ReportGridMultiSort.SetTypedSortValue(
            grid,
            "Position",
            row => (row.Tag as OutstandingReportRow)?.Balance);

        ReportGridMultiSort.SetTypedSortValue(
            grid,
            "LastMovement",
            row => (row.Tag as OutstandingReportRow)?.LastMovementDate);
    }

    private async Task InitialiseAsync()
    {
        if (containerFilter.Items.Count == 0)
        {
            containerFilter.Items.Add(
                new ContainerChoice(null, "All containers"));

            // Report filters must come from configured Container Types, not
            // from today's non-zero outstanding balances. Otherwise a valid
            // historical type disappears from every as-of-date filter when its
            // current balance happens to be zero. Include inactive types so
            // historical reporting remains possible after deactivation.
            var configured = await containerTypes.SearchAsync(
                search: null,
                includeInactive: true);

            foreach (var container in configured)
            {
                var label = container.IsActive
                    ? container.Name
                    : $"{container.Name} (inactive)";

                containerFilter.Items.Add(
                    new ContainerChoice(container.Id, label));
            }

            containerFilter.SelectedIndex = 0;
        }

        if (balanceFilter.Items.Count == 0)
        {
            balanceFilter.Items.Add(new BalanceChoice(
                OutstandingBalanceFilter.OutstandingOnly, "Outstanding only"));
            balanceFilter.Items.Add(new BalanceChoice(
                OutstandingBalanceFilter.CreditsOnly, "Credits only"));
            balanceFilter.Items.Add(new BalanceChoice(
                OutstandingBalanceFilter.AllNonZero, "All non-zero"));
            balanceFilter.SelectedIndex = 0;
        }

        autoRefreshReady = true;
        await LoadReportAsync();
    }

    private async Task LoadReportAsync()
    {
        try
        {
            UseWaitCursor = true;
            Enabled = false;

            var selected =
                containerFilter.SelectedItem as ContainerChoice;

            var selectedBalance =
                balanceFilter.SelectedItem as BalanceChoice;

            var query = new OutstandingReportQuery(
                DateOnly.FromDateTime(reportDate.Value.Date),
                customerSearch.Text,
                selected?.Id,
                selectedBalance?.Filter ?? OutstandingBalanceFilter.OutstandingOnly,
                includeInactive.Checked);

            var result = await outstanding.QueryAsync(query);
            currentResult = result;

            grid.Rows.Clear();

            foreach (var row in result.Rows)
            {
                var rowIndex = grid.Rows.Add(
                    row.CustomerCode,
                    row.CustomerName,
                    row.CustomerType ==
                        BinTracker.Core.CustomerType.Account
                            ? "Account"
                            : "Cash / COD",
                    row.ContainerType,
                    row.PositionText,
                    row.LastMovementDate?.ToString("dd/MM/yyyy") ?? "—",
                    row.IsActive ? "Active" : "Inactive");

                grid.Rows[rowIndex].Tag = row;
                grid.Rows[rowIndex].HeaderCell.Tag = rowIndex;
            }

            ResizeContentColumns();
            ReportGridMultiSort.Reapply(grid);

            var totals = result.ContainerTotals
                .Select(x => result.BalanceFilter switch
                {
                    OutstandingBalanceFilter.CreditsOnly =>
                        $"{x.ContainerType}: {x.CreditQuantity:N0} CREDIT",
                    OutstandingBalanceFilter.AllNonZero =>
                        $"{x.ContainerType}: {x.OutstandingQuantity:N0} OUT / {x.CreditQuantity:N0} CREDIT",
                    _ => $"{x.ContainerType}: {x.OutstandingQuantity:N0} OUT"
                })
                .ToList();

            var positionSummary = result.BalanceFilter switch
            {
                OutstandingBalanceFilter.CreditsOnly =>
                    $"{result.CreditPositionCount:N0} credit position(s)",
                OutstandingBalanceFilter.AllNonZero =>
                    $"{result.OutstandingPositionCount:N0} outstanding position(s) • {result.CreditPositionCount:N0} credit position(s)",
                _ => $"{result.OutstandingPositionCount:N0} outstanding position(s)"
            };

            summary.Text =
                $"As at {result.AsOfDate:dd/MM/yyyy} — {positionSummary}" +
                (totals.Count > 0
                    ? Environment.NewLine + string.Join("   •   ", totals)
                    : Environment.NewLine +
                      (result.BalanceFilter == OutstandingBalanceFilter.CreditsOnly
                          ? "No matching credit positions."
                          : "No matching outstanding positions."));
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                this,
                ex.Message,
                "Outstanding Containers",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
        finally
        {
            Enabled = true;
            UseWaitCursor = false;
        }
    }

    private OutstandingReportResult BuildDisplayedResultFromCurrentGrid(
        OutstandingReportResult result)
    {
        var displayedRows = grid.Rows
            .Cast<DataGridViewRow>()
            .Where(row => !row.IsNewRow)
            .Select(row => row.Tag as OutstandingReportRow)
            .Where(row => row is not null)
            .Cast<OutstandingReportRow>()
            .ToList();

        // The PDF is intentionally a printable snapshot of the dataset the
        // operator is currently viewing, including their chosen grid sort.
        return new OutstandingReportResult(
            result.AsOfDate,
            displayedRows,
            result.ContainerTotals,
            result.BalanceFilter);
    }

    private async Task GeneratePdfAsync(bool openAfter)
    {
        var result = currentResult;

        if (result is null)
        {
            MessageBox.Show(
                this,
                "Run the report first.",
                "Outstanding Containers",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return;
        }

        using var dialog = new SaveFileDialog
        {
            Title = "Generate Outstanding Containers PDF",
            Filter = "PDF file (*.pdf)|*.pdf",
            FileName = result.BalanceFilter == OutstandingBalanceFilter.CreditsOnly
                ? $"BinTracker_Credits_{result.AsOfDate:yyyyMMdd}.pdf"
                : $"BinTracker_Outstanding_{result.AsOfDate:yyyyMMdd}.pdf",
            AddExtension = true,
            DefaultExt = "pdf"
        };

        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;

        try
        {
            UseWaitCursor = true;
            Enabled = false;

            var printableResult =
                BuildDisplayedResultFromCurrentGrid(result);

            await pdfReports.GeneratePdfAsync(
                printableResult,
                dialog.FileName);

            if (openAfter)
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
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
                "Outstanding Containers PDF",
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
        var result = currentResult;

        if (result is null ||
            result.Rows.Count == 0)
        {
            MessageBox.Show(
                this,
                "Run the report first. There are no rows to export.",
                "Outstanding Containers",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return;
        }

        var exportResult =
            BuildDisplayedResultFromCurrentGrid(result);

        using var dialog = new SaveFileDialog
        {
            Title = "Export Outstanding Containers",
            Filter = "CSV file (*.csv)|*.csv",
            FileName = exportResult.BalanceFilter == OutstandingBalanceFilter.CreditsOnly
                ? $"BinTracker_Credits_{exportResult.AsOfDate:yyyyMMdd}.csv"
                : $"BinTracker_Outstanding_{exportResult.AsOfDate:yyyyMMdd}.csv",
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
            "As Of,Customer Code,Customer Name,Customer Type,Container,Position,Quantity,Last Movement,Status");

        foreach (var row in exportResult.Rows)
        {
            writer.WriteLine(string.Join(",",
                Csv(exportResult.AsOfDate.ToString("dd/MM/yyyy")),
                Csv(row.CustomerCode),
                Csv(row.CustomerName),
                Csv(row.CustomerType ==
                    BinTracker.Core.CustomerType.Account
                        ? "Account"
                        : "Cash / COD"),
                Csv(row.ContainerType),
                Csv(row.Balance > 0 ? "OUT" : "CREDIT"),
                Math.Abs(row.Balance).ToString(
                    System.Globalization.CultureInfo.InvariantCulture),
                Csv(row.LastMovementDate?.ToString("dd/MM/yyyy")
                    ?? string.Empty),
                Csv(row.IsActive ? "Active" : "Inactive")));
        }

        summary.Text +=
            Environment.NewLine +
            $"Exported CSV: {dialog.FileName}";

        await ReportCsvAudit.WriteAsync(
            this, audit,
            "OUTSTANDING_CONTAINERS_CSV_EXPORTED",
            exportResult.AsOfDate.ToString("yyyy-MM-dd"),
            $"Outstanding Containers CSV exported for {exportResult.AsOfDate:dd/MM/yyyy}: {exportResult.Rows.Count:N0} row(s).",
            dialog.FileName,
            exportResult.Rows.Count,
            new
            {
                AsOfDate = exportResult.AsOfDate,
                CustomerSearch = customerSearch.Text.Trim(),
                Container = containerFilter.Text,
                BalanceFilter = exportResult.BalanceFilter.ToString(),
                IncludeInactive = includeInactive.Checked
            });
    }

    private static Label ControlLabel(
        string text,
        int leftMargin = 0) =>
        new()
        {
            Text = text,
            AutoSize = true,
            Margin = new Padding(leftMargin, 9, 8, 0)
        };

    private static DataGridViewTextBoxColumn Column(
        string header,
        int width,
        DataGridViewAutoSizeColumnMode autoSize =
            DataGridViewAutoSizeColumnMode.None,
        string? name = null) =>
        new()
        {
            Name = name ?? header,
            HeaderText = header,
            Width = width,
            AutoSizeMode = autoSize,
            MinimumWidth = Math.Min(width, 90),
            SortMode = DataGridViewColumnSortMode.Programmatic
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

    private sealed record BalanceChoice(OutstandingBalanceFilter Filter, string Name)
    {
        public override string ToString() => Name;
    }

    private sealed record ContainerChoice(
        int? Id,
        string Name)
    {
        public override string ToString() => Name;
    }
}
