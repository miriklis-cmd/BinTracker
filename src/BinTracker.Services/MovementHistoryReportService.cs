using BinTracker.Core;
using BinTracker.Data;
using Microsoft.EntityFrameworkCore;

namespace BinTracker.Services;

public sealed record MovementHistoryReportQuery(
    DateOnly StartDate,
    DateOnly EndDate,
    string? CustomerSearch = null,
    int? ContainerTypeId = null,
    MovementType? Direction = null,
    MovementSource? Source = null,
    bool IncludeAdjustments = false);

public sealed record MovementCorrectionLineage(
    long CorrectionOperationId,
    long OriginalMovementId,
    long NeutralisingMovementId,
    long ReplacementMovementId);

public sealed record MovementHistoryReportRow(
    long MovementId,
    DateOnly MovementDate,
    DateTime RecordedUtc,
    int CustomerId,
    string CustomerCode,
    string CustomerName,
    CustomerType CustomerType,
    int ContainerTypeId,
    string ContainerType,
    int ContainerDisplayOrder,
    MovementType Direction,
    int Quantity,
    MovementSource Source,
    string Reference,
    string Notes,
    string EnteredBy,
    long? ReversesMovementId,
    long? CorrectedByMovementId,
    string LinkedReversalReference,
    string CorrectionReason,
    IReadOnlyList<MovementCorrectionLineage>? CorrectionLines = null)
{
    public string DirectionText =>
        Direction == MovementType.Out ? "OUT" : "IN";

    public IReadOnlyList<MovementCorrectionLineage> Lineage => CorrectionLines ?? [];
    public IReadOnlyList<MovementCorrectionLineage> CorrectedByCorrections =>
        Lineage.Where(x => x.OriginalMovementId == MovementId).ToArray();
    public IReadOnlyList<MovementCorrectionLineage> NeutraliserForCorrections =>
        Lineage.Where(x => x.NeutralisingMovementId == MovementId).ToArray();
    public IReadOnlyList<MovementCorrectionLineage> CreatedByCorrections =>
        Lineage.Where(x => x.ReplacementMovementId == MovementId).ToArray();

    public bool IsCorrectionOriginal => CorrectedByCorrections.Count > 0;
    public bool IsCorrectionNeutraliser => NeutraliserForCorrections.Count > 0;
    public bool IsCorrectionReplacement => CreatedByCorrections.Count > 0;
    public bool IsCorrectionRelated => Lineage.Count > 0;

    public string SourceText => IsCorrectionRelated ? "Correction"
        : ReversesMovementId.HasValue ? "Reversal" : Source switch
    {
        MovementSource.Manual => "Single Entry",
        MovementSource.Batch => "Batch Entry",
        MovementSource.ExcelImport => "Excel Import",
        MovementSource.Adjustment => "Opening Adjustment",
        _ => Source.ToString()
    };

    public string Status => IsCorrectionRelated
        ? string.Join("; ", CorrectionStatusParts())
        : CorrectedByMovementId.HasValue
        ? $"Reversed — see {(!string.IsNullOrWhiteSpace(LinkedReversalReference) ? LinkedReversalReference : $"movement #{CorrectedByMovementId}") }"
        : ReversesMovementId.HasValue
            ? $"Reversal of #{ReversesMovementId}" + (string.IsNullOrWhiteSpace(CorrectionReason) ? "" : $" — {CorrectionReason}")
            : "";

    public bool CanReverse => !ReversesMovementId.HasValue && !CorrectedByMovementId.HasValue;

    private IEnumerable<string> CorrectionStatusParts()
    {
        foreach (var line in CreatedByCorrections)
            yield return $"Corrected replacement for #{line.OriginalMovementId} (correction #{line.CorrectionOperationId})";
        foreach (var line in CorrectedByCorrections)
            yield return $"Corrected — neutraliser #{line.NeutralisingMovementId}; replacement #{line.ReplacementMovementId} (correction #{line.CorrectionOperationId})";
        foreach (var line in NeutraliserForCorrections)
            yield return $"Correction neutraliser for #{line.OriginalMovementId} — replacement #{line.ReplacementMovementId} (correction #{line.CorrectionOperationId})";
    }
}

public sealed record MovementHistoryContainerTotal(
    int ContainerTypeId,
    string ContainerType,
    int DisplayOrder,
    int OutQuantity,
    int InQuantity)
{
    public int NetQuantity => OutQuantity - InQuantity;
}

public sealed record MovementHistoryReportResult(
    DateOnly StartDate,
    DateOnly EndDate,
    IReadOnlyList<MovementHistoryReportRow> Rows,
    IReadOnlyList<MovementHistoryContainerTotal> ContainerTotals)
{
    public int OutQuantity =>
        Rows.Where(x => x.Direction == MovementType.Out)
            .Sum(x => x.Quantity);

    public int InQuantity =>
        Rows.Where(x => x.Direction == MovementType.In)
            .Sum(x => x.Quantity);

    public int NetQuantity => OutQuantity - InQuantity;
}

public interface IMovementHistoryReportService
{
    Task<MovementHistoryReportResult> QueryAsync(
        MovementHistoryReportQuery query,
        CancellationToken cancellationToken = default);
}

