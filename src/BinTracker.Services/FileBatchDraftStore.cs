using System.Globalization;
using System.Text.Json;
using BinTracker.Core;

namespace BinTracker.Services;

/// <summary>
/// Persists unfinished Batch Entry work in client-local, owner-specific files.
/// The authenticated account's stable persisted ID is the ownership authority;
/// filenames, Windows profiles, usernames and roles are not authorization.
/// </summary>
public sealed class FileBatchDraftStore : IBatchDraftStore
{
    private sealed record StoredDraft(
        int SchemaVersion,
        int OwnerUserId,
        long Revision,
        string State,
        Guid ClientOperationId,
        DateOnly? MovementDate,
        MovementType? MovementType,
        IReadOnlyList<DraftMovementLine>? Lines,
        DateTimeOffset SavedAtUtc);

    private sealed record ParsedDraft(
        int OwnerUserId,
        long Revision,
        bool HasDraft,
        Guid ClientOperationId,
        DateOnly MovementDate,
        MovementType MovementType,
        IReadOnlyList<DraftMovementLine> Lines,
        DateTimeOffset SavedAtUtc,
        bool OperationIdentityPersisted);

    public const int MaximumFileBytes = 16 * 1024 * 1024;
    public const int MaximumLineCount = 10_000;

    private const int CurrentSchemaVersion = 4;
    private const int MaximumJsonDepth = 8;
    private readonly string legacyFilePath;
    private readonly JsonSerializerOptions jsonOptions = new()
    {
        WriteIndented = true
    };

