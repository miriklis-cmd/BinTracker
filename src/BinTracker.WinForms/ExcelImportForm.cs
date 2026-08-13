using BinTracker.Services;

namespace BinTracker.WinForms;

public sealed class ExcelImportForm : Form
{
    private readonly IExcelImportService service;
    private readonly ICustomerService customerService;
    private readonly IContainerTypeService containerTypeService;
    private readonly IBalanceService balanceService;
    private readonly IImportExecutionService importExecutionService;

    private readonly Panel pageHost = new()
    {
        Dock = DockStyle.Fill,
        BackColor = Color.FromArgb(245, 247, 250)
    };

    private readonly ImportProgressControl progress = new()
    {
        Dock = DockStyle.Top,
        ActiveStep = 1
    };

    private readonly TextBox filePath = new()
    {
        Dock = DockStyle.Fill,
        ReadOnly = true,
        Margin = Padding.Empty
    };

    private readonly Label analyseResultTitle = new()
    {
        AutoSize = true,
        Font = new Font("Segoe UI Semibold", 12F, FontStyle.Bold),
        ForeColor = Color.FromArgb(25, 95, 190)
    };

    private readonly Label analyseResultDetails = new()
    {
        AutoSize = true,
        ForeColor = Color.FromArgb(70, 80, 95),
        MaximumSize = new Size(860, 0)
    };

    private readonly Panel analyseWarningPanel = new()
    {
        Dock = DockStyle.Top,
        AutoSize = true,
        BackColor = Color.FromArgb(255, 248, 225),
        Padding = new Padding(12),
        Visible = false
    };

    private readonly Label analyseWarningText = new()
    {
        AutoSize = true,
        ForeColor = Color.FromArgb(150, 95, 0),
        MaximumSize = new Size(720, 0)
    };

    private readonly Button analyseButton = PrimaryButton("Analyse", 125);
    private readonly Button nextButton = PrimaryButton("Next >", 115);
    private readonly Button backButton = SecondaryButton("< Back", 115);
    private readonly Button cancelButton = SecondaryButton("Cancel", 115);
    private readonly Button viewDuplicatesButton = SecondaryButton("View duplicates...", 150);

    private readonly DataGridView mappingGrid = Grid();
    private readonly DataGridView candidateGrid = Grid();
    private readonly DataGridView reviewGrid = Grid();
    private readonly DataGridView reconciliationGrid = Grid();

    private readonly Label reviewSummary = new()
    {
        AutoSize = true,
        ForeColor = Color.FromArgb(25, 95, 190),
        MaximumSize = new Size(1260, 0)
    };

    private readonly Label reviewWarning = new()
    {
        AutoSize = true,
        ForeColor = Color.FromArgb(150, 95, 0),
        MaximumSize = new Size(1260, 0)
    };

    private readonly Label reviewSourcePrimary = ReviewMetricPrimary();
    private readonly Label reviewSourceSecondary = ReviewMetricSecondary();
    private readonly Label reviewCustomersPrimary = ReviewMetricPrimary();
    private readonly Label reviewCustomersSecondary = ReviewMetricSecondary();
    private readonly Label reviewExistingPrimary = ReviewMetricPrimary();
    private readonly Label reviewExistingSecondary = ReviewMetricSecondary();
    private readonly Label reviewNewPrimary = ReviewMetricPrimary();
    private readonly Label reviewNewSecondary = ReviewMetricSecondary();
    private readonly Label reviewContainersPrimary = ReviewMetricPrimary();
    private readonly Label reviewContainersSecondary = ReviewMetricSecondary();
    private readonly Label reviewReconciliationPrimary = ReviewMetricPrimary();
    private readonly Label reviewReconciliationSecondary = ReviewMetricSecondary();

    private readonly Label mappingSummary = new()
    {
        AutoSize = true,
        ForeColor = Color.FromArgb(25, 95, 190)
    };

    private ExcelImportAnalysis? analysis;
    private readonly Dictionary<string, ImportWorksheetMapping> mappingState =
        new(StringComparer.OrdinalIgnoreCase);
    
    private readonly Dictionary<string, int> containerTokenMappings =
        new(StringComparer.OrdinalIgnoreCase);

    private readonly Dictionary<string, ImportCustomerDecision> customerDecisions =
        new(StringComparer.OrdinalIgnoreCase);

    private readonly Dictionary<string, ImportExistingCustomerDecision> existingCustomerDecisions =
        new(StringComparer.OrdinalIgnoreCase);

    private int currentStep = 1;
    private string? step4ExpectedSha256;

    public ExcelImportForm(
        IExcelImportService service,
        ICustomerService customerService,
        IContainerTypeService containerTypeService,
        IBalanceService balanceService,
        IImportExecutionService importExecutionService)
    {
        this.service = service;
        this.customerService = customerService;
        this.containerTypeService = containerTypeService;
        this.balanceService = balanceService;
        this.importExecutionService = importExecutionService;

        Text = "Excel Import Wizard";
        StartPosition = FormStartPosition.CenterParent;
        AutoScaleMode = AutoScaleMode.Dpi;
        ClientSize = new Size(1540, 930);
        MinimumSize = new Size(1420, 850);
        BackColor = Color.FromArgb(245, 247, 250);
        Font = new Font("Segoe UI", 10F);
        FormBorderStyle = FormBorderStyle.Sizable;

        nextButton.Enabled = false;
        backButton.Visible = false;

        analyseButton.Click += async (_, _) => await AnalyseAsync();
        nextButton.Click += async (_, _) => await NextAsync();
        backButton.Click += (_, _) => Back();
        cancelButton.Click += (_, _) => Close();
        viewDuplicatesButton.Click += (_, _) => ViewDuplicates();

        BuildShell();
        ShowAnalysePage();
    }

