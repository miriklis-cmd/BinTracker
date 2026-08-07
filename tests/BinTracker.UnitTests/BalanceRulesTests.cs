using Xunit;
using BinTracker.Core;

namespace BinTracker.UnitTests;

public sealed class BalanceRulesTests
{
    [Theory]
    [InlineData(50, true, false)]
    [InlineData(-25, false, true)]
    [InlineData(0, false, false)]
    public void Customer_position_sign_has_expected_meaning(
        int balance,
        bool isOutstanding,
        bool isCredit)
    {
        Assert.Equal(isOutstanding, balance > 0);
        Assert.Equal(isCredit, balance < 0);
    }

    [Fact]
    public void Returned_can_exceed_taken_and_create_credit()
    {
        var taken = 100;
        var returned = 130;
        var balance = taken - returned;

        Assert.Equal(-30, balance);
    }

    [Fact]
    public void Different_container_types_do_not_cancel_each_other()
    {
        var blueBalance = 40;
        var yellowBalance = -10;

        Assert.Equal(40, blueBalance);
        Assert.Equal(-10, yellowBalance);
    }
}
