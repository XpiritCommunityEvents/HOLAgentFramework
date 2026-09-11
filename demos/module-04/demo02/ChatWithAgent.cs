using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;

namespace ModuleWorkflow;

internal sealed class ChatWithAgent(AIAgent hotelAgent, AIAgent rideAgent)
{
    private const string TravelRequest = """
        I am going to a concert at Seattle Kraken Stadium at 7:30 PM on November 20.
        Book a hotel in Seattle, then arrange a ride from the hotel to the concert.
        """;

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        var workflow = AgentWorkflowBuilder.BuildSequential(hotelAgent, rideAgent);

        Console.WriteLine("Sequential Workflow: HotelReservationAgent -> TransportationAgent\n");

        await using StreamingRun run = await InProcessExecution
            .RunStreamingAsync(workflow, new ChatMessage(ChatRole.User, TravelRequest), cancellationToken: cancellationToken);

        // Must send the turn token to trigger the agents.
        // The agents are wrapped as executors. When they receive messages,
        // they will cache the messages and only start processing when they receive a TurnToken.
        await run.TrySendMessageAsync(new TurnToken(emitEvents: true));

        string? currentAgent = null;
        await foreach (var workflowEvent in run.WatchStreamAsync(blockOnPendingRequest: true, cancellationToken: cancellationToken))
        {
            switch (workflowEvent)
            {
                // An agent has something to say
                case AgentResponseUpdateEvent update when !string.IsNullOrEmpty(update.Update.Text):
                    if (update.Update.AuthorName != currentAgent)
                    {
                        currentAgent = update.Update.AuthorName;
                        Console.WriteLine();
                        Console.WriteLine();
                        Console.Write($"\n{currentAgent}: ");
                    }
                    Console.Write(update.Update.Text);
                    break;

                // An agent needs input (tool approval)
                case RequestInfoEvent request
                when request.Request.TryGetDataAs<ToolApprovalRequestContent>(out var approval) && approval is not null:
                    bool approved = AskForApproval(approval);
                    var response = request.Request.CreateResponse(approval.CreateResponse(approved, reason: approved ? "Approved." : "Rejected."));
                    await run.SendResponseAsync(response);
                    break;

                // Something went wrong
                case WorkflowErrorEvent error:
                    Console.WriteLine(error.Exception);
                    break;
            }
        }
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
