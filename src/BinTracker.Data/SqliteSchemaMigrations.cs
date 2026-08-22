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
        new(6, "Password self-service and account lockout", ApplyV6Async),
        new(7, "Container type master data", ApplyV7Async),
        new(8, "Business information master data", ApplyV8Async),
        new(9, "Excel import provenance", ApplyV9Async),
        new(10, "Import movement relational provenance", ApplyV10Async),
        new(11, "Import cutover and replacement chain", ApplyV11Async),
        new(12, "Import correction difference provenance", ApplyV12Async),
        new(13, "Movement correction and reversal linkage", ApplyV13Async)
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

    private static async Task ApplyV7Async(BinTrackerDbContext db)
    {
        await AddContainerColumnIfMissingAsync(db, "ShortCode", "TEXT NOT NULL DEFAULT ''");
        await AddContainerColumnIfMissingAsync(db, "SystemCode", "TEXT NOT NULL DEFAULT ''");
        await AddContainerColumnIfMissingAsync(db, "Notes", "TEXT NULL");
        await AddContainerColumnIfMissingAsync(db, "IsSpecialFloorReportContainer", "INTEGER NOT NULL DEFAULT 0");
        await AddContainerColumnIfMissingAsync(db, "DashboardColour", "TEXT NULL");

        // Existing alpha databases keep the original IDs referenced by every
        // movement. We only enrich those records with stable master-data codes.
        await db.Database.ExecuteSqlRawAsync("""
            UPDATE ContainerTypes SET ShortCode='BLUE',   SystemCode='BLUE_BIN'    WHERE Id=1 AND ShortCode='';
            UPDATE ContainerTypes SET ShortCode='SMALL',  SystemCode='SMALL_BIN'   WHERE Id=2 AND ShortCode='';
            UPDATE ContainerTypes SET ShortCode='YELLOW', SystemCode='YELLOW_BIN'  WHERE Id=3 AND ShortCode='';
            UPDATE ContainerTypes SET ShortCode='BULK',   SystemCode='BULK_BIN'    WHERE Id=4 AND ShortCode='';
            UPDATE ContainerTypes SET ShortCode='CHEP',   SystemCode='CHEP_PALLET', IsSpecialFloorReportContainer=1 WHERE Id=5 AND ShortCode='';
            """);

        // Any custom rows that pre-date this migration still receive stable,
        // deterministic values without changing their primary key.
        await db.Database.ExecuteSqlRawAsync("""
            UPDATE ContainerTypes
            SET ShortCode = 'CT' || Id
            WHERE ShortCode = '';
            UPDATE ContainerTypes
            SET SystemCode = 'CONTAINER_' || Id
            WHERE SystemCode = '';
            """);

        await db.Database.ExecuteSqlRawAsync(
            "CREATE UNIQUE INDEX IF NOT EXISTS IX_ContainerTypes_ShortCode ON ContainerTypes (ShortCode COLLATE NOCASE);");
        await db.Database.ExecuteSqlRawAsync(
            "CREATE UNIQUE INDEX IF NOT EXISTS IX_ContainerTypes_SystemCode ON ContainerTypes (SystemCode COLLATE NOCASE);");
    }

    private static async Task ApplyV8Async(BinTrackerDbContext db)
    {
        await AddBusinessInfoColumnIfMissingAsync(db, "BusinessName");
        await AddBusinessInfoColumnIfMissingAsync(db, "TradingName");
        await AddBusinessInfoColumnIfMissingAsync(db, "Abn");
        await AddBusinessInfoColumnIfMissingAsync(db, "Address");
        await AddBusinessInfoColumnIfMissingAsync(db, "Phone");
        await AddBusinessInfoColumnIfMissingAsync(db, "Email");
        await AddBusinessInfoColumnIfMissingAsync(db, "DefaultReportHeader");
    }

    private static async Task ApplyV9Async(BinTrackerDbContext db)
    {
        await db.Database.ExecuteSqlRawAsync("""
            CREATE TABLE IF NOT EXISTS ImportRuns (
                Id INTEGER NOT NULL CONSTRAINT PK_ImportRuns PRIMARY KEY AUTOINCREMENT,
                SourceFileName TEXT NOT NULL,
                SourceFullPath TEXT NOT NULL,
                SourceSha256 TEXT NOT NULL,
                SourceLength INTEGER NOT NULL,
                SourceLastWriteUtc TEXT NOT NULL,
                StartedUtc TEXT NOT NULL,
                CompletedUtc TEXT NULL,
                Status TEXT NOT NULL,
                CreatedCustomers INTEGER NOT NULL DEFAULT 0,
                MovementCount INTEGER NOT NULL DEFAULT 0,
                UserId INTEGER NULL,
                Username TEXT NOT NULL,
                SessionId TEXT NOT NULL,
                Notes TEXT NULL
            );
            """);

        await db.Database.ExecuteSqlRawAsync(
            "CREATE INDEX IF NOT EXISTS IX_ImportRuns_SourceSha256 ON ImportRuns (SourceSha256);");

        await db.Database.ExecuteSqlRawAsync(
            "CREATE INDEX IF NOT EXISTS IX_ImportRuns_CompletedUtc ON ImportRuns (CompletedUtc);");
    }

    private static async Task ApplyV10Async(BinTrackerDbContext db)
    {
        var columns = await db.Database
            .SqlQueryRaw<string>(
                "SELECT name AS Value FROM pragma_table_info('BinMovements')")
            .ToListAsync();

        if (!columns.Contains(
                "ImportRunId",
                StringComparer.OrdinalIgnoreCase))
        {
            // SQLite permits a nullable REFERENCES column to be added in place.
            // Existing operator/manual/batch movements remain NULL.
            await db.Database.ExecuteSqlRawAsync("""
                ALTER TABLE BinMovements
                ADD COLUMN ImportRunId INTEGER NULL
                    REFERENCES ImportRuns(Id) ON DELETE RESTRICT;
                """);
        }

        await db.Database.ExecuteSqlRawAsync(
            "CREATE INDEX IF NOT EXISTS IX_BinMovements_ImportRunId ON BinMovements (ImportRunId);");

        // Backfill imports made by alpha.19.x before the FK existed.
        //
        // Only Import/Adjustment sources are eligible and the historical
        // ReferenceNumber must be exactly IMPORT-<numeric run id> with a
        // matching ImportRun row. Manual/Batch movements are never inferred.
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
    }

    private static async Task ApplyV11Async(BinTrackerDbContext db)
    {
        var columns = await db.Database
            .SqlQueryRaw<string>(
                "SELECT name AS Value FROM pragma_table_info('ImportRuns')")
            .ToListAsync();

        if (!columns.Contains(
                "CutoverDate",
                StringComparer.OrdinalIgnoreCase))
        {
            await db.Database.ExecuteSqlRawAsync(
                "ALTER TABLE ImportRuns ADD COLUMN CutoverDate TEXT NULL;");
        }

        if (!columns.Contains(
                "ReplacesImportRunId",
                StringComparer.OrdinalIgnoreCase))
        {
            await db.Database.ExecuteSqlRawAsync("""
                ALTER TABLE ImportRuns
                ADD COLUMN ReplacesImportRunId INTEGER NULL
                    REFERENCES ImportRuns(Id) ON DELETE RESTRICT;
                """);
        }

        await db.Database.ExecuteSqlRawAsync(
            "CREATE INDEX IF NOT EXISTS IX_ImportRuns_CutoverDate ON ImportRuns (CutoverDate);");

        await db.Database.ExecuteSqlRawAsync(
            "CREATE UNIQUE INDEX IF NOT EXISTS IX_ImportRuns_ReplacesImportRunId ON ImportRuns (ReplacesImportRunId) WHERE ReplacesImportRunId IS NOT NULL;");

        // Earlier alpha.19 runs already wrote this stable note prefix.
        await db.Database.ExecuteSqlRawAsync("""
            UPDATE ImportRuns
            SET CutoverDate = substr(Notes, 14, 10)
            WHERE CutoverDate IS NULL
              AND Notes GLOB 'Cutover date [0-9][0-9][0-9][0-9]-[0-9][0-9]-[0-9][0-9].*';
            """);
    }

    private static async Task ApplyV12Async(BinTrackerDbContext db)
    {
        var columns = await db.Database
            .SqlQueryRaw<string>(
                "SELECT name AS Value FROM pragma_table_info('ImportRuns')")
            .ToListAsync();

        if (!columns.Contains(
                "CorrectionChangesJson",
                StringComparer.OrdinalIgnoreCase))
        {
            await db.Database.ExecuteSqlRawAsync(
                "ALTER TABLE ImportRuns ADD COLUMN CorrectionChangesJson TEXT NULL;");
        }
    }

    private static async Task AddBusinessInfoColumnIfMissingAsync(
        BinTrackerDbContext db,
        string column)
    {
        var existing = await db.Database
            .SqlQueryRaw<string>(
                "SELECT name AS Value FROM pragma_table_info('ApplicationSettings') WHERE name = {0}",
                column)
            .ToListAsync();

        if (existing.Count != 0)
            return;

        var sql = column switch
        {
            "BusinessName" => "ALTER TABLE ApplicationSettings ADD COLUMN BusinessName TEXT NULL;",
            "TradingName" => "ALTER TABLE ApplicationSettings ADD COLUMN TradingName TEXT NULL;",
            "Abn" => "ALTER TABLE ApplicationSettings ADD COLUMN Abn TEXT NULL;",
            "Address" => "ALTER TABLE ApplicationSettings ADD COLUMN Address TEXT NULL;",
            "Phone" => "ALTER TABLE ApplicationSettings ADD COLUMN Phone TEXT NULL;",
            "Email" => "ALTER TABLE ApplicationSettings ADD COLUMN Email TEXT NULL;",
            "DefaultReportHeader" => "ALTER TABLE ApplicationSettings ADD COLUMN DefaultReportHeader TEXT NULL;",
            _ => throw new InvalidOperationException("Unsupported business information schema column.")
        };

        await db.Database.ExecuteSqlRawAsync(sql);
    }

    private static async Task AddContainerColumnIfMissingAsync(
        BinTrackerDbContext db,
        string column,
        string definition)
    {
        var existing = await db.Database
            .SqlQueryRaw<string>(
                "SELECT name AS Value FROM pragma_table_info('ContainerTypes') WHERE name = {0}",
                column)
            .ToListAsync();

        if (existing.Count != 0)
            return;

        var sql = column switch
        {
            "ShortCode" => "ALTER TABLE ContainerTypes ADD COLUMN ShortCode TEXT NOT NULL DEFAULT '';",
            "SystemCode" => "ALTER TABLE ContainerTypes ADD COLUMN SystemCode TEXT NOT NULL DEFAULT '';",
            "Notes" => "ALTER TABLE ContainerTypes ADD COLUMN Notes TEXT NULL;",
            "IsSpecialFloorReportContainer" => "ALTER TABLE ContainerTypes ADD COLUMN IsSpecialFloorReportContainer INTEGER NOT NULL DEFAULT 0;",
            "DashboardColour" => "ALTER TABLE ContainerTypes ADD COLUMN DashboardColour TEXT NULL;",
            _ => throw new InvalidOperationException("Unsupported container type schema column.")
        };

        await db.Database.ExecuteSqlRawAsync(sql);
    }

    
    private static async Task ApplyV13Async(BinTrackerDbContext db)
    {
        await AddMovementColumnIfMissingAsync(db, "ReversesMovementId");
        await AddMovementColumnIfMissingAsync(db, "CorrectedByMovementId");
        await AddMovementColumnIfMissingAsync(db, "CorrectionReason");

        await db.Database.ExecuteSqlRawAsync(
            "CREATE UNIQUE INDEX IF NOT EXISTS IX_BinMovements_ReversesMovementId ON BinMovements (ReversesMovementId) WHERE ReversesMovementId IS NOT NULL;");
    }

    private static async Task AddMovementColumnIfMissingAsync(
        BinTrackerDbContext db,
        string column)
    {
        // Movement correction schema identifiers are internal constants only.
        // Keep a strict allow-list rather than interpolating arbitrary DDL input.
        var sql = column switch
        {
            "ReversesMovementId" =>
                "ALTER TABLE BinMovements ADD COLUMN ReversesMovementId INTEGER NULL;",
            "CorrectedByMovementId" =>
                "ALTER TABLE BinMovements ADD COLUMN CorrectedByMovementId INTEGER NULL;",
            "CorrectionReason" =>
                "ALTER TABLE BinMovements ADD COLUMN CorrectionReason TEXT NULL;",
            _ => throw new InvalidOperationException(
                "Unsupported BinMovements correction schema column.")
        };

        var existing = await db.Database
            .SqlQueryRaw<string>(
                "SELECT name AS Value FROM pragma_table_info('BinMovements') WHERE name = {0}",
                column)
            .ToListAsync();

        if (existing.Count == 0)
            await db.Database.ExecuteSqlRawAsync(sql);
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
