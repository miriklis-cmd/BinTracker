using BinTracker.Core;
using BinTracker.Services;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace BinTracker.IntegrationTests;

public sealed class Task20MutationActivationTests
{
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
        MovementChangeAuditDetail? detail = null;
        var error = await Record.ExceptionAsync(async () => detail = await f.Audit.GetMovementChangeDetailAsync(auditId));
        Assert.Equal(state, await f.StateAsync());
        if (error is not null)
            Task20FailureBoundary.AssertDomainFailure(error, ["native", "audit", "lineage"],
                ["unsupported", "not supported", "invalid", "integrity", "health"]);
        Assert.Null(detail); // No legacy detail from corrupt native evidence.
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
        await f.Audit.MarkMovementChangesReviewedAsync([auditId]);
        Assert.Equal(1, await f.ScalarAsync($"SELECT COUNT(*) FROM AuditEvents WHERE Id={auditId} AND ReviewedUtc IS NOT NULL"));
        Assert.Equal(1, await f.ScalarAsync("SELECT COUNT(*) FROM AuditEvents WHERE Action='MOVEMENT_CHANGE_REVIEWED'"));
    }

    [Fact]
    public async Task Native_correction_payload_currently_returns_no_legacy_detail()
    {
        await using var f = await Task20Fixture.CreateAsync(role: UserRole.Administrator);
        var root = await f.Database.CreateSingleAsync(Task20Fixture.Today, f.CustomerId, 1, 7);
        var line = Assert.Single(await f.Database.LineIdsAsync(root.RootId));
        var result = await f.Database.MutateAsync(root.RootId, 0,
            MovementMutationRequest.Correct(MovementMutationScope.Individual, [new(line)], "native correction",
                quantity: MovementFieldIntent<int>.Selected(8)));
        var auditId = await f.ScalarAsync($"SELECT Id FROM AuditEvents WHERE MovementCorrectionOperationId={result.OperationId}");
        Assert.Null(await f.Audit.GetMovementChangeDetailAsync(auditId));
    }
}
