using BinTracker.Services;
using Xunit;

namespace BinTracker.UnitTests;

public sealed class PasswordPolicyTests
{
    [Theory]
    [InlineData("short1A")]
    [InlineData("alllowercase1")]
    [InlineData("ALLUPPERCASE1")]
    [InlineData("NoNumberHere")]
    public void Invalid_passwords_are_rejected(string password)
    {
        Assert.Throws<ArgumentException>(() => PasswordPolicy.Validate(password));
    }

    [Theory]
    [InlineData("GoodPassword1")]
    [InlineData("LongerPassword2026")]
    public void Valid_passwords_are_accepted(string password)
    {
        PasswordPolicy.Validate(password);
    }

    [Fact]
    public void Strength_feedback_is_available()
    {
        Assert.Equal("Strong", PasswordPolicy.StrengthText("LongerPassword2026!"));
    }
}
