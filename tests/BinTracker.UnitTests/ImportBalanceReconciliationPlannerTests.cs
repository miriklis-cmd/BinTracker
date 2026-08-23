using BinTracker.Core;
using BinTracker.Services;
using Xunit;

namespace BinTracker.UnitTests;

public sealed class ImportBalanceReconciliationPlannerTests
{
    private static readonly ImportWorksheetMapping[] SourceMapping =
    [
        new("Update Account", ImportWorksheetRole.Source, "")
    ];

    private static readonly ContainerTypeListRow[] Containers =
    [
        new(1, "Blue Bin", "BLUE", 1, true, false, 0),
        new(3, "Yellow Bin", "YELLOW", 3, true, false, 0),
        new(4, "Bulk Bin", "BULK", 4, true, false, 0)
    ];

    [Fact]
    public void Existing_balance_is_reconciled_to_bfwd_then_daily_movements_are_preserved()
    {
        var analysis = Analysis(new ImportSnapshotCandidate(
            "Update Account", "Clamms", CustomerType.Account, "Y",
            Out: 5, In: 3, BroughtForward: 20, ExcelTotal: 22, SourceRow: "10"));

        var result = ImportBalanceReconciliationPlanner.Build(
            analysis,
            SourceMapping,
            CustomerPlan(7, "Clamms", ImportCustomerReviewStatus.Existing),
            Containers,
            [new BalanceRow(7, "Clamms", 3, "Yellow Bin", 12)]);

        var row = Assert.Single(result.Rows);
        Assert.Equal(12, row.CurrentBinTrackerBalance);
        Assert.Equal(20, row.ExcelBroughtForward);
        Assert.Equal(8, row.OpeningAdjustment);
        Assert.Equal(22, row.ProjectedBalance);
        Assert.Equal(22, row.ExcelTarget);
        Assert.Equal(ImportBalanceReconciliationStatus.Ready, row.Status);
    }

    [Fact]
    public void Fresh_database_uses_bfwd_as_opening_adjustment()
    {
        var analysis = Analysis(new ImportSnapshotCandidate(
            "Update Account", "Clamms", CustomerType.Account, "Bulk",
            Out: 4, In: 1, BroughtForward: 10, ExcelTotal: 13, SourceRow: "11"));

        var result = ImportBalanceReconciliationPlanner.Build(
            analysis,
            SourceMapping,
            CustomerPlan(
                existingId: null,
                code: "Clamms",
                status: ImportCustomerReviewStatus.New),
            Containers,
            [],
            null,
            new Dictionary<string, ImportCustomerDecision>(
                StringComparer.OrdinalIgnoreCase)
            {
                ["Clamms"] = new(
                    "Clamms",
                    "Clamms",
                    ImportCustomerDecisionAction.Create)
            });

        var row = Assert.Single(result.Rows);

        Assert.Equal(0, row.CurrentBinTrackerBalance);
        Assert.Equal(10, row.OpeningAdjustment);
        Assert.Equal(13, row.ProjectedBalance);
        Assert.Equal(
            ImportBalanceReconciliationStatus.Ready,
            row.Status);
    }

    [Fact]
    public void Existing_test_balance_is_not_added_to_excel_target()
    {
        var analysis = Analysis(new ImportSnapshotCandidate(
            "Update Account", "Clamms", CustomerType.Account, "Y",
            Out: 5, In: 3, BroughtForward: 20, ExcelTotal: 22, SourceRow: "10"));

        var result = ImportBalanceReconciliationPlanner.Build(
            analysis,
            SourceMapping,
            CustomerPlan(7, "Clamms", ImportCustomerReviewStatus.Existing),
            Containers,
            [new BalanceRow(7, "Clamms", 3, "Yellow Bin", 12)]);

        var row = Assert.Single(result.Rows);
        Assert.NotEqual(34, row.ProjectedBalance);
        Assert.Equal(22, row.ProjectedBalance);
    }

