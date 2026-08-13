
using BinTracker.Services;
using Xunit;

namespace BinTracker.UnitTests;

public sealed class LegacyBuyerParserTests
{
    [Theory]
    [InlineData("(Bulk) Clamms", "Clamms", "Bulk")]
    [InlineData("(Y) Barwon", "Barwon", "Y")]
    [InlineData("(CHEP) Customer", "Customer", "CHEP")]
    public void Leading_parenthesised_token_is_container_hint(
        string raw,
        string expectedCustomer,
        string expectedHint)
    {
        var parsed = LegacyBuyerParser.Parse(raw);

        Assert.Equal(expectedCustomer, parsed.CustomerCode);
        Assert.Equal(expectedHint, parsed.ContainerHint);
        Assert.Equal(raw, parsed.RawValue);
    }

    [Fact]
    public void Ordinary_customer_name_is_unchanged()
    {
        var parsed = LegacyBuyerParser.Parse("Clamms");

        Assert.Equal("Clamms", parsed.CustomerCode);
        Assert.Null(parsed.ContainerHint);
    }
}
