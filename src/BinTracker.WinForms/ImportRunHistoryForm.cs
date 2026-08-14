using BinTracker.Services;

namespace BinTracker.WinForms;

public sealed class ImportRunHistoryForm : Form
{
    private readonly IImportRunHistoryService service;

    private readonly DataGridView runsGrid = new()
    {
        Dock = DockStyle.Fill,
        ReadOnly = true,
        AllowUserToAddRows = false,
        AllowUserToDeleteRows = false,
        AllowUserToResizeRows = false,
        MultiSelect = false,
        SelectionMode = DataGridViewSelectionMode.FullRowSelect,
        RowHeadersVisible = false,
        AutoGenerateColumns = false,
        BackgroundColor = Color.White,
        BorderStyle = BorderStyle.FixedSingle
    };

    private readonly DataGridView correctionGrid = new()
    {
        Dock = DockStyle.Fill,
        ReadOnly = true,
        AllowUserToAddRows = false,
        AllowUserToDeleteRows = false,
        AllowUserToResizeRows = false,
        MultiSelect = false,
        SelectionMode = DataGridViewSelectionMode.FullRowSelect,
        RowHeadersVisible = false,
        AutoGenerateColumns = false,
        BackgroundColor = Color.White,
        BorderStyle = BorderStyle.FixedSingle
    };

    private readonly DataGridView movementsGrid = new()
    {
        Dock = DockStyle.Fill,
        ReadOnly = true,
        AllowUserToAddRows = false,
        AllowUserToDeleteRows = false,
        AllowUserToResizeRows = false,
        MultiSelect = false,
        SelectionMode = DataGridViewSelectionMode.FullRowSelect,
        RowHeadersVisible = false,
        AutoGenerateColumns = false,
        BackgroundColor = Color.White,
        BorderStyle = BorderStyle.FixedSingle
    };

    private readonly Label summary = new()
    {
        AutoSize = true,
        Font = new Font("Segoe UI Semibold", 11F, FontStyle.Bold)
    };

    private readonly Label provenance = new()
    {
        AutoSize = false,
        Dock = DockStyle.Top,
        Height = 28,
        AutoEllipsis = true,
        ForeColor = Color.FromArgb(70, 80, 95),
        Margin = Padding.Empty
    };

    private readonly Label sha = new()
    {
        AutoSize = true,
        Font = new Font("Consolas", 9F),
        ForeColor = Color.FromArgb(70, 80, 95),
        MaximumSize = new Size(1180, 0)
    };

    private readonly Label replacement = new()
    {
        AutoSize = true,
        ForeColor = Color.FromArgb(150, 95, 0),
        MaximumSize = new Size(1180, 0)
    };

    private readonly Label notes = new()
    {
        AutoSize = true,
        ForeColor = Color.FromArgb(80, 90, 105),
        MaximumSize = new Size(1180, 0)
    };

