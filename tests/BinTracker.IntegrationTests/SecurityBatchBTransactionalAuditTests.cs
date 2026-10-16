using BinTracker.Core;
using BinTracker.Data;
using BinTracker.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BinTracker.IntegrationTests;

public sealed class SecurityBatchBTransactionalAuditTests
{
    [Fact]
    public async Task Customer_create_is_rolled_back_when_required_audit_persistence_fails()
    {
        await using var harness = await Harness.CreateAsync();
        await harness.FailAuditWritesAsync();

        await Assert.ThrowsAsync<DbUpdateException>(() =>
            harness.Get<ICustomerService>().SaveAsync(new CustomerEditModel
            {
                CustomerCode = "ATOMIC-CUSTOMER",
                Name = "Atomic Customer"
            }));

        await using var verify = harness.CreateContext();
        Assert.False(await verify.Customers.AnyAsync(x => x.CustomerCode == "ATOMIC-CUSTOMER"));
    }

    [Fact]
    public async Task Container_type_create_is_rolled_back_when_required_audit_persistence_fails()
    {
        await using var harness = await Harness.CreateAsync();
        await harness.FailAuditWritesAsync();

        await Assert.ThrowsAsync<DbUpdateException>(() =>
            harness.Get<IContainerTypeService>().SaveAsync(new ContainerTypeEditModel(
                0,
                "Atomic Container",
                "ATOM",
                string.Empty,
                null,
                null,
                10,
                true,
                false,
                null,
                new ContainerTypeUsage(0, 0, null, null),
                0)));

        await using var verify = harness.CreateContext();
        Assert.False(await verify.ContainerTypes.AnyAsync(x => x.ShortCode == "ATOM"));
    }

    [Fact]
    public async Task Business_information_update_is_rolled_back_when_required_audit_persistence_fails()
    {
        await using var harness = await Harness.CreateAsync();
        var service = harness.Get<IBusinessInformationService>();
        var before = await service.GetAsync();
        await harness.FailAuditWritesAsync();

        await Assert.ThrowsAsync<DbUpdateException>(() => service.SaveAsync(
            before with { BusinessName = "Must Roll Back" }));

        await harness.AllowAuditWritesAsync();
        Assert.Equal(before.BusinessName, (await service.GetAsync()).BusinessName);
    }

    [Fact]
    public async Task User_create_is_rolled_back_when_required_audit_persistence_fails()
    {
        await using var harness = await Harness.CreateAsync();
        await harness.FailAuditWritesAsync();

        await Assert.ThrowsAsync<DbUpdateException>(() =>
            harness.Get<IUserService>().CreateUserAsync(
                "atomic-user",
                "Atomic User",
                "CorrectHorse!43",
                UserRole.Viewer));

        await using var verify = harness.CreateContext();
        Assert.False(await verify.UserAccounts.AnyAsync(x => x.Username == "atomic-user"));
    }

    [Fact]
    public async Task Failed_login_state_is_rolled_back_when_required_audit_persistence_fails()
    {
        await using var harness = await Harness.CreateAsync();
        await harness.SetMaximumFailedLoginsAsync(3);
        await harness.FailAuditWritesAsync();

        await Assert.ThrowsAsync<DbUpdateException>(() =>
            harness.Authentication.LoginAsync(Harness.AdminUsername, "wrong-password"));

        await using var verify = harness.CreateContext();
        var account = await verify.UserAccounts.AsNoTracking()
            .SingleAsync(x => x.Username == Harness.AdminUsername);
        Assert.Equal(2, account.FailedLoginCount);
        Assert.False(account.IsLocked);
        Assert.Null(account.LockedUtc);
    }

