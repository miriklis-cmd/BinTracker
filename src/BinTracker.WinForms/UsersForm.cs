using BinTracker.Core;
using BinTracker.Services;

namespace BinTracker.WinForms;

public sealed class UsersForm : Form
{
    private sealed record UserGridRow(
        int Id,
        string Username,
        string DisplayName,
        string Role,
        string Status,
        string LastLogin,
        UserAccount Source);

    private readonly IUserService users;

    private readonly Button toggleActive = ActionButton("Deactivate");
    private readonly Button toggleLock = ActionButton("Lock");

    private readonly DataGridView grid = new()
    {
        Dock = DockStyle.Fill,
        ReadOnly = true,
        AutoGenerateColumns = false,
        SelectionMode = DataGridViewSelectionMode.FullRowSelect,
        MultiSelect = false,
        AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells,
        RowHeadersVisible = false,
        BackgroundColor = Color.White,
        BorderStyle = BorderStyle.FixedSingle,
        AllowUserToAddRows = false,
        AllowUserToDeleteRows = false
    };

    public UsersForm(IUserService users)
    {
        this.users = users;

        Text = "Users";
        StartPosition = FormStartPosition.CenterParent;
        AutoScaleMode = AutoScaleMode.Dpi;
        ClientSize = new Size(1060, 640);
        MinimumSize = new Size(880, 540);

        grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "Username",
            HeaderText = "Username",
            DataPropertyName = nameof(UserGridRow.Username),
            Width = 140
        });

        grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "DisplayName",
            HeaderText = "Display name",
            DataPropertyName = nameof(UserGridRow.DisplayName),
            Width = 185
        });

        grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "Role",
            HeaderText = "Role",
            DataPropertyName = nameof(UserGridRow.Role),
            Width = 155
        });

        grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "Status",
            HeaderText = "Status",
            DataPropertyName = nameof(UserGridRow.Status),
            AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
            MinimumWidth = 230
        });

        grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "LastLogin",
            HeaderText = "Last login",
            DataPropertyName = nameof(UserGridRow.LastLogin),
            Width = 165
        });

        grid.CellFormatting += GridCellFormatting;
        grid.SelectionChanged += (_, _) => UpdateContextButtons();

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Padding = new Padding(14)
        };

        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true,
            Padding = new Padding(0, 0, 0, 12),
            Margin = new Padding(0)
        };

        var add = ActionButton("Add User");
        add.Click += async (_, _) => await AddAsync();

        toggleActive.Click += async (_, _) => await ToggleActiveAsync();

        var editRole = ActionButton("Change Role");
        editRole.Click += async (_, _) => await ChangeRoleAsync();

        var reset = ActionButton("Reset Password");
        reset.Click += async (_, _) => await ResetPasswordAsync();

        toggleLock.Click += async (_, _) => await ToggleLockAsync();

        buttons.Controls.Add(add);
        buttons.Controls.Add(toggleActive);
        buttons.Controls.Add(editRole);
        buttons.Controls.Add(reset);
        buttons.Controls.Add(toggleLock);

        root.Controls.Add(buttons, 0, 0);
        root.Controls.Add(grid, 0, 1);

        Controls.Add(root);

        Shown += async (_, _) => await ReloadAsync();
    }

    private async Task ReloadAsync()
    {
        var source = await users.GetUsersAsync();

        grid.DataSource = source
            .Select(user => new UserGridRow(
                user.Id,
                user.Username,
                user.DisplayName,
                user.Role.ToString(),
                StatusText(user),
                user.LastLoginUtc?.ToLocalTime().ToString("dd/MM/yyyy HH:mm") ?? "Never",
                user))
            .ToList();

        UpdateContextButtons();
    }

    private void GridCellFormatting(
        object? sender,
        DataGridViewCellFormattingEventArgs e)
    {
        if (e.RowIndex < 0 || e.ColumnIndex < 0)
            return;

        if (grid.Rows[e.RowIndex].DataBoundItem is not UserGridRow row)
            return;

        var column = grid.Columns[e.ColumnIndex].Name;

        if (column == "Status")
        {
            var style = e.CellStyle ?? new DataGridViewCellStyle();
            e.CellStyle = style;

            style.Font = new Font(grid.Font, FontStyle.Bold);
            style.ForeColor = row.Status switch
            {
                "Active" => Color.ForestGreen,
                "Password Reset Required" => Color.DarkOrange,
                "Locked" => Color.Firebrick,
                "Inactive" => Color.DimGray,
                _ => grid.ForeColor
            };

            return;
        }

        if (column != "Role")
            return;

        var role = row.Source.Role;
        var roleStyle = e.CellStyle ?? new DataGridViewCellStyle();
        e.CellStyle = roleStyle;

        roleStyle.Font = new Font(grid.Font, FontStyle.Bold);
        roleStyle.ForeColor = role switch
        {
            UserRole.Administrator => Color.RoyalBlue,
            UserRole.Operator => Color.SeaGreen,
            _ => Color.DimGray
        };
    }

    private void UpdateContextButtons()
    {
        var selected = SelectedUser();

        if (selected is null)
        {
            toggleActive.Text = "Deactivate";
            toggleLock.Text = "Lock";
            toggleActive.Enabled = false;
            toggleLock.Enabled = false;
            return;
        }

        toggleActive.Enabled = true;
        toggleLock.Enabled = true;

        toggleActive.Text = selected.IsActive ? "Deactivate" : "Activate";
        toggleLock.Text = selected.IsLocked ? "Unlock" : "Lock";
    }

    private async Task AddAsync()
    {
        using var form = new AddUserForm();

        if (form.ShowDialog(this) != DialogResult.OK)
            return;

        try
        {
            await users.CreateUserAsync(
                form.Username,
                form.DisplayName,
                form.Password,
                form.Role);

            await ReloadAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Add user", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async Task ToggleActiveAsync()
    {
        if (SelectedUser() is not { } user)
            return;

        try
        {
            await users.SetActiveAsync(user.Id, !user.IsActive);
            await ReloadAsync();
            SelectUser(user.Id);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Users", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async Task ChangeRoleAsync()
    {
        if (SelectedUser() is not { } user)
            return;

        using var form = new ChangeRoleForm(user.Username, user.Role);

        if (form.ShowDialog(this) != DialogResult.OK)
            return;

        try
        {
            await users.SetRoleAsync(user.Id, form.SelectedRole);
            await ReloadAsync();
            SelectUser(user.Id);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Change role", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async Task ResetPasswordAsync()
    {
        if (SelectedUser() is not { } user)
            return;

        using var form = new ResetPasswordForm(user.Username);

        if (form.ShowDialog(this) != DialogResult.OK)
            return;

        try
        {
            await users.ResetPasswordAsync(user.Id, form.TemporaryPassword);

            MessageBox.Show(
                "Password reset. The user must change the temporary password at next login.",
                "Users",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);

            await ReloadAsync();
            SelectUser(user.Id);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Reset password", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async Task ToggleLockAsync()
    {
        if (SelectedUser() is not { } user)
            return;

        try
        {
            await users.SetLockedAsync(user.Id, !user.IsLocked);
            await ReloadAsync();
            SelectUser(user.Id);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Lock / Unlock user", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private UserAccount? SelectedUser() =>
        grid.CurrentRow?.DataBoundItem is UserGridRow row
            ? row.Source
            : null;

    private void SelectUser(int id)
    {
        foreach (DataGridViewRow row in grid.Rows)
        {
            if (row.DataBoundItem is UserGridRow item && item.Id == id)
            {
                row.Selected = true;
                grid.CurrentCell = row.Cells[0];
                UpdateContextButtons();
                return;
            }
        }
    }

    private static string StatusText(UserAccount user)
    {
        if (!user.IsActive)
            return "Inactive";

        if (user.IsLocked)
            return "Locked";

        if (user.MustChangePassword)
            return "Password Reset Required";

        return "Active";
    }

    private static Button ActionButton(string text) => new()
    {
        Text = text,
        AutoSize = false,
        Size = new Size(150, 40),
        Margin = new Padding(0, 0, 10, 0)
    };
}


internal sealed class ChangeRoleForm : Form
{
    private readonly ComboBox role = new()
    {
        Dock = DockStyle.Fill,
        DropDownStyle = ComboBoxStyle.DropDownList
    };

    public UserRole SelectedRole => (UserRole)role.SelectedItem!;

    public ChangeRoleForm(string username, UserRole currentRole)
    {
        Text = "Change User Role";
        StartPosition = FormStartPosition.CenterParent;
        AutoScaleMode = AutoScaleMode.Dpi;
        ClientSize = new Size(520, 320);
        MinimumSize = new Size(500, 300);
        MaximizeBox = false;
        MinimizeBox = false;
        BackColor = Color.White;

        role.DataSource = Enum.GetValues<UserRole>();
        role.SelectedItem = currentRole;

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
            Text = "Save Role",
            AutoSize = false,
            Size = new Size(145, 46),
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

        save.Click += (_, _) =>
        {
            DialogResult = DialogResult.OK;
            Close();
        };

        actions.Controls.Add(save);
        actions.Controls.Add(cancel);
        actionBar.Controls.Add(actions);

        var body = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = false,
            ColumnCount = 1,
            RowCount = 4,
            Padding = new Padding(30)
        };

        body.Controls.Add(new Label
        {
            Text = "Change user role",
            AutoSize = true,
            Font = new Font("Segoe UI Semibold", 18F, FontStyle.Bold),
            Margin = new Padding(0, 0, 0, 8)
        });

        body.Controls.Add(new Label
        {
            Text = username,
            AutoSize = true,
            Font = new Font("Segoe UI Semibold", 11F, FontStyle.Bold),
            Margin = new Padding(0, 0, 0, 16)
        });

        body.Controls.Add(new Label
        {
            Text = "Role",
            AutoSize = true,
            Margin = new Padding(0, 0, 0, 4)
        });

        body.Controls.Add(role);

        Controls.Add(body);
        Controls.Add(actionBar);

        AcceptButton = save;
        CancelButton = cancel;
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
        ClientSize = new Size(580, 620);
        MinimumSize = new Size(540, 580);
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
            AutoScroll = false,
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
