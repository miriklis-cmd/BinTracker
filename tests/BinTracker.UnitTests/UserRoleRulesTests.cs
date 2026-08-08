using BinTracker.Core;
using Xunit;

namespace BinTracker.UnitTests;

public sealed class UserRoleRulesTests
{
    [Fact]
    public void Viewer_can_be_promoted_to_operator()
    {
        var user = new UserAccount { Role = UserRole.Viewer };

        user.Role = UserRole.Operator;

        Assert.Equal(UserRole.Operator, user.Role);
    }

    [Fact]
    public void Operator_can_be_promoted_to_administrator()
    {
        var user = new UserAccount { Role = UserRole.Operator };

        user.Role = UserRole.Administrator;

        Assert.Equal(UserRole.Administrator, user.Role);
    }
}
