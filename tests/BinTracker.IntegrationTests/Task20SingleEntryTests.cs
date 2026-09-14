using System.Text.Json;
using System.Data.Common;
using BinTracker.Core;
using BinTracker.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Xunit;

namespace BinTracker.IntegrationTests;

public sealed class Task20SingleCharacterizationTests
{
    [Fact]
    public async Task Backdated_single_currently_returns_whole_account_position_through_later_activity()
    {
        await using var f = await Task20Fixture.CreateAsync(schema17: false, enabled: false, projection: false);
        await f.Movements.SaveSingleAsync(f.Single(11));
        var result = await f.Movements.SaveSingleAsync(f.Single(7, Task20Fixture.Today.AddDays(-3)));
        Assert.Equal(18, result.NewBalance);
        Assert.Equal(7, await f.ScalarAsync("SELECT SUM(Quantity) FROM BinMovements WHERE MovementDate < '2026-09-05'"));
    }

    [Fact]
    public async Task Identical_single_retry_currently_recomputes_position_after_intervening_activity()
    {
        await using var f = await Task20Fixture.CreateAsync(schema17: false, enabled: false, projection: false);
        var request = f.Single();
        var first = await f.Movements.SaveSingleAsync(request);
        await f.Movements.SaveSingleAsync(f.Single(5));
        var retry = await f.Movements.SaveSingleAsync(request);
        Assert.Equal(first.MovementId, retry.MovementId);
        Assert.Equal(7, first.NewBalance);
        Assert.Equal(12, retry.NewBalance);
        Assert.Equal(2, await f.ScalarAsync("SELECT COUNT(*) FROM BinMovements"));
        Assert.Equal(2, await f.ScalarAsync("SELECT COUNT(*) FROM AuditEvents"));
    }

    [Fact]
    public async Task Persisted_future_rows_currently_contribute_to_raw_single_result()
    {
        await using var f = await Task20Fixture.CreateAsync(schema17: false, enabled: false, projection: false);
        var future = await f.Movements.SaveSingleAsync(f.Single(11));
        await f.ExecuteAsync($"UPDATE BinMovements SET MovementDate='2026-09-06' WHERE Id={future.MovementId}");
        var result = await f.Movements.SaveSingleAsync(f.Single());
        Assert.Equal(18, result.NewBalance);
        Assert.Equal(7, await f.ScalarAsync("SELECT SUM(Quantity) FROM BinMovements WHERE MovementDate <= '2026-09-05'"));
    }
}

public sealed class Task20SingleActivationTests
{
    [Fact]
    public async Task Single_result_and_audit_use_one_post_lineage_caller_transaction_projection()
    {
        await using var f = await Task20Fixture.CreateAsync();
        var saved = await f.Movements.SaveSingleAsync(f.Single());
        await using var db = f.Database.CreateDbContext();
        var audit = Assert.Single(await db.AuditEvents.ToListAsync());
        using var payload = JsonDocument.Parse(Assert.IsType<string>(audit.AfterValues));
        Assert.Equal(MovementPositionMath.Format(saved.NewBalance), payload.RootElement.GetProperty("NewPosition").GetString());
        Assert.Equal(1, f.Projection.TransactionCalls);
        Assert.Equal(0, f.Projection.IndependentCalls);
        var projected = Assert.IsType<OperationalMovementProjectionResult>(f.Projection.Result);
        Assert.Equal(Task20Fixture.Today, projected.Scope.ThroughDateInclusive);
        Assert.Equal(saved.NewBalance, Assert.Single(projected.Positions).Quantity);
        Assert.Contains(projected.Activity, x => x.EvidenceMovementId == saved.MovementId);
        Assert.Equal(1, await f.ScalarAsync("SELECT COUNT(*) FROM BinMovements"));
        Assert.Equal(1, await f.ScalarAsync("SELECT COUNT(*) FROM LogicalMovementBatches"));
        Assert.Equal(1, await f.ScalarAsync("SELECT COUNT(*) FROM LogicalMovementGenerations WHERE Kind=0"));
        Assert.Equal(1, await f.ScalarAsync("SELECT COUNT(*) FROM LogicalMovementLedgerLinks WHERE Role=0"));
        Assert.Equal(1, await f.ScalarAsync("SELECT COUNT(*) FROM SingleMovementResponseReceipts"));
        Assert.Equal(saved.MovementId, await f.ScalarAsync(
            "SELECT MovementId FROM SingleMovementResponseReceipts"));
        Assert.Equal(saved.NewBalance, await f.ScalarAsync(
            "SELECT ResultingPosition FROM SingleMovementResponseReceipts"));
        Assert.Equal(1, await f.ScalarAsync(
            "SELECT COUNT(*) FROM SingleMovementResponseReceipts WHERE BusinessDate='2026-09-05'"));
    }

