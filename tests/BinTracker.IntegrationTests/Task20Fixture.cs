using BinTracker.Core;
using BinTracker.Data;
using BinTracker.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;
using Xunit;

namespace BinTracker.IntegrationTests;

// Reuses the proven disposable migration fixture. This is explicit isolated schema17
// composition. Coordinator lifecycle evidence uses Coordinator/StartCoordinatedAsync.
internal sealed class Task20Fixture : IAsyncDisposable
{
    private readonly ServiceProvider services;
    internal OperationalMovementProjectionSchema17Tests.Harness Database { get; }
    internal static DateOnly Today => new(2026, 9, 5);
    internal Task20Projection Projection { get; }
    private Task20ReceiptFailureInjector ReceiptFailure { get; } = new();
    internal IMovementService Movements => services.GetRequiredService<IMovementService>();
    internal IMovementCorrectionService Corrections => services.GetRequiredService<IMovementCorrectionService>();
    internal IMovementHistoryReportService History =>
        services.GetRequiredService<IMovementHistoryReportService>();
    internal IAuditService Audit => services.GetRequiredService<IAuditService>();
    internal ICustomerService Customers => services.GetRequiredService<ICustomerService>();
    internal IImportExecutionService Imports => services.GetRequiredService<IImportExecutionService>();
    internal int CustomerId => Database.CustomerId;

    private Task20Fixture(OperationalMovementProjectionSchema17Tests.Harness database,
        bool enabled, bool projection, UserRole role, IInterceptor? interceptor)
    {
        Database = database;
        Projection = new(new SqliteOperationalMovementProjectionAuthority(database.ConnectionString));
        var collection = new ServiceCollection();
        collection.AddDbContextFactory<BinTrackerDbContext>(b =>
        {
            b.UseSqlite(database.ConnectionString);
            if (interceptor is not null) b.AddInterceptors(interceptor);
        });
        collection.AddSingleton<IBusinessClock>(new Clock());
        collection.AddSingleton<IUserContext>(new User(role));
        collection.AddSingleton<IClientContext>(new Client());
        if (enabled)
        {
            collection.AddScoped<IInitialMovementLineageWriter>(_ =>
                new SqliteInitialMovementLineageWriter(NoInitialMovementLineageFailureInjector.Instance));
            collection.AddScoped<ISingleMovementResponseReceiptStore>(_ =>
                new SqliteSingleMovementResponseReceiptStore(ReceiptFailure));
            collection.AddSingleton<ITransactionalOperationalMovementProjectionAuthority>(Projection);
            collection.AddScoped<IMovementMutationWriter>(_ =>
                new SqliteMovementMutationWriter(NoMovementMutationFailureInjector.Instance));
        }
        if (projection) collection.AddSingleton<IOperationalMovementProjectionAuthority>(Projection);
        collection.AddBinTrackerBusinessServices();
        services = collection.BuildServiceProvider();
    }

    internal static async Task<Task20Fixture> CreateAsync(bool schema17 = true,
        bool enabled = true, bool projection = true, UserRole role = UserRole.Operator,
        Guid? legacySingleOperation = null, IInterceptor? interceptor = null,
        UserRole? nativeActorRole = null, bool legacyCorrection = false) =>
        new(await OperationalMovementProjectionSchema17Tests.Harness.CreateAsync(
            migrateToSchema17: schema17, enableSchema17Writers: enabled,
            enableProjectionBackedServices: projection, userRole: nativeActorRole ?? role,
            beforeMigration: legacySingleOperation is null && !legacyCorrection ? null : async db =>
            {
                if (legacySingleOperation is not null)
                    await CreateAcceptedSchema16SingleAsync(db, legacySingleOperation.Value);
                if (legacyCorrection)
                    await CreateAcceptedSchema16CorrectionAsync(db);
            }), enabled, projection, role, interceptor);

    internal static async Task<Task20Fixture> CreateHistoricalSchema16Async() =>
        new(await OperationalMovementProjectionSchema17Tests.Harness.CreateAsync(
            migrateToSchema17: false,
            enableSchema17Writers: false,
            enableProjectionBackedServices: false,
            beforeMigration: ReproduceHistoricalSchema16MovementShapeAsync),
            enabled: false, projection: false, UserRole.Operator, interceptor: null);

