using BinTracker.Core;
using BinTracker.Data;
using BinTracker.Services;

namespace BinTracker.WinForms;

public sealed class MainForm : BinTrackerForm
{
    private readonly Label title = new();
    private readonly Label pageSubtitle = new();
    private readonly FlowLayoutPanel pageBreadcrumb = new();
    private readonly LinkLabel reportsBreadcrumbLink = new();
    private readonly Label breadcrumbSeparator = new();
    private readonly Label breadcrumbCurrentPage = new();
    private readonly Panel content = new();
    private Control? selectedNav;
    private readonly Dictionary<string, Panel> navByPage = new(StringComparer.OrdinalIgnoreCase);
    private CustomersView? activeCustomersView;
    private ContainerTypesForm? activeContainerTypesForm;
    private Form? activeReportPage;
    private bool bypassCustomerClosePrompt;
    private readonly UserSession session;
    private readonly IBusinessClock clock;
    private readonly IUserService users;
    private readonly IAuditService audit;
    private readonly ICustomerService customers;
    private readonly ICustomerStatementReportService statementReports;
    private readonly IAuthenticationService auth;
    private readonly IMovementService movements;
    private readonly ApplicationState appState;
    private readonly IMarketFloorReportService marketFloorReports;
    private readonly IDailyPrintPackService dailyPrintPack;
    private readonly IOutstandingReportService outstandingReports;
    private readonly IOutstandingReportPdfService outstandingReportPdfs;
    private readonly IDailyMovementsReportService dailyMovementReports;
    private readonly IDailyMovementsReportPdfService dailyMovementReportPdfs;
    private readonly IWeeklyMovementsReportService weeklyMovementReports;
    private readonly IWeeklyMovementsReportPdfService weeklyMovementReportPdfs;
    private readonly IMovementHistoryReportService movementHistoryReports;
    private readonly IMovementHistoryReportPdfService movementHistoryReportPdfs;
    private readonly IMovementCorrectionService movementCorrections;
    private readonly IMonthlySummaryReportService monthlySummaryReports;
    private readonly IMonthlySummaryReportPdfService monthlySummaryReportPdfs;
    private readonly IContainerTypeService containerTypes;
    private readonly IBusinessInformationService businessInformation;
    private readonly IExcelImportService excelImport;
    private readonly IImportExecutionService importExecution;
    private readonly IImportRunHistoryService importRunHistory;
    private readonly IBalanceService balances;
    private readonly IDeveloperDatabaseService developerDatabase;

    /// <summary>
    /// True when the user deliberately chose Logout rather than closing
    /// BinTracker. Program.cs uses this to return to the login screen.
    /// </summary>
    public bool LogoutRequested { get; private set; }
    public bool RestartRequested { get; private set; }

    public MainForm(
        UserSession session,
        IBusinessClock clock,
        IUserService users,
        IAuditService audit,
        ICustomerService customers,
        ICustomerStatementReportService statementReports,
        IAuthenticationService auth,
        IMovementService movements,
        ApplicationState appState,
        IMarketFloorReportService marketFloorReports,
        IDailyPrintPackService dailyPrintPack,
        IOutstandingReportService outstandingReports,
        IOutstandingReportPdfService outstandingReportPdfs,
        IDailyMovementsReportService dailyMovementReports,
        IDailyMovementsReportPdfService dailyMovementReportPdfs,
        IWeeklyMovementsReportService weeklyMovementReports,
        IWeeklyMovementsReportPdfService weeklyMovementReportPdfs,
        IMovementHistoryReportService movementHistoryReports,
        IMovementHistoryReportPdfService movementHistoryReportPdfs,
        IMovementCorrectionService movementCorrections,
        IMonthlySummaryReportService monthlySummaryReports,
        IMonthlySummaryReportPdfService monthlySummaryReportPdfs,
        IContainerTypeService containerTypes,
        IBusinessInformationService businessInformation,
        IExcelImportService excelImport,
        IImportExecutionService importExecution,
        IImportRunHistoryService importRunHistory,
        IBalanceService balances,
        IDeveloperDatabaseService developerDatabase)
    {
        this.session = session;
        this.clock = clock;
        this.users = users;
        this.audit = audit;
        this.customers = customers;
        this.statementReports = statementReports;
        this.auth = auth;
        this.movements = movements;
        this.appState = appState;
        this.marketFloorReports = marketFloorReports;
        this.dailyPrintPack = dailyPrintPack;
        this.outstandingReports = outstandingReports;
        this.outstandingReportPdfs = outstandingReportPdfs;
        this.dailyMovementReports = dailyMovementReports;
        this.dailyMovementReportPdfs = dailyMovementReportPdfs;
        this.weeklyMovementReports = weeklyMovementReports;
        this.weeklyMovementReportPdfs = weeklyMovementReportPdfs;
        this.movementHistoryReports = movementHistoryReports;
        this.movementHistoryReportPdfs = movementHistoryReportPdfs;
        this.movementCorrections = movementCorrections;
        this.monthlySummaryReports = monthlySummaryReports;
        this.monthlySummaryReportPdfs = monthlySummaryReportPdfs;
        this.containerTypes = containerTypes;
        this.businessInformation = businessInformation;
        this.excelImport = excelImport;
        this.importExecution = importExecution;
        this.importRunHistory = importRunHistory;
        this.balances = balances;
        this.developerDatabase = developerDatabase;

        Text = $"BinTracker - {session.DisplayName}";
        StartPosition = FormStartPosition.CenterScreen;
        WindowState = FormWindowState.Maximized;
        AutoScaleMode = AutoScaleMode.Dpi;
        ClientSize = new Size(1280, 800);
        MinimumSize = new Size(980, 640);
        BackColor = Color.FromArgb(245, 247, 250);
        Font = new Font("Segoe UI", 10F);

        Build();
        FormClosing += MainForm_FormClosing;
        ShowDashboard();
        Shown += async (_, _) => await HandleRecoveredBatchAsync();
    }

