using System.Text.Json;
using BinTracker.Core;
using BinTracker.Data;

namespace BinTracker.Services;

internal interface ITransactionalAudit
{
    Task<TResult> ExecuteAsync<TResult>(
        Func<TransactionalAuditScope, CancellationToken, Task<TResult>> operation,
        CancellationToken cancellationToken = default);
}

internal sealed class TransactionalAudit(
    PersistenceTransactionRunner transactions,
    TransactionAuditAppender appender,
    IUserContext user,
    IBusinessClock clock,
    IClientContext client) : ITransactionalAudit
{
    public Task<TResult> ExecuteAsync<TResult>(
        Func<TransactionalAuditScope, CancellationToken, Task<TResult>> operation,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(operation);
        return transactions.ExecuteAsync(
            (db, token) => operation(
                new TransactionalAuditScope(db, appender, user, clock, client),
                token),
            cancellationToken);
    }
}

internal sealed class TransactionalAuditScope(
    BinTrackerDbContext db,
    TransactionAuditAppender appender,
    IUserContext user,
    IBusinessClock clock,
    IClientContext client)
{
    public BinTrackerDbContext Db => db;

    public AuditEvent Append(
        string action,
        string entityType,
        string? entityId,
        string description,
        bool succeeded = true,
        object? before = null,
        object? after = null) =>
        AppendAuthentication(
            action,
            entityType,
            entityId,
            description,
            user.UserId,
            user.Username,
            succeeded,
            before,
            after);

    public AuditEvent AppendAuthentication(
        string action,
        string entityType,
        string? entityId,
        string description,
        int? actorUserId,
        string actorUsername,
        bool succeeded = true,
        object? before = null,
        object? after = null) =>
        appender.AppendPrimary(
            db,
            TrustedAuditEventFactory.Create(
                action,
                entityType,
                entityId,
                description,
                actorUserId,
                actorUsername,
                succeeded,
                before,
                after,
                clock,
                client,
                user.SessionId));
}

internal static class TrustedAuditEventFactory
{
    public static AuditEvent Create(
        string action,
        string entityType,
        string? entityId,
        string description,
        int? actorUserId,
        string actorUsername,
        bool succeeded,
        object? before,
        object? after,
        IBusinessClock clock,
        IClientContext client,
        string sessionId) =>
        new()
        {
            TimestampUtc = clock.UtcNow,
            UserId = actorUserId,
            Username = actorUsername,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            Description = description,
            BeforeValues = before is null ? null : JsonSerializer.Serialize(before),
            AfterValues = after is null ? null : JsonSerializer.Serialize(after),
            ComputerName = client.DeviceName,
            SessionId = sessionId,
            Succeeded = succeeded
        };
}
