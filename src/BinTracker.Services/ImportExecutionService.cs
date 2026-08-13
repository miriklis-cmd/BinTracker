
using System.Security.Cryptography;
using BinTracker.Data;
using Microsoft.EntityFrameworkCore;

namespace BinTracker.Services;

public sealed record ImportSourceFingerprint(
    string FileName,
    string FullPath,
    string Sha256,
    long Length,
    DateTime LastWriteUtc);

public sealed record ImportPreflightResult(
    ImportSourceFingerprint Source,
    bool ExactWorkbookPreviouslyImported,
    long? PreviousImportRunId,
    DateTime? PreviousCompletedUtc,
    string? PreviousUsername)
{
    public bool CanProceed => !ExactWorkbookPreviouslyImported;
}

public interface IImportExecutionService
{
    Task<ImportPreflightResult> PreflightAsync(
        string filePath,
        CancellationToken cancellationToken = default);
}

internal sealed class ImportExecutionService(
    IDbContextFactory<BinTrackerDbContext> factory) : IImportExecutionService
{
    public async Task<ImportPreflightResult> PreflightAsync(
        string filePath,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("Import workbook path is required.");

        if (!File.Exists(filePath))
            throw new FileNotFoundException(
                "The workbook no longer exists.",
                filePath);

        var info = new FileInfo(filePath);
        var sha = await ComputeSha256Async(filePath, cancellationToken);

        var fingerprint = new ImportSourceFingerprint(
            info.Name,
            info.FullName,
            sha,
            info.Length,
            info.LastWriteTimeUtc);

        await using var db = await factory.CreateDbContextAsync(cancellationToken);

        var previous = await db.ImportRuns
            .AsNoTracking()
            .Where(x =>
                x.SourceSha256 == sha &&
                x.Status == "Completed")
            .OrderByDescending(x => x.CompletedUtc)
            .Select(x => new
            {
                x.Id,
                x.CompletedUtc,
                x.Username
            })
            .FirstOrDefaultAsync(cancellationToken);

        return new ImportPreflightResult(
            fingerprint,
            previous is not null,
            previous?.Id,
            previous?.CompletedUtc,
            previous?.Username);
    }

    private static async Task<string> ComputeSha256Async(
        string filePath,
        CancellationToken cancellationToken)
    {
        await using var stream = new FileStream(
            filePath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.ReadWrite,
            1024 * 128,
            FileOptions.Asynchronous | FileOptions.SequentialScan);

        using var sha256 = SHA256.Create();
        var hash = await sha256.ComputeHashAsync(stream, cancellationToken);
        return Convert.ToHexString(hash);
    }
}
