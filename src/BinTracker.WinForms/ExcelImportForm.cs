using BinTracker.Services;

namespace BinTracker.WinForms;

public sealed class ExcelImportForm : Form
{
    private readonly IExcelImportService service;

    private readonly TextBox filePath = new()
    {
        Dock = DockStyle.Fill,
        ReadOnly = true,
        Margin = Padding.Empty
    };

    private readonly Label analysisTitle = new()
    {
        AutoSize = true,
        Font = new Font("Segoe UI Semibold", 10.5F, FontStyle.Bold),
        ForeColor = Color.FromArgb(25, 95, 190),
        Margin = Padding.Empty
    };

    private readonly Label analysisDetails = new()
    {
        AutoSize = true,
        ForeColor = Color.FromArgb(70, 80, 95),
        Margin = new Padding(0, 4, 0, 0)
    };

    private readonly Panel warningPanel = new()
    {
        Dock = DockStyle.Top,
        Height = 64,
        BackColor = Color.FromArgb(255, 248, 225),
        Visible = false,
        Margin = new Padding(0, 7, 0, 0),
        Padding = new Padding(12, 9, 12, 9)
    };

    private readonly Label warningText = new()
    {
        Dock = DockStyle.Fill,
        AutoEllipsis = true,
        ForeColor = Color.FromArgb(150, 95, 0),
        TextAlign = ContentAlignment.MiddleLeft
    };

    private readonly Button viewDuplicatesButton = SecondaryButton("View duplicates...", 150);

    private readonly Label candidateSummary = new()
    {
        AutoSize = true,
        ForeColor = Color.FromArgb(25, 95, 190),
        Margin = new Padding(0, 6, 0, 0)
    };

    private readonly DataGridView sheets = Grid();
    private readonly DataGridView customers = Grid();

    private readonly Button analyseButton = PrimaryButton("Analyse", 125);
    private readonly Button nextButton = SecondaryButton("Next >", 115);
    private readonly Button cancelButton = SecondaryButton("Cancel", 115);

    private ExcelImportAnalysis? lastAnalysis;

    public ExcelImportForm(IExcelImportService service)
    {
        this.service = service;

        Text = "Excel Import Wizard";
        StartPosition = FormStartPosition.CenterParent;
        AutoScaleMode = AutoScaleMode.Dpi;
        ClientSize = new Size(1080, 900);
        MinimumSize = new Size(900, 700);
        BackColor = Color.FromArgb(245, 247, 250);
        Font = new Font("Segoe UI", 10F);
        FormBorderStyle = FormBorderStyle.Sizable;

        nextButton.Enabled = false;
        viewDuplicatesButton.Click += (_, _) => ViewDuplicates();

        Build();
    }

