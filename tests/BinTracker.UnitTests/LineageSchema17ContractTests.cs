using BinTracker.Core;
using BinTracker.Data;
using Xunit;

namespace BinTracker.UnitTests;

public sealed class LineageSchema17ContractTests
{
    [Fact]
    public void Movement_correction_kind_persisted_values_are_permanent()
    {
        Assert.Equal(0, (int)MovementCorrectionKind.Single);
        Assert.Equal(1, (int)MovementCorrectionKind.WholeBatch);
        Assert.Equal(2, (int)MovementCorrectionKind.Reverse);
        Assert.Equal(3, (int)MovementCorrectionKind.Restore);
        Assert.Equal([0, 1, 2, 3], Enum.GetValues<MovementCorrectionKind>().Select(x => (int)x));
    }

    [Fact]
    public void Dormant_migrator_contract_is_exactly_schema_16_to_17()
    {
        Assert.Equal(16, SqliteLineageSchema17Migrator.SourceSchemaVersion);
        Assert.Equal(17, SqliteLineageSchema17Migrator.TargetSchemaVersion);
        Assert.Equal(16, DatabaseSetup.LatestSchemaVersion);
    }

    [Fact]
    public void Failure_injection_contract_covers_every_required_transaction_stage()
    {
        Assert.Equal(
        [
            LineageSchema17MigrationCheckpoint.BeforeSchemaMutation,
            LineageSchema17MigrationCheckpoint.AfterFirstSchemaChange,
            LineageSchema17MigrationCheckpoint.DuringRootCreation,
            LineageSchema17MigrationCheckpoint.DuringLineCreation,
            LineageSchema17MigrationCheckpoint.DuringBaselineGenerationCreation,
            LineageSchema17MigrationCheckpoint.DuringGenerationLineCreation,
            LineageSchema17MigrationCheckpoint.DuringLedgerLinkCreation,
            LineageSchema17MigrationCheckpoint.DuringLegacyOperationMapping,
            LineageSchema17MigrationCheckpoint.DuringLegacyAuditMapping,
            LineageSchema17MigrationCheckpoint.DuringMovementBatchForeignKeyRebuild,
            LineageSchema17MigrationCheckpoint.BeforePostflight,
            LineageSchema17MigrationCheckpoint.AfterPostflightBeforePublication
        ], Enum.GetValues<LineageSchema17MigrationCheckpoint>());
    }
}
