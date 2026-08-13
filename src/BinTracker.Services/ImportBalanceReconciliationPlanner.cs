namespace BinTracker.Services;

public enum ImportBalanceReconciliationStatus
{
    Ready = 0,
    NewCustomerPendingConfirmation = 1,
    UnresolvedCustomer = 2,
    UnresolvedContainer = 3,
    MissingBroughtForward = 4,
    ExcelTotalMismatch = 5,
    ExistingCustomerPendingConfirmation = 6
}

public sealed record ImportBalanceReconciliationRow(
    string CustomerCode,
    int? ExistingCustomerId,
    string ExistingCustomerName,
    string Container,
    string ContainerToken,
    int? ContainerTypeId,
    int CurrentBinTrackerBalance,
    int? ExcelBroughtForward,
    int ExcelOut,
    int ExcelIn,
    int? ExcelTarget,
    int? OpeningAdjustment,
    int? ProjectedBalance,
    ImportBalanceReconciliationStatus Status,
    string ContainerReason,
    string SourceWorksheet,
    string SourceRow)
{
    public bool IsReady =>
        Status == ImportBalanceReconciliationStatus.Ready;
}

public sealed record ImportBalanceReconciliationPlan(
    IReadOnlyList<ImportBalanceReconciliationRow> Rows)
{
    public int ReadyCount => Rows.Count(x => x.Status == ImportBalanceReconciliationStatus.Ready);
    public int NewCustomerPendingCount =>
        Rows.Count(x => x.Status == ImportBalanceReconciliationStatus.NewCustomerPendingConfirmation);
    public int UnresolvedCustomerCount =>
        Rows.Count(x => x.Status == ImportBalanceReconciliationStatus.UnresolvedCustomer);
    public int UnresolvedContainerCount =>
        Rows.Count(x => x.Status == ImportBalanceReconciliationStatus.UnresolvedContainer);
    public int MissingBroughtForwardCount =>
        Rows.Count(x => x.Status == ImportBalanceReconciliationStatus.MissingBroughtForward);
    public int ExcelMismatchCount =>
        Rows.Count(x => x.Status == ImportBalanceReconciliationStatus.ExcelTotalMismatch);

    public int ExistingCustomerPendingCount =>
        Rows.Count(x => x.Status == ImportBalanceReconciliationStatus.ExistingCustomerPendingConfirmation);

    public bool HasBlockingIssues =>
        NewCustomerPendingCount > 0 ||
        UnresolvedCustomerCount > 0 ||
        UnresolvedContainerCount > 0 ||
        MissingBroughtForwardCount > 0 ||
        ExcelMismatchCount > 0 ||
        ExistingCustomerPendingCount > 0;
}

