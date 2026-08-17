namespace BinTracker.WinForms;

internal enum UnsavedChangesChoice
{
    Save,
    Discard,
    Cancel
}

internal sealed class UnsavedChangesDialog : BinTrackerForm
{
    private UnsavedChangesChoice choice = UnsavedChangesChoice.Cancel;

    private UnsavedChangesDialog(string title, string message)
    {
        Text = title;
        StartPosition = FormStartPosition.CenterParent;
        AutoScaleMode = AutoScaleMode.Dpi;
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;
        MinimumSize = new Size(500, 220);
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        Font = new Font("Segoe UI", 10F);
        Padding = new Padding(22);

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 1,
            RowCount = 2,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        root.Controls.Add(new Label
        {
            Text = message,
            AutoSize = true,
            MaximumSize = new Size(520, 0),
            Margin = new Padding(0, 0, 0, 24)
        }, 0, 0);

        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            Margin = Padding.Empty
        };

        var cancel = ButtonOf("Cancel", 110);
        var discard = ButtonOf("Discard", 110);
        var save = ButtonOf("Save", 110);

        cancel.Click += (_, _) => Finish(UnsavedChangesChoice.Cancel);
        discard.Click += (_, _) => Finish(UnsavedChangesChoice.Discard);
        save.Click += (_, _) => Finish(UnsavedChangesChoice.Save);

        buttons.Controls.Add(cancel);
        buttons.Controls.Add(discard);
        buttons.Controls.Add(save);
        root.Controls.Add(buttons, 0, 1);

        AcceptButton = save;
        CancelButton = cancel;
        Controls.Add(root);
    }

    public static UnsavedChangesChoice Ask(
        IWin32Window? owner,
        string title,
        string message)
    {
        using var dialog = new UnsavedChangesDialog(title, message);
        dialog.ShowDialog(owner);
        return dialog.choice;
    }

    private void Finish(UnsavedChangesChoice result)
    {
        choice = result;
        DialogResult = result == UnsavedChangesChoice.Cancel
            ? DialogResult.Cancel
            : DialogResult.OK;
        Close();
    }

    private static Button ButtonOf(string text, int width) => new()
    {
        Text = text,
        AutoSize = false,
        Size = new Size(width, 38),
        Margin = new Padding(10, 0, 0, 0)
    };
}
