using System.Data;
using System.Globalization;
using BinTracker.Core;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace BinTracker.Data;

public enum MovementMutationFreshStateKind
{
    Replay = 0,
    OperationIdConflict = 1,
    StaleGeneration = 2,
    NotFound = 3,
    Unhealthy = 4,
    IntegrityFailure = 5
}

public sealed record MovementMutationReplay(
    long OperationId,
    LogicalMovementBatchId RootId,
    LogicalMovementGenerationNumber ResultGeneration,
    int? PhysicalOutputBatchId);

public sealed record MovementMutationFreshState(
    MovementMutationFreshStateKind Kind,
    MovementMutationReplay? Replay = null);

public sealed record PersistedMovementMutationLineResult(
    LogicalMovementLineId LineId,
    LogicalMovementGenerationAction Action,
    LogicalMovementLineState State,
    MovementChangeField AppliedFieldMask,
    long EffectiveMovementId,
    long? TerminalReversalMovementId);

public sealed record PersistedMovementMutationMovementResult(
    LogicalMovementLineId LineId,
    PlannedMovementPurpose Purpose,
    long MovementId);

public sealed class PendingMovementMutation
{
    internal PendingMovementMutation(
        long operationId,
        LogicalMovementBatchId rootId,
        LogicalMovementGenerationNumber resultGeneration,
        long generationId,
        int? physicalOutputBatchId,
        IReadOnlyList<PersistedMovementMutationLineResult> lines,
        IReadOnlyList<PersistedMovementMutationMovementResult> movements)
    {
        OperationId = operationId;
        RootId = rootId;
        ResultGeneration = resultGeneration;
        GenerationId = generationId;
        PhysicalOutputBatchId = physicalOutputBatchId;
        Lines = lines;
        Movements = movements;
    }

    public long OperationId { get; }
    public LogicalMovementBatchId RootId { get; }
    public LogicalMovementGenerationNumber ResultGeneration { get; }
    public long GenerationId { get; }
    public int? PhysicalOutputBatchId { get; }
    public IReadOnlyList<PersistedMovementMutationLineResult> Lines { get; }
    public IReadOnlyList<PersistedMovementMutationMovementResult> Movements { get; }
}

public interface IMovementMutationWriter
{
    bool IsEnabled { get; }
    Task EnsureReadyAsync(BinTrackerDbContext db, LogicalMovementBatchId rootId,
        CancellationToken cancellationToken = default);
    Task<MovementMutationReplay?> FindCommittedAsync(BinTrackerDbContext db,
        MovementMutationOperationIntent intent, CancellationToken cancellationToken = default);
    Task<TrustedMovementPlanningSnapshot> MaterializeAsync(BinTrackerDbContext db,
        LogicalMovementBatchId rootId, CancellationToken cancellationToken = default);
    Task<PendingMovementMutation> PersistAsync(BinTrackerDbContext db,
        MovementMutationOperationIntent intent, TrustedMovementPlanningSnapshot snapshot,
        MovementMutationPlan plan, int actorUserId, string actorUsername,
        DateTime createdUtc, CancellationToken cancellationToken = default);
    Task AssociatePrimaryAuditAsync(BinTrackerDbContext db, PendingMovementMutation pending,
        long auditEventId, CancellationToken cancellationToken = default);
    Task ValidateOperationAuditHealthAsync(BinTrackerDbContext db, LogicalMovementBatchId rootId,
        CancellationToken cancellationToken = default);
    Task<bool> TryPublishAsync(BinTrackerDbContext db, PendingMovementMutation pending,
        LogicalMovementGenerationNumber expectedGeneration,
        CancellationToken cancellationToken = default);
    Task ValidatePublishedAsync(BinTrackerDbContext db, PendingMovementMutation pending,
        CancellationToken cancellationToken = default);
    Task<MovementMutationFreshState> ClassifyFreshAsync(BinTrackerDbContext db,
        MovementMutationOperationIntent intent, CancellationToken cancellationToken = default);
}

/// <summary>Preserves schema-16 runtime composition without probing schema 17.</summary>
public sealed class DormantMovementMutationWriter : IMovementMutationWriter
{
    private const string Dormant = "Logical movement mutation execution is not active in normal runtime composition.";
    public bool IsEnabled => false;
    public Task EnsureReadyAsync(BinTrackerDbContext db, LogicalMovementBatchId rootId,
        CancellationToken cancellationToken = default) => Fail();
    public Task<MovementMutationReplay?> FindCommittedAsync(BinTrackerDbContext db,
        MovementMutationOperationIntent intent, CancellationToken cancellationToken = default) => Fail<MovementMutationReplay?>();
    public Task<TrustedMovementPlanningSnapshot> MaterializeAsync(BinTrackerDbContext db,
        LogicalMovementBatchId rootId, CancellationToken cancellationToken = default) => Fail<TrustedMovementPlanningSnapshot>();
    public Task<PendingMovementMutation> PersistAsync(BinTrackerDbContext db,
        MovementMutationOperationIntent intent, TrustedMovementPlanningSnapshot snapshot,
        MovementMutationPlan plan, int actorUserId, string actorUsername,
        DateTime createdUtc, CancellationToken cancellationToken = default) => Fail<PendingMovementMutation>();
    public Task AssociatePrimaryAuditAsync(BinTrackerDbContext db, PendingMovementMutation pending,
        long auditEventId, CancellationToken cancellationToken = default) => Fail();
    public Task ValidateOperationAuditHealthAsync(BinTrackerDbContext db, LogicalMovementBatchId rootId,
        CancellationToken cancellationToken = default) => Fail();
    public Task<bool> TryPublishAsync(BinTrackerDbContext db, PendingMovementMutation pending,
        LogicalMovementGenerationNumber expectedGeneration,
        CancellationToken cancellationToken = default) => Fail<bool>();
    public Task ValidatePublishedAsync(BinTrackerDbContext db, PendingMovementMutation pending,
        CancellationToken cancellationToken = default) => Fail();
    public Task<MovementMutationFreshState> ClassifyFreshAsync(BinTrackerDbContext db,
        MovementMutationOperationIntent intent, CancellationToken cancellationToken = default) => Fail<MovementMutationFreshState>();
    private static Task Fail() => Task.FromException(new InvalidOperationException(Dormant));
    private static Task<T> Fail<T>() => Task.FromException<T>(new InvalidOperationException(Dormant));
}