    [Fact]
    public async Task Customer_mutation_and_required_audit_commit_with_actor_attribution()
    {
        await using var harness = await Harness.CreateAsync();

        var customerId = await harness.Get<ICustomerService>().SaveAsync(
            new CustomerEditModel
            {
                CustomerCode = "ATOMIC-SUCCESS",
                Name = "Atomic Success"
            });

        await using var verify = harness.CreateContext();
        var customer = await verify.Customers.AsNoTracking()
            .SingleAsync(x => x.Id == customerId);
        var audit = await verify.AuditEvents.AsNoTracking()
            .SingleAsync(x => x.Action == "CUSTOMER_CREATED" && x.EntityId == customerId.ToString());
        var administrator = await verify.UserAccounts.AsNoTracking()
            .SingleAsync(x => x.Username == Harness.AdminUsername);
        Assert.Equal("ATOMIC-SUCCESS", customer.CustomerCode);
        Assert.Equal(administrator.Id, audit.UserId);
        Assert.Equal(Harness.AdminUsername, audit.Username);
    }

    [Fact]
    public async Task Stale_customer_revision_commits_neither_mutation_nor_false_success_audit()
    {
        await using var harness = await Harness.CreateAsync();
        var customers = harness.Get<ICustomerService>();
        var customerId = await customers.SaveAsync(new CustomerEditModel
        {
            CustomerCode = "STALE-CUSTOMER",
            Name = "Original Name"
        });
        var stale = (await customers.GetAsync(customerId))!;
        stale.Name = "First Update";
        await customers.SaveAsync(stale);
        stale.Name = "Rejected Stale Update";

        await Assert.ThrowsAsync<InvalidOperationException>(() => customers.SaveAsync(stale));

        await using var verify = harness.CreateContext();
        Assert.Equal("First Update", (await verify.Customers.AsNoTracking()
            .SingleAsync(x => x.Id == customerId)).Name);
        Assert.Equal(1, await verify.AuditEvents.CountAsync(
            x => x.Action == "CUSTOMER_UPDATED" && x.EntityId == customerId.ToString()));
    }

    [Fact]
    public async Task Failed_login_state_and_required_audits_commit_together_at_lockout_threshold()
    {
        await using var harness = await Harness.CreateAsync();
        await harness.SetMaximumFailedLoginsAsync(3);

        Assert.False(await harness.Authentication.LoginAsync(
            Harness.AdminUsername,
            "wrong-password"));

        await using var verify = harness.CreateContext();
        var account = await verify.UserAccounts.AsNoTracking()
            .SingleAsync(x => x.Username == Harness.AdminUsername);
        Assert.Equal(3, account.FailedLoginCount);
        Assert.True(account.IsLocked);
        Assert.Equal(Harness.UtcNow, account.LockedUtc);
        Assert.Equal(1, await verify.AuditEvents.CountAsync(x => x.Action == "LOGIN_FAILED"));
        Assert.Equal(1, await verify.AuditEvents.CountAsync(x => x.Action == "ACCOUNT_LOCKED"));
    }

    [Fact]
    public async Task Successful_login_state_and_required_audit_commit_before_session_publication()
    {
        await using var harness = await Harness.CreateAsync();

        Assert.True(await harness.Authentication.LoginAsync(
            Harness.AdminUsername,
            Harness.AdminPassword));

        await using var verify = harness.CreateContext();
        var account = await verify.UserAccounts.AsNoTracking()
            .SingleAsync(x => x.Username == Harness.AdminUsername);
        var audit = await verify.AuditEvents.AsNoTracking()
            .SingleAsync(x => x.Action == "LOGIN_SUCCEEDED");
        Assert.Equal(Harness.UtcNow, account.LastLoginUtc);
        Assert.Equal(0, account.FailedLoginCount);
        Assert.Equal(account.Id, audit.UserId);
        Assert.Equal(account.Username, audit.Username);
        Assert.True(harness.Session.IsAuthenticated);
        Assert.Equal(account.Id, harness.Session.UserId);
    }

