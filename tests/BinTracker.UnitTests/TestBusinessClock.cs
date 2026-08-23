using BinTracker.Services;

namespace BinTracker.UnitTests;

internal sealed class TestBusinessClock(
    DateOnly? today = null) : IBusinessClock
{
    private readonly DateOnly value = today ?? new DateOnly(2026, 8, 23);

    public DateTime UtcNow =>
        value.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);

    public DateTime LocalNow =>
        value.ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified);

    public DateOnly Today => value;

    public string TimeZoneId => "Australia/Melbourne";
}

