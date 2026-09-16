using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System.Data;
using BinTracker.Core;
using Microsoft.Extensions.DependencyInjection;

namespace BinTracker.Data;

public static class DatabaseSetup
{
    private static DatabaseSettings? _settings;

    public static DatabaseSettings Settings => _settings ??= DatabaseConfiguration.Load();

    public static string AppFolder => DatabaseConfiguration.AppFolder;
    public static string DatabasePath => DatabaseConfiguration.DefaultSqlitePath;
    public static string ActiveSqlitePath =>
        DatabaseConfiguration.GetSqlitePath(ConnectionString)
        ?? DatabaseConfiguration.DefaultSqlitePath;
    public static string ConnectionString =>
        Settings.ConnectionString ?? $"Data Source={DatabasePath};Cache=Shared";

    public static string StatusText => DatabaseConfiguration.GetStatusText(Settings);

    /// <summary>
    /// The schema version a fully upgraded SQLite database should be on.
    /// Keeping this derived from the migration catalogue avoids hard-coded
    /// version numbers in tests and upgrade diagnostics.
    /// </summary>
    public static int LatestSchemaVersion =>
        SqliteSchemaMigrations.All.Count == 0
            ? 0
            : SqliteSchemaMigrations.All.Max(x => x.Version);

    internal static int LatestSchema16CompatibilityVersion =>
        SqliteSchemaMigrations.Schema16Baseline.Max(x => x.Version);

    public static IServiceCollection AddBinTrackerData(this IServiceCollection services)
    {
        return AddBinTrackerData(services, Settings);
    }

    internal static IServiceCollection AddBinTrackerData(
        this IServiceCollection services,
        DatabaseSettings settings,
        string? backupDirectory = null,
        string? lockDirectory = null,
        string? pendingOperationPath = null)
    {
        ArgumentNullException.ThrowIfNull(settings);

        services.AddDbContextFactory<BinTrackerDbContext>(options =>
            ConfigureProvider(options, settings));

        services.AddDbContext<BinTrackerDbContext>(options =>
            ConfigureProvider(options, settings));

        services.AddSingleton<IDeveloperDatabaseService, DeveloperDatabaseService>();

        if (settings.Provider == DatabaseProvider.Sqlite)
        {
            var connectionString = settings.ConnectionString ??
                $"Data Source={DatabaseConfiguration.DefaultSqlitePath};Cache=Shared";
            var databasePath = DatabaseConfiguration.GetSqlitePath(connectionString)
                ?? throw new InvalidOperationException("The SQLite database path is required.");

            services.AddSingleton<IStartupDatabaseCoordinator>(_ =>
                new SqliteStartupDatabaseCoordinator(
                    databasePath, backupDirectory, lockDirectory, pendingOperationPath));
            services.AddScoped<IInitialMovementLineageWriter>(_ =>
                new SqliteInitialMovementLineageWriter(
                    NoInitialMovementLineageFailureInjector.Instance));
            services.AddScoped<ISingleMovementResponseReceiptStore>(_ =>
                new SqliteSingleMovementResponseReceiptStore(
                    NoSingleMovementResponseReceiptFailureInjector.Instance));
            services.AddScoped<IMovementMutationWriter>(_ =>
                new SqliteMovementMutationWriter(NoMovementMutationFailureInjector.Instance));
            services.AddSingleton<ITransactionalOperationalMovementProjectionAuthority>(_ =>
                new SqliteOperationalMovementProjectionAuthority(connectionString));
            services.AddSingleton<IOperationalMovementProjectionAuthority>(sp =>
                sp.GetRequiredService<ITransactionalOperationalMovementProjectionAuthority>());
        }

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
                    "PostgreSQL support is prepared but not enabled in this alpha.");

