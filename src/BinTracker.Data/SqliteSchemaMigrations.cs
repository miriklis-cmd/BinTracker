using Microsoft.EntityFrameworkCore;

namespace BinTracker.Data;

internal sealed record SqliteSchemaMigration(
    int Version,
    string Name,
    Func<BinTrackerDbContext, Task> ApplyAsync);

internal static class SqliteSchemaMigrations
{
    public static IReadOnlyList<SqliteSchemaMigration> All { get; } =
    [
        new(1, "Security and audit tables", ApplyV1Async),
        new(2, "Customer communication fields", ApplyV2Async),
        new(3, "Reminder delivery history", ApplyV3Async),
        new(4, "Customer type and Blue Bin terminology", ApplyV4Async),
        new(5, "Case-insensitive customer code index", ApplyV5Async),
        new(6, "Password self-service and account lockout", ApplyV6Async)
    ];

    private static async Task ApplyV1Async(BinTrackerDbContext db)
    {
        await db.Database.ExecuteSqlRawAsync("""
            CREATE TABLE IF NOT EXISTS UserAccounts (
                Id INTEGER NOT NULL CONSTRAINT PK_UserAccounts PRIMARY KEY AUTOINCREMENT,
                Username TEXT NOT NULL,
                DisplayName TEXT NOT NULL,
                PasswordHash TEXT NOT NULL,
                PasswordSalt TEXT NOT NULL,
                Role INTEGER NOT NULL,
                IsActive INTEGER NOT NULL,
                MustChangePassword INTEGER NOT NULL,
                CreatedUtc TEXT NOT NULL,
                CreatedByUserId INTEGER NULL,
                LastLoginUtc TEXT NULL
            );
            """);

        await db.Database.ExecuteSqlRawAsync(
            "CREATE UNIQUE INDEX IF NOT EXISTS IX_UserAccounts_Username ON UserAccounts (Username);");

        await db.Database.ExecuteSqlRawAsync("""
            CREATE TABLE IF NOT EXISTS AuditEvents (
                Id INTEGER NOT NULL CONSTRAINT PK_AuditEvents PRIMARY KEY AUTOINCREMENT,
                TimestampUtc TEXT NOT NULL,
                UserId INTEGER NULL,
                Username TEXT NOT NULL,
                Action TEXT NOT NULL,
                EntityType TEXT NOT NULL,
                EntityId TEXT NULL,
                Description TEXT NOT NULL,
                BeforeValues TEXT NULL,
                AfterValues TEXT NULL,
                ComputerName TEXT NOT NULL,
                SessionId TEXT NOT NULL,
                Succeeded INTEGER NOT NULL
            );
            """);

        await db.Database.ExecuteSqlRawAsync(
            "CREATE INDEX IF NOT EXISTS IX_AuditEvents_TimestampUtc ON AuditEvents (TimestampUtc);");

        await db.Database.ExecuteSqlRawAsync(
            "CREATE INDEX IF NOT EXISTS IX_AuditEvents_UserId_TimestampUtc ON AuditEvents (UserId, TimestampUtc);");
    }

    private static async Task ApplyV2Async(BinTrackerDbContext db)
    {
        await AddColumnIfMissingAsync(db, "Customers", "MobileNumber", "TEXT NULL");
        await AddColumnIfMissingAsync(db, "Customers", "Notes", "TEXT NULL");
        await AddColumnIfMissingAsync(db, "Customers", "AllowEmailReminders", "INTEGER NOT NULL DEFAULT 1");
        await AddColumnIfMissingAsync(db, "Customers", "AllowSmsReminders", "INTEGER NOT NULL DEFAULT 1");
        await AddColumnIfMissingAsync(db, "Customers", "ReminderOptOut", "INTEGER NOT NULL DEFAULT 0");
    }

    private static async Task ApplyV3Async(BinTrackerDbContext db)
    {
        await db.Database.ExecuteSqlRawAsync("""
            CREATE TABLE IF NOT EXISTS ReminderDeliveries (
                Id INTEGER NOT NULL CONSTRAINT PK_ReminderDeliveries PRIMARY KEY AUTOINCREMENT,
                CustomerId INTEGER NOT NULL,
                Channel INTEGER NOT NULL,
                Status INTEGER NOT NULL,
                Destination TEXT NOT NULL,
                Subject TEXT NOT NULL,
                MessageBody TEXT NOT NULL,
                ProviderMessageId TEXT NULL,
                ProviderResponse TEXT NULL,
                CreatedUtc TEXT NOT NULL,
                SentUtc TEXT NULL,
                InitiatedByUserId INTEGER NULL,
                OutstandingSnapshotJson TEXT NULL,
                CONSTRAINT FK_ReminderDeliveries_Customers_CustomerId
                    FOREIGN KEY (CustomerId) REFERENCES Customers (Id) ON DELETE RESTRICT
            );
            """);

        await db.Database.ExecuteSqlRawAsync(
            "CREATE INDEX IF NOT EXISTS IX_ReminderDeliveries_CustomerId_CreatedUtc ON ReminderDeliveries (CustomerId, CreatedUtc);");
    }

    private static async Task ApplyV4Async(BinTrackerDbContext db)
    {
        await AddColumnIfMissingAsync(
            db,
            "Customers",
            "CustomerType",
            "INTEGER NOT NULL DEFAULT 0");

        await db.Database.ExecuteSqlRawAsync("""
            UPDATE ContainerTypes
            SET Name = 'Blue Bin',
                Description = 'Standard blue reusable bin'
            WHERE Id = 1
              AND Name = 'Standard Bin';
            """);
    }