    private void Build()
    {
        SuspendLayout();

        var side = new Panel
        {
            Dock = DockStyle.Left,
            Width = 260,
            MinimumSize = new Size(245, 0),
            BackColor = Color.FromArgb(29, 39, 54),
            Padding = new Padding(16)
        };

        side.Controls.Add(Nav("nav_settings", "Settings", ShowSettings));
        side.Controls.Add(Nav("nav_reports", "Reports", ShowReports));
        side.Controls.Add(Nav("nav_single", "Single Entry", ShowSingleEntry));
        side.Controls.Add(Nav("nav_batch", "Batch Entry", ShowBatchEntry));
        side.Controls.Add(Nav("nav_containers", "Containers", ShowContainers));
        side.Controls.Add(Nav("nav_customers", "Customers", ShowCustomers));
        side.Controls.Add(Nav("nav_dashboard", "Dashboard", ShowDashboard));
        var brand = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 82,
            BackColor = Color.Transparent,
            ColumnCount = 2,
            RowCount = 1,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        brand.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 52F));
        brand.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        brand.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

        brand.Controls.Add(new PictureBox
        {
            Image = IconAssets.Get("bintracker_logo"),
            SizeMode = PictureBoxSizeMode.Zoom,
            Dock = DockStyle.Fill,
            Margin = new Padding(2, 14, 6, 14),
            BackColor = Color.Transparent
        }, 0, 0);

        brand.Controls.Add(new Label
        {
            Text = "BinTracker",
            Dock = DockStyle.Fill,
            ForeColor = Color.White,
            Font = new Font("Segoe UI Semibold", 16F, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft,
            AutoEllipsis = false,
            Margin = new Padding(0, 0, 2, 0),
            Padding = new Padding(0, 0, 0, 1)
        }, 1, 0);

        side.Controls.Add(brand);

        var header = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 96,
            MinimumSize = new Size(0, 96),
            BackColor = Color.White,
            Padding = new Padding(24, 10, 24, 8),
            ColumnCount = 2,
            RowCount = 1
        };
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 56F));
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 44F));

        // AutoSize avoids the slight glyph clipping that can occur at
        // non-100% Windows scaling when a fixed-height Label renders Segoe UI.
        title.AutoSize = true;
        title.Font = new Font("Segoe UI Semibold", 20F, FontStyle.Bold);
        title.ForeColor = Color.FromArgb(29, 39, 54);
        title.TextAlign = ContentAlignment.MiddleLeft;
        title.AutoEllipsis = false;
        title.Margin = new Padding(0, 0, 0, 1);
        title.Padding = new Padding(0, 0, 0, 2);

        pageSubtitle.AutoSize = true;
        pageSubtitle.Font = new Font("Segoe UI", 10F, FontStyle.Regular);
        pageSubtitle.ForeColor = Color.FromArgb(75, 82, 96);
        pageSubtitle.Margin = Padding.Empty;
        pageSubtitle.Visible = false;

        pageBreadcrumb.AutoSize = true;
        pageBreadcrumb.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        pageBreadcrumb.FlowDirection = FlowDirection.LeftToRight;
        pageBreadcrumb.WrapContents = false;
        pageBreadcrumb.Margin = Padding.Empty;
        pageBreadcrumb.Padding = Padding.Empty;
        pageBreadcrumb.Visible = false;

        reportsBreadcrumbLink.Text = "Reports";
        reportsBreadcrumbLink.AutoSize = true;
        reportsBreadcrumbLink.LinkColor = Color.FromArgb(26, 91, 171);
        reportsBreadcrumbLink.ActiveLinkColor = Color.FromArgb(18, 70, 135);
        reportsBreadcrumbLink.VisitedLinkColor = reportsBreadcrumbLink.LinkColor;
        reportsBreadcrumbLink.LinkBehavior = LinkBehavior.HoverUnderline;
        reportsBreadcrumbLink.Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold);
        reportsBreadcrumbLink.Margin = new Padding(0, 0, 8, 0);
        reportsBreadcrumbLink.TabStop = true;
        reportsBreadcrumbLink.LinkClicked += (_, _) => ShowReports();

        breadcrumbSeparator.Text = "›";
        breadcrumbSeparator.AutoSize = true;
        breadcrumbSeparator.ForeColor = Color.FromArgb(75, 82, 96);
        breadcrumbSeparator.Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold);
        breadcrumbSeparator.Margin = new Padding(0, 0, 8, 0);

        breadcrumbCurrentPage.AutoSize = true;
        breadcrumbCurrentPage.ForeColor = Color.FromArgb(29, 39, 54);
        breadcrumbCurrentPage.Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold);
        breadcrumbCurrentPage.Margin = Padding.Empty;

        pageBreadcrumb.Controls.Add(reportsBreadcrumbLink);
        pageBreadcrumb.Controls.Add(breadcrumbSeparator);
        pageBreadcrumb.Controls.Add(breadcrumbCurrentPage);

        var titleStack = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = false,
            ColumnCount = 1,
            RowCount = 3,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        titleStack.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        titleStack.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        titleStack.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        titleStack.Controls.Add(pageBreadcrumb, 0, 0);
        titleStack.Controls.Add(title, 0, 1);
        titleStack.Controls.Add(pageSubtitle, 0, 2);

        var sessionArea = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = false,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };

        var logout = new LogoutControl
        {
            AutoSize = false,
            Size = new Size(126, 42),
            Margin = new Padding(12, 5, 0, 0),
            Cursor = Cursors.Hand
        };

        logout.Click += (_, _) => Logout();

        var signedIn = new Label
        {
            Text = $"Signed in: {session.DisplayName} ({session.Role})",
            AutoSize = true,
            TextAlign = ContentAlignment.MiddleRight,
            ForeColor = Color.DimGray,
            AutoEllipsis = true,
            Margin = new Padding(0, 16, 0, 0),
            MaximumSize = new Size(520, 0)
        };

        sessionArea.Controls.Add(logout);
        sessionArea.Controls.Add(signedIn);

        header.Controls.Add(titleStack, 0, 0);
        header.Controls.Add(sessionArea, 1, 0);

        content.Dock = DockStyle.Fill;
        content.Padding = new Padding(24);
        content.AutoScroll = true;
        content.BackColor = Color.FromArgb(245, 247, 250);

        var status = new StatusStrip
        {
            SizingGrip = true
        };

        status.Items.Add(new ToolStripStatusLabel
        {
            Text = $"BinTracker {AppVersion.Display}"
        });

        status.Items.Add(new ToolStripStatusLabel
        {
            Text = $"Database: {DatabaseSetup.StatusText}",
            Spring = true,
            TextAlign = ContentAlignment.MiddleLeft
        });

        Controls.Add(content);
        Controls.Add(header);
        Controls.Add(side);
        Controls.Add(status);

        ResumeLayout(true);
    }

    private async void Logout()
    {
        if(!await ConfirmCanLeaveCurrentPageAsync())
            return;

        var draftMessage = appState.DraftBatch.HasLines
            ? "\n\nYour unsaved Batch Entry draft will be kept on this computer for the next login."
            : string.Empty;

        if (MessageBox.Show(
                $"Log out {session.DisplayName}?{draftMessage}",
                "Logout",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question) != DialogResult.Yes)
        {
            return;
        }

        LogoutRequested = true;
        bypassCustomerClosePrompt = true;
        Close();
    }

    /// <summary>
    /// Creates an embedded-icon navigation row and highlights the active page.
    /// </summary>
    private Control Nav(string iconName, string text, Action action)
    {
        var normal = Color.FromArgb(29, 39, 54);
        var selected = Color.FromArgb(40, 78, 128);

        var row = new Panel
        {
            Dock = DockStyle.Top,
            Height = 50,
            BackColor = normal,
            Cursor = Cursors.Hand,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };

        var icon = new PictureBox
        {
            Image = IconAssets.Get(iconName),
            SizeMode = PictureBoxSizeMode.CenterImage,
            Dock = DockStyle.Left,
            Width = 42,
            Cursor = Cursors.Hand,
            BackColor = Color.Transparent
        };

        var caption = new Button
        {
            Text = text,
            Dock = DockStyle.Fill,
            FlatStyle = FlatStyle.Flat,
            BackColor = normal,
            ForeColor = Color.White,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(4, 0, 0, 0),
            Cursor = Cursors.Hand,
            AutoEllipsis = true,
            Margin = Padding.Empty
        };

        caption.FlatAppearance.BorderSize = 0;

        async void Activate()
        {
            if(ReferenceEquals(selectedNav, row))
                return;

            if(!await ConfirmCanLeaveCurrentPageAsync())
                return;

            if (selectedNav is Panel previous)
            {
                previous.BackColor = normal;
                foreach (Control child in previous.Controls)
                    child.BackColor = child is PictureBox ? Color.Transparent : normal;
            }

            selectedNav = row;
            row.BackColor = selected;
            caption.BackColor = selected;
            icon.BackColor = Color.Transparent;
            action();
        }

        caption.Click += (_, _) => Activate();
        icon.Click += (_, _) => Activate();
        row.Click += (_, _) => Activate();

        row.Controls.Add(caption);
        row.Controls.Add(icon);
        navByPage[text] = row;
        return row;
    }

    private void SelectNavigationForPage(string page)
    {
        if (!navByPage.TryGetValue(page, out var row))
            return;

        var normal = Color.FromArgb(29, 39, 54);
        var selected = Color.FromArgb(40, 78, 128);

        if (selectedNav is Panel previous && !ReferenceEquals(previous, row))
        {
            previous.BackColor = normal;
            foreach (Control child in previous.Controls)
                child.BackColor = child is PictureBox ? Color.Transparent : normal;
        }

        selectedNav = row;
        row.BackColor = selected;
        foreach (Control child in row.Controls)
            child.BackColor = child is PictureBox ? Color.Transparent : selected;
    }

    private async void ShowDashboard()
    {
        SetPage("Dashboard");

        OperationalDashboardSummary summary;
        try
        {
            summary = await movements.GetDashboardSummaryAsync(
                clock.Today);
        }
        catch
        {
            summary = new OperationalDashboardSummary(0, 0, 0, 0);
        }

        var page = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 1,
            RowCount = 3,
            Margin = new Padding(0),
            Padding = new Padding(0)
        };
        page.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

        var cards = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 245,
            MinimumSize = new Size(0, 245),
            ColumnCount = 4,
            RowCount = 1,
            Margin = new Padding(0, 0, 0, 16),
            Padding = new Padding(0)
        };

        for (var column = 0; column < 4; column++)
            cards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));

        cards.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

        cards.Controls.Add(
            Card("Returned Today", summary.ReturnedToday.ToString("N0"), "IN"),
            0, 0);

        cards.Controls.Add(
            Card("Taken Today", summary.TakenToday.ToString("N0"), "OUT"),
            1, 0);

        cards.Controls.Add(
            Card(
                "Outstanding",
                summary.Outstanding.ToString("N0"),
                "Positive customer/container positions"),
            2, 0);

        cards.Controls.Add(
            Card(
                "Requires Attention",
                summary.RequiresAttention.ToString("N0"),
                "Customers over the configured quantity threshold"),
            3, 0);

        var draftText = appState.DraftBatch.HasLines
            ? $" Unsaved Batch Entry draft: {appState.DraftBatch.Lines.Count} line(s), {appState.DraftBatch.TotalQuantity} containers."
            : string.Empty;

        var info = PanelBox(
            "Security and audit enabled",
            $"Signed in as {session.DisplayName}. Logins, user administration, customers and movement batches are recorded in the audit trail.{draftText}");

        page.Controls.Add(cards, 0, 0);
        page.Controls.Add(info, 0, 1);
        content.Controls.Add(page);
    }

    private async Task HandleRecoveredBatchAsync()
    {
        if (!appState.RecoveryPromptPending || !appState.DraftBatch.HasLines)
            return;

        // Only a persisted draft loaded when this application process started
        // is a recovery event. Mark it handled before showing the dialog so a
        // later logout/login in the same process does not prompt again.
        appState.MarkRecoveryPromptHandled();

        using var dialog = new RecoveredBatchDialog(appState.DraftBatch, appState.RecoveryDraftLastSavedAtUtc);
        dialog.ShowDialog(this);

        switch (dialog.SelectedAction)
        {
            case RecoveredBatchAction.Continue:
                ShowBatchEntry();
                break;

            case RecoveredBatchAction.Save:
                await SaveRecoveredBatchAsync();
                break;

            case RecoveredBatchAction.Discard:
                var answer = MessageBox.Show(
                    "Permanently discard this recovered batch?\n\n" +
                    "The unsaved movements cannot be recovered after this.",
                    "Discard Recovered Batch",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);

                if (answer == DialogResult.Yes)
                {
                    appState.ClearDraft();
                    ShowDashboard();
                }
                else
                {
                    ShowBatchEntry();
                }
                break;
        }
    }

    private async Task SaveRecoveredBatchAsync()
    {
        var draft = appState.DraftBatch;

        try
        {
            var result = await movements.SaveBatchAsync(
                new SaveMovementBatchRequest(
                Guid.NewGuid(),
                    draft.MovementDate,
                    draft.MovementType,
                    null,
                    draft.Lines
                        .Select(x => new MovementBatchLine(
                            x.CustomerId,
                            x.ContainerTypeId,
                            x.Quantity,
                            x.Reference,
                            x.Notes))
                        .ToList()));

            appState.ClearDraft();

            MessageBox.Show(
                $"Recovered batch #{result.BatchId} saved successfully.\n\n" +
                $"Lines: {result.LineCount}\n" +
                $"Total containers: {result.TotalQuantity}",
                "Recovered Batch Saved",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);

            ShowDashboard();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                "The recovered batch could not be saved. It has not been discarded.\n\n" +
                ex.Message,
                "Recovered Batch",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);

            ShowBatchEntry();
        }
    }

    private void ShowBatchEntry()
    {
        SetPage("Batch Entry");
        content.AutoScroll = false;
        content.Controls.Add(new BatchEntryView(movements, session, appState, ShowDashboard));
    }

    private void ShowCustomers()
    {
        SetPage("Customers");
        content.AutoScroll = false;
        activeCustomersView = new CustomersView(customers, session, statementReports, clock);
        content.Controls.Add(activeCustomersView);
    }

    private void ShowContainers()
    {
        SetPage(
            "Containers",
            "View configured container types. Administrators can add, rename, reorder, deactivate and reactivate them.");

        content.AutoScroll = false;

        activeContainerTypesForm = new ContainerTypesForm(
            containerTypes,
            canEdit: session.Role == UserRole.Administrator)
        {
            TopLevel = false,
            FormBorderStyle = FormBorderStyle.None,
            Dock = DockStyle.Fill
        };

        content.Controls.Add(activeContainerTypesForm);
        activeContainerTypesForm.Show();
    }

    private void ShowSingleEntry()
    {
        SetPage("Single Entry");
        content.AutoScroll = false;
        content.Controls.Add(new SingleEntryView(movements, session, clock));
    }


    private void ShowReports()
    {
        SetPage(
            "Reports",
            "Generate operational sheets and explore detailed reports.");
        // ReportsView owns its scrolling. Keeping the host scrollable as well
        // creates a second horizontal scrollbar at some DPI/working-area sizes.
        content.AutoScroll = false;
        content.Controls.Add(
            new ReportsView(
                marketFloorReports,
                dailyPrintPack,
                clock,
                OpenOutstandingReport,
                OpenDailyMovementsReport,
                OpenWeeklyMovementsReport,
                OpenMovementHistoryReport,
                OpenCustomerStatementReport,
                OpenMonthlySummaryReport));
    }

    private void OpenOutstandingReport()
    {
        OpenEmbeddedReport(
            "Outstanding Containers",
            "Shows customer/container position at the end of the selected date. Container types for the same customer stay together. Future movements do not affect a historical result.",
            new OutstandingContainersReportForm(
                outstandingReports,
                outstandingReportPdfs,
                containerTypes,
                audit,
                clock));
    }

    private void OpenDailyMovementsReport()
    {
        OpenEmbeddedReport(
            "Daily Movements",
            "Physical IN/OUT activity for one day. Opening adjustments are excluded by default and can be included explicitly.",
            new DailyMovementsReportForm(
                dailyMovementReports,
                dailyMovementReportPdfs,
                containerTypes,
                audit,
                clock));
    }

    private void OpenWeeklyMovementsReport()
    {
        OpenEmbeddedReport(
            "Weekly Movements",
            "Monday-to-Sunday reporting. Daily Detail shows every movement; Weekly Overview totals OUT and IN by customer/container.",
            new WeeklyMovementsReportForm(
                weeklyMovementReports,
                weeklyMovementReportPdfs,
                containerTypes,
                audit,
                clock));
    }

    private void OpenMovementHistoryReport()
    {
        OpenEmbeddedReport(
            "Movement History",
            "Search, export and reverse saved movements without leaving the main BinTracker workspace.",
            new MovementHistoryReportForm(
                movementHistoryReports,
                movementHistoryReportPdfs,
                containerTypes,
                audit,
                movementCorrections,
                session,
                clock),
            hideInternalHeader: false);
    }

    private void OpenCustomerStatementReport()
    {
        OpenEmbeddedReport(
            "Customer Statement",
            "Select a customer, then generate or open a statement for the required period.",
            new CustomerStatementReportForm(
                customers,
                statementReports,
                clock));
    }

    private void OpenMonthlySummaryReport()
    {
        OpenEmbeddedReport(
            "Monthly Summary",
            "Monthly OUT, IN and net movement totals by customer and container. Opening adjustments are excluded by default.",
            new MonthlySummaryReportForm(
                monthlySummaryReports,
                monthlySummaryReportPdfs,
                containerTypes,
                audit,
                clock));
    }

    private void OpenEmbeddedReport(
        string pageName,
        string subtitle,
        Form report,
        bool hideInternalHeader = true)
    {
        SetPage(pageName, subtitle, showReportsBreadcrumb: true);
        content.AutoScroll = false;

        activeReportPage = report;
        report.TopLevel = false;
        report.FormBorderStyle = FormBorderStyle.None;
        report.Dock = DockStyle.Fill;
        report.MinimumSize = Size.Empty;
        report.MaximumSize = Size.Empty;

        if (hideInternalHeader)
            HideEmbeddedReportChrome(report, pageName);

        content.Controls.Add(report);
        report.Show();
    }

    private static void HideEmbeddedReportChrome(
        Control root,
        string pageName)
    {
        foreach (Control child in root.Controls.Cast<Control>().ToArray())
        {
            if (child is Button button &&
                string.Equals(
                    button.Text,
                    "Close",
                    StringComparison.OrdinalIgnoreCase))
            {
                button.Visible = false;
                continue;
            }

            if (child is Label label &&
                label.Font.Size >= 18F &&
                label.Text.StartsWith(
                    pageName,
                    StringComparison.OrdinalIgnoreCase) &&
                label.Parent is not null)
            {
                // The embedded report form still contains the standalone-window
                // title/description header. The MainForm shell already renders
                // the single report title and carries the accepted explanation,
                // so remove this entire legacy header to avoid duplicate titles
                // and wasted vertical space.
                label.Parent.Visible = false;
                continue;
            }

            HideEmbeddedReportChrome(child, pageName);
        }
    }

    private void ShowSettings()
    {
        SetPage("Settings");

        var page = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 1,
            RowCount = 2,
            Margin = new Padding(0),
            Padding = new Padding(0)
        };

        page.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        page.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        page.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        page.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        page.Controls.Add(BuildProfileSettingsSection(), 0, 0);
        page.Controls.Add(BuildAdministrationSettingsSection(), 0, 1);

        if (session.Role == UserRole.Administrator)
            page.Controls.Add(BuildDeveloperSettingsSection(), 0, 2);

        content.Controls.Add(page);
    }

    private Panel BuildProfileSettingsSection()
    {
        var section = SettingsSection("My Profile");

        AddSettingsRow(section, "Display name", session.DisplayName);
        AddSettingsRow(section, "Username", session.Username);
        AddSettingsRow(section, "Role", session.Role.ToString());
        AddSettingsRow(section, "Logged in",
            session.LoginUtc?.ToLocalTime().ToString("dd/MM/yyyy HH:mm") ?? "Unknown");
        AddSettingsRow(section, "Session ID", session.SessionId);

        var actions = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true,
            Margin = new Padding(0, 16, 0, 0),
            Padding = new Padding(0)
        };

        var changePassword = new Button
        {
            Text = "Change Password",
            AutoSize = false,
            Size = new Size(165, 44),
            Margin = new Padding(0)
        };

        changePassword.Click += (_, _) =>
        {
            using var form = new ChangePasswordForm(auth);
            form.ShowDialog(this);
        };

        actions.Controls.Add(changePassword);

        var row = section.RowCount++;
        section.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        section.Controls.Add(actions, 0, row);
        section.SetColumnSpan(actions, 2);

        return WrapSettingsSection(section);
    }

    private Panel BuildAdministrationSettingsSection()
    {
        var section = SettingsSection("Administration");

        var description = new Label
        {
            Text = "Manage authorised users, business information, Excel import/history and inspect the audit trail. Container Types are managed from the Containers navigation page.",
            AutoSize = true,
            ForeColor = Color.FromArgb(80, 90, 105),
            MaximumSize = new Size(900, 0),
            Margin = new Padding(0, 0, 0, 14)
        };

        var descriptionRow = section.RowCount++;
        section.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        section.Controls.Add(description, 0, descriptionRow);
        section.SetColumnSpan(description, 2);

        var actions = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true,
            Margin = new Padding(0),
            Padding = new Padding(0)
        };

        if (session.Role == UserRole.Administrator)
        {
            var usersButton = new Button
            {
                Text = "Manage Users",
                AutoSize = false,
                Size = new Size(165, 44),
                Margin = new Padding(0, 0, 12, 0),
                TextAlign = ContentAlignment.MiddleCenter,
                UseCompatibleTextRendering = false
            };

            usersButton.Click += (_, _) =>
            {
                using var form = new UsersForm(users);
                form.ShowDialog(this);
            };

            var businessInformationButton = new Button
            {
                Text = "Business Information",
                AutoSize = false,
                Size = new Size(210, 44),
                Margin = new Padding(0, 0, 12, 0),
                TextAlign = ContentAlignment.MiddleCenter,
                UseCompatibleTextRendering = false
            };

            businessInformationButton.Click += (_, _) =>
            {
                using var form = new BusinessInformationForm(businessInformation);
                form.ShowDialog(this);
            };

            var importExcelButton = new Button
            {
                Text = "Import Excel",
                AutoSize = false,
                Size = new Size(165, 44),
                Margin = new Padding(0, 0, 12, 0),
                TextAlign = ContentAlignment.MiddleCenter,
                UseCompatibleTextRendering = false
            };

            importExcelButton.Click += (_, _) =>
            {
                using var form = new ExcelImportForm(
                    excelImport,
                    customers,
                    containerTypes,
                    balances,
                    importExecution,
                    clock);
                form.ShowDialog(this);
            };

            var importHistoryButton = new Button
            {
                Text = "Import History",
                AutoSize = false,
                Size = new Size(165, 44),
                Margin = new Padding(0, 0, 12, 0),
                TextAlign = ContentAlignment.MiddleCenter,
                UseCompatibleTextRendering = false
            };

            importHistoryButton.Click += (_, _) =>
            {
                using var form = new ImportRunHistoryForm(importRunHistory);
                form.ShowDialog(this);
            };

            var auditButton = new Button
            {
                Text = "View Audit Trail",
                AutoSize = false,
                Size = new Size(165, 44),
                Margin = new Padding(0),
                TextAlign = ContentAlignment.MiddleCenter,
                UseCompatibleTextRendering = false
            };

            auditButton.Click += (_, _) =>
            {
                using var form = new AuditLogForm(audit);
                form.ShowDialog(this);
            };

            actions.Controls.Add(usersButton);
            actions.Controls.Add(businessInformationButton);
            actions.Controls.Add(importExcelButton);
            actions.Controls.Add(importHistoryButton);
            actions.Controls.Add(auditButton);
        }
        else
        {
            actions.Controls.Add(new Label
            {
                Text = "Administrator access is required for user, business information and audit controls. Container Types can be viewed from Containers but only administrators can change them.",
                AutoSize = true,
                ForeColor = Color.Firebrick,
                MaximumSize = new Size(720, 0),
                Margin = new Padding(0)
            });
        }

        var actionRow = section.RowCount++;
        section.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        section.Controls.Add(actions, 0, actionRow);
        section.SetColumnSpan(actions, 2);

        return WrapSettingsSection(section);
    }

    private Panel BuildDeveloperSettingsSection()
    {
        var section = SettingsSection("Developer Tools");

        var description = new Label
        {
            Text =
                "Database test utilities for import development. Backup the current state, " +
                "load a previous test database, or restart with a completely fresh database.",
            AutoSize = true,
            ForeColor = Color.FromArgb(80, 90, 105),
            MaximumSize = new Size(900, 0),
            Margin = new Padding(0, 0, 0, 14)
        };

        var descriptionRow = section.RowCount++;
        section.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        section.Controls.Add(description, 0, descriptionRow);
        section.SetColumnSpan(description, 2);

        var button = new Button
        {
            Text = "Developer Database",
            AutoSize = false,
            Size = new Size(190, 44),
            Margin = Padding.Empty,
            TextAlign = ContentAlignment.MiddleCenter
        };

        button.Click += (_, _) =>
        {
            using var form = new DeveloperDatabaseToolsForm(
                developerDatabase,
                () =>
                {
                    RestartRequested = true;
                    Close();
                });

            form.ShowDialog(this);
        };

        var actions = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Margin = Padding.Empty
        };
        actions.Controls.Add(button);

        var row = section.RowCount++;
        section.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        section.Controls.Add(actions, 0, row);
        section.SetColumnSpan(actions, 2);

        return WrapSettingsSection(section);
    }

    private static TableLayoutPanel SettingsSection(string heading)
    {
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 2,
            RowCount = 1,
            Margin = new Padding(0),
            Padding = new Padding(0)
        };

        layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var headingLabel = new Label
        {
            Text = heading,
            AutoSize = true,
            Font = new Font("Segoe UI Semibold", 18F, FontStyle.Bold),
            Margin = new Padding(0, 0, 0, 14)
        };

        layout.Controls.Add(headingLabel, 0, 0);
        layout.SetColumnSpan(headingLabel, 2);

        return layout;
    }

    private static void AddSettingsRow(TableLayoutPanel section, string labelText, string valueText)
    {
        var row = section.RowCount++;
        section.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        section.Controls.Add(new Label
        {
            Text = labelText,
            AutoSize = true,
            ForeColor = Color.FromArgb(90, 100, 115),
            Margin = new Padding(0, 6, 24, 6)
        }, 0, row);

        section.Controls.Add(new Label
        {
            Text = valueText,
            AutoSize = true,
            ForeColor = Color.FromArgb(29, 39, 54),
            MaximumSize = new Size(760, 0),
            Margin = new Padding(0, 6, 0, 6)
        }, 1, row);
    }

    private static Panel WrapSettingsSection(TableLayoutPanel child)
    {
        var panel = new Panel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            BackColor = Color.White,
            Padding = new Padding(24),
            Margin = new Padding(0, 0, 0, 16),
            MinimumSize = new Size(0, 140)
        };

        panel.Controls.Add(child);
        return panel;
    }

        private async Task<bool> ConfirmCanLeaveCurrentPageAsync()
    {
        if (!await ConfirmCanLeaveActiveCustomerAsync())
            return false;

        if (activeContainerTypesForm is not null &&
            !activeContainerTypesForm.IsDisposed &&
            !await activeContainerTypesForm.ConfirmCanLeaveAsync())
        {
            return false;
        }

        return true;
    }

