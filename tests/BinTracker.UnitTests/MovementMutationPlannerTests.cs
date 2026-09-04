using System.Collections.ObjectModel;
using System.Reflection;
using BinTracker.Core;
using Xunit;

namespace BinTracker.UnitTests;

public sealed class MovementMutationPlannerTests
{
    private static readonly DateOnly Today = new(2026, 9, 1);

    public enum ReversalPairCorruption
    {
        WrongReversesMovementId,
        NullReversesMovementId,
        SameDirection,
        WrongCustomer,
        WrongContainerType,
        WrongQuantity,
        NonManualSource,
        PhysicalBatchMembership,
        ImportRunMembership
    }

    [Fact]
    public void Complete_noop_has_no_artifacts_and_partial_noop_is_already_matches()
    {
        var noOp = Plan(Snapshot(true, Active(1, 101, source: MovementSource.Batch)), Correct([new(1)], 5));
        Assert.Equal(MovementMutationPlanKind.NoOp, noOp.Kind);
        Assert.Empty(noOp.Lines);
        Assert.Null(noOp.PhysicalOutput);
        var partial = Plan(Snapshot(true, Active(1, 101, source: MovementSource.Batch), Active(2, 102, 7, MovementSource.Batch)),
            Correct([new(1), new(2)], 5));
        Assert.Equal([LogicalMovementGenerationAction.AlreadyMatches, LogicalMovementGenerationAction.Corrected], partial.Lines.Select(x => x.Action));
        Assert.All(partial.Lines, x => Assert.Equal(MovementChangeField.Quantity, x.AppliedFieldMask));
        Assert.Null(partial.PhysicalOutput);
    }

    [Fact]
    public void Individual_correction_carries_untargeted_and_encodes_result_pointer()
    {
        var plan = Plan(Snapshot(false, Active(1, 101), Active(2, 102)),
            MovementMutationRequest.Correct(MovementMutationScope.Individual, [new(2)], "quantity wrong",
                quantity: MovementFieldIntent<int>.Selected(9)));
        Assert.Equal(LogicalMovementGenerationAction.CarriedForward, plan.Lines[0].Action);
        Assert.Equal(101, plan.Lines[0].EffectiveMovement.ExistingMovementId);
        Assert.Equal(PlannedMovementPurpose.CorrectionReplacement, plan.Lines[1].EffectiveMovement.PlannedPurpose);
        Assert.Equal([PlannedMovementPurpose.CorrectionNeutraliser, PlannedMovementPurpose.CorrectionReplacement], plan.Lines[1].Movements.Select(x => x.Purpose));
    }

    [Fact]
    public void Correction_mask_text_clear_and_neutraliser_are_exact()
    {
        var request = MovementMutationRequest.Correct(MovementMutationScope.Individual, [new(1)], " fields ",
            movementDate: MovementFieldIntent<DateOnly>.Selected(new(2026, 8, 2)), direction: MovementFieldIntent<MovementType>.Selected(MovementType.Out),
            customer: MovementFieldIntent<int>.Selected(2), containerType: MovementFieldIntent<int>.Selected(2), quantity: MovementFieldIntent<int>.Selected(8),
            reference: MovementFieldIntent<string>.Selected("  "), notes: MovementFieldIntent<string>.Selected(" changed "));
        var line = Assert.Single(Plan(Snapshot(false, Active(1, 101)), request).Lines);
        Assert.Equal((MovementChangeField)127, line.AppliedFieldMask);
        Assert.Equal(new DateOnly(2026, 8, 1), line.Movements[0].MovementDate);
        Assert.Equal(MovementSource.Manual, line.Movements[0].Source);
        Assert.Null(line.Movements[1].Reference);
        Assert.Equal("changed", line.Movements[1].Notes);
        Assert.Equal("fields", line.Movements[1].Reason);
    }

