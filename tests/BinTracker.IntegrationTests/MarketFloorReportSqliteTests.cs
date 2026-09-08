using BinTracker.Core;
using BinTracker.Data;
using BinTracker.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BinTracker.IntegrationTests;

public sealed class MarketFloorReportSqliteTests
{
    [Fact]
    public async Task Selected_date_combines_opening_position_and_same_day_activity_by_source()
    {
        await using var h = await Harness.CreateAsync();
        var reportDate = new DateOnly(2026, 9, 8);
        var account = await h.AddCustomerAsync("ZULU", "Zulu Account", CustomerType.Account);
        var cash = await h.AddCustomerAsync(null, "Alpha Cash", CustomerType.CashCod);

        await h.AddMovementsAsync(
            Movement(account, 1, reportDate.AddDays(-1), MovementType.Out,
                MovementSource.Manual, 4),
            Movement(account, 1, reportDate, MovementType.Out,
                MovementSource.Adjustment, 2),
            Movement(account, 1, reportDate, MovementType.Out,
                MovementSource.Manual, 3),
            Movement(account, 1, reportDate, MovementType.In,
                MovementSource.ExcelImport, 1),
            Movement(account, 1, reportDate.AddDays(1), MovementType.Out,
                MovementSource.Manual, 50),
            Movement(account, 3, reportDate, MovementType.In,
                MovementSource.Manual, 2),
            Movement(account, 4, reportDate, MovementType.Out,
                MovementSource.Manual, 2),
            Movement(account, 4, reportDate, MovementType.In,
                MovementSource.Manual, 2),
            Movement(account, 5, reportDate.AddDays(-1), MovementType.Out,
                MovementSource.Manual, 5),
            Movement(account, 5, reportDate, MovementType.In,
                MovementSource.Adjustment, 2),
            Movement(cash, 1, reportDate.AddDays(-1), MovementType.In,
                MovementSource.Manual, 3));

        var result = await h.Service.GetAsync(reportDate);

        Assert.Equal(reportDate, result.Date);
        Assert.Equal(
            new[]
            {
                (account, "ZULU", CustomerType.Account, "Blue", 3, 1, 6, 8),
                (account, "ZULU", CustomerType.Account, "Yellow", 0, 2, 0, -2),
                (account, "ZULU", CustomerType.Account, "Bulk", 2, 2, 0, 0)
            },
            result.AccountDaily.Select(x =>
                (x.CustomerId, x.Buyer, x.CustomerType, x.Container,
                    x.Out, x.In, x.BroughtForward, x.Total)));
        Assert.Equal(
            new[]
            {
                (cash, "Alpha Cash", CustomerType.CashCod, "Blue", 0, 0, -3, -3)
            },
            result.CashDaily.Select(x =>
                (x.CustomerId, x.Buyer, x.CustomerType, x.Container,
                    x.Out, x.In, x.BroughtForward, x.Total)));
        Assert.Equal(
            new[] { (account, "ZULU", CustomerType.Account, "Blue", 8) },
            result.AccountOwing.Select(x =>
                (x.CustomerId, x.Buyer, x.CustomerType, x.Container, x.Total)));
        Assert.Equal(
            new[] { (cash, "Alpha Cash", CustomerType.CashCod, "Blue", -3) },
            result.CashOwing.Select(x =>
                (x.CustomerId, x.Buyer, x.CustomerType, x.Container, x.Total)));
        Assert.Equal(
            new[] { (account, "ZULU", CustomerType.Account, "Yellow", -2) },
            result.Credits.Select(x =>
                (x.CustomerId, x.Buyer, x.CustomerType, x.Container, x.Total)));
        Assert.Equal(
            new[] { ("ZULU", "CHEP Pallet", 3) },
            result.SpecialContainers.Select(x => (x.Buyer, x.Container, x.Balance)));
    }

