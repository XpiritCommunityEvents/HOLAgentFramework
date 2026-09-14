using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;

namespace Module04.Demo05.Orchestration;

public static class SequentialOrchestration
{
    public static async Task RunAsync(
        string request,
        AIAgent ticketAgent,
        AIAgent hotelAgent,
        AIAgent rideAgent,
        AIAgent itineraryAgent)
    {
        Workflow workflow = AgentWorkflowBuilder.BuildSequential(
            ticketAgent,
            hotelAgent,
            rideAgent,
            itineraryAgent);
        await using StreamingRun run = await InProcessExecution.OpenStreamingAsync(workflow);
        await run.TrySendMessageAsync(request);
        await run.TrySendMessageAsync(new TurnToken(emitEvents: true));

        await foreach (WorkflowEvent evt in run.WatchStreamAsync())
        {
            if (evt is AgentResponseUpdateEvent update)
            {
                Console.Write(update.Update.Text);
            }
        }
    }
}
