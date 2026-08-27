using System.Security.Cryptography;
using System.Text.Json;
using BinTracker.Core;
using BinTracker.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace BinTracker.Services;

public sealed record BalanceRow(int CustomerId, string CustomerName, int ContainerTypeId, string ContainerTypeName, int Balance)
{
    public bool IsOutstanding => Balance > 0;
    public bool IsCredit => Balance < 0;
}

public sealed class UserSession(IBusinessClock clock) : IUserContext
{
    public string SessionId { get; } = Guid.NewGuid().ToString("N");
    public int? UserId { get; private set; }
    public string Username { get; private set; } = "anonymous";
    public string DisplayName { get; private set; } = "Not signed in";
    public UserRole Role { get; private set; } = UserRole.Viewer;
    public bool MustChangePassword { get; private set; }
    public DateTime? LoginUtc { get; private set; }
    public bool IsAuthenticated => UserId.HasValue;

    public void SignIn(UserAccount user)
    {
        UserId = user.Id;
        Username = user.Username;
        DisplayName = user.DisplayName;
        Role = user.Role;
        MustChangePassword = user.MustChangePassword;
        LoginUtc = clock.UtcNow;
    }

    public void PasswordChanged() => MustChangePassword = false;
}

public interface IAuditService
{
    Task WriteAsync(string action, string entityType, string? entityId, string description,
        bool succeeded = true, object? before = null, object? after = null,
        int? userIdOverride = null, string? usernameOverride = null,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AuditEvent>> GetRecentAsync(int limit = 500, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AuditTrailRow>> GetAuditTrailAsync(AuditReviewFilter filter = AuditReviewFilter.All,
        int limit = 500, CancellationToken cancellationToken = default);
    Task<AdministratorReviewState> GetAdministratorReviewStateAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AuditEvent>> GetUnreviewedMovementChangesAsync(CancellationToken cancellationToken = default);
    Task MarkMovementChangesReviewedAsync(IReadOnlyCollection<long> auditEventIds, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MovementBatchAuditLine>> GetMovementBatchDetailAsync(int batchId, CancellationToken cancellationToken = default);
    Task<MovementChangeAuditDetail?> GetMovementChangeDetailAsync(long auditEventId, CancellationToken cancellationToken = default);
    event EventHandler<AdministratorReviewState>? AdministratorReviewStateChanged;
}

public enum AuditReviewFilter { All, NeedsReview, Reviewed }

public sealed record AdministratorReviewState(int PendingCount);

public sealed record AuditTrailRow(long Id, DateTime TimestampUtc, string Username, string Action,
    string EntityType, string? EntityId, string Description, bool Succeeded,
    bool RequiresAdministratorReview, DateTime? ReviewedUtc, int? ReviewedByUserId,
    string? ReviewedByUsername)
{
    public string ActionDisplay => AuditActionDisplay.Label(Action);
    public string ReviewState => AuditReviewPolicy.StateText(RequiresAdministratorReview, ReviewedUtc);
    public string ReviewedBy => ReviewedUtc.HasValue
        ? $"{ReviewedByUsername ?? "Administrator"} · {ReviewedUtc.Value:yyyy-MM-dd HH:mm} UTC"
        : string.Empty;
    public bool CanMarkReviewed => AuditReviewPolicy.CanMarkReviewed(this);
    public bool HasAuthoritativeBatchDetail =>
        EntityType == "MovementBatch" && int.TryParse(EntityId, out _);
    public long? ReferencedMovementChangeAuditEventId =>
        AuditReviewPolicy.TryGetAcknowledgedAuditEventId(Action, EntityType, EntityId, out var id) ? id : null;
    public bool HasMovementChangeDetail =>
        AuditReviewPolicy.IsMovementChangeAction(Action) || ReferencedMovementChangeAuditEventId.HasValue;
}

public static class AuditActionDisplay
{
    public static string Label(string action) => action switch
    {
        "MOVEMENT_REVERSED" => "Movement reversed",
        "MOVEMENT_CORRECTED" => "Movement corrected",
        "MOVEMENT_BATCH_CORRECTED" => "Batch corrected",
        "MOVEMENT_CHANGE_REVIEWED" => "Movement change reviewed",
        "LOGIN_SUCCESS" => "Login succeeded",
        "LOGIN_FAILED" => "Login failed",
        "LOGOUT" => "Logout",
        _ => string.Join(' ', action.Split('_', StringSplitOptions.RemoveEmptyEntries)
            .Select((word, index) => index == 0
                ? char.ToUpperInvariant(word[0]) + word[1..].ToLowerInvariant()
                : word.ToLowerInvariant()))
    };
}

public static class AuditReviewPolicy
{
    private static readonly HashSet<string> MovementChangeActions = new(StringComparer.Ordinal)
    {
        "MOVEMENT_REVERSED", "MOVEMENT_CORRECTED", "MOVEMENT_BATCH_CORRECTED"
    };

    public static bool IsMovementChangeAction(string action) => MovementChangeActions.Contains(action);
    public static bool TryGetAcknowledgedAuditEventId(string action, string entityType, string? entityId, out long id)
    {
        id = 0;
        return action == "MOVEMENT_CHANGE_REVIEWED" && entityType == "AuditEvent" &&
            long.TryParse(entityId, out id) && id > 0;
    }
    public static string StateText(bool required, DateTime? reviewedUtc) =>
        !required ? string.Empty : reviewedUtc.HasValue ? "Reviewed" : "Needs review";
    public static bool CanMarkReviewed(AuditTrailRow? row) => row is not null && row.Succeeded &&
        row.RequiresAdministratorReview && !row.ReviewedUtc.HasValue && IsMovementChangeAction(row.Action);
    public static bool Matches(AuditTrailRow row, AuditReviewFilter filter) => filter switch
    {
        AuditReviewFilter.NeedsReview => row.RequiresAdministratorReview && !row.ReviewedUtc.HasValue,
        AuditReviewFilter.Reviewed => row.RequiresAdministratorReview && row.ReviewedUtc.HasValue,
        _ => true
    };
    public static AuditTrailRow? SelectOldestPending(IEnumerable<AuditTrailRow> rows) => rows
        .Where(CanMarkReviewed).OrderBy(x => x.TimestampUtc).ThenBy(x => x.Id).FirstOrDefault();
}

public sealed record MovementBatchAuditLine(long MovementId, int BatchId, DateOnly MovementDate,
    string CustomerCode, string CustomerName, string ContainerType, MovementType Direction,
    int Quantity, string Reference, string Notes);

public sealed record MovementChangeAuditLine(string Role, long MovementId, int? BatchId,
    DateOnly MovementDate, string CustomerCode, string CustomerName, string ContainerType,
    MovementType Direction, int Quantity, string Reference, string Notes, long? LinkedMovementId)
{
    public string ReferenceAndNotes => string.Join(" · ", new[] { Reference, Notes }.Where(x => !string.IsNullOrWhiteSpace(x)));
}

public sealed record MovementChangeAuditDetail(long AuditEventId, string Action, string Actor,
    DateTime ChangedUtc, string Reason, int? OriginalBatchId, int? ReplacementBatchId,
    IReadOnlyList<MovementChangeAuditLine> Lines, bool OpenedFromReviewAcknowledgement = false,
    string? ReviewedBy = null, DateTime? ReviewedUtc = null);

public sealed record MovementChangeDifference(string Field, string OriginalValue, string CorrectedValue)
{
    public string Display => $"{Field}: {OriginalValue} → {CorrectedValue}";
}

/// <summary>Builds presentation-independent, field-accurate before/after correction differences.</summary>
public static class MovementChangeComparison
{
    public static IReadOnlyList<MovementChangeDifference> Compare(IReadOnlyList<MovementChangeAuditLine> lines)
    {
        var originals = lines.Where(x => x.Role == "Original").OrderBy(x => x.MovementId).ToArray();
        var replacements = lines.Where(x => x.Role == "Corrected replacement").OrderBy(x => x.MovementId).ToArray();
        if (originals.Length == 0 || originals.Length != replacements.Length) return [];
        if (originals.Length > 1) return CompareBatch(originals, replacements);
        var before = originals[0]; var after = replacements[0];
        var differences = new List<MovementChangeDifference>();
        Add(differences, "Date", before.MovementDate.ToString("dd/MM/yyyy"), after.MovementDate.ToString("dd/MM/yyyy"));
        Add(differences, "Customer", Customer(before), Customer(after));
        Add(differences, "Container", before.ContainerType, after.ContainerType);
        Add(differences, "Direction", before.Direction.ToString().ToUpperInvariant(), after.Direction.ToString().ToUpperInvariant());
        Add(differences, "Quantity", before.Quantity.ToString("N0"), after.Quantity.ToString("N0"));
        Add(differences, "Reference", Text(before.Reference), Text(after.Reference));
        Add(differences, "Notes", Text(before.Notes), Text(after.Notes));
        return differences;
    }

    public static string Describe(IReadOnlyList<MovementChangeAuditLine> lines)
    {
        var differences = Compare(lines);
        if (differences.Count > 0) return string.Join(Environment.NewLine, differences.Select(x => x.Display));
        return lines.Any(x => x.Role.Contains("reversal", StringComparison.OrdinalIgnoreCase))
            ? "Reversal neutralises the selected original movement."
            : "No corrected field difference could be resolved.";
    }

    private static IReadOnlyList<MovementChangeDifference> CompareBatch(
        IReadOnlyList<MovementChangeAuditLine> originals, IReadOnlyList<MovementChangeAuditLine> replacements)
    {
        var result = new List<MovementChangeDifference>();
        AddDistinct(result, "Date", originals.Select(x => x.MovementDate.ToString("dd/MM/yyyy")), replacements.Select(x => x.MovementDate.ToString("dd/MM/yyyy")));
        AddDistinct(result, "Direction", originals.Select(x => x.Direction.ToString().ToUpperInvariant()), replacements.Select(x => x.Direction.ToString().ToUpperInvariant()));
        return result;
    }

    private static void AddDistinct(List<MovementChangeDifference> result, string field,
        IEnumerable<string> originals, IEnumerable<string> replacements)
    {
        var before = originals.Distinct().Order().ToArray(); var after = replacements.Distinct().Order().ToArray();
        Add(result, field, string.Join(", ", before), string.Join(", ", after));
    }
    private static void Add(List<MovementChangeDifference> result, string field, string before, string after)
    { if (!string.Equals(before, after, StringComparison.Ordinal)) result.Add(new(field, before, after)); }
    private static string Customer(MovementChangeAuditLine line) => string.IsNullOrWhiteSpace(line.CustomerCode)
        ? line.CustomerName : string.IsNullOrWhiteSpace(line.CustomerName) ? line.CustomerCode : $"{line.CustomerCode} — {line.CustomerName}";
    private static string Text(string value) => string.IsNullOrWhiteSpace(value) ? "(blank)" : value;
}

internal sealed class AuditService(
    IDbContextFactory<BinTrackerDbContext> factory,
    IUserContext session,
    IBusinessClock clock,
    IClientContext client) : IAuditService
{
    public event EventHandler<AdministratorReviewState>? AdministratorReviewStateChanged;
    public async Task WriteAsync(string action, string entityType, string? entityId, string description,
        bool succeeded = true, object? before = null, object? after = null,
        int? userIdOverride = null, string? usernameOverride = null,
        CancellationToken cancellationToken = default)
    {
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        db.AuditEvents.Add(new AuditEvent
        {
            TimestampUtc = clock.UtcNow,
            UserId = userIdOverride ?? session.UserId,
            Username = usernameOverride ?? session.Username,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            Description = description,
            BeforeValues = before is null ? null : JsonSerializer.Serialize(before),
            AfterValues = after is null ? null : JsonSerializer.Serialize(after),
            ComputerName = client.DeviceName,
            SessionId = session.SessionId,
            Succeeded = succeeded
        });
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AuditEvent>> GetRecentAsync(int limit = 500, CancellationToken cancellationToken = default)
    {
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        return await db.AuditEvents.AsNoTracking()
            .OrderByDescending(x => x.TimestampUtc)
            .Take(Math.Clamp(limit, 1, 5000))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AuditTrailRow>> GetAuditTrailAsync(AuditReviewFilter filter = AuditReviewFilter.All,
        int limit = 500, CancellationToken cancellationToken = default)
    {
        if (session.Role != UserRole.Administrator) throw new UnauthorizedAccessException("Administrator access is required.");
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        var query = db.AuditEvents.AsNoTracking();
        query = filter switch
        {
            AuditReviewFilter.NeedsReview => query.Where(x => x.RequiresAdministratorReview && x.ReviewedUtc == null),
            AuditReviewFilter.Reviewed => query.Where(x => x.RequiresAdministratorReview && x.ReviewedUtc != null),
            _ => query
        };
        return await query.OrderByDescending(x => x.TimestampUtc).ThenByDescending(x => x.Id)
            .Take(Math.Clamp(limit, 1, 5000))
            .Select(x => new AuditTrailRow(x.Id, x.TimestampUtc, x.Username, x.Action, x.EntityType,
                x.EntityId, x.Description, x.Succeeded, x.RequiresAdministratorReview, x.ReviewedUtc,
                x.ReviewedByUserId, x.ReviewedByUsername))
            .ToListAsync(cancellationToken);
    }

    public async Task<AdministratorReviewState> GetAdministratorReviewStateAsync(CancellationToken cancellationToken = default)
    {
        if (session.Role != UserRole.Administrator) return new(0);
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        return new(await db.AuditEvents.CountAsync(
            x => x.RequiresAdministratorReview && x.ReviewedUtc == null, cancellationToken));
    }

    public async Task<IReadOnlyList<AuditEvent>> GetUnreviewedMovementChangesAsync(CancellationToken cancellationToken = default)
    {
        if (session.Role != UserRole.Administrator) throw new UnauthorizedAccessException("Administrator access is required.");
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        return await db.AuditEvents.AsNoTracking()
            .Where(x => x.RequiresAdministratorReview && x.ReviewedUtc == null)
            .OrderBy(x => x.TimestampUtc).ToListAsync(cancellationToken);
    }

    public async Task MarkMovementChangesReviewedAsync(IReadOnlyCollection<long> auditEventIds, CancellationToken cancellationToken = default)
    {
        if (session.Role != UserRole.Administrator || !session.UserId.HasValue)
            throw new UnauthorizedAccessException("Administrator access is required.");
        if (auditEventIds.Count == 0) return;
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        await using var tx = await db.Database.BeginTransactionAsync(cancellationToken);
        var events = await db.AuditEvents.Where(x => auditEventIds.Contains(x.Id) &&
            x.RequiresAdministratorReview && x.ReviewedUtc == null).ToListAsync(cancellationToken);
        if (events.Count != auditEventIds.Distinct().Count())
        {
            await tx.RollbackAsync(cancellationToken);
            throw new InvalidOperationException("One or more selected movement-change events are not eligible for review or were already reviewed.");
        }
        var reviewedAt = clock.UtcNow;
        foreach (var item in events)
        {
            item.ReviewedUtc = reviewedAt; item.ReviewedByUserId = session.UserId;
            item.ReviewedByUsername = session.Username;
        }
        db.AuditEvents.Add(new AuditEvent { TimestampUtc = reviewedAt, UserId = session.UserId,
            Username = session.Username, Action = "MOVEMENT_CHANGE_REVIEWED", EntityType = "AuditEvent",
            EntityId = string.Join(",", events.Select(x => x.Id)),
            Description = string.Join(" ", events.OrderBy(x => x.Id).Select(x =>
                $"Administrator {session.Username} reviewed {AuditActionDisplay.Label(x.Action).ToLowerInvariant()} " +
                $"#{x.EntityId} performed by {x.Username} (audit event #{x.Id}).")),
            BeforeValues = JsonSerializer.Serialize(events.Select(x => new { x.Id, x.Action, x.Username })),
            AfterValues = JsonSerializer.Serialize(new { ReviewedAt = reviewedAt, ReviewedBy = session.Username }),
            ComputerName = client.DeviceName, SessionId = session.SessionId, Succeeded = true });
        await db.SaveChangesAsync(cancellationToken); await tx.CommitAsync(cancellationToken);
        AdministratorReviewStateChanged?.Invoke(this, await GetAdministratorReviewStateAsync(cancellationToken));
    }

    public async Task<IReadOnlyList<MovementBatchAuditLine>> GetMovementBatchDetailAsync(int batchId, CancellationToken cancellationToken = default)
    {
        if (session.Role != UserRole.Administrator) throw new UnauthorizedAccessException("Administrator access is required.");
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        return await db.BinMovements.AsNoTracking().Where(x => x.MovementBatchId == batchId)
            .OrderBy(x => x.Id).Select(x => new MovementBatchAuditLine(x.Id, batchId, x.MovementDate,
                x.Customer.CustomerCode ?? "", x.Customer.Name, x.ContainerType.Name, x.MovementType,
                x.Quantity, x.ReferenceNumber ?? "", x.Notes ?? "")).ToListAsync(cancellationToken);
    }

    public async Task<MovementChangeAuditDetail?> GetMovementChangeDetailAsync(long auditEventId, CancellationToken cancellationToken = default)
    {
        if (session.Role != UserRole.Administrator) throw new UnauthorizedAccessException("Administrator access is required.");
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        var requestedEvent = await db.AuditEvents.AsNoTracking().SingleOrDefaultAsync(x => x.Id == auditEventId, cancellationToken);
        if (requestedEvent is null) return null;
        var openedFromAcknowledgement = AuditReviewPolicy.TryGetAcknowledgedAuditEventId(
            requestedEvent.Action, requestedEvent.EntityType, requestedEvent.EntityId, out var referencedId);
        var auditEvent = openedFromAcknowledgement
            ? await db.AuditEvents.AsNoTracking().SingleOrDefaultAsync(x => x.Id == referencedId, cancellationToken)
            : requestedEvent;
        if (auditEvent is null || !AuditReviewPolicy.IsMovementChangeAction(auditEvent.Action)) return null;
        if (openedFromAcknowledgement && (!auditEvent.ReviewedUtc.HasValue ||
            !string.Equals(auditEvent.ReviewedByUsername, requestedEvent.Username, StringComparison.Ordinal))) return null;

        if (auditEvent.Action == "MOVEMENT_REVERSED")
        {
            if (!long.TryParse(auditEvent.EntityId, out var originalId)) return null;
            var rows = await db.BinMovements.AsNoTracking()
                .Where(x => x.Id == originalId || x.ReversesMovementId == originalId)
                .OrderBy(x => x.Id).Select(x => new { Movement = x, x.Customer.CustomerCode,
                    CustomerName = x.Customer.Name, Container = x.ContainerType.Name }).ToListAsync(cancellationToken);
            if (rows.Count != 2 || rows.Count(x => x.Movement.Id == originalId) != 1 ||
                rows.Count(x => x.Movement.ReversesMovementId == originalId) != 1) return null;
            return new(auditEvent.Id, auditEvent.Action, auditEvent.Username, auditEvent.TimestampUtc,
                rows.Single(x => x.Movement.ReversesMovementId == originalId).Movement.CorrectionReason ?? "",
                rows.Single(x => x.Movement.Id == originalId).Movement.MovementBatchId, null,
                rows.Select(x => ToLine(x.Movement.Id == originalId ? "Original" : "Neutralising reversal",
                    x.Movement, x.CustomerCode ?? "", x.CustomerName, x.Container,
                    x.Movement.Id == originalId ? rows.Single(y => y.Movement.ReversesMovementId == originalId).Movement.Id : originalId)).ToArray(),
                openedFromAcknowledgement, auditEvent.ReviewedByUsername, auditEvent.ReviewedUtc);
        }

        var parsed = ParseCorrectionLineage(auditEvent.AfterValues);
        if (parsed.Length == 0) return null;
        var originalIds = parsed.Select(x => x.Original).ToList();
        var neutralIds = parsed.Select(x => x.Neutral).ToList();
        var replacementIds = parsed.Select(x => x.Replacement).ToList();
        var allIds = originalIds.Concat(neutralIds).Concat(replacementIds).Distinct().ToList();
        if (allIds.Count != parsed.Length * 3) return null;
        var movements = await db.BinMovements.AsNoTracking().Where(x => allIds.Contains(x.Id))
            .Select(x => new { Movement = x, x.Customer.CustomerCode, CustomerName = x.Customer.Name,
                Container = x.ContainerType.Name }).ToListAsync(cancellationToken);
        if (movements.Count != allIds.Count) return null;
        var operations = await db.MovementCorrectionOperations.AsNoTracking().Include(x => x.Lines)
            .Where(x => x.Lines.Any(l => originalIds.Contains(l.OriginalMovementId))).ToListAsync(cancellationToken);
        var matching = operations.Where(op => op.Lines.Count == parsed.Length && parsed.All(p => op.Lines.Any(l =>
            l.OriginalMovementId == p.Original && l.NeutralisingMovementId == p.Neutral && l.ReplacementMovementId == p.Replacement))).ToArray();
        if (matching.Length != 1) return null;
        var operation = matching[0];
        var lines = new List<MovementChangeAuditLine>();
        foreach (var item in parsed)
        {
            var original = movements.Single(x => x.Movement.Id == item.Original);
            var neutral = movements.Single(x => x.Movement.Id == item.Neutral);
            var replacement = movements.Single(x => x.Movement.Id == item.Replacement);
            lines.Add(ToLine("Original", original.Movement, original.CustomerCode ?? "", original.CustomerName, original.Container, neutral.Movement.Id));
            lines.Add(ToLine("Neutraliser", neutral.Movement, neutral.CustomerCode ?? "", neutral.CustomerName, neutral.Container, original.Movement.Id));
            lines.Add(ToLine("Corrected replacement", replacement.Movement, replacement.CustomerCode ?? "", replacement.CustomerName, replacement.Container, original.Movement.Id));
        }
        return new(auditEvent.Id, auditEvent.Action, operation.ActorUsername, operation.CreatedUtc,
            operation.Reason, operation.OriginalBatchId, operation.ReplacementBatchId, lines,
            openedFromAcknowledgement, auditEvent.ReviewedByUsername, auditEvent.ReviewedUtc);
    }

    private static MovementChangeAuditLine ToLine(string role, BinMovement movement, string code,
        string customer, string container, long? linkedId) => new(role, movement.Id, movement.MovementBatchId,
        movement.MovementDate, code, customer, container, movement.MovementType, movement.Quantity,
        movement.ReferenceNumber ?? "", movement.Notes ?? "", linkedId);

    private static (long Original, long Neutral, long Replacement)[] ParseCorrectionLineage(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return [];
        try
        {
            using var document = JsonDocument.Parse(json);
            if (document.RootElement.ValueKind != JsonValueKind.Array) return [];
            return document.RootElement.EnumerateArray().Select(x => (
                x.GetProperty("Id").GetInt64(),
                x.GetProperty("NeutralisingMovementId").GetInt64(),
                x.GetProperty("ReplacementMovementId").GetInt64())).ToArray();
        }
        catch (JsonException) { return []; }
        catch (InvalidOperationException) { return []; }
        catch (KeyNotFoundException) { return []; }
    }
}

public interface IAuthenticationService
{
    Task<bool> HasUsersAsync(CancellationToken cancellationToken = default);
    Task CreateInitialAdministratorAsync(string username, string displayName, string password, CancellationToken cancellationToken = default);
    Task<bool> LoginAsync(string username, string password, CancellationToken cancellationToken = default);
    Task ChangeOwnPasswordAsync(string currentPassword, string newPassword, CancellationToken cancellationToken = default);
    Task LogoutAsync(CancellationToken cancellationToken = default);
}

internal sealed class AuthenticationService(
    IDbContextFactory<BinTrackerDbContext> factory,
    UserSession session,
    IAuditService audit,
    IBusinessClock clock) : IAuthenticationService
{
    public async Task<bool> HasUsersAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        return await db.UserAccounts.AnyAsync(cancellationToken);
    }

    public async Task CreateInitialAdministratorAsync(string username, string displayName, string password, CancellationToken cancellationToken = default)
    {
        username = username.Trim();
        displayName = displayName.Trim();
        ValidateCredentials(username, displayName, password);

        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        if (await db.UserAccounts.AnyAsync(cancellationToken))
            throw new InvalidOperationException("The initial administrator has already been created.");

        var (hash, salt) = PasswordSecurity.Hash(password);
        var user = new UserAccount
        {
            Username = username,
            DisplayName = displayName,
            PasswordHash = hash,
            PasswordSalt = salt,
            Role = UserRole.Administrator,
            IsActive = true,
            CreatedUtc = clock.UtcNow
        };
        db.UserAccounts.Add(user);
        await db.SaveChangesAsync(cancellationToken);
        await audit.WriteAsync("USER_CREATED", "UserAccount", user.Id.ToString(),
            $"Initial administrator '{username}' created.", after: new { user.Username, user.DisplayName, user.Role },
            userIdOverride: user.Id, usernameOverride: user.Username, cancellationToken: cancellationToken);
    }

    public async Task<bool> LoginAsync(string username, string password, CancellationToken cancellationToken = default)
    {
        username = username.Trim();
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        var user = await db.UserAccounts.SingleOrDefaultAsync(x => x.Username == username, cancellationToken);

        if (user is not null && user.IsLocked)
        {
            await audit.WriteAsync(
                "LOGIN_BLOCKED_LOCKED",
                "Session",
                null,
                $"Login blocked because account '{username}' is locked.",
                false,
                userIdOverride: user.Id,
                usernameOverride: user.Username,
                cancellationToken: cancellationToken);

            throw new InvalidOperationException(
                "This account is locked after too many failed login attempts. Ask an administrator to unlock it.");
        }

        var valid = user is not null &&
                    user.IsActive &&
                    PasswordSecurity.Verify(password, user.PasswordHash, user.PasswordSalt);

        if (!valid)
        {
            if (user is not null && user.IsActive)
            {
                var settings = await db.ApplicationSettings.AsNoTracking()
                    .SingleOrDefaultAsync(x => x.Id == 1, cancellationToken);
                var maximum = Math.Max(1, settings?.MaxFailedLoginAttempts ?? 5);

                // Atomic set-based update prevents simultaneous remote login
                // attempts from losing failed-attempt increments.
                await db.UserAccounts
                    .Where(x => x.Id == user.Id && x.IsActive && !x.IsLocked)
                    .ExecuteUpdateAsync(
                        setters => setters.SetProperty(
                            x => x.FailedLoginCount,
                            x => x.FailedLoginCount + 1),
                        cancellationToken);

                await db.UserAccounts
                    .Where(x =>
                        x.Id == user.Id &&
                        !x.IsLocked &&
                        x.FailedLoginCount >= maximum)
                    .ExecuteUpdateAsync(
                        setters => setters
                            .SetProperty(x => x.IsLocked, true)
                            .SetProperty(x => x.LockedUtc, clock.UtcNow),
                        cancellationToken);

                user = await db.UserAccounts
                    .AsNoTracking()
                    .SingleAsync(x => x.Id == user.Id, cancellationToken);
            }

            await audit.WriteAsync(
                "LOGIN_FAILED",
                "Session",
                null,
                $"Failed login attempt for username '{username}'.",
                false,
                userIdOverride: user?.Id,
                usernameOverride: string.IsNullOrWhiteSpace(username) ? "unknown" : username,
                cancellationToken: cancellationToken);

            if (user?.IsLocked == true)
            {
                await audit.WriteAsync(
                    "ACCOUNT_LOCKED",
                    "UserAccount",
                    user.Id.ToString(),
                    $"User '{user.Username}' locked after repeated failed login attempts.",
                    false,
                    userIdOverride: user.Id,
                    usernameOverride: user.Username,
                    cancellationToken: cancellationToken);
            }

            return false;
        }

        var loginUtc = clock.UtcNow;
        var loginUpdated = await db.UserAccounts
            .Where(x => x.Id == user!.Id && x.IsActive && !x.IsLocked)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(x => x.LastLoginUtc, loginUtc)
                    .SetProperty(x => x.FailedLoginCount, 0)
                    .SetProperty(x => x.LockedUtc, (DateTime?)null),
                cancellationToken);

        if (loginUpdated != 1)
            throw new InvalidOperationException(
                "This account changed while you were signing in. Please try again.");

        user = await db.UserAccounts
            .AsNoTracking()
            .SingleAsync(x => x.Id == user!.Id, cancellationToken);

        session.SignIn(user);

        await audit.WriteAsync(
            "LOGIN_SUCCEEDED",
            "Session",
            session.SessionId,
            $"{user.DisplayName} logged in.",
            cancellationToken: cancellationToken);

        return true;
    }

    public async Task ChangeOwnPasswordAsync(
        string currentPassword,
        string newPassword,
        CancellationToken cancellationToken = default)
    {
        if (!session.UserId.HasValue)
            throw new UnauthorizedAccessException("You must be logged in to change your password.");

        PasswordPolicy.Validate(newPassword);

        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        var user = await db.UserAccounts.SingleAsync(x => x.Id == session.UserId.Value, cancellationToken);

        if (!PasswordSecurity.Verify(currentPassword, user.PasswordHash, user.PasswordSalt))
            throw new InvalidOperationException("The current password is incorrect.");

        if (PasswordSecurity.Verify(newPassword, user.PasswordHash, user.PasswordSalt))
            throw new InvalidOperationException("The new password must be different from the current password.");

        var (hash, salt) = PasswordSecurity.Hash(newPassword);
        var changed = await db.UserAccounts
            .Where(x =>
                x.Id == user.Id &&
                x.PasswordHash == user.PasswordHash &&
                x.PasswordSalt == user.PasswordSalt)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(x => x.PasswordHash, hash)
                    .SetProperty(x => x.PasswordSalt, salt)
                    .SetProperty(x => x.MustChangePassword, false)
                    .SetProperty(x => x.PasswordChangedUtc, clock.UtcNow)
                    .SetProperty(x => x.FailedLoginCount, 0)
                    .SetProperty(x => x.IsLocked, false)
                    .SetProperty(x => x.LockedUtc, (DateTime?)null),
                cancellationToken);

        if (changed != 1)
            throw new InvalidOperationException(
                "Your account credentials changed while this request was in progress. Sign in again and retry the password change.");

        session.PasswordChanged();

        await audit.WriteAsync(
            "PASSWORD_CHANGED",
            "UserAccount",
            user.Id.ToString(),
            $"User '{user.Username}' changed their password.",
            cancellationToken: cancellationToken);
    }

    public async Task LogoutAsync(CancellationToken cancellationToken = default)
    {
        if (session.IsAuthenticated)
            await audit.WriteAsync("LOGOUT", "Session", session.SessionId,
                $"{session.DisplayName} logged out.", cancellationToken: cancellationToken);
    }

    private static void ValidateCredentials(string username, string displayName, string password)
    {
        if (username.Length < 3) throw new ArgumentException("Username must be at least 3 characters.");
        if (displayName.Length < 2) throw new ArgumentException("Display name is required.");
        PasswordPolicy.Validate(password);
    }
}

public interface IUserService
{
    Task<IReadOnlyList<UserAccount>> GetUsersAsync(CancellationToken cancellationToken = default);
    Task CreateUserAsync(string username, string displayName, string password, UserRole role, CancellationToken cancellationToken = default);
    Task SetActiveAsync(int userId, bool active, CancellationToken cancellationToken = default);
    Task ResetPasswordAsync(int userId, string temporaryPassword, CancellationToken cancellationToken = default);
    Task SetLockedAsync(int userId, bool locked, CancellationToken cancellationToken = default);
    Task SetRoleAsync(int userId, UserRole role, CancellationToken cancellationToken = default);
}

internal sealed class UserService(
    IDbContextFactory<BinTrackerDbContext> factory,
    IUserContext session,
    IAuditService audit,
    IBusinessClock clock) : IUserService
{
    public async Task<IReadOnlyList<UserAccount>> GetUsersAsync(CancellationToken cancellationToken = default)
    {
        RequireAdmin();
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        return await db.UserAccounts.AsNoTracking().OrderBy(x => x.Username).ToListAsync(cancellationToken);
    }

    public async Task CreateUserAsync(string username, string displayName, string password, UserRole role, CancellationToken cancellationToken = default)
    {
        RequireAdmin();
        username = username.Trim(); displayName = displayName.Trim();
        if (username.Length < 3) throw new ArgumentException("Username must be at least 3 characters.");
        if (displayName.Length < 2) throw new ArgumentException("Display name is required.");
        PasswordPolicy.Validate(password);

        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        if (await db.UserAccounts.AnyAsync(x => x.Username == username, cancellationToken))
            throw new InvalidOperationException("That username already exists.");
        var (hash, salt) = PasswordSecurity.Hash(password);
        var user = new UserAccount
        {
            Username=username, DisplayName=displayName, PasswordHash=hash, PasswordSalt=salt,
            Role=role, IsActive=true, MustChangePassword=true, CreatedUtc=clock.UtcNow, CreatedByUserId=session.UserId
        };
        db.UserAccounts.Add(user);
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            await using var verify = await factory.CreateDbContextAsync(cancellationToken);
            if (await verify.UserAccounts.AsNoTracking()
                    .AnyAsync(x => x.Username == username, cancellationToken))
            {
                throw new InvalidOperationException("That username already exists.");
            }

            throw;
        }

        await audit.WriteAsync("USER_CREATED", "UserAccount", user.Id.ToString(),
            $"User '{username}' created with role {role}.", after: new { user.Username, user.DisplayName, user.Role }, cancellationToken:cancellationToken);
    }

    public async Task SetActiveAsync(int userId, bool active, CancellationToken cancellationToken = default)
    {
        RequireAdmin();
        if (session.UserId == userId && !active) throw new InvalidOperationException("You cannot deactivate your own account.");
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        var user = await db.UserAccounts.AsNoTracking()
            .SingleAsync(x => x.Id == userId, cancellationToken);
        var before = user.IsActive;

        await db.UserAccounts
            .Where(x => x.Id == userId)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(x => x.IsActive, active),
                cancellationToken);

        await audit.WriteAsync(active ? "USER_ACTIVATED" : "USER_DEACTIVATED", "UserAccount", user.Id.ToString(),
            $"User '{user.Username}' {(active ? "activated" : "deactivated")}.", before:new { IsActive=before }, after:new { IsActive=active }, cancellationToken:cancellationToken);
    }

