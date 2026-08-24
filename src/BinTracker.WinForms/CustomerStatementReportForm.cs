using BinTracker.Core;
using BinTracker.Services;

namespace BinTracker.WinForms;

public sealed class CustomerStatementReportForm : BinTrackerForm
{
    private readonly ICustomerService customers;
    private readonly ICustomerStatementReportService statementReports;
    private readonly IBusinessClock clock;

    private readonly TextBox search = new()
    {
        Width = 395,
        MinimumSize = new Size(395, 0),
        PlaceholderText = "Type customer code/name, then press Enter"
    };

    private readonly CheckBox includeInactive = new()
    {
        Text = "Include inactive",
        AutoSize = true,
        Margin = new Padding(14, 8, 0, 0)
    };

    private readonly Label status = new()
    {
        AutoSize = true,
        ForeColor = Color.DimGray,
        MaximumSize = new Size(950, 0)
    };

    private readonly DataGridView grid = new()
    {
        Dock = DockStyle.Fill,
        ReadOnly = true,
        AllowUserToAddRows = false,
        AllowUserToDeleteRows = false,
        MultiSelect = false,
        SelectionMode = DataGridViewSelectionMode.FullRowSelect,
        RowHeadersVisible = false,
        AutoGenerateColumns = false,
        BackgroundColor = Color.White,
        BorderStyle = BorderStyle.FixedSingle
    };

    public CustomerStatementReportForm(
        ICustomerService customers,
        ICustomerStatementReportService statementReports,
        IBusinessClock clock)
    {
        this.customers = customers;
        this.statementReports = statementReports;
        this.clock = clock;

        Text = "Customer Statement";
        StartPosition = FormStartPosition.Manual;
        AutoScaleMode = AutoScaleMode.Dpi;
        MinimumSize = new Size(850, 600);
        Font = new Font("Segoe UI", 10F);
        BackColor = Color.FromArgb(245, 247, 250);

        search.KeyDown += async (_, e) =>
        {
            if (e.KeyCode != Keys.Enter)
                return;

            e.SuppressKeyPress = true;
            await ReloadAsync();
        };

        includeInactive.CheckedChanged += async (_, _) =>
            await ReloadAsync();

        Build();

        Load += async (_, _) =>
        {
            ApplyResponsiveBounds();
            await ReloadAsync();
        };
    }

