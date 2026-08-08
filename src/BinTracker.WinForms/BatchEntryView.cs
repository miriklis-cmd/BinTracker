using BinTracker.Core;
using BinTracker.Services;

namespace BinTracker.WinForms;

public sealed class BatchEntryView : UserControl
{
    private sealed record PendingLine(
        int CustomerId,
        string CustomerCode,
        string CustomerName,
        int ContainerTypeId,
        string ContainerType,
        int Quantity,
        string? Reference,
        string? Notes);

    private readonly IMovementService movements;
    private readonly UserSession session;

    private readonly DateTimePicker movementDate = new()
    {
        Format = DateTimePickerFormat.Short,
        Width = 150
    };

    private readonly ComboBox batchType = new()
    {
        DropDownStyle = ComboBoxStyle.DropDownList,
        Width = 190
    };

    private readonly TextBox customerCode = new()
    {
        CharacterCasing = CharacterCasing.Upper,
        Width = 220
    };

    private readonly Label customerInfo = new()
    {
        AutoSize = true,
        ForeColor = Color.DimGray,
        MaximumSize = new Size(620, 0)
    };

    private readonly ComboBox containerType = new()
    {
        DropDownStyle = ComboBoxStyle.DropDownList,
        Width = 240
    };

    private readonly NumericUpDown quantity = new()
    {
        Minimum = 1,
        Maximum = 100000,
        Value = 1,
        Width = 140,
        ThousandsSeparator = true
    };

    private readonly TextBox reference = new() { Width = 260 };
    private readonly TextBox notes = new() { Width = 420 };

    private readonly Label validation = new()
    {
        AutoSize = true,
        ForeColor = Color.Firebrick,
        MaximumSize = new Size(700, 0)
    };

    private readonly DataGridView pending = new()
    {
        Dock = DockStyle.Fill,
        ReadOnly = true,
        AutoGenerateColumns = false,
        SelectionMode = DataGridViewSelectionMode.FullRowSelect,
        MultiSelect = false,
        AllowUserToAddRows = false,
        AllowUserToDeleteRows = false,
        RowHeadersVisible = false,
        BackgroundColor = Color.White,
        BorderStyle = BorderStyle.FixedSingle
    };

    private readonly DataGridView balances = new()
    {
        Dock = DockStyle.Fill,
        ReadOnly = true,
        AutoGenerateColumns = false,
        SelectionMode = DataGridViewSelectionMode.FullRowSelect,
        MultiSelect = false,
        AllowUserToAddRows = false,
        AllowUserToDeleteRows = false,
        RowHeadersVisible = false,
        BackgroundColor = Color.White,
        BorderStyle = BorderStyle.FixedSingle
    };

    private readonly Label totals = new()
    {
        AutoSize = true,
        Font = new Font("Segoe UI Semibold", 11F, FontStyle.Bold)
    };

    private readonly Label status = new()
    {
        AutoSize = true,
        ForeColor = Color.DimGray,
        MaximumSize = new Size(900, 0)
    };

    private readonly List<PendingLine> lines = [];
    private IReadOnlyList<MovementCustomerOption> customers = [];
    private IReadOnlyList<MovementContainerOption> containers = [];
    private MovementCustomerSummary? selectedCustomer;

    public BatchEntryView(IMovementService movements, UserSession session)
    {
        this.movements = movements;
        this.session = session;

        Dock = DockStyle.Fill;
        AutoScaleMode = AutoScaleMode.Dpi;
        BackColor = Color.FromArgb(245, 247, 250);
        Font = new Font("Segoe UI", 10F);

        Build();

        Load += async (_, _) => await InitialiseAsync();
    }

