using BinTracker.Core;
using BinTracker.Services;
using Microsoft.Extensions.DependencyInjection;

namespace BinTracker.IntegrationTests;

internal static class TestAuthenticatedUserContextRegistration
{
    public static IServiceCollection AddTestAuthenticatedUserContext(
        this IServiceCollection services,
        UserRole role = UserRole.Viewer)
    {
        services.AddScoped<IUserContext>(_ => new TestAuthenticatedUserContext(role));
        services.AddScoped<ICurrentAccountAuthorization>(
            _ => new TestCurrentAccountAuthorization(role));
        return services;
    }

    private sealed class TestAuthenticatedUserContext(UserRole role) : IUserContext
    {
        public string SessionId => "test-request-session";
        public int? UserId => 1;
        public string Username => "report-reader";
        public string DisplayName => "Report Reader";
        public UserRole Role => role;
        public bool MustChangePassword => false;
        public bool IsAuthenticated => true;
    }

    private sealed class TestCurrentAccountAuthorization(UserRole role) :
        ICurrentAccountAuthorization
    {
        private static AuthorizedUser User(UserRole currentRole) =>
            new(1, "report-reader", "Report Reader", currentRole);

        public Task<AuthorizedUser> RequireAuthenticatedAsync(
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(User(role));
        }

        public Task<AuthorizedUser> RequireCredentialChangeAsync(
            CancellationToken cancellationToken = default) =>
            RequireAuthenticatedAsync(cancellationToken);

        public async Task<AuthorizedUser> RequireAnyRoleAsync(
            IReadOnlyCollection<UserRole> roles,
            CancellationToken cancellationToken = default)
        {
            var user = await RequireAuthenticatedAsync(cancellationToken);
            if (!roles.Contains(user.Role))
                throw new UnauthorizedAccessException("The requested role is required.");
            return user;
        }
    }
}
