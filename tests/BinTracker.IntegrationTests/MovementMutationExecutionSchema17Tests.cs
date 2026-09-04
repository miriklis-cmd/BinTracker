using BinTracker.Core;
using BinTracker.Data;
using BinTracker.Services;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BinTracker.IntegrationTests;

public sealed class MovementMutationExecutionSchema17Tests
{
    [Fact]
    public async Task Individual_correct_persists_complete_generation_and_carries_untargeted_lines()
    {
        await using var harness = await Harness.CreateAsync();
        var root = await harness.CreateBatchRootAsync();
        var lines = await harness.LineIdsAsync(root.RootId);
        var request = MovementMutationRequest.Correct(MovementMutationScope.Individual,
            [new(lines[1])], "fix quantity", quantity: MovementFieldIntent<int>.Selected(12),
            reference: MovementFieldIntent<string>.Selected(null));

        var clientOperationId = Guid.NewGuid();
        var result = await harness.ExecuteAsync(root.RootId, 0, request, clientOperationId);

        Assert.Equal(LogicalMovementMutationResultKind.Committed, result.Kind);
        Assert.Equal(1, result.ResultGeneration.Value);
        Assert.Null(result.PhysicalOutputBatchId);
        await using var connection = await harness.OpenAsync();
        Assert.Equal(1, await ScalarAsync(connection,
            $"SELECT COUNT(*) FROM LogicalMovementGenerationLines WHERE LogicalMovementBatchId={root.RootId} AND Action=4;"));
        Assert.Equal(2, await ScalarAsync(connection,
            $"SELECT COUNT(*) FROM LogicalMovementGenerationLines WHERE LogicalMovementBatchId={root.RootId} AND Action=2;"));
        Assert.Equal(2, await ScalarAsync(connection,
            $"SELECT COUNT(*) FROM LogicalMovementLedgerLinks WHERE LogicalMovementBatchId={root.RootId} AND Role IN (1,2);"));
        Assert.Equal(1, await ScalarAsync(connection,
            "SELECT COUNT(*) FROM AuditEvents WHERE Action='MOVEMENT_CORRECTED' AND EntityType='LogicalMovementBatch';"));
        Assert.Equal(1, await ScalarAsync(connection,
            "SELECT COUNT(*) FROM MovementCorrectionOperations WHERE RequestSchemaVersion=1 AND ResultGenerationNumber=1;"));
        Assert.Equal(0, await ScalarAsync(connection, "SELECT COUNT(*) FROM MovementCorrectionLines;"));
        var (requestJson, fingerprint) = await ReadOperationIntentAsync(connection, result.OperationId!.Value);
        Assert.Equal(Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(requestJson))), fingerprint);
        Assert.DoesNotContain(clientOperationId.ToString(), requestJson, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("mutation-operator", requestJson, StringComparison.Ordinal);
        using var document = JsonDocument.Parse(requestJson);
        Assert.Equal(1, document.RootElement.GetProperty("schemaVersion").GetInt32());
        Assert.Equal("value", document.RootElement.GetProperty("fields")
            .GetProperty("quantity").GetProperty("selection").GetString());
        Assert.Equal("clear", document.RootElement.GetProperty("fields")
            .GetProperty("reference").GetProperty("selection").GetString());
        Assert.Equal("unselected", document.RootElement.GetProperty("fields")
            .GetProperty("movementDate").GetProperty("selection").GetString());
    }

    [Fact]
    public async Task Reverse_restore_and_repeated_later_generation_use_exact_actions_and_restore_audit()
    {
        await using var harness = await Harness.CreateAsync();
        var root = await harness.CreateSingleRootAsync(quantity: 5);
        var line = (await harness.LineIdsAsync(root.RootId)).Single();

        await harness.ExecuteAsync(root.RootId, 0,
            MovementMutationRequest.Reverse(MovementMutationScope.Individual, [new(line)], "entered twice"));
        await harness.ExecuteAsync(root.RootId, 1,
            MovementMutationRequest.Restore(MovementMutationScope.Individual, [new(line)], "restore entry"));
        await harness.ExecuteAsync(root.RootId, 2,
            MovementMutationRequest.Correct(MovementMutationScope.Individual, [new(line)], "fix quantity",
                quantity: MovementFieldIntent<int>.Selected(6)));

        await using var connection = await harness.OpenAsync();
        Assert.Equal(3, await ScalarAsync(connection,
            $"SELECT CurrentGenerationNumber FROM LogicalMovementBatches WHERE Id={root.RootId};"));
        Assert.Equal(1, await ScalarAsync(connection,
            $"SELECT COUNT(*) FROM LogicalMovementGenerationLines WHERE LogicalMovementBatchId={root.RootId} AND Action=5;"));
        Assert.Equal(1, await ScalarAsync(connection,
            $"SELECT COUNT(*) FROM LogicalMovementGenerationLines WHERE LogicalMovementBatchId={root.RootId} AND Action=6 AND AppliedFieldMask=0;"));
        Assert.Equal(1, await ScalarAsync(connection,
            "SELECT COUNT(*) FROM AuditEvents WHERE Action='MOVEMENT_RESTORED';"));
        Assert.Equal(1, await ScalarAsync(connection,
            $"SELECT COUNT(*) FROM LogicalMovementLedgerLinks WHERE LogicalMovementBatchId={root.RootId} AND Role=4;"));
    }

    [Theory]
    [InlineData("Correct", "MOVEMENT_CORRECTED")]
    [InlineData("Reverse", "MOVEMENT_REVERSED")]
    [InlineData("Restore", "MOVEMENT_RESTORED")]
    public async Task Primary_audit_records_trusted_before_and_resulting_business_state(
        string mutation, string auditAction)
    {
        await using var harness = await Harness.CreateAsync();
        var root = await harness.CreateSingleRootAsync(quantity: 5);
        var line = (await harness.LineIdsAsync(root.RootId)).Single();
        var expected = 0;
        if (mutation == "Restore")
        {
            await harness.ExecuteAsync(root.RootId, expected,
                MovementMutationRequest.Reverse(MovementMutationScope.Individual,
                    [new(line)], "prepare restore"));
            expected++;
        }
        var request = mutation switch
        {
            "Correct" => MovementMutationRequest.Correct(MovementMutationScope.Individual,
                [new(line)], "audit correction", quantity: MovementFieldIntent<int>.Selected(6)),
            "Reverse" => MovementMutationRequest.Reverse(MovementMutationScope.Individual,
                [new(line)], "audit reversal"),
            "Restore" => MovementMutationRequest.Restore(MovementMutationScope.Individual,
                [new(line)], "audit restoration"),
            _ => throw new InvalidOperationException()
        };

        await harness.ExecuteAsync(root.RootId, expected, request);

        await using var connection = await harness.OpenAsync();
        var (beforeJson, afterJson) = await ReadAuditPayloadAsync(connection, auditAction);
        using var before = JsonDocument.Parse(beforeJson);
        using var after = JsonDocument.Parse(afterJson);
        Assert.Equal(expected, before.RootElement.GetProperty("CurrentGeneration").GetInt32());
        var beforeLine = before.RootElement.GetProperty("Lines")[0];
        Assert.Equal(line, beforeLine.GetProperty("LineId").GetInt64());
        Assert.True(beforeLine.GetProperty("GenerationLineId").GetInt64() > 0);
        AssertBusinessState(beforeLine.GetProperty("LastEffective"), expectedMovementId: null);
        if (mutation == "Restore")
            AssertBusinessState(beforeLine.GetProperty("TerminalReversal"), expectedMovementId: null);

        Assert.Equal(expected + 1, after.RootElement.GetProperty("ResultGeneration").GetInt32());
        var afterLine = after.RootElement.GetProperty("Lines")[0];
        Assert.Equal(line, afterLine.GetProperty("LineId").GetInt64());
        Assert.True(afterLine.TryGetProperty("Action", out _));
        Assert.True(afterLine.TryGetProperty("State", out _));
        Assert.True(afterLine.TryGetProperty("AppliedFieldMask", out _));
        Assert.True(afterLine.GetProperty("EffectiveMovementId").GetInt64() > 0);
        AssertBusinessState(afterLine.GetProperty("LastEffective"), expectedMovementId: null);
        if (mutation == "Reverse")
            AssertBusinessState(afterLine.GetProperty("TerminalReversal"), expectedMovementId: null);
        Assert.NotEmpty(afterLine.GetProperty("NewMovements").EnumerateArray());
    }

    [Fact]
    public async Task Whole_root_uniform_correction_creates_exact_eligible_physical_output()
    {
        await using var harness = await Harness.CreateAsync();
        var root = await harness.CreateBatchRootAsync();
        var lines = await harness.LineIdsAsync(root.RootId);
        var request = MovementMutationRequest.Correct(MovementMutationScope.WholeRoot,
            lines.Select(x => new LogicalMovementLineId(x)), "move date",
            movementDate: MovementFieldIntent<DateOnly>.Selected(new DateOnly(2026, 9, 2)));

        var operationId = Guid.NewGuid();
        var result = await harness.ExecuteAsync(root.RootId, 0, request, operationId);
        var replay = await harness.ExecuteAsync(root.RootId, 0, request, operationId);

        Assert.NotNull(result.PhysicalOutputBatchId);
        Assert.Equal(LogicalMovementMutationResultKind.Replayed, replay.Kind);
        Assert.Equal(result.PhysicalOutputBatchId, replay.PhysicalOutputBatchId);
        await using var connection = await harness.OpenAsync();
        Assert.Equal(1, await ScalarAsync(connection, "SELECT COUNT(*) FROM LogicalMovementPhysicalOutputs;"));
        Assert.Equal(1, await ScalarAsync(connection,
            "SELECT COUNT(*) FROM MovementCorrectionOperations WHERE OriginalBatchId IS NULL AND ReplacementBatchId IS NULL;"));
        Assert.Equal(lines.Count, await ScalarAsync(connection,
            $"SELECT COUNT(*) FROM BinMovements WHERE MovementBatchId={result.PhysicalOutputBatchId};"));
        Assert.Equal(0, await ScalarAsync(connection, $"""
            SELECT COUNT(*) FROM BinMovements m
            JOIN LogicalMovementLedgerLinks l ON l.BinMovementId=m.Id
            WHERE m.MovementBatchId={result.PhysicalOutputBatchId} AND l.Role<>2;
            """));
        Assert.Equal(1, await ScalarAsync(connection,
            "SELECT COUNT(*) FROM AuditEvents WHERE Action='MOVEMENT_BATCH_CORRECTED';"));
    }

    [Fact]
    public async Task Mixed_complete_generation_persists_already_matches_restored_remain_reversed_and_no_output()
    {
        await using var harness = await Harness.CreateAsync();
        var root = await harness.CreateBatchRootAsync(equalQuantities: true);
        var lines = await harness.LineIdsAsync(root.RootId);
        await harness.ExecuteAsync(root.RootId, 0,
            MovementMutationRequest.Reverse(MovementMutationScope.Individual, [new(lines[1])], "reverse second"));
        await harness.ExecuteAsync(root.RootId, 1,
            MovementMutationRequest.Reverse(MovementMutationScope.Individual, [new(lines[2])], "reverse third"));
        var decisions = new[]
        {
            ReversedLineDecision.Restore(new(lines[1])),
            ReversedLineDecision.RemainReversed(new(lines[2]))
        };
        var request = MovementMutationRequest.Correct(MovementMutationScope.WholeRoot,
            lines.Select(x => new LogicalMovementLineId(x)), "mixed correction",
            quantity: MovementFieldIntent<int>.Selected(2), reversedLineDecisions: decisions);

        await harness.ExecuteAsync(root.RootId, 2, request);

        await using var connection = await harness.OpenAsync();
        var generationId = await ScalarAsync(connection,
            $"SELECT Id FROM LogicalMovementGenerations WHERE LogicalMovementBatchId={root.RootId} AND GenerationNumber=3;");
        Assert.Equal(1, await ScalarAsync(connection,
            $"SELECT COUNT(*) FROM LogicalMovementGenerationLines WHERE LogicalMovementGenerationId={generationId} AND Action=3;"));
        Assert.Equal(1, await ScalarAsync(connection,
            $"SELECT COUNT(*) FROM LogicalMovementGenerationLines WHERE LogicalMovementGenerationId={generationId} AND Action=6;"));
        Assert.Equal(1, await ScalarAsync(connection,
            $"SELECT COUNT(*) FROM LogicalMovementGenerationLines WHERE LogicalMovementGenerationId={generationId} AND Action=7;"));
        Assert.Equal(0, await ScalarAsync(connection,
            $"SELECT COUNT(*) FROM LogicalMovementPhysicalOutputs WHERE LogicalMovementGenerationId={generationId};"));
    }

    [Fact]
    public async Task Complete_noop_returns_no_change_and_persists_zero_artifacts_or_operation_identity()
    {
        await using var harness = await Harness.CreateAsync();
        var root = await harness.CreateSingleRootAsync(quantity: 7);
        var line = (await harness.LineIdsAsync(root.RootId)).Single();
        var before = await harness.SnapshotAsync();
        var operationId = Guid.NewGuid();
        var request = MovementMutationRequest.Correct(MovementMutationScope.Individual,
            [new(line)], "already correct", quantity: MovementFieldIntent<int>.Selected(7));

        var result = await harness.ExecuteAsync(root.RootId, 0, request, operationId);

        Assert.Equal(LogicalMovementMutationResultKind.NoChange, result.Kind);
        Assert.Null(result.OperationId);
        Assert.Equal(before, await harness.SnapshotAsync());
        await using var connection = await harness.OpenAsync();
        Assert.Equal(0, await ScalarAsync(connection,
            $"SELECT COUNT(*) FROM MovementCorrectionOperations WHERE ClientOperationId='{operationId}';"));
    }

    [Fact]
    public async Task Viewer_is_denied_before_any_mutation_read_or_write()
    {
        await using var harness = await Harness.CreateAsync();
        var root = await harness.CreateSingleRootAsync();
        var line = (await harness.LineIdsAsync(root.RootId)).Single();
        var before = await harness.SnapshotAsync();
        harness.SetRole(UserRole.Viewer);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            harness.ExecuteAsync(root.RootId, 0,
                MovementMutationRequest.Correct(MovementMutationScope.Individual,
                    [new(line)], "viewer denied", quantity: MovementFieldIntent<int>.Selected(8))));

        Assert.Equal(before, await harness.SnapshotAsync());
    }

    [Fact]
    public async Task Normal_composition_dormant_writer_does_not_probe_or_activate_schema17()
    {
        await using var harness = await Harness.CreateAsync(migrateToSchema17: false,
            enableInitialWriter: false, enableMutationWriter: false);

        var error = await Assert.ThrowsAsync<LogicalMovementMutationException>(() =>
            harness.Service.ExecuteLogicalAsync(new(Guid.NewGuid(), new(1), new(0),
                MovementMutationRequest.Correct(MovementMutationScope.Individual,
                    [new(1)], "dormant path", quantity: MovementFieldIntent<int>.Selected(2)))));

        Assert.Equal(LogicalMovementMutationFailure.SchemaUnavailable, error.Failure);
        await using var connection = await harness.OpenAsync();
        Assert.Equal(16, await ScalarAsync(connection, "SELECT Version FROM SchemaVersion WHERE Id=1;"));
        Assert.Equal(0, await ScalarAsync(connection,
            "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='LogicalMovementBatches';"));
        Assert.Equal(0, await ScalarAsync(connection, "SELECT COUNT(*) FROM BinMovements;"));
    }

    [Fact]
    public async Task Exact_retry_returns_original_generation_after_newer_generation_and_conflicts_are_stable()
    {
        await using var harness = await Harness.CreateAsync();
        var root = await harness.CreateSingleRootAsync(quantity: 5);
        var line = (await harness.LineIdsAsync(root.RootId)).Single();
        var firstId = Guid.NewGuid();
        var firstRequest = MovementMutationRequest.Correct(MovementMutationScope.Individual,
            [new(line)], "first change", quantity: MovementFieldIntent<int>.Selected(6));
        var first = await harness.ExecuteAsync(root.RootId, 0, firstRequest, firstId);
        var immediateReplay = await harness.ExecuteAsync(root.RootId, 0, firstRequest, firstId);
        await harness.ExecuteAsync(root.RootId, 1,
            MovementMutationRequest.Correct(MovementMutationScope.Individual,
                [new(line)], "second change", quantity: MovementFieldIntent<int>.Selected(7)));
        var laterReplay = await harness.ExecuteAsync(root.RootId, 0, firstRequest, firstId);

        Assert.Equal(LogicalMovementMutationResultKind.Replayed, immediateReplay.Kind);
        Assert.Equal(first.OperationId, immediateReplay.OperationId);
        Assert.Equal(1, laterReplay.ResultGeneration.Value);
        Assert.Equal(first.OperationId, laterReplay.OperationId);

        var conflict = await Assert.ThrowsAsync<LogicalMovementMutationException>(() =>
            harness.ExecuteAsync(root.RootId, 0,
                MovementMutationRequest.Correct(MovementMutationScope.Individual,
                    [new(line)], "changed payload", quantity: MovementFieldIntent<int>.Selected(8)), firstId));
        Assert.Equal(LogicalMovementMutationFailure.OperationIdConflict, conflict.Failure);

        var stale = await Assert.ThrowsAsync<LogicalMovementMutationException>(() =>
            harness.ExecuteAsync(root.RootId, 0, firstRequest));
        Assert.Equal(LogicalMovementMutationFailure.StaleGeneration, stale.Failure);
    }

    [Fact]
    public async Task Migrated_migration_baseline_is_a_valid_exact_predecessor()
    {
        await using var harness = await Harness.CreateAsync(migrateToSchema17: false,
            enableInitialWriter: false, enableMutationWriter: false);
        var saved = await harness.SaveSchema16SingleAsync();
        await harness.MigrateAndEnableAsync();
        var rootId = await harness.RootForMovementAsync(saved.MovementId);
        var line = (await harness.LineIdsAsync(rootId)).Single();

        await harness.ExecuteAsync(rootId, 0,
            MovementMutationRequest.Correct(MovementMutationScope.Individual,
                [new(line)], "migrated correction", quantity: MovementFieldIntent<int>.Selected(9)));

        await using var connection = await harness.OpenAsync();
        Assert.Equal(1, await ScalarAsync(connection,
            $"SELECT COUNT(*) FROM LogicalMovementGenerations WHERE LogicalMovementBatchId={rootId} AND GenerationNumber=0 AND Kind=1;"));
        Assert.Equal(1, await ScalarAsync(connection, $"""
            SELECT COUNT(*) FROM LogicalMovementGenerationLines current
            JOIN LogicalMovementGenerationLines previous ON previous.Id=current.PreviousGenerationLineId
            WHERE current.LogicalMovementBatchId={rootId} AND current.Action=4 AND previous.Action=1;
            """));
    }

    [Theory]
    [InlineData(LogicalMovementBatchStatus.ReadOnly, LogicalMovementMutationFailure.ReadOnly)]
    [InlineData(LogicalMovementBatchStatus.Invalid, LogicalMovementMutationFailure.Unhealthy)]
    [InlineData(LogicalMovementBatchStatus.Initializing, LogicalMovementMutationFailure.Unhealthy)]
    public async Task Nonmutable_root_status_fails_closed(LogicalMovementBatchStatus status,
        LogicalMovementMutationFailure expected)
    {
        await using var harness = await Harness.CreateAsync();
        var root = await harness.CreateSingleRootAsync();
        var line = (await harness.LineIdsAsync(root.RootId)).Single();
        await harness.ExecuteSqlAsync($"UPDATE LogicalMovementBatches SET Status={(int)status} WHERE Id={root.RootId};");

        var error = await Assert.ThrowsAsync<LogicalMovementMutationException>(() =>
            harness.ExecuteAsync(root.RootId, 0,
                MovementMutationRequest.Correct(MovementMutationScope.Individual,
                    [new(line)], "blocked state", quantity: MovementFieldIntent<int>.Selected(8))));
        Assert.Equal(expected, error.Failure);
    }

    [Fact]
    public async Task Schema16_malformed_schema17_unrooted_evidence_and_audit_corruption_fail_before_writes()
    {
        await using (var schema16 = await Harness.CreateAsync(migrateToSchema17: false))
        {
            var error = await Assert.ThrowsAnyAsync<InvalidOperationException>(() =>
                schema16.Service.ExecuteLogicalAsync(new(Guid.NewGuid(), new(1), new(0),
                    MovementMutationRequest.Correct(MovementMutationScope.Individual,
                        [new(1)], "schema check", quantity: MovementFieldIntent<int>.Selected(2)))));
            Assert.Contains("SCHEMA17", error.ToString(), StringComparison.OrdinalIgnoreCase);
        }

        await using (var malformed = await Harness.CreateAsync())
        {
            await malformed.ExecuteSqlAsync("DROP TABLE LogicalMovementPhysicalOutputs;");
            var error = await Assert.ThrowsAnyAsync<InvalidOperationException>(() =>
                malformed.Service.ExecuteLogicalAsync(new(Guid.NewGuid(), new(1), new(0),
                    MovementMutationRequest.Correct(MovementMutationScope.Individual,
                        [new(1)], "schema check", quantity: MovementFieldIntent<int>.Selected(2)))));
            Assert.Contains("TABLE", error.ToString(), StringComparison.OrdinalIgnoreCase);
        }

        await using (var unrooted = await Harness.CreateAsync())
        {
            await unrooted.InsertUnrootedMovementAsync();
            var before = await unrooted.SnapshotAsync();
            await Assert.ThrowsAnyAsync<InvalidOperationException>(() =>
                unrooted.Service.ExecuteLogicalAsync(new(Guid.NewGuid(), new(1), new(0),
                    MovementMutationRequest.Correct(MovementMutationScope.Individual,
                        [new(1)], "health check", quantity: MovementFieldIntent<int>.Selected(2)))));
            Assert.Equal(before, await unrooted.SnapshotAsync());
        }

        await using (var audit = await Harness.CreateAsync())
        {
            var root = await audit.CreateSingleRootAsync();
            var line = (await audit.LineIdsAsync(root.RootId)).Single();
            await audit.ExecuteAsync(root.RootId, 0,
                MovementMutationRequest.Correct(MovementMutationScope.Individual,
                    [new(line)], "first change", quantity: MovementFieldIntent<int>.Selected(8)));
            await audit.ExecuteSqlAsync("UPDATE AuditEvents SET MovementCorrectionOperationId=NULL WHERE MovementCorrectionOperationId IS NOT NULL;");
            var before = await audit.SnapshotAsync();
            await Assert.ThrowsAnyAsync<InvalidOperationException>(() => audit.ExecuteAsync(root.RootId, 1,
                MovementMutationRequest.Correct(MovementMutationScope.Individual,
                    [new(line)], "blocked audit", quantity: MovementFieldIntent<int>.Selected(9))));
            Assert.Equal(before, await audit.SnapshotAsync());
        }
    }

    [Fact]
    public async Task Operation_audit_corruption_blocks_only_its_target_root_and_failed_attempt_leaves_no_artifacts()
    {
        await using var harness = await Harness.CreateAsync();
        var rootA = await harness.CreateSingleRootAsync(quantity: 5);
        var rootB = await harness.CreateSingleRootAsync(quantity: 6);
        var lineA = (await harness.LineIdsAsync(rootA.RootId)).Single();
        var lineB = (await harness.LineIdsAsync(rootB.RootId)).Single();
        var requestA = MovementMutationRequest.Correct(MovementMutationScope.Individual,
            [new(lineA)], "root A first", quantity: MovementFieldIntent<int>.Selected(7));
        var requestB = MovementMutationRequest.Correct(MovementMutationScope.Individual,
            [new(lineB)], "root B first", quantity: MovementFieldIntent<int>.Selected(8));
        var operationA = Guid.NewGuid();
        await harness.ExecuteAsync(rootA.RootId, 0, requestA, operationA);
        await harness.ExecuteAsync(rootB.RootId, 0, requestB);
        await harness.ExecuteSqlAsync($"""
            UPDATE AuditEvents SET MovementCorrectionOperationId=NULL
            WHERE MovementCorrectionOperationId=(
                SELECT Id FROM MovementCorrectionOperations
                WHERE LogicalMovementBatchId={rootA.RootId} AND ResultGenerationNumber=1);
            """);
        var beforeFailedA = await harness.SnapshotAsync();

        var replayFailure = await Assert.ThrowsAsync<LogicalMovementMutationException>(() =>
            harness.ExecuteAsync(rootA.RootId, 0, requestA, operationA));
        Assert.Equal(LogicalMovementMutationFailure.Unhealthy, replayFailure.Failure);
        var mutationFailure = await Assert.ThrowsAsync<LogicalMovementMutationException>(() =>
            harness.ExecuteAsync(rootA.RootId, 1,
                MovementMutationRequest.Correct(MovementMutationScope.Individual,
                    [new(lineA)], "root A blocked", quantity: MovementFieldIntent<int>.Selected(9))));
        Assert.Equal(LogicalMovementMutationFailure.Unhealthy, mutationFailure.Failure);
        Assert.Equal(beforeFailedA, await harness.SnapshotAsync());

        var resultB = await harness.ExecuteAsync(rootB.RootId, 1,
            MovementMutationRequest.Correct(MovementMutationScope.Individual,
                [new(lineB)], "root B remains healthy", quantity: MovementFieldIntent<int>.Selected(9)));
        Assert.Equal(LogicalMovementMutationResultKind.Committed, resultB.Kind);
        Assert.Equal(2, resultB.ResultGeneration.Value);
    }

    [Theory]
    [InlineData(5)]
    [InlineData(6)]
    public async Task Sqlite_contention_during_write_is_reported_as_persistence_failure_not_stale_or_integrity_failure(
        int sqliteErrorCode)
    {
        await using var harness = await Harness.CreateAsync();
        var root = await harness.CreateSingleRootAsync(quantity: 5);
        var line = (await harness.LineIdsAsync(root.RootId)).Single();
        var before = await harness.SnapshotAsync();
        harness.FailWithSqliteContentionAt(
            MovementMutationWriteCheckpoint.AfterOperationEnvelope, sqliteErrorCode);

        var error = await Assert.ThrowsAsync<LogicalMovementMutationException>(() =>
            harness.ExecuteAsync(root.RootId, 0,
                MovementMutationRequest.Correct(MovementMutationScope.Individual,
                    [new(line)], "contention", quantity: MovementFieldIntent<int>.Selected(6))));

        Assert.Equal(LogicalMovementMutationFailure.PersistenceFailure, error.Failure);
        Assert.NotEqual(LogicalMovementMutationFailure.StaleGeneration, error.Failure);
        Assert.NotEqual(LogicalMovementMutationFailure.IntegrityFailure, error.Failure);
        Assert.Equal(before, await harness.SnapshotAsync());
    }

    public static IEnumerable<object[]> FailureCheckpoints() =>
        Enum.GetValues<MovementMutationWriteCheckpoint>().Select(x => new object[] { x.ToString() });

    [Theory]
    [MemberData(nameof(FailureCheckpoints))]
    public async Task Every_writer_checkpoint_rolls_back_the_complete_attempted_graph(
        string checkpointName)
    {
        var checkpoint = Enum.Parse<MovementMutationWriteCheckpoint>(checkpointName);
        await using var harness = await Harness.CreateAsync();
        var root = await harness.CreateBatchRootAsync();
        var lines = await harness.LineIdsAsync(root.RootId);
        var expected = 0;
        MovementMutationRequest request;
        if (checkpoint == MovementMutationWriteCheckpoint.AfterOrdinaryReversal)
        {
            request = MovementMutationRequest.Reverse(MovementMutationScope.Individual,
                [new(lines[0])], "injected reversal");
        }
        else if (checkpoint == MovementMutationWriteCheckpoint.AfterRestoration)
        {
            await harness.ExecuteAsync(root.RootId, 0,
                MovementMutationRequest.Reverse(MovementMutationScope.Individual,
                    [new(lines[0])], "prepare restore"));
            expected = 1;
            request = MovementMutationRequest.Restore(MovementMutationScope.Individual,
                [new(lines[0])], "injected restore");
        }
        else
        {
            request = MovementMutationRequest.Correct(MovementMutationScope.WholeRoot,
                lines.Select(x => new LogicalMovementLineId(x)), "injected correction",
                movementDate: MovementFieldIntent<DateOnly>.Selected(new DateOnly(2026, 9, 2)));
        }
        var before = await harness.SnapshotAsync();
        harness.FailAt(checkpoint);

        await Assert.ThrowsAnyAsync<Exception>(() =>
            harness.ExecuteAsync(root.RootId, expected, request));

        Assert.True(harness.WasObserved(checkpoint));
        Assert.Equal(before, await harness.SnapshotAsync());
    }

    [Fact]
    public async Task Audit_save_failure_rolls_back_and_exact_retry_succeeds_once()
    {
        await using var harness = await Harness.CreateAsync();
        var root = await harness.CreateSingleRootAsync();
        var line = (await harness.LineIdsAsync(root.RootId)).Single();
        var request = MovementMutationRequest.Correct(MovementMutationScope.Individual,
            [new(line)], "retry after failure", quantity: MovementFieldIntent<int>.Selected(8));
        var id = Guid.NewGuid();
        var before = await harness.SnapshotAsync();
        harness.FailNextSave();

        await Assert.ThrowsAnyAsync<Exception>(() => harness.ExecuteAsync(root.RootId, 0, request, id));
        Assert.Equal(before, await harness.SnapshotAsync());

        var result = await harness.ExecuteAsync(root.RootId, 0, request, id);
        Assert.Equal(LogicalMovementMutationResultKind.Committed, result.Kind);
        Assert.Equal(LogicalMovementMutationResultKind.Replayed,
            (await harness.ExecuteAsync(root.RootId, 0, request, id)).Kind);
    }

    [Theory]
    [InlineData("correct-correct")]
    [InlineData("reverse-correct")]
    [InlineData("restore-correct")]
    [InlineData("whole-whole")]
    [InlineData("different-lines")]
    public async Task Same_root_races_publish_only_one_generation(string race)
    {
        await using var harness = await Harness.CreateAsync();
        var root = await harness.CreateBatchRootAsync();
        var lines = await harness.LineIdsAsync(root.RootId);
        var expected = 0;
        if (race == "restore-correct")
        {
            await harness.ExecuteAsync(root.RootId, 0,
                MovementMutationRequest.Reverse(MovementMutationScope.Individual,
                    [new(lines[0])], "prepare restore"));
            expected = 1;
        }
        var first = race switch
        {
            "reverse-correct" => MovementMutationRequest.Reverse(MovementMutationScope.Individual,
                [new(lines[0])], "race reverse"),
            "restore-correct" => MovementMutationRequest.Restore(MovementMutationScope.Individual,
                [new(lines[0])], "race restore"),
            "whole-whole" => MovementMutationRequest.Correct(MovementMutationScope.WholeRoot,
                lines.Select(x => new LogicalMovementLineId(x)), "race whole one",
                movementDate: MovementFieldIntent<DateOnly>.Selected(new DateOnly(2026, 9, 2))),
            _ => MovementMutationRequest.Correct(MovementMutationScope.Individual,
                [new(lines[0])], "race first", quantity: MovementFieldIntent<int>.Selected(11))
        };
        var second = race switch
        {
            "whole-whole" => MovementMutationRequest.Correct(MovementMutationScope.WholeRoot,
                lines.Select(x => new LogicalMovementLineId(x)), "race whole two",
                movementDate: MovementFieldIntent<DateOnly>.Selected(new DateOnly(2026, 9, 2))),
            "different-lines" => MovementMutationRequest.Correct(MovementMutationScope.Individual,
                [new(lines[1])], "race second", quantity: MovementFieldIntent<int>.Selected(12)),
            _ => MovementMutationRequest.Correct(MovementMutationScope.Individual,
                [new(lines[^1])], "race correction", quantity: MovementFieldIntent<int>.Selected(13))
        };

        var calls = new[]
        {
            CaptureAsync(() => harness.ExecuteAsync(root.RootId, expected, first)),
            CaptureAsync(() => harness.ExecuteAsync(root.RootId, expected, second))
        };
        var outcomes = await Task.WhenAll(calls);

        Assert.Single(outcomes, x => x.Result?.Kind == LogicalMovementMutationResultKind.Committed);
        Assert.Single(outcomes, x => x.Error is not null);
        Assert.Contains(outcomes, x => x.Error is LogicalMovementMutationException
            { Failure: LogicalMovementMutationFailure.StaleGeneration });
        await using var connection = await harness.OpenAsync();
        Assert.Equal(expected + 1, await ScalarAsync(connection,
            $"SELECT CurrentGenerationNumber FROM LogicalMovementBatches WHERE Id={root.RootId};"));
        Assert.Equal(1, await ScalarAsync(connection,
            $"SELECT COUNT(*) FROM LogicalMovementGenerations WHERE LogicalMovementBatchId={root.RootId} AND GenerationNumber={expected + 1};"));
    }

    [Fact]
    public async Task Concurrent_exact_same_operation_id_returns_one_commit_and_one_replay()
    {
        await using var harness = await Harness.CreateAsync();
        var root = await harness.CreateSingleRootAsync(quantity: 5);
        var line = (await harness.LineIdsAsync(root.RootId)).Single();
        var id = Guid.NewGuid();
        var request = MovementMutationRequest.Correct(MovementMutationScope.Individual,
            [new(line)], "same operation race", quantity: MovementFieldIntent<int>.Selected(6));

        var results = await Task.WhenAll(
            harness.ExecuteAsync(root.RootId, 0, request, id),
            harness.ExecuteAsync(root.RootId, 0, request, id));

        Assert.Single(results, x => x.Kind == LogicalMovementMutationResultKind.Committed);
        Assert.Single(results, x => x.Kind == LogicalMovementMutationResultKind.Replayed);
        Assert.Equal(results[0].OperationId, results[1].OperationId);
        await using var connection = await harness.OpenAsync();
        Assert.Equal(1, await ScalarAsync(connection,
            $"SELECT COUNT(*) FROM MovementCorrectionOperations WHERE ClientOperationId='{id}';"));
        Assert.Equal(1, await ScalarAsync(connection,
            "SELECT COUNT(*) FROM AuditEvents WHERE MovementCorrectionOperationId IS NOT NULL;"));
    }

    private static async Task<(LogicalMovementMutationResult? Result, Exception? Error)> CaptureAsync(
        Func<Task<LogicalMovementMutationResult>> action)
    {
        try { return (await action(), null); }
        catch (Exception ex) { return (null, ex); }
    }

    private static async Task<long> ScalarAsync(SqliteConnection connection, string sql)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        return Convert.ToInt64(await command.ExecuteScalarAsync());
    }

    private static async Task<(string RequestJson, string Fingerprint)> ReadOperationIntentAsync(
        SqliteConnection connection, long operationId)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = $"SELECT RequestJson,RequestFingerprint FROM MovementCorrectionOperations WHERE Id={operationId};";
        await using var reader = await command.ExecuteReaderAsync();
        Assert.True(await reader.ReadAsync());
        return (reader.GetString(0), reader.GetString(1));
    }

    private static async Task<(string BeforeJson, string AfterJson)> ReadAuditPayloadAsync(
        SqliteConnection connection, string action)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT BeforeValues,AfterValues FROM AuditEvents
            WHERE Action=$action ORDER BY Id DESC LIMIT 1;
            """;
        command.Parameters.AddWithValue("$action", action);
        await using var reader = await command.ExecuteReaderAsync();
        Assert.True(await reader.ReadAsync());
        return (reader.GetString(0), reader.GetString(1));
    }

    private static void AssertBusinessState(JsonElement state, long? expectedMovementId)
    {
        Assert.Equal(JsonValueKind.Object, state.ValueKind);
        var movementId = state.GetProperty("MovementId").GetInt64();
        Assert.True(movementId > 0);
        if (expectedMovementId is not null)
            Assert.Equal(expectedMovementId.Value, movementId);
        Assert.True(state.TryGetProperty("MovementDate", out _));
        Assert.True(state.TryGetProperty("Direction", out _));
        Assert.True(state.TryGetProperty("Source", out _));
        Assert.True(state.GetProperty("CustomerId").GetInt32() > 0);
        Assert.True(state.GetProperty("ContainerTypeId").GetInt32() > 0);
        Assert.True(state.GetProperty("Quantity").GetInt32() > 0);
        Assert.True(state.TryGetProperty("Reference", out _));
        Assert.True(state.TryGetProperty("Notes", out _));
        Assert.True(state.TryGetProperty("MovementBatchId", out _));
        Assert.True(state.TryGetProperty("ImportRunId", out _));
        Assert.True(state.TryGetProperty("ReversesMovementId", out _));
    }

    private sealed class Harness : IAsyncDisposable
    {
        internal static readonly DateTime UtcNow = new(2026, 9, 3, 1, 2, 3, DateTimeKind.Utc);
        private readonly string root;
        private ServiceProvider services;
        private LineageSchema17MigrationPrerequisites? prerequisites;
        private readonly TestMutationFailureInjector mutationFailure;
        private readonly TestSaveChangesInterceptor saveFailure;
        private readonly TestUserContext user;

        private Harness(string root, string connectionString, ServiceProvider services,
            LineageSchema17MigrationPrerequisites? prerequisites,
            TestMutationFailureInjector mutationFailure, TestSaveChangesInterceptor saveFailure,
            TestUserContext user)
        {
            this.root = root;
            ConnectionString = connectionString;
            this.services = services;
            this.prerequisites = prerequisites;
            this.mutationFailure = mutationFailure;
            this.saveFailure = saveFailure;
            this.user = user;
            RefreshServices();
        }

        public string ConnectionString { get; }
        public IMovementService Movements { get; private set; } = null!;
        public IMovementCorrectionService Service { get; private set; } = null!;
        public int CustomerId { get; private set; }

        public static async Task<Harness> CreateAsync(bool migrateToSchema17 = true,
            bool enableInitialWriter = true, bool enableMutationWriter = true)
        {
            var root = Path.Combine(Path.GetTempPath(), $"BinTracker-mutation-v17-{Guid.NewGuid():N}");
            Directory.CreateDirectory(root);
            var databasePath = Path.Combine(root, "db", "BinTracker.db");
            Directory.CreateDirectory(Path.GetDirectoryName(databasePath)!);
            var connectionString = $"Data Source={databasePath};Foreign Keys=True;Pooling=False;Default Timeout=10";
            int customerId;
            await using (var db = new BinTrackerDbContext(
                new DbContextOptionsBuilder<BinTrackerDbContext>().UseSqlite(connectionString).Options))
            {
                await DatabaseSetup.InitializeSqliteAsync(db);
                var customer = new Customer { CustomerCode = "MUT", Name = "Mutation", IsActive = true };
                db.Add(customer);
                await db.SaveChangesAsync();
                customerId = customer.Id;
            }
            LineageSchema17MigrationPrerequisites? prerequisites = null;
            if (migrateToSchema17)
                prerequisites = await MigrateAsync(root, databasePath);
            var mutationFailure = new TestMutationFailureInjector();
            var saveFailure = new TestSaveChangesInterceptor();
            var user = new TestUserContext();
            var services = BuildServices(connectionString, mutationFailure, saveFailure, user,
                enableInitialWriter, enableMutationWriter);
            return new(root, connectionString, services, prerequisites, mutationFailure, saveFailure, user)
            { CustomerId = customerId };
        }

        public async Task<(long RootId, SaveSingleMovementResult Result)> CreateSingleRootAsync(int quantity = 7)
        {
            var result = await Movements.SaveSingleAsync(new(Guid.NewGuid(), new DateOnly(2026, 9, 1),
                MovementType.Out, CustomerId, 1, quantity, "single", "single note"));
            return (await RootForMovementAsync(result.MovementId), result);
        }

        public async Task<(long RootId, SaveMovementBatchResult Result)> CreateBatchRootAsync(bool equalQuantities = false)
        {
            var quantities = equalQuantities ? new[] { 2, 2, 2 } : new[] { 2, 3, 4 };
            var result = await Movements.SaveBatchAsync(new(Guid.NewGuid(), new DateOnly(2026, 9, 1),
                MovementType.Out, "batch",
                quantities.Select((quantity, index) => new MovementBatchLine(CustomerId,
                    index + 1, quantity, $"line-{index}", $"note-{index}")).ToArray()));
            await using var connection = await OpenAsync();
            return (await ScalarAsync(connection,
                $"SELECT Id FROM LogicalMovementBatches WHERE RootMovementBatchId={result.BatchId};"), result);
        }

        public Task<LogicalMovementMutationResult> ExecuteAsync(long rootId, int expected,
            MovementMutationRequest request, Guid? operationId = null) =>
            Service.ExecuteLogicalAsync(new(operationId ?? Guid.NewGuid(), new(rootId), new(expected), request));

        public async Task<SaveSingleMovementResult> SaveSchema16SingleAsync() =>
            await Movements.SaveSingleAsync(new(Guid.NewGuid(), new DateOnly(2026, 9, 1),
                MovementType.Out, CustomerId, 1, 7, "legacy", "legacy note"));

        public async Task MigrateAndEnableAsync()
        {
            await services.DisposeAsync();
            prerequisites = await MigrateAsync(root,
                new SqliteConnectionStringBuilder(ConnectionString).DataSource);
            services = BuildServices(ConnectionString, mutationFailure, saveFailure, user, true, true);
            RefreshServices();
        }

        public async Task<long> RootForMovementAsync(long movementId)
        {
            await using var connection = await OpenAsync();
            return await ScalarAsync(connection,
                $"SELECT LogicalMovementBatchId FROM LogicalMovementLines WHERE RootMovementId={movementId};");
        }

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

        public async Task<string> SnapshotAsync()
        {
            await using var connection = await OpenAsync();
            var result = new List<string>();
            foreach (var table in new[] { "MovementBatches", "BinMovements", "MovementCorrectionOperations",
                "LogicalMovementBatches", "LogicalMovementLines", "LogicalMovementGenerations",
                "LogicalMovementGenerationLines", "LogicalMovementLedgerLinks",
                "LogicalMovementPhysicalOutputs", "AuditEvents" })
            {
                await using var command = connection.CreateCommand();
                command.CommandText = $"SELECT * FROM {table} ORDER BY 1;";
                await using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                    result.Add(table + ":" + string.Join('|', Enumerable.Range(0, reader.FieldCount)
                        .Select(i => reader.IsDBNull(i) ? "NULL" : Convert.ToString(reader.GetValue(i)))));
            }
            return string.Join(Environment.NewLine, result);
        }

        public async Task ExecuteSqlAsync(string sql)
        {
            await using var connection = await OpenAsync();
            await using var command = connection.CreateCommand();
            command.CommandText = sql;
            await command.ExecuteNonQueryAsync();
        }

        public async Task InsertUnrootedMovementAsync()
        {
            await using var db = new BinTrackerDbContext(
                new DbContextOptionsBuilder<BinTrackerDbContext>().UseSqlite(ConnectionString).Options);
            db.Add(new BinMovement { ClientOperationId = Guid.NewGuid(),
                MovementDate = new DateOnly(2026, 9, 1), MovementType = MovementType.Out,
                Source = MovementSource.Manual, CustomerId = CustomerId, ContainerTypeId = 1,
                Quantity = 1, CreatedBy = "fixture", CreatedUtc = UtcNow });
            await db.SaveChangesAsync();
        }

        public async Task<SqliteConnection> OpenAsync()
        {
            var connection = new SqliteConnection(ConnectionString);
            await connection.OpenAsync();
            return connection;
        }

        public void FailAt(MovementMutationWriteCheckpoint checkpoint) => mutationFailure.Arm(checkpoint);
        public void FailWithSqliteContentionAt(MovementMutationWriteCheckpoint checkpoint,
            int sqliteErrorCode) => mutationFailure.ArmSqliteContention(checkpoint, sqliteErrorCode);
        public bool WasObserved(MovementMutationWriteCheckpoint checkpoint) => mutationFailure.Observed.Contains(checkpoint);
        public void FailNextSave() => saveFailure.Arm();
        public void SetRole(UserRole role) => user.Role = role;

        private void RefreshServices()
        {
            Movements = services.GetRequiredService<IMovementService>();
            Service = services.GetRequiredService<IMovementCorrectionService>();
        }

        private static ServiceProvider BuildServices(string connectionString,
            TestMutationFailureInjector mutationFailure, TestSaveChangesInterceptor saveFailure,
            TestUserContext user,
            bool enableInitialWriter, bool enableMutationWriter)
        {
            var collection = new ServiceCollection();
            collection.AddSingleton<IBusinessClock>(new FixedClock());
            collection.AddSingleton<IUserContext>(user);
            collection.AddSingleton<IClientContext>(new TestClientContext());
            collection.AddDbContextFactory<BinTrackerDbContext>(builder =>
                builder.UseSqlite(connectionString).AddInterceptors(saveFailure));
            if (enableInitialWriter)
                collection.AddScoped<IInitialMovementLineageWriter>(_ =>
                    new SqliteInitialMovementLineageWriter(NoInitialMovementLineageFailureInjector.Instance));
            if (enableMutationWriter)
                collection.AddScoped<IMovementMutationWriter>(_ =>
                    new SqliteMovementMutationWriter(mutationFailure));
            collection.AddBinTrackerBusinessServices();
            return collection.BuildServiceProvider();
        }

        private static async Task<LineageSchema17MigrationPrerequisites> MigrateAsync(
            string root, string databasePath)
        {
            var gate = new WindowsFileDatabaseUpgradeGate(Path.Combine(root, "locks"), new NoConflictProbe());
            var lease = gate.AcquireUpgrade(databasePath);
            try
            {
                var preflightService = new SqliteLineageMigrationPreflight();
                var preflight = await preflightService.InspectAsync(databasePath);
                var backupService = new SqliteLineageMigrationBackupService(gate, preflightService);
                var backup = await backupService.CreateVerifiedAsync(lease, Path.Combine(root, "recovery"));
                var prerequisites = new LineageSchema17MigrationPrerequisites(lease, preflight, backup, backupService);
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

        private sealed class TestUserContext : IUserContext
        {
            public string SessionId => "mutation-session";
            public int? UserId => 51;
            public string Username => "mutation-operator";
            public string DisplayName => "Mutation Operator";
            public UserRole Role { get; set; } = UserRole.Operator;
            public bool MustChangePassword => false;
            public bool IsAuthenticated => true;
        }

        private sealed class FixedClock : IBusinessClock
        {
            public DateTime UtcNow => Harness.UtcNow;
            public DateTime LocalNow => UtcNow;
            public DateOnly Today => new(2026, 9, 3);
            public string TimeZoneId => "UTC";
        }

        private sealed class TestClientContext : IClientContext
        {
            public string ClientInstanceId => "mutation-client";
            public string DeviceName => "mutation-device";
        }

        private sealed class NoConflictProbe : IDatabaseOperationConflictProbe
        {
            public void EnsureNoConflict(string databasePath) { }
        }

        private sealed class TestMutationFailureInjector : IMovementMutationFailureInjector
        {
            private MovementMutationWriteCheckpoint? requested;
            private int? sqliteContentionErrorCode;
            public HashSet<MovementMutationWriteCheckpoint> Observed { get; } = [];
            public void Arm(MovementMutationWriteCheckpoint checkpoint)
            {
                requested = checkpoint;
                sqliteContentionErrorCode = null;
            }
            public void ArmSqliteContention(MovementMutationWriteCheckpoint checkpoint,
                int sqliteErrorCode)
            {
                if (sqliteErrorCode is not (5 or 6))
                    throw new ArgumentOutOfRangeException(nameof(sqliteErrorCode));
                requested = checkpoint;
                sqliteContentionErrorCode = sqliteErrorCode;
            }
            public void ThrowIfRequested(MovementMutationWriteCheckpoint checkpoint)
            {
                Observed.Add(checkpoint);
                if (requested == checkpoint)
                {
                    requested = null;
                    if (sqliteContentionErrorCode is { } errorCode)
                    {
                        sqliteContentionErrorCode = null;
                        throw new SqliteException("database is busy or locked", errorCode);
                    }
                    throw new InvalidOperationException("MOVEMENT_MUTATION_INJECTED_FAILURE");
                }
            }
        }

        private sealed class TestSaveChangesInterceptor : SaveChangesInterceptor
        {
            private bool armed;
            public void Arm() => armed = true;
            public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
                DbContextEventData eventData, InterceptionResult<int> result,
                CancellationToken cancellationToken = default)
            {
                if (armed)
                {
                    armed = false;
                    throw new InvalidOperationException("MOVEMENT_MUTATION_AUDIT_SAVE_FAILURE");
                }
                return base.SavingChangesAsync(eventData, result, cancellationToken);
            }
        }
    }
}
