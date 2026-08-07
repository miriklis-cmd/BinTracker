using BinTracker.Core;
using BinTracker.Data;
using Microsoft.EntityFrameworkCore;

namespace BinTracker.Services;

public sealed record CustomerListRow(int Id, string Name, string CustomerCode, CustomerType CustomerType, bool IsActive, int NetBalance);
public sealed record CustomerBalanceRow(string ContainerType, int Balance)
{
    public string Position => Balance > 0 ? $"{Balance} OUT" : Balance < 0 ? $"{Math.Abs(Balance)} CREDIT" : "Even";
}
public sealed record CustomerMovementRow(DateOnly Date, string Direction, string ContainerType, int Quantity, string? Reference, string? CreatedBy);
public sealed record CustomerStatementMovement(DateOnly Date, string Direction, int Quantity, int RunningBalance, string? Reference);
public sealed record CustomerStatementContainer(
    string ContainerType,
    int OpeningBalance,
    int ClosingBalance,
    IReadOnlyList<CustomerStatementMovement> Movements);
public sealed record CustomerStatementData(
    int CustomerId,
    string CustomerCode,
    string CustomerName,
    DateOnly FromDate,
    DateOnly ToDate,
    IReadOnlyList<CustomerStatementContainer> Containers);

public sealed class CustomerEditModel
{
    public int Id { get; set; }
    public string CustomerCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public CustomerType CustomerType { get; set; } = CustomerType.Account;
    public string? ContactName { get; set; }
    public string? Phone { get; set; }
    public string? MobileNumber { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;
    public bool AllowEmailReminders { get; set; } = true;
    public bool AllowSmsReminders { get; set; } = true;
    public bool ReminderOptOut { get; set; }
}

public interface ICustomerService
{
    Task<IReadOnlyList<CustomerListRow>> SearchAsync(string? query, bool includeInactive, CancellationToken cancellationToken = default);
    Task<CustomerEditModel?> GetAsync(int id, CancellationToken cancellationToken = default);
    Task<int> SaveAsync(CustomerEditModel model, CancellationToken cancellationToken = default);
    Task SetActiveAsync(int id, bool active, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CustomerBalanceRow>> GetBalancesAsync(int customerId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CustomerMovementRow>> GetRecentMovementsAsync(int customerId, int limit = 100, CancellationToken cancellationToken = default);
    Task<CustomerStatementData> GetStatementAsync(int customerId, DateOnly fromDate, DateOnly toDate, CancellationToken cancellationToken = default);
}

internal sealed class CustomerService(
    IDbContextFactory<BinTrackerDbContext> factory,
    UserSession session,
    IAuditService audit) : ICustomerService
{
    public async Task<IReadOnlyList<CustomerListRow>> SearchAsync(string? query, bool includeInactive, CancellationToken cancellationToken = default)
    {
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        var customers = db.Customers.AsNoTracking();
        if (!includeInactive) customers = customers.Where(x => x.IsActive);
        query = query?.Trim();
        if (!string.IsNullOrWhiteSpace(query))
            customers = customers.Where(x =>
                x.Name.Contains(query) ||
                (x.CustomerCode != null && x.CustomerCode.Contains(query)) ||
                (x.ContactName != null && x.ContactName.Contains(query)) ||
                (x.Phone != null && x.Phone.Contains(query)) ||
                (x.MobileNumber != null && x.MobileNumber.Contains(query)) ||
                (x.Email != null && x.Email.Contains(query)));

        return await customers
            .OrderBy(x => x.CustomerCode)
            .ThenBy(x => x.Name)
            .Select(x => new CustomerListRow(
                x.Id, x.Name, x.CustomerCode ?? string.Empty, x.CustomerType, x.IsActive,
                x.Movements.Sum(m => m.MovementType == MovementType.Out ? m.Quantity : -m.Quantity)))
            .ToListAsync(cancellationToken);
    }

    public async Task<CustomerEditModel?> GetAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        return await db.Customers.AsNoTracking().Where(x => x.Id == id).Select(x => new CustomerEditModel
        {
            Id=x.Id, CustomerCode=x.CustomerCode ?? string.Empty, Name=x.Name, CustomerType=x.CustomerType, ContactName=x.ContactName,
            Phone=x.Phone, MobileNumber=x.MobileNumber, Email=x.Email, Address=x.Address, Notes=x.Notes,
            IsActive=x.IsActive, AllowEmailReminders=x.AllowEmailReminders,
            AllowSmsReminders=x.AllowSmsReminders, ReminderOptOut=x.ReminderOptOut
        }).SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<int> SaveAsync(CustomerEditModel model, CancellationToken cancellationToken = default)
    {
        RequireEditor();
        model.Name = model.Name.Trim();
        model.CustomerCode = (Clean(model.CustomerCode) ?? string.Empty).ToUpperInvariant();
        model.ContactName = Clean(model.ContactName);
        model.Phone = Clean(model.Phone);
        model.MobileNumber = Clean(model.MobileNumber);
        model.Email = Clean(model.Email);
        model.Address = Clean(model.Address);
        model.Notes = Clean(model.Notes);

        if (model.CustomerCode.Length < 1)
            throw new ArgumentException("Customer code is required.");
        if (model.Name.Length < 2)
            throw new ArgumentException("Customer name is required.");

        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        // Customer codes are business identifiers and are unique irrespective of case.
        // This deliberately treats "Albury", "ALBURY" and "albury" as the same code.
        var normalisedCode = model.CustomerCode.ToUpperInvariant();
        if (await db.Customers.AnyAsync(
                x => x.Id != model.Id &&
                     x.CustomerCode != null &&
                     x.CustomerCode.ToUpper() == normalisedCode,
                cancellationToken))
            throw new InvalidOperationException(
                $"Customer code '{model.CustomerCode}' is already in use. Customer codes are not case-sensitive.");

        if (model.Id == 0)
        {
            var entity = new Customer
            {
                CustomerCode=model.CustomerCode, Name=model.Name, CustomerType=model.CustomerType, ContactName=model.ContactName,
                Phone=model.Phone, MobileNumber=model.MobileNumber, Email=model.Email, Address=model.Address,
                Notes=model.Notes, IsActive=true, AllowEmailReminders=model.AllowEmailReminders,
                AllowSmsReminders=model.AllowSmsReminders, ReminderOptOut=model.ReminderOptOut,
                CreatedUtc=DateTime.UtcNow, UpdatedUtc=DateTime.UtcNow,
                CreatedByUserId=session.UserId, UpdatedByUserId=session.UserId
            };
            db.Customers.Add(entity);
            await db.SaveChangesAsync(cancellationToken);
            await audit.WriteAsync("CUSTOMER_CREATED", "Customer", entity.Id.ToString(),
                $"Customer '{entity.CustomerCode} - {entity.Name}' created.", after: Snapshot(entity), cancellationToken:cancellationToken);
            return entity.Id;
        }

        var customer = await db.Customers.SingleAsync(x => x.Id == model.Id, cancellationToken);
        var before = Snapshot(customer);
        customer.CustomerCode=model.CustomerCode; customer.Name=model.Name; customer.CustomerType=model.CustomerType; customer.ContactName=model.ContactName;
        customer.Phone=model.Phone; customer.MobileNumber=model.MobileNumber; customer.Email=model.Email;
        customer.Address=model.Address; customer.Notes=model.Notes;
        customer.AllowEmailReminders=model.AllowEmailReminders; customer.AllowSmsReminders=model.AllowSmsReminders;
        customer.ReminderOptOut=model.ReminderOptOut; customer.UpdatedUtc=DateTime.UtcNow; customer.UpdatedByUserId=session.UserId;
        await db.SaveChangesAsync(cancellationToken);
        await audit.WriteAsync("CUSTOMER_UPDATED", "Customer", customer.Id.ToString(),
            $"Customer '{customer.CustomerCode} - {customer.Name}' updated.", before:before, after:Snapshot(customer), cancellationToken:cancellationToken);
        return customer.Id;
    }

    public async Task SetActiveAsync(int id, bool active, CancellationToken cancellationToken = default)
    {
        RequireEditor();
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        var customer = await db.Customers.SingleAsync(x => x.Id == id, cancellationToken);
        if (customer.IsActive == active) return;
        var before = customer.IsActive;
        customer.IsActive = active;
        customer.UpdatedUtc = DateTime.UtcNow;
        customer.UpdatedByUserId = session.UserId;
        await db.SaveChangesAsync(cancellationToken);
        await audit.WriteAsync(active ? "CUSTOMER_ACTIVATED" : "CUSTOMER_DEACTIVATED", "Customer", id.ToString(),
            $"Customer '{customer.CustomerCode} - {customer.Name}' {(active ? "reactivated" : "deactivated")}.",
            before:new { IsActive=before }, after:new { customer.IsActive }, cancellationToken:cancellationToken);
    }

    public async Task<IReadOnlyList<CustomerBalanceRow>> GetBalancesAsync(int customerId, CancellationToken cancellationToken = default)
    {
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        var types = await db.ContainerTypes.AsNoTracking().OrderBy(x => x.DisplayOrder).ThenBy(x => x.Name).ToListAsync(cancellationToken);
        var sums = await db.BinMovements.AsNoTracking().Where(x => x.CustomerId == customerId)
            .GroupBy(x => x.ContainerTypeId)
            .Select(g => new { ContainerTypeId=g.Key, Balance=g.Sum(x => x.MovementType == MovementType.Out ? x.Quantity : -x.Quantity) })
            .ToDictionaryAsync(x => x.ContainerTypeId, x => x.Balance, cancellationToken);
        return types.Select(t => new CustomerBalanceRow(t.Name, sums.GetValueOrDefault(t.Id))).ToList();
    }

    public async Task<IReadOnlyList<CustomerMovementRow>> GetRecentMovementsAsync(int customerId, int limit = 100, CancellationToken cancellationToken = default)
    {
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        return await db.BinMovements.AsNoTracking().Where(x => x.CustomerId == customerId)
            .OrderByDescending(x => x.MovementDate).ThenByDescending(x => x.Id)
            .Take(Math.Clamp(limit, 1, 1000))
            .Select(x => new CustomerMovementRow(
                x.MovementDate,
                x.MovementType == MovementType.In ? "IN (Returned)" : "OUT (Taken)",
                x.ContainerType.Name,
                x.Quantity,
                x.ReferenceNumber,
                x.CreatedBy))
            .ToListAsync(cancellationToken);
    }

    public async Task<CustomerStatementData> GetStatementAsync(int customerId, DateOnly fromDate, DateOnly toDate, CancellationToken cancellationToken = default)
    {
        if (toDate < fromDate) throw new ArgumentException("Statement end date must be on or after the start date.");

        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        var customer = await db.Customers.AsNoTracking().SingleAsync(x => x.Id == customerId, cancellationToken);
        var containerTypes = await db.ContainerTypes.AsNoTracking().OrderBy(x => x.DisplayOrder).ThenBy(x => x.Name).ToListAsync(cancellationToken);
        var all = await db.BinMovements.AsNoTracking()
            .Where(x => x.CustomerId == customerId && x.MovementDate <= toDate)
            .OrderBy(x => x.MovementDate).ThenBy(x => x.Id)
            .Select(x => new { x.ContainerTypeId, x.MovementDate, x.MovementType, x.Quantity, x.ReferenceNumber })
            .ToListAsync(cancellationToken);

        var sections = new List<CustomerStatementContainer>();
        foreach (var type in containerTypes)
        {
            var typed = all.Where(x => x.ContainerTypeId == type.Id).ToList();
            var opening = typed.Where(x => x.MovementDate < fromDate)
                .Sum(x => x.MovementType == MovementType.Out ? x.Quantity : -x.Quantity);
            var running = opening;
            var movements = new List<CustomerStatementMovement>();
            foreach (var movement in typed.Where(x => x.MovementDate >= fromDate))
            {
                running += movement.MovementType == MovementType.Out ? movement.Quantity : -movement.Quantity;
                movements.Add(new CustomerStatementMovement(
                    movement.MovementDate,
                    movement.MovementType == MovementType.Out ? "OUT (Taken)" : "IN (Returned)",
                    movement.Quantity,
                    running,
                    movement.ReferenceNumber));
            }

            if (opening != 0 || running != 0 || movements.Count > 0)
                sections.Add(new CustomerStatementContainer(type.Name, opening, running, movements));
        }

        return new CustomerStatementData(customer.Id, customer.CustomerCode ?? string.Empty, customer.Name, fromDate, toDate, sections);
    }

    private void RequireEditor()
    {
        if (session.Role == UserRole.Viewer)
            throw new UnauthorizedAccessException("Viewer accounts cannot change customer records.");
    }

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static object Snapshot(Customer x) => new
    {
        x.CustomerCode, x.Name, x.CustomerType, x.ContactName, x.Phone, x.MobileNumber, x.Email, x.Address, x.Notes,
        x.AllowEmailReminders, x.AllowSmsReminders, x.ReminderOptOut, x.IsActive
    };
}
