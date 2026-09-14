using System.Data;
using System.Globalization;
using BinTracker.Core;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace BinTracker.Data;

public interface ISingleMovementResponseReceiptStore
{
    bool IsEnabled { get; }

    Task<SingleMovementResponseReceipt?> FindAsync(
        BinTrackerDbContext db,
        Guid clientOperationId,
        CancellationToken cancellationToken = default);

    Task WriteAsync(
        BinTrackerDbContext db,
        SingleMovementResponseReceipt receipt,
        CancellationToken cancellationToken = default);
}

/// <summary>Preserves schema-16 behavior without probing schema-17 receipt storage.</summary>
public sealed class DormantSingleMovementResponseReceiptStore : ISingleMovementResponseReceiptStore
{
    public bool IsEnabled => false;

    public Task<SingleMovementResponseReceipt?> FindAsync(
        BinTrackerDbContext db,
        Guid clientOperationId,
        CancellationToken cancellationToken = default) => Task.FromResult<SingleMovementResponseReceipt?>(null);

    public Task WriteAsync(
        BinTrackerDbContext db,
        SingleMovementResponseReceipt receipt,
        CancellationToken cancellationToken = default) => Task.CompletedTask;
}

internal enum SingleMovementResponseReceiptWriteCheckpoint
{
    AfterInsert = 0
}

internal interface ISingleMovementResponseReceiptFailureInjector
{
    void ThrowIfRequested(SingleMovementResponseReceiptWriteCheckpoint checkpoint);
}

internal sealed class NoSingleMovementResponseReceiptFailureInjector
    : ISingleMovementResponseReceiptFailureInjector
{
    internal static NoSingleMovementResponseReceiptFailureInjector Instance { get; } = new();
    private NoSingleMovementResponseReceiptFailureInjector() { }
    public void ThrowIfRequested(SingleMovementResponseReceiptWriteCheckpoint checkpoint) { }
}

/// <summary>
/// SQLite schema-17 receipt persistence. The caller owns the context,
/// transaction, save boundary and commit.
/// </summary>
internal sealed class SqliteSingleMovementResponseReceiptStore(
    ISingleMovementResponseReceiptFailureInjector failureInjector)
    : ISingleMovementResponseReceiptStore
{
    private const string InvalidReceipt = "SINGLE_MOVEMENT_RESPONSE_RECEIPT_INVALID";
    private const string PersistenceFailure = "SINGLE_MOVEMENT_RESPONSE_RECEIPT_PERSISTENCE_FAILURE";

    public bool IsEnabled => true;

    public async Task<SingleMovementResponseReceipt?> FindAsync(
        BinTrackerDbContext db,
        Guid clientOperationId,
        CancellationToken cancellationToken = default)
    {
        if (clientOperationId == Guid.Empty)
            throw new ArgumentException("Client operation ID is required.", nameof(clientOperationId));

        var (connection, transaction) = RequireTransaction(db);
        try
        {
            await using var command = Command(connection, transaction, """
                SELECT r.ClientOperationId,r.MovementId,r.BusinessDate,r.ResultingPosition,
                       m.ClientOperationId,m.Source
                FROM SingleMovementResponseReceipts r
                JOIN BinMovements m ON m.Id=r.MovementId
                WHERE r.ClientOperationId=$operation;
                """, ("$operation", clientOperationId));
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken))
                return null;

            var receiptOperation = ReadGuid(reader, 0);
            var movementId = reader.GetInt64(1);
            var dateText = reader.GetString(2);
            var position = reader.GetInt64(3);
            var movementOperation = ReadGuid(reader, 4);
            var source = reader.GetInt32(5);
            if (await reader.ReadAsync(cancellationToken) ||
                receiptOperation != clientOperationId ||
                movementOperation != clientOperationId ||
                source != (int)MovementSource.Manual ||
                !DateOnly.TryParseExact(dateText, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out var businessDate) ||
                position is < int.MinValue or > int.MaxValue)
            {
                throw new InvalidOperationException(InvalidReceipt);
            }

            return new SingleMovementResponseReceipt(
                receiptOperation,
                movementId,
                businessDate,
                checked((int)position));
        }
        catch (SqliteException exception)
        {
            throw new InvalidOperationException(PersistenceFailure, exception);
        }
    }

    public async Task WriteAsync(
        BinTrackerDbContext db,
        SingleMovementResponseReceipt receipt,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(receipt);
        var (connection, transaction) = RequireTransaction(db);
        try
        {
            await using (var identity = Command(connection, transaction, """
                SELECT COUNT(*)
                FROM BinMovements
                WHERE Id=$movement AND ClientOperationId=$operation AND Source=$source;
                """, ("$movement", receipt.MovementId),
                ("$operation", receipt.ClientOperationId),
                ("$source", (int)MovementSource.Manual)))
            {
                if (Convert.ToInt64(await identity.ExecuteScalarAsync(cancellationToken)) != 1)
                    throw new InvalidOperationException(InvalidReceipt);
            }

            await using var insert = Command(connection, transaction, """
                INSERT INTO SingleMovementResponseReceipts
                    (ClientOperationId,MovementId,BusinessDate,ResultingPosition)
                VALUES ($operation,$movement,$date,$position);
                """, ("$operation", receipt.ClientOperationId),
                ("$movement", receipt.MovementId),
                ("$date", receipt.BusinessDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)),
                ("$position", receipt.ResultingPosition));
            if (await insert.ExecuteNonQueryAsync(cancellationToken) != 1)
                throw new InvalidOperationException(PersistenceFailure);
            failureInjector.ThrowIfRequested(SingleMovementResponseReceiptWriteCheckpoint.AfterInsert);
        }
        catch (SqliteException exception)
        {
            throw new InvalidOperationException(PersistenceFailure, exception);
        }
    }

    private static (SqliteConnection Connection, SqliteTransaction Transaction) RequireTransaction(
        BinTrackerDbContext db)
    {
        ArgumentNullException.ThrowIfNull(db);
        if (db.Database.CurrentTransaction is null ||
            db.Database.GetDbConnection() is not SqliteConnection connection ||
            db.Database.CurrentTransaction.GetDbTransaction() is not SqliteTransaction transaction ||
            connection.State != ConnectionState.Open)
        {
            throw new InvalidOperationException(
                "The SQLite Single response receipt store requires the caller's active SQLite transaction.");
        }

        return (connection, transaction);
    }

    private static Guid ReadGuid(SqliteDataReader reader, int ordinal) =>
        reader.GetFieldValue<Guid>(ordinal);

    private static SqliteCommand Command(
        SqliteConnection connection,
        SqliteTransaction transaction,
        string sql,
        params (string Name, object Value)[] parameters)
    {
        var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = sql;
        foreach (var (name, value) in parameters)
            command.Parameters.AddWithValue(name, value);
        return command;
    }
}
