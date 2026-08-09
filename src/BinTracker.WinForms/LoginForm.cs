using BinTracker.Services;

namespace BinTracker.WinForms;

public sealed class LoginForm : Form
{
    private readonly IAuthenticationService auth;
    private readonly TextBox username = new() { Dock = DockStyle.Fill };
    private readonly TextBox password = new() { Dock = DockStyle.Fill, UseSystemPasswordChar = true };
    private readonly Label error = new()
    {
        AutoSize = true,
        ForeColor = Color.Firebrick,
        MaximumSize = new Size(420, 70)
    };

    public LoginForm(IAuthenticationService auth)
    {
        this.auth = auth;

        Text = "BinTracker Login";
        StartPosition = FormStartPosition.CenterScreen;
        AutoScaleMode = AutoScaleMode.Dpi;
        ClientSize = new Size(520, 470);
        MinimumSize = new Size(460, 420);
        FormBorderStyle = FormBorderStyle.Sizable;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = true;
        BackColor = Color.White;

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
            MaximumSize = new Size(420, 0),
            Padding = new Padding(0)
        };
        fields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

        AddControl(fields, new Label
        {
            Text = "BinTracker",
            AutoSize = true,
            Font = new Font("Segoe UI Semibold", 24F, FontStyle.Bold),
            Margin = new Padding(0, 0, 0, 18)
        });

        AddField(fields, "Username", username);
        AddField(fields, "Password", PasswordUi.WithVisibilityToggle(password));

        error.Margin = new Padding(0, 8, 0, 0);
        AddControl(fields, error);
        scroll.Controls.Add(fields);

        var actions = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Padding = new Padding(0, 18, 0, 0)
        };

        var login = new Button
        {
            Text = "Log in",
            AutoSize = false,
            Size = new Size(150, 44),
            Margin = new Padding(0, 0, 10, 0),
            Padding = new Padding(18, 0, 18, 0)
        };
        login.Click += async (_, _) => await LoginAsync();

        var cancel = new Button
        {
            Text = "Cancel",
            AutoSize = false,
            Size = new Size(150, 44),
            DialogResult = DialogResult.Cancel,
            Margin = new Padding(0)
        };

        actions.Controls.Add(login);
        actions.Controls.Add(cancel);

        root.Controls.Add(scroll, 0, 0);
        root.Controls.Add(actions, 0, 1);
        Controls.Add(root);

        AcceptButton = login;
        CancelButton = cancel;
        Shown += (_, _) => username.Focus();
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

        control.Height = 34;
        control.Margin = new Padding(0, 0, 0, 4);
        AddControl(panel, control);
    }

    private async Task LoginAsync()
    {
        error.Text = string.Empty;
        Enabled = false;

        try
        {
            if (await auth.LoginAsync(username.Text, password.Text))
            {
                DialogResult = DialogResult.OK;
                Close();
            }
            else
            {
                error.Text = "Incorrect username or password.";
                password.SelectAll();
                password.Focus();
            }
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
