using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;

namespace modulerag;

internal sealed class ChatWithAgent(IChatClient chatClient)
{
    private const string TravelRequest = """
        I am going to a concert at Seattle Kraken Stadium at 7:30 PM on November 20.
        Recommend a hotel in Seattle, then arrange transportation from that hotel to the concert.
        """;

    public async Task RunAsync()
    {
        AIAgent hotelAgent = chatClient.AsAIAgent(
            name: "HotelAgent",
            instructions: "Recommend a Seattle hotel near the venue. End with the selected hotel's name and address.");

        AIAgent transportationAgent = chatClient.AsAIAgent(
            name: "TransportationAgent",
            instructions: "Use the hotel selected by the previous agent to recommend transportation to the concert. Summarize the complete itinerary.");

#pragma warning disable MAAIW001 // Sequential workflows are the subject of this exercise.
        Workflow workflow = AgentWorkflowBuilder.BuildSequential(hotelAgent, transportationAgent);
#pragma warning restore MAAIW001

        Console.WriteLine("Workflow: HotelAgent -> TransportationAgent\n");

        await using StreamingRun run = await InProcessExecution.OpenStreamingAsync(workflow);
        if (!await run.TrySendMessageAsync(TravelRequest))
        {
            throw new InvalidOperationException("The workflow did not accept the travel request.");
        }

        await foreach (WorkflowEvent workflowEvent in run.WatchStreamAsync())
        {
            if (workflowEvent is AgentResponseUpdateEvent update)
            {
                Console.Write(update.Update.Text);
            }
            else if (workflowEvent is WorkflowOutputEvent)
            {
                Console.WriteLine("\n\nWorkflow complete.");
                return;
            }
            else if (workflowEvent is WorkflowErrorEvent error)
            {
                throw new InvalidOperationException("The travel workflow failed.", error.Exception);
            }
        }
    }
}
