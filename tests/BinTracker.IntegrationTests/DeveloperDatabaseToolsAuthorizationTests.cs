using BinTracker.Core;
using BinTracker.Data;
using BinTracker.Services;
using Xunit;

namespace BinTracker.IntegrationTests;

public sealed class DeveloperDatabaseToolsAuthorizationTests
{
    [Fact]
    public async Task Authenticated_administrator_can_use_each_developer_database_operation()
    {
        var data = new RecordingDeveloperDatabaseService();
        var service = new DeveloperDatabaseToolsService(
            data,
            new TestUserContext(authenticated: true, UserRole.Administrator));

        Assert.Equal(
            new DeveloperDatabaseToolsStatus("active.db", "backups", PendingRestart: true),
            service.GetStatus());
        await service.BackupAsync("chosen-backup.db");
        Assert.Equal("automatic-load-backup.db", await service.StageLoadAsync("chosen-source.db"));
        Assert.Equal("automatic-fresh-backup.db", await service.StageFreshAsync());

        Assert.Equal(1, data.StatusCalls);
        Assert.Equal(["chosen-backup.db"], data.BackupDestinations);
        Assert.Equal(["chosen-source.db"], data.LoadSources);
        Assert.Equal(1, data.FreshCalls);
    }

    [Theory]
    [InlineData(false, UserRole.Administrator)]
    [InlineData(true, UserRole.Operator)]
    [InlineData(true, UserRole.Viewer)]
    public async Task Unauthorized_callers_are_denied_before_any_data_or_restart_side_effect(
        bool authenticated,
        UserRole role)
    {
        var data = new RecordingDeveloperDatabaseService();
        var service = new DeveloperDatabaseToolsService(
            data,
            new TestUserContext(authenticated, role));

        Assert.Throws<UnauthorizedAccessException>(() => service.GetStatus());
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.BackupAsync("denied-backup.db"));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.StageLoadAsync("denied-source.db"));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.StageFreshAsync());

        Assert.Equal(0, data.StatusCalls);
        Assert.Empty(data.BackupDestinations);
        Assert.Empty(data.LoadSources);
        Assert.Equal(0, data.FreshCalls);
        Assert.False(data.PendingMarkerCreated);
        Assert.False(data.RestartRequested);
    }

    private sealed class RecordingDeveloperDatabaseService : IDeveloperDatabaseService
    {
        public int StatusCalls { get; private set; }
        public List<string> BackupDestinations { get; } = [];
        public List<string> LoadSources { get; } = [];
        public int FreshCalls { get; private set; }
        public bool PendingMarkerCreated { get; private set; }
        public bool RestartRequested { get; private set; }

        public DeveloperDatabaseStatus GetStatus()
        {
            StatusCalls++;
            return new("active.db", "backups", PendingRestart: true);
        }

        public Task BackupAsync(
            string destinationPath,
            CancellationToken cancellationToken = default)
        {
            BackupDestinations.Add(destinationPath);
            return Task.CompletedTask;
        }

        public Task<string> StageLoadAsync(
            string sourcePath,
            CancellationToken cancellationToken = default)
        {
            LoadSources.Add(sourcePath);
            PendingMarkerCreated = true;
            RestartRequested = true;
            return Task.FromResult("automatic-load-backup.db");
        }

        public Task<string> StageFreshAsync(
            CancellationToken cancellationToken = default)
        {
            FreshCalls++;
            PendingMarkerCreated = true;
            RestartRequested = true;
            return Task.FromResult("automatic-fresh-backup.db");
        }
    }

    private sealed class TestUserContext(bool authenticated, UserRole role) : IUserContext
    {
        public string SessionId { get; } = "test-session";
        public int? UserId { get; } = authenticated ? 17 : null;
        public string Username { get; } = authenticated ? "test-user" : "anonymous";
        public string DisplayName { get; } = authenticated ? "Test User" : "Not signed in";
        public UserRole Role { get; } = role;
        public bool MustChangePassword => false;
        public bool IsAuthenticated { get; } = authenticated;
    }
}