    [Fact]
    public void Mixed_corrected_and_remain_reversed_preserves_both_ids_and_is_not_physical()
    {
        var plan = Plan(Snapshot(true, Active(1, 101, source: MovementSource.Batch), Reversed(2, 102, 202, MovementSource.Batch)),
            Correct([new(1), new(2)], 8, [ReversedLineDecision.RemainReversed(new(2))]));
        Assert.Equal(LogicalMovementGenerationAction.Corrected, plan.Lines[0].Action);
        var reversed = plan.Lines[1];
        Assert.Equal(LogicalMovementGenerationAction.RemainReversed, reversed.Action);
        Assert.Equal(102, reversed.EffectiveMovement.ExistingMovementId);
        Assert.Equal(202, reversed.TerminalReversalMovement!.ExistingMovementId);
        Assert.Equal(MovementChangeField.None, reversed.AppliedFieldMask);
        Assert.Empty(reversed.Movements);
        Assert.Null(plan.PhysicalOutput);
    }

    [Fact]
    public void Already_matches_plus_restore_uses_per_line_exact_mask()
    {
        var decision = ReversedLineDecision.Restore(new(2), notes: MovementFieldIntent<string>.Selected(null));
        var plan = Plan(Snapshot(true, Active(1, 101, source: MovementSource.Batch), Reversed(2, 102, 202, MovementSource.Batch)),
            Correct([new(1), new(2)], 5, [decision]));
        Assert.Equal(LogicalMovementGenerationAction.AlreadyMatches, plan.Lines[0].Action);
        Assert.Equal(MovementChangeField.Quantity, plan.Lines[0].AppliedFieldMask);
        Assert.Equal(LogicalMovementGenerationAction.Restored, plan.Lines[1].Action);
        Assert.Equal(MovementChangeField.Notes, plan.Lines[1].AppliedFieldMask);
        Assert.Null(Assert.Single(plan.Lines[1].Movements).Notes);
        Assert.Null(plan.PhysicalOutput);
    }

    [Fact]
    public void Uniform_mixed_corrected_and_restored_is_physical()
    {
        var decision = ReversedLineDecision.Restore(new(2), quantity: MovementFieldIntent<int>.Selected(8));
        var plan = Plan(Snapshot(true, Active(1, 101, source: MovementSource.Batch), Reversed(2, 102, 202, MovementSource.Batch)),
            Correct([new(1), new(2)], 8, [decision]));
        Assert.Equal([LogicalMovementGenerationAction.Corrected, LogicalMovementGenerationAction.Restored], plan.Lines.Select(x => x.Action));
        Assert.Equal([PlannedMovementPurpose.CorrectionReplacement, PlannedMovementPurpose.Restoration],
            Assert.IsType<PlannedPhysicalOutput>(plan.PhysicalOutput).Members.Select(x => x.Purpose));
    }

    [Fact]
    public void Whole_root_decisions_fail_closed_when_missing_extra_or_for_active()
    {
        var snapshot = Snapshot(false, Active(1, 101), Reversed(2, 102, 202));
        Assert.Throws<InvalidOperationException>(() => Plan(snapshot, Correct([new(1), new(2)], 8)));
        Assert.Throws<InvalidOperationException>(() => Plan(snapshot, Correct([new(1), new(2)], 8, [ReversedLineDecision.Restore(new(1))])));
        Assert.Throws<InvalidOperationException>(() => Plan(snapshot, Correct([new(1), new(2)], 8,
            [ReversedLineDecision.Restore(new(2)), ReversedLineDecision.RemainReversed(new(1))])));
        Assert.Throws<ArgumentException>(() => Correct([new(1), new(2)], 8,
            [ReversedLineDecision.Restore(new(2)), ReversedLineDecision.RemainReversed(new(2))]));
    }

    [Fact]
    public void Standalone_restore_none_is_substantive_and_plan_local()
    {
        var line = Assert.Single(Plan(Snapshot(false, Reversed(1, 101, 201)),
            MovementMutationRequest.Restore(MovementMutationScope.Individual, [new(1)], "bad reversal")).Lines);
        Assert.Equal(LogicalMovementGenerationAction.Restored, line.Action);
        Assert.Equal(MovementChangeField.None, line.AppliedFieldMask);
        Assert.Equal(PlannedMovementPurpose.Restoration, line.EffectiveMovement.PlannedPurpose);
        Assert.Null(line.TerminalReversalMovement);
        Assert.Equal(201, Assert.Single(line.Movements).ReversesMovementId);
    }

