namespace BinTracker.WinForms;

/// <summary>
/// Lightweight product splash shown while BinTracker performs startup/database
/// initialisation. It deliberately contains no business/customer branding.
/// </summary>
internal sealed class SplashForm : BinTrackerForm
{
    public SplashForm()
    {
        Text = "BinTracker";
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = true;
        TopMost = true;
        ClientSize = new Size(520, 360);
        BackColor = Color.White;
        Font = new Font("Segoe UI", 10F);

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4,
            Padding = new Padding(36, 26, 36, 24)
        };
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        root.Controls.Add(new PictureBox
        {
            Image = IconAssets.Get("bintracker_logo"),
            SizeMode = PictureBoxSizeMode.Zoom,
            Dock = DockStyle.Fill,
            Margin = new Padding(80, 0, 80, 8),
            BackColor = Color.Transparent
        }, 0, 0);

        root.Controls.Add(new Label
        {
            Text = "BinTracker",
            AutoSize = true,
            Anchor = AnchorStyles.None,
            Font = new Font("Segoe UI Semibold", 25F, FontStyle.Bold),
            ForeColor = Color.FromArgb(29, 39, 54),
            Margin = new Padding(0, 0, 0, 4)
        }, 0, 1);

        root.Controls.Add(new Label
        {
            Text = "Container tracking made simple",
            AutoSize = true,
            Anchor = AnchorStyles.None,
            ForeColor = Color.FromArgb(85, 95, 110),
            Margin = new Padding(0, 0, 0, 16)
        }, 0, 2);

        root.Controls.Add(new Label
        {
            Text = $"Starting BinTracker {AppVersion.Display}…",
            AutoSize = true,
            Anchor = AnchorStyles.None,
            ForeColor = Color.DimGray
        }, 0, 3);

        Controls.Add(root);
    }
}
