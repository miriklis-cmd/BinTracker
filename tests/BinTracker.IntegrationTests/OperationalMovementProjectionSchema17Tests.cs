using BinTracker.Core;
using BinTracker.Data;
using BinTracker.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BinTracker.IntegrationTests;

public sealed class OperationalMovementProjectionSchema17Tests
{
    [Fact]
    public async Task Active_and_readonly_roots_project_once_and_position_as_of_uses_movement_date()
    {
        await using var h = await Harness.CreateAsync();
        var date = new DateOnly(2026, 9, 1);
        var root = await h.CreateSingleAsync(date, h.CustomerId, 1, 7);

        var current = await h.Authority.QueryAsync(OperationalMovementProjectionScope.All());
        var movement = Assert.Single(current.Activity);
        Assert.Equal((root.MovementId, OperationalMovementDomain.LineageOrdinary, 0, 7L),
            (movement.EvidenceMovementId, movement.Domain, movement.CurrentGeneration!.Value.Value,
                movement.SignedQuantity));
        Assert.Empty(current.Positions);

        Assert.Empty((await h.Authority.QueryAsync(
            OperationalMovementProjectionScope.PositionAsOf(date.AddDays(-1)))).Activity);
        Assert.Equal(7, Assert.Single((await h.Authority.QueryAsync(
            OperationalMovementProjectionScope.PositionAsOf(date))).Positions).Quantity);
        Assert.Equal(7, Assert.Single((await h.Authority.QueryAsync(
            OperationalMovementProjectionScope.PositionAsOf(date.AddDays(3)))).Positions).Quantity);

        await h.SetRootStatusAsync(root.RootId, LogicalMovementBatchStatus.ReadOnly);
        var readOnly = await h.Authority.QueryAsync(OperationalMovementProjectionScope.All());
        Assert.Equal(root.MovementId, Assert.Single(readOnly.Activity).EvidenceMovementId);
        var mutation = await Assert.ThrowsAsync<LogicalMovementMutationException>(() => h.MutateAsync(
            root.RootId, 0, MovementMutationRequest.Reverse(MovementMutationScope.Individual,
                [movement.LogicalLineId!.Value], "read only roots cannot mutate")));
        Assert.Equal(LogicalMovementMutationFailure.ReadOnly, mutation.Failure);
    }

    [Fact]
    public async Task Reversed_line_projects_last_effective_and_exact_terminal_once()
    {
        await using var h = await Harness.CreateAsync();
        var root = await h.CreateSingleAsync(new(2026, 9, 1), h.CustomerId, 1, 5);
        var lineId = Assert.Single(await h.LineIdsAsync(root.RootId));
        await h.MutateAsync(root.RootId, 0,
            MovementMutationRequest.Reverse(MovementMutationScope.Individual,
                [new(lineId)], "movement did not occur"));

        var projected = await h.Authority.QueryAsync(OperationalMovementProjectionScope.All());
        Assert.Equal(2, projected.Activity.Count);
        Assert.Equal(new[] { MovementType.Out, MovementType.In },
            projected.Activity.OrderBy(x => x.MovementDate).Select(x => x.MovementType));
        Assert.Equal(2, projected.Activity.Select(x => x.EvidenceMovementId).Distinct().Count());
        Assert.Equal(0, projected.Activity.Sum(x => x.SignedQuantity));

        Assert.Equal(5, Assert.Single((await h.Authority.QueryAsync(
            OperationalMovementProjectionScope.PositionAsOf(new(2026, 9, 4)))).Positions).Quantity);
        Assert.Equal(0, Assert.Single((await h.Authority.QueryAsync(
            OperationalMovementProjectionScope.PositionAsOf(Harness.Today))).Positions).Quantity);
    }

    [Fact]
    public async Task Repeated_correction_projects_only_latest_generation_and_filters_after_relevance_validation()
    {
        await using var h = await Harness.CreateAsync();
        var root = await h.CreateSingleAsync(new(2026, 9, 1), h.CustomerId, 1, 3);
        var lineId = Assert.Single(await h.LineIdsAsync(root.RootId));
        await h.MutateAsync(root.RootId, 0,
            MovementMutationRequest.Correct(MovementMutationScope.Individual, [new(lineId)],
                "move corrected activity", movementDate: MovementFieldIntent<DateOnly>.Selected(new(2026, 9, 3)),
                customer: MovementFieldIntent<int>.Selected(h.OtherCustomerId),
                containerType: MovementFieldIntent<int>.Selected(2),
                quantity: MovementFieldIntent<int>.Selected(4)));
        await h.MutateAsync(root.RootId, 1,
            MovementMutationRequest.Correct(MovementMutationScope.Individual, [new(lineId)],
                "correct quantity again", quantity: MovementFieldIntent<int>.Selected(5)));

        var oldCoordinates = await h.Authority.QueryAsync(
            OperationalMovementProjectionScope.Activity(new(2026, 9, 1), new(2026, 9, 1),
                h.CustomerId, 1));
        Assert.Empty(oldCoordinates.Activity);
        var current = await h.Authority.QueryAsync(
            OperationalMovementProjectionScope.Activity(new(2026, 9, 3), new(2026, 9, 3),
                h.OtherCustomerId, 2));
        var movement = Assert.Single(current.Activity);
        Assert.Equal((2, 5, h.OtherCustomerId, 2),
            (movement.CurrentGeneration!.Value.Value, movement.Quantity,
                movement.CustomerId, movement.ContainerTypeId));
        Assert.Equal(5, movement.SignedQuantity);
    }

    [Fact]
    public async Task Mixed_root_restore_and_remain_reversed_emit_complete_current_contributions()
    {
        await using var h = await Harness.CreateAsync();
        var root = await h.CreateBatchAsync(2, 2, 2);
        var lines = await h.LineIdsAsync(root.RootId);
        await h.MutateAsync(root.RootId, 0,
            MovementMutationRequest.Reverse(MovementMutationScope.Individual,
                [new(lines[1])], "reverse second"));
        await h.MutateAsync(root.RootId, 1,
            MovementMutationRequest.Reverse(MovementMutationScope.Individual,
                [new(lines[2])], "reverse third"));
        await h.MutateAsync(root.RootId, 2,
            MovementMutationRequest.Correct(MovementMutationScope.WholeRoot,
                lines.Select(x => new LogicalMovementLineId(x)), "restore one and retain one",
                quantity: MovementFieldIntent<int>.Selected(2),
                reversedLineDecisions:
                [
                    ReversedLineDecision.Restore(new(lines[1])),
                    ReversedLineDecision.RemainReversed(new(lines[2]))
                ]));

        var projected = await h.Authority.QueryAsync(OperationalMovementProjectionScope.All());
        Assert.Equal(4, projected.Activity.Count);
        Assert.Equal(new[] { 1, 1, 2 }, projected.Activity.GroupBy(x => x.LogicalLineId)
            .Select(x => x.Count()).OrderBy(x => x));
        Assert.Equal(4, projected.Activity.Sum(x => x.SignedQuantity));
        Assert.Single(projected.Activity, x => x.Source == MovementSource.Batch &&
            x.LogicalLineId == new LogicalMovementLineId(lines[1]));
    }

    [Fact]
    public async Task Lineage_adjustment_and_excel_import_form_one_disjoint_operational_stream()
    {
        await using var h = await Harness.CreateAsync();
        await h.CreateSingleAsync(new(2026, 9, 1), h.CustomerId, 1, 4);
        await h.AddExcludedAsync(MovementSource.Adjustment, MovementType.Out, 2, importOwned: false);
        await h.AddExcludedAsync(MovementSource.ExcelImport, MovementType.In, 1, importOwned: true);

        var projected = await h.Authority.QueryAsync(
            OperationalMovementProjectionScope.PositionAsOf(Harness.Today));
        Assert.Equal(3, projected.Activity.Count);
        Assert.Equal(3, projected.Activity.Select(x => x.EvidenceMovementId).Distinct().Count());
        Assert.Single(projected.Activity, x => x.Domain == OperationalMovementDomain.LineageOrdinary);
        Assert.Single(projected.Activity, x => x.Domain == OperationalMovementDomain.Adjustment);
        Assert.Single(projected.Activity, x => x.Domain == OperationalMovementDomain.ExcelImport);
        Assert.Equal(5, Assert.Single(projected.Positions).Quantity);
    }

    [Fact]
    public async Task Adjustment_provenance_allows_standalone_and_import_owned_rows()
    {
        await using var h = await Harness.CreateAsync();
        await h.AddExcludedAsync(
            MovementSource.Adjustment, MovementType.Out, 2, importOwned: false);
        await h.AddExcludedAsync(
            MovementSource.Adjustment, MovementType.In, 1, importOwned: true);

        var projected = await h.Authority.QueryAsync(
            OperationalMovementProjectionScope.PositionAsOf(Harness.Today));

        Assert.Equal(2, projected.Activity.Count);
        Assert.All(projected.Activity,
            movement => Assert.Equal(OperationalMovementDomain.Adjustment, movement.Domain));
        Assert.Equal(1, Assert.Single(projected.Positions).Quantity);
    }

    [Theory]
    [InlineData(MovementSource.ExcelImport, false)]
    [InlineData(MovementSource.ExcelImport, true)]
    [InlineData(MovementSource.Adjustment, true)]
    public async Task Excluded_domain_rows_with_missing_or_dangling_required_provenance_fail_closed(
        MovementSource source,
        bool danglingImportRun)
    {
        await using var h = await Harness.CreateAsync();
        if (danglingImportRun)
            await h.AddExcludedWithDanglingImportRunAsync(source);
        else
            await h.AddExcludedAsync(source, MovementType.Out, 1, importOwned: false);

        var exception = await Assert.ThrowsAsync<OperationalMovementProjectionException>(() =>
            h.Authority.QueryAsync(OperationalMovementProjectionScope.All()));

        Assert.Equal(OperationalMovementProjectionFailure.InvalidExcludedDomain,
            exception.Failure);
    }

    [Fact]
    public async Task Invalid_root_fails_relevant_queries_but_not_a_provably_disjoint_customer_query()
    {
        await using var h = await Harness.CreateAsync();
        var healthy = await h.CreateSingleAsync(new(2026, 9, 1), h.CustomerId, 1, 4);
        var corrupt = await h.CreateSingleAsync(new(2026, 9, 1), h.OtherCustomerId, 1, 9);
        await h.SetRootStatusAsync(corrupt.RootId, LogicalMovementBatchStatus.Invalid);

        var narrow = await h.Authority.QueryAsync(
            OperationalMovementProjectionScope.All(h.CustomerId));
        Assert.Equal(healthy.MovementId, Assert.Single(narrow.Activity).EvidenceMovementId);

        var relevant = await Assert.ThrowsAsync<OperationalMovementProjectionException>(() =>
            h.Authority.QueryAsync(OperationalMovementProjectionScope.All(h.OtherCustomerId)));
        Assert.Equal(OperationalMovementProjectionFailure.RelevantLineageInvalid, relevant.Failure);
        await Assert.ThrowsAsync<OperationalMovementProjectionException>(() =>
            h.Authority.QueryAsync(OperationalMovementProjectionScope.All()));
    }

    [Fact]
    public async Task Incomplete_relevant_current_generation_fails_closed_without_raw_fallback()
    {
        await using var h = await Harness.CreateAsync();
        var root = await h.CreateSingleAsync(new(2026, 9, 1), h.CustomerId, 1, 6);
        await h.SetCurrentGenerationAsync(root.RootId, 99);

        var exception = await Assert.ThrowsAsync<OperationalMovementProjectionException>(() =>
            h.Authority.QueryAsync(OperationalMovementProjectionScope.All(h.CustomerId)));
        Assert.Equal(OperationalMovementProjectionFailure.RelevantLineageInvalid,
            exception.Failure);
    }

    [Fact]
    public async Task Unexpected_unrooted_ordinary_movement_fails_closed()
    {
        await using var h = await Harness.CreateAsync();
        await h.InsertUnrootedOrdinaryAsync(h.CustomerId);

        var exception = await Assert.ThrowsAsync<OperationalMovementProjectionException>(() =>
            h.Authority.QueryAsync(OperationalMovementProjectionScope.All(h.CustomerId)));
        Assert.Equal(OperationalMovementProjectionFailure.UnexpectedUnrootedOrdinary,
            exception.Failure);
    }

    [Fact]
    public async Task Unknown_relevance_from_malformed_unrooted_evidence_fails_closed()
    {
        await using var h = await Harness.CreateAsync();
        await h.InsertMalformedUnknownMovementAsync();

        var exception = await Assert.ThrowsAsync<OperationalMovementProjectionException>(() =>
            h.Authority.QueryAsync(OperationalMovementProjectionScope.All(h.CustomerId)));
        Assert.Equal(OperationalMovementProjectionFailure.UnknownRelevance, exception.Failure);
    }

    [Fact]
    public async Task Projection_backed_position_services_map_corrected_reversed_and_current_lineage()
    {
        await using var h = await Harness.CreateAsync(enableProjectionBackedServices: true);

        var corrected = await h.CreateSingleAsync(
            new(2026, 9, 1), h.CustomerId, 1, 7);
        var correctedLineId = Assert.Single(await h.LineIdsAsync(corrected.RootId));
        await h.MutateAsync(corrected.RootId, 0,
            MovementMutationRequest.Correct(
                MovementMutationScope.Individual,
                [new(correctedLineId)],
                "correct projected position",
                quantity: MovementFieldIntent<int>.Selected(9)));

        await h.CreateSingleAsync(
            new(2026, 9, 2), h.CustomerId, 2, 3, MovementType.In);

        var reversed = await h.CreateSingleAsync(
            new(2026, 9, 1), h.OtherCustomerId, 1, 4);
        var reversedLineId = Assert.Single(await h.LineIdsAsync(reversed.RootId));
        await h.MutateAsync(reversed.RootId, 0,
            MovementMutationRequest.Reverse(
                MovementMutationScope.Individual,
                [new(reversedLineId)],
                "reverse projected position"));

        var allBalances = await h.Balances.GetBalancesAsync();
        Assert.Collection(
            allBalances,
            row => Assert.Equal(
                (h.CustomerId, "Projection A", 1, "Blue Bin", 9, true, false),
                (row.CustomerId, row.CustomerName, row.ContainerTypeId,
                    row.ContainerTypeName, row.Balance, row.IsOutstanding, row.IsCredit)),
            row => Assert.Equal(
                (h.CustomerId, "Projection A", 2, "Small Bin", -3, false, true),
                (row.CustomerId, row.CustomerName, row.ContainerTypeId,
                    row.ContainerTypeName, row.Balance, row.IsOutstanding, row.IsCredit)),
            row => Assert.Equal(
                (h.OtherCustomerId, "Projection B", 1, "Blue Bin", 0, false, false),
                (row.CustomerId, row.CustomerName, row.ContainerTypeId,
                    row.ContainerTypeName, row.Balance, row.IsOutstanding, row.IsCredit)));

        var customerBalances = await h.Customers.GetBalancesAsync(h.CustomerId);
        Assert.Equal(
            new[]
            {
                ("Blue Bin", 9, "9 OUT"),
                ("Small Bin", -3, "3 CREDIT"),
                ("Yellow Bin", 0, "Even"),
                ("Bulk Bin", 0, "Even"),
                ("CHEP Pallet", 0, "Even")
            },
            customerBalances.Select(x => (x.ContainerType, x.Balance, x.Position)));

        var summary = await h.Movements.GetCustomerSummaryByCodeAsync("  proj-a  ");
        Assert.NotNull(summary);
        Assert.Equal((h.CustomerId, "PROJ-A", "Projection A"),
            (summary.CustomerId, summary.Code, summary.Name));
        Assert.Equal(
            new[]
            {
                (1, "Blue Bin", 9, "9 OUT"),
                (2, "Small Bin", -3, "3 CREDIT"),
                (3, "Yellow Bin", 0, "Even"),
                (4, "Bulk Bin", 0, "Even"),
                (5, "CHEP Pallet", 0, "Even")
            },
            summary.Balances.Select(x =>
                (x.ContainerTypeId, x.ContainerType, x.Balance, x.Position)));

        var container = await h.ContainerTypes.GetAsync(1);
        Assert.NotNull(container);
        Assert.Equal(5L, container.Usage.MovementCount);
        Assert.Equal(1, container.Usage.CustomersWithBalance);
        Assert.Equal(new DateOnly(2026, 9, 1), container.Usage.FirstUsed);
        Assert.Equal(Harness.Today, container.Usage.LastUsed);
        Assert.True(h.ProjectionAuthorityIsRegistered);
    }

