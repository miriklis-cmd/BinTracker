using BinTracker.Core;
using BinTracker.Services;

namespace BinTracker.WinForms;

public sealed class WeeklyMovementsReportForm : BinTrackerForm
{
    private readonly IWeeklyMovementsReportService reports;
    private readonly IWeeklyMovementsReportPdfService pdfReports;
    private readonly IContainerTypeService containerTypes;

    private readonly DateTimePicker weekPicker = new()
    {
        Format = DateTimePickerFormat.Short,
        Width = 140,
        Value = DateTime.Today
    };
    private readonly TextBox customerSearch = new()
    {
        Width = 220,
        PlaceholderText = "Type, then press Enter"
    };
    private readonly ComboBox containerFilter = ChoiceBox(175);
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
    private readonly Label resolvedWeekLabel = new()
    {
        AutoSize = true,
        ForeColor = Color.FromArgb(60, 75, 95),
        Margin = new Padding(14, 9, 0, 0)
    };
    private readonly TabControl tabs = new() { Dock = DockStyle.Fill };
    private readonly Label summaryLabel = new()
    {
        AutoSize = true,
        ForeColor = Color.FromArgb(60, 75, 95),
        MaximumSize = new Size(1350, 0)
    };
    private readonly DataGridView detailGrid = Grid();
    private readonly DataGridView summaryGrid = Grid();
    private WeeklyMovementsReportResult? current;
    private bool autoRefreshReady;

