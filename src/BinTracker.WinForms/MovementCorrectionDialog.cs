using BinTracker.Core;
using BinTracker.Services;

namespace BinTracker.WinForms;

internal sealed class MovementCorrectionDialog : BinTrackerForm
{
    private readonly DateTimePicker date = new() { Format = DateTimePickerFormat.Short, Width = 150 };
    private readonly ComboBox customer = new() { DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Fill };
    private readonly ComboBox container = new() { DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Fill };
    private readonly ComboBox direction = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 220 };
    private readonly NumericUpDown quantity = new() { Minimum = 1, Maximum = 100000000, Width = 150, ThousandsSeparator = true };
    private readonly TextBox reference = new() { Dock = DockStyle.Fill, MaxLength = 200 };
    private readonly TextBox notes = new() { Dock = DockStyle.Fill, MaxLength = 2000 };
    private readonly TextBox reason = new() { Multiline = true, MaxLength = 500, Dock = DockStyle.Fill };

    public DateOnly CorrectedDate => DateOnly.FromDateTime(date.Value);
    public int CustomerId => Selected<int>(customer, "customer");
    public int ContainerTypeId => Selected<int>(container, "container type");
    public MovementType CorrectedDirection => Selected<MovementType>(direction, "direction");
    public int CorrectedQuantity => decimal.ToInt32(quantity.Value);
    public string Reference => reference.Text.Trim();
    public string Notes => notes.Text.Trim();
    public string Reason => reason.Text.Trim();

    public MovementCorrectionDialog(MovementCorrectionDetail movement,
        IReadOnlyList<CustomerListRow> customers, IReadOnlyList<ContainerTypeListRow> containers,
        DateOnly businessToday)
    {
        Text = "Correct Saved Movement";
        StartPosition = FormStartPosition.CenterParent; FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false; MinimizeBox = false; ShowInTaskbar = false;
        AutoScaleMode = AutoScaleMode.Dpi; ClientSize = new Size(800, 720);
        Font = new Font("Segoe UI", 10F);
        date.MaxDate = businessToday.ToDateTime(TimeOnly.MinValue);
        date.Value = movement.MovementDate.ToDateTime(TimeOnly.MinValue);
        var persisted = MovementCorrectionSelection.Resolve(movement, customers, containers);
        var customerChoices = customers.Select(x => new Choice<int>(x.Id, $"{x.CustomerCode} — {x.Name}")).ToArray();
        var containerChoices = containers.Select(x => new Choice<int>(x.Id, x.Name + (x.IsActive ? "" : " (inactive)"))).ToArray();
        var directionChoices = new[] { new Choice<MovementType>(MovementType.In, "IN — Returned"), new Choice<MovementType>(MovementType.Out, "OUT — Taken") };
        customer.Items.AddRange(customerChoices);
        container.Items.AddRange(containerChoices);
        direction.Items.AddRange(directionChoices);
        customer.SelectedItem = customerChoices.Single(x => x.Value == persisted.Customer.Id);
        container.SelectedItem = containerChoices.Single(x => x.Value == persisted.ContainerType.Id);
        direction.SelectedItem = directionChoices.Single(x => x.Value == movement.Direction);
        quantity.Value = movement.Quantity; reference.Text = movement.Reference; notes.Text = movement.Notes;

        var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 11, Padding = new Padding(22) };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 220)); root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        var heading = new Label { Text = "Correct Saved Movement", AutoSize = true, Font = new Font("Segoe UI Semibold", 18F, FontStyle.Bold) };
        root.Controls.Add(heading, 0, 0);
        root.SetColumnSpan(heading, 2);
        var warning = new Label { Text = $"Movement #{movement.MovementId} is saved history. The original row remains preserved; BinTracker will neutralise it and create a linked corrected replacement.", AutoSize = true, MaximumSize = new Size(700, 0), ForeColor = Color.FromArgb(70, 80, 95), Margin = new Padding(0, 8, 0, 14) };
        root.Controls.Add(warning, 0, 1); root.SetColumnSpan(warning, 2);
        Add(root, 2, "Original/current", $"{movement.MovementDate:dd/MM/yyyy} · {movement.CustomerCode} — {movement.CustomerName} · {movement.ContainerType} · {movement.Direction.ToString().ToUpperInvariant()} {movement.Quantity:N0}");
        Add(root, 3, "Corrected date", date); Add(root, 4, "Corrected customer", customer);
        Add(root, 5, "Corrected container", container); Add(root, 6, "Corrected direction", direction);
        Add(root, 7, "Corrected quantity", quantity); Add(root, 8, "Reference", reference); Add(root, 9, "Notes", notes);
        var reasonPanel = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2 };
        reasonPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize)); reasonPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 80));
        reasonPanel.Controls.Add(new Label { Text = "Correction reason (required)", AutoSize = true }, 0, 0); reasonPanel.Controls.Add(reason, 0, 1);
        root.Controls.Add(reasonPanel, 0, 10); root.SetColumnSpan(reasonPanel, 2);
        var buttons = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 58, FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(10) };
        var save = new Button { Text = "Create Correction", Size = new Size(160, 38) };
        var cancel = new Button { Text = "Cancel", Size = new Size(100, 38) };
        save.Click += (_, _) => { if (Reason.Length < 3) { MessageBox.Show(this, "Enter a correction reason."); reason.Focus(); return; } DialogResult = DialogResult.OK; };
        cancel.Click += (_, _) => DialogResult = DialogResult.Cancel; buttons.Controls.Add(save); buttons.Controls.Add(cancel);
        Controls.Add(root); Controls.Add(buttons); AcceptButton = save; CancelButton = cancel;
    }

    private static void Add(TableLayoutPanel root, int row, string label, object control)
    {
        root.Controls.Add(new Label { Text = label, AutoSize = true, AutoEllipsis = false,
            Margin = new Padding(0, 8, 12, 0) }, 0, row);
        var value = control is Control c ? c : new Label { Text = control.ToString(), AutoSize = true };
        value.Margin = new Padding(0, 4, 0, 4);
        root.Controls.Add(value, 1, row);
    }
    private static T Selected<T>(ComboBox box, string field) => box.SelectedItem is Choice<T> choice
        ? choice.Value
        : throw new InvalidOperationException($"Select a corrected {field}.");
    private sealed record Choice<T>(T Value, string Text) { public override string ToString() => Text; }
}

