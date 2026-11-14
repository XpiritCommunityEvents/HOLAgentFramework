using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Module04.Demo05.Agents;

namespace Module04.Demo05.Orchestration;

public static class GroupChatOrchestration
{
    public static async Task RunAsync(
        string request,
        AIAgent ticketAgent,
        AIAgent hotelAgent,
        AIAgent rideAgent,
        AIAgent concierge)
    {
        Workflow workflow = AgentWorkflowBuilder
            .CreateGroupChatBuilderWith(agents => new RoundRobinGroupChatManager(
                agents,
                (_, messages, _) => ValueTask.FromResult(messages.Any(message =>
                    message.Text?.Contains(TravelConciergeAgent.CompletionMarker, StringComparison.Ordinal) is true)))
            {
                MaximumIterationCount = 16
            })
            .AddParticipants([ticketAgent, hotelAgent, rideAgent, concierge])
            .Build();

        await using StreamingRun run = await InProcessExecution.OpenStreamingAsync(workflow);
        await run.TrySendMessageAsync(request);
        await run.TrySendMessageAsync(new TurnToken(emitEvents: true));

        var output = new System.Text.StringBuilder();
        string? speaker = null;
        await foreach (WorkflowEvent evt in run.WatchStreamAsync())
        {
            if (evt is AgentResponseUpdateEvent update)
            {
                if (!string.IsNullOrEmpty(update.Update.Text) && speaker != update.ExecutorId)
                {
                    speaker = update.ExecutorId;
                    Console.WriteLine($"\n[{speaker}]");
                }

                Console.Write(update.Update.Text);
                output.Append(update.Update.Text);
            }
            else if (evt is WorkflowErrorEvent error)
            {
                throw new InvalidOperationException("The group chat workflow failed.", error.Exception);
            }
        }

        if (!output.ToString().Contains(TravelConciergeAgent.CompletionMarker, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("The group chat ended without completing or cancelling the booking.");
        }
    }
}
