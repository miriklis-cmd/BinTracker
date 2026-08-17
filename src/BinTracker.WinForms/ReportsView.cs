using BinTracker.Services;
using System.Diagnostics;
using System.Drawing.Drawing2D;

namespace BinTracker.WinForms;

/// <summary>
/// Reports landing page. The layout intentionally mirrors the approved
/// alpha.24 mock-up: two prominent Quick Reports followed by a 3x2
/// Explore Reports grid. The report icon artwork is embedded from the
/// approved mock-up so it is not substituted with a different icon set.
/// </summary>
public sealed class ReportsView : UserControl
{
    private static readonly Color PageBackground = Color.FromArgb(248, 249, 251);
    private static readonly Color CardBackground = Color.White;
    private static readonly Color CardBorder = Color.FromArgb(225, 229, 235);
    private static readonly Color Primary = Color.FromArgb(15, 91, 209);
    private static readonly Color BodyText = Color.FromArgb(39, 44, 55);
    private static readonly Color MutedText = Color.FromArgb(75, 82, 96);

    private readonly IMarketFloorReportService marketFloor;
    private readonly IDailyPrintPackService dailyPrintPack;
    private readonly Action openOutstanding;
    private readonly Action openDailyMovements;
    private readonly Action openWeeklyMovements;
    private readonly Action openMovementHistory;
    private readonly Action openCustomerStatement;
    private readonly Action openMonthlySummary;

    private readonly DateTimePicker reportDate = ReportDatePicker();
    private readonly DateTimePicker printPackDate = ReportDatePicker();

    private readonly Label status = StatusLabel();
    private readonly Label printPackStatus = StatusLabel();

    public ReportsView(
        IMarketFloorReportService marketFloor,
        IDailyPrintPackService dailyPrintPack,
        Action openOutstanding,
        Action openDailyMovements,
        Action openWeeklyMovements,
        Action openMovementHistory,
        Action openCustomerStatement,
        Action openMonthlySummary)
    {
        this.marketFloor = marketFloor;
        this.dailyPrintPack = dailyPrintPack;
        this.openOutstanding = openOutstanding;
        this.openDailyMovements = openDailyMovements;
        this.openWeeklyMovements = openWeeklyMovements;
        this.openMovementHistory = openMovementHistory;
        this.openCustomerStatement = openCustomerStatement;
        this.openMonthlySummary = openMonthlySummary;

        reportDate.MaxDate = DateTime.Today;
        printPackDate.MaxDate = DateTime.Today;

        Dock = DockStyle.Fill;
        BackColor = PageBackground;
        Font = new Font("Segoe UI", 10F);
        // ReportsView uses one dedicated vertical scroll host.  Do not make the
        // UserControl itself scrollable: WinForms can otherwise expose a tiny
        // horizontal scrollbar when the vertical scrollbar reduces ClientSize
        // after DPI scaling.
        AutoScroll = false;
        Padding = new Padding(0);

        Build();
    }

