using BinTracker.Core;
using Microsoft.EntityFrameworkCore;

namespace BinTracker.Data;

public sealed class BinTrackerDbContext(DbContextOptions<BinTrackerDbContext> options) : DbContext(options)
{
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<ContainerType> ContainerTypes => Set<ContainerType>();
    public DbSet<MovementBatch> MovementBatches => Set<MovementBatch>();
    public DbSet<BinMovement> BinMovements => Set<BinMovement>();
    public DbSet<ApplicationSettings> ApplicationSettings => Set<ApplicationSettings>();
    public DbSet<UserAccount> UserAccounts => Set<UserAccount>();
    public DbSet<AuditEvent> AuditEvents => Set<AuditEvent>();
    public DbSet<ReminderDelivery> ReminderDeliveries => Set<ReminderDelivery>();
    public DbSet<ImportRun> ImportRuns => Set<ImportRun>();
    public DbSet<MovementCorrectionOperation> MovementCorrectionOperations => Set<MovementCorrectionOperation>();
    public DbSet<MovementCorrectionLine> MovementCorrectionLines => Set<MovementCorrectionLine>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Customer>(e =>
        {
            e.Property(x => x.Revision).IsConcurrencyToken();
            e.ToTable("Customers");
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.Property(x => x.CustomerCode).HasMaxLength(50);
            e.Property(x => x.CustomerType).IsRequired();
            e.Property(x => x.ContactName).HasMaxLength(150);
            e.Property(x => x.Phone).HasMaxLength(50);
            e.Property(x => x.MobileNumber).HasMaxLength(50);
            e.Property(x => x.Email).HasMaxLength(200);
            e.Property(x => x.Address).HasMaxLength(500);
            e.Property(x => x.Notes).HasMaxLength(2000);
            e.HasIndex(x => x.CustomerCode).IsUnique();
            e.HasIndex(x => x.Name);
        });

        b.Entity<ContainerType>(e =>
        {
            e.Property(x => x.Revision).IsConcurrencyToken();
            e.ToTable("ContainerTypes");
            e.Property(x => x.Name).HasMaxLength(100).IsRequired();
            e.Property(x => x.NameKey).HasMaxLength(100).IsRequired();
            e.Property(x => x.ShortCode).HasMaxLength(30).IsRequired();
            e.Property(x => x.SystemCode).HasMaxLength(50).IsRequired();
            e.Property(x => x.Description).HasMaxLength(500);
            e.Property(x => x.Notes).HasMaxLength(2000);
            e.Property(x => x.DashboardColour).HasMaxLength(50);
            e.HasIndex(x => x.NameKey).IsUnique();
            e.HasIndex(x => x.ShortCode).IsUnique();
            e.HasIndex(x => x.SystemCode).IsUnique();
            e.HasData(
                new ContainerType { Id=1, Name="Blue Bin", NameKey="BLUE BIN", ShortCode="BLUE", SystemCode="BLUE_BIN", Description="Standard blue reusable bin", IsActive=true, IsSpecialFloorReportContainer=false, DisplayOrder=1, CreatedUtc=DateTime.UnixEpoch, UpdatedUtc=DateTime.UnixEpoch },
                new ContainerType { Id=2, Name="Small Bin", NameKey="SMALL BIN", ShortCode="SMALL", SystemCode="SMALL_BIN", Description="Small reusable bin", IsActive=true, IsSpecialFloorReportContainer=false, DisplayOrder=2, CreatedUtc=DateTime.UnixEpoch, UpdatedUtc=DateTime.UnixEpoch },
                new ContainerType { Id=3, Name="Yellow Bin", NameKey="YELLOW BIN", ShortCode="YELLOW", SystemCode="YELLOW_BIN", Description="Yellow reusable bin", IsActive=true, IsSpecialFloorReportContainer=false, DisplayOrder=3, CreatedUtc=DateTime.UnixEpoch, UpdatedUtc=DateTime.UnixEpoch },
                new ContainerType { Id=4, Name="Bulk Bin", NameKey="BULK BIN", ShortCode="BULK", SystemCode="BULK_BIN", Description="Large bulk bin", IsActive=true, IsSpecialFloorReportContainer=false, DisplayOrder=4, CreatedUtc=DateTime.UnixEpoch, UpdatedUtc=DateTime.UnixEpoch },
                new ContainerType { Id=5, Name="CHEP Pallet", NameKey="CHEP PALLET", ShortCode="CHEP", SystemCode="CHEP_PALLET", Description="CHEP pallet", IsActive=true, IsSpecialFloorReportContainer=true, DisplayOrder=5, CreatedUtc=DateTime.UnixEpoch, UpdatedUtc=DateTime.UnixEpoch });
        });

        b.Entity<MovementBatch>(e =>
        {
            e.ToTable("MovementBatches");
            e.HasIndex(x => x.ClientOperationId).IsUnique();
            e.HasMany(x => x.Movements).WithOne(x => x.MovementBatch)
                .HasForeignKey(x => x.MovementBatchId).OnDelete(DeleteBehavior.SetNull);
        });

