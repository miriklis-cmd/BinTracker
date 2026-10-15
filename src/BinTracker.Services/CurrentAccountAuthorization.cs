using BinTracker.Core;
using BinTracker.Data;
using Microsoft.EntityFrameworkCore;

namespace BinTracker.Services;

public sealed record AuthorizedUser(
    int UserId,
    string Username,
    string DisplayName,
    UserRole Role);

/// <summary>
/// Revalidates the current desktop account at a Services workflow boundary.
/// A future API supplies an equivalent request-scoped implementation/context.
/// </summary>
public interface ICurrentAccountAuthorization
{
    Task<AuthorizedUser> RequireAuthenticatedAsync(
        CancellationToken cancellationToken = default);

    Task<AuthorizedUser> RequireCredentialChangeAsync(
        CancellationToken cancellationToken = default);

    Task<AuthorizedUser> RequireAnyRoleAsync(
        IReadOnlyCollection<UserRole> roles,
        CancellationToken cancellationToken = default);
}

internal sealed class CurrentAccountAuthorization(
    IDbContextFactory<BinTrackerDbContext> factory,
    IUserContext userContext) : ICurrentAccountAuthorization
{
    public async Task<AuthorizedUser> RequireAuthenticatedAsync(
        CancellationToken cancellationToken = default) =>
        await RequireCurrentAsync(
            allowPasswordChangeRequired: false,
            cancellationToken);

    public async Task<AuthorizedUser> RequireCredentialChangeAsync(
        CancellationToken cancellationToken = default) =>
        await RequireCurrentAsync(
            allowPasswordChangeRequired: true,
            cancellationToken);

    private async Task<AuthorizedUser> RequireCurrentAsync(
        bool allowPasswordChangeRequired,
        CancellationToken cancellationToken)
    {
        if (!userContext.IsAuthenticated || !userContext.UserId.HasValue)
            throw new UnauthorizedAccessException("Authentication is required.");

        // The desktop context is process-lived, so its cached role/status is
        // never authoritative for a new workflow. API hosts use request-scoped
        // IUserContext instances established from current account state.
        if (userContext is not UserSession desktopSession)
            return ValidateRequestScopedContext(
                userContext,
                allowPasswordChangeRequired);

        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        var account = await db.UserAccounts.AsNoTracking()
            .SingleOrDefaultAsync(
                user => user.Id == userContext.UserId.Value,
                cancellationToken);

        if (account is null ||
            !account.IsActive ||
            account.IsLocked ||
            (!allowPasswordChangeRequired && account.MustChangePassword) ||
            !Enum.IsDefined(account.Role))
        {
            desktopSession.SignOut();
            throw new UnauthorizedAccessException(
                "The current account is no longer authorized. Sign in again.");
        }

        desktopSession.RefreshAuthorization(account);
        return new(account.Id, account.Username, account.DisplayName, account.Role);
    }

    public async Task<AuthorizedUser> RequireAnyRoleAsync(
        IReadOnlyCollection<UserRole> roles,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(roles);
        var user = await RequireAuthenticatedAsync(cancellationToken);
        if (!roles.Contains(user.Role))
        {
            var message = roles.Count == 1 && roles.Contains(UserRole.Administrator)
                ? "Administrator access is required."
                : "Operator or Administrator access is required.";
            throw new UnauthorizedAccessException(message);
        }

        return user;
    }

    private static AuthorizedUser ValidateRequestScopedContext(
        IUserContext context,
        bool allowPasswordChangeRequired)
    {
        if (!Enum.IsDefined(context.Role) ||
            (!allowPasswordChangeRequired && context.MustChangePassword))
            throw new UnauthorizedAccessException("The current account is not authorized.");

        return new(
            context.UserId!.Value,
            context.Username,
            context.DisplayName,
            context.Role);
    }
}

internal static class CurrentAccountAuthorizationExtensions
{
    private static readonly UserRole[] Administrator = [UserRole.Administrator];
    private static readonly UserRole[] Operational =
        [UserRole.Administrator, UserRole.Operator];

    public static Task<AuthorizedUser> RequireAdministratorAsync(
        this ICurrentAccountAuthorization authorization,
        CancellationToken cancellationToken = default) =>
        authorization.RequireAnyRoleAsync(Administrator, cancellationToken);

    public static Task<AuthorizedUser> RequireOperationalAsync(
        this ICurrentAccountAuthorization authorization,
        CancellationToken cancellationToken = default) =>
        authorization.RequireAnyRoleAsync(Operational, cancellationToken);
}