    public WeeklyMovementsReportForm(
        IWeeklyMovementsReportService reports,
        IWeeklyMovementsReportPdfService pdfReports,
        IContainerTypeService containerTypes)
    {
        this.reports = reports;
        this.pdfReports = pdfReports;
        this.containerTypes = containerTypes;

        Text = "Weekly Movements";
        StartPosition = FormStartPosition.Manual;
        AutoScaleMode = AutoScaleMode.Dpi;
        MinimumSize = new Size(1100, 720);
        Font = new Font("Segoe UI", 10F);
        BackColor = Color.FromArgb(245, 247, 250);

        weekPicker.MaxDate = DateTime.Today;

        weekPicker.ValueChanged += async (_, _) =>
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
        ConfigureDetailGrid();
        ConfigureSummaryGrid();

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
            Text = "Weekly Movements",
            AutoSize = true,
            Font = new Font("Segoe UI Semibold", 19F, FontStyle.Bold),
            Margin = new Padding(0, 0, 0, 5)
        }, 0, 0);
        header.Controls.Add(new Label
        {
            Text = "Monday-to-Sunday reporting. Daily Detail shows every movement; Weekly Overview totals OUT and IN by customer/container.",
            AutoSize = true,
            ForeColor = Color.FromArgb(80, 90, 105)
        }, 0, 1);
        root.Controls.Add(header, 0, 0);

        // Use an auto-sizing TableLayoutPanel rather than a Panel with a
        // docked child.  A WinForms Panel can under-measure a docked
        // AutoSize child after FlowLayoutPanel wrapping, which allowed the
        // summary row to overlap/clamp the action buttons.
        var controlsCard = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 1,
            RowCount = 1,
            BackColor = Color.White,
            Padding = new Padding(16, 12, 16, 12),
            Margin = new Padding(0, 0, 0, 10)
        };
        controlsCard.ColumnStyles.Add(
            new ColumnStyle(SizeType.Percent, 100F));
        controlsCard.RowStyles.Add(
            new RowStyle(SizeType.AutoSize));
        var rows = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 1,
            RowCount = 3
        };
        rows.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        rows.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        rows.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        var filters = Flow();
        filters.Controls.Add(ControlLabel("Select date"));
        filters.Controls.Add(weekPicker);
        filters.Controls.Add(resolvedWeekLabel);
        filters.Controls.Add(ControlLabel("Customer", 14));
        filters.Controls.Add(customerSearch);
        filters.Controls.Add(ControlLabel("Container", 14));
        filters.Controls.Add(containerFilter);
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
        // This row is measured by the surrounding AutoSize TableLayoutPanels,
        // so wrapped buttons contribute their full preferred height.
        actions.Padding = new Padding(0, 8, 0, 8);
        actions.AutoSize = true;
        actions.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        actions.Controls.Add(ActionButton(
            "This Week",
            110,
            async () => await SetDateAndRefreshAsync(DateTime.Today)));
        actions.Controls.Add(ActionButton(
            "Last Week",
            110,
            async () => await SetDateAndRefreshAsync(
                DateTime.Today.AddDays(-7))));
        actions.Controls.Add(ActionButton("Generate PDF", 140, async () => await GeneratePdfAsync(false)));
        actions.Controls.Add(ActionButton("Generate && Open", 175, async () => await GeneratePdfAsync(true)));
        var csv = new Button { Text = "Export CSV", Size = new Size(135, 40), Margin = new Padding(8,0,0,0) };
        csv.Click += (_, _) => ExportCsv();
        actions.Controls.Add(csv);

        rows.Controls.Add(filters,0,0);
        rows.Controls.Add(options,0,1);
        rows.Controls.Add(actions,0,2);
        controlsCard.Controls.Add(rows, 0, 0);
        root.Controls.Add(controlsCard,0,1);

        var summaryCard = new Panel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            BackColor = Color.White,
            Padding = new Padding(16,10,16,10),
            Margin = new Padding(0,0,0,10)
        };
        summaryCard.Controls.Add(summaryLabel);
        root.Controls.Add(summaryCard,0,2);

        var detailPage = new TabPage("Daily Detail") { Padding = new Padding(8) };
        detailPage.Controls.Add(detailGrid);
        var summaryPage = new TabPage("Weekly Overview") { Padding = new Padding(8) };
        summaryPage.Controls.Add(summaryGrid);
        tabs.TabPages.Add(detailPage);
        tabs.TabPages.Add(summaryPage);
        tabs.SelectedIndexChanged += (_, _) => UpdateViewOptions();
        root.Controls.Add(tabs,0,3);

        var close = new Button { Text="Close", Size=new Size(110,38), Margin=new Padding(0,10,0,0) };
        close.Click += (_,_) => Close();
        var footer = new FlowLayoutPanel
        {
            Dock=DockStyle.Fill, AutoSize=true, FlowDirection=FlowDirection.RightToLeft
        };
        footer.Controls.Add(close);
        root.Controls.Add(footer,0,4);
        Controls.Add(root);
        UpdateViewOptions();
    }

    private void UpdateViewOptions()
    {
        var detailView = tabs.SelectedIndex == 0;

        includeNotesInExports.Enabled = detailView;

        if (!detailView)
            includeNotesInExports.Checked = false;
    }

    private async Task InitialiseAsync()
    {
        containerFilter.Items.Add(new Choice<int?>(null, "All containers"));

        var configuredContainers =
            await containerTypes.SearchAsync(
                search: null,
                includeInactive: true);

        foreach (var container in configuredContainers)
        {
            var label = container.IsActive
                ? container.Name
                : $"{container.Name} (inactive)";

            containerFilter.Items.Add(
                new Choice<int?>(container.Id, label));
        }

        containerFilter.SelectedIndex = 0;

        sourceFilter.Items.Add(new Choice<MovementSource?>(null,"All sources"));
        sourceFilter.Items.Add(new Choice<MovementSource?>(MovementSource.Manual,"Single Entry"));
        sourceFilter.Items.Add(new Choice<MovementSource?>(MovementSource.Batch,"Batch Entry"));
        sourceFilter.Items.Add(new Choice<MovementSource?>(MovementSource.ExcelImport,"Excel Import"));
        sourceFilter.SelectedIndex=0;

        autoRefreshReady = true;
        await LoadReportAsync();
    }

    private async Task SetDateAndRefreshAsync(DateTime date)
    {
        autoRefreshReady = false;
        weekPicker.Value = date;
        autoRefreshReady = true;
        await LoadReportAsync();
    }

    private async Task LoadReportAsync()
    {
        try
        {
            Enabled=false; UseWaitCursor=true;
            current = await reports.QueryAsync(new WeeklyMovementsReportQuery(
                DateOnly.FromDateTime(weekPicker.Value.Date),
                customerSearch.Text,
                (containerFilter.SelectedItem as Choice<int?>)?.Value,
                (sourceFilter.SelectedItem as Choice<MovementSource?>)?.Value,
                includeAdjustments.Checked));

            detailGrid.Rows.Clear();
            foreach (var row in current.Rows)
            {
                var i=detailGrid.Rows.Add(
                    row.MovementDate.ToString("ddd dd/MM"),
                    row.CustomerCode,row.CustomerName,CustomerTypeText(row.CustomerType),
                    row.ContainerType,row.DirectionText,row.Quantity.ToString("N0"),
                    row.SourceText,row.Reference,row.Notes,row.EnteredBy);
                detailGrid.Rows[i].Tag=row;
            }

            summaryGrid.Rows.Clear();
            foreach (var row in current.Summary)
            {
                var i=summaryGrid.Rows.Add(
                    row.CustomerCode,row.CustomerName,row.ContainerType,
                    row.OutQuantity.ToString("N0"),row.InQuantity.ToString("N0"),
                    row.NetQuantity.ToString("N0"));
                summaryGrid.Rows[i].Tag=row;
            }

            resolvedWeekLabel.Text =
                current.DataThroughDate < current.WeekEnd
                    ? $"Week: {current.WeekStart:dd/MM/yyyy} – {current.WeekEnd:dd/MM/yyyy} (activity through {current.DataThroughDate:dd/MM/yyyy})"
                    : $"Week: {current.WeekStart:dd/MM/yyyy} – {current.WeekEnd:dd/MM/yyyy}";

            AdjustGridColumnWidths();

            summaryLabel.Text =
                $"{current.WeekStart:dd/MM/yyyy} – {current.WeekEnd:dd/MM/yyyy}" +
                (current.DataThroughDate < current.WeekEnd
                    ? $" • activity through {current.DataThroughDate:dd/MM/yyyy}"
                    : "") +
                $" — " +
                $"{current.Rows.Count:N0} movement row(s) • {current.OutQuantity:N0} OUT • " +
                $"{current.InQuantity:N0} IN • Net {current.NetQuantity:+#;-#;0}" +
                Environment.NewLine +
                "Daily Detail = individual movements   •   Weekly Overview = customer/container totals for the whole week";
        }
        catch(Exception ex)
        {
            MessageBox.Show(this,ex.Message,"Weekly Movements",MessageBoxButtons.OK,MessageBoxIcon.Error);
        }
        finally { Enabled=true; UseWaitCursor=false; }
    }

    private WeeklyMovementsReportResult BuildDisplayedResult()
    {
        var source = current ?? throw new InvalidOperationException("Run the report first.");
        var rows = detailGrid.Rows.Cast<DataGridViewRow>()
            .Where(x => !x.IsNewRow).Select(x => x.Tag as WeeklyMovementReportRow)
            .Where(x => x is not null).Cast<WeeklyMovementReportRow>().ToList();
        var summary = summaryGrid.Rows.Cast<DataGridViewRow>()
            .Where(x => !x.IsNewRow).Select(x => x.Tag as WeeklyMovementSummaryRow)
            .Where(x => x is not null).Cast<WeeklyMovementSummaryRow>().ToList();
        return new WeeklyMovementsReportResult(source.WeekStart, source.WeekEnd, rows, summary);
    }

    private async Task GeneratePdfAsync(bool openAfter)
    {
        if (current is null) { MessageBox.Show(this, "Run the report first."); return; }
        var result = BuildDisplayedResult();
        var summaryView = tabs.SelectedIndex == 1;
        using var dialog = new SaveFileDialog
        {
            Title = "Generate Weekly Movements PDF", Filter = "PDF file (*.pdf)|*.pdf",
            FileName = $"BinTracker_Weekly_Movements_{result.WeekStart:yyyyMMdd}_{result.WeekEnd:yyyyMMdd}_{(summaryView ? "Overview" : "Detail")}.pdf",
            AddExtension = true, DefaultExt = "pdf"
        };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        try
        {
            Enabled = false; UseWaitCursor = true;
            await pdfReports.GeneratePdfAsync(result, dialog.FileName, summaryView, includeNotesInExports.Checked);
            if (openAfter) System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            { FileName = dialog.FileName, UseShellExecute = true });
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Weekly Movements PDF", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally { Enabled = true; UseWaitCursor = false; }
    }

    private void AdjustGridColumnWidths()
    {
        static int WidthFor(DataGridView grid, string columnName, IEnumerable<string> values, int minimum, int maximum)
        {
            var header = grid.Columns[columnName].HeaderText;
            var longest = values.Append(header).OrderByDescending(x => x?.Length ?? 0).FirstOrDefault() ?? header;
            return Math.Clamp(TextRenderer.MeasureText(longest, grid.Font).Width + 34, minimum, maximum);
        }
        if (current is null) return;
        detailGrid.Columns["Date"].Width = WidthFor(detailGrid, "Date", current.Rows.Select(x => x.MovementDate.ToString("ddd dd/MM/yyyy")), 135, 175);
        detailGrid.Columns["Code"].Width = WidthFor(detailGrid, "Code", current.Rows.Select(x => x.CustomerCode), 150, 260);
        summaryGrid.Columns["Code"].Width = WidthFor(summaryGrid, "Code", current.Summary.Select(x => x.CustomerCode), 150, 260);
    }

    private void ExportCsv()
    {
        if (current is null)
        {
            MessageBox.Show(this, "Run the report first.");
            return;
        }

        var overviewView = tabs.SelectedIndex == 1;
        var viewName = overviewView ? "Overview" : "Detail";

        using var dialog = new SaveFileDialog
        {
            Title = "Export Weekly Movements",
            Filter = "CSV file (*.csv)|*.csv",
            FileName =
                $"BinTracker_Weekly_Movements_{current.WeekStart:yyyyMMdd}_{current.WeekEnd:yyyyMMdd}_{viewName}.csv",
            AddExtension = true,
            DefaultExt = "csv"
        };

        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;

        using var writer = new StreamWriter(
            dialog.FileName,
            false,
            new System.Text.UTF8Encoding(true));

        if (overviewView)
        {
            writer.WriteLine(
                "Customer Code,Customer Name,Container,OUT,IN,Net");

            foreach (DataGridViewRow gridRow in summaryGrid.Rows)
            {
                if (gridRow.Tag is not WeeklyMovementSummaryRow row)
                    continue;

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

            return;
        }

        writer.WriteLine(includeNotesInExports.Checked
            ? "Date,Customer Code,Customer Name,Customer Type,Container,Direction,Quantity,Source,Reference,Notes,Entered By"
            : "Date,Customer Code,Customer Name,Customer Type,Container,Direction,Quantity,Source,Reference,Entered By");

        foreach (DataGridViewRow gridRow in detailGrid.Rows)
        {
            if (gridRow.Tag is not WeeklyMovementReportRow row)
                continue;

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
    }

    private void ConfigureDetailGrid()
    {
        detailGrid.Columns.Add(Column("Date",135,"Date"));
        detailGrid.Columns.Add(Column("Code",150,"Code"));
        detailGrid.Columns.Add(Column("Customer",220,"Customer",DataGridViewAutoSizeColumnMode.Fill));
        detailGrid.Columns.Add(Column("Type",115,"Type"));
        detailGrid.Columns.Add(Column("Container",135,"Container"));
        detailGrid.Columns.Add(Column("Direction",95,"Direction"));
        detailGrid.Columns.Add(Column("Qty",75,"Quantity"));
        detailGrid.Columns.Add(Column("Source",135,"Source"));
        detailGrid.Columns.Add(Column("Reference",140,"Reference"));
        detailGrid.Columns.Add(Column("Notes",180,"Notes"));
        detailGrid.Columns.Add(Column("Entered by",110,"EnteredBy"));
        detailGrid.SortCompare += (_,e) =>
        {
            if(detailGrid.Columns[e.Column.Index].Name!="Quantity") return;
            var l=detailGrid.Rows[e.RowIndex1].Tag as WeeklyMovementReportRow;
            var r=detailGrid.Rows[e.RowIndex2].Tag as WeeklyMovementReportRow;
            if(l is null || r is null) return;
            e.SortResult=l.Quantity.CompareTo(r.Quantity);
            if(e.SortResult==0) e.SortResult=l.MovementId.CompareTo(r.MovementId);
            e.Handled=true;
        };
    }

    private void ConfigureSummaryGrid()
    {
        summaryGrid.Columns.Add(Column("Code",150,"Code"));
        summaryGrid.Columns.Add(Column("Customer",250,"Customer",DataGridViewAutoSizeColumnMode.Fill));
        summaryGrid.Columns.Add(Column("Container",150,"Container"));
        summaryGrid.Columns.Add(Column("OUT",100,"Out"));
        summaryGrid.Columns.Add(Column("IN",100,"In"));
        summaryGrid.Columns.Add(Column("Net",100,"Net"));
        summaryGrid.SortCompare += (_,e) =>
        {
            if(summaryGrid.Rows[e.RowIndex1].Tag is not WeeklyMovementSummaryRow l ||
               summaryGrid.Rows[e.RowIndex2].Tag is not WeeklyMovementSummaryRow r) return;
            e.SortResult=summaryGrid.Columns[e.Column.Index].Name switch
            {
                "Out"=>l.OutQuantity.CompareTo(r.OutQuantity),
                "In"=>l.InQuantity.CompareTo(r.InQuantity),
                "Net"=>l.NetQuantity.CompareTo(r.NetQuantity),
                _=>0
            };
            if(summaryGrid.Columns[e.Column.Index].Name is "Out" or "In" or "Net") e.Handled=true;
        };
    }

    private void ApplyResponsiveBounds()
    {
        var screen=Owner is not null ? Screen.FromControl(Owner) : Screen.FromPoint(Cursor.Position);
        var area=screen.WorkingArea;
        Width=Math.Clamp((int)Math.Round(area.Width*.92),MinimumSize.Width,1900);
        Height=Math.Clamp((int)Math.Round(area.Height*.88),MinimumSize.Height,1100);
        Left=area.Left+Math.Max(0,(area.Width-Width)/2);
        Top=area.Top+Math.Max(0,(area.Height-Height)/2);
    }

    private static DataGridView Grid()=>new()
    {
        Dock=DockStyle.Fill,ReadOnly=true,AllowUserToAddRows=false,AllowUserToDeleteRows=false,
        AllowUserToResizeRows=false,MultiSelect=false,SelectionMode=DataGridViewSelectionMode.FullRowSelect,
        RowHeadersVisible=false,AutoGenerateColumns=false,BackgroundColor=Color.White,
        BorderStyle=BorderStyle.FixedSingle,ScrollBars=ScrollBars.Both
    };
    private static FlowLayoutPanel Flow()=>new()
    {
        Dock=DockStyle.Top,AutoSize=true,AutoSizeMode=AutoSizeMode.GrowAndShrink,
        FlowDirection=FlowDirection.LeftToRight,WrapContents=true,Margin=Padding.Empty
    };
    private static Button ActionButton(string text,int width,Func<Task> action)
    {
        var b=new Button{Text=text,Size=new Size(width,40),Margin=new Padding(8,0,0,0)};
        b.Click+=async(_,_)=>await action(); return b;
    }
    private static Label ControlLabel(string text,int left=0)=>new()
    {Text=text,AutoSize=true,Margin=new Padding(left,9,8,0)};
    private static ComboBox ChoiceBox(int width)=>new()
    {Width=width,DropDownStyle=ComboBoxStyle.DropDownList};
    private static DataGridViewTextBoxColumn Column(string header,int width,string name,
        DataGridViewAutoSizeColumnMode auto=DataGridViewAutoSizeColumnMode.None)=>new()
    {Name=name,HeaderText=header,Width=width,MinimumWidth=Math.Min(width,90),AutoSizeMode=auto,
     SortMode=DataGridViewColumnSortMode.Automatic};
    private static string CustomerTypeText(CustomerType t)=>t==CustomerType.Account?"Account":"Cash / COD";
    private static string Csv(string value)
    {
        var escaped=value.Replace("\"","\"\"");
        return value.Contains(',')||value.Contains('"')||value.Contains('\r')||value.Contains('\n')
            ? $"\"{escaped}\"":escaped;
    }
    private sealed record Choice<T>(T Value,string Text)
    { public override string ToString()=>Text; }
}
