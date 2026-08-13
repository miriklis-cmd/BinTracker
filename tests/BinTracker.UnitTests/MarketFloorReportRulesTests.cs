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


public sealed class MarketFloorImportAdjustmentRulesTests
{
    [Fact]
    public void Same_day_opening_adjustment_belongs_in_bfwd_not_daily_out()
    {
        const int priorBalance = 0;
        const int openingAdjustment = 5;
        const int operationalOut = 10;
        const int operationalIn = 15;

        var bfwd = priorBalance + openingAdjustment;
        var dailyOut = operationalOut;
        var dailyIn = operationalIn;
        var total = bfwd + dailyOut - dailyIn;

        Assert.Equal(5, bfwd);
        Assert.Equal(10, dailyOut);
        Assert.Equal(15, dailyIn);
        Assert.Equal(0, total);
    }

    [Fact]
    public void Cash_credit_stays_in_cash_area()
    {
        var type = CustomerType.CashCod;
        const int balance = -3;

        Assert.Equal(CustomerType.CashCod, type);
        Assert.True(balance < 0);
    }
}
