using BinTracker.Core;
using Microsoft.EntityFrameworkCore;

namespace BinTracker.Data;

/// <summary>
/// Appends one primary audit event to a caller-owned context and transaction.
/// Saving and transaction completion remain the caller's responsibility so the
/// audit cannot commit independently of the future movement-change operation.
/// </summary>
public sealed class TransactionAuditAppender
{
    public AuditEvent AppendPrimary(
        BinTrackerDbContext db,
        AuditEvent auditEvent)
    {
        ArgumentNullException.ThrowIfNull(db);
        ArgumentNullException.ThrowIfNull(auditEvent);

        if (db.Database.CurrentTransaction is null)
        {
            throw new InvalidOperationException(
                "A caller-owned transaction is required to append a primary audit event.");
        }

        if (auditEvent.Id != 0 || db.Entry(auditEvent).State != EntityState.Detached)
        {
            throw new InvalidOperationException(
                "The primary audit event must be a new, untracked event.");
        }

        db.AuditEvents.Add(auditEvent);
        return auditEvent;
    }
}
