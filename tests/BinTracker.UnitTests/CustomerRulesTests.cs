using Xunit;
namespace BinTracker.UnitTests;

public sealed class CustomerRulesTests
{
    [Theory]
    [InlineData("albury", "ALBURY")]
    [InlineData(" Albury ", "ALBURY")]
    [InlineData("JMPL", "JMPL")]
    public void Customer_code_normalises_to_uppercase(string input, string expected)
    {
        var actual = input.Trim().ToUpperInvariant();

        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData("Albury", "ALBURY")]
    [InlineData("albury", "ALBURY")]
    [InlineData(" ALBURY ", "ALBURY")]
    public void Customer_codes_are_case_insensitive(string existing, string proposed)
    {
        Assert.Equal(
            existing.Trim().ToUpperInvariant(),
            proposed.Trim().ToUpperInvariant());
    }
}
