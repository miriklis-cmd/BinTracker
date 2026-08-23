using BinTracker.Core;

namespace BinTracker.Services;

public interface IUserContext
{
    string SessionId { get; }
    int? UserId { get; }
    string Username { get; }
    string DisplayName { get; }
    UserRole Role { get; }
    bool MustChangePassword { get; }
    bool IsAuthenticated { get; }
}

public interface IBusinessClock
{
    DateTime UtcNow { get; }
    DateTime LocalNow { get; }
    DateOnly Today { get; }
    string TimeZoneId { get; }
}

public interface IClientContext
{
    string ClientInstanceId { get; }
    string DeviceName { get; }
}

internal sealed class ConfiguredBusinessClock : IBusinessClock
{
    private readonly TimeZoneInfo timeZone;

    public ConfiguredBusinessClock()
    {
        var configured = Environment.GetEnvironmentVariable("BINTRACKER_TIMEZONE");
        timeZone = ResolveTimeZone(configured);
    }

    public DateTime UtcNow => DateTime.UtcNow;

    public DateTime LocalNow =>
        TimeZoneInfo.ConvertTimeFromUtc(UtcNow, timeZone);

    public DateOnly Today =>
        DateOnly.FromDateTime(LocalNow);

    public string TimeZoneId => timeZone.Id;

    private static TimeZoneInfo ResolveTimeZone(string? configured)
    {
        if (!string.IsNullOrWhiteSpace(configured))
            return TimeZoneInfo.FindSystemTimeZoneById(configured.Trim());

        // Current BinTracker business default. Windows and Linux use different
        // IDs; support both so the same service layer can later run in an API.
        foreach (var id in new[] { "Australia/Melbourne", "AUS Eastern Standard Time" })
        {
            try { return TimeZoneInfo.FindSystemTimeZoneById(id); }
            catch (TimeZoneNotFoundException) { }
            catch (InvalidTimeZoneException) { }
        }

        return TimeZoneInfo.Local;
    }
}

internal sealed class DesktopClientContext : IClientContext
{
    public string ClientInstanceId { get; } = Guid.NewGuid().ToString("N");
    public string DeviceName => Environment.MachineName;
}

