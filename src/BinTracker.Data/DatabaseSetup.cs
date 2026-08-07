using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace BinTracker.Data;

public static class DatabaseSetup
{
    private static DatabaseSettings? _settings;

    public static DatabaseSettings Settings => _settings ??= DatabaseConfiguration.Load();

    // Kept for compatibility with existing code and backup work. SQLite is the active
    // provider in this alpha; PostgreSQL will be enabled before multi-user deployment.
    public static string AppFolder => DatabaseConfiguration.AppFolder;
    public static string DatabasePath => DatabaseConfiguration.DefaultSqlitePath;
    public static string ConnectionString =>
        Settings.ConnectionString ?? $"Data Source={DatabasePath};Cache=Shared";

    public static string StatusText => DatabaseConfiguration.GetStatusText(Settings);

    public static IServiceCollection AddBinTrackerData(this IServiceCollection services)
    {
        var settings = Settings;

        services.AddDbContextFactory<BinTrackerDbContext>(options =>
            ConfigureProvider(options, settings));

        services.AddDbContext<BinTrackerDbContext>(options =>
            ConfigureProvider(options, settings));

        return services;
    }

    private static void ConfigureProvider(
        DbContextOptionsBuilder options,
        DatabaseSettings settings)
    {
        switch (settings.Provider)
        {
            case DatabaseProvider.Sqlite:
                options.UseSqlite(
                    settings.ConnectionString ??
                    $"Data Source={DatabaseConfiguration.DefaultSqlitePath};Cache=Shared");
                break;

            case DatabaseProvider.PostgreSql:
                throw new NotSupportedException(
                    "PostgreSQL support is prepared but not enabled in this alpha. " +
                    "SQLite remains active until the multi-user deployment milestone.");

            default:
                throw new NotSupportedException(
                    $"Database provider '{settings.Provider}' is not supported.");
        }
    }

    public static async Task InitializeAsync(IServiceProvider services)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<BinTrackerDbContext>();

        switch (Settings.Provider)
        {
            case DatabaseProvider.Sqlite:
                await InitializeSqliteAsync(db);
                break;

            case DatabaseProvider.PostgreSql:
                throw new NotSupportedException(
                    "PostgreSQL initialisation will be enabled at the multi-user milestone.");

            default:
                throw new NotSupportedException(
                    $"Database provider '{Settings.Provider}' is not supported.");
        }
    }

    private static async Task InitializeSqliteAsync(BinTrackerDbContext db)
    {
        await db.Database.EnsureCreatedAsync();

        // These statements upgrade older alpha SQLite databases without deleting
        // customer/container/movement data. Keeping this SQLite-specific work here
        // prevents provider details leaking into services or the UI.
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

        // Business schema upgrades for customer management and reminder automation groundwork.
        await AddColumnIfMissingAsync(db, "Customers", "MobileNumber", "TEXT NULL");
        await AddColumnIfMissingAsync(db, "Customers", "CustomerType", "INTEGER NOT NULL DEFAULT 0");
        await AddColumnIfMissingAsync(db, "Customers", "Notes", "TEXT NULL");
        await AddColumnIfMissingAsync(db, "Customers", "AllowEmailReminders", "INTEGER NOT NULL DEFAULT 1");
        await AddColumnIfMissingAsync(db, "Customers", "AllowSmsReminders", "INTEGER NOT NULL DEFAULT 1");
        await AddColumnIfMissingAsync(db, "Customers", "ReminderOptOut", "INTEGER NOT NULL DEFAULT 0");
        await AddColumnIfMissingAsync(db, "Customers", "CreatedByUserId", "INTEGER NULL");
        await AddColumnIfMissingAsync(db, "Customers", "UpdatedByUserId", "INTEGER NULL");

        // Rename the original seeded type without affecting its stable Id or movement history.
        await db.Database.ExecuteSqlRawAsync(
            "UPDATE ContainerTypes SET Name = 'Blue Bin', Description = 'Standard blue reusable bin' WHERE Id = 1 AND Name = 'Standard Bin';");

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
                CONSTRAINT FK_ReminderDeliveries_Customers_CustomerId FOREIGN KEY (CustomerId) REFERENCES Customers (Id) ON DELETE RESTRICT
            );
            """);
        await db.Database.ExecuteSqlRawAsync(
            "CREATE INDEX IF NOT EXISTS IX_ReminderDeliveries_CustomerId_CreatedUtc ON ReminderDeliveries (CustomerId, CreatedUtc);");

        // Enforce case-insensitive customer-code uniqueness at the database level when
        // the existing alpha data has no collisions. If an older test database already
        // contains e.g. "Albury" and "ALBURY", the service layer still blocks any new
        // duplicates and the user can rename one of the existing test records first.
        var duplicateCodeCount = await db.Database.SqlQueryRaw<int>("""
            SELECT COUNT(*) AS Value
            FROM (
                SELECT UPPER(TRIM(CustomerCode))
                FROM Customers
                WHERE CustomerCode IS NOT NULL AND TRIM(CustomerCode) <> ''
                GROUP BY UPPER(TRIM(CustomerCode))
                HAVING COUNT(*) > 1
            );
            """).SingleAsync();

        if (duplicateCodeCount == 0)
        {
            await db.Database.ExecuteSqlRawAsync(
                "CREATE UNIQUE INDEX IF NOT EXISTS IX_Customers_CustomerCode_NoCase ON Customers (CustomerCode COLLATE NOCASE);");
        }
    }

    private static async Task AddColumnIfMissingAsync(
        BinTrackerDbContext db, string table, string column, string definition)
    {
        await using var connection = db.Database.GetDbConnection();
        if (connection.State != System.Data.ConnectionState.Open)
            await connection.OpenAsync();

        await using var check = connection.CreateCommand();
        check.CommandText = $"PRAGMA table_info({table});";
        await using var reader = await check.ExecuteReaderAsync();
        var exists = false;
        while (await reader.ReadAsync())
        {
            if (string.Equals(reader.GetString(1), column, StringComparison.OrdinalIgnoreCase))
            {
                exists = true;
                break;
            }
        }
        await reader.CloseAsync();

        if (!exists)
            await db.Database.ExecuteSqlRawAsync($"ALTER TABLE {table} ADD COLUMN {column} {definition};");
    }

}
