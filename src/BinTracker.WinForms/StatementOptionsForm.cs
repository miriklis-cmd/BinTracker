namespace BinTracker.WinForms;

public sealed class StatementOptionsForm : Form
{
    private readonly DateTimePicker from = new()
    {
        Format = DateTimePickerFormat.Short,
        Dock = DockStyle.Fill
    };

    private readonly DateTimePicker to = new()
    {
        Format = DateTimePickerFormat.Short,
        Dock = DockStyle.Fill
    };

    public DateOnly FromDate => DateOnly.FromDateTime(from.Value.Date);
    public DateOnly ToDate => DateOnly.FromDateTime(to.Value.Date);

    public StatementOptionsForm()
    {
        Text = "Customer Statement Period";
        StartPosition = FormStartPosition.CenterParent;
        AutoScaleMode = AutoScaleMode.Dpi;
        ClientSize = new Size(580, 330);
        MinimumSize = new Size(540, 320);
        FormBorderStyle = FormBorderStyle.Sizable;
        MaximizeBox = false;
        MinimizeBox = false;

        to.Value = DateTime.Today;
        from.Value = DateTime.Today.AddDays(-90);

        var actionBar = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 78,
            Padding = new Padding(24, 14, 24, 14),
            BackColor = SystemColors.Control
        };

        var actions = new FlowLayoutPanel
        {
            Dock = DockStyle.Right,
            Width = 300,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Padding = new Padding(0),
            Margin = new Padding(0)
        };

        var generate = new Button
        {
            Text = "Generate PDF",
            DialogResult = DialogResult.OK,
            AutoSize = false,
            Size = new Size(160, 46),
            Margin = new Padding(0, 0, 10, 0)
        };

        var cancel = new Button
        {
            Text = "Cancel",
            DialogResult = DialogResult.Cancel,
            AutoSize = false,
            Size = new Size(120, 46),
            Margin = new Padding(0)
        };

        actions.Controls.Add(generate);
        actions.Controls.Add(cancel);
        actionBar.Controls.Add(actions);

        var body = new Panel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            Padding = new Padding(28, 26, 28, 18),
            BackColor = Color.White
        };

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            ColumnCount = 2,
            RowCount = 3
        };

        layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var heading = new Label
        {
            Text = "Statement period",
            AutoSize = true,
            Font = new Font("Segoe UI Semibold", 15F, FontStyle.Bold),
            Margin = new Padding(0, 0, 0, 20)
        };

        layout.Controls.Add(heading, 0, 0);
        layout.SetColumnSpan(heading, 2);
        layout.Controls.Add(LabelFor("From"), 0, 1);
        layout.Controls.Add(from, 1, 1);
        layout.Controls.Add(LabelFor("To"), 0, 2);
        layout.Controls.Add(to, 1, 2);

        body.Controls.Add(layout);

        Controls.Add(body);
        Controls.Add(actionBar);

        AcceptButton = generate;
        CancelButton = cancel;

        generate.Click += (_, _) =>
        {
            if (to.Value.Date < from.Value.Date)
            {
                MessageBox.Show(
                    "The statement end date must be on or after the start date.",
                    "Customer Statement",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                DialogResult = DialogResult.None;
            }
        };
    }

    private static Label LabelFor(string text) => new()
    {
        Text = text,
        AutoSize = true,
        Anchor = AnchorStyles.Left,
        Margin = new Padding(0, 9, 18, 9)
    };
}
