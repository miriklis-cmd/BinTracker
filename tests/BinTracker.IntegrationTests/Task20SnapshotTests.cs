using BinTracker.Core;
using BinTracker.Services;
using Xunit;

namespace BinTracker.IntegrationTests;

internal static class Task20SnapshotScenarios
{
    internal static async Task<(OperationalDashboardSummary Before, OperationalDashboardSummary Mixed, OperationalDashboardSummary Fresh)> DashboardAsync()
    {
        await using var f = await Task20Fixture.CreateAsync();
        await f.Database.AddExcludedAsync(MovementSource.Adjustment, MovementType.Out, 30, importOwned: false, movementDate: Task20Fixture.Today);
        await f.Database.SetAttentionThresholdAsync(20);
        var before = await f.Movements.GetDashboardSummaryAsync(Task20Fixture.Today);
        f.Projection.AfterRead = async () =>
        {
            f.Projection.AfterRead = null;
            // One committed competing transaction changes both result-affecting
            // inputs. Before and after each require attention; a mixed read does not.
            await f.ExecuteAsync("""
                BEGIN IMMEDIATE;
                UPDATE ApplicationSettings SET AttentionQuantityThreshold=50 WHERE Id=1;
                UPDATE BinMovements SET Quantity=60 WHERE Source=3;
                COMMIT;
                """);
        };
        var mixed = await f.Movements.GetDashboardSummaryAsync(Task20Fixture.Today);
        var fresh = await f.Movements.GetDashboardSummaryAsync(Task20Fixture.Today);
        return (before, mixed, fresh);
    }

    internal static async Task<(IReadOnlyList<CustomerListRow> Before, IReadOnlyList<CustomerListRow> Mixed, IReadOnlyList<CustomerListRow> Fresh)> CustomerAsync()
    {
        await using var f = await Task20Fixture.CreateAsync();
        await f.Database.AddExcludedAsync(MovementSource.Adjustment, MovementType.Out, 7, importOwned: false, movementDate: Task20Fixture.Today);
        var before = await f.Customers.SearchAsync("PROJ-A", false);
        f.Projection.BeforeRead = async () =>
        {
            f.Projection.BeforeRead = null;
            await f.ExecuteAsync($"""
                BEGIN IMMEDIATE;
                UPDATE Customers SET Name='Changed', IsActive=0 WHERE Id={f.CustomerId};
                UPDATE BinMovements SET Quantity=11 WHERE Source=3;
                COMMIT;
                """);
        };
        var mixed = await f.Customers.SearchAsync("PROJ-A", false);
        var fresh = await f.Customers.SearchAsync("PROJ-A", false);
        return (before, mixed, fresh);
    }

    internal static async Task<(MovementCustomerSummary Before, MovementCustomerSummary Mixed, MovementCustomerSummary Fresh)> ContainerAsync()
    {
        await using var f = await Task20Fixture.CreateAsync();
        await f.Database.AddExcludedAsync(MovementSource.Adjustment, MovementType.Out, 7, importOwned: false, movementDate: Task20Fixture.Today);
        var before = Assert.IsType<MovementCustomerSummary>(await f.Movements.GetCustomerSummaryByCodeAsync("PROJ-A"));
        f.Projection.BeforeRead = async () =>
        {
            f.Projection.BeforeRead = null;
            await f.ExecuteAsync("""
                BEGIN IMMEDIATE;
                UPDATE ContainerTypes SET Name='Changed Blue', IsActive=0, DisplayOrder=99 WHERE Id=1;
                UPDATE BinMovements SET Quantity=11 WHERE Source=3;
                COMMIT;
                """);
        };
        var mixed = Assert.IsType<MovementCustomerSummary>(await f.Movements.GetCustomerSummaryByCodeAsync("PROJ-A"));
        var fresh = Assert.IsType<MovementCustomerSummary>(await f.Movements.GetCustomerSummaryByCodeAsync("PROJ-A"));
        return (before, mixed, fresh);
    }