    [Fact]
    public void No_container_hint_defaults_to_blue_bin()
    {
        var analysis = Analysis(new ImportSnapshotCandidate(
            "Update Account", "Clamms", CustomerType.Account, null,
            Out: 1, In: 0, BroughtForward: 5, ExcelTotal: 6, SourceRow: "12"));

        var result = ImportBalanceReconciliationPlanner.Build(
            analysis,
            SourceMapping,
            CustomerPlan(7, "Clamms", ImportCustomerReviewStatus.Existing),
            Containers,
            []);

        var row = Assert.Single(result.Rows);
        Assert.Equal("Blue Bin", row.Container);
        Assert.Equal(ImportBalanceReconciliationStatus.Ready, row.Status);
        Assert.False(result.HasBlockingIssues);
    }

    [Fact]
    public void Unknown_explicit_container_token_blocks_reconciliation()
    {
        var analysis = Analysis(new ImportSnapshotCandidate(
            "Update Account", "Clamms", CustomerType.Account, "Tub",
            Out: 1, In: 0, BroughtForward: 5, ExcelTotal: 6, SourceRow: "13"));

        var result = ImportBalanceReconciliationPlanner.Build(
            analysis,
            SourceMapping,
            CustomerPlan(7, "Clamms", ImportCustomerReviewStatus.Existing),
            Containers,
            []);

        var row = Assert.Single(result.Rows);
        Assert.Equal("Tub", row.Container);
        Assert.Equal(
            ImportBalanceReconciliationStatus.UnresolvedContainer,
            row.Status);
        Assert.Contains("Unknown legacy container token", row.ContainerReason);
        Assert.True(result.HasBlockingIssues);
    }

    [Fact]
    public void Manual_container_mapping_unblocks_unknown_token()
    {
        var analysis = Analysis(new ImportSnapshotCandidate(
            "Update Account", "Clamms", CustomerType.Account, "Tub",
            Out: 1, In: 0, BroughtForward: 5, ExcelTotal: 6, SourceRow: "14"));

        var result = ImportBalanceReconciliationPlanner.Build(
            analysis,
            SourceMapping,
            CustomerPlan(7, "Clamms", ImportCustomerReviewStatus.Existing),
            Containers,
            [],
            new Dictionary<string,int>(StringComparer.OrdinalIgnoreCase) { ["Tub"] = 4 });

        var row = Assert.Single(result.Rows);
        Assert.Equal("Bulk Bin", row.Container);
        Assert.Equal("Tub", row.ContainerToken);
        Assert.Equal(ImportBalanceReconciliationStatus.Ready, row.Status);
        Assert.False(result.HasBlockingIssues);
    }

    [Fact]
    public void Unconfirmed_new_customer_blocks_reconciliation()
    {
        var analysis = Analysis(new ImportSnapshotCandidate("Update Account", "NEWCO", CustomerType.Account, null, 1, 0, 5, 6, "20"));
        var result = ImportBalanceReconciliationPlanner.Build(analysis, SourceMapping, CustomerPlan(null, "NEWCO", ImportCustomerReviewStatus.New), Containers, [], null,
            new Dictionary<string, ImportCustomerDecision>(StringComparer.OrdinalIgnoreCase) { ["NEWCO"] = new("NEWCO", "New Company", ImportCustomerDecisionAction.Unconfirmed) });
        Assert.Equal(ImportBalanceReconciliationStatus.NewCustomerPendingConfirmation, Assert.Single(result.Rows).Status);
        Assert.True(result.HasBlockingIssues);
    }

    [Fact]
    public void Skipped_new_customer_is_excluded_without_blocking()
    {
        var analysis = Analysis(new ImportSnapshotCandidate("Update Account", "NEWCO", CustomerType.Account, null, 1, 0, 5, 6, "21"));
        var result = ImportBalanceReconciliationPlanner.Build(analysis, SourceMapping, CustomerPlan(null, "NEWCO", ImportCustomerReviewStatus.New), Containers, [], null,
            new Dictionary<string, ImportCustomerDecision>(StringComparer.OrdinalIgnoreCase) { ["NEWCO"] = new("NEWCO", "New Company", ImportCustomerDecisionAction.Skip) });
        Assert.Empty(result.Rows); Assert.False(result.HasBlockingIssues);
    }

