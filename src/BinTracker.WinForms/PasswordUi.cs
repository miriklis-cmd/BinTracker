namespace BinTracker.WinForms;

/// <summary>
/// Creates BinTracker's standard password field with an integrated visibility eye.
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

        var eye = new PictureBox
        {
            Dock = DockStyle.Right,
            Width = 34,
            Image = IconAssets.Get("eye_open"),
            SizeMode = PictureBoxSizeMode.CenterImage,
            BackColor = SystemColors.Window,
            Cursor = Cursors.Hand,
            TabStop = false,
            AccessibleName = "Show password"
        };

        eye.Click += (_, _) =>
        {
            var masked = passwordBox.UseSystemPasswordChar;
            passwordBox.UseSystemPasswordChar = !masked;
            eye.Image = IconAssets.Get(masked ? "eye_off" : "eye_open");
            eye.AccessibleName = masked ? "Hide password" : "Show password";
            passwordBox.Focus();
            passwordBox.SelectionStart = passwordBox.TextLength;
        };

        host.Controls.Add(passwordBox);
        host.Controls.Add(eye);
        return host;
    }
}
