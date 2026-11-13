using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.Json.Serialization;
using GloboTicket.Ordering.Services;
using Microsoft.Extensions.AI;
using ModelContextProtocol;
using ModelContextProtocol.Server;

namespace GloboTicket.Ordering.MCP;

[McpServerResourceType, McpServerPromptType, McpServerToolType]
public sealed class OrderingContext(OrderingService ordering)
{
    [McpServerResource(UriTemplate = "ordering://policy", MimeType = "text/plain")]
    [Description("Pricing, approval and simulation rules for the GloboTicket workshop.")]
    public static string Policy() => """
        Trusted workshop demonstration. Use fictitious name/email only; never supply payment details.
        No payment, email delivery, seat reservation or ticket issuance occurs.
        Quotes are in USD, valid for 15 minutes, and contain 1-20 distinct events with 1-100 tickets each.
        SAVE10, SAVE15 and SAVE25 discount current catalog prices, including special offers.
        Discounts round per event line to cents; the administration fee is 5% of the discounted
        subtotal when that subtotal is below USD 500, rounded to cents (midpoints to even).
        The final quote total includes all fees. An optional budget applies to that final total.
        The client authorizes the exact quote and customer details before submitting.
        Interactive clients should show an explicit approval control; headless clients may apply a predefined policy.
        Expired quotes require a new quote and approval. A quote creates at most one order;
        repeating its submission with the same customer returns the original order, even after expiry.
        Changed customer details on a previously submitted quote produce a conflict.
        Order references are not authentication or authorization. Do not expose this service to untrusted users.
        """;

    [McpServerResource(UriTemplate = "ordering://orders/{orderId}", MimeType = "application/json")]
    [Description("Persisted simulated order receipt without customer contact details.")]
    public async Task<string> Receipt(string orderId, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(orderId, out var id) || id == Guid.Empty)
            throw new McpException("Supply a non-empty order UUID.");
        try { return JsonSerializer.Serialize(await ordering.GetOrderAsync(id, cancellationToken)); }
        catch (OrderException exception) { throw new McpException(exception.Message); }
    }

    [McpServerPrompt(Name = "guided_checkout")]
    [Description("Guide a simulated checkout from a catalog event ID, ticket count and total USD budget.")]
    public static ChatMessage GuidedCheckout(string eventId, string ticketCount, string budgetUsd) =>
        new(ChatRole.User, $"""
            Help me order {ticketCount} tickets for catalog event {eventId}, within USD {budgetUsd} including fees.
            Read ordering://policy. Get an authoritative quote with quote_tickets and show its full breakdown and expiry.
            Ask for a fictitious name/email or, if your client supports it, use collect_demo_customer.
            Obtain client approval for the exact quoted purchase before submit_order.
            Never ask for card details. Show the persisted reference and receipt and explain that the order is simulated.
            """);

    [McpServerTool(ReadOnly = true, Destructive = false, OpenWorld = false, UseStructuredContent = true)]
    [Description("Optionally request fictitious customer name/email through MCP form elicitation. This does not authorize or submit an order. Clients without form support should supply details directly to submit_order.")]
    public static async Task<DemoCustomer> CollectDemoCustomer(McpServer server, CancellationToken cancellationToken)
    {
        if (server.ClientCapabilities?.Elicitation?.Form is null)
            throw new McpException("Client does not support form elicitation. Supply fictitious name/email directly to submit_order.");
        var result = await server.ElicitAsync<DemoCustomer>(
            "Workshop checkout: enter a fictitious name and email only. No payment or order submission occurs here.",
            cancellationToken: cancellationToken);
        if (!result.IsAccepted || result.Content is null)
            throw new McpException($"Customer detail collection was {result.Action}. No order was submitted.");
        if (!Validator.TryValidateObject(result.Content, new ValidationContext(result.Content), [], validateAllProperties: true))
            throw new McpException("Supply a fictitious name and valid demo email address. No order was submitted.");
        return result.Content;
    }
}

public sealed class DemoCustomer
{
    [Required, StringLength(120), JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [Required, EmailAddress, StringLength(254), JsonPropertyName("email")]
    public string Email { get; set; } = "";
}