    [Fact]
    public void Create_decision_allows_new_customer_reconciliation()
    {
        var analysis = Analysis(new ImportSnapshotCandidate("Update Account", "NEWCO", CustomerType.Account, null, 1, 0, 5, 6, "22"));
        var result = ImportBalanceReconciliationPlanner.Build(analysis, SourceMapping, CustomerPlan(null, "NEWCO", ImportCustomerReviewStatus.New), Containers, [], null,
            new Dictionary<string, ImportCustomerDecision>(StringComparer.OrdinalIgnoreCase) { ["NEWCO"] = new("NEWCO", "New Company", ImportCustomerDecisionAction.Create) });
        var row = Assert.Single(result.Rows); Assert.Equal(ImportBalanceReconciliationStatus.Ready, row.Status); Assert.Equal("Blue Bin", row.Container);
    }

    [Fact]
    public void Missing_customer_decision_entry_blocks_new_customer()
    {
        var analysis = Analysis(new ImportSnapshotCandidate(
            "Update Account", "NEWCO", CustomerType.Account, null,
            Out: 1, In: 0, BroughtForward: 5, ExcelTotal: 6, SourceRow: "23"));

        var result = ImportBalanceReconciliationPlanner.Build(
            analysis,
            SourceMapping,
            CustomerPlan(null, "NEWCO", ImportCustomerReviewStatus.New),
            Containers,
            [],
            null,
            new Dictionary<string, ImportCustomerDecision>(
                StringComparer.OrdinalIgnoreCase));

        var row = Assert.Single(result.Rows);
        Assert.Equal(
            ImportBalanceReconciliationStatus.NewCustomerPendingConfirmation,
            row.Status);
    }

    [Fact]
    public void Fresh_database_without_create_decision_is_blocked()
    {
        var analysis = Analysis(new ImportSnapshotCandidate(
            "Update Account", "FRESHNEW", CustomerType.Account, null,
            Out: 2, In: 1, BroughtForward: 10, ExcelTotal: 11, SourceRow: "24"));

        var result = ImportBalanceReconciliationPlanner.Build(
            analysis,
            SourceMapping,
            CustomerPlan(null, "FRESHNEW", ImportCustomerReviewStatus.New),
            Containers,
            []);

        var row = Assert.Single(result.Rows);

        Assert.Equal(10, row.OpeningAdjustment);
        Assert.Equal(11, row.ProjectedBalance);
        Assert.Equal("Blue Bin", row.Container);
        Assert.Equal(
            ImportBalanceReconciliationStatus.NewCustomerPendingConfirmation,
            row.Status);
    }

    [Fact]
    public void Existing_customer_is_blocked_when_confirmation_dictionary_is_supplied_but_unconfirmed()
    {
        var analysis = Analysis(new ImportSnapshotCandidate(
            "Update Account", "Clamms", CustomerType.Account, null,
            Out: 1, In: 0, BroughtForward: 5, ExcelTotal: 6, SourceRow: "30"));

        var result = ImportBalanceReconciliationPlanner.Build(
            analysis,
            SourceMapping,
            CustomerPlan(7, "Clamms", ImportCustomerReviewStatus.Existing),
            Containers,
            [],
            null,
            null,
            new Dictionary<string, ImportExistingCustomerDecision>(
                StringComparer.OrdinalIgnoreCase));

        var row = Assert.Single(result.Rows);
        Assert.Equal("Blue Bin", row.Container);
        Assert.Equal(5, row.OpeningAdjustment);
        Assert.Equal(6, row.ProjectedBalance);
        Assert.Equal(
            ImportBalanceReconciliationStatus.ExistingCustomerPendingConfirmation,
            row.Status);
    }

