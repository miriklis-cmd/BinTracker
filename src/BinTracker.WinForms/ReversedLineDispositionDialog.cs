using BinTracker.Core;
using BinTracker.Services;

namespace BinTracker.WinForms;

internal sealed class ReversedLineDispositionDialog : BinTrackerForm
{
    private const string RestoreChoice = "Restore";
    private const string RemainReversedChoice = "Remain Reversed";

    private readonly DataGridView grid = new()
    {
        Dock = DockStyle.Fill,
        ReadOnly = false,
        AllowUserToAddRows = false,
        AllowUserToDeleteRows = false,
        AllowUserToResizeRows = false,
        AutoGenerateColumns = false,
        MultiSelect = false,
        RowHeadersVisible = false,
        SelectionMode = DataGridViewSelectionMode.FullRowSelect,
        AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells,
        BackgroundColor = Color.White,
        BorderStyle = BorderStyle.FixedSingle,
        ScrollBars = ScrollBars.Vertical,
        EditMode = DataGridViewEditMode.EditOnEnter
    };

    private readonly TextBox reason = new()
    {
        Multiline = true,
        MaxLength = 500,
        ScrollBars = ScrollBars.Vertical,
        Dock = DockStyle.Fill
    };

    private IReadOnlyList<LogicalMovementReversedLineDisposition> decisions = [];

    public IReadOnlyList<LogicalMovementReversedLineDisposition> Decisions => decisions;
    public string RestorationReason => reason.Text.Trim();

