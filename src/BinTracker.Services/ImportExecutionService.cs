using System.Security.Cryptography;
using BinTracker.Core;
using BinTracker.Data;
using Microsoft.EntityFrameworkCore;

namespace BinTracker.Services;

public sealed record ImportSourceFingerprint(
    string FileName,
    string FullPath,
    string Sha256,
    long Length,
    DateTime LastWriteUtc);

public sealed record ImportCutoverRunSummary(
    long ImportRunId,
    DateOnly CutoverDate,
    string SourceFileName,
    string SourceSha256,
    DateTime? CompletedUtc,
    string Username,
    int CreatedCustomers,
    int MovementCount);

public sealed record ImportPreflightResult(
    ImportSourceFingerprint Source,
    bool ExactWorkbookPreviouslyImported,
    long? PreviousImportRunId,
    DateTime? PreviousCompletedUtc,
    string? PreviousUsername,
    ImportCutoverRunSummary? PreviousCutoverRun)
{
    public bool CanProceed => !ExactWorkbookPreviouslyImported;

    public bool RequiresReplacement =>
        !ExactWorkbookPreviouslyImported &&
        PreviousCutoverRun is not null;
}

public enum ImportExecutionMode
{
    NewImport = 0,
    ReplacePreviousCutover = 1
}

public sealed record ImportExecutionRequest(
    string FilePath,
    string ExpectedSourceSha256,
    ExcelImportAnalysis Analysis,
    IReadOnlyList<ImportWorksheetMapping> Mappings,
    IReadOnlyDictionary<string, int> ContainerTokenMappings,
    IReadOnlyDictionary<string, ImportCustomerDecision> CustomerDecisions,
    IReadOnlyDictionary<string, ImportExistingCustomerDecision> ExistingCustomerDecisions,
    DateOnly CutoverDate,
    ImportExecutionMode Mode = ImportExecutionMode.NewImport,
    long? PreviousImportRunId = null);

public sealed record ImportReplacementDifference(
    string CustomerCode,
    string Container,
    int PreviousNetEffect,
    int ProposedNetEffect)
{
    public int Difference => ProposedNetEffect - PreviousNetEffect;
}

public sealed record ImportReplacementComparison(
    ImportCutoverRunSummary PreviousRun,
    int PreviousMovementCount,
    int ProposedMovementCount,
    IReadOnlyList<ImportReplacementDifference> Differences)
{
    public int ChangedPositionCount => Differences.Count;
}

public sealed record ImportExecutionResult(
    long ImportRunId,
    int CreatedCustomers,
    int OpeningAdjustmentMovements,
    int OutMovements,
    int InMovements,
    int MovementCount);

public enum ImportExecutionFailurePoint
{
    AfterDatabaseSaveBeforeCommit
}

public interface IImportExecutionFailureInjector
{
    Task ReachAsync(
        ImportExecutionFailurePoint point,
        CancellationToken cancellationToken = default);
}

