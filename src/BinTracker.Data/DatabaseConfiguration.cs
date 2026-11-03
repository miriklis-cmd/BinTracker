using System.Text.Json;
using Microsoft.Data.Sqlite;

namespace BinTracker.Data;

public enum DatabaseProvider
{
    Sqlite = 0,
    PostgreSql = 1
}

public sealed class DatabaseSettings
{
    public DatabaseProvider Provider { get; set; } = DatabaseProvider.Sqlite;
    public string? ConnectionString { get; set; }
}

public static class DatabaseConfiguration
{
    private const string SettingsFileName = "database.json";

    public static string AppFolder
    {
        get
        {
            var path = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "BinTracker");

            Directory.CreateDirectory(path);
            return path;
        }
    }

    public static string SettingsPath => Path.Combine(AppFolder, SettingsFileName);
    public static string DefaultSqlitePath => Path.Combine(AppFolder, "BinTracker.db");
    public static string DeveloperBackupFolder => Path.Combine(AppFolder, "DeveloperBackups");
    public static string LineageRecoveryFolder => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "BinTracker-RecoveryPreUpgrade");
    public static string DatabaseAccessLockFolder => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "BinTracker-DatabaseAccessLocks");
    public static string PendingDatabaseOperationPath =>
        Path.Combine(AppFolder, "pending-database-operation.json");
    public static string PendingDatabaseFilePath =>
        Path.Combine(AppFolder, "pending-database-load.db");

    public static DatabaseSettings Load() => Load(SettingsPath, DefaultSqlitePath);

    internal static DatabaseSettings Load(string settingsPath, string defaultSqlitePath)
    {
        var fullSettingsPath = Path.GetFullPath(settingsPath);
        var fullDefaultPath = Path.GetFullPath(defaultSqlitePath);
        if (!File.Exists(fullSettingsPath))
        {
            var defaults = new DatabaseSettings
            {
                Provider = DatabaseProvider.Sqlite,
                ConnectionString = CreateSqliteConnectionString(fullDefaultPath)
            };

            Save(defaults, fullSettingsPath);
            return defaults;
        }

        try
        {
            var json = File.ReadAllText(fullSettingsPath);
            var settings = JsonSerializer.Deserialize<DatabaseSettings>(json)
                ?? throw new InvalidOperationException("Database settings are empty.");

            if (settings.Provider == DatabaseProvider.Sqlite &&
                string.IsNullOrWhiteSpace(settings.ConnectionString))
            {
                settings.ConnectionString = CreateSqliteConnectionString(fullDefaultPath);
            }

            return settings;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Could not read database settings from '{fullSettingsPath}'.", ex);
        }
    }

    public static void Save(DatabaseSettings settings)
        => Save(settings, SettingsPath);

    internal static void Save(
        DatabaseSettings settings,
        string settingsPath,
        Action? beforePublication = null)
    {
        ArgumentNullException.ThrowIfNull(settings);
        var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions
        {
            WriteIndented = true
        });
        AtomicFilePublication.Write(
            settingsPath,
            stream =>
            {
                using var writer = new StreamWriter(stream, leaveOpen: true);
                writer.Write(json);
                writer.Flush();
            },
            AtomicFilePublicationMode.CreateOrReplace,
            beforePublication: beforePublication);
    }

    public static string GetStatusText(DatabaseSettings settings) =>
        settings.Provider switch
        {
            DatabaseProvider.Sqlite => $"SQLite: {GetSqlitePath(settings.ConnectionString) ?? DefaultSqlitePath}",
            DatabaseProvider.PostgreSql => "PostgreSQL: central database",
            _ => settings.Provider.ToString()
        };

    public static string? GetSqlitePath(string? connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            return null;

        return new SqliteConnectionStringBuilder(connectionString).DataSource;
    }

    internal static string CreateSqliteConnectionString(string path) =>
        new SqliteConnectionStringBuilder
        {
            DataSource = Path.GetFullPath(path),
            Cache = SqliteCacheMode.Shared
        }.ConnectionString;
}