    public ReversedLineDispositionDialog(int batchId, LogicalMovementMutationPreview preview)
    {
        ArgumentNullException.ThrowIfNull(preview);
        var reversedLines = preview.Lines
            .Where(x => x.State == LogicalMovementLineState.Reversed)
            .OrderBy(x => x.OriginalDisplayOrdinal)
            .ToArray();
        if (reversedLines.Length == 0)
            throw new ArgumentException("The preview does not contain a reversed logical line.", nameof(preview));

        Text = "Resolve Reversed Batch Lines";
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.Sizable;
        MaximizeBox = true;
        MinimizeBox = false;
        ShowInTaskbar = false;
        AutoScaleMode = AutoScaleMode.Dpi;
        // Open wide enough for every primary heading and the decision column to remain readable
        // on the tested Windows desktop while remaining resizable and non-maximized.
        ClientSize = new Size(1450, 720);
        // Preserve the full evidence and mandatory decision columns at the resize floor.
        // SizeFromClientSize includes DPI/theme-dependent non-client chrome in the minimum.
        MinimumSize = SizeFromClientSize(new Size(1220, 600));
        Font = new Font("Segoe UI", 10F);

        ConfigureGrid();
        foreach (var line in reversedLines)
        {
            var reversalMovementId = line.TerminalReversalMovementId
                ?? throw new InvalidOperationException(
                    "A reversed logical line is missing its terminal reversal movement identity.");
            var customer = string.IsNullOrWhiteSpace(line.LastEffective.CustomerCode) ||
                string.Equals(line.LastEffective.CustomerCode, line.LastEffective.CustomerName,
                    StringComparison.OrdinalIgnoreCase)
                ? line.LastEffective.CustomerName
                : $"{line.LastEffective.CustomerCode} — {line.LastEffective.CustomerName}";
            var technicalEvidence =
                $"Reversed by movement #{reversalMovementId}\r\n" +
                $"Technical: logical root #{preview.LogicalMovementBatchId.Value} · " +
                $"line #{line.LogicalMovementLineId.Value} · original movement #{line.RootMovementId} · " +
                $"last effective #{line.LastEffective.MovementId} · reversal #{reversalMovementId} · " +
                $"generation {preview.ExpectedGeneration.Value}";
            var rowIndex = grid.Rows.Add(
                customer,
                line.LastEffective.ContainerTypeName,
                line.LastEffective.Direction.ToString().ToUpperInvariant(),
                line.LastEffective.Quantity.ToString("N0"),
                line.LastEffective.MovementDate.ToString("dd/MM/yyyy"),
                technicalEvidence,
                null);
            grid.Rows[rowIndex].Tag = line;
            grid.Rows[rowIndex].Cells["ReversalContext"].ToolTipText = technicalEvidence;
        }

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 7,
            Padding = new Padding(22, 18, 22, 0)
        };
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 84F));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 58F));

        root.Controls.Add(new Label
        {
            Text = "Resolve Reversed Lines Before Batch Correction",
            AutoSize = true,
            Font = new Font("Segoe UI Semibold", 18F, FontStyle.Bold),
            Margin = new Padding(0, 0, 0, 8)
        }, 0, 0);
        root.Controls.Add(new Label
        {
            Text = "Choose Restore or Remain Reversed for each customer/container movement below. " +
                "Restores are saved together before batch correction continues; retained lines are not changed.",
            AutoSize = true,
            MaximumSize = new Size(990, 0),
            Margin = new Padding(0, 0, 0, 4)
        }, 0, 1);
        root.Controls.Add(new Label
        {
            Text = $"Technical context: persisted batch #{batchId} · " +
                $"logical root #{preview.LogicalMovementBatchId.Value} · " +
                $"preview generation {preview.ExpectedGeneration.Value}",
            AutoSize = true,
            MaximumSize = new Size(990, 0),
            Font = new Font("Segoe UI", 9F),
            ForeColor = Color.FromArgb(80, 95, 115),
            Margin = new Padding(0, 0, 0, 12)
        }, 0, 2);
        root.Controls.Add(grid, 0, 3);
        root.Controls.Add(new Label
        {
            Text = "Restoration reason (required when any line is restored)",
            AutoSize = true,
            Margin = new Padding(0, 10, 0, 4)
        }, 0, 4);
        root.Controls.Add(reason, 0, 5);

        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            Padding = new Padding(0, 9, 0, 9),
            Margin = Padding.Empty
        };
        var apply = new Button
        {
            Text = "Apply Explicit Decisions",
            AutoSize = true,
            MinimumSize = new Size(200, 40),
            Margin = new Padding(8, 0, 0, 0)
        };
        var cancel = new Button
        {
            Text = "Cancel",
            AutoSize = true,
            MinimumSize = new Size(110, 40),
            Margin = Padding.Empty
        };
        apply.Click += (_, _) => ApplyDecisions();
        cancel.Click += (_, _) => DialogResult = DialogResult.Cancel;
        buttons.Controls.Add(apply);
        buttons.Controls.Add(cancel);
        root.Controls.Add(buttons, 0, 6);

        Controls.Add(root);
        AcceptButton = apply;
        CancelButton = cancel;
    }

    private void ConfigureGrid()
    {
        grid.RowTemplate.Height = 34;
        grid.Columns.Add(TextColumn("Customer", "Customer", 210, true));
        grid.Columns.Add(TextColumn("Container", "Container", 125, true));
        grid.Columns.Add(TextColumn("Direction", "Direction", 95));
        grid.Columns.Add(TextColumn("Quantity", "Quantity", 90,
            alignment: DataGridViewContentAlignment.MiddleRight));
        grid.Columns.Add(TextColumn("Date", "Date", 105));
        grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "ReversalContext",
            HeaderText = "Reversal and Technical Evidence",
            AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
            MinimumWidth = 300,
            ReadOnly = true,
            DefaultCellStyle = new DataGridViewCellStyle
            {
                WrapMode = DataGridViewTriState.True
            }
        });
        grid.Columns.Add(new DataGridViewComboBoxColumn
        {
            Name = "Decision",
            HeaderText = "Explicit Decision",
            Width = 190,
            MinimumWidth = 185,
            FlatStyle = FlatStyle.Popup,
            DisplayStyle = DataGridViewComboBoxDisplayStyle.ComboBox,
            Items = { RestoreChoice, RemainReversedChoice }
        });
        grid.DataError += (_, e) => e.ThrowException = false;
    }

    private void ApplyDecisions()
    {
        grid.EndEdit();
        var selected = new List<LogicalMovementReversedLineDisposition>(grid.Rows.Count);
        foreach (DataGridViewRow row in grid.Rows)
        {
            var line = row.Tag as LogicalMovementMutationPreviewLine
                ?? throw new InvalidOperationException("The persisted logical line identity is unavailable.");
            var choice = row.Cells["Decision"].Value as string;
            if (choice is not (RestoreChoice or RemainReversedChoice))
            {
                MessageBox.Show(this,
                    $"Choose Restore or Remain Reversed for logical line #{line.LogicalMovementLineId.Value}.",
                    "Decision Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                grid.CurrentCell = row.Cells["Decision"];
                grid.BeginEdit(true);
                return;
            }
            selected.Add(new(line.LogicalMovementLineId,
                choice == RestoreChoice
                    ? ReversedLineDisposition.Restore
                    : ReversedLineDisposition.RemainReversed));
        }

        if (selected.Any(x => x.Disposition == ReversedLineDisposition.Restore) &&
            RestorationReason.Length < 3)
        {
            MessageBox.Show(this, "Enter a restoration reason.", "Restoration Reason Required",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            reason.Focus();
            return;
        }

        decisions = selected;
        DialogResult = DialogResult.OK;
    }

    private static DataGridViewTextBoxColumn TextColumn(string header, string name, int width,
        bool wrap = false,
        DataGridViewContentAlignment alignment = DataGridViewContentAlignment.MiddleLeft) => new()
        {
            Name = name,
            HeaderText = header,
            Width = width,
            MinimumWidth = width,
            ReadOnly = true,
            DefaultCellStyle = new DataGridViewCellStyle
            {
                WrapMode = wrap ? DataGridViewTriState.True : DataGridViewTriState.False,
                Alignment = alignment
            }
        };
}