internal sealed class BatchCorrectionDialog : BinTrackerForm
{
    private readonly CheckBox changeDate = new() { Text = "Correct date for every line", AutoSize = true };
    private readonly DateTimePicker date = new() { Format = DateTimePickerFormat.Short, Width = 150 };
    private readonly CheckBox changeDirection = new() { Text = "Correct direction for every line", AutoSize = true };
    private readonly ComboBox direction = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 170 };
    private readonly TextBox reason = new() { Multiline = true, MaxLength = 500, Dock = DockStyle.Fill };
    public DateOnly? CorrectedDate => changeDate.Checked ? DateOnly.FromDateTime(date.Value) : null;
    public MovementType? CorrectedDirection => changeDirection.Checked && direction.SelectedItem is DirectionChoice selected
        ? selected.Value
        : null;
    public string Reason => reason.Text.Trim();

    public BatchCorrectionDialog(MovementBatchCorrectionDetail batch, DateOnly businessToday)
    {
        Text = "Correct Entire Saved Batch"; StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog; ClientSize = new Size(780, 620);
        AutoScaleMode = AutoScaleMode.Dpi; Font = new Font("Segoe UI", 10F);
        MaximizeBox = false; MinimizeBox = false; ShowInTaskbar = false;
        date.MaxDate = businessToday.ToDateTime(TimeOnly.MinValue);
        date.Value = batch.MovementDate.ToDateTime(TimeOnly.MinValue);
        var directionChoices = new[] { new DirectionChoice(MovementType.In, "IN — Returned"), new DirectionChoice(MovementType.Out, "OUT — Taken") };
        var directionIndex = MovementCorrectionSelection.ResolveBatchDirectionIndex(
            batch, directionChoices.Select(x => x.Value).ToArray());
        direction.Items.AddRange(directionChoices);
        direction.SelectedIndex = directionIndex;
        // Only the potentially long persisted-line list scrolls. Batch identity,
        // correction controls and the alpha.8.5 fixed action band stay visible.
        var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 5, Padding = new Padding(24, 18, 24, 0) };
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.Controls.Add(new Label { Text = "Correct Entire Persisted Batch", AutoSize = true, Font = new Font("Segoe UI Semibold", 18F, FontStyle.Bold), Margin = new Padding(0, 0, 0, 8) }, 0, 0);
        root.Controls.Add(new Label { Text = $"Batch #{batch.BatchId} · {batch.LineCount:N0} affected lines · {batch.TotalContainers:N0} total containers\r\nExisting date: {batch.MovementDate:dd/MM/yyyy} · Existing direction: {batch.Direction.ToString().ToUpperInvariant()}\r\nEVERY line in this persisted batch will be neutralised and replaced atomically.", AutoSize = true, MaximumSize = new Size(710, 0), Margin = new Padding(0, 0, 0, 10) }, 0, 1);
        var preview = new ListBox { Dock = DockStyle.Fill, IntegralHeight = false, HorizontalScrollbar = false, MinimumSize = new Size(0, 100), Margin = new Padding(0, 0, 0, 10) };
        preview.Items.AddRange(batch.Lines.Select(x =>
            (object)$"Movement #{x.MovementId} · {x.CustomerCode} — {x.CustomerName} · {x.ContainerType} · {x.Quantity:N0}").ToArray());
        root.Controls.Add(preview, 0, 2);
        var fields = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, ColumnCount = 2, RowCount = 3, Margin = Padding.Empty };
        fields.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 310)); fields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        fields.Controls.Add(changeDate, 0, 0); fields.Controls.Add(date, 1, 0);
        fields.Controls.Add(changeDirection, 0, 1); fields.Controls.Add(direction, 1, 1);
        var reasonLabel = new Label { Text = "Correction reason (required)", AutoSize = true, Margin = new Padding(0, 10, 0, 3) };
        fields.Controls.Add(reasonLabel, 0, 2);
        fields.SetColumnSpan(reasonLabel, 2);
        var reasonHost = new Panel { Dock = DockStyle.Top, Height = 76, Margin = new Padding(0, 0, 0, 8) }; reasonHost.Controls.Add(reason);
        var fieldHost = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, ColumnCount = 1, RowCount = 2, Margin = Padding.Empty };
        fieldHost.Controls.Add(fields, 0, 0); fieldHost.Controls.Add(reasonHost, 0, 1);
        root.Controls.Add(fieldHost, 0, 3);
        var buttons = new FlowLayoutPanel
        {
            AutoSize = true,
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            Padding = new Padding(0, 8, 0, 10)
        };
        var save = new Button { Text = "Confirm Every Line", Size = new Size(180, 40) };
        var cancel = new Button { Text = "Cancel", Size = new Size(110, 40) };
        save.Click += (_, _) => { if ((!changeDate.Checked && !changeDirection.Checked) || Reason.Length < 3) { MessageBox.Show(this, "Select a date and/or direction correction and enter a reason."); return; } DialogResult = DialogResult.OK; };
        cancel.Click += (_, _) => DialogResult = DialogResult.Cancel;
        buttons.Controls.Add(save);
        buttons.Controls.Add(cancel);
        root.Controls.Add(buttons, 0, 4);
        Controls.Add(root);
        AcceptButton = save;
        CancelButton = cancel;
    }
    private sealed record DirectionChoice(MovementType Value, string Text) { public override string ToString() => Text; }
}
