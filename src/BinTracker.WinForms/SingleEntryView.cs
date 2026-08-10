using BinTracker.Core;
using BinTracker.Services;

namespace BinTracker.WinForms;

/// <summary>
/// Records one manual container movement and previews the resulting customer
/// position before anything is committed to the database.
/// </summary>
public sealed class SingleEntryView : UserControl
{
    private readonly IMovementService movements;
    private readonly UserSession session;

    private readonly DateTimePicker movementDate = new()
    {
        Format = DateTimePickerFormat.Short,
        Width = 145
    };

    private readonly ComboBox movementType = new()
    {
        DropDownStyle = ComboBoxStyle.DropDownList,
        Width = 180
    };

    private readonly TextBox customerCode = new()
    {
        Dock = DockStyle.Fill,
        CharacterCasing = CharacterCasing.Upper
    };

    private readonly ComboBox containerType = new()
    {
        Dock = DockStyle.Fill,
        DropDownStyle = ComboBoxStyle.DropDownList
    };

    private readonly NumericUpDown quantity = new()
    {
        Dock = DockStyle.Fill,
        Minimum = 0,
        Maximum = 100000,
        Value = 0,
        ThousandsSeparator = true
    };

    private readonly TextBox reference = new() { Dock = DockStyle.Fill };
    private readonly TextBox notes = new() { Dock = DockStyle.Fill };

    private readonly Label customerInfo = new()
    {
        AutoSize = true,
        ForeColor = Color.DimGray
    };

    private readonly Label validation = new()
    {
        AutoSize = true,
        ForeColor = Color.Firebrick,
        MaximumSize = new Size(760, 0)
    };

    private readonly Label status = new()
    {
        AutoSize = true,
        ForeColor = Color.DimGray
    };

    private readonly DataGridView balances = new()
    {
        Dock = DockStyle.Fill,
        AutoGenerateColumns = false,
        AllowUserToAddRows = false,
        AllowUserToDeleteRows = false,
        AllowUserToResizeRows = false,
        ReadOnly = true,
        MultiSelect = false,
        SelectionMode = DataGridViewSelectionMode.FullRowSelect,
        RowHeadersVisible = false,
        BackgroundColor = Color.White,
        BorderStyle = BorderStyle.FixedSingle,
        ShowCellToolTips = false
    };

    private IReadOnlyList<MovementCustomerOption> customers = [];
    private MovementCustomerSummary? selectedCustomer;

    public SingleEntryView(IMovementService movements, UserSession session)
    {
        this.movements = movements;
        this.session = session;

        Dock = DockStyle.Fill;
        BackColor = Color.FromArgb(245, 247, 250);
        Font = new Font("Segoe UI", 10F);

        movementType.Items.Add(new DirectionOption("Returned (IN)", MovementType.In));
        movementType.Items.Add(new DirectionOption("Taken (OUT)", MovementType.Out));
        movementType.SelectedIndex = 0;

        ConfigureBalanceGrid();
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
            _ = SaveAsync();
            return true;
        }

