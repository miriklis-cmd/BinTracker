using BinTracker.Core;
using BinTracker.Data;
using BinTracker.Services;

namespace BinTracker.WinForms;

public sealed class MainForm : Form
{
    private readonly Label title = new();
    private readonly Panel content = new();
    private readonly UserSession session;
    private readonly IUserService users;
    private readonly IAuditService audit;
    private readonly ICustomerService customers;
    private readonly ICustomerStatementReportService statementReports;
    private readonly IAuthenticationService auth;

    public MainForm(UserSession session, IUserService users, IAuditService audit, ICustomerService customers, ICustomerStatementReportService statementReports, IAuthenticationService auth)
    {
        this.session = session;
        this.users = users;
        this.audit = audit;
        this.customers = customers;
        this.statementReports = statementReports;
        this.auth = auth;

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

        side.Controls.Add(Nav("Settings", ShowSettings));
        side.Controls.Add(Nav("Reports", () => Placeholder("Reports", "Report generation will record the user, filters, dates and export format in the audit trail.")));
        side.Controls.Add(Nav("Single Entry", () => Placeholder("Single Entry", "Record one IN (Returned) or OUT (Taken) movement.")));
        side.Controls.Add(Nav("Batch Entry", () => Placeholder("Batch Entry", "Enter a whole day of returned bins, then taken bins.")));
        side.Controls.Add(Nav("Customers", ShowCustomers));
        side.Controls.Add(Nav("Dashboard", ShowDashboard));
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
            Height = 76,
            BackColor = Color.White,
            Padding = new Padding(24, 12, 24, 10),
            ColumnCount = 2,
            RowCount = 1
        };
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60F));
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40F));

        title.Dock = DockStyle.Fill;
        title.AutoSize = false;
        title.Font = new Font("Segoe UI Semibold", 20F, FontStyle.Bold);
        title.ForeColor = Color.FromArgb(29, 39, 54);
        title.TextAlign = ContentAlignment.MiddleLeft;
        title.AutoEllipsis = true;

        var signedIn = new Label
        {
            Text = $"Signed in: {session.DisplayName} ({session.Role})",
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleRight,
            ForeColor = Color.DimGray,
            AutoEllipsis = true
        };

        header.Controls.Add(title, 0, 0);
        header.Controls.Add(signedIn, 1, 0);

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

    private Button Nav(string text, Action action)
    {
        var button = new Button
        {
            Text = text,
            Dock = DockStyle.Top,
            Height = 50,
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(29, 39, 54),
            ForeColor = Color.White,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(12, 0, 0, 0),
            Cursor = Cursors.Hand,
            AutoEllipsis = true
        };

        button.FlatAppearance.BorderSize = 0;
        button.Click += (_, _) => action();
        return button;
    }

    private void ShowDashboard()
    {
        SetPage("Dashboard");

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

        cards.Controls.Add(Card("Returned Today", "0", "IN"), 0, 0);
        cards.Controls.Add(Card("Taken Today", "0", "OUT"), 1, 0);
        cards.Controls.Add(Card("Outstanding", "0", "Calculated from movement history"), 2, 0);
        cards.Controls.Add(Card("Requires Attention", "0", "Over 20 outstanding or older than 7 days"), 3, 0);

        var info = PanelBox(
            "Security and audit enabled",
            $"Signed in as {session.DisplayName}. Logins, failed logins, user administration and future customer, movement, settings, backup and report actions are recorded in the append-only audit trail.");

        page.Controls.Add(cards, 0, 0);
        page.Controls.Add(info, 0, 1);
        content.Controls.Add(page);
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

        var stack = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false
        };

        var profile = PanelBox(
            "My Profile",
            $"Username: {session.Username}\nRole: {session.Role}\nLogged in: {(session.LoginUtc?.ToLocalTime().ToString("dd/MM/yyyy HH:mm") ?? "Unknown")}\nSession ID: {session.SessionId}");

        var profileActions = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            Padding = new Padding(0, 18, 0, 0)
        };

        var passwordButton = new Button
        {
            Text = "Change Password",
            AutoSize = false,
            Size = new Size(165, 44)
        };

        passwordButton.Click += (_, _) =>
        {
            using var form = new ChangePasswordForm(auth);
            form.ShowDialog(this);
        };

        profileActions.Controls.Add(passwordButton);
        profile.Controls.Add(profileActions);
        stack.Controls.Add(profile);

        var admin = PanelBox("Administration", "Manage authorised users and inspect the audit trail.");
        var actions = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true,
            Padding = new Padding(0, 18, 0, 0)
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
                MaximumSize = new Size(720, 0)
            });
        }

        admin.Controls.Add(actions);
        stack.Controls.Add(admin);
        content.Controls.Add(stack);
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
