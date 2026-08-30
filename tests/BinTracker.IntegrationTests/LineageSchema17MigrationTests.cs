using System.Text.Json;
using BinTracker.Core;
using BinTracker.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace BinTracker.IntegrationTests;

public sealed class LineageSchema17MigrationTests
{
    [Fact]
    public async Task Normal_startup_remains_schema_16_without_lineage_tables()
    {
        await using var fixture = await MigrationFixture.CreateAsync(seed: false);
        await using var connection = await fixture.OpenAsync();

        Assert.Equal(16L, await ScalarAsync(connection, "SELECT Version FROM SchemaVersion WHERE Id=1;"));
        Assert.Equal(0L, await ScalarAsync(connection,
            "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name LIKE 'LogicalMovement%';"));
        Assert.Equal(16, DatabaseSetup.LatestSchemaVersion);
    }

    [Fact]
    public async Task Deterministic_schema_16_graph_migrates_to_complete_schema_17_baseline()
    {
        await using var fixture = await MigrationFixture.CreateAsync(seed: true);
        var result = await fixture.MigrateAsync();
        await using var connection = await fixture.OpenAsync();

        Assert.Equal(LineageSchema17MigrationOutcome.Migrated, result.Outcome);
        Assert.Equal(3, result.Postflight.Roots);
        Assert.Equal(4, result.Postflight.Lines);
        Assert.Equal(4, result.Postflight.GenerationLines);
        Assert.Equal(11, result.Postflight.LedgerLinks);
        Assert.Equal(0, result.Postflight.HistoricalPhysicalOutputs);
        Assert.Equal(3, result.Postflight.StrongLegacyAuditLinks);
        Assert.Equal(17L, await ScalarAsync(connection, "SELECT Version FROM SchemaVersion WHERE Id=1;"));
        Assert.Equal(0L, await ScalarAsync(connection, "SELECT COUNT(*) FROM LogicalMovementPhysicalOutputs;"));
        Assert.Equal(2L, await ScalarAsync(connection,
            "SELECT COUNT(*) FROM LogicalMovementBatches WHERE RootMovementBatchId IS NOT NULL AND Status=1 AND CurrentGenerationNumber=0;"));
        Assert.Equal(1L, await ScalarAsync(connection,
            "SELECT COUNT(*) FROM LogicalMovementBatches WHERE RootMovementBatchId IS NULL AND Status=1 AND CurrentGenerationNumber=0;"));
        Assert.Equal(0L, await ScalarAsync(connection, """
            SELECT COUNT(*) FROM LogicalMovementBatches b
            WHERE b.LineCount<>(SELECT COUNT(*) FROM LogicalMovementLines l WHERE l.LogicalMovementBatchId=b.Id);
            """));
        Assert.Equal(0L, await ScalarAsync(connection, """
            SELECT COUNT(*) FROM LogicalMovementLines l
            WHERE l.OriginalDisplayOrdinal<>(
                SELECT COUNT(*) FROM LogicalMovementLines earlier
                WHERE earlier.LogicalMovementBatchId=l.LogicalMovementBatchId
                  AND earlier.RootMovementId<l.RootMovementId);
            """));
        Assert.Equal(3L, await ScalarAsync(connection, """
            SELECT COUNT(*) FROM LogicalMovementGenerations
            WHERE GenerationNumber=0 AND PreviousGenerationNumber IS NULL
              AND MovementCorrectionOperationId IS NULL AND Kind=1;
            """));
        Assert.Equal(4L, await ScalarAsync(connection, """
            SELECT COUNT(*) FROM LogicalMovementGenerationLines
            WHERE Action=1 AND AppliedFieldMask=0 AND PreviousGenerationLineId IS NULL;
            """));
        Assert.Equal(0L, await ScalarAsync(connection, """
            SELECT COUNT(*) FROM MovementCorrectionOperations
            WHERE RequestJson IS NOT NULL OR RequestSchemaVersion IS NOT NULL
               OR ExpectedGenerationNumber IS NOT NULL OR ResultGenerationNumber IS NOT NULL;
            """));
        Assert.Equal(3L, await ScalarAsync(connection,
            "SELECT COUNT(*) FROM MovementCorrectionOperations WHERE LogicalMovementBatchId IS NOT NULL;"));
        Assert.Equal(3L, await ScalarAsync(connection, """
            SELECT COUNT(*) FROM MovementCorrectionOperations
            WHERE ClientOperationId IS NOT NULL AND length(RequestFingerprint)=64
              AND OriginalBatchId IS NULL AND ReplacementBatchId IS NULL OR
                  OriginalBatchId=2 AND ReplacementBatchId=3;
            """));
        Assert.Equal(6L, await ScalarAsync(connection,
            "SELECT COUNT(*) FROM LogicalMovementLedgerLinks WHERE LegacyMovementCorrectionLineId IS NOT NULL;"));
        Assert.Equal(4L, await ScalarAsync(connection,
            "SELECT COUNT(DISTINCT Role) FROM LogicalMovementLedgerLinks;"));
        Assert.Equal(1L, await ScalarAsync(connection, """
            SELECT COUNT(*) FROM LogicalMovementGenerationLines gl
            JOIN LogicalMovementLines l ON l.Id=gl.LogicalMovementLineId
            WHERE l.RootMovementId=4 AND gl.State=0 AND gl.ResultEffectiveMovementId=12;
            """));
        Assert.Equal(3L, await ScalarAsync(connection,
            "SELECT COUNT(*) FROM LogicalMovementGenerationLines WHERE State=0;"));
        Assert.Equal(1L, await ScalarAsync(connection,
            "SELECT COUNT(*) FROM LogicalMovementGenerationLines WHERE State=1;"));
        Assert.Equal(0L, await ScalarAsync(connection, """
            SELECT COUNT(*) FROM LogicalMovementLedgerLinks ll
            JOIN LogicalMovementGenerationLines gl ON gl.Id=ll.IntroducedByGenerationLineId
            WHERE ll.LogicalMovementBatchId<>gl.LogicalMovementBatchId OR ll.LogicalMovementLineId<>gl.LogicalMovementLineId;
            """));
    }

