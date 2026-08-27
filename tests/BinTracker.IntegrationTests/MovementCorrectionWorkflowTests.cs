using BinTracker.Core;
using BinTracker.Data;
using BinTracker.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BinTracker.IntegrationTests;

public sealed class MovementCorrectionWorkflowTests
{
    [Fact]
    public void Correction_selection_resolves_exact_persisted_ids_including_inactive_history()
    {
        var movement = new MovementCorrectionDetail(960, new DateOnly(2026, 8, 24), 161,
            "TEST", "test", 3, "Yellow Bin", MovementType.Out, 10,
            MovementSource.Manual, "ref", "notes", "operator", false, null);
        CustomerListRow[] customers =
        [
            new(5, "Other", "OTHER", CustomerType.Account, true, 0),
            new(161, "test", "TEST", CustomerType.Account, false, 10)
        ];
        ContainerTypeListRow[] containers =
        [
            new(1, "Blue Bin", "BLUE", 1, true, false, 1),
            new(3, "Yellow Bin", "YELLOW", 3, false, false, 1)
        ];

        var selected = MovementCorrectionSelection.Resolve(movement, customers, containers);

        Assert.Equal(161, selected.Customer.Id);
        Assert.Equal(3, selected.ContainerType.Id);
        Assert.False(selected.Customer.IsActive);
        Assert.False(selected.ContainerType.IsActive);
    }

    [Fact]
    public void Correction_selection_rejects_missing_or_duplicate_persistent_identifier_matches()
    {
        var movement = new MovementCorrectionDetail(1, new DateOnly(2026, 8, 24), 9,
            "X", "X", 4, "Bulk", MovementType.In, 1, MovementSource.Batch,
            "", "", "operator", false, 2);
        var customer = new CustomerListRow(9, "X", "X", CustomerType.Account, true, 0);
        var container = new ContainerTypeListRow(4, "Bulk", "BULK", 4, true, false, 1);

        var duplicate = Assert.Throws<InvalidOperationException>(() =>
            MovementCorrectionSelection.Resolve(movement, [customer, customer], [container]));
        Assert.Contains("2 matching records", duplicate.Message);

        var missing = Assert.Throws<InvalidOperationException>(() =>
            MovementCorrectionSelection.Resolve(movement, [customer], []));
        Assert.Contains("container type ID 4", missing.Message);
    }

    [Fact]
    public void Batch_direction_resolution_is_exact_explicit_and_independent_of_combo_box_binding()
    {
        var batch = new MovementBatchCorrectionDetail(41, 1, 35, new DateOnly(2026, 8, 25),
            MovementType.In, true, [new(1040, 41, 161, "JMPL", "JMPL", 1, "Blue Bin", 35)]);

        Assert.Equal(1, MovementCorrectionSelection.ResolveBatchDirectionIndex(
            batch, [MovementType.Out, MovementType.In]));
        var missing = Assert.Throws<InvalidOperationException>(() =>
            MovementCorrectionSelection.ResolveBatchDirectionIndex(batch, [MovementType.Out]));
        Assert.Contains("contains 0 matching records", missing.Message);
        var duplicate = Assert.Throws<InvalidOperationException>(() =>
            MovementCorrectionSelection.ResolveBatchDirectionIndex(batch,
                [MovementType.In, MovementType.Out, MovementType.In]));
        Assert.Contains("contains 2 matching records", duplicate.Message);
    }

