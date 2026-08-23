using System.Security.Cryptography;
using System.Text.Json;
using BinTracker.Core;
using BinTracker.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace BinTracker.Services;

public sealed record BalanceRow(int CustomerId, string CustomerName, int ContainerTypeId, string ContainerTypeName, int Balance)
{
    public bool IsOutstanding => Balance > 0;
    public bool IsCredit => Balance < 0;
}

public sealed class UserSession(IBusinessClock clock) : IUserContext
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
        LoginUtc = clock.UtcNow;
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

internal sealed class AuditService(
    IDbContextFactory<BinTrackerDbContext> factory,
    IUserContext session,
    IBusinessClock clock,
    IClientContext client) : IAuditService
{
    public async Task WriteAsync(string action, string entityType, string? entityId, string description,
        bool succeeded = true, object? before = null, object? after = null,
        int? userIdOverride = null, string? usernameOverride = null,
        CancellationToken cancellationToken = default)
    {
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        db.AuditEvents.Add(new AuditEvent
        {
            TimestampUtc = clock.UtcNow,
            UserId = userIdOverride ?? session.UserId,
            Username = usernameOverride ?? session.Username,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            Description = description,
            BeforeValues = before is null ? null : JsonSerializer.Serialize(before),
            AfterValues = after is null ? null : JsonSerializer.Serialize(after),
            ComputerName = client.DeviceName,
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
    IAuditService audit,
    IBusinessClock clock) : IAuthenticationService
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
            CreatedUtc = clock.UtcNow
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

                // Atomic set-based update prevents simultaneous remote login
                // attempts from losing failed-attempt increments.
                await db.UserAccounts
                    .Where(x => x.Id == user.Id && x.IsActive && !x.IsLocked)
                    .ExecuteUpdateAsync(
                        setters => setters.SetProperty(
                            x => x.FailedLoginCount,
                            x => x.FailedLoginCount + 1),
                        cancellationToken);

                await db.UserAccounts
                    .Where(x =>
                        x.Id == user.Id &&
                        !x.IsLocked &&
                        x.FailedLoginCount >= maximum)
                    .ExecuteUpdateAsync(
                        setters => setters
                            .SetProperty(x => x.IsLocked, true)
                            .SetProperty(x => x.LockedUtc, clock.UtcNow),
                        cancellationToken);

                user = await db.UserAccounts
                    .AsNoTracking()
                    .SingleAsync(x => x.Id == user.Id, cancellationToken);
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

        var loginUtc = clock.UtcNow;
        var loginUpdated = await db.UserAccounts
            .Where(x => x.Id == user!.Id && x.IsActive && !x.IsLocked)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(x => x.LastLoginUtc, loginUtc)
                    .SetProperty(x => x.FailedLoginCount, 0)
                    .SetProperty(x => x.LockedUtc, (DateTime?)null),
                cancellationToken);

        if (loginUpdated != 1)
            throw new InvalidOperationException(
                "This account changed while you were signing in. Please try again.");

        user = await db.UserAccounts
            .AsNoTracking()
            .SingleAsync(x => x.Id == user!.Id, cancellationToken);

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
        var changed = await db.UserAccounts
            .Where(x =>
                x.Id == user.Id &&
                x.PasswordHash == user.PasswordHash &&
                x.PasswordSalt == user.PasswordSalt)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(x => x.PasswordHash, hash)
                    .SetProperty(x => x.PasswordSalt, salt)
                    .SetProperty(x => x.MustChangePassword, false)
                    .SetProperty(x => x.PasswordChangedUtc, clock.UtcNow)
                    .SetProperty(x => x.FailedLoginCount, 0)
                    .SetProperty(x => x.IsLocked, false)
                    .SetProperty(x => x.LockedUtc, (DateTime?)null),
                cancellationToken);

        if (changed != 1)
            throw new InvalidOperationException(
                "Your account credentials changed while this request was in progress. Sign in again and retry the password change.");

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

internal sealed class UserService(
    IDbContextFactory<BinTrackerDbContext> factory,
    IUserContext session,
    IAuditService audit,
    IBusinessClock clock) : IUserService
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
            Role=role, IsActive=true, MustChangePassword=true, CreatedUtc=clock.UtcNow, CreatedByUserId=session.UserId
        };
        db.UserAccounts.Add(user);
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            await using var verify = await factory.CreateDbContextAsync(cancellationToken);
            if (await verify.UserAccounts.AsNoTracking()
                    .AnyAsync(x => x.Username == username, cancellationToken))
            {
                throw new InvalidOperationException("That username already exists.");
            }

            throw;
        }

        await audit.WriteAsync("USER_CREATED", "UserAccount", user.Id.ToString(),
            $"User '{username}' created with role {role}.", after: new { user.Username, user.DisplayName, user.Role }, cancellationToken:cancellationToken);
    }

    public async Task SetActiveAsync(int userId, bool active, CancellationToken cancellationToken = default)
    {
        RequireAdmin();
        if (session.UserId == userId && !active) throw new InvalidOperationException("You cannot deactivate your own account.");
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        var user = await db.UserAccounts.AsNoTracking()
            .SingleAsync(x => x.Id == userId, cancellationToken);
        var before = user.IsActive;

        await db.UserAccounts
            .Where(x => x.Id == userId)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(x => x.IsActive, active),
                cancellationToken);

        await audit.WriteAsync(active ? "USER_ACTIVATED" : "USER_DEACTIVATED", "UserAccount", user.Id.ToString(),
            $"User '{user.Username}' {(active ? "activated" : "deactivated")}.", before:new { IsActive=before }, after:new { IsActive=active }, cancellationToken:cancellationToken);
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
        await db.UserAccounts
            .Where(x => x.Id == userId)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(x => x.PasswordHash, hash)
                    .SetProperty(x => x.PasswordSalt, salt)
                    .SetProperty(x => x.MustChangePassword, true)
                    .SetProperty(x => x.PasswordChangedUtc, clock.UtcNow)
                    .SetProperty(x => x.FailedLoginCount, 0)
                    .SetProperty(x => x.IsLocked, false)
                    .SetProperty(x => x.LockedUtc, (DateTime?)null),
                cancellationToken);

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
        var user = await db.UserAccounts.AsNoTracking()
            .SingleAsync(x => x.Id == userId, cancellationToken);

        if (locked)
        {
            await db.UserAccounts
                .Where(x => x.Id == userId)
                .ExecuteUpdateAsync(
                    setters => setters
                        .SetProperty(x => x.IsLocked, true)
                        .SetProperty(x => x.LockedUtc, clock.UtcNow),
                    cancellationToken);
        }
        else
        {
            await db.UserAccounts
                .Where(x => x.Id == userId)
                .ExecuteUpdateAsync(
                    setters => setters
                        .SetProperty(x => x.IsLocked, false)
                        .SetProperty(x => x.LockedUtc, (DateTime?)null)
                        .SetProperty(x => x.FailedLoginCount, 0),
                    cancellationToken);
        }

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
        var user = await db.UserAccounts.AsNoTracking()
            .SingleAsync(x => x.Id == userId, cancellationToken);

        var before = user.Role;
        if (before == role)
            return;

        await db.UserAccounts
            .Where(x => x.Id == userId)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(x => x.Role, role),
                cancellationToken);

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

        // Keep the SQL-translatable aggregation entirely in the database:
        // group by scalar foreign keys and calculate the signed balance.
        //
        // Do not group by navigation-property names here. Some EF Core providers
        // cannot reliably translate the previous join + navigation GroupBy +
        // BalanceRow constructor projection used by the Import Review path.
        var totals = await db.BinMovements.AsNoTracking()
            .GroupBy(x => new { x.CustomerId, x.ContainerTypeId })
            .Select(g => new
            {
                g.Key.CustomerId,
                g.Key.ContainerTypeId,
                Balance = g.Sum(x =>
                    x.MovementType == MovementType.Out
                        ? x.Quantity
                        : -x.Quantity)
            })
            .ToListAsync(cancellationToken);

        if (totals.Count == 0)
            return [];

        // Load the small master-data lookup tables directly.
        //
        // Do not filter these with int[].Contains(...) inside the EF query.
        // In the .NET 8 / EF Core 8 runtime used by BinTracker that pattern can
        // be reduced to a ReadOnlySpan<int> call while EF extracts parameters,
        // which is not valid inside the LINQ expression interpreter and throws
        // before the provider receives any SQL.
        var customerNames = await db.Customers.AsNoTracking()
            .ToDictionaryAsync(
                x => x.Id,
                x => x.Name,
                cancellationToken);

        var containerNames = await db.ContainerTypes.AsNoTracking()
            .ToDictionaryAsync(
                x => x.Id,
                x => x.Name,
                cancellationToken);

        return totals
            .Select(x => new BalanceRow(
                x.CustomerId,
                customerNames.TryGetValue(x.CustomerId, out var customerName)
                    ? customerName
                    : $"Customer #{x.CustomerId}",
                x.ContainerTypeId,
                containerNames.TryGetValue(x.ContainerTypeId, out var containerName)
                    ? containerName
                    : $"Container #{x.ContainerTypeId}",
                x.Balance))
            .OrderBy(x => x.CustomerName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(x => x.ContainerTypeName, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}

public static class ServiceSetup
{
    /// <summary>
    /// Registers provider-neutral BinTracker business services. The host must
    /// supply IUserContext and IClientContext with lifetimes appropriate to its
    /// execution model. A future API uses request-scoped implementations;
    /// the local desktop composition is provided by AddBinTrackerServices().
    /// </summary>
    public static IServiceCollection AddBinTrackerBusinessServices(
        this IServiceCollection services)
    {
        services.TryAddSingleton<IBusinessClock, ConfiguredBusinessClock>();
        services.AddScoped<IAuditService, AuditService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IBalanceService, BalanceService>();
        services.AddScoped<ICustomerService, CustomerService>();
        services.AddScoped<ICustomerStatementReportService, CustomerStatementReportService>();
        services.AddScoped<IContainerTypeService, ContainerTypeService>();
        services.AddScoped<IBusinessInformationService, BusinessInformationService>();
        services.AddScoped<IExcelImportService, ExcelImportService>();
        services.AddSingleton<
            IImportExecutionFailureInjector,
            NoOpImportExecutionFailureInjector>();
        services.AddScoped<IImportExecutionService, ImportExecutionService>();
        services.AddScoped<IImportRunHistoryService, ImportRunHistoryService>();
        services.AddScoped<IMarketFloorReportService, MarketFloorReportService>();
        services.AddScoped<IOutstandingReportService, OutstandingReportService>();
        services.AddScoped<IOutstandingReportPdfService, OutstandingReportPdfService>();
        services.AddScoped<IDailyMovementsReportService, DailyMovementsReportService>();
        services.AddScoped<IDailyMovementsReportPdfService, DailyMovementsReportPdfService>();
        services.AddScoped<IDailyPrintPackService, DailyPrintPackService>();
        services.AddScoped<IWeeklyMovementsReportService, WeeklyMovementsReportService>();
        services.AddScoped<IWeeklyMovementsReportPdfService, WeeklyMovementsReportPdfService>();
        services.AddScoped<IMovementHistoryReportService, MovementHistoryReportService>();
        services.AddScoped<IMovementHistoryReportPdfService, MovementHistoryReportPdfService>();
        services.AddScoped<IMonthlySummaryReportService, MonthlySummaryReportService>();
        services.AddScoped<IMonthlySummaryReportPdfService, MonthlySummaryReportPdfService>();
        services.AddScoped<IMovementService, MovementService>();
        services.AddScoped<IMovementCorrectionService, MovementCorrectionService>();
        return services;
    }

    /// <summary>
    /// Current local WinForms composition. Desktop-only mutable session state,
    /// device identity, authentication adapter and crash-draft storage live
    /// here so a future central API cannot inherit them accidentally.
    /// </summary>
    public static IServiceCollection AddBinTrackerServices(
        this IServiceCollection services)
    {
        services.AddSingleton<IClientContext, DesktopClientContext>();
        services.AddSingleton<UserSession>();
        services.AddSingleton<IUserContext>(sp => sp.GetRequiredService<UserSession>());
        services.AddSingleton<IBatchDraftStore, FileBatchDraftStore>();
        services.AddSingleton<ApplicationState>();
        services.AddScoped<IAuthenticationService, AuthenticationService>();

        return services.AddBinTrackerBusinessServices();
    }
}