    [Theory]
    [InlineData("projection")]
    [InlineData("cancellation")]
    [InlineData("overflow")]
    public async Task Single_projection_failure_rolls_back_all_physical_lineage_and_audit_evidence(string failure)
    {
        await using var f = await Task20Fixture.CreateAsync();
        var before = await f.CountsAsync();
        f.Projection.Failure = failure switch
        {
            "projection" => new OperationalMovementProjectionException(
                OperationalMovementProjectionFailure.RelevantLineageInvalid, "TASK20_PROJECTION_FAILURE"),
            "cancellation" => new OperationCanceledException("TASK20_PROJECTION_CANCELLED"),
            "overflow" => new OverflowException("TASK20_PROJECTION_OVERFLOW"),
            _ => throw new ArgumentOutOfRangeException(nameof(failure))
        };
        var error = await Record.ExceptionAsync(() => f.Movements.SaveSingleAsync(f.Single()));
        Assert.Same(f.Projection.Failure, error);
        Assert.Equal(before, await f.CountsAsync());
    }

    [Fact]
    public async Task Single_integer_position_overflow_rolls_back_instead_of_committing_a_wrapped_credit()
    {
        await using var f = await Task20Fixture.CreateAsync();
        await f.Movements.SaveSingleAsync(f.Single(int.MaxValue - 1));
        var before = await f.CountsAsync();
        await Assert.ThrowsAsync<OverflowException>(() => f.Movements.SaveSingleAsync(f.Single(3)));
        Assert.Equal(before, await f.CountsAsync());
    }

    [Fact]
    public async Task Single_receipt_persistence_failure_rolls_back_all_evidence()
    {
        await using var f = await Task20Fixture.CreateAsync();
        var before = await f.CountsAsync();
        var failure = new InvalidOperationException("TASK20_RECEIPT_FAILURE");
        f.FailReceiptWith(failure);

        var error = await Record.ExceptionAsync(() => f.Movements.SaveSingleAsync(f.Single()));

        Assert.Same(failure, error);
        Assert.Equal(before, await f.CountsAsync());
    }

    [Fact]
    public async Task Single_cancellation_after_receipt_insert_rolls_back_all_evidence()
    {
        await using var f = await Task20Fixture.CreateAsync();
        var before = await f.CountsAsync();
        using var source = new CancellationTokenSource();
        OperationCanceledException? cancellation = null;
        f.FailReceiptWith(() =>
        {
            source.Cancel();
            return cancellation = new OperationCanceledException(
                "TASK20_CANCEL_BEFORE_COMMIT", source.Token);
        });

        var error = await Record.ExceptionAsync(() =>
            f.Movements.SaveSingleAsync(f.Single(), source.Token));

        Assert.Same(cancellation, error);
        Assert.True(source.IsCancellationRequested);
        Assert.Equal(before, await f.CountsAsync());
    }

