using BinTracker.Core;
using BinTracker.Data;
using BinTracker.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BinTracker.IntegrationTests;

public sealed class MovementCorrectionConcurrencyTests
{
    [Theory]
    [InlineData("reverse-reverse")]
    [InlineData("reverse-correct")]
    [InlineData("correct-correct")]
    public async Task Competing_commands_leave_exactly_one_consuming_lineage(string race)
    {
        await using var h = await ConcurrentHarness.Create();
        var id = await h.AddMovement();
        var a = h.Service(0); var b = h.Service(1);
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        async Task<object> RunA() { await gate.Task; return race == "correct-correct"
            ? await a.CorrectAsync(h.Correction(id, 1, "First correction"))
            : await a.ReverseAsync(new(Guid.NewGuid(), id, "First reversal")); }
        async Task<object> RunB() { await gate.Task; return race == "reverse-reverse"
            ? await b.ReverseAsync(new(Guid.NewGuid(), id, "Second reversal"))
            : await b.CorrectAsync(h.Correction(id, 2, "Second correction")); }
        var first = Capture(RunA); var second = Capture(RunB); gate.SetResult();
        var outcomes = await Task.WhenAll(first, second);
        Assert.Single(outcomes, x => x.Error is null);
        Assert.Single(outcomes, x => x.Error is InvalidOperationException);
        await using var db = await h.Factory.CreateDbContextAsync();
        Assert.Equal(1, await db.BinMovements.CountAsync(x => x.ReversesMovementId == id));
        Assert.NotNull((await db.BinMovements.FindAsync(id))!.CorrectedByMovementId);
        Assert.InRange(await db.MovementCorrectionOperations.CountAsync(), 0, 1);
    }

    [Fact]
    public async Task Concurrent_identical_correction_retry_returns_one_operation_without_duplicates()
    {
        await using var h = await ConcurrentHarness.Create();
        var movement = await h.AddMovement(); var operation = Guid.NewGuid();
        var request = h.Correction(movement, 1, "Retry correction", operation);
        var results = await Task.WhenAll(h.Service(0).CorrectAsync(request), h.Service(1).CorrectAsync(request));
        Assert.Equal(results[0].CorrectionOperationId, results[1].CorrectionOperationId);
        await using var db = await h.Factory.CreateDbContextAsync();
        Assert.Equal(1, await db.MovementCorrectionOperations.CountAsync());
        Assert.Equal(1, await db.BinMovements.CountAsync(x => x.ReversesMovementId == movement));
    }

    [Fact]
    public async Task Concurrent_identical_reversal_retry_returns_one_reversal_without_duplicates()
    {
        await using var h = await ConcurrentHarness.Create();
        var movement = await h.AddMovement(); var operation = Guid.NewGuid();
        var request = new ReverseMovementRequest(operation, movement, "Retry reversal");
        var results = await Task.WhenAll(h.Service(0).ReverseAsync(request), h.Service(1).ReverseAsync(request));
        Assert.Equal(results[0], results[1]);
        await using var db = await h.Factory.CreateDbContextAsync();
        Assert.Equal(1, await db.BinMovements.CountAsync(x => x.ReversesMovementId == movement));
    }

    [Fact]
    public async Task Racing_whole_batch_corrections_have_one_winner_and_no_partial_second_operation()
    {
        await using var h = await ConcurrentHarness.Create();
        var batch = await h.AddBatch();
        var a = Capture(() => h.Service(0).CorrectBatchAsync(new(Guid.NewGuid(), batch,
            new DateOnly(2026, 8, 18), null, "First batch correction")));
        var b = Capture(() => h.Service(1).CorrectBatchAsync(new(Guid.NewGuid(), batch,
            null, MovementType.In, "Second batch correction")));
        var outcomes = await Task.WhenAll(a, b);
        Assert.Single(outcomes, x => x.Error is null);
        Assert.Single(outcomes, x => x.Error is InvalidOperationException);
        await using var db = await h.Factory.CreateDbContextAsync();
        Assert.Equal(1, await db.MovementCorrectionOperations.CountAsync());
        Assert.Equal(3, await db.MovementCorrectionLines.CountAsync());
        var originals = await db.BinMovements.Where(x => x.MovementBatchId == batch).ToListAsync();
        Assert.All(originals, x => Assert.NotNull(x.CorrectedByMovementId));
    }