    private static async Task ReproduceHistoricalSchema16MovementShapeAsync(BinTrackerDbContext db)
    {
        // Current EnsureCreated produces the accepted fresh-schema16 form with the
        // self-FK. Rewind only the V13/V14 movement additions, then execute the real
        // numbered V13..V16 catalogue so this fixture reproduces an upgraded database.
        await db.Database.OpenConnectionAsync();
        var connection = (SqliteConnection)db.Database.GetDbConnection();
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT sql FROM sqlite_master WHERE type='table' AND name='BinMovements'";
        var declaration = Convert.ToString(await command.ExecuteScalarAsync())
            ?? throw new InvalidOperationException("BinMovements declaration is missing.");
        var lines = declaration.Split('\n').ToList();
        var reversalForeignKeyLines = lines
            .Select((line, index) => (line, index))
            .Where(x => x.line.Contains("FOREIGN KEY (\"ReversesMovementId\")", StringComparison.Ordinal))
            .ToArray();
        if (reversalForeignKeyLines.Length != 1)
            throw new InvalidOperationException("Fresh schema16 reversal FK shape is unexpected.");
        lines.RemoveAt(reversalForeignKeyLines[0].index);
        for (var i = lines.Count - 1; i >= 0; i--)
        {
            if (lines[i].TrimStart().StartsWith(')')) continue;
            lines[i] = lines[i].TrimEnd().TrimEnd(',');
            break;
        }
        var historicalDeclaration = string.Join('\n', lines);

        command.CommandText = "PRAGMA schema_version";
        var schemaCookie = Convert.ToInt32(await command.ExecuteScalarAsync());
        try
        {
            command.CommandText = "PRAGMA writable_schema=ON";
            await command.ExecuteNonQueryAsync();
            command.CommandText = "UPDATE sqlite_master SET sql=$sql WHERE type='table' AND name='BinMovements'";
            command.Parameters.AddWithValue("$sql", historicalDeclaration);
            await command.ExecuteNonQueryAsync();
        }
        finally
        {
            command.Parameters.Clear();
            command.CommandText = "PRAGMA writable_schema=OFF";
            await command.ExecuteNonQueryAsync();
        }
        command.CommandText = $"PRAGMA schema_version={schemaCookie + 1}";
        await command.ExecuteNonQueryAsync();
        await db.Database.CloseConnectionAsync();
        await db.Database.OpenConnectionAsync();

        await db.Database.ExecuteSqlRawAsync("""
            DROP INDEX IX_BinMovements_ReversesMovementId;
            DROP INDEX IX_BinMovements_ClientOperationId;
            ALTER TABLE BinMovements DROP COLUMN ReversesMovementId;
            ALTER TABLE BinMovements DROP COLUMN CorrectedByMovementId;
            ALTER TABLE BinMovements DROP COLUMN CorrectionReason;
            ALTER TABLE BinMovements DROP COLUMN ClientOperationId;
            UPDATE SchemaVersion SET Version=12 WHERE Id=1;
            """);
        await DatabaseSetup.InitializeSchema16CompatibilityAsync(db);
    }

    private static async Task CreateAcceptedSchema16CorrectionAsync(BinTrackerDbContext db)
    {
        var collection = new ServiceCollection();
        collection.AddDbContextFactory<BinTrackerDbContext>(b => b.UseSqlite(db.Database.GetConnectionString()));
        collection.AddSingleton<IBusinessClock>(new Clock());
        collection.AddSingleton<IUserContext>(new User(UserRole.Operator));
        collection.AddSingleton<IClientContext>(new Client());
        collection.AddBinTrackerBusinessServices();
        await using var legacyServices = collection.BuildServiceProvider();
        var customer = await db.Customers.SingleAsync(x => x.CustomerCode == "PROJ-A");
        var saved = await legacyServices.GetRequiredService<IMovementService>().SaveSingleAsync(
            new(Guid.NewGuid(), Today, MovementType.Out, customer.Id, 1, 7, "legacy", null));
        await legacyServices.GetRequiredService<IMovementCorrectionService>().CorrectAsync(
            new(Guid.NewGuid(), saved.MovementId, Today, customer.Id, 1,
                MovementType.Out, 8, "legacy-corrected", null, "legacy correction"));
    }