    [Fact]
    public async Task Projection_backed_import_replacement_uses_corrected_pre_cutover_position()
    {
        await using var h = await Harness.CreateAsync(
            enableProjectionBackedServices: true,
            userRole: UserRole.Administrator);
        var cutover = new DateOnly(2026, 9, 4);

        var corrected = await h.CreateSingleAsync(
            new(2026, 9, 1), h.CustomerId, 1, 7);
        var correctedLine = Assert.Single(await h.LineIdsAsync(corrected.RootId));
        await h.MutateAsync(corrected.RootId, 0,
            MovementMutationRequest.Correct(
                MovementMutationScope.Individual,
                [new(correctedLine)],
                "correct pre-cutover quantity",
                quantity: MovementFieldIntent<int>.Selected(9)));

        var restored = await h.CreateSingleAsync(
            new(2026, 9, 2), h.CustomerId, 1, 4);
        var restoredLine = Assert.Single(await h.LineIdsAsync(restored.RootId));
        await h.MutateAsync(restored.RootId, 0,
            MovementMutationRequest.Reverse(
                MovementMutationScope.Individual,
                [new(restoredLine)],
                "reverse entry"));
        await h.MutateAsync(restored.RootId, 1,
            MovementMutationRequest.Restore(
                MovementMutationScope.Individual,
                [new(restoredLine)],
                "restore entry"));

        await h.CreateSingleAsync(cutover, h.CustomerId, 1, 100);
        await h.CreateSingleAsync(Harness.Today, h.CustomerId, 1, 200);
        await h.AddExcludedAsync(
            MovementSource.Adjustment, MovementType.Out, 2, importOwned: false,
            movementDate: new(2026, 9, 2));
        await h.AddExcludedAsync(
            MovementSource.ExcelImport, MovementType.In, 1, importOwned: true,
            movementDate: new(2026, 9, 3));
        var previousRunId = await h.CreatePreviousImportRunAsync(
            cutover,
            cutover.AddDays(-1));

        await using (var db = new BinTrackerDbContext(
                         new DbContextOptionsBuilder<BinTrackerDbContext>()
                             .UseSqlite(h.ConnectionString).Options))
        {
            var rawPreCutover = await db.BinMovements
                .Where(x => x.MovementDate < cutover && x.ImportRunId != previousRunId)
                .SumAsync(x => x.MovementType == MovementType.Out
                    ? x.Quantity
                    : -x.Quantity);
            Assert.Equal(18, rawPreCutover);
        }

        var projected = await h.Authority.QueryAsync(
            OperationalMovementProjectionScope.PositionAsOf(cutover.AddDays(-1)));
        Assert.Equal(25, Assert.Single(projected.Positions).Quantity);
        Assert.Equal(6, projected.Activity.Count);

        h.ClearProjectionServiceCalls();
        var comparison = await h.ImportExecution.CompareReplacementAsync(
            ReplacementRequest(h, previousRunId, cutover));

        var difference = Assert.Single(comparison.Differences);
        Assert.Equal(2, comparison.PreviousMovementCount);
        Assert.Equal(2, comparison.PreviousRun.MovementCount);
        Assert.Equal(2, comparison.ProposedMovementCount);
        Assert.Equal(11, difference.PreviousNetEffect);
        Assert.Equal(8, difference.ProposedNetEffect);
        Assert.Equal(-3, difference.Difference);

        var call = Assert.Single(h.ProjectionServiceCalls);
        Assert.True(call.IsPositionAsOf);
        Assert.Equal(cutover.AddDays(-1), call.ThroughDateInclusive);
        Assert.Null(call.FromDateInclusive);
        Assert.Null(call.CustomerId);
        Assert.Null(call.ContainerTypeId);
    }

    [Fact]
    public async Task Projection_backed_import_replacement_fails_closed_without_persistence_or_raw_fallback()
    {
        await using var h = await Harness.CreateAsync(
            enableProjectionBackedServices: true,
            userRole: UserRole.Administrator);
        var cutover = new DateOnly(2026, 9, 4);
        var root = await h.CreateSingleAsync(
            new(2026, 9, 1), h.CustomerId, 1, 6);
        await h.SetRootStatusAsync(root.RootId, LogicalMovementBatchStatus.Invalid);
        var previousRunId = await h.CreatePreviousImportRunAsync(cutover);

        long movementCount;
        long importRunCount;
        long auditCount;
        await using (var connection = await h.OpenAsync())
        {
            movementCount = await ScalarAsync(connection, "SELECT COUNT(*) FROM BinMovements;");
            importRunCount = await ScalarAsync(connection, "SELECT COUNT(*) FROM ImportRuns;");
            auditCount = await ScalarAsync(connection, "SELECT COUNT(*) FROM AuditEvents;");
        }

        h.ClearProjectionServiceCalls();
        var failure = await Assert.ThrowsAsync<OperationalMovementProjectionException>(() =>
            h.ImportExecution.CompareReplacementAsync(
                ReplacementRequest(h, previousRunId, cutover)));

        Assert.Equal(
            OperationalMovementProjectionFailure.RelevantLineageInvalid,
            failure.Failure);
        Assert.Single(h.ProjectionServiceCalls);
        await using var verify = await h.OpenAsync();
        Assert.Equal(movementCount,
            await ScalarAsync(verify, "SELECT COUNT(*) FROM BinMovements;"));
        Assert.Equal(importRunCount,
            await ScalarAsync(verify, "SELECT COUNT(*) FROM ImportRuns;"));
        Assert.Equal(auditCount,
            await ScalarAsync(verify, "SELECT COUNT(*) FROM AuditEvents;"));
    }

    [Fact]
    public async Task Projection_backed_import_replacement_handles_minimum_cutover_without_underflow()
    {
        await using var h = await Harness.CreateAsync(
            enableProjectionBackedServices: true,
            userRole: UserRole.Administrator);
        var previousRunId = await h.CreatePreviousImportRunAsync(DateOnly.MinValue);

        h.ClearProjectionServiceCalls();
        var comparison = await h.ImportExecution.CompareReplacementAsync(
            ReplacementRequest(h, previousRunId, DateOnly.MinValue));

        Assert.Equal(2, comparison.PreviousMovementCount);
        Assert.Equal(2, comparison.ProposedMovementCount);
        Assert.Empty(h.ProjectionServiceCalls);
    }

    [Fact]
    public async Task Projection_backed_import_replacement_executes_from_the_corrected_transaction_snapshot()
    {
        await using var h = await Harness.CreateAsync(
            enableProjectionBackedServices: true,
            userRole: UserRole.Administrator);
        var cutover = new DateOnly(2026, 9, 4);

        var corrected = await h.CreateSingleAsync(
            new(2026, 9, 1), h.CustomerId, 1, 7);
        var correctedLine = Assert.Single(await h.LineIdsAsync(corrected.RootId));
        await h.MutateAsync(corrected.RootId, 0,
            MovementMutationRequest.Correct(
                MovementMutationScope.Individual,
                [new(correctedLine)],
                "correct pre-cutover quantity",
                quantity: MovementFieldIntent<int>.Selected(9)));

        var restored = await h.CreateSingleAsync(
            new(2026, 9, 2), h.CustomerId, 1, 4);
        var restoredLine = Assert.Single(await h.LineIdsAsync(restored.RootId));
        await h.MutateAsync(restored.RootId, 0,
            MovementMutationRequest.Reverse(
                MovementMutationScope.Individual,
                [new(restoredLine)],
                "reverse entry"));
        await h.MutateAsync(restored.RootId, 1,
            MovementMutationRequest.Restore(
                MovementMutationScope.Individual,
                [new(restoredLine)],
                "restore entry"));

        await h.CreateSingleAsync(cutover, h.CustomerId, 1, 100);
        await h.CreateSingleAsync(Harness.Today, h.CustomerId, 1, 200);
        await h.AddExcludedAsync(
            MovementSource.Adjustment, MovementType.Out, 2, importOwned: false,
            movementDate: new(2026, 9, 2));
        await h.AddExcludedAsync(
            MovementSource.ExcelImport, MovementType.In, 1, importOwned: true,
            movementDate: new(2026, 9, 3));
        var previousRunId = await h.CreatePreviousImportRunAsync(
            cutover,
            cutover.AddDays(-1));

        await using (var db = new BinTrackerDbContext(
                         new DbContextOptionsBuilder<BinTrackerDbContext>()
                             .UseSqlite(h.ConnectionString).Options))
        {
            var rawPreCutover = await db.BinMovements
                .Where(x => x.MovementDate < cutover && x.ImportRunId != previousRunId)
                .SumAsync(x => x.MovementType == MovementType.Out
                    ? x.Quantity
                    : -x.Quantity);
            Assert.Equal(18, rawPreCutover);
        }

        h.ClearProjectionServiceCalls();
        var result = await h.ImportExecution.ExecuteAsync(
            await ExecutableReplacementRequestAsync(h, previousRunId, cutover));

        var call = Assert.Single(h.ProjectionServiceCalls);
        Assert.Equal(1, h.ProjectionTransactionCallCount);
        Assert.True(call.IsPositionAsOf);
        Assert.Equal(cutover.AddDays(-1), call.ThroughDateInclusive);

        await using var verify = new BinTrackerDbContext(
            new DbContextOptionsBuilder<BinTrackerDbContext>()
                .UseSqlite(h.ConnectionString).Options);
        var oldRun = await verify.ImportRuns.SingleAsync(x => x.Id == previousRunId);
        var newRun = await verify.ImportRuns.SingleAsync(x => x.Id == result.ImportRunId);
        Assert.Equal("Replaced", oldRun.Status);
        Assert.Null(oldRun.CurrentCutoverDate);
        Assert.Equal(previousRunId, newRun.ReplacesImportRunId);
        Assert.Equal(0, await verify.BinMovements.CountAsync(x => x.ImportRunId == previousRunId));

        var generated = await verify.BinMovements
            .Where(x => x.ImportRunId == result.ImportRunId)
            .ToListAsync();
        Assert.Equal(2, generated.Count);
        Assert.Contains(generated, x =>
            x.Source == MovementSource.Adjustment &&
            x.MovementType == MovementType.Out && x.Quantity == 6);
        Assert.Contains(generated, x =>
            x.Source == MovementSource.ExcelImport &&
            x.MovementType == MovementType.Out && x.Quantity == 2);
        Assert.Contains("\"PreviousBinTrackerBalance\":14",
            Assert.IsType<string>(newRun.OpeningReconciliationChangesJson));
        Assert.Contains("\"Difference\":-3",
            Assert.IsType<string>(newRun.CorrectionChangesJson));

        Assert.Equal(2, await verify.BinMovements.CountAsync(x =>
            x.Source == MovementSource.Manual && x.MovementDate >= cutover &&
            (x.Quantity == 100 || x.Quantity == 200)));
        var finalPosition = await h.Authority.QueryAsync(
            OperationalMovementProjectionScope.PositionAsOf(DateOnly.MaxValue));
        Assert.Equal(322, Assert.Single(finalPosition.Positions).Quantity);
    }

    [Fact]
    public async Task Projection_backed_new_import_preserves_the_whole_ledger_temporal_scope()
    {
        await using var h = await Harness.CreateAsync(
            enableProjectionBackedServices: true,
            userRole: UserRole.Administrator);
        var cutover = new DateOnly(2026, 9, 4);
        await h.CreateSingleAsync(new(2026, 9, 1), h.CustomerId, 1, 9);
        await h.CreateSingleAsync(Harness.Today, h.CustomerId, 1, 4);
        await h.AddExcludedAsync(
            MovementSource.Adjustment, MovementType.Out, 2, importOwned: false,
            movementDate: new(2026, 9, 2));
        await h.AddExcludedAsync(
            MovementSource.ExcelImport, MovementType.In, 1, importOwned: true,
            movementDate: new(2026, 9, 3));

        h.ClearProjectionServiceCalls();
        var result = await h.ImportExecution.ExecuteAsync(
            await NewImportRequestAsync(h, cutover, "new import source"));

        var call = Assert.Single(h.ProjectionServiceCalls);
        Assert.Equal(1, h.ProjectionTransactionCallCount);
        Assert.True(call.IsPositionAsOf);
        Assert.Equal(DateOnly.MaxValue, call.ThroughDateInclusive);

        await using var verify = new BinTrackerDbContext(
            new DbContextOptionsBuilder<BinTrackerDbContext>()
                .UseSqlite(h.ConnectionString).Options);
        var adjustment = await verify.BinMovements.SingleAsync(x =>
            x.ImportRunId == result.ImportRunId && x.Source == MovementSource.Adjustment);
        Assert.Equal(6, adjustment.Quantity);
        var run = await verify.ImportRuns.SingleAsync(x => x.Id == result.ImportRunId);
        Assert.Contains("\"PreviousBinTrackerBalance\":14",
            Assert.IsType<string>(run.OpeningReconciliationChangesJson));
    }