    [Fact]
    public async Task Single_audit_failure_rolls_back_physical_lineage_receipt_and_audit()
    {
        var failure = new FailSecondSaveChanges();
        await using var f = await Task20Fixture.CreateAsync(interceptor: failure);
        var before = await f.CountsAsync();

        var error = await Record.ExceptionAsync(() => f.Movements.SaveSingleAsync(f.Single()));

        Assert.Same(failure.Failure, error);
        Assert.Equal(2, failure.Calls);
        Assert.Equal(before, await f.CountsAsync());
    }

    [Fact]
    public async Task New_single_identical_retry_preserves_original_response_after_later_activity()
    {
        await using var f = await Task20Fixture.CreateAsync();
        var request = f.Single();
        var first = await f.Movements.SaveSingleAsync(request);
        await f.Movements.SaveSingleAsync(f.Single(5));
        var before = await f.CountsAsync();
        var transactionCalls = f.Projection.TransactionCalls;
        var retry = await f.Movements.SaveSingleAsync(request);
        Assert.Equal(before, await f.CountsAsync());
        Assert.Equal(first, retry);
        Assert.Equal(transactionCalls, f.Projection.TransactionCalls);
        Assert.Equal(0, f.Projection.IndependentCalls);
    }

    [Fact]
    public async Task Pre_receipt_single_retry_reports_legacy_replay_unavailable_without_duplicate_or_lineage_rewrite()
    {
        var operationId = Guid.NewGuid();
        await using var f = await Task20Fixture.CreateAsync(legacySingleOperation: operationId);
        var request = f.Single() with { ClientOperationId = operationId };
        // The real schema16 service already proved its complete successful audit
        // before migration. Preserve all values, not just lineage/artifact counts.
        var before = await f.CountsAsync();
        var state = await f.StateAsync();
        SaveSingleMovementResult? returned = null;
        var error = await Record.ExceptionAsync(async () => returned = await f.Movements.SaveSingleAsync(request));
        Assert.Equal(before, await f.CountsAsync());
        Assert.Equal(state, await f.StateAsync());
        Assert.Equal(1, await f.ScalarAsync("SELECT COUNT(*) FROM BinMovements"));
        Assert.Equal(1, await f.ScalarAsync("SELECT COUNT(*) FROM AuditEvents WHERE Action='MOVEMENT_RECORDED' AND Succeeded=1"));
        Assert.Equal(1, await f.ScalarAsync("SELECT COUNT(*) FROM LogicalMovementGenerations WHERE Kind=1"));
        Assert.IsType<SingleMovementReplayUnavailableException>(error);
        Assert.Null(returned);
    }

    [Fact]
    public async Task Missing_native_receipt_fails_closed_instead_of_claiming_legacy_compatibility()
    {
        await using var f = await Task20Fixture.CreateAsync();
        var request = f.Single();
        await f.Movements.SaveSingleAsync(request);
        await f.ExecuteAsync("DELETE FROM SingleMovementResponseReceipts");
        var before = await f.CountsAsync();

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            f.Movements.SaveSingleAsync(request));

        Assert.IsNotType<SingleMovementReplayUnavailableException>(error);
        Assert.Contains("native Single Entry response receipt is missing", error.Message);
        Assert.Equal(before, await f.CountsAsync());
        Assert.Equal(1, await f.ScalarAsync("SELECT COUNT(*) FROM BinMovements"));
        Assert.Equal(1, await f.ScalarAsync("SELECT COUNT(*) FROM AuditEvents"));
    }

    private sealed class FailSecondSaveChanges : SaveChangesInterceptor
    {
        internal Exception Failure { get; } = new InvalidOperationException("TASK20_AUDIT_FAILURE");
        internal int Calls { get; private set; }

        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            Calls++;
            if (Calls == 2) throw Failure;
            return base.SavingChangesAsync(eventData, result, cancellationToken);
        }
    }
}

