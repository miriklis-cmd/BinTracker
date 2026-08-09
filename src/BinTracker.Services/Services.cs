using System.Security.Cryptography;
using System.Text.Json;
using BinTracker.Core;
using BinTracker.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace BinTracker.Services;

public sealed record BalanceRow(int CustomerId, string CustomerName, int ContainerTypeId, string ContainerTypeName, int Balance)
{
    public bool IsOutstanding => Balance > 0;
    public bool IsCredit => Balance < 0;
}

public sealed class UserSession
{
    public string SessionId { get; } = Guid.NewGuid().ToString("N");
    public int? UserId { get; private set; }
    public string Username { get; private set; } = "anonymous";
    public string DisplayName { get; private set; } = "Not signed in";
    public UserRole Role { get; private set; } = UserRole.Viewer;
    public bool MustChangePassword { get; private set; }
    public DateTime? LoginUtc { get; private set; }
    public bool IsAuthenticated => UserId.HasValue;

    public void SignIn(UserAccount user)
    {
        UserId = user.Id;
        Username = user.Username;
        DisplayName = user.DisplayName;
        Role = user.Role;
        MustChangePassword = user.MustChangePassword;
        LoginUtc = DateTime.UtcNow;
    }

    public void PasswordChanged() => MustChangePassword = false;
}

public interface IAuditService
{
    Task WriteAsync(string action, string entityType, string? entityId, string description,
        bool succeeded = true, object? before = null, object? after = null,
        int? userIdOverride = null, string? usernameOverride = null,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AuditEvent>> GetRecentAsync(int limit = 500, CancellationToken cancellationToken = default);
}

internal sealed class AuditService(IDbContextFactory<BinTrackerDbContext> factory, UserSession session) : IAuditService
{
    public async Task WriteAsync(string action, string entityType, string? entityId, string description,
        bool succeeded = true, object? before = null, object? after = null,
        int? userIdOverride = null, string? usernameOverride = null,
        CancellationToken cancellationToken = default)
    {
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        db.AuditEvents.Add(new AuditEvent
        {
            TimestampUtc = DateTime.UtcNow,
            UserId = userIdOverride ?? session.UserId,
            Username = usernameOverride ?? session.Username,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            Description = description,
            BeforeValues = before is null ? null : JsonSerializer.Serialize(before),
            AfterValues = after is null ? null : JsonSerializer.Serialize(after),
            ComputerName = Environment.MachineName,
            SessionId = session.SessionId,
            Succeeded = succeeded
        });
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AuditEvent>> GetRecentAsync(int limit = 500, CancellationToken cancellationToken = default)
    {
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        return await db.AuditEvents.AsNoTracking()
            .OrderByDescending(x => x.TimestampUtc)
            .Take(Math.Clamp(limit, 1, 5000))
            .ToListAsync(cancellationToken);
    }
}

public interface IAuthenticationService
{
    Task<bool> HasUsersAsync(CancellationToken cancellationToken = default);
    Task CreateInitialAdministratorAsync(string username, string displayName, string password, CancellationToken cancellationToken = default);
    Task<bool> LoginAsync(string username, string password, CancellationToken cancellationToken = default);
    Task ChangeOwnPasswordAsync(string currentPassword, string newPassword, CancellationToken cancellationToken = default);
    Task LogoutAsync(CancellationToken cancellationToken = default);
}

internal sealed class AuthenticationService(
    IDbContextFactory<BinTrackerDbContext> factory,
    UserSession session,
    IAuditService audit) : IAuthenticationService
{
    public async Task<bool> HasUsersAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        return await db.UserAccounts.AnyAsync(cancellationToken);
    }

    public async Task CreateInitialAdministratorAsync(string username, string displayName, string password, CancellationToken cancellationToken = default)
    {
        username = username.Trim();
        displayName = displayName.Trim();
        ValidateCredentials(username, displayName, password);

        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        if (await db.UserAccounts.AnyAsync(cancellationToken))
            throw new InvalidOperationException("The initial administrator has already been created.");

        var (hash, salt) = PasswordSecurity.Hash(password);
        var user = new UserAccount
        {
            Username = username,
            DisplayName = displayName,
            PasswordHash = hash,
            PasswordSalt = salt,
            Role = UserRole.Administrator,
            IsActive = true,
            CreatedUtc = DateTime.UtcNow
        };
        db.UserAccounts.Add(user);
        await db.SaveChangesAsync(cancellationToken);
        await audit.WriteAsync("USER_CREATED", "UserAccount", user.Id.ToString(),
            $"Initial administrator '{username}' created.", after: new { user.Username, user.DisplayName, user.Role },
            userIdOverride: user.Id, usernameOverride: user.Username, cancellationToken: cancellationToken);
    }

