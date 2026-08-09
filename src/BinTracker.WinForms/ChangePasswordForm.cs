using BinTracker.Services;

namespace BinTracker.WinForms;

public sealed class ChangePasswordForm : Form
{
    private readonly IAuthenticationService auth;
    private readonly bool required;
    private readonly TextBox current = PasswordBox();
    private readonly TextBox next = PasswordBox();
    private readonly TextBox confirm = PasswordBox();
    private readonly Label strength = new() { AutoSize = true, ForeColor = Color.DimGray };
    private readonly Label error = new() { AutoSize = true, ForeColor = Color.Firebrick, MaximumSize = new Size(480, 0) };

    public ChangePasswordForm(IAuthenticationService auth, bool required = false)
    {
        this.auth = auth;
        this.required = required;

        Text = required ? "Change Password Required" : "Change Password";
        StartPosition = FormStartPosition.CenterParent;
        AutoScaleMode = AutoScaleMode.Dpi;
        ClientSize = new Size(640, 610);
        MinimumSize = new Size(600, 570);
        MaximizeBox = false;
        MinimizeBox = false;
        BackColor = Color.White;

        var actionBar = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 82,
            Padding = new Padding(28, 16, 28, 16)
        };

        var actions = new FlowLayoutPanel
        {
            Dock = DockStyle.Right,
            Width = required ? 180 : 330,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false
        };

        var save = new Button { Text = "Change password", AutoSize = false, Size = new Size(170, 46), Margin = new Padding(0, 0, 10, 0) };
        save.Click += async (_, _) => await SaveAsync();

        actions.Controls.Add(save);

        if (!required)
        {
            var cancel = new Button { Text = "Cancel", AutoSize = false, Size = new Size(130, 46), DialogResult = DialogResult.Cancel, Margin = new Padding(0) };
            actions.Controls.Add(cancel);
            CancelButton = cancel;
        }

        actionBar.Controls.Add(actions);

        var scroll = new Panel { Dock = DockStyle.Fill, AutoScroll = true, Padding = new Padding(30) };
        var form = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            ColumnCount = 1
        };

        form.Controls.Add(new Label
        {
            Text = required ? "Choose a new password" : "Change your password",
            AutoSize = true,
            Font = new Font("Segoe UI Semibold", 18F, FontStyle.Bold),
            Margin = new Padding(0, 0, 0, 8)
        });

        form.Controls.Add(new Label
        {
            Text = required
                ? "An administrator reset your password. You must choose your own password before continuing."
                : "Enter your current password, then choose a new one.",
            AutoSize = true,
            MaximumSize = new Size(500, 0),
            ForeColor = Color.DimGray,
            Margin = new Padding(0, 0, 0, 14)
        });

        AddField(form, "Current password", PasswordUi.WithVisibilityToggle(current));
        AddField(form, "New password", PasswordUi.WithVisibilityToggle(next));
        strength.Margin = new Padding(0, 2, 0, 8);
        form.Controls.Add(strength);
        AddField(form, "Confirm new password", PasswordUi.WithVisibilityToggle(confirm));

        form.Controls.Add(new Label
        {
            Text = "Minimum 10 characters with uppercase, lowercase and a number.",
            AutoSize = true,
            ForeColor = Color.DimGray,
            Margin = new Padding(0, 8, 0, 8)
        });

        form.Controls.Add(error);
        scroll.Controls.Add(form);

        Controls.Add(scroll);
        Controls.Add(actionBar);

        AcceptButton = save;
        next.TextChanged += (_, _) => strength.Text = $"Strength: {PasswordPolicy.StrengthText(next.Text)}";
        Shown += (_, _) => current.Focus();
    }

    private async Task SaveAsync()
    {
        error.Text = string.Empty;

        if (next.Text != confirm.Text)
        {
            error.Text = "The new passwords do not match.";
            return;
        }

        Enabled = false;
        try
        {
            await auth.ChangeOwnPasswordAsync(current.Text, next.Text);
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

    private static TextBox PasswordBox() => new()
    {
        Dock = DockStyle.Top,
        UseSystemPasswordChar = true,
        MinimumSize = new Size(0, 34)
    };

    private static void AddField(TableLayoutPanel panel, string label, Control control)
    {
        panel.Controls.Add(new Label { Text = label, AutoSize = true, Margin = new Padding(0, 10, 0, 4) });
        control.Margin = new Padding(0, 0, 0, 5);
        panel.Controls.Add(control);
    }
}
