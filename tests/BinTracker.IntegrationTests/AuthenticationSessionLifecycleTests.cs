using BinTracker.Core;
using BinTracker.Data;
using BinTracker.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BinTracker.IntegrationTests;

public sealed class AuthenticationSessionLifecycleTests
{
    [Fact]
    public async Task Successful_login_establishes_authenticated_identity_and_role()
    {
        await using var harness = await Harness.CreateAsync();
        var administrator = await harness.AddUserAsync(
            "first-admin",
            "First Administrator",
            UserRole.Administrator,
            "CorrectHorse!42");

        Assert.True(await harness.Authentication.LoginAsync(
            administrator.Username,
            "CorrectHorse!42"));

        Assert.True(harness.Session.IsAuthenticated);
        Assert.Equal(administrator.Id, harness.Session.UserId);
        Assert.Equal(administrator.Username, harness.Session.Username);
        Assert.Equal(administrator.DisplayName, harness.Session.DisplayName);
        Assert.Equal(UserRole.Administrator, harness.Session.Role);
        Assert.Equal(Harness.UtcNow, harness.Session.LoginUtc);
        Assert.Same(harness.Session, harness.UserContext);
        Assert.NotNull(await harness.DeveloperDatabase.GetStatusAsync());
    }

    [Fact]
    public async Task Logout_clears_identity_and_administrator_authority()
    {
        await using var harness = await Harness.CreateAsync();
        var administrator = await harness.AddUserAsync(
            "logout-admin",
            "Logout Administrator",
            UserRole.Administrator,
            "CorrectHorse!42",
            mustChangePassword: true);
        Assert.True(await harness.Authentication.LoginAsync(
            administrator.Username,
            "CorrectHorse!42"));

        await harness.Authentication.LogoutAsync();

        AssertAnonymous(harness.Session);
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            harness.DeveloperDatabase.GetStatusAsync());
        var logout = Assert.Single(await harness.GetAuditAsync("LOGOUT"));
        Assert.Equal(administrator.Id, logout.UserId);
        Assert.Equal(administrator.Username, logout.Username);
        Assert.Equal(harness.Session.SessionId, logout.SessionId);
    }

    [Fact]
    public async Task Logout_followed_by_failed_login_remains_unauthenticated()
    {
        await using var harness = await Harness.CreateAsync();
        var administrator = await harness.AddUserAsync(
            "failed-admin",
            "Failed Login Administrator",
            UserRole.Administrator,
            "CorrectHorse!42");
        Assert.True(await harness.Authentication.LoginAsync(
            administrator.Username,
            "CorrectHorse!42"));
        await harness.Authentication.LogoutAsync();

        Assert.False(await harness.Authentication.LoginAsync(
            administrator.Username,
            "wrong-password"));

        AssertAnonymous(harness.Session);
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            harness.DeveloperDatabase.GetStatusAsync());
    }

    [Fact]
    public async Task Logout_followed_by_successful_login_establishes_only_new_identity()
    {
        await using var harness = await Harness.CreateAsync();
        var administrator = await harness.AddUserAsync(
            "old-admin",
            "Old Administrator",
            UserRole.Administrator,
            "CorrectHorse!42");
        var @operator = await harness.AddUserAsync(
            "new-operator",
            "New Operator",
            UserRole.Operator,
            "DifferentHorse!42");
        Assert.True(await harness.Authentication.LoginAsync(
            administrator.Username,
            "CorrectHorse!42"));

        await harness.Authentication.LogoutAsync();
        AssertAnonymous(harness.Session);
        Assert.True(await harness.Authentication.LoginAsync(
            @operator.Username,
            "DifferentHorse!42"));

        Assert.True(harness.Session.IsAuthenticated);
        Assert.Equal(@operator.Id, harness.Session.UserId);
        Assert.Equal(@operator.Username, harness.Session.Username);
        Assert.Equal(@operator.DisplayName, harness.Session.DisplayName);
        Assert.Equal(UserRole.Operator, harness.Session.Role);
        Assert.False(harness.Session.MustChangePassword);
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            harness.DeveloperDatabase.GetStatusAsync());
    }

    [Fact]
    public async Task Repeated_logout_is_idempotent_and_writes_one_logout_audit()
    {
        await using var harness = await Harness.CreateAsync();
        var administrator = await harness.AddUserAsync(
            "repeat-admin",
            "Repeat Administrator",
            UserRole.Administrator,
            "CorrectHorse!42");
        Assert.True(await harness.Authentication.LoginAsync(
            administrator.Username,
            "CorrectHorse!42"));

        await harness.Authentication.LogoutAsync();
        await harness.Authentication.LogoutAsync();

        AssertAnonymous(harness.Session);
        Assert.Equal(1, await harness.CountAuditAsync("LOGOUT"));
    }

    [Fact]
    public async Task Cancelled_logout_still_clears_authenticated_identity()
    {
        await using var harness = await Harness.CreateAsync();
        var administrator = await harness.AddUserAsync(
            "cancel-admin",
            "Cancel Administrator",
            UserRole.Administrator,
            "CorrectHorse!42");
        Assert.True(await harness.Authentication.LoginAsync(
            administrator.Username,
            "CorrectHorse!42"));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            harness.Authentication.LogoutAsync(new CancellationToken(canceled: true)));

        AssertAnonymous(harness.Session);
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            harness.DeveloperDatabase.GetStatusAsync());
    }

    [Fact]
    public async Task Failed_login_attempt_cannot_retain_a_previous_identity()
    {
        await using var harness = await Harness.CreateAsync();
        var administrator = await harness.AddUserAsync(
            "replace-admin",
            "Replace Administrator",
            UserRole.Administrator,
            "CorrectHorse!42");
        Assert.True(await harness.Authentication.LoginAsync(
            administrator.Username,
            "CorrectHorse!42"));

        Assert.False(await harness.Authentication.LoginAsync(
            administrator.Username,
            "wrong-password"));

        AssertAnonymous(harness.Session);
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            harness.DeveloperDatabase.GetStatusAsync());
    }

    [Fact]
    public async Task Cancelled_login_attempt_cannot_retain_a_previous_identity()
    {
        await using var harness = await Harness.CreateAsync();
        var administrator = await harness.AddUserAsync(
            "login-cancel-admin",
            "Login Cancel Administrator",
            UserRole.Administrator,
            "CorrectHorse!42");
        Assert.True(await harness.Authentication.LoginAsync(
            administrator.Username,
            "CorrectHorse!42"));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            harness.Authentication.LoginAsync(
                administrator.Username,
                "CorrectHorse!42",
                new CancellationToken(canceled: true)));

        AssertAnonymous(harness.Session);
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            harness.DeveloperDatabase.GetStatusAsync());
    }

    private static void AssertAnonymous(UserSession session)
    {
        Assert.False(session.IsAuthenticated);
        Assert.Null(session.UserId);
        Assert.Equal("anonymous", session.Username);
        Assert.Equal("Not signed in", session.DisplayName);
        Assert.Equal(UserRole.Viewer, session.Role);
        Assert.False(session.MustChangePassword);
        Assert.Null(session.LoginUtc);
    }

    private sealed class Harness : IAsyncDisposable
    {
        public static readonly DateTime UtcNow =
            new(2026, 10, 15, 1, 2, 3, DateTimeKind.Utc);

        private readonly SqliteConnection connection;
        private readonly ServiceProvider provider;
        private readonly AsyncServiceScope scope;
        private readonly IDbContextFactory<BinTrackerDbContext> factory;

        private Harness(
            SqliteConnection connection,
            ServiceProvider provider,
            AsyncServiceScope scope,
            IDbContextFactory<BinTrackerDbContext> factory)
        {
            this.connection = connection;
            this.provider = provider;
            this.scope = scope;
            this.factory = factory;
            Authentication = scope.ServiceProvider
                .GetRequiredService<IAuthenticationService>();
            Session = scope.ServiceProvider.GetRequiredService<UserSession>();
            UserContext = scope.ServiceProvider.GetRequiredService<IUserContext>();
            DeveloperDatabase = scope.ServiceProvider
                .GetRequiredService<IDeveloperDatabaseToolsService>();
        }

        public IAuthenticationService Authentication { get; }
        public UserSession Session { get; }
        public IUserContext UserContext { get; }
        public IDeveloperDatabaseToolsService DeveloperDatabase { get; }

        public static async Task<Harness> CreateAsync()
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();

            var services = new ServiceCollection();
            services.AddSingleton<IBusinessClock>(new FixedClock());
            services.AddDbContextFactory<BinTrackerDbContext>(options =>
                options.UseSqlite(connection));
            services.AddBinTrackerServices();
            services.AddSingleton<IDeveloperDatabaseService>(
                new RecordingDeveloperDatabaseService());

            var provider = services.BuildServiceProvider();
            var scope = provider.CreateAsyncScope();
            var factory = scope.ServiceProvider
                .GetRequiredService<IDbContextFactory<BinTrackerDbContext>>();
            await using (var db = await factory.CreateDbContextAsync())
            {
                await db.Database.EnsureCreatedAsync();
                await DatabaseSetup.InitializeSqliteAsync(db);
            }

            return new(connection, provider, scope, factory);
        }

        public async Task<UserAccount> AddUserAsync(
            string username,
            string displayName,
            UserRole role,
            string password,
            bool mustChangePassword = false)
        {
            var (hash, salt) = PasswordSecurity.Hash(password);
            var user = new UserAccount
            {
                Username = username,
                DisplayName = displayName,
                PasswordHash = hash,
                PasswordSalt = salt,
                Role = role,
                IsActive = true,
                MustChangePassword = mustChangePassword,
                CreatedUtc = UtcNow
            };

            await using var db = await factory.CreateDbContextAsync();
            db.UserAccounts.Add(user);
            await db.SaveChangesAsync();
            return user;
        }

        public async Task<int> CountAuditAsync(string action)
        {
            await using var db = await factory.CreateDbContextAsync();
            return await db.AuditEvents.CountAsync(item => item.Action == action);
        }

        public async Task<IReadOnlyList<AuditEvent>> GetAuditAsync(string action)
        {
            await using var db = await factory.CreateDbContextAsync();
            return await db.AuditEvents.AsNoTracking()
                .Where(item => item.Action == action)
                .OrderBy(item => item.Id)
                .ToListAsync();
        }

        public async ValueTask DisposeAsync()
        {
            await scope.DisposeAsync();
            await provider.DisposeAsync();
            await connection.DisposeAsync();
        }
    }

    private sealed class FixedClock : IBusinessClock
    {
        public DateTime UtcNow => Harness.UtcNow;
        public DateTime LocalNow => Harness.UtcNow;
        public DateOnly Today => DateOnly.FromDateTime(Harness.UtcNow);
        public string TimeZoneId => "Test/Fixed";
    }

    private sealed class RecordingDeveloperDatabaseService : IDeveloperDatabaseService
    {
        public DeveloperDatabaseStatus GetStatus() =>
            new("active.db", "backups", PendingRestart: false);

        public Task BackupAsync(
            string destinationPath,
            CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task<string> StageLoadAsync(
            string sourcePath,
            CancellationToken cancellationToken = default) =>
            Task.FromResult("load-backup.db");

        public Task<string> StageFreshAsync(
            CancellationToken cancellationToken = default) =>
            Task.FromResult("fresh-backup.db");
    }
}
