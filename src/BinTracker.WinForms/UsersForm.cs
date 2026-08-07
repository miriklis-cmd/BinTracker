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
        grid.Columns.Add(new DataGridViewCheckBoxColumn { HeaderText = "Active", DataPropertyName = "IsActive", Width = 80 });
        grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Last login (UTC)", DataPropertyName = "LastLoginUtc", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, MinimumWidth = 180 });

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
        var toggle = new Button { Text = "Activate / deactivate", AutoSize = true, MinimumSize = new Size(170, 38) };
        toggle.Click += async (_, _) => await ToggleAsync();

        buttons.Controls.Add(add);
        buttons.Controls.Add(toggle);
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

    public string Username => username.Text;
    public string DisplayName => display.Text;
    public string Password => password.Text;
    public UserRole Role => (UserRole)role.SelectedItem!;

    public AddUserForm()
    {
        Text = "Add user";
        StartPosition = FormStartPosition.CenterParent;
        AutoScaleMode = AutoScaleMode.Dpi;
        ClientSize = new Size(500, 470);
        MinimumSize = new Size(430, 400);
        FormBorderStyle = FormBorderStyle.Sizable;
        MaximizeBox = false;
        MinimizeBox = false;

        role.DataSource = Enum.GetValues<UserRole>();
        role.SelectedItem = UserRole.Operator;

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Padding = new Padding(24)
        };
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var scroll = new Panel { Dock = DockStyle.Fill, AutoScroll = true };
        var fields = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 1,
            RowCount = 0
        };
        fields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

        AddField(fields, "Username", username);
        AddField(fields, "Display name", display);
        AddField(fields, "Password", password);
        AddField(fields, "Role", role);
        scroll.Controls.Add(fields);

        var actions = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            Padding = new Padding(0, 16, 0, 0)
        };
        var save = new Button { Text = "Save", AutoSize = true, MinimumSize = new Size(120, 42) };
        save.Click += (_, _) => { DialogResult = DialogResult.OK; Close(); };
        var cancel = new Button { Text = "Cancel", AutoSize = true, MinimumSize = new Size(100, 42), DialogResult = DialogResult.Cancel, Margin = new Padding(10, 0, 0, 0) };
        actions.Controls.Add(save);
        actions.Controls.Add(cancel);

        root.Controls.Add(scroll, 0, 0);
        root.Controls.Add(actions, 0, 1);
        Controls.Add(root);

        AcceptButton = save;
        CancelButton = cancel;
    }

    private static void AddField(TableLayoutPanel panel, string text, Control control)
    {
        panel.RowCount++;
        panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        panel.Controls.Add(new Label { Text = text, AutoSize = true, Margin = new Padding(0, 10, 0, 4) }, 0, panel.RowCount - 1);

        panel.RowCount++;
        panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        control.MinimumSize = new Size(0, 34);
        control.Margin = new Padding(0, 0, 0, 4);
        panel.Controls.Add(control, 0, panel.RowCount - 1);
    }
}