    [Fact]
    public void Pending_clamms_rows_still_show_blue_bulk_yellow_and_preview_math()
    {
        var analysis = Analysis(
            new ImportSnapshotCandidate(
                "Update Account", "Clamms", CustomerType.Account, null,
                Out: 0, In: 0, BroughtForward: 5, ExcelTotal: 5, SourceRow: "50"),
            new ImportSnapshotCandidate(
                "Update Account", "Clamms", CustomerType.Account, "Bulk",
                Out: 2, In: 1, BroughtForward: 20, ExcelTotal: 21, SourceRow: "51"),
            new ImportSnapshotCandidate(
                "Update Account", "Clamms", CustomerType.Account, "Y",
                Out: 1, In: 3, BroughtForward: 8, ExcelTotal: 6, SourceRow: "52"));

        var result = ImportBalanceReconciliationPlanner.Build(
            analysis,
            SourceMapping,
            CustomerPlan(7, "Clamms", ImportCustomerReviewStatus.Existing),
            Containers,
            [
                new BalanceRow(7, "Clamms Seafood", 1, "Blue Bin", 2),
                new BalanceRow(7, "Clamms Seafood", 4, "Bulk Bin", 11),
                new BalanceRow(7, "Clamms Seafood", 3, "Yellow Bin", 4)
            ],
            null,
            null,
            new Dictionary<string, ImportExistingCustomerDecision>(
                StringComparer.OrdinalIgnoreCase));

        Assert.Equal(3, result.Rows.Count);

        var blue = Assert.Single(result.Rows, x => x.Container == "Blue Bin");
        Assert.Equal(3, blue.OpeningAdjustment);
        Assert.Equal(5, blue.ProjectedBalance);
        Assert.Contains("defaulted to standard Blue Bin", blue.ContainerReason);

        var bulk = Assert.Single(result.Rows, x => x.Container == "Bulk Bin");
        Assert.Equal(9, bulk.OpeningAdjustment);
        Assert.Equal(21, bulk.ProjectedBalance);
        Assert.Equal("Bulk", bulk.ContainerToken);

        var yellow = Assert.Single(result.Rows, x => x.Container == "Yellow Bin");
        Assert.Equal(4, yellow.OpeningAdjustment);
        Assert.Equal(6, yellow.ProjectedBalance);
        Assert.Equal("Y", yellow.ContainerToken);

        Assert.All(
            result.Rows,
            row => Assert.Equal(
                ImportBalanceReconciliationStatus.ExistingCustomerPendingConfirmation,
                row.Status));
    }

    [Fact]
    public void Confirmed_existing_customer_can_reconcile()
    {
        var analysis = Analysis(new ImportSnapshotCandidate(
            "Update Account", "Clamms", CustomerType.Account, null,
            Out: 1, In: 0, BroughtForward: 5, ExcelTotal: 6, SourceRow: "31"));

        var result = ImportBalanceReconciliationPlanner.Build(
            analysis,
            SourceMapping,
            CustomerPlan(7, "Clamms", ImportCustomerReviewStatus.Existing),
            Containers,
            [],
            null,
            null,
            new Dictionary<string, ImportExistingCustomerDecision>(
                StringComparer.OrdinalIgnoreCase)
            {
                ["Clamms"] = new(
                    "Clamms",
                    ImportExistingCustomerDecisionAction.AcceptMatch,
                    7,
                    "CLAMMS",
                    "Clamms Seafood")
            });

        var row = Assert.Single(result.Rows);
        Assert.Equal(ImportBalanceReconciliationStatus.Ready, row.Status);
    }

    [Fact]
    public void Cutover_math_matches_Zahos_example_bfwd_plus_out_minus_in()
    {
        var analysis = Analysis(new ImportSnapshotCandidate(
            "Update Account", "Zahos", CustomerType.Account, null,
            Out: 10, In: 15, BroughtForward: 5, ExcelTotal: 0, SourceRow: "40"));

        var result = ImportBalanceReconciliationPlanner.Build(
            analysis,
            SourceMapping,
            CustomerPlan(null, "Zahos", ImportCustomerReviewStatus.New),
            Containers,
            [],
            null,
            new Dictionary<string, ImportCustomerDecision>(
                StringComparer.OrdinalIgnoreCase)
            {
                ["Zahos"] = new(
                    "Zahos",
                    "Zahos",
                    ImportCustomerDecisionAction.Create)
            });

        var row = Assert.Single(result.Rows);
        Assert.Equal(0, row.CurrentBinTrackerBalance);
        Assert.Equal(5, row.ExcelBroughtForward);
        Assert.Equal(10, row.ExcelOut);
        Assert.Equal(15, row.ExcelIn);
        Assert.Equal(5, row.OpeningAdjustment);
        Assert.Equal(0, row.ProjectedBalance);
        Assert.Equal(0, row.ExcelTarget);
        Assert.Equal(ImportBalanceReconciliationStatus.Ready, row.Status);
    }

