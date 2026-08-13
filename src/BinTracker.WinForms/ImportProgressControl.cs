namespace BinTracker.WinForms;

/// <summary>
/// Wizard progress indicator used by the Excel Import Wizard.
/// Steps are rendered as circles joined by a horizontal progress line.
/// </summary>
internal sealed class ImportProgressControl : Control
{
    private static readonly string[] Titles = ["Analyse", "Map", "Review", "Import"];
    private static readonly string[] Subtitles =
        ["Read-only", "Select data", "Preview changes", "Apply to database"];

    public int ActiveStep { get; set; } = 1;

    public ImportProgressControl()
    {
        Height = 120;
        MinimumSize = new Size(560, 120);
        BackColor = Color.White;

        SetStyle(
            ControlStyles.UserPaint |
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.ResizeRedraw,
            true);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        e.Graphics.SmoothingMode =
            System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

        var left = Math.Max(70F, Width * 0.09F);
        var right = Math.Max(left + 300F, Width - left);
        var centerY = 30F;
        var radius = 16F;
        var spacing = (right - left) / 3F;

        using var linePen = new Pen(Color.FromArgb(195, 202, 212), 2F);
        e.Graphics.DrawLine(linePen, left, centerY, right, centerY);

        using var numberFont = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold);
        using var titleFont = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold);

        for (var i = 0; i < 4; i++)
        {
            var step = i + 1;
            var x = left + spacing * i;
            var completedOrActive = step <= ActiveStep;

            using var fill = new SolidBrush(
                completedOrActive ? Color.FromArgb(25, 95, 190) : Color.White);

            using var outline = new Pen(
                completedOrActive
                    ? Color.FromArgb(25, 95, 190)
                    : Color.FromArgb(145, 155, 170),
                1.5F);

            e.Graphics.FillEllipse(
                fill,
                x - radius,
                centerY - radius,
                radius * 2,
                radius * 2);

            e.Graphics.DrawEllipse(
                outline,
                x - radius,
                centerY - radius,
                radius * 2,
                radius * 2);

            using var numberBrush = new SolidBrush(
                completedOrActive ? Color.White : Color.DimGray);

            var numberText = step.ToString();
            var numberSize = e.Graphics.MeasureString(numberText, numberFont);

            e.Graphics.DrawString(
                numberText,
                numberFont,
                numberBrush,
                x - numberSize.Width / 2F,
                centerY - numberSize.Height / 2F);

            var titleRect = new Rectangle(
                (int)x - 75,
                55,
                150,
                22);

            TextRenderer.DrawText(
                e.Graphics,
                Titles[i],
                titleFont,
                titleRect,
                Color.FromArgb(45, 55, 70),
                TextFormatFlags.HorizontalCenter |
                TextFormatFlags.VerticalCenter |
                TextFormatFlags.NoPadding |
                TextFormatFlags.SingleLine);

            var subtitleRect = new Rectangle(
                (int)x - 90,
                78,
                180,
                30);

            TextRenderer.DrawText(
                e.Graphics,
                Subtitles[i],
                Font,
                subtitleRect,
                Color.DimGray,
                TextFormatFlags.HorizontalCenter |
                TextFormatFlags.VerticalCenter |
                TextFormatFlags.SingleLine);
        }
    }
}
