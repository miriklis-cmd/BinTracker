using BinTracker.Core;
using Xunit;

namespace BinTracker.UnitTests;

public sealed class MovementLineageContractTests
{
    [Fact]
    public void Existing_persisted_enum_values_remain_stable()
    {
        Assert.Equal(0, (int)MovementType.In);
        Assert.Equal(1, (int)MovementType.Out);

        Assert.Equal(0, (int)MovementSource.Manual);
        Assert.Equal(1, (int)MovementSource.Batch);
        Assert.Equal(2, (int)MovementSource.ExcelImport);
        Assert.Equal(3, (int)MovementSource.Adjustment);

        Assert.Equal(0, (int)UserRole.Administrator);
        Assert.Equal(1, (int)UserRole.Operator);
        Assert.Equal(2, (int)UserRole.Viewer);

        Assert.Equal(0, (int)CustomerType.Account);
        Assert.Equal(1, (int)CustomerType.CashCod);

        Assert.Equal(0, (int)CommunicationChannel.Email);
        Assert.Equal(1, (int)CommunicationChannel.Sms);

        Assert.Equal(0, (int)ReminderDeliveryStatus.Pending);
        Assert.Equal(1, (int)ReminderDeliveryStatus.Sent);
        Assert.Equal(2, (int)ReminderDeliveryStatus.Failed);
        Assert.Equal(3, (int)ReminderDeliveryStatus.Skipped);

        Assert.Equal(0, (int)MovementCorrectionKind.Single);
        Assert.Equal(1, (int)MovementCorrectionKind.WholeBatch);
        Assert.Equal(2, (int)MovementCorrectionKind.Reverse);
        Assert.Equal(3, (int)MovementCorrectionKind.Restore);
        Assert.Equal(
            [MovementCorrectionKind.Single, MovementCorrectionKind.WholeBatch,
                MovementCorrectionKind.Reverse, MovementCorrectionKind.Restore],
            Enum.GetValues<MovementCorrectionKind>());
    }

    [Fact]
    public void Logical_lineage_persisted_enum_values_are_explicit_and_stable()
    {
        Assert.Equal(
            [0, 1, 2, 3],
            Enum.GetValues<LogicalMovementBatchStatus>().Select(x => (int)x));
        Assert.Equal(
            [0, 1],
            Enum.GetValues<LogicalMovementLineState>().Select(x => (int)x));
        Assert.Equal(
            [0, 1, 2, 3, 4, 5, 6, 7],
            Enum.GetValues<LogicalMovementGenerationAction>().Select(x => (int)x));
        Assert.Equal(
            [0, 1, 2, 3, 4],
            Enum.GetValues<LogicalMovementTransformationRole>().Select(x => (int)x));
    }

    [Fact]
    public void Movement_change_field_values_are_independent_flags()
    {
        Assert.Equal(0, (int)MovementChangeField.None);
        Assert.Equal(1, (int)MovementChangeField.MovementDate);
        Assert.Equal(2, (int)MovementChangeField.Direction);
        Assert.Equal(4, (int)MovementChangeField.Customer);
        Assert.Equal(8, (int)MovementChangeField.ContainerType);
        Assert.Equal(16, (int)MovementChangeField.Quantity);
        Assert.Equal(32, (int)MovementChangeField.Reference);
        Assert.Equal(64, (int)MovementChangeField.Notes);

        var all = MovementChangeField.MovementDate |
            MovementChangeField.Direction |
            MovementChangeField.Customer |
            MovementChangeField.ContainerType |
            MovementChangeField.Quantity |
            MovementChangeField.Reference |
            MovementChangeField.Notes;

        Assert.Equal(127, (int)all);
    }

    [Fact]
    public void Logical_identity_contracts_keep_root_line_generation_and_revision_distinct()
    {
        var root = new LogicalMovementBatchId(17);
        var line = new LogicalMovementLineId(23);
        var generation = new LogicalMovementGenerationId(31);
        var generationNumber = new LogicalMovementGenerationNumber(4);

        Assert.Equal(17, root.Value);
        Assert.Equal(23, line.Value);
        Assert.Equal(31, generation.Value);
        Assert.Equal(4, generationNumber.Value);
    }
}
