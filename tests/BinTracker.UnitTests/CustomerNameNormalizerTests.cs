using BinTracker.Core;
using BinTracker.Services;
using Xunit;

namespace BinTracker.UnitTests;

public sealed class CustomerNameNormalizerTests
{
    [Theory]
    [InlineData("S & J", "SJ")]
    [InlineData("S&J", "SJ")]
    [InlineData("S  &  J", "SJ")]
    [InlineData("A.E.G.I.R", "AEGIR")]
    [InlineData("Fish N' Grill", "FISHNGRILL")]
    [InlineData("D V Fresh", "DVFRESH")]
    [InlineData("d-v fresh", "DVFRESH")]
    public void Comparison_key_ignores_case_spacing_and_punctuation(
        string input,
        string expected)
    {
        Assert.Equal(expected, CustomerNameNormalizer.ComparisonKey(input));
    }

    [Fact]
    public void S_ampersand_J_matches_S_and_J_existing_code()
    {
        var existing = new[]
        {
            new CustomerListRow(
                10,
                "S & J Seafood",
                "S & J",
                CustomerType.Account,
                true,
                0)
        };

        var match = CustomerNameNormalizer.FindBestMatch("S&J", existing);

        Assert.True(match.IsMatch);
        Assert.Equal(10, match.Customer!.Id);
        Assert.Equal(CustomerMatchKind.NormalizedCode, match.Kind);
    }

    [Fact]
    public void Ambiguous_normalized_names_do_not_auto_match()
    {
        var existing = new[]
        {
            new CustomerListRow(1, "A & B", "A1", CustomerType.Account, true, 0),
            new CustomerListRow(2, "A&B", "A2", CustomerType.Account, true, 0)
        };

        var match = CustomerNameNormalizer.FindBestMatch("A & B", existing);

        // Imported code doesn't equal A1/A2, and two names normalize to AB.
        Assert.False(match.IsMatch);
    }
}