    private static async Task CreateAcceptedSchema16SingleAsync(BinTrackerDbContext db, Guid operationId)
    {
        // Use the real dormant/schema16 command path, before migration and before
        // any receipt storage exists. Audit is produced by the service, never used
        // to reconstruct a response receipt or historical lineage.
        var collection = new ServiceCollection();
        collection.AddDbContextFactory<BinTrackerDbContext>(b => b.UseSqlite(db.Database.GetConnectionString()));
        collection.AddSingleton<IBusinessClock>(new Clock());
        collection.AddSingleton<IUserContext>(new User(UserRole.Operator));
        collection.AddSingleton<IClientContext>(new Client());
        collection.AddBinTrackerBusinessServices();
        await using var legacyServices = collection.BuildServiceProvider();
        var customer = await db.Customers.SingleAsync(x => x.CustomerCode == "PROJ-A");
        var saved = await legacyServices.GetRequiredService<IMovementService>().SaveSingleAsync(
            new(operationId, Today, MovementType.Out, customer.Id, 1, 7, "task20", null));
        Assert.Equal(7, saved.NewBalance);
        var movement = Assert.Single(await db.BinMovements.AsNoTracking().ToListAsync());
        Assert.Equal((saved.MovementId, operationId, customer.Id, 1, Today, MovementType.Out, 7),
            (movement.Id, movement.ClientOperationId, movement.CustomerId, movement.ContainerTypeId,
                movement.MovementDate, movement.MovementType, movement.Quantity));
        Assert.Equal(MovementSource.Manual, movement.Source);
        Assert.Equal("task20", movement.ReferenceNumber);
        Assert.Null(movement.Notes);
        Assert.Null(movement.MovementBatchId);
        Assert.Null(movement.ImportRunId);
        Assert.Null(movement.ReversesMovementId);
        Assert.Equal("task20-operator", movement.CreatedBy);
        Assert.Equal(new Clock().UtcNow, movement.CreatedUtc);
        var audit = Assert.Single(await db.AuditEvents.AsNoTracking().ToListAsync());
        Assert.Equal(("MOVEMENT_RECORDED", "BinMovement", saved.MovementId.ToString(), true),
            (audit.Action, audit.EntityType, audit.EntityId, audit.Succeeded));
        Assert.Equal((61, "task20-operator", "task20-session", "task20-device"),
            (audit.UserId, audit.Username, audit.SessionId, audit.ComputerName));
        Assert.Equal(movement.CreatedUtc, audit.TimestampUtc);
        Assert.Equal("OUT (Taken) manual movement recorded: 7 Blue Bin for PROJ-A.", audit.Description);
        Assert.Null(audit.BeforeValues);
        Assert.Null(audit.ReviewedUtc);
        using var payload = JsonDocument.Parse(Assert.IsType<string>(audit.AfterValues));
        Assert.Equal("2026-09-05", payload.RootElement.GetProperty("MovementDate").GetString());
        Assert.Equal("OUT (Taken)", payload.RootElement.GetProperty("Direction").GetString());
        Assert.Equal("PROJ-A", payload.RootElement.GetProperty("Customer").GetString());
        Assert.Equal("Blue Bin", payload.RootElement.GetProperty("Container").GetString());
        Assert.Equal(7, payload.RootElement.GetProperty("Quantity").GetInt32());
        Assert.Equal("task20", payload.RootElement.GetProperty("ReferenceNumber").GetString());
        Assert.Equal("7 OUT", payload.RootElement.GetProperty("NewPosition").GetString());
        await using var command = db.Database.GetDbConnection().CreateCommand();
        await db.Database.OpenConnectionAsync();
        command.CommandText = "SELECT Version FROM SchemaVersion";
        Assert.Equal(16L, await command.ExecuteScalarAsync());
        command.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND (name LIKE 'LogicalMovement%' OR name LIKE '%Receipt%')";
        Assert.Equal(0L, await command.ExecuteScalarAsync());
    }

    internal SaveSingleMovementRequest Single(int quantity = 7, DateOnly? date = null) =>
        new(Guid.NewGuid(), date ?? Today, MovementType.Out, CustomerId, 1, quantity, "task20", null);

