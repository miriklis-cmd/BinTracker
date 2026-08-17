using BinTracker.Services;

namespace BinTracker.WinForms;

public sealed class ContainerTypesForm : BinTrackerForm
{
    private readonly IContainerTypeService service;
    private readonly bool canEdit;
    private readonly TextBox search = new() { PlaceholderText = "Search name or short code...", Dock = DockStyle.Fill };
    private readonly CheckBox includeInactive = new() { Text = "Inactive", AutoSize = true };
    private readonly DataGridView grid = new()
    {
        Dock = DockStyle.Fill, ReadOnly = true, AutoGenerateColumns = false,
        AllowUserToAddRows = false, AllowUserToDeleteRows = false,
        MultiSelect = false, SelectionMode = DataGridViewSelectionMode.FullRowSelect,
        RowHeadersVisible = false, BackgroundColor = Color.White, ShowCellToolTips = false
    };

    private readonly TextBox name = Field();
    private readonly TextBox shortCode = Field();
    private readonly TextBox systemCode = Field();
    private readonly NumericUpDown displayOrder = new() { Dock = DockStyle.Left, Width = 120, Minimum = 0, Maximum = 9999 };
    private readonly CheckBox active = new() { Text = "Active", AutoSize = true };
    private readonly CheckBox special = new() { Text = "Special Floor Report Container", AutoSize = true };
    private readonly TextBox dashboardColour = Field();
    private readonly TextBox description = Field(multiline: true);
    private readonly TextBox notes = Field(multiline: true);
    private readonly Label usage = new() { AutoSize = true, ForeColor = Color.DimGray, MaximumSize = new Size(680, 0) };
    private readonly Label validation = new() { AutoSize = true, ForeColor = Color.Firebrick, MaximumSize = new Size(680, 0) };
    private readonly Button deactivate = ButtonOf("Deactivate", 120);
    private int selectedId;
    private bool suppressSelectionChanged;
    private bool bypassClosePrompt;
    private ContainerEditorSnapshot? savedSnapshot;

    public ContainerTypesForm(
        IContainerTypeService service,
        bool canEdit = true)
    {
        this.service = service;
        this.canEdit = canEdit;
        Text = "Container Types";
        StartPosition = FormStartPosition.CenterParent;
        AutoScaleMode = AutoScaleMode.Dpi;
        ClientSize = new Size(1120, 720);
        MinimumSize = new Size(920, 620);
        BackColor = Color.FromArgb(245,247,250);
        Font = new Font("Segoe UI",10F);
        Build();
        Shown += async (_,_) => await ReloadAsync();
        FormClosing += ContainerTypesForm_FormClosing;
    }

    public bool HasUnsavedChanges =>
        canEdit && HasUnsavedChangesInternal();

    public Task<bool> ConfirmCanLeaveAsync() =>
        canEdit
            ? ConfirmLeaveCurrentAsync()
            : Task.FromResult(true);

    public void PrepareForHostClose() =>
        bypassClosePrompt = true;

