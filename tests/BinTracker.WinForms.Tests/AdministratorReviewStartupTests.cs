using BinTracker.Core;
using BinTracker.WinForms;
using Xunit;

namespace BinTracker.WinForms.Tests;

public sealed class AdministratorReviewStartupTests
{
    [Fact]
    public async Task Administrator_startup_runs_review_work()
    {
        var calls = 0;

        await AdministratorReviewStartup.RunAsync(
            UserRole.Administrator,
            () =>
            {
                calls++;
                return Task.CompletedTask;
            });

        Assert.Equal(1, calls);
    }

    [Theory]
    [InlineData(UserRole.Operator)]
    [InlineData(UserRole.Viewer)]
    public async Task Non_administrator_startup_skips_review_work(UserRole role)
    {
        var calls = 0;

        await AdministratorReviewStartup.RunAsync(
            role,
            () =>
            {
                calls++;
                return Task.CompletedTask;
            });

        Assert.Equal(0, calls);
    }
}