public sealed class Task20SingleSafetyTests
{
    [Fact]
    public async Task Accepted_schema16_single_migrates_with_its_original_success_audit_and_no_invented_receipt()
    {
        var operationId = Guid.NewGuid();
        // Creation proves the complete normal schema16 command/audit before the
        // migration callback returns. Here verify its resulting legacy identity.
        await using var f = await Task20Fixture.CreateAsync(legacySingleOperation: operationId);
        await using var db = f.Database.CreateDbContext();
        var movement = Assert.Single(await db.BinMovements.ToListAsync());
        Assert.Equal(operationId, movement.ClientOperationId);
        var audit = Assert.Single(await db.AuditEvents.ToListAsync());
        Assert.Equal(("MOVEMENT_RECORDED", movement.Id.ToString(), true),
            (audit.Action, audit.EntityId, audit.Succeeded));
        Assert.Equal(1, await f.ScalarAsync("SELECT COUNT(*) FROM LogicalMovementGenerations WHERE Kind=1 AND GenerationNumber=0"));
        Assert.Equal(0, await f.ScalarAsync("SELECT COUNT(*) FROM MovementCorrectionOperations"));
        Assert.Equal(0, await f.ScalarAsync("SELECT COUNT(*) FROM LogicalMovementPhysicalOutputs"));
        Assert.Equal(0, await f.ScalarAsync("SELECT COUNT(*) FROM SingleMovementResponseReceipts"));
    }

    [Fact]
    public async Task Other_root_commit_before_single_returns_cannot_change_its_committed_result_or_audit()
    {
        var interceptor = new AfterCommit();
        await using var f = await Task20Fixture.CreateAsync(interceptor: interceptor);
        interceptor.Callback = async () =>
        {
            interceptor.Callback = null;
            await f.Database.CreateSingleAsync(Task20Fixture.Today, f.CustomerId, 1, 5);
        };
        var saved = await f.Movements.SaveSingleAsync(f.Single());
        Assert.Equal(7, saved.NewBalance);
        Assert.Equal(1, f.Projection.TransactionCalls);
        Assert.Equal(0, f.Projection.IndependentCalls);
        Assert.Equal(12, await f.ScalarAsync("SELECT SUM(Quantity) FROM BinMovements"));
        await using var db = f.Database.CreateDbContext();
        var audit = await db.AuditEvents.SingleAsync(x => x.Action == "MOVEMENT_RECORDED" && x.EntityId == saved.MovementId.ToString());
        using var payload = JsonDocument.Parse(Assert.IsType<string>(audit.AfterValues));
        Assert.Equal("7 OUT", payload.RootElement.GetProperty("NewPosition").GetString());
    }

    private sealed class AfterCommit : DbTransactionInterceptor
    {
        internal Func<Task>? Callback { get; set; }
        public override async Task TransactionCommittedAsync(DbTransaction transaction,
            TransactionEndEventData eventData, CancellationToken cancellationToken = default)
        {
            if (Callback is not null) await Callback();
        }
    }

    [Fact]
    public async Task Same_single_operation_id_with_changed_payload_still_conflicts()
    {
        await using var f = await Task20Fixture.CreateAsync();
        var request = f.Single();
        await f.Movements.SaveSingleAsync(request);
        var before = await f.CountsAsync();
        var error = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            f.Movements.SaveSingleAsync(request with { Quantity = 8 }));
        Assert.Contains("different movement request", error.Message);
        Assert.Equal(before, await f.CountsAsync());
    }

    [Fact]
    public async Task Corrupt_current_root_prevents_single_attempt_from_persisting_evidence()
    {
        await using var f = await Task20Fixture.CreateAsync();
        await f.Movements.SaveSingleAsync(f.Single());
        await f.ExecuteAsync("UPDATE LogicalMovementBatches SET CurrentGenerationNumber=99");
        var before = await f.CountsAsync();
        var error = await Record.ExceptionAsync(() => f.Movements.SaveSingleAsync(f.Single(3)));
        var invalid = Assert.IsType<InvalidOperationException>(error);
        Assert.StartsWith("INITIAL_MOVEMENT_LINEAGE_SCHEMA17_HEALTH_INVALID", invalid.Message);
        Assert.Equal(before, await f.CountsAsync());
    }
}
