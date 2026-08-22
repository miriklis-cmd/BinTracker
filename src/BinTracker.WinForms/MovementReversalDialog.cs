using BinTracker.Core;
using BinTracker.Services;

namespace BinTracker.WinForms;

internal sealed class MovementReversalDialog : BinTrackerForm
{
    private readonly TextBox reason = new()
    {
        Multiline = true,
        MaxLength = 500,
        ScrollBars = ScrollBars.Vertical,
        Dock = DockStyle.Fill,
        MinimumSize = new Size(0, 96)
    };

    public string Reason => reason.Text.Trim();

    public MovementReversalDialog(MovementCorrectionDetail movement)
    {
        Text = "Reverse Saved Movement";
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        AutoScaleMode = AutoScaleMode.Dpi;
        ClientSize = new Size(620, 540);
        MinimumSize = new Size(636, 579);
        Font = new Font("Segoe UI", 10F);

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 5,
            Padding = new Padding(22)
        };
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 130F));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 54F));

        root.Controls.Add(new Label
        {
            Text = "Reverse Saved Movement",
            AutoSize = true,
            Font = new Font("Segoe UI Semibold", 18F, FontStyle.Bold)
        }, 0, 0);

        root.Controls.Add(new Label
        {
            Text = "The original ledger row will NOT be edited or deleted. BinTracker will create an equal and opposite linked movement.",
            AutoSize = true,
            MaximumSize = new Size(560, 0),
            ForeColor = Color.FromArgb(70, 80, 95),
            Margin = new Padding(0, 8, 0, 14)
        }, 0, 1);

        root.Controls.Add(new Label
        {
            Text =
                $"Movement #{movement.MovementId}\r\n" +
                $"Date: {movement.MovementDate:dd/MM/yyyy}\r\n" +
                $"Customer: {movement.CustomerCode} — {movement.CustomerName}\r\n" +
                $"Container: {movement.ContainerType}\r\n" +
                $"Movement: {(movement.Direction == MovementType.Out ? "OUT" : "IN")} {movement.Quantity:N0}\r\n" +
                $"Source: {movement.Source}",
            AutoSize = true,
            Margin = new Padding(0, 0, 0, 14)
        }, 0, 2);

        var reasonPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        reasonPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        reasonPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 92F));
        reasonPanel.Controls.Add(new Label { Text = "Reason (required)", AutoSize = true, Margin = new Padding(0, 0, 0, 5) }, 0, 0);
        reasonPanel.Controls.Add(reason, 0, 1);
        root.Controls.Add(reasonPanel, 0, 3);

        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = false,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            Margin = Padding.Empty,
            Padding = new Padding(0, 7, 0, 7)
        };
        var reverse = new Button { Text = "Create Reversal", Size = new Size(145, 40) };
        var cancel = new Button { Text = "Cancel", Size = new Size(105, 40) };
        reverse.Click += (_, _) =>
        {
            if (Reason.Length < 3)
            {
                MessageBox.Show(this, "Enter a reason for the reversal.", "Reverse Movement",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                reason.Focus();
                return;
            }
            DialogResult = DialogResult.OK;
            Close();
        };
        cancel.Click += (_, _) => { DialogResult = DialogResult.Cancel; Close(); };
        buttons.Controls.Add(reverse);
        buttons.Controls.Add(cancel);
        root.Controls.Add(buttons, 0, 4);

        Controls.Add(root);
        AcceptButton = reverse;
        CancelButton = cancel;
    }
}
