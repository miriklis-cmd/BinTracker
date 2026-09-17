using BinTracker.Core;
using BinTracker.Services;
using System.Globalization;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace BinTracker.IntegrationTests;

public sealed class Task20MutationActivationTests
{
    [Fact]
    public async Task Whole_batch_selection_accepts_root_and_native_output_physical_anchors()
    {
        await using var f = await Task20Fixture.CreateAsync();
        var root = await f.Database.CreateBatchAsync(7, 4);

        var originalBatch = Assert.IsType<MovementBatchCorrectionDetail>(
            await f.Corrections.GetBatchAsync(root.BatchId));
        var originalPreview = Assert.IsType<LogicalMovementMutationPreview>(
            await f.Corrections.PreviewLogicalForBatchAsync(root.BatchId));
        Assert.Equal(root.RootId, originalPreview.LogicalMovementBatchId.Value);
        Assert.Equal(root.BatchId, originalPreview.RootMovementBatchId);
        Assert.Equal(LogicalMovementPhysicalBatchAnchorKind.RootOriginal,
            originalPreview.PhysicalBatchAnchor?.Kind);
        Assert.True(MovementCorrectionSelection.IsValidWholeBatchAnchor(
            root.BatchId, originalBatch, originalPreview));

        var firstCurrentMovementIds = originalPreview.Lines
            .Select(x => x.LastEffective.MovementId)
            .ToArray();
        var first = await f.Corrections.ExecuteLogicalAsync(new(
            Guid.NewGuid(), originalPreview.LogicalMovementBatchId,
            originalPreview.ExpectedGeneration,
            MovementMutationRequest.Correct(MovementMutationScope.WholeRoot,
                originalPreview.Lines.Select(x => x.LogicalMovementLineId),
                "create native physical output",
                movementDate: MovementFieldIntent<DateOnly>.Selected(Task20Fixture.Today))));
        var outputBatchId = Assert.IsType<int>(first.PhysicalOutputBatchId);
        Assert.NotEqual(root.BatchId, outputBatchId);

        var outputBatch = Assert.IsType<MovementBatchCorrectionDetail>(
            await f.Corrections.GetBatchAsync(outputBatchId));
        var outputPreview = Assert.IsType<LogicalMovementMutationPreview>(
            await f.Corrections.PreviewLogicalForBatchAsync(outputBatchId));
        Assert.Equal(root.RootId, outputPreview.LogicalMovementBatchId.Value);
        Assert.Equal(root.BatchId, outputPreview.RootMovementBatchId);
        Assert.Equal(LogicalMovementPhysicalBatchAnchorKind.LineageOutput,
            outputPreview.PhysicalBatchAnchor?.Kind);
        Assert.Equal(outputBatchId, outputPreview.PhysicalBatchAnchor?.MovementBatchId);
        Assert.Equal(1, outputPreview.ExpectedGeneration.Value);
        Assert.True(outputPreview.IsWholeRootCorrectionEligible);
        Assert.True(MovementCorrectionSelection.IsValidWholeBatchAnchor(
            outputBatchId, outputBatch, outputPreview));

        var originalAnchorCurrentPreview = Assert.IsType<LogicalMovementMutationPreview>(
            await f.Corrections.PreviewLogicalForBatchAsync(root.BatchId));
        var originalAnchorCurrentDetail =
            MovementCorrectionSelection.BuildCurrentWholeRootCorrectionDetail(
                root.BatchId, originalAnchorCurrentPreview);
        var outputAnchorCurrentDetail =
            MovementCorrectionSelection.BuildCurrentWholeRootCorrectionDetail(
                outputBatchId, outputPreview);
        var expectedCurrentIds = outputPreview.Lines
            .Select(x => x.LastEffective.MovementId)
            .OrderBy(x => x)
            .ToArray();

        Assert.Equal(Task20Fixture.Today, originalAnchorCurrentDetail.MovementDate);
        Assert.Equal(outputPreview.Lines[0].LastEffective.Direction,
            originalAnchorCurrentDetail.Direction);
        Assert.Equal(expectedCurrentIds,
            originalAnchorCurrentDetail.Lines
                .Select(x => x.MovementId)
                .OrderBy(x => x)
                .ToArray());
        Assert.Equal(expectedCurrentIds,
            outputAnchorCurrentDetail.Lines
                .Select(x => x.MovementId)
                .OrderBy(x => x)
                .ToArray());
        Assert.DoesNotContain(firstCurrentMovementIds,
            id => originalAnchorCurrentDetail.Lines.Any(x => x.MovementId == id));
        Assert.All(originalAnchorCurrentDetail.Lines,
            line => Assert.Equal(outputBatchId, line.MovementBatchId));

        var currentReplacement = outputPreview.Lines[0].LastEffective.MovementId;
        var selectedCurrent = Assert.IsType<LogicalMovementMutationPreview>(
            await f.Corrections.PreviewLogicalForMovementAsync(currentReplacement));
        Assert.Single(selectedCurrent.Lines, x =>
            x.LastEffective.MovementId == currentReplacement &&
            x.State == LogicalMovementLineState.Active);

        var supersededOriginal = firstCurrentMovementIds[0];
        var selectedHistorical = Assert.IsType<LogicalMovementMutationPreview>(
            await f.Corrections.PreviewLogicalForMovementAsync(supersededOriginal));
        Assert.DoesNotContain(selectedHistorical.Lines, x =>
            x.LastEffective.MovementId == supersededOriginal &&
            x.State == LogicalMovementLineState.Active);

        var currentMovementIds = outputPreview.Lines
            .Select(x => x.LastEffective.MovementId)
            .OrderBy(x => x)
            .ToArray();
        var second = await f.Corrections.ExecuteLogicalAsync(new(
            Guid.NewGuid(), outputPreview.LogicalMovementBatchId,
            outputPreview.ExpectedGeneration,
            MovementMutationRequest.Correct(MovementMutationScope.WholeRoot,
                outputPreview.Lines.Select(x => x.LogicalMovementLineId),
                "correct again from native output anchor",
                direction: MovementFieldIntent<MovementType>.Selected(MovementType.In))));
        Assert.Equal(2, second.ResultGeneration.Value);
        Assert.Equal(root.BatchId, await f.ScalarAsync(
            $"SELECT RootMovementBatchId FROM LogicalMovementBatches WHERE Id={root.RootId}"));
        var neutralisedCurrentIds = await f.ReadInt64sAsync($"""
            SELECT m.ReversesMovementId
            FROM LogicalMovementLedgerLinks l
            JOIN BinMovements m ON m.Id=l.BinMovementId
            WHERE l.LogicalMovementBatchId={root.RootId}
              AND l.Role={(int)LogicalMovementTransformationRole.CorrectionNeutraliser}
              AND l.IntroducedByGenerationLineId IN (
                  SELECT Id FROM LogicalMovementGenerationLines
                  WHERE LogicalMovementBatchId={root.RootId}
                    AND LogicalMovementGenerationId=(
                        SELECT Id FROM LogicalMovementGenerations
                        WHERE LogicalMovementBatchId={root.RootId} AND GenerationNumber=2))
            ORDER BY m.ReversesMovementId;
            """);
        Assert.Equal(currentMovementIds, neutralisedCurrentIds);
        Assert.DoesNotContain(supersededOriginal, neutralisedCurrentIds);
    }

    [Fact]
    public async Task Physical_batch_without_authoritative_root_or_output_association_fails_closed()
    {
        await using var f = await Task20Fixture.CreateAsync();
        var root = await f.Database.CreateBatchAsync(7, 4);
        var preview = Assert.IsType<LogicalMovementMutationPreview>(
            await f.Corrections.PreviewLogicalForBatchAsync(root.BatchId));
        var mutation = await f.Corrections.ExecuteLogicalAsync(new(
            Guid.NewGuid(), preview.LogicalMovementBatchId, preview.ExpectedGeneration,
            MovementMutationRequest.Correct(MovementMutationScope.WholeRoot,
                preview.Lines.Select(x => x.LogicalMovementLineId), "create output",
                movementDate: MovementFieldIntent<DateOnly>.Selected(Task20Fixture.Today))));
        var outputBatchId = Assert.IsType<int>(mutation.PhysicalOutputBatchId);
        await f.ExecuteAsync(
            $"DELETE FROM LogicalMovementPhysicalOutputs WHERE MovementBatchId={outputBatchId};");

        Assert.Null(await f.Corrections.PreviewLogicalForBatchAsync(outputBatchId));
        var detail = Assert.IsType<MovementBatchCorrectionDetail>(
            await f.Corrections.GetBatchAsync(outputBatchId));
        Assert.False(detail.IsEligible);
    }

    [Fact]
    public async Task Physical_batch_claiming_original_and_output_roots_fails_closed_as_ambiguous()
    {
        await using var f = await Task20Fixture.CreateAsync();
        var firstRoot = await f.Database.CreateBatchAsync(7, 4);
        var preview = Assert.IsType<LogicalMovementMutationPreview>(
            await f.Corrections.PreviewLogicalForBatchAsync(firstRoot.BatchId));
        var mutation = await f.Corrections.ExecuteLogicalAsync(new(
            Guid.NewGuid(), preview.LogicalMovementBatchId, preview.ExpectedGeneration,
            MovementMutationRequest.Correct(MovementMutationScope.WholeRoot,
                preview.Lines.Select(x => x.LogicalMovementLineId), "create output",
                movementDate: MovementFieldIntent<DateOnly>.Selected(Task20Fixture.Today))));
        var outputBatchId = Assert.IsType<int>(mutation.PhysicalOutputBatchId);
        var secondRoot = await f.Database.CreateSingleAsync(
            Task20Fixture.Today, f.CustomerId, 1, 3);
        await f.ExecuteAsync($"""
            UPDATE LogicalMovementBatches
            SET RootMovementBatchId={outputBatchId}
            WHERE Id={secondRoot.RootId};
            """);

        var error = await Assert.ThrowsAsync<LogicalMovementMutationException>(
            () => f.Corrections.PreviewLogicalForBatchAsync(outputBatchId));
        Assert.Equal(LogicalMovementMutationFailure.Unhealthy, error.Failure);
    }