    private readonly Label correctionCount = new()
    {
        AutoSize = true,
        Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold),
        ForeColor = Color.FromArgb(150, 95, 0)
    };

    private readonly Label movementCount = new()
    {
        AutoSize = true,
        ForeColor = Color.DimGray
    };

    public ImportRunHistoryForm(IImportRunHistoryService service)
    {
        this.service = service;

        Text = "Import Run History";
        StartPosition = FormStartPosition.CenterParent;
        AutoScaleMode = AutoScaleMode.Dpi;
        ClientSize = new Size(1400, 900);
        MinimumSize = new Size(1120, 760);
        Font = new Font("Segoe UI", 10F);
        BackColor = Color.FromArgb(245, 247, 250);

        Build();
        Shown += async (_, _) => await LoadRunsAsync();
    }

    private void Build()
    {
        runsGrid.ColumnHeadersHeightSizeMode =
            DataGridViewColumnHeadersHeightSizeMode.AutoSize;
        runsGrid.ColumnHeadersDefaultCellStyle.WrapMode =
            DataGridViewTriState.False;

        runsGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Run",
            Width = 70
        });
        runsGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Cutover",
            Width = 105
        });
        runsGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Status",
            Width = 110
        });
        runsGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Workbook",
            AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
            MinimumWidth = 220
        });
        runsGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "User",
            Width = 95
        });
        runsGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Completed",
            Width = 170
        });
        runsGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Customers",
            Width = 105
        });
        runsGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Movements",
            Width = 110
        });
        runsGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Replaces",
            Width = 90
        });

        correctionGrid.ColumnHeadersHeightSizeMode =
            DataGridViewColumnHeadersHeightSizeMode.AutoSize;
        correctionGrid.ColumnHeadersDefaultCellStyle.WrapMode =
            DataGridViewTriState.False;

        correctionGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Customer",
            Width = 120
        });
        correctionGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Customer name",
            Width = 190
        });
        correctionGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Container",
            Width = 140
        });
        correctionGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Previous",
            Width = 125
        });
        correctionGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Corrected",
            Width = 125
        });
        correctionGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Change",
            AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
            MinimumWidth = 100
        });

        movementsGrid.ColumnHeadersHeightSizeMode =
            DataGridViewColumnHeadersHeightSizeMode.AutoSize;
        movementsGrid.ColumnHeadersDefaultCellStyle.WrapMode =
            DataGridViewTriState.False;

        movementsGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Date",
            Width = 100
        });
        movementsGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Customer",
            Width = 120
        });
        movementsGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Customer name",
            Width = 180
        });
        movementsGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Container",
            Width = 130
        });
        movementsGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Direction",
            Width = 90
        });
        movementsGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Source",
            Width = 150
        });
        movementsGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Qty",
            Width = 70
        });
        movementsGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Reference",
            Width = 110
        });
        movementsGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Entered by",
            AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
            MinimumWidth = 110
        });

        runsGrid.MinimumSize = new Size(0, 160);
        correctionGrid.MinimumSize = new Size(0, 115);
        movementsGrid.MinimumSize = new Size(0, 190);

        runsGrid.SelectionChanged += async (_, _) =>
        {
            if (runsGrid.CurrentRow?.Tag is long runId)
                await LoadRunAsync(runId);
        };

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4,
            Padding = new Padding(16)
        };
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 205F));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var header = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            BackColor = Color.White,
            Padding = new Padding(16, 10, 16, 10),
            Margin = new Padding(0, 0, 0, 8),
            ColumnCount = 1,
            RowCount = 2
        };
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        header.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        header.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        header.Controls.Add(new Label
        {
            Text = "Import Run history",
            AutoSize = true,
            Font = new Font("Segoe UI Semibold", 18F, FontStyle.Bold),
            Margin = new Padding(0, 0, 0, 4)
        }, 0, 0);

        header.Controls.Add(new Label
        {
            Text = "Review every completed/replaced Excel import, correction details and the movements generated by each run.",
            AutoSize = true,
            ForeColor = Color.DimGray,
            MaximumSize = new Size(1250, 0),
            Margin = Padding.Empty
        }, 0, 1);

        var runsCard = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.White,
            Padding = new Padding(10),
            Margin = new Padding(0, 0, 0, 8)
        };
        runsCard.Controls.Add(runsGrid);

        var detail = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.White,
            ColumnCount = 1,
            RowCount = 9,
            Padding = new Padding(12),
            Margin = Padding.Empty
        };
        detail.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        detail.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        detail.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        detail.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        detail.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        detail.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        detail.RowStyles.Add(new RowStyle(SizeType.Absolute, 128F));
        detail.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        detail.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

        detail.Controls.Add(summary, 0, 0);
        detail.Controls.Add(provenance, 0, 1);
        detail.Controls.Add(sha, 0, 2);
        detail.Controls.Add(replacement, 0, 3);
        detail.Controls.Add(notes, 0, 4);
        detail.Controls.Add(correctionCount, 0, 5);
        detail.Controls.Add(correctionGrid, 0, 6);
        detail.Controls.Add(movementCount, 0, 7);
        detail.Controls.Add(movementsGrid, 0, 8);

        var close = new Button
        {
            Text = "Close",
            AutoSize = false,
            Size = new Size(110, 38),
            Anchor = AnchorStyles.Right,
            Margin = new Padding(0, 8, 0, 0)
        };
        close.Click += (_, _) => Close();

        var footer = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            Margin = Padding.Empty
        };
        footer.Controls.Add(close);

        root.Controls.Add(header, 0, 0);
        root.Controls.Add(runsCard, 0, 1);
        root.Controls.Add(detail, 0, 2);
        root.Controls.Add(footer, 0, 3);

        Controls.Add(root);
    }

    private async Task LoadRunsAsync()
    {
        try
        {
            var runs = await service.GetRunsAsync();

            runsGrid.Rows.Clear();

            foreach (var run in runs)
            {
                var rowIndex = runsGrid.Rows.Add(
                    $"#{run.Id}",
                    run.CutoverDate?.ToString("dd/MM/yyyy") ?? "—",
                    run.Status,
                    run.SourceFileName,
                    run.Username,
                    run.CompletedUtc?.ToLocalTime().ToString("g") ?? "—",
                    run.CreatedCustomers,
                    run.MovementCount,
                    run.ReplacesImportRunId.HasValue
                        ? $"#{run.ReplacesImportRunId}"
                        : "—");

                runsGrid.Rows[rowIndex].Tag = run.Id;
            }

            if (runsGrid.Rows.Count == 0)
            {
                ClearDetail("No Import Runs have been recorded yet.");
                return;
            }

            runsGrid.Rows[0].Selected = true;
            runsGrid.CurrentCell = runsGrid.Rows[0].Cells[0];
            await LoadRunAsync((long)runsGrid.Rows[0].Tag!);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                this,
                ex.Message,
                "Import Run History",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }
    }

    private async Task LoadRunAsync(long runId)
    {
        var detail = await service.GetRunAsync(runId);

        if (detail is null)
        {
            ClearDetail($"Import Run #{runId} no longer exists.");
            return;
        }

        summary.Text =
            $"Run #{detail.Id} — {detail.Status} — Cutover " +
            $"{detail.CutoverDate?.ToString("dd/MM/yyyy") ?? "unknown"} — " +
            $"{detail.Username}";

        provenance.Text =
            $"Workbook: {detail.SourceFileName}   " +
            $"Size: {detail.SourceLength:N0} bytes   " +
            $"Source modified: {detail.SourceLastWriteUtc.ToLocalTime():g}   " +
            $"Completed: {detail.CompletedUtc?.ToLocalTime().ToString("g") ?? "—"}";

        sha.Text = $"SHA-256: {detail.SourceSha256}";

        replacement.Text =
            detail.ReplacesImportRunId.HasValue
                ? $"Corrected/replaced run #{detail.ReplacesImportRunId}."
                : detail.ReplacedByImportRunId.HasValue
                    ? $"This run was replaced by run #{detail.ReplacedByImportRunId}."
                    : "No replacement relationship.";

        notes.Text = string.IsNullOrWhiteSpace(detail.Notes)
            ? "Notes: —"
            : $"Notes: {detail.Notes}";

        correctionGrid.Rows.Clear();

        foreach (var change in detail.CorrectionChanges)
        {
            correctionGrid.Rows.Add(
                change.CustomerCode,
                change.CustomerName,
                change.ContainerType,
                Signed(change.PreviousNetEffect),
                Signed(change.CorrectedNetEffect),
                Signed(change.Difference));
        }

        correctionCount.Text =
            detail.ReplacesImportRunId.HasValue
                ? detail.CorrectionChanges.Count > 0
                    ? $"Correction changes ({detail.CorrectionChanges.Count:N0})"
                    : "Correction changes: not captured by the build that created this run."
                : "Correction changes: not applicable.";

        correctionGrid.Visible =
            detail.ReplacesImportRunId.HasValue;

        movementCount.Text =
            $"Customers created: {detail.CreatedCustomers:N0}   " +
            $"Recorded movement count: {detail.MovementCount:N0}   " +
            $"Currently linked movement rows: {detail.Movements.Count:N0}";

        movementsGrid.Rows.Clear();

        foreach (var movement in detail.Movements)
        {
            movementsGrid.Rows.Add(
                movement.MovementDate.ToString("dd/MM/yyyy"),
                movement.CustomerCode,
                movement.CustomerName,
                movement.ContainerType,
                movement.Direction,
                movement.Source,
                movement.Quantity,
                movement.ReferenceNumber,
                movement.EnteredBy);
        }
    }

    private static string Signed(int value) =>
        value > 0
            ? $"+{value}"
            : value.ToString();

    private void ClearDetail(string message)
    {
        summary.Text = message;
        provenance.Text = string.Empty;
        sha.Text = string.Empty;
        replacement.Text = string.Empty;
        notes.Text = string.Empty;
        correctionCount.Text = string.Empty;
        correctionGrid.Rows.Clear();
        correctionGrid.Visible = false;
        movementCount.Text = string.Empty;
        movementsGrid.Rows.Clear();
    }
}