    public async Task ResetPasswordAsync(
        int userId,
        string temporaryPassword,
        CancellationToken cancellationToken = default)
    {
        RequireAdmin();
        PasswordPolicy.Validate(temporaryPassword);

        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        var user = await db.UserAccounts.SingleAsync(x => x.Id == userId, cancellationToken);

        var (hash, salt) = PasswordSecurity.Hash(temporaryPassword);
        await db.UserAccounts
            .Where(x => x.Id == userId)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(x => x.PasswordHash, hash)
                    .SetProperty(x => x.PasswordSalt, salt)
                    .SetProperty(x => x.MustChangePassword, true)
                    .SetProperty(x => x.PasswordChangedUtc, clock.UtcNow)
                    .SetProperty(x => x.FailedLoginCount, 0)
                    .SetProperty(x => x.IsLocked, false)
                    .SetProperty(x => x.LockedUtc, (DateTime?)null),
                cancellationToken);

        await audit.WriteAsync(
            "PASSWORD_RESET_BY_ADMIN",
            "UserAccount",
            user.Id.ToString(),
            $"Administrator reset the password for '{user.Username}'. User must change it at next login.",
            cancellationToken: cancellationToken);
    }

    public async Task SetLockedAsync(
        int userId,
        bool locked,
        CancellationToken cancellationToken = default)
    {
        RequireAdmin();

        if (session.UserId == userId && locked)
            throw new InvalidOperationException("You cannot lock your own account.");

        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        var user = await db.UserAccounts.AsNoTracking()
            .SingleAsync(x => x.Id == userId, cancellationToken);

        if (locked)
        {
            await db.UserAccounts
                .Where(x => x.Id == userId)
                .ExecuteUpdateAsync(
                    setters => setters
                        .SetProperty(x => x.IsLocked, true)
                        .SetProperty(x => x.LockedUtc, clock.UtcNow),
                    cancellationToken);
        }
        else
        {
            await db.UserAccounts
                .Where(x => x.Id == userId)
                .ExecuteUpdateAsync(
                    setters => setters
                        .SetProperty(x => x.IsLocked, false)
                        .SetProperty(x => x.LockedUtc, (DateTime?)null)
                        .SetProperty(x => x.FailedLoginCount, 0),
                    cancellationToken);
        }

        await audit.WriteAsync(
            locked ? "ACCOUNT_LOCKED_BY_ADMIN" : "ACCOUNT_UNLOCKED",
            "UserAccount",
            user.Id.ToString(),
            $"Administrator {(locked ? "locked" : "unlocked")} user '{user.Username}'.",
            cancellationToken: cancellationToken);
    }

    public async Task SetRoleAsync(
        int userId,
        UserRole role,
        CancellationToken cancellationToken = default)
    {
        RequireAdmin();

        if (session.UserId == userId && role != UserRole.Administrator)
            throw new InvalidOperationException(
                "You cannot remove your own Administrator role while you are signed in.");

        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        var user = await db.UserAccounts.AsNoTracking()
            .SingleAsync(x => x.Id == userId, cancellationToken);

        var before = user.Role;
        if (before == role)
            return;

        await db.UserAccounts
            .Where(x => x.Id == userId)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(x => x.Role, role),
                cancellationToken);

        await audit.WriteAsync(
            "USER_ROLE_CHANGED",
            "UserAccount",
            user.Id.ToString(),
            $"User '{user.Username}' role changed from {before} to {role}.",
            before: new { Role = before },
            after: new { Role = role },
            cancellationToken: cancellationToken);
    }

    private void RequireAdmin()
    {
        if (session.Role != UserRole.Administrator) throw new UnauthorizedAccessException("Administrator access is required.");
    }
}