    [Fact]
    public async Task Active_customer_and_container_metadata_rules_preserve_zero_and_unknown_rows()
    {
        await using var h = await Harness.CreateAsync();
        var reportDate = new DateOnly(2026, 9, 8);
        var empty = await h.AddCustomerAsync("EMPTY", "No History", CustomerType.Account);
        var specialOnly = await h.AddCustomerAsync("SPECIAL", "Special Only", CustomerType.CashCod);
        var inactive = await h.AddCustomerAsync("HIDDEN", "Inactive", CustomerType.Account,
            isActive: false);
        var active = await h.AddCustomerAsync("ACTIVE", "Active", CustomerType.Account);

        await h.SetContainerActiveAsync(2, false);
        await h.AddMovementsAsync(
            Movement(inactive, 1, reportDate, MovementType.Out, MovementSource.Manual, 9),
            Movement(active, 2, reportDate, MovementType.Out, MovementSource.Manual, 4),
            Movement(specialOnly, 5, reportDate, MovementType.Out, MovementSource.Manual, 6));

        var result = await h.Service.GetAsync(reportDate);

        Assert.Equal(
            new[]
            {
                (active, "ACTIVE", "Unknown", 4),
                (empty, "EMPTY", "Blue", 0)
            },
            result.AccountDaily.Select(x =>
                (x.CustomerId, x.Buyer, x.Container, x.Total)));
        Assert.Equal(
            new[] { (specialOnly, "SPECIAL", "Blue", 0) },
            result.CashDaily.Select(x =>
                (x.CustomerId, x.Buyer, x.Container, x.Total)));
        Assert.Equal(
            new[] { ("SPECIAL", "CHEP Pallet", 6) },
            result.SpecialContainers.Select(x => (x.Buyer, x.Container, x.Balance)));
        Assert.DoesNotContain(result.AccountDaily, x => x.CustomerId == inactive);
        Assert.DoesNotContain(result.AccountOwing, x => x.CustomerId == inactive);
    }

    [Fact]
    public async Task Effective_alpha8_correction_and_ordinary_reversal_are_reflected_once()
    {
        await using var h = await Harness.CreateAsync(UserRole.Administrator);
        var originalCustomer = await h.AddCustomerAsync(
            "ORIGINAL", "Original Buyer", CustomerType.Account);
        var correctedCustomer = await h.AddCustomerAsync(
            "CORRECTED", "Corrected Buyer", CustomerType.Account);
        var original = await h.AddMovementAsync(Movement(
            originalCustomer, 1, new DateOnly(2026, 9, 1), MovementType.Out,
            MovementSource.Manual, 10));

        var correction = Assert.Single((await h.Corrections.CorrectAsync(new(
            Guid.NewGuid(), original, new DateOnly(2026, 9, 2), correctedCustomer, 3,
            MovementType.In, 4, null, null, "correct every operational dimension"))).Lines);

        var correctedDay = await h.Service.GetAsync(new DateOnly(2026, 9, 2));
        var corrected = Assert.Single(correctedDay.AccountDaily,
            x => x.CustomerId == correctedCustomer && x.Container == "Yellow");
        Assert.Equal((0, 4, 0, -4),
            (corrected.Out, corrected.In, corrected.BroughtForward, corrected.Total));
        Assert.DoesNotContain(correctedDay.AccountDaily,
            x => x.CustomerId == originalCustomer && x.Total != 0);

        await h.Corrections.ReverseAsync(new(
            Guid.NewGuid(), correction.ReplacementMovementId, "movement did not occur"));
        var reversalDay = await h.Service.GetAsync(Harness.Today);
        var reversed = Assert.Single(reversalDay.AccountDaily,
            x => x.CustomerId == correctedCustomer && x.Container == "Yellow");
        Assert.Equal((4, 0, -4, 0),
            (reversed.Out, reversed.In, reversed.BroughtForward, reversed.Total));
        Assert.DoesNotContain(reversalDay.Credits, x => x.CustomerId == correctedCustomer);
    }

    [Fact]
    public async Task Future_selected_date_is_not_clamped_by_get_async()
    {
        await using var h = await Harness.CreateAsync();
        var customer = await h.AddCustomerAsync("FUTURE", "Future Buyer", CustomerType.Account);
        var futureDate = Harness.Today.AddDays(2);
        await h.AddMovementAsync(Movement(
            customer, 1, futureDate, MovementType.Out, MovementSource.Manual, 7));

        var result = await h.Service.GetAsync(futureDate);

        Assert.Equal(futureDate, result.Date);
        Assert.Equal((7, 0, 0, 7),
            (Assert.Single(result.AccountDaily).Out,
                Assert.Single(result.AccountDaily).In,
                Assert.Single(result.AccountDaily).BroughtForward,
                Assert.Single(result.AccountDaily).Total));
    }

