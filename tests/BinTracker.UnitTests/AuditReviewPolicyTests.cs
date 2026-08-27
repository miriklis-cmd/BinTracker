using BinTracker.Services;
using Xunit;

namespace BinTracker.UnitTests;

public sealed class AuditReviewPolicyTests
{
    [Fact]
    public void Review_state_is_explicit_for_pending_reviewed_and_not_applicable()
    {
        Assert.Equal("Needs review", AuditReviewPolicy.StateText(true, null));
        Assert.Equal("Reviewed", AuditReviewPolicy.StateText(true, DateTime.UtcNow));
        Assert.Equal("", AuditReviewPolicy.StateText(false, null));
    }

    [Theory]
    [InlineData("MOVEMENT_REVERSED", true, false, true)]
    [InlineData("MOVEMENT_CORRECTED", true, false, true)]
    [InlineData("MOVEMENT_BATCH_CORRECTED", true, false, true)]
    [InlineData("MOVEMENT_CORRECTED", true, true, false)]
    [InlineData("LOGIN_SUCCESS", false, false, false)]
    [InlineData("LOGOUT", false, false, false)]
    [InlineData("MOVEMENT_HISTORY_CSV_EXPORTED", false, false, false)]
    [InlineData("MOVEMENT_RECORDED", false, false, false)]
    [InlineData("MOVEMENT_CORRECTED", false, false, false)]
    public void Mark_reviewed_eligibility_is_deterministic(string action, bool required, bool reviewed, bool expected)
    {
        var row = new AuditTrailRow(1, DateTime.UtcNow, "actor", action, "BinMovement", "10", "description",
            true, required, reviewed ? DateTime.UtcNow : null, reviewed ? 2 : null, reviewed ? "admin" : null);
        Assert.Equal(expected, AuditReviewPolicy.CanMarkReviewed(row));
    }

    [Fact]
    public void Filters_partition_review_state_without_hiding_non_review_events_from_all()
    {
        var pending = Row(true, null); var reviewed = Row(true, DateTime.UtcNow); var ordinary = Row(false, null);
        Assert.All(new[] { pending, reviewed, ordinary }, x => Assert.True(AuditReviewPolicy.Matches(x, AuditReviewFilter.All)));
        Assert.True(AuditReviewPolicy.Matches(pending, AuditReviewFilter.NeedsReview));
        Assert.False(AuditReviewPolicy.Matches(reviewed, AuditReviewFilter.NeedsReview));
        Assert.True(AuditReviewPolicy.Matches(reviewed, AuditReviewFilter.Reviewed));
        Assert.False(AuditReviewPolicy.Matches(ordinary, AuditReviewFilter.Reviewed));
    }

    [Fact]
    public void Pending_route_selects_oldest_then_lowest_event_id_deterministically()
    {
        var time = new DateTime(2026, 8, 27, 1, 0, 0, DateTimeKind.Utc);
        var newer = Row(true, null) with { Id = 3, TimestampUtc = time.AddMinutes(1) };
        var oldestHigherId = Row(true, null) with { Id = 2, TimestampUtc = time };
        var oldestLowerId = Row(true, null) with { Id = 1, TimestampUtc = time };
        Assert.Equal(1, AuditReviewPolicy.SelectOldestPending([newer, oldestHigherId, oldestLowerId])!.Id);
    }

    private static AuditTrailRow Row(bool required, DateTime? reviewed) =>
        new(1, DateTime.UtcNow, "operator", "MOVEMENT_CORRECTED", "BinMovement", "1", "change", true,
            required, reviewed, reviewed.HasValue ? 2 : null, reviewed.HasValue ? "admin" : null);
}
