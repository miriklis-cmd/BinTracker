using BinTracker.Services;

namespace BinTracker.WinForms;

public sealed class AuditLogForm : BinTrackerForm
{
    private readonly IAuditService audit;
    private readonly ComboBox filter = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 150 };
    private readonly Label countLabel = new() { AutoSize = true, ForeColor = Color.DimGray };
    private readonly Label feedback = new() { AutoSize = true, ForeColor = Color.FromArgb(35, 105, 60), Margin = new Padding(12, 8, 0, 0) };
    private readonly Button detail = new() { Text = "View Detail", AutoSize = true, Enabled = false };
    private readonly Button reviewed = new() { Text = "Mark Selected Reviewed", AutoSize = true, Enabled = false };
    private readonly DataGridView grid = new() { Dock = DockStyle.Fill, ReadOnly = true, AutoGenerateColumns = false,
        SelectionMode = DataGridViewSelectionMode.FullRowSelect, MultiSelect = false, RowHeadersVisible = false,
        AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells, ScrollBars = ScrollBars.Both, BackgroundColor = Color.White };

    public AuditLogForm(IAuditService audit, bool openNeedsReview = false)
    {
        this.audit = audit;
        Text = "Audit Trail"; StartPosition = FormStartPosition.CenterParent; AutoScaleMode = AutoScaleMode.Dpi;
        KeyPreview = true; ClientSize = new Size(1320, 760); MinimumSize = new Size(960, 560);
        grid.Columns.Add(TextColumn("Time (UTC)", nameof(AuditTrailRow.TimestampUtc), 155));
        grid.Columns.Add(TextColumn("User", nameof(AuditTrailRow.Username), 100));
        grid.Columns.Add(TextColumn("Action", nameof(AuditTrailRow.Action), 185));
        grid.Columns.Add(TextColumn("Entity", nameof(AuditTrailRow.EntityType), 115));
        grid.Columns.Add(TextColumn("ID", nameof(AuditTrailRow.EntityId), 65));
        grid.Columns.Add(TextColumn("Review state", nameof(AuditTrailRow.ReviewState), 105));
        grid.Columns.Add(TextColumn("Reviewed by / time", nameof(AuditTrailRow.ReviewedBy), 205));
        grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Description", DataPropertyName = nameof(AuditTrailRow.Description),
            AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, MinimumWidth = 260,
            DefaultCellStyle = new DataGridViewCellStyle { WrapMode = DataGridViewTriState.True } });
        grid.Columns.Add(new DataGridViewCheckBoxColumn { HeaderText = "Success", DataPropertyName = nameof(AuditTrailRow.Succeeded), Width = 70 });
        filter.Items.AddRange(["All", "Needs review", "Reviewed"]); filter.SelectedIndex = openNeedsReview ? 1 : 0;
        filter.SelectedIndexChanged += async (_, _) => await ReloadAsync(CurrentFilter == AuditReviewFilter.NeedsReview);
        detail.Click += async (_, _) => await ShowContextDetailAsync();
        reviewed.Click += async (_, _) => await MarkReviewedAsync();
        grid.SelectionChanged += (_, _) => UpdateActions();
        grid.CellDoubleClick += async (_, e) => { if (e.RowIndex >= 0 && detail.Enabled) await ShowContextDetailAsync(); };
        grid.CellFormatting += GridCellFormatting;
        var actions = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, Padding = new Padding(8), WrapContents = false };
        actions.Controls.Add(new Label { Text = "Review:", AutoSize = true, Margin = new Padding(0, 8, 4, 0) });
        actions.Controls.Add(filter); actions.Controls.Add(detail); actions.Controls.Add(reviewed); actions.Controls.Add(feedback);
        var footer = new Panel { Dock = DockStyle.Bottom, Height = 34, Padding = new Padding(8, 7, 8, 0) }; footer.Controls.Add(countLabel);
        Controls.Add(grid); Controls.Add(footer); Controls.Add(actions);
        Shown += async (_, _) => await ReloadAsync(openNeedsReview);
    }

    private AuditReviewFilter CurrentFilter => filter.SelectedIndex switch { 1 => AuditReviewFilter.NeedsReview, 2 => AuditReviewFilter.Reviewed, _ => AuditReviewFilter.All };
    private AuditTrailRow? Selected => grid.CurrentRow?.DataBoundItem as AuditTrailRow;
    private static DataGridViewTextBoxColumn TextColumn(string header, string property, int width) => new() { HeaderText = header, DataPropertyName = property, Width = width };

    private async Task ReloadAsync(bool selectOldestPending)
    {
        var rows = (await audit.GetAuditTrailAsync(CurrentFilter)).ToList(); grid.DataSource = rows;
        var state = await audit.GetAdministratorReviewStateAsync();
        countLabel.Text = $"Displaying {rows.Count:N0} audit event{(rows.Count == 1 ? "" : "s")}. Pending review: {state.PendingCount:N0}.";
        var target = selectOldestPending ? AuditReviewPolicy.SelectOldestPending(rows) : null;
        if (target is not null)
        {
            var row = grid.Rows.Cast<DataGridViewRow>().Single(x => ((AuditTrailRow)x.DataBoundItem!).Id == target.Id);
            row.Selected = true; grid.CurrentCell = row.Cells[0]; grid.FirstDisplayedScrollingRowIndex = row.Index;
        }
        UpdateActions();
    }

    private void UpdateActions()
    {
        reviewed.Enabled = Selected?.CanMarkReviewed == true;
        detail.Enabled = Selected is { } row && (row.HasMovementChangeDetail || row.HasAuthoritativeBatchDetail);
    }

    private async Task ShowContextDetailAsync()
    {
        var selected = Selected; if (selected is null || !detail.Enabled) return;
        if (selected.HasMovementChangeDetail)
        {
            var change = await audit.GetMovementChangeDetailAsync(selected.Id);
            if (change is null) { MessageBox.Show(this, "Authoritative movement-change lineage could not be resolved uniquely. No detail was shown.", "Movement Change Detail", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            using var form = new MovementChangeDetailForm(change); form.ShowDialog(this); return;
        }
        if (!int.TryParse(selected.EntityId, out var batchId)) return;
        using var batch = new MovementBatchDetailForm(batchId, await audit.GetMovementBatchDetailAsync(batchId)); batch.ShowDialog(this);
    }

    private async Task MarkReviewedAsync()
    {
        var selected = Selected; if (!AuditReviewPolicy.CanMarkReviewed(selected)) return;
        var identity = selected!.EntityType == "MovementBatch" ? $"batch #{selected.EntityId}" : $"movement #{selected.EntityId}";
        var prompt = $"Mark this Operator movement change as reviewed?\r\n\r\nAudit event: #{selected.Id}\r\nChange: {selected.Action} ({identity})\r\nActor: {selected.Username}\r\n{selected.Description}";
        if (MessageBox.Show(this, prompt, "Confirm Movement Change Review", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2) != DialogResult.Yes) return;
        await audit.MarkMovementChangesReviewedAsync([selected.Id]); feedback.Text = $"Audit event #{selected.Id} marked Reviewed.";
        await ReloadAsync(selectOldestPending: true);
    }

    private void GridCellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
    {
        if (e.RowIndex < 0 || grid.Rows[e.RowIndex].DataBoundItem is not AuditTrailRow row) return;
        if (row.ReviewState == "Needs review") { grid.Rows[e.RowIndex].DefaultCellStyle.BackColor = Color.FromArgb(255, 247, 220); grid.Rows[e.RowIndex].DefaultCellStyle.SelectionBackColor = Color.FromArgb(177, 126, 25); }
        else if (row.ReviewState == "Reviewed") grid.Rows[e.RowIndex].DefaultCellStyle.BackColor = Color.FromArgb(239, 248, 242);
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData) { if (keyData == Keys.Escape) { Close(); return true; } return base.ProcessCmdKey(ref msg, keyData); }
}