    [Fact]
    public void Reversed_carried_forward_preserves_both_pointer_ids()
    {
        var line = Plan(Snapshot(false, Active(1, 101), Reversed(2, 102, 202)),
            MovementMutationRequest.Correct(MovementMutationScope.Individual, [new(1)], "quantity wrong", quantity: MovementFieldIntent<int>.Selected(8))).Lines[1];
        Assert.Equal(LogicalMovementGenerationAction.CarriedForward, line.Action);
        Assert.Equal(102, line.EffectiveMovement.ExistingMovementId);
        Assert.Equal(202, line.TerminalReversalMovement!.ExistingMovementId);
    }

    [Fact]
    public void Ordinary_reversal_date_is_separate_authoritative_input()
    {
        var request = MovementMutationRequest.Reverse(MovementMutationScope.Individual, [new(1)], "wrong movement");
        Assert.DoesNotContain(typeof(MovementMutationRequest).GetProperties(), x => x.Name.Contains("OperationDate"));
        Assert.Equal(new DateOnly(2026, 8, 30), Assert.Single(Plan(Snapshot(false, Active(1, 101)), request, new(2026, 8, 30)).Lines[0].Movements).MovementDate);
        Assert.Equal(Today, Assert.Single(Plan(Snapshot(false, Active(1, 101)), request).Lines[0].Movements).MovementDate);
    }

    [Fact]
    public void Future_date_uses_separate_authoritative_input()
    {
        var request = MovementMutationRequest.Correct(MovementMutationScope.Individual, [new(1)], "future date",
            movementDate: MovementFieldIntent<DateOnly>.Selected(Today));
        Assert.Throws<InvalidOperationException>(() => Plan(Snapshot(false, Active(1, 101)), request, Today.AddDays(-1)));
        Assert.Equal(MovementMutationPlanKind.Substantive, Plan(Snapshot(false, Active(1, 101)), request).Kind);
    }

    [Theory]
    [InlineData(ReversalPairCorruption.WrongReversesMovementId)]
    [InlineData(ReversalPairCorruption.NullReversesMovementId)]
    [InlineData(ReversalPairCorruption.SameDirection)]
    [InlineData(ReversalPairCorruption.WrongCustomer)]
    [InlineData(ReversalPairCorruption.WrongContainerType)]
    [InlineData(ReversalPairCorruption.WrongQuantity)]
    [InlineData(ReversalPairCorruption.NonManualSource)]
    [InlineData(ReversalPairCorruption.PhysicalBatchMembership)]
    [InlineData(ReversalPairCorruption.ImportRunMembership)]
    public void Reversed_current_pair_business_fact_corruption_fails_closed(ReversalPairCorruption corruption)
    {
        var snapshot = Snapshot(false, Reversed(1, 101, 201, corruption: corruption));
        var request = MovementMutationRequest.Restore(MovementMutationScope.Individual,
            [new(1)], "invalid reversal pair");

        Assert.Throws<InvalidOperationException>(() => Plan(snapshot, request));
    }

    [Fact]
    public void Terminal_reversal_after_authoritative_business_date_fails_closed()
    {
        var snapshot = Snapshot(false, Reversed(1, 101, 201,
            terminalDate: Today.AddDays(1)));
        var request = MovementMutationRequest.Restore(MovementMutationScope.Individual,
            [new(1)], "future terminal reversal");

        Assert.Throws<InvalidOperationException>(() => Plan(snapshot, request));
    }

    [Fact]
    public void Last_effective_after_authoritative_business_date_fails_closed()
    {
        var snapshot = Snapshot(false, Reversed(1, 101, 201,
            effectiveDate: Today.AddDays(1)));
        var request = MovementMutationRequest.Restore(MovementMutationScope.Individual,
            [new(1)], "future last effective");

        Assert.Throws<InvalidOperationException>(() => Plan(snapshot, request));
    }