    [Fact]
    public async Task Projection_backed_replacement_execution_failure_preserves_every_prior_artifact()
    {
        await using var h = await Harness.CreateAsync(
            enableProjectionBackedServices: true,
            userRole: UserRole.Administrator);
        var root = await h.CreateSingleAsync(
            new(2026, 9, 1), h.CustomerId, 1, 6);
        await h.SetRootStatusAsync(root.RootId, LogicalMovementBatchStatus.Invalid);
        var cutover = new DateOnly(2026, 9, 4);
        var previousRunId = await h.CreatePreviousImportRunAsync(
            cutover,
            cutover.AddDays(-1));

        long customerCount;
        long movementCount;
        long importRunCount;
        long auditCount;
        await using (var connection = await h.OpenAsync())
        {
            customerCount = await ScalarAsync(connection, "SELECT COUNT(*) FROM Customers;");
            movementCount = await ScalarAsync(connection, "SELECT COUNT(*) FROM BinMovements;");
            importRunCount = await ScalarAsync(connection, "SELECT COUNT(*) FROM ImportRuns;");
            auditCount = await ScalarAsync(connection, "SELECT COUNT(*) FROM AuditEvents;");
        }

        h.ClearProjectionServiceCalls();
        var request = await ExecutableReplacementRequestAsync(
            h,
            previousRunId,
            cutover);
        var failure = await Assert.ThrowsAsync<OperationalMovementProjectionException>(() =>
            h.ImportExecution.ExecuteAsync(request));

        Assert.Equal(
            OperationalMovementProjectionFailure.RelevantLineageInvalid,
            failure.Failure);
        Assert.Single(h.ProjectionServiceCalls);
        Assert.Equal(1, h.ProjectionTransactionCallCount);
        await using var verify = await h.OpenAsync();
        Assert.Equal(customerCount,
            await ScalarAsync(verify, "SELECT COUNT(*) FROM Customers;"));
        Assert.Equal(movementCount,
            await ScalarAsync(verify, "SELECT COUNT(*) FROM BinMovements;"));
        Assert.Equal(importRunCount,
            await ScalarAsync(verify, "SELECT COUNT(*) FROM ImportRuns;"));
        Assert.Equal(auditCount,
            await ScalarAsync(verify, "SELECT COUNT(*) FROM AuditEvents;"));
        await using var command = verify.CreateCommand();
        command.CommandText =
            "SELECT COUNT(*) FROM ImportRuns WHERE Id=$id AND Status='Completed' AND CurrentCutoverDate='2026-09-04';";
        command.Parameters.AddWithValue("$id", previousRunId);
        Assert.Equal(1L, Convert.ToInt64(await command.ExecuteScalarAsync()));
        Assert.Equal(2L, await ScalarAsync(verify,
            $"SELECT COUNT(*) FROM BinMovements WHERE ImportRunId={previousRunId};"));
    }

    [Fact]
    public async Task Projection_cancellation_propagates_and_rolls_back_import_execution()
    {
        await using var h = await Harness.CreateAsync(
            enableProjectionBackedServices: true,
            userRole: UserRole.Administrator);
        await h.CreateSingleAsync(new(2026, 9, 1), h.CustomerId, 1, 6);
        var request = await NewImportRequestAsync(h, new(2026, 9, 4), "cancel source");

        long customerCount;
        long importRunCount;
        long movementCount;
        long auditCount;
        await using (var connection = await h.OpenAsync())
        {
            customerCount = await ScalarAsync(connection, "SELECT COUNT(*) FROM Customers;");
            importRunCount = await ScalarAsync(connection, "SELECT COUNT(*) FROM ImportRuns;");
            movementCount = await ScalarAsync(connection, "SELECT COUNT(*) FROM BinMovements;");
            auditCount = await ScalarAsync(connection, "SELECT COUNT(*) FROM AuditEvents;");
        }

        h.ClearProjectionServiceCalls();
        h.CancelNextProjectionTransaction();
        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            h.ImportExecution.ExecuteAsync(request));