internal sealed class NoOpImportExecutionFailureInjector
    : IImportExecutionFailureInjector
{
    public Task ReachAsync(
        ImportExecutionFailurePoint point,
        CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}

public interface IImportExecutionService
{
    Task<ImportPreflightResult> PreflightAsync(
        string filePath,
        DateOnly? cutoverDate = null,
        CancellationToken cancellationToken = default);

    Task<ImportReplacementComparison> CompareReplacementAsync(
        ImportExecutionRequest request,
        CancellationToken cancellationToken = default);

    Task<ImportExecutionResult> ExecuteAsync(
        ImportExecutionRequest request,
        CancellationToken cancellationToken = default);
}

internal sealed class ImportExecutionService(
    IDbContextFactory<BinTrackerDbContext> factory,
    UserSession session,
    IImportExecutionFailureInjector failureInjector)
    : IImportExecutionService
{
    public async Task<ImportPreflightResult> PreflightAsync(
        string filePath,
        DateOnly? cutoverDate = null,
        CancellationToken cancellationToken = default)
    {
        var fingerprint = await FingerprintAsync(
            filePath,
            cancellationToken);

        await using var db =
            await factory.CreateDbContextAsync(cancellationToken);

        var previous = await db.ImportRuns
            .AsNoTracking()
            .Where(x =>
                x.SourceSha256 == fingerprint.Sha256 &&
                x.Status == "Completed")
            .OrderByDescending(x => x.CompletedUtc)
            .Select(x => new
            {
                x.Id,
                x.CompletedUtc,
                x.Username
            })
            .FirstOrDefaultAsync(cancellationToken);

        ImportCutoverRunSummary? previousCutover = null;

        if (cutoverDate.HasValue)
        {
            previousCutover = await db.ImportRuns
                .AsNoTracking()
                .Where(x =>
                    x.CutoverDate == cutoverDate.Value &&
                    x.Status == "Completed")
                .OrderByDescending(x => x.CompletedUtc)
                .Select(x => new ImportCutoverRunSummary(
                    x.Id,
                    x.CutoverDate!.Value,
                    x.SourceFileName,
                    x.SourceSha256,
                    x.CompletedUtc,
                    x.Username,
                    x.CreatedCustomers,
                    x.MovementCount))
                .FirstOrDefaultAsync(cancellationToken);
        }

        return new ImportPreflightResult(
            fingerprint,
            previous is not null,
            previous?.Id,
            previous?.CompletedUtc,
            previous?.Username,
            previousCutover);
    }

    public async Task<ImportReplacementComparison> CompareReplacementAsync(
        ImportExecutionRequest request,
        CancellationToken cancellationToken = default)
    {
        RequireAdministrator();

        if (request.Mode != ImportExecutionMode.ReplacePreviousCutover ||
            !request.PreviousImportRunId.HasValue)
        {
            throw new InvalidOperationException(
                "Replacement comparison requires a previous Import Run.");
        }

        await using var db =
            await factory.CreateDbContextAsync(cancellationToken);

        var previous = await db.ImportRuns
            .AsNoTracking()
            .Where(x =>
                x.Id == request.PreviousImportRunId.Value &&
                x.CutoverDate == request.CutoverDate &&
                x.Status == "Completed")
            .Select(x => new ImportCutoverRunSummary(
                x.Id,
                x.CutoverDate!.Value,
                x.SourceFileName,
                x.SourceSha256,
                x.CompletedUtc,
                x.Username,
                x.CreatedCustomers,
                x.MovementCount))
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new InvalidOperationException(
                "The previous completed Import Run is no longer available.");

        var plan = await BuildReplacementPlanAsync(
            db,
            request,
            previous.ImportRunId,
            cancellationToken);

        var previousRows = await db.BinMovements
            .AsNoTracking()
            .Where(x => x.ImportRunId == previous.ImportRunId)
            .Select(x => new
            {
                CustomerCode = x.Customer.CustomerCode ?? string.Empty,
                Container = x.ContainerType.Name,
                x.MovementType,
                x.Quantity
            })
            .ToListAsync(cancellationToken);

        var previousEffects = previousRows
            .GroupBy(x => ReplacementKey(
                x.CustomerCode,
                NormalizeContainerLabel(x.Container)))
            .ToDictionary(
                g => g.Key,
                g => new
                {
                    CustomerCode = g.First().CustomerCode,
                    Container = NormalizeContainerLabel(g.First().Container),
                    Net = g.Sum(x =>
                        x.MovementType == MovementType.Out
                            ? x.Quantity
                            : -x.Quantity)
                },
                StringComparer.OrdinalIgnoreCase);

        var proposedRows = plan.Reconciliation.Rows
            .Where(x =>
                x.IsReady &&
                x.ContainerTypeId.HasValue)
            .ToList();

        var proposedEffects = proposedRows
            .GroupBy(x => ReplacementKey(
                x.CustomerCode,
                x.Container))
            .ToDictionary(
                g => g.Key,
                g => new
                {
                    CustomerCode = g.First().CustomerCode,
                    Container = g.First().Container,
                    Net = g.Sum(x =>
                        (x.OpeningAdjustment ?? 0) +
                        x.ExcelOut -
                        x.ExcelIn)
                },
                StringComparer.OrdinalIgnoreCase);

        var keys = previousEffects.Keys
            .Concat(proposedEffects.Keys)
            .Distinct(StringComparer.OrdinalIgnoreCase);

        var differences = new List<ImportReplacementDifference>();

        foreach (var key in keys)
        {
            previousEffects.TryGetValue(key, out var oldValue);
            proposedEffects.TryGetValue(key, out var newValue);

            var previousNet = oldValue?.Net ?? 0;
            var proposedNet = newValue?.Net ?? 0;

            if (previousNet == proposedNet)
                continue;

            differences.Add(new ImportReplacementDifference(
                newValue?.CustomerCode ?? oldValue!.CustomerCode,
                newValue?.Container ?? oldValue!.Container,
                previousNet,
                proposedNet));
        }

        var proposedMovementCount = proposedRows.Sum(x =>
            ((x.OpeningAdjustment ?? 0) != 0 ? 1 : 0) +
            (x.ExcelOut > 0 ? 1 : 0) +
            (x.ExcelIn > 0 ? 1 : 0));

        return new ImportReplacementComparison(
            previous,
            previousRows.Count,
            proposedMovementCount,
            differences
                .OrderBy(x => x.CustomerCode, StringComparer.OrdinalIgnoreCase)
                .ThenBy(x => x.Container, StringComparer.OrdinalIgnoreCase)
                .ToList());
    }

    public async Task<ImportExecutionResult> ExecuteAsync(
        ImportExecutionRequest request,
        CancellationToken cancellationToken = default)
    {
        RequireAdministrator();

        if (request.Analysis is null)
            throw new ArgumentException(
                "Import analysis is required.");

        if (request.Mappings.Count == 0 ||
            request.Mappings.All(x => x.Role != ImportWorksheetRole.Source))
        {
            throw new InvalidOperationException(
                "At least one Source worksheet is required.");
        }

        var fingerprint = await FingerprintAsync(
            request.FilePath,
            cancellationToken);

        if (!string.Equals(
                fingerprint.Sha256,
                request.ExpectedSourceSha256,
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "The workbook changed after Step 4 preflight. " +
                "Go back to Analyse and review the changed workbook before importing.");
        }

        await using var db =
            await factory.CreateDbContextAsync(cancellationToken);
        await using var transaction =
            await db.Database.BeginTransactionAsync(cancellationToken);

        // Re-check inside the write transaction so a second process cannot
        // silently apply the exact same source after preflight.
        var alreadyImported = await db.ImportRuns
            .AnyAsync(
                x =>
                    x.SourceSha256 == fingerprint.Sha256 &&
                    x.Status == "Completed",
                cancellationToken);

        if (alreadyImported)
        {
            throw new InvalidOperationException(
                "This exact workbook has already been imported. " +
                "Exact re-import is blocked.");
        }

        ImportRun? previousRun = null;

        if (request.Mode == ImportExecutionMode.ReplacePreviousCutover)
        {
            if (!request.PreviousImportRunId.HasValue)
            {
                throw new InvalidOperationException(
                    "Replacement requires a previous Import Run.");
            }

            previousRun = await db.ImportRuns
                .SingleOrDefaultAsync(
                    x =>
                        x.Id == request.PreviousImportRunId.Value &&
                        x.CutoverDate == request.CutoverDate &&
                        x.Status == "Completed",
                    cancellationToken)
                ?? throw new InvalidOperationException(
                    "The previous completed Import Run is no longer eligible for replacement.");

            var competingCompletedRun = await db.ImportRuns.AnyAsync(
                x =>
                    x.CutoverDate == request.CutoverDate &&
                    x.Status == "Completed" &&
                    x.Id != previousRun.Id,
                cancellationToken);

            if (competingCompletedRun)
            {
                throw new InvalidOperationException(
                    "Another completed Import Run now exists for this cutover date. Return to Review and reopen Step 4.");
            }
        }
        else
        {
            var sameCutoverExists = await db.ImportRuns.AnyAsync(
                x =>
                    x.CutoverDate == request.CutoverDate &&
                    x.Status == "Completed",
                cancellationToken);

            if (sameCutoverExists)
            {
                throw new InvalidOperationException(
                    "A completed Import Run already exists for this cutover date. Use Replace/Correct instead of importing again.");
            }
        }

        var existingCustomers = await db.Customers
            .AsNoTracking()
            .OrderBy(x => x.CustomerCode)
            .Select(x => new CustomerListRow(
                x.Id,
                x.Name,
                x.CustomerCode ?? string.Empty,
                x.CustomerType,
                x.IsActive,
                0))
            .ToListAsync(cancellationToken);

        var containerTypes = await db.ContainerTypes
            .AsNoTracking()
            .OrderBy(x => x.DisplayOrder)
            .ThenBy(x => x.Name)
            .Select(x => new ContainerTypeListRow(
                x.Id,
                x.Name,
                x.ShortCode,
                x.DisplayOrder,
                x.IsActive,
                x.IsSpecialFloorReportContainer,
                0))
            .ToListAsync(cancellationToken);

        var balanceQuery = db.BinMovements
            .AsNoTracking()
            .AsQueryable();

        if (previousRun is not null)
        {
            var previousId = previousRun.Id;

            // Same-cutover correction must reconstruct the workbook position
            // from history that existed before the cutover. Legitimate
            // operator activity on/after the cutover stays in the database
            // but must not be absorbed into the corrected Excel adjustment.
            balanceQuery = balanceQuery.Where(
                x =>
                    x.MovementDate < request.CutoverDate &&
                    (x.ImportRunId == null ||
                     x.ImportRunId != previousId));
        }

        var totals = await balanceQuery
            .GroupBy(x => new
            {
                x.CustomerId,
                x.ContainerTypeId
            })
            .Select(g => new
            {
                g.Key.CustomerId,
                g.Key.ContainerTypeId,
                Balance = g.Sum(x =>
                    x.MovementType == MovementType.Out
                        ? x.Quantity
                        : -x.Quantity)
            })
            .ToListAsync(cancellationToken);

        var currentBalances = totals
            .Select(x => new BalanceRow(
                x.CustomerId,
                string.Empty,
                x.ContainerTypeId,
                string.Empty,
                x.Balance))
            .ToList();

        var review = ExcelImportReviewPlanner.Build(
            request.Analysis,
            request.Mappings,
            existingCustomers);

        ValidateDecisions(review, request);

        var reconciliation =
            ImportBalanceReconciliationPlanner.Build(
                request.Analysis,
                request.Mappings,
                review,
                containerTypes,
                currentBalances,
                request.ContainerTokenMappings,
                request.CustomerDecisions,
                request.ExistingCustomerDecisions);

        ValidateReadiness(review, reconciliation);

        var now = DateTime.UtcNow;

        var run = new ImportRun
        {
            SourceFileName = fingerprint.FileName,
            SourceFullPath = fingerprint.FullPath,
            SourceSha256 = fingerprint.Sha256,
            SourceLength = fingerprint.Length,
            SourceLastWriteUtc = fingerprint.LastWriteUtc,
            CutoverDate = request.CutoverDate,
            ReplacesImportRunId = previousRun?.Id,
            StartedUtc = now,
            Status = "Pending",
            UserId = session.UserId,
            Username = session.Username,
            SessionId = session.SessionId,
            Notes =
                $"Cutover date {request.CutoverDate:yyyy-MM-dd}. " +
                "Excel balances treated as authoritative cutover targets."
        };

        db.ImportRuns.Add(run);
        await db.SaveChangesAsync(cancellationToken);

        var customerIds =
            new Dictionary<string, int>(
                StringComparer.OrdinalIgnoreCase);

        // Existing matches: use the operator-confirmed target, not the
        // automatic proposal.
        foreach (var row in review.Customers
                     .Where(x =>
                         x.Status == ImportCustomerReviewStatus.Existing))
        {
            if (!request.ExistingCustomerDecisions.TryGetValue(
                    row.CustomerCode,
                    out var decision) ||
                decision.Action ==
                    ImportExistingCustomerDecisionAction.Unconfirmed ||
                !decision.CustomerId.HasValue)
            {
                throw new InvalidOperationException(
                    $"Existing customer '{row.CustomerCode}' has not been confirmed.");
            }

            var targetExists = await db.Customers
                .AnyAsync(
                    x => x.Id == decision.CustomerId.Value,
                    cancellationToken);

            if (!targetExists)
            {
                throw new InvalidOperationException(
                    $"The confirmed BinTracker customer for '{row.CustomerCode}' no longer exists.");
            }

            customerIds[
                CustomerNameNormalizer.ComparisonKey(
                    row.CustomerCode)] =
                decision.CustomerId.Value;
        }

        // New customers: create only explicit Create decisions. Skip rows were
        // already excluded from reconciliation by the planner.
        var existingCodes = (await db.Customers
                .AsNoTracking()
                .Select(x => x.CustomerCode ?? string.Empty)
                .ToListAsync(cancellationToken))
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var createdCustomers = 0;

        foreach (var row in review.Customers
                     .Where(x =>
                         x.Status == ImportCustomerReviewStatus.New))
        {
            if (!request.CustomerDecisions.TryGetValue(
                    row.CustomerCode,
                    out var decision))
            {
                throw new InvalidOperationException(
                    $"New customer '{row.CustomerCode}' has no decision.");
            }

            if (decision.Action == ImportCustomerDecisionAction.Skip)
                continue;

            if (decision.Action != ImportCustomerDecisionAction.Create)
            {
                throw new InvalidOperationException(
                    $"New customer '{row.CustomerCode}' is not confirmed for creation.");
            }

            var customerCode =
                row.CustomerCode.Trim().ToUpperInvariant();
            var name = decision.ProposedName.Trim();

            if (customerCode.Length == 0)
            {
                throw new InvalidOperationException(
                    "A new imported customer has a blank customer code.");
            }

            if (name.Length < 2)
            {
                throw new InvalidOperationException(
                    $"Customer '{row.CustomerCode}' needs a valid proposed name.");
            }

            if (!existingCodes.Add(customerCode))
            {
                throw new InvalidOperationException(
                    $"Customer code '{customerCode}' already exists. " +
                    "Return to Review and resolve the customer match.");
            }

            var customer = new Customer
            {
                CustomerCode = customerCode,
                Name = name,
                CustomerType = row.DetectedType,
                IsActive = true,
                CreatedUtc = now,
                UpdatedUtc = now,
                CreatedByUserId = session.UserId,
                UpdatedByUserId = session.UserId,
                Notes =
                    $"Created by Excel import run #{run.Id} " +
                    $"from '{fingerprint.FileName}'."
            };

            db.Customers.Add(customer);
            await db.SaveChangesAsync(cancellationToken);

            customerIds[
                CustomerNameNormalizer.ComparisonKey(
                    row.CustomerCode)] =
                customer.Id;

            createdCustomers++;
        }

        if (previousRun is not null)
        {
            var previousMovements = await db.BinMovements
                .Where(x => x.ImportRunId == previousRun.Id)
                .ToListAsync(cancellationToken);

            db.BinMovements.RemoveRange(previousMovements);

            previousRun.Status = "Replaced";
            previousRun.Notes =
                (previousRun.Notes ?? string.Empty).TrimEnd() +
                $" Replaced by corrected import run #{run.Id}.";
        }

        var openingCount = 0;
        var outCount = 0;
        var inCount = 0;
        var movementCount = 0;

        foreach (var item in reconciliation.Rows)
        {
            if (!item.IsReady ||
                !item.ContainerTypeId.HasValue ||
                !item.OpeningAdjustment.HasValue ||
                !item.ProjectedBalance.HasValue)
            {
                throw new InvalidOperationException(
                    $"Balance row '{item.CustomerCode}' / '{item.Container}' is not ready to import.");
            }

            var key =
                CustomerNameNormalizer.ComparisonKey(
                    item.CustomerCode);

            if (!customerIds.TryGetValue(key, out var customerId))
            {
                throw new InvalidOperationException(
                    $"No resolved BinTracker customer exists for '{item.CustomerCode}'.");
            }

            var commonReference = $"IMPORT-{run.Id}";
            var sourceText =
                $"{item.SourceWorksheet} row {item.SourceRow}";

            if (item.OpeningAdjustment.Value != 0)
            {
                var adjustment = item.OpeningAdjustment.Value;

                db.BinMovements.Add(new BinMovement
                {
                    MovementDate = request.CutoverDate,
                    MovementType =
                        adjustment > 0
                            ? MovementType.Out
                            : MovementType.In,
                    Source = MovementSource.Adjustment,
                    CustomerId = customerId,
                    ContainerTypeId = item.ContainerTypeId.Value,
                    Quantity = Math.Abs(adjustment),
                    ImportRunId = run.Id,
                    ReferenceNumber = commonReference,
                    Notes =
                        $"Excel import run #{run.Id}: opening adjustment " +
                        $"to B/Fwd {item.ExcelBroughtForward}. Source {sourceText}.",
                    CreatedBy = session.Username,
                    CreatedUtc = now
                });

                openingCount++;
                movementCount++;
            }

            if (item.ExcelOut > 0)
            {
                db.BinMovements.Add(new BinMovement
                {
                    MovementDate = request.CutoverDate,
                    MovementType = MovementType.Out,
                    Source = MovementSource.ExcelImport,
                    CustomerId = customerId,
                    ContainerTypeId = item.ContainerTypeId.Value,
                    Quantity = item.ExcelOut,
                    ImportRunId = run.Id,
                    ReferenceNumber = commonReference,
                    Notes =
                        $"Excel import run #{run.Id}: legacy OUT movement. " +
                        $"Source {sourceText}.",
                    CreatedBy = session.Username,
                    CreatedUtc = now
                });

                outCount++;
                movementCount++;
            }

            if (item.ExcelIn > 0)
            {
                db.BinMovements.Add(new BinMovement
                {
                    MovementDate = request.CutoverDate,
                    MovementType = MovementType.In,
                    Source = MovementSource.ExcelImport,
                    CustomerId = customerId,
                    ContainerTypeId = item.ContainerTypeId.Value,
                    Quantity = item.ExcelIn,
                    ImportRunId = run.Id,
                    ReferenceNumber = commonReference,
                    Notes =
                        $"Excel import run #{run.Id}: legacy IN movement. " +
                        $"Source {sourceText}.",
                    CreatedBy = session.Username,
                    CreatedUtc = now
                });

                inCount++;
                movementCount++;
            }
        }

        run.CreatedCustomers = createdCustomers;
        run.MovementCount = movementCount;
        run.Status = "Completed";
        run.CompletedUtc = DateTime.UtcNow;

        db.AuditEvents.Add(new AuditEvent
        {
            TimestampUtc = DateTime.UtcNow,
            UserId = session.UserId,
            Username = session.Username,
            Action = previousRun is null
                ? "EXCEL_IMPORT_COMPLETED"
                : "EXCEL_IMPORT_REPLACED",
            EntityType = "ImportRun",
            EntityId = run.Id.ToString(),
            Description =
                (previousRun is null
                    ? $"Imported '{fingerprint.FileName}' as run #{run.Id}: "
                    : $"Replaced import run #{previousRun.Id} with '{fingerprint.FileName}' as run #{run.Id}: ") +
                $"{createdCustomers} customer(s), " +
                $"{openingCount} opening adjustment(s), " +
                $"{outCount} OUT movement(s), " +
                $"{inCount} IN movement(s).",
            ComputerName = Environment.MachineName,
            SessionId = session.SessionId,
            Succeeded = true
        });

        await db.SaveChangesAsync(cancellationToken);

        // Deliberate test seam: at this point all import writes have been
        // flushed into the open database transaction, but none are committed.
        // A forced exception here must leave the database exactly as it was
        // before ExecuteAsync began.
        await failureInjector.ReachAsync(
            ImportExecutionFailurePoint.AfterDatabaseSaveBeforeCommit,
            cancellationToken);

        await transaction.CommitAsync(cancellationToken);

        return new ImportExecutionResult(
            run.Id,
            createdCustomers,
            openingCount,
            outCount,
            inCount,
            movementCount);
    }

    private sealed record ReplacementPlan(
        ImportBalanceReconciliationPlan Reconciliation);

    private async Task<ReplacementPlan> BuildReplacementPlanAsync(
        BinTrackerDbContext db,
        ImportExecutionRequest request,
        long previousImportRunId,
        CancellationToken cancellationToken)
    {
        var existingCustomers = await db.Customers
            .AsNoTracking()
            .OrderBy(x => x.CustomerCode)
            .Select(x => new CustomerListRow(
                x.Id,
                x.Name,
                x.CustomerCode ?? string.Empty,
                x.CustomerType,
                x.IsActive,
                0))
            .ToListAsync(cancellationToken);

        var containerTypes = await db.ContainerTypes
            .AsNoTracking()
            .OrderBy(x => x.DisplayOrder)
            .ThenBy(x => x.Name)
            .Select(x => new ContainerTypeListRow(
                x.Id,
                x.Name,
                x.ShortCode,
                x.DisplayOrder,
                x.IsActive,
                x.IsSpecialFloorReportContainer,
                0))
            .ToListAsync(cancellationToken);

        var totals = await db.BinMovements
            .AsNoTracking()
            .Where(x =>
                x.MovementDate < request.CutoverDate &&
                (x.ImportRunId == null ||
                 x.ImportRunId != previousImportRunId))
            .GroupBy(x => new
            {
                x.CustomerId,
                x.ContainerTypeId
            })
            .Select(g => new
            {
                g.Key.CustomerId,
                g.Key.ContainerTypeId,
                Balance = g.Sum(x =>
                    x.MovementType == MovementType.Out
                        ? x.Quantity
                        : -x.Quantity)
            })
            .ToListAsync(cancellationToken);

        var balances = totals
            .Select(x => new BalanceRow(
                x.CustomerId,
                string.Empty,
                x.ContainerTypeId,
                string.Empty,
                x.Balance))
            .ToList();

        var review = ExcelImportReviewPlanner.Build(
            request.Analysis,
            request.Mappings,
            existingCustomers);

        ValidateDecisions(review, request);

        var reconciliation =
            ImportBalanceReconciliationPlanner.Build(
                request.Analysis,
                request.Mappings,
                review,
                containerTypes,
                balances,
                request.ContainerTokenMappings,
                request.CustomerDecisions,
                request.ExistingCustomerDecisions);

        ValidateReadiness(review, reconciliation);

        return new ReplacementPlan(reconciliation);
    }

    private static string ReplacementKey(
        string customerCode,
        string container) =>
        $"{CustomerNameNormalizer.ComparisonKey(customerCode)}|" +
        container.Trim().ToUpperInvariant();

    private static string NormalizeContainerLabel(string name)
    {
        if (name.Equals("Blue Bin", StringComparison.OrdinalIgnoreCase))
            return "Blue";
        if (name.Equals("Yellow Bin", StringComparison.OrdinalIgnoreCase))
            return "Yellow";
        if (name.Equals("Bulk Bin", StringComparison.OrdinalIgnoreCase))
            return "Bulk";

        return name.EndsWith(
                " Bin",
                StringComparison.OrdinalIgnoreCase)
            ? name[..^4].Trim()
            : name;
    }

    private static void ValidateDecisions(
        ImportReviewPlan review,
        ImportExecutionRequest request)
    {
        if (review.HasBlockingCustomerConflicts)
        {
            throw new InvalidOperationException(
                "Customer-code/type conflicts remain unresolved.");
        }

        if (review.SnapshotTotalMismatchCount > 0)
        {
            throw new InvalidOperationException(
                "One or more Excel B/Fwd/OUT/IN rows do not reconcile to the workbook total.");
        }

        foreach (var row in review.Customers)
        {
            if (row.Status == ImportCustomerReviewStatus.New)
            {
                if (!request.CustomerDecisions.TryGetValue(
                        row.CustomerCode,
                        out var decision) ||
                    decision.Action ==
                        ImportCustomerDecisionAction.Unconfirmed)
                {
                    throw new InvalidOperationException(
                        $"New customer '{row.CustomerCode}' still needs a Create or Skip decision.");
                }
            }

            if (row.Status == ImportCustomerReviewStatus.Existing)
            {
                if (!request.ExistingCustomerDecisions.TryGetValue(
                        row.CustomerCode,
                        out var decision) ||
                    decision.Action ==
                        ImportExistingCustomerDecisionAction.Unconfirmed ||
                    !decision.CustomerId.HasValue)
                {
                    throw new InvalidOperationException(
                        $"Existing customer '{row.CustomerCode}' still needs match confirmation.");
                }
            }
        }
    }

    private static void ValidateReadiness(
        ImportReviewPlan review,
        ImportBalanceReconciliationPlan reconciliation)
    {
        if (reconciliation.HasBlockingIssues)
        {
            throw new InvalidOperationException(
                "Balance reconciliation still contains blocking issues.");
        }

        if (reconciliation.Rows.Any(x => !x.IsReady))
        {
            throw new InvalidOperationException(
                "Every balance reconciliation row must be Ready before import.");
        }

        if (review.SnapshotRowCount > 0 &&
            reconciliation.Rows.Count == 0)
        {
            throw new InvalidOperationException(
                "No reconciled Source balance rows are available to import.");
        }
    }

    private void RequireAdministrator()
    {
        if (!session.IsAuthenticated)
            throw new UnauthorizedAccessException(
                "You must be logged in to import Excel data.");

        if (session.Role != UserRole.Administrator)
            throw new UnauthorizedAccessException(
                "Only administrators can import Excel data.");
    }

    private static async Task<ImportSourceFingerprint> FingerprintAsync(
        string filePath,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException(
                "Import workbook path is required.");

        if (!File.Exists(filePath))
            throw new FileNotFoundException(
                "The workbook no longer exists.",
                filePath);

        var info = new FileInfo(filePath);
        var sha = await ComputeSha256Async(
            filePath,
            cancellationToken);

        return new ImportSourceFingerprint(
            info.Name,
            info.FullName,
            sha,
            info.Length,
            info.LastWriteTimeUtc);
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
            FileOptions.Asynchronous |
            FileOptions.SequentialScan);

        using var sha256 = SHA256.Create();
        var hash = await sha256.ComputeHashAsync(
            stream,
            cancellationToken);
        return Convert.ToHexString(hash);
    }
}