    [Fact]
    public async Task Batch_30_shape_remains_one_root_with_active_and_reversed_lines()
    {
        await using var fixture = await MigrationFixture.CreateAsync(seed: true);
        await fixture.MigrateAsync();
        await using var connection = await fixture.OpenAsync();

        Assert.Equal(1L, await ScalarAsync(connection,
            "SELECT COUNT(*) FROM LogicalMovementBatches WHERE RootMovementBatchId=1 AND LineCount=2;"));
        Assert.Equal(1L, await ScalarAsync(connection, """
            SELECT COUNT(*) FROM LogicalMovementGenerationLines gl
            JOIN LogicalMovementLines l ON l.Id=gl.LogicalMovementLineId
            JOIN LogicalMovementBatches b ON b.Id=l.LogicalMovementBatchId
            WHERE b.RootMovementBatchId=1 AND gl.State=0 AND gl.ResultEffectiveMovementId IS NOT NULL;
            """));
        Assert.Equal(1L, await ScalarAsync(connection, """
            SELECT COUNT(*) FROM LogicalMovementGenerationLines gl
            JOIN LogicalMovementLines l ON l.Id=gl.LogicalMovementLineId
            JOIN LogicalMovementBatches b ON b.Id=l.LogicalMovementBatchId
            WHERE b.RootMovementBatchId=1 AND gl.State=1
              AND gl.LastEffectiveMovementId IS NOT NULL AND gl.TerminalReversalMovementId IS NOT NULL;
            """));
    }

    [Fact]
    public async Task Legacy_audit_mapping_requires_one_unique_complete_structured_match()
    {
        await using var fixture = await MigrationFixture.CreateAsync(seed: true);
        await using (var connection = await fixture.OpenAsync())
        {
            await ExecuteAsync(connection, """
                INSERT INTO AuditEvents
                    (TimestampUtc, UserId, Username, Action, EntityType, EntityId, Description,
                     BeforeValues, AfterValues, ComputerName, SessionId, Succeeded,
                     RequiresAdministratorReview)
                SELECT TimestampUtc, UserId, Username, Action, EntityType, EntityId, 'duplicate strong match',
                       BeforeValues, AfterValues, ComputerName, SessionId, Succeeded,
                       RequiresAdministratorReview
                FROM AuditEvents WHERE Action='MOVEMENT_CORRECTED' LIMIT 1;
                UPDATE AuditEvents SET AfterValues='not-json' WHERE Action='MOVEMENT_BATCH_CORRECTED';
                """);
        }

        var result = await fixture.MigrateAsync(refreshPrerequisites: true);
        Assert.Equal(1, result.Postflight.StrongLegacyAuditLinks);
        await using var verify = await fixture.OpenAsync();
        Assert.Equal(1L, await ScalarAsync(verify,
            "SELECT COUNT(*) FROM AuditEvents WHERE MovementCorrectionOperationId IS NOT NULL;"));
    }

