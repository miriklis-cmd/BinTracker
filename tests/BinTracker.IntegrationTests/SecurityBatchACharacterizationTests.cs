using BinTracker.Core;
using BinTracker.Data;
using BinTracker.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BinTracker.IntegrationTests;

public sealed class SecurityBatchACharacterizationTests
{
    [Fact]
    public async Task Username_creation_and_login_use_one_canonical_identity()
    {
        await using var harness = await Harness.CreateAsync();
        var authentication = harness.Get<IAuthenticationService>();

        await authentication.CreateInitialAdministratorAsync(
            "AdminUser", "Administrator", "CorrectHorse!42");
        Assert.True(await authentication.LoginAsync("adminuser", "CorrectHorse!42"));

        var users = harness.Get<IUserService>();
        await users.CreateUserAsync(
            "CaseUser", "Case User", "Temporary!42", UserRole.Viewer);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            users.CreateUserAsync(
                "caseuser", "Duplicate User", "Temporary!43", UserRole.Viewer));
    }

    [Fact]
    public async Task Anonymous_audit_and_report_reads_fail_closed_in_services()
    {
        await using var harness = await Harness.CreateAsync();
        var date = new DateOnly(2026, 10, 15);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            harness.Get<IAuditService>().GetRecentAsync());

        Func<Task>[] reads =
        [
            async () => await harness.Get<IDailyMovementsReportService>()
                .QueryAsync(new DailyMovementsReportQuery(date)),
            async () => await harness.Get<IWeeklyMovementsReportService>()
                .QueryAsync(new WeeklyMovementsReportQuery(date)),
            async () => await harness.Get<IMonthlySummaryReportService>()
                .QueryAsync(new MonthlySummaryReportQuery(date)),
            async () => await harness.Get<IOutstandingReportService>()
                .QueryAsync(new OutstandingReportQuery(date)),
            async () => await harness.Get<IMovementHistoryReportService>()
                .QueryAsync(new MovementHistoryReportQuery(date, date)),
            async () => await harness.Get<IMarketFloorReportService>()
                .GetAsync(date),
            async () => await harness.Get<ICustomerService>()
                .SearchAsync(null, includeInactive: false),
            async () => await harness.Get<IContainerTypeService>()
                .SearchAsync(null, includeInactive: false),
            async () => await harness.Get<IMovementService>()
                .GetDashboardSummaryAsync(date),
            async () => await harness.Get<IBalanceService>()
                .GetBalancesAsync()
        ];

        foreach (var read in reads)
            await Assert.ThrowsAsync<UnauthorizedAccessException>(read);
    }

    [Theory]
    [InlineData(UserRole.Operator)]
    [InlineData(UserRole.Viewer)]
    public async Task Non_administrators_remain_denied_direct_audit_review_access(
        UserRole role)
    {
        await using var harness = await Harness.CreateAsync();
        var user = await harness.AddUserAsync(
            $"{role.ToString().ToLowerInvariant()}-audit",
            $"{role} Audit User",
            role);
        harness.Get<UserSession>().SignIn(user);
        var audit = harness.Get<IAuditService>();

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            audit.GetAdministratorReviewStateAsync());
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            audit.GetUnreviewedMovementChangesAsync());
    }

    [Fact]
    public async Task Persisted_role_reduction_revokes_cached_administrator_authority()
    {
        await using var harness = await Harness.CreateAsync();
        var administrator = await harness.AddUserAsync(
            "role-admin", "Role Administrator", UserRole.Administrator);
        harness.Get<UserSession>().SignIn(administrator);

        await harness.UpdateUserAsync(
            administrator.Id,
            user => user.Role = UserRole.Operator);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            harness.Get<IUserService>().GetUsersAsync());
        Assert.Equal(UserRole.Operator, harness.Get<UserSession>().Role);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, true)]
    public async Task Persisted_inactive_or_locked_account_loses_subsequent_read_authority(
        bool active,
        bool locked)
    {
        await using var harness = await Harness.CreateAsync();
        var user = await harness.AddUserAsync(
            "report-user", "Report User", UserRole.Viewer);
        harness.Get<UserSession>().SignIn(user);

        await harness.UpdateUserAsync(user.Id, account =>
        {
            account.IsActive = active;
            account.IsLocked = locked;
        });

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            harness.Get<IDailyMovementsReportService>().QueryAsync(
                new DailyMovementsReportQuery(new DateOnly(2026, 10, 15))));
        Assert.False(harness.Get<UserSession>().IsAuthenticated);
    }

    [Fact]
    public async Task Administrator_self_reset_ends_the_current_session()
    {
        await using var harness = await Harness.CreateAsync();
        var administrator = await harness.AddUserAsync(
            "self-admin", "Self Administrator", UserRole.Administrator);
        harness.Get<UserSession>().SignIn(administrator);

        await harness.Get<IUserService>().ResetPasswordAsync(
            administrator.Id,
            "Temporary!42");

        Assert.False(harness.Get<UserSession>().IsAuthenticated);
    }

    [Fact]
    public void New_authentication_identifiers_reject_confusable_and_invisible_unicode()
    {
        Assert.Throws<ArgumentException>(() =>
            SecurityIdentifierPolicy.CanonicalizeNewUsername("\u0430dmin"));
        Assert.Throws<ArgumentException>(() =>
            SecurityIdentifierPolicy.CanonicalizeNewUsername("ad\u200Bmin"));
        Assert.Equal(
            "admin",
            SecurityIdentifierPolicy.CanonicalizeNewUsername("\uFF21\uFF24\uFF2D\uFF29\uFF2E"));
    }

    [Fact]
    public async Task Canonically_colliding_legacy_rows_fail_login_closed()
    {
        await using var harness = await Harness.CreateAsync();
        await harness.AddUserAsync(
            "LegacyAdmin", "Legacy Administrator", UserRole.Administrator);
        await harness.AddUserAsync(
            "legacyadmin", "Colliding Administrator", UserRole.Administrator);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            harness.Get<IAuthenticationService>().LoginAsync(
                "LEGACYADMIN",
                "CorrectHorse!42"));
        Assert.False(harness.Get<UserSession>().IsAuthenticated);
    }

    [Fact]
    public async Task Missing_persisted_account_fails_closed_and_ends_desktop_session()
    {
        await using var harness = await Harness.CreateAsync();
        harness.Get<UserSession>().SignIn(new UserAccount
        {
            Id = 999,
            Username = "missing-user",
            DisplayName = "Missing User",
            Role = UserRole.Viewer,
            IsActive = true
        });

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            harness.Get<IDailyMovementsReportService>().QueryAsync(
                new DailyMovementsReportQuery(new DateOnly(2026, 10, 15))));
        Assert.False(harness.Get<UserSession>().IsAuthenticated);
    }

    [Fact]
    public async Task Revalidation_database_failure_propagates_without_recasting_the_session()
    {
        var session = new UserSession(new FixedClock());
        session.SignIn(new UserAccount
        {
            Id = 17,
            Username = "current-user",
            DisplayName = "Current User",
            Role = UserRole.Viewer,
            IsActive = true
        });
        var authorization = new CurrentAccountAuthorization(
            new ThrowingFactory(),
            session);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            authorization.RequireAuthenticatedAsync());
        Assert.True(session.IsAuthenticated);
        Assert.Equal(17, session.UserId);
    }

    [Fact]
    public async Task Required_password_change_is_the_only_allowed_authenticated_workflow()
    {
        await using var harness = await Harness.CreateAsync();
        var user = await harness.AddUserAsync(
            "change-user",
            "Change User",
            UserRole.Viewer,
            mustChangePassword: true);
        harness.Get<UserSession>().SignIn(user);

        await harness.Get<IAuthenticationService>().ChangeOwnPasswordAsync(
            "CorrectHorse!42",
            "DifferentHorse!43");

        Assert.True(harness.Get<UserSession>().IsAuthenticated);
        Assert.False(harness.Get<UserSession>().MustChangePassword);
        await harness.Get<IDailyMovementsReportService>().QueryAsync(
            new DailyMovementsReportQuery(new DateOnly(2026, 10, 15)));
    }

    [Fact]
    public async Task Self_reset_audit_failure_still_revokes_session_after_persistence()
    {
        await using var harness = await Harness.CreateAsync();
        var administrator = await harness.AddUserAsync(
            "audit-failure-admin",
            "Audit Failure Administrator",
            UserRole.Administrator);
        var session = harness.Get<UserSession>();
        session.SignIn(administrator);
        var service = new UserService(
            harness.Factory,
            session,
            new FailingAuditService(),
            new FixedClock(),
            new CurrentAccountAuthorization(harness.Factory, session));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.ResetPasswordAsync(administrator.Id, "Temporary!42"));

        Assert.False(session.IsAuthenticated);
        await using var db = await harness.Factory.CreateDbContextAsync();
        var persisted = await db.UserAccounts.AsNoTracking()
            .SingleAsync(item => item.Id == administrator.Id);
        Assert.True(persisted.MustChangePassword);
        Assert.True(PasswordSecurity.Verify(
            "Temporary!42",
            persisted.PasswordHash,
            persisted.PasswordSalt));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Oversized_login_input_is_rejected_before_database_access(bool username)
    {
        var clock = new FixedClock();
        var factory = new ThrowingFactory();
        var audit = new NoOpAuthenticationAudit();
        var session = new UserSession(clock);
        var service = new AuthenticationService(
            factory,
            session,
            audit,
            audit,
            clock,
            new CurrentAccountAuthorization(factory, session));

        var result = await service.LoginAsync(
            username ? new string('a', 10_000) : "bounded-user",
            username ? "CorrectHorse!42" : new string('P', 10_000));

        Assert.False(result);
        Assert.Equal(0, factory.CreateCount);
    }

    private sealed class Harness : IAsyncDisposable
    {
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
        }

        public static async Task<Harness> CreateAsync()
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();

            var services = new ServiceCollection();
            services.AddSingleton<IBusinessClock>(new FixedClock());
            services.AddDbContextFactory<BinTrackerDbContext>(options =>
                options.UseSqlite(connection));
            services.AddBinTrackerServices();

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

        public T Get<T>() where T : notnull =>
            scope.ServiceProvider.GetRequiredService<T>();

        public IDbContextFactory<BinTrackerDbContext> Factory => factory;

        public async Task<UserAccount> AddUserAsync(
            string username,
            string displayName,
            UserRole role,
            bool mustChangePassword = false)
        {
            var (hash, salt) = PasswordSecurity.Hash("CorrectHorse!42");
            var user = new UserAccount
            {
                Username = username,
                DisplayName = displayName,
                PasswordHash = hash,
                PasswordSalt = salt,
                Role = role,
                IsActive = true,
                MustChangePassword = mustChangePassword,
                CreatedUtc = FixedClock.Utc
            };

            await using var db = await factory.CreateDbContextAsync();
            db.UserAccounts.Add(user);
            await db.SaveChangesAsync();
            return user;
        }

        public async Task UpdateUserAsync(
            int userId,
            Action<UserAccount> update)
        {
            await using var db = await factory.CreateDbContextAsync();
            var user = await db.UserAccounts.SingleAsync(item => item.Id == userId);
            update(user);
            await db.SaveChangesAsync();
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
        public static readonly DateTime Utc =
            new(2026, 10, 15, 1, 2, 3, DateTimeKind.Utc);
        public DateTime UtcNow => Utc;
        public DateTime LocalNow => Utc;
        public DateOnly Today => DateOnly.FromDateTime(Utc);
        public string TimeZoneId => "UTC";
    }

    private sealed class ThrowingFactory : IDbContextFactory<BinTrackerDbContext>
    {
        public int CreateCount { get; private set; }

        public BinTrackerDbContext CreateDbContext()
        {
            CreateCount++;
            throw new InvalidOperationException("Database access was not expected.");
        }

        public Task<BinTrackerDbContext> CreateDbContextAsync(
            CancellationToken cancellationToken = default)
        {
            CreateCount++;
            throw new InvalidOperationException("Database access was not expected.");
        }
    }

    private sealed class NoOpAuthenticationAudit :
        IAuditAppender,
        IAuthenticationAuditAppender
    {
        public Task AppendAsync(
            string action,
            string entityType,
            string? entityId,
            string description,
            bool succeeded = true,
            object? before = null,
            object? after = null,
            CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task AppendAuthenticationEventAsync(
            string action,
            string entityType,
            string? entityId,
            string description,
            int? actorUserId,
            string actorUsername,
            bool succeeded = true,
            object? before = null,
            object? after = null,
            CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed class FailingAuditService : IAuditService, IAuditAppender
    {
        public event EventHandler<AdministratorReviewState>? AdministratorReviewStateChanged
        {
            add { }
            remove { }
        }

        public Task AppendAsync(
            string action,
            string entityType,
            string? entityId,
            string description,
            bool succeeded = true,
            object? before = null,
            object? after = null,
            CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("Simulated audit persistence failure.");

        public Task<IReadOnlyList<AuditEventSummary>> GetRecentAsync(
            int limit = 500,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<AuditTrailRow>> GetAuditTrailAsync(
            AuditReviewFilter filter = AuditReviewFilter.All,
            int limit = 500,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<AdministratorReviewState> GetAdministratorReviewStateAsync(
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<PendingMovementChangeAuditEvent>> GetUnreviewedMovementChangesAsync(
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task MarkMovementChangesReviewedAsync(
            IReadOnlyCollection<long> auditEventIds,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<MovementBatchAuditLine>> GetMovementBatchDetailAsync(
            int batchId,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<MovementChangeAuditDetail?> GetMovementChangeDetailAsync(
            long auditEventId,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }
}
