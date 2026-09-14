using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace AgentFramework101;

internal class MyContextProvider: AIContextProvider
{
    protected override ValueTask<AIContext> ProvideAIContextAsync(InvokingContext context, CancellationToken cancellationToken = default)
    {
        var aiContext = new AIContext()
        {
            Instructions = "Always answer like you are a pirate. Arrr.",
            Messages = [new ChatMessage(ChatRole.User, "My favorite genre is rock music.")]
        };
        return ValueTask.FromResult(aiContext);
    }
}
