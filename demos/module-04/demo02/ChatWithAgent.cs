using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;

namespace modulerag;

internal sealed class ChatWithAgent(IChatClient chatClient)
{
    private const string TravelRequest = """
        I am going to a concert at Seattle Kraken Stadium at 7:30 PM on November 20.
        Book a hotel in Seattle, then arrange a ride from the hotel to the concert.
        """;

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        AIAgent hotelAgent = HotelBookingAgent.Create(chatClient);
        AIAgent rideAgent = CreateTransportationAgent();

        Workflow workflow = AgentWorkflowBuilder.BuildSequential(hotelAgent, rideAgent);

        Console.WriteLine("Workflow: HotelReservationAgent -> TransportationAgent\n");

        await using StreamingRun run = await InProcessExecution
            .OpenStreamingAsync(workflow, cancellationToken: cancellationToken);

        await run.TrySendMessageAsync(TravelRequest);

        string? currentAgent = null;
        await foreach (WorkflowEvent workflowEvent in run.WatchStreamAsync(cancellationToken))
        {
            switch (workflowEvent)
            {
                case WorkflowStartedEvent workflowStartedEvent:
                    Console.WriteLine($"Workflow started: {workflowStartedEvent.Data}");
                    break;
                case SuperStepEvent superStepEvent:
                    Console.WriteLine($"Super step event: {superStepEvent.StepNumber} {superStepEvent.Data}");
                    break;
                case ExecutorEvent executorEvent:
                    Console.WriteLine($"Executor event: {executorEvent.ExecutorId} {executorEvent.Data}");
                    break;
                case AgentResponseUpdateEvent update when !string.IsNullOrEmpty(update.Update.Text):
                    if (update.Update.AuthorName != currentAgent)
                    {
                        currentAgent = update.Update.AuthorName;
                        Console.Write($"\n{currentAgent}: ");
                    }

                    Console.Write(update.Update.Text);
                    break;

                case RequestInfoEvent request
                    when request.Request.TryGetDataAs<ToolApprovalRequestContent>(out var approval)
                         && approval is not null:
                    bool approved = AskForApproval(approval);
                    await run.SendResponseAsync(request.Request.CreateResponse(
                        approval.CreateResponse(approved, approved ? "Approved." : "Rejected.")));
                    break;

                case WorkflowOutputEvent:
                    Console.WriteLine("\n\nWorkflow complete.");
                    return;

                case WorkflowErrorEvent error:
                    throw new InvalidOperationException("The travel workflow failed.", error.Exception);
            }
        }
    }

    private AIAgent CreateTransportationAgent()
    {
        AIFunction findRides = AIFunctionFactory.Create(
            RideInformationSystemService.GetAvailableRides,
            "get_available_rides");
        AIFunction bookRide = new ApprovalRequiredAIFunction(AIFunctionFactory.Create(
            RideInformationSystemService.BookRide,
            "book_ride"));

        return chatClient.AsAIAgent(
            name: "TransportationAgent",
            description: "Finds transportation from the selected hotel to the concert.",
            instructions: """
                Use the hotel selected by the previous agent. Find available rides in Seattle,
                choose an affordable option, and call book_ride. Summarize the complete itinerary.
                """,
            tools: [findRides, bookRide]);
    }

    private static bool AskForApproval(ToolApprovalRequestContent approval)
    {
        string toolName = approval.ToolCall is FunctionCallContent functionCall
            ? functionCall.Name
            : "the requested action";

        Console.Write($"\nApprove {toolName}? [y/N] ");
        string? answer = Console.ReadLine();
        return answer?.Equals("y", StringComparison.OrdinalIgnoreCase) is true;
    }
}
