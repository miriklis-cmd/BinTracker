namespace BinTracker.WinForms;

/// <summary>
/// Creates BinTracker's standard password field with a DPI-safe, custom-drawn
/// visibility eye. The eye is rendered directly by WinForms so it cannot fail
/// because of PNG scaling, transparency or resource extraction.
/// </summary>
internal static class PasswordUi
{
    public static Control WithVisibilityToggle(TextBox passwordBox)
    {
        passwordBox.UseSystemPasswordChar = true;
        passwordBox.BorderStyle = BorderStyle.None;
        passwordBox.Dock = DockStyle.Fill;
        passwordBox.Margin = new Padding(8, 8, 0, 0);

        var host = new Panel
        {
            Dock = DockStyle.Top,
            Height = 36,
            MinimumSize = new Size(0, 36),
            BackColor = SystemColors.Window,
            BorderStyle = BorderStyle.FixedSingle,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };

        var eye = new EyeToggleControl(passwordBox)
        {
            Dock = DockStyle.Right,
            Width = 38,
            BackColor = SystemColors.Window,
            Cursor = Cursors.Hand,
            TabStop = false,
            AccessibleName = "Show password"
        };

        host.Controls.Add(passwordBox);
        host.Controls.Add(eye);
        return host;
    }

    private sealed class EyeToggleControl : Control
    {
        private readonly TextBox passwordBox;
        private bool showing;

        public EyeToggleControl(TextBox passwordBox)
        {
            this.passwordBox = passwordBox;
            SetStyle(
                ControlStyles.UserPaint |
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw,
                true);
        }

        protected override void OnClick(EventArgs e)
        {
            base.OnClick(e);

            showing = !showing;
            passwordBox.UseSystemPasswordChar = !showing;
            AccessibleName = showing ? "Hide password" : "Show password";

            passwordBox.Focus();
            passwordBox.SelectionStart = passwordBox.TextLength;
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            using var pen = new Pen(Color.FromArgb(42, 57, 79), 1.8F);
            using var pupil = new SolidBrush(Color.FromArgb(42, 57, 79));

            var cx = ClientRectangle.Width / 2F;
            var cy = ClientRectangle.Height / 2F;
            var eyeWidth = 20F;
            var eyeHeight = 11F;

            var left = cx - eyeWidth / 2F;
            var right = cx + eyeWidth / 2F;
            var top = cy - eyeHeight / 2F;
            var bottom = cy + eyeHeight / 2F;

            using var upper = new System.Drawing.Drawing2D.GraphicsPath();
            upper.AddBezier(left, cy, cx - 5F, top, cx + 5F, top, right, cy);

            using var lower = new System.Drawing.Drawing2D.GraphicsPath();
            lower.AddBezier(left, cy, cx - 5F, bottom, cx + 5F, bottom, right, cy);

            e.Graphics.DrawPath(pen, upper);
            e.Graphics.DrawPath(pen, lower);
            e.Graphics.FillEllipse(pupil, cx - 2.6F, cy - 2.6F, 5.2F, 5.2F);

            if (showing)
            {
                using var slashPen = new Pen(Color.FromArgb(42, 57, 79), 1.8F);
                e.Graphics.DrawLine(
                    slashPen,
                    left + 1F, top - 1F,
                    right - 1F, bottom + 1F);
            }
        }
    }
}
