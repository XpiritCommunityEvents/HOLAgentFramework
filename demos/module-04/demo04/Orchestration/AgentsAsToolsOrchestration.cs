using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using ChatMessage = Microsoft.Extensions.AI.ChatMessage;
using Module04.Demo05.Agents;

namespace Module04.Demo05.Orchestration;

public static class AgentsAsToolsOrchestration
{
    private const int MaxTurns = 12;

    public static async Task RunAsync(AIAgent concierge, string request)
    {
        AgentSession session = await concierge.CreateSessionAsync();
        List<ChatMessage> input = [new ChatMessage(ChatRole.User, request)];

        for (int turn = 0; turn < MaxTurns && input.Count > 0; turn++)
        {
            var updates = new List<AgentResponseUpdate>();
            await foreach (AgentResponseUpdate update in concierge.RunStreamingAsync(input, session))
            {
                Console.Write(update.Text);
                updates.Add(update);
            }

            AgentResponse response = updates.ToAgentResponse();
            if (response.Messages.Any(message =>
                    message.Text?.Contains(TravelConciergeAgent.CompletionMarker, StringComparison.Ordinal) is true))
            {
                return;
            }

            var approvals = response.Messages
                .SelectMany(message => message.Contents)
                .OfType<ToolApprovalRequestContent>()
                .ToList();

            List<AIContent> replies = [];
            foreach (ToolApprovalRequestContent approval in approvals)
            {
                Console.Write($"\nApprove final itinerary booking? [y/N] ");
                bool approved = Console.ReadLine()?.Trim().StartsWith("y", StringComparison.OrdinalIgnoreCase) is true;
                replies.Add(approval.CreateResponse(approved, approved ? "Approved." : "Rejected."));
            }

            input = approvals.Count > 0
                ? [new ChatMessage(ChatRole.User, replies)]
                : [new ChatMessage(ChatRole.User, "Continue the orchestration. Use ask_user for any question and do not finish until the completion marker is emitted.")];
        }

        throw new InvalidOperationException($"The concierge did not emit {TravelConciergeAgent.CompletionMarker} within {MaxTurns} turns.");
    }
}
