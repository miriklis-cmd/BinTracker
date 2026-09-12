using BinTracker.Core;
using BinTracker.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.Sqlite;
using Xunit;

namespace BinTracker.IntegrationTests;

public sealed class Task20StartupCharacterizationTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Existing_native_health_component_accepts_initial_and_later_generation_without_migration_prerequisite_arguments(bool later)
    {
        await using var f = await Task20Fixture.CreateAsync();
        var root = await f.Database.CreateSingleAsync(Task20Fixture.Today, f.CustomerId, 1, 7);
        if (later)
        {
            var line = Assert.Single(await f.Database.LineIdsAsync(root.RootId));
            await f.Database.MutateAsync(root.RootId, 0,
                MovementMutationRequest.Correct(MovementMutationScope.Individual, [new(line)], "native later generation",
                    quantity: MovementFieldIntent<int>.Selected(8)));
        }
        await using var connection = await f.Database.OpenAsync();
        await using var transaction = (SqliteTransaction)await connection.BeginTransactionAsync();
        await SqliteLineageSchema17Migrator.ValidateStructuralAndCurrentHealthAsync(connection, transaction,
            "TASK20_MISSING_TABLE", "TASK20_INVALID_HEALTH", default);
        Assert.Equal(later ? 1 : 0, await f.ScalarAsync($"SELECT CurrentGenerationNumber FROM LogicalMovementBatches WHERE Id={root.RootId}"));
        // This is component characterization only: it cannot prove composition,
        // physical-identity reacquisition or the absent production startup funnel.
    }

    [Fact]
    public async Task Normal_startup_currently_opens_schema17_with_dormant_writers_and_allows_unrooted_entry()
    {
        await using var f = await Task20Fixture.CreateAsync(enabled: false, projection: false);
        await f.StartExistingAsync();
        var saved = await f.Movements.SaveSingleAsync(f.Single());
        Assert.True(saved.MovementId > 0);
        Assert.Equal(17, await f.ScalarAsync("SELECT Version FROM SchemaVersion"));
        Assert.Equal(0, await f.ScalarAsync("SELECT COUNT(*) FROM LogicalMovementBatches"));
        Assert.Equal(1, await f.ScalarAsync("SELECT COUNT(*) FROM BinMovements"));
    }
}

// TRANSITIONAL DatabaseSetup-facing defect regressions. Final Task20 acceptance
// must retarget/complement them at the real R4 coordinator, including structural
// rejection plus A2/A3/A8-A11/A13-A15 lifecycle paths. Never add a second startup
// authority in DatabaseSetup solely to satisfy this temporary seam.
public sealed class Task20StartupActivationTests
{
    [Theory]
    [InlineData("unique-index")]
    [InlineData("restrict-fk")]
    [InlineData("check")]
    public async Task Schema17_startup_rejects_missing_required_structural_capability(string capability)
    {
        await using var f = await Task20Fixture.CreateAsync();
        switch (capability)
        {
            case "unique-index":
                await f.ExecuteAsync("DROP INDEX IX_LogicalMovementLines_RootMovementId");
                break;
            case "restrict-fk":
                // Corrupt only a disposable fixture's declared FK; healthy rows alone
                // must not conceal an adapter that allows evidence detachment.
                await f.ExecuteAsync("""
                    PRAGMA writable_schema=ON;
                    UPDATE sqlite_master SET sql=replace(sql,
                        'REFERENCES MovementBatches (Id) ON DELETE RESTRICT',
                        'REFERENCES MovementBatches (Id) ON DELETE SET NULL')
                    WHERE name='LogicalMovementBatches';
                    PRAGMA writable_schema=OFF;
                    """);
                Assert.Equal(1, await f.ScalarAsync("SELECT COUNT(*) FROM pragma_foreign_key_list('LogicalMovementBatches') WHERE on_delete='SET NULL'"));
                break;
            case "check":
                await f.ExecuteAsync("""
                    PRAGMA writable_schema=ON;
                    UPDATE sqlite_master SET sql=replace(sql,'CHECK (LineCount > 0)','CHECK (LineCount >= 0)')
                    WHERE name='LogicalMovementBatches';
                    PRAGMA writable_schema=OFF;
                    """);
                Assert.Equal(1, await f.ScalarAsync("SELECT COUNT(*) FROM sqlite_master WHERE name='LogicalMovementBatches' AND sql LIKE '%CHECK (LineCount >= 0)%'"));
                break;
            default: throw new ArgumentOutOfRangeException(nameof(capability));
        }
        var before = await f.StateAsync();
        var error = await Record.ExceptionAsync(f.StartExistingAsync);
        Assert.Equal(before, await f.StateAsync());
        Task20FailureBoundary.AssertDomainFailure(error, ["schema", "structure", "capability"],
            ["invalid", "missing", "unsupported", "mismatch", "required"]);
    }

