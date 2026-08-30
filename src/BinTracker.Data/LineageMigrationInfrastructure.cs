using System.Security.Cryptography;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using Microsoft.Win32.SafeHandles;

namespace BinTracker.Data;

public enum LineagePreflightClassification
{
    Migratable = 0,
    ReadOnly = 1,
    Invalid = 2,
    GlobalBlocker = 3
}

public enum LineagePreflightReasonCode
{
    RequiredTableMissing = 0,
    UnsupportedSchemaVersion = 1,
    IntegrityCheckFailed = 2,
    ForeignKeyViolation = 3,
    MissingStructuralReference = 4,
    InvalidCorrectionTriple = 5,
    DuplicateMovementConsumption = 6,
    CorrectedByRelationshipMismatch = 7,
    CrossDomainLineage = 8,
    MixedDomainPhysicalBatch = 9,
    UnsupportedCorrectionKind = 10,
    CorrectionGraphCycle = 11,
    InvalidPhysicalBatchRelationship = 12
}

public sealed record LineagePreflightIssue(
    LineagePreflightClassification Classification,
    LineagePreflightReasonCode ReasonCode,
    string EntityType,
    long? EntityId = null);

public sealed record LineagePreflightCounts(
    long Movements,
    long MovementBatches,
    long CorrectionOperations,
    long CorrectionLines,
    long OrdinaryReversals,
    long ImportOwnedMovements,
    long AdjustmentMovements,
    long SingleCorrections,
    long WholeBatchCorrections,
    long RepeatedCorrections,
    long PartiallyReversedBatches);

public sealed record LineageMigrationPreflightResult(
    string SourceFileName,
    string SourceIdentityHash,
    int SchemaVersion,
    LineagePreflightCounts Counts,
    IReadOnlyDictionary<string, long> TableRowCounts,
    LineagePreflightClassification Classification,
    IReadOnlyList<LineagePreflightIssue> Issues,
    string StructuralFingerprint,
    string JournalMode,
    bool IntegrityCheckPassed,
    bool ForeignKeyCheckPassed);

public interface ILineageMigrationPreflight
{
    Task<LineageMigrationPreflightResult> InspectAsync(
        string databasePath,
        CancellationToken cancellationToken = default);
}

public interface IDatabaseUpgradeGate
{
    IDatabaseRuntimeLease AcquireRuntime(string databasePath);
    IDatabaseUpgradeLease AcquireUpgrade(string databasePath);
}

public interface IDatabaseAccessLease : IDisposable
{
    string DatabasePath { get; }
    string GateIdentity { get; }
    string DatabaseFileIdentity { get; }
}

public interface IDatabaseRuntimeLease : IDatabaseAccessLease;

public interface IDatabaseUpgradeLease : IDatabaseAccessLease
{
    bool PendingOperationCheckPassed { get; }
}

public enum DatabaseUpgradeUnavailableReason
{
    ConflictingDatabaseUse = 0,
    PendingDatabaseOperation = 1,
    FileIdentityUnavailable = 2
}

public sealed class DatabaseUpgradeUnavailableException(
    DatabaseUpgradeUnavailableReason reason,
    string message,
    Exception? inner = null)
    : InvalidOperationException(message, inner)
{
    public DatabaseUpgradeUnavailableReason Reason { get; } = reason;
}

public interface IDatabaseOperationConflictProbe
{
    void EnsureNoConflict(string databasePath);
}

public sealed class PendingDatabaseOperationConflictProbe(string markerPath)
    : IDatabaseOperationConflictProbe
{
    public void EnsureNoConflict(string databasePath)
    {
        if (File.Exists(markerPath))
        {
            throw new DatabaseUpgradeUnavailableException(
                DatabaseUpgradeUnavailableReason.PendingDatabaseOperation,
                "A pending BinTracker database operation blocks the upgrade gate.");
        }
    }
}

/// <summary>
/// Uses shared runtime/exclusive upgrade OS handles scoped to the physical
/// Windows database-file identity. Windows closes handles on process
/// termination, so a leftover companion file cannot create a stale lockout.
/// </summary>
public sealed class WindowsFileDatabaseUpgradeGate : IDatabaseUpgradeGate
{
    private readonly string lockDirectory;
    private readonly IDatabaseOperationConflictProbe conflictProbe;

    public WindowsFileDatabaseUpgradeGate(
        string? lockDirectory = null,
        IDatabaseOperationConflictProbe? conflictProbe = null)
    {
        this.lockDirectory = Path.GetFullPath(
            lockDirectory ?? DatabaseConfiguration.DatabaseAccessLockFolder);
        this.conflictProbe = conflictProbe ?? new PendingDatabaseOperationConflictProbe(
            DatabaseConfiguration.PendingDatabaseOperationPath);
    }

    public IDatabaseRuntimeLease AcquireRuntime(string databasePath) =>
        (IDatabaseRuntimeLease)Acquire(databasePath, upgrade: false);

    public IDatabaseUpgradeLease AcquireUpgrade(string databasePath) =>
        (IDatabaseUpgradeLease)Acquire(databasePath, upgrade: true);

    private IDatabaseAccessLease Acquire(string databasePath, bool upgrade)
    {
        var canonicalPath = SqliteMigrationPath.NormalizeExistingDatabase(databasePath);
        var fileIdentity = WindowsFileIdentity.Get(canonicalPath);
        Directory.CreateDirectory(lockDirectory);
        var lockPath = Path.Combine(
            lockDirectory,
            $"{SqliteMigrationPath.IdentityHash(fileIdentity)}.bintracker-db.lock");

        try
        {
            EnsureLockFileExists(lockPath);
            var stream = new FileStream(
                lockPath,
                FileMode.Open,
                upgrade ? FileAccess.ReadWrite : FileAccess.Read,
                upgrade ? FileShare.None : FileShare.Read,
                bufferSize: 1,
                FileOptions.WriteThrough);

            if (!upgrade)
                return new RuntimeLease(canonicalPath, lockPath, fileIdentity, stream);

            try
            {
                conflictProbe.EnsureNoConflict(canonicalPath);
                return new UpgradeLease(canonicalPath, lockPath, fileIdentity, stream);
            }
            catch
            {
                stream.Dispose();
                throw;
            }
        }
        catch (IOException ex)
        {
            throw new DatabaseUpgradeUnavailableException(
                DatabaseUpgradeUnavailableReason.ConflictingDatabaseUse,
                "The BinTracker database upgrade gate is already held or unavailable.", ex);
        }
        catch (UnauthorizedAccessException ex)
        {
            throw new DatabaseUpgradeUnavailableException(
                DatabaseUpgradeUnavailableReason.ConflictingDatabaseUse,
                "The BinTracker database upgrade gate could not be acquired.", ex);
        }
    }