    public FileBatchDraftStore()
        : this(Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "BinTracker",
            "batch-entry-draft.json"))
    {
    }

    public FileBatchDraftStore(string legacyFilePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(legacyFilePath);
        this.legacyFilePath = legacyFilePath;
    }

    public BatchDraftLoadResult Load(int ownerUserId)
    {
        RequireOwnerId(ownerUserId);

        var legacyDiagnostic = PreserveLegacyOwnerlessDraft();
        var ownerPath = OwnerPath(ownerUserId);
        if (!File.Exists(ownerPath))
            return new BatchDraftLoadResult(null, legacyDiagnostic, 0);

        byte[] bytes;
        try
        {
            bytes = ReadBounded(ownerPath);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidDataException)
        {
            return RejectAndPreserve(ownerPath, ex.Message, legacyDiagnostic);
        }

        ParsedDraft stored;
        try
        {
            stored = ParseAndValidate(bytes, ownerUserId);
        }
        catch (Exception ex) when (ex is JsonException or InvalidDataException or OverflowException)
        {
            return RejectAndPreserve(ownerPath, ex.Message, legacyDiagnostic);
        }

        if (!stored.HasDraft)
            return new BatchDraftLoadResult(null, legacyDiagnostic, stored.Revision);

        return new BatchDraftLoadResult(
            new DraftMovementBatchSnapshot(
                stored.OwnerUserId,
                stored.ClientOperationId,
                stored.MovementDate,
                stored.MovementType,
                stored.Lines,
                stored.SavedAtUtc,
                stored.Revision,
                stored.OperationIdentityPersisted),
            legacyDiagnostic,
            stored.Revision);
    }

    public long Save(int ownerUserId, DraftMovementBatch draft)
    {
        RequireOwnerId(ownerUserId);
        ArgumentNullException.ThrowIfNull(draft);
        ProviderNeutralValidation.RequireDefined(draft.MovementType, nameof(draft.MovementType));

        if (!draft.HasLines)
        {
            return Clear(ownerUserId, draft.StorageRevision, draft.ClientOperationId);
        }

        ValidateLines(draft.Lines);

        var ownerPath = OwnerPath(ownerUserId);
        var directory = Path.GetDirectoryName(ownerPath);
        if (!string.IsNullOrWhiteSpace(directory))
            Directory.CreateDirectory(directory);

        var nextRevision = checked(draft.StorageRevision + 1);
        var stored = new StoredDraft(
            CurrentSchemaVersion,
            ownerUserId,
            nextRevision,
            "Active",
            draft.ClientOperationId,
            draft.MovementDate,
            draft.MovementType,
            draft.Lines.ToList(),
            DateTimeOffset.UtcNow);
        var bytes = JsonSerializer.SerializeToUtf8Bytes(stored, jsonOptions);
        if (bytes.Length > MaximumFileBytes)
            throw new InvalidOperationException(
                $"The unfinished batch exceeds the {MaximumFileBytes:N0}-byte recovery safety limit and was not written.");

        var temporaryPath = ownerPath + $".{Guid.NewGuid():N}.tmp";
        try
        {
            using (var stream = new FileStream(
                       temporaryPath,
                       FileMode.CreateNew,
                       FileAccess.Write,
                       FileShare.None,
                       bufferSize: 64 * 1024,
                       FileOptions.WriteThrough))
            {
                stream.Write(bytes);
                stream.Flush(flushToDisk: true);
            }

            using var publicationLease = AcquirePublicationLease(ownerPath);
            RequireExpectedRevision(ownerPath, ownerUserId, draft.StorageRevision);
            File.Move(temporaryPath, ownerPath, overwrite: true);
            draft.MarkPersisted(nextRevision);
            return nextRevision;
        }
        finally
        {
            TryDeleteOwnTemporaryFile(temporaryPath);
        }
    }

    public long Clear(int ownerUserId, long expectedRevision, Guid clearedOperationId)
    {
        RequireOwnerId(ownerUserId);
        if (expectedRevision < 0)
            throw new ArgumentOutOfRangeException(nameof(expectedRevision));
        if (clearedOperationId == Guid.Empty)
            throw new ArgumentException("A durable draft operation ID is required.", nameof(clearedOperationId));

        var ownerPath = OwnerPath(ownerUserId);
        var directory = Path.GetDirectoryName(ownerPath);
        if (!string.IsNullOrWhiteSpace(directory))
            Directory.CreateDirectory(directory);

        var nextRevision = checked(expectedRevision + 1);
        var stored = new StoredDraft(
            CurrentSchemaVersion,
            ownerUserId,
            nextRevision,
            "Cleared",
            clearedOperationId,
            null,
            null,
            null,
            DateTimeOffset.UtcNow);
        var bytes = JsonSerializer.SerializeToUtf8Bytes(stored, jsonOptions);
        var temporaryPath = ownerPath + $".{Guid.NewGuid():N}.tmp";
        try
        {
            using (var stream = new FileStream(
                       temporaryPath,
                       FileMode.CreateNew,
                       FileAccess.Write,
                       FileShare.None,
                       bufferSize: 64 * 1024,
                       FileOptions.WriteThrough))
            {
                stream.Write(bytes);
                stream.Flush(flushToDisk: true);
            }

            using var publicationLease = AcquirePublicationLease(ownerPath);
            RequireExpectedRevision(ownerPath, ownerUserId, expectedRevision);
            File.Move(temporaryPath, ownerPath, overwrite: true);
            return nextRevision;
        }
        finally
        {
            TryDeleteOwnTemporaryFile(temporaryPath);
        }
    }

    private string OwnerPath(int ownerUserId)
    {
        var directory = Path.GetDirectoryName(legacyFilePath) ?? string.Empty;
        var extension = Path.GetExtension(legacyFilePath);
        var name = Path.GetFileNameWithoutExtension(legacyFilePath);
        return Path.Combine(directory, $"{name}.user-{ownerUserId}{extension}");
    }

    private string? PreserveLegacyOwnerlessDraft()
    {
        if (!File.Exists(legacyFilePath))
            return null;

        return Quarantine(
            legacyFilePath,
            "An older ownerless Batch Entry draft was preserved but cannot be assigned to any user.");
    }

    private BatchDraftLoadResult RejectAndPreserve(
        string path,
        string reason,
        string? existingDiagnostic)
    {
        var diagnostic = Quarantine(
            path,
            $"The persisted Batch Entry draft was rejected: {reason}");
        return new BatchDraftLoadResult(
            null,
            CombineDiagnostics(existingDiagnostic, diagnostic));
    }

    private static string Quarantine(string sourcePath, string reason)
    {
        var quarantinePath =
            sourcePath + $".corrupt-{DateTime.UtcNow:yyyyMMddHHmmssfffffff}-{Guid.NewGuid():N}";
        try
        {
            File.Move(sourcePath, quarantinePath, overwrite: false);
            return $"{reason} Evidence was quarantined without overwrite as '{Path.GetFileName(quarantinePath)}'.";
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return $"{reason} Quarantine failed, so the original evidence remains in place. {ex.Message}";
        }
    }

    private static byte[] ReadBounded(string path)
    {
        using var source = new FileStream(
            path,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 64 * 1024,
            FileOptions.SequentialScan);
        if (source.Length > MaximumFileBytes)
            throw new InvalidDataException(
                $"The recovery file exceeds the {MaximumFileBytes:N0}-byte safety limit.");

        using var destination = new MemoryStream((int)source.Length);
        var buffer = new byte[64 * 1024];
        var total = 0;
        while (true)
        {
            var read = source.Read(buffer, 0, buffer.Length);
            if (read == 0)
                break;
            total = checked(total + read);
            if (total > MaximumFileBytes)
                throw new InvalidDataException(
                    $"The recovery file exceeds the {MaximumFileBytes:N0}-byte safety limit.");
            destination.Write(buffer, 0, read);
        }

        return destination.ToArray();
    }

    private static ParsedDraft ParseAndValidate(byte[] bytes, int expectedOwnerUserId)
    {
        using var document = JsonDocument.Parse(bytes, new JsonDocumentOptions
        {
            AllowTrailingCommas = false,
            CommentHandling = JsonCommentHandling.Disallow,
            MaxDepth = MaximumJsonDepth
        });
        var root = document.RootElement;
        RequireObject(root, "draft");

        var schemaVersion = RequiredInt32(root, "SchemaVersion");
        if (schemaVersion is not (3 or CurrentSchemaVersion))
            throw new InvalidDataException($"Draft schema version {schemaVersion} is not supported.");

        if (schemaVersion == 3)
        {
            RequireExactProperties(root,
                "SchemaVersion", "OwnerUserId", "MovementDate", "MovementType", "Lines", "SavedAtUtc");
        }
        else
        {
            RequireExactProperties(root,
                "SchemaVersion", "OwnerUserId", "Revision", "State", "ClientOperationId",
                "MovementDate", "MovementType", "Lines", "SavedAtUtc");
        }

        var ownerUserId = RequiredInt32(root, "OwnerUserId");
        if (ownerUserId <= 0)
            throw new InvalidDataException("OwnerUserId must identify a persisted user.");
        if (ownerUserId != expectedOwnerUserId)
            throw new InvalidDataException("Draft ownership does not match the authenticated account.");

        var savedAtUtc = RequiredDateTimeOffset(root, "SavedAtUtc");

        if (schemaVersion == 3)
        {
            var legacyDate = RequiredDateOnly(root, "MovementDate");
            var legacyType = RequiredMovementType(root, "MovementType");
            var legacyLines = RequiredLines(root);
            return new ParsedDraft(
                ownerUserId,
                0,
                true,
                Guid.NewGuid(),
                legacyDate,
                legacyType,
                legacyLines,
                savedAtUtc,
                false);
        }

        var revision = RequiredInt64(root, "Revision");
        if (revision <= 0)
            throw new InvalidDataException("Revision must be positive.");

        var state = RequiredString(root, "State");
        var clientOperationText = RequiredString(root, "ClientOperationId");
        if (!Guid.TryParseExact(clientOperationText, "D", out var clientOperationId) ||
            clientOperationId == Guid.Empty)
        {
            throw new InvalidDataException("ClientOperationId is invalid.");
        }

        if (string.Equals(state, "Cleared", StringComparison.Ordinal))
        {
            RequireNull(root, "MovementDate");
            RequireNull(root, "MovementType");
            RequireNull(root, "Lines");
            return new ParsedDraft(
                ownerUserId,
                revision,
                false,
                clientOperationId,
                default,
                default,
                [],
                savedAtUtc,
                true);
        }

        if (!string.Equals(state, "Active", StringComparison.Ordinal))
            throw new InvalidDataException("Draft state is invalid.");

        var movementDate = RequiredDateOnly(root, "MovementDate");
        var movementType = RequiredMovementType(root, "MovementType");
        var lines = RequiredLines(root);
        return new ParsedDraft(
            ownerUserId,
            revision,
            true,
            clientOperationId,
            movementDate,
            movementType,
            lines,
            savedAtUtc,
            true);
    }

    private static DateOnly RequiredDateOnly(JsonElement root, string name)
    {
        var movementDateText = RequiredString(root, name);
        if (!DateOnly.TryParseExact(
                movementDateText,
                "yyyy-MM-dd",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var movementDate))
        {
            throw new InvalidDataException("MovementDate is invalid.");
        }
        return movementDate;
    }

    private static MovementType RequiredMovementType(JsonElement root, string name)
    {
        var movementTypeValue = RequiredInt32(root, name);
        var movementType = (MovementType)movementTypeValue;
        if (!Enum.IsDefined(movementType))
            throw new InvalidDataException("MovementType is invalid.");
        return movementType;
    }

    private static DateTimeOffset RequiredDateTimeOffset(JsonElement root, string name)
    {
        var savedAtText = RequiredString(root, name);
        if (!DateTimeOffset.TryParse(
                savedAtText,
                CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind,
                out var savedAtUtc))
        {
            throw new InvalidDataException("SavedAtUtc is invalid.");
        }
        return savedAtUtc;
    }

    private static IReadOnlyList<DraftMovementLine> RequiredLines(JsonElement root)
    {
        var linesElement = RequiredProperty(root, "Lines");
        if (linesElement.ValueKind != JsonValueKind.Array)
            throw new InvalidDataException("Lines must be an array.");

        var lines = new List<DraftMovementLine>();
        foreach (var item in linesElement.EnumerateArray())
        {
            if (lines.Count == MaximumLineCount)
                throw new InvalidDataException(
                    $"The recovery draft exceeds the {MaximumLineCount:N0}-line safety limit.");

            RequireObject(item, "draft line");
            RequireExactProperties(item,
                "CustomerId", "CustomerCode", "CustomerName", "ContainerTypeId",
                "ContainerType", "Quantity", "Reference", "Notes");
            lines.Add(new DraftMovementLine(
                RequiredPositiveInt32(item, "CustomerId"),
                RequiredBoundedString(item, "CustomerCode", MasterDataLimits.CustomerCode),
                RequiredBoundedString(item, "CustomerName", MasterDataLimits.CustomerName),
                RequiredPositiveInt32(item, "ContainerTypeId"),
                RequiredBoundedString(item, "ContainerType", MasterDataLimits.ContainerName),
                RequiredPositiveInt32(item, "Quantity"),
                OptionalString(item, "Reference"),
                OptionalString(item, "Notes")));
        }

        ValidateLines(lines);
        return lines;
    }

    private static void ValidateLines(IReadOnlyList<DraftMovementLine> lines)
    {
        if (lines.Count is 0 or > MaximumLineCount)
            throw new InvalidDataException(
                $"A recovery draft must contain between 1 and {MaximumLineCount:N0} lines.");

        foreach (var line in lines)
        {
            if (line is null)
                throw new InvalidDataException("A recovery draft line cannot be null.");
            if (line.CustomerId <= 0 || line.ContainerTypeId <= 0 || line.Quantity <= 0)
                throw new InvalidDataException("Draft line identities and quantities must be positive.");
            RequireBoundedText(line.CustomerCode, MasterDataLimits.CustomerCode, "CustomerCode");
            RequireBoundedText(line.CustomerName, MasterDataLimits.CustomerName, "CustomerName");
            RequireBoundedText(line.ContainerType, MasterDataLimits.ContainerName, "ContainerType");
        }

        _ = QuantityMath.Sum(lines.Select(line => line.Quantity));
    }

    private static void RequireBoundedText(string? value, int maximum, string name)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length > maximum)
            throw new InvalidDataException($"{name} is missing or exceeds its authoritative limit.");
    }

    private static void RequireObject(JsonElement element, string name)
    {
        if (element.ValueKind != JsonValueKind.Object)
            throw new InvalidDataException($"The {name} must be a JSON object.");
    }

    private static void RequireExactProperties(JsonElement element, params string[] names)
    {
        var allowed = names.ToHashSet(StringComparer.Ordinal);
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var property in element.EnumerateObject())
        {
            if (!allowed.Contains(property.Name))
                throw new InvalidDataException($"Unexpected draft field '{property.Name}'.");
            if (!seen.Add(property.Name))
                throw new InvalidDataException($"Duplicate draft field '{property.Name}'.");
        }

        var missing = names.FirstOrDefault(name => !seen.Contains(name));
        if (missing is not null)
            throw new InvalidDataException($"Required draft field '{missing}' is missing.");
    }

    private static JsonElement RequiredProperty(JsonElement element, string name) =>
        element.TryGetProperty(name, out var value)
            ? value
            : throw new InvalidDataException($"Required draft field '{name}' is missing.");

    private static int RequiredInt32(JsonElement element, string name)
    {
        var value = RequiredProperty(element, name);
        if (value.ValueKind != JsonValueKind.Number || !value.TryGetInt32(out var result))
            throw new InvalidDataException($"Draft field '{name}' must be a 32-bit integer.");
        return result;
    }

    private static long RequiredInt64(JsonElement element, string name)
    {
        var value = RequiredProperty(element, name);
        if (value.ValueKind != JsonValueKind.Number || !value.TryGetInt64(out var result))
            throw new InvalidDataException($"Draft field '{name}' must be a 64-bit integer.");
        return result;
    }

    private static int RequiredPositiveInt32(JsonElement element, string name)
    {
        var value = RequiredInt32(element, name);
        if (value <= 0)
            throw new InvalidDataException($"Draft field '{name}' must be positive.");
        return value;
    }

    private static string RequiredString(JsonElement element, string name)
    {
        var value = RequiredProperty(element, name);
        if (value.ValueKind != JsonValueKind.String)
            throw new InvalidDataException($"Draft field '{name}' must be a string.");
        return value.GetString()
            ?? throw new InvalidDataException($"Draft field '{name}' cannot be null.");
    }

    private static string RequiredBoundedString(JsonElement element, string name, int maximum)
    {
        var value = RequiredString(element, name);
        RequireBoundedText(value, maximum, name);
        return value;
    }

    private static string? OptionalString(JsonElement element, string name)
    {
        var value = RequiredProperty(element, name);
        return value.ValueKind switch
        {
            JsonValueKind.Null => null,
            JsonValueKind.String => value.GetString(),
            _ => throw new InvalidDataException($"Draft field '{name}' must be text or null.")
        };
    }

    private static void RequireNull(JsonElement element, string name)
    {
        if (RequiredProperty(element, name).ValueKind != JsonValueKind.Null)
            throw new InvalidDataException($"Cleared draft field '{name}' must be null.");
    }

    private static void RequireOwnerId(int ownerUserId)
    {
        if (ownerUserId <= 0)
            throw new ArgumentOutOfRangeException(
                nameof(ownerUserId),
                ownerUserId,
                "A positive persisted user ID is required for draft ownership.");
    }

    private static string? CombineDiagnostics(string? first, string? second) =>
        string.IsNullOrWhiteSpace(first) ? second :
        string.IsNullOrWhiteSpace(second) ? first :
        first + Environment.NewLine + second;

    private static void TryDeleteOwnTemporaryFile(string path)
    {
        try
        {
            if (File.Exists(path))
                File.Delete(path);
        }
        catch (IOException)
        {
            // The uniquely named artifact cannot collide with another writer and
            // is never considered a published draft. A later cleanup may remove it.
        }
        catch (UnauthorizedAccessException)
        {
            // The uniquely named artifact cannot collide with another writer and
            // is never considered a published draft. A later cleanup may remove it.
        }
    }

    private static void RequireExpectedRevision(
        string ownerPath,
        int ownerUserId,
        long expectedRevision)
    {
        var actualRevision = File.Exists(ownerPath)
            ? ParseAndValidate(ReadBounded(ownerPath), ownerUserId).Revision
            : 0;
        if (actualRevision != expectedRevision)
        {
            throw new InvalidOperationException(
                "The Batch Entry draft changed in another operation. Reload the owned draft before retrying.");
        }
    }

    private static FileStream AcquirePublicationLease(string ownerPath)
    {
        var leasePath = ownerPath + ".publish-lock";
        Exception? lastFailure = null;
        for (var attempt = 0; attempt < 200; attempt++)
        {
            try
            {
                return new FileStream(
                    leasePath,
                    FileMode.OpenOrCreate,
                    FileAccess.ReadWrite,
                    FileShare.None,
                    bufferSize: 1,
                    FileOptions.DeleteOnClose);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                lastFailure = ex;
                Thread.Sleep(10);
            }
        }

        throw new IOException(
            "The unfinished batch could not obtain exclusive atomic-publication ownership.",
            lastFailure);
    }
}