    [Fact]
    public async Task Cross_root_physical_output_association_fails_closed()
    {
        await using var f = await Task20Fixture.CreateAsync();
        var firstRoot = await f.Database.CreateBatchAsync(7, 4);
        var preview = Assert.IsType<LogicalMovementMutationPreview>(
            await f.Corrections.PreviewLogicalForBatchAsync(firstRoot.BatchId));
        var mutation = await f.Corrections.ExecuteLogicalAsync(new(
            Guid.NewGuid(), preview.LogicalMovementBatchId, preview.ExpectedGeneration,
            MovementMutationRequest.Correct(MovementMutationScope.WholeRoot,
                preview.Lines.Select(x => x.LogicalMovementLineId), "create output",
                movementDate: MovementFieldIntent<DateOnly>.Selected(Task20Fixture.Today))));
        var outputBatchId = Assert.IsType<int>(mutation.PhysicalOutputBatchId);
        var secondRoot = await f.Database.CreateSingleAsync(
            Task20Fixture.Today, f.CustomerId, 1, 3);
        await f.ExecuteAsync($"""
            PRAGMA foreign_keys=OFF;
            UPDATE LogicalMovementPhysicalOutputs
            SET LogicalMovementBatchId={secondRoot.RootId}
            WHERE MovementBatchId={outputBatchId};
            PRAGMA foreign_keys=ON;
            """);

        var error = await Assert.ThrowsAsync<LogicalMovementMutationException>(
            () => f.Corrections.PreviewLogicalForBatchAsync(outputBatchId));
        Assert.Equal(LogicalMovementMutationFailure.Unhealthy, error.Failure);
    }

    [Fact]
    public async Task Dormant_schema16_keeps_legacy_correction_and_does_not_offer_logical_preview()
    {
        await using var f = await Task20Fixture.CreateAsync(
            schema17: false, enabled: false, projection: false);
        var saved = await f.Movements.SaveSingleAsync(f.Single());
        var beforePreview = await f.StateAsync();
        var unavailable = await Assert.ThrowsAsync<LogicalMovementMutationException>(
            () => f.Corrections.PreviewLogicalForMovementAsync(saved.MovementId));
        Assert.Equal(LogicalMovementMutationFailure.SchemaUnavailable, unavailable.Failure);
        Assert.Equal(beforePreview, await f.StateAsync());

        var correction = await f.Corrections.CorrectAsync(new(
            Guid.NewGuid(), saved.MovementId, Task20Fixture.Today,
            f.CustomerId, 1, MovementType.Out, 8, "schema16", null,
            "schema16 compatibility"));
        Assert.Single(correction.Lines);
        Assert.Equal(1, await f.ScalarAsync("SELECT COUNT(*) FROM MovementCorrectionLines"));
        Assert.Equal(16, await f.ScalarAsync("SELECT Version FROM SchemaVersion WHERE Id=1"));

        await using var incompatible = await Task20Fixture.CreateAsync(
            schema17: false, enabled: true, projection: false);
        var schemaFailure = await Assert.ThrowsAsync<LogicalMovementMutationException>(
            () => incompatible.Corrections.PreviewLogicalForMovementAsync(1));
        Assert.Equal(LogicalMovementMutationFailure.SchemaUnavailable, schemaFailure.Failure);
        Assert.IsNotType<Microsoft.Data.Sqlite.SqliteException>(schemaFailure.InnerException);
    }

    [Fact]
    public async Task Native_correction_uses_the_preview_identity_and_generation_and_replays_exactly()
    {
        await using var f = await Task20Fixture.CreateAsync();
        var root = await f.Database.CreateBatchAsync(7, 4);
        var movementId = await f.ScalarAsync(
            $"SELECT Id FROM BinMovements WHERE MovementBatchId={root.BatchId} ORDER BY Id LIMIT 1");

        var batchPreview = Assert.IsType<LogicalMovementMutationPreview>(
            await f.Corrections.PreviewLogicalForBatchAsync(root.BatchId));
        var movementPreview = Assert.IsType<LogicalMovementMutationPreview>(
            await f.Corrections.PreviewLogicalForMovementAsync(movementId));
        var rootPreview = Assert.IsType<LogicalMovementMutationPreview>(
            await f.Corrections.PreviewLogicalAsync(batchPreview.LogicalMovementBatchId));

        Assert.Equal(root.RootId, batchPreview.LogicalMovementBatchId.Value);
        Assert.Equal(new LogicalMovementGenerationNumber(0), batchPreview.ExpectedGeneration);
        Assert.Equal(root.BatchId, batchPreview.RootMovementBatchId);
        Assert.True(batchPreview.IsWholeRootCorrectionEligible);
        Assert.Equal((batchPreview.LogicalMovementBatchId, batchPreview.ExpectedGeneration,
                batchPreview.RootMovementBatchId, batchPreview.IsWholeRootCorrectionEligible),
            (movementPreview.LogicalMovementBatchId, movementPreview.ExpectedGeneration,
                movementPreview.RootMovementBatchId, movementPreview.IsWholeRootCorrectionEligible));
        Assert.Equal((batchPreview.LogicalMovementBatchId, batchPreview.ExpectedGeneration,
                batchPreview.RootMovementBatchId, batchPreview.IsWholeRootCorrectionEligible),
            (rootPreview.LogicalMovementBatchId, rootPreview.ExpectedGeneration,
                rootPreview.RootMovementBatchId, rootPreview.IsWholeRootCorrectionEligible));
        Assert.Equal(batchPreview.Lines.Select(x => (x.LogicalMovementLineId, x.State, x.LastEffective.MovementId)),
            movementPreview.Lines.Select(x => (x.LogicalMovementLineId, x.State, x.LastEffective.MovementId)));
        Assert.Equal(batchPreview.Lines.Select(x => (x.LogicalMovementLineId, x.State, x.LastEffective.MovementId)),
            rootPreview.Lines.Select(x => (x.LogicalMovementLineId, x.State, x.LastEffective.MovementId)));
        Assert.Equal(new[] { 0, 1 }, batchPreview.Lines.Select(x => x.OriginalDisplayOrdinal));
        Assert.All(batchPreview.Lines, x => Assert.Equal(LogicalMovementLineState.Active, x.State));
        Assert.Equal(new[] { 7, 4 }, batchPreview.Lines.Select(x => x.LastEffective.Quantity));

        var operationId = Guid.NewGuid();
        var intent = MovementMutationRequest.Correct(
            MovementMutationScope.WholeRoot,
            batchPreview.Lines.Select(x => x.LogicalMovementLineId),
            "preview correction",
            movementDate: MovementFieldIntent<DateOnly>.Selected(Task20Fixture.Today));
        var command = new LogicalMovementMutationCommand(operationId,
            batchPreview.LogicalMovementBatchId, batchPreview.ExpectedGeneration, intent);

        var committed = await f.Corrections.ExecuteLogicalAsync(command);
        Assert.Equal(LogicalMovementMutationResultKind.Committed, committed.Kind);
        Assert.Equal(1, committed.ResultGeneration.Value);
        Assert.NotNull(committed.PhysicalOutputBatchId);
        Assert.Equal(2, await f.ScalarAsync($"""
            SELECT COUNT(*) FROM LogicalMovementGenerationLines gl
            JOIN LogicalMovementGenerations g ON g.Id=gl.LogicalMovementGenerationId
            WHERE g.LogicalMovementBatchId={root.RootId} AND g.GenerationNumber=1;
            """));
        Assert.Equal(2, await f.ScalarAsync($"""
            SELECT COUNT(*) FROM LogicalMovementLedgerLinks
            WHERE LogicalMovementBatchId={root.RootId}
              AND Role={(int)LogicalMovementTransformationRole.CorrectionNeutraliser};
            """));
        Assert.Equal(2, await f.ScalarAsync($"""
            SELECT COUNT(*) FROM LogicalMovementLedgerLinks
            WHERE LogicalMovementBatchId={root.RootId}
              AND Role={(int)LogicalMovementTransformationRole.CorrectionReplacement};
            """));
        Assert.Equal(1, await f.ScalarAsync(
            $"SELECT CurrentGenerationNumber FROM LogicalMovementBatches WHERE Id={root.RootId}"));
        var outputPreview = Assert.IsType<LogicalMovementMutationPreview>(
            await f.Corrections.PreviewLogicalForBatchAsync(committed.PhysicalOutputBatchId!.Value));
        Assert.Equal(batchPreview.LogicalMovementBatchId, outputPreview.LogicalMovementBatchId);
        Assert.Equal(1, outputPreview.ExpectedGeneration.Value);

        var committedState = await f.StateAsync();
        var replay = await f.Corrections.ExecuteLogicalAsync(command);
        Assert.Equal(LogicalMovementMutationResultKind.Replayed, replay.Kind);
        Assert.Equal(committed.OperationId, replay.OperationId);
        Assert.Equal(committed.ResultGeneration, replay.ResultGeneration);
        Assert.Equal(committed.PhysicalOutputBatchId, replay.PhysicalOutputBatchId);
        Assert.Equal(committedState, await f.StateAsync());

        var changed = command with
        {
            Mutation = MovementMutationRequest.Correct(
                MovementMutationScope.WholeRoot,
                batchPreview.Lines.Select(x => x.LogicalMovementLineId),
                "preview correction",
                direction: MovementFieldIntent<MovementType>.Selected(MovementType.In))
        };
        var conflict = await Assert.ThrowsAsync<LogicalMovementMutationException>(
            () => f.Corrections.ExecuteLogicalAsync(changed));
        Assert.Equal(LogicalMovementMutationFailure.OperationIdConflict, conflict.Failure);
        Assert.Equal(committedState, await f.StateAsync());
    }