    public async Task<bool> LoginAsync(string username, string password, CancellationToken cancellationToken = default)
    {
        username = username.Trim();
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        var user = await db.UserAccounts.SingleOrDefaultAsync(x => x.Username == username, cancellationToken);

        if (user is not null && user.IsLocked)
        {
            await audit.WriteAsync(
                "LOGIN_BLOCKED_LOCKED",
                "Session",
                null,
                $"Login blocked because account '{username}' is locked.",
                false,
                userIdOverride: user.Id,
                usernameOverride: user.Username,
                cancellationToken: cancellationToken);

            throw new InvalidOperationException(
                "This account is locked after too many failed login attempts. Ask an administrator to unlock it.");
        }

        var valid = user is not null &&
                    user.IsActive &&
                    PasswordSecurity.Verify(password, user.PasswordHash, user.PasswordSalt);

        if (!valid)
        {
            if (user is not null && user.IsActive)
            {
                var settings = await db.ApplicationSettings.AsNoTracking()
                    .SingleOrDefaultAsync(x => x.Id == 1, cancellationToken);
                var maximum = Math.Max(1, settings?.MaxFailedLoginAttempts ?? 5);

                user.FailedLoginCount++;
                if (user.FailedLoginCount >= maximum)
                {
                    user.IsLocked = true;
                    user.LockedUtc = DateTime.UtcNow;
                }

                await db.SaveChangesAsync(cancellationToken);
            }

            await audit.WriteAsync(
                "LOGIN_FAILED",
                "Session",
                null,
                $"Failed login attempt for username '{username}'.",
                false,
                userIdOverride: user?.Id,
                usernameOverride: string.IsNullOrWhiteSpace(username) ? "unknown" : username,
                cancellationToken: cancellationToken);

            if (user?.IsLocked == true)
            {
                await audit.WriteAsync(
                    "ACCOUNT_LOCKED",
                    "UserAccount",
                    user.Id.ToString(),
                    $"User '{user.Username}' locked after repeated failed login attempts.",
                    false,
                    userIdOverride: user.Id,
                    usernameOverride: user.Username,
                    cancellationToken: cancellationToken);
            }

            return false;
        }

        user!.LastLoginUtc = DateTime.UtcNow;
        user.FailedLoginCount = 0;
        user.IsLocked = false;
        user.LockedUtc = null;
        await db.SaveChangesAsync(cancellationToken);

        session.SignIn(user);

        await audit.WriteAsync(
            "LOGIN_SUCCEEDED",
            "Session",
            session.SessionId,
            $"{user.DisplayName} logged in.",
            cancellationToken: cancellationToken);

        return true;
    }

    public async Task ChangeOwnPasswordAsync(
        string currentPassword,
        string newPassword,
        CancellationToken cancellationToken = default)
    {
        if (!session.UserId.HasValue)
            throw new UnauthorizedAccessException("You must be logged in to change your password.");

        PasswordPolicy.Validate(newPassword);

        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        var user = await db.UserAccounts.SingleAsync(x => x.Id == session.UserId.Value, cancellationToken);

        if (!PasswordSecurity.Verify(currentPassword, user.PasswordHash, user.PasswordSalt))
            throw new InvalidOperationException("The current password is incorrect.");

        if (PasswordSecurity.Verify(newPassword, user.PasswordHash, user.PasswordSalt))
            throw new InvalidOperationException("The new password must be different from the current password.");

        var (hash, salt) = PasswordSecurity.Hash(newPassword);
        user.PasswordHash = hash;
        user.PasswordSalt = salt;
        user.MustChangePassword = false;
        user.PasswordChangedUtc = DateTime.UtcNow;
        user.FailedLoginCount = 0;
        user.IsLocked = false;
        user.LockedUtc = null;

        await db.SaveChangesAsync(cancellationToken);
        session.PasswordChanged();

        await audit.WriteAsync(
            "PASSWORD_CHANGED",
            "UserAccount",
            user.Id.ToString(),
            $"User '{user.Username}' changed their password.",
            cancellationToken: cancellationToken);
    }

    public async Task LogoutAsync(CancellationToken cancellationToken = default)
    {
        if (session.IsAuthenticated)
            await audit.WriteAsync("LOGOUT", "Session", session.SessionId,
                $"{session.DisplayName} logged out.", cancellationToken: cancellationToken);
    }

