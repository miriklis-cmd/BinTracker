using BinTracker.Core;
using BinTracker.Data;

namespace BinTracker.Services;

/// <summary>
/// Desktop-local developer database status presented without exposing the Data contract.
/// Paths remain display/selection values for this explicitly local tool; Data retains all
/// provider, backup, staging and replacement mechanics.
/// </summary>
public sealed record DeveloperDatabaseToolsStatus(
    string ActiveDatabasePath,
    string BackupFolder,
    bool PendingRestart);

public interface IDeveloperDatabaseToolsService
{
    DeveloperDatabaseToolsStatus GetStatus();
    Task BackupAsync(string destinationPath, CancellationToken cancellationToken = default);
    Task<string> StageLoadAsync(string sourcePath, CancellationToken cancellationToken = default);
    Task<string> StageFreshAsync(CancellationToken cancellationToken = default);
}

internal sealed class DeveloperDatabaseToolsService(
    IDeveloperDatabaseService dataService,
    IUserContext userContext) : IDeveloperDatabaseToolsService
{
    public DeveloperDatabaseToolsStatus GetStatus()
    {
        RequireAdministrator();
        var status = dataService.GetStatus();
        return new(
            status.ActiveDatabasePath,
            status.BackupFolder,
            status.PendingRestart);
    }

    public Task BackupAsync(
        string destinationPath,
        CancellationToken cancellationToken = default)
    {
        RequireAdministrator();
        return dataService.BackupAsync(destinationPath, cancellationToken);
    }

    public Task<string> StageLoadAsync(
        string sourcePath,
        CancellationToken cancellationToken = default)
    {
        RequireAdministrator();
        return dataService.StageLoadAsync(sourcePath, cancellationToken);
    }

    public Task<string> StageFreshAsync(
        CancellationToken cancellationToken = default)
    {
        RequireAdministrator();
        return dataService.StageFreshAsync(cancellationToken);
    }

    private void RequireAdministrator()
    {
        if (!userContext.IsAuthenticated ||
            !userContext.UserId.HasValue ||
            userContext.Role != UserRole.Administrator)
        {
            throw new UnauthorizedAccessException("Administrator access is required.");
        }
    }
}