public static class PasswordPolicy
{
    public static void Validate(string password)
    {
        if (password.Length < 10)
            throw new ArgumentException("Password must be at least 10 characters.");

        if (!password.Any(char.IsUpper) ||
            !password.Any(char.IsLower) ||
            !password.Any(char.IsDigit))
        {
            throw new ArgumentException(
                "Password must contain at least one uppercase letter, one lowercase letter, and one number.");
        }
    }

    public static string StrengthText(string password)
    {
        if (string.IsNullOrEmpty(password))
            return string.Empty;

        var score = 0;
        if (password.Length >= 10) score++;
        if (password.Length >= 14) score++;
        if (password.Any(char.IsUpper) && password.Any(char.IsLower)) score++;
        if (password.Any(char.IsDigit)) score++;
        if (password.Any(ch => !char.IsLetterOrDigit(ch))) score++;

        return score switch
        {
            <= 2 => "Weak",
            3 => "Good",
            _ => "Strong"
        };
    }
}

internal static class PasswordSecurity
{
    private const int Iterations = 210_000;
    public static (string Hash, string Salt) Hash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(32);
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, 32);
        return (Convert.ToBase64String(hash), Convert.ToBase64String(salt));
    }
    public static bool Verify(string password, string expectedHash, string salt)
    {
        try
        {
            var actual = Rfc2898DeriveBytes.Pbkdf2(password, Convert.FromBase64String(salt), Iterations, HashAlgorithmName.SHA256, 32);
            return CryptographicOperations.FixedTimeEquals(actual, Convert.FromBase64String(expectedHash));
        }
        catch { return false; }
    }
}

