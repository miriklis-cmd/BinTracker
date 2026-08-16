using BinTracker.Core;
using BinTracker.Data;
using BinTracker.Services;

namespace BinTracker.WinForms;

public sealed class MainForm : Form
{
    private readonly Label title = new();
    private readonly Panel content = new();
    private Control? selectedNav;
    private CustomersView? activeCustomersView;
    private OutstandingContainersReportForm? outstandingReportForm;
    private DailyMovementsReportForm? dailyMovementsReportForm;
    private WeeklyMovementsReportForm? weeklyMovementsReportForm;
    private bool bypassCustomerClosePrompt;
    private readonly UserSession session;
    private readonly IUserService users;
    private readonly IAuditService audit;
    private readonly ICustomerService customers;
    private readonly ICustomerStatementReportService statementReports;
    private readonly IAuthenticationService auth;
    private readonly IMovementService movements;
    private readonly ApplicationState appState;
    private readonly IMarketFloorReportService marketFloorReports;
    private readonly IOutstandingReportService outstandingReports;
    private readonly IOutstandingReportPdfService outstandingReportPdfs;
    private readonly IDailyMovementsReportService dailyMovementReports;
    private readonly IDailyMovementsReportPdfService dailyMovementReportPdfs;
    private readonly IWeeklyMovementsReportService weeklyMovementReports;
    private readonly IWeeklyMovementsReportPdfService weeklyMovementReportPdfs;
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
        IUserService users,
        IAuditService audit,
        ICustomerService customers,
        ICustomerStatementReportService statementReports,
        IAuthenticationService auth,
        IMovementService movements,
        ApplicationState appState,
        IMarketFloorReportService marketFloorReports,
        IOutstandingReportService outstandingReports,
        IOutstandingReportPdfService outstandingReportPdfs,
        IDailyMovementsReportService dailyMovementReports,
        IDailyMovementsReportPdfService dailyMovementReportPdfs,
        IWeeklyMovementsReportService weeklyMovementReports,
        IWeeklyMovementsReportPdfService weeklyMovementReportPdfs,
        IContainerTypeService containerTypes,
        IBusinessInformationService businessInformation,
        IExcelImportService excelImport,
        IImportExecutionService importExecution,
        IImportRunHistoryService importRunHistory,
        IBalanceService balances,
        IDeveloperDatabaseService developerDatabase)
    {
        this.session = session;
        this.users = users;
        this.audit = audit;
        this.customers = customers;
        this.statementReports = statementReports;
        this.auth = auth;
        this.movements = movements;
        this.appState = appState;
        this.marketFloorReports = marketFloorReports;
        this.outstandingReports = outstandingReports;
        this.outstandingReportPdfs = outstandingReportPdfs;
        this.dailyMovementReports = dailyMovementReports;
        this.dailyMovementReportPdfs = dailyMovementReportPdfs;
        this.weeklyMovementReports = weeklyMovementReports;
        this.weeklyMovementReportPdfs = weeklyMovementReportPdfs;
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
    }

    private void Build()
    {
        SuspendLayout();

        var side = new Panel
        {
            Dock = DockStyle.Left,
            Width = 250,
            MinimumSize = new Size(230, 0),
            BackColor = Color.FromArgb(29, 39, 54),
            Padding = new Padding(16)
        };

        side.Controls.Add(Nav("nav_settings", "Settings", ShowSettings));
        side.Controls.Add(Nav("nav_reports", "Reports", ShowReports));
        side.Controls.Add(Nav("nav_single", "Single Entry", ShowSingleEntry));
        side.Controls.Add(Nav("nav_batch", "Batch Entry", ShowBatchEntry));
        side.Controls.Add(Nav("nav_customers", "Customers", ShowCustomers));
        side.Controls.Add(Nav("nav_dashboard", "Dashboard", ShowDashboard));
        side.Controls.Add(new Label
        {
            Text = "BinTracker",
            Dock = DockStyle.Top,
            Height = 78,
            ForeColor = Color.White,
            Font = new Font("Segoe UI Semibold", 18F, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft,
            AutoEllipsis = false
        });

        var header = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 84,
            MinimumSize = new Size(0, 84),
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
        title.Anchor = AnchorStyles.Left;
        title.Font = new Font("Segoe UI Semibold", 20F, FontStyle.Bold);
        title.ForeColor = Color.FromArgb(29, 39, 54);
        title.TextAlign = ContentAlignment.MiddleLeft;
        title.AutoEllipsis = false;
        title.Margin = new Padding(0, 2, 0, 0);
        title.Padding = new Padding(0, 0, 0, 3);

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

        header.Controls.Add(title, 0, 0);
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
        if(!await ConfirmCanLeaveActiveCustomerAsync())
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

            if(!await ConfirmCanLeaveActiveCustomerAsync())
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
        return row;
    }

    private async void ShowDashboard()
    {
        SetPage("Dashboard");

        OperationalDashboardSummary summary;
        try
        {
            summary = await movements.GetDashboardSummaryAsync(
                DateOnly.FromDateTime(DateTime.Today));
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

    private void ShowBatchEntry()
    {
        SetPage("Batch Entry");
        content.AutoScroll = false;
        content.Controls.Add(new BatchEntryView(movements, session, appState));
    }

    private void ShowCustomers()
    {
        SetPage("Customers");
        content.AutoScroll = false;
        activeCustomersView = new CustomersView(customers, session, statementReports);
        content.Controls.Add(activeCustomersView);
    }

    private void ShowSingleEntry()
    {
        SetPage("Single Entry");
        content.AutoScroll = false;
        content.Controls.Add(new SingleEntryView(movements, session));
    }


    private void ShowReports()
    {
        SetPage("Reports");
        content.AutoScroll = true;
        content.Controls.Add(
            new ReportsView(
                marketFloorReports,
                OpenOutstandingReport,
                OpenDailyMovementsReport,
                OpenWeeklyMovementsReport));
    }

    private void OpenOutstandingReport()
    {
        if (outstandingReportForm is not null &&
            !outstandingReportForm.IsDisposed)
        {
            if (outstandingReportForm.WindowState ==
                FormWindowState.Minimized)
            {
                outstandingReportForm.WindowState =
                    FormWindowState.Normal;
            }

            outstandingReportForm.BringToFront();
            outstandingReportForm.Activate();
            return;
        }

        outstandingReportForm =
            new OutstandingContainersReportForm(
                outstandingReports,
                outstandingReportPdfs);

        outstandingReportForm.FormClosed += (_, _) =>
        {
            outstandingReportForm = null;
        };

        outstandingReportForm.Show(this);
    }

    private void OpenDailyMovementsReport()
    {
        if (dailyMovementsReportForm is not null &&
            !dailyMovementsReportForm.IsDisposed)
        {
            if (dailyMovementsReportForm.WindowState ==
                FormWindowState.Minimized)
            {
                dailyMovementsReportForm.WindowState =
                    FormWindowState.Normal;
            }

            dailyMovementsReportForm.BringToFront();
            dailyMovementsReportForm.Activate();
            return;
        }

        dailyMovementsReportForm =
            new DailyMovementsReportForm(
                dailyMovementReports,
                dailyMovementReportPdfs,
                outstandingReports);

        dailyMovementsReportForm.FormClosed += (_, _) =>
        {
            dailyMovementsReportForm = null;
        };

        dailyMovementsReportForm.Show(this);
    }

    private void OpenWeeklyMovementsReport()
    {
        if (weeklyMovementsReportForm is not null &&
            !weeklyMovementsReportForm.IsDisposed)
        {
            if (weeklyMovementsReportForm.WindowState == FormWindowState.Minimized)
                weeklyMovementsReportForm.WindowState = FormWindowState.Normal;

            weeklyMovementsReportForm.BringToFront();
            weeklyMovementsReportForm.Activate();
            return;
        }

        weeklyMovementsReportForm =
            new WeeklyMovementsReportForm(weeklyMovementReports, weeklyMovementReportPdfs, outstandingReports);

        weeklyMovementsReportForm.FormClosed += (_, _) =>
        {
            weeklyMovementsReportForm = null;
        };

        weeklyMovementsReportForm.Show(this);
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
            Text = "Manage authorised users, container types, business information, Excel import/history and inspect the audit trail.",
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

            var containerTypesButton = new Button
            {
                Text = "Container Types",
                AutoSize = false,
                Size = new Size(165, 44),
                Margin = new Padding(0, 0, 12, 0),
                TextAlign = ContentAlignment.MiddleCenter,
                UseCompatibleTextRendering = false
            };

            containerTypesButton.Click += (_, _) =>
            {
                using var form = new ContainerTypesForm(containerTypes);
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
                    importExecution);
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
            actions.Controls.Add(containerTypesButton);
            actions.Controls.Add(businessInformationButton);
            actions.Controls.Add(importExcelButton);
            actions.Controls.Add(importHistoryButton);
            actions.Controls.Add(auditButton);
        }
        else
        {
            actions.Controls.Add(new Label
            {
                Text = "Administrator access is required for user, container type, business information and audit controls.",
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

    private void SetPage(string page)
    {
        title.Text = page;
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