    [Fact]
    public async Task Schema_constraints_reject_invalid_enum_selector_and_duplicate_identity()
    {
        await using var fixture = await MigrationFixture.CreateAsync(seed: true);
        await fixture.MigrateAsync();
        await using var connection = await fixture.OpenAsync();

        await Assert.ThrowsAsync<SqliteException>(() => ExecuteAsync(connection, """
            INSERT INTO MovementCorrectionOperations
                (ClientOperationId, RequestFingerprint, Kind, Reason, ActorUserId, ActorUsername, CreatedUtc)
            VALUES ('11111111-1111-1111-1111-111111111111','x',9,'bad',1,'x',CURRENT_TIMESTAMP);
            """));
        await Assert.ThrowsAsync<SqliteException>(() => ExecuteAsync(connection, """
            INSERT INTO LogicalMovementPhysicalOutputs
                (MovementBatchId,LogicalMovementBatchId,LogicalMovementGenerationId,LegacyMovementCorrectionOperationId,CreatedUtc)
            SELECT 1,Id,NULL,NULL,CURRENT_TIMESTAMP FROM LogicalMovementBatches LIMIT 1;
            """));
        await Assert.ThrowsAsync<SqliteException>(() => ExecuteAsync(connection, """
            INSERT INTO LogicalMovementBatches
                (RootMovementBatchId,Status,CurrentGenerationNumber,LineCount,CreatedUtc)
            SELECT RootMovementBatchId,1,0,1,CURRENT_TIMESTAMP
            FROM LogicalMovementBatches WHERE RootMovementBatchId IS NOT NULL LIMIT 1;
            """));

        await ExecuteAsync(connection, """
            INSERT INTO MovementBatches (MovementDate,MovementType,Source,CreatedUtc,IsReversed)
            VALUES ('2026-08-30',0,1,CURRENT_TIMESTAMP,0);
            INSERT INTO LogicalMovementPhysicalOutputs
                (MovementBatchId,LogicalMovementBatchId,LogicalMovementGenerationId,
                 LegacyMovementCorrectionOperationId,CreatedUtc)
            SELECT last_insert_rowid(),g.LogicalMovementBatchId,g.Id,NULL,CURRENT_TIMESTAMP
            FROM LogicalMovementGenerations g ORDER BY g.Id LIMIT 1;
            """);
        Assert.Equal(1L, await ScalarAsync(connection,
            "SELECT COUNT(*) FROM LogicalMovementPhysicalOutputs WHERE LogicalMovementGenerationId IS NOT NULL;"));
    }

    [Fact]
    public async Task Schema_17_contains_required_tables_indexes_and_restrict_membership_fk()
    {
        await using var fixture = await MigrationFixture.CreateAsync(seed: true);
        await fixture.MigrateAsync();
        await using var connection = await fixture.OpenAsync();

        Assert.Equal(6L, await ScalarAsync(connection, """
            SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name IN
              ('LogicalMovementBatches','LogicalMovementLines','LogicalMovementGenerations',
               'LogicalMovementGenerationLines','LogicalMovementLedgerLinks','LogicalMovementPhysicalOutputs');
            """));
        Assert.Equal(1L, await ScalarAsync(connection, """
            SELECT COUNT(*) FROM pragma_table_info('AuditEvents')
            WHERE name='MovementCorrectionOperationId';
            """));
        Assert.Equal(1L, await ScalarAsync(connection, """
            SELECT COUNT(*) FROM sqlite_master
            WHERE type='index' AND name='IX_AuditEvents_MovementCorrectionOperationId';
            """));
        Assert.Equal(1L, await ScalarAsync(connection, """
            SELECT COUNT(*) FROM pragma_foreign_key_list('BinMovements')
            WHERE "table"='MovementBatches' AND "from"='MovementBatchId' AND on_delete='RESTRICT';
            """));
        Assert.Equal(0L, await ForeignKeyViolationsAsync(connection));
    }

