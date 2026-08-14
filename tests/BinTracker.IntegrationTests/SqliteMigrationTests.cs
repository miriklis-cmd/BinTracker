using Xunit;
using BinTracker.Core;
using BinTracker.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace BinTracker.IntegrationTests;

public sealed class SqliteMigrationTests
{
    [Fact]
    public async Task All_migrations_complete_on_fresh_database()
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

        Assert.Equal(DatabaseSetup.LatestSchemaVersion, version);
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

        Assert.Equal(DatabaseSetup.LatestSchemaVersion, await DatabaseSetup.GetSchemaVersionAsync(db));
        Assert.Equal(2, await db.Customers.CountAsync());
    }

    [Fact]
    public async Task Container_type_master_data_migration_is_applied()
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
                "SELECT name AS Value FROM pragma_table_info('ContainerTypes')")
            .ToListAsync();

        Assert.Contains("ShortCode", columns);
        Assert.Contains("SystemCode", columns);
        Assert.Contains("Notes", columns);
        Assert.Contains("IsSpecialFloorReportContainer", columns);
        Assert.Contains("DashboardColour", columns);

        var chep = await db.ContainerTypes.AsNoTracking()
            .SingleAsync(x => x.Id == 5);

        Assert.Equal("CHEP", chep.ShortCode);
        Assert.Equal("CHEP_PALLET", chep.SystemCode);
        Assert.True(chep.IsSpecialFloorReportContainer);
    }


    [Fact]
    public async Task Business_information_columns_are_added_by_latest_migration()
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
                "SELECT name AS Value FROM pragma_table_info('ApplicationSettings')")
            .ToListAsync();

        Assert.Contains("BusinessName", columns);
        Assert.Contains("TradingName", columns);
        Assert.Contains("Abn", columns);
        Assert.Contains("Address", columns);
        Assert.Contains("Phone", columns);
        Assert.Contains("Email", columns);
        Assert.Contains("DefaultReportHeader", columns);

        Assert.Equal(
            DatabaseSetup.LatestSchemaVersion,
            await DatabaseSetup.GetSchemaVersionAsync(db));
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

    [Fact]
    public async Task Import_movement_provenance_migration_adds_fk_and_index()
    {
        await using var connection =
            new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options =
            new DbContextOptionsBuilder<BinTrackerDbContext>()
                .UseSqlite(connection)
                .Options;

        await using var db =
            new BinTrackerDbContext(options);

        await db.Database.EnsureCreatedAsync();
        await DatabaseSetup.InitializeSqliteAsync(db);

        var columns = await db.Database
            .SqlQueryRaw<string>(
                "SELECT name AS Value FROM pragma_table_info('BinMovements')")
            .ToListAsync();

        Assert.Contains("ImportRunId", columns);

        var indexes = await db.Database
            .SqlQueryRaw<string>(
                "SELECT name AS Value FROM pragma_index_list('BinMovements')")
            .ToListAsync();

        Assert.Contains(
            "IX_BinMovements_ImportRunId",
            indexes);

        var foreignTables = await db.Database
            .SqlQueryRaw<string>(
                "SELECT [table] AS Value FROM pragma_foreign_key_list('BinMovements')")
            .ToListAsync();

        Assert.Contains(
            "ImportRuns",
            foreignTables);

        Assert.Equal(
            DatabaseSetup.LatestSchemaVersion,
            await DatabaseSetup.GetSchemaVersionAsync(db));
    }

    [Fact]
    public async Task Import_movement_provenance_migration_backfills_legacy_import_references_only()
    {
        await using var connection =
            new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options =
            new DbContextOptionsBuilder<BinTrackerDbContext>()
                .UseSqlite(connection)
                .Options;

        await using var db =
            new BinTrackerDbContext(options);

        await db.Database.EnsureCreatedAsync();
        await DatabaseSetup.InitializeSqliteAsync(db);

        var run = new ImportRun
        {
            SourceFileName = "legacy.xlsx",
            SourceFullPath = "legacy.xlsx",
            SourceSha256 = new string('A', 64),
            SourceLength = 10,
            SourceLastWriteUtc = DateTime.UtcNow,
            StartedUtc = DateTime.UtcNow,
            CompletedUtc = DateTime.UtcNow,
            Status = "Completed",
            Username = "admin",
            SessionId = "test"
        };

        db.ImportRuns.Add(run);

        var customer = new Customer
        {
            CustomerCode = "LEGACY",
            Name = "Legacy Import Customer"
        };

        db.Customers.Add(customer);
        await db.SaveChangesAsync();

        // Simulate rows created by alpha.19.x before the relational FK was
        // populated. EF knows the new property, but we intentionally leave it
        // null and use only the historical IMPORT-<run id> reference.
        db.BinMovements.AddRange(
            new BinMovement
            {
                MovementDate = new DateOnly(2026, 8, 13),
                MovementType = MovementType.Out,
                Source = MovementSource.Adjustment,
                CustomerId = customer.Id,
                ContainerTypeId = 1,
                Quantity = 5,
                ReferenceNumber = $"IMPORT-{run.Id}",
                ImportRunId = null
            },
            new BinMovement
            {
                MovementDate = new DateOnly(2026, 8, 13),
                MovementType = MovementType.Out,
                Source = MovementSource.Manual,
                CustomerId = customer.Id,
                ContainerTypeId = 1,
                Quantity = 1,
                ReferenceNumber = $"IMPORT-{run.Id}",
                ImportRunId = null
            });

        await db.SaveChangesAsync();

        // Apply the same guarded backfill statement used by migration V10.
        await db.Database.ExecuteSqlRawAsync("""
            UPDATE BinMovements
            SET ImportRunId =
                CAST(substr(ReferenceNumber, 8) AS INTEGER)
            WHERE ImportRunId IS NULL
              AND Source IN (2, 3)
              AND ReferenceNumber GLOB 'IMPORT-[0-9]*'
              AND ReferenceNumber NOT GLOB 'IMPORT-*[^0-9]*'
              AND EXISTS (
                  SELECT 1
                  FROM ImportRuns
                  WHERE ImportRuns.Id =
                      CAST(substr(BinMovements.ReferenceNumber, 8) AS INTEGER)
              );
            """);

        var rows = await db.BinMovements
            .AsNoTracking()
            .OrderBy(x => x.Id)
            .ToListAsync();

        Assert.Equal(2, rows.Count);
        Assert.Equal(run.Id, rows[0].ImportRunId);
        Assert.Null(rows[1].ImportRunId);
    }


    [Fact]
    public async Task Import_cutover_replacement_migration_adds_metadata()
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
                "SELECT name AS Value FROM pragma_table_info('ImportRuns')")
            .ToListAsync();

        Assert.Contains("CutoverDate", columns);
        Assert.Contains("ReplacesImportRunId", columns);

        var indexes = await db.Database
            .SqlQueryRaw<string>(
                "SELECT name AS Value FROM pragma_index_list('ImportRuns')")
            .ToListAsync();

        Assert.Contains("IX_ImportRuns_CutoverDate", indexes);
        Assert.Contains("IX_ImportRuns_ReplacesImportRunId", indexes);
    }


    [Fact]
    public async Task Import_correction_provenance_migration_adds_json_snapshot_column()
    {
        await using var connection =
            new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<BinTrackerDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var db = new BinTrackerDbContext(options);
        await db.Database.EnsureCreatedAsync();
        await DatabaseSetup.InitializeSqliteAsync(db);

        var columns = await db.Database
            .SqlQueryRaw<string>(
                "SELECT name AS Value FROM pragma_table_info('ImportRuns')")
            .ToListAsync();

        Assert.Contains("CorrectionChangesJson", columns);
    }

}