    private void Build()
    {
        grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "Id",
            Visible = false
        });

        grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "Code",
            HeaderText = "Customer Code",
            Width = 160
        });

        grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "Customer",
            HeaderText = "Customer",
            AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
            MinimumWidth = 220
        });

        grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "Type",
            HeaderText = "Type",
            Width = 125
        });

        grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "Position",
            HeaderText = "Net Position",
            Width = 135
        });

        grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "Status",
            HeaderText = "Status",
            Width = 100
        });

        grid.CellDoubleClick += async (_, e) =>
        {
            if (e.RowIndex >= 0)
                await OpenSelectedAsync();
        };

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 5,
            Padding = new Padding(18)
        };

        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var header = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            ColumnCount = 1,
            RowCount = 2,
            BackColor = Color.White,
            Padding = new Padding(18, 12, 18, 12),
            Margin = new Padding(0, 0, 0, 10)
        };

        header.Controls.Add(new Label
        {
            Text = "Customer Statement",
            AutoSize = true,
            Font = new Font(
                "Segoe UI Semibold",
                19F,
                FontStyle.Bold),
            Margin = new Padding(0, 0, 0, 5)
        }, 0, 0);

        header.Controls.Add(new Label
        {
            Text =
                "Select a customer, then generate or open a statement for the required period.",
            AutoSize = true,
            ForeColor = Color.FromArgb(80, 90, 105)
        }, 0, 1);

        root.Controls.Add(header, 0, 0);

        var filters = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true,
            BackColor = Color.White,
            Padding = new Padding(16, 12, 16, 12),
            Margin = new Padding(0, 0, 0, 10)
        };

        filters.Controls.Add(new Label
        {
            Text = "Customer",
            AutoSize = true,
            Margin = new Padding(0, 8, 8, 0)
        });
        filters.Controls.Add(search);
        filters.Controls.Add(includeInactive);
        filters.Controls.Add(new Label
        {
            Text = "Press Enter to search",
            AutoSize = true,
            ForeColor = Color.DimGray,
            Margin = new Padding(14, 8, 0, 0)
        });

        root.Controls.Add(filters, 0, 1);

        var statusCard = new Panel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            BackColor = Color.White,
            Padding = new Padding(16, 10, 16, 10),
            Margin = new Padding(0, 0, 0, 10)
        };
        statusCard.Controls.Add(status);
        root.Controls.Add(statusCard, 0, 2);

        var gridCard = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.White,
            Padding = new Padding(10)
        };
        gridCard.Controls.Add(ReportGridMultiSort.Wrap(grid));
        root.Controls.Add(gridCard, 0, 3);

        var actions = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            Padding = new Padding(0, 10, 0, 0),
            Margin = new Padding(0)
        };

        var close = new Button
        {
            Text = "Close",
            AutoSize = false,
            Size = new Size(110, 40),
            Margin = new Padding(8, 0, 0, 0)
        };
        close.Click += (_, _) => Close();

        var statement = new Button
        {
            Text = "Customer Statement",
            AutoSize = false,
            Size = new Size(210, 40),
            MinimumSize = new Size(210, 40),
            Margin = new Padding(8, 0, 0, 0)
        };
        statement.Click += async (_, _) => await OpenSelectedAsync();

        actions.Controls.Add(close);
        actions.Controls.Add(statement);
        root.Controls.Add(actions, 0, 4);

        Controls.Add(root);
    }

    private async Task ReloadAsync()
    {
        try
        {
            Enabled = false;
            UseWaitCursor = true;

            var rows = await customers.SearchAsync(
                search.Text,
                includeInactive.Checked);

            grid.Rows.Clear();

            foreach (var row in rows)
            {
                var index = grid.Rows.Add(
                    row.Id,
                    row.CustomerCode,
                    row.Name,
                    row.CustomerType == CustomerType.CashCod
                        ? "Cash / COD"
                        : "Account",
                    row.NetBalance == 0
                        ? "Even"
                        : row.NetBalance > 0
                            ? $"{row.NetBalance} OUT"
                            : $"{Math.Abs(row.NetBalance)} CREDIT",
                    row.IsActive ? "Active" : "Inactive");

                grid.Rows[index].Tag = row.Id;
            }

            ReportGridMultiSort.Reapply(grid);

            if (grid.Rows.Count > 0)
            {
                grid.ClearSelection();
                grid.Rows[0].Selected = true;
                grid.CurrentCell = grid.Rows[0].Cells["Code"];
            }

            status.Text = rows.Count == 0
                ? "No matching customers."
                : $"{rows.Count:N0} customer(s). Double-click a customer or use Customer Statement.";
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                this,
                ex.Message,
                "Customer Statement",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
        finally
        {
            Enabled = true;
            UseWaitCursor = false;
        }
    }

    private async Task OpenSelectedAsync()
    {
        if (grid.SelectedRows.Count == 0)
        {
            MessageBox.Show(
                this,
                "Select a customer first.",
                "Customer Statement",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return;
        }

        var id = Convert.ToInt32(
            grid.SelectedRows[0].Cells["Id"].Value);

        await CustomerStatementWorkflow.RunAsync(
            this,
            id,
            customers,
            statementReports,
            clock);
    }

    private void ApplyResponsiveBounds()
    {
        var screen = Owner is not null
            ? Screen.FromControl(Owner)
            : Screen.FromPoint(Cursor.Position);

        var area = screen.WorkingArea;

        Width = Math.Clamp(
            (int)Math.Round(area.Width * 0.72),
            MinimumSize.Width,
            1400);

        Height = Math.Clamp(
            (int)Math.Round(area.Height * 0.78),
            MinimumSize.Height,
            950);

        Left = area.Left + Math.Max(0, (area.Width - Width) / 2);
        Top = area.Top + Math.Max(0, (area.Height - Height) / 2);
    }
}
