using BinTracker.Core;
using BinTracker.Services;

namespace BinTracker.WinForms;

public sealed class CustomersView : UserControl
{
    private readonly ICustomerService service;
    private readonly UserSession session;
    private readonly ICustomerStatementReportService statementReports;
    private readonly TextBox search = new();
    private readonly CheckBox includeInactive = new();
    private readonly DataGridView customerGrid = Grid();
    private readonly TextBox code = Field();
    private readonly TextBox name = Field();
    private readonly ComboBox customerType = new()
    {
        DropDownStyle = ComboBoxStyle.DropDownList,
        Dock = DockStyle.Fill,
        Margin = new Padding(0, 4, 14, 4)
    };
    private readonly TextBox contact = Field();
    private readonly TextBox phone = Field();
    private readonly TextBox mobile = Field();
    private readonly TextBox email = Field();
    private readonly TextBox address = Field(multiline:true);
    private readonly TextBox notes = Field(multiline:true);
    private readonly CheckBox emailReminders = new() { Text="Email reminders", AutoSize=true };
    private readonly CheckBox smsReminders = new() { Text="SMS reminders", AutoSize=true };
    private readonly CheckBox optOut = new() { Text="Do not send automatic reminders", AutoSize=true };
    private readonly Label status = new() { AutoSize=true, ForeColor=Color.DimGray };
    private readonly Button save = new() { Text="Save Customer", AutoSize=true, MinimumSize=new Size(145, 42) };
    private readonly Button deactivate = new() { Text="Deactivate", AutoSize=true, MinimumSize=new Size(120, 42) };
    private readonly Button addNew = new() { Text="+ New Customer", AutoSize=true, MinimumSize=new Size(145, 42) };
    private readonly Button statement = new() { Text="Customer Statement", AutoSize=true, MinimumSize=new Size(155, 42) };
    private readonly DataGridView balances = Grid();
    private readonly DataGridView movements = Grid();
    private int selectedId;

    public CustomersView(ICustomerService service, UserSession session, ICustomerStatementReportService statementReports)
    {
        this.service=service; this.session=session; this.statementReports=statementReports;
        Dock=DockStyle.Fill; AutoScaleMode=AutoScaleMode.Dpi; BackColor=Color.FromArgb(245,247,250);
        customerType.Items.Add(new CustomerTypeOption(CustomerType.Account, "Account"));
        customerType.Items.Add(new CustomerTypeOption(CustomerType.CashCod, "Cash / COD"));
        customerType.SelectedIndex = 0;
        Build();
        code.Leave += (_, _) => code.Text = code.Text.Trim().ToUpperInvariant();
        _ = ReloadAsync();
    }

