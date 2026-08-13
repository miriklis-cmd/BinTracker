using BinTracker.Services;

namespace BinTracker.WinForms;

public sealed class BusinessInformationForm : Form
{
    private readonly IBusinessInformationService service;

    private readonly TextBox businessName = Field();
    private readonly TextBox tradingName = Field();
    private readonly TextBox abn = Field();
    private readonly TextBox address = Field(multiline: true);
    private readonly TextBox phone = Field();
    private readonly TextBox email = Field();
    private readonly TextBox reportHeader = Field();

    private readonly Label validation = new()
    {
        AutoSize = true,
        ForeColor = Color.Firebrick,
        MaximumSize = new Size(660, 0)
    };

    public BusinessInformationForm(IBusinessInformationService service)
    {
        this.service = service;

        Text = "Business Information";
        StartPosition = FormStartPosition.CenterParent;
        AutoScaleMode = AutoScaleMode.Dpi;
        ClientSize = new Size(760, 690);
        MinimumSize = new Size(680, 620);
        BackColor = Color.FromArgb(245, 247, 250);
        Font = new Font("Segoe UI", 10F);

        Build();
        Load += async (_, _) => await LoadAsync();
    }

    private void Build()
    {
        // This remains a separate modal dialog. AutoScroll plus an auto-sized
        // card prevents the bottom action buttons from being clipped at 125% /
        // 150% Windows display scaling.
        var outer = new Panel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            Padding = new Padding(22),
            BackColor = Color.FromArgb(245, 247, 250)
        };

        var card = new Panel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            MinimumSize = new Size(680, 0),
            BackColor = Color.White,
            Padding = new Padding(24),
            Margin = Padding.Empty
        };

        var form = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 2,
            RowCount = 0,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };

        form.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 180F));
        form.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

        Add(form, "Business name", businessName);
        Add(form, "Trading name", tradingName);
        Add(form, "ABN", abn);
        Add(form, "Address", address);
        Add(form, "Phone", phone);
        Add(form, "Email", email);
        Add(form, "Default report header", reportHeader);

        var hintRow = form.RowCount++;
        form.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        form.Controls.Add(new Label
        {
            Text =
                "Trading name is used on reports when supplied. " +
                "Default report header is optional and replaces that heading if you want different wording.",
            AutoSize = true,
            ForeColor = Color.DimGray,
            MaximumSize = new Size(500, 0),
            Margin = new Padding(0, 4, 0, 12)
        }, 1, hintRow);

        var validationRow = form.RowCount++;
        form.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        form.Controls.Add(validation, 1, validationRow);

        // Use a fixed-row table rather than FlowLayoutPanel so both action
        // buttons share the exact same top/bottom coordinates at every DPI.
        var actions = new TableLayoutPanel
        {
            Dock = DockStyle.None,
            Anchor = AnchorStyles.Left,
            AutoSize = false,
            Size = new Size(250, 44),
            ColumnCount = 2,
            RowCount = 1,
            Margin = new Padding(0, 18, 0, 18),
            Padding = Padding.Empty
        };
        actions.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130F));
        actions.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120F));
        actions.RowStyles.Add(new RowStyle(SizeType.Absolute, 44F));

        var save = new Button
        {
            Text = "Save",
            AutoSize = false,
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 0, 10, 0),
            TextAlign = ContentAlignment.MiddleCenter
        };
        save.Click += async (_, _) => await SaveAsync();

        var close = new Button
        {
            Text = "Close",
            AutoSize = false,
            Dock = DockStyle.Fill,
            Margin = Padding.Empty,
            TextAlign = ContentAlignment.MiddleCenter
        };
        close.Click += (_, _) => Close();

        actions.Controls.Add(save, 0, 0);
        actions.Controls.Add(close, 1, 0);

        var actionRow = form.RowCount++;
        form.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        form.Controls.Add(actions, 1, actionRow);

        card.Controls.Add(form);
        outer.Controls.Add(card);
        Controls.Add(outer);
    }

    private async Task LoadAsync()
    {
        try
        {
            var value = await service.GetAsync();

            businessName.Text = value.BusinessName;
            tradingName.Text = value.TradingName;
            abn.Text = value.Abn;
            address.Text = value.Address;
            phone.Text = value.Phone;
            email.Text = value.Email;
            reportHeader.Text = value.DefaultReportHeader;
        }
        catch (Exception ex)
        {
            validation.Text = ex.Message;
        }
    }

    private async Task SaveAsync()
    {
        validation.Text = string.Empty;

        try
        {
            await service.SaveAsync(new BusinessInformation(
                businessName.Text,
                tradingName.Text,
                abn.Text,
                address.Text,
                phone.Text,
                email.Text,
                reportHeader.Text));

            validation.ForeColor = Color.ForestGreen;
            validation.Text = "Business Information saved.";
        }
        catch (Exception ex)
        {
            validation.ForeColor = Color.Firebrick;
            validation.Text = ex.Message;
        }
    }

    private static void Add(
        TableLayoutPanel form,
        string label,
        Control control)
    {
        var row = form.RowCount++;
        form.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        form.Controls.Add(new Label
        {
            Text = label,
            AutoSize = true,
            ForeColor = Color.FromArgb(70, 80, 95),
            Margin = new Padding(0, 8, 18, 8)
        }, 0, row);

        control.Margin = new Padding(0, 4, 0, 8);
        form.Controls.Add(control, 1, row);
    }

    private static TextBox Field(bool multiline = false) => new()
    {
        Dock = DockStyle.Fill,
        Multiline = multiline,
        Height = multiline ? 90 : 30
    };
}
