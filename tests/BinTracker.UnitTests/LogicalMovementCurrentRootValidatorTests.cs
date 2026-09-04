using BinTracker.Core;
using System.Reflection;
using Xunit;

namespace BinTracker.UnitTests;

public sealed class LogicalMovementCurrentRootValidatorTests
{
    [Fact]
    public void Valid_active_and_reversed_current_snapshot_resolves()
    {
        var result = LogicalMovementCurrentRootValidator.Validate(Valid());
        Assert.Equal(LogicalMovementCurrentRootResolutionKind.Resolved, result.Kind);
        Assert.Equal(2, result.Root!.Lines.Count);
        Assert.Equal(100, result.Root.Lines[0].CurrentGenerationLineId.Value);
        Assert.Equal(101, result.Root.Lines[1].CurrentGenerationLineId.Value);
        Assert.Equal(LogicalMovementLineState.Reversed, result.Root.Lines[1].State);
    }

    [Fact]
    public void ReadOnly_requires_and_accepts_the_same_complete_current_snapshot()
    {
        var candidate = Valid() with { Root = Valid().Root! with
            { Status = (int)LogicalMovementBatchStatus.ReadOnly, StatusReasonCode = "LEGACY_UNSUPPORTED" } };
        var result = LogicalMovementCurrentRootValidator.Validate(candidate);
        Assert.Equal(LogicalMovementCurrentRootResolutionKind.Resolved, result.Kind);
        Assert.Equal("LEGACY_UNSUPPORTED", result.Root!.StatusReasonCode);
    }

