using BinTracker.Core;
using BinTracker.Data;
using Microsoft.EntityFrameworkCore;

namespace BinTracker.Services;

public sealed record ContainerTypeListRow(
    int Id,
    string Name,
    string ShortCode,
    int DisplayOrder,
    bool IsActive,
    bool IsSpecialFloorReportContainer,
    long MovementCount);

public sealed record ContainerTypeUsage(
    long MovementCount,
    int CustomersWithBalance,
    DateOnly? FirstUsed,
    DateOnly? LastUsed);

public sealed record ContainerTypeEditModel(
    int Id,
    string Name,
    string ShortCode,
    string SystemCode,
    string? Description,
    string? Notes,
    int DisplayOrder,
    bool IsActive,
    bool IsSpecialFloorReportContainer,
    string? DashboardColour,
    ContainerTypeUsage Usage);

public interface IContainerTypeService
{
    Task<IReadOnlyList<ContainerTypeListRow>> SearchAsync(string? search, bool includeInactive, CancellationToken cancellationToken = default);
    Task<ContainerTypeEditModel?> GetAsync(int id, CancellationToken cancellationToken = default);
    Task<int> SaveAsync(ContainerTypeEditModel model, CancellationToken cancellationToken = default);
    Task SetActiveAsync(int id, bool active, CancellationToken cancellationToken = default);
}

