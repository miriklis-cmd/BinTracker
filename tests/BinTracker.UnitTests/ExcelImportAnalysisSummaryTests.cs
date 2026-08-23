using BinTracker.Core;
using BinTracker.Services;
using Xunit;

namespace BinTracker.UnitTests;

public sealed class ExcelImportAnalysisSummaryTests
{
    [Fact]
    public void Unique_customer_count_is_case_insensitive()
    {
        var analysis = new ExcelImportAnalysis(
            new ImportSourceDocument(
                "test.xlsx",
                new byte[] { 1 },
                "test.xlsx"),
            [],
            [
                new ImportCustomerCandidate("A", "ALBURY", CustomerType.Account, "A2"),
                new ImportCustomerCandidate("B", "albury", CustomerType.Account, "B2"),
                new ImportCustomerCandidate("C", "FISH PIER", CustomerType.Account, "C2")
            ],
            [],
            []);

        Assert.Equal(3, analysis.CustomerCandidateCount);
        Assert.Equal(2, analysis.UniqueCustomerCount);
    }
}