    [Theory]
    [InlineData(typeof(ValidatedLogicalMovementCurrentRoot))]
    [InlineData(typeof(ValidatedLogicalMovementCurrentLine))]
    [InlineData(typeof(LogicalMovementCurrentRootResolution))]
    public void Validated_application_models_have_no_public_construction_or_mutation(Type type)
    {
        Assert.Empty(type.GetConstructors(BindingFlags.Public | BindingFlags.Instance));
        Assert.All(type.GetProperties(BindingFlags.Public | BindingFlags.Instance),
            property => Assert.Null(property.SetMethod));
        Assert.DoesNotContain(type.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance),
            method => method.Name == "<Clone>$");
    }

    [Theory]
    [InlineData((int)LogicalMovementBatchStatus.Initializing)]
    [InlineData((int)LogicalMovementBatchStatus.Invalid)]
    public void Nonprojectable_committed_status_does_not_escape(int status)
    {
        var candidate = Valid() with { Root = Valid().Root! with { Status = status } };
        Assert.Equal(LogicalMovementCurrentRootResolutionKind.Unhealthy,
            LogicalMovementCurrentRootValidator.Validate(candidate).Kind);
    }

    [Fact]
    public void Same_count_with_wrong_exact_current_membership_fails_closed()
    {
        var candidate = Valid();
        candidate = candidate with { CurrentLines = [candidate.CurrentLines[0], candidate.CurrentLines[1] with { LineId = 99 }] };
        Assert.Equal(LogicalMovementCurrentRootFailure.InvalidCurrentMembership,
            LogicalMovementCurrentRootValidator.Validate(candidate).Failure);
    }

    [Fact]
    public void Nonpositive_current_generation_line_identity_fails_closed()
    {
        var candidate = Valid();
        candidate = candidate with
        {
            CurrentLines = [candidate.CurrentLines[0] with { Id = 0 }, candidate.CurrentLines[1]]
        };
        Assert.Equal(LogicalMovementCurrentRootFailure.InvalidCurrentMembership,
            LogicalMovementCurrentRootValidator.Validate(candidate).Failure);
    }

    [Fact]
    public void Malformed_pointer_shapes_fail_closed()
    {
        var candidate = Valid();
        candidate = candidate with { CurrentLines = [candidate.CurrentLines[0] with { LastEffectiveMovementId = 10 }, candidate.CurrentLines[1]] };
        Assert.Equal(LogicalMovementCurrentRootFailure.InvalidCurrentState,
            LogicalMovementCurrentRootValidator.Validate(candidate).Failure);
    }

    [Theory]
    [InlineData(1, 1)]
    [InlineData(3, 0)]
    public void Incompatible_current_roles_fail_closed(long movementId, int role)
    {
        var candidate = Valid();
        candidate = candidate with { LedgerLinks = candidate.LedgerLinks.Select(x => x.MovementId == movementId ? x with { Role = role } : x).ToList() };
        Assert.Equal(LogicalMovementCurrentRootFailure.InvalidTransformationRole,
            LogicalMovementCurrentRootValidator.Validate(candidate).Failure);
    }

    [Fact]
    public void RootOriginal_and_introduction_ownership_are_structural()
    {
        var candidate = Valid();
        var wrongRoot = candidate with { LedgerLinks = candidate.LedgerLinks.Select(x => x.MovementId == 10 ? x with { LineId = 2 } : x).ToList() };
        Assert.Equal(LogicalMovementCurrentRootFailure.InvalidRootOriginal,
            LogicalMovementCurrentRootValidator.Validate(wrongRoot).Failure);
        var wrongIntroduction = candidate with { Introductions = candidate.Introductions.Select(x => x.GenerationLineId == 100 ? x with { LineId = 2 } : x).ToList() };
        Assert.Equal(LogicalMovementCurrentRootFailure.InvalidIntroduction,
            LogicalMovementCurrentRootValidator.Validate(wrongIntroduction).Failure);
    }

    [Fact]
    public void Missing_current_generation_and_undefined_enums_fail_closed()
    {
        var candidate = Valid();
        Assert.Equal(LogicalMovementCurrentRootFailure.InvalidCurrentGeneration,
            LogicalMovementCurrentRootValidator.Validate(candidate with { SelectedGenerations = [] }).Failure);
        Assert.Equal(LogicalMovementCurrentRootFailure.InvalidRoot,
            LogicalMovementCurrentRootValidator.Validate(candidate with { Root = candidate.Root! with { Status = 99 } }).Failure);
        Assert.Equal(LogicalMovementCurrentRootFailure.InvalidCurrentState,
            LogicalMovementCurrentRootValidator.Validate(candidate with { CurrentLines = [candidate.CurrentLines[0] with { State = 99 }, candidate.CurrentLines[1]] }).Failure);
    }

    [Fact]
    public void Missing_movement_and_wrong_root_or_line_ownership_fail_closed()
    {
        var candidate = Valid();
        Assert.Equal(LogicalMovementCurrentRootFailure.InvalidMovementOwnership,
            LogicalMovementCurrentRootValidator.Validate(candidate with
            { Movements = candidate.Movements.Where(x => x.Key != 1).ToDictionary(x => x.Key, x => x.Value) }).Failure);
        Assert.Equal(LogicalMovementCurrentRootFailure.InvalidMovementOwnership,
            LogicalMovementCurrentRootValidator.Validate(candidate with
            { LedgerLinks = candidate.LedgerLinks.Select(x => x.MovementId == 1 ? x with { RootId = 2 } : x).ToList() }).Failure);
        Assert.Equal(LogicalMovementCurrentRootFailure.InvalidTransformationRole,
            LogicalMovementCurrentRootValidator.Validate(candidate with
            { LedgerLinks = candidate.LedgerLinks.Select(x => x.MovementId == 1 ? x with { LineId = 2 } : x).ToList() }).Failure);
    }

    [Fact]
    public void Duplicate_permanent_identity_and_wrong_generation_root_fail_closed()
    {
        var candidate = Valid();
        Assert.Equal(LogicalMovementCurrentRootFailure.InvalidPermanentMembership,
            LogicalMovementCurrentRootValidator.Validate(candidate with
            { PermanentLines = [candidate.PermanentLines[0], candidate.PermanentLines[1] with { RootMovementId = 10 }] }).Failure);
        Assert.Equal(LogicalMovementCurrentRootFailure.InvalidCurrentGeneration,
            LogicalMovementCurrentRootValidator.Validate(candidate with
            { SelectedGenerations = [candidate.SelectedGenerations[0] with { RootId = 2 }] }).Failure);
    }

    [Theory]
    [InlineData(-1, 1)]
    [InlineData(0, 0)]
    public void Invalid_display_ordinals_fail_closed(int first, int second)
    {
        var candidate = Valid();
        var lines = new[]
        {
            candidate.PermanentLines[0] with { OriginalDisplayOrdinal = first },
            candidate.PermanentLines[1] with { OriginalDisplayOrdinal = second }
        };
        Assert.Equal(LogicalMovementCurrentRootFailure.InvalidPermanentMembership,
            LogicalMovementCurrentRootValidator.Validate(candidate with { PermanentLines = lines }).Failure);
    }

    [Fact]
    public void Root_batch_relationship_is_proven_from_original_physical_membership()
    {
        var candidate = Valid();
        Assert.Equal(LogicalMovementCurrentRootFailure.InvalidRootOriginal,
            LogicalMovementCurrentRootValidator.Validate(candidate with
            { Root = candidate.Root! with { RootMovementBatchId = 31 }, ExistingMovementBatchIds = new HashSet<int> { 30, 31 } }).Failure);

        var single = candidate with
        {
            Root = candidate.Root! with { RootMovementBatchId = null },
            Movements = candidate.Movements.ToDictionary(x => x.Key,
                x => x.Key is 10 or 20 ? x.Value with { MovementBatchId = null } : x.Value),
            ExistingMovementBatchIds = new HashSet<int>()
        };
        Assert.Equal(LogicalMovementCurrentRootResolutionKind.Resolved,
            LogicalMovementCurrentRootValidator.Validate(single).Kind);
    }

    private static LogicalMovementCurrentRootCandidate Valid() => new(
        1,
        new(1, 30, (int)LogicalMovementBatchStatus.Active, null, 0, 2),
        [new(1, 1, 10, 0), new(2, 1, 20, 1)],
        [new(50, 1, 0, 2)],
        [new(100, 1, 50, 1, (int)LogicalMovementLineState.Active, 1, null, null),
         new(101, 1, 50, 2, (int)LogicalMovementLineState.Reversed, null, 2, 3)],
        [new(10, 1, 1, (int)LogicalMovementTransformationRole.RootOriginal, 100),
         new(1, 1, 1, (int)LogicalMovementTransformationRole.CorrectionReplacement, 100),
         new(20, 1, 2, (int)LogicalMovementTransformationRole.RootOriginal, 101),
         new(2, 1, 2, (int)LogicalMovementTransformationRole.Restoration, 101),
         new(3, 1, 2, (int)LogicalMovementTransformationRole.OrdinaryReversal, 101)],
        [new(100, 1, 1), new(101, 1, 2)],
        new Dictionary<long, RawLogicalMovementFact>
        {
            [1] = new(1, null), [2] = new(2, null), [3] = new(3, null),
            [10] = new(10, 30), [20] = new(20, 30)
        },
        new HashSet<int> { 30 });
}