    [Fact]
    public async Task Native_reversal_uses_preview_generation_and_a_stale_preview_never_refreshes_itself()
    {
        await using var f = await Task20Fixture.CreateAsync();
        var root = await f.Database.CreateSingleAsync(Task20Fixture.Today, f.CustomerId, 1, 7);
        var preview = Assert.IsType<LogicalMovementMutationPreview>(
            await f.Corrections.PreviewLogicalForMovementAsync(root.MovementId));
        var line = Assert.Single(preview.Lines);
        Assert.Equal(root.RootId, preview.LogicalMovementBatchId.Value);
        Assert.Equal(0, preview.ExpectedGeneration.Value);
        Assert.Equal(root.MovementId, line.RootMovementId);
        Assert.Equal(LogicalMovementLineState.Active, line.State);

        var operationId = Guid.NewGuid();
        var command = new LogicalMovementMutationCommand(operationId,
            preview.LogicalMovementBatchId, preview.ExpectedGeneration,
            MovementMutationRequest.Reverse(MovementMutationScope.Individual,
                [line.LogicalMovementLineId], "preview reversal"));
        var committed = await f.Corrections.ExecuteLogicalAsync(command);
        Assert.Equal(LogicalMovementMutationResultKind.Committed, committed.Kind);
        Assert.Equal(1, committed.ResultGeneration.Value);

        var current = Assert.IsType<LogicalMovementMutationPreview>(
            await f.Corrections.PreviewLogicalAsync(preview.LogicalMovementBatchId));
        var reversed = Assert.Single(current.Lines);
        Assert.Equal(1, current.ExpectedGeneration.Value);
        Assert.Equal(LogicalMovementLineState.Reversed, reversed.State);
        Assert.Equal(root.MovementId, reversed.LastEffective.MovementId);
        Assert.NotNull(reversed.TerminalReversalMovementId);
        Assert.False(current.IsWholeRootCorrectionEligible);

        var committedState = await f.StateAsync();
        var replay = await f.Corrections.ExecuteLogicalAsync(command);
        Assert.Equal(LogicalMovementMutationResultKind.Replayed, replay.Kind);
        Assert.Equal(committed.OperationId, replay.OperationId);
        Assert.Equal(committedState, await f.StateAsync());

        var stale = command with { ClientOperationId = Guid.NewGuid() };
        var staleError = await Assert.ThrowsAsync<LogicalMovementMutationException>(
            () => f.Corrections.ExecuteLogicalAsync(stale));
        Assert.Equal(LogicalMovementMutationFailure.StaleGeneration, staleError.Failure);
        Assert.Equal(committedState, await f.StateAsync());
    }

    [Fact]
    public async Task Current_whole_batch_preview_blocks_a_native_reversed_line_until_explicit_decision_UI_exists()
    {
        await using var f = await Task20Fixture.CreateAsync();
        var root = await f.Database.CreateBatchAsync(7, 4);
        var ids = await f.Database.LineIdsAsync(root.RootId);
        await f.Database.MutateAsync(root.RootId, 0,
            MovementMutationRequest.Reverse(MovementMutationScope.Individual, [new(ids[1])], "reverse second"));
        var preview = Assert.IsType<MovementBatchCorrectionDetail>(await f.Corrections.GetBatchAsync(root.BatchId));
        Assert.False(preview.IsEligible);
    }

    [Theory]
    [InlineData("restore")]
    [InlineData("remain-reversed")]
    public async Task Native_whole_root_correction_rejects_reversed_lines_even_with_explicit_decisions(
        string disposition)
    {
        await using var f = await Task20Fixture.CreateAsync();
        var root = await f.Database.CreateBatchAsync(7, 4);
        var lineIds = (await f.Database.LineIdsAsync(root.RootId))
            .Select(x => new LogicalMovementLineId(x))
            .ToArray();
        await f.Database.MutateAsync(root.RootId, 0,
            MovementMutationRequest.Reverse(MovementMutationScope.Individual,
                [lineIds[1]], "reverse second"));

        var preview = Assert.IsType<LogicalMovementMutationPreview>(
            await f.Corrections.PreviewLogicalAsync(new(root.RootId)));
        var reversed = Assert.Single(preview.Lines,
            x => x.State == LogicalMovementLineState.Reversed);
        var decision = disposition == "restore"
            ? ReversedLineDecision.Restore(reversed.LogicalMovementLineId)
            : ReversedLineDecision.RemainReversed(reversed.LogicalMovementLineId);
        var beforeState = await f.StateAsync();
        var beforeGeneration = await f.ScalarAsync(
            $"SELECT CurrentGenerationNumber FROM LogicalMovementBatches WHERE Id={root.RootId}");
        var beforeMovements = await f.ScalarAsync("SELECT COUNT(*) FROM BinMovements");
        var beforeGenerations = await f.ScalarAsync("SELECT COUNT(*) FROM LogicalMovementGenerations");
        var beforeOperations = await f.ScalarAsync("SELECT COUNT(*) FROM MovementCorrectionOperations");
        var beforeAudits = await f.ScalarAsync("SELECT COUNT(*) FROM AuditEvents");
        var beforeOutputs = await f.ScalarAsync("SELECT COUNT(*) FROM LogicalMovementPhysicalOutputs");
        var beforeLegacyLines = await f.ScalarAsync("SELECT COUNT(*) FROM MovementCorrectionLines");

        var error = await Record.ExceptionAsync(() => f.Corrections.ExecuteLogicalAsync(new(
            Guid.NewGuid(), preview.LogicalMovementBatchId, preview.ExpectedGeneration,
            MovementMutationRequest.Correct(MovementMutationScope.WholeRoot,
                lineIds, "blocked whole-root correction",
                quantity: MovementFieldIntent<int>.Selected(8),
                reversedLineDecisions: [decision]))));

        var unavailable = Assert.IsType<LogicalMovementMutationException>(error);
        Assert.Equal(LogicalMovementMutationFailure.WholeRootCorrectionUnavailable,
            unavailable.Failure);
        Assert.Equal(beforeState, await f.StateAsync());
        Assert.Equal(beforeGeneration, await f.ScalarAsync(
            $"SELECT CurrentGenerationNumber FROM LogicalMovementBatches WHERE Id={root.RootId}"));
        Assert.Equal(beforeMovements, await f.ScalarAsync("SELECT COUNT(*) FROM BinMovements"));
        Assert.Equal(beforeGenerations, await f.ScalarAsync("SELECT COUNT(*) FROM LogicalMovementGenerations"));
        Assert.Equal(beforeOperations, await f.ScalarAsync("SELECT COUNT(*) FROM MovementCorrectionOperations"));
        Assert.Equal(beforeAudits, await f.ScalarAsync("SELECT COUNT(*) FROM AuditEvents"));
        Assert.Equal(beforeOutputs, await f.ScalarAsync("SELECT COUNT(*) FROM LogicalMovementPhysicalOutputs"));
        Assert.Equal(beforeLegacyLines, await f.ScalarAsync("SELECT COUNT(*) FROM MovementCorrectionLines"));
    }

    [Theory]
    [InlineData("correct")]
    [InlineData("reverse")]
    [InlineData("whole-batch")]
    public async Task Enabled_schema17_rejects_legacy_commands_without_appending_alpha8_artifacts(string command)
    {
        await using var f = await Task20Fixture.CreateAsync();
        // Offset physical batch and logical root identity deliberately.
        await f.Movements.SaveSingleAsync(f.Single(1));
        var root = await f.Database.CreateBatchAsync(7);
        Assert.NotEqual(root.RootId, root.BatchId);
        var movementId = await f.ScalarAsync($"SELECT Id FROM BinMovements WHERE MovementBatchId={root.BatchId}");
        var before = await f.CountsAsync();
        var state = await f.StateAsync();
        var error = await Record.ExceptionAsync(async () =>
        {
            if (command == "reverse")
                await f.Corrections.ReverseAsync(new(Guid.NewGuid(), movementId, "Task20 reversal"));
            else if (command == "correct")
                await f.Corrections.CorrectAsync(new(Guid.NewGuid(), movementId, Task20Fixture.Today,
                    f.CustomerId, 1, MovementType.Out, 9, null, null, "Task20 correction"));
            else
                await f.Corrections.CorrectBatchAsync(new(Guid.NewGuid(), root.BatchId,
                    Task20Fixture.Today, null, "Task20 whole root"));
        });
        // These requests cannot carry preview generation. They must fail closed
        // rather than invent one or use the physical ID as a logical root ID.
        Assert.Equal(before, await f.CountsAsync());
        Assert.Equal(state, await f.StateAsync());
        Assert.Equal(0, await f.ScalarAsync("SELECT COUNT(*) FROM MovementCorrectionLines"));
        var legacy = Assert.IsType<LogicalMovementMutationException>(error);
        Assert.Equal(LogicalMovementMutationFailure.LegacyRouteUnavailable, legacy.Failure);
        Task20FailureBoundary.AssertDomainFailure(error, ["legacy", "schema17", "schema 17", "logical", "generation"],
            ["disabled", "unavailable", "unsupported", "not supported", "required", "reject"]);
    }
}