private async Task<bool> ConfirmCanLeaveActiveCustomerAsync() =>
        activeCustomersView is null ||
        await activeCustomersView.ConfirmCanLeaveAsync();

    private async void MainForm_FormClosing(
        object? sender,
        FormClosingEventArgs e)
    {
        if(bypassCustomerClosePrompt || activeCustomersView is null)
            return;

        e.Cancel = true;

        if(await activeCustomersView.ConfirmCanLeaveAsync())
        {
            bypassCustomerClosePrompt = true;
            Close();
        }
    }

    private void Placeholder(string page, string text)
    {
        SetPage(page);
        content.Controls.Add(PanelBox(page, text));
    }

    private void SetPage(
        string page,
        string? subtitle = null,
        bool showReportsBreadcrumb = false)
    {
        if (activeReportPage is not null)
        {
            activeReportPage.Dispose();
            activeReportPage = null;
        }

        if (activeContainerTypesForm is not null)
        {
            activeContainerTypesForm.PrepareForHostClose();
            activeContainerTypesForm.Dispose();
            activeContainerTypesForm = null;
        }

        SelectNavigationForPage(page);
        title.Text = page;
        pageSubtitle.Text = subtitle ?? string.Empty;
        pageSubtitle.Visible = !string.IsNullOrWhiteSpace(subtitle);
        breadcrumbCurrentPage.Text = page;
        pageBreadcrumb.Visible = showReportsBreadcrumb;
        content.Controls.Clear();
        activeCustomersView = null;
        content.AutoScroll = true;
    }

    private static Panel Card(string heading, string value, string subtitle)
    {
        var panel = new Panel
        {
            Dock = DockStyle.Fill,
            MinimumSize = new Size(180, 220),
            BackColor = Color.White,
            Padding = new Padding(18),
            Margin = new Padding(0, 0, 16, 0)
        };

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Margin = new Padding(0),
            Padding = new Padding(0)
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

        var headingLabel = new Label
        {
            Text = heading,
            Dock = DockStyle.Top,
            AutoSize = true,
            ForeColor = Color.Gray,
            Margin = new Padding(0, 0, 0, 8)
        };

        var valueLabel = new Label
        {
            Text = value,
            Dock = DockStyle.Top,
            AutoSize = true,
            Font = new Font("Segoe UI Semibold", 28F, FontStyle.Bold),
            Margin = new Padding(0, 0, 0, 8)
        };

        var subtitleLabel = new Label
        {
            Text = subtitle,
            Dock = DockStyle.Fill,
            AutoSize = false,
            ForeColor = Color.DimGray,
            TextAlign = ContentAlignment.TopLeft,
            Margin = new Padding(0),
            UseMnemonic = false
        };

        layout.Controls.Add(headingLabel, 0, 0);
        layout.Controls.Add(valueLabel, 0, 1);
        layout.Controls.Add(subtitleLabel, 0, 2);
        panel.Controls.Add(layout);
        return panel;
    }

    private static Panel PanelBox(string heading, string body)
    {
        var panel = new Panel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            MinimumSize = new Size(0, 175),
            BackColor = Color.White,
            Padding = new Padding(24),
            Margin = new Padding(0, 0, 0, 16)
        };

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 1,
            RowCount = 2,
            Margin = new Padding(0),
            Padding = new Padding(0)
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

        layout.Controls.Add(new Label
        {
            Text = heading,
            AutoSize = true,
            Font = new Font("Segoe UI Semibold", 18F, FontStyle.Bold),
            Margin = new Padding(0, 0, 0, 8),
            MaximumSize = new Size(900, 0)
        }, 0, 0);

        layout.Controls.Add(new Label
        {
            Text = body,
            AutoSize = true,
            Font = new Font("Segoe UI", 11F),
            ForeColor = Color.FromArgb(80, 90, 105),
            MaximumSize = new Size(900, 0),
            Margin = new Padding(0)
        }, 0, 1);

        panel.Controls.Add(layout);
        return panel;
    }

    /// <summary>
    /// DPI-safe logout control. Icon and text are drawn directly so WinForms
    /// cannot crop the bitmap or truncate the caption at scaled display settings.
    /// </summary>
    private sealed class LogoutControl : Control
    {
        public LogoutControl()
        {
            BackColor = Color.White;
            ForeColor = Color.Black;
            Font = new Font("Segoe UI", 10F);
            SetStyle(
                ControlStyles.UserPaint |
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw,
                true);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            var borderRect = new Rectangle(
                0,
                0,
                Math.Max(0, Width - 1),
                Math.Max(0, Height - 1));

            using var borderPen = new Pen(Color.FromArgb(120, 135, 155), 1F);
            e.Graphics.DrawRectangle(borderPen, borderRect);

            var iconColor = Color.FromArgb(42, 57, 79);
            using var iconPen = new Pen(iconColor, 1.8F);

            var iconX = 14F;
            var iconY = Height / 2F - 8F;

            // Door.
            e.Graphics.DrawRectangle(
                iconPen,
                iconX,
                iconY,
                10F,
                16F);

            // Outward arrow.
            var midY = Height / 2F;
            e.Graphics.DrawLine(iconPen, iconX + 8F, midY, iconX + 24F, midY);
            e.Graphics.DrawLine(iconPen, iconX + 19F, midY - 5F, iconX + 24F, midY);
            e.Graphics.DrawLine(iconPen, iconX + 19F, midY + 5F, iconX + 24F, midY);

            var textRect = new Rectangle(
                48,
                0,
                Math.Max(0, Width - 54),
                Height);

            TextRenderer.DrawText(
                e.Graphics,
                "Logout",
                Font,
                textRect,
                ForeColor,
                TextFormatFlags.VerticalCenter |
                TextFormatFlags.Left |
                TextFormatFlags.NoPadding |
                TextFormatFlags.SingleLine);
        }
    }

}