    private static async Task ApplyV5Async(BinTrackerDbContext db)
    {
        // Do not fail an existing alpha database that already contains test duplicates
        // such as "Albury" and "ALBURY". The application service blocks all NEW
        // case-insensitive duplicates. Once old collisions are corrected, the next
        // launch creates the database-level NOCASE constraint.
        var duplicateGroups = await db.Customers
            .AsNoTracking()
            .Where(x => x.CustomerCode != null && x.CustomerCode != "")
            .GroupBy(x => x.CustomerCode!.ToUpper())
            .Where(g => g.Count() > 1)
            .CountAsync();

        if (duplicateGroups == 0)
        {
            await db.Database.ExecuteSqlRawAsync(
                "CREATE UNIQUE INDEX IF NOT EXISTS IX_Customers_CustomerCode_NoCase ON Customers (CustomerCode COLLATE NOCASE);");
        }
    }


    private static async Task ApplyV6Async(BinTrackerDbContext db)
    {
        await AddUserColumnIfMissingAsync(db, "PasswordChangedUtc", "TEXT NULL");
        await AddUserColumnIfMissingAsync(db, "FailedLoginCount", "INTEGER NOT NULL DEFAULT 0");
        await AddUserColumnIfMissingAsync(db, "IsLocked", "INTEGER NOT NULL DEFAULT 0");
        await AddUserColumnIfMissingAsync(db, "LockedUtc", "TEXT NULL");
        await AddSettingsColumnIfMissingAsync(db, "MaxFailedLoginAttempts", "INTEGER NOT NULL DEFAULT 5");
    }

    private static async Task AddColumnIfMissingAsync(
        BinTrackerDbContext db,
        string table,
        string column,
        string definition)
    {
        // These identifiers come only from our internal migration definitions.
        // Validate against a strict allow-list before composing SQLite DDL.
        var allowedTables = new HashSet<string>(StringComparer.Ordinal)
        {
            "Customers"
        };

        var allowedColumns = new HashSet<string>(StringComparer.Ordinal)
        {
            "MobileNumber",
            "Notes",
            "AllowEmailReminders",
            "AllowSmsReminders",
            "ReminderOptOut",
            "CustomerType"
        };

        if (!allowedTables.Contains(table) || !allowedColumns.Contains(column))
            throw new InvalidOperationException("Unsafe schema identifier supplied.");

        var existing = await db.Database
            .SqlQueryRaw<string>(
                "SELECT name AS Value FROM pragma_table_info('Customers') WHERE name = {0}",
                column)
            .ToListAsync();

        if (existing.Count == 0)
        {
            var sql = column switch
            {
                "MobileNumber" => "ALTER TABLE Customers ADD COLUMN MobileNumber TEXT NULL;",
                "Notes" => "ALTER TABLE Customers ADD COLUMN Notes TEXT NULL;",
                "AllowEmailReminders" => "ALTER TABLE Customers ADD COLUMN AllowEmailReminders INTEGER NOT NULL DEFAULT 1;",
                "AllowSmsReminders" => "ALTER TABLE Customers ADD COLUMN AllowSmsReminders INTEGER NOT NULL DEFAULT 1;",
                "ReminderOptOut" => "ALTER TABLE Customers ADD COLUMN ReminderOptOut INTEGER NOT NULL DEFAULT 0;",
                "CustomerType" => "ALTER TABLE Customers ADD COLUMN CustomerType INTEGER NOT NULL DEFAULT 0;",
                _ => throw new InvalidOperationException("Unsupported customer schema column.")
            };

            await db.Database.ExecuteSqlRawAsync(sql);
        }
    }

    private static async Task AddUserColumnIfMissingAsync(
        BinTrackerDbContext db,
        string column,
        string definition)
    {
        var existing = await db.Database
            .SqlQueryRaw<string>(
                "SELECT name AS Value FROM pragma_table_info('UserAccounts') WHERE name = {0}",
                column)
            .ToListAsync();

        if (existing.Count != 0)
            return;

        var sql = column switch
        {
            "PasswordChangedUtc" => "ALTER TABLE UserAccounts ADD COLUMN PasswordChangedUtc TEXT NULL;",
            "FailedLoginCount" => "ALTER TABLE UserAccounts ADD COLUMN FailedLoginCount INTEGER NOT NULL DEFAULT 0;",
            "IsLocked" => "ALTER TABLE UserAccounts ADD COLUMN IsLocked INTEGER NOT NULL DEFAULT 0;",
            "LockedUtc" => "ALTER TABLE UserAccounts ADD COLUMN LockedUtc TEXT NULL;",
            _ => throw new InvalidOperationException("Unsupported user schema column.")
        };

        await db.Database.ExecuteSqlRawAsync(sql);
    }

    private static async Task AddSettingsColumnIfMissingAsync(
        BinTrackerDbContext db,
        string column,
        string definition)
    {
        var existing = await db.Database
            .SqlQueryRaw<string>(
                "SELECT name AS Value FROM pragma_table_info('ApplicationSettings') WHERE name = {0}",
                column)
            .ToListAsync();

        if (existing.Count != 0)
            return;

        if (column != "MaxFailedLoginAttempts")
            throw new InvalidOperationException("Unsupported settings schema column.");

        await db.Database.ExecuteSqlRawAsync(
            "ALTER TABLE ApplicationSettings ADD COLUMN MaxFailedLoginAttempts INTEGER NOT NULL DEFAULT 5;");
    }
}
