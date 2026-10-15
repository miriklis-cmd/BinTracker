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
    Task<DeveloperDatabaseToolsStatus> GetStatusAsync(
        CancellationToken cancellationToken = default);
    Task BackupAsync(string destinationPath, CancellationToken cancellationToken = default);
    Task<string> StageLoadAsync(string sourcePath, CancellationToken cancellationToken = default);
    Task<string> StageFreshAsync(CancellationToken cancellationToken = default);
}

internal sealed class DeveloperDatabaseToolsService(
    IDeveloperDatabaseService dataService,
    ICurrentAccountAuthorization authorization) : IDeveloperDatabaseToolsService
{
    public async Task<DeveloperDatabaseToolsStatus> GetStatusAsync(
        CancellationToken cancellationToken = default)
    {
        await authorization.RequireAdministratorAsync(cancellationToken);
        var status = dataService.GetStatus();
        return new(
            status.ActiveDatabasePath,
            status.BackupFolder,
            status.PendingRestart);
    }

    public async Task BackupAsync(
        string destinationPath,
        CancellationToken cancellationToken = default)
    {
        await authorization.RequireAdministratorAsync(cancellationToken);
        await dataService.BackupAsync(destinationPath, cancellationToken);
    }

    public async Task<string> StageLoadAsync(
        string sourcePath,
        CancellationToken cancellationToken = default)
    {
        await authorization.RequireAdministratorAsync(cancellationToken);
        return await dataService.StageLoadAsync(sourcePath, cancellationToken);
    }

    public async Task<string> StageFreshAsync(
        CancellationToken cancellationToken = default)
    {
        await authorization.RequireAdministratorAsync(cancellationToken);
        return await dataService.StageFreshAsync(cancellationToken);
    }
}
