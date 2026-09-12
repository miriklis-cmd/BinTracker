using BinTracker.Core;
using BinTracker.Services;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace BinTracker.IntegrationTests;

public sealed class Task20MutationActivationTests
{
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
    public async Task Whole_root_with_reversed_line_requires_explicit_decisions_and_creates_no_implicit_restore()
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
        var invalid = Assert.IsType<InvalidOperationException>(error);
        Assert.Equal("Every and only reversed root line requires an explicit decision.", invalid.Message);
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
