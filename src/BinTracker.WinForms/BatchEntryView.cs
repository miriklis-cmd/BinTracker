using BinTracker.Core;
using BinTracker.Services;

namespace BinTracker.WinForms;

public sealed class BatchEntryView : UserControl
{
    private sealed record PreviewBalanceRow(
        int ContainerTypeId,
        string ContainerType,
        int CurrentBalance,
        int PreviewBalance)
    {
        public string Current => MovementPositionMath.Format(CurrentBalance);
        public string Preview => MovementPositionMath.Format(PreviewBalance);
    }

    private readonly IMovementService movements;
    private readonly UserSession session;
    private readonly ApplicationState appState;
    private readonly Action? exitRequested;
    private DraftMovementBatch Draft => appState.DraftBatch;

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
        Minimum = 0,
        Maximum = 100000,
        Value = 0,
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
        BorderStyle = BorderStyle.FixedSingle,
        ShowCellToolTips = false
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
        BorderStyle = BorderStyle.FixedSingle,
        ShowCellToolTips = false
    };

    private readonly Label totals = new()
    {
        AutoSize = true,
        Font = new Font("Segoe UI Semibold", 11F, FontStyle.Bold),
        Margin = new Padding(8, 10, 0, 0)
    };

    private readonly Label status = new()
    {
        AutoSize = true,
        ForeColor = Color.DimGray,
        MaximumSize = new Size(900, 0)
    };

    private IReadOnlyList<MovementCustomerOption> customers = [];
    private IReadOnlyList<MovementContainerOption> containers = [];
    private MovementCustomerSummary? selectedCustomer;
    private bool loadingDraft;
    private DraftMovementLine? editingLine;
    private Button? addOrUpdateButton;
    private bool suppressPendingSelectionChanged;
    private int editLoadGeneration;

    public BatchEntryView(
        IMovementService movements,
        UserSession session,
        ApplicationState appState,
        Action? exitRequested = null)
    {
        this.movements = movements;
        this.session = session;
        this.appState = appState;
        this.exitRequested = exitRequested;

        Dock = DockStyle.Fill;
        AutoScaleMode = AutoScaleMode.Dpi;
        BackColor = Color.FromArgb(245, 247, 250);
        Font = new Font("Segoe UI", 10F);

        Build();
        Load += async (_, _) =>
        {
            quantity.Text = string.Empty;
            await InitialiseAsync();
        };
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if (keyData == (Keys.Control | Keys.Enter))
        {
            _ = SaveBatchAsync();
            return true;
        }

        if (keyData == Keys.Escape)
        {
            if (editingLine is not null)
            {
                CancelEdit(clearCustomer: true);
                ClearCurrentLineEntry(clearContainer: false);
                status.Text = "Edit cancelled. Draft retained.";
                return true;
            }

            if (HasCurrentLineInput())
            {
                ClearCurrentLineEntry();
                status.Text = "Current entry cleared. Draft retained.";
                return true;
            }

            if (exitRequested is not null)
            {
                exitRequested();
                return true;
            }
        }

        return base.ProcessCmdKey(ref msg, keyData);
    }

    private void Build()
    {
        batchType.Items.Add(new BatchTypeOption(MovementType.In, "Returned (IN)"));
        batchType.Items.Add(new BatchTypeOption(MovementType.Out, "Taken (OUT)"));

        pending.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Code",
            DataPropertyName = nameof(DraftMovementLine.CustomerCode),
            Width = 115
        });
        pending.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Customer",
            DataPropertyName = nameof(DraftMovementLine.CustomerName),
            AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
            MinimumWidth = 160
        });
        pending.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Container",
            DataPropertyName = nameof(DraftMovementLine.ContainerType),
            Width = 135
        });
        pending.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Qty",
            DataPropertyName = nameof(DraftMovementLine.Quantity),
            Width = 70
        });
        pending.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Reference",
            DataPropertyName = nameof(DraftMovementLine.Reference),
            Width = 130
        });

        pending.SelectionChanged += async (_, _) =>
        {
            if (suppressPendingSelectionChanged)
                return;

            if (pending.CurrentRow?.DataBoundItem is DraftMovementLine line)
                await LoadLineForEditAsync(line);
        };

        balances.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Container",
            DataPropertyName = nameof(PreviewBalanceRow.ContainerType),
            AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
            MinimumWidth = 120
        });
        balances.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Current",
            DataPropertyName = nameof(PreviewBalanceRow.Current),
            Width = 110
        });
        balances.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "With Draft",
            DataPropertyName = nameof(PreviewBalanceRow.Preview),
            Width = 125
        });

        balances.CellFormatting += (_, e) =>
        {
            if (e.RowIndex < 0 || e.ColumnIndex != 2)
                return;

            if (balances.Rows[e.RowIndex].DataBoundItem is not PreviewBalanceRow row)
                return;

            var style = e.CellStyle ?? new DataGridViewCellStyle();
            e.CellStyle = style;

            style.ForeColor =
                row.PreviewBalance < 0 ? Color.ForestGreen :
                row.PreviewBalance > 0 ? Color.Firebrick :
                Color.DimGray;

            if (row.PreviewBalance != row.CurrentBalance)
                style.Font = new Font(balances.Font, FontStyle.Bold);
        };

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
            Text = "Enter returns and dispatches as separate batches.",
            AutoSize = true,
            ForeColor = Color.DimGray,
            MaximumSize = new Size(520, 0),
            Margin = new Padding(22, 5, 0, 0)
        };

        layout.Controls.Add(hint);
        panel.Controls.Add(layout);

        movementDate.ValueChanged += (_, _) =>
        {
            if (!loadingDraft)
            {
                Draft.MovementDate = DateOnly.FromDateTime(movementDate.Value.Date);
                appState.PersistDraft();
            }
        };

        batchType.SelectionChangeCommitted += (_, _) =>
        {
            if (batchType.SelectedItem is not BatchTypeOption selected)
                return;

            if (Draft.HasLines && selected.Value != Draft.MovementType)
            {
                validation.Text = "Save or clear the current draft before changing between IN and OUT.";
                SelectBatchType(Draft.MovementType);
                return;
            }

            Draft.MovementType = selected.Value;
            appState.PersistDraft();
            RefreshPreview();
        };

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

                if (selectedCustomer is not null)
                    containerType.Focus();
            }
        };

        quantity.KeyDown += async (_, e) =>
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                await SubmitCurrentLineAsync();
            }
        };

        reference.KeyDown += async (_, e) =>
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                await SubmitCurrentLineAsync();
            }
        };

        notes.KeyDown += async (_, e) =>
        {
            if (e.KeyCode == Keys.Enter && !e.Shift)
            {
                e.SuppressKeyPress = true;
                await SubmitCurrentLineAsync();
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
            RowCount = 2,
            Margin = new Padding(0, 14, 0, 0)
        };

        bar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        bar.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        bar.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        bar.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var left = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true
        };

        addOrUpdateButton = ButtonOf("Add to Batch");
        var remove = ButtonOf("Remove");
        var clear = ButtonOf("Clear Batch");

        addOrUpdateButton.Click += async (_, _) =>
        {
            if (editingLine is null)
                await AddLineAsync();
            else
                await UpdateLineAsync();
        };

        remove.Click += (_, _) => RemoveSelected();
        clear.Click += (_, _) => ClearBatch();

        left.Controls.Add(addOrUpdateButton);
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

        var save = ButtonOf("Save Batch");
        save.Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold);
        save.Click += async (_, _) => await SaveBatchAsync();

        right.Controls.Add(new Label
        {
            Text = "Ctrl+Enter",
            AutoSize = true,
            ForeColor = Color.DimGray,
            Margin = new Padding(0, 11, 10, 0)
        });
        right.Controls.Add(save);

        bar.Controls.Add(left, 0, 0);
        bar.Controls.Add(right, 1, 0);
        status.Margin = new Padding(8, 4, 0, 0);
        bar.Controls.Add(status, 0, 1);
        bar.SetColumnSpan(status, 2);

        return bar;
    }

    private Control BuildCustomerPanel()
    {
        var panel = CardPanel();

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Padding = new Padding(20)
        };

        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
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
            Text = "'With Draft' includes unsaved lines currently in this batch. CREDIT means more have been returned than recorded OUT.",
            AutoSize = true,
            ForeColor = Color.DimGray,
            MaximumSize = new Size(430, 0),
            Margin = new Padding(0, 12, 0, 8)
        }, 0, 2);


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

            loadingDraft = true;
            movementDate.Value = Draft.MovementDate.ToDateTime(TimeOnly.MinValue);
            SelectBatchType(Draft.MovementType);
            loadingDraft = false;

            RefreshPendingGrid();

            if (Draft.HasLines)
            {
                status.Text =
                    $"Draft restored: {Draft.Lines.Count} line(s), {Draft.TotalQuantity} containers.";
            }

            customerCode.Focus();
        }
        catch (Exception ex)
        {
            validation.Text = ex.Message;
        }
    }

    private void SelectBatchType(MovementType type)
    {
        for (var i = 0; i < batchType.Items.Count; i++)
        {
            if (batchType.Items[i] is BatchTypeOption option && option.Value == type)
            {
                batchType.SelectedIndex = i;
                return;
            }
        }

        batchType.SelectedIndex = 0;
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

        RefreshPreview();
        status.Text = $"Ready: {selectedCustomer.Code}";
    }

    private void RefreshPreview()
    {
        var customer = selectedCustomer;

        if (customer is null)
        {
            balances.DataSource = null;
            return;
        }

        var rows = customer.Balances
            .Select(balance =>
            {
                var pendingQuantity = Draft.Lines
                    .Where(x =>
                        x.CustomerId == customer.CustomerId &&
                        x.ContainerTypeId == balance.ContainerTypeId)
                    .Sum(x => x.Quantity);

                var preview = balance.Balance;

                if (pendingQuantity > 0)
                {
                    preview = MovementPositionMath.Apply(
                        balance.Balance,
                        Draft.MovementType,
                        pendingQuantity);
                }

                return new PreviewBalanceRow(
                    balance.ContainerTypeId,
                    balance.ContainerType,
                    balance.Balance,
                    preview);
            })
            .ToList();

        balances.DataSource = rows;
    }

    private Task SubmitCurrentLineAsync() =>
        editingLine is null ? AddLineAsync() : UpdateLineAsync();

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

        if (quantity.Value <= 0)
        {
            validation.Text = "Quantity is required and must be greater than zero.";
            quantity.Focus();
            return;
        }

        Draft.MovementDate = DateOnly.FromDateTime(movementDate.Value.Date);

        if (batchType.SelectedItem is BatchTypeOption type)
            Draft.MovementType = type.Value;

        Draft.Lines.Add(new DraftMovementLine(
            selectedCustomer.CustomerId,
            selectedCustomer.Code,
            selectedCustomer.Name,
            selectedContainer.Id,
            selectedContainer.Name,
            (int)quantity.Value,
            Clean(reference.Text),
            Clean(notes.Text)));

        appState.PersistDraft();

        // Movement date, batch direction and container type intentionally carry
        // forward for rapid keyboard entry. Customer-specific fields do not.
        // Clear the editor before rebinding the grid. DataGridView selects the
        // first row during a rebind; without suppressing SelectionChanged that
        // row is immediately loaded back into the editor and makes the reset
        // appear to have failed.
        ClearCurrentLineEntry(clearContainer: false);
        RefreshPendingGrid();
        status.Text = "Line added to draft.";
    }


    private async Task LoadLineForEditAsync(DraftMovementLine line)
    {
        var generation = ++editLoadGeneration;
        editingLine = line;
        customerCode.Text = line.CustomerCode;
        quantity.Value = Math.Clamp(line.Quantity, (int)quantity.Minimum, (int)quantity.Maximum);
        reference.Text = line.Reference ?? string.Empty;
        notes.Text = line.Notes ?? string.Empty;

        for (var i = 0; i < containerType.Items.Count; i++)
        {
            if (containerType.Items[i] is MovementContainerOption option &&
                option.Id == line.ContainerTypeId)
            {
                containerType.SelectedIndex = i;
                break;
            }
        }

        await ResolveCustomerAsync();

        // ResolveCustomerAsync is asynchronous. The user may press Esc, remove/clear
        // the draft, or otherwise leave edit mode while that lookup is running.
        // Never let a stale continuation resurrect Update Line mode afterwards.
        if (generation != editLoadGeneration || editingLine != line)
            return;

        if (addOrUpdateButton is not null)
            addOrUpdateButton.Text = "Update Line";

        status.Text = $"Editing draft line for {line.CustomerCode}. Press Esc to cancel.";
    }

    private async Task UpdateLineAsync()
    {
        if (editingLine is null)
            return;

        await ResolveCustomerAsync();

        if (selectedCustomer is null)
            return;

        if (containerType.SelectedItem is not MovementContainerOption selectedContainer)
            return;

        if (quantity.Value <= 0)
        {
            validation.Text = "Quantity is required and must be greater than zero.";
            quantity.Focus();
            return;
        }

        var index = Draft.Lines.IndexOf(editingLine);
        if (index < 0)
        {
            CancelEdit();
            return;
        }

        Draft.Lines[index] = new DraftMovementLine(
            selectedCustomer.CustomerId,
            selectedCustomer.Code,
            selectedCustomer.Name,
            selectedContainer.Id,
            selectedContainer.Name,
            (int)quantity.Value,
            Clean(reference.Text),
            Clean(notes.Text));
        appState.PersistDraft();

        var code = selectedCustomer.Code;
        RefreshPendingGrid();
        CancelEdit(clearCustomer: true);
        ClearCurrentLineEntry(clearContainer: false);
        status.Text = $"Draft line updated for {code}.";
    }

    private void CancelEdit(bool clearCustomer = true)
    {
        // Invalidate any in-flight asynchronous row-load before resetting the UI.
        editLoadGeneration++;
        editingLine = null;

        if (addOrUpdateButton is not null)
            addOrUpdateButton.Text = "Add to Batch";

        quantity.Value = 0;
        quantity.Text = string.Empty;
        reference.Clear();
        notes.Clear();

        if (clearCustomer)
        {
            customerCode.Clear();
            customerInfo.Text = string.Empty;
            balances.DataSource = null;
            selectedCustomer = null;
        }

        // DataGridView.ClearSelection() does not clear CurrentRow/CurrentCell.
        // SelectionChanged can therefore fire during an Esc/reset and reload the
        // same row asynchronously, putting the editor straight back into Update mode.
        suppressPendingSelectionChanged = true;
        try
        {
            pending.ClearSelection();
            pending.CurrentCell = null;
        }
        finally
        {
            suppressPendingSelectionChanged = false;
        }
    }

    private void RemoveSelected()
    {
        if (pending.CurrentRow?.DataBoundItem is not DraftMovementLine line)
            return;

        Draft.Lines.Remove(line);
        appState.PersistDraft();

        RefreshPendingGrid();
        CancelEdit(clearCustomer: true);
        ClearCurrentLineEntry(clearContainer: false);

        status.Text = "Draft line removed.";
    }

    private void ClearBatch()
    {
        if (Draft.HasLines && MessageBox.Show(
                "Clear every unsaved movement from this batch?",
                "Clear Batch",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question) != DialogResult.Yes)
            return;

        // Clear Batch is also the explicit escape hatch from any stale/partial edit
        // state. Even with zero draft rows it must leave the editor ready to add.
        if (Draft.HasLines)
            appState.ClearDraft();

        CancelEdit(clearCustomer: true);
        ClearCurrentLineEntry(clearContainer: false);

        loadingDraft = true;
        movementDate.Value = Draft.MovementDate.ToDateTime(TimeOnly.MinValue);
        SelectBatchType(Draft.MovementType);
        loadingDraft = false;

        RefreshPendingGrid();
        RefreshPreview();

        status.Text = "Draft batch cleared.";
    }

    private async Task SaveBatchAsync()
    {
        validation.Text = string.Empty;

        if (!Draft.HasLines)
        {
            validation.Text = "Add at least one movement before saving the batch.";
            customerCode.Focus();
            return;
        }

        var direction = Draft.MovementType == MovementType.In
            ? "Returned (IN)"
            : "Taken (OUT)";

        var answer = MessageBox.Show(
            $"Save this {direction} batch?\n\n" +
            $"Lines: {Draft.Lines.Count}\n" +
            $"Total containers: {Draft.TotalQuantity}\n" +
            $"Date: {Draft.MovementDate:dd/MM/yyyy}",
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
                    Draft.MovementDate,
                    Draft.MovementType,
                    null,
                    Draft.Lines
                        .Select(x => new MovementBatchLine(
                            x.CustomerId,
                            x.ContainerTypeId,
                            x.Quantity,
                            x.Reference,
                            x.Notes))
                        .ToList()));

            appState.ClearDraft();

            loadingDraft = true;
            movementDate.Value = Draft.MovementDate.ToDateTime(TimeOnly.MinValue);
            SelectBatchType(Draft.MovementType);
            loadingDraft = false;

            RefreshPendingGrid();
            ClearCurrentLineEntry(clearContainer: false);

            status.Text =
                $"Saved batch #{result.BatchId}: {result.LineCount} line(s), " +
                $"{result.TotalQuantity} total containers.";
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

    private bool HasCurrentLineInput() =>
        selectedCustomer is not null ||
        !string.IsNullOrWhiteSpace(customerCode.Text) ||
        quantity.Value > 0 ||
        !string.IsNullOrWhiteSpace(reference.Text) ||
        !string.IsNullOrWhiteSpace(notes.Text);

    private void ClearCurrentLineEntry(bool clearContainer = false)
    {
        selectedCustomer = null;
        customerCode.Clear();
        customerInfo.Text = string.Empty;
        quantity.Value = 0;
        quantity.Text = string.Empty;
        reference.Clear();
        notes.Clear();
        validation.Text = string.Empty;
        balances.DataSource = null;

        if (clearContainer && containerType.Items.Count > 0)
            containerType.SelectedIndex = 0;

        pending.ClearSelection();
        customerCode.Focus();
    }

    private void RefreshPendingGrid()
    {
        suppressPendingSelectionChanged = true;
        try
        {
            pending.DataSource = null;
            pending.DataSource = Draft.Lines.ToList();
            pending.ClearSelection();
            pending.CurrentCell = null;
        }
        finally
        {
            suppressPendingSelectionChanged = false;
        }

        totals.Text = Draft.HasLines
            ? $"{Draft.Lines.Count} line(s)  •  {Draft.TotalQuantity} containers"
            : "No unsaved movements";
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

    private static Button ButtonOf(string text) => new()
    {
        Text = text,
        AutoSize = false,
        Size = new Size(150, 42),
        Margin = new Padding(0, 0, 10, 0),
        Padding = new Padding(0),
        TextAlign = ContentAlignment.MiddleCenter,
        UseVisualStyleBackColor = true
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
