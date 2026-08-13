using Xunit;

namespace BinTracker.UnitTests;

public sealed class CustomerSearchTests
{
    [Theory]
    [InlineData("zahos", "ZAHOS", "Zahos")]
    [InlineData("big", "BIG", "Big")]
    [InlineData("SWAZ", "SWAZZY", "Swazzy")]
    public void Search_contract_is_case_insensitive_for_code_and_name(
        string search,
        string code,
        string name)
    {
        var term = search.Trim().ToUpperInvariant();

        Assert.True(
            code.ToUpperInvariant().Contains(term) ||
            name.ToUpperInvariant().Contains(term));
    }
}
