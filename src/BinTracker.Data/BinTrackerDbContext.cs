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

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Customer>(e =>
        {
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
            e.ToTable("ContainerTypes");
            e.Property(x => x.Name).HasMaxLength(100).IsRequired();
            e.Property(x => x.ShortCode).HasMaxLength(30).IsRequired();
            e.Property(x => x.SystemCode).HasMaxLength(50).IsRequired();
            e.Property(x => x.Description).HasMaxLength(500);
            e.Property(x => x.Notes).HasMaxLength(2000);
            e.Property(x => x.DashboardColour).HasMaxLength(50);
            e.HasIndex(x => x.Name).IsUnique();
            e.HasIndex(x => x.ShortCode).IsUnique();
            e.HasIndex(x => x.SystemCode).IsUnique();
            e.HasData(
                new ContainerType { Id=1, Name="Blue Bin", ShortCode="BLUE", SystemCode="BLUE_BIN", Description="Standard blue reusable bin", IsActive=true, IsSpecialFloorReportContainer=false, DisplayOrder=1, CreatedUtc=DateTime.UnixEpoch, UpdatedUtc=DateTime.UnixEpoch },
                new ContainerType { Id=2, Name="Small Bin", ShortCode="SMALL", SystemCode="SMALL_BIN", Description="Small reusable bin", IsActive=true, IsSpecialFloorReportContainer=false, DisplayOrder=2, CreatedUtc=DateTime.UnixEpoch, UpdatedUtc=DateTime.UnixEpoch },
                new ContainerType { Id=3, Name="Yellow Bin", ShortCode="YELLOW", SystemCode="YELLOW_BIN", Description="Yellow reusable bin", IsActive=true, IsSpecialFloorReportContainer=false, DisplayOrder=3, CreatedUtc=DateTime.UnixEpoch, UpdatedUtc=DateTime.UnixEpoch },
                new ContainerType { Id=4, Name="Bulk Bin", ShortCode="BULK", SystemCode="BULK_BIN", Description="Large bulk bin", IsActive=true, IsSpecialFloorReportContainer=false, DisplayOrder=4, CreatedUtc=DateTime.UnixEpoch, UpdatedUtc=DateTime.UnixEpoch },
                new ContainerType { Id=5, Name="CHEP Pallet", ShortCode="CHEP", SystemCode="CHEP_PALLET", Description="CHEP pallet", IsActive=true, IsSpecialFloorReportContainer=true, DisplayOrder=5, CreatedUtc=DateTime.UnixEpoch, UpdatedUtc=DateTime.UnixEpoch });
        });

        b.Entity<MovementBatch>(e =>
        {
            e.ToTable("MovementBatches");
            e.HasMany(x => x.Movements).WithOne(x => x.MovementBatch)
                .HasForeignKey(x => x.MovementBatchId).OnDelete(DeleteBehavior.SetNull);
        });

        b.Entity<BinMovement>(e =>
        {
            e.ToTable("BinMovements", t => t.HasCheckConstraint("CK_BinMovements_Quantity_Positive", "Quantity > 0"));
            e.HasOne(x => x.Customer).WithMany(x => x.Movements)
                .HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.ContainerType).WithMany(x => x.Movements)
                .HasForeignKey(x => x.ContainerTypeId).OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x => new { x.CustomerId, x.ContainerTypeId, x.MovementDate });
        });

        b.Entity<ApplicationSettings>(e =>
        {
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
        });
    }
}