    private static void ValidateCredentials(string username, string displayName, string password)
    {
        if (username.Length < 3) throw new ArgumentException("Username must be at least 3 characters.");
        if (displayName.Length < 2) throw new ArgumentException("Display name is required.");
        PasswordPolicy.Validate(password);
    }
}

public interface IUserService
{
    Task<IReadOnlyList<UserAccount>> GetUsersAsync(CancellationToken cancellationToken = default);
    Task CreateUserAsync(string username, string displayName, string password, UserRole role, CancellationToken cancellationToken = default);
    Task SetActiveAsync(int userId, bool active, CancellationToken cancellationToken = default);
    Task ResetPasswordAsync(int userId, string temporaryPassword, CancellationToken cancellationToken = default);
    Task SetLockedAsync(int userId, bool locked, CancellationToken cancellationToken = default);
    Task SetRoleAsync(int userId, UserRole role, CancellationToken cancellationToken = default);
}

internal sealed class UserService(IDbContextFactory<BinTrackerDbContext> factory, UserSession session, IAuditService audit) : IUserService
{
    public async Task<IReadOnlyList<UserAccount>> GetUsersAsync(CancellationToken cancellationToken = default)
    {
        RequireAdmin();
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        return await db.UserAccounts.AsNoTracking().OrderBy(x => x.Username).ToListAsync(cancellationToken);
    }

    public async Task CreateUserAsync(string username, string displayName, string password, UserRole role, CancellationToken cancellationToken = default)
    {
        RequireAdmin();
        username = username.Trim(); displayName = displayName.Trim();
        if (username.Length < 3) throw new ArgumentException("Username must be at least 3 characters.");
        if (displayName.Length < 2) throw new ArgumentException("Display name is required.");
        PasswordPolicy.Validate(password);

        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        if (await db.UserAccounts.AnyAsync(x => x.Username == username, cancellationToken))
            throw new InvalidOperationException("That username already exists.");
        var (hash, salt) = PasswordSecurity.Hash(password);
        var user = new UserAccount
        {
            Username=username, DisplayName=displayName, PasswordHash=hash, PasswordSalt=salt,
            Role=role, IsActive=true, MustChangePassword=true, CreatedUtc=DateTime.UtcNow, CreatedByUserId=session.UserId
        };
        db.UserAccounts.Add(user); await db.SaveChangesAsync(cancellationToken);
        await audit.WriteAsync("USER_CREATED", "UserAccount", user.Id.ToString(),
            $"User '{username}' created with role {role}.", after: new { user.Username, user.DisplayName, user.Role }, cancellationToken:cancellationToken);
    }

    public async Task SetActiveAsync(int userId, bool active, CancellationToken cancellationToken = default)
    {
        RequireAdmin();
        if (session.UserId == userId && !active) throw new InvalidOperationException("You cannot deactivate your own account.");
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        var user = await db.UserAccounts.SingleAsync(x => x.Id == userId, cancellationToken);
        var before = user.IsActive; user.IsActive = active; await db.SaveChangesAsync(cancellationToken);
        await audit.WriteAsync(active ? "USER_ACTIVATED" : "USER_DEACTIVATED", "UserAccount", user.Id.ToString(),
            $"User '{user.Username}' {(active ? "activated" : "deactivated")}.", before:new { IsActive=before }, after:new { user.IsActive }, cancellationToken:cancellationToken);
    }

    public async Task ResetPasswordAsync(
        int userId,
        string temporaryPassword,
        CancellationToken cancellationToken = default)
    {
        RequireAdmin();
        PasswordPolicy.Validate(temporaryPassword);

        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        var user = await db.UserAccounts.SingleAsync(x => x.Id == userId, cancellationToken);

        var (hash, salt) = PasswordSecurity.Hash(temporaryPassword);
        user.PasswordHash = hash;
        user.PasswordSalt = salt;
        user.MustChangePassword = true;
        user.PasswordChangedUtc = DateTime.UtcNow;
        user.FailedLoginCount = 0;
        user.IsLocked = false;
        user.LockedUtc = null;

        await db.SaveChangesAsync(cancellationToken);

        await audit.WriteAsync(
            "PASSWORD_RESET_BY_ADMIN",
            "UserAccount",
            user.Id.ToString(),
            $"Administrator reset the password for '{user.Username}'. User must change it at next login.",
            cancellationToken: cancellationToken);
    }

    public async Task SetLockedAsync(
        int userId,
        bool locked,
        CancellationToken cancellationToken = default)
    {
        RequireAdmin();

        if (session.UserId == userId && locked)
            throw new InvalidOperationException("You cannot lock your own account.");

        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        var user = await db.UserAccounts.SingleAsync(x => x.Id == userId, cancellationToken);

        user.IsLocked = locked;
        user.LockedUtc = locked ? DateTime.UtcNow : null;

        if (!locked)
            user.FailedLoginCount = 0;

        await db.SaveChangesAsync(cancellationToken);

        await audit.WriteAsync(
            locked ? "ACCOUNT_LOCKED_BY_ADMIN" : "ACCOUNT_UNLOCKED",
            "UserAccount",
            user.Id.ToString(),
            $"Administrator {(locked ? "locked" : "unlocked")} user '{user.Username}'.",
            cancellationToken: cancellationToken);
    }