    private void Build()
    {
        SuspendLayout();

        var scrollHost = new Panel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            AutoScrollMargin = Size.Empty,
            Padding = Padding.Empty,
            Margin = Padding.Empty,
            BackColor = PageBackground
        };

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 1,
            RowCount = 6,
            Padding = new Padding(0),
            Margin = new Padding(0),
            BackColor = PageBackground
        };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

        root.Controls.Add(SectionHeader(
            "reports_quick_section",
            "Quick Reports"), 0, 0);

        var quickGrid = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = false,
            Height = 270,
            MinimumSize = new Size(0, 270),
            ColumnCount = 2,
            RowCount = 1,
            Margin = new Padding(0, 8, 0, 20),
            Padding = new Padding(0)
        };
        quickGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
        quickGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
        quickGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

        quickGrid.Controls.Add(
            BuildQuickReportCard(
                "report_market",
                "Market Floor Sheet",
                "Daily two-page floor worksheet.",
                reportDate,
                GenerateMarketFloorAsync),
            0, 0);

        quickGrid.Controls.Add(
            BuildQuickReportCard(
                "report_print",
                "Daily Print Pack",
                "Outstanding containers + today's movements.",
                printPackDate,
                GeneratePrintPackAsync),
            1, 0);

        root.Controls.Add(quickGrid, 0, 1);

        root.Controls.Add(Separator(), 0, 2);

        root.Controls.Add(SectionHeader(
            "reports_explore_section",
            "Explore Reports"), 0, 3);

        var exploreGrid = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = false,
            Height = 390,
            MinimumSize = new Size(0, 390),
            ColumnCount = 3,
            RowCount = 2,
            Margin = new Padding(0, 8, 0, 16),
            Padding = new Padding(0)
        };

        exploreGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.333F));
        exploreGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.333F));
        exploreGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.334F));
        exploreGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
        exploreGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));

        exploreGrid.Controls.Add(
            ExploreReportCard(
                "report_outstanding",
                "Outstanding Containers",
                "Who currently has containers\nand historical positions.",
                openOutstanding),
            0, 0);

        exploreGrid.Controls.Add(
            ExploreReportCard(
                "report_daily",
                "Daily Movements",
                "Physical IN/OUT activity\nfor a selected day.",
                openDailyMovements),
            1, 0);

        exploreGrid.Controls.Add(
            ExploreReportCard(
                "report_weekly",
                "Weekly Movements",
                "Weekly activity and\ncustomer/container totals.",
                openWeeklyMovements),
            2, 0);

        exploreGrid.Controls.Add(
            ExploreReportCard(
                "report_history",
                "Movement History",
                "Search actual movement history\nacross a date range.",
                openMovementHistory),
            0, 1);

        exploreGrid.Controls.Add(
            ExploreReportCard(
                "report_monthly",
                "Monthly Summary",
                "Monthly IN/OUT and net\nmovement totals.",
                openMonthlySummary),
            1, 1);

        exploreGrid.Controls.Add(
            ExploreReportCard(
                "report_statement",
                "Customer Statement",
                "Generate a statement for a\ncustomer and period.",
                openCustomerStatement),
            2, 1);

        root.Controls.Add(exploreGrid, 0, 4);

        root.Controls.Add(InformationBar(), 0, 5);

        // Keep the content exactly inside the scroll host's current client
        // width.  This is deliberately explicit rather than relying only on
        // Dock=Top because the vertical scrollbar changes ClientSize after
        // layout at 125%/150% DPI.
        void FitRootToViewport()
        {
            var width = Math.Max(1, scrollHost.ClientSize.Width);
            if (root.Width != width)
                root.Width = width;
        }

        scrollHost.Controls.Add(root);
        scrollHost.ClientSizeChanged += (_, _) => FitRootToViewport();
        root.SizeChanged += (_, _) =>
        {
            if (root.Width > scrollHost.ClientSize.Width)
                FitRootToViewport();
        };

        Controls.Add(scrollHost);
        FitRootToViewport();
        ResumeLayout(true);
    }

    private Control BuildQuickReportCard(
        string iconName,
        string title,
        string description,
        DateTimePicker datePicker,
        Func<bool, Task> generate)
    {
        var card = new RoundedPanel
        {
            Dock = DockStyle.Fill,
            MinimumSize = Size.Empty,
            BackColor = CardBackground,
            BorderColor = CardBorder,
            CornerRadius = 10,
            Padding = new Padding(22, 20, 22, 20),
            Margin = new Padding(0, 0, 12, 0)
        };

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 3,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 92F));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

        var icon = new PictureBox
        {
            Image = IconAssets.Get(iconName),
            SizeMode = PictureBoxSizeMode.Zoom,
            Size = new Size(82, 82),
            Margin = new Padding(0, 0, 10, 0),
            BackColor = Color.Transparent
        };
        layout.Controls.Add(icon, 0, 0);
        layout.SetRowSpan(icon, 3);

        var headingBlock = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            ColumnCount = 1,
            RowCount = 2,
            Margin = new Padding(8, 10, 0, 0),
            Padding = Padding.Empty
        };

        headingBlock.Controls.Add(new Label
        {
            Text = title,
            AutoSize = true,
            Font = new Font("Segoe UI Semibold", 14F, FontStyle.Bold),
            ForeColor = Color.Black,
            Margin = new Padding(0, 0, 0, 6)
        }, 0, 0);

        headingBlock.Controls.Add(new Label
        {
            Text = description,
            AutoSize = true,
            ForeColor = MutedText,
            Margin = Padding.Empty
        }, 0, 1);

        layout.Controls.Add(headingBlock, 1, 0);

        var dateRow = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Margin = new Padding(8, 18, 0, 0),
            Padding = Padding.Empty
        };
        dateRow.Controls.Add(new Label
        {
            Text = "Date",
            AutoSize = true,
            Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold),
            ForeColor = Color.Black,
            Margin = new Padding(0, 8, 10, 0)
        });
        datePicker.Margin = Padding.Empty;
        dateRow.Controls.Add(datePicker);
        layout.Controls.Add(dateRow, 1, 1);

        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Margin = new Padding(8, 18, 0, 0),
            Padding = Padding.Empty
        };

        var generatePdf = SecondaryActionButton("Generate PDF", 182);
        generatePdf.Image = CreateDocumentIcon(Color.FromArgb(24, 28, 36));
        ConfigureButtonImage(generatePdf);
        generatePdf.Click += async (_, _) => await generate(false);

        var generateOpen = PrimaryActionButton("Generate && Open", 210);
        generateOpen.Image = CreateExternalLinkIcon(Color.White);
        ConfigureButtonImage(generateOpen);
        generateOpen.Margin = new Padding(12, 0, 0, 0);
        generateOpen.Click += async (_, _) => await generate(true);

        buttons.Controls.Add(generatePdf);
        buttons.Controls.Add(generateOpen);

        layout.Controls.Add(buttons, 1, 2);

        card.Controls.Add(layout);

        // Keep operational status available without changing the approved visual
        // hierarchy. It is surfaced as a tooltip after generation.
        var statusLabel = title == "Market Floor Sheet" ? status : printPackStatus;
        var tooltip = new ToolTip();
        statusLabel.TextChanged += (_, _) =>
        {
            if (!string.IsNullOrWhiteSpace(statusLabel.Text))
                tooltip.SetToolTip(card, statusLabel.Text);
        };

        return card;
    }

    private static Control ExploreReportCard(
        string iconName,
        string title,
        string description,
        Action open)
    {
        var card = new RoundedPanel
        {
            Dock = DockStyle.Fill,
            MinimumSize = Size.Empty,
            BackColor = CardBackground,
            BorderColor = CardBorder,
            CornerRadius = 9,
            Padding = new Padding(18),
            Margin = new Padding(0, 0, 12, 12),
            Cursor = Cursors.Hand
        };

        var body = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 2,
            Padding = Padding.Empty,
            Margin = Padding.Empty
        };
        body.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 82F));
        body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        body.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        body.RowStyles.Add(new RowStyle(SizeType.Absolute, 42F));

        var icon = new PictureBox
        {
            Image = IconAssets.Get(iconName),
            SizeMode = PictureBoxSizeMode.Zoom,
            Size = new Size(72, 72),
            Margin = new Padding(0, 4, 10, 0),
            BackColor = Color.Transparent,
            Cursor = Cursors.Hand
        };

        var text = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = false,
            ColumnCount = 1,
            RowCount = 2,
            Margin = new Padding(2, 5, 0, 0),
            Padding = Padding.Empty
        };
        text.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        text.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

        var titleLabel = new Label
        {
            Text = title,
            AutoSize = true,
            Font = new Font("Segoe UI Semibold", 12.25F, FontStyle.Bold),
            ForeColor = Color.Black,
            Margin = new Padding(0, 0, 0, 5),
            Cursor = Cursors.Hand
        };
        var descriptionLabel = new Label
        {
            Text = description,
            AutoSize = true,
            ForeColor = BodyText,
            Margin = Padding.Empty,
            Cursor = Cursors.Hand
        };

        text.Controls.Add(titleLabel, 0, 0);
        text.Controls.Add(descriptionLabel, 0, 1);

        body.Controls.Add(icon, 0, 0);
        body.Controls.Add(text, 1, 0);

        var footer = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.White,
            Margin = new Padding(0),
            Padding = new Padding(0),
            Cursor = Cursors.Default
        };

        footer.Paint += (_, e) =>
        {
            using var pen = new Pen(CardBorder);
            e.Graphics.DrawLine(pen, 0, 0, footer.Width, 0);
        };

        var openButton = PrimaryActionButton("Open", 112);
        openButton.Image = CreateExternalLinkIcon(Color.White);
        ConfigureButtonImage(openButton);
        openButton.Size = new Size(112, 34);
        openButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        openButton.Location = new Point(Math.Max(0, footer.Width - openButton.Width), 7);
        footer.Resize += (_, _) =>
            openButton.Location = new Point(
                Math.Max(0, footer.Width - openButton.Width),
                7);

        footer.Controls.Add(openButton);
        body.Controls.Add(footer, 0, 1);
        body.SetColumnSpan(footer, 2);

        EventHandler click = (_, _) => open();
        card.Click += click;
        icon.Click += click;
        text.Click += click;
        titleLabel.Click += click;
        descriptionLabel.Click += click;
        openButton.Click += click;

        card.Controls.Add(body);
        return card;
    }

    private static Control SectionHeader(string iconName, string text)
    {
        var row = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 36,
            AutoSize = false,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Margin = new Padding(0),
            Padding = Padding.Empty,
            BackColor = PageBackground
        };

        row.Controls.Add(new PictureBox
        {
            Image = IconAssets.Get(iconName),
            SizeMode = PictureBoxSizeMode.Zoom,
            Size = new Size(28, 28),
            Margin = new Padding(0, 1, 8, 0),
            BackColor = Color.Transparent
        });

        row.Controls.Add(new Label
        {
            Text = text,
            AutoSize = true,
            Font = new Font("Segoe UI Semibold", 14F, FontStyle.Bold),
            ForeColor = Color.FromArgb(24, 28, 36),
            Margin = new Padding(0, 2, 0, 0)
        });

        return row;
    }

    private static Control Separator() => new Panel
    {
        Dock = DockStyle.Top,
        Height = 1,
        BackColor = Color.FromArgb(225, 229, 235),
        Margin = new Padding(0, 4, 0, 18)
    };

    private static Control InformationBar()
    {
        var panel = new RoundedPanel
        {
            Dock = DockStyle.Top,
            Height = 52,
            BackColor = Color.FromArgb(243, 246, 251),
            BorderColor = Color.FromArgb(232, 236, 242),
            CornerRadius = 8,
            Padding = new Padding(14, 8, 14, 8),
            Margin = new Padding(0, 0, 12, 0)
        };

        var row = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Margin = Padding.Empty,
            Padding = Padding.Empty,
            BackColor = Color.Transparent
        };

        row.Controls.Add(new PictureBox
        {
            Image = IconAssets.Get("reports_info"),
            SizeMode = PictureBoxSizeMode.Zoom,
            Size = new Size(24, 24),
            Margin = new Padding(0, 4, 8, 0),
            BackColor = Color.Transparent
        });

        row.Controls.Add(new Label
        {
            Text = "All reports can be exported to PDF and CSV. Dates up to today only.",
            AutoSize = true,
            ForeColor = MutedText,
            Margin = new Padding(0, 6, 0, 0)
        });

        panel.Controls.Add(row);
        return panel;
    }

    private async Task GenerateMarketFloorAsync(bool openAfter)
    {
        status.Text = string.Empty;
        var date = DateOnly.FromDateTime(reportDate.Value.Date);

        using var dialog = new SaveFileDialog
        {
            Title = "Save Market Floor Sheet",
            Filter = "PDF document (*.pdf)|*.pdf",
            FileName = $"BinTracker_Market_Floor_{date:yyyyMMdd}.pdf",
            AddExtension = true,
            DefaultExt = "pdf"
        };

        if (dialog.ShowDialog(FindForm()) != DialogResult.OK)
            return;

        try
        {
            Enabled = false;
            await marketFloor.GeneratePdfAsync(date, dialog.FileName);

            status.Text =
                $"Created 2-page Market Floor Sheet for {date:dd/MM/yyyy}: {dialog.FileName}";

            if (openAfter)
                OpenFile(dialog.FileName);
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

    private async Task GeneratePrintPackAsync(bool openAfter)
    {
        printPackStatus.Text = string.Empty;
        var date = DateOnly.FromDateTime(printPackDate.Value.Date);

        using var dialog = new SaveFileDialog
        {
            Title = "Save Daily Print Pack",
            Filter = "PDF document (*.pdf)|*.pdf",
            FileName = $"BinTracker_Daily_Print_Pack_{date:yyyyMMdd}.pdf",
            AddExtension = true,
            DefaultExt = "pdf"
        };

        if (dialog.ShowDialog(FindForm()) != DialogResult.OK)
            return;

        try
        {
            Enabled = false;
            await dailyPrintPack.GeneratePdfAsync(date, dialog.FileName);

            printPackStatus.Text =
                $"Created Daily Print Pack for {date:dd/MM/yyyy}: {dialog.FileName}";

            if (openAfter)
                OpenFile(dialog.FileName);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                ex.Message,
                "Daily Print Pack",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
        finally
        {
            Enabled = true;
        }
    }

    private static void OpenFile(string path) =>
        Process.Start(new ProcessStartInfo
        {
            FileName = path,
            UseShellExecute = true
        });

    private static DateTimePicker ReportDatePicker() => new()
    {
        Format = DateTimePickerFormat.Short,
        Width = 178,
        Height = 38,
        Value = DateTime.Today,
        Font = new Font("Segoe UI", 10F)
    };

    private static Label StatusLabel() => new()
    {
        AutoSize = true,
        ForeColor = Color.DimGray,
        MaximumSize = new Size(900, 0)
    };


    private static void ConfigureButtonImage(Button button)
    {
        button.TextImageRelation = TextImageRelation.ImageBeforeText;
        button.ImageAlign = ContentAlignment.MiddleCenter;
        button.TextAlign = ContentAlignment.MiddleCenter;
        button.Padding = new Padding(8, 0, 8, 0);
    }

    private static Image CreateDocumentIcon(Color color)
    {
        const int size = 18;
        var bitmap = new Bitmap(size, size);
        bitmap.SetResolution(96, 96);

        using var graphics = Graphics.FromImage(bitmap);
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        graphics.Clear(Color.Transparent);

        using var pen = new Pen(color, 1.7F)
        {
            StartCap = LineCap.Round,
            EndCap = LineCap.Round,
            LineJoin = LineJoin.Round
        };

        // Page with a folded top-right corner.  This is vector-drawn so it
        // renders consistently without relying on symbol/emoji fonts.
        graphics.DrawLines(pen, new[]
        {
            new PointF(4.5F, 2.5F),
            new PointF(10.5F, 2.5F),
            new PointF(14.0F, 6.0F),
            new PointF(14.0F, 15.0F),
            new PointF(4.5F, 15.0F),
            new PointF(4.5F, 2.5F)
        });
        graphics.DrawLines(pen, new[]
        {
            new PointF(10.5F, 2.8F),
            new PointF(10.5F, 6.0F),
            new PointF(13.7F, 6.0F)
        });
        graphics.DrawLine(pen, 7.0F, 9.0F, 11.5F, 9.0F);
        graphics.DrawLine(pen, 7.0F, 12.0F, 11.5F, 12.0F);

        return bitmap;
    }

    private static Image CreateExternalLinkIcon(Color color)
    {
        const int size = 18;
        var bitmap = new Bitmap(size, size);
        bitmap.SetResolution(96, 96);

        using var graphics = Graphics.FromImage(bitmap);
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        graphics.Clear(Color.Transparent);

        using var pen = new Pen(color, 1.7F)
        {
            StartCap = LineCap.Round,
            EndCap = LineCap.Round,
            LineJoin = LineJoin.Round
        };

        graphics.DrawLines(pen, new[]
        {
            new PointF(8.0F, 5.0F),
            new PointF(4.0F, 5.0F),
            new PointF(4.0F, 14.0F),
            new PointF(13.0F, 14.0F),
            new PointF(13.0F, 10.0F)
        });
        graphics.DrawLine(pen, 8.0F, 10.0F, 14.0F, 4.0F);
        graphics.DrawLine(pen, 10.5F, 4.0F, 14.0F, 4.0F);
        graphics.DrawLine(pen, 14.0F, 4.0F, 14.0F, 7.5F);

        return bitmap;
    }

    private static Button SecondaryActionButton(string text, int width) => new()
    {
        Text = text,
        AutoSize = false,
        Size = new Size(width, 42),
        FlatStyle = FlatStyle.Flat,
        BackColor = Color.White,
        ForeColor = Color.FromArgb(24, 28, 36),
        Font = new Font("Segoe UI Semibold", 10F, FontStyle.Regular),
        Cursor = Cursors.Hand,
        Margin = Padding.Empty
    };

    private static Button PrimaryActionButton(string text, int width)
    {
        var button = new Button
        {
            Text = text,
            AutoSize = false,
            Size = new Size(width, 42),
            FlatStyle = FlatStyle.Flat,
            BackColor = Primary,
            ForeColor = Color.White,
            Font = new Font("Segoe UI Semibold", 10F, FontStyle.Regular),
            Cursor = Cursors.Hand,
            Margin = Padding.Empty
        };
        button.FlatAppearance.BorderColor = Primary;
        return button;
    }

    private sealed class RoundedPanel : Panel
    {
        public int CornerRadius { get; init; } = 8;
        public Color BorderColor { get; init; } = Color.Gainsboro;

        public RoundedPanel()
        {
            DoubleBuffered = true;
            Resize += (_, _) => UpdateRegion();
        }

        protected override void OnCreateControl()
        {
            base.OnCreateControl();
            UpdateRegion();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            using var path = RoundedRect(
                new Rectangle(
                    0,
                    0,
                    Math.Max(1, Width - 1),
                    Math.Max(1, Height - 1)),
                CornerRadius);

            using var pen = new Pen(BorderColor);
            e.Graphics.DrawPath(pen, path);

            base.OnPaint(e);
        }

        private void UpdateRegion()
        {
            if (Width <= 0 || Height <= 0)
                return;

            using var path = RoundedRect(
                new Rectangle(0, 0, Width, Height),
                CornerRadius);

            Region?.Dispose();
            Region = new Region(path);
            Invalidate();
        }

        private static GraphicsPath RoundedRect(
            Rectangle bounds,
            int radius)
        {
            var diameter = Math.Max(2, radius * 2);
            var path = new GraphicsPath();

            path.AddArc(bounds.Left, bounds.Top, diameter, diameter, 180, 90);
            path.AddArc(bounds.Right - diameter, bounds.Top, diameter, diameter, 270, 90);
            path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
            path.AddArc(bounds.Left, bounds.Bottom - diameter, diameter, diameter, 90, 90);
            path.CloseFigure();

            return path;
        }
    }
}