    internal void FailReceiptWith(Exception exception) => ReceiptFailure.Arm(exception);
    internal void FailReceiptWith(Func<Exception> failure) => ReceiptFailure.Arm(failure);

    internal async Task ExecuteAsync(string sql)
    {
        await using var connection = await Database.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        await command.ExecuteNonQueryAsync();
    }

    internal async Task<long> ScalarAsync(string sql)
    {
        await using var connection = await Database.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        return Convert.ToInt64(await command.ExecuteScalarAsync());
    }

    internal async Task<long[]> ReadInt64sAsync(string sql)
    {
        await using var connection = await Database.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        var values = new List<long>();
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            values.Add(reader.GetInt64(0));
        return values.ToArray();
    }

    internal async Task<long[]> CountsAsync()
    {
        var counts = new List<long>();
        foreach (var table in new[] { "BinMovements", "MovementBatches", "AuditEvents",
            "LogicalMovementBatches", "LogicalMovementLines", "LogicalMovementGenerations",
            "LogicalMovementGenerationLines", "LogicalMovementLedgerLinks", "MovementCorrectionOperations",
            "SingleMovementResponseReceipts" })
            counts.Add(await ScalarAsync($"SELECT COUNT(*) FROM {table}"));
        return counts.ToArray();
    }

    internal Task StartExistingAsync() => StartAsync(Database.ConnectionString);

    internal SqliteStartupDatabaseCoordinator Coordinator(Func<StartupDatabasePhase, Task>? checkpoint = null,
        ILineageSchema17FailureInjector? migrationFailures = null,
        Func<SqliteConnection, SqliteTransaction, Task>? beforePublicationValidation = null, Action? afterCommit = null)
    {
        var path = new SqliteConnectionStringBuilder(Database.ConnectionString).DataSource;
        var root = Directory.GetParent(Path.GetDirectoryName(path) ?? throw new InvalidOperationException())
            ?? throw new InvalidOperationException();
        return new(path, Path.Combine(root.FullName, "startup-backups"), Path.Combine(root.FullName, "startup-locks"),
            Path.Combine(root.FullName, "pending.json"), checkpoint, migrationFailures, beforePublicationValidation, afterCommit);
    }

    internal async Task StartCoordinatedAsync()
    {
        using var session = await Coordinator().StartAsync();
        Assert.True(session.IsReadyForActivatedHost);
    }

    // Full persisted schema/data evidence detects rewrites as well as added rows,
    // including native Single response receipts.
    internal async Task<string> StateAsync()
    {
        await using var connection = await Database.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT name, sql FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%' ORDER BY name";
        var tables = new List<(string Name, string Sql)>();
        await using (var reader = await command.ExecuteReaderAsync())
            while (await reader.ReadAsync()) tables.Add((reader.GetString(0), reader.GetString(1)));
        var evidence = new List<string>();
        foreach (var (name, sql) in tables)
        {
            evidence.Add(sql);
            command.CommandText = $"SELECT * FROM \"{name.Replace("\"", "\"\"")}\" ORDER BY rowid";
            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var values = new object[reader.FieldCount];
                reader.GetValues(values);
                evidence.Add(JsonSerializer.Serialize(values));
            }
        }
        command.CommandText = "SELECT sql FROM sqlite_master WHERE type IN ('index','trigger','view') AND sql IS NOT NULL ORDER BY type,name";
        await using (var reader = await command.ExecuteReaderAsync())
            while (await reader.ReadAsync()) evidence.Add(reader.GetString(0));
        return JsonSerializer.Serialize(evidence);
    }

    internal static async Task StartAsync(string connectionString)
    {
        // Explicit schema16 compatibility fixture only. Activated-runtime tests
        // use the Data coordinator and normal production composition.
        await using var db = new BinTrackerDbContext(
            new DbContextOptionsBuilder<BinTrackerDbContext>().UseSqlite(connectionString).Options);
        await DatabaseSetup.InitializeSchema16CompatibilityAsync(db);
    }

    public async ValueTask DisposeAsync()
    {
        await services.DisposeAsync();
        await Database.DisposeAsync();
    }

    private sealed class Clock : IBusinessClock
    {
        public DateTime UtcNow => new(2026, 9, 5, 1, 2, 3, DateTimeKind.Utc);
        public DateTime LocalNow => UtcNow;
        public DateOnly Today => Task20Fixture.Today;
        public string TimeZoneId => "UTC";
    }

    private sealed class User(UserRole role) : IUserContext
    {
        public string SessionId => "task20-session";
        public int? UserId => 61;
        public string Username => "task20-operator";
        public string DisplayName => Username;
        public UserRole Role => role;
        public bool MustChangePassword => false;
        public bool IsAuthenticated => true;
    }

    private sealed class Client : IClientContext
    {
        public string ClientInstanceId => "task20-client";
        public string DeviceName => "task20-device";
    }

    private sealed class Task20ReceiptFailureInjector : ISingleMovementResponseReceiptFailureInjector
    {
        private Func<Exception>? failure;
        internal void Arm(Exception exception) => failure = () => exception;
        internal void Arm(Func<Exception> factory) => failure = factory;
        public void ThrowIfRequested(SingleMovementResponseReceiptWriteCheckpoint checkpoint)
        {
            if (failure is null) return;
            var selected = failure;
            failure = null;
            throw selected();
        }
    }
}