    private static void EnsureLockFileExists(string lockPath)
    {
        if (File.Exists(lockPath))
            return;

        try
        {
            using var stream = new FileStream(
                lockPath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.ReadWrite,
                bufferSize: 1,
                FileOptions.WriteThrough);
        }
        catch (IOException) when (File.Exists(lockPath))
        {
            // Another participant won the one-time lock-file creation race.
        }
    }

    private abstract class Lease(
        string databasePath,
        string gateIdentity,
        string databaseFileIdentity,
        FileStream stream) : IDatabaseAccessLease
    {
        private FileStream? stream = stream;

        public string DatabasePath { get; } = databasePath;
        public string GateIdentity { get; } = gateIdentity;
        public string DatabaseFileIdentity { get; } = databaseFileIdentity;
        public void Dispose() => Interlocked.Exchange(ref stream, null)?.Dispose();
    }

    private sealed class RuntimeLease(
        string databasePath,
        string gateIdentity,
        string databaseFileIdentity,
        FileStream stream) : Lease(databasePath, gateIdentity, databaseFileIdentity, stream),
            IDatabaseRuntimeLease
    { }

    private sealed class UpgradeLease(
        string databasePath,
        string gateIdentity,
        string databaseFileIdentity,
        FileStream stream) : Lease(databasePath, gateIdentity, databaseFileIdentity, stream),
            IDatabaseUpgradeLease
    {
        public bool PendingOperationCheckPassed => true;
    }
}

public sealed class SqliteLineageMigrationPreflight : ILineageMigrationPreflight
{
    public const int ExpectedSourceSchemaVersion = 16;

    private static readonly string[] RequiredTables =
    [
        "SchemaVersion",
        "BinMovements",
        "MovementBatches",
        "MovementCorrectionOperations",
        "MovementCorrectionLines",
        "ImportRuns"
    ];

    public async Task<LineageMigrationPreflightResult> InspectAsync(
        string databasePath,
        CancellationToken cancellationToken = default)
    {
        var canonicalPath = SqliteMigrationPath.NormalizeExistingDatabase(databasePath);
        var issues = new List<LineagePreflightIssue>();
        var builder = new SqliteConnectionStringBuilder
        {
            DataSource = canonicalPath,
            Mode = SqliteOpenMode.ReadOnly,
            Cache = SqliteCacheMode.Private,
            ForeignKeys = true,
            Pooling = false
        };

        await using var connection = new SqliteConnection(builder.ConnectionString);
        try
        {
            await connection.OpenAsync(cancellationToken);
            await ExecuteNonQueryAsync(connection, "PRAGMA query_only=ON;", cancellationToken);
        }
        catch (SqliteException ex)
        {
            throw new InvalidOperationException(
                "The lineage preflight could not open the SQLite database read-only.", ex);
        }

        var tables = await ReadStringsAsync(
            connection,
            "SELECT name FROM sqlite_master WHERE type='table' ORDER BY name;",
            cancellationToken);
        foreach (var required in RequiredTables.Except(tables, StringComparer.Ordinal))
        {
            issues.Add(new(
                LineagePreflightClassification.GlobalBlocker,
                LineagePreflightReasonCode.RequiredTableMissing,
                required));
        }

        var integrityPassed = await IntegrityCheckAsync(connection, cancellationToken);
        var journalMode = Convert.ToString(await ScalarAsync(
            connection,
            "PRAGMA journal_mode;",
            cancellationToken)) ?? "unknown";
        if (!integrityPassed)
        {
            issues.Add(new(
                LineagePreflightClassification.GlobalBlocker,
                LineagePreflightReasonCode.IntegrityCheckFailed,
                "Database"));
        }

        var foreignKeyPassed = await ForeignKeyCheckAsync(connection, cancellationToken);
        if (!foreignKeyPassed)
        {
            issues.Add(new(
                LineagePreflightClassification.GlobalBlocker,
                LineagePreflightReasonCode.ForeignKeyViolation,
                "Database"));
        }

        if (issues.Any(x => x.ReasonCode == LineagePreflightReasonCode.RequiredTableMissing))
        {
            return CreateResult(
                canonicalPath,
                0,
                new(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0),
                new Dictionary<string, long>(StringComparer.Ordinal),
                issues,
                [],
                journalMode,
                integrityPassed,
                foreignKeyPassed);
        }

        var schemaVersion = Convert.ToInt32(await ScalarAsync(
            connection,
            "SELECT Version FROM SchemaVersion WHERE Id=1;",
            cancellationToken));
        if (schemaVersion != ExpectedSourceSchemaVersion)
        {
            issues.Add(new(
                LineagePreflightClassification.GlobalBlocker,
                LineagePreflightReasonCode.UnsupportedSchemaVersion,
                "SchemaVersion",
                schemaVersion));
        }

        var movements = await ReadMovementsAsync(connection, cancellationToken);
        var tableRowCounts = await ReadTableRowCountsAsync(connection, tables, cancellationToken);
        var batches = await ReadBatchesAsync(connection, cancellationToken);
        var operations = await ReadOperationsAsync(connection, cancellationToken);
        var lines = await ReadCorrectionLinesAsync(connection, cancellationToken);

        ValidateRelationships(movements, batches, operations, lines, issues);

        var neutralisers = lines.Select(x => x.NeutralisingMovementId).ToHashSet();
        var ordinaryReversals = movements.Values
            .Where(x => x.ReversesMovementId.HasValue && !neutralisers.Contains(x.Id))
            .ToList();
        var repeatedCorrections = lines.Count(x =>
            lines.Any(next => next.OriginalMovementId == x.ReplacementMovementId));
        var partialBatches = CountPartiallyReversedBatches(movements, ordinaryReversals);
        var counts = new LineagePreflightCounts(
            movements.Count,
            batches.Count,
            operations.Count,
            lines.Count,
            ordinaryReversals.Count,
            movements.Values.Count(IsImportOwned),
            movements.Values.Count(x => x.Source == 3),
            operations.Values.Count(x => x.Kind == 0),
            operations.Values.Count(x => x.Kind == 1),
            repeatedCorrections,
            partialBatches);

        var structuralRows = new List<string>
        {
            $"schema|{schemaVersion}"
        };
        structuralRows.AddRange(movements.Values.OrderBy(x => x.Id).Select(x =>
            $"m|{x.Id}|{x.Source}|{x.MovementBatchId}|{x.ImportRunId}|{x.ReversesMovementId}|{x.CorrectedByMovementId}|{x.MovementDate}|{x.MovementType}"));
        structuralRows.AddRange(batches.Values.OrderBy(x => x.Id).Select(x =>
            $"b|{x.Id}|{x.MovementDate}|{x.MovementType}|{x.Source}"));
        structuralRows.AddRange(operations.Values.OrderBy(x => x.Id).Select(x =>
            $"o|{x.Id}|{x.Kind}|{x.OriginalBatchId}|{x.ReplacementBatchId}"));
        structuralRows.AddRange(lines.OrderBy(x => x.Id).Select(x =>
            $"l|{x.Id}|{x.OperationId}|{x.OriginalMovementId}|{x.NeutralisingMovementId}|{x.ReplacementMovementId}"));

        return CreateResult(
            canonicalPath,
            schemaVersion,
            counts,
            tableRowCounts,
            issues,
            structuralRows,
            journalMode,
            integrityPassed,
            foreignKeyPassed);
    }

