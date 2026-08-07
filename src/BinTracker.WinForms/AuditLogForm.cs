using BinTracker.Services;

namespace BinTracker.WinForms;

public sealed class AuditLogForm : Form
{
    private readonly IAuditService audit;
    private readonly Label countLabel = new() { AutoSize = true, ForeColor = Color.DimGray, TextAlign = ContentAlignment.MiddleLeft };
    private readonly DataGridView grid = new()
    {
        Dock = DockStyle.Fill,
        ReadOnly = true,
        AutoGenerateColumns = false,
        SelectionMode = DataGridViewSelectionMode.FullRowSelect,
        MultiSelect = false,
        RowHeadersVisible = false,
        AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells,
        AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None,
        ScrollBars = ScrollBars.Both,
        BackgroundColor = Color.White
    };

    public AuditLogForm(IAuditService audit)
    {
        this.audit = audit;
        Text = "Audit Trail";
        StartPosition = FormStartPosition.CenterParent;
        AutoScaleMode = AutoScaleMode.Dpi;
        ClientSize = new Size(1280, 720);
        MinimumSize = new Size(900, 520);

        grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Time (UTC)", DataPropertyName = "TimestampUtc", Width = 165 });
        grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "User", DataPropertyName = "Username", Width = 120 });
        grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Action", DataPropertyName = "Action", Width = 175 });
        grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Entity", DataPropertyName = "EntityType", Width = 125 });
        grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "ID", DataPropertyName = "EntityId", Width = 80 });
        grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Description",
            DataPropertyName = "Description",
            AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
            MinimumWidth = 260,
            DefaultCellStyle = new DataGridViewCellStyle { WrapMode = DataGridViewTriState.True }
        });
        grid.Columns.Add(new DataGridViewCheckBoxColumn
        {
            HeaderText = "Success",
            DataPropertyName = "Succeeded",
            Width = 80,
            MinimumWidth = 80,
            DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter }
        });

        var footer = new Panel { Dock = DockStyle.Bottom, Height = 34, Padding = new Padding(8, 7, 8, 0) };
        footer.Controls.Add(countLabel);
        Controls.Add(grid);
        Controls.Add(footer);
        Shown += async (_, _) => await ReloadAsync();
    }

    private async Task ReloadAsync()
    {
        var rows = (await audit.GetRecentAsync()).ToList();
        grid.DataSource = rows;
        countLabel.Text = $"Displaying {rows.Count:N0} audit event{(rows.Count == 1 ? string.Empty : "s")}.";
    }
}