internal static class Task20FailureBoundary
{
    // Temporary semantic diagnostic boundary for contracts that do not yet exist.
    // No generic exception type or exact new message is prescribed. Each group
    // must identify the intended domain failure; unrelated runtime/provider faults
    // cannot satisfy this merely by throwing. Replace with the precise typed
    // outcome/code when its production contract is introduced, before acceptance.
    internal static void AssertDomainFailure(Exception? error, params string[][] concepts)
    {
        Assert.NotNull(error);
        for (Exception? cause = error; cause is not null; cause = cause.InnerException)
            Assert.False(cause is ArgumentException or NullReferenceException or IndexOutOfRangeException
                or IOException or SqliteException or DbUpdateException or OperationCanceledException
                or UnauthorizedAccessException or AggregateException, $"Unexpected failure: {error}");
        foreach (var alternatives in concepts)
            Assert.True(alternatives.Any(x => error.Message.Contains(x, StringComparison.OrdinalIgnoreCase)),
                $"Failure does not identify the intended domain path ({string.Join(" / ", alternatives)}). " +
                $"Bind the future typed contract when introduced; actual: {error}");
    }
}

internal sealed class Task20Projection(ITransactionalOperationalMovementProjectionAuthority inner)
    : ITransactionalOperationalMovementProjectionAuthority
{
    internal int IndependentCalls { get; private set; }
    internal int TransactionCalls { get; private set; }
    internal Exception? Failure { get; set; }
    internal Func<Task>? AfterRead { get; set; }
    internal Func<Task>? BeforeRead { get; set; }
    internal OperationalMovementProjectionResult? Result { get; private set; }
    internal OperationalMovementProjectionScope? Scope { get; private set; }

    public Task<T> ReadSnapshotAsync<T>(BinTrackerDbContext db, Func<CancellationToken, Task<T>> read,
        CancellationToken cancellationToken = default) => inner.ReadSnapshotAsync(db, read, cancellationToken);

    public async Task<OperationalMovementProjectionResult> QueryAsync(OperationalMovementProjectionScope scope,
        CancellationToken cancellationToken = default)
    {
        IndependentCalls++;
        if (Failure is not null) throw Failure;
        if (BeforeRead is not null) await BeforeRead();
        Scope = scope;
        Result = await inner.QueryAsync(scope, cancellationToken);
        if (AfterRead is not null) await AfterRead();
        return Result;
    }

    public async Task<OperationalMovementProjectionResult> QueryInTransactionAsync(BinTrackerDbContext db,
        OperationalMovementProjectionScope scope, CancellationToken cancellationToken = default)
    {
        TransactionCalls++;
        Assert.NotNull(db.Database.CurrentTransaction);
        // The real validator rejects an unrooted physical row: it must see Initial
        // publication completed in this caller transaction before projecting.
        if (Failure is not null) throw Failure;
        if (BeforeRead is not null) await BeforeRead();
        Scope = scope;
        Result = await inner.QueryInTransactionAsync(db, scope, cancellationToken);
        if (AfterRead is not null) await AfterRead();
        return Result;
    }
}