    private static void ValidateRelationships(
        IReadOnlyDictionary<long, MovementFact> movements,
        IReadOnlyDictionary<long, BatchFact> batches,
        IReadOnlyDictionary<long, OperationFact> operations,
        IReadOnlyList<CorrectionLineFact> lines,
        ICollection<LineagePreflightIssue> issues)
    {
        foreach (var movement in movements.Values)
        {
            if (movement.MovementBatchId is long batchId && !batches.ContainsKey(batchId) ||
                movement.ReversesMovementId is long reversedId && !movements.ContainsKey(reversedId) ||
                movement.CorrectedByMovementId is long correctedId && !movements.ContainsKey(correctedId))
            {
                issues.Add(Invalid(LineagePreflightReasonCode.MissingStructuralReference, "BinMovement", movement.Id));
            }
            if (movement.MovementBatchId is long physicalBatchId &&
                batches.TryGetValue(physicalBatchId, out var batch) &&
                (movement.MovementDate != batch.MovementDate ||
                 movement.MovementType != batch.MovementType ||
                 movement.Source != batch.Source))
            {
                issues.Add(Invalid(
                    LineagePreflightReasonCode.InvalidPhysicalBatchRelationship,
                    "BinMovement",
                    movement.Id));
            }
        }

        var usedNeutralisers = new HashSet<long>();
        var usedReplacements = new HashSet<long>();
        foreach (var line in lines)
        {
            if (!operations.TryGetValue(line.OperationId, out var operation) ||
                !movements.TryGetValue(line.OriginalMovementId, out var original) ||
                !movements.TryGetValue(line.NeutralisingMovementId, out var neutraliser) ||
                !movements.TryGetValue(line.ReplacementMovementId, out var replacement))
            {
                issues.Add(Invalid(LineagePreflightReasonCode.MissingStructuralReference, "MovementCorrectionLine", line.Id));
                continue;
            }

            if (line.OriginalMovementId == line.NeutralisingMovementId ||
                line.OriginalMovementId == line.ReplacementMovementId ||
                line.NeutralisingMovementId == line.ReplacementMovementId ||
                neutraliser.ReversesMovementId != original.Id)
            {
                issues.Add(Invalid(LineagePreflightReasonCode.InvalidCorrectionTriple, "MovementCorrectionLine", line.Id));
            }

            if (!usedNeutralisers.Add(neutraliser.Id) || !usedReplacements.Add(replacement.Id))
            {
                issues.Add(Invalid(LineagePreflightReasonCode.DuplicateMovementConsumption, "MovementCorrectionLine", line.Id));
            }

            if (original.CorrectedByMovementId != neutraliser.Id)
            {
                issues.Add(Invalid(LineagePreflightReasonCode.CorrectedByRelationshipMismatch, "BinMovement", original.Id));
            }

            if (IsOutsideGenericLineage(original) ||
                IsOutsideGenericLineage(neutraliser) ||
                IsOutsideGenericLineage(replacement))
            {
                issues.Add(new(
                    LineagePreflightClassification.GlobalBlocker,
                    LineagePreflightReasonCode.CrossDomainLineage,
                    "MovementCorrectionOperation",
                    operation.Id));
            }
        }

        foreach (var reversal in movements.Values.Where(x => x.ReversesMovementId.HasValue && !usedNeutralisers.Contains(x.Id)))
        {
            var original = movements.GetValueOrDefault(reversal.ReversesMovementId!.Value);
            if (original is not null && original.CorrectedByMovementId != reversal.Id)
            {
                issues.Add(Invalid(LineagePreflightReasonCode.CorrectedByRelationshipMismatch, "BinMovement", original.Id));
            }
            if (original is not null &&
                (IsOutsideGenericLineage(original) || IsOutsideGenericLineage(reversal)))
            {
                issues.Add(new(
                    LineagePreflightClassification.GlobalBlocker,
                    LineagePreflightReasonCode.CrossDomainLineage,
                    "BinMovement",
                    reversal.Id));
            }
        }

        foreach (var operation in operations.Values)
        {
            var operationLines = lines.Where(x => x.OperationId == operation.Id).ToList();
            if (operation.Kind is not (0 or 1))
            {
                issues.Add(new(
                    LineagePreflightClassification.GlobalBlocker,
                    LineagePreflightReasonCode.UnsupportedCorrectionKind,
                    "MovementCorrectionOperation",
                    operation.Id));
            }
            if (operationLines.Count == 0 || operation.Kind == 0 && operationLines.Count != 1)
            {
                issues.Add(Invalid(LineagePreflightReasonCode.InvalidCorrectionTriple, "MovementCorrectionOperation", operation.Id));
            }
            if (operation.Kind == 1)
            {
                var originalBatchMembers = operation.OriginalBatchId.HasValue
                    ? movements.Values.Count(x => x.MovementBatchId == operation.OriginalBatchId)
                    : 0;
                var replacementBatchMembers = operation.ReplacementBatchId.HasValue
                    ? movements.Values.Count(x => x.MovementBatchId == operation.ReplacementBatchId)
                    : 0;
                var physicalRelationshipValid =
                    operation.OriginalBatchId.HasValue &&
                    operation.ReplacementBatchId.HasValue &&
                    operationLines.Count == originalBatchMembers &&
                    operationLines.Count == replacementBatchMembers &&
                    operationLines.All(line =>
                        movements.GetValueOrDefault(line.OriginalMovementId)?.MovementBatchId == operation.OriginalBatchId &&
                        movements.GetValueOrDefault(line.ReplacementMovementId)?.MovementBatchId == operation.ReplacementBatchId);
                if (!physicalRelationshipValid)
                {
                    issues.Add(Invalid(
                        LineagePreflightReasonCode.InvalidPhysicalBatchRelationship,
                        "MovementCorrectionOperation",
                        operation.Id));
                }
            }
        }

        foreach (var batchGroup in movements.Values.Where(x => x.MovementBatchId.HasValue).GroupBy(x => x.MovementBatchId!.Value))
        {
            var domains = batchGroup
                .Select(x => IsOutsideGenericLineage(x) ? "Excluded" : "Ordinary")
                .Distinct()
                .Count();
            if (domains > 1)
            {
                issues.Add(new(
                    LineagePreflightClassification.GlobalBlocker,
                    LineagePreflightReasonCode.MixedDomainPhysicalBatch,
                    "MovementBatch",
                    batchGroup.Key));
            }
        }

        DetectCycles(lines, issues);
    }