public sealed class Task20ReversedDispositionWorkflowTests
{
    [Fact]
    public async Task Movement_history_distinguishes_reversal_restoration_and_correction_neutraliser_roles()
    {
        await using var f = await Task20Fixture.CreateAsync();
        var root = await f.Database.CreateBatchAsync(7);
        var initial = Assert.IsType<LogicalMovementMutationPreview>(
            await f.Corrections.PreviewLogicalForBatchAsync(root.BatchId));
        var lineId = Assert.Single(initial.Lines).LogicalMovementLineId;
        await f.Corrections.ExecuteLogicalAsync(new(
            Guid.NewGuid(), initial.LogicalMovementBatchId, initial.ExpectedGeneration,
            MovementMutationRequest.Reverse(MovementMutationScope.Individual,
                [lineId], "ordinary reversal")));
        var reversalId = (await f.Database.MovementIdsByRoleAsync(root.RootId))
            [LogicalMovementTransformationRole.OrdinaryReversal];

        var reversed = Assert.IsType<LogicalMovementMutationPreview>(
            await f.Corrections.PreviewLogicalAsync(new(root.RootId)));
        var restored = await f.Corrections.ResolveReversedLinesAsync(new(
            Guid.NewGuid(), reversed.LogicalMovementBatchId, reversed.ExpectedGeneration,
            [new(lineId, ReversedLineDisposition.Restore)], "restore mistaken reversal"));
        var restorationId = (await f.Database.MovementIdsByRoleAsync(root.RootId))
            [LogicalMovementTransformationRole.Restoration];

        await f.Corrections.ExecuteLogicalAsync(new(
            Guid.NewGuid(), restored.CurrentPreview.LogicalMovementBatchId,
            restored.CurrentPreview.ExpectedGeneration,
            MovementMutationRequest.Correct(MovementMutationScope.WholeRoot,
                [lineId], "correct restored line",
                direction: MovementFieldIntent<MovementType>.Selected(MovementType.In))));
        var correctionNeutraliserId = (await f.Database.MovementIdsByRoleAsync(root.RootId))
            [LogicalMovementTransformationRole.CorrectionNeutraliser];

        var history = await f.History.QueryAsync(new(
            new DateOnly(2026, 9, 1), Task20Fixture.Today,
            IncludeAdjustments: true));
        var reversal = Assert.Single(history.Rows, x => x.MovementId == reversalId);
        Assert.Equal("Reversal", reversal.SourceText);
        Assert.StartsWith($"Reversal of #{initial.Lines[0].LastEffective.MovementId}",
            reversal.Status);
        Assert.Equal($"Reversal — #{initial.Lines[0].LastEffective.MovementId}",
            reversal.PresentationStatus);

        var restoration = Assert.Single(history.Rows, x => x.MovementId == restorationId);
        Assert.Equal("Restoration", restoration.SourceText);
        Assert.StartsWith($"Restoration — #{reversalId}", restoration.Status);
        Assert.Equal($"Restoration — #{reversalId}", restoration.PresentationStatus);

        var correctionNeutraliser = Assert.Single(history.Rows,
            x => x.MovementId == correctionNeutraliserId);
        Assert.Equal("Correction", correctionNeutraliser.SourceText);
        Assert.StartsWith("Correction neutraliser", correctionNeutraliser.Status);
        Assert.StartsWith("Correction neutraliser", correctionNeutraliser.PresentationStatus);
    }

    [Fact]
    public async Task Reversed_preview_carries_authoritative_operator_display_and_forensic_identity()
    {
        await using var f = await Task20Fixture.CreateAsync();
        var root = await f.Database.CreateBatchAsync(7, 4);
        var lineId = new LogicalMovementLineId((await f.Database.LineIdsAsync(root.RootId))[1]);
        await f.Database.MutateAsync(root.RootId, 0,
            MovementMutationRequest.Reverse(MovementMutationScope.Individual,
                [lineId], "prepare display contract"));

        var preview = Assert.IsType<LogicalMovementMutationPreview>(
            await f.Corrections.PreviewLogicalAsync(new(root.RootId)));
        var line = Assert.Single(preview.Lines,
            x => x.State == LogicalMovementLineState.Reversed);

        Assert.Equal("PROJ-A", line.LastEffective.CustomerCode);
        Assert.Equal("Projection A", line.LastEffective.CustomerName);
        Assert.Equal("Small Bin", line.LastEffective.ContainerTypeName);
        Assert.Equal((new DateOnly(2026, 9, 1), MovementType.Out, 4),
            (line.LastEffective.MovementDate, line.LastEffective.Direction,
                line.LastEffective.Quantity));
        Assert.Equal(lineId, line.LogicalMovementLineId);
        Assert.Equal(root.RootId, preview.LogicalMovementBatchId.Value);
        Assert.True(line.RootMovementId > 0);
        Assert.True(line.LastEffective.MovementId > 0);
        Assert.True(line.TerminalReversalMovementId > 0);
        Assert.Equal(1, preview.ExpectedGeneration.Value);
    }

    [Fact]
    public async Task Disposition_identity_and_mapping_do_not_depend_on_display_labels()
    {
        await using var f = await Task20Fixture.CreateAsync();
        var root = await f.Database.CreateBatchAsync(7, 4);
        var lineId = new LogicalMovementLineId((await f.Database.LineIdsAsync(root.RootId))[1]);
        await f.Database.MutateAsync(root.RootId, 0,
            MovementMutationRequest.Reverse(MovementMutationScope.Individual,
                [lineId], "prepare display rename"));
        var preview = Assert.IsType<LogicalMovementMutationPreview>(
            await f.Corrections.PreviewLogicalAsync(new(root.RootId)));

        await f.ExecuteAsync($"""
            UPDATE Customers SET CustomerCode='RENAMED', Name='Renamed Customer'
            WHERE Id={f.CustomerId};
            UPDATE ContainerTypes SET Name='Renamed Container', NameKey='RENAMED CONTAINER'
            WHERE Id=2;
            """);
        var beforeDecision = await f.StateAsync();

        var result = await f.Corrections.ResolveReversedLinesAsync(new(
            Guid.NewGuid(), preview.LogicalMovementBatchId, preview.ExpectedGeneration,
            [new(lineId, ReversedLineDisposition.RemainReversed)], null));

        Assert.Equal(LogicalMovementMutationResultKind.NoChange, result.Kind);
        var currentLine = Assert.Single(result.CurrentPreview.Lines,
            x => x.LogicalMovementLineId == lineId);
        Assert.Equal("RENAMED", currentLine.LastEffective.CustomerCode);
        Assert.Equal("Renamed Customer", currentLine.LastEffective.CustomerName);
        Assert.Equal("Renamed Container", currentLine.LastEffective.ContainerTypeName);
        Assert.Equal(LogicalMovementLineState.Reversed, currentLine.State);
        Assert.Equal(beforeDecision, await f.StateAsync());
    }

    [Fact]
    public async Task One_reversed_line_restore_uses_existing_authority_and_returns_fresh_eligible_preview()
    {
        await using var f = await Task20Fixture.CreateAsync();
        var root = await f.Database.CreateBatchAsync(7, 4);
        var lines = (await f.Database.LineIdsAsync(root.RootId))
            .Select(x => new LogicalMovementLineId(x)).ToArray();
        await f.Database.MutateAsync(root.RootId, 0,
            MovementMutationRequest.Reverse(MovementMutationScope.Individual,
                [lines[1]], "prepare disposition"));
        var preview = Assert.IsType<LogicalMovementMutationPreview>(
            await f.Corrections.PreviewLogicalAsync(new(root.RootId)));
        var reversed = Assert.Single(preview.Lines,
            x => x.State == LogicalMovementLineState.Reversed);

        var result = await f.Corrections.ResolveReversedLinesAsync(new(
            Guid.NewGuid(), preview.LogicalMovementBatchId, preview.ExpectedGeneration,
            [new(reversed.LogicalMovementLineId, ReversedLineDisposition.Restore)],
            "the reversal was entered in error"));

        Assert.Equal(LogicalMovementMutationResultKind.Committed, result.Kind);
        Assert.NotNull(result.OperationId);
        Assert.Equal(2, result.CurrentPreview.ExpectedGeneration.Value);
        Assert.True(result.CurrentPreview.IsWholeRootCorrectionEligible);
        Assert.All(result.CurrentPreview.Lines,
            x => Assert.Equal(LogicalMovementLineState.Active, x.State));
        Assert.Equal(1, await f.ScalarAsync($"""
            SELECT COUNT(*) FROM LogicalMovementGenerationLines
            WHERE LogicalMovementBatchId={root.RootId}
              AND LogicalMovementGenerationId=(
                  SELECT Id FROM LogicalMovementGenerations
                  WHERE LogicalMovementBatchId={root.RootId} AND GenerationNumber=2)
              AND Action={(int)LogicalMovementGenerationAction.Restored};
            """));
    }

