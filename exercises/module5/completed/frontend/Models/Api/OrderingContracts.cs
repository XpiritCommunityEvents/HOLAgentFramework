namespace GloboTicket.Frontend.Models.Api;

public sealed record QuoteLine(Guid EventId, int TicketCount);
public sealed record QuoteRequest(IReadOnlyList<QuoteLine> Lines, string PromoCode);
public sealed record PricedLine(Guid EventId, string EventName, int TicketCount, decimal UnitPrice);
public sealed record OrderQuote(Guid QuoteId, DateTimeOffset ExpiresAt, IReadOnlyList<PricedLine> Lines,
    string Currency, string PromoCode, decimal Subtotal, decimal Discount, decimal AdministrationFee, decimal Total);
public sealed record OrderReceipt(Guid OrderId, DateTimeOffset SubmittedAt, OrderQuote Quote, string Status);