public interface IBalanceService
{
    Task<IReadOnlyList<BalanceRow>> GetBalancesAsync(CancellationToken cancellationToken = default);
}

internal sealed class BalanceService(IDbContextFactory<BinTrackerDbContext> factory) : IBalanceService
{
    public async Task<IReadOnlyList<BalanceRow>> GetBalancesAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await factory.CreateDbContextAsync(cancellationToken);

        // Keep the SQL-translatable aggregation entirely in the database:
        // group by scalar foreign keys and calculate the signed balance.
        //
        // Do not group by navigation-property names here. Some EF Core providers
        // cannot reliably translate the previous join + navigation GroupBy +
        // BalanceRow constructor projection used by the Import Review path.
        var totals = await db.BinMovements.AsNoTracking()
            .GroupBy(x => new { x.CustomerId, x.ContainerTypeId })
            .Select(g => new
            {
                g.Key.CustomerId,
                g.Key.ContainerTypeId,
                Balance = g.Sum(x =>
                    x.MovementType == MovementType.Out
                        ? x.Quantity
                        : -x.Quantity)
            })
            .ToListAsync(cancellationToken);

        if (totals.Count == 0)
            return [];

        // Load the small master-data lookup tables directly.
        //
        // Do not filter these with int[].Contains(...) inside the EF query.
        // In the .NET 8 / EF Core 8 runtime used by BinTracker that pattern can
        // be reduced to a ReadOnlySpan<int> call while EF extracts parameters,
        // which is not valid inside the LINQ expression interpreter and throws
        // before the provider receives any SQL.
        var customerNames = await db.Customers.AsNoTracking()
            .ToDictionaryAsync(
                x => x.Id,
                x => x.Name,
                cancellationToken);

