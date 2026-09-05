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
    ContainerTypeUsage Usage,
    long Revision);

public interface IContainerTypeService
{
    Task<IReadOnlyList<ContainerTypeListRow>> SearchAsync(string? search, bool includeInactive, CancellationToken cancellationToken = default);
    Task<ContainerTypeEditModel?> GetAsync(int id, CancellationToken cancellationToken = default);
    Task<int> SaveAsync(ContainerTypeEditModel model, CancellationToken cancellationToken = default);
    Task SetActiveAsync(int id, bool active, CancellationToken cancellationToken = default);
}

internal sealed class ContainerTypeService(
    IDbContextFactory<BinTrackerDbContext> factory,
    IUserContext session,
    IAuditService audit,
    IBusinessClock clock,
    IOperationalMovementProjectionAuthority? operationalProjection = null) : IContainerTypeService
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

        int customersWithBalance;
        if (operationalProjection is null)
        {
            var customerBalances = await movements
                .GroupBy(x => x.CustomerId)
                .Select(g => g.Sum(x => x.MovementType == MovementType.Out ? x.Quantity : -x.Quantity))
                .ToListAsync(cancellationToken);
            customersWithBalance = customerBalances.Count(x => x != 0);
        }
        else
        {
            var projected = await operationalProjection.QueryAsync(
                OperationalMovementProjectionScope.PositionAsOf(
                    clock.Today,
                    containerTypeId: id),
                cancellationToken);
            customersWithBalance = projected.Positions.Count(x => x.Quantity != 0);
        }

        return new ContainerTypeEditModel(
            item.Id, item.Name, item.ShortCode, item.SystemCode,
            item.Description, item.Notes, item.DisplayOrder, item.IsActive,
            item.IsSpecialFloorReportContainer, item.DashboardColour,
            new ContainerTypeUsage(movementCount, customersWithBalance, first, last),
            item.Revision);
    }

    public async Task<int> SaveAsync(ContainerTypeEditModel model, CancellationToken cancellationToken = default)
    {
        RequireAdmin();
        var name = model.Name.Trim();
        var nameKey = ContainerTypeNameKey.Normalize(name);
        var shortCode = model.ShortCode.Trim().ToUpperInvariant();

        if (name.Length < 2) throw new ArgumentException("Container name is required.");
        if (shortCode.Length < 2) throw new ArgumentException("Short code must be at least 2 characters.");
        if (shortCode.Any(ch => !char.IsLetterOrDigit(ch) && ch != '_' && ch != '-'))
            throw new ArgumentException("Short code can contain letters, numbers, hyphen and underscore only.");

        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        if (await db.ContainerTypes.AnyAsync(
                x => x.Id != model.Id && x.NameKey == nameKey,
                cancellationToken))
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
                CreatedUtc = clock.UtcNow
            };
            db.ContainerTypes.Add(entity);
        }
        else
        {
            entity = await db.ContainerTypes.SingleAsync(x => x.Id == model.Id, cancellationToken);
            db.Entry(entity).Property(x => x.Revision).OriginalValue = model.Revision;
            before = Snapshot(entity);
            // SystemCode is intentionally immutable after creation.
        }

        entity.Name = name;
        entity.NameKey = nameKey;
        entity.ShortCode = shortCode;
        entity.Description = Clean(model.Description);
        entity.Notes = Clean(model.Notes);
        entity.DisplayOrder = Math.Max(0, model.DisplayOrder);
        entity.IsActive = model.IsActive;
        entity.IsSpecialFloorReportContainer = model.IsSpecialFloorReportContainer;
        entity.DashboardColour = Clean(model.DashboardColour);
        entity.UpdatedUtc = clock.UtcNow;
        if (model.Id != 0) entity.Revision++;

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new InvalidOperationException(
                "This container type was changed by another user after you opened it. Reload it, review the latest values, and try again.");
        }
        catch (DbUpdateException)
        {
            await using var verify =
                await factory.CreateDbContextAsync(cancellationToken);

            if (await verify.ContainerTypes.AsNoTracking().AnyAsync(
                    x => x.Id != model.Id && x.NameKey == nameKey,
                    cancellationToken))
            {
                throw new InvalidOperationException(
                    "A container type with that name already exists.");
            }

            if (await verify.ContainerTypes.AsNoTracking().AnyAsync(
                    x => x.Id != model.Id && x.ShortCode == shortCode,
                    cancellationToken))
            {
                throw new InvalidOperationException(
                    "A container type with that short code already exists.");
            }

            throw;
        }

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
        entity.UpdatedUtc = clock.UtcNow;
        entity.Revision++;
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new InvalidOperationException(
                "This container type was changed by another user while its active status was being updated. Reload and try again.");
        }

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
        x.Name, x.NameKey, x.ShortCode, x.SystemCode, x.Description, x.Notes,
        x.DisplayOrder, x.IsActive, x.IsSpecialFloorReportContainer, x.DashboardColour
    };

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
