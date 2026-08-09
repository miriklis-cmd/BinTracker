namespace BinTracker.WinForms;

/// <summary>
/// Provides the standard BinTracker password-entry control.
/// Passwords remain masked by default; the eye button toggles visibility
/// without altering the underlying TextBox value.
/// </summary>
internal static class PasswordUi
{
    public static Control WithVisibilityToggle(TextBox passwordBox)
    {
        passwordBox.Dock = DockStyle.Fill;
        passwordBox.UseSystemPasswordChar = true;
        passwordBox.Margin = Padding.Empty;

        var host = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = false,
            Height = 36,
            MinimumSize = new Size(0, 36),
            ColumnCount = 2,
            RowCount = 1,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };

        host.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        host.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 40F));

        var eye = new Button
        {
            Dock = DockStyle.Fill,
            FlatStyle = FlatStyle.Flat,
            Image = IconAssets.Get("eye_open"),
            Margin = Padding.Empty,
            Padding = Padding.Empty,
            TabStop = false,
            Cursor = Cursors.Hand,
            AccessibleName = "Show password"
        };

        eye.FlatAppearance.BorderSize = 1;

        eye.Click += (_, _) =>
        {
            var showing = passwordBox.UseSystemPasswordChar;
            passwordBox.UseSystemPasswordChar = !showing;
            eye.Image = IconAssets.Get(showing ? "eye_off" : "eye_open");
            eye.AccessibleName = showing ? "Hide password" : "Show password";

            // Keep the caret/focus in the password field so keyboard entry
            // continues naturally after toggling visibility.
            passwordBox.Focus();
            passwordBox.SelectionStart = passwordBox.TextLength;
        };

        host.Controls.Add(passwordBox, 0, 0);
        host.Controls.Add(eye, 1, 0);

        return host;
    }
}
