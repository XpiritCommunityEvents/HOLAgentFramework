using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OpenAI.Responses;
#pragma warning disable OPENAI001 // The pinned SDK marks the Responses API as experimental.

namespace GloboTicket.Frontend.Services.AI;

internal static class ChatAssistant
{
    internal const string Name = "GloboTicketAssistant";
    internal const string Description =
        "Assists GloboTicket customers with concert and ticket questions.";

    internal const string Instructions = """
        You are a digital assistant for GloboTicket, a concert ticketing company.
        Help customers find tickets and answer questions about concert dates, venues, and
        artists. Be warm, friendly, and concise. Use the available catalog tools for current
        event information. Do not invent facts; say when the available information does not
        answer the question.
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
