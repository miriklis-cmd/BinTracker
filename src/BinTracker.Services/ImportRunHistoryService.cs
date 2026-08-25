using System.Text.Json;
using BinTracker.Core;
using BinTracker.Data;
using Microsoft.EntityFrameworkCore;

namespace BinTracker.Services;

public sealed record ImportRunHistoryRow(
    long Id,
    DateOnly? CutoverDate,
    string Status,
    string SourceFileName,
    string Username,
    DateTime StartedUtc,
    DateTime? CompletedUtc,
    int CreatedCustomers,
    int MovementCount,
    long? ReplacesImportRunId);

public sealed record ImportRunCorrectionChangeRow(
    int CustomerId,
    string CustomerCode,
    string CustomerName,
    int ContainerTypeId,
    string ContainerType,
    int PreviousNetEffect,
    int CorrectedNetEffect)
{
    public int Difference => CorrectedNetEffect - PreviousNetEffect;
}

public sealed record ImportRunOpeningReconciliationRow(
    int CustomerId,
    string CustomerCode,
    string CustomerName,
    int ContainerTypeId,
    string ContainerType,
    int PreviousBinTrackerBalance,
    int ExcelBroughtForward,
    int ExcelTarget,
    int OpeningAdjustment);

public sealed record ImportRunMovementRow(
    long Id,
    DateOnly MovementDate,
    string CustomerCode,
    string CustomerName,
    string ContainerType,
    string Direction,
    string Source,
    int Quantity,
    string ReferenceNumber,
    string EnteredBy);

public sealed record ImportRunDetail(
    long Id,
    DateOnly? CutoverDate,
    string Status,
    string SourceFileName,
    string SourceClientPath,
    string SourceSha256,
    long SourceLength,
    DateTime SourceLastWriteUtc,
    string Username,
    string SessionId,
    DateTime StartedUtc,
    DateTime? CompletedUtc,
    int CreatedCustomers,
    int MovementCount,
    long? ReplacesImportRunId,
    long? ReplacedByImportRunId,
    string Notes,
    IReadOnlyList<ImportRunCorrectionChangeRow> CorrectionChanges,
    bool CorrectionChangesCaptured,
    IReadOnlyList<ImportRunOpeningReconciliationRow> OpeningReconciliationChanges,
    bool OpeningReconciliationCaptured,
    IReadOnlyList<ImportRunMovementRow> Movements);

public interface IImportRunHistoryService
{
    Task<IReadOnlyList<ImportRunHistoryRow>> GetRunsAsync(
        int limit = 500,
        CancellationToken cancellationToken = default);

    Task<ImportRunDetail?> GetRunAsync(
        long importRunId,
        CancellationToken cancellationToken = default);
}

internal sealed class ImportRunHistoryService(
    IDbContextFactory<BinTrackerDbContext> factory,
    IUserContext session)
    : IImportRunHistoryService
{
    public async Task<IReadOnlyList<ImportRunHistoryRow>> GetRunsAsync(
        int limit = 500,
        CancellationToken cancellationToken = default)
    {
        RequireAdministrator();

        await using var db =
            await factory.CreateDbContextAsync(cancellationToken);

        return await db.ImportRuns
            .AsNoTracking()
            .OrderByDescending(x => x.Id)
            .Take(Math.Clamp(limit, 1, 5000))
            .Select(x => new ImportRunHistoryRow(
                x.Id,
                x.CutoverDate,
                x.Status,
                x.SourceFileName,
                x.Username,
                x.StartedUtc,
                x.CompletedUtc,
                x.CreatedCustomers,
                x.MovementCount,
                x.ReplacesImportRunId))
            .ToListAsync(cancellationToken);
    }

    public async Task<ImportRunDetail?> GetRunAsync(
        long importRunId,
        CancellationToken cancellationToken = default)
    {
        RequireAdministrator();

        await using var db =
            await factory.CreateDbContextAsync(cancellationToken);

        var run = await db.ImportRuns
            .AsNoTracking()
            .SingleOrDefaultAsync(
                x => x.Id == importRunId,
                cancellationToken);

        if (run is null)
            return null;

        var replacedById = await db.ImportRuns
            .AsNoTracking()
            .Where(x => x.ReplacesImportRunId == run.Id)
            .Select(x => (long?)x.Id)
            .SingleOrDefaultAsync(cancellationToken);

        var movements = await db.BinMovements
            .AsNoTracking()
            .Where(x => x.ImportRunId == run.Id)
            .OrderBy(x => x.MovementDate)
            .ThenBy(x => x.Customer.CustomerCode)
            .ThenBy(x => x.ContainerType.DisplayOrder)
            .ThenBy(x => x.Id)
            .Select(x => new ImportRunMovementRow(
                x.Id,
                x.MovementDate,
                x.Customer.CustomerCode ?? string.Empty,
                x.Customer.Name,
                x.ContainerType.Name,
                x.MovementType == MovementType.Out
                    ? "OUT"
                    : "IN",
                x.Source == MovementSource.Adjustment
                    ? "Opening adjustment"
                    : "Excel import",
                x.Quantity,
                x.ReferenceNumber ?? string.Empty,
                x.CreatedBy ?? string.Empty))
            .ToListAsync(cancellationToken);

        IReadOnlyList<ImportRunCorrectionChangeRow> correctionChanges =
            [];
        var correctionChangesCaptured =
            run.CorrectionChangesJson is not null;

        if (!string.IsNullOrWhiteSpace(run.CorrectionChangesJson))
        {
            try
            {
                correctionChanges =
                    JsonSerializer.Deserialize<
                        List<ImportRunCorrectionChangeRow>>(
                        run.CorrectionChangesJson) ?? [];
            }
            catch (JsonException)
            {
                // History must remain viewable even if an old/development
                // database contains malformed optional provenance JSON.
                correctionChanges = [];
                correctionChangesCaptured = false;
            }
        }

        IReadOnlyList<ImportRunOpeningReconciliationRow>
            openingReconciliationChanges = [];
        var openingReconciliationCaptured =
            run.OpeningReconciliationChangesJson is not null;

        if (!string.IsNullOrWhiteSpace(
                run.OpeningReconciliationChangesJson))
        {
            try
            {
                openingReconciliationChanges =
                    JsonSerializer.Deserialize<
                        List<ImportRunOpeningReconciliationRow>>(
                        run.OpeningReconciliationChangesJson) ?? [];
            }
            catch (JsonException)
            {
                // Preserve viewability for malformed optional historical
                // provenance, but do not claim that usable detail was captured.
                openingReconciliationChanges = [];
                openingReconciliationCaptured = false;
            }
        }

        return new ImportRunDetail(
            run.Id,
            run.CutoverDate,
            run.Status,
            run.SourceFileName,
            run.SourceClientPath,
            run.SourceSha256,
            run.SourceLength,
            run.SourceLastWriteUtc,
            run.Username,
            run.SessionId,
            run.StartedUtc,
            run.CompletedUtc,
            run.CreatedCustomers,
            run.MovementCount,
            run.ReplacesImportRunId,
            replacedById,
            run.Notes ?? string.Empty,
            correctionChanges,
            correctionChangesCaptured,
            openingReconciliationChanges,
            openingReconciliationCaptured,
            movements);
    }

    private void RequireAdministrator()
    {
        if (!session.IsAuthenticated ||
            session.Role != UserRole.Administrator)
        {
            throw new UnauthorizedAccessException(
                "Administrator access is required to view Import Run history.");
        }
    }
}
