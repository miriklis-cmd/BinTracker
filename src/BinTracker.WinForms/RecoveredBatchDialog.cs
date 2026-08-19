using BinTracker.Core;
using BinTracker.Services;

namespace BinTracker.WinForms;

internal enum RecoveredBatchAction
{
    Continue,
    Save,
    Discard
}

internal sealed class RecoveredBatchDialog : BinTrackerForm
{
    public RecoveredBatchAction SelectedAction { get; private set; } = RecoveredBatchAction.Continue;

    public RecoveredBatchDialog(DraftMovementBatch draft, DateTimeOffset? lastSavedAtUtc)
    {
        Text = "Unsaved Batch Recovered";
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ControlBox = false;
        ShowInTaskbar = false;
        AutoScaleMode = AutoScaleMode.Dpi;
        ClientSize = new Size(560, 300);
        MinimumSize = new Size(560, 300);
        Font = new Font("Segoe UI", 10F);
        BackColor = Color.White;

        var direction = draft.MovementType == MovementType.In
            ? "Returned (IN)"
            : "Taken (OUT)";

        var lastSaved = lastSavedAtUtc.HasValue
            ? lastSavedAtUtc.Value.ToLocalTime().ToString("dd/MM/yyyy h:mm tt")
            : "Unknown";

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4,
            Padding = new Padding(24),
            Margin = Padding.Empty
        };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        root.Controls.Add(new Label
        {
            Text = "Unsaved Batch Recovered",
            AutoSize = true,
            Font = new Font("Segoe UI Semibold", 18F, FontStyle.Bold),
            ForeColor = Color.FromArgb(29, 39, 54),
            Margin = new Padding(0, 0, 0, 10)
        }, 0, 0);

        root.Controls.Add(new Label
        {
            Text = "BinTracker found an unfinished Batch Entry from the previous application session.",
            AutoSize = true,
            MaximumSize = new Size(500, 0),
            ForeColor = Color.FromArgb(70, 80, 95),
            Margin = new Padding(0, 0, 0, 16)
        }, 0, 1);

        root.Controls.Add(new Label
        {
            Text =
                $"Movement date: {draft.MovementDate:dd/MM/yyyy}\r\n" +
                $"Batch type: {direction}\r\n" +
                $"Pending lines: {draft.Lines.Count}\r\n" +
                $"Total containers: {draft.TotalQuantity}\r\n" +
                $"Last saved: {lastSaved}\r\n\r\n" +
                "Choose whether to continue working on it, save it now, or permanently discard it.",
            AutoSize = true,
            MaximumSize = new Size(500, 0),
            ForeColor = Color.FromArgb(45, 52, 62),
            Margin = Padding.Empty
        }, 0, 2);

        var buttons = new FlowLayoutPanel
        {
            AutoSize = true,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 18, 0, 0)
        };

        var continueButton = CreateButton("Continue Batch", 140);
        var saveButton = CreateButton("Save Batch", 120);
        var discardButton = CreateButton("Discard Batch", 130);

        continueButton.Click += (_, _) => Choose(RecoveredBatchAction.Continue);
        saveButton.Click += (_, _) => Choose(RecoveredBatchAction.Save);
        discardButton.Click += (_, _) => Choose(RecoveredBatchAction.Discard);

        buttons.Controls.Add(continueButton);
        buttons.Controls.Add(saveButton);
        buttons.Controls.Add(discardButton);
        root.Controls.Add(buttons, 0, 3);

        Controls.Add(root);
        AcceptButton = continueButton;
    }

    private void Choose(RecoveredBatchAction action)
    {
        SelectedAction = action;
        DialogResult = DialogResult.OK;
        Close();
    }

    private static Button CreateButton(string text, int width) => new()
    {
        Text = text,
        Width = width,
        Height = 40,
        Margin = new Padding(8, 0, 0, 0),
        UseVisualStyleBackColor = true
    };
}
