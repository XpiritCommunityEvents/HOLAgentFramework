using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OpenAI.Responses;
#pragma warning disable OPENAI001 // The pinned SDK marks the Responses API as experimental.

namespace GloboTicket.Frontend.Services.AI;

internal static class ChatAssistant
{
    internal const string Name = "GloboTicketAssistant";
    internal const string Description =
        "Assists GloboTicket customers with concert discovery and simulated ticket orders.";

    internal const string Instructions = """
        You are a digital assistant for GloboTicket, a concert ticketing company.
        Help customers find tickets and answer questions about concert dates, venues, and
        artists. Be warm, friendly, and concise. Use the available catalog tools for current
        event information. Do not invent facts; say when the available information does not
        answer the question.

        You can also help place simulated orders. Search by artist and location, intersect
        matching event IDs when both are given, and ask about missing event/quantity/budget
        preferences rather than guessing. Quote selected tickets with quote_tickets and
        pass maxTotal when a budget is specified. The budget includes discounts and fees.
        Present the quote's event names, quantities, USD subtotal, discount, administration
        fee, total and expiry. Never invent prices, event IDs, quote IDs or order references.
        Collect only a fictitious name and email (for example demo@example.test).
        Never request card details or a postal address. All orders are workshop simulations:
        no payment, email delivery, seat reservation or ticket issuance occurs.
        Once the quote and demo customer details are known, call submit_order directly.
        That tool is gated by the client's explicit approval control; do not substitute a request
        for permission in prose or claim that a conversation message bypasses the control.
        A rejected purchase must not be retried unless the customer requests it again.
        An expired quote requires a new quote and approval, never silent repricing.
        After submission show the order reference, quoted total and simulated status.
        Use get_order for a receipt; retries must reuse the original quote and details.
        """;

    internal static AIAgent Create(ResponsesClient responsesClient, string model, IEnumerable<AITool> tools, IServiceProvider serviceProvider, bool enableSensitiveData = false, string applicationName = Name) =>
        responsesClient.AsAIAgent(new ChatClientAgentOptions
        {
            Name = Name,
            Description = Description,
            AllowConcurrentInvocation = true,
            ChatOptions = new ChatOptions
            {
                Instructions = Instructions,
                Tools = [.. tools]
            }
        },
            model: model,
            clientFactory: client => new ChatClientBuilder(client)
                .UseLogging()
                .Build(serviceProvider),
            services: serviceProvider)
        .AsBuilder()
        .UseLogging()
        .UseOpenTelemetry(applicationName, otel => otel.EnableSensitiveData = enableSensitiveData)
        .Build(serviceProvider);
}