    private void Build()
    {
        grid.Columns.Add(new DataGridViewTextBoxColumn { Name="Id", Visible=false });
        grid.Columns.Add(new DataGridViewTextBoxColumn { Name="Order", HeaderText="Order", Width=65 });
        grid.Columns.Add(new DataGridViewTextBoxColumn { Name="Name", HeaderText="Container", AutoSizeMode=DataGridViewAutoSizeColumnMode.Fill, MinimumWidth=150 });
        grid.Columns.Add(new DataGridViewTextBoxColumn { Name="Code", HeaderText="Short Code", Width=105 });
        grid.Columns.Add(new DataGridViewTextBoxColumn { Name="Usage", HeaderText="Movements", Width=95 });
        grid.Columns.Add(new DataGridViewTextBoxColumn { Name="Status", HeaderText="Status", Width=80 });
        grid.SelectionChanged += async (_,_) =>
        {
            if (!suppressSelectionChanged)
                await GridSelectionChangedAsync();
        };

        var root = new TableLayoutPanel { Dock=DockStyle.Fill, ColumnCount=2, RowCount=1, Padding=new Padding(18), Margin=Padding.Empty };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,42));
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,58));
        root.Controls.Add(BuildList(),0,0);
        root.Controls.Add(BuildEditor(),1,0);
        Controls.Add(root);
    }

    private Control BuildList()
    {
        var panel=Card(); panel.Margin=new Padding(0,0,12,0);
        var layout=new TableLayoutPanel { Dock=DockStyle.Fill, ColumnCount=1, RowCount=4, Padding=new Padding(18) };
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent,100));
        var heading = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            Margin = new Padding(0, 0, 0, 12)
        };
        heading.Controls.Add(new Label
        {
            Text = "Container Types",
            AutoSize = true,
            Font = new Font("Segoe UI Semibold", 16F, FontStyle.Bold)
        });
        if (!canEdit)
        {
            heading.Controls.Add(new Label
            {
                Text = "View only — administrator access is required to add or change container types.",
                AutoSize = true,
                ForeColor = Color.DimGray,
                MaximumSize = new Size(430, 0),
                Margin = new Padding(0, 4, 0, 0)
            });
        }
        layout.Controls.Add(heading,0,0);
        var tools=new TableLayoutPanel { Dock=DockStyle.Top, AutoSize=true, ColumnCount=2 };
        tools.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100)); tools.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        tools.Controls.Add(search,0,0); tools.Controls.Add(includeInactive,1,0);
        search.TextChanged += async (_,_) =>
        {
            if (!HasUnsavedChangesInternal())
                await ReloadAsync(selectedId == 0 ? null : selectedId);
        };
        includeInactive.CheckedChanged += async (_,_) =>
        {
            if (await ConfirmLeaveCurrentAsync())
                await ReloadAsync(selectedId == 0 ? null : selectedId);
            else
                includeInactive.Checked = !includeInactive.Checked;
        };
        layout.Controls.Add(tools,0,1);
        var add=ButtonOf("+ New Container",150);
        add.Margin=new Padding(0,10,0,10);
        add.Visible = canEdit;
        add.Enabled = canEdit;
        add.Click += async (_,_) =>
        {
            if (canEdit && await ConfirmLeaveCurrentAsync())
                NewContainer();
        };
        layout.Controls.Add(add,0,2);
        layout.Controls.Add(grid,0,3); panel.Controls.Add(layout); return panel;
    }

    private Control BuildEditor()
    {
        var panel=Card();
        var form=new TableLayoutPanel { Dock=DockStyle.Top, AutoSize=true, ColumnCount=2, Padding=new Padding(22) };
        form.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize)); form.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));
        Add(form,"Container name",name);
        Add(form,"Short code",shortCode);
        systemCode.ReadOnly=true; systemCode.BackColor=Color.FromArgb(245,247,250); Add(form,"System code",systemCode);
        Add(form,"Display order",displayOrder);
        Add(form,"Description",description);
        Add(form,"Notes",notes);
        dashboardColour.PlaceholderText="Reserved for dashboard/chart styling"; Add(form,"Dashboard Colour",dashboardColour);
        Add(form,"",active); Add(form,"",special);

        if (!canEdit)
        {
            name.ReadOnly = true;
            shortCode.ReadOnly = true;
            description.ReadOnly = true;
            notes.ReadOnly = true;
            dashboardColour.ReadOnly = true;
            displayOrder.Enabled = false;
            active.Enabled = false;
            special.Enabled = false;
        }

        var usageHeading=new Label { Text="Usage", AutoSize=true, Font=new Font("Segoe UI Semibold",12F,FontStyle.Bold), Margin=new Padding(0,14,0,4) };
        var row=form.RowCount++; form.RowStyles.Add(new RowStyle(SizeType.AutoSize)); form.Controls.Add(usageHeading,0,row); form.SetColumnSpan(usageHeading,2);
        row=form.RowCount++; form.RowStyles.Add(new RowStyle(SizeType.AutoSize)); usage.Margin=new Padding(0,4,0,12); form.Controls.Add(usage,0,row); form.SetColumnSpan(usage,2);
        row=form.RowCount++; form.RowStyles.Add(new RowStyle(SizeType.AutoSize)); form.Controls.Add(validation,0,row); form.SetColumnSpan(validation,2);

        var actions=new FlowLayoutPanel { Dock=DockStyle.Top, AutoSize=true, FlowDirection=FlowDirection.LeftToRight, Margin=new Padding(0,14,0,0) };
        var save=ButtonOf("Save",110);
        save.Visible = canEdit;
        save.Enabled = canEdit;
        deactivate.Visible = canEdit;
        deactivate.Enabled = canEdit;
        save.Click += async (_,_) => await SaveAsync();
        deactivate.Click += async (_,_) => await ToggleActiveAsync();
        actions.Controls.Add(save);
        actions.Controls.Add(deactivate);
        row=form.RowCount++; form.RowStyles.Add(new RowStyle(SizeType.AutoSize)); form.Controls.Add(actions,0,row); form.SetColumnSpan(actions,2);
        panel.Controls.Add(form); return panel;
    }

    private async Task ReloadAsync(int? selectId=null)
    {
        var rows=await service.SearchAsync(search.Text,includeInactive.Checked);

        suppressSelectionChanged=true;
        try
        {
            grid.Rows.Clear();
            foreach(var r in rows)
                grid.Rows.Add(r.Id,r.DisplayOrder,r.Name,r.ShortCode,r.MovementCount.ToString("N0"),r.IsActive?"Active":"Inactive");

            grid.ClearSelection();

            if(selectId.HasValue && rows.Any(x => x.Id == selectId.Value))
                SelectRow(selectId.Value);
            else if(rows.Count>0)
                SelectRow(rows[0].Id);
        }
        finally
        {
            suppressSelectionChanged=false;
        }

        if(grid.SelectedRows.Count>0)
            await LoadSelectedAsync();
        else if(selectedId!=0)
        {
            selectedId=0;
            savedSnapshot=null;
        }
    }

    private async Task GridSelectionChangedAsync()
    {
        if(grid.SelectedRows.Count==0 || grid.SelectedRows[0].Cells[0].Value is null)
            return;

        var targetId=Convert.ToInt32(grid.SelectedRows[0].Cells[0].Value);

        if(targetId==selectedId)
            return;

        var oldId=selectedId;

        if(!await ConfirmLeaveCurrentAsync())
        {
            suppressSelectionChanged=true;
            try
            {
                grid.ClearSelection();
                if(oldId!=0)
                    SelectRow(oldId);
            }
            finally
            {
                suppressSelectionChanged=false;
            }
            return;
        }

        await LoadSelectedAsync();
    }

    private async Task LoadSelectedAsync()
    {
        if(grid.SelectedRows.Count==0 || grid.SelectedRows[0].Cells[0].Value is null) return;
        selectedId=Convert.ToInt32(grid.SelectedRows[0].Cells[0].Value);
        var item=await service.GetAsync(selectedId); if(item is null) return;
        name.Text=item.Name; shortCode.Text=item.ShortCode; systemCode.Text=item.SystemCode; displayOrder.Value=Math.Clamp(item.DisplayOrder,(int)displayOrder.Minimum,(int)displayOrder.Maximum);
        description.Text=item.Description??""; notes.Text=item.Notes??""; active.Checked=item.IsActive; special.Checked=item.IsSpecialFloorReportContainer; dashboardColour.Text=item.DashboardColour??"";
        deactivate.Text=item.IsActive?"Deactivate":"Reactivate";
        usage.Text=$"Movement records: {item.Usage.MovementCount:N0}   •   Customers with balance: {item.Usage.CustomersWithBalance:N0}\nFirst used: {DateText(item.Usage.FirstUsed)}   •   Last movement: {DateText(item.Usage.LastUsed)}";
        validation.Text="";
        savedSnapshot=CaptureSnapshot();
    }

    private void NewContainer()
    {
        if (!canEdit)
            return;
        suppressSelectionChanged=true;
        try
        {
            selectedId=0; name.Clear(); shortCode.Clear(); systemCode.Text="Created automatically on first save"; description.Clear(); notes.Clear(); dashboardColour.Clear(); displayOrder.Value=0; active.Checked=true; special.Checked=false; usage.Text="No movement history yet."; deactivate.Enabled=false; validation.Text=""; grid.ClearSelection();
            savedSnapshot=CaptureSnapshot();
        }
        finally
        {
            suppressSelectionChanged=false;
        }
        name.Focus();
    }

    private async Task<bool> SaveAsync()
    {
        if (!canEdit)
            return false;

        try
        {
            validation.Text="";
            var existing=selectedId==0?null:await service.GetAsync(selectedId);
            var model=new ContainerTypeEditModel(selectedId,name.Text,shortCode.Text,existing?.SystemCode??"",description.Text,notes.Text,(int)displayOrder.Value,active.Checked,special.Checked,dashboardColour.Text,existing?.Usage??new ContainerTypeUsage(0,0,null,null));
            selectedId=await service.SaveAsync(model);
            deactivate.Enabled=canEdit;
            await ReloadAsync(selectedId);
            savedSnapshot=CaptureSnapshot();
            return true;
        }
        catch(Exception ex)
        {
            validation.Text=ex.Message;
            return false;
        }
    }

    private ContainerEditorSnapshot CaptureSnapshot() =>
        new(
            selectedId,
            name.Text,
            shortCode.Text,
            description.Text,
            notes.Text,
            (int)displayOrder.Value,
            active.Checked,
            special.Checked,
            dashboardColour.Text);

    private bool HasUnsavedChangesInternal() =>
        savedSnapshot is not null &&
        CaptureSnapshot() != savedSnapshot;

    private async Task<bool> ConfirmLeaveCurrentAsync()
    {
        if(!HasUnsavedChangesInternal())
            return true;

        var label=string.IsNullOrWhiteSpace(name.Text)
            ? "this container type"
            : $"'{name.Text.Trim()}'";

        var answer=UnsavedChangesDialog.Ask(
            this,
            "Unsaved Container Type Changes",
            $"You have unsaved changes to {label}.\n\nWhat would you like to do?");

        if(answer==UnsavedChangesChoice.Cancel)
            return false;

        if(answer==UnsavedChangesChoice.Discard)
            return true;

        return await SaveAsync();
    }

    private async void ContainerTypesForm_FormClosing(
        object? sender,
        FormClosingEventArgs e)
    {
        if(bypassClosePrompt || !HasUnsavedChangesInternal())
            return;

        e.Cancel=true;

        if(await ConfirmLeaveCurrentAsync())
        {
            bypassClosePrompt=true;
            Close();
        }
    }

    private async Task ToggleActiveAsync()
    {
        if(!canEdit || selectedId==0) return;
        var item=await service.GetAsync(selectedId); if(item is null) return;
        var action=item.IsActive?"deactivate":"reactivate";
        if(MessageBox.Show($"{char.ToUpper(action[0])+action[1..]} {item.Name}?", "Container Type", MessageBoxButtons.YesNo, MessageBoxIcon.Question)!=DialogResult.Yes) return;
        await service.SetActiveAsync(selectedId,!item.IsActive); await ReloadAsync(selectedId); await LoadSelectedAsync();
    }

    private void SelectRow(int id){ foreach(DataGridViewRow row in grid.Rows) if(Convert.ToInt32(row.Cells[0].Value)==id){ row.Selected=true; grid.CurrentCell=row.Cells[1]; break; } }
    private sealed record ContainerEditorSnapshot(
        int Id,
        string Name,
        string ShortCode,
        string Description,
        string Notes,
        int DisplayOrder,
        bool Active,
        bool Special,
        string DashboardColour);

    private static string DateText(DateOnly? d)=>d?.ToString("dd/MM/yyyy")??"Never";
    private static void Add(TableLayoutPanel f,string label,Control control){ var row=f.RowCount++; f.RowStyles.Add(new RowStyle(SizeType.AutoSize)); f.Controls.Add(new Label{Text=label,AutoSize=true,Margin=new Padding(0,8,18,8),ForeColor=Color.FromArgb(70,80,95)},0,row); control.Margin=new Padding(0,4,0,6); f.Controls.Add(control,1,row); }
    private static TextBox Field(bool multiline=false)=>new(){Dock=DockStyle.Fill,Multiline=multiline,Height=multiline?58:30};
    private static Button ButtonOf(string text,int width)=>new(){Text=text,AutoSize=false,Size=new Size(width,40),Margin=new Padding(0,0,10,0)};
    private static Panel Card()=>new(){Dock=DockStyle.Fill,BackColor=Color.White,Margin=Padding.Empty};
}
