using BinTracker.Core;
using BinTracker.Data;
using BinTracker.Services;

namespace BinTracker.WinForms;

public sealed class MainForm : Form
{
    private readonly Label title = new();
    private readonly Panel content = new();
    private Control? selectedNav;
    private readonly UserSession session;
    private readonly IUserService users;
    private readonly IAuditService audit;
    private readonly ICustomerService customers;
    private readonly ICustomerStatementReportService statementReports;
    private readonly IAuthenticationService auth;
    private readonly IMovementService movements;
    private readonly ApplicationState appState;
    private const string ApplicationVersion = "v0.2.0-alpha.7.2.11";

    /// <summary>
    /// True when the user deliberately chose Logout rather than closing
    /// BinTracker. Program.cs uses this to return to the login screen.
    /// </summary>
    public bool LogoutRequested { get; private set; }

    public MainForm(
        UserSession session,
        IUserService users,
        IAuditService audit,
        ICustomerService customers,
        ICustomerStatementReportService statementReports,
        IAuthenticationService auth,
        IMovementService movements,
        ApplicationState appState)
    {
        this.session = session;
        this.users = users;
        this.audit = audit;
        this.customers = customers;
        this.statementReports = statementReports;
        this.auth = auth;
        this.movements = movements;
        this.appState = appState;

        Text = $"BinTracker - {session.DisplayName}";
        StartPosition = FormStartPosition.CenterScreen;
        WindowState = FormWindowState.Maximized;
        AutoScaleMode = AutoScaleMode.Dpi;
        ClientSize = new Size(1280, 800);
        MinimumSize = new Size(980, 640);
        BackColor = Color.FromArgb(245, 247, 250);
        Font = new Font("Segoe UI", 10F);

        Build();
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
        side.Controls.Add(Nav("nav_reports", "Reports", () => Placeholder("Reports", "Report generation will record the user, filters, dates and export format in the audit trail.")));
        side.Controls.Add(Nav("nav_single", "Single Entry", () => Placeholder("Single Entry", "Record one IN (Returned) or OUT (Taken) movement.")));
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

        var logout = new Button
        {
            Text = "Logout",
            Image = IconAssets.Get("logout"),
            ImageAlign = ContentAlignment.MiddleLeft,
            TextAlign = ContentAlignment.MiddleRight,
            TextImageRelation = TextImageRelation.ImageBeforeText,
            AutoSize = false,
            Size = new Size(122, 42),
            Padding = new Padding(10, 0, 10, 0),
            Margin = new Padding(12, 5, 0, 0),
            Cursor = Cursors.Hand,
            UseVisualStyleBackColor = true
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
            Text = $"BinTracker {ApplicationVersion}"
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

    private void Logout()
    {
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

        void Activate()
        {
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
            RowCount = 2,
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
        content.Controls.Add(new CustomersView(customers, session, statementReports));
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

        page.Controls.Add(BuildProfileSettingsSection(), 0, 0);
        page.Controls.Add(BuildAdministrationSettingsSection(), 0, 1);

        content.Controls.Add(page);
    }

    private Control BuildProfileSettingsSection()
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

    private Control BuildAdministrationSettingsSection()
    {
        var section = SettingsSection("Administration");

        var description = new Label
        {
            Text = "Manage authorised users and inspect the audit trail.",
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
                Margin = new Padding(0, 0, 12, 0)
            };

            usersButton.Click += (_, _) =>
            {
                using var form = new UsersForm(users);
                form.ShowDialog(this);
            };

            var auditButton = new Button
            {
                Text = "View Audit Trail",
                AutoSize = false,
                Size = new Size(165, 44),
                Margin = new Padding(0)
            };

            auditButton.Click += (_, _) =>
            {
                using var form = new AuditLogForm(audit);
                form.ShowDialog(this);
            };

            actions.Controls.Add(usersButton);
            actions.Controls.Add(auditButton);
        }
        else
        {
            actions.Controls.Add(new Label
            {
                Text = "Administrator access is required for user and audit controls.",
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

    private static Panel WrapSettingsSection(Control child)
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

    private void Placeholder(string page, string text)
    {
        SetPage(page);
        content.Controls.Add(PanelBox(page, text));
    }

    private void SetPage(string page)
    {
        title.Text = page;
        content.Controls.Clear();
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
}