internal enum MovementMutationWriteCheckpoint
{
    AfterOperationEnvelope = 0,
    AfterOutputBatchHeader = 1,
    AfterCorrectionNeutraliser = 2,
    AfterCorrectionReplacement = 3,
    AfterOrdinaryReversal = 4,
    AfterRestoration = 5,
    AfterGeneration = 6,
    AfterGenerationLineFirst = 7,
    AfterGenerationLineMiddle = 8,
    AfterGenerationLineLast = 9,
    AfterLedgerLink = 10,
    AfterPhysicalOutputLink = 11,
    AfterOperationResultCompletion = 12,
    AfterConstructionValidation = 13,
    AfterAuditAssociation = 14,
    ImmediatelyBeforeCas = 15,
    ImmediatelyAfterSuccessfulCas = 16
}

internal interface IMovementMutationFailureInjector
{
    void ThrowIfRequested(MovementMutationWriteCheckpoint checkpoint);
}

internal sealed class NoMovementMutationFailureInjector : IMovementMutationFailureInjector
{
    internal static NoMovementMutationFailureInjector Instance { get; } = new();
    private NoMovementMutationFailureInjector() { }
    public void ThrowIfRequested(MovementMutationWriteCheckpoint checkpoint) { }
}

public enum MovementMutationWriteConflictKind
{
    ConstraintOrCas = 0,
    TransientContention = 1
}

public sealed class MovementMutationWriteConflictException(
    MovementMutationWriteConflictKind kind, Exception innerException) :
    Exception("MOVEMENT_MUTATION_WRITE_CONFLICT", innerException)
{
    public MovementMutationWriteConflictKind Kind { get; } = kind;
}

