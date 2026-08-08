using BinTracker.Core;
using BinTracker.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace BinTracker.IntegrationTests;

public sealed class MovementPersistenceTests
{
    [Fact]
    public async Task Saved_in_and_out_movements_produce_expected_balance()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<BinTrackerDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var db = new BinTrackerDbContext(options);
        await db.Database.EnsureCreatedAsync();

        var customer = new Customer
        {
            CustomerCode = "TEST",
            Name = "Test Customer",
            IsActive = true
        };

        db.Customers.Add(customer);
        await db.SaveChangesAsync();

        db.BinMovements.AddRange(
            new BinMovement
            {
                CustomerId = customer.Id,
                ContainerTypeId = 1,
                MovementDate = DateOnly.FromDateTime(DateTime.Today),
                MovementType = MovementType.Out,
                Source = MovementSource.Batch,
                Quantity = 20,
                CreatedBy = "test"
            },
            new BinMovement
            {
                CustomerId = customer.Id,
                ContainerTypeId = 1,
                MovementDate = DateOnly.FromDateTime(DateTime.Today),
                MovementType = MovementType.In,
                Source = MovementSource.Batch,
                Quantity = 7,
                CreatedBy = "test"
            });

        await db.SaveChangesAsync();

        var balance = await db.BinMovements
            .Where(x => x.CustomerId == customer.Id && x.ContainerTypeId == 1)
            .SumAsync(x => x.MovementType == MovementType.Out
                ? x.Quantity
                : -x.Quantity);

        Assert.Equal(13, balance);
    }

    [Fact]
    public async Task Movement_batch_can_hold_multiple_customer_lines()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<BinTrackerDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var db = new BinTrackerDbContext(options);
        await db.Database.EnsureCreatedAsync();

        var a = new Customer { CustomerCode = "A", Name = "Customer A" };
        var b = new Customer { CustomerCode = "B", Name = "Customer B" };
        db.Customers.AddRange(a, b);
        await db.SaveChangesAsync();

        var batch = new MovementBatch
        {
            MovementDate = DateOnly.FromDateTime(DateTime.Today),
            MovementType = MovementType.In,
            Source = MovementSource.Batch,
            CreatedBy = "test"
        };

        db.MovementBatches.Add(batch);
        await db.SaveChangesAsync();

        db.BinMovements.AddRange(
            new BinMovement
            {
                CustomerId = a.Id,
                ContainerTypeId = 1,
                MovementBatchId = batch.Id,
                MovementDate = batch.MovementDate,
                MovementType = batch.MovementType,
                Source = MovementSource.Batch,
                Quantity = 10,
                CreatedBy = "test"
            },
            new BinMovement
            {
                CustomerId = b.Id,
                ContainerTypeId = 1,
                MovementBatchId = batch.Id,
                MovementDate = batch.MovementDate,
                MovementType = batch.MovementType,
                Source = MovementSource.Batch,
                Quantity = 15,
                CreatedBy = "test"
            });

        await db.SaveChangesAsync();

        Assert.Equal(2, await db.BinMovements.CountAsync(x => x.MovementBatchId == batch.Id));
        Assert.Equal(25, await db.BinMovements
            .Where(x => x.MovementBatchId == batch.Id)
            .SumAsync(x => x.Quantity));
    }
}