    private static void DetectCycles(
        IReadOnlyList<CorrectionLineFact> lines,
        ICollection<LineagePreflightIssue> issues)
    {
        var successor = lines.ToDictionary(x => x.OriginalMovementId, x => x.ReplacementMovementId);
        foreach (var start in successor.Keys)
        {
            var seen = new HashSet<long>();
            var current = start;
            while (successor.TryGetValue(current, out var next))
            {
                if (!seen.Add(current))
                {
                    issues.Add(Invalid(LineagePreflightReasonCode.CorrectionGraphCycle, "BinMovement", start));
                    break;
                }
                current = next;
            }
        }
    }

    private static long CountPartiallyReversedBatches(
        IReadOnlyDictionary<long, MovementFact> movements,
        IReadOnlyCollection<MovementFact> ordinaryReversals)
    {
        var reversedTargets = ordinaryReversals
            .Select(x => x.ReversesMovementId!.Value)
            .ToHashSet();
        return movements.Values
            .Where(x => x.MovementBatchId.HasValue)
            .GroupBy(x => x.MovementBatchId!.Value)
            .LongCount(group => group.Any(x => reversedTargets.Contains(x.Id)) &&
                                group.Any(x => !reversedTargets.Contains(x.Id)));
    }

    private static bool IsImportOwned(MovementFact movement) =>
        movement.ImportRunId.HasValue || movement.Source == 2;

    private static bool IsOutsideGenericLineage(MovementFact movement) =>
        IsImportOwned(movement) || movement.Source == 3;

    private static LineagePreflightIssue Invalid(
        LineagePreflightReasonCode reason,
        string entityType,
        long id) => new(LineagePreflightClassification.Invalid, reason, entityType, id);

    private static LineageMigrationPreflightResult CreateResult(
        string path,
        int schemaVersion,
        LineagePreflightCounts counts,
        IReadOnlyDictionary<string, long> tableRowCounts,
        IReadOnlyList<LineagePreflightIssue> issues,
        IReadOnlyList<string> structuralRows,
        string journalMode,
        bool integrityPassed,
        bool foreignKeyPassed)
    {
        var classification = issues.Count == 0
            ? LineagePreflightClassification.Migratable
            : issues.MaxBy(x => (int)x.Classification)!.Classification;
        var fingerprint = Convert.ToHexString(SHA256.HashData(
            Encoding.UTF8.GetBytes(string.Join('\n', structuralRows))));

        return new(
            Path.GetFileName(path),
            SqliteMigrationPath.IdentityHash(path),
            schemaVersion,
            counts,
            tableRowCounts,
            classification,
            issues,
            fingerprint,
            journalMode,
            integrityPassed,
            foreignKeyPassed);
    }

    private static async Task<Dictionary<long, MovementFact>> ReadMovementsAsync(
        SqliteConnection connection,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT Id, Source, MovementBatchId, ImportRunId, ReversesMovementId, CorrectedByMovementId, MovementDate, MovementType FROM BinMovements ORDER BY Id;";
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var result = new Dictionary<long, MovementFact>();
        while (await reader.ReadAsync(cancellationToken))
        {
            var fact = new MovementFact(
                reader.GetInt64(0),
                reader.GetInt32(1),
                NullableInt64(reader, 2),
                NullableInt64(reader, 3),
                NullableInt64(reader, 4),
                NullableInt64(reader, 5),
                reader.GetString(6),
                reader.GetInt32(7));
            result.Add(fact.Id, fact);
        }
        return result;
    }

    private static async Task<Dictionary<long, OperationFact>> ReadOperationsAsync(
        SqliteConnection connection,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT Id, Kind, OriginalBatchId, ReplacementBatchId FROM MovementCorrectionOperations ORDER BY Id;";
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var result = new Dictionary<long, OperationFact>();
        while (await reader.ReadAsync(cancellationToken))
        {
            var fact = new OperationFact(
                reader.GetInt64(0),
                reader.GetInt32(1),
                NullableInt64(reader, 2),
                NullableInt64(reader, 3));
            result.Add(fact.Id, fact);
        }
        return result;
    }

    private static async Task<List<CorrectionLineFact>> ReadCorrectionLinesAsync(
        SqliteConnection connection,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT Id, CorrectionOperationId, OriginalMovementId, NeutralisingMovementId, ReplacementMovementId FROM MovementCorrectionLines ORDER BY Id;";
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var result = new List<CorrectionLineFact>();
        while (await reader.ReadAsync(cancellationToken))
        {
            result.Add(new(
                reader.GetInt64(0),
                reader.GetInt64(1),
                reader.GetInt64(2),
                reader.GetInt64(3),
                reader.GetInt64(4)));
        }
        return result;
    }

    private static long? NullableInt64(SqliteDataReader reader, int ordinal) =>
        reader.IsDBNull(ordinal) ? null : reader.GetInt64(ordinal);

    private static async Task<Dictionary<long, BatchFact>> ReadBatchesAsync(
        SqliteConnection connection,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT Id, MovementDate, MovementType, Source FROM MovementBatches ORDER BY Id;";
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var result = new Dictionary<long, BatchFact>();
        while (await reader.ReadAsync(cancellationToken))
        {
            var fact = new BatchFact(
                reader.GetInt64(0),
                reader.GetString(1),
                reader.GetInt32(2),
                reader.GetInt32(3));
            result.Add(fact.Id, fact);
        }
        return result;
    }

    private static async Task<IReadOnlyDictionary<string, long>> ReadTableRowCountsAsync(
        SqliteConnection connection,
        IEnumerable<string> tableNames,
        CancellationToken cancellationToken)
    {
        var result = new SortedDictionary<string, long>(StringComparer.Ordinal);
        foreach (var tableName in tableNames.Where(x => !x.StartsWith("sqlite_", StringComparison.Ordinal)))
        {
            var quotedName = tableName.Replace("\"", "\"\"", StringComparison.Ordinal);
            result.Add(
                tableName,
                Convert.ToInt64(await ScalarAsync(
                    connection,
                    $"SELECT COUNT(*) FROM \"{quotedName}\";",
                    cancellationToken)));
        }
        return result;
    }

    private static async Task<List<string>> ReadStringsAsync(
        SqliteConnection connection,
        string sql,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var result = new List<string>();
        while (await reader.ReadAsync(cancellationToken))
            result.Add(reader.GetString(0));
        return result;
    }

    private static async Task<object> ScalarAsync(
        SqliteConnection connection,
        string sql,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        return await command.ExecuteScalarAsync(cancellationToken)
            ?? throw new InvalidOperationException("A required preflight value was missing.");
    }