        Assert.Single(h.ProjectionServiceCalls);
        Assert.Equal(1, h.ProjectionTransactionCallCount);
        await using var verify = await h.OpenAsync();
        Assert.Equal(customerCount,
            await ScalarAsync(verify, "SELECT COUNT(*) FROM Customers;"));
        Assert.Equal(importRunCount,
            await ScalarAsync(verify, "SELECT COUNT(*) FROM ImportRuns;"));
        Assert.Equal(movementCount,
            await ScalarAsync(verify, "SELECT COUNT(*) FROM BinMovements;"));
        Assert.Equal(auditCount,
            await ScalarAsync(verify, "SELECT COUNT(*) FROM AuditEvents;"));
    }

    [Fact]
    public async Task Projection_backed_customer_reads_map_corrected_coordinates_without_superseded_activity()
    {
        await using var h = await Harness.CreateAsync(enableProjectionBackedServices: true);
        var statementStart = new DateOnly(2026, 9, 3);
        var statementEnd = new DateOnly(2026, 9, 4);

        await h.CreateSingleAsync(new(2026, 9, 1), h.CustomerId, 1, 2);
        var crossing = await h.CreateSingleAsync(new(2026, 9, 2), h.CustomerId, 1, 3);
        var crossingLineId = Assert.Single(await h.LineIdsAsync(crossing.RootId));
        await h.MutateAsync(crossing.RootId, 0,
            MovementMutationRequest.Correct(
                MovementMutationScope.Individual,
                [new(crossingLineId)],
                "move activity across the statement boundary and container",
                movementDate: MovementFieldIntent<DateOnly>.Selected(statementStart),
                containerType: MovementFieldIntent<int>.Selected(2),
                quantity: MovementFieldIntent<int>.Selected(4)));

        var movedCustomer = await h.CreateSingleAsync(
            new(2026, 9, 2), h.CustomerId, 1, 5);
        var movedCustomerLineId = Assert.Single(await h.LineIdsAsync(movedCustomer.RootId));
        await h.MutateAsync(movedCustomer.RootId, 0,
            MovementMutationRequest.Correct(
                MovementMutationScope.Individual,
                [new(movedCustomerLineId)],
                "move activity to the correct customer and container",
                movementDate: MovementFieldIntent<DateOnly>.Selected(statementStart),
                customer: MovementFieldIntent<int>.Selected(h.OtherCustomerId),
                containerType: MovementFieldIntent<int>.Selected(3),
                quantity: MovementFieldIntent<int>.Selected(6)));

        var laterEvidence = await h.CreateSingleAsync(
            statementStart, h.CustomerId, 2, 7);
        var projected = await h.Authority.QueryAsync(
            OperationalMovementProjectionScope.All(h.CustomerId));
        var correctedEvidenceId = Assert.Single(projected.Activity, x => x.Quantity == 4)
            .EvidenceMovementId;
        Assert.True(laterEvidence.MovementId > correctedEvidenceId);
        Assert.Equal(3, projected.Activity.Count);

        var statement = await h.Customers.GetStatementAsync(
            h.CustomerId, statementStart, statementEnd);
        var statementCall = Assert.Single(h.ProjectionServiceCalls);
        Assert.True(statementCall.IsPositionAsOf);
        Assert.Equal((statementEnd, h.CustomerId),
            (statementCall.ThroughDateInclusive, statementCall.CustomerId));
        Assert.Collection(
            statement.Containers,
            blue =>
            {
                Assert.Equal(("Blue Bin", 2, 2),
                    (blue.ContainerType, blue.OpeningBalance, blue.ClosingBalance));
                Assert.Empty(blue.Movements);
            },
            small =>
            {
                Assert.Equal(("Small Bin", 0, 11),
                    (small.ContainerType, small.OpeningBalance, small.ClosingBalance));
                Assert.Equal(
                    new[]
                    {
                        (statementStart, 4, 4),
                        (statementStart, 7, 11)
                    },
                    small.Movements.Select(x => (x.Date, x.Quantity, x.RunningBalance)));
            });

        var search = await h.Customers.SearchAsync(null, includeInactive: true);
        Assert.Equal(13, Assert.Single(search, x => x.Id == h.CustomerId).NetBalance);
        Assert.Equal(6, Assert.Single(search, x => x.Id == h.OtherCustomerId).NetBalance);

        var recent = await h.Customers.GetRecentMovementsAsync(h.CustomerId);
        Assert.Equal(
            new[]
            {
                (statementStart, "Small Bin", 7),
                (statementStart, "Small Bin", 4),
                (new DateOnly(2026, 9, 1), "Blue Bin", 2)
            },
            recent.Select(x => (x.Date, x.ContainerType, x.Quantity)));
        var otherRecent = Assert.Single(
            await h.Customers.GetRecentMovementsAsync(h.OtherCustomerId));
        Assert.Equal((statementStart, "Yellow Bin", 6),
            (otherRecent.Date, otherRecent.ContainerType, otherRecent.Quantity));
    }

    [Fact]
    public async Task Projection_backed_customer_reads_preserve_reversed_current_truth()
    {
        await using var h = await Harness.CreateAsync(enableProjectionBackedServices: true);
        var originalDate = new DateOnly(2026, 9, 2);
        var root = await h.CreateSingleAsync(originalDate, h.CustomerId, 1, 5);
        var lineId = Assert.Single(await h.LineIdsAsync(root.RootId));
        await h.MutateAsync(root.RootId, 0,
            MovementMutationRequest.Reverse(
                MovementMutationScope.Individual,
                [new(lineId)],
                "the dispatch did not occur"));

        var search = await h.Customers.SearchAsync(null, includeInactive: true);
        Assert.Equal(0, Assert.Single(search, x => x.Id == h.CustomerId).NetBalance);

        var recent = await h.Customers.GetRecentMovementsAsync(h.CustomerId);
        Assert.Equal(
            new[]
            {
                (Harness.Today, "IN (Returned)", 5),
                (originalDate, "OUT (Taken)", 5)
            },
            recent.Select(x => (x.Date, x.Direction, x.Quantity)));

        var statement = await h.Customers.GetStatementAsync(
            h.CustomerId, new(2026, 9, 3), Harness.Today);
        var blue = Assert.Single(statement.Containers);
        Assert.Equal(("Blue Bin", 5, 0),
            (blue.ContainerType, blue.OpeningBalance, blue.ClosingBalance));
        var reversal = Assert.Single(blue.Movements);
        Assert.Equal((Harness.Today, "IN (Returned)", 5, 0),
            (reversal.Date, reversal.Direction, reversal.Quantity, reversal.RunningBalance));
    }

    [Fact]
    public async Task Projection_backed_dashboard_uses_one_result_for_corrected_and_reversed_truth()
    {
        await using var h = await Harness.CreateAsync(enableProjectionBackedServices: true);
        await h.SetAttentionThresholdAsync(5);

        var corrected = await h.CreateSingleAsync(
            Harness.Today, h.CustomerId, 1, 3);
        var correctedLineId = Assert.Single(await h.LineIdsAsync(corrected.RootId));
        await h.MutateAsync(corrected.RootId, 0,
            MovementMutationRequest.Correct(
                MovementMutationScope.Individual,
                [new(correctedLineId)],
                "correct dashboard customer container and quantity",
                customer: MovementFieldIntent<int>.Selected(h.OtherCustomerId),
                containerType: MovementFieldIntent<int>.Selected(2),
                quantity: MovementFieldIntent<int>.Selected(6)));

        var reversed = await h.CreateSingleAsync(
            Harness.Today, h.CustomerId, 3, 4);
        var reversedLineId = Assert.Single(await h.LineIdsAsync(reversed.RootId));
        await h.MutateAsync(reversed.RootId, 0,
            MovementMutationRequest.Reverse(
                MovementMutationScope.Individual,
                [new(reversedLineId)],
                "reverse dashboard movement"));

        await h.CreateSingleAsync(
            Harness.Today, h.OtherCustomerId, 1, 7);
        await h.AddExcludedAsync(
            MovementSource.Adjustment, MovementType.Out, 2, importOwned: false,
            movementDate: Harness.Today);
        await h.AddExcludedAsync(
            MovementSource.ExcelImport, MovementType.In, 1, importOwned: true,
            movementDate: Harness.Today);

        var summary = await h.Movements.GetDashboardSummaryAsync(Harness.Today);

        Assert.Equal(
            new OperationalDashboardSummary(
                ReturnedToday: 5,
                TakenToday: 19,
                Outstanding: 14,
                RequiresAttention: 1),
            summary);
        var call = Assert.Single(h.ProjectionServiceCalls);
        Assert.True(call.IsPositionAsOf);
        Assert.Equal(Harness.Today, call.ThroughDateInclusive);
        Assert.Null(call.FromDateInclusive);
        Assert.Null(call.CustomerId);
        Assert.Null(call.ContainerTypeId);

        await h.SetRootStatusAsync(
            corrected.RootId, LogicalMovementBatchStatus.Invalid);
        var failure = await Assert.ThrowsAsync<OperationalMovementProjectionException>(() =>
            h.Movements.GetDashboardSummaryAsync(Harness.Today));
        Assert.Equal(
            OperationalMovementProjectionFailure.RelevantLineageInvalid,
            failure.Failure);
    }

    [Fact]
    public async Task Projection_backed_outstanding_maps_corrected_reversed_excluded_and_as_of_truth_once()
    {
        await using var h = await Harness.CreateAsync(enableProjectionBackedServices: true);
        var reportDate = new DateOnly(2026, 9, 4);

        var corrected = await h.CreateSingleAsync(
            new(2026, 9, 1), h.CustomerId, 1, 7);
        var correctedLineId = Assert.Single(await h.LineIdsAsync(corrected.RootId));
        await h.MutateAsync(corrected.RootId, 0,
            MovementMutationRequest.Correct(
                MovementMutationScope.Individual,
                [new(correctedLineId)],
                "correct outstanding quantity",
                quantity: MovementFieldIntent<int>.Selected(9)));

        var reversed = await h.CreateSingleAsync(
            new(2026, 9, 1), h.CustomerId, 2, 5);
        var reversedLineId = Assert.Single(await h.LineIdsAsync(reversed.RootId));
        await h.MutateAsync(reversed.RootId, 0,
            MovementMutationRequest.Reverse(
                MovementMutationScope.Individual,
                [new(reversedLineId)],
                "reverse outstanding movement"));

        await h.CreateSingleAsync(Harness.Today, h.CustomerId, 1, 100);
        await h.AddExcludedAsync(
            MovementSource.Adjustment, MovementType.Out, 2, importOwned: false,
            movementDate: new(2026, 9, 3));
        await h.AddExcludedAsync(
            MovementSource.ExcelImport, MovementType.In, 1, importOwned: true,
            movementDate: reportDate);

        var historical = await h.Outstanding.QueryAsync(new OutstandingReportQuery(
            reportDate,
            BalanceFilter: OutstandingBalanceFilter.AllNonZero));

        Assert.Collection(
            historical.Rows,
            row => Assert.Equal(
                (1, 10, reportDate),
                (row.ContainerTypeId, row.Balance, row.LastMovementDate)),
            row => Assert.Equal(
                (2, 5, new DateOnly(2026, 9, 1)),
                (row.ContainerTypeId, row.Balance, row.LastMovementDate)));
        Assert.Equal(
            new[]
            {
                (1, 10, 0, 1),
                (2, 5, 0, 1)
            },
            historical.ContainerTotals.Select(x =>
                (x.ContainerTypeId, x.OutstandingQuantity, x.CreditQuantity,
                    x.PositionCount)));

        var current = await h.Outstanding.QueryAsync(new OutstandingReportQuery(
            Harness.Today,
            BalanceFilter: OutstandingBalanceFilter.AllNonZero));
        var currentRow = Assert.Single(current.Rows);
        Assert.Equal((1, 110, Harness.Today),
            (currentRow.ContainerTypeId, currentRow.Balance, currentRow.LastMovementDate));

        Assert.Equal(2, h.ProjectionServiceCalls.Count);
        Assert.All(h.ProjectionServiceCalls, call =>
        {
            Assert.True(call.IsPositionAsOf);
            Assert.Null(call.FromDateInclusive);
            Assert.Null(call.CustomerId);
            Assert.Null(call.ContainerTypeId);
        });
        Assert.Equal(reportDate, h.ProjectionServiceCalls[0].ThroughDateInclusive);
        Assert.Equal(Harness.Today, h.ProjectionServiceCalls[1].ThroughDateInclusive);
    }

    [Fact]
    public async Task Projection_backed_outstanding_preserves_filters_metadata_order_and_visible_totals()
    {
        await using var h = await Harness.CreateAsync(enableProjectionBackedServices: true);

        await h.CreateSingleAsync(new(2026, 9, 1), h.CustomerId, 1, 3);
        await h.CreateSingleAsync(
            new(2026, 9, 2), h.CustomerId, 2, 2, MovementType.In);
        await h.CreateSingleAsync(new(2026, 9, 2), h.CustomerId, 3, 4);
        await h.CreateSingleAsync(
            new(2026, 9, 3), h.CustomerId, 3, 4, MovementType.In);
        await h.CreateSingleAsync(new(2026, 9, 1), h.OtherCustomerId, 1, 1);

        await using (var db = new BinTrackerDbContext(
                         new DbContextOptionsBuilder<BinTrackerDbContext>()
                             .UseSqlite(h.ConnectionString).Options))
        {
            (await db.Customers.SingleAsync(x => x.Id == h.CustomerId)).IsActive = false;
            (await db.ContainerTypes.SingleAsync(x => x.Id == 2)).IsActive = false;
            await db.SaveChangesAsync();
        }

        var all = await h.Outstanding.QueryAsync(new OutstandingReportQuery(
            Harness.Today,
            BalanceFilter: OutstandingBalanceFilter.AllNonZero));
        Assert.Collection(
            all.Rows,
            row => Assert.Equal(
                ("PROJ-A", "Projection A", false, 1, "Blue Bin", 1, 3),
                (row.CustomerCode, row.CustomerName, row.IsActive,
                    row.ContainerTypeId, row.ContainerType,
                    row.ContainerDisplayOrder, row.Balance)),
            row => Assert.Equal(
                ("PROJ-A", "Projection A", false, 2, "Small Bin", 2, -2),
                (row.CustomerCode, row.CustomerName, row.IsActive,
                    row.ContainerTypeId, row.ContainerType,
                    row.ContainerDisplayOrder, row.Balance)),
            row => Assert.Equal(
                ("PROJ-B", "Projection B", true, 1, "Blue Bin", 1, 1),
                (row.CustomerCode, row.CustomerName, row.IsActive,
                    row.ContainerTypeId, row.ContainerType,
                    row.ContainerDisplayOrder, row.Balance)));
        Assert.DoesNotContain(all.Rows, row => row.ContainerTypeId == 3);
        Assert.Equal(
            new[]
            {
                (1, 4, 0, 2),
                (2, 0, 2, 1)
            },
            all.ContainerTotals.Select(x =>
                (x.ContainerTypeId, x.OutstandingQuantity, x.CreditQuantity,
                    x.PositionCount)));

        var customerFiltered = await h.Outstanding.QueryAsync(new OutstandingReportQuery(
            Harness.Today,
            CustomerSearch: "  projection a  ",
            BalanceFilter: OutstandingBalanceFilter.AllNonZero));
        Assert.Equal(2, customerFiltered.Rows.Count);
        Assert.All(customerFiltered.Rows,
            row => Assert.Equal(h.CustomerId, row.CustomerId));

        var containerFiltered = await h.Outstanding.QueryAsync(new OutstandingReportQuery(
            Harness.Today,
            ContainerTypeId: 2,
            BalanceFilter: OutstandingBalanceFilter.CreditsOnly));
        var credit = Assert.Single(containerFiltered.Rows);
        Assert.Equal((h.CustomerId, 2, -2),
            (credit.CustomerId, credit.ContainerTypeId, credit.Balance));

        var outstandingOnly = await h.Outstanding.QueryAsync(new OutstandingReportQuery(
            Harness.Today,
            BalanceFilter: OutstandingBalanceFilter.OutstandingOnly));
        Assert.Equal(new[] { 3, 1 }, outstandingOnly.Rows.Select(x => x.Balance));

        var activeOnly = await h.Outstanding.QueryAsync(new OutstandingReportQuery(
            Harness.Today,
            BalanceFilter: OutstandingBalanceFilter.AllNonZero,
            IncludeInactiveCustomers: false));
        Assert.Equal(h.OtherCustomerId, Assert.Single(activeOnly.Rows).CustomerId);

        Assert.Equal(5, h.ProjectionServiceCalls.Count);
        Assert.Equal(2, h.ProjectionServiceCalls[2].ContainerTypeId);
        Assert.All(h.ProjectionServiceCalls, call =>
        {
            Assert.True(call.IsPositionAsOf);
            Assert.Equal(Harness.Today, call.ThroughDateInclusive);
            Assert.Null(call.CustomerId);
        });
    }

    [Fact]
    public async Task Projection_backed_outstanding_propagates_integrity_failure_without_raw_fallback()
    {
        await using var h = await Harness.CreateAsync(enableProjectionBackedServices: true);
        var root = await h.CreateSingleAsync(
            new(2026, 9, 1), h.CustomerId, 1, 6);
        await h.SetRootStatusAsync(root.RootId, LogicalMovementBatchStatus.Invalid);

        var failure = await Assert.ThrowsAsync<OperationalMovementProjectionException>(() =>
            h.Outstanding.QueryAsync(new OutstandingReportQuery(Harness.Today)));

        Assert.Equal(
            OperationalMovementProjectionFailure.RelevantLineageInvalid,
            failure.Failure);
        Assert.Single(h.ProjectionServiceCalls);
    }

    [Fact]
    public async Task Projection_backed_daily_maps_corrected_reversed_and_excluded_activity_once()
    {
        await using var h = await Harness.CreateAsync(enableProjectionBackedServices: true);
        var reportDate = new DateOnly(2026, 9, 3);

        var corrected = await h.CreateSingleAsync(
            new(2026, 9, 1), h.CustomerId, 1, 7);
        var correctedLineId = Assert.Single(await h.LineIdsAsync(corrected.RootId));
        await h.MutateAsync(corrected.RootId, 0,
            MovementMutationRequest.Correct(
                MovementMutationScope.Individual,
                [new(correctedLineId)],
                "correct daily movement coordinates",
                movementDate: MovementFieldIntent<DateOnly>.Selected(reportDate),
                direction: MovementFieldIntent<MovementType>.Selected(MovementType.In),
                customer: MovementFieldIntent<int>.Selected(h.OtherCustomerId),
                containerType: MovementFieldIntent<int>.Selected(2),
                quantity: MovementFieldIntent<int>.Selected(9),
                reference: MovementFieldIntent<string>.Selected("daily-ref"),
                notes: MovementFieldIntent<string>.Selected("daily-notes")));
        var correctedRoles = await h.MovementIdsByRoleAsync(corrected.RootId);
        var replacementId = correctedRoles[LogicalMovementTransformationRole.CorrectionReplacement];
        var neutraliserId = correctedRoles[LogicalMovementTransformationRole.CorrectionNeutraliser];

        var reversed = await h.CreateSingleAsync(
            reportDate, h.CustomerId, 1, 5);
        var reversedLineId = Assert.Single(await h.LineIdsAsync(reversed.RootId));
        await h.MutateAsync(reversed.RootId, 0,
            MovementMutationRequest.Reverse(
                MovementMutationScope.Individual,
                [new(reversedLineId)],
                "reverse daily movement"));
        var reversalId = (await h.MovementIdsByRoleAsync(reversed.RootId))
            [LogicalMovementTransformationRole.OrdinaryReversal];

        var unselected = await h.CreateSingleAsync(
            Harness.Today, h.CustomerId, 3, 100);
        await h.AddExcludedAsync(
            MovementSource.Adjustment, MovementType.Out, 2, importOwned: false,
            movementDate: reportDate);
        await h.AddExcludedAsync(
            MovementSource.ExcelImport, MovementType.In, 1, importOwned: true,
            movementDate: reportDate);

        await using (var db = new BinTrackerDbContext(
                         new DbContextOptionsBuilder<BinTrackerDbContext>()
                             .UseSqlite(h.ConnectionString).Options))
        {
            (await db.Customers.SingleAsync(x => x.Id == h.OtherCustomerId)).IsActive = false;
            (await db.ContainerTypes.SingleAsync(x => x.Id == 2)).IsActive = false;
            await db.SaveChangesAsync();
        }

        var daily = await h.Daily.QueryAsync(new(reportDate));

        Assert.Equal(reportDate, daily.ReportDate);
        Assert.Equal(3, daily.Rows.Count);
        Assert.DoesNotContain(daily.Rows, x => x.MovementId == corrected.MovementId);
        Assert.DoesNotContain(daily.Rows, x => x.MovementId == neutraliserId);
        Assert.DoesNotContain(daily.Rows, x => x.MovementId == unselected.MovementId);
        Assert.DoesNotContain(daily.Rows, x => x.Source == MovementSource.Adjustment);
        Assert.Single(daily.Rows, x => x.MovementId == replacementId);
        Assert.Single(daily.Rows, x => x.MovementId == reversed.MovementId);
        Assert.Single(daily.Rows, x => x.Source == MovementSource.ExcelImport);
        Assert.Equal(
            new[]
            {
                ("PROJ-A", MovementType.In, MovementSource.ExcelImport),
                ("PROJ-A", MovementType.Out, MovementSource.Manual),
                ("PROJ-B", MovementType.In, MovementSource.Manual)
            },
            daily.Rows.Select(x => (x.CustomerCode, x.Direction, x.Source)));
        var replacement = Assert.Single(daily.Rows, x => x.MovementId == replacementId);
        Assert.Equal(
            (reportDate, h.OtherCustomerId, "PROJ-B", "Projection B", 2,
                "Small Bin", 2, MovementType.In, 9, MovementSource.Manual,
                "daily-ref", "daily-notes", "projection-operator"),
            (replacement.MovementDate, replacement.CustomerId, replacement.CustomerCode,
                replacement.CustomerName, replacement.ContainerTypeId,
                replacement.ContainerType, replacement.ContainerDisplayOrder,
                replacement.Direction, replacement.Quantity, replacement.Source,
                replacement.Reference, replacement.Notes, replacement.EnteredBy));
        Assert.Equal((5, 10), (daily.OutQuantity, daily.InQuantity));
        Assert.Equal(
            new[]
            {
                (1, "Blue Bin", 1, 5, 1),
                (2, "Small Bin", 2, 0, 9)
            },
            daily.ContainerTotals.Select(x =>
                (x.ContainerTypeId, x.ContainerType, x.DisplayOrder,
                    x.OutQuantity, x.InQuantity)));

        var excel = await h.Daily.QueryAsync(new(reportDate,
            Source: MovementSource.ExcelImport));
        Assert.Equal(MovementSource.ExcelImport, Assert.Single(excel.Rows).Source);

        Assert.Empty((await h.Daily.QueryAsync(new(reportDate,
            Source: MovementSource.Adjustment))).Rows);
        var adjustment = await h.Daily.QueryAsync(new(reportDate,
            Source: MovementSource.Adjustment,
            IncludeAdjustments: true));
        Assert.Equal(MovementSource.Adjustment, Assert.Single(adjustment.Rows).Source);

        var filtered = await h.Daily.QueryAsync(new(reportDate,
            CustomerSearch: "  projection b  ",
            ContainerTypeId: 2,
            Direction: MovementType.In,
            Source: MovementSource.Manual));
        Assert.Equal(replacementId, Assert.Single(filtered.Rows).MovementId);

        Assert.Empty((await h.Daily.QueryAsync(new(new DateOnly(2026, 9, 1)))).Rows);

        var clamped = await h.Daily.QueryAsync(new(Harness.Today.AddDays(1)));
        Assert.Equal(Harness.Today, clamped.ReportDate);
        Assert.Single(clamped.Rows, x => x.MovementId == reversalId);
        Assert.Single(clamped.Rows, x => x.MovementId == unselected.MovementId);

        Assert.Equal(7, h.ProjectionServiceCalls.Count);
        Assert.All(h.ProjectionServiceCalls, call =>
        {
            Assert.False(call.IsPositionAsOf);
            Assert.Equal(call.FromDateInclusive, call.ThroughDateInclusive);
            Assert.Null(call.CustomerId);
        });
        Assert.Equal(2, h.ProjectionServiceCalls[4].ContainerTypeId);
        Assert.Equal(Harness.Today, h.ProjectionServiceCalls[^1].ThroughDateInclusive);
    }

    [Fact]
    public async Task Projection_backed_daily_propagates_integrity_failure_without_raw_fallback()
    {
        await using var h = await Harness.CreateAsync(enableProjectionBackedServices: true);
        var root = await h.CreateSingleAsync(
            new(2026, 9, 3), h.CustomerId, 1, 6);
        await h.SetRootStatusAsync(root.RootId, LogicalMovementBatchStatus.Invalid);

        var failure = await Assert.ThrowsAsync<OperationalMovementProjectionException>(() =>
            h.Daily.QueryAsync(new(new DateOnly(2026, 9, 3))));

        Assert.Equal(
            OperationalMovementProjectionFailure.RelevantLineageInvalid,
            failure.Failure);
        Assert.Single(h.ProjectionServiceCalls);
    }

    [Fact]
    public async Task Projection_backed_daily_fails_closed_when_integer_totals_overflow()
    {
        await using var h = await Harness.CreateAsync(enableProjectionBackedServices: true);
        var reportDate = new DateOnly(2026, 9, 3);
        await h.CreateSingleAsync(reportDate, h.CustomerId, 1, int.MaxValue);
        var moved = await h.CreateSingleAsync(
            reportDate, h.OtherCustomerId, 1, 1);
        var movedLineId = Assert.Single(await h.LineIdsAsync(moved.RootId));
        await h.MutateAsync(moved.RootId, 0,
            MovementMutationRequest.Correct(
                MovementMutationScope.Individual,
                [new(movedLineId)],
                "move daily activity across int boundary",
                customer: MovementFieldIntent<int>.Selected(h.CustomerId)));

        await Assert.ThrowsAsync<OverflowException>(() =>
            h.Daily.QueryAsync(new(reportDate)));

        Assert.Single(h.ProjectionServiceCalls);
    }

    [Fact]
    public async Task Projection_backed_weekly_maps_corrected_reversed_and_excluded_activity_once()
    {
        await using var h = await Harness.CreateAsync(enableProjectionBackedServices: true);
        var currentWeekDate = new DateOnly(2026, 9, 2);

        var corrected = await h.CreateSingleAsync(
            new(2026, 8, 30), h.CustomerId, 1, 7);
        var correctedLineId = Assert.Single(await h.LineIdsAsync(corrected.RootId));
        await h.MutateAsync(corrected.RootId, 0,
            MovementMutationRequest.Correct(
                MovementMutationScope.Individual,
                [new(correctedLineId)],
                "move weekly activity into the authoritative week",
                movementDate: MovementFieldIntent<DateOnly>.Selected(currentWeekDate),
                direction: MovementFieldIntent<MovementType>.Selected(MovementType.In),
                customer: MovementFieldIntent<int>.Selected(h.OtherCustomerId),
                containerType: MovementFieldIntent<int>.Selected(2),
                quantity: MovementFieldIntent<int>.Selected(9),
                reference: MovementFieldIntent<string>.Selected("weekly-ref"),
                notes: MovementFieldIntent<string>.Selected("weekly-notes")));
        var firstRoles = await h.MovementIdsByRoleAsync(corrected.RootId);
        var firstReplacementId = firstRoles[
            LogicalMovementTransformationRole.CorrectionReplacement];
        var firstNeutraliserId = firstRoles[
            LogicalMovementTransformationRole.CorrectionNeutraliser];

        await h.MutateAsync(corrected.RootId, 1,
            MovementMutationRequest.Correct(
                MovementMutationScope.Individual,
                [new(correctedLineId)],
                "move weekly activity to its final authoritative day",
                movementDate: MovementFieldIntent<DateOnly>.Selected(new(2026, 9, 3)),
                notes: MovementFieldIntent<string>.Selected("weekly-final-notes")));
        var currentRoles = await h.MovementIdsByRoleAsync(corrected.RootId);
        var finalReplacementId = currentRoles[
            LogicalMovementTransformationRole.CorrectionReplacement];
        var finalNeutraliserId = currentRoles[
            LogicalMovementTransformationRole.CorrectionNeutraliser];

        var reversed = await h.CreateSingleAsync(
            new(2026, 9, 4), h.CustomerId, 1, 5);
        var reversedLineId = Assert.Single(await h.LineIdsAsync(reversed.RootId));
        await h.MutateAsync(reversed.RootId, 0,
            MovementMutationRequest.Reverse(
                MovementMutationScope.Individual,
                [new(reversedLineId)],
                "reverse weekly movement"));
        var reversalId = (await h.MovementIdsByRoleAsync(reversed.RootId))[
            LogicalMovementTransformationRole.OrdinaryReversal];

        var outside = await h.CreateSingleAsync(
            new(2026, 8, 20), h.CustomerId, 3, 100);
        await h.AddExcludedAsync(
            MovementSource.Adjustment, MovementType.Out, 2, importOwned: false,
            movementDate: new(2026, 9, 1));
        await h.AddExcludedAsync(
            MovementSource.ExcelImport, MovementType.In, 1, importOwned: true,
            movementDate: new(2026, 9, 1));
        await h.AddExcludedAsync(
            MovementSource.ExcelImport, MovementType.Out, 99, importOwned: true,
            movementDate: Harness.Today.AddDays(1));

        await using (var db = new BinTrackerDbContext(
                         new DbContextOptionsBuilder<BinTrackerDbContext>()
                             .UseSqlite(h.ConnectionString).Options))
        {
            (await db.Customers.SingleAsync(x => x.Id == h.OtherCustomerId)).IsActive = false;
            (await db.ContainerTypes.SingleAsync(x => x.Id == 2)).IsActive = false;
            await db.SaveChangesAsync();
        }

        var weekly = await h.Weekly.QueryAsync(new(currentWeekDate));

        Assert.Equal((new DateOnly(2026, 8, 31), new DateOnly(2026, 9, 6),
            Harness.Today), (weekly.WeekStart, weekly.WeekEnd, weekly.DataThroughDate));
        Assert.Equal(4, weekly.Rows.Count);
        Assert.DoesNotContain(weekly.Rows, x => x.MovementId == corrected.MovementId);
        Assert.DoesNotContain(weekly.Rows, x => x.MovementId == firstNeutraliserId);
        Assert.DoesNotContain(weekly.Rows, x => x.MovementId == firstReplacementId);
        Assert.DoesNotContain(weekly.Rows, x => x.MovementId == finalNeutraliserId);
        Assert.DoesNotContain(weekly.Rows, x => x.MovementId == outside.MovementId);
        Assert.Single(weekly.Rows, x => x.MovementId == finalReplacementId);
        Assert.Single(weekly.Rows, x => x.MovementId == reversed.MovementId);
        Assert.Single(weekly.Rows, x => x.MovementId == reversalId);
        Assert.Single(weekly.Rows, x => x.Source == MovementSource.ExcelImport);
        Assert.DoesNotContain(weekly.Rows, x => x.MovementDate > Harness.Today);
        Assert.DoesNotContain(weekly.Rows,
            x => x.Source == MovementSource.Adjustment);
        Assert.Equal(
            new[]
            {
                (new DateOnly(2026, 9, 1), "PROJ-A", MovementType.In,
                    MovementSource.ExcelImport),
                (new DateOnly(2026, 9, 3), "PROJ-B", MovementType.In,
                    MovementSource.Manual),
                (new DateOnly(2026, 9, 4), "PROJ-A", MovementType.Out,
                    MovementSource.Manual),
                (Harness.Today, "PROJ-A", MovementType.In,
                    MovementSource.Manual)
            },
            weekly.Rows.Select(x =>
                (x.MovementDate, x.CustomerCode, x.Direction, x.Source)));

        var replacement = Assert.Single(weekly.Rows,
            x => x.MovementId == finalReplacementId);
        Assert.Equal(
            (new DateOnly(2026, 9, 3), h.OtherCustomerId, "PROJ-B", "Projection B",
                2, "Small Bin", 2, MovementType.In, 9, MovementSource.Manual,
                "weekly-ref", "weekly-final-notes", "projection-operator"),
            (replacement.MovementDate, replacement.CustomerId, replacement.CustomerCode,
                replacement.CustomerName, replacement.ContainerTypeId,
                replacement.ContainerType, replacement.ContainerDisplayOrder,
                replacement.Direction, replacement.Quantity, replacement.Source,
                replacement.Reference, replacement.Notes, replacement.EnteredBy));
        Assert.Equal((5, 15, -10),
            (weekly.OutQuantity, weekly.InQuantity, weekly.NetQuantity));
        Assert.Equal(
            new[]
            {
                (h.CustomerId, "PROJ-A", 1, "Blue Bin", 1, 5, 6, -1),
                (h.OtherCustomerId, "PROJ-B", 2, "Small Bin", 2, 0, 9, -9)
            },
            weekly.Summary.Select(x =>
                (x.CustomerId, x.CustomerCode, x.ContainerTypeId, x.ContainerType,
                    x.ContainerDisplayOrder, x.OutQuantity, x.InQuantity, x.NetQuantity)));

        Assert.Single(h.ProjectionServiceCalls);
        var currentWeekCall = h.ProjectionServiceCalls[0];
        Assert.False(currentWeekCall.IsPositionAsOf);
        Assert.Equal(new DateOnly(2026, 8, 31), currentWeekCall.FromDateInclusive);
        Assert.Equal(Harness.Today, currentWeekCall.ThroughDateInclusive);
        Assert.Null(currentWeekCall.CustomerId);
        Assert.Null(currentWeekCall.ContainerTypeId);

        var sourceWeek = await h.Weekly.QueryAsync(new(new DateOnly(2026, 8, 30)));
        Assert.Empty(sourceWeek.Rows);
        Assert.Equal(2, h.ProjectionServiceCalls.Count);

        var excel = await h.Weekly.QueryAsync(new(currentWeekDate,
            Source: MovementSource.ExcelImport));
        Assert.Equal(MovementSource.ExcelImport, Assert.Single(excel.Rows).Source);

        Assert.Empty((await h.Weekly.QueryAsync(new(currentWeekDate,
            Source: MovementSource.Adjustment))).Rows);
        var adjustment = await h.Weekly.QueryAsync(new(currentWeekDate,
            Source: MovementSource.Adjustment,
            IncludeAdjustments: true));
        Assert.Equal(MovementSource.Adjustment, Assert.Single(adjustment.Rows).Source);

        var filtered = await h.Weekly.QueryAsync(new(currentWeekDate,
            CustomerSearch: "  projection b  ",
            ContainerTypeId: 2,
            Source: MovementSource.Manual));
        Assert.Equal(finalReplacementId, Assert.Single(filtered.Rows).MovementId);
        Assert.Equal(2, h.ProjectionServiceCalls[^1].ContainerTypeId);

        var clamped = await h.Weekly.QueryAsync(new(Harness.Today.AddDays(14)));
        Assert.Equal((new DateOnly(2026, 8, 31), Harness.Today),
            (clamped.WeekStart, clamped.DataThroughDate));
        Assert.Equal(weekly.Rows.Select(x => x.MovementId),
            clamped.Rows.Select(x => x.MovementId));

        Assert.Equal(7, h.ProjectionServiceCalls.Count);
        Assert.All(h.ProjectionServiceCalls, call =>
        {
            Assert.False(call.IsPositionAsOf);
            Assert.NotNull(call.FromDateInclusive);
            Assert.NotNull(call.ThroughDateInclusive);
            Assert.Null(call.CustomerId);
        });
        Assert.Equal(Harness.Today,
            h.ProjectionServiceCalls[^1].ThroughDateInclusive);
    }

    [Fact]
    public async Task Projection_backed_weekly_propagates_integrity_failure_without_raw_fallback()
    {
        await using var h = await Harness.CreateAsync(enableProjectionBackedServices: true);
        var root = await h.CreateSingleAsync(
            new(2026, 9, 3), h.CustomerId, 1, 6);
        await h.SetRootStatusAsync(root.RootId, LogicalMovementBatchStatus.Invalid);

        var failure = await Assert.ThrowsAsync<OperationalMovementProjectionException>(() =>
            h.Weekly.QueryAsync(new(new DateOnly(2026, 9, 3))));

        Assert.Equal(
            OperationalMovementProjectionFailure.RelevantLineageInvalid,
            failure.Failure);
        Assert.Single(h.ProjectionServiceCalls);
    }

    [Fact]
    public async Task Projection_backed_weekly_fails_closed_when_integer_totals_overflow()
    {
        await using var h = await Harness.CreateAsync(enableProjectionBackedServices: true);
        var reportDate = new DateOnly(2026, 9, 3);
        await h.CreateSingleAsync(reportDate, h.CustomerId, 1, int.MaxValue);
        var moved = await h.CreateSingleAsync(
            reportDate, h.OtherCustomerId, 1, 1);
        var movedLineId = Assert.Single(await h.LineIdsAsync(moved.RootId));
        await h.MutateAsync(moved.RootId, 0,
            MovementMutationRequest.Correct(
                MovementMutationScope.Individual,
                [new(movedLineId)],
                "move weekly activity across int boundary",
                customer: MovementFieldIntent<int>.Selected(h.CustomerId)));

        await Assert.ThrowsAsync<OverflowException>(() =>
            h.Weekly.QueryAsync(new(reportDate)));

        Assert.Single(h.ProjectionServiceCalls);
    }

    [Fact]
    public async Task Projection_backed_monthly_preserves_corrected_reversed_filtered_and_grouped_truth_once()
    {
        await using var h = await Harness.CreateAsync(enableProjectionBackedServices: true);

        var corrected = await h.CreateSingleAsync(
            new(2026, 8, 30), h.CustomerId, 1, 7);
        var correctedLineId = Assert.Single(await h.LineIdsAsync(corrected.RootId));
        await h.MutateAsync(corrected.RootId, 0,
            MovementMutationRequest.Correct(
                MovementMutationScope.Individual,
                [new(correctedLineId)],
                "move monthly activity into the authoritative month",
                movementDate: MovementFieldIntent<DateOnly>.Selected(new(2026, 9, 2)),
                direction: MovementFieldIntent<MovementType>.Selected(MovementType.In),
                customer: MovementFieldIntent<int>.Selected(h.OtherCustomerId),
                containerType: MovementFieldIntent<int>.Selected(2),
                quantity: MovementFieldIntent<int>.Selected(9)));
        await h.MutateAsync(corrected.RootId, 1,
            MovementMutationRequest.Correct(
                MovementMutationScope.Individual,
                [new(correctedLineId)],
                "keep only the final monthly replacement",
                movementDate: MovementFieldIntent<DateOnly>.Selected(new(2026, 9, 3)),
                quantity: MovementFieldIntent<int>.Selected(6)));

        var reversed = await h.CreateSingleAsync(
            new(2026, 9, 4), h.CustomerId, 1, 5);
        var reversedLineId = Assert.Single(await h.LineIdsAsync(reversed.RootId));
        await h.MutateAsync(reversed.RootId, 0,
            MovementMutationRequest.Reverse(
                MovementMutationScope.Individual,
                [new(reversedLineId)],
                "reverse monthly movement"));

        await h.AddExcludedAsync(
            MovementSource.Adjustment, MovementType.Out, 2, importOwned: false,
            movementDate: new(2026, 9, 1));
        await h.AddExcludedAsync(
            MovementSource.ExcelImport, MovementType.In, 1, importOwned: true,
            movementDate: new(2026, 9, 1));
        await h.AddExcludedAsync(
            MovementSource.ExcelImport, MovementType.Out, 99, importOwned: true,
            movementDate: Harness.Today.AddDays(1));

        await using (var db = new BinTrackerDbContext(
                         new DbContextOptionsBuilder<BinTrackerDbContext>()
                             .UseSqlite(h.ConnectionString).Options))
        {
            (await db.Customers.SingleAsync(x => x.Id == h.OtherCustomerId)).IsActive = false;
            (await db.ContainerTypes.SingleAsync(x => x.Id == 2)).IsActive = false;
            await db.SaveChangesAsync();
        }

        var monthly = await h.Monthly.QueryAsync(
            new MonthlySummaryReportQuery(new DateOnly(2026, 9, 20)));

        Assert.Equal((new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 30),
            Harness.Today),
            (monthly.MonthStart, monthly.MonthEnd, monthly.DataThroughDate));
        Assert.Equal(
            new[]
            {
                (h.CustomerId, "PROJ-A", "Projection A", 1, "Blue Bin", 1,
                    5, 6, -1),
                (h.OtherCustomerId, "PROJ-B", "Projection B", 2, "Small Bin", 2,
                    0, 6, -6)
            },
            monthly.Rows.Select(x =>
                (x.CustomerId, x.CustomerCode, x.CustomerName,
                    x.ContainerTypeId, x.ContainerType, x.ContainerDisplayOrder,
                    x.OutQuantity, x.InQuantity, x.NetQuantity)));
        Assert.Equal(
            new[]
            {
                (1, "Blue Bin", 1, 5, 6, -1),
                (2, "Small Bin", 2, 0, 6, -6)
            },
            monthly.ContainerTotals.Select(x =>
                (x.ContainerTypeId, x.ContainerType, x.DisplayOrder,
                    x.OutQuantity, x.InQuantity, x.NetQuantity)));
        Assert.Equal((5, 12, -7),
            (monthly.OutQuantity, monthly.InQuantity, monthly.NetQuantity));
        Assert.Single(h.ProjectionServiceCalls);

        var sourceMonth = await h.Monthly.QueryAsync(
            new MonthlySummaryReportQuery(new DateOnly(2026, 8, 1)));
        Assert.Empty(sourceMonth.Rows);

        var excel = await h.Monthly.QueryAsync(new MonthlySummaryReportQuery(
            new DateOnly(2026, 9, 1), Source: MovementSource.ExcelImport));
        Assert.Equal((0, 1), (excel.OutQuantity, excel.InQuantity));

        Assert.Empty((await h.Monthly.QueryAsync(new MonthlySummaryReportQuery(
            new DateOnly(2026, 9, 1),
            Source: MovementSource.Adjustment))).Rows);
        var adjustment = await h.Monthly.QueryAsync(new MonthlySummaryReportQuery(
            new DateOnly(2026, 9, 1),
            Source: MovementSource.Adjustment,
            IncludeAdjustments: true));
        Assert.Equal((2, 0), (adjustment.OutQuantity, adjustment.InQuantity));

        var filtered = await h.Monthly.QueryAsync(new MonthlySummaryReportQuery(
            new DateOnly(2026, 9, 1),
            CustomerSearch: "  projection b  ",
            ContainerTypeId: 2,
            Source: MovementSource.Manual));
        Assert.Equal((h.OtherCustomerId, 2, 0, 6),
            (Assert.Single(filtered.Rows).CustomerId,
                Assert.Single(filtered.Rows).ContainerTypeId,
                filtered.OutQuantity, filtered.InQuantity));

        var clamped = await h.Monthly.QueryAsync(new MonthlySummaryReportQuery(
            new DateOnly(2026, 10, 1)));
        Assert.Equal((new DateOnly(2026, 9, 1), Harness.Today),
            (clamped.MonthStart, clamped.DataThroughDate));
        Assert.Equal(monthly.Rows, clamped.Rows);

        Assert.Equal(7, h.ProjectionServiceCalls.Count);
        Assert.All(h.ProjectionServiceCalls, call =>
        {
            Assert.False(call.IsPositionAsOf);
            Assert.NotNull(call.FromDateInclusive);
            Assert.NotNull(call.ThroughDateInclusive);
            Assert.Null(call.CustomerId);
        });
        Assert.Equal(new DateOnly(2026, 8, 1),
            h.ProjectionServiceCalls[1].FromDateInclusive);
        Assert.Equal(new DateOnly(2026, 8, 31),
            h.ProjectionServiceCalls[1].ThroughDateInclusive);
        Assert.Equal(2, h.ProjectionServiceCalls[5].ContainerTypeId);
        Assert.Equal(Harness.Today,
            h.ProjectionServiceCalls[^1].ThroughDateInclusive);
    }

    [Fact]
    public async Task Projection_backed_monthly_propagates_integrity_failure_without_raw_fallback()
    {
        await using var h = await Harness.CreateAsync(enableProjectionBackedServices: true);
        var root = await h.CreateSingleAsync(
            new(2026, 9, 3), h.CustomerId, 1, 6);
        await h.SetRootStatusAsync(root.RootId, LogicalMovementBatchStatus.Invalid);

        var failure = await Assert.ThrowsAsync<OperationalMovementProjectionException>(() =>
            h.Monthly.QueryAsync(new MonthlySummaryReportQuery(
                new DateOnly(2026, 9, 1))));

        Assert.Equal(
            OperationalMovementProjectionFailure.RelevantLineageInvalid,
            failure.Failure);
        Assert.Single(h.ProjectionServiceCalls);
    }

    [Fact]
    public async Task Projection_backed_monthly_fails_closed_when_integer_totals_overflow()
    {
        await using var h = await Harness.CreateAsync(enableProjectionBackedServices: true);
        var reportMonth = new DateOnly(2026, 9, 1);
        await h.CreateSingleAsync(reportMonth, h.CustomerId, 1, int.MaxValue);
        var moved = await h.CreateSingleAsync(
            reportMonth, h.OtherCustomerId, 1, 1);
        var movedLineId = Assert.Single(await h.LineIdsAsync(moved.RootId));
        await h.MutateAsync(moved.RootId, 0,
            MovementMutationRequest.Correct(
                MovementMutationScope.Individual,
                [new(movedLineId)],
                "move monthly activity across int boundary",
                customer: MovementFieldIntent<int>.Selected(h.CustomerId)));

        await Assert.ThrowsAsync<OverflowException>(() =>
            h.Monthly.QueryAsync(new MonthlySummaryReportQuery(reportMonth)));

        Assert.Single(h.ProjectionServiceCalls);
    }

    [Fact]
    public async Task Projection_backed_market_floor_uses_one_as_of_result_for_corrected_reversed_and_excluded_truth()
    {
        await using var h = await Harness.CreateAsync(enableProjectionBackedServices: true);

        var corrected = await h.CreateSingleAsync(
            new(2026, 8, 30), h.CustomerId, 1, 7);
        var correctedLineId = Assert.Single(await h.LineIdsAsync(corrected.RootId));
        await h.MutateAsync(corrected.RootId, 0,
            MovementMutationRequest.Correct(
                MovementMutationScope.Individual,
                [new(correctedLineId)],
                "move every market-floor coordinate",
                movementDate: MovementFieldIntent<DateOnly>.Selected(new(2026, 9, 2)),
                direction: MovementFieldIntent<MovementType>.Selected(MovementType.In),
                customer: MovementFieldIntent<int>.Selected(h.OtherCustomerId),
                containerType: MovementFieldIntent<int>.Selected(3),
                quantity: MovementFieldIntent<int>.Selected(9)));
        await h.MutateAsync(corrected.RootId, 1,
            MovementMutationRequest.Correct(
                MovementMutationScope.Individual,
                [new(correctedLineId)],
                "retain only the current corrected market-floor generation",
                movementDate: MovementFieldIntent<DateOnly>.Selected(new(2026, 9, 3)),
                quantity: MovementFieldIntent<int>.Selected(6)));

        var reversed = await h.CreateSingleAsync(
            new(2026, 9, 4), h.CustomerId, 1, 5);
        var reversedLineId = Assert.Single(await h.LineIdsAsync(reversed.RootId));
        await h.MutateAsync(reversed.RootId, 0,
            MovementMutationRequest.Reverse(
                MovementMutationScope.Individual,
                [new(reversedLineId)],
                "reverse market-floor movement"));

        await h.AddExcludedAsync(
            MovementSource.Adjustment, MovementType.Out, 2, importOwned: false,
            movementDate: Harness.Today);
        await h.AddExcludedAsync(
            MovementSource.ExcelImport, MovementType.In, 1, importOwned: true,
            movementDate: Harness.Today);
        await h.SetCustomerTypeAsync(h.OtherCustomerId, CustomerType.CashCod);

        var supersededDate = await h.MarketFloor.GetAsync(new(2026, 9, 2));
        Assert.All(supersededDate.AccountDaily, x => Assert.Equal(0, x.Total));
        Assert.All(supersededDate.CashDaily, x => Assert.Equal(0, x.Total));

        var current = await h.MarketFloor.GetAsync(Harness.Today);

        Assert.Equal(
            new[]
            {
                (h.CustomerId, "PROJ-A", "Blue", 0, 6, 7, 1)
            },
            current.AccountDaily.Select(x =>
                (x.CustomerId, x.Buyer, x.Container,
                    x.Out, x.In, x.BroughtForward, x.Total)));
        Assert.Equal(
            new[]
            {
                (h.OtherCustomerId, "PROJ-B", "Yellow", 0, 0, -6, -6)
            },
            current.CashDaily.Select(x =>
                (x.CustomerId, x.Buyer, x.Container,
                    x.Out, x.In, x.BroughtForward, x.Total)));
        Assert.Equal(
            new[] { (h.CustomerId, "Blue", 1) },
            current.AccountOwing.Select(x =>
                (x.CustomerId, x.Container, x.Total)));
        Assert.Equal(
            new[] { (h.OtherCustomerId, "Yellow", -6) },
            current.CashOwing.Select(x =>
                (x.CustomerId, x.Container, x.Total)));
        Assert.Empty(current.Credits);
        Assert.Empty(current.SpecialContainers);

        Assert.Equal(2, h.ProjectionServiceCalls.Count);
        Assert.All(h.ProjectionServiceCalls, call =>
        {
            Assert.True(call.IsPositionAsOf);
            Assert.Null(call.FromDateInclusive);
            Assert.Null(call.CustomerId);
            Assert.Null(call.ContainerTypeId);
        });
        Assert.Equal(new DateOnly(2026, 9, 2),
            h.ProjectionServiceCalls[0].ThroughDateInclusive);
        Assert.Equal(Harness.Today,
            h.ProjectionServiceCalls[1].ThroughDateInclusive);
    }

    [Fact]
    public async Task Projection_backed_market_floor_preserves_metadata_eligibility_zero_rows_and_order()
    {
        await using var h = await Harness.CreateAsync(enableProjectionBackedServices: true);
        var emptyCustomerId = await h.AddCustomerAsync(
            "EMPTY", "No History", CustomerType.Account);
        var specialCustomerId = await h.AddCustomerAsync(
            "SPECIAL", "Special Only", CustomerType.CashCod);

        await h.CreateSingleAsync(Harness.Today, h.CustomerId, 2, 4);
        await h.CreateSingleAsync(Harness.Today, h.OtherCustomerId, 1, 9);
        await h.CreateSingleAsync(Harness.Today, specialCustomerId, 5, 6);
        await h.SetContainerActiveAsync(2, false);
        await h.SetCustomerActiveAsync(h.OtherCustomerId, false);

        var result = await h.MarketFloor.GetAsync(Harness.Today);

        Assert.Equal(
            new[]
            {
                (emptyCustomerId, "EMPTY", "Blue", 0),
                (h.CustomerId, "PROJ-A", "Unknown", 4)
            },
            result.AccountDaily.Select(x =>
                (x.CustomerId, x.Buyer, x.Container, x.Total)));
        Assert.Equal(
            new[] { (specialCustomerId, "SPECIAL", "Blue", 0) },
            result.CashDaily.Select(x =>
                (x.CustomerId, x.Buyer, x.Container, x.Total)));
        Assert.Equal(
            new[] { ("SPECIAL", "CHEP Pallet", 6) },
            result.SpecialContainers.Select(x =>
                (x.Buyer, x.Container, x.Balance)));
        Assert.DoesNotContain(result.AccountDaily,
            x => x.CustomerId == h.OtherCustomerId);
        Assert.Single(h.ProjectionServiceCalls);
    }

    [Fact]
    public async Task Projection_backed_market_floor_propagates_integrity_failure_without_raw_fallback()
    {
        await using var h = await Harness.CreateAsync(enableProjectionBackedServices: true);
        var root = await h.CreateSingleAsync(
            new(2026, 9, 3), h.CustomerId, 1, 6);
        await h.SetRootStatusAsync(root.RootId, LogicalMovementBatchStatus.Invalid);

        var failure = await Assert.ThrowsAsync<OperationalMovementProjectionException>(() =>
            h.MarketFloor.GetAsync(Harness.Today));

        Assert.Equal(
            OperationalMovementProjectionFailure.RelevantLineageInvalid,
            failure.Failure);
        Assert.Single(h.ProjectionServiceCalls);
    }

    [Fact]
    public async Task Projection_backed_market_floor_fails_closed_when_position_narrowing_overflows()
    {
        await using var h = await Harness.CreateAsync(enableProjectionBackedServices: true);
        await h.CreateSingleAsync(
            new(2026, 9, 1), h.CustomerId, 1, int.MaxValue);
        var moved = await h.CreateSingleAsync(
            new(2026, 9, 2), h.OtherCustomerId, 1, 1);
        var movedLineId = Assert.Single(await h.LineIdsAsync(moved.RootId));
        await h.MutateAsync(moved.RootId, 0,
            MovementMutationRequest.Correct(
                MovementMutationScope.Individual,
                [new(movedLineId)],
                "move market-floor position across int boundary",
                customer: MovementFieldIntent<int>.Selected(h.CustomerId)));

        await Assert.ThrowsAsync<OverflowException>(() =>
            h.MarketFloor.GetAsync(Harness.Today));

        Assert.Single(h.ProjectionServiceCalls);
    }

    [Fact]
    public async Task Projection_backed_daily_print_pack_uses_delegated_corrected_position_and_activity_truth()
    {
        await using var h = await Harness.CreateAsync(enableProjectionBackedServices: true);

        var corrected = await h.CreateSingleAsync(
            new(2026, 9, 1), h.CustomerId, 1, 7);
        var correctedLineId = Assert.Single(await h.LineIdsAsync(corrected.RootId));
        await h.MutateAsync(corrected.RootId, 0,
            MovementMutationRequest.Correct(
                MovementMutationScope.Individual,
                [new(correctedLineId)],
                "move daily print-pack coordinates",
                movementDate: MovementFieldIntent<DateOnly>.Selected(new(2026, 9, 2)),
                direction: MovementFieldIntent<MovementType>.Selected(MovementType.In),
                customer: MovementFieldIntent<int>.Selected(h.OtherCustomerId),
                containerType: MovementFieldIntent<int>.Selected(3),
                quantity: MovementFieldIntent<int>.Selected(9)));
        await h.MutateAsync(corrected.RootId, 1,
            MovementMutationRequest.Correct(
                MovementMutationScope.Individual,
                [new(correctedLineId)],
                "retain only the current print-pack generation",
                movementDate: MovementFieldIntent<DateOnly>.Selected(Harness.Today),
                direction: MovementFieldIntent<MovementType>.Selected(MovementType.Out),
                quantity: MovementFieldIntent<int>.Selected(4)));
        var replacementId = (await h.MovementIdsByRoleAsync(corrected.RootId))
            [LogicalMovementTransformationRole.CorrectionReplacement];

        var reversed = await h.CreateSingleAsync(
            Harness.Today.AddDays(-1), h.CustomerId, 1, 5);
        var reversedLineId = Assert.Single(await h.LineIdsAsync(reversed.RootId));
        await h.MutateAsync(reversed.RootId, 0,
            MovementMutationRequest.Reverse(
                MovementMutationScope.Individual,
                [new(reversedLineId)],
                "reverse daily print-pack movement"));
        var reversalId = (await h.MovementIdsByRoleAsync(reversed.RootId))
            [LogicalMovementTransformationRole.OrdinaryReversal];

        await h.AddExcludedAsync(
            MovementSource.Adjustment, MovementType.Out, 2, importOwned: false,
            movementDate: Harness.Today);
        await h.AddExcludedAsync(
            MovementSource.ExcelImport, MovementType.In, 1, importOwned: true,
            movementDate: Harness.Today);
        await h.AddExcludedAsync(
            MovementSource.ExcelImport, MovementType.Out, 99, importOwned: true,
            movementDate: Harness.Today.AddDays(1));

        var outstanding = await h.Outstanding.QueryAsync(new(
            Harness.Today,
            BalanceFilter: OutstandingBalanceFilter.OutstandingOnly,
            IncludeInactiveCustomers: false));
        Assert.Equal(
            new[]
            {
                (h.CustomerId, "PROJ-A", 1, "Blue Bin", 1),
                (h.OtherCustomerId, "PROJ-B", 3, "Yellow Bin", 4)
            },
            outstanding.Rows.Select(x =>
                (x.CustomerId, x.CustomerCode, x.ContainerTypeId,
                    x.ContainerType, x.Balance)));
        Assert.Equal((1, 4),
            (outstanding.ContainerTotals[0].OutstandingQuantity,
                outstanding.ContainerTotals[1].OutstandingQuantity));

        var daily = await h.Daily.QueryAsync(new(
            Harness.Today,
            IncludeAdjustments: false));
        Assert.Equal(3, daily.Rows.Count);
        Assert.DoesNotContain(daily.Rows, x => x.MovementId == corrected.MovementId);
        Assert.DoesNotContain(daily.Rows, x => x.Source == MovementSource.Adjustment);
        Assert.DoesNotContain(daily.Rows, x => x.Quantity == 99);
        Assert.Single(daily.Rows, x => x.MovementId == replacementId);
        Assert.Single(daily.Rows, x => x.MovementId == reversalId);
        Assert.Single(daily.Rows, x => x.Source == MovementSource.ExcelImport);
        var replacement = Assert.Single(daily.Rows, x => x.MovementId == replacementId);
        Assert.Equal(
            (Harness.Today, h.OtherCustomerId, "PROJ-B", 3, "Yellow Bin",
                MovementType.Out, 4, MovementSource.Manual, "projection",
                "projection-operator"),
            (replacement.MovementDate, replacement.CustomerId,
                replacement.CustomerCode, replacement.ContainerTypeId,
                replacement.ContainerType, replacement.Direction,
                replacement.Quantity, replacement.Source, replacement.Reference,
                replacement.EnteredBy));
        Assert.Equal((4, 6), (daily.OutQuantity, daily.InQuantity));
        Assert.Equal(
            new[]
            {
                (1, "Blue Bin", 0, 6),
                (3, "Yellow Bin", 4, 0)
            },
            daily.ContainerTotals.Select(x =>
                (x.ContainerTypeId, x.ContainerType, x.OutQuantity, x.InQuantity)));

        h.ClearProjectionServiceCalls();
        var pdf = await h.DailyPrintPack.BuildPdfAsync(Harness.Today.AddDays(2));

        Assert.Equal("%PDF", System.Text.Encoding.ASCII.GetString(pdf, 0, 4));
        Assert.Equal(2, h.ProjectionServiceCalls.Count);
        var positionCall = Assert.Single(h.ProjectionServiceCalls, x => x.IsPositionAsOf);
        Assert.Equal(Harness.Today, positionCall.ThroughDateInclusive);
        Assert.Null(positionCall.FromDateInclusive);
        var activityCall = Assert.Single(h.ProjectionServiceCalls, x => !x.IsPositionAsOf);
        Assert.Equal(Harness.Today, activityCall.FromDateInclusive);
        Assert.Equal(Harness.Today, activityCall.ThroughDateInclusive);
        Assert.All(h.ProjectionServiceCalls, call =>
        {
            Assert.Null(call.CustomerId);
            Assert.Null(call.ContainerTypeId);
        });

        await using var db = new BinTrackerDbContext(
            new DbContextOptionsBuilder<BinTrackerDbContext>()
                .UseSqlite(h.ConnectionString).Options);
        var audit = Assert.Single(await db.AuditEvents.AsNoTracking()
            .Where(x => x.Action == "DAILY_PRINT_PACK_GENERATED")
            .ToListAsync());
        Assert.Equal(Harness.Today.ToString("yyyy-MM-dd"), audit.EntityId);
        Assert.Contains("2 outstanding row(s)", audit.Description);
        Assert.Contains("3 movement row(s)", audit.Description);
        Assert.Contains("4 OUT, 6 IN", audit.Description);
    }

    [Fact]
    public async Task Projection_backed_daily_print_pack_propagates_integrity_failure_without_raw_fallback()
    {
        await using var h = await Harness.CreateAsync(enableProjectionBackedServices: true);
        var root = await h.CreateSingleAsync(
            new(2026, 9, 3), h.CustomerId, 1, 6);
        await h.SetRootStatusAsync(root.RootId, LogicalMovementBatchStatus.Invalid);

        var failure = await Assert.ThrowsAsync<OperationalMovementProjectionException>(() =>
            h.DailyPrintPack.BuildPdfAsync(Harness.Today));

        Assert.Equal(
            OperationalMovementProjectionFailure.RelevantLineageInvalid,
            failure.Failure);
        Assert.Equal(2, h.ProjectionServiceCalls.Count);
        await using var db = new BinTrackerDbContext(
            new DbContextOptionsBuilder<BinTrackerDbContext>()
                .UseSqlite(h.ConnectionString).Options);
        Assert.False(await db.AuditEvents.AsNoTracking()
            .AnyAsync(x => x.Action == "DAILY_PRINT_PACK_GENERATED"));
    }

    [Fact]
    public async Task Projection_backed_int_position_mappings_fail_closed_on_overflow()
    {
        await using var h = await Harness.CreateAsync(enableProjectionBackedServices: true);
        await h.CreateSingleAsync(
            new(2026, 9, 1), h.CustomerId, 1, int.MaxValue);
        var moved = await h.CreateSingleAsync(
            new(2026, 9, 1), h.OtherCustomerId, 1, 1);
        var movedLineId = Assert.Single(await h.LineIdsAsync(moved.RootId));
        await h.MutateAsync(moved.RootId, 0,
            MovementMutationRequest.Correct(
                MovementMutationScope.Individual,
                [new(movedLineId)],
                "move projected position across int boundary",
                customer: MovementFieldIntent<int>.Selected(h.CustomerId)));

        await Assert.ThrowsAsync<OverflowException>(() => h.Balances.GetBalancesAsync());
        await Assert.ThrowsAsync<OverflowException>(() =>
            h.Customers.SearchAsync(null, includeInactive: true));
        await Assert.ThrowsAsync<OverflowException>(() =>
            h.Customers.GetBalancesAsync(h.CustomerId));
        await Assert.ThrowsAsync<OverflowException>(() =>
            h.Customers.GetStatementAsync(h.CustomerId, new(2026, 9, 1), Harness.Today));
        await Assert.ThrowsAsync<OverflowException>(() =>
            h.Movements.GetCustomerSummaryByCodeAsync("PROJ-A"));
        await Assert.ThrowsAsync<OverflowException>(() =>
            h.Movements.GetDashboardSummaryAsync(Harness.Today));
        await Assert.ThrowsAsync<OverflowException>(() =>
            h.Outstanding.QueryAsync(new OutstandingReportQuery(
                Harness.Today,
                BalanceFilter: OutstandingBalanceFilter.AllNonZero)));
    }

    [Fact]
    public async Task Schema17_projection_is_explicit_and_normal_composition_remains_schema16()
    {
        await using var h = await Harness.CreateAsync(migrateToSchema17: false,
            enableSchema17Writers: false);

        Assert.False(h.ProjectionAuthorityIsRegistered);
        var exception = await Assert.ThrowsAsync<OperationalMovementProjectionException>(() =>
            h.Authority.QueryAsync(OperationalMovementProjectionScope.All()));
        Assert.Equal(OperationalMovementProjectionFailure.SchemaUnavailable, exception.Failure);
        await using var connection = await h.OpenAsync();
        Assert.Equal(0, await ScalarAsync(connection,
            "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='LogicalMovementBatches';"));
    }

    private static async Task<long> ScalarAsync(SqliteConnection connection, string sql)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        return Convert.ToInt64(await command.ExecuteScalarAsync());
    }

    private static ImportExecutionRequest ReplacementRequest(
        Harness harness,
        long previousImportRunId,
        DateOnly cutoverDate)
    {
        var source = new ImportSourceDocument(
            "corrected.xlsx",
            "corrected replacement source"u8.ToArray(),
            "corrected.xlsx",
            new DateTime(2026, 9, 5, 1, 2, 3, DateTimeKind.Utc));
        var snapshot = new ImportSnapshotCandidate(
            "Update Account",
            "PROJ-A",
            CustomerType.Account,
            null,
            Out: 2,
            In: 0,
            BroughtForward: 20,
            ExcelTotal: 22,
            SourceRow: "12");
        var analysis = new ExcelImportAnalysis(
            source,
            [new ImportWorksheetAnalysis(
                "Update Account", 12, 7, 1, 1, "Detected")],
            [new ImportCustomerCandidate(
                "Update Account", "PROJ-A", CustomerType.Account, "A1")],
            [snapshot],
            []);

        return new ImportExecutionRequest(
            source,
            "comparison-does-not-fingerprint",
            analysis,
            [new ImportWorksheetMapping(
                "Update Account", ImportWorksheetRole.Source, string.Empty)],
            new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase),
            new Dictionary<string, ImportCustomerDecision>(StringComparer.OrdinalIgnoreCase),
            new Dictionary<string, ImportExistingCustomerDecision>(StringComparer.OrdinalIgnoreCase)
            {
                ["PROJ-A"] = new(
                    "PROJ-A",
                    ImportExistingCustomerDecisionAction.AcceptMatch,
                    harness.CustomerId,
                    "PROJ-A",
                    "Projection A")
            },
            cutoverDate,
            ImportExecutionMode.ReplacePreviousCutover,
            previousImportRunId,
            Guid.NewGuid());
    }

    private static async Task<ImportExecutionRequest> ExecutableReplacementRequestAsync(
        Harness harness,
        long previousImportRunId,
        DateOnly cutoverDate)
    {
        var request = ReplacementRequest(harness, previousImportRunId, cutoverDate);
        var preflight = await harness.ImportExecution.PreflightAsync(
            request.Source,
            cutoverDate);
        return request with { ExpectedSourceSha256 = preflight.Source.Sha256 };
    }

    private static async Task<ImportExecutionRequest> NewImportRequestAsync(
        Harness harness,
        DateOnly cutoverDate,
        string sourceContent)
    {
        var source = new ImportSourceDocument(
            "new.xlsx",
            System.Text.Encoding.UTF8.GetBytes(sourceContent),
            "new.xlsx",
            new DateTime(2026, 9, 5, 1, 2, 3, DateTimeKind.Utc));
        var preflight = await harness.ImportExecution.PreflightAsync(source, cutoverDate);
        var analysis = new ExcelImportAnalysis(
            source,
            [new ImportWorksheetAnalysis(
                "Update Account", 12, 7, 1, 1, "Detected")],
            [new ImportCustomerCandidate(
                "Update Account", "PROJ-A", CustomerType.Account, "A1")],
            [new ImportSnapshotCandidate(
                "Update Account",
                "PROJ-A",
                CustomerType.Account,
                null,
                Out: 2,
                In: 0,
                BroughtForward: 20,
                ExcelTotal: 22,
                SourceRow: "12")],
            []);

        return new ImportExecutionRequest(
            source,
            preflight.Source.Sha256,
            analysis,
            [new ImportWorksheetMapping(
                "Update Account", ImportWorksheetRole.Source, string.Empty)],
            new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase),
            new Dictionary<string, ImportCustomerDecision>(StringComparer.OrdinalIgnoreCase),
            new Dictionary<string, ImportExistingCustomerDecision>(StringComparer.OrdinalIgnoreCase)
            {
                ["PROJ-A"] = new(
                    "PROJ-A",
                    ImportExistingCustomerDecisionAction.AcceptMatch,
                    harness.CustomerId,
                    "PROJ-A",
                    "Projection A")
            },
            cutoverDate,
            ClientOperationId: Guid.NewGuid());
    }

    private sealed class Harness : IAsyncDisposable
    {
        internal static readonly DateOnly Today = new(2026, 9, 5);
        private static readonly DateTime UtcNow = new(2026, 9, 5, 1, 2, 3, DateTimeKind.Utc);
        private readonly string root;
        private readonly ServiceProvider services;
        private readonly LineageSchema17MigrationPrerequisites? prerequisites;

        private Harness(string root, string connectionString, ServiceProvider services,
            LineageSchema17MigrationPrerequisites? prerequisites, int customerId,
            int otherCustomerId)
        {
            this.root = root;
            ConnectionString = connectionString;
            this.services = services;
            this.prerequisites = prerequisites;
            CustomerId = customerId;
            OtherCustomerId = otherCustomerId;
            Movements = services.GetRequiredService<IMovementService>();
            Mutations = services.GetRequiredService<IMovementCorrectionService>();
            Balances = services.GetRequiredService<IBalanceService>();
            Customers = services.GetRequiredService<ICustomerService>();
            ContainerTypes = services.GetRequiredService<IContainerTypeService>();
            Outstanding = services.GetRequiredService<IOutstandingReportService>();
            Daily = services.GetRequiredService<IDailyMovementsReportService>();
            Weekly = services.GetRequiredService<IWeeklyMovementsReportService>();
            Monthly = services.GetRequiredService<IMonthlySummaryReportService>();
            MarketFloor = services.GetRequiredService<IMarketFloorReportService>();
            DailyPrintPack = services.GetRequiredService<IDailyPrintPackService>();
            ImportExecution = services.GetRequiredService<IImportExecutionService>();
            Authority = new SqliteOperationalMovementProjectionAuthority(connectionString);
        }

        public string ConnectionString { get; }
        public int CustomerId { get; }
        public int OtherCustomerId { get; }
        public IMovementService Movements { get; }
        public IMovementCorrectionService Mutations { get; }
        public IBalanceService Balances { get; }
        public ICustomerService Customers { get; }
        public IContainerTypeService ContainerTypes { get; }
        public IOutstandingReportService Outstanding { get; }
        public IDailyMovementsReportService Daily { get; }
        public IWeeklyMovementsReportService Weekly { get; }
        public IMonthlySummaryReportService Monthly { get; }
        public IMarketFloorReportService MarketFloor { get; }
        public IDailyPrintPackService DailyPrintPack { get; }
        public IImportExecutionService ImportExecution { get; }
        public IOperationalMovementProjectionAuthority Authority { get; }
        public IReadOnlyList<OperationalMovementProjectionScope> ProjectionServiceCalls =>
            services.GetService<IOperationalMovementProjectionAuthority>() is RecordingProjectionAuthority recording
                ? recording.Calls
                : [];
        public int ProjectionTransactionCallCount =>
            services.GetService<IOperationalMovementProjectionAuthority>() is RecordingProjectionAuthority recording
                ? recording.TransactionCallCount
                : 0;
        public bool ProjectionAuthorityIsRegistered =>
            services.GetService<IOperationalMovementProjectionAuthority>() is not null;

        public void ClearProjectionServiceCalls()
        {
            if (services.GetService<IOperationalMovementProjectionAuthority>() is
                RecordingProjectionAuthority recording)
            {
                recording.Clear();
            }
        }

        public void CancelNextProjectionTransaction()
        {
            if (services.GetService<IOperationalMovementProjectionAuthority>() is
                RecordingProjectionAuthority recording)
            {
                recording.CancelNextTransaction();
            }
        }

        public static async Task<Harness> CreateAsync(bool migrateToSchema17 = true,
            bool enableSchema17Writers = true,
            bool enableProjectionBackedServices = false,
            UserRole userRole = UserRole.Operator)
        {
            var root = Path.Combine(Path.GetTempPath(), $"BinTracker-projection-v17-{Guid.NewGuid():N}");
            Directory.CreateDirectory(root);
            var databasePath = Path.Combine(root, "db", "BinTracker.db");
            Directory.CreateDirectory(Path.GetDirectoryName(databasePath)!);
            var connectionString = $"Data Source={databasePath};Foreign Keys=True;Pooling=False;Default Timeout=10";

            int customerId;
            int otherCustomerId;
            await using (var db = new BinTrackerDbContext(
                new DbContextOptionsBuilder<BinTrackerDbContext>().UseSqlite(connectionString).Options))
            {
                await DatabaseSetup.InitializeSqliteAsync(db);
                var customer = new Customer { CustomerCode = "PROJ-A", Name = "Projection A", IsActive = true };
                var other = new Customer { CustomerCode = "PROJ-B", Name = "Projection B", IsActive = true };
                db.AddRange(customer, other);
                await db.SaveChangesAsync();
                customerId = customer.Id;
                otherCustomerId = other.Id;
            }

            LineageSchema17MigrationPrerequisites? prerequisites = null;
            if (migrateToSchema17)
                prerequisites = await MigrateAsync(root, databasePath);
            var services = BuildServices(
                connectionString,
                enableSchema17Writers,
                enableProjectionBackedServices,
                userRole);
            return new(root, connectionString, services, prerequisites, customerId, otherCustomerId);
        }

        public async Task<(long RootId, long MovementId)> CreateSingleAsync(DateOnly date,
            int customerId, int containerTypeId, int quantity,
            MovementType direction = MovementType.Out)
        {
            var result = await Movements.SaveSingleAsync(new(Guid.NewGuid(), date,
                direction, customerId, containerTypeId, quantity, "projection", null));
            return (await RootForMovementAsync(result.MovementId), result.MovementId);
        }

        public async Task<(long RootId, int BatchId)> CreateBatchAsync(params int[] quantities)
        {
            var result = await Movements.SaveBatchAsync(new(Guid.NewGuid(), new(2026, 9, 1),
                MovementType.Out, "projection batch", quantities.Select((quantity, index) =>
                    new MovementBatchLine(CustomerId, index + 1, quantity, null, null)).ToArray()));
            await using var connection = await OpenAsync();
            return (await ScalarAsync(connection,
                $"SELECT Id FROM LogicalMovementBatches WHERE RootMovementBatchId={result.BatchId};"),
                result.BatchId);
        }

        public Task<LogicalMovementMutationResult> MutateAsync(long rootId, int expectedGeneration,
            MovementMutationRequest request) => Mutations.ExecuteLogicalAsync(new(Guid.NewGuid(),
                new(rootId), new(expectedGeneration), request));

        public async Task<IReadOnlyList<long>> LineIdsAsync(long rootId)
        {
            await using var connection = await OpenAsync();
            await using var command = connection.CreateCommand();
            command.CommandText = $"SELECT Id FROM LogicalMovementLines WHERE LogicalMovementBatchId={rootId} ORDER BY OriginalDisplayOrdinal;";
            var result = new List<long>();
            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync()) result.Add(reader.GetInt64(0));
            return result;
        }

        public async Task<IReadOnlyDictionary<LogicalMovementTransformationRole, long>>
            MovementIdsByRoleAsync(long rootId)
        {
            await using var connection = await OpenAsync();
            await using var command = connection.CreateCommand();
            command.CommandText = $"""
                SELECT Role,BinMovementId
                FROM LogicalMovementLedgerLinks
                WHERE LogicalMovementBatchId={rootId}
                ORDER BY BinMovementId;
                """;
            var result = new Dictionary<LogicalMovementTransformationRole, long>();
            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
                result[(LogicalMovementTransformationRole)reader.GetInt32(0)] = reader.GetInt64(1);
            return result;
        }

        public async Task SetRootStatusAsync(long rootId, LogicalMovementBatchStatus status) =>
            await ExecuteAsync($"UPDATE LogicalMovementBatches SET Status={(int)status} WHERE Id={rootId};");

        public async Task SetCurrentGenerationAsync(long rootId, int generation) =>
            await ExecuteAsync($"UPDATE LogicalMovementBatches SET CurrentGenerationNumber={generation} WHERE Id={rootId};");

        public async Task SetAttentionThresholdAsync(int threshold)
        {
            await using var db = new BinTrackerDbContext(
                new DbContextOptionsBuilder<BinTrackerDbContext>().UseSqlite(ConnectionString).Options);
            var settings = await db.ApplicationSettings.SingleAsync(x => x.Id == 1);
            settings.AttentionQuantityThreshold = threshold;
            await db.SaveChangesAsync();
        }

        public async Task<int> AddCustomerAsync(
            string code,
            string name,
            CustomerType customerType)
        {
            await using var db = new BinTrackerDbContext(
                new DbContextOptionsBuilder<BinTrackerDbContext>().UseSqlite(ConnectionString).Options);
            var customer = new Customer
            {
                CustomerCode = code,
                Name = name,
                CustomerType = customerType,
                IsActive = true
            };
            db.Add(customer);
            await db.SaveChangesAsync();
            return customer.Id;
        }

        public async Task SetCustomerTypeAsync(int customerId, CustomerType customerType)
        {
            await using var db = new BinTrackerDbContext(
                new DbContextOptionsBuilder<BinTrackerDbContext>().UseSqlite(ConnectionString).Options);
            (await db.Customers.SingleAsync(x => x.Id == customerId)).CustomerType = customerType;
            await db.SaveChangesAsync();
        }

        public async Task SetCustomerActiveAsync(int customerId, bool isActive)
        {
            await using var db = new BinTrackerDbContext(
                new DbContextOptionsBuilder<BinTrackerDbContext>().UseSqlite(ConnectionString).Options);
            (await db.Customers.SingleAsync(x => x.Id == customerId)).IsActive = isActive;
            await db.SaveChangesAsync();
        }

        public async Task SetContainerActiveAsync(int containerTypeId, bool isActive)
        {
            await using var db = new BinTrackerDbContext(
                new DbContextOptionsBuilder<BinTrackerDbContext>().UseSqlite(ConnectionString).Options);
            (await db.ContainerTypes.SingleAsync(x => x.Id == containerTypeId)).IsActive = isActive;
            await db.SaveChangesAsync();
        }

        public async Task AddExcludedAsync(MovementSource source, MovementType direction,
            int quantity, bool importOwned, DateOnly? movementDate = null)
        {
            await using var db = new BinTrackerDbContext(
                new DbContextOptionsBuilder<BinTrackerDbContext>().UseSqlite(ConnectionString).Options);
            ImportRun? run = null;
            if (importOwned)
            {
                run = new ImportRun
                {
                    SourceFileName = "projection.xlsx", SourceClientPath = "projection.xlsx",
                    SourceSha256 = Guid.NewGuid().ToString("N") +
                        Guid.NewGuid().ToString("N"), SourceLength = 1,
                    SourceLastWriteUtc = UtcNow, CutoverDate = new(2026, 9, 1),
                    StartedUtc = UtcNow, CompletedUtc = UtcNow, Status = "Completed",
                    Username = "projection", SessionId = "projection"
                };
                db.Add(run);
                await db.SaveChangesAsync();
            }
            db.Add(new BinMovement
            {
                ClientOperationId = Guid.NewGuid(),
                MovementDate = movementDate ?? new(2026, 9, 1),
                MovementType = direction, Source = source, CustomerId = CustomerId,
                ContainerTypeId = 1, Quantity = quantity, ImportRunId = run?.Id,
                CreatedBy = "projection", CreatedUtc = UtcNow
            });
            await db.SaveChangesAsync();
        }

        public async Task<long> CreatePreviousImportRunAsync(
            DateOnly cutover,
            DateOnly? movementDate = null)
        {
            await using var db = new BinTrackerDbContext(
                new DbContextOptionsBuilder<BinTrackerDbContext>().UseSqlite(ConnectionString).Options);
            var run = new ImportRun
            {
                SourceFileName = "previous.xlsx",
                SourceClientPath = "previous.xlsx",
                SourceSha256 = Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N"),
                SourceLength = 1,
                SourceLastWriteUtc = UtcNow,
                CutoverDate = cutover,
                CurrentCutoverDate = cutover,
                StartedUtc = UtcNow,
                CompletedUtc = UtcNow,
                Status = "Completed",
                Username = "projection-operator",
                SessionId = "projection-session",
                MovementCount = 2
            };
            db.Add(run);
            await db.SaveChangesAsync();
            db.AddRange(
                new BinMovement
                {
                    ClientOperationId = Guid.NewGuid(),
                    MovementDate = movementDate ?? cutover,
                    MovementType = MovementType.Out,
                    Source = MovementSource.Adjustment,
                    CustomerId = CustomerId,
                    ContainerTypeId = 1,
                    Quantity = 10,
                    ImportRunId = run.Id,
                    CreatedBy = "projection-operator",
                    CreatedUtc = UtcNow
                },
                new BinMovement
                {
                    ClientOperationId = Guid.NewGuid(),
                    MovementDate = movementDate ?? cutover,
                    MovementType = MovementType.Out,
                    Source = MovementSource.ExcelImport,
                    CustomerId = CustomerId,
                    ContainerTypeId = 1,
                    Quantity = 1,
                    ImportRunId = run.Id,
                    CreatedBy = "projection-operator",
                    CreatedUtc = UtcNow
                });
            await db.SaveChangesAsync();
            return run.Id;
        }

        public async Task AddExcludedWithDanglingImportRunAsync(MovementSource source)
        {
            var databasePath = new SqliteConnectionStringBuilder(ConnectionString).DataSource;
            await using var connection = new SqliteConnection(
                $"Data Source={databasePath};Foreign Keys=False;Pooling=False");
            await connection.OpenAsync();
            await using var command = connection.CreateCommand();
            command.CommandText = """
                INSERT INTO BinMovements
                    (ClientOperationId,MovementDate,MovementType,Source,CustomerId,
                     ContainerTypeId,ImportRunId,Quantity,CreatedBy,CreatedUtc)
                VALUES ($operation,'2026-09-01',1,$source,$customer,1,999999,1,
                        'projection','2026-09-05T01:02:03.0000000Z');
                """;
            command.Parameters.AddWithValue("$operation", Guid.NewGuid().ToString());
            command.Parameters.AddWithValue("$source", (int)source);
            command.Parameters.AddWithValue("$customer", CustomerId);
            await command.ExecuteNonQueryAsync();
        }

        public async Task InsertUnrootedOrdinaryAsync(int customerId)
        {
            await using var db = new BinTrackerDbContext(
                new DbContextOptionsBuilder<BinTrackerDbContext>().UseSqlite(ConnectionString).Options);
            db.Add(new BinMovement
            {
                ClientOperationId = Guid.NewGuid(), MovementDate = new(2026, 9, 1),
                MovementType = MovementType.Out, Source = MovementSource.Manual,
                CustomerId = customerId, ContainerTypeId = 1, Quantity = 1,
                CreatedBy = "projection", CreatedUtc = UtcNow
            });
            await db.SaveChangesAsync();
        }

        public async Task InsertMalformedUnknownMovementAsync()
        {
            var databasePath = new SqliteConnectionStringBuilder(ConnectionString).DataSource;
            await using var connection = new SqliteConnection(
                $"Data Source={databasePath};Foreign Keys=False;Pooling=False");
            await connection.OpenAsync();
            await using var command = connection.CreateCommand();
            command.CommandText = """
                INSERT INTO BinMovements
                    (MovementDate,MovementType,Source,CustomerId,ContainerTypeId,Quantity,CreatedBy,CreatedUtc)
                VALUES ('not-a-date',1,0,0,1,1,'projection','2026-09-05 01:02:03');
                """;
            await command.ExecuteNonQueryAsync();
        }

        public async Task<SqliteConnection> OpenAsync()
        {
            var connection = new SqliteConnection(ConnectionString);
            await connection.OpenAsync();
            return connection;
        }

        private async Task<long> RootForMovementAsync(long movementId)
        {
            await using var connection = await OpenAsync();
            return await ScalarAsync(connection,
                $"SELECT LogicalMovementBatchId FROM LogicalMovementLines WHERE RootMovementId={movementId};");
        }

        private async Task ExecuteAsync(string sql)
        {
            await using var connection = await OpenAsync();
            await using var command = connection.CreateCommand();
            command.CommandText = sql;
            await command.ExecuteNonQueryAsync();
        }

        private static ServiceProvider BuildServices(
            string connectionString,
            bool enableSchema17Writers,
            bool enableProjectionBackedServices,
            UserRole userRole)
        {
            var collection = new ServiceCollection();
            collection.AddSingleton<IBusinessClock>(new FixedClock());
            collection.AddSingleton<IUserContext>(new TestUserContext(userRole));
            collection.AddSingleton<IClientContext>(new TestClientContext());
            collection.AddDbContextFactory<BinTrackerDbContext>(builder => builder.UseSqlite(connectionString));
            if (enableSchema17Writers)
            {
                collection.AddScoped<IInitialMovementLineageWriter>(_ =>
                    new SqliteInitialMovementLineageWriter(NoInitialMovementLineageFailureInjector.Instance));
                collection.AddScoped<IMovementMutationWriter>(_ =>
                    new SqliteMovementMutationWriter(NoMovementMutationFailureInjector.Instance));
            }
            if (enableProjectionBackedServices)
            {
                collection.AddScoped<IOperationalMovementProjectionAuthority>(_ =>
                    new RecordingProjectionAuthority(
                        new SqliteOperationalMovementProjectionAuthority(connectionString)));
            }
            collection.AddBinTrackerBusinessServices();
            return collection.BuildServiceProvider();
        }

        private static async Task<LineageSchema17MigrationPrerequisites> MigrateAsync(
            string root, string databasePath)
        {
            var gate = new WindowsFileDatabaseUpgradeGate(
                Path.Combine(root, "locks"), new NoConflictProbe());
            var lease = gate.AcquireUpgrade(databasePath);
            try
            {
                var preflightService = new SqliteLineageMigrationPreflight();
                var preflight = await preflightService.InspectAsync(databasePath);
                var backupService = new SqliteLineageMigrationBackupService(gate, preflightService);
                var backup = await backupService.CreateVerifiedAsync(
                    lease, Path.Combine(root, "recovery"));
                var prerequisites = new LineageSchema17MigrationPrerequisites(
                    lease, preflight, backup, backupService);
                await new SqliteLineageSchema17Migrator().MigrateAsync(prerequisites);
                return prerequisites;
            }
            catch
            {
                lease.Dispose();
                throw;
            }
        }

        public async ValueTask DisposeAsync()
        {
            await services.DisposeAsync();
            prerequisites?.UpgradeLease.Dispose();
            SqliteConnection.ClearAllPools();
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }

        private sealed class FixedClock : IBusinessClock
        {
            public DateTime UtcNow => Harness.UtcNow;
            public DateTime LocalNow => UtcNow;
            public DateOnly Today => Harness.Today;
            public string TimeZoneId => "UTC";
        }

        private sealed class RecordingProjectionAuthority(
            IOperationalMovementProjectionAuthority inner)
            : ITransactionalOperationalMovementProjectionAuthority
        {
            private readonly List<OperationalMovementProjectionScope> calls = [];
            private bool cancelNextTransaction;

            public IReadOnlyList<OperationalMovementProjectionScope> Calls => calls;
            public int TransactionCallCount { get; private set; }

            public void Clear()
            {
                calls.Clear();
                TransactionCallCount = 0;
            }

            public void CancelNextTransaction() => cancelNextTransaction = true;

            public Task<OperationalMovementProjectionResult> QueryAsync(
                OperationalMovementProjectionScope scope,
                CancellationToken cancellationToken = default)
            {
                calls.Add(scope);
                return inner.QueryAsync(scope, cancellationToken);
            }

            public Task<OperationalMovementProjectionResult> QueryInTransactionAsync(
                BinTrackerDbContext db,
                OperationalMovementProjectionScope scope,
                CancellationToken cancellationToken = default)
            {
                calls.Add(scope);
                TransactionCallCount++;
                Assert.NotNull(db.Database.CurrentTransaction);
                Assert.Equal(
                    System.Data.IsolationLevel.Serializable,
                    db.Database.CurrentTransaction.GetDbTransaction().IsolationLevel);
                if (cancelNextTransaction)
                {
                    cancelNextTransaction = false;
                    throw new OperationCanceledException("Injected projection cancellation.");
                }

                return ((ITransactionalOperationalMovementProjectionAuthority)inner)
                    .QueryInTransactionAsync(db, scope, cancellationToken);
            }
        }

        private sealed class TestUserContext(UserRole role) : IUserContext
        {
            public string SessionId => "projection-session";
            public int? UserId => 61;
            public string Username => "projection-operator";
            public string DisplayName => "Projection Operator";
            public UserRole Role => role;
            public bool MustChangePassword => false;
            public bool IsAuthenticated => true;
        }

        private sealed class TestClientContext : IClientContext
        {
            public string ClientInstanceId => "projection-client";
            public string DeviceName => "projection-device";
        }

        private sealed class NoConflictProbe : IDatabaseOperationConflictProbe
        {
            public void EnsureNoConflict(string databasePath) { }
        }
    }
}
