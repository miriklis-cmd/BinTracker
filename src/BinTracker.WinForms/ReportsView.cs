using BinTracker.Services;
using System.Diagnostics;

namespace BinTracker.WinForms;

public sealed class ReportsView : UserControl
{
    private readonly IMarketFloorReportService marketFloor;
    private readonly Action openOutstanding;
    private readonly Action openDailyMovements;
    private readonly Action openWeeklyMovements;
    private readonly Action openMovementHistory;
    private readonly Action openCustomerStatement;
    private readonly Action openMonthlySummary;

    private readonly DateTimePicker reportDate = new()
    {
        Format = DateTimePickerFormat.Short,
        Width = 145,
        Value = DateTime.Today
    };

    private readonly Label status = new()
    {
        AutoSize = true,
        ForeColor = Color.DimGray,
        MaximumSize = new Size(900, 0)
    };

    public ReportsView(
        IMarketFloorReportService marketFloor,
        Action openOutstanding,
        Action openDailyMovements,
        Action openWeeklyMovements,
        Action openMovementHistory,
        Action openCustomerStatement,
        Action openMonthlySummary)
    {
        AutoScroll = true;
        this.marketFloor = marketFloor;
        this.openOutstanding = openOutstanding;
        this.openDailyMovements = openDailyMovements;
        this.openWeeklyMovements = openWeeklyMovements;
        this.openMovementHistory = openMovementHistory;
        this.openCustomerStatement = openCustomerStatement;
        this.openMonthlySummary = openMonthlySummary;

        Dock = DockStyle.Fill;
        BackColor = Color.FromArgb(245, 247, 250);
        Font = new Font("Segoe UI", 10F);

        Build();
    }

