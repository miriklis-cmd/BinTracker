using BinTracker.Services;

namespace BinTracker.WinForms;

public sealed class CustomerDecisionForm : BinTrackerForm
{
    private readonly ImportReviewPlan review;
    private readonly Dictionary<string, ImportCustomerDecision> decisions;
    private readonly DataGridView grid = new()
    {
        Dock = DockStyle.Fill,
        AllowUserToAddRows = false,
        AllowUserToDeleteRows = false,
        RowHeadersVisible = false,
        AutoGenerateColumns = false,
        BackgroundColor = Color.White,
        SelectionMode = DataGridViewSelectionMode.FullRowSelect,
        MultiSelect = true
    };

    public IReadOnlyDictionary<string, ImportCustomerDecision> Decisions => decisions;

    public CustomerDecisionForm(ImportReviewPlan review, IReadOnlyDictionary<string, ImportCustomerDecision> existing)
    {
        this.review = review;
        decisions = new Dictionary<string, ImportCustomerDecision>(ImportCustomerDecisionPlanner.MergeDefaults(review, existing), StringComparer.OrdinalIgnoreCase);
        Text = "Confirm New Customers";
        StartPosition = FormStartPosition.CenterParent;
        AutoScaleMode = AutoScaleMode.Dpi;
        ClientSize = new Size(1080, 720);
        MinimumSize = new Size(980, 640);
        BackColor = Color.FromArgb(245, 247, 250);
        Font = new Font("Segoe UI", 10F);
        Build();
        LoadRows();
    }

    private void Build()
    {
        var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 4, Padding = new Padding(16) };
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.Controls.Add(new Label { Text = "Confirm new customers", AutoSize = true, Font = new Font("Segoe UI Semibold", 18F, FontStyle.Bold) }, 0, 0);
        root.Controls.Add(new Label { Text = "Edit the proposed customer name if required, then explicitly choose Create or Skip. Unconfirmed customers block reconciliation; skipped customers are deliberately excluded.", AutoSize = true, ForeColor = Color.DimGray, MaximumSize = new Size(1020, 0), Margin = new Padding(0, 6, 0, 12) }, 0, 1);
        ConfigureGrid(); root.Controls.Add(grid, 0, 2); root.Controls.Add(BuildFooter(), 0, 3); Controls.Add(root);
    }

    private void ConfigureGrid()
    {
        grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Code", HeaderText = "Imported code", ReadOnly = true, FillWeight = 110, MinimumWidth = 150 });
        grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Name", HeaderText = "Proposed BinTracker name", FillWeight = 180, MinimumWidth = 240 });
        var action = new DataGridViewComboBoxColumn { Name = "Action", HeaderText = "Decision", FillWeight = 90, MinimumWidth = 130, ValueType = typeof(ImportCustomerDecisionAction) };
        action.Items.AddRange(ImportCustomerDecisionAction.Unconfirmed, ImportCustomerDecisionAction.Create, ImportCustomerDecisionAction.Skip); grid.Columns.Add(action);
        grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Source", HeaderText = "Source worksheet(s)", ReadOnly = true, FillWeight = 120, MinimumWidth = 170 });
    }

    private void LoadRows()
    {
        grid.Rows.Clear();
        foreach (var customer in review.Customers.Where(x => x.Status == ImportCustomerReviewStatus.New).OrderBy(x => x.CustomerCode, StringComparer.OrdinalIgnoreCase))
        {
            var d = decisions[customer.CustomerCode];
            grid.Rows.Add(customer.CustomerCode, d.ProposedName, d.Action, customer.SourceWorksheets);
        }
    }

    private Control BuildFooter()
    {
        var footer = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, ColumnCount = 2, Margin = new Padding(0, 12, 0, 0) };
        footer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F)); footer.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        var bulk = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.LeftToRight, WrapContents = false };
        var selectedCreate = Btn("Selected → Create", 195); selectedCreate.Click += (_, _) => SetSelected(ImportCustomerDecisionAction.Create);
        var selectedSkip = Btn("Selected → Skip", 185); selectedSkip.Click += (_, _) => SetSelected(ImportCustomerDecisionAction.Skip);
        var allCreate = Btn("All → Create", 150); allCreate.Click += (_, _) => SetAll(ImportCustomerDecisionAction.Create);
        bulk.Controls.Add(selectedCreate); bulk.Controls.Add(selectedSkip); bulk.Controls.Add(allCreate);
        var right = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.RightToLeft, WrapContents = false };
        var cancel = Btn("Cancel", 110); cancel.Click += (_, _) => { DialogResult = DialogResult.Cancel; Close(); };
        var apply = Btn("Apply decisions", 150); apply.Click += (_, _) => { SaveRows(); DialogResult = DialogResult.OK; Close(); };
        right.Controls.Add(cancel); right.Controls.Add(apply); footer.Controls.Add(bulk, 0, 0); footer.Controls.Add(right, 1, 0); return footer;
    }

    private void SetSelected(ImportCustomerDecisionAction action) { foreach (DataGridViewRow row in grid.SelectedRows) row.Cells["Action"].Value = action; }
    private void SetAll(ImportCustomerDecisionAction action) { foreach (DataGridViewRow row in grid.Rows) row.Cells["Action"].Value = action; }

    private void SaveRows()
    {
        foreach (DataGridViewRow row in grid.Rows)
        {
            if (row.Cells["Code"].Value is not string code) continue;
            var name = row.Cells["Name"].Value?.ToString()?.Trim() ?? string.Empty;
            var action = row.Cells["Action"].Value is ImportCustomerDecisionAction typed ? typed : Enum.TryParse<ImportCustomerDecisionAction>(row.Cells["Action"].Value?.ToString(), out var parsed) ? parsed : ImportCustomerDecisionAction.Unconfirmed;
            if (action == ImportCustomerDecisionAction.Create && name.Length == 0) throw new InvalidOperationException($"Customer '{code}' cannot be created with a blank name.");
            decisions[code] = new ImportCustomerDecision(code, name, action);
        }
    }

    private static Button Btn(string text, int width) => new() { Text = text, Size = new Size(width, 40), AutoSize = false, Margin = new Padding(0, 0, 8, 0) };
}