/// <summary>
/// Dormant SQLite realization of a trusted provider-neutral mutation plan. The
/// service owns the supplied context, transaction, SaveChanges and commit.
/// </summary>
internal sealed class SqliteMovementMutationWriter(
    IMovementMutationFailureInjector failureInjector) : IMovementMutationWriter
{
    private const string SchemaRequired = "MOVEMENT_MUTATION_SCHEMA17_REQUIRED";
    private const string HealthInvalid = "MOVEMENT_MUTATION_SCHEMA17_HEALTH_INVALID";
    private const string PersistenceFailure = "MOVEMENT_MUTATION_PERSISTENCE_FAILURE";

    public bool IsEnabled => true;

    public async Task EnsureReadyAsync(BinTrackerDbContext db, LogicalMovementBatchId rootId,
        CancellationToken cancellationToken = default)
    {
        var (connection, transaction) = RequireTransaction(db);
        try
        {
            await using var version = Command(connection, transaction,
                "SELECT Version FROM SchemaVersion WHERE Id=1;");
            if (Convert.ToInt32(await version.ExecuteScalarAsync(cancellationToken)) !=
                SqliteLineageSchema17Migrator.TargetSchemaVersion)
                throw new InvalidOperationException(SchemaRequired);
            await SqliteLineageSchema17Migrator.ValidateStructuralAndCurrentHealthAsync(
                connection, transaction, "MOVEMENT_MUTATION_SCHEMA17_TABLE_MISSING",
                HealthInvalid, cancellationToken);
            await ValidateOperationAuditHealthAsync(db, rootId, cancellationToken);
        }
        catch (SqliteException ex)
        {
            throw new InvalidOperationException(SchemaRequired, ex);
        }
    }

    public async Task<MovementMutationReplay?> FindCommittedAsync(BinTrackerDbContext db,
        MovementMutationOperationIntent intent, CancellationToken cancellationToken = default)
    {
        var (connection, transaction) = RequireTransaction(db);
        var operation = await ReadOperationByClientIdAsync(
            connection, transaction, intent.ClientOperationId, cancellationToken);
        if (operation is null)
            return null;
        if (!Equivalent(operation, intent))
            throw new InvalidOperationException("MOVEMENT_MUTATION_OPERATION_ID_CONFLICT");
        if (operation.ResultGenerationNumber is null)
            throw new InvalidOperationException("MOVEMENT_MUTATION_OPERATION_INCOMPLETE");
        if (await CountAsync(connection, transaction, """
                SELECT COUNT(*) FROM LogicalMovementGenerations
                WHERE LogicalMovementBatchId=$root AND GenerationNumber=$generation
                  AND MovementCorrectionOperationId=$operation;
                """, cancellationToken, ("$root", intent.RootId.Value),
                ("$generation", operation.ResultGenerationNumber), ("$operation", operation.Id)) != 1)
            throw new InvalidOperationException("MOVEMENT_MUTATION_REPLAY_HEALTH_INVALID");
        await ValidateOperationAuditHealthAsync(db, intent.RootId, cancellationToken);
        return new(operation.Id, intent.RootId, new(operation.ResultGenerationNumber.Value),
            await ReadOutputBatchIdAsync(connection, transaction, operation.Id, cancellationToken));
    }

    public Task<TrustedMovementPlanningSnapshot> MaterializeAsync(BinTrackerDbContext db,
        LogicalMovementBatchId rootId, CancellationToken cancellationToken = default) =>
        SqliteMovementPlanningSnapshotMaterializer.MaterializeAsync(db, rootId, cancellationToken);

    public async Task<PendingMovementMutation> PersistAsync(BinTrackerDbContext db,
        MovementMutationOperationIntent intent, TrustedMovementPlanningSnapshot snapshot,
        MovementMutationPlan plan, int actorUserId, string actorUsername,
        DateTime createdUtc, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(intent);
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(plan);
        if (actorUserId <= 0 || string.IsNullOrWhiteSpace(actorUsername) ||
            plan.Kind != MovementMutationPlanKind.Substantive)
            throw new ArgumentException("A substantive mutation and authenticated actor are required.");

        var (connection, transaction) = RequireTransaction(db);
        try
        {
            var operationId = await ScalarInt64Async(connection, transaction, """
                INSERT INTO MovementCorrectionOperations
                    (ClientOperationId,RequestFingerprint,Kind,OriginalBatchId,ReplacementBatchId,
                     Reason,ActorUserId,ActorUsername,CreatedUtc,RequestJson,RequestSchemaVersion,
                     LogicalMovementBatchId,ExpectedGenerationNumber,ResultGenerationNumber)
                VALUES ($client,$fingerprint,$kind,NULL,NULL,$reason,$actor,$username,$utc,
                        $json,$schema,$root,$expected,NULL)
                RETURNING Id;
                """, cancellationToken, ("$client", intent.ClientOperationId.ToString()),
                ("$fingerprint", intent.RequestFingerprint), ("$kind", (int)intent.OperationKind),
                ("$reason", plan.Lines.SelectMany(x => x.Movements)
                    .Select(x => x.Reason).DefaultIfEmpty(string.Empty).First()),
                ("$actor", actorUserId), ("$username", actorUsername), ("$utc", createdUtc),
                ("$json", intent.RequestJson), ("$schema", intent.RequestSchemaVersion),
                ("$root", intent.RootId.Value), ("$expected", intent.ExpectedGeneration.Value));
            failureInjector.ThrowIfRequested(MovementMutationWriteCheckpoint.AfterOperationEnvelope);

            int? outputBatchId = null;
            if (plan.PhysicalOutput is { } output)
            {
                outputBatchId = checked((int)await ScalarInt64Async(connection, transaction, """
                    INSERT INTO MovementBatches
                        (ClientOperationId,MovementDate,MovementType,Source,Notes,CreatedBy,CreatedUtc,IsReversed)
                    VALUES (NULL,$date,$direction,$source,$notes,$createdBy,$utc,0)
                    RETURNING Id;
                    """, cancellationToken, ("$date", Date(output.MovementDate)),
                    ("$direction", (int)output.Direction), ("$source", (int)output.Source),
                    ("$notes", $"Logical movement correction output for root #{intent.RootId.Value}."),
                    ("$createdBy", actorUsername), ("$utc", createdUtc)));
                failureInjector.ThrowIfRequested(MovementMutationWriteCheckpoint.AfterOutputBatchHeader);
            }

            var movementIds = new Dictionary<(LogicalMovementLineId, PlannedMovementPurpose), long>();
            foreach (var spec in plan.Lines.SelectMany(x => x.Movements))
            {
                var member = plan.PhysicalOutput?.Members.Any(x => x.LineId == spec.LineId && x.Purpose == spec.Purpose) == true;
                var movementId = await ScalarInt64Async(connection, transaction, """
                    INSERT INTO BinMovements
                        (ClientOperationId,MovementDate,MovementType,Source,CustomerId,ContainerTypeId,
                         MovementBatchId,ImportRunId,Quantity,ReferenceNumber,Notes,CreatedBy,CreatedUtc,
                         ReversesMovementId,CorrectedByMovementId,CorrectionReason)
                    VALUES (NULL,$date,$direction,$source,$customer,$container,$batch,NULL,$quantity,
                            $reference,$notes,$createdBy,$utc,$reverses,NULL,$reason)
                    RETURNING Id;
                    """, cancellationToken, ("$date", Date(spec.MovementDate)),
                    ("$direction", (int)spec.Direction), ("$source", (int)spec.Source),
                    ("$customer", spec.CustomerId), ("$container", spec.ContainerTypeId),
                    ("$batch", member ? outputBatchId : null), ("$quantity", spec.Quantity),
                    ("$reference", spec.Reference), ("$notes", spec.Notes),
                    ("$createdBy", actorUsername), ("$utc", createdUtc),
                    ("$reverses", spec.ReversesMovementId), ("$reason", spec.Reason));
                if (!movementIds.TryAdd((spec.LineId, spec.Purpose), movementId))
                    throw new InvalidOperationException(PersistenceFailure);
                failureInjector.ThrowIfRequested(spec.Purpose switch
                {
                    PlannedMovementPurpose.CorrectionNeutraliser => MovementMutationWriteCheckpoint.AfterCorrectionNeutraliser,
                    PlannedMovementPurpose.CorrectionReplacement => MovementMutationWriteCheckpoint.AfterCorrectionReplacement,
                    PlannedMovementPurpose.OrdinaryReversal => MovementMutationWriteCheckpoint.AfterOrdinaryReversal,
                    PlannedMovementPurpose.Restoration => MovementMutationWriteCheckpoint.AfterRestoration,
                    _ => throw new InvalidOperationException(PersistenceFailure)
                });
            }

            var resultGeneration = checked(intent.ExpectedGeneration.Value + 1);
            var generationId = await ScalarInt64Async(connection, transaction, """
                INSERT INTO LogicalMovementGenerations
                    (LogicalMovementBatchId,GenerationNumber,PreviousGenerationNumber,
                     MovementCorrectionOperationId,Kind,LineCount,CreatedUtc)
                VALUES ($root,$generation,$previous,$operation,$kind,$count,$utc)
                RETURNING Id;
                """, cancellationToken, ("$root", intent.RootId.Value),
                ("$generation", resultGeneration), ("$previous", intent.ExpectedGeneration.Value),
                ("$operation", operationId), ("$kind", (int)intent.GenerationKind),
                ("$count", plan.Lines.Count), ("$utc", createdUtc));
            failureInjector.ThrowIfRequested(MovementMutationWriteCheckpoint.AfterGeneration);

            var persistedLines = new List<PersistedMovementMutationLineResult>(plan.Lines.Count);
            var generationLineIds = new Dictionary<LogicalMovementLineId, long>();
            for (var index = 0; index < plan.Lines.Count; index++)
            {
                var line = plan.Lines[index];
                var current = snapshot.Lines.Single(x => x.Current.Id == line.LineId).Current;
                var effective = Resolve(line.EffectiveMovement, line.LineId, movementIds);
                var terminal = line.TerminalReversalMovement is null
                    ? (long?)null
                    : Resolve(line.TerminalReversalMovement, line.LineId, movementIds);
                var generationLineId = await ScalarInt64Async(connection, transaction, """
                    INSERT INTO LogicalMovementGenerationLines
                        (LogicalMovementBatchId,LogicalMovementGenerationId,LogicalMovementLineId,
                         State,Action,AppliedFieldMask,PreviousGenerationLineId,
                         ResultEffectiveMovementId,LastEffectiveMovementId,TerminalReversalMovementId,CreatedUtc)
                    VALUES ($root,$generation,$line,$state,$action,$mask,$previousLine,
                            $result,$last,$terminal,$utc)
                    RETURNING Id;
                    """, cancellationToken, ("$root", intent.RootId.Value),
                    ("$generation", generationId), ("$line", line.LineId.Value),
                    ("$state", (int)line.State), ("$action", (int)line.Action),
                    ("$mask", (int)line.AppliedFieldMask),
                    ("$previousLine", current.CurrentGenerationLineId.Value),
                    ("$result", line.State == LogicalMovementLineState.Active ? effective : null),
                    ("$last", line.State == LogicalMovementLineState.Reversed ? effective : null),
                    ("$terminal", terminal), ("$utc", createdUtc));
                generationLineIds.Add(line.LineId, generationLineId);
                persistedLines.Add(new(line.LineId, line.Action, line.State, line.AppliedFieldMask,
                    effective, terminal));
                if (index == 0)
                    failureInjector.ThrowIfRequested(MovementMutationWriteCheckpoint.AfterGenerationLineFirst);
                if (plan.Lines.Count > 2 && index == plan.Lines.Count / 2)
                    failureInjector.ThrowIfRequested(MovementMutationWriteCheckpoint.AfterGenerationLineMiddle);
                if (index == plan.Lines.Count - 1)
                    failureInjector.ThrowIfRequested(MovementMutationWriteCheckpoint.AfterGenerationLineLast);
            }

            foreach (var pair in movementIds)
            {
                await NonQueryExactlyOneAsync(connection, transaction, """
                    INSERT INTO LogicalMovementLedgerLinks
                        (BinMovementId,LogicalMovementBatchId,LogicalMovementLineId,Role,
                         IntroducedByGenerationLineId,LegacyMovementCorrectionLineId,CreatedUtc)
                    VALUES ($movement,$root,$line,$role,$introduced,NULL,$utc);
                    """, cancellationToken, ("$movement", pair.Value), ("$root", intent.RootId.Value),
                    ("$line", pair.Key.Item1.Value), ("$role", (int)Role(pair.Key.Item2)),
                    ("$introduced", generationLineIds[pair.Key.Item1]), ("$utc", createdUtc));
                failureInjector.ThrowIfRequested(MovementMutationWriteCheckpoint.AfterLedgerLink);
            }

            if (outputBatchId is { } batchId)
            {
                await NonQueryExactlyOneAsync(connection, transaction, """
                    INSERT INTO LogicalMovementPhysicalOutputs
                        (MovementBatchId,LogicalMovementBatchId,LogicalMovementGenerationId,
                         LegacyMovementCorrectionOperationId,CreatedUtc)
                    VALUES ($batch,$root,$generation,NULL,$utc);
                    """, cancellationToken, ("$batch", batchId), ("$root", intent.RootId.Value),
                    ("$generation", generationId), ("$utc", createdUtc));
                failureInjector.ThrowIfRequested(MovementMutationWriteCheckpoint.AfterPhysicalOutputLink);
            }

            await NonQueryExactlyOneAsync(connection, transaction, """
                UPDATE MovementCorrectionOperations SET ResultGenerationNumber=$result
                WHERE Id=$operation AND ResultGenerationNumber IS NULL;
                """, cancellationToken, ("$result", resultGeneration), ("$operation", operationId));
            failureInjector.ThrowIfRequested(MovementMutationWriteCheckpoint.AfterOperationResultCompletion);

            var construction = await ReadConstructionAsync(connection, transaction, operationId,
                generationId, movementIds, outputBatchId, cancellationToken);
            LogicalMovementMutationConstructionValidator.Validate(intent, snapshot, plan, construction);
            failureInjector.ThrowIfRequested(MovementMutationWriteCheckpoint.AfterConstructionValidation);

            return new(operationId, intent.RootId, new(resultGeneration), generationId, outputBatchId,
                persistedLines, movementIds.Select(x => new PersistedMovementMutationMovementResult(
                    x.Key.Item1, x.Key.Item2, x.Value)).ToArray());
        }
        catch (SqliteException ex) when (ex.SqliteErrorCode is 5 or 6)
        {
            throw new MovementMutationWriteConflictException(
                MovementMutationWriteConflictKind.TransientContention, ex);
        }
        catch (SqliteException ex) when (ex.SqliteErrorCode == 19)
        {
            throw new MovementMutationWriteConflictException(
                MovementMutationWriteConflictKind.ConstraintOrCas, ex);
        }
        catch (SqliteException ex)
        {
            throw new InvalidOperationException(PersistenceFailure, ex);
        }
    }

    public async Task AssociatePrimaryAuditAsync(BinTrackerDbContext db, PendingMovementMutation pending,
        long auditEventId, CancellationToken cancellationToken = default)
    {
        var (connection, transaction) = RequireTransaction(db);
        if (auditEventId <= 0)
            throw new ArgumentOutOfRangeException(nameof(auditEventId));
        await NonQueryExactlyOneAsync(connection, transaction, """
            UPDATE AuditEvents SET MovementCorrectionOperationId=$operation
            WHERE Id=$audit AND MovementCorrectionOperationId IS NULL;
            """, cancellationToken, ("$operation", pending.OperationId), ("$audit", auditEventId));
        failureInjector.ThrowIfRequested(MovementMutationWriteCheckpoint.AfterAuditAssociation);
    }

    public async Task ValidateOperationAuditHealthAsync(BinTrackerDbContext db,
        LogicalMovementBatchId rootId,
        CancellationToken cancellationToken = default)
    {
        var (connection, transaction) = RequireTransaction(db);
        var generations = new List<NativeMovementGenerationAuditFact>();
        await using (var command = Command(connection, transaction, """
            SELECT Id,LogicalMovementBatchId,GenerationNumber,PreviousGenerationNumber,
                   MovementCorrectionOperationId,Kind
            FROM LogicalMovementGenerations
            WHERE LogicalMovementBatchId=$root;
            """, ("$root", rootId.Value)))
        await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
            while (await reader.ReadAsync(cancellationToken))
                generations.Add(new(reader.GetInt64(0), reader.GetInt64(1), reader.GetInt32(2),
                    NullableInt32(reader, 3), NullableInt64(reader, 4),
                    (LogicalMovementGenerationAction)reader.GetInt32(5)));

        var operations = new List<NativeMovementOperationAuditFact>();
        await using (var command = Command(connection, transaction, """
            SELECT Id,LogicalMovementBatchId,ExpectedGenerationNumber,ResultGenerationNumber,
                   RequestSchemaVersion,Kind
            FROM MovementCorrectionOperations
            WHERE LogicalMovementBatchId=$root;
            """, ("$root", rootId.Value)))
        await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
            while (await reader.ReadAsync(cancellationToken))
                operations.Add(new(reader.GetInt64(0), NullableInt64(reader, 1),
                    NullableInt32(reader, 2), NullableInt32(reader, 3), NullableInt32(reader, 4),
                    (MovementCorrectionKind)reader.GetInt32(5)));

        var audits = new List<PrimaryMovementAuditFact>();
        await using (var command = Command(connection, transaction, """
            SELECT Id,MovementCorrectionOperationId,Action,EntityType,EntityId,Succeeded FROM AuditEvents
            WHERE MovementCorrectionOperationId IN
                (SELECT Id FROM MovementCorrectionOperations WHERE LogicalMovementBatchId=$root);
            """, ("$root", rootId.Value)))
        await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
            while (await reader.ReadAsync(cancellationToken))
                audits.Add(new(reader.GetInt64(0), reader.GetInt64(1), reader.GetString(2),
                    reader.GetString(3), NullableString(reader, 4), reader.GetBoolean(5)));
        LogicalMovementOperationAuditHealthValidator.Validate(generations, operations, audits);
    }

    public async Task<bool> TryPublishAsync(BinTrackerDbContext db, PendingMovementMutation pending,
        LogicalMovementGenerationNumber expectedGeneration,
        CancellationToken cancellationToken = default)
    {
        var (connection, transaction) = RequireTransaction(db);
        failureInjector.ThrowIfRequested(MovementMutationWriteCheckpoint.ImmediatelyBeforeCas);
        await using var command = Command(connection, transaction, """
            UPDATE LogicalMovementBatches
            SET CurrentGenerationNumber=$next
            WHERE Id=$root AND Status=$active AND CurrentGenerationNumber=$expected;
            """, ("$next", checked(expectedGeneration.Value + 1)), ("$root", pending.RootId.Value),
            ("$active", (int)LogicalMovementBatchStatus.Active), ("$expected", expectedGeneration.Value));
        bool published;
        try
        {
            published = await command.ExecuteNonQueryAsync(cancellationToken) == 1;
        }
        catch (SqliteException ex) when (ex.SqliteErrorCode is 5 or 6)
        {
            throw new MovementMutationWriteConflictException(
                MovementMutationWriteConflictKind.TransientContention, ex);
        }
        catch (SqliteException ex) when (ex.SqliteErrorCode == 19)
        {
            throw new MovementMutationWriteConflictException(
                MovementMutationWriteConflictKind.ConstraintOrCas, ex);
        }
        if (published)
            failureInjector.ThrowIfRequested(MovementMutationWriteCheckpoint.ImmediatelyAfterSuccessfulCas);
        return published;
    }

    public async Task ValidatePublishedAsync(BinTrackerDbContext db, PendingMovementMutation pending,
        CancellationToken cancellationToken = default)
    {
        var (connection, transaction) = RequireTransaction(db);
        var resolution = await SqliteLogicalMovementCurrentRootResolver.ResolveInSnapshotAsync(
            connection, transaction, pending.RootId, cancellationToken);
        if (resolution.Kind != LogicalMovementCurrentRootResolutionKind.Resolved ||
            resolution.Root?.CurrentGenerationNumber != pending.ResultGeneration)
            throw new InvalidOperationException(HealthInvalid);
        await ValidateOperationAuditHealthAsync(db, pending.RootId, cancellationToken);
        await SqliteLineageSchema17Migrator.ValidateStructuralAndCurrentHealthAsync(
            connection, transaction, "MOVEMENT_MUTATION_SCHEMA17_TABLE_MISSING",
            HealthInvalid, cancellationToken);
    }

    public async Task<MovementMutationFreshState> ClassifyFreshAsync(BinTrackerDbContext db,
        MovementMutationOperationIntent intent, CancellationToken cancellationToken = default)
    {
        var (connection, transaction) = RequireTransaction(db);
        var existing = await ReadOperationByClientIdAsync(
            connection, transaction, intent.ClientOperationId, cancellationToken);
        if (existing is not null && !Equivalent(existing, intent))
            return new(MovementMutationFreshStateKind.OperationIdConflict);

        try
        {
            await EnsureReadyAsync(db, intent.RootId, cancellationToken);
        }
        catch (InvalidOperationException)
        {
            return new(MovementMutationFreshStateKind.Unhealthy);
        }

        MovementMutationReplay? operation;
        try
        {
            operation = await FindCommittedAsync(db, intent, cancellationToken);
        }
        catch (InvalidOperationException ex) when (ex.Message == "MOVEMENT_MUTATION_OPERATION_ID_CONFLICT")
        {
            return new(MovementMutationFreshStateKind.OperationIdConflict);
        }
        if (operation is not null)
            return new(MovementMutationFreshStateKind.Replay, operation);

        var resolution = await SqliteLogicalMovementCurrentRootResolver.ResolveInSnapshotAsync(
            connection, transaction, intent.RootId, cancellationToken);
        if (resolution.Kind == LogicalMovementCurrentRootResolutionKind.NotFound)
            return new(MovementMutationFreshStateKind.NotFound);
        if (resolution.Kind != LogicalMovementCurrentRootResolutionKind.Resolved || resolution.Root is null)
            return new(MovementMutationFreshStateKind.Unhealthy);
        if (resolution.Root.CurrentGenerationNumber != intent.ExpectedGeneration)
            return new(MovementMutationFreshStateKind.StaleGeneration);
        return new(MovementMutationFreshStateKind.IntegrityFailure);
    }

    private static async Task<LogicalMovementMutationConstruction> ReadConstructionAsync(
        SqliteConnection connection, SqliteTransaction transaction, long operationId, long generationId,
        IReadOnlyDictionary<(LogicalMovementLineId, PlannedMovementPurpose), long> movementIds,
        int? outputBatchId, CancellationToken token)
    {
        var operation = await ReadOperationAsync(connection, transaction, operationId, token)
            ?? throw new InvalidOperationException(PersistenceFailure);
        PersistedMovementMutationGeneration generation;
        await using (var command = Command(connection, transaction, """
            SELECT Id,LogicalMovementBatchId,GenerationNumber,PreviousGenerationNumber,
                   MovementCorrectionOperationId,Kind,LineCount
            FROM LogicalMovementGenerations WHERE Id=$id;
            """, ("$id", generationId)))
        await using (var reader = await command.ExecuteReaderAsync(token))
        {
            if (!await reader.ReadAsync(token)) throw new InvalidOperationException(PersistenceFailure);
            generation = new(reader.GetInt64(0), reader.GetInt64(1), reader.GetInt32(2),
                NullableInt32(reader, 3), NullableInt64(reader, 4),
                (LogicalMovementGenerationAction)reader.GetInt32(5), reader.GetInt32(6));
        }

        var lines = new List<PersistedMovementMutationLine>();
        await using (var command = Command(connection, transaction, """
            SELECT Id,LogicalMovementBatchId,LogicalMovementGenerationId,LogicalMovementLineId,
                   State,Action,AppliedFieldMask,PreviousGenerationLineId,ResultEffectiveMovementId,
                   LastEffectiveMovementId,TerminalReversalMovementId
            FROM LogicalMovementGenerationLines WHERE LogicalMovementGenerationId=$generation;
            """, ("$generation", generationId)))
        await using (var reader = await command.ExecuteReaderAsync(token))
            while (await reader.ReadAsync(token))
                lines.Add(new(reader.GetInt64(0), reader.GetInt64(1), reader.GetInt64(2), reader.GetInt64(3),
                    (LogicalMovementLineState)reader.GetInt32(4), (LogicalMovementGenerationAction)reader.GetInt32(5),
                    (MovementChangeField)reader.GetInt32(6), NullableInt64(reader, 7), NullableInt64(reader, 8),
                    NullableInt64(reader, 9), NullableInt64(reader, 10)));

        var movementPurposes = movementIds.ToDictionary(x => x.Value, x => x.Key);
        var movements = new List<PersistedPlannedMovement>();
        await using (var command = InCommand(connection, transaction, """
            SELECT Id,MovementDate,MovementType,Source,CustomerId,ContainerTypeId,Quantity,
                   ReferenceNumber,Notes,CorrectionReason,ReversesMovementId,MovementBatchId,
                   ImportRunId,ClientOperationId
            FROM BinMovements WHERE Id IN (
            """, movementPurposes.Keys.ToArray()))
        await using (var reader = await command.ExecuteReaderAsync(token))
            while (await reader.ReadAsync(token))
            {
                var id = reader.GetInt64(0);
                var purpose = movementPurposes[id];
                movements.Add(new(id, purpose.Item1, purpose.Item2,
                    DateOnly.Parse(reader.GetString(1), CultureInfo.InvariantCulture),
                    (MovementType)reader.GetInt32(2), (MovementSource)reader.GetInt32(3),
                    reader.GetInt32(4), reader.GetInt32(5), reader.GetInt32(6),
                    NullableString(reader, 7), NullableString(reader, 8), NullableString(reader, 9),
                    NullableInt64(reader, 10), NullableInt32(reader, 11), NullableInt64(reader, 12),
                    reader.IsDBNull(13) ? null : Guid.Parse(reader.GetString(13))));
            }

        var links = new List<PersistedMovementMutationLedgerLink>();
        await using (var command = InCommand(connection, transaction, """
            SELECT BinMovementId,LogicalMovementBatchId,LogicalMovementLineId,Role,
                   IntroducedByGenerationLineId,LegacyMovementCorrectionLineId
            FROM LogicalMovementLedgerLinks WHERE BinMovementId IN (
            """, movementPurposes.Keys.ToArray()))
        await using (var reader = await command.ExecuteReaderAsync(token))
            while (await reader.ReadAsync(token))
                links.Add(new(reader.GetInt64(0), reader.GetInt64(1), reader.GetInt64(2),
                    (LogicalMovementTransformationRole)reader.GetInt32(3), NullableInt64(reader, 4),
                    NullableInt64(reader, 5)));

        PersistedMovementMutationPhysicalOutput? output = null;
        if (outputBatchId is { } batchId)
        {
            int persistedBatchId;
            long outputRootId;
            long? outputGenerationId;
            long? legacyOperationId;
            DateOnly outputDate;
            MovementType outputDirection;
            MovementSource outputSource;
            await using (var command = Command(connection, transaction, """
                SELECT p.MovementBatchId,p.LogicalMovementBatchId,p.LogicalMovementGenerationId,
                       p.LegacyMovementCorrectionOperationId,b.MovementDate,b.MovementType,b.Source
                FROM LogicalMovementPhysicalOutputs p
                JOIN MovementBatches b ON b.Id=p.MovementBatchId
                WHERE p.MovementBatchId=$batch;
                """, ("$batch", batchId)))
            await using (var reader = await command.ExecuteReaderAsync(token))
            {
                if (!await reader.ReadAsync(token)) throw new InvalidOperationException(PersistenceFailure);
                persistedBatchId = reader.GetInt32(0);
                outputRootId = reader.GetInt64(1);
                outputGenerationId = NullableInt64(reader, 2);
                legacyOperationId = NullableInt64(reader, 3);
                outputDate = DateOnly.Parse(reader.GetString(4), CultureInfo.InvariantCulture);
                outputDirection = (MovementType)reader.GetInt32(5);
                outputSource = (MovementSource)reader.GetInt32(6);
            }
            output = new(persistedBatchId, outputRootId, outputGenerationId, legacyOperationId,
                outputDate, outputDirection, outputSource,
                (await ReadInt64sAsync(connection, transaction,
                    "SELECT Id FROM BinMovements WHERE MovementBatchId=$batch;", token,
                    ("$batch", persistedBatchId))).ToHashSet());
        }
        return new(operation, generation, lines, movements, links, output);
    }

    private static async Task<PersistedMovementMutationOperation?> ReadOperationByClientIdAsync(
        SqliteConnection connection, SqliteTransaction transaction, Guid id, CancellationToken token)
    {
        await using var command = Command(connection, transaction, OperationSql +
            " WHERE ClientOperationId=$client;", ("$client", id.ToString()));
        return await ReadOperationAsync(command, token);
    }

    private static async Task<PersistedMovementMutationOperation?> ReadOperationAsync(
        SqliteConnection connection, SqliteTransaction transaction, long id, CancellationToken token)
    {
        await using var command = Command(connection, transaction, OperationSql +
            " WHERE Id=$id;", ("$id", id));
        return await ReadOperationAsync(command, token);
    }

    private static async Task<PersistedMovementMutationOperation?> ReadOperationAsync(
        SqliteCommand command, CancellationToken token)
    {
        await using var reader = await command.ExecuteReaderAsync(token);
        if (!await reader.ReadAsync(token)) return null;
        var operation = new PersistedMovementMutationOperation(reader.GetInt64(0),
            Guid.Parse(reader.GetString(1)), reader.GetString(2),
            (MovementCorrectionKind)reader.GetInt32(3), NullableInt32(reader, 4),
            NullableInt32(reader, 5), reader.GetString(6), reader.GetInt32(7),
            reader.GetString(8), reader.GetDateTime(9), NullableString(reader, 10),
            NullableInt32(reader, 11), NullableInt64(reader, 12),
            NullableInt32(reader, 13), NullableInt32(reader, 14));
        if (await reader.ReadAsync(token)) throw new InvalidOperationException(PersistenceFailure);
        return operation;
    }

    private const string OperationSql = """
        SELECT Id,ClientOperationId,RequestFingerprint,Kind,OriginalBatchId,ReplacementBatchId,
               Reason,ActorUserId,ActorUsername,
               CreatedUtc,RequestJson,RequestSchemaVersion,LogicalMovementBatchId,
               ExpectedGenerationNumber,ResultGenerationNumber
        FROM MovementCorrectionOperations
        """;

    private static bool Equivalent(PersistedMovementMutationOperation operation,
        MovementMutationOperationIntent intent) =>
        operation.ClientOperationId == intent.ClientOperationId &&
        operation.RequestFingerprint == intent.RequestFingerprint &&
        operation.RequestJson == intent.RequestJson &&
        operation.RequestSchemaVersion == intent.RequestSchemaVersion &&
        operation.RootId == intent.RootId.Value &&
        operation.ExpectedGenerationNumber == intent.ExpectedGeneration.Value &&
        operation.Kind == intent.OperationKind;

    private static async Task<int?> ReadOutputBatchIdAsync(SqliteConnection connection,
        SqliteTransaction transaction, long operationId, CancellationToken token)
    {
        await using var command = Command(connection, transaction, """
            SELECT p.MovementBatchId
            FROM LogicalMovementGenerations g
            JOIN LogicalMovementPhysicalOutputs p ON p.LogicalMovementGenerationId=g.Id
            WHERE g.MovementCorrectionOperationId=$id;
            """, ("$id", operationId));
        await using var reader = await command.ExecuteReaderAsync(token);
        if (!await reader.ReadAsync(token)) return null;
        var result = reader.GetInt32(0);
        if (await reader.ReadAsync(token)) throw new InvalidOperationException(PersistenceFailure);
        return result;
    }

    private static LogicalMovementTransformationRole Role(PlannedMovementPurpose purpose) => purpose switch
    {
        PlannedMovementPurpose.CorrectionNeutraliser => LogicalMovementTransformationRole.CorrectionNeutraliser,
        PlannedMovementPurpose.CorrectionReplacement => LogicalMovementTransformationRole.CorrectionReplacement,
        PlannedMovementPurpose.OrdinaryReversal => LogicalMovementTransformationRole.OrdinaryReversal,
        PlannedMovementPurpose.Restoration => LogicalMovementTransformationRole.Restoration,
        _ => throw new InvalidOperationException(PersistenceFailure)
    };

    private static long Resolve(PlannedMovementReference reference, LogicalMovementLineId lineId,
        IReadOnlyDictionary<(LogicalMovementLineId, PlannedMovementPurpose), long> movementIds) =>
        reference.ExistingMovementId ?? movementIds[(lineId,
            reference.PlannedPurpose ?? throw new InvalidOperationException(PersistenceFailure))];

    private static string Date(DateOnly date) => date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    private static (SqliteConnection Connection, SqliteTransaction Transaction) RequireTransaction(BinTrackerDbContext db)
    {
        ArgumentNullException.ThrowIfNull(db);
        if (db.Database.CurrentTransaction is null ||
            db.Database.GetDbConnection() is not SqliteConnection connection ||
            db.Database.CurrentTransaction.GetDbTransaction() is not SqliteTransaction transaction ||
            connection.State != ConnectionState.Open)
            throw new InvalidOperationException("The SQLite mutation writer requires the caller's active SQLite transaction.");
        return (connection, transaction);
    }

    private static async Task<long> ScalarInt64Async(SqliteConnection connection, SqliteTransaction transaction,
        string sql, CancellationToken token, params (string Name, object? Value)[] parameters)
    {
        await using var command = Command(connection, transaction, sql, parameters);
        return Convert.ToInt64(await command.ExecuteScalarAsync(token));
    }

    private static async Task<int> CountAsync(SqliteConnection connection, SqliteTransaction transaction,
        string sql, CancellationToken token, params (string Name, object? Value)[] parameters) =>
        Convert.ToInt32(await ScalarInt64Async(connection, transaction, sql, token, parameters));

    private static async Task NonQueryExactlyOneAsync(SqliteConnection connection, SqliteTransaction transaction,
        string sql, CancellationToken token, params (string Name, object? Value)[] parameters)
    {
        await using var command = Command(connection, transaction, sql, parameters);
        if (await command.ExecuteNonQueryAsync(token) != 1)
            throw new InvalidOperationException(PersistenceFailure);
    }

    private static async Task<List<long>> ReadInt64sAsync(SqliteConnection connection,
        SqliteTransaction transaction, string sql, CancellationToken token,
        params (string Name, object? Value)[] parameters)
    {
        await using var command = Command(connection, transaction, sql, parameters);
        var result = new List<long>();
        await using var reader = await command.ExecuteReaderAsync(token);
        while (await reader.ReadAsync(token)) result.Add(reader.GetInt64(0));
        return result;
    }

    private static SqliteCommand InCommand(SqliteConnection connection, SqliteTransaction transaction,
        string prefix, IReadOnlyList<long> ids)
    {
        if (ids.Count == 0) throw new InvalidOperationException(PersistenceFailure);
        var command = connection.CreateCommand();
        command.Transaction = transaction;
        var names = new string[ids.Count];
        for (var i = 0; i < ids.Count; i++)
        {
            names[i] = $"$id{i}";
            command.Parameters.AddWithValue(names[i], ids[i]);
        }
        command.CommandText = prefix + string.Join(',', names) + ");";
        return command;
    }

    private static SqliteCommand Command(SqliteConnection connection, SqliteTransaction transaction,
        string sql, params (string Name, object? Value)[] parameters)
    {
        var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = sql;
        foreach (var (name, value) in parameters)
            command.Parameters.AddWithValue(name, value ?? DBNull.Value);
        return command;
    }

    private static int? NullableInt32(SqliteDataReader reader, int ordinal) =>
        reader.IsDBNull(ordinal) ? null : reader.GetInt32(ordinal);
    private static long? NullableInt64(SqliteDataReader reader, int ordinal) =>
        reader.IsDBNull(ordinal) ? null : reader.GetInt64(ordinal);
    private static string? NullableString(SqliteDataReader reader, int ordinal) =>
        reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
}