    private static async Task<(object? Value, Exception? Error)> Capture(Func<Task<object>> action)
    { try { return (await action(), null); } catch (Exception ex) { return (null, ex); } }
    private static async Task<(object? Value, Exception? Error)> Capture<T>(Func<Task<T>> action)
    { try { return (await action(), null); } catch (Exception ex) { return (null, ex); } }

    private sealed class ConcurrentHarness : IAsyncDisposable
    {
        private readonly string path;
        private readonly ServiceProvider[] providers;
        public IDbContextFactory<BinTrackerDbContext> Factory { get; }
        private int customerId;
        private ConcurrentHarness(string path, ServiceProvider[] providers, IDbContextFactory<BinTrackerDbContext> factory)
            => (this.path, this.providers, Factory) = (path, providers, factory);
        public static async Task<ConcurrentHarness> Create()
        {
            var path = Path.Combine(Path.GetTempPath(), $"bintracker-correction-{Guid.NewGuid():N}.db");
            var connection = $"Data Source={path};Cache=Shared;Default Timeout=30";
            ServiceProvider Make()
            {
                var services = new ServiceCollection();
                services.AddDbContextFactory<BinTrackerDbContext>(o => o.UseSqlite(connection));
                services.AddBinTrackerServices(); return services.BuildServiceProvider();
            }
            var providers = new[] { Make(), Make() };
            var factory = providers[0].GetRequiredService<IDbContextFactory<BinTrackerDbContext>>();
            var h = new ConcurrentHarness(path, providers, factory);
            await using var db = await factory.CreateDbContextAsync(); await DatabaseSetup.InitializeSqliteAsync(db);
            var users = new[] {
                new UserAccount { Username="one", DisplayName="One", PasswordHash="x", PasswordSalt="x", Role=UserRole.Administrator, IsActive=true },
                new UserAccount { Username="two", DisplayName="Two", PasswordHash="x", PasswordSalt="x", Role=UserRole.Administrator, IsActive=true }};
            var customer = new Customer { CustomerCode="RACE", Name="Race Customer" };
            db.AddRange(users[0], users[1], customer); await db.SaveChangesAsync(); h.customerId = customer.Id;
            providers[0].GetRequiredService<UserSession>().SignIn(users[0]);
            providers[1].GetRequiredService<UserSession>().SignIn(users[1]);
            return h;
        }
        public IMovementCorrectionService Service(int index) => providers[index].GetRequiredService<IMovementCorrectionService>();
        public CorrectMovementRequest Correction(long id, int quantity, string reason, Guid? operation = null) =>
            new(operation ?? Guid.NewGuid(), id, new DateOnly(2026, 8, 19), customerId, 1, MovementType.Out, quantity, "ref", "notes", reason);
        public async Task<long> AddMovement()
        { await using var db = await Factory.CreateDbContextAsync(); var row = new BinMovement { MovementDate=new DateOnly(2026,8,19), MovementType=MovementType.Out, Source=MovementSource.Manual, CustomerId=customerId, ContainerTypeId=1, Quantity=10 }; db.Add(row); await db.SaveChangesAsync(); return row.Id; }
        public async Task<int> AddBatch()
        { await using var db = await Factory.CreateDbContextAsync(); var batch = new MovementBatch { MovementDate=new DateOnly(2026,8,19), MovementType=MovementType.Out, Source=MovementSource.Batch }; db.Add(batch); foreach (var q in new[] { 2, 3, 4 }) batch.Movements.Add(new BinMovement { MovementDate=batch.MovementDate, MovementType=batch.MovementType, Source=MovementSource.Batch, CustomerId=customerId, ContainerTypeId=1, Quantity=q }); await db.SaveChangesAsync(); return batch.Id; }
        public async ValueTask DisposeAsync()
        {
            foreach (var provider in providers) await provider.DisposeAsync();
            SqliteConnection.ClearAllPools();
            if (File.Exists(path)) File.Delete(path);
        }
    }
}
