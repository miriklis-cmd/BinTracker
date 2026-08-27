using BinTracker.Services;

namespace BinTracker.WinForms;

public sealed class MovementChangeDetailForm : BinTrackerForm
{
    public MovementChangeDetailForm(MovementChangeAuditDetail detail)
    {
        Text = detail.Action == "MOVEMENT_BATCH_CORRECTED" ? "Batch Correction Detail" : "Movement Change Detail";
        StartPosition = FormStartPosition.CenterParent; AutoScaleMode = AutoScaleMode.Dpi; KeyPreview = true;
        ClientSize = new Size(1380, 780); MinimumSize = new Size(1080, 640);
        var summary = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, ColumnCount = 2, Padding = new Padding(16, 12, 16, 8) };
        summary.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 190)); summary.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        Add(summary, "Performed by", detail.Actor); Add(summary, "Changed", $"{detail.ChangedUtc:yyyy-MM-dd HH:mm:ss} UTC");
        Add(summary, "Original batch", detail.OriginalBatchId is int original ? $"Batch #{original}" : "Not a whole-batch correction");
        Add(summary, "Replacement batch", detail.ReplacementBatchId is int replacement ? $"Batch #{replacement}" : "Not applicable");
        if (detail.OpenedFromReviewAcknowledgement)
            Add(summary, "Review acknowledgement", $"Showing movement change audit event #{detail.AuditEventId} referenced by the selected acknowledgement.");
        if (detail.ReviewedUtc.HasValue)
            Add(summary, "Reviewed", $"{detail.ReviewedBy ?? "Administrator"} · {detail.ReviewedUtc:yyyy-MM-dd HH:mm:ss} UTC");
        Add(summary, "What changed", MovementChangeComparison.Describe(detail.Lines));
        Add(summary, "Correction reason", detail.Reason);
        var explanation = new Label { Dock = DockStyle.Top, AutoSize = true, Padding = new Padding(16, 4, 16, 8), ForeColor = Color.FromArgb(55, 65, 80),
            Text = "Original rows remain immutable. Neutralisers remove their original effect; corrected replacements carry the intended operational values." };
        var grid = new DataGridView { Dock = DockStyle.Fill, ReadOnly = true, AutoGenerateColumns = false, RowHeadersVisible = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect, AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells,
            BackgroundColor = Color.White, ScrollBars = ScrollBars.Vertical, DataSource = detail.Lines.ToList() };
        grid.Columns.Add(Column("Line role", nameof(MovementChangeAuditLine.Role), 190, wrap: true)); grid.Columns.Add(Column("Movement ID", nameof(MovementChangeAuditLine.MovementId), 115));
        grid.Columns.Add(Column("Batch ID", nameof(MovementChangeAuditLine.BatchId), 90)); grid.Columns.Add(Column("Date", nameof(MovementChangeAuditLine.MovementDate), 115));
        grid.Columns.Add(Column("Customer", nameof(MovementChangeAuditLine.CustomerCode), 105)); grid.Columns.Add(Column("Container", nameof(MovementChangeAuditLine.ContainerType), 150));
        grid.Columns.Add(Column("Direction", nameof(MovementChangeAuditLine.Direction), 105)); grid.Columns.Add(Column("Quantity", nameof(MovementChangeAuditLine.Quantity), 95));
        grid.Columns.Add(Column("Linked movement", nameof(MovementChangeAuditLine.LinkedMovementId), 135));
        grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Reference / notes", DataPropertyName = nameof(MovementChangeAuditLine.ReferenceAndNotes), AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, MinimumWidth = 180, DefaultCellStyle = new DataGridViewCellStyle { WrapMode = DataGridViewTriState.True } });
        var close = new Button { Text = "Close", AutoSize = true, DialogResult = DialogResult.Cancel };
        var footer = new FlowLayoutPanel { Dock = DockStyle.Bottom, AutoSize = true, FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(12) };
        footer.Controls.Add(close); CancelButton = close; Controls.Add(grid); Controls.Add(explanation); Controls.Add(summary); Controls.Add(footer);
        Shown += (_, _) => UseWorkingArea();
    }

    private static void Add(TableLayoutPanel panel, string label, string value)
    {
        var row = panel.RowCount++; panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        panel.Controls.Add(new Label { Text = label, AutoSize = true, Font = new Font("Segoe UI Semibold", 9.5F), Margin = new Padding(0, 3, 10, 3) }, 0, row);
        panel.Controls.Add(new Label { Text = value, AutoSize = true, MaximumSize = new Size(850, 0), Margin = new Padding(0, 3, 0, 3) }, 1, row);
    }
    private static DataGridViewTextBoxColumn Column(string header, string property, int width, bool wrap = false) => new()
    {
        HeaderText = header, DataPropertyName = property, Width = width,
        DefaultCellStyle = new DataGridViewCellStyle { WrapMode = wrap ? DataGridViewTriState.True : DataGridViewTriState.False }
    };

    private void UseWorkingArea()
    {
        var working = Screen.FromControl(this).WorkingArea;
        Size = new Size(Math.Min(1700, working.Width - 80), Math.Min(980, working.Height - 80));
        Location = new Point(working.Left + (working.Width - Width) / 2, working.Top + (working.Height - Height) / 2);
    }
}

public sealed class MovementBatchDetailForm : BinTrackerForm
{
    public MovementBatchDetailForm(int batchId, IReadOnlyList<MovementBatchAuditLine> rows)
    {
        Text = $"Batch #{batchId} Detail"; StartPosition = FormStartPosition.CenterParent; AutoScaleMode = AutoScaleMode.Dpi; KeyPreview = true;
        ClientSize = new Size(1080, 620); MinimumSize = new Size(860, 500);
        var heading = new Label { Dock = DockStyle.Top, AutoSize = true, Padding = new Padding(14), Font = new Font("Segoe UI Semibold", 12F), Text = $"Persisted batch #{batchId} · {rows.Count} movement line{(rows.Count == 1 ? "" : "s")}" };
        var grid = new DataGridView { Dock = DockStyle.Fill, ReadOnly = true, AutoGenerateColumns = false, RowHeadersVisible = false, BackgroundColor = Color.White, DataSource = rows.ToList(), ScrollBars = ScrollBars.Vertical };
        grid.Columns.Add(Column("Movement ID", nameof(MovementBatchAuditLine.MovementId), 100)); grid.Columns.Add(Column("Date", nameof(MovementBatchAuditLine.MovementDate), 100));
        grid.Columns.Add(Column("Customer", nameof(MovementBatchAuditLine.CustomerCode), 100)); grid.Columns.Add(Column("Customer name", nameof(MovementBatchAuditLine.CustomerName), 190));
        grid.Columns.Add(Column("Container", nameof(MovementBatchAuditLine.ContainerType), 140)); grid.Columns.Add(Column("Direction", nameof(MovementBatchAuditLine.Direction), 85));
        grid.Columns.Add(Column("Quantity", nameof(MovementBatchAuditLine.Quantity), 85));
        grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Reference / notes", DataPropertyName = nameof(MovementBatchAuditLine.Notes), AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, MinimumWidth = 180 });
        var close = new Button { Text = "Close", AutoSize = true, DialogResult = DialogResult.Cancel };
        var footer = new FlowLayoutPanel { Dock = DockStyle.Bottom, AutoSize = true, FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(12) };
        footer.Controls.Add(close); CancelButton = close; Controls.Add(grid); Controls.Add(heading); Controls.Add(footer);
    }
    private static DataGridViewTextBoxColumn Column(string header, string property, int width) => new() { HeaderText = header, DataPropertyName = property, Width = width };
}
