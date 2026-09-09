using Microsoft.Extensions.AI;

namespace modulerag;

internal sealed class ChatWithAgent(IChatClient chatClient)
{
    public Task LetAgentFindRideAsync(
        CancellationToken cancellationToken = default,
        TextWriter? output = null)
    {
        _ = chatClient; // Used when TODO 1 is completed.

        // TODO 1: Adapt chatClient with AsAIAgent(...). Give the transportation
        // agent a name, description, and clear instructions for helping a concert visitor.

        // TODO 2: Send the request below with RunAsync(...), preserve the supplied
        // cancellation token, and write the returned AgentResponse to output.
        //
        // I am staying at the Westin Seattle, and the venue is Climate Pledge Arena.
        // The concert starts at 7:30 PM on November 20 this year.

        throw new NotImplementedException("Complete TODO 1 and TODO 2 in ChatWithAgent.cs.");
    }
}