internal sealed class MovementHistoryReportService(
    IDbContextFactory<BinTrackerDbContext> factory,
    IBusinessClock clock)
    : IMovementHistoryReportService
{
    public async Task<MovementHistoryReportResult> QueryAsync(
        MovementHistoryReportQuery query,
        CancellationToken cancellationToken = default)
    {
        var today = clock.Today;

        var requestedStart = query.StartDate > today
            ? today
            : query.StartDate;

        var requestedEnd = query.EndDate > today
            ? today
            : query.EndDate;

        var start = requestedStart <= requestedEnd
            ? requestedStart
            : requestedEnd;

        var end = requestedStart <= requestedEnd
            ? requestedEnd
            : requestedStart;

        await using var db =
            await factory.CreateDbContextAsync(cancellationToken);

        var movements = db.BinMovements
            .AsNoTracking()
            .Where(x =>
                x.MovementDate >= start &&
                x.MovementDate <= end);

        if (!query.IncludeAdjustments)
            movements = movements.Where(
                x => x.Source != MovementSource.Adjustment);

        if (query.ContainerTypeId.HasValue)
            movements = movements.Where(
                x => x.ContainerTypeId == query.ContainerTypeId.Value);

        if (query.Direction.HasValue)
            movements = movements.Where(
                x => x.MovementType == query.Direction.Value);

        if (query.Source.HasValue)
            movements = movements.Where(
                x => x.Source == query.Source.Value);

        var raw = await movements
            .Select(x => new
            {
                x.Id,
                x.MovementDate,
                x.CreatedUtc,
                x.CustomerId,
                CustomerCode = x.Customer.CustomerCode ?? "",
                CustomerName = x.Customer.Name,
                x.Customer.CustomerType,
                x.ContainerTypeId,
                ContainerType = x.ContainerType.Name,
                ContainerDisplayOrder = x.ContainerType.DisplayOrder,
                Direction = x.MovementType,
                x.Quantity,
                x.Source,
                Reference = x.ReferenceNumber ?? "",
                Notes = x.Notes ?? "",
                EnteredBy = x.CreatedBy ?? "",
                x.ReversesMovementId,
                x.CorrectedByMovementId,
                LinkedReversalReference = x.CorrectedByMovement != null
                    ? x.CorrectedByMovement.ReferenceNumber ?? ""
                    : "",
                CorrectionReason = x.CorrectionReason ?? ""
            })
            .ToListAsync(cancellationToken);

        var movementIds = raw.Select(x => x.Id).ToList();
        var correctionLines = await db.MovementCorrectionLines.AsNoTracking()
            .Where(line => movementIds.Contains(line.OriginalMovementId) ||
                movementIds.Contains(line.NeutralisingMovementId) ||
                movementIds.Contains(line.ReplacementMovementId))
            .Select(line => new { line.CorrectionOperationId, line.OriginalMovementId,
                line.NeutralisingMovementId, line.ReplacementMovementId })
            .ToListAsync(cancellationToken);
        var correctionLinesByMovementId = correctionLines
            .SelectMany(line => new[]
            {
                (MovementId: line.OriginalMovementId, Line: new MovementCorrectionLineage(
                    line.CorrectionOperationId, line.OriginalMovementId, line.NeutralisingMovementId, line.ReplacementMovementId)),
                (MovementId: line.NeutralisingMovementId, Line: new MovementCorrectionLineage(
                    line.CorrectionOperationId, line.OriginalMovementId, line.NeutralisingMovementId, line.ReplacementMovementId)),
                (MovementId: line.ReplacementMovementId, Line: new MovementCorrectionLineage(
                    line.CorrectionOperationId, line.OriginalMovementId, line.NeutralisingMovementId, line.ReplacementMovementId))
            })
            .GroupBy(x => x.MovementId)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<MovementCorrectionLineage>)group
                    .Select(x => x.Line)
                    .OrderBy(x => x.CorrectionOperationId)
                    .ToArray());

        var search = query.CustomerSearch?.Trim();

        var rows = raw
            .Where(x =>
                string.IsNullOrWhiteSpace(search) ||
                Contains(x.CustomerCode, search) ||
                Contains(x.CustomerName, search))
            .Select(x =>
            {
                correctionLinesByMovementId.TryGetValue(x.Id, out var movementCorrectionLines);
                return new MovementHistoryReportRow(
                x.Id,
                x.MovementDate,
                x.CreatedUtc,
                x.CustomerId,
                x.CustomerCode,
                x.CustomerName,
                x.CustomerType,
                x.ContainerTypeId,
                x.ContainerType,
                x.ContainerDisplayOrder,
                x.Direction,
                x.Quantity,
                x.Source,
                x.Reference,
                x.Notes,
                x.EnteredBy,
                x.ReversesMovementId,
                x.CorrectedByMovementId,
                x.LinkedReversalReference,
                x.CorrectionReason,
                movementCorrectionLines);
            })
            .OrderBy(x => x.MovementDate)
            .ThenBy(x => x.CustomerCode, StringComparer.OrdinalIgnoreCase)
            .ThenBy(x => x.ContainerDisplayOrder)
            .ThenBy(x => x.MovementId)
            .ToList();

        var totals = rows
            .GroupBy(x => new
            {
                x.ContainerTypeId,
                x.ContainerType,
                x.ContainerDisplayOrder
            })
            .Select(g => new MovementHistoryContainerTotal(
                g.Key.ContainerTypeId,
                g.Key.ContainerType,
                g.Key.ContainerDisplayOrder,
                g.Where(x => x.Direction == MovementType.Out)
                    .Sum(x => x.Quantity),
                g.Where(x => x.Direction == MovementType.In)
                    .Sum(x => x.Quantity)))
            .OrderBy(x => x.DisplayOrder)
            .ThenBy(x => x.ContainerType, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return new MovementHistoryReportResult(
            start,
            end,
            rows,
            totals);
    }

    private static bool Contains(string? value, string term) =>
        !string.IsNullOrWhiteSpace(value) &&
        value.Contains(term, StringComparison.OrdinalIgnoreCase);
}
