
using BinTracker.Services;

namespace BinTracker.WinForms;

public sealed class ExistingCustomerMatchForm : Form
{
    private readonly ImportReviewPlan review;
    private readonly IReadOnlyList<CustomerListRow> customers;
    private readonly Dictionary<string, ImportExistingCustomerDecision> decisions;

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

    public IReadOnlyDictionary<string, ImportExistingCustomerDecision> Decisions => decisions;

    public ExistingCustomerMatchForm(
        ImportReviewPlan review,
        IReadOnlyList<CustomerListRow> customers,
        IReadOnlyDictionary<string, ImportExistingCustomerDecision> existing)
    {
        this.review = review;
        this.customers = customers;
        decisions = new Dictionary<string, ImportExistingCustomerDecision>(
            ImportExistingCustomerDecisionPlanner.MergeDefaults(review, existing),
            StringComparer.OrdinalIgnoreCase);

        Text = "Confirm Existing Customer Matches";
        StartPosition = FormStartPosition.CenterParent;
        AutoScaleMode = AutoScaleMode.Dpi;
        ClientSize = new Size(1180, 720);
        MinimumSize = new Size(1050, 640);
        BackColor = Color.FromArgb(245, 247, 250);
        Font = new Font("Segoe UI", 10F);

        Build();
        LoadRows();
    }