    [Fact]
    public async Task Remain_reversed_is_an_explicit_no_write_outcome()
    {
        await using var f = await Task20Fixture.CreateAsync();
        var root = await f.Database.CreateBatchAsync(7, 4);
        var line = new LogicalMovementLineId((await f.Database.LineIdsAsync(root.RootId))[1]);
        await f.Database.MutateAsync(root.RootId, 0,
            MovementMutationRequest.Reverse(MovementMutationScope.Individual,
                [line], "prepare remain reversed"));
        var preview = Assert.IsType<LogicalMovementMutationPreview>(
            await f.Corrections.PreviewLogicalAsync(new(root.RootId)));
        var before = await f.StateAsync();

        var result = await f.Corrections.ResolveReversedLinesAsync(new(
            Guid.NewGuid(), preview.LogicalMovementBatchId, preview.ExpectedGeneration,
            [new(line, ReversedLineDisposition.RemainReversed)], null));

        Assert.Equal(LogicalMovementMutationResultKind.NoChange, result.Kind);
        Assert.Null(result.OperationId);
        Assert.False(result.CurrentPreview.IsWholeRootCorrectionEligible);
        Assert.Equal(before, await f.StateAsync());
    }

    [Fact]
    public async Task Multiple_reversed_lines_restore_and_remain_are_one_complete_atomic_generation()
    {
        await using var f = await Task20Fixture.CreateAsync();
        var root = await f.Database.CreateBatchAsync(7, 4, 3);
        var lines = (await f.Database.LineIdsAsync(root.RootId))
            .Select(x => new LogicalMovementLineId(x)).ToArray();
        await f.Database.MutateAsync(root.RootId, 0,
            MovementMutationRequest.Reverse(MovementMutationScope.Individual,
                [lines[1], lines[2]], "prepare mixed dispositions"));
        var preview = Assert.IsType<LogicalMovementMutationPreview>(
            await f.Corrections.PreviewLogicalAsync(new(root.RootId)));

        var result = await f.Corrections.ResolveReversedLinesAsync(new(
            Guid.NewGuid(), preview.LogicalMovementBatchId, preview.ExpectedGeneration,
            [new(lines[1], ReversedLineDisposition.Restore),
                new(lines[2], ReversedLineDisposition.RemainReversed)],
            "restore only the erroneous reversal"));

        Assert.Equal(LogicalMovementMutationResultKind.Committed, result.Kind);
        Assert.Equal(2, result.CurrentPreview.ExpectedGeneration.Value);
        Assert.Equal(LogicalMovementLineState.Active,
            result.CurrentPreview.Lines.Single(x => x.LogicalMovementLineId == lines[1]).State);
        Assert.Equal(LogicalMovementLineState.Reversed,
            result.CurrentPreview.Lines.Single(x => x.LogicalMovementLineId == lines[2]).State);
        Assert.False(result.CurrentPreview.IsWholeRootCorrectionEligible);
        Assert.Equal(3, await f.ScalarAsync($"""
            SELECT COUNT(*) FROM LogicalMovementGenerationLines
            WHERE LogicalMovementBatchId={root.RootId}
              AND LogicalMovementGenerationId=(
                  SELECT Id FROM LogicalMovementGenerations
                  WHERE LogicalMovementBatchId={root.RootId} AND GenerationNumber=2);
            """));
        Assert.Equal(1, await f.ScalarAsync($"""
            SELECT COUNT(*) FROM LogicalMovementGenerationLines
            WHERE LogicalMovementBatchId={root.RootId}
              AND LogicalMovementGenerationId=(
                  SELECT Id FROM LogicalMovementGenerations
                  WHERE LogicalMovementBatchId={root.RootId} AND GenerationNumber=2)
              AND Action={(int)LogicalMovementGenerationAction.Restored};
            """));
    }

    [Fact]
    public async Task Stale_disposition_generation_is_controlled_and_writes_nothing()
    {
        await using var f = await Task20Fixture.CreateAsync();
        var root = await f.Database.CreateBatchAsync(7, 4);
        var lines = (await f.Database.LineIdsAsync(root.RootId))
            .Select(x => new LogicalMovementLineId(x)).ToArray();
        await f.Database.MutateAsync(root.RootId, 0,
            MovementMutationRequest.Reverse(MovementMutationScope.Individual,
                [lines[1]], "prepare stale disposition"));
        var stale = Assert.IsType<LogicalMovementMutationPreview>(
            await f.Corrections.PreviewLogicalAsync(new(root.RootId)));
        await f.Database.MutateAsync(root.RootId, stale.ExpectedGeneration.Value,
            MovementMutationRequest.Correct(MovementMutationScope.Individual,
                [lines[0]], "advance root", quantity: MovementFieldIntent<int>.Selected(8)));
        var before = await f.StateAsync();

        var error = await Assert.ThrowsAsync<LogicalMovementMutationException>(() =>
            f.Corrections.ResolveReversedLinesAsync(new(
                Guid.NewGuid(), stale.LogicalMovementBatchId, stale.ExpectedGeneration,
                [new(lines[1], ReversedLineDisposition.Restore)], "stale restore")));

        Assert.Equal(LogicalMovementMutationFailure.StaleGeneration, error.Failure);
        Assert.Equal(before, await f.StateAsync());
    }

    [Fact]
    public async Task Disposition_retry_replays_exact_restore_and_changed_reuse_conflicts()
    {
        await using var f = await Task20Fixture.CreateAsync();
        var root = await f.Database.CreateSingleAsync(Task20Fixture.Today, f.CustomerId, 1, 7);
        var line = new LogicalMovementLineId((await f.Database.LineIdsAsync(root.RootId)).Single());
        await f.Database.MutateAsync(root.RootId, 0,
            MovementMutationRequest.Reverse(MovementMutationScope.Individual,
                [line], "prepare retry"));
        var preview = Assert.IsType<LogicalMovementMutationPreview>(
            await f.Corrections.PreviewLogicalAsync(new(root.RootId)));
        var operationId = Guid.NewGuid();
        var command = new LogicalMovementReversedDispositionCommand(
            operationId, preview.LogicalMovementBatchId, preview.ExpectedGeneration,
            [new(line, ReversedLineDisposition.Restore)], "restore retry");

        var committed = await f.Corrections.ResolveReversedLinesAsync(command);
        var committedState = await f.StateAsync();
        var replay = await f.Corrections.ResolveReversedLinesAsync(command);

        Assert.Equal(LogicalMovementMutationResultKind.Committed, committed.Kind);
        Assert.Equal(LogicalMovementMutationResultKind.Replayed, replay.Kind);
        Assert.Equal(committed.OperationId, replay.OperationId);
        Assert.Equal(committedState, await f.StateAsync());
        var conflict = await Assert.ThrowsAsync<LogicalMovementMutationException>(() =>
            f.Corrections.ResolveReversedLinesAsync(command with
            {
                RestorationReason = "changed restoration reason"
            }));
        Assert.Equal(LogicalMovementMutationFailure.OperationIdConflict, conflict.Failure);
        Assert.Equal(committedState, await f.StateAsync());
    }

    [Fact]
    public async Task Viewer_and_incomplete_decisions_leave_no_disposition_artifacts()
    {
        await using var viewer = await Task20Fixture.CreateAsync(
            role: UserRole.Viewer, nativeActorRole: UserRole.Operator);
        var root = await viewer.Database.CreateBatchAsync(7, 4);
        var line = new LogicalMovementLineId((await viewer.Database.LineIdsAsync(root.RootId))[1]);
        await viewer.Database.MutateAsync(root.RootId, 0,
            MovementMutationRequest.Reverse(MovementMutationScope.Individual,
                [line], "prepare viewer"));
        var preview = Assert.IsType<LogicalMovementMutationPreview>(
            await viewer.Corrections.PreviewLogicalAsync(new(root.RootId)));
        var beforeViewer = await viewer.StateAsync();

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            viewer.Corrections.ResolveReversedLinesAsync(new(
                Guid.NewGuid(), preview.LogicalMovementBatchId, preview.ExpectedGeneration,
                [new(line, ReversedLineDisposition.Restore)], "viewer denied")));
        Assert.Equal(beforeViewer, await viewer.StateAsync());

        await using var operatorFixture = await Task20Fixture.CreateAsync();
        var operatorRoot = await operatorFixture.Database.CreateBatchAsync(7, 4);
        var operatorLine = new LogicalMovementLineId(
            (await operatorFixture.Database.LineIdsAsync(operatorRoot.RootId))[1]);
        await operatorFixture.Database.MutateAsync(operatorRoot.RootId, 0,
            MovementMutationRequest.Reverse(MovementMutationScope.Individual,
                [operatorLine], "prepare cancel"));
        var operatorPreview = Assert.IsType<LogicalMovementMutationPreview>(
            await operatorFixture.Corrections.PreviewLogicalAsync(new(operatorRoot.RootId)));
        var beforeIncomplete = await operatorFixture.StateAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            operatorFixture.Corrections.ResolveReversedLinesAsync(new(
                Guid.NewGuid(), operatorPreview.LogicalMovementBatchId,
                operatorPreview.ExpectedGeneration, [], null)));
        Assert.Equal(beforeIncomplete, await operatorFixture.StateAsync());
    }
}