    [Fact]
    public async Task Future_schema_is_rejected_without_changing_persisted_schema_or_data()
    {
        await using var f = await Task20Fixture.CreateAsync();
        await f.ExecuteAsync("UPDATE SchemaVersion SET Version=18");
        var before = await f.StateAsync();
        var error = await Record.ExceptionAsync(f.StartExistingAsync);
        Assert.Equal(18, await f.ScalarAsync("SELECT Version FROM SchemaVersion"));
        Assert.Equal(before, await f.StateAsync());
        Task20FailureBoundary.AssertDomainFailure(error, ["schema", "database version"],
            ["future", "newer", "unsupported", "not supported"]);
    }

    [Theory]
    [InlineData("invalid-root")]
    [InlineData("unrooted-ordinary")]
    [InlineData("missing-current-generation")]
    public async Task Schema17_startup_rejects_invalid_or_unrooted_operational_evidence(string damage)
    {
        await using var f = await Task20Fixture.CreateAsync();
        await f.Movements.SaveSingleAsync(f.Single());
        if (damage == "invalid-root")
            await f.ExecuteAsync("UPDATE LogicalMovementBatches SET Status=3, StatusReasonCode='TASK20_INVALID'");
        else if (damage == "missing-current-generation")
            await f.ExecuteAsync("UPDATE LogicalMovementBatches SET CurrentGenerationNumber=99");
        else
            await f.Database.InsertUnrootedOrdinaryAsync(f.CustomerId);
        var before = await f.StateAsync();
        var error = await Record.ExceptionAsync(f.StartExistingAsync);
        Assert.Equal(before, await f.StateAsync());
        Task20FailureBoundary.AssertDomainFailure(error, ["lineage", "root", "generation", "unrooted"],
            ["invalid", "missing", "health", "integrity", "unrooted"]);
    }

    [Fact]
    public async Task Normal_schema16_startup_rejects_partial_lineage_before_upgrade_writes()
    {
        await using var f = await Task20Fixture.CreateAsync(schema17: false, enabled: false, projection: false);
        await f.ExecuteAsync("CREATE TABLE LogicalMovementBatches (Id INTEGER PRIMARY KEY)");
        var before = await f.StateAsync();
        var error = await Record.ExceptionAsync(f.StartExistingAsync);
        Assert.Equal(before, await f.StateAsync());
        Assert.Equal(16, await f.ScalarAsync("SELECT Version FROM SchemaVersion"));
        Assert.Equal(0, await f.ScalarAsync("SELECT COUNT(*) FROM LogicalMovementBatches"));
        Task20FailureBoundary.AssertDomainFailure(error, ["schema", "lineage"],
            ["partial", "incomplete", "unexpected", "invalid"]);
    }
}

public sealed class Task20MembershipCharacterizationTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Schema16_batch_delete_currently_detaches_members_when_loaded_or_unloaded(bool loaded)
    {
        await using var f = await Task20Fixture.CreateAsync(schema17: false, enabled: false, projection: false);
        var batch = await f.Movements.SaveBatchAsync(new(Guid.NewGuid(), Task20Fixture.Today,
            MovementType.Out, null, [new(f.CustomerId, 1, 7, null, null)]));
        await using var db = f.Database.CreateDbContext();
        var query = loaded ? db.MovementBatches.Include(x => x.Movements) : db.MovementBatches.AsQueryable();
        db.Remove(await query.SingleAsync(x => x.Id == batch.BatchId));
        await db.SaveChangesAsync();
        Assert.Equal(0, await f.ScalarAsync("SELECT COUNT(*) FROM MovementBatches"));
        Assert.Equal(1, await f.ScalarAsync("SELECT COUNT(*) FROM BinMovements WHERE MovementBatchId IS NULL"));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Schema17_root_restriction_rolls_back_batch_delete_and_any_EF_detachment(bool loaded)
    {
        await using var f = await Task20Fixture.CreateAsync();
        var (_, batchId) = await f.Database.CreateBatchAsync(7);
        await using var db = f.Database.CreateDbContext();
        var query = loaded ? db.MovementBatches.Include(x => x.Movements) : db.MovementBatches.AsQueryable();
        db.Remove(await query.SingleAsync(x => x.Id == batchId));
        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
        Assert.Equal(1, await f.ScalarAsync($"SELECT COUNT(*) FROM MovementBatches WHERE Id={batchId}"));
        Assert.Equal(1, await f.ScalarAsync($"SELECT COUNT(*) FROM BinMovements WHERE MovementBatchId={batchId}"));
    }
}

public sealed class Task20MembershipActivationTests
{
    [Fact]
    public async Task Loaded_schema17_batch_delete_does_not_nullify_tracked_immutable_member_identity()
    {
        await using var f = await Task20Fixture.CreateAsync();
        var (_, batchId) = await f.Database.CreateBatchAsync(7);
        await using var db = f.Database.CreateDbContext();
        var batch = await db.MovementBatches.Include(x => x.Movements).SingleAsync(x => x.Id == batchId);
        var member = Assert.Single(batch.Movements);
        db.Remove(batch);
        db.ChangeTracker.DetectChanges();
        Assert.Equal(batchId, member.MovementBatchId);
    }
}
