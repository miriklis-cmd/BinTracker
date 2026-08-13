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

            e.Graphics.SmoothingMode =
                System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            var navy = Color.FromArgb(30, 61, 112);
            var cx = ClientRectangle.Width / 2F;
            var cy = ClientRectangle.Height / 2F;
            var eyeWidth = 22F;
            var eyeHeight = 14F;

            var left = cx - eyeWidth / 2F;
            var right = cx + eyeWidth / 2F;
            var top = cy - eyeHeight / 2F;
            var bottom = cy + eyeHeight / 2F;

            using var eyePath = new System.Drawing.Drawing2D.GraphicsPath();
            eyePath.AddBezier(
                left, cy,
                cx - 6F, top - 1F,
                cx + 6F, top - 1F,
                right, cy);
            eyePath.AddBezier(
                right, cy,
                cx + 6F, bottom + 1F,
                cx - 6F, bottom + 1F,
                left, cy);
            eyePath.CloseFigure();

            using var eyeBrush = new SolidBrush(navy);
            e.Graphics.FillPath(eyeBrush, eyePath);

            using var white = new SolidBrush(Color.White);
            e.Graphics.FillEllipse(white, cx - 5.1F, cy - 5.1F, 10.2F, 10.2F);

            using var pupil = new SolidBrush(navy);
            e.Graphics.FillEllipse(pupil, cx - 2.8F, cy - 2.8F, 5.6F, 5.6F);

            // Hidden password = normal eye ("show password").
            // Visible password = eye with slash ("hide password").
            if (showing)
            {
                using var underlay = new Pen(Color.White, 4.6F)
                {
                    StartCap = System.Drawing.Drawing2D.LineCap.Square,
                    EndCap = System.Drawing.Drawing2D.LineCap.Square
                };
                using var slash = new Pen(navy, 2.8F)
                {
                    StartCap = System.Drawing.Drawing2D.LineCap.Square,
                    EndCap = System.Drawing.Drawing2D.LineCap.Square
                };

                var x1 = left + 1F;
                var y1 = bottom + 3F;
                var x2 = right - 1F;
                var y2 = top - 3F;

                e.Graphics.DrawLine(underlay, x1, y1, x2, y2);
                e.Graphics.DrawLine(slash, x1, y1, x2, y2);
            }
        }
    }
}
