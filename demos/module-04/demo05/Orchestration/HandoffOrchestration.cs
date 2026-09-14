using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Module04.Demo05.Agents;

namespace Module04.Demo05.Orchestration;

public static class HandoffOrchestration
{
    public static async Task RunAsync(
        string request,
        AIAgent concierge,
        AIAgent ticketAgent,
        AIAgent hotelAgent,
        AIAgent rideAgent,
        AIAgent itineraryAgent)
    {
        Workflow workflow = AgentWorkflowBuilder
            .CreateHandoffBuilderWith(concierge)
            .WithHandoff(concierge, ticketAgent, "Read the concert ticket and return verified details.")
            .WithHandoff(ticketAgent, concierge, "Return the ticket details to the concierge.")
            .WithHandoff(concierge, hotelAgent, "Find hotel options after the ticket details are known.")
            .WithHandoff(hotelAgent, concierge, "Return the recommended hotel.")
            .WithHandoff(concierge, rideAgent, "Find a ride after the hotel is selected.")
            .WithHandoff(rideAgent, concierge, "Return the recommended ride.")
            .WithHandoff(concierge, itineraryAgent, "Create the PDF itinerary after booking.")
            .WithHandoff(itineraryAgent, concierge, "Return the generated PDF path.")
            .WithAutonomousMode(turnLimit: 12)
            .WithTerminationCondition(messages => messages.Any(message =>
                message.Text?.Contains(TravelConciergeAgent.CompletionMarker, StringComparison.Ordinal) is true))
            .Build();

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