    [Fact]
    public async Task Single_correction_replaces_all_editable_values_and_moves_day_week_month_effect()
    {
        await using var h = await Harness.Create(UserRole.Operator);
        var buyerB = await h.AddCustomer("B", "Buyer B");
        var original = await h.AddMovement(new DateOnly(2026, 8, 4), MovementType.In, 10,
            h.CustomerId, 1, MovementSource.Manual, "wrong-ref", "wrong-notes");
        var operationId = Guid.NewGuid();
        var request = new CorrectMovementRequest(operationId, original, new DateOnly(2026, 8, 3),
            buyerB, 3, MovementType.Out, 1, "right-ref", "right-notes", "Camera confirmed correct return");
        var result = await h.Corrections.CorrectAsync(request);
        var retried = await h.Corrections.CorrectAsync(request);
        Assert.Equal(result.CorrectionOperationId, retried.CorrectionOperationId);
        Assert.Equal(result.Lines.ToArray(), retried.Lines.ToArray());
        await Assert.ThrowsAsync<InvalidOperationException>(() => h.Corrections.CorrectAsync(
            request with { ClientOperationId = Guid.NewGuid() }));
        await Assert.ThrowsAsync<InvalidOperationException>(() => h.Corrections.ReverseAsync(
            new(Guid.NewGuid(), original, "Cannot reverse superseded original")));

        await using var db = await h.Factory.CreateDbContextAsync();
        var line = Assert.Single(await db.MovementCorrectionLines.AsNoTracking().ToListAsync());
        var source = await db.BinMovements.AsNoTracking().SingleAsync(x => x.Id == original);
        var neutral = await db.BinMovements.AsNoTracking().SingleAsync(x => x.Id == line.NeutralisingMovementId);
        var replacement = await db.BinMovements.AsNoTracking().SingleAsync(x => x.Id == line.ReplacementMovementId);
        Assert.Equal(new DateOnly(2026, 8, 4), source.MovementDate);
        Assert.Equal(new DateOnly(2026, 8, 4), neutral.MovementDate);
        Assert.Equal(MovementType.Out, neutral.MovementType);
        Assert.Equal(10, neutral.Quantity);
        Assert.Equal(new DateOnly(2026, 8, 3), replacement.MovementDate);
        Assert.Equal((buyerB, 3, MovementType.Out, 1),
            (replacement.CustomerId, replacement.ContainerTypeId, replacement.MovementType, replacement.Quantity));
        Assert.Equal(("right-ref", "right-notes"), (replacement.ReferenceNumber, replacement.Notes));
        Assert.Equal(neutral.Id, source.CorrectedByMovementId);

        var daily = h.Provider.GetRequiredService<IDailyMovementsReportService>();
        var monday = await daily.QueryAsync(new(new DateOnly(2026, 8, 3)));
        var tuesday = await daily.QueryAsync(new(new DateOnly(2026, 8, 4)));
        Assert.Equal(1, monday.Net());
        Assert.Equal(0, tuesday.Net());
        Assert.Empty(tuesday.Rows);
        Assert.Single(monday.Rows, x => x.MovementId == replacement.Id);
        var weekly = await h.Provider.GetRequiredService<IWeeklyMovementsReportService>()
            .QueryAsync(new(new DateOnly(2026, 8, 3)));
        Assert.Equal(1, weekly.NetQuantity);
        Assert.Single(weekly.Rows, x => x.MovementId == replacement.Id);
        var monthly = await h.Provider.GetRequiredService<IMonthlySummaryReportService>()
            .QueryAsync(new(new DateOnly(2026, 8, 1)));
        Assert.Equal(1, monthly.NetQuantity);
        var balances = await h.Provider.GetRequiredService<IBalanceService>().GetBalancesAsync();
        Assert.DoesNotContain(balances, x => x.CustomerId == h.CustomerId && x.Balance != 0);
        var buyerBPosition = Assert.Single(balances, x => x.CustomerId == buyerB && x.ContainerTypeId == 3);
        Assert.Equal(1, buyerBPosition.Balance);
        Assert.True(await db.AuditEvents.AnyAsync(x => x.Action == "MOVEMENT_CORRECTED" && x.RequiresAdministratorReview));
        Assert.True(await db.MovementCorrectionOperations.AnyAsync(x => x.ClientOperationId == operationId));

        var customerService = h.Provider.GetRequiredService<ICustomerService>();
        Assert.Empty(await customerService.GetRecentMovementsAsync(h.CustomerId));
        var recentB = Assert.Single(await customerService.GetRecentMovementsAsync(buyerB));
        Assert.Equal((new DateOnly(2026, 8, 3), "OUT (Taken)", "Yellow Bin", 1),
            (recentB.Date, recentB.Direction, recentB.ContainerType, recentB.Quantity));
        var statementA = await customerService.GetStatementAsync(h.CustomerId,
            new DateOnly(2026, 8, 1), new DateOnly(2026, 8, 31));
        Assert.Empty(statementA.Containers);
        var statementB = await customerService.GetStatementAsync(buyerB,
            new DateOnly(2026, 8, 1), new DateOnly(2026, 8, 31));
        var statementMovement = Assert.Single(Assert.Single(statementB.Containers).Movements);
        Assert.Equal((new DateOnly(2026, 8, 3), "OUT (Taken)", 1),
            (statementMovement.Date, statementMovement.Direction, statementMovement.Quantity));

        var floor = await h.Provider.GetRequiredService<IMarketFloorReportService>()
            .GetAsync(new DateOnly(2026, 8, 3));
        var floorB = Assert.Single(floor.AccountDaily, x => x.CustomerId == buyerB && x.Container == "Yellow");
        Assert.Equal((1, 0, 0, 1), (floorB.Out, floorB.In, floorB.BroughtForward, floorB.Total));
    }

