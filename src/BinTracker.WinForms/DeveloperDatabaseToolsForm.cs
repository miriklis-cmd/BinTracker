using BinTracker.Data;

namespace BinTracker.WinForms;

public sealed class DeveloperDatabaseToolsForm : Form
{
    private readonly IDeveloperDatabaseService service;
    private readonly Action requestRestart;

    private readonly Label activeDatabase = new()
    {
        AutoSize = true,
        MaximumSize = new Size(1020, 0),
        ForeColor = Color.FromArgb(65, 75, 90)
    };

    private readonly Label status = new()
    {
        AutoSize = true,
        MaximumSize = new Size(1020, 0),
        ForeColor = Color.FromArgb(25, 95, 190)
    };

    public DeveloperDatabaseToolsForm(
        IDeveloperDatabaseService service,
        Action requestRestart)
    {
        this.service = service;
        this.requestRestart = requestRestart;

        Text = "Developer Database Tools";
        StartPosition = FormStartPosition.CenterParent;
        AutoScaleMode = AutoScaleMode.Dpi;
        ClientSize = new Size(1180, 820);
        MinimumSize = new Size(1080, 760);
        BackColor = Color.FromArgb(245, 247, 250);
        Font = new Font("Segoe UI", 10F);

        Build();
        RefreshStatus();
    }

