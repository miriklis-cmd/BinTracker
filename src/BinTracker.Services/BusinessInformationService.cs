using BinTracker.Core;
using BinTracker.Data;
using Microsoft.EntityFrameworkCore;

namespace BinTracker.Services;

public sealed record BusinessInformation(
    string BusinessName,
    string TradingName,
    string Abn,
    string Address,
    string Phone,
    string Email,
    string DefaultReportHeader)
{
    public string DisplayName =>
        !string.IsNullOrWhiteSpace(TradingName) ? TradingName :
        !string.IsNullOrWhiteSpace(BusinessName) ? BusinessName :
        "BinTracker";

    public string ReportHeader =>
        !string.IsNullOrWhiteSpace(DefaultReportHeader)
            ? DefaultReportHeader
            : DisplayName;
}

public interface IBusinessInformationService
{
    Task<BusinessInformation> GetAsync(
        CancellationToken cancellationToken = default);

    Task SaveAsync(
        BusinessInformation information,
        CancellationToken cancellationToken = default);
}

internal sealed class BusinessInformationService(
    IDbContextFactory<BinTrackerDbContext> factory,
    UserSession session,
    IAuditService audit) : IBusinessInformationService
{
    public async Task<BusinessInformation> GetAsync(
        CancellationToken cancellationToken = default)
    {
        await using var db = await factory.CreateDbContextAsync(cancellationToken);

        var settings = await db.ApplicationSettings
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == 1, cancellationToken);

        return settings is null
            ? Empty()
            : new BusinessInformation(
                settings.BusinessName ?? string.Empty,
                settings.TradingName ?? string.Empty,
                settings.Abn ?? string.Empty,
                settings.Address ?? string.Empty,
                settings.Phone ?? string.Empty,
                settings.Email ?? string.Empty,
                settings.DefaultReportHeader ?? string.Empty);
    }

    public async Task SaveAsync(
        BusinessInformation information,
        CancellationToken cancellationToken = default)
    {
        if (session.Role != UserRole.Administrator)
            throw new UnauthorizedAccessException(
                "Administrator access is required to edit Business Information.");

        Validate(information);

        await using var db = await factory.CreateDbContextAsync(cancellationToken);

        var settings = await db.ApplicationSettings
            .SingleOrDefaultAsync(x => x.Id == 1, cancellationToken);

        if (settings is null)
        {
            settings = new ApplicationSettings { Id = 1 };
            db.ApplicationSettings.Add(settings);
        }

        var before = new
        {
            settings.BusinessName,
            settings.TradingName,
            settings.Abn,
            settings.Address,
            settings.Phone,
            settings.Email,
            settings.DefaultReportHeader
        };

        settings.BusinessName = Clean(information.BusinessName);
        settings.TradingName = Clean(information.TradingName);
        settings.Abn = Clean(information.Abn);
        settings.Address = Clean(information.Address);
        settings.Phone = Clean(information.Phone);
        settings.Email = Clean(information.Email);
        settings.DefaultReportHeader = Clean(information.DefaultReportHeader);

        await db.SaveChangesAsync(cancellationToken);

        await audit.WriteAsync(
            "BUSINESS_INFORMATION_UPDATED",
            "ApplicationSettings",
            "1",
            "Business Information was updated.",
            before: before,
            after: new
            {
                settings.BusinessName,
                settings.TradingName,
                settings.Abn,
                settings.Address,
                settings.Phone,
                settings.Email,
                settings.DefaultReportHeader
            },
            cancellationToken: cancellationToken);
    }

    private static BusinessInformation Empty() =>
        new(string.Empty, string.Empty, string.Empty, string.Empty,
            string.Empty, string.Empty, string.Empty);

    private static void Validate(BusinessInformation value)
    {
        if (value.BusinessName.Trim().Length > 200)
            throw new ArgumentException("Business name must be 200 characters or fewer.");

        if (value.TradingName.Trim().Length > 200)
            throw new ArgumentException("Trading name must be 200 characters or fewer.");

        if (value.Abn.Trim().Length > 50)
            throw new ArgumentException("ABN must be 50 characters or fewer.");

        if (value.Address.Trim().Length > 500)
            throw new ArgumentException("Address must be 500 characters or fewer.");

        if (value.Phone.Trim().Length > 80)
            throw new ArgumentException("Phone must be 80 characters or fewer.");

        if (value.Email.Trim().Length > 200)
            throw new ArgumentException("Email must be 200 characters or fewer.");

        if (value.DefaultReportHeader.Trim().Length > 200)
            throw new ArgumentException("Default report header must be 200 characters or fewer.");
    }

    private static string? Clean(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