    [Fact]
    public async Task Own_password_change_is_rolled_back_when_required_audit_persistence_fails()
    {
        await using var harness = await Harness.CreateAsync();
        await harness.FailAuditWritesAsync();

        await Assert.ThrowsAsync<DbUpdateException>(() =>
            harness.Authentication.ChangeOwnPasswordAsync(
                Harness.AdminPassword,
                "DifferentHorse!43"));

        await using var verify = harness.CreateContext();
        var account = await verify.UserAccounts.AsNoTracking()
            .SingleAsync(x => x.Username == Harness.AdminUsername);
        Assert.True(PasswordSecurity.Verify(
            Harness.AdminPassword,
            account.PasswordHash,
            account.PasswordSalt));
        Assert.False(PasswordSecurity.Verify(
            "DifferentHorse!43",
            account.PasswordHash,
            account.PasswordSalt));
    }

    [Fact]
    public async Task Successful_login_state_is_rolled_back_when_required_audit_persistence_fails()
    {
        await using var harness = await Harness.CreateAsync();
        await harness.FailAuditWritesAsync();

        await Assert.ThrowsAsync<DbUpdateException>(() =>
            harness.Authentication.LoginAsync(Harness.AdminUsername, Harness.AdminPassword));

        await using var verify = harness.CreateContext();
        var account = await verify.UserAccounts.AsNoTracking()
            .SingleAsync(x => x.Username == Harness.AdminUsername);
        Assert.Null(account.LastLoginUtc);
        Assert.Equal(2, account.FailedLoginCount);
        Assert.False(harness.Session.IsAuthenticated);
    }

    private sealed class Harness : IAsyncDisposable
    {
        public const string AdminUsername = "batch-b-admin";
        public const string AdminPassword = "CorrectHorse!42";
        public static readonly DateTime UtcNow =
            new(2026, 10, 16, 1, 2, 3, DateTimeKind.Utc);

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
            Authentication = Get<IAuthenticationService>();
            Session = Get<UserSession>();
        }

        public IAuthenticationService Authentication { get; }
        public UserSession Session { get; }

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
                var (hash, salt) = PasswordSecurity.Hash(AdminPassword);
                var administrator = new UserAccount
                {
                    Username = AdminUsername,
                    DisplayName = "Batch B Administrator",
                    PasswordHash = hash,
                    PasswordSalt = salt,
                    Role = UserRole.Administrator,
                    IsActive = true,
                    FailedLoginCount = 2,
                    CreatedUtc = UtcNow
                };
                db.UserAccounts.Add(administrator);
                await db.SaveChangesAsync();
                scope.ServiceProvider.GetRequiredService<UserSession>()
                    .SignIn(administrator);
            }

            return new(connection, provider, scope, factory);
        }

        public T Get<T>() where T : notnull =>
            scope.ServiceProvider.GetRequiredService<T>();

        public BinTrackerDbContext CreateContext() =>
            factory.CreateDbContext();

        public Task FailAuditWritesAsync() => ExecuteAsync(
            "CREATE TRIGGER fail_audit_insert BEFORE INSERT ON AuditEvents " +
            "BEGIN SELECT RAISE(ABORT, 'forced audit failure'); END;");

        public Task AllowAuditWritesAsync() =>
            ExecuteAsync("DROP TRIGGER fail_audit_insert;");

        public async Task SetMaximumFailedLoginsAsync(int maximum)
        {
            await using var db = await factory.CreateDbContextAsync();
            var settings = await db.ApplicationSettings.SingleAsync(x => x.Id == 1);
            settings.MaxFailedLoginAttempts = maximum;
            await db.SaveChangesAsync();
        }

        private async Task ExecuteAsync(string sql)
        {
            await using var command = connection.CreateCommand();
            command.CommandText = sql;
            await command.ExecuteNonQueryAsync();
        }

        public async ValueTask DisposeAsync()
        {
            await scope.DisposeAsync();
            await provider.DisposeAsync();
            await connection.DisposeAsync();
        }

        private sealed class FixedClock : IBusinessClock
        {
            public DateTime UtcNow => Harness.UtcNow;
            public DateTime LocalNow => Harness.UtcNow;
            public DateOnly Today => DateOnly.FromDateTime(Harness.UtcNow);
            public string TimeZoneId => "Test/Fixed";
        }
    }
}
