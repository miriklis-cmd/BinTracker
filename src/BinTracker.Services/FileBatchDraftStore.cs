using System.Text.Json;
using BinTracker.Core;

namespace BinTracker.Services;

/// <summary>
/// Persists the unsaved Batch Entry draft outside the SQLite database so a
/// process crash or power loss does not discard operator-entered pending lines.
/// The saved file contains draft movement metadata only; no authentication
/// credentials or database connection information are written here.
/// </summary>
public sealed class FileBatchDraftStore : IBatchDraftStore
{
    private sealed record StoredDraft(
        int SchemaVersion,
        DateOnly MovementDate,
        MovementType MovementType,
        List<DraftMovementLine> Lines,
        DateTimeOffset? SavedAtUtc = null);

    private const int CurrentSchemaVersion = 2;
    private readonly string filePath;
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

    public FileBatchDraftStore(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        this.filePath = filePath;
    }

    public DraftMovementBatchSnapshot? Load()
    {
        if (!File.Exists(filePath))
            return null;

        try
        {
            var json = File.ReadAllText(filePath);
            var stored = JsonSerializer.Deserialize<StoredDraft>(json, jsonOptions);

            if (stored is null ||
                (stored.SchemaVersion != 1 && stored.SchemaVersion != CurrentSchemaVersion) ||
                stored.Lines.Count == 0)
            {
                Clear();
                return null;
            }

            var savedAtUtc = stored.SavedAtUtc ?? new DateTimeOffset(File.GetLastWriteTimeUtc(filePath));

            return new DraftMovementBatchSnapshot(
                stored.MovementDate,
                stored.MovementType,
                stored.Lines,
                savedAtUtc);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            QuarantineUnreadableDraft();
            return null;
        }
    }

    public void Save(DraftMovementBatch draft)
    {
        ArgumentNullException.ThrowIfNull(draft);

        if (!draft.HasLines)
        {
            Clear();
            return;
        }

        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrWhiteSpace(directory))
            Directory.CreateDirectory(directory);

        var stored = new StoredDraft(
            CurrentSchemaVersion,
            draft.MovementDate,
            draft.MovementType,
            draft.Lines.ToList(),
            DateTimeOffset.UtcNow);

        var json = JsonSerializer.Serialize(stored, jsonOptions);
        var temporaryPath = filePath + ".tmp";

        File.WriteAllText(temporaryPath, json);
        File.Move(temporaryPath, filePath, overwrite: true);
    }

    public void Clear()
    {
        if (File.Exists(filePath))
            File.Delete(filePath);

        var temporaryPath = filePath + ".tmp";
        if (File.Exists(temporaryPath))
            File.Delete(temporaryPath);
    }

    private void QuarantineUnreadableDraft()
    {
        try
        {
            var quarantinePath =
                filePath + $".corrupt-{DateTime.Now:yyyyMMddHHmmss}";
            File.Move(filePath, quarantinePath, overwrite: true);
        }
        catch
        {
            // Recovery must never prevent BinTracker from starting. If the bad
            // file cannot be moved, leave it in place and start with no draft.
        }
    }
}
