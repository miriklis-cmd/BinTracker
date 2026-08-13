using BinTracker.Data;
using Xunit;

namespace BinTracker.UnitTests;

public sealed class DeveloperDatabaseConfigurationTests
{
    [Fact]
    public void Sqlite_path_is_extracted_from_connection_string()
    {
        var path = DatabaseConfiguration.GetSqlitePath(
            @"Data Source=C:\Temp\BinTracker-test.db;Cache=Shared");

        Assert.Equal(@"C:\Temp\BinTracker-test.db", path);
    }

    [Fact]
    public void Developer_paths_live_under_BinTracker_app_folder()
    {
        Assert.StartsWith(
            DatabaseConfiguration.AppFolder,
            DatabaseConfiguration.DeveloperBackupFolder,
            StringComparison.OrdinalIgnoreCase);

        Assert.StartsWith(
            DatabaseConfiguration.AppFolder,
            DatabaseConfiguration.PendingDatabaseOperationPath,
            StringComparison.OrdinalIgnoreCase);
    }
}
