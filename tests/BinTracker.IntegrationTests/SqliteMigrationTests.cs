using Xunit;
using BinTracker.Core;
using BinTracker.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace BinTracker.IntegrationTests;

public sealed class SqliteMigrationTests
{
    [Fact]
    public async Task Alpha6_migrations_complete_on_fresh_database()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<BinTrackerDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var db = new BinTrackerDbContext(options);
        await db.Database.EnsureCreatedAsync();

        await DatabaseSetup.InitializeSqliteAsync(db);

        var version = await DatabaseSetup.GetSchemaVersionAsync(db);

        Assert.Equal(6, version);
    }

    [Fact]
    public async Task Existing_case_only_customer_duplicates_do_not_crash_upgrade()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<BinTrackerDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var db = new BinTrackerDbContext(options);
        await db.Database.EnsureCreatedAsync();

        // Drop the original case-sensitive unique index so this test can model an
        // older alpha database that already contains a case-only collision.
        await db.Database.ExecuteSqlRawAsync(
            "DROP INDEX IF EXISTS IX_Customers_CustomerCode;");

        db.Customers.Add(new Customer
        {
            CustomerCode = "Albury",
            Name = "First test customer"
        });

        db.Customers.Add(new Customer
        {
            CustomerCode = "ALBURY",
            Name = "Second test customer"
        });

        await db.SaveChangesAsync();

        await DatabaseSetup.InitializeSqliteAsync(db);

        Assert.Equal(6, await DatabaseSetup.GetSchemaVersionAsync(db));
        Assert.Equal(2, await db.Customers.CountAsync());
    }

    [Fact]
    public async Task Blue_bin_name_is_applied_during_upgrade()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<BinTrackerDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var db = new BinTrackerDbContext(options);
        await db.Database.EnsureCreatedAsync();

        var bin = await db.ContainerTypes.SingleAsync(x => x.Id == 1);
        bin.Name = "Standard Bin";
        await db.SaveChangesAsync();

        await DatabaseSetup.InitializeSqliteAsync(db);

        Assert.Equal(
            "Blue Bin",
            (await db.ContainerTypes.AsNoTracking().SingleAsync(x => x.Id == 1)).Name);
    }

    [Fact]
    public async Task Security_polish_migration_adds_lockout_columns()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<BinTrackerDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var db = new BinTrackerDbContext(options);
        await db.Database.EnsureCreatedAsync();
        await DatabaseSetup.InitializeSqliteAsync(db);

        var columns = await db.Database
            .SqlQueryRaw<string>(
                "SELECT name AS Value FROM pragma_table_info('UserAccounts')")
            .ToListAsync();

        Assert.Contains("FailedLoginCount", columns);
        Assert.Contains("IsLocked", columns);
        Assert.Contains("LockedUtc", columns);
        Assert.Contains("PasswordChangedUtc", columns);
    }
}
