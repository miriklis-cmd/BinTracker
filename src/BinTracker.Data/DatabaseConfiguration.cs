using System.Text.Json;

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

    public static DatabaseSettings Load()
    {
        if (!File.Exists(SettingsPath))
        {
            var defaults = new DatabaseSettings
            {
                Provider = DatabaseProvider.Sqlite,
                ConnectionString = $"Data Source={DefaultSqlitePath};Cache=Shared"
            };

            Save(defaults);
            return defaults;
        }

        try
        {
            var json = File.ReadAllText(SettingsPath);
            var settings = JsonSerializer.Deserialize<DatabaseSettings>(json)
                ?? throw new InvalidOperationException("Database settings are empty.");

            if (settings.Provider == DatabaseProvider.Sqlite &&
                string.IsNullOrWhiteSpace(settings.ConnectionString))
            {
                settings.ConnectionString = $"Data Source={DefaultSqlitePath};Cache=Shared";
            }

            return settings;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Could not read database settings from '{SettingsPath}'.", ex);
        }
    }

    public static void Save(DatabaseSettings settings)
    {
        Directory.CreateDirectory(AppFolder);
        var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions
        {
            WriteIndented = true
        });
        File.WriteAllText(SettingsPath, json);
    }

    public static string GetStatusText(DatabaseSettings settings) =>
        settings.Provider switch
        {
            DatabaseProvider.Sqlite => $"SQLite: {TryGetSqlitePath(settings.ConnectionString) ?? DefaultSqlitePath}",
            DatabaseProvider.PostgreSql => "PostgreSQL: central database",
            _ => settings.Provider.ToString()
        };

    private static string? TryGetSqlitePath(string? connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            return null;

        const string prefix = "Data Source=";
        var part = connectionString
            .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .FirstOrDefault(x => x.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));

        return part is null ? null : part[prefix.Length..];
    }
}
