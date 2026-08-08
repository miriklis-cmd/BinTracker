using BinTracker.Core;
using BinTracker.Services;

namespace BinTracker.WinForms;

public sealed class UsersForm : Form
{
    private readonly IUserService users;
    private readonly DataGridView grid = new()
    {
        Dock = DockStyle.Fill,
        ReadOnly = true,
        AutoGenerateColumns = false,
        SelectionMode = DataGridViewSelectionMode.FullRowSelect,
        MultiSelect = false,
        AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells,
        RowHeadersVisible = false
    };

    public UsersForm(IUserService users)
    {
        this.users = users;
        Text = "Users";
        StartPosition = FormStartPosition.CenterParent;
        AutoScaleMode = AutoScaleMode.Dpi;
        ClientSize = new Size(900, 560);
        MinimumSize = new Size(720, 460);

        grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Username", DataPropertyName = "Username", Width = 160 });
        grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Display name", DataPropertyName = "DisplayName", Width = 220 });
        grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Role", DataPropertyName = "Role", Width = 130 });
        grid.Columns.Add(new DataGridViewCheckBoxColumn { HeaderText = "Active", DataPropertyName = "IsActive", Width = 75 });
        grid.Columns.Add(new DataGridViewCheckBoxColumn { HeaderText = "Locked", DataPropertyName = "IsLocked", Width = 75 });
        grid.Columns.Add(new DataGridViewCheckBoxColumn { HeaderText = "Change password", DataPropertyName = "MustChangePassword", Width = 115 });
        grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Last login (UTC)", DataPropertyName = "LastLoginUtc", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, MinimumWidth = 170 });

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Padding = new Padding(12)
        };
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true,
            Padding = new Padding(0, 0, 0, 10)
        };

        var add = new Button { Text = "Add user", AutoSize = true, MinimumSize = new Size(120, 38) };
        add.Click += async (_, _) => await AddAsync();
        var toggle = new Button { Text = "Activate / deactivate", AutoSize = false, Size = new Size(180, 40) };
        toggle.Click += async (_, _) => await ToggleAsync();

        var reset = new Button { Text = "Reset password", AutoSize = false, Size = new Size(150, 40) };
        reset.Click += async (_, _) => await ResetPasswordAsync();

        var lockToggle = new Button
        {
            Text = "Lock / Unlock",
            AutoSize = false,
            Size = new Size(140, 40)
        };
        lockToggle.Click += async (_, _) => await ToggleLockAsync();

        buttons.Controls.Add(add);
        buttons.Controls.Add(toggle);
        buttons.Controls.Add(reset);
        buttons.Controls.Add(lockToggle);
        root.Controls.Add(buttons, 0, 0);
        root.Controls.Add(grid, 0, 1);
        Controls.Add(root);

        Shown += async (_, _) => await ReloadAsync();
    }

    private async Task ReloadAsync() => grid.DataSource = (await users.GetUsersAsync()).ToList();

    private async Task AddAsync()
    {
        using var form = new AddUserForm();
        if (form.ShowDialog(this) != DialogResult.OK) return;

        try
        {
            await users.CreateUserAsync(form.Username, form.DisplayName, form.Password, form.Role);
            await ReloadAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Add user", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }


    private async Task ResetPasswordAsync()
    {
        if (grid.CurrentRow?.DataBoundItem is not UserAccount user) return;

        using var form = new ResetPasswordForm(user.Username);
        if (form.ShowDialog(this) != DialogResult.OK) return;

        try
        {
            await users.ResetPasswordAsync(user.Id, form.TemporaryPassword);
            MessageBox.Show(
                "Password reset. The user must change the temporary password at next login.",
                "Users",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            await ReloadAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Reset password", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async Task ToggleLockAsync()
    {
        if (grid.CurrentRow?.DataBoundItem is not UserAccount user) return;

        try
        {
            await users.SetLockedAsync(user.Id, !user.IsLocked);
            await ReloadAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Lock / Unlock user", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async Task ToggleAsync()
    {
        if (grid.CurrentRow?.DataBoundItem is not UserAccount user) return;

        try
        {
            await users.SetActiveAsync(user.Id, !user.IsActive);
            await ReloadAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Users", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}


internal sealed class AddUserForm : Form
{
    private readonly TextBox username = new() { Dock = DockStyle.Fill };
    private readonly TextBox display = new() { Dock = DockStyle.Fill };
    private readonly TextBox password = new() { Dock = DockStyle.Fill, UseSystemPasswordChar = true };
    private readonly ComboBox role = new() { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly Label strength = new() { AutoSize = true, ForeColor = Color.DimGray };

    public string Username => username.Text;
    public string DisplayName => display.Text;
    public string Password => password.Text;
    public UserRole Role => (UserRole)role.SelectedItem!;

    public AddUserForm()
    {
        Text = "Add User";
        StartPosition = FormStartPosition.CenterParent;
        AutoScaleMode = AutoScaleMode.Dpi;
        ClientSize = new Size(560, 540);
        MinimumSize = new Size(520, 500);
        FormBorderStyle = FormBorderStyle.Sizable;
        MaximizeBox = false;
        MinimizeBox = false;
        BackColor = Color.White;

        role.DataSource = Enum.GetValues<UserRole>();
        role.SelectedItem = UserRole.Operator;

        var actionBar = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 82,
            Padding = new Padding(28, 16, 28, 16),
            BackColor = SystemColors.Control
        };

        var actions = new FlowLayoutPanel
        {
            Dock = DockStyle.Left,
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Margin = new Padding(0),
            Padding = new Padding(0)
        };

        var save = new Button
        {
            Text = "Save",
            AutoSize = false,
            Size = new Size(140, 46),
            Margin = new Padding(0, 0, 10, 0)
        };

        var cancel = new Button
        {
            Text = "Cancel",
            AutoSize = false,
            Size = new Size(140, 46),
            DialogResult = DialogResult.Cancel,
            Margin = new Padding(0)
        };

        save.Click += (_, _) =>
        {
            try
            {
                PasswordPolicy.Validate(password.Text);
                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Add user", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        };

        actions.Controls.Add(save);
        actions.Controls.Add(cancel);
        actionBar.Controls.Add(actions);

        var body = new Panel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            Padding = new Padding(30),
            BackColor = Color.White
        };

        var fields = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 1
        };

        fields.Controls.Add(new Label
        {
            Text = "Add user",
            AutoSize = true,
            Font = new Font("Segoe UI Semibold", 18F, FontStyle.Bold),
            Margin = new Padding(0, 0, 0, 14)
        });

        AddField(fields, "Username", username);
        AddField(fields, "Display name", display);
        AddField(fields, "Temporary password", password);

        strength.Margin = new Padding(0, 0, 0, 8);
        fields.Controls.Add(strength);

        fields.Controls.Add(new Label
        {
            Text = "The user will be required to change this password at first login.",
            AutoSize = true,
            ForeColor = Color.DimGray,
            MaximumSize = new Size(470, 0),
            Margin = new Padding(0, 0, 0, 8)
        });

        AddField(fields, "Role", role);

        body.Controls.Add(fields);
        Controls.Add(body);
        Controls.Add(actionBar);

        AcceptButton = save;
        CancelButton = cancel;

        password.TextChanged += (_, _) =>
            strength.Text = $"Strength: {PasswordPolicy.StrengthText(password.Text)}";
    }

    private static void AddField(TableLayoutPanel panel, string label, Control control)
    {
        panel.Controls.Add(new Label
        {
            Text = label,
            AutoSize = true,
            Margin = new Padding(0, 10, 0, 4)
        });

        control.MinimumSize = new Size(0, 36);
        control.Margin = new Padding(0, 0, 0, 6);
        panel.Controls.Add(control);
    }
}


internal sealed class ResetPasswordForm : Form
{
    private readonly TextBox password = new() { Dock = DockStyle.Fill, UseSystemPasswordChar = true };
    private readonly TextBox confirm = new() { Dock = DockStyle.Fill, UseSystemPasswordChar = true };
    private readonly Label strength = new() { AutoSize = true, ForeColor = Color.DimGray };

    public string TemporaryPassword => password.Text;

    public ResetPasswordForm(string username)
    {
        Text = "Reset Password";
        StartPosition = FormStartPosition.CenterParent;
        AutoScaleMode = AutoScaleMode.Dpi;
        ClientSize = new Size(600, 500);
        MinimumSize = new Size(560, 470);
        MaximizeBox = false;
        MinimizeBox = false;
        BackColor = Color.White;

        var actionBar = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 82,
            Padding = new Padding(28, 16, 28, 16),
            BackColor = SystemColors.Control
        };

        var actions = new FlowLayoutPanel
        {
            Dock = DockStyle.Left,
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false
        };

        var save = new Button
        {
            Text = "Reset Password",
            AutoSize = false,
            Size = new Size(165, 46),
            Margin = new Padding(0, 0, 10, 0)
        };

        var cancel = new Button
        {
            Text = "Cancel",
            AutoSize = false,
            Size = new Size(130, 46),
            DialogResult = DialogResult.Cancel,
            Margin = new Padding(0)
        };

        actions.Controls.Add(save);
        actions.Controls.Add(cancel);
        actionBar.Controls.Add(actions);

        var body = new Panel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            Padding = new Padding(30),
            BackColor = Color.White
        };

        var form = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 1
        };

        form.Controls.Add(new Label
        {
            Text = "Reset Password",
            AutoSize = true,
            Font = new Font("Segoe UI Semibold", 18F, FontStyle.Bold),
            Margin = new Padding(0, 0, 0, 10)
        });

        form.Controls.Add(new Label
        {
            Text = "User",
            AutoSize = true,
            ForeColor = Color.DimGray,
            Margin = new Padding(0, 0, 0, 2)
        });

        form.Controls.Add(new Label
        {
            Text = username,
            AutoSize = true,
            Font = new Font("Segoe UI Semibold", 11F, FontStyle.Bold),
            MaximumSize = new Size(500, 0),
            Margin = new Padding(0, 0, 0, 14)
        });

        form.Controls.Add(new Label
        {
            Text = "Set a temporary password. The user will be required to change it immediately after their next login.",
            AutoSize = true,
            MaximumSize = new Size(500, 0),
            ForeColor = Color.DimGray,
            Margin = new Padding(0, 0, 0, 12)
        });

        AddField(form, "Temporary password", password);
        strength.Margin = new Padding(0, 0, 0, 8);
        form.Controls.Add(strength);
        AddField(form, "Confirm password", confirm);

        body.Controls.Add(form);

        save.Click += (_, _) =>
        {
            if (password.Text != confirm.Text)
            {
                MessageBox.Show("The passwords do not match.", "Reset password",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                PasswordPolicy.Validate(password.Text);
                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Reset password",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        };

        password.TextChanged += (_, _) =>
            strength.Text = $"Strength: {PasswordPolicy.StrengthText(password.Text)}";

        Controls.Add(body);
        Controls.Add(actionBar);

        AcceptButton = save;
        CancelButton = cancel;
    }

    private static void AddField(TableLayoutPanel panel, string label, Control control)
    {
        panel.Controls.Add(new Label
        {
            Text = label,
            AutoSize = true,
            Margin = new Padding(0, 10, 0, 4)
        });

        control.MinimumSize = new Size(0, 36);
        control.Margin = new Padding(0, 0, 0, 6);
        panel.Controls.Add(control);
    }
}