    internal static async Task<(ImportReplacementComparison Before, ImportReplacementComparison? Mixed, Exception? MixedError, Exception? FreshError)> ImportAsync()
    {
        await using var f = await Task20Fixture.CreateAsync(role: UserRole.Administrator);
        var cutover = Task20Fixture.Today;
        await f.Database.CreateSingleAsync(cutover.AddDays(-1), f.CustomerId, 1, 7);
        var runId = await f.Database.CreatePreviousImportRunAsync(cutover, cutover.AddDays(-1));
        var request = OperationalMovementProjectionSchema17Tests.ReplacementRequest(f.Database, runId, cutover);
        var before = await f.Imports.CompareReplacementAsync(request);
        f.Projection.BeforeRead = async () =>
        {
            f.Projection.BeforeRead = null;
            await f.ExecuteAsync($"""
                BEGIN IMMEDIATE;
                UPDATE ImportRuns SET CurrentCutoverDate=NULL, Status='Replaced' WHERE Id={runId};
                DELETE FROM BinMovements WHERE ImportRunId={runId};
                COMMIT;
                """);
        };
        ImportReplacementComparison? mixed;
        Exception? mixedError = null;
        try { mixed = await f.Imports.CompareReplacementAsync(request); }
        catch (InvalidOperationException unavailable) when (unavailable.Message.Contains("no longer available", StringComparison.Ordinal))
        {
            // A revalidated stale request is a controlled alternative to returning
            // the original consistent snapshot. Other failures remain unexpected.
            mixed = null;
            mixedError = unavailable;
        }
        var error = await Record.ExceptionAsync(() => f.Imports.CompareReplacementAsync(request));
        return (before, mixed, mixedError, error);
    }

    internal static void AssertCoherent<T>(T before, T observed, T after)
    {
        // These are result DTOs, not source-text checks. Serializing their complete
        // value shape gives structural equality for nested IReadOnlyList records
        // and useful failure values; record equality alone compares list references.
        var oldState = System.Text.Json.JsonSerializer.Serialize(before);
        var actual = System.Text.Json.JsonSerializer.Serialize(observed);
        var newState = System.Text.Json.JsonSerializer.Serialize(after);
        Assert.True(actual == oldState || actual == newState,
            $"Hybrid result. BEFORE={oldState}; OBSERVED={actual}; AFTER={newState}");
    }
}

public sealed class Task20SnapshotCharacterizationTests
{
    [Fact]
    public async Task Dashboard_currently_combines_old_position_with_new_attention_threshold()
    {
        var (before, mixed, fresh) = await Task20SnapshotScenarios.DashboardAsync();
        Assert.Equal(new(0, 30, 30, 1), before);
        Assert.Equal(new(0, 30, 30, 0), mixed);
        Assert.Equal(new(0, 60, 60, 1), fresh);
    }

    [Fact]
    public async Task Customer_search_currently_combines_old_active_identity_with_new_position()
    {
        var (before, rows, fresh) = await Task20SnapshotScenarios.CustomerAsync();
        var mixed = Assert.Single(rows);
        Assert.True(mixed.IsActive);
        Assert.Equal("Projection A", mixed.Name);
        Assert.Equal(11, mixed.NetBalance);
        Assert.Equal(Assert.Single(before) with { NetBalance = 11 }, mixed);
        Assert.Empty(fresh);
    }

    [Fact]
    public async Task Customer_summary_currently_combines_old_container_metadata_with_new_position()
    {
        var (before, mixed, fresh) = await Task20SnapshotScenarios.ContainerAsync();
        Assert.Equal(7, Assert.Single(before.Balances, x => x.ContainerTypeId == 1).Balance);
        var blue = Assert.Single(mixed.Balances, x => x.ContainerTypeId == 1);
        Assert.Equal("Blue Bin", blue.ContainerType);
        Assert.Equal(11, blue.Balance);
        Assert.DoesNotContain(fresh.Balances, x => x.ContainerTypeId == 1);
    }

