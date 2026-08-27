using BinTracker.Core;
using BinTracker.Data;
using Microsoft.EntityFrameworkCore;

namespace BinTracker.Services;

internal static class EffectiveMovementQuery
{
    /// <summary>
    /// Operational reports show the corrected replacement as the effective
    /// history. The preserved original and correction-only neutraliser remain
    /// queryable in Movement History and Audit Trail, but do not masquerade as
    /// physical activity in the wrong day/week/month.
    /// </summary>
    public static IQueryable<BinMovement> EffectiveOperationalMovements(
        this BinTrackerDbContext db) =>
        db.BinMovements.AsNoTracking().Where(movement =>
            !db.MovementCorrectionLines.Any(line =>
                line.OriginalMovementId == movement.Id ||
                line.NeutralisingMovementId == movement.Id));
}