    [Fact]
    public void Existing_balance_is_adjusted_to_bfwd_before_daily_movements()
    {
        var analysis = Analysis(new ImportSnapshotCandidate(
            "Update Account", "Clamms", CustomerType.Account, null,
            Out: 4, In: 1, BroughtForward: 12, ExcelTotal: 15, SourceRow: "41"));

        var result = ImportBalanceReconciliationPlanner.Build(
            analysis,
            SourceMapping,
            CustomerPlan(7, "Clamms", ImportCustomerReviewStatus.Existing),
            Containers,
            [new BalanceRow(7, "Clamms Seafood", 1, "Blue Bin", 20)],
            null,
            null,
            new Dictionary<string, ImportExistingCustomerDecision>(
                StringComparer.OrdinalIgnoreCase)
            {
                ["Clamms"] = new(
                    "Clamms",
                    ImportExistingCustomerDecisionAction.AcceptMatch,
                    7,
                    "CLAMMS",
                    "Clamms Seafood")
            });

        var row = Assert.Single(result.Rows);
        Assert.Equal(20, row.CurrentBinTrackerBalance);
        Assert.Equal(-8, row.OpeningAdjustment);
        Assert.Equal(15, row.ProjectedBalance);
        Assert.Equal(15, row.ExcelTarget);
        Assert.Equal(ImportBalanceReconciliationStatus.Ready, row.Status);
    }

    [Fact]
    public void Opening_adjustment_can_be_positive_or_negative_without_changing_daily_out_in()
    {
        // Current 3 -> B/Fwd 8 requires +5. Then OUT 2 and IN 6 produce target 4.
        var analysis = Analysis(new ImportSnapshotCandidate(
            "Update Account", "Clamms", CustomerType.Account, null,
            Out: 2, In: 6, BroughtForward: 8, ExcelTotal: 4, SourceRow: "42"));

        var result = ImportBalanceReconciliationPlanner.Build(
            analysis,
            SourceMapping,
            CustomerPlan(7, "Clamms", ImportCustomerReviewStatus.Existing),
            Containers,
            [new BalanceRow(7, "Clamms Seafood", 1, "Blue Bin", 3)],
            null,
            null,
            new Dictionary<string, ImportExistingCustomerDecision>(
                StringComparer.OrdinalIgnoreCase)
            {
                ["Clamms"] = new(
                    "Clamms",
                    ImportExistingCustomerDecisionAction.AcceptMatch,
                    7,
                    "CLAMMS",
                    "Clamms Seafood")
            });

        var row = Assert.Single(result.Rows);
        Assert.Equal(5, row.OpeningAdjustment);
        Assert.Equal(2, row.ExcelOut);
        Assert.Equal(6, row.ExcelIn);
        Assert.Equal(4, row.ProjectedBalance);
    }

    private static ExcelImportAnalysis Analysis(
        params ImportSnapshotCandidate[] snapshots) =>
        new(
            new ImportSourceDocument("test.xlsx", [], "test.xlsx"),
            [],
            snapshots.Select(x =>
                new ImportCustomerCandidate(
                    x.Worksheet,
                    x.CustomerCode,
                    x.CustomerType,
                    $"A{x.SourceRow}")).ToArray(),
            snapshots,
            []);

    private static ImportReviewPlan CustomerPlan(
        int? existingId,
        string code,
        ImportCustomerReviewStatus status) =>
        new(
            [
                new ImportCustomerReviewRow(
                    code,
                    CustomerType.Account,
                    "Update Account",
                    "",
                    "",
                    status,
                    existingId,
                    existingId.HasValue ? code : "",
                    existingId.HasValue ? CustomerType.Account : null,
                    existingId.HasValue ? CustomerMatchKind.ExactCode : CustomerMatchKind.None,
                    existingId.HasValue ? "Exact customer-code match" : "No match")
            ],
            1,
            1,
            1,
            0);
}