    private static async Task ExecuteNonQueryAsync(
        SqliteConnection connection,
        string sql,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task<bool> IntegrityCheckAsync(
        SqliteConnection connection,
        CancellationToken cancellationToken)
    {
        var values = await ReadStringsAsync(connection, "PRAGMA integrity_check;", cancellationToken);
        return values.Count == 1 && values[0].Equals("ok", StringComparison.OrdinalIgnoreCase);
    }

    private static async Task<bool> ForeignKeyCheckAsync(
        SqliteConnection connection,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA foreign_key_check;";
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return !await reader.ReadAsync(cancellationToken);
    }

    private sealed record MovementFact(
        long Id,
        int Source,
        long? MovementBatchId,
        long? ImportRunId,
        long? ReversesMovementId,
        long? CorrectedByMovementId,
        string MovementDate,
        int MovementType);

    private sealed record BatchFact(long Id, string MovementDate, int MovementType, int Source);
    private sealed record OperationFact(long Id, int Kind, long? OriginalBatchId, long? ReplacementBatchId);
    private sealed record CorrectionLineFact(
        long Id,
        long OperationId,
        long OriginalMovementId,
        long NeutralisingMovementId,
        long ReplacementMovementId);
}

public static class LineageMigrationBackupPolicy
{
    public const int ManifestFormatVersion = 2;
    public const int ChecksumFormatVersion = 1;
    public const string Purpose = "PreLineageUpgradeRecovery";
    public const string Provider = "SQLite";
    public const string RecoveryPolicyId = "BinTracker-Lineage-Recovery-v1";
    public const string RecoveryInstructions =
        "Stop all BinTracker clients; preserve the failed database; verify this artifact for the configured source; restore while stopped; rerun integrity, FK, schema and lineage preflight checks before startup.";

    public static string DefaultRecoveryDirectory =>
        DatabaseConfiguration.LineageRecoveryFolder;
}

public interface ILineageMigrationBackupNameSource
{
    string CreateFileName(int sourceSchemaVersion);
}

public sealed class LineageMigrationBackupNameSource(
    TimeProvider? timeProvider = null,
    Func<Guid>? guidFactory = null) : ILineageMigrationBackupNameSource
{
    private readonly TimeProvider timeProvider = timeProvider ?? TimeProvider.System;
    private readonly Func<Guid> guidFactory = guidFactory ?? Guid.NewGuid;

    public string CreateFileName(int sourceSchemaVersion)
    {
        var timestamp = timeProvider.GetUtcNow().UtcDateTime;
        var shortGuid = guidFactory().ToString("N")[..8];
        return $"BinTracker-pre-lineage-v{sourceSchemaVersion}-{timestamp:yyyyMMddTHHmmssfffZ}-{shortGuid}.db";
    }
}

public sealed record LineageMigrationBackupManifest(
    int FormatVersion,
    Guid ArtifactId,
    string Purpose,
    DateTime CreatedUtc,
    string ApplicationInformationalVersion,
    string Provider,
    string SourceDatabasePath,
    string SourceFileName,
    string SourcePathIdentityHash,
    string SourceFileIdentity,
    int SourceSchemaVersion,
    long SourceDatabaseSize,
    DateTime SourceLastWriteUtc,
    string SourceJournalMode,
    string BackupFileName,
    long BackupSize,
    string BackupSha256,
    IReadOnlyDictionary<string, long> TableRowCounts,
    string IntegrityCheckResult,
    string ForeignKeyCheckResult,
    LineagePreflightClassification PreflightClassification,
    LineagePreflightCounts PreflightCounts,
    string StructuralFingerprint,
    string RecoveryPolicyId,
    string RecoveryInstructions);

public sealed record LineageMigrationChecksumEvidence(
    int FormatVersion,
    Guid ArtifactId,
    string BackupFileName,
    string BackupSha256,
    string ManifestFileName,
    string ManifestSha256);

public sealed record VerifiedLineageMigrationBackup(
    string BackupPath,
    string ManifestPath,
    string ChecksumPath,
    LineageMigrationBackupManifest Manifest);

public sealed record LineageMigrationBackupVerification(
    bool IsValidForExpectedSource,
    string? FailureCode,
    LineageMigrationBackupManifest? Manifest);

public interface ILineageMigrationBackupVerifier
{
    Task<LineageMigrationBackupVerification> VerifyForSourceAsync(
        string manifestPath,
        string expectedSourceDatabasePath,
        int expectedSourceSchemaVersion = SqliteLineageMigrationPreflight.ExpectedSourceSchemaVersion,
        CancellationToken cancellationToken = default);
}

public sealed class SqliteLineageMigrationBackupService : ILineageMigrationBackupVerifier
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private readonly IDatabaseUpgradeGate upgradeGate;
    private readonly ILineageMigrationPreflight preflight;
    private readonly ILineageMigrationBackupNameSource nameSource;
    private readonly Func<Guid> artifactIdFactory;
    private readonly TimeProvider timeProvider;

    public SqliteLineageMigrationBackupService(
        IDatabaseUpgradeGate upgradeGate,
        ILineageMigrationPreflight preflight,
        ILineageMigrationBackupNameSource? nameSource = null,
        Func<Guid>? artifactIdFactory = null,
        TimeProvider? timeProvider = null)
    {
        this.upgradeGate = upgradeGate;
        this.preflight = preflight;
        this.nameSource = nameSource ?? new LineageMigrationBackupNameSource();
        this.artifactIdFactory = artifactIdFactory ?? Guid.NewGuid;
        this.timeProvider = timeProvider ?? TimeProvider.System;
    }

    public Task<VerifiedLineageMigrationBackup> CreateVerifiedAsync(
        string sourceDatabasePath,
        CancellationToken cancellationToken = default) =>
        CreateVerifiedAsync(
            sourceDatabasePath,
            LineageMigrationBackupPolicy.DefaultRecoveryDirectory,
            cancellationToken);

    public async Task<VerifiedLineageMigrationBackup> CreateVerifiedAsync(
        string sourceDatabasePath,
        string backupDirectory,
        CancellationToken cancellationToken = default)
    {
        using var gate = upgradeGate.AcquireUpgrade(sourceDatabasePath);
        return await CreateVerifiedAsync(gate, backupDirectory, cancellationToken);
    }

