using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

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

    internal static AIAgent Create(IChatClient chatClient, IEnumerable<AITool> tools) =>
        chatClient.AsAIAgent(new ChatClientAgentOptions
        {
            Name = Name,
            Description = Description,
            AllowConcurrentInvocation = true,
            ChatOptions = new ChatOptions
            {
                Instructions = Instructions,
                Tools = tools.ToList()
            }
        });
}
