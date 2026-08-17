
using BinTracker.Services;

namespace BinTracker.WinForms;

public sealed class ContainerTokenMappingForm : BinTrackerForm
{
    private readonly IContainerTypeService service;
    private readonly IReadOnlyList<string> tokens;
    private readonly Dictionary<string,int> mappings;
    private readonly DataGridView grid = new()
    {
        Dock=DockStyle.Fill, AllowUserToAddRows=false, AllowUserToDeleteRows=false,
        RowHeadersVisible=false, AutoGenerateColumns=false, BackgroundColor=Color.White
    };

    public IReadOnlyDictionary<string,int> Mappings => mappings;

    public ContainerTokenMappingForm(
        IContainerTypeService service,
        IEnumerable<string> unresolvedTokens,
        IReadOnlyDictionary<string,int> existingMappings)
    {
        this.service=service;
        tokens=unresolvedTokens.Where(x=>!string.IsNullOrWhiteSpace(x))
            .Select(x=>x.Trim()).Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x=>x,StringComparer.OrdinalIgnoreCase).ToList();
        mappings=new Dictionary<string,int>(existingMappings,StringComparer.OrdinalIgnoreCase);

        Text="Map Legacy Container Tokens";
        StartPosition=FormStartPosition.CenterParent;
        AutoScaleMode=AutoScaleMode.Dpi;
        ClientSize=new Size(860,560);
        MinimumSize=new Size(760,500);
        BackColor=Color.FromArgb(245,247,250);
        Font=new Font("Segoe UI",10F);
        Build();
        Shown += async (_,_) => await ReloadAsync();
    }

    private void Build()
    {
        var root=new TableLayoutPanel { Dock=DockStyle.Fill, ColumnCount=1, RowCount=4, Padding=new Padding(16) };
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent,100));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        root.Controls.Add(new Label { Text="Map unknown container tokens", AutoSize=true, Font=new Font("Segoe UI Semibold",17F,FontStyle.Bold) },0,0);
        root.Controls.Add(new Label {
            Text="Unknown bracket tokens are never guessed. Map them to an existing Container Type, or create the missing type first.",
            AutoSize=true, ForeColor=Color.DimGray, MaximumSize=new Size(800,0), Margin=new Padding(0,6,0,12)
        },0,1);

        grid.Columns.Add(new DataGridViewTextBoxColumn { Name="Token", HeaderText="Legacy token", ReadOnly=true, Width=180 });
        grid.Columns.Add(new DataGridViewTextBoxColumn { Name="Example", HeaderText="Example", ReadOnly=true, AutoSizeMode=DataGridViewAutoSizeColumnMode.Fill });
        grid.Columns.Add(new DataGridViewComboBoxColumn { Name="Container", HeaderText="Map to Container Type", Width=280 });
        grid.DataError += (_,_) => {};
        root.Controls.Add(grid,0,2);

        var footer=new TableLayoutPanel { Dock=DockStyle.Top, AutoSize=true, ColumnCount=2, Margin=new Padding(0,12,0,0) };
        footer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));
        footer.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        var manage=new Button { Text="Manage Container Types...", Size=new Size(205,40) };
        manage.Click += async (_,_) => { SaveSelections(); using var f=new ContainerTypesForm(service); f.ShowDialog(this); await ReloadAsync(); };

        var right=new FlowLayoutPanel { AutoSize=true, FlowDirection=FlowDirection.RightToLeft, WrapContents=false };
        var cancel=new Button { Text="Cancel", Size=new Size(110,40) };
        cancel.Click += (_,_) => { DialogResult=DialogResult.Cancel; Close(); };
        var apply=new Button { Text="Apply mappings", Size=new Size(150,40) };
        apply.Click += (_,_) => { SaveSelections(); DialogResult=DialogResult.OK; Close(); };
        right.Controls.Add(cancel); right.Controls.Add(apply);

        footer.Controls.Add(manage,0,0); footer.Controls.Add(right,1,0);
        root.Controls.Add(footer,0,3);
        Controls.Add(root);
    }

    private async Task ReloadAsync()
    {
        var types=await service.SearchAsync(null,false);
        var choices=types.OrderBy(x=>x.DisplayOrder).ThenBy(x=>x.Name)
            .Select(x=>new Choice(x.Id,$"{x.Name} ({x.ShortCode})")).ToList();

        grid.Rows.Clear();
        foreach(var token in tokens)
        {
            var i=grid.Rows.Add(token,$"({token}) Customer");
            var cell=(DataGridViewComboBoxCell)grid.Rows[i].Cells["Container"];
            cell.DisplayMember=nameof(Choice.Name);
            cell.ValueMember=nameof(Choice.Id);
            cell.DataSource=choices.ToList();
            if(mappings.TryGetValue(token,out var id) && choices.Any(x=>x.Id==id))
                cell.Value=id;
        }
    }

    private void SaveSelections()
    {
        foreach(DataGridViewRow row in grid.Rows)
        {
            if(row.Cells["Token"].Value is not string token) continue;
            var value=row.Cells["Container"].Value;
            if(value is null) mappings.Remove(token);
            else mappings[token]=Convert.ToInt32(value);
        }
    }

    private sealed record Choice(int Id,string Name);
}