    private void BuildShell()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Padding = new Padding(12),
            BackColor = Color.FromArgb(245, 247, 250)
        };

        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var progressCard = Card(new Padding(12, 4, 12, 2));
        progressCard.Controls.Add(progress);

        root.Controls.Add(progressCard, 0, 0);
        root.Controls.Add(pageHost, 0, 1);
        root.Controls.Add(BuildFooter(), 0, 2);

        Controls.Add(root);
    }

    private Control BuildFooter()
    {
        var footer = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            ColumnCount = 2,
            Margin = Padding.Empty,
            Padding = new Padding(0, 8, 0, 0)
        };
        footer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        footer.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        var left = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Margin = Padding.Empty
        };
        left.Controls.Add(backButton);

        var right = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            Margin = Padding.Empty
        };
        right.Controls.Add(cancelButton);
        right.Controls.Add(nextButton);

        footer.Controls.Add(left, 0, 0);
        footer.Controls.Add(right, 1, 0);

        return footer;
    }

    private void ShowAnalysePage()
    {
        currentStep = 1;
        progress.ActiveStep = 1;
        progress.Invalidate();

        backButton.Visible = false;
        nextButton.Text = "Next >";
        nextButton.Enabled = analysis is not null;

        pageHost.Controls.Clear();

        var page = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Padding = Padding.Empty,
            Margin = Padding.Empty
        };
        page.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        page.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        page.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

        page.Controls.Add(BuildAnalyseHeader(), 0, 0);
        page.Controls.Add(BuildWorkbookPicker(), 0, 1);
        page.Controls.Add(BuildAnalyseResult(), 0, 2);

        pageHost.Controls.Add(page);
    }

    private Control BuildAnalyseHeader()
    {
        var card = Card(new Padding(20, 16, 20, 16));

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            ColumnCount = 1,
            RowCount = 2
        };

        layout.Controls.Add(new Label
        {
            Text = "Analyse workbook",
            AutoSize = true,
            Font = new Font("Segoe UI Semibold", 17F, FontStyle.Bold),
            Margin = new Padding(0, 0, 0, 3)
        }, 0, 0);

        layout.Controls.Add(new Label
        {
            Text =
                "Choose the legacy Excel workbook and let BinTracker inspect it. " +
                "Nothing is imported during this step.",
            AutoSize = true,
            ForeColor = Color.DimGray,
            MaximumSize = new Size(1260, 0)
        }, 0, 1);

        card.Controls.Add(layout);
        return card;
    }

    private Control BuildWorkbookPicker()
    {
        var card = Card(new Padding(20, 16, 20, 16));

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            ColumnCount = 1,
            RowCount = 2
        };

        layout.Controls.Add(SectionHeading("Select Excel workbook"), 0, 0);

        var row = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            ColumnCount = 3,
            Margin = new Padding(0, 8, 0, 0)
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
        row.Controls.Add(analyseButton, 2, 0);

        layout.Controls.Add(row, 0, 1);
        card.Controls.Add(layout);

        return card;
    }

    private Control BuildAnalyseResult()
    {
        var card = Card(new Padding(20, 18, 20, 18));
        card.Dock = DockStyle.Fill;

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            ColumnCount = 1,
            RowCount = 5
        };

        layout.Controls.Add(SectionHeading("Analysis result"), 0, 0);
        layout.Controls.Add(analyseResultTitle, 0, 1);
        layout.Controls.Add(analyseResultDetails, 0, 2);

        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true,
            Margin = new Padding(0, 12, 0, 0)
        };

        var viewAll = SecondaryButton("View all worksheets", 180);
        viewAll.Click += (_, _) => ViewAllWorksheets();

        buttons.Controls.Add(viewAll);
        buttons.Controls.Add(viewDuplicatesButton);

        layout.Controls.Add(buttons, 0, 3);

        analyseWarningPanel.Controls.Clear();

        var analyseWarningLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            ColumnCount = 2,
            RowCount = 1,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        analyseWarningLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        analyseWarningLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

        analyseWarningLayout.Controls.Add(new Label
        {
            Text = "⚠",
            AutoSize = true,
            ForeColor = Color.FromArgb(150, 95, 0),
            Margin = new Padding(0, 0, 6, 0)
        }, 0, 0);

        analyseWarningText.Margin = Padding.Empty;
        analyseWarningText.MaximumSize = new Size(1120, 0);
        analyseWarningLayout.Controls.Add(analyseWarningText, 1, 0);

        analyseWarningPanel.Controls.Add(analyseWarningLayout);
        layout.Controls.Add(analyseWarningPanel, 0, 4);

        // The enabled Next button is the continuation cue. Keeping this
        // result card concise prevents the last explanatory line from being
        // squeezed behind the fixed wizard footer at scaled DPI.
        card.Controls.Add(layout);
        return card;
    }

    private void ShowMapPage()
    {
        if (analysis is null)
            return;

        currentStep = 2;
        progress.ActiveStep = 2;
        progress.Invalidate();

        backButton.Visible = true;
        nextButton.Text = "Review >";
        nextButton.Enabled = true;

        pageHost.Controls.Clear();

        ConfigureMappingGrid();
        ConfigureCandidateGrid();

        PopulateMappingDefaults();
        RefreshMappedCandidates();

        var page = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Margin = Padding.Empty
        };

        page.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        page.RowStyles.Add(new RowStyle(SizeType.Percent, 52F));
        page.RowStyles.Add(new RowStyle(SizeType.Percent, 48F));

        page.Controls.Add(BuildMapHeader(), 0, 0);
        page.Controls.Add(BuildWorksheetMappingSection(), 0, 1);
        page.Controls.Add(BuildMappedCandidatesSection(), 0, 2);

        pageHost.Controls.Add(page);
    }

    private Control BuildMapHeader()
    {
        var card = Card(new Padding(18, 9, 18, 9));

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            ColumnCount = 1,
            RowCount = 2
        };

        layout.Controls.Add(new Label
        {
            Text = "Map workbook data",
            AutoSize = true,
            Font = new Font("Segoe UI Semibold", 18F, FontStyle.Bold),
            Margin = new Padding(0, 0, 0, 5)
        }, 0, 0);

        layout.Controls.Add(new Label
        {
            Text =
                "Classify each worksheet. Only Source sheets feed customer/import data. " +
                "Validation and Report sheets remain available for checking but will not create duplicate records.",
            AutoSize = true,
            ForeColor = Color.DimGray,
            MaximumSize = new Size(1260, 0)
        }, 0, 1);

        card.Controls.Add(layout);
        return card;
    }

    private Control BuildWorksheetMappingSection()
    {
        var card = Card(new Padding(18, 12, 18, 12));
        card.Dock = DockStyle.Fill;

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3
        };
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        layout.Controls.Add(SectionHeading("Worksheet classification"), 0, 0);

        mappingGrid.Dock = DockStyle.Fill;
        mappingGrid.Margin = new Padding(0, 8, 0, 0);
        layout.Controls.Add(mappingGrid, 0, 1);

        layout.Controls.Add(mappingSummary, 0, 2);

        card.Controls.Add(layout);
        return card;
    }

    private Control BuildMappedCandidatesSection()
    {
        var card = Card(new Padding(18, 12, 18, 12));
        card.Dock = DockStyle.Fill;

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3
        };
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

        layout.Controls.Add(SectionHeading("Customer candidates from Source sheets"), 0, 0);

        layout.Controls.Add(new Label
        {
            Text =
                "This preview automatically updates when you change a worksheet classification.",
            AutoSize = true,
            ForeColor = Color.DimGray,
            Margin = new Padding(0, 4, 0, 6)
        }, 0, 1);

        candidateGrid.Dock = DockStyle.Fill;
        layout.Controls.Add(candidateGrid, 0, 2);

        card.Controls.Add(layout);
        return card;
    }

    private void ConfigureMappingGrid()
    {
        if (mappingGrid.Columns.Count > 0)
            return;

        mappingGrid.ReadOnly = false;
        mappingGrid.EditMode = DataGridViewEditMode.EditOnEnter;
        mappingGrid.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;
        mappingGrid.DefaultCellStyle.WrapMode = DataGridViewTriState.False;

        mappingGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "Worksheet",
            HeaderText = "Worksheet",
            ReadOnly = true,
            Width = 205
        });

        mappingGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "Rows",
            HeaderText = "Rows",
            ReadOnly = true,
            Width = 65
        });

        mappingGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "Candidates",
            HeaderText = "Candidates",
            ReadOnly = true,
            Width = 125
        });

        var roleColumn = new DataGridViewComboBoxColumn
        {
            Name = "Role",
            HeaderText = "Classification",
            Width = 140,
            ValueType = typeof(ImportWorksheetRole),
            DisplayStyle = DataGridViewComboBoxDisplayStyle.DropDownButton,
            FlatStyle = FlatStyle.Standard
        };
        roleColumn.Items.AddRange(
            ImportWorksheetRole.Source,
            ImportWorksheetRole.Validation,
            ImportWorksheetRole.Report,
            ImportWorksheetRole.Ignore);
        mappingGrid.Columns.Add(roleColumn);

        var reasonColumn = new DataGridViewTextBoxColumn
        {
            Name = "Reason",
            HeaderText = "Suggested reason",
            ReadOnly = true,
            AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
        };
        reasonColumn.DefaultCellStyle.WrapMode = DataGridViewTriState.True;
        mappingGrid.Columns.Add(reasonColumn);

        mappingGrid.CurrentCellDirtyStateChanged += (_, _) =>
        {
            if (mappingGrid.IsCurrentCellDirty)
                mappingGrid.CommitEdit(DataGridViewDataErrorContexts.Commit);
        };

        mappingGrid.CellValueChanged += (_, e) =>
        {
            if (e.RowIndex >= 0 &&
                mappingGrid.Columns[e.ColumnIndex].Name == "Role")
            {
                SaveMappingState();
                RefreshMappedCandidates();
            }
        };

        mappingGrid.DataError += (_, _) => { };
    }

    private void ConfigureCandidateGrid()
    {
        if (candidateGrid.Columns.Count > 0)
            return;

        candidateGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Worksheet",
            Width = 190
        });
        candidateGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Customer / Buyer",
            AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
        });
        candidateGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Detected Type",
            Width = 140
        });
        candidateGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Source Cell",
            Width = 110
        });
    }

    private void PopulateMappingDefaults()
    {
        if (analysis is null)
            return;

        mappingGrid.Rows.Clear();

        foreach (var sheet in analysis.Worksheets)
        {
            var mapping = mappingState.TryGetValue(sheet.Name, out var saved)
                ? saved
                : SuggestRole(sheet.Name);

            mappingState[sheet.Name] = mapping;

            var rowIndex = mappingGrid.Rows.Add(
                sheet.Name,
                sheet.UsedRows,
                sheet.BuyerCandidates,
                mapping.Role,
                mapping.Reason);

            // Explicitly set the enum value after the row exists. This prevents
            // DataGridViewComboBoxCell display binding from falling back to a
            // blank display when the Map page is reconstructed after Back.
            mappingGrid.Rows[rowIndex].Cells["Role"].Value = mapping.Role;
        }
    }

    private static ImportWorksheetMapping SuggestRole(string worksheet)
    {
        if (worksheet.Equals("Update Account", StringComparison.OrdinalIgnoreCase))
            return new(worksheet, ImportWorksheetRole.Source, "Primary Account source sheet.");

        if (worksheet.Equals("Update Cash", StringComparison.OrdinalIgnoreCase))
            return new(worksheet, ImportWorksheetRole.Source, "Primary Cash/COD source sheet.");

        if (worksheet.Equals("CREDITS", StringComparison.OrdinalIgnoreCase))
            return new(worksheet, ImportWorksheetRole.Validation, "Derived credit list; validation only.");

        if (worksheet.Equals("Print This", StringComparison.OrdinalIgnoreCase))
            return new(worksheet, ImportWorksheetRole.Report, "Front-side floor report; report only.");

        if (worksheet.Equals("Print this on reverse side", StringComparison.OrdinalIgnoreCase))
            return new(worksheet, ImportWorksheetRole.Report, "Reverse-side daily report; report only.");

        if (worksheet.Equals("Summary", StringComparison.OrdinalIgnoreCase))
            return new(worksheet, ImportWorksheetRole.Ignore, "Derived summary; ignore for import.");

        if (worksheet.Contains("check", StringComparison.OrdinalIgnoreCase))
            return new(worksheet, ImportWorksheetRole.Validation, "Checking worksheet; validation only.");

        return new(worksheet, ImportWorksheetRole.Ignore, "Unknown sheet — review classification.");
    }

    private void SaveMappingState()
    {
        if (mappingGrid.Columns.Count == 0)
            return;

        foreach (DataGridViewRow row in mappingGrid.Rows)
        {
            if (row.Cells["Worksheet"].Value is not string worksheet ||
                string.IsNullOrWhiteSpace(worksheet))
            {
                continue;
            }

            var role = row.Cells["Role"].Value is ImportWorksheetRole typedRole
                ? typedRole
                : Enum.TryParse<ImportWorksheetRole>(
                    row.Cells["Role"].Value?.ToString(),
                    out var parsed)
                    ? parsed
                    : ImportWorksheetRole.Ignore;

            mappingState[worksheet] = new ImportWorksheetMapping(
                worksheet,
                role,
                row.Cells["Reason"].Value?.ToString() ?? string.Empty);
        }
    }

    private HashSet<string> SourceSheets()
    {
        SaveMappingState();

        return mappingState.Values
            .Where(x => x.Role == ImportWorksheetRole.Source)
            .Select(x => x.Worksheet)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    private void RefreshMappedCandidates()
    {
        if (analysis is null || mappingGrid.Columns.Count == 0)
            return;

        var sourceSheets = SourceSheets();
        var candidates = analysis.CustomerCandidates
            .Where(x => sourceSheets.Contains(x.Worksheet))
            .ToList();

        candidateGrid.Rows.Clear();

        foreach (var customer in candidates.Take(200))
        {
            candidateGrid.Rows.Add(
                customer.Worksheet,
                customer.CustomerCode,
                customer.CustomerType,
                customer.SourceCell);
        }

        var unique = candidates
            .Select(x => x.CustomerCode.Trim())
            .Where(x => x.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count();

        mappingSummary.Text =
            $"Source sheets: {sourceSheets.Count:N0}    " +
            $"Unique customers: {unique:N0}    " +
            $"Source occurrences: {candidates.Count:N0}" +
            (candidates.Count > 200 ? "    (preview shows first 200)" : string.Empty);
    }

    private IReadOnlyList<ImportWorksheetMapping> CurrentMappings()
    {
        SaveMappingState();

        return mappingState.Values
            .OrderBy(x => x.Worksheet, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private async Task ShowReviewPageAsync()
    {
        if (analysis is null)
            return;

        currentStep = 3;
        progress.ActiveStep = 3;
        progress.Invalidate();

        backButton.Visible = true;
        nextButton.Text = "Import >";

        pageHost.Controls.Clear();

        var existing = await customerService.SearchAsync(
            query: null,
            includeInactive: true);

        var containerTypes = await containerTypeService.SearchAsync(
            search: null,
            includeInactive: true);

        var mappings = CurrentMappings();

        var plan = ExcelImportReviewPlanner.Build(
            analysis,
            mappings,
            existing);

        var mergedDecisions = ImportCustomerDecisionPlanner.MergeDefaults(plan, customerDecisions);
        customerDecisions.Clear();
        foreach (var pair in mergedDecisions)
            customerDecisions[pair.Key] = pair.Value;

        var mergedExistingDecisions =
            ImportExistingCustomerDecisionPlanner.MergeDefaults(
                plan,
                existingCustomerDecisions);
        existingCustomerDecisions.Clear();
        foreach (var pair in mergedExistingDecisions)
            existingCustomerDecisions[pair.Key] = pair.Value;

        var currentBalances = await balanceService.GetBalancesAsync();

        var reconciliation = ImportBalanceReconciliationPlanner.Build(
            analysis,
            mappings,
            plan,
            containerTypes,
            currentBalances,
            containerTokenMappings,
            customerDecisions,
            existingCustomerDecisions);

        ConfigureReviewGrid();
        ConfigureReconciliationGrid();
        reviewGrid.Rows.Clear();

        foreach (var customer in plan.Customers)
        {
            var resolvedContainers = ResolveContainerHints(
                customer.ContainerHints,
                containerTypes,
                containerTokenMappings);

            var displayExistingName = customer.ExistingCustomerName;
            if (existingCustomerDecisions.TryGetValue(customer.CustomerCode, out var existingDecision) &&
                existingDecision.Action != ImportExistingCustomerDecisionAction.Unconfirmed &&
                !string.IsNullOrWhiteSpace(existingDecision.CustomerName))
            {
                displayExistingName = existingDecision.CustomerName;
            }

            var rowIndex = reviewGrid.Rows.Add(
                customer.CustomerCode,
                customer.DetectedType,
                resolvedContainers,
                displayExistingName,
                customer.ExistingCustomerType?.ToString() ?? string.Empty,
                ReviewStatusText(customer.Status),
                MatchReasonText(customer),
                customer.SourceWorksheets);

            if (!string.IsNullOrWhiteSpace(customer.LegacyVariants))
            {
                var tooltip =
                    $"Legacy workbook variant(s): {customer.LegacyVariants}";

                foreach (DataGridViewCell cell in reviewGrid.Rows[rowIndex].Cells)
                    cell.ToolTipText = tooltip;
            }
        }

        reconciliationGrid.Rows.Clear();

        foreach (var item in reconciliation.Rows)
        {
            reconciliationGrid.Rows.Add(
                item.CustomerCode,
                item.Container,
                item.CurrentBinTrackerBalance,
                item.ExcelBroughtForward?.ToString() ?? "—",
                item.ExcelOut,
                item.ExcelIn,
                item.ExcelTarget?.ToString() ?? "—",
                item.OpeningAdjustment?.ToString("+0;-0;0") ?? "—",
                item.ProjectedBalance?.ToString() ?? "—",
                ReconciliationStatusText(item.Status),
                item.ContainerReason);
        }

        var newCreateCount = ImportCustomerDecisionPlanner.CreateCount(customerDecisions);
        var newSkipCount = ImportCustomerDecisionPlanner.SkipCount(customerDecisions);
        var newUnconfirmedCount = ImportCustomerDecisionPlanner.UnconfirmedCount(customerDecisions);
        var existingConfirmedCount = ImportExistingCustomerDecisionPlanner.ConfirmedCount(existingCustomerDecisions);
        var existingUnconfirmedCount = ImportExistingCustomerDecisionPlanner.UnconfirmedCount(existingCustomerDecisions);

        reviewSourcePrimary.Text = $"{plan.SourceSheetCount:N0} sheets";
        reviewSourceSecondary.Text =
            $"{plan.SnapshotRowCount:N0} balance rows";

        reviewCustomersPrimary.Text =
            $"{plan.UniqueCustomerCount:N0} customers";
        reviewCustomersSecondary.Text =
            $"{plan.SnapshotTotalMismatchCount:N0} formula issues";

        reviewExistingPrimary.Text =
            $"{existingConfirmedCount:N0} confirmed";
        reviewExistingSecondary.Text =
            $"{existingUnconfirmedCount:N0} unconfirmed";

        reviewNewPrimary.Text =
            newUnconfirmedCount == 0
                ? $"{newCreateCount:N0} created"
                : $"{newUnconfirmedCount:N0} pending";
        reviewNewSecondary.Text =
            $"{newSkipCount:N0} skipped";

        reviewContainersPrimary.Text =
            $"{reconciliation.UnresolvedContainerCount:N0} to map";
        reviewContainersSecondary.Text =
            $"{containerTokenMappings.Count:N0} manual mappings";

        var reconciliationIssueCount =
            reconciliation.Rows.Count - reconciliation.ReadyCount;
        reviewReconciliationPrimary.Text =
            $"{reconciliation.ReadyCount:N0} ready";
        reviewReconciliationSecondary.Text =
            $"{reconciliationIssueCount:N0} issues";

        var blockers = new List<string>();

        if (plan.TypeMismatchCount > 0)
            blockers.Add($"{plan.TypeMismatchCount:N0} customer type mismatch(es)");

        if (plan.SourceConflictCount > 0)
            blockers.Add($"{plan.SourceConflictCount:N0} Source-sheet conflict(s)");

        if (plan.SnapshotTotalMismatchCount > 0)
            blockers.Add($"{plan.SnapshotTotalMismatchCount:N0} B/Fwd/OUT/IN total mismatch(es)");

        var unconfirmedCustomers = ImportCustomerDecisionPlanner.UnconfirmedCount(customerDecisions);
        if (unconfirmedCustomers > 0)
            blockers.Add($"{unconfirmedCustomers:N0} new customer(s) still need an explicit Create or Skip decision");

        var unconfirmedExistingMatches =
            ImportExistingCustomerDecisionPlanner.UnconfirmedCount(existingCustomerDecisions);
        if (unconfirmedExistingMatches > 0)
            blockers.Add($"{unconfirmedExistingMatches:N0} existing customer match(es) still need confirmation");

        if (reconciliation.UnresolvedContainerCount > 0)
            blockers.Add(
                $"{reconciliation.UnresolvedContainerCount:N0} balance row(s) still need container mapping (unknown or unconfigured token)");

        if (reconciliation.UnresolvedCustomerCount > 0)
            blockers.Add(
                $"{reconciliation.UnresolvedCustomerCount:N0} balance row(s) have unresolved customer matching");

        if (reconciliation.MissingBroughtForwardCount > 0)
            blockers.Add(
                $"{reconciliation.MissingBroughtForwardCount:N0} balance row(s) have no B/Fwd");

        if (reconciliation.ExcelMismatchCount > 0)
            blockers.Add(
                $"{reconciliation.ExcelMismatchCount:N0} balance row(s) fail Excel Total reconciliation");

        nextButton.Enabled = ImportReviewReadiness.CanAdvanceToImport(blockers.Count, reconciliation);

        reviewWarning.Text = blockers.Count == 0
            ? "No customer-code/type conflicts detected. Excel balances are treated as cutover targets, not amounts to add. " +
              "When every Review item is resolved, continue to Step 4 for import preflight and transactional import."
            : "⚠ Review required: " + string.Join("; ", blockers) +
              ". Resolve these items before continuing to Step 4.";

        var page = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Margin = Padding.Empty
        };

        page.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        page.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        page.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

        page.Controls.Add(BuildReviewHeader(), 0, 0);
        page.Controls.Add(
            BuildReviewSummaryCard(
                newUnconfirmedCount,
                existingUnconfirmedCount,
                reconciliation.UnresolvedContainerCount),
            0,
            1);

        var reviewSection = BuildReviewCustomerSection();
        reviewSection.Dock = DockStyle.Fill;
        page.Controls.Add(reviewSection, 0, 2);

        pageHost.Controls.Add(page);
    }

    private async Task ConfirmExistingMatchesAsync()
    {
        if (analysis is null) return;

        var existing = await customerService.SearchAsync(null, true);
        var plan = ExcelImportReviewPlanner.Build(
            analysis,
            CurrentMappings(),
            existing);

        var merged = ImportExistingCustomerDecisionPlanner.MergeDefaults(
            plan,
            existingCustomerDecisions);

        using var form = new ExistingCustomerMatchForm(plan, existing, merged);
        if (form.ShowDialog(this) != DialogResult.OK) return;

        existingCustomerDecisions.Clear();
        foreach (var pair in form.Decisions)
            existingCustomerDecisions[pair.Key] = pair.Value;

        await ShowReviewPageAsync();
    }

    private async Task ConfirmNewCustomersAsync()
    {
        if (analysis is null) return;
        var existing = await customerService.SearchAsync(null, true);
        var plan = ExcelImportReviewPlanner.Build(analysis, CurrentMappings(), existing);
        var merged = ImportCustomerDecisionPlanner.MergeDefaults(plan, customerDecisions);
        using var form = new CustomerDecisionForm(plan, merged);
        if (form.ShowDialog(this) != DialogResult.OK) return;
        customerDecisions.Clear();
        foreach (var pair in form.Decisions) customerDecisions[pair.Key] = pair.Value;
        await ShowReviewPageAsync();
    }

    private async Task MapContainerTokensAsync()
    {
        if (analysis is null) return;

        var containerTypes = await containerTypeService.SearchAsync(null, true);
        var existing = await customerService.SearchAsync(null, true);
        var mappings = CurrentMappings();
        var customerPlan = ExcelImportReviewPlanner.Build(analysis, mappings, existing);
        var currentBalances = await balanceService.GetBalancesAsync();

        var reconciliation = ImportBalanceReconciliationPlanner.Build(
            analysis, mappings, customerPlan, containerTypes, currentBalances, containerTokenMappings, customerDecisions, existingCustomerDecisions);

        var tokens = reconciliation.Rows
            .Where(x => x.Status == ImportBalanceReconciliationStatus.UnresolvedContainer &&
                        !string.IsNullOrWhiteSpace(x.ContainerToken))
            .Select(x => x.ContainerToken)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (tokens.Count == 0)
        {
            MessageBox.Show(this, "There are no unresolved explicit container tokens to map.",
                "Container Mapping", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        using var form = new ContainerTokenMappingForm(
            containerTypeService, tokens, containerTokenMappings);

        if (form.ShowDialog(this) != DialogResult.OK) return;

        containerTokenMappings.Clear();
        foreach (var pair in form.Mappings) containerTokenMappings[pair.Key] = pair.Value;

        await ShowReviewPageAsync();
    }

    private static string ResolveContainerHints(
        string commaSeparatedHints,
        IReadOnlyCollection<ContainerTypeListRow> containerTypes,
        IReadOnlyDictionary<string, int> containerTokenMappings)
    {
        if (string.IsNullOrWhiteSpace(commaSeparatedHints))
            return string.Empty;

        return string.Join(
            ", ",
            commaSeparatedHints
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(hint => LegacyContainerHintResolver.Resolve(
                    hint,
                    containerTypes,
                    containerTokenMappings).DisplayName)
                .Distinct(StringComparer.OrdinalIgnoreCase));
    }

    private static string MatchReasonText(ImportCustomerReviewRow customer)
    {
        if (customer.Status == ImportCustomerReviewStatus.New)
            return "No confident match";

        if (customer.Status == ImportCustomerReviewStatus.SourceConflict)
            return "Source conflict";

        return customer.MatchKind switch
        {
            CustomerMatchKind.ExactCode => "Exact code",
            CustomerMatchKind.CaseInsensitiveCode => "Code / case",
            CustomerMatchKind.NormalizedCode => "Normalized code",
            CustomerMatchKind.NormalizedName => "Normalized name",
            _ => customer.MatchReason
        };
    }

    private Control BuildReviewHeader()
    {
        var card = Card(new Padding(18, 8, 18, 8));

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            ColumnCount = 1,
            RowCount = 2
        };

        layout.Controls.Add(new Label
        {
            Text = "Review proposed import",
            AutoSize = true,
            Font = new Font("Segoe UI Semibold", 18F, FontStyle.Bold),
            Margin = new Padding(0, 0, 0, 5)
        }, 0, 0);

        layout.Controls.Add(new Label
        {
            Text =
                "This page compares Source-sheet customers with the current BinTracker database. " +
                "Nothing is written until the separate Import stage is enabled.",
            AutoSize = true,
            ForeColor = Color.DimGray,
            MaximumSize = new Size(1260, 0)
        }, 0, 1);

        card.Controls.Add(layout);
        return card;
    }

    private Control BuildReviewSummaryCard(int newPending, int existingPending, int containersPending)
    {
        var card = Card(new Padding(12, 7, 12, 7));

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            ColumnCount = 1,
            RowCount = 3,
            Margin = Padding.Empty
        };

        var metrics = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = false,
            Height = 142,
            ColumnCount = 6,
            RowCount = 1,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };

        for (var i = 0; i < 6; i++)
            metrics.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 16.6667F));

        metrics.Controls.Add(
            ReviewMetricCard(ReviewIconKind.Database, "Source", reviewSourcePrimary, reviewSourceSecondary), 0, 0);
        metrics.Controls.Add(
            ReviewMetricCard(ReviewIconKind.People, "Customers", reviewCustomersPrimary, reviewCustomersSecondary), 1, 0);
        metrics.Controls.Add(
            ReviewMetricCard(ReviewIconKind.CheckCircle, "Existing matches", reviewExistingPrimary, reviewExistingSecondary), 2, 0);
        metrics.Controls.Add(
            ReviewMetricCard(ReviewIconKind.PersonPlus, "New candidates", reviewNewPrimary, reviewNewSecondary), 3, 0);
        metrics.Controls.Add(
            ReviewMetricCard(ReviewIconKind.Container, "Containers", reviewContainersPrimary, reviewContainersSecondary), 4, 0);
        metrics.Controls.Add(
            ReviewMetricCard(ReviewIconKind.Scales, "Reconciliation", reviewReconciliationPrimary, reviewReconciliationSecondary), 5, 0);

        root.Controls.Add(metrics, 0, 0);

        reviewWarning.Margin = new Padding(2, 4, 0, 3);
        root.Controls.Add(reviewWarning, 0, 1);

        var actionRow = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            MinimumSize = new Size(0, 46),
            ColumnCount = 2,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        actionRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        actionRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        var actions = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };

        var confirmCustomers = ReviewActionButton(
            $"Confirm new ({newPending:N0})",
            ReviewIconKind.PersonPlus,
            245);
        confirmCustomers.Click += async (_, _) => await ConfirmNewCustomersAsync();

        var confirmMatches = ReviewActionButton(
            $"Confirm existing ({existingPending:N0})",
            ReviewIconKind.CheckCircle,
            260);
        confirmMatches.Click += async (_, _) => await ConfirmExistingMatchesAsync();

        var mapContainers = ReviewActionButton(
            $"Map container ({containersPending:N0})",
            ReviewIconKind.Container,
            260);
        mapContainers.Click += async (_, _) => await MapContainerTokensAsync();

        actions.Controls.Add(confirmCustomers);
        actions.Controls.Add(confirmMatches);
        actions.Controls.Add(mapContainers);

        var larger = ReviewActionButton(
            "Open reconciliation",
            ReviewIconKind.Expand,
            250);
        larger.Margin = Padding.Empty;
        larger.Click += (_, _) => ShowReconciliationLarge();

        actionRow.Controls.Add(actions, 0, 0);
        actionRow.Controls.Add(larger, 1, 0);
        root.Controls.Add(actionRow, 0, 2);

        card.Controls.Add(root);
        return card;
    }

    private static Label ReviewMetricPrimary() => new()
    {
        AutoSize = true,
        Font = new Font("Segoe UI Semibold", 10.5F, FontStyle.Bold),
        ForeColor = Color.FromArgb(20, 28, 40),
        MaximumSize = new Size(195, 0),
        Margin = Padding.Empty
    };

    private static Label ReviewMetricSecondary() => new()
    {
        AutoSize = true,
        Font = new Font("Segoe UI", 9F),
        ForeColor = Color.FromArgb(105, 110, 120),
        MaximumSize = new Size(195, 0),
        Margin = Padding.Empty
    };

    private static Control ReviewMetricCard(
        ReviewIconKind iconKind,
        string title,
        Label primary,
        Label secondary)
    {
        var panel = new Panel
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(5, 4, 5, 4),
            Padding = new Padding(12, 10, 10, 10),
            BackColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle
        };

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 3,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 52F));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

        var iconBox = new PictureBox
        {
            Image = LoadReviewIcon(iconKind, new Size(36, 36)),
            SizeMode = PictureBoxSizeMode.CenterImage,
            Dock = DockStyle.Fill,
            Margin = Padding.Empty,
            TabStop = false
        };

        layout.Controls.Add(iconBox, 0, 0);
        layout.SetRowSpan(iconBox, 3);

        layout.Controls.Add(new Label
        {
            Text = title,
            AutoSize = true,
            Font = new Font("Segoe UI", 9F),
            ForeColor = Color.FromArgb(48, 55, 70),
            Margin = new Padding(4, 0, 0, 1)
        }, 1, 0);

        primary.Margin = new Padding(4, 0, 0, 4);
        secondary.Margin = new Padding(4, 0, 0, 0);

        layout.Controls.Add(primary, 1, 1);
        layout.Controls.Add(secondary, 1, 2);

        panel.Controls.Add(layout);
        return panel;
    }

    private static Button ReviewActionButton(
        string text,
        ReviewIconKind iconKind,
        int width)
    {
        var button = SecondaryButton(text, width);
        button.Image = LoadReviewIcon(
            iconKind,
            iconKind == ReviewIconKind.Container
                ? new Size(14, 14)
                : new Size(16, 16));
        button.ImageAlign = ContentAlignment.MiddleLeft;
        button.TextAlign = ContentAlignment.MiddleCenter;
        button.UseVisualStyleBackColor = true;
        button.TextImageRelation = TextImageRelation.ImageBeforeText;
        button.Padding = new Padding(16, 0, 14, 0);
        button.AutoSize = false;
        button.Height = 44;
        return button;
    }

    private enum ReviewIconKind
    {
        Database,
        People,
        CheckCircle,
        PersonPlus,
        Container,
        Scales,
        Expand
    }

    private static Bitmap LoadReviewIcon(
        ReviewIconKind kind,
        Size size)
    {
        var fileName = kind switch
        {
            ReviewIconKind.Database => "review-source.png",
            ReviewIconKind.People => "review-customers.png",
            ReviewIconKind.CheckCircle => "review-existing.png",
            ReviewIconKind.PersonPlus => "review-new.png",
            ReviewIconKind.Container => "review-container.png",
            ReviewIconKind.Scales => "review-reconciliation.png",
            ReviewIconKind.Expand => "review-expand.png",
            _ => throw new ArgumentOutOfRangeException(nameof(kind))
        };

        var assembly = typeof(ExcelImportForm).Assembly;
        var resourceName = assembly
            .GetManifestResourceNames()
            .FirstOrDefault(x =>
                x.EndsWith(
                    $".Assets.{fileName}",
                    StringComparison.OrdinalIgnoreCase));

        if (resourceName is null)
            throw new InvalidOperationException(
                $"Embedded Review icon '{fileName}' was not found.");

        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException(
                $"Embedded Review icon '{fileName}' could not be opened.");
        using var original = Image.FromStream(stream);

        // Preserve transparent breathing room around approved raster artwork.
        // Stretching the source edge-to-edge can crop tall icons such as the bin.
        var canvas = new Bitmap(size.Width, size.Height);
        using var g = Graphics.FromImage(canvas);
        g.Clear(Color.Transparent);
        g.InterpolationMode =
            System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
        g.PixelOffsetMode =
            System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;

        var inset = Math.Max(2, Math.Min(size.Width, size.Height) / 10);
        var target = new Rectangle(
            inset,
            inset,
            Math.Max(1, size.Width - (inset * 2)),
            Math.Max(1, size.Height - (inset * 2)));

        g.DrawImage(original, target);
        return canvas;
    }

    private Control BuildReviewCustomerSection()
    {
        var card = Card(new Padding(4, 3, 4, 4));
        card.AutoSize = false;
        card.AutoSizeMode = AutoSizeMode.GrowOnly;
        card.Dock = DockStyle.Fill;
        card.Margin = Padding.Empty;

        var tabs = new TabControl
        {
            Dock = DockStyle.Fill,
            Font = Font,
            MinimumSize = new Size(0, 420),
            Margin = Padding.Empty
        };

        var customerTab = new TabPage("Customer matches")
        {
            BackColor = Color.White,
            Padding = new Padding(4)
        };

        reviewGrid.Dock = DockStyle.Fill;
        reviewGrid.MinimumSize = new Size(0, 360);
        customerTab.Controls.Add(reviewGrid);

        var balanceTab = new TabPage("Balance reconciliation")
        {
            BackColor = Color.White,
            Padding = new Padding(4)
        };

        var balanceLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        balanceLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        balanceLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

        var explanation = new Label
        {
            Text =
                "Excel is authoritative at cutover. Opening adjustment = B/Fwd - Current.  " +
                "Projected = Current + Opening adjustment + OUT - IN = Excel target.",
            AutoSize = true,
            ForeColor = Color.DimGray,
            MaximumSize = new Size(1260, 0),
            Margin = new Padding(0, 0, 0, 4)
        };

        balanceLayout.Controls.Add(explanation, 0, 0);

        reconciliationGrid.Dock = DockStyle.Fill;
        reconciliationGrid.MinimumSize = new Size(0, 360);
        balanceLayout.Controls.Add(reconciliationGrid, 0, 1);

        balanceTab.Controls.Add(balanceLayout);

        tabs.TabPages.Add(customerTab);
        tabs.TabPages.Add(balanceTab);

        card.Controls.Add(tabs);
        return card;
    }

    private void ShowReconciliationLarge()
    {
        using var form = new Form
        {
            Text = "Balance Reconciliation",
            StartPosition = FormStartPosition.CenterParent,
            AutoScaleMode = AutoScaleMode.Dpi,
            ClientSize = new Size(1380, 760),
            MinimumSize = new Size(1100, 620),
            BackColor = Color.White,
            Font = Font
        };

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Padding = new Padding(14)
        };
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        root.Controls.Add(new Label
        {
            Text =
                "Projected = Current + Opening adjustment + OUT - IN. " +
                "Opening adjustment = B/Fwd - Current. Projected must equal Excel target.",
            AutoSize = true,
            ForeColor = Color.DimGray,
            Margin = new Padding(0, 0, 0, 8)
        }, 0, 0);

        var grid = Grid();
        grid.ScrollBars = ScrollBars.Both;
        grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        grid.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;
        grid.DefaultCellStyle.WrapMode = DataGridViewTriState.True;
        grid.ColumnHeadersDefaultCellStyle.WrapMode = DataGridViewTriState.True;
        grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;

        foreach (DataGridViewColumn source in reconciliationGrid.Columns)
        {
            grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                HeaderText = source.HeaderText,
                FillWeight = source.FillWeight,
                MinimumWidth = source.MinimumWidth
            });
        }

        foreach (DataGridViewRow sourceRow in reconciliationGrid.Rows)
        {
            if (sourceRow.IsNewRow) continue;
            var values = sourceRow.Cells
                .Cast<DataGridViewCell>()
                .Select(x => x.Value)
                .ToArray();
            grid.Rows.Add(values);
        }

        root.Controls.Add(grid, 0, 1);

        var close = SecondaryButton("Close", 110);
        close.Anchor = AnchorStyles.Right;
        close.Click += (_, _) => form.Close();
        root.Controls.Add(close, 0, 2);

        form.Controls.Add(root);
        form.ShowDialog(this);
    }

    private void ConfigureReviewGrid()
    {
        if (reviewGrid.Columns.Count > 0)
            return;

        reviewGrid.ScrollBars = ScrollBars.Vertical;
        reviewGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        reviewGrid.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;
        reviewGrid.DefaultCellStyle.WrapMode = DataGridViewTriState.True;
        reviewGrid.ColumnHeadersDefaultCellStyle.WrapMode = DataGridViewTriState.True;
        reviewGrid.ColumnHeadersHeightSizeMode =
            DataGridViewColumnHeadersHeightSizeMode.AutoSize;

        reviewGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Customer / Code",
            FillWeight = 125,
            MinimumWidth = 150
        });
        reviewGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Type",
            FillWeight = 70,
            MinimumWidth = 82
        });
        reviewGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Container(s)",
            FillWeight = 120,
            MinimumWidth = 150
        });
        reviewGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Existing customer",
            FillWeight = 140,
            MinimumWidth = 175
        });
        reviewGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Existing type",
            FillWeight = 80,
            MinimumWidth = 100
        });
        reviewGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Review status",
            FillWeight = 120,
            MinimumWidth = 145
        });
        reviewGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Match reason",
            FillWeight = 110,
            MinimumWidth = 135
        });
        reviewGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Source worksheet",
            FillWeight = 110,
            MinimumWidth = 140
        });
    }

    private void ConfigureReconciliationGrid()
    {
        if (reconciliationGrid.Columns.Count > 0)
            return;

        reconciliationGrid.ScrollBars = ScrollBars.Vertical;
        reconciliationGrid.AutoSizeColumnsMode =
            DataGridViewAutoSizeColumnsMode.Fill;
        reconciliationGrid.AutoSizeRowsMode =
            DataGridViewAutoSizeRowsMode.AllCells;
        reconciliationGrid.DefaultCellStyle.WrapMode =
            DataGridViewTriState.True;
        reconciliationGrid.ColumnHeadersDefaultCellStyle.WrapMode =
            DataGridViewTriState.True;
        reconciliationGrid.ColumnHeadersHeightSizeMode =
            DataGridViewColumnHeadersHeightSizeMode.AutoSize;

        reconciliationGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Customer", FillWeight = 125, MinimumWidth = 140 });
        reconciliationGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Container", FillWeight = 78, MinimumWidth = 90 });
        reconciliationGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Current", FillWeight = 64, MinimumWidth = 72 });
        reconciliationGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "B/Fwd", FillWeight = 64, MinimumWidth = 72 });
        reconciliationGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "OUT", FillWeight = 52, MinimumWidth = 60 });
        reconciliationGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "IN", FillWeight = 52, MinimumWidth = 60 });
        reconciliationGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Excel target", FillWeight = 100, MinimumWidth = 120 });
        reconciliationGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Opening adjustment", FillWeight = 88, MinimumWidth = 105 });
        reconciliationGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Projected", FillWeight = 76, MinimumWidth = 90 });
        reconciliationGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Status", FillWeight = 105, MinimumWidth = 130 });
        reconciliationGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Container rule", FillWeight = 145, MinimumWidth = 180 });
    }

    private static string ReconciliationStatusText(
        ImportBalanceReconciliationStatus status) =>
        status switch
        {
            ImportBalanceReconciliationStatus.Ready => "Ready",
            ImportBalanceReconciliationStatus.NewCustomerPendingConfirmation => "New customer — confirm",
            ImportBalanceReconciliationStatus.UnresolvedCustomer => "Customer unresolved",
            ImportBalanceReconciliationStatus.UnresolvedContainer => "Container mapping required",
            ImportBalanceReconciliationStatus.MissingBroughtForward => "B/Fwd missing",
            ImportBalanceReconciliationStatus.ExcelTotalMismatch => "Excel total mismatch",
            ImportBalanceReconciliationStatus.ExistingCustomerPendingConfirmation => "Existing match — confirm",
            _ => status.ToString()
        };

    private static string ReviewStatusText(ImportCustomerReviewStatus status) =>
        status switch
        {
            ImportCustomerReviewStatus.Existing => "Existing — match",
            ImportCustomerReviewStatus.New => "New candidate",
            ImportCustomerReviewStatus.TypeMismatch => "TYPE MISMATCH",
            ImportCustomerReviewStatus.SourceConflict => "SOURCE CONFLICT",
            _ => status.ToString()
        };

    private async Task AnalyseAsync()
    {
        analyseResultTitle.Text = string.Empty;
        analyseResultDetails.Text = string.Empty;
        analyseWarningText.Text = string.Empty;
        analyseWarningPanel.Visible = false;
        nextButton.Enabled = false;
        analysis = null;
        mappingState.Clear();
        containerTokenMappings.Clear();
        customerDecisions.Clear();
        existingCustomerDecisions.Clear();

        try
        {
            UseWaitCursor = true;
            analyseButton.Enabled = false;

            analysis = await service.AnalyzeAsync(filePath.Text);

            analyseResultTitle.Text = "✓ Workbook analysed successfully";
            analyseResultDetails.Text =
                $"Worksheets: {analysis.WorksheetCount:N0}" + Environment.NewLine +
                $"Unique customer codes/names detected: {analysis.UniqueCustomerCount:N0}" + Environment.NewLine +
                $"Occurrences found across all sheets: {analysis.CustomerCandidateCount:N0}" + Environment.NewLine +
                $"B/Fwd / daily movement rows detected: {analysis.SnapshotCandidateCount:N0}";

            var duplicateCount = DuplicateGroups().Count();

            if (duplicateCount > 0 || analysis.Warnings.Count > 0)
            {
                analyseWarningPanel.Visible = true;

                var parts = new List<string>();

                if (duplicateCount > 0)
                    parts.Add($"{duplicateCount:N0} repeated customer code/name(s) detected across workbook sheets.");

                var structuralWarnings = analysis.Warnings.Count(x =>
                    !x.StartsWith(
                        "Potential duplicate customer codes detected",
                        StringComparison.OrdinalIgnoreCase));

                if (structuralWarnings > 0)
                    parts.Add($"{structuralWarnings:N0} structural warning(s) detected.");

                parts.Add("The Map step will separate Source sheets from Validation/Report sheets.");

                analyseWarningText.Text = string.Join(" ", parts);
            }

            nextButton.Enabled = true;
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

    private async Task ShowImportPageAsync()
    {
        if (analysis is null)
            return;

        ImportPreflightResult preflight;
        try
        {
            UseWaitCursor = true;
            nextButton.Enabled = false;
            preflight = await importExecutionService.PreflightAsync(
                analysis.FullPath);
        }
        catch (IOException)
        {
            // Excel/OneDrive may hold the workbook without allowing a second
            // reader. This is recoverable and must never terminate BinTracker.
            currentStep = 3;
            progress.ActiveStep = 3;
            progress.Invalidate();
            nextButton.Text = "Import >";
            nextButton.Enabled = true;

            MessageBox.Show(
                this,
                "BinTracker cannot read the workbook because it is currently open or locked by another program.\n\n" +
                "Close the workbook in Excel (and allow OneDrive to finish syncing if necessary), then click Import again.\n\n" +
                "Nothing has been imported.",
                "Workbook is in use",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return;
        }
        catch (UnauthorizedAccessException)
        {
            currentStep = 3;
            progress.ActiveStep = 3;
            progress.Invalidate();
            nextButton.Text = "Import >";
            nextButton.Enabled = true;

            MessageBox.Show(
                this,
                "BinTracker cannot read the workbook because access was denied. " +
                "Check that the file is available and not locked, then try again. Nothing has been imported.",
                "Cannot access workbook",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return;
        }
        finally
        {
            UseWaitCursor = false;
        }

        currentStep = 4;
        progress.ActiveStep = 4;
        progress.Invalidate();

        step4ExpectedSha256 = preflight.Source.Sha256;

        backButton.Visible = true;
        nextButton.Text = "Import now";
        nextButton.Enabled = preflight.CanProceed;

        pageHost.Controls.Clear();

        var page = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Padding = Padding.Empty
        };
        page.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        page.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        page.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

        var header = Card(new Padding(20, 14, 20, 14));
        header.Controls.Add(new Label
        {
            Text = "Import workbook",
            Dock = DockStyle.Top,
            AutoSize = true,
            Font = new Font("Segoe UI Semibold", 18F, FontStyle.Bold)
        });

        var statusCard = Card(new Padding(20, 16, 20, 16));
        var statusLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            ColumnCount = 1,
            RowCount = 6
        };

        statusLayout.Controls.Add(SectionHeading("Source provenance"), 0, 0);
        statusLayout.Controls.Add(new Label
        {
            AutoSize = true,
            Text = $"Workbook: {preflight.Source.FileName}",
            Margin = new Padding(0, 8, 0, 2)
        }, 0, 1);
        statusLayout.Controls.Add(new Label
        {
            AutoSize = true,
            Text = $"SHA-256: {preflight.Source.Sha256}",
            Font = new Font("Consolas", 9F),
            MaximumSize = new Size(1260, 0)
        }, 0, 2);
        statusLayout.Controls.Add(new Label
        {
            AutoSize = true,
            Text = $"Size: {preflight.Source.Length:N0} bytes    Modified: {preflight.Source.LastWriteUtc.ToLocalTime():g}",
            Margin = new Padding(0, 2, 0, 8)
        }, 0, 3);

        var duplicateText = preflight.ExactWorkbookPreviouslyImported
            ? $"⚠ This exact workbook was already imported in run #{preflight.PreviousImportRunId} on " +
              $"{preflight.PreviousCompletedUtc?.ToLocalTime():g} by {preflight.PreviousUsername}. " +
              "Exact re-import is blocked."
            : "✓ This exact workbook has not previously been recorded as a completed import.";

        statusLayout.Controls.Add(new Label
        {
            AutoSize = true,
            Text = duplicateText,
            ForeColor = preflight.ExactWorkbookPreviouslyImported
                ? Color.DarkOrange
                : Color.FromArgb(20, 115, 70),
            MaximumSize = new Size(1240, 0),
            Margin = new Padding(0, 8, 0, 8)
        }, 0, 4);

        statusLayout.Controls.Add(new Label
        {
            AutoSize = true,
            Text =
                preflight.CanProceed
                    ? "Ready for transactional import. BinTracker will re-check this fingerprint immediately before writing."
                    : "Import is blocked because this exact workbook has already completed an import.",
            ForeColor = Color.DimGray,
            MaximumSize = new Size(1240, 0)
        }, 0, 5);

        statusCard.Controls.Add(statusLayout);

        var body = Card(new Padding(20, 16, 20, 16));
        body.Dock = DockStyle.Fill;
        body.Controls.Add(new Label
        {
            AutoSize = true,
            Text =
                preflight.CanProceed
                    ? "Import now will create the confirmed new customers, reconcile each opening position to Excel B/Fwd, " +
                      "then preserve today's OUT and IN movements. All database changes, including the ImportRun, are committed " +
                      "together in one SQLite transaction. If any line fails, the entire import is rolled back."
                    : "Go Back to Review or Cancel. Exact re-imports are blocked to prevent duplicate balances and movements.",
            MaximumSize = new Size(1240, 0),
            ForeColor = Color.FromArgb(45, 55, 70)
        });

        page.Controls.Add(header, 0, 0);
        page.Controls.Add(statusCard, 0, 1);
        page.Controls.Add(body, 0, 2);

        pageHost.Controls.Add(page);
    }

    private async Task NextAsync()
    {
        if (currentStep == 1)
        {
            ShowMapPage();
            return;
        }

        if (currentStep == 2)
        {
            SaveMappingState();
            await ShowReviewPageAsync();
            return;
        }

        if (currentStep == 3)
        {
            await ShowImportPageAsync();
            return;
        }

        if (currentStep == 4)
        {
            await ExecuteImportAsync();
        }
    }

    private async Task ExecuteImportAsync()
    {
        if (analysis is null ||
            string.IsNullOrWhiteSpace(step4ExpectedSha256))
        {
            MessageBox.Show(
                this,
                "Import preflight is no longer available. Return to Review and open Step 4 again.",
                "Excel Import Wizard",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return;
        }

        var createCount =
            ImportCustomerDecisionPlanner.CreateCount(
                customerDecisions);

        var answer = MessageBox.Show(
            this,
            "BinTracker is ready to write this legacy workbook to the database.\n\n" +
            $"Cutover date: {DateOnly.FromDateTime(DateTime.Today):dd/MM/yyyy}\n" +
            $"New customers to create: {createCount:N0}\n\n" +
            "Excel B/Fwd will become the authoritative opening position. " +
            "The workbook's OUT and IN quantities will then be written as real movements.\n\n" +
            "The entire operation is atomic: if any line fails, all import changes are rolled back.\n\n" +
            "Proceed with Import now?",
            "Confirm Excel Import",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning,
            MessageBoxDefaultButton.Button2);

        if (answer != DialogResult.Yes)
            return;

        try
        {
            UseWaitCursor = true;
            nextButton.Enabled = false;
            backButton.Enabled = false;
            cancelButton.Enabled = false;

            var result = await importExecutionService.ExecuteAsync(
                new ImportExecutionRequest(
                    analysis.FullPath,
                    step4ExpectedSha256,
                    analysis,
                    CurrentMappings(),
                    new Dictionary<string, int>(
                        containerTokenMappings,
                        StringComparer.OrdinalIgnoreCase),
                    new Dictionary<string, ImportCustomerDecision>(
                        customerDecisions,
                        StringComparer.OrdinalIgnoreCase),
                    new Dictionary<string, ImportExistingCustomerDecision>(
                        existingCustomerDecisions,
                        StringComparer.OrdinalIgnoreCase),
                    DateOnly.FromDateTime(DateTime.Today)));

            MessageBox.Show(
                this,
                $"Import completed successfully.\n\n" +
                $"Import run: #{result.ImportRunId}\n" +
                $"Customers created: {result.CreatedCustomers:N0}\n" +
                $"Opening adjustments: {result.OpeningAdjustmentMovements:N0}\n" +
                $"OUT movements: {result.OutMovements:N0}\n" +
                $"IN movements: {result.InMovements:N0}\n" +
                $"Total movements written: {result.MovementCount:N0}\n\n" +
                "The source fingerprint has been recorded. An exact re-import of this workbook will now be blocked.",
                "Excel Import Complete",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);

            DialogResult = DialogResult.OK;
            Close();
        }
        catch (IOException)
        {
            MessageBox.Show(
                this,
                "BinTracker could not read the workbook because it is open or locked. " +
                "Close it in Excel, allow OneDrive to finish syncing, then try Import now again.\n\n" +
                "No partial import was committed.",
                "Workbook is in use",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }
        catch (UnauthorizedAccessException ex)
        {
            MessageBox.Show(
                this,
                ex.Message + "\n\nNo partial import was committed.",
                "Import not allowed",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }
        catch (InvalidOperationException ex)
        {
            MessageBox.Show(
                this,
                ex.Message + "\n\nThe import transaction was not committed. Return to Review if a decision needs to be corrected.",
                "Import stopped",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                this,
                "The import failed and the transaction was rolled back.\n\n" +
                ex.Message +
                "\n\nNo partial import was committed.",
                "Import failed",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
        finally
        {
            UseWaitCursor = false;

            if (!IsDisposed)
            {
                backButton.Enabled = true;
                cancelButton.Enabled = true;
                nextButton.Enabled = true;
            }
        }
    }

    private void Back()
    {
        if (currentStep == 2)
        {
            SaveMappingState();
            ShowAnalysePage();
            return;
        }

        if (currentStep == 3)
        {
            ShowMapPage();
            return;
        }

        if (currentStep == 4)
        {
            _ = ShowReviewPageAsync();
        }
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
        {
            filePath.Text = dialog.FileName;
            analysis = null;
            step4ExpectedSha256 = null;
            mappingState.Clear();
        containerTokenMappings.Clear();
        customerDecisions.Clear();
        existingCustomerDecisions.Clear();
            nextButton.Enabled = false;
            analyseResultTitle.Text = string.Empty;
            analyseResultDetails.Text = string.Empty;
            analyseWarningPanel.Visible = false;
        }
    }

    private IEnumerable<IGrouping<string, ImportCustomerCandidate>> DuplicateGroups()
    {
        if (analysis is null)
            return [];

        return analysis.CustomerCandidates
            .Where(x => !string.IsNullOrWhiteSpace(x.CustomerCode))
            .GroupBy(
                x => x.CustomerCode.Trim(),
                StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1)
            .OrderBy(group => group.Key, StringComparer.OrdinalIgnoreCase);
    }

    private void ViewDuplicates()
    {
        if (analysis is null)
        {
            MessageBox.Show(
                this,
                "Analyse a workbook first.",
                "Potential duplicates",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return;
        }

        var groups = DuplicateGroups().ToList();

        if (groups.Count == 0)
        {
            MessageBox.Show(
                this,
                "No repeated customer code/name occurrences were detected.",
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
            ClientSize = new Size(1060, 620),
            MinimumSize = new Size(900, 520),
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
                "Most should disappear from the import set once report/validation sheets are classified in Map.",
            AutoSize = true,
            MaximumSize = new Size(1000, 0),
            ForeColor = Color.DimGray,
            Margin = new Padding(0, 0, 0, 10)
        }, 0, 0);

        var grid = Grid();
        grid.Dock = DockStyle.Fill;

        grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Customer / Buyer",
            Width = 240
        });
        grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Occurrences",
            Width = 140
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
            Width = 125
        });
        grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Map status",
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
                    "Classify in Map");
            }
        }

        root.Controls.Add(grid, 0, 1);

        var close = SecondaryButton("Close", 110);
        close.Click += (_, _) => form.Close();

        var footer = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            Margin = new Padding(0, 10, 0, 0)
        };
        footer.Controls.Add(close);
        root.Controls.Add(footer, 0, 2);

        form.Controls.Add(root);
        form.ShowDialog(this);
    }

    private void ViewAllWorksheets()
    {
        if (analysis is null)
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
            ClientSize = new Size(980, 560),
            MinimumSize = new Size(840, 480),
            BackColor = Color.White,
            Font = Font
        };

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Padding = new Padding(12)
        };
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));

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
            Width = 95
        });
        grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Buyer columns",
            Width = 120
        });
        grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Candidates",
            Width = 115
        });
        grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Detection notes",
            AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
        });

        foreach (var sheet in analysis.Worksheets)
        {
            grid.Rows.Add(
                sheet.Name,
                sheet.UsedRows,
                sheet.UsedColumns,
                sheet.BuyerColumns,
                sheet.BuyerCandidates,
                sheet.Status);
        }

        root.Controls.Add(grid, 0, 0);

        var close = SecondaryButton("Close", 110);
        close.Click += (_, _) => form.Close();

        var footer = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            Margin = new Padding(0, 8, 0, 0)
        };
        footer.Controls.Add(close);

        root.Controls.Add(footer, 0, 1);

        form.Controls.Add(root);
        form.ShowDialog(this);
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
}
