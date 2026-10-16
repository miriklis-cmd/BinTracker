using Microsoft.EntityFrameworkCore;

namespace BinTracker.Data;

/// <summary>
/// Owns the provider transaction used by one Services workflow. The callback
/// may save intermediate state to obtain generated identifiers, but no state
/// becomes durable until this runner commits the transaction.
/// </summary>
public sealed class PersistenceTransactionRunner(
    IDbContextFactory<BinTrackerDbContext> factory)
{
    public async Task<TResult> ExecuteAsync<TResult>(
        Func<BinTrackerDbContext, CancellationToken, Task<TResult>> operation,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(operation);

        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        if (db.Database.CurrentTransaction is not null)
        {
            throw new InvalidOperationException(
                "The persistence transaction runner requires a context without an active transaction.");
        }

        await using var transaction =
            await db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var result = await operation(db, cancellationToken);
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return result;
        }
        catch (Exception operationException)
        {
            try
            {
                await transaction.RollbackAsync(CancellationToken.None);
            }
            catch (Exception rollbackException)
            {
                throw new AggregateException(
                    "The persistence workflow failed and its transaction could not be rolled back.",
                    operationException,
                    rollbackException);
            }

            throw;
        }
    }
}