public static class ImportBalanceReconciliationPlanner
{
    public static ImportBalanceReconciliationPlan Build(
        ExcelImportAnalysis analysis,
        IReadOnlyCollection<ImportWorksheetMapping> mappings,
        ImportReviewPlan customerReview,
        IReadOnlyCollection<ContainerTypeListRow> containerTypes,
        IReadOnlyCollection<BalanceRow> currentBalances,
        IReadOnlyDictionary<string, int>? containerTokenMappings = null,
        IReadOnlyDictionary<string, ImportCustomerDecision>? customerDecisions = null,
        IReadOnlyDictionary<string, ImportExistingCustomerDecision>? existingCustomerDecisions = null)
    {
        var sourceSheets = mappings
            .Where(x => x.Role == ImportWorksheetRole.Source)
            .Select(x => x.Worksheet)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var customerByKey = customerReview.Customers
            .ToDictionary(
                x => CustomerNameNormalizer.ComparisonKey(x.CustomerCode),
                x => x,
                StringComparer.OrdinalIgnoreCase);

        var rows = new List<ImportBalanceReconciliationRow>();

        foreach (var snapshot in analysis.SnapshotCandidates
                     .Where(x => sourceSheets.Contains(x.Worksheet)))
        {
            var customerKey = CustomerNameNormalizer.ComparisonKey(snapshot.CustomerCode);

            if (!customerByKey.TryGetValue(customerKey, out var customer))
            {
                rows.Add(CreateBlocked(
                    snapshot,
                    ImportBalanceReconciliationStatus.UnresolvedCustomer,
                    string.Empty,
                    null,
                    0));
                continue;
            }

            if (customer.Status is ImportCustomerReviewStatus.TypeMismatch
                or ImportCustomerReviewStatus.SourceConflict)
            {
                rows.Add(CreateBlocked(
                    snapshot,
                    ImportBalanceReconciliationStatus.UnresolvedCustomer,
                    string.Empty,
                    null,
                    0,
                    customer));
                continue;
            }

            ImportBalanceReconciliationStatus customerStatus =
                ImportBalanceReconciliationStatus.Ready;

            if (customer.Status == ImportCustomerReviewStatus.New)
            {
                ImportCustomerDecision? decision = null;

                if (customerDecisions is not null)
                {
                    customerDecisions.TryGetValue(
                        customer.CustomerCode,
                        out decision);
                }

                if (decision?.Action == ImportCustomerDecisionAction.Skip)
                    continue;

                if (decision is null ||
                    decision.Action == ImportCustomerDecisionAction.Unconfirmed)
                {
                    customerStatus =
                        ImportBalanceReconciliationStatus.NewCustomerPendingConfirmation;
                }
            }

            if (customer.Status == ImportCustomerReviewStatus.Existing &&
                existingCustomerDecisions is not null)
            {
                existingCustomerDecisions.TryGetValue(
                    customer.CustomerCode,
                    out var existingDecision);

                if (existingDecision is not null &&
                    existingDecision.Action !=
                        ImportExistingCustomerDecisionAction.Unconfirmed &&
                    existingDecision.CustomerId.HasValue)
                {
                    customer = customer with
                    {
                        ExistingCustomerId = existingDecision.CustomerId,
                        ExistingCustomerName = existingDecision.CustomerName
                    };
                }
                else
                {
                    // Keep the automatically proposed existing match for
                    // preview-only current-balance maths, but still block
                    // Import until the operator explicitly confirms/overrides it.
                    customerStatus =
                        ImportBalanceReconciliationStatus.ExistingCustomerPendingConfirmation;
                }
            }

            // Container identity is independent of customer confirmation.
            // Resolve it before applying customer-decision blockers so Review
            // can still show e.g. CLAMMS Blue / Bulk / Yellow clearly.
            var resolution = LegacyContainerHintResolver.Resolve(
                snapshot.ContainerHint,
                containerTypes,
                containerTokenMappings);

            if (!resolution.IsResolved || !resolution.ContainerTypeId.HasValue)
            {
                rows.Add(CreateBlocked(
                    snapshot,
                    ImportBalanceReconciliationStatus.UnresolvedContainer,
                    resolution.DisplayName,
                    resolution.ContainerTypeId,
                    0,
                    customer,
                    resolution.Reason));
                continue;
            }

            var currentBalance = customer.ExistingCustomerId.HasValue
                ? currentBalances
                    .Where(x =>
                        x.CustomerId == customer.ExistingCustomerId.Value &&
                        x.ContainerTypeId == resolution.ContainerTypeId.Value)
                    .Select(x => x.Balance)
                    .FirstOrDefault()
                : 0;

            if (!snapshot.BroughtForward.HasValue)
            {
                rows.Add(CreateBlocked(
                    snapshot,
                    ImportBalanceReconciliationStatus.MissingBroughtForward,
                    resolution.DisplayName,
                    resolution.ContainerTypeId,
                    currentBalance,
                    customer,
                    resolution.Reason));
                continue;
            }

            var outQuantity = snapshot.Out ?? 0;
            var inQuantity = snapshot.In ?? 0;
            var openingAdjustment =
                snapshot.BroughtForward.Value - currentBalance;
            var projected =
                currentBalance +
                openingAdjustment +
                outQuantity -
                inQuantity;
            var target =
                snapshot.ExcelTotal ??
                snapshot.CalculatedTotal;

            if (!snapshot.TotalMatches)
            {
                rows.Add(new ImportBalanceReconciliationRow(
                    customer.CustomerCode,
                    customer.ExistingCustomerId,
                    customer.ExistingCustomerName,
                    resolution.DisplayName,
                    snapshot.ContainerHint?.Trim() ?? string.Empty,
                    resolution.ContainerTypeId,
                    currentBalance,
                    snapshot.BroughtForward,
                    outQuantity,
                    inQuantity,
                    target,
                    openingAdjustment,
                    projected,
                    ImportBalanceReconciliationStatus.ExcelTotalMismatch,
                    resolution.Reason,
                    snapshot.Worksheet,
                    snapshot.SourceRow));
                continue;
            }

            rows.Add(new ImportBalanceReconciliationRow(
                customer.CustomerCode,
                customer.ExistingCustomerId,
                customer.ExistingCustomerName,
                resolution.DisplayName,
                snapshot.ContainerHint?.Trim() ?? string.Empty,
                resolution.ContainerTypeId,
                currentBalance,
                snapshot.BroughtForward,
                outQuantity,
                inQuantity,
                target,
                openingAdjustment,
                projected,
                customerStatus,
                resolution.Reason,
                snapshot.Worksheet,
                snapshot.SourceRow));
        }

        return new ImportBalanceReconciliationPlan(rows);
    }

    private static ImportBalanceReconciliationRow CreateBlocked(
        ImportSnapshotCandidate snapshot,
        ImportBalanceReconciliationStatus status,
        string container,
        int? containerTypeId,
        int currentBalance,
        ImportCustomerReviewRow? customer = null,
        string containerReason = "") =>
        new(
            customer?.CustomerCode ?? snapshot.CustomerCode,
            customer?.ExistingCustomerId,
            customer?.ExistingCustomerName ?? string.Empty,
            container,
            snapshot.ContainerHint?.Trim() ?? string.Empty,
            containerTypeId,
            currentBalance,
            snapshot.BroughtForward,
            snapshot.Out ?? 0,
            snapshot.In ?? 0,
            snapshot.ExcelTotal ?? snapshot.CalculatedTotal,
            null,
            null,
            status,
            containerReason,
            snapshot.Worksheet,
            snapshot.SourceRow);
}
