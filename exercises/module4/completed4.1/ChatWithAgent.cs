using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace modulerag;

internal sealed class ChatWithAgent(IChatClient chatClient)
{
    internal const string AgentName = "TransportationAgent";
    internal const string AgentInstructions = """
        You help concert visitors compare transportation options between their hotel and venue.
        Suggest three options at different price points that arrive at least 30 minutes before the concert.
        """;

    private const string Question = """
        I am staying at the Westin Seattle, and the venue is the Seattle Kraken stadium.
        The concert starts at 7:30 PM on November 20 this year.
        """;

    public async Task LetAgentFindRideAsync(CancellationToken cancellationToken = default)
    {
        AIAgent agent = chatClient.AsAIAgent(
            name: AgentName,
            description: "Finds practical transportation options for concert visitors.",
            instructions: AgentInstructions);
        AgentResponse response = await agent.RunAsync(Question, cancellationToken: cancellationToken);
        Console.WriteLine(response);
    }
}