    [Fact]
    public void Active_effective_after_authoritative_business_date_fails_closed()
    {
        var snapshot = Snapshot(false, Active(1, 101, movementDate: Today.AddDays(1)));
        var request = MovementMutationRequest.Reverse(MovementMutationScope.Individual,
            [new(1)], "future active movement");

        Assert.Throws<InvalidOperationException>(() => Plan(snapshot, request));
    }

    [Theory]
    [InlineData(MovementSource.ExcelImport, null)]
    [InlineData(MovementSource.Adjustment, null)]
    [InlineData(MovementSource.Manual, 99L)]
    public void All_generic_mutations_reject_excluded_facts(MovementSource source, long? importRun)
    {
        Assert.Throws<InvalidOperationException>(() => Plan(Snapshot(false, Active(1, 101, source: source, importRun: importRun)), CorrectIndividual()));
        Assert.Throws<InvalidOperationException>(() => Plan(Snapshot(false, Active(1, 101, source: source, importRun: importRun)),
            MovementMutationRequest.Reverse(MovementMutationScope.Individual, [new(1)], "not generic")));
        Assert.Throws<InvalidOperationException>(() => Plan(Snapshot(false, Reversed(1, 101, 201, source, importRun)),
            MovementMutationRequest.Restore(MovementMutationScope.Individual, [new(1)], "not generic")));
    }

    [Fact]
    public void Trust_and_plan_surfaces_are_nonforgeable_and_get_only()
    {
        Type[] types = [typeof(TrustedMovementPlanningSnapshot), typeof(TrustedMovementPlanningLine), typeof(MovementBusinessState),
            typeof(MovementMutationRequest), typeof(ReversedLineDecision), typeof(MovementMutationPlan),
            typeof(PlannedMovementLine), typeof(PlannedMovementReference), typeof(PlannedMovementSpec), typeof(PlannedPhysicalOutput)];
        Assert.All(types, x => Assert.Empty(x.GetConstructors(BindingFlags.Public | BindingFlags.Instance)));
        Assert.All(types.SelectMany(x => x.GetProperties()), x => Assert.False(x.CanWrite));
        var materializer = Type.GetType(
            "BinTracker.Data.SqliteMovementPlanningSnapshotMaterializer, BinTracker.Data", true)!;
        Assert.False(materializer.IsPublic);
        Assert.Empty(materializer.GetConstructors(BindingFlags.Public | BindingFlags.Instance));
    }

    [Fact]
    public void Snapshot_pointer_mismatch_fails_closed()
    {
        var one = Active(1, 101);
        Assert.Throws<InvalidOperationException>(() => Plan(SnapshotFrom([one.Current], []),
            MovementMutationRequest.Reverse(MovementMutationScope.Individual, [new(1)], "valid reason")));
        var wrong = new TrustedMovementPlanningLine(one.Current, State(999, 5, MovementSource.Manual, null), null);
        Assert.Throws<InvalidOperationException>(() => Plan(SnapshotFrom([one.Current], [wrong]),
            MovementMutationRequest.Reverse(MovementMutationScope.Individual, [new(1)], "valid reason")));
    }

    [Fact]
    public void Malformed_action_pointer_and_spec_shape_is_rejected()
    {
        var request = Correct([new(1)], 8);
        var malformed = new PlannedMovementLine(new(1), LogicalMovementGenerationAction.Corrected,
            LogicalMovementLineState.Active, request.AppliedFieldMask, PlannedMovementReference.Existing(101), null, []);
        var validator = typeof(MovementMutationPlanner).GetMethod("ValidateResultShape",
            BindingFlags.NonPublic | BindingFlags.Static)!;
        var failure = Assert.Throws<TargetInvocationException>(() => validator.Invoke(null,
            [new ReadOnlyCollection<PlannedMovementLine>([malformed])]));
        Assert.IsType<InvalidOperationException>(failure.InnerException);
    }