public sealed class Task20LogicalMutationSafetyTests
{
    [Fact]
    public async Task Captured_generation_loses_after_another_command_advances_the_root()
    {
        await using var f = await Task20Fixture.CreateAsync();
        var root = await f.Database.CreateBatchAsync(7, 4);
        var ids = (await f.Database.LineIdsAsync(root.RootId)).Select(x => new LogicalMovementLineId(x)).ToArray();
        var stale = new LogicalMovementMutationCommand(Guid.NewGuid(), new(root.RootId), new(0),
            MovementMutationRequest.Correct(MovementMutationScope.WholeRoot, ids, "captured generation",
                movementDate: MovementFieldIntent<DateOnly>.Selected(Task20Fixture.Today)));
        await f.Database.MutateAsync(root.RootId, 0, MovementMutationRequest.Correct(
            MovementMutationScope.Individual, [ids[0]], "other committed command",
            quantity: MovementFieldIntent<int>.Selected(8)));
        var before = await f.CountsAsync();
        var error = await Assert.ThrowsAsync<LogicalMovementMutationException>(() => f.Corrections.ExecuteLogicalAsync(stale));
        Assert.Equal(LogicalMovementMutationFailure.StaleGeneration, error.Failure);
        Assert.Equal(before, await f.CountsAsync());
    }

    [Fact]
    public async Task All_active_whole_root_date_correction_is_representable_by_existing_logical_intent()
    {
        await using var f = await Task20Fixture.CreateAsync();
        var root = await f.Database.CreateBatchAsync(7, 4);
        var ids = (await f.Database.LineIdsAsync(root.RootId)).Select(x => new LogicalMovementLineId(x)).ToArray();
        var result = await f.Database.MutateAsync(root.RootId, 0,
            MovementMutationRequest.Correct(MovementMutationScope.WholeRoot, ids, "whole date correction",
                movementDate: MovementFieldIntent<DateOnly>.Selected(Task20Fixture.Today),
                direction: MovementFieldIntent<MovementType>.Selected(MovementType.In)));
        Assert.Equal(LogicalMovementMutationResultKind.Committed, result.Kind);
        Assert.Equal(1, result.ResultGeneration.Value);
        Assert.NotNull(result.PhysicalOutputBatchId);
        Assert.Equal(0, await f.ScalarAsync("SELECT COUNT(*) FROM MovementCorrectionLines"));
    }

    [Fact]
    public async Task Whole_root_with_reversed_line_is_rejected_without_implicit_restore()
    {
        await using var f = await Task20Fixture.CreateAsync();
        var root = await f.Database.CreateBatchAsync(7, 4);
        var ids = (await f.Database.LineIdsAsync(root.RootId)).Select(x => new LogicalMovementLineId(x)).ToArray();
        await f.Database.MutateAsync(root.RootId, 0,
            MovementMutationRequest.Reverse(MovementMutationScope.Individual, [ids[1]], "reverse second"));
        var before = await f.CountsAsync();
        var error = await Record.ExceptionAsync(() => f.Database.MutateAsync(root.RootId, 1,
            MovementMutationRequest.Correct(MovementMutationScope.WholeRoot, ids, "missing decisions",
                movementDate: MovementFieldIntent<DateOnly>.Selected(Task20Fixture.Today))));
        var unavailable = Assert.IsType<LogicalMovementMutationException>(error);
        Assert.Equal(LogicalMovementMutationFailure.WholeRootCorrectionUnavailable,
            unavailable.Failure);
        Assert.Equal(before, await f.CountsAsync());
        Assert.Equal(1, await f.ScalarAsync($"SELECT CurrentGenerationNumber FROM LogicalMovementBatches WHERE Id={root.RootId}"));
    }
}

public sealed class Task20AuditActivationTests
{
    [Theory]
    [InlineData("cross-line-predecessor")]
    [InlineData("cross-line-ledger-introduction")]
    public async Task Native_detail_and_review_reject_cross_line_lineage_evidence(string damage)
    {
        await using var f = await Task20Fixture.CreateAsync(role: UserRole.Administrator,
            nativeActorRole: UserRole.Operator);
        var root = await f.Database.CreateBatchAsync(7, 4);
        var lines = (await f.Database.LineIdsAsync(root.RootId)).ToArray();
        Assert.Equal(2, lines.Length);
        var result = await f.Database.MutateAsync(root.RootId, 0,
            MovementMutationRequest.Correct(MovementMutationScope.WholeRoot,
                lines.Select(x => new LogicalMovementLineId(x)), "cross-line audit evidence",
                quantity: MovementFieldIntent<int>.Selected(8)));
        var operationId = Assert.IsType<long>(result.OperationId);
        var auditId = await f.ScalarAsync(
            $"SELECT Id FROM AuditEvents WHERE MovementCorrectionOperationId={operationId}");
        var generationId = await f.ScalarAsync(
            $"SELECT Id FROM LogicalMovementGenerations WHERE MovementCorrectionOperationId={operationId}");
        var firstGenerationLineId = await f.ScalarAsync($"""
            SELECT Id FROM LogicalMovementGenerationLines
            WHERE LogicalMovementGenerationId={generationId} AND LogicalMovementLineId={lines[0]};
            """);
        var secondGenerationLineId = await f.ScalarAsync($"""
            SELECT Id FROM LogicalMovementGenerationLines
            WHERE LogicalMovementGenerationId={generationId} AND LogicalMovementLineId={lines[1]};
            """);

        if (damage == "cross-line-predecessor")
        {
            // The FK remains valid: it is a baseline row in this root, but it
            // belongs to the other permanent logical line.
            await f.ExecuteAsync($"""
                UPDATE LogicalMovementGenerationLines
                SET PreviousGenerationLineId=(
                    SELECT Id FROM LogicalMovementGenerationLines
                    WHERE LogicalMovementBatchId={root.RootId}
                      AND LogicalMovementGenerationId=(
                          SELECT Id FROM LogicalMovementGenerations
                          WHERE LogicalMovementBatchId={root.RootId} AND GenerationNumber=0)
                      AND LogicalMovementLineId={lines[1]})
                WHERE Id={firstGenerationLineId};
                """);
        }
        else
        {
            var movementId = await f.ScalarAsync($"""
                SELECT BinMovementId FROM LogicalMovementLedgerLinks
                WHERE LogicalMovementBatchId={root.RootId}
                  AND LogicalMovementLineId={lines[0]}
                  AND IntroducedByGenerationLineId={firstGenerationLineId}
                  AND Role={(int)LogicalMovementTransformationRole.CorrectionNeutraliser};
                """);
            // The composite FK remains valid because both generation lines are
            // in this root. Only the permanent-line attribution is corrupt.
            await f.ExecuteAsync($"""
                UPDATE LogicalMovementLedgerLinks
                SET IntroducedByGenerationLineId={secondGenerationLineId}
                WHERE BinMovementId={movementId};
                """);
        }

        var before = await f.CountsAsync();
        var state = await f.StateAsync();
        MovementChangeAuditDetail? detail = null;
        var detailError = await Record.ExceptionAsync(async () =>
            detail = await f.Audit.GetMovementChangeDetailAsync(auditId));
        Assert.Equal(before, await f.CountsAsync());
        Assert.Equal(state, await f.StateAsync());
        if (detailError is not null)
            Task20FailureBoundary.AssertDomainFailure(detailError, ["native", "audit", "lineage"],
                ["invalid", "health", "integrity"]);
        var detailFailedClosed = detailError is not null || detail is null;

        var reviewError = await Record.ExceptionAsync(() =>
            f.Audit.MarkMovementChangesReviewedAsync([auditId]));
        if (reviewError is not null)
            Task20FailureBoundary.AssertDomainFailure(reviewError, ["native", "audit", "lineage"],
                ["invalid", "health", "integrity"]);
        var reviewedCount = await f.ScalarAsync(
            $"SELECT COUNT(*) FROM AuditEvents WHERE Id={auditId} AND ReviewedUtc IS NOT NULL");
        var acknowledgementCount = await f.ScalarAsync(
            $"SELECT COUNT(*) FROM AuditEvents WHERE Action='MOVEMENT_CHANGE_REVIEWED' AND EntityId='{auditId}'");
        var afterReview = await f.CountsAsync();
        var stateAfterReview = await f.StateAsync();
        var reviewFailedClosed = reviewError is not null && before.SequenceEqual(afterReview) &&
            state == stateAfterReview && reviewedCount == 0 && acknowledgementCount == 0;
        Assert.True(detailFailedClosed && reviewFailedClosed,
            "Cross-line lineage evidence must fail closed for both detail and review without persisted acknowledgement.");
    }

    [Fact]
    public async Task Legitimate_migrated_alpha8_operation_with_root_association_remains_legacy_detail_and_review()
    {
        await using var f = await Task20Fixture.CreateAsync(
            role: UserRole.Administrator, legacyCorrection: true);
        var auditId = await f.ScalarAsync("""
            SELECT a.Id
            FROM AuditEvents a
            JOIN MovementCorrectionOperations o ON o.Id=a.MovementCorrectionOperationId
            WHERE a.Action='MOVEMENT_CORRECTED'
              AND o.LogicalMovementBatchId IS NOT NULL
              AND o.RequestJson IS NULL
              AND o.RequestSchemaVersion IS NULL
              AND o.ExpectedGenerationNumber IS NULL
              AND o.ResultGenerationNumber IS NULL
              AND NOT EXISTS (
                  SELECT 1 FROM LogicalMovementGenerations g
                  WHERE g.MovementCorrectionOperationId=o.Id);
            """);

        var detail = Assert.IsType<MovementChangeAuditDetail>(
            await f.Audit.GetMovementChangeDetailAsync(auditId));
        Assert.Equal(3, detail.Lines.Count);

        await f.Audit.MarkMovementChangesReviewedAsync([auditId]);
        Assert.Equal(1, await f.ScalarAsync(
            $"SELECT COUNT(*) FROM AuditEvents WHERE Id={auditId} AND ReviewedUtc IS NOT NULL"));
        Assert.Equal(1, await f.ScalarAsync(
            $"SELECT COUNT(*) FROM AuditEvents WHERE Action='MOVEMENT_CHANGE_REVIEWED' AND EntityId='{auditId}'"));
    }