    [Fact]
    public async Task Alpha8_final_total_uses_unchecked_integer_arithmetic()
    {
        await using var h = await Harness.CreateAsync();
        var customer = await h.AddCustomerAsync("OVERFLOW", "Overflow", CustomerType.Account);
        await h.AddMovementsAsync(
            Movement(customer, 1, new DateOnly(2026, 9, 1), MovementType.Out,
                MovementSource.Manual, int.MaxValue),
            Movement(customer, 1, new DateOnly(2026, 9, 2), MovementType.Out,
                MovementSource.Manual, 1));

        var result = await h.Service.GetAsync(new DateOnly(2026, 9, 2));

        var row = Assert.Single(result.AccountDaily);
        Assert.Equal((int.MaxValue, 1, 0, int.MinValue),
            (row.BroughtForward, row.Out, row.In, row.Total));
        Assert.Empty(result.AccountOwing);
        Assert.Equal(int.MinValue, Assert.Single(result.Credits).Total);
    }

    private static BinMovement Movement(
        int customerId,
        int containerTypeId,
        DateOnly date,
        MovementType type,
        MovementSource source,
        int quantity) =>
        new()
        {
            CustomerId = customerId,
            ContainerTypeId = containerTypeId,
            MovementDate = date,
            MovementType = type,
            Source = source,
            Quantity = quantity,
            CreatedBy = "market-floor-characterization",
            CreatedUtc = Harness.UtcNow
        };

    private sealed class Harness : IAsyncDisposable
    {
        private readonly SqliteConnection connection;
        private readonly ServiceProvider provider;

        private Harness(
            SqliteConnection connection,
            ServiceProvider provider,
            IDbContextFactory<BinTrackerDbContext> factory)
        {
            this.connection = connection;
            this.provider = provider;
            Factory = factory;
            Service = provider.GetRequiredService<IMarketFloorReportService>();
            Corrections = provider.GetRequiredService<IMovementCorrectionService>();
        }

        public static DateOnly Today => new(2026, 9, 8);
        public static DateTime UtcNow => new(2026, 9, 8, 1, 2, 3, DateTimeKind.Utc);
        public IDbContextFactory<BinTrackerDbContext> Factory { get; }
        public IMarketFloorReportService Service { get; }
        public IMovementCorrectionService Corrections { get; }

        public static async Task<Harness> CreateAsync(UserRole? signedInRole = null)
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();
            var services = new ServiceCollection();
            services.AddSingleton<IBusinessClock>(new FixedClock());
            services.AddDbContextFactory<BinTrackerDbContext>(
                options => options.UseSqlite(connection));
            services.AddBinTrackerServices();
            var provider = services.BuildServiceProvider();
            var factory = provider.GetRequiredService<IDbContextFactory<BinTrackerDbContext>>();

            await using (var db = await factory.CreateDbContextAsync())
            {
                await db.Database.EnsureCreatedAsync();
                await DatabaseSetup.InitializeSqliteAsync(db);
                if (signedInRole is { } role)
                {
                    var user = new UserAccount
                    {
                        Username = "market-floor-user",
                        DisplayName = "Market Floor User",
                        PasswordHash = "x",
                        PasswordSalt = "x",
                        Role = role,
                        IsActive = true
                    };
                    db.Add(user);
                    await db.SaveChangesAsync();
                    provider.GetRequiredService<UserSession>().SignIn(user);
                }
            }

            return new(connection, provider, factory);
        }

        public async Task<int> AddCustomerAsync(
            string? code,
            string name,
            CustomerType type,
            bool isActive = true)
        {
            await using var db = await Factory.CreateDbContextAsync();
            var customer = new Customer
            {
                CustomerCode = code,
                Name = name,
                CustomerType = type,
                IsActive = isActive
            };
            db.Add(customer);
            await db.SaveChangesAsync();
            return customer.Id;
        }

        public async Task<long> AddMovementAsync(BinMovement movement)
        {
            await using var db = await Factory.CreateDbContextAsync();
            db.Add(movement);
            await db.SaveChangesAsync();
            return movement.Id;
        }

        public async Task AddMovementsAsync(params BinMovement[] movements)
        {
            await using var db = await Factory.CreateDbContextAsync();
            db.AddRange(movements);
            await db.SaveChangesAsync();
        }

        public async Task SetContainerActiveAsync(int containerTypeId, bool isActive)
        {
            await using var db = await Factory.CreateDbContextAsync();
            var container = await db.ContainerTypes.SingleAsync(x => x.Id == containerTypeId);
            container.IsActive = isActive;
            await db.SaveChangesAsync();
        }

        public async ValueTask DisposeAsync()
        {
            await provider.DisposeAsync();
            await connection.DisposeAsync();
        }

        private sealed class FixedClock : IBusinessClock
        {
            public DateTime UtcNow => Harness.UtcNow;
            public DateTime LocalNow => UtcNow;
            public DateOnly Today => Harness.Today;
            public string TimeZoneId => "UTC";
        }
    }
}
