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
            e.HasIndex(x => x.Name).IsUnique();
            e.HasData(
                new ContainerType { Id=1, Name="Blue Bin", Description="Standard blue reusable bin", IsActive=true, DisplayOrder=1, CreatedUtc=DateTime.UnixEpoch, UpdatedUtc=DateTime.UnixEpoch },
                new ContainerType { Id=2, Name="Small Bin", Description="Small reusable bin", IsActive=true, DisplayOrder=2, CreatedUtc=DateTime.UnixEpoch, UpdatedUtc=DateTime.UnixEpoch },
                new ContainerType { Id=3, Name="Yellow Bin", Description="Yellow reusable bin", IsActive=true, DisplayOrder=3, CreatedUtc=DateTime.UnixEpoch, UpdatedUtc=DateTime.UnixEpoch },
                new ContainerType { Id=4, Name="Bulk Bin", Description="Large bulk bin", IsActive=true, DisplayOrder=4, CreatedUtc=DateTime.UnixEpoch, UpdatedUtc=DateTime.UnixEpoch },
                new ContainerType { Id=5, Name="CHEP Pallet", Description="CHEP pallet", IsActive=true, DisplayOrder=5, CreatedUtc=DateTime.UnixEpoch, UpdatedUtc=DateTime.UnixEpoch });
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

        b.Entity<ApplicationSettings>().HasData(new ApplicationSettings
        {
            Id=1, AttentionQuantityThreshold=20, AttentionAgeDays=7, BackupRetentionCount=30
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