    [Fact]
    public async Task Quantity_correction_is_one_effective_row_but_three_distinct_immutable_history_roles()
    {
        await using var h = await Harness.Create(UserRole.Administrator);
        var date = new DateOnly(2026, 8, 23);
        var original = await h.AddMovement(date, MovementType.In, 3, h.CustomerId, 1,
            MovementSource.Manual, "entry-ref", "entered as three");

        var correction = await h.Corrections.CorrectAsync(new(Guid.NewGuid(), original, date,
            h.CustomerId, 1, MovementType.In, 1, "entry-ref", "entered as one",
            "Quantity entered as 3 instead of 1"));
        var line = Assert.Single(correction.Lines);

        var daily = await h.Provider.GetRequiredService<IDailyMovementsReportService>()
            .QueryAsync(new(date));
        var effective = Assert.Single(daily.Rows);
        Assert.Equal(line.ReplacementMovementId, effective.MovementId);
        Assert.Equal((MovementType.In, 1, 0, 1),
            (effective.Direction, effective.Quantity, daily.OutQuantity, daily.InQuantity));

        var history = await h.Provider.GetRequiredService<IMovementHistoryReportService>()
            .QueryAsync(new(date, date, IncludeAdjustments: true));
        Assert.Equal(3, history.Rows.Count);
        var preserved = Assert.Single(history.Rows, x => x.MovementId == original);
        var neutraliser = Assert.Single(history.Rows, x => x.MovementId == line.NeutralisingMovementId);
        var replacement = Assert.Single(history.Rows, x => x.MovementId == line.ReplacementMovementId);
        Assert.True(preserved.IsCorrectionOriginal);
        Assert.Equal("Correction", neutraliser.SourceText);
        Assert.True(neutraliser.IsCorrectionNeutraliser);
        Assert.Contains("Correction neutraliser", neutraliser.Status);
        Assert.Equal("Correction", replacement.SourceText);
        Assert.True(replacement.IsCorrectionReplacement);
        Assert.Contains($"replacement #{line.ReplacementMovementId}", preserved.Status);
    }

    [Fact]
    public async Task Ordinary_reversal_keeps_established_operational_and_history_semantics()
    {
        await using var h = await Harness.Create(UserRole.Administrator);
        var date = new DateOnly(2026, 8, 23);
        var original = await h.AddMovement(date, MovementType.Out, 3, h.CustomerId, 1);
        var reversal = await h.Corrections.ReverseAsync(new(Guid.NewGuid(), original, "Wrong dispatch"));

        var dailyService = h.Provider.GetRequiredService<IDailyMovementsReportService>();
        var originalDay = await dailyService
            .QueryAsync(new(date));
        Assert.Single(originalDay.Rows, x => x.MovementId == original);
        Assert.Equal((3, 0), (originalDay.OutQuantity, originalDay.InQuantity));
        var reversalDay = await dailyService.QueryAsync(new(new DateOnly(2026, 8, 26)));
        Assert.Single(reversalDay.Rows, x => x.MovementId == reversal.ReversalMovementId);
        Assert.Equal((0, 3), (reversalDay.OutQuantity, reversalDay.InQuantity));

        var history = await h.Provider.GetRequiredService<IMovementHistoryReportService>()
            .QueryAsync(new(date, new DateOnly(2026, 8, 26)));
        var reversalRow = Assert.Single(history.Rows, x => x.MovementId == reversal.ReversalMovementId);
        Assert.Equal("Reversal", reversalRow.SourceText);
        Assert.StartsWith($"Reversal of #{original}", reversalRow.Status);
        Assert.Empty(reversalRow.Lineage);
    }

    [Fact]
    public async Task Chained_correction_preserves_both_intermediate_roles_and_only_final_replacement_is_effective_and_eligible()
    {
        await using var h = await Harness.Create(UserRole.Administrator);
        var date = new DateOnly(2026, 8, 23);
        var original = await h.AddMovement(date, MovementType.In, 3, h.CustomerId, 1,
            MovementSource.Manual, "chain", "original IN 3");
        var first = Assert.Single((await h.Corrections.CorrectAsync(new(Guid.NewGuid(), original,
            date, h.CustomerId, 1, MovementType.In, 1, "chain", "replacement IN 1",
            "First quantity correction"))).Lines);
        var second = Assert.Single((await h.Corrections.CorrectAsync(new(Guid.NewGuid(), first.ReplacementMovementId,
            date, h.CustomerId, 1, MovementType.In, 2, "chain", "replacement IN 2",
            "Second quantity correction"))).Lines);

        // The report service creates a new DbContext for this later query. This
        // reproduces opening Movement History after the write context is gone.
        var history = await h.Provider.GetRequiredService<IMovementHistoryReportService>()
            .QueryAsync(new(date, date, IncludeAdjustments: true));
        Assert.Equal(5, history.Rows.Count);
        var intermediate = Assert.Single(history.Rows, x => x.MovementId == first.ReplacementMovementId);
        Assert.True(intermediate.IsCorrectionReplacement);
        Assert.True(intermediate.IsCorrectionOriginal);
        Assert.Equal(2, intermediate.Lineage.Count);
        Assert.Contains(intermediate.CreatedByCorrections, x => x.OriginalMovementId == original);
        Assert.Contains(intermediate.CorrectedByCorrections, x => x.ReplacementMovementId == second.ReplacementMovementId);
        Assert.Contains("Corrected replacement", intermediate.Status);
        Assert.Contains("Corrected —", intermediate.Status);
        Assert.False(intermediate.CanReverse);
        var final = Assert.Single(history.Rows, x => x.MovementId == second.ReplacementMovementId);
        Assert.True(final.IsCorrectionReplacement);
        Assert.False(final.IsCorrectionOriginal);
        Assert.True(final.CanReverse);
        Assert.All(history.Rows.Where(x => x.IsCorrectionRelated), x => Assert.Equal("Correction", x.SourceText));

        var daily = await h.Provider.GetRequiredService<IDailyMovementsReportService>().QueryAsync(new(date));
        var dailyRow = Assert.Single(daily.Rows);
        Assert.Equal((second.ReplacementMovementId, MovementType.In, 2, 0, 2),
            (dailyRow.MovementId, dailyRow.Direction, dailyRow.Quantity, daily.OutQuantity, daily.InQuantity));
        var weekly = await h.Provider.GetRequiredService<IWeeklyMovementsReportService>().QueryAsync(new(date));
        Assert.Equal((0, 2), (weekly.OutQuantity, weekly.InQuantity));
        Assert.Single(weekly.Rows, x => x.MovementId == second.ReplacementMovementId);
        var monthly = await h.Provider.GetRequiredService<IMonthlySummaryReportService>().QueryAsync(new(date));
        Assert.Equal((0, 2), (monthly.OutQuantity, monthly.InQuantity));

        Assert.True((await h.Corrections.GetAsync(first.ReplacementMovementId))!.IsAlreadyReversed);
        Assert.False((await h.Corrections.GetAsync(second.ReplacementMovementId))!.IsAlreadyReversed);
        await Assert.ThrowsAsync<InvalidOperationException>(() => h.Corrections.ReverseAsync(
            new(Guid.NewGuid(), first.ReplacementMovementId, "Superseded replacement cannot reverse")));
        await using var db = await h.Factory.CreateDbContextAsync();
        Assert.Equal(5, await db.BinMovements.CountAsync());
        Assert.Equal(2, await db.MovementCorrectionLines.CountAsync());

        var pdf = await h.Provider.GetRequiredService<IMovementHistoryReportPdfService>()
            .BuildPdfAsync(history, includeNotes: true);
        Assert.True(pdf.Length > 1000);
        Assert.Equal("%PDF", System.Text.Encoding.ASCII.GetString(pdf, 0, 4));
    }