    [Fact]
    public async Task Contradictory_native_generation_envelope_never_downgrades_to_alpha8_detail()
    {
        await using var f = await Task20Fixture.CreateAsync(role: UserRole.Administrator);
        var root = await f.Database.CreateSingleAsync(Task20Fixture.Today, f.CustomerId, 1, 7);
        var line = Assert.Single(await f.Database.LineIdsAsync(root.RootId));
        var result = await f.Database.MutateAsync(root.RootId, 0,
            MovementMutationRequest.Correct(MovementMutationScope.Individual, [new(line)], "native correction",
                quantity: MovementFieldIntent<int>.Selected(8)));
        var roles = await f.Database.MovementIdsByRoleAsync(root.RootId);
        var neutral = roles[LogicalMovementTransformationRole.CorrectionNeutraliser];
        var replacement = roles[LogicalMovementTransformationRole.CorrectionReplacement];
        var operationId = Assert.IsType<long>(result.OperationId);
        var auditId = await f.ScalarAsync(
            $"SELECT Id FROM AuditEvents WHERE MovementCorrectionOperationId={operationId}");
        await using (var db = f.Database.CreateDbContext())
        {
            db.MovementCorrectionLines.Add(new MovementCorrectionLine
            {
                CorrectionOperationId = operationId,
                OriginalMovementId = root.MovementId,
                NeutralisingMovementId = neutral,
                ReplacementMovementId = replacement
            });
            var audit = await db.AuditEvents.SingleAsync(x => x.Id == auditId);
            audit.EntityType = "BinMovement";
            audit.EntityId = root.MovementId.ToString(CultureInfo.InvariantCulture);
            audit.AfterValues = JsonSerializer.Serialize(new[] { new
            {
                Id = root.MovementId,
                NeutralisingMovementId = neutral,
                ReplacementMovementId = replacement
            } });
            await db.SaveChangesAsync();
        }
        await f.ExecuteAsync(
            $"UPDATE MovementCorrectionOperations SET RequestSchemaVersion=NULL WHERE Id={operationId}");

        Assert.Equal(root.RootId, await f.ScalarAsync(
            $"SELECT LogicalMovementBatchId FROM MovementCorrectionOperations WHERE Id={operationId}"));
        Assert.Equal(0, await f.ScalarAsync(
            $"SELECT ExpectedGenerationNumber FROM MovementCorrectionOperations WHERE Id={operationId}"));
        Assert.Equal(1, await f.ScalarAsync(
            $"SELECT ResultGenerationNumber FROM MovementCorrectionOperations WHERE Id={operationId}"));
        Assert.Equal(operationId, await f.ScalarAsync(
            $"SELECT MovementCorrectionOperationId FROM AuditEvents WHERE Id={auditId}"));

        var state = await f.StateAsync();
        MovementChangeAuditDetail? detail = null;
        var error = await Record.ExceptionAsync(async () =>
            detail = await f.Audit.GetMovementChangeDetailAsync(auditId));

        Assert.Equal(state, await f.StateAsync());
        if (error is not null)
            Task20FailureBoundary.AssertDomainFailure(error, ["native", "audit", "lineage"],
                ["invalid", "integrity", "health"]);
        Assert.Null(detail);
    }

    [Fact]
    public async Task Contradictory_native_generation_envelope_blocks_review_without_writing()
    {
        await using var f = await Task20Fixture.CreateAsync(role: UserRole.Administrator,
            nativeActorRole: UserRole.Operator);
        var root = await f.Database.CreateSingleAsync(Task20Fixture.Today, f.CustomerId, 1, 7);
        var line = Assert.Single(await f.Database.LineIdsAsync(root.RootId));
        var result = await f.Database.MutateAsync(root.RootId, 0,
            MovementMutationRequest.Reverse(MovementMutationScope.Individual, [new(line)], "native review"));
        var operationId = Assert.IsType<long>(result.OperationId);
        var auditId = await f.ScalarAsync(
            $"SELECT Id FROM AuditEvents WHERE MovementCorrectionOperationId={operationId}");
        await f.ExecuteAsync(
            $"UPDATE MovementCorrectionOperations SET RequestSchemaVersion=NULL WHERE Id={operationId}");
        await f.ExecuteAsync(
            $"UPDATE AuditEvents SET EntityType='BinMovement', EntityId='{root.MovementId}' WHERE Id={auditId}");

        Assert.Equal(root.RootId, await f.ScalarAsync(
            $"SELECT LogicalMovementBatchId FROM MovementCorrectionOperations WHERE Id={operationId}"));
        Assert.Equal(0, await f.ScalarAsync(
            $"SELECT ExpectedGenerationNumber FROM MovementCorrectionOperations WHERE Id={operationId}"));
        Assert.Equal(1, await f.ScalarAsync(
            $"SELECT ResultGenerationNumber FROM MovementCorrectionOperations WHERE Id={operationId}"));
        Assert.Equal(operationId, await f.ScalarAsync(
            $"SELECT MovementCorrectionOperationId FROM AuditEvents WHERE Id={auditId}"));

        var counts = await f.CountsAsync();
        var state = await f.StateAsync();
        var error = await Record.ExceptionAsync(() =>
            f.Audit.MarkMovementChangesReviewedAsync([auditId]));

        Assert.Equal(counts, await f.CountsAsync());
        Assert.Equal(state, await f.StateAsync());
        Assert.Equal(0, await f.ScalarAsync(
            $"SELECT COUNT(*) FROM AuditEvents WHERE Id={auditId} AND ReviewedUtc IS NOT NULL"));
        Task20FailureBoundary.AssertDomainFailure(error, ["audit", "operation", "association"],
            ["invalid", "health", "integrity"]);
    }

    [Fact]
    public async Task Native_operation_association_prevents_alpha8_detail_parsing_even_when_payload_matches_legacy_shape()
    {
        await using var f = await Task20Fixture.CreateAsync(role: UserRole.Administrator);
        var root = await f.Database.CreateSingleAsync(Task20Fixture.Today, f.CustomerId, 1, 7);
        var line = Assert.Single(await f.Database.LineIdsAsync(root.RootId));
        var result = await f.Database.MutateAsync(root.RootId, 0,
            MovementMutationRequest.Correct(MovementMutationScope.Individual, [new(line)], "native correction",
                quantity: MovementFieldIntent<int>.Selected(8)));
        var roles = await f.Database.MovementIdsByRoleAsync(root.RootId);
        var neutral = roles[LogicalMovementTransformationRole.CorrectionNeutraliser];
        var replacement = roles[LogicalMovementTransformationRole.CorrectionReplacement];
        var auditId = await f.ScalarAsync($"SELECT Id FROM AuditEvents WHERE MovementCorrectionOperationId={result.OperationId}");
        await using (var db = f.Database.CreateDbContext())
        {
            db.MovementCorrectionLines.Add(new MovementCorrectionLine
            {
                CorrectionOperationId = Assert.IsType<long>(result.OperationId),
                OriginalMovementId = root.MovementId, NeutralisingMovementId = neutral,
                ReplacementMovementId = replacement
            });
            var audit = await db.AuditEvents.SingleAsync(x => x.Id == auditId);
            audit.AfterValues = JsonSerializer.Serialize(new[] { new
            {
                Id = root.MovementId, NeutralisingMovementId = neutral, ReplacementMovementId = replacement
            } });
            await db.SaveChangesAsync();
        }
        // Adversarial evidence deliberately resembles alpha8. Its authoritative
        // native association still forbids routing it through the legacy parser.
        var state = await f.StateAsync();
        var detail = Assert.IsType<MovementChangeAuditDetail>(
            await f.Audit.GetMovementChangeDetailAsync(auditId));
        Assert.Equal(state, await f.StateAsync());
        var native = Assert.IsType<NativeMovementChangeAuditEvidence>(detail.NativeEvidence);
        Assert.Equal(root.RootId, native.LogicalRootId);
        Assert.Equal(result.OperationId, native.OperationId);
        Assert.Equal(0, native.ExpectedGenerationNumber);
        Assert.Equal(1, native.ResultGenerationNumber);
        var nativeLine = Assert.Single(native.Lines);
        Assert.Contains(nativeLine.Evidence, x => x.Role == "Correction neutraliser" && x.MovementId == neutral);
        Assert.Contains(nativeLine.Evidence, x => x.Role == "Correction replacement" && x.MovementId == replacement);
    }