        b.Entity<BinMovement>(e =>
        {
            e.ToTable("BinMovements");
            e.HasOne(x => x.Customer).WithMany(x => x.Movements)
                .HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.ContainerType).WithMany(x => x.Movements)
                .HasForeignKey(x => x.ContainerTypeId).OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.ImportRun)
                .WithMany(x => x.Movements)
                .HasForeignKey(x => x.ImportRunId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasIndex(x => new { x.CustomerId, x.ContainerTypeId, x.MovementDate });
            e.HasIndex(x => x.ClientOperationId).IsUnique();
            e.HasIndex(x => x.ImportRunId);
            e.HasIndex(x => x.ReversesMovementId).IsUnique();

            e.HasOne(x => x.ReversesMovement)
                .WithOne(x => x.CorrectedByMovement)
                .HasForeignKey<BinMovement>(x => x.ReversesMovementId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<ApplicationSettings>(e =>
        {
            e.Property(x => x.Revision).IsConcurrencyToken();
            e.Property(x => x.BusinessName).HasMaxLength(200);
            e.Property(x => x.TradingName).HasMaxLength(200);
            e.Property(x => x.Abn).HasMaxLength(50);
            e.Property(x => x.Address).HasMaxLength(500);
            e.Property(x => x.Phone).HasMaxLength(80);
            e.Property(x => x.Email).HasMaxLength(200);
            e.Property(x => x.DefaultReportHeader).HasMaxLength(200);

            e.HasData(new ApplicationSettings
            {
                Id=1,
                AttentionQuantityThreshold=20,
                AttentionAgeDays=7,
                BackupRetentionCount=30,
                MaxFailedLoginAttempts=5
            });
        });

        b.Entity<UserAccount>(e =>
        {
            e.ToTable("UserAccounts");
            e.Property(x => x.Username).HasMaxLength(100).IsRequired();
            e.Property(x => x.DisplayName).HasMaxLength(150).IsRequired();
            e.Property(x => x.PasswordHash).HasMaxLength(500).IsRequired();
            e.Property(x => x.PasswordSalt).HasMaxLength(500).IsRequired();
            e.HasIndex(x => x.Username).IsUnique();
        });

        b.Entity<ReminderDelivery>(e =>
        {
            e.ToTable("ReminderDeliveries");
            e.Property(x => x.Destination).HasMaxLength(300).IsRequired();
            e.Property(x => x.Subject).HasMaxLength(300).IsRequired();
            e.Property(x => x.MessageBody).HasMaxLength(5000).IsRequired();
            e.Property(x => x.ProviderMessageId).HasMaxLength(300);
            e.Property(x => x.ProviderResponse).HasMaxLength(2000);
            e.HasOne(x => x.Customer).WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x => new { x.CustomerId, x.CreatedUtc });
        });

        b.Entity<ImportRun>(e =>
        {
            e.ToTable("ImportRuns");
            e.Property(x => x.SourceFileName).HasMaxLength(260).IsRequired();
            e.Property(x => x.SourceClientPath).HasMaxLength(2000).IsRequired();
            e.Property(x => x.ClientRequestFingerprint).HasMaxLength(64);
            e.Property(x => x.SourceSha256).HasMaxLength(64).IsRequired();
            e.Property(x => x.Status).HasMaxLength(40).IsRequired();
            e.Property(x => x.Username).HasMaxLength(100).IsRequired();
            e.Property(x => x.SessionId).HasMaxLength(100).IsRequired();
            e.Property(x => x.Notes).HasMaxLength(2000);
            e.HasIndex(x => x.SourceSha256).IsUnique();
            e.HasIndex(x => x.ClientOperationId).IsUnique();
            e.HasIndex(x => x.CompletedUtc);
            e.HasIndex(x => x.CutoverDate);
            e.HasIndex(x => x.CurrentCutoverDate).IsUnique();
            // Both SQLite and PostgreSQL allow multiple NULLs in a unique
            // index, so a provider-specific filtered-index SQL fragment is
            // unnecessary here.
            e.HasIndex(x => x.ReplacesImportRunId)
                .IsUnique();
        });

        b.Entity<AuditEvent>(e =>
        {
            e.ToTable("AuditEvents");
            e.Property(x => x.Username).HasMaxLength(100).IsRequired();
            e.Property(x => x.Action).HasMaxLength(100).IsRequired();
            e.Property(x => x.EntityType).HasMaxLength(100).IsRequired();
            e.Property(x => x.EntityId).HasMaxLength(100);
            e.Property(x => x.Description).HasMaxLength(2000).IsRequired();
            e.Property(x => x.ComputerName).HasMaxLength(200).IsRequired();
            e.Property(x => x.SessionId).HasMaxLength(100).IsRequired();
            e.HasIndex(x => x.TimestampUtc);
            e.HasIndex(x => new { x.UserId, x.TimestampUtc });
            e.HasIndex(x => new { x.RequiresAdministratorReview, x.ReviewedUtc });
        });

        b.Entity<MovementCorrectionOperation>(e =>
        {
            e.ToTable("MovementCorrectionOperations");
            e.Property(x => x.RequestFingerprint).HasMaxLength(64).IsRequired();
            e.Property(x => x.Reason).HasMaxLength(500).IsRequired();
            e.Property(x => x.ActorUsername).HasMaxLength(100).IsRequired();
            e.HasIndex(x => x.ClientOperationId).IsUnique();
            e.HasOne(x => x.OriginalBatch).WithMany()
                .HasForeignKey(x => x.OriginalBatchId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.ReplacementBatch).WithMany()
                .HasForeignKey(x => x.ReplacementBatchId).OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<MovementCorrectionLine>(e =>
        {
            e.ToTable("MovementCorrectionLines");
            e.HasOne(x => x.CorrectionOperation).WithMany(x => x.Lines)
                .HasForeignKey(x => x.CorrectionOperationId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.OriginalMovement).WithMany()
                .HasForeignKey(x => x.OriginalMovementId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.NeutralisingMovement).WithMany()
                .HasForeignKey(x => x.NeutralisingMovementId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.ReplacementMovement).WithMany()
                .HasForeignKey(x => x.ReplacementMovementId).OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x => x.OriginalMovementId).IsUnique();
            e.HasIndex(x => x.NeutralisingMovementId).IsUnique();
            e.HasIndex(x => x.ReplacementMovementId).IsUnique();
        });
    }
}
