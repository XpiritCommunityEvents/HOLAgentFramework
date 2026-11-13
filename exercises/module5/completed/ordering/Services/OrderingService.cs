using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using GloboTicket.Ordering.Model;
using Microsoft.EntityFrameworkCore;

namespace GloboTicket.Ordering.Services;

public sealed class OrderException(string message, int statusCode = 400) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
}

public sealed class OrderingService(OrderingDbContext db, IOrderingCatalog catalog, TimeProvider time)
{
    public async Task<OrderQuote> QuoteAsync(QuoteRequest request, CancellationToken cancellationToken = default)
    {
        if (request.Lines is null || request.Lines.Count is < 1 or > 20 ||
            request.Lines.Any(l => l is null || l.EventId == Guid.Empty || l.TicketCount is < 1 or > 100) ||
            request.Lines.Select(l => l.EventId).Distinct().Count() != request.Lines.Count)
            throw new OrderException("Supply 1-20 distinct events, each with a non-empty ID and 1-100 tickets.");
        if (request.MaxTotal is < 0)
            throw new OrderException("The budget cannot be negative.");

        string? promo = string.IsNullOrWhiteSpace(request.PromoCode) ? null : request.PromoCode.Trim().ToUpperInvariant();
        decimal rate = promo switch
        {
            null => 0m,
            "SAVE10" => .10m,
            "SAVE15" => .15m,
            "SAVE25" => .25m,
            _ => throw new OrderException("Unknown promo code. Use SAVE10, SAVE15 or SAVE25.")
        };
        var lines = new List<PricedLine>();
        foreach (var line in request.Lines)
        {
            var item = await catalog.GetEventAsync(line.EventId, cancellationToken);
            if (item.Price < 0 || item.Price > 1_000_000m)
                throw new OrderException("The catalog returned an unsupported ticket price.", 502);
            lines.Add(new(item.EventId, item.Name, line.TicketCount, item.Price));
        }
        decimal subtotal = lines.Sum(l => l.UnitPrice * l.TicketCount);
        decimal discounted = lines.Sum(l => Math.Round(l.UnitPrice * l.TicketCount * (1 - rate), 2));
        decimal fee = discounted < 500m ? Math.Round(discounted * .05m, 2) : 0m;
        decimal total = discounted + fee;
        if (request.MaxTotal is { } budget && total > budget)
            throw new OrderException($"The total USD {total:F2}, including fees, exceeds the budget USD {budget:F2}.");
        var quote = new OrderQuote(Guid.NewGuid(), time.GetUtcNow().AddMinutes(15), lines,
            "USD", promo, subtotal, subtotal - discounted, fee, total);
        db.Quotes.Add(new StoredQuote { Id = quote.QuoteId, Snapshot = JsonSerializer.Serialize(quote) });
        await db.SaveChangesAsync(cancellationToken);
        return quote;
    }

    public async Task<OrderQuote> GetQuoteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var stored = await db.Quotes.AsNoTracking().SingleOrDefaultAsync(q => q.Id == id, cancellationToken)
            ?? throw new OrderException("Quote not found.", 404);
        return JsonSerializer.Deserialize<OrderQuote>(stored.Snapshot)!;
    }

    public async Task<OrderReceipt> SubmitAsync(SubmitOrderRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Name) || request.Name.Length > 120 ||
            string.IsNullOrWhiteSpace(request.Email) || request.Email.Length > 254 ||
            !new EmailAddressAttribute().IsValid(request.Email.Trim()))
            throw new OrderException("Supply a fictitious customer name and valid demo email address.");
        string name = request.Name.Trim();
        string email = request.Email.Trim().ToLowerInvariant();
        var existing = await db.Orders.AsNoTracking().SingleOrDefaultAsync(o => o.QuoteId == request.QuoteId, cancellationToken);
        if (existing is not null) return Retry(existing, name, email);
        var quote = await GetQuoteAsync(request.QuoteId, cancellationToken);
        if (quote.ExpiresAt <= time.GetUtcNow())
            throw new OrderException("Quote expired. Request a new quote and approval.", 409);

        var receipt = new OrderReceipt(Guid.NewGuid(), time.GetUtcNow(), quote);
        var order = new StoredOrder
        {
            QuoteId = quote.QuoteId, OrderId = receipt.OrderId,
            Name = name, Email = email, Receipt = JsonSerializer.Serialize(receipt)
        };
        db.Orders.Add(order);
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            // A unique quote key arbitrates concurrent submissions across service instances.
            db.Entry(order).State = EntityState.Detached;
            existing = await db.Orders.AsNoTracking().SingleOrDefaultAsync(o => o.QuoteId == request.QuoteId, cancellationToken);
            if (existing is null) throw;
            return Retry(existing, name, email);
        }
        return receipt;
    }

    public async Task<OrderReceipt> GetOrderAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var stored = await db.Orders.AsNoTracking().SingleOrDefaultAsync(o => o.OrderId == id, cancellationToken)
            ?? throw new OrderException("Order not found.", 404);
        return JsonSerializer.Deserialize<OrderReceipt>(stored.Receipt)!;
    }

    private static OrderReceipt Retry(StoredOrder existing, string name, string email)
    {
        if (existing.Name != name || existing.Email != email)
            throw new OrderException("This quote was already submitted with different customer details.", 409);
        return JsonSerializer.Deserialize<OrderReceipt>(existing.Receipt)!;
    }
}