    /// <summary>
    /// Creates the backup while a future upgrade coordinator retains one lease
    /// across preflight, backup, migration and postflight.
    /// </summary>
    public async Task<VerifiedLineageMigrationBackup> CreateVerifiedAsync(
        IDatabaseUpgradeLease upgradeLease,
        string backupDirectory,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(upgradeLease);
        if (!upgradeLease.PendingOperationCheckPassed)
            throw new InvalidOperationException("The upgrade lease did not prove pending-operation clearance.");

        var sourcePath = SqliteMigrationPath.NormalizeExistingDatabase(
            upgradeLease.DatabasePath);
        var sourceFileIdentity = WindowsFileIdentity.Get(sourcePath);
        if (!sourceFileIdentity.Equals(
                upgradeLease.DatabaseFileIdentity,
                StringComparison.Ordinal))
        {
            throw new InvalidOperationException("The upgrade lease belongs to a different database file.");
        }

        var destinationDirectory = Path.GetFullPath(backupDirectory);
        Directory.CreateDirectory(destinationDirectory);
        destinationDirectory = SqliteMigrationPath.NormalizeExistingDirectory(destinationDirectory);
        if (SqliteMigrationPath.IsSameOrChildDirectory(sourcePath, destinationDirectory))
        {
            throw new InvalidOperationException(
                "The migration recovery backup must be outside the active database directory.");
        }

        var sourceBefore = await preflight.InspectAsync(sourcePath, cancellationToken);
        EnsureEligible(sourceBefore);
        var sourceInfo = new FileInfo(sourcePath);
        var temporaryBackupPath = Path.Combine(
            destinationDirectory,
            $".bintracker-pre-lineage-{Guid.NewGuid():N}.tmp.db");
        string? backupPath = null;
        string? manifestPath = null;
        string? checksumPath = null;

        try
        {
            await CreateSqliteBackupAsync(sourcePath, temporaryBackupPath, cancellationToken);
            EnsureNonEmptyFile(temporaryBackupPath, "BACKUP_EMPTY");
            backupPath = PublishCreateNew(
                temporaryBackupPath,
                destinationDirectory,
                sourceBefore.SchemaVersion);
            manifestPath = backupPath + ".manifest.json";
            checksumPath = backupPath + ".checksums.json";

            var sourceAfter = await preflight.InspectAsync(sourcePath, cancellationToken);
            var backup = await preflight.InspectAsync(backupPath, cancellationToken);
            EnsureEquivalent(sourceBefore, sourceAfter, "SOURCE_CHANGED_DURING_BACKUP");
            EnsureEquivalent(sourceAfter, backup, "BACKUP_PREFLIGHT_MISMATCH");
            EnsureNonEmptyFile(backupPath, "BACKUP_EMPTY");

            var artifactId = artifactIdFactory();
            var backupInfo = new FileInfo(backupPath);
            var backupHash = await SqliteMigrationPath.Sha256FileAsync(backupPath, cancellationToken);
            var manifest = new LineageMigrationBackupManifest(
                LineageMigrationBackupPolicy.ManifestFormatVersion,
                artifactId,
                LineageMigrationBackupPolicy.Purpose,
                timeProvider.GetUtcNow().UtcDateTime,
                GetApplicationInformationalVersion(),
                LineageMigrationBackupPolicy.Provider,
                sourcePath,
                sourceAfter.SourceFileName,
                sourceAfter.SourceIdentityHash,
                sourceFileIdentity,
                sourceAfter.SchemaVersion,
                sourceInfo.Length,
                sourceInfo.LastWriteTimeUtc,
                sourceAfter.JournalMode,
                backupInfo.Name,
                backupInfo.Length,
                backupHash,
                backup.TableRowCounts,
                backup.IntegrityCheckPassed ? "ok" : "failed",
                backup.ForeignKeyCheckPassed ? "ok" : "failed",
                backup.Classification,
                backup.Counts,
                backup.StructuralFingerprint,
                LineageMigrationBackupPolicy.RecoveryPolicyId,
                LineageMigrationBackupPolicy.RecoveryInstructions);

            await WriteCreateNewAsync(
                manifestPath,
                JsonSerializer.Serialize(manifest, JsonOptions),
                cancellationToken);
            var manifestHash = await SqliteMigrationPath.Sha256FileAsync(manifestPath, cancellationToken);
            var checksum = new LineageMigrationChecksumEvidence(
                LineageMigrationBackupPolicy.ChecksumFormatVersion,
                artifactId,
                backupInfo.Name,
                backupHash,
                Path.GetFileName(manifestPath),
                manifestHash);
            await WriteCreateNewAsync(
                checksumPath,
                JsonSerializer.Serialize(checksum, JsonOptions),
                cancellationToken);

            var verification = await VerifyForSourceAsync(
                manifestPath,
                sourcePath,
                sourceAfter.SchemaVersion,
                cancellationToken);
            if (!verification.IsValidForExpectedSource)
                throw new InvalidOperationException($"Migration backup verification failed: {verification.FailureCode}.");

            return new(backupPath, manifestPath, checksumPath, manifest);
        }
        catch
        {
            TryDelete(temporaryBackupPath);
            TryDelete(backupPath);
            TryDelete(manifestPath);
            TryDelete(checksumPath);
            throw;
        }
    }