    private void Build()
    {
        var split = new TableLayoutPanel { Dock=DockStyle.Fill, ColumnCount=2, RowCount=1, Padding=new Padding(0), Margin=new Padding(0) };
        split.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 34));
        split.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 66));

        var left = new TableLayoutPanel { Dock=DockStyle.Fill, RowCount=3, ColumnCount=1, Padding=new Padding(0,0,14,0) };
        left.RowStyles.Add(new RowStyle(SizeType.AutoSize)); left.RowStyles.Add(new RowStyle(SizeType.AutoSize)); left.RowStyles.Add(new RowStyle(SizeType.Percent,100));
        var tools = new TableLayoutPanel { Dock=DockStyle.Top, AutoSize=true, ColumnCount=2, Padding=new Padding(0,0,0,8) };
        tools.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100)); tools.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        search.PlaceholderText="Search code, customer, contact, phone or email..."; search.Dock=DockStyle.Fill; search.Margin=new Padding(0,0,8,0);
        search.TextChanged += async (_,_) => await ReloadAsync();
        includeInactive.Text="Inactive"; includeInactive.AutoSize=true; includeInactive.CheckedChanged += async (_,_) => await ReloadAsync();
        tools.Controls.Add(search,0,0); tools.Controls.Add(includeInactive,1,0);
        customerGrid.Columns.Add(new DataGridViewTextBoxColumn { Name="Id", Visible=false });
        customerGrid.Columns.Add(new DataGridViewTextBoxColumn { Name="Code", HeaderText="Code", Width=110 });
        customerGrid.Columns.Add(new DataGridViewTextBoxColumn { Name="Customer", HeaderText="Customer", AutoSizeMode=DataGridViewAutoSizeColumnMode.Fill });
        customerGrid.Columns.Add(new DataGridViewTextBoxColumn { Name="Type", HeaderText="Type", Width=105 });
        customerGrid.Columns.Add(new DataGridViewTextBoxColumn { Name="Position", HeaderText="Net Position", Width=120 });
        customerGrid.Columns.Add(new DataGridViewTextBoxColumn { Name="Status", HeaderText="Status", Width=85 });
        addNew.Click += (_,_) => NewCustomer();
        customerGrid.SelectionChanged += async (_,_) => await SelectionChangedAsync();
        left.Controls.Add(tools,0,0); left.Controls.Add(addNew,0,1); left.Controls.Add(customerGrid,0,2);

        var right = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            AutoScroll = false,
            Margin = new Padding(0),
            Padding = new Padding(0)
        };

        // Customer details take only the height they need.
        // Current position is deliberately compact.
        // Movement history receives every remaining pixel so it cannot be
        // pushed below the visible client area on smaller displays.
        // The details area should consume only the height required by its controls.
        // The remaining height is then shared between the two operational grids.
        // The customer editor needs enough fixed height for the action row
        // (Save Customer / Deactivate / Customer Statement). At 360 px that
        // final row was clipped on the laptop DPI setting.
        right.RowStyles.Add(new RowStyle(SizeType.Absolute, 405F));

        // Keep Current Position useful, but give most of the remaining space
        // to Recent Movement History.
        right.RowStyles.Add(new RowStyle(SizeType.Percent, 36F));
        right.RowStyles.Add(new RowStyle(SizeType.Percent, 64F));
        right.Controls.Add(BuildDetails(),0,0);
        right.Controls.Add(BuildBalances(),0,1);
        right.Controls.Add(BuildMovements(),0,2);

        split.Controls.Add(left,0,0); split.Controls.Add(right,1,0); Controls.Add(split);
        SetEditEnabled(session.Role != UserRole.Viewer);
        statement.Enabled = false;
    }


    private Control BuildDetails()
    {
        // The details table is the section itself. There is deliberately no
        // fixed-height wrapper panel here: the table grows only as tall as its
        // controls require, so the operational grids start immediately below
        // the customer action row.
        var form = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = false,
            ColumnCount = 4,
            RowCount = 8,
            BackColor = Color.White,
            Padding = new Padding(18, 12, 18, 4),
            Margin = Padding.Empty
        };

        form.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        form.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
        form.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        form.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));

        for (var row = 0; row < 8; row++)
            form.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        AddRow(form, 0, "Customer code", code, "Customer name", name);

        form.Controls.Add(LabelFor("Customer type"), 0, 1);
        form.Controls.Add(customerType, 1, 1);

        AddRow(form, 2, "Contact", contact, "Phone", phone);
        AddRow(form, 3, "Mobile", mobile, "Email", email);

        form.Controls.Add(LabelFor("Address"), 0, 4);
        form.Controls.Add(address, 1, 4);
        form.SetColumnSpan(address, 3);

        form.Controls.Add(LabelFor("Notes"), 0, 5);
        form.Controls.Add(notes, 1, 5);
        form.SetColumnSpan(notes, 3);

        var prefs = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            WrapContents = true,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };

        prefs.Controls.Add(emailReminders);
        prefs.Controls.Add(smsReminders);
        prefs.Controls.Add(optOut);

        form.Controls.Add(LabelFor("Reminders"), 0, 6);
        form.Controls.Add(prefs, 1, 6);
        form.SetColumnSpan(prefs, 3);

        var actions = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true,
            Margin = new Padding(0, 2, 0, 0),
            Padding = Padding.Empty
        };

        save.Click += async (_, _) => await SaveAsync();
        deactivate.Click += async (_, _) => await ToggleActiveAsync();
        statement.Click += async (_, _) => await GenerateStatementAsync();

        actions.Controls.Add(save);
        actions.Controls.Add(deactivate);
        actions.Controls.Add(statement);
        actions.Controls.Add(status);

        form.Controls.Add(actions, 0, 7);
        form.SetColumnSpan(actions, 4);

        return form;
    }

    private Control BuildBalances()
    {
        var box = Section("Current position by type", balances);
        box.Dock = DockStyle.Fill;
        balances.Columns.Add("Type","Container Type"); balances.Columns.Add("Balance","Balance"); balances.Columns.Add("Position","Position");
        balances.Columns[0].AutoSizeMode=DataGridViewAutoSizeColumnMode.Fill; balances.Columns[1].Width=100; balances.Columns[2].Width=140;
        return box;
    }

    private Control BuildMovements()
    {
        var box = Section("Recent movement history", movements);
        box.Dock = DockStyle.Fill;

        movements.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "Date",
            HeaderText = "Date",
            Width = 125
        });

        movements.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "Direction",
            HeaderText = "Direction",
            Width = 145
        });

        movements.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "Type",
            HeaderText = "Container Type",
            AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
            MinimumWidth = 115
        });

        movements.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "Quantity",
            HeaderText = "Qty",
            Width = 58
        });

        movements.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "Reference",
            HeaderText = "Reference",
            Width = 105
        });

        movements.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "User",
            HeaderText = "Entered By",
            Width = 125
        });

        return box;
    }

    private async Task ReloadAsync(int? selectId=null)
    {
        var rows=await service.SearchAsync(search.Text, includeInactive.Checked);
        customerGrid.SuspendLayout(); customerGrid.Rows.Clear();
        foreach(var r in rows) customerGrid.Rows.Add(r.Id, r.CustomerCode, r.Name, CustomerTypeText(r.CustomerType), r.NetBalance == 0 ? "Even" : r.NetBalance > 0 ? $"{r.NetBalance} OUT" : $"{Math.Abs(r.NetBalance)} CREDIT", r.IsActive ? "Active" : "Inactive");
        customerGrid.ResumeLayout();
        if(selectId.HasValue) SelectRow(selectId.Value);
    }

    private async Task SelectionChangedAsync()
    {
        if(customerGrid.SelectedRows.Count==0 || customerGrid.SelectedRows[0].Cells[0].Value is null) return;
        selectedId=Convert.ToInt32(customerGrid.SelectedRows[0].Cells[0].Value);
        var c=await service.GetAsync(selectedId); if(c is null) return;
        code.Text=c.CustomerCode ?? ""; name.Text=c.Name; SelectCustomerType(c.CustomerType); contact.Text=c.ContactName ?? ""; phone.Text=c.Phone ?? ""; mobile.Text=c.MobileNumber ?? ""; email.Text=c.Email ?? ""; address.Text=c.Address ?? ""; notes.Text=c.Notes ?? "";
        emailReminders.Checked=c.AllowEmailReminders; smsReminders.Checked=c.AllowSmsReminders; optOut.Checked=c.ReminderOptOut;
        deactivate.Text=c.IsActive ? "Deactivate" : "Reactivate"; statement.Enabled=true; status.Text=c.IsActive ? "Active customer" : "Inactive customer";
        await LoadRelatedAsync();
    }

    private async Task LoadRelatedAsync()
    {
        balances.Rows.Clear(); foreach(var b in await service.GetBalancesAsync(selectedId)) balances.Rows.Add(b.ContainerType,b.Balance,b.Position);
        movements.Rows.Clear(); foreach(var m in await service.GetRecentMovementsAsync(selectedId)) movements.Rows.Add(m.Date.ToString("dd/MM/yyyy"),m.Direction,m.ContainerType,m.Quantity,m.Reference ?? "",m.CreatedBy ?? "");
    }

    private void NewCustomer()
    {
        selectedId=0; code.Clear(); name.Clear(); customerType.SelectedIndex=0; contact.Clear(); phone.Clear(); mobile.Clear(); email.Clear(); address.Clear(); notes.Clear(); emailReminders.Checked=true; smsReminders.Checked=true; optOut.Checked=false; deactivate.Enabled=false; status.Text="New customer"; balances.Rows.Clear(); movements.Rows.Clear(); statement.Enabled=false; code.Focus();
    }

    private async Task SaveAsync()
    {
        try
        {
            var id=await service.SaveAsync(new CustomerEditModel { Id=selectedId, CustomerCode=code.Text, Name=name.Text, CustomerType=SelectedCustomerType(), ContactName=contact.Text, Phone=phone.Text, MobileNumber=mobile.Text, Email=email.Text, Address=address.Text, Notes=notes.Text, AllowEmailReminders=emailReminders.Checked, AllowSmsReminders=smsReminders.Checked, ReminderOptOut=optOut.Checked });
            selectedId=id; deactivate.Enabled=true; statement.Enabled=true; status.Text="Saved"; await ReloadAsync(id); await LoadRelatedAsync();
        }
        catch(Exception ex){ MessageBox.Show(ex.Message,"Customer",MessageBoxButtons.OK,MessageBoxIcon.Warning); }
    }

    private async Task GenerateStatementAsync()
    {
        if (selectedId == 0) return;
        var customer = await service.GetAsync(selectedId);
        if (customer is null) return;

        using var options = new StatementOptionsForm();
        if (options.ShowDialog(FindForm()) != DialogResult.OK) return;

        var safeCode = string.Join("_", customer.CustomerCode.Split(Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries));
        using var dialog = new SaveFileDialog
        {
            Title = "Save Customer Statement",
            Filter = "PDF document (*.pdf)|*.pdf",
            FileName = $"BinTracker_Statement_{safeCode}_{options.FromDate:yyyyMMdd}-{options.ToDate:yyyyMMdd}.pdf",
            AddExtension = true,
            DefaultExt = "pdf"
        };
        if (dialog.ShowDialog(FindForm()) != DialogResult.OK) return;

        try
        {
            await statementReports.GeneratePdfAsync(selectedId, options.FromDate, options.ToDate, dialog.FileName);
            MessageBox.Show($"Statement created successfully.\n\n{dialog.FileName}", "Customer Statement", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Customer Statement", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async Task ToggleActiveAsync()
    {
        if(selectedId==0) return;
        var c=await service.GetAsync(selectedId); if(c is null) return;
        var action=c.IsActive ? "deactivate" : "reactivate";
        if(MessageBox.Show($"{char.ToUpper(action[0]) + action[1..]} {c.Name}?", "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Question)!=DialogResult.Yes) return;
        await service.SetActiveAsync(selectedId,!c.IsActive); await ReloadAsync(selectedId); await SelectionChangedAsync();
    }

    private void SetEditEnabled(bool enabled){ foreach(var t in new[]{code,name,contact,phone,mobile,email,address,notes}) t.ReadOnly=!enabled; customerType.Enabled=enabled; emailReminders.Enabled=enabled; smsReminders.Enabled=enabled; optOut.Enabled=enabled; save.Enabled=enabled; deactivate.Enabled=enabled; addNew.Enabled=enabled; }
    private CustomerType SelectedCustomerType() =>
        customerType.SelectedItem is CustomerTypeOption option ? option.Value : CustomerType.Account;

    private void SelectCustomerType(CustomerType type)
    {
        for (var i = 0; i < customerType.Items.Count; i++)
        {
            if (customerType.Items[i] is CustomerTypeOption option && option.Value == type)
            {
                customerType.SelectedIndex = i;
                return;
            }
        }

        customerType.SelectedIndex = 0;
    }

    private static string CustomerTypeText(CustomerType type) =>
        type == CustomerType.CashCod ? "Cash / COD" : "Account";

    private void SelectRow(int id){ foreach(DataGridViewRow row in customerGrid.Rows) if(Convert.ToInt32(row.Cells[0].Value)==id){ row.Selected=true; customerGrid.CurrentCell=row.Cells[1]; break; } }
    private static void AddRow(TableLayoutPanel f,int row,string l1,Control c1,string l2,Control c2){ f.Controls.Add(LabelFor(l1),0,row); f.Controls.Add(c1,1,row); f.Controls.Add(LabelFor(l2),2,row); f.Controls.Add(c2,3,row); }
    private static Label LabelFor(string text)=>new(){Text=text,AutoSize=true,Anchor=AnchorStyles.Left,Margin=new Padding(0,7,10,7),ForeColor=Color.FromArgb(70,80,95)};
    private static TextBox Field(bool multiline=false)=>new(){Dock=DockStyle.Fill,Multiline=multiline,Height=multiline?52:30,Margin=new Padding(0,4,14,4)};
    private static DataGridView Grid()=>new(){Dock=DockStyle.Fill,AllowUserToAddRows=false,AllowUserToDeleteRows=false,ReadOnly=true,MultiSelect=false,SelectionMode=DataGridViewSelectionMode.FullRowSelect,AutoGenerateColumns=false,RowHeadersVisible=false,BackgroundColor=Color.White,BorderStyle=BorderStyle.FixedSingle,AutoSizeRowsMode=DataGridViewAutoSizeRowsMode.AllCells,ShowCellToolTips=false};
    private static Panel Section(string heading, Control child)
    {
        var panel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.White,
            Padding = new Padding(18, 12, 18, 4),
            Margin = Padding.Empty,
            MinimumSize = Size.Empty
        };

        var label = new Label
        {
            Text = heading,
            Dock = DockStyle.Top,
            Height = 34,
            Font = new Font("Segoe UI Semibold", 12F, FontStyle.Bold)
        };

        child.Dock = DockStyle.Fill;

        panel.Controls.Add(child);
        panel.Controls.Add(label);

        return panel;
    }
    private sealed record CustomerTypeOption(CustomerType Value, string Text)
    {
        public override string ToString() => Text;
    }
}
