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
        ClientSize = new Size(760, 620);
        MinimumSize = new Size(650, 540);
        BackColor = Color.FromArgb(245, 247, 250);
        Font = new Font("Segoe UI", 10F);

        Build();
        Load += async (_, _) => await LoadAsync();
    }

    private void Build()
    {
        var outer = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(22),
            BackColor = Color.FromArgb(245, 247, 250)
        };

        var card = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.White,
            Padding = new Padding(24)
        };

        var form = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            ColumnCount = 2,
            RowCount = 0
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
                "Trading name is used as the display name when supplied. " +
                "Default report header can override that wording on printed reports.",
            AutoSize = true,
            ForeColor = Color.DimGray,
            MaximumSize = new Size(500, 0),
            Margin = new Padding(0, 4, 0, 12)
        }, 1, hintRow);

        var validationRow = form.RowCount++;
        form.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        form.Controls.Add(validation, 1, validationRow);

        var actions = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            Margin = new Padding(0, 18, 0, 0)
        };

        var save = new Button
        {
            Text = "Save",
            AutoSize = false,
            Size = new Size(120, 42),
            Margin = new Padding(0, 0, 10, 0)
        };
        save.Click += async (_, _) => await SaveAsync();

        var close = new Button
        {
            Text = "Close",
            AutoSize = false,
            Size = new Size(120, 42)
        };
        close.Click += (_, _) => Close();

        actions.Controls.Add(save);
        actions.Controls.Add(close);

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
        Height = multiline ? 70 : 30
    };
}
