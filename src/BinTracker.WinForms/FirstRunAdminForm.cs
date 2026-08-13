using BinTracker.Services;

namespace BinTracker.WinForms;

public sealed class FirstRunAdminForm : Form
{
    private readonly IAuthenticationService auth;
    private readonly TextBox username = new() { Dock = DockStyle.Fill, Text = "admin" };
    private readonly TextBox displayName = new() { Dock = DockStyle.Fill };
    private readonly TextBox password = new() { Dock = DockStyle.Fill, UseSystemPasswordChar = true };
    private readonly TextBox confirm = new() { Dock = DockStyle.Fill, UseSystemPasswordChar = true };
    private readonly Label error = new()
    {
        AutoSize = true,
        ForeColor = Color.Firebrick,
        MaximumSize = new Size(520, 80)
    };

    public FirstRunAdminForm(IAuthenticationService auth)
    {
        this.auth = auth;

        Text = "Set up BinTracker Administrator";
        StartPosition = FormStartPosition.CenterScreen;
        AutoScaleMode = AutoScaleMode.Dpi;
        ClientSize = new Size(620, 680);
        MinimumSize = new Size(540, 600);
        FormBorderStyle = FormBorderStyle.Sizable;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Padding = new Padding(28),
            BackColor = Color.White
        };
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var scroll = new Panel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            Padding = new Padding(18, 8, 18, 8)
        };

        var fields = new TableLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 1,
            RowCount = 0,
            Dock = DockStyle.Top,
            MaximumSize = new Size(520, 0),
            Padding = new Padding(0)
        };
        fields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

        AddControl(fields, new Label
        {
            Text = "Create administrator",
            AutoSize = true,
            Font = new Font("Segoe UI Semibold", 22F, FontStyle.Bold),
            Margin = new Padding(0, 0, 0, 10)
        });

        AddControl(fields, new Label
        {
            Text = "This account controls users, settings and audit records.",
            AutoSize = true,
            MaximumSize = new Size(520, 0),
            Margin = new Padding(0, 0, 0, 20)
        });

        AddField(fields, "Username", username);
        AddField(fields, "Display name", displayName);
        AddField(fields, "Password", PasswordUi.WithVisibilityToggle(password));
        AddField(fields, "Confirm password", PasswordUi.WithVisibilityToggle(confirm));

        AddControl(fields, new Label
        {
            Text = "At least 10 characters with uppercase, lowercase and a number.",
            AutoSize = true,
            MaximumSize = new Size(520, 0),
            ForeColor = Color.DimGray,
            Margin = new Padding(0, 4, 0, 12)
        });

        AddControl(fields, error);
        scroll.Controls.Add(fields);

        var actions = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            ColumnCount = 3,
            RowCount = 1,
            Padding = new Padding(0, 18, 0, 0),
            Margin = Padding.Empty
        };
        actions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        actions.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120F));
        actions.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 210F));
        actions.RowStyles.Add(new RowStyle(SizeType.Absolute, 46F));

        var cancel = new Button
        {
            Text = "Cancel",
            Dock = DockStyle.Fill,
            DialogResult = DialogResult.Cancel,
            Margin = new Padding(0, 0, 10, 0)
        };

        var save = new Button
        {
            Text = "Create administrator",
            Dock = DockStyle.Fill,
            Margin = Padding.Empty
        };
        save.Click += async (_, _) => await SaveAsync();

        actions.Controls.Add(cancel, 1, 0);
        actions.Controls.Add(save, 2, 0);

        root.Controls.Add(scroll, 0, 0);
        root.Controls.Add(actions, 0, 1);
        Controls.Add(root);

        AcceptButton = save;
        CancelButton = cancel;
    }

    private static void AddControl(TableLayoutPanel panel, Control control)
    {
        panel.RowCount++;
        panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        panel.Controls.Add(control, 0, panel.RowCount - 1);
    }

    private static void AddField(TableLayoutPanel panel, string labelText, Control control)
    {
        AddControl(panel, new Label
        {
            Text = labelText,
            AutoSize = true,
            Margin = new Padding(0, 10, 0, 4)
        });

        control.Height = 32;
        control.Margin = new Padding(0, 0, 0, 4);
        AddControl(panel, control);
    }

    private async Task SaveAsync()
    {
        error.Text = "";

        if (password.Text != confirm.Text)
        {
            error.Text = "Passwords do not match.";
            return;
        }

        Enabled = false;
        try
        {
            await auth.CreateInitialAdministratorAsync(
                username.Text,
                displayName.Text,
                password.Text);

            DialogResult = DialogResult.OK;
            Close();
        }
        catch (Exception ex)
        {
            error.Text = ex.Message;
        }
        finally
        {
            Enabled = true;
        }
    }
}