    private void Build()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 1,
            RowCount = 3,
            Padding = Padding.Empty,
            Margin = Padding.Empty
        };

        root.Controls.Add(BuildMarketFloorCard(), 0, 0);
        root.Controls.Add(BuildReportLauncherSection(), 0, 1);
        root.Controls.Add(BuildComingReports(), 0, 2);

        Controls.Add(root);
    }

    private Control BuildMarketFloorCard()
    {
        var panel = Card();

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            ColumnCount = 1,
            RowCount = 5
        };

        layout.Controls.Add(new Label
        {
            Text = "Market Floor Sheet",
            AutoSize = true,
            Font = new Font(
                "Segoe UI Semibold",
                18F,
                FontStyle.Bold),
            Margin = new Padding(0, 0, 0, 8)
        }, 0, 0);

        layout.Controls.Add(new Label
        {
            Text =
                "Two-page duplex floor report. Front: Account owing in two columns, Cash owing, Credits and special containers. " +
                "Reverse: Account and Cash daily Out / In / B/Fwd / Total.",
            AutoSize = true,
            ForeColor = Color.FromArgb(80, 90, 105),
            MaximumSize = new Size(980, 0),
            Margin = new Padding(0, 0, 0, 14)
        }, 0, 1);

        var controls = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true
        };

        controls.Controls.Add(new Label
        {
            Text = "Report date",
            AutoSize = true,
            Margin = new Padding(0, 9, 8, 0)
        });
        controls.Controls.Add(reportDate);

        var generate = new Button
        {
            Text = "Generate PDF",
            AutoSize = false,
            Size = new Size(145, 40),
            Margin = new Padding(16, 0, 0, 0)
        };
        generate.Click +=
            async (_, _) =>
                await GenerateAsync(openAfter: false);

        var generateAndOpen = new Button
        {
            Text = "Generate && Open",
            AutoSize = false,
            Size = new Size(190, 40),
            Margin = new Padding(10, 0, 0, 0)
        };
        generateAndOpen.Click +=
            async (_, _) =>
                await GenerateAsync(openAfter: true);

        controls.Controls.Add(generate);
        controls.Controls.Add(generateAndOpen);

        layout.Controls.Add(controls, 0, 2);

        layout.Controls.Add(new Label
        {
            Text =
                "Print double-sided: Page 1 is the floor reference sheet; Page 2 is the reverse-side daily worksheet.",
            AutoSize = true,
            ForeColor = Color.DimGray,
            Margin = new Padding(0, 12, 0, 6)
        }, 0, 3);

        layout.Controls.Add(status, 0, 4);

        panel.Controls.Add(layout);
        return panel;
    }

    private Control BuildReportLauncherSection()
    {
        var panel = Card();

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 2,
            RowCount = 4
        };

        layout.ColumnStyles.Add(
            new ColumnStyle(SizeType.Percent, 50F));
        layout.ColumnStyles.Add(
            new ColumnStyle(SizeType.Percent, 50F));

        var title = new Label
        {
            Text = "Other Reports",
            AutoSize = true,
            Font = new Font(
                "Segoe UI Semibold",
                17F,
                FontStyle.Bold),
            Margin = new Padding(0, 0, 0, 12)
        };

        layout.Controls.Add(title, 0, 0);
        layout.SetColumnSpan(title, 2);

        layout.Controls.Add(
            ReportLauncher(
                "Outstanding Containers",
                "Current or historical customer/container positions, with filters and CSV export.",
                openOutstanding),
            0,
            1);

        layout.Controls.Add(
            ReportLauncher(
                "Daily Movements",
                "Physical IN/OUT activity for today, yesterday or any selected day, with filters, PDF and CSV.",
                openDailyMovements),
            1,
            1);

        layout.Controls.Add(
            ReportLauncher(
                "Weekly Movements",
                "Monday-to-Sunday movement detail plus customer/container OUT, IN and net summary.",
                openWeeklyMovements),
            0,
            2);

        layout.Controls.Add(
            ReportLauncher(
                "Movement History",
                "Search actual movement history across a date range with customer, container, direction and source filters.",
                openMovementHistory),
            1,
            2);

        layout.Controls.Add(
            ReportLauncher(
                "Customer Statement",
                "Select a customer and generate, open or print a statement for the required period.",
                openCustomerStatement),
            0,
            3);

        layout.Controls.Add(
            ReportLauncher(
                "Monthly Summary",
                "Selected-month OUT, IN and net movement totals by customer and container, with PDF and CSV.",
                openMonthlySummary),
            1,
            3);

        panel.Controls.Add(layout);
        return panel;
    }

    private static Control ReportLauncher(
        string title,
        string description,
        Action open)
    {
        var card = LauncherCard();

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            ColumnCount = 1,
            RowCount = 3
        };

        layout.Controls.Add(new Label
        {
            Text = title,
            AutoSize = true,
            Font = new Font(
                "Segoe UI Semibold",
                13F,
                FontStyle.Bold),
            Margin = new Padding(0, 0, 0, 6)
        }, 0, 0);

        layout.Controls.Add(new Label
        {
            Text = description,
            AutoSize = true,
            ForeColor = Color.FromArgb(80, 90, 105),
            MaximumSize = new Size(470, 0),
            Margin = new Padding(0, 0, 0, 12)
        }, 0, 1);

        var button = new Button
        {
            Text = "Open Report",
            AutoSize = false,
            Size = new Size(135, 38),
            Margin = Padding.Empty
        };
        button.Click += (_, _) => open();

        layout.Controls.Add(button, 0, 2);
        card.Controls.Add(layout);

        return card;
    }

    private static Control ComingSoonLauncher(
        string title,
        string description)
    {
        var card = LauncherCard();

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            ColumnCount = 1,
            RowCount = 3
        };

        layout.Controls.Add(new Label
        {
            Text = title,
            AutoSize = true,
            Font = new Font(
                "Segoe UI Semibold",
                13F,
                FontStyle.Bold),
            Margin = new Padding(0, 0, 0, 6)
        }, 0, 0);

        layout.Controls.Add(new Label
        {
            Text = description,
            AutoSize = true,
            ForeColor = Color.FromArgb(80, 90, 105),
            MaximumSize = new Size(470, 0),
            Margin = new Padding(0, 0, 0, 12)
        }, 0, 1);

        layout.Controls.Add(new Label
        {
            Text = "Coming during the Reports phase",
            AutoSize = true,
            ForeColor = Color.DimGray,
            Margin = Padding.Empty
        }, 0, 2);

        card.Controls.Add(layout);
        return card;
    }

    private Control BuildComingReports()
    {
        var panel = Card();

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            ColumnCount = 1
        };

        layout.Controls.Add(new Label
        {
            Text = "Reports phase",
            AutoSize = true,
            Font = new Font(
                "Segoe UI Semibold",
                15F,
                FontStyle.Bold),
            Margin = new Padding(0, 0, 0, 8)
        });

        layout.Controls.Add(new Label
        {
            Text =
                "Next: Daily Print Pack",
            AutoSize = true,
            ForeColor = Color.DimGray
        });

        panel.Controls.Add(layout);
        return panel;
    }

    private async Task GenerateAsync(bool openAfter)
    {
        status.Text = string.Empty;

        var date =
            DateOnly.FromDateTime(reportDate.Value.Date);

        using var dialog = new SaveFileDialog
        {
            Title = "Save Market Floor Sheet",
            Filter = "PDF document (*.pdf)|*.pdf",
            FileName =
                $"BinTracker_Market_Floor_{date:yyyyMMdd}.pdf",
            AddExtension = true,
            DefaultExt = "pdf"
        };

        if (dialog.ShowDialog(FindForm()) != DialogResult.OK)
            return;

        try
        {
            Enabled = false;

            await marketFloor.GeneratePdfAsync(
                date,
                dialog.FileName);

            status.Text =
                $"Created 2-page Market Floor Sheet for " +
                $"{date:dd/MM/yyyy}: {dialog.FileName}";

            if (openAfter)
            {
                Process.Start(
                    new ProcessStartInfo
                    {
                        FileName = dialog.FileName,
                        UseShellExecute = true
                    });
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                ex.Message,
                "Market Floor Sheet",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
        finally
        {
            Enabled = true;
        }
    }

    private static Panel Card() => new()
    {
        Dock = DockStyle.Top,
        AutoSize = true,
        AutoSizeMode = AutoSizeMode.GrowAndShrink,
        BackColor = Color.White,
        Padding = new Padding(24),
        Margin = new Padding(0, 0, 0, 16)
    };

    private static Panel LauncherCard() => new()
    {
        Dock = DockStyle.Fill,
        AutoSize = true,
        AutoSizeMode = AutoSizeMode.GrowAndShrink,
        BackColor = Color.FromArgb(250, 251, 253),
        BorderStyle = BorderStyle.FixedSingle,
        Padding = new Padding(16),
        Margin = new Padding(0, 0, 12, 0)
    };
}