    [Fact]
    public async Task Postflight_fails_closed_when_pointer_ownership_is_tampered()
    {
        await using var fixture = await MigrationFixture.CreateAsync(seed: true);
        await fixture.MigrateAsync();
        await using var connection = await fixture.OpenAsync();
        await ExecuteAsync(connection, """
            PRAGMA ignore_check_constraints=ON;
            UPDATE LogicalMovementGenerationLines
            SET ResultEffectiveMovementId=7
            WHERE State=0 AND LogicalMovementLineId=(SELECT Id FROM LogicalMovementLines WHERE RootMovementId=1);
            """);

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            SqliteLineageSchema17Migrator.ValidatePostflightAsync(connection, null));

        Assert.Equal("LINEAGE_POSTFLIGHT_INVARIANT_FAILURE", error.Message);
    }

    [Fact]
    public async Task Movement_batch_membership_is_preserved_and_cannot_be_detached_by_batch_delete()
    {
        await using var fixture = await MigrationFixture.CreateAsync(seed: true);
        await fixture.MigrateAsync();
        await using var connection = await fixture.OpenAsync();

        Assert.Equal(4L, await ScalarAsync(connection,
            "SELECT COUNT(*) FROM BinMovements WHERE MovementBatchId IS NOT NULL;"));
        await Assert.ThrowsAsync<SqliteException>(() => ExecuteAsync(connection,
            "DELETE FROM MovementBatches WHERE Id=1;"));
        Assert.Equal(2L, await ScalarAsync(connection,
            "SELECT COUNT(*) FROM BinMovements WHERE MovementBatchId=1;"));
        Assert.Equal(1L, await ScalarAsync(connection,
            "SELECT COUNT(*) FROM BinMovements WHERE Source=2 AND ImportRunId IS NOT NULL;"));
        await ExecuteAsync(connection, "DELETE FROM BinMovements WHERE Source=2 AND ImportRunId IS NOT NULL;");
        Assert.Equal(0L, await ScalarAsync(connection,
            "SELECT COUNT(*) FROM BinMovements WHERE Source=2 AND ImportRunId IS NOT NULL;"));
    }

    [Fact]
    public async Task Whole_database_reset_remains_possible_after_schema_17_connections_close()
    {
        var fixture = await MigrationFixture.CreateAsync(seed: true);
        await fixture.MigrateAsync();
        var root = fixture.RootPath;

        await fixture.DisposeAsync();

        Assert.False(Directory.Exists(root));
    }

    [Fact]
    public async Task Completed_schema_17_is_recognised_without_duplicate_backfill()
    {
        await using var fixture = await MigrationFixture.CreateAsync(seed: true);
        var prerequisites = await fixture.CreatePrerequisitesAsync();
        LineageSchema17MigrationResult first;
        LineageSchema17MigrationResult second;
        try
        {
            var migrator = new SqliteLineageSchema17Migrator();
            first = await migrator.MigrateAsync(prerequisites);
            second = await migrator.MigrateAsync(prerequisites);
        }
        finally
        {
            prerequisites.UpgradeLease.Dispose();
        }

        Assert.Equal(LineageSchema17MigrationOutcome.Migrated, first.Outcome);
        Assert.Equal(LineageSchema17MigrationOutcome.AlreadyComplete, second.Outcome);
        Assert.Equal(first.Postflight, second.Postflight);
    }

    [Fact]
    public async Task Partial_lineage_schema_is_rejected_before_writes()
    {
        await using var fixture = await MigrationFixture.CreateAsync(seed: true);
        await using (var connection = await fixture.OpenAsync())
            await ExecuteAsync(connection, "CREATE TABLE LogicalMovementBatches (Id INTEGER PRIMARY KEY);");

        await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.MigrateAsync(refreshPrerequisites: true));
        await using var verify = await fixture.OpenAsync();
        Assert.Equal(16L, await ScalarAsync(verify, "SELECT Version FROM SchemaVersion WHERE Id=1;"));
        Assert.Equal(1L, await ScalarAsync(verify,
            "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='LogicalMovementBatches';"));
    }

    [Fact]
    public async Task Wrong_or_unverified_prerequisite_fails_before_schema_writes()
    {
        await using var fixture = await MigrationFixture.CreateAsync(seed: true);
        var prerequisites = await fixture.CreatePrerequisitesAsync();
        var bad = prerequisites with
        {
            Preflight = prerequisites.Preflight with { StructuralFingerprint = "BAD" }
        };

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new SqliteLineageSchema17Migrator().MigrateAsync(bad));
        await using var verify = await fixture.OpenAsync();
        Assert.Equal(16L, await ScalarAsync(verify, "SELECT Version FROM SchemaVersion WHERE Id=1;"));
        Assert.Equal(0L, await ScalarAsync(verify,
            "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name LIKE 'LogicalMovement%';"));
        prerequisites.UpgradeLease.Dispose();
    }

    [Fact]
    public async Task Backup_tampered_after_verification_fails_before_schema_writes()
    {
        await using var fixture = await MigrationFixture.CreateAsync(seed: true);
        var prerequisites = await fixture.CreatePrerequisitesAsync();
        await File.AppendAllTextAsync(prerequisites.VerifiedBackup.BackupPath, "tamper");

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new SqliteLineageSchema17Migrator().MigrateAsync(prerequisites));

        Assert.Equal("LINEAGE_BACKUP_REVERIFICATION_FAILED", error.Message);
        prerequisites.UpgradeLease.Dispose();
        await using var verify = await fixture.OpenAsync();
        Assert.Equal(16L, await ScalarAsync(verify, "SELECT Version FROM SchemaVersion WHERE Id=1;"));
        Assert.Equal(0L, await ScalarAsync(verify,
            "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name LIKE 'LogicalMovement%';"));
    }

    [Fact]
    public async Task Source_schema_changed_after_verified_prerequisites_fails_before_writes()
    {
        await using var fixture = await MigrationFixture.CreateAsync(seed: true);
        var prerequisites = await fixture.CreatePrerequisitesAsync();
        await using (var connection = await fixture.OpenAsync())
            await ExecuteAsync(connection, "UPDATE SchemaVersion SET Version=15 WHERE Id=1;");

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new SqliteLineageSchema17Migrator().MigrateAsync(prerequisites));

        Assert.Equal("LINEAGE_MIGRATION_SOURCE_SCHEMA_UNSUPPORTED", error.Message);
        prerequisites.UpgradeLease.Dispose();
        await using var verify = await fixture.OpenAsync();
        Assert.Equal(15L, await ScalarAsync(verify, "SELECT Version FROM SchemaVersion WHERE Id=1;"));
        Assert.Equal(0L, await ScalarAsync(verify,
            "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name LIKE 'LogicalMovement%';"));
    }

    public static IEnumerable<object[]> FailureCheckpoints() =>
        Enum.GetValues<LineageSchema17MigrationCheckpoint>().Select(x => new object[] { x });

    [Theory]
    [MemberData(nameof(FailureCheckpoints))]
    public async Task Every_failure_checkpoint_rolls_back_to_valid_schema_16(
        LineageSchema17MigrationCheckpoint checkpoint)
    {
        await using var fixture = await MigrationFixture.CreateAsync(seed: true);
        var backup = await fixture.CreatePrerequisitesAsync();
        var migrator = new SqliteLineageSchema17Migrator(
            failureInjector: new ThrowAtCheckpoint(checkpoint));

        await Assert.ThrowsAsync<InjectedMigrationFailure>(() => migrator.MigrateAsync(backup));
        Assert.True(File.Exists(backup.VerifiedBackup.BackupPath));
        backup.UpgradeLease.Dispose();
        await using var verify = await fixture.OpenAsync();
        Assert.Equal(16L, await ScalarAsync(verify, "SELECT Version FROM SchemaVersion WHERE Id=1;"));
        Assert.Equal(0L, await ScalarAsync(verify,
            "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name LIKE 'LogicalMovement%';"));
        Assert.Equal(0L, await ForeignKeyViolationsAsync(verify));
    }

    [Fact]
    public async Task Invalid_legacy_graph_is_rejected_by_preflight_and_never_migrated()
    {
        await using var fixture = await MigrationFixture.CreateAsync(seed: true);
        await using (var connection = await fixture.OpenAsync())
            await ExecuteAsync(connection, "UPDATE BinMovements SET CorrectedByMovementId=NULL WHERE Id=4;");

        var preflight = await new SqliteLineageMigrationPreflight().InspectAsync(fixture.DatabasePath);
        Assert.Equal(LineagePreflightClassification.Invalid, preflight.Classification);
        Assert.Contains(preflight.Issues,
            x => x.ReasonCode == LineagePreflightReasonCode.CorrectedByRelationshipMismatch);
        await using var verify = await fixture.OpenAsync();
        Assert.Equal(16L, await ScalarAsync(verify, "SELECT Version FROM SchemaVersion WHERE Id=1;"));
    }

    [Theory]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(-1)]
    [InlineData(99)]
    public async Task Unsupported_schema_16_operation_kind_blocks_before_schema_mutation(int unsupportedKind)
    {
        await using var fixture = await MigrationFixture.CreateAsync(seed: true);
        var prerequisites = await fixture.CreatePrerequisitesAsync();
        await using (var connection = await fixture.OpenAsync())
            await ExecuteAsync(connection,
                $"PRAGMA ignore_check_constraints=ON; UPDATE MovementCorrectionOperations SET Kind={unsupportedKind} WHERE Id=1;");

        var preflight = await new SqliteLineageMigrationPreflight().InspectAsync(fixture.DatabasePath);
        Assert.Equal(LineagePreflightClassification.GlobalBlocker, preflight.Classification);
        Assert.Contains(preflight.Issues,
            x => x.ReasonCode == LineagePreflightReasonCode.UnsupportedCorrectionKind);

        var rejectedPrerequisites = prerequisites with { Preflight = preflight };
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new SqliteLineageSchema17Migrator().MigrateAsync(rejectedPrerequisites));

        await using var verify = await fixture.OpenAsync();
        Assert.Equal(16L, await ScalarAsync(verify, "SELECT Version FROM SchemaVersion WHERE Id=1;"));
        Assert.Equal(0L, await ScalarAsync(verify,
            "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name LIKE 'LogicalMovement%';"));
        Assert.True(File.Exists(prerequisites.VerifiedBackup.BackupPath));
        Assert.Equal(unsupportedKind, await ScalarAsync(verify,
            "SELECT Kind FROM MovementCorrectionOperations WHERE Id=1;"));
        prerequisites.UpgradeLease.Dispose();
    }

    private sealed class ThrowAtCheckpoint(LineageSchema17MigrationCheckpoint requested)
        : ILineageSchema17FailureInjector
    {
        public void ThrowIfRequested(LineageSchema17MigrationCheckpoint checkpoint)
        {
            if (checkpoint == requested) throw new InjectedMigrationFailure(checkpoint);
        }
    }

    private sealed class InjectedMigrationFailure(LineageSchema17MigrationCheckpoint checkpoint)
        : Exception(checkpoint.ToString());

    private sealed class MigrationFixture : IAsyncDisposable
    {
        private readonly string root;
        public string DatabasePath { get; }
        public string RootPath => root;
        private string RecoveryPath => Path.Combine(root, "recovery");

        private MigrationFixture(string root)
        {
            this.root = root;
            DatabasePath = Path.Combine(root, "db", "BinTracker.db");
        }

        public static async Task<MigrationFixture> CreateAsync(bool seed)
        {
            var fixture = new MigrationFixture(Path.Combine(Path.GetTempPath(), $"BinTracker-v17-{Guid.NewGuid():N}"));
            Directory.CreateDirectory(Path.GetDirectoryName(fixture.DatabasePath)!);
            await using var db = fixture.CreateContext();
            await DatabaseSetup.InitializeSqliteAsync(db);
            if (seed) await SeedAsync(db);
            return fixture;
        }

        public async Task<LineageSchema17MigrationResult> MigrateAsync(bool refreshPrerequisites = false)
        {
            var prerequisites = await CreatePrerequisitesAsync(refreshPrerequisites);
            try
            {
                return await new SqliteLineageSchema17Migrator().MigrateAsync(prerequisites);
            }
            finally
            {
                prerequisites.UpgradeLease.Dispose();
            }
        }

        public async Task<LineageSchema17MigrationPrerequisites> CreatePrerequisitesAsync(bool refresh = false)
        {
            _ = refresh;
            var gate = new WindowsFileDatabaseUpgradeGate(
                Path.Combine(root, "locks"), new NoConflictProbe());
            var lease = gate.AcquireUpgrade(DatabasePath);
            try
            {
                var preflightService = new SqliteLineageMigrationPreflight();
                var preflight = await preflightService.InspectAsync(DatabasePath);
                var backupService = new SqliteLineageMigrationBackupService(gate, preflightService);
                var backup = await backupService.CreateVerifiedAsync(lease, RecoveryPath);
                return new(lease, preflight, backup, backupService);
            }
            catch
            {
                lease.Dispose();
                throw;
            }
        }

        public async Task<SqliteConnection> OpenAsync()
        {
            var connection = new SqliteConnection($"Data Source={DatabasePath};Foreign Keys=True;Pooling=False");
            await connection.OpenAsync();
            return connection;
        }

        private BinTrackerDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<BinTrackerDbContext>()
                .UseSqlite($"Data Source={DatabasePath};Foreign Keys=True;Pooling=False").Options;
            return new(options);
        }

        private static async Task SeedAsync(BinTrackerDbContext db)
        {
            var customer = new Customer { Name = "Lineage test" };
            db.Customers.Add(customer);
            await db.SaveChangesAsync();

            var importRun = new ImportRun
            {
                Id = 1, SourceFileName = "fixture.xlsx", SourceClientPath = "fixture.xlsx",
                SourceSha256 = new string('B', 64), SourceLength = 1,
                SourceLastWriteUtc = DateTime.UtcNow, StartedUtc = DateTime.UtcNow,
                CompletedUtc = DateTime.UtcNow, Status = "Completed", Username = "admin",
                SessionId = "migration-fixture", MovementCount = 1
            };
            db.ImportRuns.Add(importRun);
            await db.SaveChangesAsync();

            var partialBatch = new MovementBatch
            {
                Id = 1, MovementDate = new(2026, 8, 20), MovementType = MovementType.In,
                Source = MovementSource.Batch, CreatedUtc = DateTime.UtcNow
            };
            var outputOriginalBatch = new MovementBatch
            {
                Id = 2, MovementDate = new(2026, 8, 21), MovementType = MovementType.In,
                Source = MovementSource.Batch, CreatedUtc = DateTime.UtcNow
            };
            var outputBatch = new MovementBatch
            {
                Id = 3, MovementDate = new(2026, 8, 22), MovementType = MovementType.In,
                Source = MovementSource.Batch, CreatedUtc = DateTime.UtcNow
            };
            db.MovementBatches.AddRange(partialBatch, outputOriginalBatch, outputBatch);
            var blue = Movement(1, MovementSource.Batch, customer.Id, 1, 4, partialBatch);
            var yellow = Movement(2, MovementSource.Batch, customer.Id, 3, 1, partialBatch);
            var yellowReversal = Movement(3, MovementSource.Batch, customer.Id, 3, 1, null, MovementType.Out);
            yellowReversal.ReversesMovementId = yellow.Id;
            yellow.CorrectedByMovementId = yellowReversal.Id;

            var single = Movement(4, MovementSource.Manual, customer.Id, 1, 2);
            var singleNeutral = Movement(5, MovementSource.Manual, customer.Id, 1, 2, type: MovementType.Out);
            singleNeutral.ReversesMovementId = single.Id;
            single.CorrectedByMovementId = singleNeutral.Id;
            var singleReplacement = Movement(6, MovementSource.Manual, customer.Id, 1, 3);
            var repeatedNeutral = Movement(11, MovementSource.Manual, customer.Id, 1, 3, type: MovementType.Out);
            repeatedNeutral.ReversesMovementId = singleReplacement.Id;
            singleReplacement.CorrectedByMovementId = repeatedNeutral.Id;
            var repeatedReplacement = Movement(12, MovementSource.Manual, customer.Id, 1, 4);

            var batchOriginal = Movement(7, MovementSource.Batch, customer.Id, 1, 2, outputOriginalBatch);
            var batchNeutral = Movement(8, MovementSource.Batch, customer.Id, 1, 2, type: MovementType.Out);
            batchNeutral.ReversesMovementId = batchOriginal.Id;
            batchOriginal.CorrectedByMovementId = batchNeutral.Id;
            var batchReplacement = Movement(9, MovementSource.Batch, customer.Id, 1, 2, outputBatch);
            var imported = Movement(10, MovementSource.ExcelImport, customer.Id, 1, 5);
            imported.ImportRunId = importRun.Id;
            db.BinMovements.AddRange(blue, yellow, yellowReversal, single, singleNeutral,
                singleReplacement, repeatedNeutral, repeatedReplacement,
                batchOriginal, batchNeutral, batchReplacement, imported);
            await db.SaveChangesAsync();

            var singleOp = Operation(1, MovementCorrectionKind.Single, null, null);
            var batchOp = Operation(2, MovementCorrectionKind.WholeBatch, 2, 3);
            var repeatedOp = Operation(3, MovementCorrectionKind.Single, null, null);
            db.MovementCorrectionOperations.AddRange(singleOp, batchOp, repeatedOp);
            await db.SaveChangesAsync();
            db.MovementCorrectionLines.AddRange(
                new MovementCorrectionLine { Id=1, CorrectionOperationId=1, OriginalMovementId=4, NeutralisingMovementId=5, ReplacementMovementId=6 },
                new MovementCorrectionLine { Id=2, CorrectionOperationId=2, OriginalMovementId=7, NeutralisingMovementId=8, ReplacementMovementId=9 },
                new MovementCorrectionLine { Id=3, CorrectionOperationId=3, OriginalMovementId=6, NeutralisingMovementId=11, ReplacementMovementId=12 });
            db.AuditEvents.AddRange(
                Audit("MOVEMENT_CORRECTED", "BinMovement", "4", new[] { new { Id=4L, NeutralisingMovementId=5L, ReplacementMovementId=6L } }),
                Audit("MOVEMENT_BATCH_CORRECTED", "MovementBatch", "2", new[] { new { Id=7L, NeutralisingMovementId=8L, ReplacementMovementId=9L } }),
                Audit("MOVEMENT_CORRECTED", "BinMovement", "6", new[] { new { Id=6L, NeutralisingMovementId=11L, ReplacementMovementId=12L } }));
            await db.SaveChangesAsync();
        }

        private static BinMovement Movement(long id, MovementSource source, int customerId,
            int containerId, int quantity, MovementBatch? batch = null, MovementType type = MovementType.In) =>
            new() { Id=id, MovementDate=batch?.MovementDate ?? new DateOnly(2026,8,20), MovementType=type,
                Source=source, CustomerId=customerId, ContainerTypeId=containerId, Quantity=quantity,
                MovementBatch=batch, CreatedUtc=DateTime.UtcNow };

        private static MovementCorrectionOperation Operation(long id, MovementCorrectionKind kind,
            int? originalBatch, int? replacementBatch) => new()
            {
                Id=id, ClientOperationId=Guid.NewGuid(), RequestFingerprint=new string('A',64), Kind=kind,
                OriginalBatchId=originalBatch, ReplacementBatchId=replacementBatch, Reason="test",
                ActorUserId=1, ActorUsername="admin", CreatedUtc=DateTime.UtcNow
            };

        private static AuditEvent Audit(string action, string entityType, string entityId, object value) => new()
        {
            TimestampUtc=DateTime.UtcNow, Username="admin", Action=action, EntityType=entityType,
            EntityId=entityId, Description="structured", AfterValues=JsonSerializer.Serialize(value),
            ComputerName="test", SessionId="test", Succeeded=true
        };

        public ValueTask DisposeAsync()
        {
            SqliteConnection.ClearAllPools();
            if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
            return ValueTask.CompletedTask;
        }
    }

    private static async Task ExecuteAsync(SqliteConnection connection, string sql)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        await command.ExecuteNonQueryAsync();
    }

    private static async Task<long> ScalarAsync(SqliteConnection connection, string sql)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        return Convert.ToInt64(await command.ExecuteScalarAsync());
    }

    private static async Task<long> ForeignKeyViolationsAsync(SqliteConnection connection)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA foreign_key_check;";
        await using var reader = await command.ExecuteReaderAsync();
        long count = 0;
        while (await reader.ReadAsync()) count++;
        return count;
    }

    private sealed class NoConflictProbe : IDatabaseOperationConflictProbe
    {
        public void EnsureNoConflict(string databasePath) { }
    }
}
