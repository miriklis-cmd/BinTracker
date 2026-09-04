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
        var generationLine = new LogicalMovementGenerationLineId(37);
        var generationNumber = new LogicalMovementGenerationNumber(4);

        Assert.Equal(17, root.Value);
        Assert.Equal(23, line.Value);
        Assert.Equal(31, generation.Value);
        Assert.Equal(37, generationLine.Value);
        Assert.Equal(4, generationNumber.Value);
    }

    [Fact]
    public void Native_later_generation_operation_and_primary_audit_health_is_provider_neutral()
    {
        NativeMovementGenerationAuditFact[] generations =
        [
            new(1, 10, 0, null, null, LogicalMovementGenerationAction.Initial),
            new(2, 10, 1, 0, 20, LogicalMovementGenerationAction.Restored)
        ];
        NativeMovementOperationAuditFact[] operations =
        [new(20, 10, 0, 1, 1, MovementCorrectionKind.Restore)];
        PrimaryMovementAuditFact[] audits =
        [new(30, 20, "MOVEMENT_RESTORED", "LogicalMovementBatch", "10", true)];

        LogicalMovementOperationAuditHealthValidator.Validate(generations, operations, audits);

        Assert.Throws<InvalidOperationException>(() =>
            LogicalMovementOperationAuditHealthValidator.Validate(generations, operations, []));
        Assert.Throws<InvalidOperationException>(() =>
            LogicalMovementOperationAuditHealthValidator.Validate(generations,
                [operations[0] with { ResultGenerationNumber = 2 }], audits));
        Assert.Throws<InvalidOperationException>(() =>
            LogicalMovementOperationAuditHealthValidator.Validate(generations, operations,
                [audits[0] with { Action = "MOVEMENT_CORRECTED" }]));
    }

    [Fact]
    public void Later_generation_construction_proves_exact_predecessor_plan_movements_and_links()
    {
        var current = new ValidatedLogicalMovementCurrentLine(new(1), new(100), 10, 0,
            LogicalMovementLineState.Active, 10, null);
        var root = new ValidatedLogicalMovementCurrentRoot(new(5), null,
            LogicalMovementBatchStatus.Active, null, new(0), [current]);
        var business = new MovementBusinessState(10, new DateOnly(2026, 9, 1), MovementType.Out,
            MovementSource.Manual, 1, 1, 1, "ref", "note", null, null, null);
        var snapshot = new TrustedMovementPlanningSnapshot(root,
            [new TrustedMovementPlanningLine(current, business, null)],
            new HashSet<int> { 1 }, new HashSet<int> { 1 });
        var request = MovementMutationRequest.Correct(MovementMutationScope.Individual,
            [new LogicalMovementLineId(1)], "fix quantity",
            quantity: MovementFieldIntent<int>.Selected(2));
        var plan = MovementMutationPlanner.Plan(snapshot, request, new DateOnly(2026, 9, 2));
        var intent = new MovementMutationOperationIntent(Guid.Parse("11111111-1111-1111-1111-111111111111"),
            new(5), new(0), MovementCorrectionKind.Single, LogicalMovementGenerationAction.Corrected,
            1, "{}", new string('A', 64));
        var operation = new PersistedMovementMutationOperation(20, intent.ClientOperationId,
            intent.RequestFingerprint, intent.OperationKind, null, null, request.Reason, 2, "operator",
            DateTime.UnixEpoch, intent.RequestJson, 1, 5, 0, 1);
        var generation = new PersistedMovementMutationGeneration(30, 5, 1, 0, 20,
            LogicalMovementGenerationAction.Corrected, 1);
        PersistedMovementMutationLine[] lines =
        [new(40, 5, 30, 1, LogicalMovementLineState.Active,
            LogicalMovementGenerationAction.Corrected, MovementChangeField.Quantity,
            100, 12, null, null)];
        PersistedPlannedMovement[] movements =
        [
            new(11, new(1), PlannedMovementPurpose.CorrectionNeutraliser,
                new DateOnly(2026, 9, 1), MovementType.In, MovementSource.Manual,
                1, 1, 1, "REV-10 / ref", "Neutralises movement #10. Reason: fix quantity",
                "fix quantity", 10, null, null, null),
            new(12, new(1), PlannedMovementPurpose.CorrectionReplacement,
                new DateOnly(2026, 9, 1), MovementType.Out, MovementSource.Manual,
                1, 1, 2, "ref", "note", "fix quantity", null, null, null, null)
        ];
        PersistedMovementMutationLedgerLink[] links =
        [
            new(11, 5, 1, LogicalMovementTransformationRole.CorrectionNeutraliser, 40, null),
            new(12, 5, 1, LogicalMovementTransformationRole.CorrectionReplacement, 40, null)
        ];
        var construction = new LogicalMovementMutationConstruction(operation, generation,
            lines, movements, links, null);

        LogicalMovementMutationConstructionValidator.Validate(intent, snapshot, plan, construction);

        Assert.Throws<InvalidOperationException>(() =>
            LogicalMovementMutationConstructionValidator.Validate(intent, snapshot, plan,
                construction with { Lines = [lines[0] with { PreviousGenerationLineId = 99 }] }));
        Assert.Throws<InvalidOperationException>(() =>
            LogicalMovementMutationConstructionValidator.Validate(intent, snapshot, plan,
                construction with { NewLedgerLinks =
                    [links[0], links[1] with { Role = LogicalMovementTransformationRole.Restoration }] }));
    }

    [Fact]
    public void Initial_construction_accepts_valid_single_and_batch_without_using_generated_id_order()
    {
        AssertValid(CreateSingle());

        var batch = CreateBatch([900, 12, 400]);
        AssertValid(batch);
        Assert.Equal(
            [900L, 12L, 400L],
            batch.Lines.OrderBy(x => x.OriginalDisplayOrdinal).Select(x => x.RootMovementId));
    }

    [Fact]
    public void Initial_construction_rejects_invalid_root_state_and_current_pointer()
    {
        var status = CreateSingle();
        status.Root.Status = LogicalMovementBatchStatus.Active;
        AssertInvalid(status);

        var current = CreateSingle();
        current.Root.CurrentGenerationNumber = 0;
        AssertInvalid(current);
    }

    [Fact]
    public void Initial_construction_rejects_invalid_generation_shape()
    {
        foreach (var mutate in new Action<Construction>[]
        {
            x => x.Generations.Clear(),
            x => x.Generations.Add(new LogicalMovementGeneration { Id=99, LogicalMovementBatchId=1,
                GenerationNumber=1, Kind=LogicalMovementGenerationAction.Initial, LineCount=1 }),
            x => x.Generations[0].GenerationNumber = 1,
            x => x.Generations[0].PreviousGenerationNumber = 0,
            x => x.Generations[0].MovementCorrectionOperationId = 4,
            x => x.Generations[0].Kind = LogicalMovementGenerationAction.MigrationBaseline
        })
        {
            var construction = CreateSingle();
            mutate(construction);
            AssertInvalid(construction);
        }
    }

    [Fact]
    public void Initial_construction_rejects_invalid_generation_line_semantics()
    {
        foreach (var mutate in new Action<LogicalMovementGenerationLine>[]
        {
            x => x.State = LogicalMovementLineState.Reversed,
            x => x.Action = LogicalMovementGenerationAction.Corrected,
            x => x.AppliedFieldMask = MovementChangeField.Quantity,
            x => x.PreviousGenerationLineId = 9,
            x => x.ResultEffectiveMovementId = 999,
            x => x.LastEffectiveMovementId = 10,
            x => x.TerminalReversalMovementId = 11
        })
        {
            var construction = CreateSingle();
            mutate(construction.GenerationLines[0]);
            AssertInvalid(construction);
        }
    }

    [Fact]
    public void Initial_construction_rejects_invalid_permanent_membership_and_ordinals()
    {
        var duplicateOrdinal = CreateBatch([10, 20, 30]);
        duplicateOrdinal.Lines[2].OriginalDisplayOrdinal = 1;
        AssertInvalid(duplicateOrdinal);

        var nonContiguous = CreateBatch([10, 20, 30]);
        nonContiguous.Lines[2].OriginalDisplayOrdinal = 4;
        AssertInvalid(nonContiguous);

        var missing = CreateBatch([10, 20, 30]);
        missing.Lines.RemoveAt(2);
        AssertInvalid(missing);

        var crossOwned = CreateSingle();
        crossOwned.Lines[0].LogicalMovementBatchId = 44;
        AssertInvalid(crossOwned);
    }

    [Fact]
    public void Initial_construction_rejects_missing_surplus_or_cross_owned_generation_lines_and_links()
    {
        var missingState = CreateSingle();
        missingState.GenerationLines.Clear();
        AssertInvalid(missingState);

        var surplusState = CreateSingle();
        surplusState.GenerationLines.Add(new LogicalMovementGenerationLine
        {
            Id=88, LogicalMovementBatchId=1, LogicalMovementGenerationId=20,
            LogicalMovementLineId=10, State=LogicalMovementLineState.Active,
            Action=LogicalMovementGenerationAction.Initial, ResultEffectiveMovementId=101
        });
        AssertInvalid(surplusState);

        var missingLink = CreateSingle();
        missingLink.Links.Clear();
        AssertInvalid(missingLink);

        var crossOwnedLink = CreateSingle();
        crossOwnedLink.Links[0].LogicalMovementBatchId = 77;
        AssertInvalid(crossOwnedLink);
    }

    [Fact]
    public void Initial_construction_rejects_invalid_introduction_and_legacy_association()
    {
        var missingIntroduction = CreateSingle();
        missingIntroduction.Links[0].IntroducedByGenerationLineId = null;
        AssertInvalid(missingIntroduction);

        var wrongIntroduction = CreateSingle();
        wrongIntroduction.Links[0].IntroducedByGenerationLineId = 999;
        AssertInvalid(wrongIntroduction);

        var legacy = CreateSingle();
        legacy.Links[0].LegacyMovementCorrectionLineId = 5;
        AssertInvalid(legacy);
    }

    [Fact]
    public void Initial_construction_rejects_incorrect_single_or_batch_physical_ownership()
    {
        var singleInBatch = CreateSingle();
        singleInBatch.PhysicalMovementBatchIds[101] = 7;
        AssertInvalid(singleInBatch);

        var incompleteBatch = CreateBatch([10, 20]);
        incompleteBatch.RootBatchMovementIds.Remove(20);
        AssertInvalid(incompleteBatch);

        var wrongBatch = CreateBatch([10, 20]);
        wrongBatch.PhysicalMovementBatchIds[20] = 51;
        AssertInvalid(wrongBatch);
    }

    [Fact]
    public void Initial_construction_rejects_operation_or_physical_output_evidence()
    {
        var operation = CreateSingle();
        operation.CorrectionOperationCount = 1;
        AssertInvalid(operation);

        var output = CreateSingle();
        output.PhysicalOutputCount = 1;
        AssertInvalid(output);
    }

    private static Construction CreateSingle() => Create(null, [101]);

    private static Construction CreateBatch(IReadOnlyList<long> movementIds) => Create(50, movementIds);

    private static Construction Create(int? batchId, IReadOnlyList<long> movementIds)
    {
        var root = new LogicalMovementBatch
        {
            Id=1, RootMovementBatchId=batchId, Status=LogicalMovementBatchStatus.Initializing,
            CurrentGenerationNumber=null, LineCount=movementIds.Count, CreatedUtc=DateTime.UnixEpoch
        };
        var lines = movementIds.Select((movementId, ordinal) => new LogicalMovementLine
        {
            Id=10 + ordinal, LogicalMovementBatchId=1, RootMovementId=movementId,
            OriginalDisplayOrdinal=ordinal, CreatedUtc=DateTime.UnixEpoch
        }).ToList();
        var generation = new LogicalMovementGeneration
        {
            Id=20, LogicalMovementBatchId=1, GenerationNumber=0, PreviousGenerationNumber=null,
            MovementCorrectionOperationId=null, Kind=LogicalMovementGenerationAction.Initial,
            LineCount=movementIds.Count, CreatedUtc=DateTime.UnixEpoch
        };
        var states = lines.Select((line, ordinal) => new LogicalMovementGenerationLine
        {
            Id=30 + ordinal, LogicalMovementBatchId=1, LogicalMovementGenerationId=20,
            LogicalMovementLineId=line.Id, State=LogicalMovementLineState.Active,
            Action=LogicalMovementGenerationAction.Initial, AppliedFieldMask=MovementChangeField.None,
            PreviousGenerationLineId=null, ResultEffectiveMovementId=line.RootMovementId,
            LastEffectiveMovementId=null, TerminalReversalMovementId=null, CreatedUtc=DateTime.UnixEpoch
        }).ToList();
        var links = lines.Select((line, ordinal) => new LogicalMovementLedgerLink
        {
            BinMovementId=line.RootMovementId, LogicalMovementBatchId=1,
            LogicalMovementLineId=line.Id, Role=LogicalMovementTransformationRole.RootOriginal,
            IntroducedByGenerationLineId=states[ordinal].Id,
            LegacyMovementCorrectionLineId=null, CreatedUtc=DateTime.UnixEpoch
        }).ToList();
        var physical = movementIds.ToDictionary(x => x, _ => batchId);
        return new(root, lines, [generation], states, links, movementIds.ToArray(),
            physical, batchId.HasValue ? movementIds.ToHashSet() : [], 0, 0);
    }

    private static void AssertValid(Construction construction) =>
        LogicalMovementInitialConstructionValidator.Validate(
            construction.Root, construction.Lines, construction.Generations,
            construction.GenerationLines, construction.Links, construction.ExpectedOrderedMovementIds,
            construction.PhysicalMovementBatchIds, construction.RootBatchMovementIds,
            construction.CorrectionOperationCount, construction.PhysicalOutputCount);

    private static void AssertInvalid(Construction construction) =>
        Assert.Throws<InvalidOperationException>(() => AssertValid(construction));

    private sealed record Construction(
        LogicalMovementBatch Root,
        List<LogicalMovementLine> Lines,
        List<LogicalMovementGeneration> Generations,
        List<LogicalMovementGenerationLine> GenerationLines,
        List<LogicalMovementLedgerLink> Links,
        IReadOnlyList<long> ExpectedOrderedMovementIds,
        Dictionary<long, int?> PhysicalMovementBatchIds,
        HashSet<long> RootBatchMovementIds,
        int InitialCorrectionOperationCount,
        int InitialPhysicalOutputCount)
    {
        public int CorrectionOperationCount { get; set; } = InitialCorrectionOperationCount;
        public int PhysicalOutputCount { get; set; } = InitialPhysicalOutputCount;
    }
}