    public async Task<LineageMigrationBackupVerification> VerifyForSourceAsync(
        string manifestPath,
        string expectedSourceDatabasePath,
        int expectedSourceSchemaVersion = SqliteLineageMigrationPreflight.ExpectedSourceSchemaVersion,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var expectedSourcePath = SqliteMigrationPath.NormalizeExistingDatabase(
                expectedSourceDatabasePath);
            var canonicalManifestPath = Path.GetFullPath(manifestPath);
            var backupPathFromManifestName = canonicalManifestPath.EndsWith(
                ".manifest.json",
                StringComparison.OrdinalIgnoreCase)
                ? canonicalManifestPath[..^".manifest.json".Length]
                : string.Empty;
            var checksumPath = backupPathFromManifestName + ".checksums.json";
            if (!File.Exists(canonicalManifestPath) || !File.Exists(checksumPath))
                return new(false, "RECOVERY_EVIDENCE_MISSING", null);

            var checksum = JsonSerializer.Deserialize<LineageMigrationChecksumEvidence>(
                await File.ReadAllTextAsync(checksumPath, cancellationToken));
            if (checksum is null ||
                checksum.FormatVersion != LineageMigrationBackupPolicy.ChecksumFormatVersion ||
                checksum.ManifestFileName != Path.GetFileName(canonicalManifestPath) ||
                checksum.BackupFileName != Path.GetFileName(backupPathFromManifestName))
            {
                return new(false, "CHECKSUM_EVIDENCE_INVALID", null);
            }

            var actualManifestHash = await SqliteMigrationPath.Sha256FileAsync(
                canonicalManifestPath,
                cancellationToken);
            if (!FixedHashEquals(checksum.ManifestSha256, actualManifestHash))
                return new(false, "MANIFEST_HASH_MISMATCH", null);

            var manifest = JsonSerializer.Deserialize<LineageMigrationBackupManifest>(
                await File.ReadAllTextAsync(canonicalManifestPath, cancellationToken));
            if (!ManifestIsStructurallyValid(manifest, checksum))
                return new(false, "MANIFEST_INVALID", manifest);

            if (!Path.GetFullPath(manifest!.SourceDatabasePath).Equals(
                    expectedSourcePath,
                    StringComparison.OrdinalIgnoreCase) ||
                manifest.SourcePathIdentityHash != SqliteMigrationPath.IdentityHash(expectedSourcePath) ||
                manifest.SourceFileIdentity != WindowsFileIdentity.Get(expectedSourcePath) ||
                manifest.SourceSchemaVersion != expectedSourceSchemaVersion)
            {
                return new(false, "SOURCE_IDENTITY_MISMATCH", manifest);
            }

            var backupPath = Path.Combine(
                Path.GetDirectoryName(canonicalManifestPath)
                    ?? throw new InvalidOperationException("Manifest directory is invalid."),
                manifest.BackupFileName);
            if (!backupPath.Equals(backupPathFromManifestName, StringComparison.OrdinalIgnoreCase))
                return new(false, "MANIFEST_INVALID", manifest);
            if (!File.Exists(backupPath))
                return new(false, "BACKUP_MISSING", manifest);
            var backupInfo = new FileInfo(backupPath);
            if (backupInfo.Length <= 0 || backupInfo.Length != manifest.BackupSize)
                return new(false, "BACKUP_SIZE_MISMATCH", manifest);

            var actualBackupHash = await SqliteMigrationPath.Sha256FileAsync(
                backupPath,
                cancellationToken);
            if (!FixedHashEquals(checksum.BackupSha256, actualBackupHash) ||
                !FixedHashEquals(manifest.BackupSha256, actualBackupHash))
            {
                return new(false, "BACKUP_HASH_MISMATCH", manifest);
            }

            var backup = await preflight.InspectAsync(backupPath, cancellationToken);
            if (!backup.IntegrityCheckPassed || !backup.ForeignKeyCheckPassed)
                return new(false, "BACKUP_INTEGRITY_FAILURE", manifest);
            if (backup.SchemaVersion != manifest.SourceSchemaVersion ||
                backup.Counts != manifest.PreflightCounts ||
                backup.Classification != manifest.PreflightClassification ||
                !SameTableCounts(backup.TableRowCounts, manifest.TableRowCounts) ||
                backup.StructuralFingerprint != manifest.StructuralFingerprint)
            {
                return new(false, "BACKUP_PREFLIGHT_MISMATCH", manifest);
            }

            return new(true, null, manifest);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException or FormatException or CryptographicException or SqliteException or InvalidOperationException or ArgumentException)
        {
            return new(false, "BACKUP_VERIFICATION_ERROR", null);
        }
    }

    private async Task CreateSqliteBackupAsync(
        string sourcePath,
        string temporaryBackupPath,
        CancellationToken cancellationToken)
    {
        using (new FileStream(
            temporaryBackupPath,
            FileMode.CreateNew,
            FileAccess.ReadWrite,
            FileShare.None))
        { }

        var sourceBuilder = new SqliteConnectionStringBuilder
        {
            DataSource = sourcePath,
            Mode = SqliteOpenMode.ReadOnly,
            Cache = SqliteCacheMode.Private,
            Pooling = false
        };
        var destinationBuilder = new SqliteConnectionStringBuilder
        {
            DataSource = temporaryBackupPath,
            Mode = SqliteOpenMode.ReadWrite,
            Cache = SqliteCacheMode.Private,
            Pooling = false
        };
        await using var source = new SqliteConnection(sourceBuilder.ConnectionString);
        await using var destination = new SqliteConnection(destinationBuilder.ConnectionString);
        await source.OpenAsync(cancellationToken);
        await destination.OpenAsync(cancellationToken);
        source.BackupDatabase(destination);
    }

    private string PublishCreateNew(
        string temporaryBackupPath,
        string destinationDirectory,
        int sourceSchemaVersion)
    {
        const int maximumAttempts = 32;
        for (var attempt = 0; attempt < maximumAttempts; attempt++)
        {
            var fileName = nameSource.CreateFileName(sourceSchemaVersion);
            if (fileName != Path.GetFileName(fileName) ||
                !fileName.EndsWith(".db", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("The backup name source returned an invalid filename.");
            }

            var candidate = Path.Combine(destinationDirectory, fileName);
            try
            {
                File.Move(temporaryBackupPath, candidate, overwrite: false);
                return candidate;
            }
            catch (IOException) when (File.Exists(candidate))
            {
                // The atomic no-overwrite publish lost a collision race. Try a
                // fresh policy-compliant name without touching the existing file.
            }
        }

        throw new IOException("A unique pre-lineage recovery backup name could not be reserved.");
    }

    private static async Task WriteCreateNewAsync(
        string path,
        string content,
        CancellationToken cancellationToken)
    {
        await using var stream = new FileStream(
            path,
            FileMode.CreateNew,
            FileAccess.Write,
            FileShare.None,
            4096,
            FileOptions.Asynchronous | FileOptions.WriteThrough);
        await using var writer = new StreamWriter(stream, new UTF8Encoding(false));
        await writer.WriteAsync(content.AsMemory(), cancellationToken);
        await writer.FlushAsync(cancellationToken);
    }

    private static void EnsureNonEmptyFile(string path, string failureCode)
    {
        if (!File.Exists(path) || new FileInfo(path).Length <= 0)
            throw new InvalidOperationException(failureCode);
    }

    private static bool ManifestIsStructurallyValid(
        LineageMigrationBackupManifest? manifest,
        LineageMigrationChecksumEvidence checksum) =>
        manifest is not null &&
        manifest.FormatVersion == LineageMigrationBackupPolicy.ManifestFormatVersion &&
        manifest.ArtifactId == checksum.ArtifactId &&
        manifest.Purpose == LineageMigrationBackupPolicy.Purpose &&
        manifest.Provider == LineageMigrationBackupPolicy.Provider &&
        manifest.RecoveryPolicyId == LineageMigrationBackupPolicy.RecoveryPolicyId &&
        manifest.BackupFileName == checksum.BackupFileName &&
        !Path.IsPathRooted(manifest.BackupFileName) &&
        manifest.BackupFileName == Path.GetFileName(manifest.BackupFileName) &&
        manifest.BackupSize > 0;

    private static bool FixedHashEquals(string expected, string actual) =>
        CryptographicOperations.FixedTimeEquals(
            Convert.FromHexString(expected),
            Convert.FromHexString(actual));

    private static string GetApplicationInformationalVersion() =>
        typeof(DatabaseSetup).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion
        ?? throw new InvalidOperationException("BinTracker informational version is unavailable.");

    private static void EnsureEligible(LineageMigrationPreflightResult result)
    {
        if (result.Classification == LineagePreflightClassification.GlobalBlocker ||
            !result.IntegrityCheckPassed ||
            !result.ForeignKeyCheckPassed)
        {
            throw new InvalidOperationException(
                "The source database failed the lineage migration preflight.");
        }
    }

    private static void EnsureEquivalent(
        LineageMigrationPreflightResult expected,
        LineageMigrationPreflightResult actual,
        string failureCode)
    {
        if (expected.SchemaVersion != actual.SchemaVersion ||
            expected.Counts != actual.Counts ||
            !SameTableCounts(expected.TableRowCounts, actual.TableRowCounts) ||
            expected.StructuralFingerprint != actual.StructuralFingerprint ||
            !actual.IntegrityCheckPassed ||
            !actual.ForeignKeyCheckPassed)
        {
            throw new InvalidOperationException(failureCode);
        }
    }

    private static bool SameTableCounts(
        IReadOnlyDictionary<string, long> expected,
        IReadOnlyDictionary<string, long> actual) =>
        expected.Count == actual.Count &&
        expected.OrderBy(x => x.Key, StringComparer.Ordinal)
            .SequenceEqual(actual.OrderBy(x => x.Key, StringComparer.Ordinal));

    private static void TryDelete(string? path)
    {
        try
        {
            if (!string.IsNullOrWhiteSpace(path) && File.Exists(path))
                File.Delete(path);
        }
        catch (IOException)
        {
            // Preserve the original failure; incomplete uniquely named files are
            // never returned as verified recovery artifacts.
        }
        catch (UnauthorizedAccessException)
        {
            // Preserve the original failure for the caller.
        }
    }
}