    [Fact]
    public async Task Native_reversal_root_id_collision_never_returns_another_roots_physical_evidence()
    {
        await using var f = await Task20Fixture.CreateAsync(role: UserRole.Administrator);
        var roots = new List<(long RootId, long MovementId)>();
        for (var i = 0; i < 3; i++)
        {
            var root = await f.Database.CreateSingleAsync(Task20Fixture.Today, f.CustomerId, 1, 7 + i);
            roots.Add(root);
            var line = Assert.Single(await f.Database.LineIdsAsync(root.RootId));
            await f.Database.MutateAsync(root.RootId, 0, MovementMutationRequest.Reverse(
                MovementMutationScope.Individual, [new(line)], "native reversal"));
        }
        var target = roots[2];
        Assert.Equal(roots[1].MovementId, target.RootId);
        var auditId = await f.ScalarAsync($"SELECT Id FROM AuditEvents WHERE Action='MOVEMENT_REVERSED' AND EntityType='LogicalMovementBatch' AND EntityId='{target.RootId}'");
        Assert.True(auditId > 0);
        var state = await f.StateAsync();
        MovementChangeAuditDetail? detail = null;
        var error = await Record.ExceptionAsync(async () => detail = await f.Audit.GetMovementChangeDetailAsync(auditId));
        Assert.Equal(state, await f.StateAsync());
        if (error is not null)
            Task20FailureBoundary.AssertDomainFailure(error, ["native", "audit", "lineage"],
                ["unsupported", "not supported", "invalid", "integrity", "health"]);
        // Null is the existing service's supported no-detail outcome. Complete
        // native UI is later work; a supported result must resolve the right root.
        Assert.True(detail is null || detail.Lines.Any(x => x.MovementId == target.MovementId),
            "Native audit returned unrelated physical evidence selected by a colliding numeric root ID.");
        if (detail is not null) Assert.DoesNotContain(detail.Lines, x => x.MovementId == roots[1].MovementId);
    }

    [Theory]
    [InlineData("missing-association")]
    [InlineData("corrupt-operation")]
    public async Task Native_review_rejects_unhealthy_operation_audit_evidence_without_writing_review(string damage)
    {
        await using var f = await Task20Fixture.CreateAsync(role: UserRole.Administrator,
            nativeActorRole: UserRole.Operator);
        var root = await f.Database.CreateSingleAsync(Task20Fixture.Today, f.CustomerId, 1, 7);
        var line = Assert.Single(await f.Database.LineIdsAsync(root.RootId));
        var result = await f.Database.MutateAsync(root.RootId, 0,
            MovementMutationRequest.Reverse(MovementMutationScope.Individual, [new(line)], "native review"));
        var auditId = await f.ScalarAsync($"SELECT Id FROM AuditEvents WHERE MovementCorrectionOperationId={result.OperationId}");
        Assert.Equal(1, await f.ScalarAsync($"SELECT RequiresAdministratorReview FROM AuditEvents WHERE Id={auditId}"));
        if (damage == "missing-association")
            await f.ExecuteAsync($"UPDATE AuditEvents SET MovementCorrectionOperationId=NULL WHERE Id={auditId}");
        else
            await f.ExecuteAsync($"UPDATE MovementCorrectionOperations SET ResultGenerationNumber=99 WHERE Id={result.OperationId}");
        var before = await f.CountsAsync();
        var state = await f.StateAsync();
        var error = await Record.ExceptionAsync(() => f.Audit.MarkMovementChangesReviewedAsync([auditId]));
        Assert.Equal(before, await f.CountsAsync());
        Assert.Equal(state, await f.StateAsync());
        Assert.Equal(0, await f.ScalarAsync($"SELECT COUNT(*) FROM AuditEvents WHERE Id={auditId} AND ReviewedUtc IS NOT NULL"));
        Task20FailureBoundary.AssertDomainFailure(error, ["audit", "operation", "association"],
            ["invalid", "missing", "health", "integrity"]);
    }
}

public sealed class Task20AuditCharacterizationTests
{
    [Fact]
    public async Task Healthy_native_operator_change_can_be_acknowledged_by_administrator()
    {
        await using var f = await Task20Fixture.CreateAsync(role: UserRole.Administrator,
            nativeActorRole: UserRole.Operator);
        var root = await f.Database.CreateSingleAsync(Task20Fixture.Today, f.CustomerId, 1, 7);
        var line = Assert.Single(await f.Database.LineIdsAsync(root.RootId));
        var result = await f.Database.MutateAsync(root.RootId, 0,
            MovementMutationRequest.Reverse(MovementMutationScope.Individual, [new(line)], "healthy review"));
        var auditId = await f.ScalarAsync($"SELECT Id FROM AuditEvents WHERE MovementCorrectionOperationId={result.OperationId}");
        Assert.Equal(1, await f.ScalarAsync($"SELECT RequiresAdministratorReview FROM AuditEvents WHERE Id={auditId}"));
        var detail = Assert.IsType<MovementChangeAuditDetail>(
            await f.Audit.GetMovementChangeDetailAsync(auditId));
        var native = Assert.IsType<NativeMovementChangeAuditEvidence>(detail.NativeEvidence);
        Assert.Equal(MovementCorrectionKind.Reverse, native.OperationKind);
        var nativeLine = Assert.Single(native.Lines);
        Assert.Equal(LogicalMovementLineState.Active, nativeLine.PriorState);
        Assert.Equal(LogicalMovementLineState.Reversed, nativeLine.ResultingState);
        Assert.Equal(LogicalMovementGenerationAction.Reversed, nativeLine.Action);
        Assert.Contains(nativeLine.Evidence, x => x.Role == "Ordinary reversal");
        await f.Audit.MarkMovementChangesReviewedAsync([auditId]);
        Assert.Equal(1, await f.ScalarAsync($"SELECT COUNT(*) FROM AuditEvents WHERE Id={auditId} AND ReviewedUtc IS NOT NULL"));
        Assert.Equal(1, await f.ScalarAsync("SELECT COUNT(*) FROM AuditEvents WHERE Action='MOVEMENT_CHANGE_REVIEWED'"));
    }

    [Fact]
    public async Task Healthy_native_correction_returns_authoritative_detail()
    {
        await using var f = await Task20Fixture.CreateAsync(role: UserRole.Administrator);
        var root = await f.Database.CreateSingleAsync(Task20Fixture.Today, f.CustomerId, 1, 7);
        var line = Assert.Single(await f.Database.LineIdsAsync(root.RootId));
        var result = await f.Database.MutateAsync(root.RootId, 0,
            MovementMutationRequest.Correct(MovementMutationScope.Individual, [new(line)], "native correction",
                quantity: MovementFieldIntent<int>.Selected(8)));
        var auditId = await f.ScalarAsync($"SELECT Id FROM AuditEvents WHERE MovementCorrectionOperationId={result.OperationId}");
        var detail = Assert.IsType<MovementChangeAuditDetail>(
            await f.Audit.GetMovementChangeDetailAsync(auditId));
        var native = Assert.IsType<NativeMovementChangeAuditEvidence>(detail.NativeEvidence);
        Assert.Equal(root.RootId, native.LogicalRootId);
        Assert.Equal(result.OperationId, native.OperationId);
        Assert.Equal(0, native.ExpectedGenerationNumber);
        Assert.Equal(1, native.ResultGenerationNumber);
        Assert.Equal(MovementCorrectionKind.Single, native.OperationKind);
        var lineEvidence = Assert.Single(native.Lines);
        Assert.Equal(line, lineEvidence.LogicalLineId);
        Assert.Equal(LogicalMovementLineState.Active, lineEvidence.PriorState);
        Assert.Equal(LogicalMovementLineState.Active, lineEvidence.ResultingState);
        Assert.Equal(LogicalMovementGenerationAction.Corrected, lineEvidence.Action);
        Assert.Equal(MovementChangeField.Quantity, lineEvidence.AppliedFieldMask);
        Assert.Contains(lineEvidence.Evidence, x => x.Role == "Correction neutraliser");
        Assert.Contains(lineEvidence.Evidence, x => x.Role == "Correction replacement" && x.Quantity == 8);
    }

    [Fact]
    public async Task Native_detail_keeps_generation_chronology_and_selected_line_evidence()
    {
        await using var f = await Task20Fixture.CreateAsync(role: UserRole.Administrator);
        var root = await f.Database.CreateSingleAsync(Task20Fixture.Today, f.CustomerId, 1, 7);
        var line = Assert.Single(await f.Database.LineIdsAsync(root.RootId));
        await f.Database.MutateAsync(root.RootId, 0,
            MovementMutationRequest.Correct(MovementMutationScope.Individual, [new(line)], "first correction",
                quantity: MovementFieldIntent<int>.Selected(8)));
        var second = await f.Database.MutateAsync(root.RootId, 1,
            MovementMutationRequest.Correct(MovementMutationScope.Individual, [new(line)], "second correction",
                quantity: MovementFieldIntent<int>.Selected(9)));
        var auditId = await f.ScalarAsync(
            $"SELECT Id FROM AuditEvents WHERE MovementCorrectionOperationId={second.OperationId}");

        var detail = Assert.IsType<MovementChangeAuditDetail>(
            await f.Audit.GetMovementChangeDetailAsync(auditId));
        var native = Assert.IsType<NativeMovementChangeAuditEvidence>(detail.NativeEvidence);
        Assert.Equal(root.RootId, native.LogicalRootId);
        Assert.Equal(1, native.ExpectedGenerationNumber);
        Assert.Equal(2, native.ResultGenerationNumber);
        var nativeLine = Assert.Single(native.Lines);
        Assert.True(nativeLine.PreviousGenerationLineId > 0);
        Assert.True(nativeLine.GenerationLineId > 0);
        Assert.Contains(nativeLine.Evidence, x => x.Role == "Prior effective" && x.Quantity == 8);
        Assert.Contains(nativeLine.Evidence, x => x.Role == "Correction replacement" && x.Quantity == 9);
        Assert.Contains("8", NativeMovementChangeComparison.Describe(native.Lines));
        Assert.Contains("9", NativeMovementChangeComparison.Describe(native.Lines));
    }
}