            default:
                throw new NotSupportedException(
                    $"Database provider '{settings.Provider}' is not supported.");
        }
    }

    public static Task<StartupDatabaseSession> InitializeAsync(
        IServiceProvider services,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(services);
        return services.GetRequiredService<IStartupDatabaseCoordinator>()
            .StartAsync(cancellationToken);
    }

    internal static async Task InitializeSchema16CompatibilityAsync(BinTrackerDbContext db)
    {
        var created = await db.Database.EnsureCreatedAsync();
        if (created)
            await PreserveFreshSchema16BatchDetachAsync(db);

        // From Alpha 6 onward, database changes are applied in explicit numbered steps.
        // This replaces the earlier collection of ad-hoc "if missing" upgrade statements.
        await EnsureSchemaVersionTableAsync(db);

        var currentVersion = await GetSchemaVersionAsync(db);

        foreach (var migration in SqliteSchemaMigrations.Schema16Baseline
                     .Where(x => x.Version > currentVersion)
                     .OrderBy(x => x.Version))
        {
            await using var transaction = await db.Database.BeginTransactionAsync();

            try
            {
                await migration.ApplyAsync(db);
                await SetSchemaVersionAsync(db, migration.Version);
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
    }

    // Retained only for existing schema16 compatibility fixtures. Production
    // startup is exclusively InitializeAsync -> IStartupDatabaseCoordinator.
    internal static Task InitializeSqliteAsync(BinTrackerDbContext db) =>
        InitializeSchema16CompatibilityAsync(db);

    private static async Task PreserveFreshSchema16BatchDetachAsync(BinTrackerDbContext db)
    {
        // EF must not null immutable tracked membership when a loaded principal is
        // deleted. Fresh schema16 databases nevertheless retain the accepted
        // persisted SET NULL behavior until the activated schema17 rebuild changes
        // that FK to RESTRICT. Keep this provider-specific compatibility shape in
        // Data without weakening the client-side relationship model.
        const string relationship =
            "CONSTRAINT \"FK_BinMovements_MovementBatches_MovementBatchId\" " +
            "FOREIGN KEY (\"MovementBatchId\") REFERENCES \"MovementBatches\" (\"Id\")";

        var createSql = await db.Database
            .SqlQueryRaw<string>(
                "SELECT sql AS Value FROM sqlite_master WHERE type='table' AND name='BinMovements'")
            .SingleAsync();
        if (createSql.Contains("ON DELETE SET NULL", StringComparison.OrdinalIgnoreCase))
            return;
        if (!createSql.Contains(relationship, StringComparison.Ordinal))
            throw new InvalidOperationException("SCHEMA16_BINMOVEMENT_BATCH_FK_SHAPE_UNEXPECTED");

        var indexSql = await db.Database
            .SqlQueryRaw<string>(
                "SELECT sql AS Value FROM sqlite_master WHERE type='index' AND tbl_name='BinMovements' AND sql IS NOT NULL ORDER BY name")
            .ToListAsync();
        var columns = await db.Database
            .SqlQueryRaw<string>("SELECT name AS Value FROM pragma_table_info('BinMovements') ORDER BY cid")
            .ToListAsync();
        if (columns.Count == 0 ||
            columns.Any(x => string.IsNullOrEmpty(x) ||
                x.Any(ch => !(char.IsLetterOrDigit(ch) || ch == '_'))))
            throw new InvalidOperationException("SCHEMA16_BINMOVEMENT_COLUMNS_UNSAFE");

        const string createPrefix = "CREATE TABLE \"BinMovements\"";
        if (!createSql.Contains(createPrefix, StringComparison.Ordinal))
            throw new InvalidOperationException("SCHEMA16_BINMOVEMENT_TABLE_SHAPE_UNEXPECTED");
        var replacementSql = createSql
            .Replace(createPrefix, "CREATE TABLE \"__BinMovements_fresh\"", StringComparison.Ordinal)
            .Replace(relationship, relationship + " ON DELETE SET NULL", StringComparison.Ordinal);

        var wasOpen = db.Database.GetDbConnection().State == ConnectionState.Open;
        if (!wasOpen)
            await db.Database.OpenConnectionAsync();
        try
        {
            // Dropping/replacing this empty fresh table with FK enforcement off
            // leaves child table declarations pointed at the stable BinMovements
            // name. Renaming the live parent would retarget them to a temporary
            // identity and corrupt the fresh schema.
            await db.Database.ExecuteSqlRawAsync("PRAGMA foreign_keys=OFF;");
            await using var transaction = await db.Database.BeginTransactionAsync();
            try
            {
                await db.Database.ExecuteSqlRawAsync(replacementSql);
                var quoted = string.Join(", ", columns.Select(x => $"\"{x}\""));
                await ExecuteProviderSqlAsync(db,
                    $"INSERT INTO __BinMovements_fresh ({quoted}) SELECT {quoted} FROM BinMovements;");
                await db.Database.ExecuteSqlRawAsync("DROP TABLE BinMovements;");
                await db.Database.ExecuteSqlRawAsync(
                    "ALTER TABLE __BinMovements_fresh RENAME TO BinMovements;");
                foreach (var sql in indexSql)
                    await db.Database.ExecuteSqlRawAsync(sql);
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
        finally
        {
            await db.Database.ExecuteSqlRawAsync("PRAGMA foreign_keys=ON;");
            if (!wasOpen)
                await db.Database.CloseConnectionAsync();
        }
    }

    private static async Task ExecuteProviderSqlAsync(BinTrackerDbContext db, string sql)
    {
        await using var command = db.Database.GetDbConnection().CreateCommand();
        command.Transaction = db.Database.CurrentTransaction?.GetDbTransaction();
        command.CommandText = sql;
        await command.ExecuteNonQueryAsync();
    }

    private static async Task EnsureSchemaVersionTableAsync(BinTrackerDbContext db)
    {
        await db.Database.ExecuteSqlRawAsync("""
            CREATE TABLE IF NOT EXISTS SchemaVersion (
                Id INTEGER NOT NULL CONSTRAINT PK_SchemaVersion PRIMARY KEY,
                Version INTEGER NOT NULL,
                UpdatedUtc TEXT NOT NULL
            );
            """);

        await db.Database.ExecuteSqlRawAsync("""
            INSERT INTO SchemaVersion (Id, Version, UpdatedUtc)
            SELECT 1, 0, CURRENT_TIMESTAMP
            WHERE NOT EXISTS (SELECT 1 FROM SchemaVersion WHERE Id = 1);
            """);
    }

    internal static Task<int> GetSchemaVersionAsync(BinTrackerDbContext db) =>
        db.Database
            .SqlQueryRaw<int>("SELECT Version AS Value FROM SchemaVersion WHERE Id = 1")
            .SingleAsync();

    private static Task SetSchemaVersionAsync(BinTrackerDbContext db, int version) =>
        db.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE SchemaVersion SET Version = {version}, UpdatedUtc = {DateTime.UtcNow} WHERE Id = 1");
}
