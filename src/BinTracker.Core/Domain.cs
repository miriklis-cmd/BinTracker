namespace BinTracker.Core;

public enum MovementType { In = 0, Out = 1 }
public enum MovementSource { Manual = 0, Batch = 1, ExcelImport = 2, Adjustment = 3 }
public enum UserRole { Administrator = 0, Operator = 1, Viewer = 2 }
public enum CustomerType { Account = 0, CashCod = 1 }
public enum CommunicationChannel { Email = 0, Sms = 1 }
public enum ReminderDeliveryStatus { Pending = 0, Sent = 1, Failed = 2, Skipped = 3 }
public enum MovementCorrectionKind { Single = 0, WholeBatch = 1 }

public sealed class Customer
{
    public long Revision { get; set; } = 1;
    public int Id { get; set; }
    public string? CustomerCode { get; set; }
    public string Name { get; set; } = string.Empty;
    public CustomerType CustomerType { get; set; } = CustomerType.Account;
    public string? ContactName { get; set; }
    public string? Phone { get; set; }
    public string? MobileNumber { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? Notes { get; set; }
    public bool AllowEmailReminders { get; set; } = true;
    public bool AllowSmsReminders { get; set; } = true;
    public bool ReminderOptOut { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedUtc { get; set; } = DateTime.UtcNow;
    public int? CreatedByUserId { get; set; }
    public int? UpdatedByUserId { get; set; }
    public ICollection<BinMovement> Movements { get; set; } = new List<BinMovement>();
}

public sealed class ContainerType
{
    public long Revision { get; set; } = 1;
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string NameKey { get; set; } = string.Empty;
    public string ShortCode { get; set; } = string.Empty;
    public string SystemCode { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsSpecialFloorReportContainer { get; set; }
    public string? DashboardColour { get; set; }
    public int DisplayOrder { get; set; }
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedUtc { get; set; } = DateTime.UtcNow;
    public ICollection<BinMovement> Movements { get; set; } = new List<BinMovement>();
}

public sealed class MovementBatch
{
    public int Id { get; set; }
    public Guid? ClientOperationId { get; set; }
    public DateOnly MovementDate { get; set; }
    public MovementType MovementType { get; set; }
    public MovementSource Source { get; set; } = MovementSource.Batch;
    public string? Notes { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    public bool IsReversed { get; set; }
    public ICollection<BinMovement> Movements { get; set; } = new List<BinMovement>();
}

public sealed class BinMovement
{
    public long Id { get; set; }
    public Guid? ClientOperationId { get; set; }
    public DateOnly MovementDate { get; set; }
    public MovementType MovementType { get; set; }
    public MovementSource Source { get; set; }
    public int CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;
    public int ContainerTypeId { get; set; }
    public ContainerType ContainerType { get; set; } = null!;
    public int? MovementBatchId { get; set; }
    public MovementBatch? MovementBatch { get; set; }

    // Nullable because operator-entered/manual/batch movements are not created
    // by an Excel Import Run. Import-generated Adjustment/ExcelImport rows must
    // carry this FK so replacement/correction tooling can target only the
    // records created by a specific import.
    public long? ImportRunId { get; set; }
    public ImportRun? ImportRun { get; set; }

    public int Quantity { get; set; }
    public string? ReferenceNumber { get; set; }
    public string? Notes { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

    // Corrections are append-only ledger entries. Original movements are never
    // edited or deleted; these links preserve the correction chain.
    public long? ReversesMovementId { get; set; }
    public BinMovement? ReversesMovement { get; set; }
    public long? CorrectedByMovementId { get; set; }
    public BinMovement? CorrectedByMovement { get; set; }
    public string? CorrectionReason { get; set; }
}

public sealed class MovementCorrectionOperation
{
    public long Id { get; set; }
    public Guid ClientOperationId { get; set; }
    public string RequestFingerprint { get; set; } = string.Empty;
    public MovementCorrectionKind Kind { get; set; }
    public int? OriginalBatchId { get; set; }
    public MovementBatch? OriginalBatch { get; set; }
    public int? ReplacementBatchId { get; set; }
    public MovementBatch? ReplacementBatch { get; set; }
    public string Reason { get; set; } = string.Empty;
    public int ActorUserId { get; set; }
    public string ActorUsername { get; set; } = string.Empty;
    public DateTime CreatedUtc { get; set; }
    public ICollection<MovementCorrectionLine> Lines { get; set; } = new List<MovementCorrectionLine>();
}

public sealed class MovementCorrectionLine
{
    public long Id { get; set; }
    public long CorrectionOperationId { get; set; }
    public MovementCorrectionOperation CorrectionOperation { get; set; } = null!;
    public long OriginalMovementId { get; set; }
    public BinMovement OriginalMovement { get; set; } = null!;
    public long NeutralisingMovementId { get; set; }
    public BinMovement NeutralisingMovement { get; set; } = null!;
    public long ReplacementMovementId { get; set; }
    public BinMovement ReplacementMovement { get; set; } = null!;
}

public sealed class ImportRun
{
    public Guid? ClientOperationId { get; set; }
    public string? ClientRequestFingerprint { get; set; }
    public DateOnly? CurrentCutoverDate { get; set; }
    public long Id { get; set; }
    public string SourceFileName { get; set; } = string.Empty;
    public string SourceClientPath { get; set; } = string.Empty;
    public string SourceSha256 { get; set; } = string.Empty;
    public long SourceLength { get; set; }
    public DateTime SourceLastWriteUtc { get; set; }
    public DateOnly? CutoverDate { get; set; }
    public long? ReplacesImportRunId { get; set; }

    // Immutable JSON snapshot of the approved replacement differences.
    // Stored on the corrected run because the previous run's generated
    // movements are deliberately removed from the live ledger.
    public string? CorrectionChangesJson { get; set; }

    // Immutable snapshot of non-zero opening reconciliation adjustments
    // approved for this run. Unlike CorrectionChangesJson, this applies to
    // ordinary new cutovers as well as replacement runs.
    public string? OpeningReconciliationChangesJson { get; set; }

    public DateTime StartedUtc { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedUtc { get; set; }
    public string Status { get; set; } = "Pending";
    public int CreatedCustomers { get; set; }
    public int MovementCount { get; set; }
    public int? UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string SessionId { get; set; } = string.Empty;
    public string? Notes { get; set; }

    public ICollection<BinMovement> Movements { get; set; } =
        new List<BinMovement>();
}

public sealed class ApplicationSettings
{
    public long Revision { get; set; } = 1;
    public int Id { get; set; } = 1;
    public int AttentionQuantityThreshold { get; set; } = 20;
    public int AttentionAgeDays { get; set; } = 7;
    public int BackupRetentionCount { get; set; } = 30;
    public int MaxFailedLoginAttempts { get; set; } = 5;

    // Business identity is configurable master data. Keeping it here means
    // reports and future communications share one authoritative source.
    public string? BusinessName { get; set; }
    public string? TradingName { get; set; }
    public string? Abn { get; set; }
    public string? Address { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? DefaultReportHeader { get; set; }
}

public sealed class UserAccount
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string PasswordSalt { get; set; } = string.Empty;
    public UserRole Role { get; set; } = UserRole.Operator;
    public bool IsActive { get; set; } = true;
    public bool MustChangePassword { get; set; }
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    public int? CreatedByUserId { get; set; }
    public DateTime? LastLoginUtc { get; set; }
    public DateTime? PasswordChangedUtc { get; set; }
    public int FailedLoginCount { get; set; }
    public bool IsLocked { get; set; }
    public DateTime? LockedUtc { get; set; }
}

public sealed class AuditEvent
{
    public long Id { get; set; }
    public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;
    public int? UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public string? EntityId { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? BeforeValues { get; set; }
    public string? AfterValues { get; set; }
    public string ComputerName { get; set; } = string.Empty;
    public string SessionId { get; set; } = string.Empty;
    public bool Succeeded { get; set; } = true;
    public bool RequiresAdministratorReview { get; set; }
    public DateTime? ReviewedUtc { get; set; }
    public int? ReviewedByUserId { get; set; }
    public string? ReviewedByUsername { get; set; }
}


public sealed class ReminderDelivery
{
    public long Id { get; set; }
    public int CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;
    public CommunicationChannel Channel { get; set; }
    public ReminderDeliveryStatus Status { get; set; } = ReminderDeliveryStatus.Pending;
    public string Destination { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string MessageBody { get; set; } = string.Empty;
    public string? ProviderMessageId { get; set; }
    public string? ProviderResponse { get; set; }
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    public DateTime? SentUtc { get; set; }
    public int? InitiatedByUserId { get; set; }
    public string? OutstandingSnapshotJson { get; set; }
}