        var containerNames = await db.ContainerTypes.AsNoTracking()
            .ToDictionaryAsync(
                x => x.Id,
                x => x.Name,
                cancellationToken);

        return totals
            .Select(x => new BalanceRow(
                x.CustomerId,
                customerNames.TryGetValue(x.CustomerId, out var customerName)
                    ? customerName
                    : $"Customer #{x.CustomerId}",
                x.ContainerTypeId,
                containerNames.TryGetValue(x.ContainerTypeId, out var containerName)
                    ? containerName
                    : $"Container #{x.ContainerTypeId}",
                x.Balance))
            .OrderBy(x => x.CustomerName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(x => x.ContainerTypeName, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}

public static class ServiceSetup
{
    /// <summary>
    /// Registers provider-neutral BinTracker business services. The host must
    /// supply IUserContext and IClientContext with lifetimes appropriate to its
    /// execution model. A future API uses request-scoped implementations;
    /// the local desktop composition is provided by AddBinTrackerServices().
    /// </summary>
    public static IServiceCollection AddBinTrackerBusinessServices(
        this IServiceCollection services)
    {
        services.TryAddSingleton<IBusinessClock, ConfiguredBusinessClock>();
        services.AddScoped<IAuditService, AuditService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IBalanceService, BalanceService>();
        services.AddScoped<ICustomerService, CustomerService>();
        services.AddScoped<ICustomerStatementReportService, CustomerStatementReportService>();
        services.AddScoped<IContainerTypeService, ContainerTypeService>();
        services.AddScoped<IBusinessInformationService, BusinessInformationService>();
        services.AddScoped<IExcelImportService, ExcelImportService>();
        services.AddSingleton<
            IImportExecutionFailureInjector,
            NoOpImportExecutionFailureInjector>();
        services.AddScoped<IImportExecutionService, ImportExecutionService>();
        services.AddScoped<IImportRunHistoryService, ImportRunHistoryService>();
        services.AddScoped<IMarketFloorReportService, MarketFloorReportService>();
        services.AddScoped<IOutstandingReportService, OutstandingReportService>();
        services.AddScoped<IOutstandingReportPdfService, OutstandingReportPdfService>();
        services.AddScoped<IDailyMovementsReportService, DailyMovementsReportService>();
        services.AddScoped<IDailyMovementsReportPdfService, DailyMovementsReportPdfService>();
        services.AddScoped<IDailyPrintPackService, DailyPrintPackService>();
        services.AddScoped<IWeeklyMovementsReportService, WeeklyMovementsReportService>();
        services.AddScoped<IWeeklyMovementsReportPdfService, WeeklyMovementsReportPdfService>();
        services.AddScoped<IMovementHistoryReportService, MovementHistoryReportService>();
        services.AddScoped<IMovementHistoryReportPdfService, MovementHistoryReportPdfService>();
        services.AddScoped<IMonthlySummaryReportService, MonthlySummaryReportService>();
        services.AddScoped<IMonthlySummaryReportPdfService, MonthlySummaryReportPdfService>();
        services.AddScoped<IMovementService, MovementService>();
        services.AddScoped<IMovementCorrectionService, MovementCorrectionService>();
        return services;
    }

    /// <summary>
    /// Current local WinForms composition. Desktop-only mutable session state,
    /// device identity, authentication adapter and crash-draft storage live
    /// here so a future central API cannot inherit them accidentally.
    /// </summary>
    public static IServiceCollection AddBinTrackerServices(
        this IServiceCollection services)
    {
        services.AddSingleton<IClientContext, DesktopClientContext>();
        services.AddSingleton<UserSession>();
        services.AddSingleton<IUserContext>(sp => sp.GetRequiredService<UserSession>());
        services.AddSingleton<IBatchDraftStore, FileBatchDraftStore>();
        services.AddSingleton<ApplicationState>();
        services.AddScoped<IAuthenticationService, AuthenticationService>();

        return services.AddBinTrackerBusinessServices();
    }
}