    [Fact]
    public async Task Three_successive_corrections_have_unbounded_projection_and_only_latest_replacement_is_effective()
    {
        await using var h = await Harness.Create(UserRole.Administrator);
        var date = new DateOnly(2026, 8, 23);
        var original = await h.AddMovement(date, MovementType.In, 3, h.CustomerId, 1);
        var current = original;
        foreach (var quantity in new[] { 1, 2, 4 })
        {
            current = Assert.Single((await h.Corrections.CorrectAsync(new(Guid.NewGuid(), current,
                date, h.CustomerId, 1, MovementType.In, quantity, null, null,
                $"Correct quantity to {quantity}"))).Lines).ReplacementMovementId;
        }

        var history = await h.Provider.GetRequiredService<IMovementHistoryReportService>()
            .QueryAsync(new(date, date, IncludeAdjustments: true));
        Assert.Equal(7, history.Rows.Count);
        Assert.Equal(2, history.Rows.Count(x => x.IsCorrectionOriginal && x.IsCorrectionReplacement));
        Assert.All(history.Rows.Where(x => x.IsCorrectionOriginal), x => Assert.False(x.CanReverse));
        Assert.True(Assert.Single(history.Rows, x => x.MovementId == current).CanReverse);
        var daily = await h.Provider.GetRequiredService<IDailyMovementsReportService>().QueryAsync(new(date));
        Assert.Equal((current, 4, 0, 4),
            (Assert.Single(daily.Rows).MovementId, Assert.Single(daily.Rows).Quantity,
                daily.OutQuantity, daily.InQuantity));
        Assert.Equal(4, (await h.Provider.GetRequiredService<IWeeklyMovementsReportService>()
            .QueryAsync(new(date))).InQuantity);
        Assert.Equal(4, (await h.Provider.GetRequiredService<IMonthlySummaryReportService>()
            .QueryAsync(new(date))).InQuantity);
    }

    [Fact]
    public async Task Individual_line_in_batch_can_be_corrected_without_heuristic_grouping()
    {
        await using var h = await Harness.Create(UserRole.Administrator);
        var batch = await h.AddBatch(new DateOnly(2026, 8, 10), MovementType.Out, [2, 4]);
        var ids = await h.BatchMovementIds(batch);
        await h.Corrections.CorrectAsync(new(Guid.NewGuid(), ids[0], new DateOnly(2026, 8, 10),
            h.CustomerId, 1, MovementType.Out, 1, "fixed", "line", "Quantity was one"));
        await using var db = await h.Factory.CreateDbContextAsync();
        Assert.NotNull((await db.BinMovements.FindAsync(ids[0]))!.CorrectedByMovementId);
        Assert.Null((await db.BinMovements.FindAsync(ids[1]))!.CorrectedByMovementId);
    }

