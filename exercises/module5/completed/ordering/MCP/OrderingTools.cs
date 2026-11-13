using System.ComponentModel;
using GloboTicket.Ordering.Model;
using GloboTicket.Ordering.Services;
using ModelContextProtocol;
using ModelContextProtocol.Server;

namespace GloboTicket.Ordering.MCP;

[McpServerToolType]
public sealed class OrderingTools(OrderingService ordering)
{
    [McpServerTool(ReadOnly = false, Destructive = false, Idempotent = false, UseStructuredContent = true)]
    [Description("Create a 15-minute USD quote for simulated tickets using catalog prices. Include the customer's total budget when given. This does not submit an order or reserve seats.")]
    public Task<OrderQuote> QuoteTickets(QuoteLine[] lines, string? promoCode = null, decimal? maxTotal = null,
        CancellationToken cancellationToken = default) =>
        Invoke(() => ordering.QuoteAsync(new(lines, promoCode, maxTotal), cancellationToken));

    [McpServerTool(ReadOnly = false, Destructive = false, Idempotent = true, UseStructuredContent = true)]
    [Description("Submit a simulated order for an existing quote. The client must authorize the exact quoted purchase. Requires fictitious name and email; never supply payment details. Retrying the same quote and customer returns the same order.")]
    public Task<OrderReceipt> SubmitOrder(Guid quoteId, string name, string email,
        CancellationToken cancellationToken = default) =>
        Invoke(() => ordering.SubmitAsync(new(quoteId, name, email), cancellationToken));

    [McpServerTool(ReadOnly = true, Destructive = false, Idempotent = true, UseStructuredContent = true)]
    [Description("Retrieve a simulated order receipt by order ID. Contains no customer contact details.")]
    public Task<OrderReceipt> GetOrder(Guid orderId, CancellationToken cancellationToken = default) =>
        Invoke(() => ordering.GetOrderAsync(orderId, cancellationToken));

    private static async Task<T> Invoke<T>(Func<Task<T>> action)
    {
        try { return await action(); }
        catch (OrderException exception) { throw new McpException(exception.Message); }
    }
}
