using GloboTicket.Ordering.Model;
using GloboTicket.Ordering.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace GloboTicket.UnitTests;

[TestClass]
public sealed class OrderingContractTests
{
    [TestMethod]
    public async Task Concurrent_submissions_from_separate_contexts_create_one_order()
    {
        string connectionString = $"Data Source={Guid.NewGuid()};Mode=Memory;Cache=Shared;Default Timeout=5";
        await using var connection = new SqliteConnection(connectionString);
        await connection.OpenAsync();
        var barrier = new ConcurrentSubmissionBarrier();
        var options = new DbContextOptionsBuilder<OrderingDbContext>().UseSqlite(connectionString)
            .AddInterceptors(barrier).Options;
        var catalog = new TestCatalog();
        await using var seed = new OrderingDbContext(options);
        await seed.Database.EnsureCreatedAsync();
        var quote = await new OrderingService(seed, catalog, TimeProvider.System)
            .QuoteAsync(new([new(catalog.EventId, 2)]));
        await using var first = new OrderingDbContext(options);
        await using var second = new OrderingDbContext(options);
        var request = new SubmitOrderRequest(quote.QuoteId, "Demo Customer", "demo@example.test");
        var results = await Task.WhenAll(
            new OrderingService(first, catalog, TimeProvider.System).SubmitAsync(request),
            new OrderingService(second, catalog, TimeProvider.System).SubmitAsync(request));
        Assert.AreEqual(results[0].OrderId, results[1].OrderId);
        Assert.AreEqual(1, await seed.Orders.CountAsync());
    }

    [TestMethod]
    [DataRow(500, null, 0, 500)]
    [DataRow(500, "SAVE10", 22.5, 472.5)]
    [DataRow(499.99, null, 25, 524.99)]
    [DataRow(100, "SAVE25", 3.75, 78.75)]
    public async Task Quote_applies_discount_before_fee_threshold(double price, string? promo, double fee, double total)
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var db = new OrderingDbContext(new DbContextOptionsBuilder<OrderingDbContext>().UseSqlite(connection).Options);
        await db.Database.EnsureCreatedAsync();
        var catalog = new TestCatalog { Price = (decimal)price };
        var ordering = new OrderingService(db, catalog, TimeProvider.System);
        var quote = await ordering.QuoteAsync(new([new(catalog.EventId, 1)], promo));
        Assert.AreEqual((decimal)fee, quote.AdministrationFee);
        Assert.AreEqual((decimal)total, quote.Total);
    }

    [TestMethod]
    public async Task Expiry_budget_and_changed_retry_cannot_silently_change_an_approved_purchase()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var db = new OrderingDbContext(new DbContextOptionsBuilder<OrderingDbContext>().UseSqlite(connection).Options);
        await db.Database.EnsureCreatedAsync();
        var catalog = new TestCatalog();
        var time = new DemoTime();
        var service = new OrderingService(db, catalog, time);
        var quote = await service.QuoteAsync(new([new(catalog.EventId, 1)]));
        await Assert.ThrowsAsync<OrderException>(() => service.QuoteAsync(new([new(catalog.EventId, 1)], MaxTotal: 100m)));
        await Assert.ThrowsAsync<OrderException>(() => service.QuoteAsync(new([new(catalog.EventId, 0)])));
        await Assert.ThrowsAsync<OrderException>(() => service.QuoteAsync(new([new(Guid.Empty, 1)])));
        time.Now = time.Now.AddMinutes(15);
        await Assert.ThrowsAsync<OrderException>(() => service.SubmitAsync(new(quote.QuoteId, "Demo", "demo@example.test")));
        var fresh = await service.QuoteAsync(new([new(catalog.EventId, 1)]));
        var receipt = await service.SubmitAsync(new(fresh.QuoteId, "Demo", "demo@example.test"));
        await Assert.ThrowsAsync<OrderException>(() => service.SubmitAsync(new(fresh.QuoteId, "Changed", "demo@example.test")));
        time.Now = time.Now.AddDays(1);
        Assert.AreEqual(receipt.OrderId, (await service.SubmitAsync(new(fresh.QuoteId, "Demo", "demo@example.test"))).OrderId);
    }

    [TestMethod]
    public async Task Quoted_purchase_uses_catalog_prices_discounts_and_fees_and_survives_restart()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<OrderingDbContext>().UseSqlite(connection).Options;
        var catalog = new TestCatalog();
        Guid quoteId;
        Guid orderId;
        await using (var db = new OrderingDbContext(options))
        {
            await db.Database.EnsureCreatedAsync();
            var service = new OrderingService(db, catalog, TimeProvider.System);
            var quote = await service.QuoteAsync(new([new(catalog.EventId, 2)], "SAVE10"));
            Assert.AreEqual(200m, quote.Subtotal);
            Assert.AreEqual(20m, quote.Discount);
            Assert.AreEqual(9m, quote.AdministrationFee);
            Assert.AreEqual(189m, quote.Total);
            quoteId = quote.QuoteId;
            var order = await service.SubmitAsync(new(quoteId, "Demo Customer", "demo@example.test"));
            orderId = order.OrderId;
        }

        await using var restartedDb = new OrderingDbContext(options);
        var restarted = new OrderingService(restartedDb, catalog, TimeProvider.System);
        var receipt = await restarted.GetOrderAsync(orderId);
        Assert.AreEqual(189m, receipt.Quote.Total);
        Assert.AreEqual(quoteId, receipt.Quote.QuoteId);
        Assert.AreEqual(orderId,
            (await restarted.SubmitAsync(new(quoteId, "Demo Customer", "demo@example.test"))).OrderId);
    }

    private sealed class TestCatalog : IOrderingCatalog
    {
        internal Guid EventId { get; } = Guid.NewGuid();
        internal decimal Price { get; init; } = 100m;

        public Task<CatalogEvent> GetEventAsync(Guid eventId, CancellationToken cancellationToken) =>
            Task.FromResult(new CatalogEvent(eventId, "Demo Concert", Price));
    }

    private sealed class DemoTime : TimeProvider
    {
        public DateTimeOffset Now { get; set; } = new(2026, 11, 13, 12, 0, 0, TimeSpan.Zero);
        public override DateTimeOffset GetUtcNow() => Now;
    }

    private sealed class ConcurrentSubmissionBarrier : SaveChangesInterceptor
    {
        private readonly TaskCompletionSource bothReady = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int submissions;

        public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            if (eventData.Context!.ChangeTracker.Entries<StoredOrder>().Any(e => e.State == EntityState.Added))
            {
                if (Interlocked.Increment(ref submissions) == 2) bothReady.SetResult();
                await bothReady.Task.WaitAsync(TimeSpan.FromSeconds(10), cancellationToken);
            }
            return result;
        }
    }
}