    [Fact]
    public async Task Whole_persisted_batch_date_and_direction_correction_is_atomic_and_retryable()
    {
        await using var h = await Harness.Create(UserRole.Operator);
        var batch = await h.AddBatch(new DateOnly(2026, 8, 25), MovementType.In, [100, 111]);
        var id = Guid.NewGuid();
        var request = new CorrectBatchRequest(id, batch, new DateOnly(2026, 8, 24), MovementType.Out, "Whole batch entered wrong");
        var result = await h.Corrections.CorrectBatchAsync(request);
        Assert.Equal(result.Lines.ToArray(), (await h.Corrections.CorrectBatchAsync(request)).Lines.ToArray());
        Assert.Equal(2, result.Lines.Count);
        await using var db = await h.Factory.CreateDbContextAsync();
        var replacements = await db.BinMovements.AsNoTracking()
            .Where(x => result.Lines.Select(l => l.ReplacementMovementId).Contains(x.Id)).ToListAsync();
        Assert.All(replacements, x => { Assert.Equal(new DateOnly(2026, 8, 24), x.MovementDate); Assert.Equal(MovementType.Out, x.MovementType); });
        Assert.Single(replacements.Select(x => x.MovementBatchId).Distinct());
        Assert.Equal(211, replacements.Sum(x => x.Quantity));
    }