internal sealed class ContainerTypeService(
    IDbContextFactory<BinTrackerDbContext> factory,
    UserSession session,
    IAuditService audit) : IContainerTypeService
{
    public async Task<IReadOnlyList<ContainerTypeListRow>> SearchAsync(
        string? search,
        bool includeInactive,
        CancellationToken cancellationToken = default)
    {
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        var query = db.ContainerTypes.AsNoTracking();

        if (!includeInactive)
            query = query.Where(x => x.IsActive);

        var term = (search ?? string.Empty).Trim();
        if (term.Length > 0)
            query = query.Where(x => x.Name.Contains(term) || x.ShortCode.Contains(term));

        return await query
            .OrderBy(x => x.DisplayOrder)
            .ThenBy(x => x.Name)
            .Select(x => new ContainerTypeListRow(
                x.Id,
                x.Name,
                x.ShortCode,
                x.DisplayOrder,
                x.IsActive,
                x.IsSpecialFloorReportContainer,
                x.Movements.LongCount()))
            .ToListAsync(cancellationToken);
    }

    public async Task<ContainerTypeEditModel?> GetAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        var item = await db.ContainerTypes.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (item is null) return null;

        var movements = db.BinMovements.AsNoTracking().Where(x => x.ContainerTypeId == id);
        var movementCount = await movements.LongCountAsync(cancellationToken);
        var first = await movements.OrderBy(x => x.MovementDate).Select(x => (DateOnly?)x.MovementDate).FirstOrDefaultAsync(cancellationToken);
        var last = await movements.OrderByDescending(x => x.MovementDate).Select(x => (DateOnly?)x.MovementDate).FirstOrDefaultAsync(cancellationToken);

        var customerBalances = await movements
            .GroupBy(x => x.CustomerId)
            .Select(g => g.Sum(x => x.MovementType == MovementType.Out ? x.Quantity : -x.Quantity))
            .ToListAsync(cancellationToken);

        return new ContainerTypeEditModel(
            item.Id, item.Name, item.ShortCode, item.SystemCode,
            item.Description, item.Notes, item.DisplayOrder, item.IsActive,
            item.IsSpecialFloorReportContainer, item.DashboardColour,
            new ContainerTypeUsage(movementCount, customerBalances.Count(x => x != 0), first, last));
    }

    public async Task<int> SaveAsync(ContainerTypeEditModel model, CancellationToken cancellationToken = default)
    {
        RequireAdmin();
        var name = model.Name.Trim();
        var shortCode = model.ShortCode.Trim().ToUpperInvariant();

        if (name.Length < 2) throw new ArgumentException("Container name is required.");
        if (shortCode.Length < 2) throw new ArgumentException("Short code must be at least 2 characters.");
        if (shortCode.Any(ch => !char.IsLetterOrDigit(ch) && ch != '_' && ch != '-'))
            throw new ArgumentException("Short code can contain letters, numbers, hyphen and underscore only.");

        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        if (await db.ContainerTypes.AnyAsync(x => x.Id != model.Id && x.Name.ToUpper() == name.ToUpper(), cancellationToken))
            throw new InvalidOperationException("A container type with that name already exists.");
        if (await db.ContainerTypes.AnyAsync(x => x.Id != model.Id && x.ShortCode.ToUpper() == shortCode, cancellationToken))
            throw new InvalidOperationException("A container type with that short code already exists.");

        ContainerType entity;
        object? before = null;
        if (model.Id == 0)
        {
            entity = new ContainerType
            {
                SystemCode = await CreateSystemCodeAsync(db, shortCode, cancellationToken),
                CreatedUtc = DateTime.UtcNow
            };
            db.ContainerTypes.Add(entity);
        }
        else
        {
            entity = await db.ContainerTypes.SingleAsync(x => x.Id == model.Id, cancellationToken);
            before = Snapshot(entity);
            // SystemCode is intentionally immutable after creation.
        }

        entity.Name = name;
        entity.ShortCode = shortCode;
        entity.Description = Clean(model.Description);
        entity.Notes = Clean(model.Notes);
        entity.DisplayOrder = Math.Max(0, model.DisplayOrder);
        entity.IsActive = model.IsActive;
        entity.IsSpecialFloorReportContainer = model.IsSpecialFloorReportContainer;
        entity.DashboardColour = Clean(model.DashboardColour);
        entity.UpdatedUtc = DateTime.UtcNow;

        await db.SaveChangesAsync(cancellationToken);

        await audit.WriteAsync(
            model.Id == 0 ? "CONTAINER_TYPE_CREATED" : "CONTAINER_TYPE_UPDATED",
            "ContainerType",
            entity.Id.ToString(),
            $"Container type '{entity.Name}' {(model.Id == 0 ? "created" : "updated")}.",
            before: before,
            after: Snapshot(entity),
            cancellationToken: cancellationToken);

        return entity.Id;
    }

    public async Task SetActiveAsync(int id, bool active, CancellationToken cancellationToken = default)
    {
        RequireAdmin();
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        var entity = await db.ContainerTypes.SingleAsync(x => x.Id == id, cancellationToken);
        if (entity.IsActive == active) return;

        var before = entity.IsActive;
        entity.IsActive = active;
        entity.UpdatedUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);

        await audit.WriteAsync(
            active ? "CONTAINER_TYPE_ACTIVATED" : "CONTAINER_TYPE_DEACTIVATED",
            "ContainerType",
            entity.Id.ToString(),
            $"Container type '{entity.Name}' {(active ? "activated" : "deactivated")}.",
            before: new { IsActive = before },
            after: new { entity.IsActive },
            cancellationToken: cancellationToken);
    }

    private async Task<string> CreateSystemCodeAsync(BinTrackerDbContext db, string shortCode, CancellationToken cancellationToken)
    {
        var baseCode = new string(shortCode.ToUpperInvariant().Select(ch => char.IsLetterOrDigit(ch) ? ch : '_').ToArray()).Trim('_');
        if (baseCode.Length == 0) baseCode = "CONTAINER";
        var code = baseCode;
        var suffix = 2;
        while (await db.ContainerTypes.AnyAsync(x => x.SystemCode == code, cancellationToken))
            code = $"{baseCode}_{suffix++}";
        return code;
    }

    private void RequireAdmin()
    {
        if (session.Role != UserRole.Administrator)
            throw new UnauthorizedAccessException("Administrator access is required to manage container types.");
    }

    private static object Snapshot(ContainerType x) => new
    {
        x.Name, x.ShortCode, x.SystemCode, x.Description, x.Notes,
        x.DisplayOrder, x.IsActive, x.IsSpecialFloorReportContainer, x.DashboardColour
    };

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
