using BinTracker.Core;
using BinTracker.Services;
using Xunit;

namespace BinTracker.UnitTests;

public sealed class ExcelImportReviewPlannerTests
{
    [Fact]
    public void Review_uses_only_source_sheets_and_matches_codes_case_insensitively()
    {
        var analysis = Analysis(
            new ImportCustomerCandidate("Update Account", "ALBURY", CustomerType.Account, "A2"),
            new ImportCustomerCandidate("CREDITS", "ALBURY", CustomerType.Account, "A2"),
            new ImportCustomerCandidate("Update Account", "NEWCO", CustomerType.Account, "A3"));

        var mappings = new[]
        {
            new ImportWorksheetMapping("Update Account", ImportWorksheetRole.Source, ""),
            new ImportWorksheetMapping("CREDITS", ImportWorksheetRole.Validation, "")
        };

        var existing = new[]
        {
            new CustomerListRow(7, "Borella Seafood", "albury", CustomerType.Account, true, 0)
        };

        var plan = ExcelImportReviewPlanner.Build(analysis, mappings, existing);

        Assert.Equal(2, plan.UniqueCustomerCount);
        Assert.Equal(1, plan.ExistingCount);
        Assert.Equal(1, plan.NewCount);
        Assert.Equal(2, plan.SourceOccurrenceCount);
    }

    [Fact]
    public void Review_flags_existing_customer_type_mismatch()
    {
        var analysis = Analysis(
            new ImportCustomerCandidate("Update Cash", "CUSTOMER1", CustomerType.CashCod, "A2"));

        var plan = ExcelImportReviewPlanner.Build(
            analysis,
            [new ImportWorksheetMapping("Update Cash", ImportWorksheetRole.Source, "")],
            [new CustomerListRow(1, "Customer One", "CUSTOMER1", CustomerType.Account, true, 0)]);

        Assert.Equal(1, plan.TypeMismatchCount);
        Assert.True(plan.HasBlockingCustomerConflicts);
    }

    [Fact]
    public void Review_flags_same_code_with_conflicting_source_types()
    {
        var analysis = Analysis(
            new ImportCustomerCandidate("Update Account", "SAME", CustomerType.Account, "A2"),
            new ImportCustomerCandidate("Update Cash", "SAME", CustomerType.CashCod, "A2"));

        var plan = ExcelImportReviewPlanner.Build(
            analysis,
            [
                new ImportWorksheetMapping("Update Account", ImportWorksheetRole.Source, ""),
                new ImportWorksheetMapping("Update Cash", ImportWorksheetRole.Source, "")
            ],
            []);

        Assert.Equal(1, plan.SourceConflictCount);
    }

    [Fact]
    public void Review_treats_bulk_prefix_as_container_hint_for_same_customer()
    {
        var analysis = Analysis(
            new ImportCustomerCandidate("Update Account", "Clamms", CustomerType.Account, "A2"),
            new ImportCustomerCandidate("Update Account", "(Bulk) Clamms", CustomerType.Account, "A3"));

        var plan = ExcelImportReviewPlanner.Build(
            analysis,
            [new ImportWorksheetMapping("Update Account", ImportWorksheetRole.Source, "")],
            [new CustomerListRow(1, "Clamms Seafood", "CLAMMS", CustomerType.Account, true, 0)]);

        Assert.Single(plan.Customers);
        Assert.Equal(1, plan.ExistingCount);

        var row = Assert.Single(plan.Customers);
        Assert.Equal("Clamms", row.CustomerCode, ignoreCase: true);
        Assert.Equal("Bulk", row.ContainerHints);
        Assert.Contains("(Bulk) Clamms", row.LegacyVariants);
        Assert.Equal(ImportCustomerReviewStatus.Existing, row.Status);
    }

    [Fact]
    public void Review_treats_y_prefix_as_container_hint_for_same_customer()
    {
        var analysis = Analysis(
            new ImportCustomerCandidate("Update Account", "(Y) Barwon", CustomerType.Account, "A2"));

        var plan = ExcelImportReviewPlanner.Build(
            analysis,
            [new ImportWorksheetMapping("Update Account", ImportWorksheetRole.Source, "")],
            [new CustomerListRow(2, "Barwon", "BARWON", CustomerType.Account, true, 0)]);

        var row = Assert.Single(plan.Customers);
        Assert.Equal("Barwon", row.CustomerCode, ignoreCase: true);
        Assert.Equal("Y", row.ContainerHints);
        Assert.Equal(ImportCustomerReviewStatus.Existing, row.Status);
    }

    [Fact]
    public void Review_merges_S_ampersand_J_spacing_variants()
    {
        var analysis = Analysis(
            new ImportCustomerCandidate("Update Account", "S & J", CustomerType.Account, "A2"),
            new ImportCustomerCandidate("Update Account", "(Bulk) S&J", CustomerType.Account, "A3"));

        var plan = ExcelImportReviewPlanner.Build(
            analysis,
            [new ImportWorksheetMapping("Update Account", ImportWorksheetRole.Source, "")],
            [new CustomerListRow(5, "S & J Seafood", "S & J", CustomerType.Account, true, 0)]);

        var row = Assert.Single(plan.Customers);

        Assert.Equal(ImportCustomerReviewStatus.Existing, row.Status);
        Assert.Equal("S & J", row.CustomerCode);
        Assert.Equal(5, row.ExistingCustomerId);
        Assert.Equal("Bulk", row.ContainerHints);
        Assert.Contains("(Bulk) S&J", row.LegacyVariants);
        Assert.Equal(1, plan.ExistingCount);
        Assert.Equal(0, plan.NewCount);
    }

    [Fact]
    public void Review_groups_spacing_and_punctuation_variants_before_matching()
    {
        var analysis = Analysis(
            new ImportCustomerCandidate("Update Account", "S & J", CustomerType.Account, "A2"),
            new ImportCustomerCandidate("Update Account", "S&J", CustomerType.Account, "A3"),
            new ImportCustomerCandidate("Update Account", "S  &  J", CustomerType.Account, "A4"));

        var plan = ExcelImportReviewPlanner.Build(
            analysis,
            [new ImportWorksheetMapping("Update Account", ImportWorksheetRole.Source, "")],
            [new CustomerListRow(5, "S & J Seafood", "S & J", CustomerType.Account, true, 0)]);

        var row = Assert.Single(plan.Customers);

        Assert.Equal("S & J", row.CustomerCode);
        Assert.Equal(ImportCustomerReviewStatus.Existing, row.Status);
        Assert.Equal(5, row.ExistingCustomerId);
        Assert.Contains("S&J", row.LegacyVariants);
        Assert.Contains("S  &  J", row.LegacyVariants);
    }

    private static ExcelImportAnalysis Analysis(
        params ImportCustomerCandidate[] customers) =>
        new(
            "test.xlsx",
            "test.xlsx",
            [],
            customers,
            [],
            []);
}