    private void Build()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4,
            Padding = new Padding(16)
        };
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        root.Controls.Add(new Label
        {
            Text = "Confirm existing customer matches",
            AutoSize = true,
            Font = new Font("Segoe UI Semibold", 18F, FontStyle.Bold)
        }, 0, 0);

        root.Controls.Add(new Label
        {
            Text =
                "Automatic matches are proposed, not silently trusted. Accept the proposed customer or select a different existing customer. " +
                "Unconfirmed matches block Import readiness.",
            AutoSize = true,
            ForeColor = Color.DimGray,
            MaximumSize = new Size(1120, 0),
            Margin = new Padding(0, 6, 0, 12)
        }, 0, 1);

        ConfigureGrid();
        root.Controls.Add(grid, 0, 2);
        root.Controls.Add(BuildFooter(), 0, 3);
        Controls.Add(root);
    }

    private void ConfigureGrid()
    {
        grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

        grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "Imported",
            HeaderText = "Imported customer",
            ReadOnly = true,
            FillWeight = 115,
            MinimumWidth = 160
        });

        grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "Reason",
            HeaderText = "Automatic match reason",
            ReadOnly = true,
            FillWeight = 140,
            MinimumWidth = 210
        });

        var customerColumn = new DataGridViewComboBoxColumn
        {
            Name = "Customer",
            HeaderText = "Existing BinTracker customer",
            FillWeight = 190,
            MinimumWidth = 300,
            DisplayMember = nameof(CustomerChoice.Display),
            ValueMember = nameof(CustomerChoice.Id),
            DisplayStyle = DataGridViewComboBoxDisplayStyle.DropDownButton
        };
        customerColumn.DataSource = customers
            .Where(x => x.IsActive)
            .OrderBy(x => x.CustomerCode, StringComparer.OrdinalIgnoreCase)
            .Select(x => new CustomerChoice(
                x.Id,
                $"{x.CustomerCode} — {x.Name}",
                x.CustomerCode,
                x.Name))
            .ToList();
        grid.Columns.Add(customerColumn);

        var decisionColumn = new DataGridViewComboBoxColumn
        {
            Name = "Decision",
            HeaderText = "Decision",
            FillWeight = 90,
            MinimumWidth = 150
        };
        decisionColumn.Items.AddRange(
            ImportExistingCustomerDecisionAction.Unconfirmed,
            ImportExistingCustomerDecisionAction.AcceptMatch,
            ImportExistingCustomerDecisionAction.OverrideMatch);
        grid.Columns.Add(decisionColumn);

        grid.DataError += (_, _) => { };
    }

    private void LoadRows()
    {
        grid.Rows.Clear();

        foreach (var row in review.Customers
                     .Where(x =>
                         x.Status == ImportCustomerReviewStatus.Existing &&
                         x.ExistingCustomerId.HasValue)
                     .OrderBy(x => x.CustomerCode, StringComparer.OrdinalIgnoreCase))
        {
            var decision = decisions[row.CustomerCode];

            var index = grid.Rows.Add(
                row.CustomerCode,
                row.MatchReason,
                decision.CustomerId,
                decision.Action);

            grid.Rows[index].Tag = row;
        }
    }

    private Control BuildFooter()
    {
        var footer = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            ColumnCount = 2,
            Margin = new Padding(0, 12, 0, 0)
        };
        footer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        footer.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        var bulk = new FlowLayoutPanel
        {
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false
        };

        var selected = Btn("Selected → Accept", 190);
        selected.Click += (_, _) => SetSelected(ImportExistingCustomerDecisionAction.AcceptMatch);

        var all = Btn("All → Accept", 155);
        all.Click += (_, _) => SetAll(ImportExistingCustomerDecisionAction.AcceptMatch);

        bulk.Controls.Add(selected);
        bulk.Controls.Add(all);

        var right = new FlowLayoutPanel
        {
            AutoSize = true,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false
        };

        var cancel = Btn("Cancel", 110);
        cancel.Click += (_, _) =>
        {
            DialogResult = DialogResult.Cancel;
            Close();
        };

        var apply = Btn("Apply decisions", 155);
        apply.Click += (_, _) =>
        {
            SaveRows();
            DialogResult = DialogResult.OK;
            Close();
        };

        right.Controls.Add(cancel);
        right.Controls.Add(apply);

        footer.Controls.Add(bulk, 0, 0);
        footer.Controls.Add(right, 1, 0);
        return footer;
    }

    private void SetSelected(ImportExistingCustomerDecisionAction action)
    {
        foreach (DataGridViewRow row in grid.SelectedRows)
            row.Cells["Decision"].Value = action;
    }

    private void SetAll(ImportExistingCustomerDecisionAction action)
    {
        foreach (DataGridViewRow row in grid.Rows)
            row.Cells["Decision"].Value = action;
    }

    private void SaveRows()
    {
        var byId = customers.ToDictionary(x => x.Id);

        foreach (DataGridViewRow row in grid.Rows)
        {
            if (row.Tag is not ImportCustomerReviewRow reviewRow)
                continue;

            var customerId = row.Cells["Customer"].Value is null
                ? (int?)null
                : Convert.ToInt32(row.Cells["Customer"].Value);

            var action = row.Cells["Decision"].Value is ImportExistingCustomerDecisionAction typed
                ? typed
                : Enum.TryParse<ImportExistingCustomerDecisionAction>(
                    row.Cells["Decision"].Value?.ToString(),
                    out var parsed)
                    ? parsed
                    : ImportExistingCustomerDecisionAction.Unconfirmed;

            if (!customerId.HasValue)
                action = ImportExistingCustomerDecisionAction.Unconfirmed;

            if (customerId.HasValue &&
                action == ImportExistingCustomerDecisionAction.AcceptMatch &&
                reviewRow.ExistingCustomerId != customerId)
            {
                action = ImportExistingCustomerDecisionAction.OverrideMatch;
            }

            var selected = customerId.HasValue &&
                           byId.TryGetValue(customerId.Value, out var found)
                ? found
                : null;

            decisions[reviewRow.CustomerCode] = new ImportExistingCustomerDecision(
                reviewRow.CustomerCode,
                action,
                customerId,
                selected?.CustomerCode ?? string.Empty,
                selected?.Name ?? string.Empty);
        }
    }

    private static Button Btn(string text, int width) => new()
    {
        Text = text,
        Size = new Size(width, 40),
        AutoSize = false,
        Margin = new Padding(0, 0, 8, 0)
    };

    private sealed record CustomerChoice(int Id, string Display, string Code, string Name);
}