    private void Build()
    {
        batchType.Items.Add(new BatchTypeOption(MovementType.In, "Returned (IN)"));
        batchType.Items.Add(new BatchTypeOption(MovementType.Out, "Taken (OUT)"));
        batchType.SelectedIndex = 0;

        pending.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Code",
            DataPropertyName = nameof(PendingLine.CustomerCode),
            Width = 120
        });
        pending.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Customer",
            DataPropertyName = nameof(PendingLine.CustomerName),
            AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
            MinimumWidth = 180
        });
        pending.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Container",
            DataPropertyName = nameof(PendingLine.ContainerType),
            Width = 150
        });
        pending.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Qty",
            DataPropertyName = nameof(PendingLine.Quantity),
            Width = 85
        });
        pending.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Reference",
            DataPropertyName = nameof(PendingLine.Reference),
            Width = 160
        });

        balances.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Container",
            DataPropertyName = nameof(MovementBalanceRow.ContainerType),
            AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
            MinimumWidth = 140
        });
        balances.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Balance",
            DataPropertyName = nameof(MovementBalanceRow.Position),
            Width = 145
        });

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 2,
            Margin = new Padding(0),
            Padding = new Padding(0)
        };

        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 66F));
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 34F));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

        var header = BuildBatchHeader();
        root.Controls.Add(header, 0, 0);
        root.SetColumnSpan(header, 2);

        root.Controls.Add(BuildEntryAndGrid(), 0, 1);
        root.Controls.Add(BuildCustomerPanel(), 1, 1);

        Controls.Add(root);
    }

    private Control BuildBatchHeader()
    {
        var panel = CardPanel();
        panel.Padding = new Padding(20);

        var layout = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            WrapContents = true,
            FlowDirection = FlowDirection.LeftToRight
        };

        layout.Controls.Add(LabelFor("Movement date"));
        layout.Controls.Add(movementDate);
        layout.Controls.Add(Spacer(20));
        layout.Controls.Add(LabelFor("Batch type"));
        layout.Controls.Add(batchType);

        var hint = new Label
        {
            Text = "Enter all returns as one batch, then switch to Taken (OUT) for dispatches.",
            AutoSize = true,
            ForeColor = Color.DimGray,
            MaximumSize = new Size(680, 0),
            Margin = new Padding(22, 5, 0, 0)
        };

        layout.Controls.Add(hint);
        panel.Controls.Add(layout);
        return panel;
    }

    private Control BuildEntryAndGrid()
    {
        var panel = CardPanel();

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4,
            Padding = new Padding(20)
        };

        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        layout.Controls.Add(new Label
        {
            Text = "Add movement to batch",
            AutoSize = true,
            Font = new Font("Segoe UI Semibold", 15F, FontStyle.Bold),
            Margin = new Padding(0, 0, 0, 12)
        }, 0, 0);

        layout.Controls.Add(BuildEntryFields(), 0, 1);
        layout.Controls.Add(pending, 0, 2);
        layout.Controls.Add(BuildBatchActions(), 0, 3);

        panel.Controls.Add(layout);
        return panel;
    }

    private Control BuildEntryFields()
    {
        var form = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            ColumnCount = 4,
            RowCount = 5,
            Margin = new Padding(0, 0, 0, 14)
        };

        form.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        form.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
        form.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        form.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));

        AddField(form, 0, "Customer code", customerCode, "Container type", containerType);
        AddField(form, 1, "Quantity", quantity, "Reference", reference);

        form.Controls.Add(LabelFor("Notes"), 0, 2);
        form.Controls.Add(notes, 1, 2);
        form.SetColumnSpan(notes, 3);
        notes.Dock = DockStyle.Fill;

        form.Controls.Add(customerInfo, 0, 3);
        form.SetColumnSpan(customerInfo, 4);

        form.Controls.Add(validation, 0, 4);
        form.SetColumnSpan(validation, 4);

        customerCode.Leave += async (_, _) => await ResolveCustomerAsync();
        customerCode.KeyDown += async (_, e) =>
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                await ResolveCustomerAsync();
                containerType.Focus();
            }
        };

        notes.KeyDown += async (_, e) =>
        {
            if (e.KeyCode == Keys.Enter && !e.Shift)
            {
                e.SuppressKeyPress = true;
                await AddLineAsync();
            }
        };

        return form;
    }

    private Control BuildBatchActions()
    {
        var bar = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            ColumnCount = 2,
            Margin = new Padding(0, 14, 0, 0)
        };

        bar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        bar.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        var left = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true
        };

        var add = ButtonOf("Add to Batch", 145);
        var remove = ButtonOf("Remove Selected", 155);
        var clear = ButtonOf("Clear Batch", 130);

        add.Click += async (_, _) => await AddLineAsync();
        remove.Click += (_, _) => RemoveSelected();
        clear.Click += (_, _) => ClearBatch();

        left.Controls.Add(add);
        left.Controls.Add(remove);
        left.Controls.Add(clear);
        left.Controls.Add(totals);

        var right = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false
        };

        var save = ButtonOf("Save Batch", 145);
        save.Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold);
        save.Click += async (_, _) => await SaveBatchAsync();

        right.Controls.Add(save);

        bar.Controls.Add(left, 0, 0);
        bar.Controls.Add(right, 1, 0);

        return bar;
    }

    private Control BuildCustomerPanel()
    {
        var panel = CardPanel();

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4,
            Padding = new Padding(20)
        };

        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        layout.Controls.Add(new Label
        {
            Text = "Customer position",
            AutoSize = true,
            Font = new Font("Segoe UI Semibold", 15F, FontStyle.Bold),
            Margin = new Padding(0, 0, 0, 12)
        }, 0, 0);

        layout.Controls.Add(balances, 0, 1);

        layout.Controls.Add(new Label
        {
            Text = "Positive = customer owes containers. CREDIT = they have returned more than recorded OUT.",
            AutoSize = true,
            ForeColor = Color.DimGray,
            MaximumSize = new Size(420, 0),
            Margin = new Padding(0, 12, 0, 8)
        }, 0, 2);

        layout.Controls.Add(status, 0, 3);

        panel.Controls.Add(layout);
        return panel;
    }

    private async Task InitialiseAsync()
    {
        try
        {
            customers = await movements.GetActiveCustomersAsync();
            containers = await movements.GetActiveContainerTypesAsync();

            containerType.DataSource = containers.ToList();
            containerType.DisplayMember = nameof(MovementContainerOption.Name);
            containerType.ValueMember = nameof(MovementContainerOption.Id);

            var autocomplete = new AutoCompleteStringCollection();
            autocomplete.AddRange(customers.Select(x => x.Code).ToArray());

            customerCode.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
            customerCode.AutoCompleteSource = AutoCompleteSource.CustomSource;
            customerCode.AutoCompleteCustomSource = autocomplete;

            RefreshPendingGrid();
            customerCode.Focus();
        }
        catch (Exception ex)
        {
            validation.Text = ex.Message;
        }
    }

    private async Task ResolveCustomerAsync()
    {
        validation.Text = string.Empty;
        var code = customerCode.Text.Trim().ToUpperInvariant();
        customerCode.Text = code;

        if (code.Length == 0)
        {
            selectedCustomer = null;
            customerInfo.Text = string.Empty;
            balances.DataSource = null;
            return;
        }

        selectedCustomer = await movements.GetCustomerSummaryByCodeAsync(code);

        if (selectedCustomer is null)
        {
            customerInfo.Text = string.Empty;
            balances.DataSource = null;
            validation.Text = $"Customer code '{code}' was not found or is inactive.";
            return;
        }

        customerInfo.Text =
            $"{selectedCustomer.Code} — {selectedCustomer.Name}  •  " +
            (selectedCustomer.CustomerType == CustomerType.CashCod ? "Cash / COD" : "Account");

        balances.DataSource = selectedCustomer.Balances.ToList();
        status.Text = $"Ready: {selectedCustomer.Code}";
    }

    private async Task AddLineAsync()
    {
        validation.Text = string.Empty;

        await ResolveCustomerAsync();

        if (selectedCustomer is null)
        {
            customerCode.Focus();
            return;
        }

        if (containerType.SelectedItem is not MovementContainerOption selectedContainer)
        {
            validation.Text = "Select a container type.";
            containerType.Focus();
            return;
        }

        var line = new PendingLine(
            selectedCustomer.CustomerId,
            selectedCustomer.Code,
            selectedCustomer.Name,
            selectedContainer.Id,
            selectedContainer.Name,
            (int)quantity.Value,
            Clean(reference.Text),
            Clean(notes.Text));

        lines.Add(line);
        RefreshPendingGrid();

        reference.Clear();
        notes.Clear();
        quantity.Value = 1;

        customerCode.Clear();
        customerInfo.Text = string.Empty;
        balances.DataSource = null;
        selectedCustomer = null;

        customerCode.Focus();
    }

    private void RemoveSelected()
    {
        if (pending.CurrentRow?.DataBoundItem is not PendingLine line)
            return;

        lines.Remove(line);
        RefreshPendingGrid();
    }

    private void ClearBatch()
    {
        if (lines.Count == 0)
            return;

        if (MessageBox.Show(
                "Clear every unsaved movement from this batch?",
                "Clear Batch",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question) != DialogResult.Yes)
            return;

        lines.Clear();
        RefreshPendingGrid();
        status.Text = "Batch cleared.";
    }

    private async Task SaveBatchAsync()
    {
        validation.Text = string.Empty;

        if (lines.Count == 0)
        {
            validation.Text = "Add at least one movement before saving the batch.";
            customerCode.Focus();
            return;
        }

        if (batchType.SelectedItem is not BatchTypeOption type)
        {
            validation.Text = "Select a batch type.";
            return;
        }

        var direction = type.Value == MovementType.In
            ? "Returned (IN)"
            : "Taken (OUT)";

        var total = lines.Sum(x => x.Quantity);

        var answer = MessageBox.Show(
            $"Save this {direction} batch?\n\n" +
            $"Lines: {lines.Count}\n" +
            $"Total containers: {total}\n" +
            $"Date: {movementDate.Value:dd/MM/yyyy}",
            "Save Batch",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);

        if (answer != DialogResult.Yes)
            return;

        try
        {
            Enabled = false;

            var result = await movements.SaveBatchAsync(
                new SaveMovementBatchRequest(
                    DateOnly.FromDateTime(movementDate.Value.Date),
                    type.Value,
                    null,
                    lines.Select(x => new MovementBatchLine(
                        x.CustomerId,
                        x.ContainerTypeId,
                        x.Quantity,
                        x.Reference,
                        x.Notes))
                    .ToList()));

            lines.Clear();
            RefreshPendingGrid();

            status.Text =
                $"Saved batch #{result.BatchId}: {result.LineCount} line(s), " +
                $"{result.TotalQuantity} total containers.";

            customerCode.Focus();
        }
        catch (Exception ex)
        {
            validation.Text = ex.Message;
        }
        finally
        {
            Enabled = true;
        }
    }

    private void RefreshPendingGrid()
    {
        pending.DataSource = null;
        pending.DataSource = lines.ToList();

        totals.Text = lines.Count == 0
            ? "No unsaved movements"
            : $"{lines.Count} line(s)  •  {lines.Sum(x => x.Quantity)} containers";
    }

    private static string? Clean(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static Panel CardPanel() => new()
    {
        Dock = DockStyle.Fill,
        BackColor = Color.White,
        Margin = new Padding(0, 0, 16, 16)
    };

    private static Label LabelFor(string text) => new()
    {
        Text = text,
        AutoSize = true,
        Anchor = AnchorStyles.Left,
        Margin = new Padding(0, 7, 10, 7),
        ForeColor = Color.FromArgb(70, 80, 95)
    };

    private static Label Spacer(int width) => new()
    {
        Text = string.Empty,
        AutoSize = false,
        Width = width,
        Height = 1
    };

    private static Button ButtonOf(string text, int width) => new()
    {
        Text = text,
        AutoSize = false,
        Size = new Size(width, 42),
        Margin = new Padding(0, 0, 10, 0)
    };

    private static void AddField(
        TableLayoutPanel form,
        int row,
        string label1,
        Control control1,
        string label2,
        Control control2)
    {
        form.Controls.Add(LabelFor(label1), 0, row);
        form.Controls.Add(control1, 1, row);
        form.Controls.Add(LabelFor(label2), 2, row);
        form.Controls.Add(control2, 3, row);

        control1.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        control2.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        control1.Margin = new Padding(0, 4, 18, 4);
        control2.Margin = new Padding(0, 4, 0, 4);
    }

    private sealed record BatchTypeOption(MovementType Value, string Text)
    {
        public override string ToString() => Text;
    }
}
