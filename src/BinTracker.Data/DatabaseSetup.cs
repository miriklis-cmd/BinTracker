using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace BinTracker.Data;

public static class DatabaseSetup
{
    private static DatabaseSettings? _settings;

    public static DatabaseSettings Settings => _settings ??= DatabaseConfiguration.Load();

    public static string AppFolder => DatabaseConfiguration.AppFolder;
    public static string DatabasePath => DatabaseConfiguration.DefaultSqlitePath;
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
                    "PostgreSQL support is prepared but not enabled in this alpha.");

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

    internal static async Task InitializeSqliteAsync(BinTrackerDbContext db)
    {
        await db.Database.EnsureCreatedAsync();

        // From Alpha 6 onward, database changes are applied in explicit numbered steps.
        // This replaces the earlier collection of ad-hoc "if missing" upgrade statements.
        await EnsureSchemaVersionTableAsync(db);

        var currentVersion = await GetSchemaVersionAsync(db);

        foreach (var migration in SqliteSchemaMigrations.All
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