    [Fact]
    public async Task Persisted_batch_entry_selection_loads_only_authoritative_batch_lines_for_dialog_preview()
    {
        await using var h = await Harness.Create(UserRole.Operator);
        var date = new DateOnly(2026, 8, 25);
        var batchId = await h.AddBatch(date, MovementType.In, [35, 6]);
        var expectedIds = await h.BatchMovementIds(batchId);
        var unrelated = await h.AddMovement(date, MovementType.In, 99, h.CustomerId, 1,
            MovementSource.Batch);
        var selected = await h.Corrections.GetAsync(expectedIds[0]);

        Assert.NotNull(selected);
        Assert.Equal(batchId, selected.MovementBatchId);
        var detail = await h.Corrections.GetBatchAsync(selected.MovementBatchId!.Value);
        Assert.NotNull(detail);
        Assert.True(detail.IsEligible);
        Assert.Equal(expectedIds, detail.Lines.Select(x => x.MovementId).ToArray());
        Assert.All(detail.Lines, x => Assert.Equal(batchId, x.MovementBatchId));
        Assert.DoesNotContain(detail.Lines, x => x.MovementId == unrelated);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Whole_batch_supports_date_only_and_direction_only(bool dateOnly)
    {
        await using var h = await Harness.Create(UserRole.Administrator);
        var batch = await h.AddBatch(new DateOnly(2026, 8, 20), MovementType.Out, [8, 9]);
        var result = await h.Corrections.CorrectBatchAsync(new(Guid.NewGuid(), batch,
            dateOnly ? new DateOnly(2026, 8, 19) : null,
            dateOnly ? null : MovementType.In,
            dateOnly ? "Batch date correction" : "Batch direction correction"));
        await using var db = await h.Factory.CreateDbContextAsync();
        var replacements = await db.BinMovements.Where(x => result.Lines.Select(l => l.ReplacementMovementId).Contains(x.Id)).ToListAsync();
        Assert.All(replacements, x => Assert.Equal(dateOnly ? new DateOnly(2026, 8, 19) : new DateOnly(2026, 8, 20), x.MovementDate));
        Assert.All(replacements, x => Assert.Equal(dateOnly ? MovementType.Out : MovementType.In, x.MovementType));
    }

    [Fact]
    public async Task Batch_conflict_rolls_back_every_line_and_reverse_correct_share_unique_guard()
    {
        await using var h = await Harness.Create(UserRole.Administrator);
        var batch = await h.AddBatch(new DateOnly(2026, 8, 20), MovementType.Out, [5, 6, 7]);
        var ids = await h.BatchMovementIds(batch);
        await h.Corrections.ReverseAsync(new(Guid.NewGuid(), ids[1], "Second line invalid"));
        await Assert.ThrowsAsync<InvalidOperationException>(() => h.Corrections.CorrectBatchAsync(
            new(Guid.NewGuid(), batch, new DateOnly(2026, 8, 19), null, "Batch date wrong")));
        await using var db = await h.Factory.CreateDbContextAsync();
        Assert.Empty(await db.MovementCorrectionOperations.AsNoTracking().ToListAsync());
        Assert.Null((await db.BinMovements.FindAsync(ids[0]))!.CorrectedByMovementId);
        Assert.Null((await db.BinMovements.FindAsync(ids[2]))!.CorrectedByMovementId);
        await Assert.ThrowsAsync<InvalidOperationException>(() => h.Corrections.CorrectAsync(new(Guid.NewGuid(),
            ids[1], new DateOnly(2026, 8, 20), h.CustomerId, 1, MovementType.Out, 6, null, null, "Try correction")));
    }

    [Fact]
    public async Task Batch_validation_failure_before_transaction_leaves_no_correction_artifacts()
    {
        await using var h = await Harness.Create(UserRole.Administrator);
        var batch = await h.AddBatch(new DateOnly(2026, 8, 20), MovementType.Out, [5, 6]);
        await Assert.ThrowsAsync<ArgumentException>(() => h.Corrections.CorrectBatchAsync(
            new(Guid.NewGuid(), batch, new DateOnly(2026, 8, 28), null, "Future date is invalid")));
        await using var db = await h.Factory.CreateDbContextAsync();
        Assert.Empty(await db.MovementCorrectionOperations.AsNoTracking().ToListAsync());
        Assert.Empty(await db.MovementCorrectionLines.AsNoTracking().ToListAsync());
        Assert.Empty(await db.BinMovements.Where(x => x.ReversesMovementId != null).ToListAsync());
        Assert.Single(await db.MovementBatches.AsNoTracking().ToListAsync());
    }

    [Theory]
    [InlineData(UserRole.Viewer, false)]
    [InlineData(UserRole.Operator, true)]
    [InlineData(UserRole.Administrator, true)]
    public async Task Whole_batch_correction_role_path_reaches_service_for_authorized_roles(UserRole role, bool allowed)
    {
        await using var h = await Harness.Create(role);
        var batch = await h.AddBatch(new DateOnly(2026, 8, 20), MovementType.Out, [2, 3]);
        var action = () => h.Corrections.CorrectBatchAsync(new(Guid.NewGuid(), batch,
            new DateOnly(2026, 8, 19), null, "Correct persisted batch date"));
        if (allowed)
            Assert.Equal(2, (await action()).Lines.Count);
        else
            await Assert.ThrowsAsync<UnauthorizedAccessException>(action);
    }

    [Theory]
    [InlineData(UserRole.Viewer, false)]
    [InlineData(UserRole.Operator, true)]
    [InlineData(UserRole.Administrator, true)]
    public async Task Correction_authorization_is_enforced(UserRole role, bool allowed)
    {
        await using var h = await Harness.Create(role);
        var id = await h.AddMovement(new DateOnly(2026, 8, 20), MovementType.Out, 10, h.CustomerId, 1);
        var action = () => h.Corrections.CorrectAsync(new(Guid.NewGuid(), id, new DateOnly(2026, 8, 20),
            h.CustomerId, 1, MovementType.Out, 1, null, null, "Quantity correction"));
        if (allowed) await action(); else await Assert.ThrowsAsync<UnauthorizedAccessException>(action);
    }

    [Theory]
    [InlineData(MovementSource.ExcelImport)]
    [InlineData(MovementSource.Adjustment)]
    public async Task Sensitive_sources_are_blocked_from_ordinary_correction(MovementSource source)
    {
        await using var h = await Harness.Create(UserRole.Administrator);
        var id = await h.AddMovement(new DateOnly(2026, 8, 20), MovementType.Out, 10, h.CustomerId, 1, source);
        await Assert.ThrowsAsync<InvalidOperationException>(() => h.Corrections.CorrectAsync(new(Guid.NewGuid(), id,
            new DateOnly(2026, 8, 20), h.CustomerId, 1, MovementType.Out, 1, null, null, "Sensitive correction")));
    }

    [Fact]
    public async Task Mandatory_reason_and_changed_payload_retry_are_rejected()
    {
        await using var h = await Harness.Create(UserRole.Administrator);
        var id = await h.AddMovement(new DateOnly(2026, 8, 20), MovementType.Out, 10, h.CustomerId, 1);
        await Assert.ThrowsAsync<InvalidOperationException>(() => h.Corrections.CorrectAsync(new(Guid.NewGuid(), id,
            new DateOnly(2026, 8, 20), h.CustomerId, 1, MovementType.Out, 1, null, null, "")));
        var op = Guid.NewGuid();
        await h.Corrections.CorrectAsync(new(op, id, new DateOnly(2026, 8, 20), h.CustomerId, 1, MovementType.Out, 1, null, null, "Quantity correction"));
        await Assert.ThrowsAsync<InvalidOperationException>(() => h.Corrections.CorrectAsync(new(op, id,
            new DateOnly(2026, 8, 20), h.CustomerId, 1, MovementType.Out, 2, null, null, "Quantity correction")));
    }

    [Fact]
    public async Task Operator_change_has_persistent_review_acknowledgement_and_audit_and_batch_drilldown()
    {
        await using var h = await Harness.Create(UserRole.Operator);
        var batch = await h.AddBatch(new DateOnly(2026, 8, 20), MovementType.Out, [2, 3]);
        var id = (await h.BatchMovementIds(batch))[0];
        await h.Corrections.ReverseAsync(new(Guid.NewGuid(), id, "Operator reversal"));
        await using (var db = await h.Factory.CreateDbContextAsync())
        {
            var admin = new UserAccount { Username = "admin", DisplayName = "Admin", PasswordHash = "x", PasswordSalt = "x", Role = UserRole.Administrator, IsActive = true };
            db.Add(admin); await db.SaveChangesAsync(); h.Provider.GetRequiredService<UserSession>().SignIn(admin);
        }
        var audit = h.Provider.GetRequiredService<IAuditService>();
        var pending = await audit.GetUnreviewedMovementChangesAsync();
        var change = Assert.Single(pending);
        await audit.MarkMovementChangesReviewedAsync([change.Id]);
        Assert.Empty(await audit.GetUnreviewedMovementChangesAsync());
        var detail = await audit.GetMovementBatchDetailAsync(batch);
        Assert.Equal(2, detail.Count); Assert.All(detail, x => Assert.Equal(batch, x.BatchId));
        await using var verify = await h.Factory.CreateDbContextAsync();
        Assert.True((await verify.AuditEvents.FindAsync(change.Id))!.ReviewedUtc.HasValue);
        Assert.True(await verify.AuditEvents.AnyAsync(x => x.Action == "MOVEMENT_CHANGE_REVIEWED"));
    }

    [Fact]
    public async Task Review_filters_counts_and_acknowledgement_are_exact_event_scoped()
    {
        await using var h = await Harness.Create(UserRole.Operator);
        await using (var db = await h.Factory.CreateDbContextAsync())
        {
            db.AuditEvents.AddRange(
                new AuditEvent { Username = "operator", Action = "MOVEMENT_BATCH_CORRECTED", EntityType = "MovementBatch", EntityId = "19", Description = "first", RequiresAdministratorReview = true },
                new AuditEvent { Username = "operator", Action = "MOVEMENT_BATCH_CORRECTED", EntityType = "MovementBatch", EntityId = "19", Description = "second", RequiresAdministratorReview = true },
                new AuditEvent { Username = "admin", Action = "LOGIN_SUCCESS", EntityType = "User", EntityId = "1", Description = "login" });
            var admin = new UserAccount { Username = "admin", DisplayName = "Admin", PasswordHash = "x", PasswordSalt = "x", Role = UserRole.Administrator, IsActive = true };
            db.Add(admin); await db.SaveChangesAsync(); h.Provider.GetRequiredService<UserSession>().SignIn(admin);
        }
        var audit = h.Provider.GetRequiredService<IAuditService>();
        Assert.Equal(2, (await audit.GetAdministratorReviewStateAsync()).PendingCount);
        Assert.Equal(3, (await audit.GetAuditTrailAsync(AuditReviewFilter.All)).Count);
        var pending = await audit.GetAuditTrailAsync(AuditReviewFilter.NeedsReview);
        Assert.Equal(2, pending.Count); Assert.All(pending, x => Assert.Equal("Needs review", x.ReviewState));
        await audit.MarkMovementChangesReviewedAsync([pending[0].Id]);
        Assert.Equal(1, (await audit.GetAdministratorReviewStateAsync()).PendingCount);
        var reviewed = Assert.Single(await audit.GetAuditTrailAsync(AuditReviewFilter.Reviewed));
        Assert.Equal(pending[0].Id, reviewed.Id); Assert.Equal("Reviewed", reviewed.ReviewState);
        Assert.NotNull(reviewed.ReviewedUtc); Assert.Equal("admin", reviewed.ReviewedByUsername);
        Assert.Contains((await audit.GetAuditTrailAsync(AuditReviewFilter.NeedsReview)).Single().Id,
            pending.Select(x => x.Id));
        await audit.MarkMovementChangesReviewedAsync([(await audit.GetAuditTrailAsync(AuditReviewFilter.NeedsReview)).Single().Id]);
        Assert.Equal(0, (await audit.GetAdministratorReviewStateAsync()).PendingCount);
        await using var verify = await h.Factory.CreateDbContextAsync();
        Assert.Equal(2, await verify.AuditEvents.CountAsync(x => x.Action == "MOVEMENT_CHANGE_REVIEWED"));
    }

    [Fact]
    public async Task Whole_batch_review_detail_resolves_exact_persisted_lineage_only()
    {
        await using var h = await Harness.Create(UserRole.Operator);
        var batch = await h.AddBatch(new DateOnly(2026, 8, 20), MovementType.Out, [2, 3]);
        await h.AddMovement(new DateOnly(2026, 8, 20), MovementType.Out, 99, h.CustomerId, 1);
        var result = await h.Corrections.CorrectBatchAsync(new(Guid.NewGuid(), batch, null, MovementType.In, "Wrong direction"));
        await using (var db = await h.Factory.CreateDbContextAsync())
        {
            var admin = new UserAccount { Username = "admin", DisplayName = "Admin", PasswordHash = "x", PasswordSalt = "x", Role = UserRole.Administrator, IsActive = true };
            db.Add(admin); await db.SaveChangesAsync(); h.Provider.GetRequiredService<UserSession>().SignIn(admin);
        }
        var audit = h.Provider.GetRequiredService<IAuditService>();
        var auditEvent = Assert.Single(await audit.GetAuditTrailAsync(AuditReviewFilter.NeedsReview));
        var detail = Assert.IsType<MovementChangeAuditDetail>(await audit.GetMovementChangeDetailAsync(auditEvent.Id));
        Assert.Equal(batch, detail.OriginalBatchId); Assert.Equal(result.ReplacementBatchId, detail.ReplacementBatchId);
        Assert.Equal("Wrong direction", detail.Reason); Assert.Equal(6, detail.Lines.Count);
        Assert.Equal(result.Lines.SelectMany(x => new[] { x.OriginalMovementId, x.NeutralisingMovementId, x.ReplacementMovementId }).Order(),
            detail.Lines.Select(x => x.MovementId).Order());
        Assert.DoesNotContain(detail.Lines, x => x.Quantity == 99);
        Assert.All(detail.Lines.Where(x => x.Role == "Corrected replacement"), x => Assert.Equal(MovementType.In, x.Direction));
    }

    [Fact]
    public async Task Movement_change_detail_fails_closed_when_audit_lineage_is_missing()
    {
        await using var h = await Harness.Create(UserRole.Administrator);
        long id;
        await using (var db = await h.Factory.CreateDbContextAsync())
        {
            var item = new AuditEvent { Username = "operator", Action = "MOVEMENT_BATCH_CORRECTED", EntityType = "MovementBatch", EntityId = "19", Description = "bad lineage", AfterValues = "[]", RequiresAdministratorReview = true };
            db.Add(item); await db.SaveChangesAsync(); id = item.Id;
        }
        Assert.Null(await h.Provider.GetRequiredService<IAuditService>().GetMovementChangeDetailAsync(id));
    }

    private sealed class Harness : IAsyncDisposable
    {
        private readonly SqliteConnection connection;
        public ServiceProvider Provider { get; }
        public IDbContextFactory<BinTrackerDbContext> Factory { get; }
        public IMovementCorrectionService Corrections { get; }
        public int CustomerId { get; }
        private Harness(SqliteConnection c, ServiceProvider p, IDbContextFactory<BinTrackerDbContext> f,
            IMovementCorrectionService corrections, int customerId) =>
            (connection, Provider, Factory, Corrections, CustomerId) = (c, p, f, corrections, customerId);
        public static async Task<Harness> Create(UserRole role)
        {
            var c = new SqliteConnection("Data Source=:memory:"); await c.OpenAsync();
            var services = new ServiceCollection();
            services.AddSingleton<IBusinessClock>(new MovementCorrectionWorkflowClock());
            services.AddDbContextFactory<BinTrackerDbContext>(o => o.UseSqlite(c));
            services.AddBinTrackerServices(); var p = services.BuildServiceProvider();
            var f = p.GetRequiredService<IDbContextFactory<BinTrackerDbContext>>();
            await using var db = await f.CreateDbContextAsync(); await db.Database.EnsureCreatedAsync(); await DatabaseSetup.InitializeSqliteAsync(db);
            var user = new UserAccount { Username = "user", DisplayName = "User", PasswordHash = "x", PasswordSalt = "x", Role = role, IsActive = true };
            var customer = new Customer { CustomerCode = "A", Name = "Buyer A" }; db.AddRange(user, customer); await db.SaveChangesAsync();
            p.GetRequiredService<UserSession>().SignIn(user);
            return new(c, p, f, p.GetRequiredService<IMovementCorrectionService>(), customer.Id);
        }
        public async Task<int> AddCustomer(string code, string name) { await using var db = await Factory.CreateDbContextAsync(); var x = new Customer { CustomerCode = code, Name = name }; db.Add(x); await db.SaveChangesAsync(); return x.Id; }
        public async Task<long> AddMovement(DateOnly date, MovementType type, int qty, int customer, int container,
            MovementSource source = MovementSource.Manual, string? reference = null, string? notes = null)
        { await using var db = await Factory.CreateDbContextAsync(); var x = new BinMovement { MovementDate = date, MovementType = type, Quantity = qty, CustomerId = customer, ContainerTypeId = container, Source = source, ReferenceNumber = reference, Notes = notes }; db.Add(x); await db.SaveChangesAsync(); return x.Id; }
        public async Task<int> AddBatch(DateOnly date, MovementType type, int[] quantities)
        { await using var db = await Factory.CreateDbContextAsync(); var b = new MovementBatch { MovementDate = date, MovementType = type, Source = MovementSource.Batch }; db.Add(b); foreach (var q in quantities) b.Movements.Add(new BinMovement { MovementDate = date, MovementType = type, Source = MovementSource.Batch, CustomerId = CustomerId, ContainerTypeId = 1, Quantity = q }); await db.SaveChangesAsync(); return b.Id; }
        public async Task<long[]> BatchMovementIds(int id) { await using var db = await Factory.CreateDbContextAsync(); return await db.BinMovements.Where(x => x.MovementBatchId == id).OrderBy(x => x.Id).Select(x => x.Id).ToArrayAsync(); }
        public async ValueTask DisposeAsync() { await Provider.DisposeAsync(); await connection.DisposeAsync(); }
    }
}

file sealed class MovementCorrectionWorkflowClock : IBusinessClock
{
    public DateTime UtcNow => new(2026, 8, 26, 2, 0, 0, DateTimeKind.Utc);
    public DateTime LocalNow => new(2026, 8, 26, 12, 0, 0, DateTimeKind.Unspecified);
    public DateOnly Today => new(2026, 8, 26);
    public string TimeZoneId => "Australia/Melbourne";
}

file static class DailyResultExtensions
{
    public static int Net(this DailyMovementsReportResult result) => result.OutQuantity - result.InQuantity;
}