        return base.ProcessCmdKey(ref msg, keyData);
    }

    private void ConfigureBalanceGrid()
    {
        balances.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Container",
            DataPropertyName = nameof(PositionRow.Container),
            AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
            MinimumWidth = 120
        });

        balances.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Current",
            DataPropertyName = nameof(PositionRow.Current),
            Width = 115
        });

        balances.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "After Save",
            DataPropertyName = nameof(PositionRow.After),
            Width = 130
        });

        balances.CellFormatting += (_, e) =>
        {
            if (e.RowIndex < 0 || e.ColumnIndex != 2)
                return;

            if (balances.Rows[e.RowIndex].DataBoundItem is not PositionRow row)
                return;

            var style = e.CellStyle ?? new DataGridViewCellStyle();
            e.CellStyle = style;

            style.ForeColor =
                row.AfterBalance < 0 ? Color.ForestGreen :
                row.AfterBalance > 0 ? Color.Firebrick :
                Color.DimGray;

            if (row.AfterBalance != row.CurrentBalance)
                style.Font = new Font(balances.Font, FontStyle.Bold);
        };
    }

    private void Build()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };

        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 66F));
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 34F));

        root.Controls.Add(BuildEntryPanel(), 0, 0);
        root.Controls.Add(BuildPositionPanel(), 1, 0);

        Controls.Add(root);
    }

    private Control BuildEntryPanel()
    {
        var panel = CardPanel();
        panel.Margin = new Padding(0, 0, 14, 0);

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = false,
            ColumnCount = 1,
            RowCount = 4,
            Padding = new Padding(22)
        };

        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

        layout.Controls.Add(new Label
        {
            Text = "Record one movement",
            AutoSize = true,
            Font = new Font("Segoe UI Semibold", 15F, FontStyle.Bold),
            Margin = new Padding(0, 0, 0, 16)
        }, 0, 0);

        var header = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true,
            Margin = new Padding(0, 0, 0, 12)
        };

        header.Controls.Add(LabelFor("Movement date"));
        header.Controls.Add(movementDate);
        header.Controls.Add(Spacer(18));
        header.Controls.Add(LabelFor("Direction"));
        header.Controls.Add(movementType);

        layout.Controls.Add(header, 0, 1);
        layout.Controls.Add(BuildFields(), 0, 2);

        var bottom = new Panel { Dock = DockStyle.Fill };

        var save = new Button
        {
            Text = "Save Movement",
            AutoSize = false,
            Size = new Size(165, 42),
            Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold),
            Anchor = AnchorStyles.Top | AnchorStyles.Left
        };
        save.Click += async (_, _) => await SaveAsync();

        var actions = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true
        };
        actions.Controls.Add(save);
        actions.Controls.Add(new Label
        {
            Text = "Ctrl+Enter",
            AutoSize = true,
            ForeColor = Color.DimGray,
            Margin = new Padding(12, 11, 0, 0)
        });
        status.Margin = new Padding(14, 11, 0, 0);
        actions.Controls.Add(status);

        bottom.Controls.Add(actions);
        layout.Controls.Add(bottom, 0, 3);

        panel.Controls.Add(layout);
        return panel;
    }

    private Control BuildFields()
    {
        var form = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            ColumnCount = 4,
            RowCount = 5,
            Margin = new Padding(0, 0, 0, 16)
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

        // Align the resolved-customer summary with the input fields rather
        // than with the left-hand labels.
        customerInfo.Margin = new Padding(8, 2, 0, 8);
        form.Controls.Add(customerInfo, 1, 3);
        form.SetColumnSpan(customerInfo, 3);

        form.Controls.Add(validation, 0, 4);
        form.SetColumnSpan(validation, 4);

        customerCode.Leave += async (_, _) => await ResolveCustomerAsync();
        customerCode.KeyDown += async (_, e) =>
        {
            if (e.KeyCode != Keys.Enter)
                return;

            e.SuppressKeyPress = true;
            await ResolveCustomerAsync();

            if (selectedCustomer is not null)
                containerType.Focus();
        };

        quantity.ValueChanged += (_, _) => RefreshPreview();
        movementType.SelectionChangeCommitted += (_, _) => RefreshPreview();
        containerType.SelectionChangeCommitted += (_, _) => RefreshPreview();

        quantity.KeyDown += async (_, e) =>
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                await SaveAsync();
            }
        };

        reference.KeyDown += async (_, e) =>
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                await SaveAsync();
            }
        };

        notes.KeyDown += async (_, e) =>
        {
            if (e.KeyCode == Keys.Enter && !e.Shift)
            {
                e.SuppressKeyPress = true;
                await SaveAsync();
            }
        };

        return form;
    }

    private Control BuildPositionPanel()
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
            Text = "'After Save' previews the selected movement before it is committed. CREDIT means more have been returned than recorded OUT.",
            AutoSize = true,
            ForeColor = Color.DimGray,
            MaximumSize = new Size(430, 0),
            Margin = new Padding(0, 12, 0, 8)
        }, 0, 2);

        layout.Controls.Add(new Label
        {
            Text = "Single Entry is intended for individual/ad-hoc movements. Use Batch Entry for high-volume daily entry.",
            AutoSize = true,
            ForeColor = Color.DimGray,
            MaximumSize = new Size(430, 0)
        }, 0, 3);

        panel.Controls.Add(layout);
        return panel;
    }

    private async Task InitialiseAsync()
    {
        try
        {
            customers = await movements.GetActiveCustomersAsync();
            var containers = await movements.GetActiveContainerTypesAsync();

            containerType.DataSource = containers.ToList();
            containerType.DisplayMember = nameof(MovementContainerOption.Name);
            containerType.ValueMember = nameof(MovementContainerOption.Id);

            var autocomplete = new AutoCompleteStringCollection();
            autocomplete.AddRange(customers.Select(x => x.Code).ToArray());

            customerCode.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
            customerCode.AutoCompleteSource = AutoCompleteSource.CustomSource;
            customerCode.AutoCompleteCustomSource = autocomplete;

            if (session.Role == UserRole.Viewer)
            {
                validation.Text = "Viewer accounts can review positions but cannot record movements.";
            }

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

        RefreshPreview();
        status.Text = string.Empty;
    }

    private void RefreshPreview()
    {
        var customer = selectedCustomer;
        if (customer is null)
        {
            balances.DataSource = null;
            return;
        }

        var selectedContainer =
            containerType.SelectedItem as MovementContainerOption;

        var direction =
            movementType.SelectedItem is DirectionOption type
                ? type.Value
                : MovementType.In;

        var enteredQuantity = quantity.Value > 0
            ? (int)quantity.Value
            : 0;

        balances.DataSource = customer.Balances
            .Select(balance =>
            {
                var after = balance.Balance;

                if (selectedContainer is not null &&
                    balance.ContainerTypeId == selectedContainer.Id &&
                    enteredQuantity > 0)
                {
                    after = MovementPositionMath.Apply(
                        balance.Balance,
                        direction,
                        enteredQuantity);
                }

                return new PositionRow(
                    balance.ContainerType,
                    balance.Balance,
                    after);
            })
            .ToList();
    }

    private async Task SaveAsync()
    {
        validation.Text = string.Empty;

        if (session.Role == UserRole.Viewer)
        {
            validation.Text = "Viewer accounts cannot record movements.";
            return;
        }

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

        if (movementType.SelectedItem is not DirectionOption selectedDirection)
        {
            validation.Text = "Select a movement direction.";
            movementType.Focus();
            return;
        }

        if (quantity.Value <= 0)
        {
            validation.Text = "Quantity is required and must be greater than zero.";
            quantity.Focus();
            return;
        }

        var date = DateOnly.FromDateTime(movementDate.Value.Date);
        var qty = (int)quantity.Value;

        var answer = MessageBox.Show(
            $"Save this movement?\n\n" +
            $"Customer: {selectedCustomer.Code} — {selectedCustomer.Name}\n" +
            $"Direction: {selectedDirection.Text}\n" +
            $"Container: {selectedContainer.Name}\n" +
            $"Quantity: {qty}\n" +
            $"Date: {date:dd/MM/yyyy}",
            "Save Movement",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);

        if (answer != DialogResult.Yes)
            return;

        try
        {
            Enabled = false;

            var result = await movements.SaveSingleAsync(
                new SaveSingleMovementRequest(
                    date,
                    selectedDirection.Value,
                    selectedCustomer.CustomerId,
                    selectedContainer.Id,
                    qty,
                    Clean(reference.Text),
                    Clean(notes.Text)));

            status.Text =
                $"Saved movement #{result.MovementId}. Position: {MovementPositionMath.Format(result.NewBalance)}";

            ResetEntryForm();
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

    /// <summary>
    /// Returns Single Entry to a clean state after a successful save.
    /// A completed movement should never leave values behind that could be
    /// accidentally reused for the next customer.
    /// </summary>
    private void ResetEntryForm()
    {
        selectedCustomer = null;

        movementDate.Value = DateTime.Today;
        movementType.SelectedIndex = 0;

        customerCode.Clear();
        customerInfo.Text = string.Empty;

        if (containerType.Items.Count > 0)
            containerType.SelectedIndex = 0;

        quantity.Value = 0;
        quantity.Text = string.Empty;

        reference.Clear();
        notes.Clear();

        validation.Text = string.Empty;
        balances.DataSource = null;

        customerCode.Focus();
    }

    private static void AddField(
        TableLayoutPanel form,
        int row,
        string leftLabel,
        Control leftControl,
        string rightLabel,
        Control rightControl)
    {
        form.Controls.Add(LabelFor(leftLabel), 0, row);
        form.Controls.Add(leftControl, 1, row);
        form.Controls.Add(LabelFor(rightLabel), 2, row);
        form.Controls.Add(rightControl, 3, row);

        leftControl.Margin = new Padding(8, 2, 20, 8);
        rightControl.Margin = new Padding(8, 2, 0, 8);
    }

    private static Label LabelFor(string text) => new()
    {
        Text = text,
        AutoSize = true,
        ForeColor = Color.FromArgb(70, 80, 96),
        Margin = new Padding(0, 7, 0, 0)
    };

    private static Label Spacer(int width) => new()
    {
        Width = width,
        Height = 1
    };

    private static Panel CardPanel() => new()
    {
        Dock = DockStyle.Fill,
        BackColor = Color.White,
        Margin = Padding.Empty
    };

    private static string? Clean(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private sealed record DirectionOption(string Text, MovementType Value)
    {
        public override string ToString() => Text;
    }

    private sealed record PositionRow(
        string Container,
        int CurrentBalance,
        int AfterBalance)
    {
        public string Current => MovementPositionMath.Format(CurrentBalance);
        public string After => MovementPositionMath.Format(AfterBalance);
    }
}
