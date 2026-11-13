using Microsoft.EntityFrameworkCore;

namespace GloboTicket.Ordering.Services;

public sealed class OrderingDbContext(DbContextOptions<OrderingDbContext> options) : DbContext(options)
{
    public DbSet<StoredQuote> Quotes => Set<StoredQuote>();
    public DbSet<StoredOrder> Orders => Set<StoredOrder>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<StoredQuote>().HasKey(q => q.Id);
        modelBuilder.Entity<StoredOrder>().HasKey(o => o.QuoteId);
        modelBuilder.Entity<StoredOrder>().HasIndex(o => o.OrderId).IsUnique();
        modelBuilder.Entity<StoredOrder>().HasOne<StoredQuote>().WithMany()
            .HasForeignKey(o => o.QuoteId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class StoredQuote
{
    public Guid Id { get; set; }
    public required string Snapshot { get; set; }
}

public sealed class StoredOrder
{
    public Guid QuoteId { get; set; }
    public Guid OrderId { get; set; }
    public required string Name { get; set; }
    public required string Email { get; set; }
    public required string Receipt { get; set; }
}