    private void Build()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Padding = new Padding(18)
        };
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var scroller = new Panel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            Margin = Padding.Empty
        };

        var content = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 1,
            RowCount = 3,
            Margin = Padding.Empty
        };
        content.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        content.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        content.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        content.Controls.Add(HeaderCard(), 0, 0);
        content.Controls.Add(CurrentDatabaseCard(), 0, 1);
        content.Controls.Add(ActionsCard(), 0, 2);

        scroller.Controls.Add(content);

        root.Controls.Add(scroller, 0, 0);
        root.Controls.Add(Footer(), 0, 1);

        Controls.Add(root);
    }

    private Control HeaderCard()
    {
        var card = Card();

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            ColumnCount = 1,
            RowCount = 2
        };

        layout.Controls.Add(new Label
        {
            Text = "Developer Database Tools",
            AutoSize = true,
            Font = new Font("Segoe UI Semibold", 18F, FontStyle.Bold)
        }, 0, 0);

        layout.Controls.Add(new Label
        {
            Text =
                "Switch between clean import testing and an existing merge-test database. " +
                "Load/Fresh operations are applied safely on restart.",
            AutoSize = true,
            MaximumSize = new Size(1020, 0),
            ForeColor = Color.DimGray,
            Margin = new Padding(0, 6, 0, 0)
        }, 0, 1);

        card.Controls.Add(layout);
        return card;
    }

    private Control CurrentDatabaseCard()
    {
        var card = Card();

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            ColumnCount = 1,
            RowCount = 3
        };

        layout.Controls.Add(Heading("Current database"), 0, 0);
        layout.Controls.Add(activeDatabase, 0, 1);
        status.Margin = new Padding(0, 8, 0, 0);
        layout.Controls.Add(status, 0, 2);

        card.Controls.Add(layout);
        return card;
    }

    private Control ActionsCard()
    {
        var card = Card();
        card.Dock = DockStyle.Fill;

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            ColumnCount = 1,
            RowCount = 4
        };

        layout.Controls.Add(Heading("Test database actions"), 0, 0);

        var backup = ActionRow(
            "Backup Database",
            "Save a consistent copy of the currently active SQLite database.",
            BackupAsync);

        var load = ActionRow(
            "Load Database",
            "Choose a previous BinTracker database. The current database is automatically backed up, then BinTracker restarts into the selected database.",
            LoadAsync);

        var fresh = ActionRow(
            "Start Fresh Test Database",
            "Automatically back up the current database, restart BinTracker, and create a brand-new empty database for clean import testing.",
            FreshAsync);

        layout.Controls.Add(backup, 0, 1);
        layout.Controls.Add(load, 0, 2);
        layout.Controls.Add(fresh, 0, 3);

        card.Controls.Add(layout);
        return card;
    }

    private Control ActionRow(
        string caption,
        string description,
        Func<Task> action)
    {
        var row = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 2,
            Margin = new Padding(0, 10, 0, 0)
        };
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 300F));

        row.Controls.Add(new Label
        {
            Text = description,
            AutoSize = true,
            MaximumSize = new Size(720, 0),
            ForeColor = Color.FromArgb(70, 80, 95),
            Margin = new Padding(0, 8, 16, 8)
        }, 0, 0);

        var button = new Button
        {
            Text = caption,
            Dock = DockStyle.Fill,
            Height = 44,
            MinimumSize = new Size(270, 44),
            Margin = new Padding(0, 4, 0, 4),
            TextAlign = ContentAlignment.MiddleCenter
        };

        button.Click += async (_, _) =>
        {
            try
            {
                button.Enabled = false;
                await action();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    this,
                    ex.Message,
                    "Developer Database Tools",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            finally
            {
                if (!IsDisposed)
                    button.Enabled = true;
            }
        };

        row.Controls.Add(button, 1, 0);
        return row;
    }

    private async Task BackupAsync()
    {
        using var dialog = new SaveFileDialog
        {
            Title = "Backup BinTracker database",
            Filter = "BinTracker SQLite database (*.db)|*.db|All files (*.*)|*.*",
            FileName = $"BinTracker-backup-{DateTime.Now:yyyyMMdd-HHmmss}.db",
            AddExtension = true,
            DefaultExt = "db"
        };

        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;

        await service.BackupAsync(dialog.FileName);

        status.ForeColor = Color.ForestGreen;
        status.Text = $"Backup created: {dialog.FileName}";
    }

    private async Task LoadAsync()
    {
        using var dialog = new OpenFileDialog
        {
            Title = "Load BinTracker database",
            Filter = "BinTracker SQLite database (*.db)|*.db|All files (*.*)|*.*",
            CheckFileExists = true,
            Multiselect = false
        };

        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;

        var confirm = MessageBox.Show(
            this,
            $"Load this database on restart?\n\n{dialog.FileName}\n\n" +
            "The current database will be automatically backed up first.",
            "Load Developer Database",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning);

        if (confirm != DialogResult.Yes)
            return;

        var backup = await service.StageLoadAsync(dialog.FileName);

        MessageBox.Show(
            this,
            $"Database staged successfully.\n\nCurrent database backup:\n{backup}\n\n" +
            "BinTracker will restart now.",
            "Load Developer Database",
            MessageBoxButtons.OK,
            MessageBoxIcon.Information);

        requestRestart();
        Close();
    }

    private async Task FreshAsync()
    {
        var confirm = MessageBox.Show(
            this,
            "Start with a completely fresh BinTracker database?\n\n" +
            "Your current database will be automatically backed up first. " +
            "After restart you will go through first-run Administrator setup again.",
            "Start Fresh Test Database",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning);

        if (confirm != DialogResult.Yes)
            return;

        var backup = await service.StageFreshAsync();

        MessageBox.Show(
            this,
            $"Fresh database staged.\n\nCurrent database backup:\n{backup}\n\n" +
            "BinTracker will restart now.",
            "Start Fresh Test Database",
            MessageBoxButtons.OK,
            MessageBoxIcon.Information);

        requestRestart();
        Close();
    }

    private void RefreshStatus()
    {
        var value = service.GetStatus();
        activeDatabase.Text = value.ActiveDatabasePath;
        status.Text = value.PendingRestart
            ? "A database switch is staged and will apply on restart."
            : $"Automatic developer backups: {value.BackupFolder}";
    }

    private Control Footer()
    {
        var panel = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            Margin = new Padding(0, 8, 0, 0)
        };

        var close = new Button
        {
            Text = "Close",
            AutoSize = false,
            Size = new Size(120, 40),
            TextAlign = ContentAlignment.MiddleCenter
        };
        close.Click += (_, _) => Close();

        panel.Controls.Add(close);
        return panel;
    }

    private static Label Heading(string text) => new()
    {
        Text = text,
        AutoSize = true,
        Font = new Font("Segoe UI Semibold", 11F, FontStyle.Bold)
    };

    private static Panel Card() => new()
    {
        Dock = DockStyle.Top,
        AutoSize = true,
        AutoSizeMode = AutoSizeMode.GrowAndShrink,
        BackColor = Color.White,
        Padding = new Padding(18),
        Margin = new Padding(0, 0, 0, 8)
    };
}