    [Fact]
    public async Task Replacement_comparison_currently_retains_previous_run_evidence_after_concurrent_replacement()
    {
        var (before, mixed, mixedError, error) = await Task20SnapshotScenarios.ImportAsync();
        Assert.Null(mixedError);
        Assert.Equal(15, Assert.Single(before.Differences).ProposedNetEffect);
        Assert.NotNull(mixed);
        Assert.Equal(26, Assert.Single(mixed.Differences).ProposedNetEffect);
        Assert.Equal(2, mixed.PreviousMovementCount);
        var unavailable = Assert.IsType<InvalidOperationException>(error);
        Assert.Contains("no longer available", unavailable.Message);
    }
}

public sealed class Task20SnapshotActivationTests
{
    [Fact]
    public async Task Replacement_comparison_previous_run_evidence_and_projection_share_one_snapshot()
    {
        var (before, result, error, afterError) = await Task20SnapshotScenarios.ImportAsync();
        // The actual post-change request has no current previous run. Its existing
        // service diagnostic is established; no arbitrary failure counts as stale.
        var stale = Assert.IsType<InvalidOperationException>(afterError);
        Assert.Equal("The previous completed Import Run is no longer available.", stale.Message);
        if (error is not null)
        {
            Assert.Null(result);
            Assert.Equal(stale.Message, error.Message);
        }
        else
        {
            Assert.NotNull(result);
            // Include run identity/cutover/counts and every previous/proposed net
            // coordinate. File/actor metadata cannot affect comparison arithmetic.
            var expected = new { before.PreviousRun.ImportRunId, before.PreviousRun.CutoverDate,
                before.PreviousRun.MovementCount, before.PreviousMovementCount,
                before.ProposedMovementCount, before.ChangedPositionCount, before.Differences };
            var observed = new { result.PreviousRun.ImportRunId, result.PreviousRun.CutoverDate,
                result.PreviousRun.MovementCount, result.PreviousMovementCount,
                result.ProposedMovementCount, result.ChangedPositionCount, result.Differences };
            // A successful comparison has only the complete BEFORE alternative;
            // the complete AFTER alternative was checked as stale above.
            var oldState = System.Text.Json.JsonSerializer.Serialize(expected);
            var actual = System.Text.Json.JsonSerializer.Serialize(observed);
            Assert.True(oldState == actual,
                $"Hybrid comparison. BEFORE={oldState}; OBSERVED={actual}; AFTER=controlled stale previous-run rejection");
        }
    }

    [Fact]
    public async Task Dashboard_attention_and_position_share_one_snapshot()
    {
        var (before, result, after) = await Task20SnapshotScenarios.DashboardAsync();
        Assert.Equal(new(0, 30, 30, 1), before);
        Assert.Equal(new(0, 60, 60, 1), after);
        Task20SnapshotScenarios.AssertCoherent(before, result, after);
    }

    [Fact]
    public async Task Customer_search_visible_active_row_and_position_share_one_snapshot()
    {
        var (before, result, after) = await Task20SnapshotScenarios.CustomerAsync();
        var oldRow = Assert.Single(before);
        Assert.Equal(("PROJ-A", "Projection A", true, 7),
            (oldRow.CustomerCode, oldRow.Name, oldRow.IsActive, oldRow.NetBalance));
        Assert.Empty(after); // Proven complete after-state: inactive customer excluded.
        Task20SnapshotScenarios.AssertCoherent(before, result, after);
    }

    [Fact]
    public async Task Customer_summary_visible_container_and_position_share_one_snapshot()
    {
        var (before, result, after) = await Task20SnapshotScenarios.ContainerAsync();
        Assert.Equal(new(1, "Blue Bin", 7), Assert.Single(before.Balances, x => x.ContainerTypeId == 1));
        Assert.DoesNotContain(after.Balances, x => x.ContainerTypeId == 1);
        // Includes customer identity and every visible container/name/position in
        // order. Disappearance is accepted only as part of the full proven after DTO.
        Task20SnapshotScenarios.AssertCoherent(before, result, after);
    }
}