    private static MovementMutationRequest Correct(IEnumerable<LogicalMovementLineId> ids, int quantity,
        IEnumerable<ReversedLineDecision>? decisions = null) => MovementMutationRequest.Correct(MovementMutationScope.WholeRoot,
        ids, "valid reason", quantity: MovementFieldIntent<int>.Selected(quantity), reversedLineDecisions: decisions);
    private static MovementMutationRequest CorrectIndividual() => MovementMutationRequest.Correct(MovementMutationScope.Individual,
        [new(1)], "not generic", quantity: MovementFieldIntent<int>.Selected(8));
    private static MovementMutationPlan Plan(TrustedMovementPlanningSnapshot snapshot, MovementMutationRequest request, DateOnly? date = null) =>
        MovementMutationPlanner.Plan(snapshot, request, date ?? Today);
    private static TrustedMovementPlanningSnapshot Snapshot(bool physical, params TrustedMovementPlanningLine[] lines) => SnapshotFrom(lines.Select(x => x.Current).ToArray(), lines, physical);
    private static TrustedMovementPlanningSnapshot SnapshotFrom(IReadOnlyList<ValidatedLogicalMovementCurrentLine> current,
        IReadOnlyList<TrustedMovementPlanningLine> facts, bool physical = false)
    {
        var root = new ValidatedLogicalMovementCurrentRoot(new(10), physical ? 30 : null, LogicalMovementBatchStatus.Active,
            null, new(2), new ReadOnlyCollection<ValidatedLogicalMovementCurrentLine>(current.ToList()));
        return new(root, new ReadOnlyCollection<TrustedMovementPlanningLine>(facts.ToList()),
            new ReadOnlySet<int>(new HashSet<int> { 1, 2 }), new ReadOnlySet<int>(new HashSet<int> { 1, 2 }));
    }
    private static TrustedMovementPlanningLine Active(long lineId, long movementId, int quantity = 5,
        MovementSource source = MovementSource.Manual, long? importRun = null, DateOnly? movementDate = null)
    {
        var current = new ValidatedLogicalMovementCurrentLine(new(lineId), new(10_001), movementId,
            checked((int)lineId - 1), LogicalMovementLineState.Active, movementId, null);
        return new(current, State(movementId, quantity, source, importRun,
            movementBatchId: source == MovementSource.Batch ? 30 : null, movementDate: movementDate), null);
    }
    private static TrustedMovementPlanningLine Reversed(long lineId, long effectiveId, long reversalId,
        MovementSource source = MovementSource.Manual, long? importRun = null,
        ReversalPairCorruption? corruption = null, DateOnly? effectiveDate = null,
        DateOnly? terminalDate = null)
    {
        var current = new ValidatedLogicalMovementCurrentLine(new(lineId), new(10_002), effectiveId,
            checked((int)lineId - 1), LogicalMovementLineState.Reversed, effectiveId, reversalId);
        var effective = State(effectiveId, 5, source, importRun,
            movementBatchId: source == MovementSource.Batch ? 30 : null, movementDate: effectiveDate);
        var reversal = State(reversalId,
            corruption == ReversalPairCorruption.WrongQuantity ? 6 : 5,
            corruption == ReversalPairCorruption.NonManualSource ? MovementSource.Batch : MovementSource.Manual,
            corruption == ReversalPairCorruption.ImportRunMembership ? 99 : null,
            corruption == ReversalPairCorruption.SameDirection ? MovementType.In : MovementType.Out,
            corruption == ReversalPairCorruption.WrongCustomer ? 2 : 1,
            corruption == ReversalPairCorruption.WrongContainerType ? 2 : 1,
            corruption == ReversalPairCorruption.PhysicalBatchMembership ? 30 : null,
            corruption switch
            {
                ReversalPairCorruption.WrongReversesMovementId => 999,
                ReversalPairCorruption.NullReversesMovementId => null,
                _ => effectiveId
            }, terminalDate);
        return new(current, effective, reversal);
    }
    private static MovementBusinessState State(long id, int quantity, MovementSource source, long? importRun,
        MovementType direction = MovementType.In, int customerId = 1, int containerTypeId = 1,
        int? movementBatchId = null, long? reversesMovementId = null, DateOnly? movementDate = null) =>
        new(id, movementDate ?? new(2026, 8, 1), direction, source, customerId, containerTypeId, quantity,
            "ref", "note", movementBatchId, importRun, reversesMovementId);
}
