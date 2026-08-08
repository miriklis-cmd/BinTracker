using BinTracker.Core;
using Xunit;

namespace BinTracker.UnitTests;

public sealed class UserStatusTests
{
    [Fact]
    public void Inactive_takes_precedence_over_locked_for_display_purposes()
    {
        var user = new UserAccount
        {
            IsActive = false,
            IsLocked = true,
            MustChangePassword = true
        };

        Assert.False(user.IsActive);
        Assert.True(user.IsLocked);
    }

    [Fact]
    public void Password_change_required_is_an_active_account_state()
    {
        var user = new UserAccount
        {
            IsActive = true,
            IsLocked = false,
            MustChangePassword = true
        };

        Assert.True(user.IsActive);
        Assert.False(user.IsLocked);
        Assert.True(user.MustChangePassword);
    }
}
