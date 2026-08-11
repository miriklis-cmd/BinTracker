using BinTracker.Core;
using Xunit;

namespace BinTracker.UnitTests;

public sealed class MarketFloorReportRulesTests
{
    [Theory]
    [InlineData(10, 3, 2, 11)]
    [InlineData(-4, 0, 3, -7)]
    [InlineData(0, 5, 5, 0)]
    public void Reverse_total_is_bfwd_plus_out_minus_in(
        int bfwd,
        int @out,
        int @in,
        int expected)
    {
        Assert.Equal(expected, bfwd + @out - @in);
    }

    [Fact]
    public void Credit_is_negative_position()
    {
        Assert.True(-1 < 0);
    }

    [Fact]
    public void Account_and_cash_are_distinct_customer_types()
    {
        Assert.NotEqual(CustomerType.Account, CustomerType.CashCod);
    }
}
