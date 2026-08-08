using BinTracker.Core;
using Xunit;

namespace BinTracker.UnitTests;

public sealed class AccountStatusRulesTests
{
    [Fact]
    public void Locked_and_active_are_distinct_states()
    {
        var user = new UserAccount { IsActive = true, IsLocked = true };

        Assert.True(user.IsActive);
        Assert.True(user.IsLocked);
    }

    [Fact]
    public void Unlocking_does_not_reactivate_an_inactive_user()
    {
        var user = new UserAccount { IsActive = false, IsLocked = true };

        user.IsLocked = false;
        user.LockedUtc = null;
        user.FailedLoginCount = 0;

        Assert.False(user.IsActive);
        Assert.False(user.IsLocked);
    }
}
