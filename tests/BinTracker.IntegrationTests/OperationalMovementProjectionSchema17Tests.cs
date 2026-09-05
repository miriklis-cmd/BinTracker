using BinTracker.Core;
using BinTracker.Data;
using BinTracker.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
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
        public IOperationalMovementProjectionAuthority Authority { get; }
        public IReadOnlyList<OperationalMovementProjectionScope> ProjectionServiceCalls =>
            services.GetService<IOperationalMovementProjectionAuthority>() is RecordingProjectionAuthority recording
                ? recording.Calls
                : [];
        public bool ProjectionAuthorityIsRegistered =>
            services.GetService<IOperationalMovementProjectionAuthority>() is not null;

        public static async Task<Harness> CreateAsync(bool migrateToSchema17 = true,
            bool enableSchema17Writers = true,
            bool enableProjectionBackedServices = false)
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
                enableProjectionBackedServices);
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

        public async Task SetRootStatusAsync(long rootId, LogicalMovementBatchStatus status) =>
            await ExecuteAsync($"UPDATE LogicalMovementBatches SET Status={(int)status} WHERE Id={rootId};");

        public async Task SetCurrentGenerationAsync(long rootId, int generation) =>
            await ExecuteAsync($"UPDATE LogicalMovementBatches SET CurrentGenerationNumber={generation} WHERE Id={rootId};");

        public async Task AddExcludedAsync(MovementSource source, MovementType direction,
            int quantity, bool importOwned)
        {
            await using var db = new BinTrackerDbContext(
                new DbContextOptionsBuilder<BinTrackerDbContext>().UseSqlite(ConnectionString).Options);
            ImportRun? run = null;
            if (importOwned)
            {
                run = new ImportRun
                {
                    SourceFileName = "projection.xlsx", SourceClientPath = "projection.xlsx",
                    SourceSha256 = new string('a', 64), SourceLength = 1,
                    SourceLastWriteUtc = UtcNow, CutoverDate = new(2026, 9, 1),
                    StartedUtc = UtcNow, CompletedUtc = UtcNow, Status = "Completed",
                    Username = "projection", SessionId = "projection"
                };
                db.Add(run);
                await db.SaveChangesAsync();
            }
            db.Add(new BinMovement
            {
                ClientOperationId = Guid.NewGuid(), MovementDate = new(2026, 9, 1),
                MovementType = direction, Source = source, CustomerId = CustomerId,
                ContainerTypeId = 1, Quantity = quantity, ImportRunId = run?.Id,
                CreatedBy = "projection", CreatedUtc = UtcNow
            });
            await db.SaveChangesAsync();
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
            bool enableProjectionBackedServices)
        {
            var collection = new ServiceCollection();
            collection.AddSingleton<IBusinessClock>(new FixedClock());
            collection.AddSingleton<IUserContext>(new TestUserContext());
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
            : IOperationalMovementProjectionAuthority
        {
            private readonly List<OperationalMovementProjectionScope> calls = [];

            public IReadOnlyList<OperationalMovementProjectionScope> Calls => calls;

            public Task<OperationalMovementProjectionResult> QueryAsync(
                OperationalMovementProjectionScope scope,
                CancellationToken cancellationToken = default)
            {
                calls.Add(scope);
                return inner.QueryAsync(scope, cancellationToken);
            }
        }

        private sealed class TestUserContext : IUserContext
        {
            public string SessionId => "projection-session";
            public int? UserId => 61;
            public string Username => "projection-operator";
            public string DisplayName => "Projection Operator";
            public UserRole Role => UserRole.Operator;
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