public enum LineageMigrationRecoveryDisposition
{
    PreserveActiveDatabase = 0,
    ControlledRestoreEligible = 1,
    RecoveryProhibited = 2
}

public sealed record LineageMigrationRecoveryFacts(
    bool MigrationRolledBack,
    bool ActiveDatabaseValid,
    bool BackupVerifiedForExactSource,
    bool MigrationCommittedOrPostflightFailed);

public static class LineageMigrationRecoveryClassifier
{
    public static LineageMigrationRecoveryDisposition Classify(
        LineageMigrationRecoveryFacts facts)
    {
        if (facts.ActiveDatabaseValid)
            return LineageMigrationRecoveryDisposition.PreserveActiveDatabase;
        if (!facts.BackupVerifiedForExactSource)
            return LineageMigrationRecoveryDisposition.RecoveryProhibited;
        return facts.MigrationRolledBack || facts.MigrationCommittedOrPostflightFailed
            ? LineageMigrationRecoveryDisposition.ControlledRestoreEligible
            : LineageMigrationRecoveryDisposition.RecoveryProhibited;
    }
}

internal static class SqliteMigrationPath
{
    public static string NormalizeExistingDatabase(string databasePath)
    {
        if (string.IsNullOrWhiteSpace(databasePath))
            throw new ArgumentException("A SQLite database path is required.", nameof(databasePath));

        var fullPath = Path.GetFullPath(databasePath);
        if (!File.Exists(fullPath))
            throw new FileNotFoundException("The SQLite database does not exist.", fullPath);
        return ResolveFileSystemLinks(new FileInfo(fullPath));
    }

    public static string NormalizeExistingDirectory(string directoryPath)
    {
        var fullPath = Path.GetFullPath(directoryPath);
        if (!Directory.Exists(fullPath))
            throw new DirectoryNotFoundException(fullPath);
        return ResolveFileSystemLinks(new DirectoryInfo(fullPath));
    }

    public static string IdentityHash(string path) => Convert.ToHexString(
        SHA256.HashData(Encoding.UTF8.GetBytes(
            Path.GetFullPath(path).ToUpperInvariant())));

    public static bool IsSameOrChildDirectory(string databasePath, string destinationDirectory)
    {
        var sourceDirectory = Path.GetDirectoryName(Path.GetFullPath(databasePath))
            ?? throw new InvalidOperationException("The source database directory is invalid.");
        var sourceWithSeparator = sourceDirectory.TrimEnd(Path.DirectorySeparatorChar) +
            Path.DirectorySeparatorChar;
        var destinationWithSeparator = Path.GetFullPath(destinationDirectory)
            .TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        return destinationWithSeparator.StartsWith(
            sourceWithSeparator,
            StringComparison.OrdinalIgnoreCase);
    }

    public static async Task<string> Sha256FileAsync(
        string path,
        CancellationToken cancellationToken)
    {
        await using var stream = new FileStream(
            path,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            81920,
            FileOptions.Asynchronous | FileOptions.SequentialScan);
        return Convert.ToHexString(await SHA256.HashDataAsync(stream, cancellationToken));
    }

    private static string ResolveFileSystemLinks(FileSystemInfo info)
    {
        FileSystemInfo? directTarget;
        try
        {
            directTarget = info.ResolveLinkTarget(returnFinalTarget: true);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or PlatformNotSupportedException)
        {
            // Access-protected ancestors (for example the legacy Windows
            // AppData junction) must not make an otherwise valid DB unusable.
            directTarget = null;
        }
        if (directTarget is not null)
            return Path.GetFullPath(directTarget.FullName);

        var parent = info switch
        {
            FileInfo file => file.Directory,
            DirectoryInfo directory => directory.Parent,
            _ => null
        };
        if (parent is null)
            return Path.GetFullPath(info.FullName);

        var resolvedParent = ResolveFileSystemLinks(parent);
        return Path.Combine(resolvedParent, info.Name);
    }
}

internal static class WindowsFileIdentity
{
    public static string Get(string path)
    {
        if (!OperatingSystem.IsWindows())
        {
            throw new DatabaseUpgradeUnavailableException(
                DatabaseUpgradeUnavailableReason.FileIdentityUnavailable,
                "SQLite upgrade file identity is supported only on Windows.");
        }

        try
        {
            using var handle = File.OpenHandle(
                path,
                FileMode.Open,
                FileAccess.Read,
                FileShare.ReadWrite | FileShare.Delete);
            if (!GetFileInformationByHandle(handle, out var info))
            {
                throw new IOException(
                    "Windows could not read the SQLite database file identity.",
                    Marshal.GetExceptionForHR(Marshal.GetHRForLastWin32Error()));
            }

            return $"WIN32:{info.VolumeSerialNumber:X8}:{info.FileIndexHigh:X8}{info.FileIndexLow:X8}";
        }
        catch (DatabaseUpgradeUnavailableException)
        {
            throw;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            throw new DatabaseUpgradeUnavailableException(
                DatabaseUpgradeUnavailableReason.FileIdentityUnavailable,
                "The SQLite database file identity could not be proven.",
                ex);
        }
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetFileInformationByHandle(
        SafeFileHandle fileHandle,
        out ByHandleFileInformation fileInformation);

    [StructLayout(LayoutKind.Sequential)]
    private struct ByHandleFileInformation
    {
        public uint FileAttributes;
        public System.Runtime.InteropServices.ComTypes.FILETIME CreationTime;
        public System.Runtime.InteropServices.ComTypes.FILETIME LastAccessTime;
        public System.Runtime.InteropServices.ComTypes.FILETIME LastWriteTime;
        public uint VolumeSerialNumber;
        public uint FileSizeHigh;
        public uint FileSizeLow;
        public uint NumberOfLinks;
        public uint FileIndexHigh;
        public uint FileIndexLow;
    }
}
