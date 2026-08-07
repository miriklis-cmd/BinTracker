using Xunit;
using BinTracker.Core;

namespace BinTracker.UnitTests;

public sealed class CustomerTypeTests
{
    [Fact]
    public void Existing_customers_default_to_account()
    {
        var customer = new Customer();

        Assert.Equal(CustomerType.Account, customer.CustomerType);
    }

    [Fact]
    public void Cash_cod_is_distinct_from_account()
    {
        Assert.NotEqual(CustomerType.Account, CustomerType.CashCod);
    }
}
