using BinTracker.Core;
using BinTracker.Data;
using BinTracker.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BinTracker.IntegrationTests;

public sealed class UserAdministrationBoundaryTests
{
    [Fact]
    public void Public_administration_list_dto_exposes_only_presentation_fields()
    {
        var properties = typeof(UserAdministrationUser)
            .GetProperties()
            .Select(property => property.Name)
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(
            new[]
            {
                "DisplayName", "Id", "IsActive", "IsLocked", "LastLoginUtc",
                "MustChangePassword", "Role", "Username"
            },
            properties);
        Assert.DoesNotContain("PasswordHash", properties);
        Assert.DoesNotContain("PasswordSalt", properties);
        Assert.DoesNotContain("FailedLoginCount", properties);
    }

    [Fact]
    public async Task Administrator_listing_and_stable_id_mutations_preserve_user_management_behavior()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var services = new ServiceCollection();
        services.AddDbContextFactory<BinTrackerDbContext>(options => options.UseSqlite(connection));
        services.AddBinTrackerServices();

        await using var provider = services.BuildServiceProvider();
        await using var scope = provider.CreateAsyncScope();
        var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<BinTrackerDbContext>>();

        UserAccount administrator;
        await using (var db = await factory.CreateDbContextAsync())
        {
            await db.Database.EnsureCreatedAsync();
            await DatabaseSetup.InitializeSqliteAsync(db);

            administrator = User("z-admin", "Zed Admin", UserRole.Administrator);
            db.UserAccounts.AddRange(
                administrator,
                User("alpha", "Alpha User", UserRole.Viewer,
                    active: false, locked: true, mustChangePassword: true,
                    lastLoginUtc: new DateTime(2026, 10, 1, 2, 3, 4, DateTimeKind.Utc)));
            await db.SaveChangesAsync();
        }

        scope.ServiceProvider.GetRequiredService<UserSession>().SignIn(administrator);
        var users = scope.ServiceProvider.GetRequiredService<IUserService>();

        var initial = await users.GetUsersAsync();
        Assert.Equal(["alpha", "z-admin"], initial.Select(user => user.Username));
        var alpha = initial[0];
        Assert.Equal("Alpha User", alpha.DisplayName);
        Assert.Equal(UserRole.Viewer, alpha.Role);
        Assert.False(alpha.IsActive);
        Assert.True(alpha.IsLocked);
        Assert.True(alpha.MustChangePassword);
        Assert.Equal(new DateTime(2026, 10, 1, 2, 3, 4, DateTimeKind.Utc), alpha.LastLoginUtc);

        await users.SetActiveAsync(alpha.Id, true);
        await users.SetLockedAsync(alpha.Id, false);
        await users.SetRoleAsync(alpha.Id, UserRole.Operator);
        await users.ResetPasswordAsync(alpha.Id, "Temporary!42");

        var updated = Assert.Single(
            await users.GetUsersAsync(), user => user.Id == alpha.Id);
        Assert.True(updated.IsActive);
        Assert.False(updated.IsLocked);
        Assert.Equal(UserRole.Operator, updated.Role);
        Assert.True(updated.MustChangePassword);

        await users.CreateUserAsync(
            "bravo", "Bravo User", "Temporary!43", UserRole.Viewer);
        var created = Assert.Single(
            await users.GetUsersAsync(), user => user.Username == "bravo");
        Assert.True(created.Id > 0);
        Assert.True(created.IsActive);
        Assert.False(created.IsLocked);
        Assert.True(created.MustChangePassword);
    }

    [Theory]
    [InlineData(UserRole.Operator, true)]
    [InlineData(UserRole.Viewer, true)]
    [InlineData(UserRole.Administrator, false)]
    public async Task Non_administrators_and_anonymous_callers_cannot_list_users(
        UserRole role,
        bool authenticated)
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var services = new ServiceCollection();
        services.AddDbContextFactory<BinTrackerDbContext>(options => options.UseSqlite(connection));
        services.AddBinTrackerServices();

        await using var provider = services.BuildServiceProvider();
        await using var scope = provider.CreateAsyncScope();
        var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<BinTrackerDbContext>>();

        var user = User("limited", "Limited User", role);
        await using (var db = await factory.CreateDbContextAsync())
        {
            await db.Database.EnsureCreatedAsync();
            await DatabaseSetup.InitializeSqliteAsync(db);
            db.UserAccounts.Add(user);
            await db.SaveChangesAsync();
        }

        if (authenticated)
            scope.ServiceProvider.GetRequiredService<UserSession>().SignIn(user);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            scope.ServiceProvider.GetRequiredService<IUserService>().GetUsersAsync());
    }

    private static UserAccount User(
        string username,
        string displayName,
        UserRole role,
        bool active = true,
        bool locked = false,
        bool mustChangePassword = false,
        DateTime? lastLoginUtc = null) =>
        new()
        {
            Username = username,
            DisplayName = displayName,
            PasswordHash = $"hash-{username}",
            PasswordSalt = $"salt-{username}",
            Role = role,
            IsActive = active,
            IsLocked = locked,
            MustChangePassword = mustChangePassword,
            LastLoginUtc = lastLoginUtc,
            CreatedUtc = new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc)
        };
}