    private void Build()
    {
        var scrollHost = new Panel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            BackColor = Color.FromArgb(245, 247, 250),
            Padding = new Padding(12)
        };

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 1,
            RowCount = 7,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };

        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        for (var i = 0; i < root.RowCount; i++)
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        root.Controls.Add(BuildHeader(), 0, 0);
        root.Controls.Add(BuildStepper(), 0, 1);
        root.Controls.Add(BuildWorkbookPicker(), 0, 2);
        root.Controls.Add(BuildWorkbookStructure(), 0, 3);
        root.Controls.Add(BuildCandidatePreview(), 0, 4);
        root.Controls.Add(BuildReadOnlyNotice(), 0, 5);
        root.Controls.Add(BuildFooter(), 0, 6);

        scrollHost.Controls.Add(root);
        Controls.Add(scrollHost);
    }

    private Control BuildHeader()
    {
        var card = Card(new Padding(18, 14, 18, 14));

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 1,
            RowCount = 2,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };

        layout.Controls.Add(new Label
        {
            Text = "Excel Import Wizard — Analyse",
            AutoSize = true,
            Font = new Font("Segoe UI Semibold", 18F, FontStyle.Bold),
            Margin = new Padding(0, 0, 0, 4),
            Padding = new Padding(0, 0, 0, 3)
        }, 0, 0);

        layout.Controls.Add(new Label
        {
            Text =
                "This first import stage is read-only. BinTracker analyses the workbook " +
                "and previews what it finds before any data is imported.",
            AutoSize = true,
            ForeColor = Color.DimGray,
            MaximumSize = new Size(950, 0),
            Margin = Padding.Empty
        }, 0, 1);

        card.Controls.Add(layout);
        return card;
    }

    private Control BuildStepper()
    {
        var card = Card(new Padding(12, 4, 12, 2));

        var progress = new ImportProgressControl
        {
            Dock = DockStyle.Top,
            ActiveStep = 1
        };

        card.Controls.Add(progress);
        return card;
    }

    private Control BuildWorkbookPicker()
    {
        var card = Card(new Padding(18, 12, 18, 12));

        var outer = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            ColumnCount = 1,
            RowCount = 2,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };

        outer.Controls.Add(SectionHeading("1. Select Excel workbook"), 0, 0);

        var row = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            ColumnCount = 3,
            Margin = new Padding(0, 7, 0, 0),
            Padding = Padding.Empty
        };

        row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130F));
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 140F));

        row.Controls.Add(filePath, 0, 0);

        var browse = SecondaryButton("Browse...", 115);
        browse.Margin = new Padding(8, 0, 0, 0);
        browse.Click += (_, _) => Browse();
        row.Controls.Add(browse, 1, 0);

        analyseButton.Margin = new Padding(8, 0, 0, 0);
        analyseButton.Click += async (_, _) => await AnalyseAsync();
        row.Controls.Add(analyseButton, 2, 0);

        outer.Controls.Add(row, 0, 1);
        card.Controls.Add(outer);

        return card;
    }

    private Control BuildWorkbookStructure()
    {
        var card = Card(new Padding(18, 12, 18, 12));

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 1,
            RowCount = 5,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };

        layout.Controls.Add(SectionHeading("2. Workbook structure (summary)"), 0, 0);

        ConfigureSheetGrid();
        sheets.Height = 160;
        sheets.Margin = new Padding(0, 7, 0, 0);
        layout.Controls.Add(sheets, 0, 1);

        var gridFooter = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            ColumnCount = 2,
            Margin = new Padding(0, 6, 0, 0),
            Padding = Padding.Empty
        };
        gridFooter.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        gridFooter.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        var statusHost = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        statusHost.Controls.Add(analysisTitle);

        var viewAll = SecondaryButton("View all worksheets", 180);
        viewAll.AutoSize = false;
        viewAll.Margin = Padding.Empty;
        viewAll.Click += (_, _) => ViewAllWorksheets();

        gridFooter.Controls.Add(statusHost, 0, 0);
        gridFooter.Controls.Add(viewAll, 1, 0);
        layout.Controls.Add(gridFooter, 0, 2);

        layout.Controls.Add(analysisDetails, 0, 3);

        var warningLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        warningLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        warningLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        warningLayout.Controls.Add(warningText, 0, 0);

        viewDuplicatesButton.Margin = new Padding(10, 2, 0, 2);
        warningLayout.Controls.Add(viewDuplicatesButton, 1, 0);

        warningPanel.Controls.Add(warningLayout);
        layout.Controls.Add(warningPanel, 0, 4);

        card.Controls.Add(layout);
        return card;
    }

    private Control BuildCandidatePreview()
    {
        var card = Card(new Padding(18, 12, 18, 12));

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 1,
            RowCount = 4,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };

        layout.Controls.Add(
            SectionHeading("3. Detected customer candidates (sample)"),
            0, 0);

        layout.Controls.Add(new Label
        {
            Text =
                "These look like customer codes/names found in the workbook. " +
                "We'll confirm and map them in the next step.",
            AutoSize = true,
            ForeColor = Color.DimGray,
            MaximumSize = new Size(950, 0),
            Margin = new Padding(0, 4, 0, 0)
        }, 0, 1);

        ConfigureCustomerGrid();
        customers.Height = 150;
        customers.Margin = new Padding(0, 7, 0, 0);
        layout.Controls.Add(customers, 0, 2);

        layout.Controls.Add(candidateSummary, 0, 3);

        card.Controls.Add(layout);
        return card;
    }

    private Control BuildReadOnlyNotice()
    {
        var panel = new Panel
        {
            Dock = DockStyle.Top,
            Height = 52,
            BackColor = Color.FromArgb(232, 242, 255),
            Padding = new Padding(14),
            Margin = new Padding(0, 0, 0, 8)
        };

        panel.Controls.Add(new Label
        {
            Dock = DockStyle.Fill,
            Text =
                "ⓘ  Nothing is imported in this step. Click Analyse to scan the workbook " +
                "and review what BinTracker finds.",
            TextAlign = ContentAlignment.MiddleLeft,
            ForeColor = Color.FromArgb(25, 75, 150)
        });

        return panel;
    }

    private Control BuildFooter()
    {
        var panel = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            Padding = new Padding(0, 6, 0, 6),
            Margin = Padding.Empty
        };

        cancelButton.Click += (_, _) => Close();
        nextButton.Click += (_, _) =>
        {
            MessageBox.Show(
                this,
                "The Map step is the next development stage. No data has been imported.",
                "Excel Import Wizard",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        };

        panel.Controls.Add(cancelButton);
        panel.Controls.Add(nextButton);

        return panel;
    }

    private void ConfigureSheetGrid()
    {
        if (sheets.Columns.Count > 0)
            return;

        sheets.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Worksheet",
            Width = 225
        });
        sheets.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Rows",
            Width = 70
        });
        sheets.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Columns",
            Width = 78
        });
        sheets.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Buyer column(s)",
            Width = 120
        });
        sheets.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Candidates",
            Width = 100
        });
        sheets.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Detection notes",
            AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
        });
    }

    private void ConfigureCustomerGrid()
    {
        if (customers.Columns.Count > 0)
            return;

        customers.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Worksheet",
            Width = 175
        });
        customers.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Customer / Buyer",
            AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
        });
        customers.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Type (detected)",
            Width = 145
        });
        customers.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Source (cell)",
            Width = 130
        });
    }

    private static IEnumerable<IGrouping<string, ImportCustomerCandidate>> GetDuplicateGroups(
        ExcelImportAnalysis analysis) =>
        analysis.CustomerCandidates
            .Where(x => !string.IsNullOrWhiteSpace(x.CustomerCode))
            .GroupBy(
                x => x.CustomerCode.Trim(),
                StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1)
            .OrderBy(group => group.Key, StringComparer.OrdinalIgnoreCase);

    private void ViewDuplicates()
    {
        if (lastAnalysis is null)
        {
            MessageBox.Show(
                this,
                "Analyse a workbook first.",
                "Potential duplicates",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return;
        }

        var groups = GetDuplicateGroups(lastAnalysis).ToList();

        if (groups.Count == 0)
        {
            MessageBox.Show(
                this,
                "No duplicate customer code/name occurrences were detected.",
                "Potential duplicates",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return;
        }

        using var form = new Form
        {
            Text = "Potential duplicate customer occurrences",
            StartPosition = FormStartPosition.CenterParent,
            AutoScaleMode = AutoScaleMode.Dpi,
            ClientSize = new Size(1000, 620),
            MinimumSize = new Size(820, 500),
            BackColor = Color.White,
            Font = Font
        };

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Padding = new Padding(16)
        };
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        root.Controls.Add(new Label
        {
            Text =
                "These are repeated appearances found across the workbook. " +
                "Some are expected because report/validation sheets repeat source data. " +
                "The upcoming Map step will classify sheets before any import occurs.",
            AutoSize = true,
            MaximumSize = new Size(940, 0),
            ForeColor = Color.DimGray,
            Margin = new Padding(0, 0, 0, 10)
        }, 0, 0);

        var grid = Grid();
        grid.Dock = DockStyle.Fill;

        grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Customer / Buyer",
            Width = 250
        });
        grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Occurrences",
            Width = 100
        });
        grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Worksheet",
            Width = 240
        });
        grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Cell",
            Width = 90
        });
        grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Detected Type",
            Width = 130
        });
        grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Current classification",
            AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
        });

        foreach (var group in groups)
        {
            foreach (var occurrence in group)
            {
                grid.Rows.Add(
                    group.Key,
                    group.Count(),
                    occurrence.Worksheet,
                    occurrence.SourceCell,
                    occurrence.CustomerType,
                    "Unclassified — Map step");
            }
        }

        root.Controls.Add(grid, 0, 1);

        var close = SecondaryButton("Close", 110);
        close.Margin = new Padding(0, 10, 0, 0);
        close.Click += (_, _) => form.Close();

        var footer = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false
        };
        footer.Controls.Add(close);
        root.Controls.Add(footer, 0, 2);

        form.Controls.Add(root);
        form.ShowDialog(this);
    }

    private void Browse()
    {
        using var dialog = new OpenFileDialog
        {
            Title = "Select BinTracker Excel workbook",
            Filter = "Excel workbooks (*.xlsm;*.xlsx)|*.xlsm;*.xlsx",
            Multiselect = false,
            CheckFileExists = true
        };

        if (dialog.ShowDialog(this) == DialogResult.OK)
            filePath.Text = dialog.FileName;
    }

    private void ViewAllWorksheets()
    {
        if (lastAnalysis is null)
        {
            MessageBox.Show(
                this,
                "Analyse a workbook first.",
                "View all worksheets",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return;
        }

        using var form = new Form
        {
            Text = "All worksheets",
            StartPosition = FormStartPosition.CenterParent,
            AutoScaleMode = AutoScaleMode.Dpi,
            ClientSize = new Size(920, 560),
            MinimumSize = new Size(760, 480),
            BackColor = Color.White,
            Font = Font
        };

        var grid = Grid();
        grid.Dock = DockStyle.Fill;

        grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Worksheet",
            Width = 260
        });
        grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Rows",
            Width = 80
        });
        grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Columns",
            Width = 85
        });
        grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Buyer columns",
            Width = 110
        });
        grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Candidates",
            Width = 100
        });
        grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Detection notes",
            AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
        });

        foreach (var sheet in lastAnalysis.Worksheets)
        {
            grid.Rows.Add(
                sheet.Name,
                sheet.UsedRows,
                sheet.UsedColumns,
                sheet.BuyerColumns,
                sheet.BuyerCandidates,
                sheet.Status);
        }

        form.Controls.Add(grid);
        form.ShowDialog(this);
    }

    private async Task AnalyseAsync()
    {
        analysisTitle.Text = string.Empty;
        analysisDetails.Text = string.Empty;
        candidateSummary.Text = string.Empty;
        warningText.Text = string.Empty;
        warningPanel.Visible = false;
        sheets.Rows.Clear();
        customers.Rows.Clear();
        nextButton.Enabled = false;
        lastAnalysis = null;

        try
        {
            UseWaitCursor = true;
            analyseButton.Enabled = false;

            var analysis = await service.AnalyzeAsync(filePath.Text);
            lastAnalysis = analysis;
            nextButton.Enabled = true;

            foreach (var sheet in analysis.Worksheets)
            {
                sheets.Rows.Add(
                    sheet.Name,
                    sheet.UsedRows,
                    sheet.UsedColumns,
                    sheet.BuyerColumns == 0 ? "-" : sheet.BuyerColumns.ToString(),
                    sheet.BuyerCandidates == 0 ? "-" : sheet.BuyerCandidates.ToString(),
                    sheet.Status);
            }

            const int sampleSize = 100;
            foreach (var customer in analysis.CustomerCandidates.Take(sampleSize))
            {
                customers.Rows.Add(
                    customer.Worksheet,
                    customer.CustomerCode,
                    customer.CustomerType,
                    customer.SourceCell);
            }

            analysisTitle.Text = "✓ Workbook analysed successfully";

            analysisDetails.Text =
                $"Worksheets: {analysis.WorksheetCount:N0}    " +
                $"Unique customers: {analysis.UniqueCustomerCount:N0}    " +
                $"Occurrences found: {analysis.CustomerCandidateCount:N0}    " +
                $"B/Fwd / daily rows: {analysis.SnapshotCandidateCount:N0}";

            candidateSummary.Text =
                analysis.CustomerCandidateCount > sampleSize
                    ? $"Showing first {sampleSize:N0} of {analysis.CustomerCandidateCount:N0} occurrence(s). " +
                      $"{analysis.UniqueCustomerCount:N0} unique customer code/name(s) detected."
                    : $"{analysis.CustomerCandidateCount:N0} occurrence(s), " +
                      $"{analysis.UniqueCustomerCount:N0} unique customer code/name(s) detected.";

            if (analysis.Warnings.Count > 0)
            {
                var duplicateGroups = GetDuplicateGroups(analysis).ToList();
                var otherWarnings = analysis.Warnings
                    .Where(x => !x.StartsWith(
                        "Potential duplicate customer codes detected",
                        StringComparison.OrdinalIgnoreCase))
                    .ToList();

                warningPanel.Visible = true;
                viewDuplicatesButton.Visible = duplicateGroups.Count > 0;

                if (duplicateGroups.Count > 0 && otherWarnings.Count > 0)
                {
                    warningText.Text =
                        $"⚠ {duplicateGroups.Count:N0} potential duplicate customer code/name(s) detected. " +
                        $"{otherWarnings.Count:N0} other workbook warning(s).";
                }
                else if (duplicateGroups.Count > 0)
                {
                    warningText.Text =
                        $"⚠ {duplicateGroups.Count:N0} potential duplicate customer code/name(s) detected. " +
                        "These will be resolved during the Map step.";
                }
                else
                {
                    warningText.Text =
                        $"⚠ {otherWarnings.Count:N0} workbook warning(s) detected. " +
                        string.Join("  ", otherWarnings.Take(2));
                }
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                this,
                ex.Message,
                "Excel Import",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
        finally
        {
            analyseButton.Enabled = true;
            UseWaitCursor = false;
        }
    }

    private static Label SectionHeading(string text) => new()
    {
        Text = text,
        AutoSize = true,
        Font = new Font("Segoe UI Semibold", 11F, FontStyle.Bold),
        Margin = Padding.Empty
    };

    private static Panel Card(Padding padding) => new()
    {
        Dock = DockStyle.Top,
        AutoSize = true,
        AutoSizeMode = AutoSizeMode.GrowAndShrink,
        BackColor = Color.White,
        Padding = padding,
        Margin = new Padding(0, 0, 0, 8)
    };

    private static Button PrimaryButton(string text, int width)
    {
        var button = new Button
        {
            Text = text,
            AutoSize = false,
            Size = new Size(width, 40),
            TextAlign = ContentAlignment.MiddleCenter,
            BackColor = Color.FromArgb(25, 95, 190),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Margin = new Padding(8, 0, 0, 0)
        };
        button.FlatAppearance.BorderSize = 0;
        return button;
    }

    private static Button SecondaryButton(string text, int width) => new()
    {
        Text = text,
        AutoSize = false,
        Size = new Size(width, 40),
        TextAlign = ContentAlignment.MiddleCenter,
        Margin = new Padding(8, 0, 0, 0)
    };

    private static DataGridView Grid() => new()
    {
        Dock = DockStyle.Top,
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
}