    public async Task SetRoleAsync(
        int userId,
        UserRole role,
        CancellationToken cancellationToken = default)
    {
        RequireAdmin();

        if (session.UserId == userId && role != UserRole.Administrator)
            throw new InvalidOperationException(
                "You cannot remove your own Administrator role while you are signed in.");

        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        var user = await db.UserAccounts.SingleAsync(x => x.Id == userId, cancellationToken);

        var before = user.Role;
        if (before == role)
            return;

        user.Role = role;
        await db.SaveChangesAsync(cancellationToken);

        await audit.WriteAsync(
            "USER_ROLE_CHANGED",
            "UserAccount",
            user.Id.ToString(),
            $"User '{user.Username}' role changed from {before} to {role}.",
            before: new { Role = before },
            after: new { Role = role },
            cancellationToken: cancellationToken);
    }

    private void RequireAdmin()
    {
        if (session.Role != UserRole.Administrator) throw new UnauthorizedAccessException("Administrator access is required.");
    }
}


public static class PasswordPolicy
{
    public static void Validate(string password)
    {
        if (password.Length < 10)
            throw new ArgumentException("Password must be at least 10 characters.");

        if (!password.Any(char.IsUpper) ||
            !password.Any(char.IsLower) ||
            !password.Any(char.IsDigit))
        {
            throw new ArgumentException(
                "Password must contain at least one uppercase letter, one lowercase letter, and one number.");
        }
    }

    public static string StrengthText(string password)
    {
        if (string.IsNullOrEmpty(password))
            return string.Empty;

        var score = 0;
        if (password.Length >= 10) score++;
        if (password.Length >= 14) score++;
        if (password.Any(char.IsUpper) && password.Any(char.IsLower)) score++;
        if (password.Any(char.IsDigit)) score++;
        if (password.Any(ch => !char.IsLetterOrDigit(ch))) score++;

        return score switch
        {
            <= 2 => "Weak",
            3 => "Good",
            _ => "Strong"
        };
    }
}

internal static class PasswordSecurity
{
    private const int Iterations = 210_000;
    public static (string Hash, string Salt) Hash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(32);
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, 32);
        return (Convert.ToBase64String(hash), Convert.ToBase64String(salt));
    }
    public static bool Verify(string password, string expectedHash, string salt)
    {
        try
        {
            var actual = Rfc2898DeriveBytes.Pbkdf2(password, Convert.FromBase64String(salt), Iterations, HashAlgorithmName.SHA256, 32);
            return CryptographicOperations.FixedTimeEquals(actual, Convert.FromBase64String(expectedHash));
        }
        catch { return false; }
    }
}

public interface IBalanceService
{
    Task<IReadOnlyList<BalanceRow>> GetBalancesAsync(CancellationToken cancellationToken = default);
}

internal sealed class BalanceService(IDbContextFactory<BinTrackerDbContext> factory) : IBalanceService
{
    public async Task<IReadOnlyList<BalanceRow>> GetBalancesAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        return await db.BinMovements.AsNoTracking()
            .GroupBy(x => new { x.CustomerId, CustomerName=x.Customer.Name, x.ContainerTypeId, ContainerTypeName=x.ContainerType.Name })
            .Select(g => new BalanceRow(g.Key.CustomerId, g.Key.CustomerName, g.Key.ContainerTypeId, g.Key.ContainerTypeName,
                g.Sum(x => x.MovementType == MovementType.Out ? x.Quantity : -x.Quantity)))
            .OrderBy(x => x.CustomerName).ThenBy(x => x.ContainerTypeName)
            .ToListAsync(cancellationToken);
    }
}

public static class ServiceSetup
{
    public static IServiceCollection AddBinTrackerServices(this IServiceCollection services)
    {
        services.AddSingleton<UserSession>();
        services.AddSingleton<ApplicationState>();
        services.AddScoped<IAuditService, AuditService>();
        services.AddScoped<IAuthenticationService, AuthenticationService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IBalanceService, BalanceService>();
        services.AddScoped<ICustomerService, CustomerService>();
        services.AddScoped<ICustomerStatementReportService, CustomerStatementReportService>();
        services.AddScoped<IMovementService, MovementService>();
        return services;
    }
}
