using System.ComponentModel;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;

namespace modulerag;

internal static class HandoffDemo
{
    private const string Done = "ITINERARY COMPLETE";

    public static async Task RunAsync(IChatClient chatClient)
    {
        AIAgent concierge = chatClient.AsAIAgent(
            name: "Concierge",
            description: "Coordinates the hotel and ride specialists.",
            instructions: $$"""
                First hand off to HotelAgent. When it returns a hotel, hand off to RideAgent.
                When both specialists have returned, explain the complete itinerary and call book_itinerary once.
                The application will ask the user to approve that booking. Whether it is approved or rejected,
                summarize the outcome and end with exactly {{Done}}.
                """,
            tools:
            [
                new ApprovalRequiredAIFunction(AIFunctionFactory.Create(
                    BookItinerary,
                    "book_itinerary",
                    "Book the selected hotel and ride."))
            ]);

        AIAgent hotelAgent = chatClient.AsAIAgent(
            name: "HotelAgent",
            description: "Recommends a hotel near the concert venue.",
            instructions: "Use find_hotels, recommend one hotel, then hand back to Concierge.",
            tools: [AIFunctionFactory.Create(FindHotels, "find_hotels", "Find hotels in a city.")]);

        AIAgent rideAgent = chatClient.AsAIAgent(
            name: "RideAgent",
            description: "Recommends a ride from the hotel to the concert venue.",
            instructions: "Use find_rides, recommend one ride, then hand back to Concierge.",
            tools: [AIFunctionFactory.Create(FindRides, "find_rides", "Find rides from a hotel to the concert venue.")]);

        Workflow workflow = AgentWorkflowBuilder
            .CreateHandoffBuilderWith(concierge)
            .WithHandoff(concierge, hotelAgent, "Choose the hotel first")
            .WithHandoff(hotelAgent, concierge, "Return the hotel choice")
            .WithHandoff(concierge, rideAgent, "Choose a ride after the hotel")
            .WithHandoff(rideAgent, concierge, "Return the ride choice")
            .WithAutonomousMode(turnLimit: 8)
            .WithTerminationCondition(messages =>
                messages.Any(message => message.Text?.Contains(Done, StringComparison.Ordinal) is true))
            .Build();

        const string request = """
            I am attending a concert at Seattle Kraken Stadium at 7:30 PM on November 20.
            Choose a nearby hotel and a ride to the concert, then book the itinerary.
            """;

        await using StreamingRun run = await InProcessExecution.OpenStreamingAsync(workflow);
        await run.TrySendMessageAsync(request);

        await run.TrySendMessageAsync(new TurnToken(emitEvents: true));

        await foreach (WorkflowEvent evt in run.WatchStreamAsync())
        {
            if (evt is AgentResponseUpdateEvent update)
            {
                Console.Write(update.Update.Text);
            }
            else if (evt is RequestInfoEvent requestEvent &&
                     requestEvent.Request.TryGetDataAs<ToolApprovalRequestContent>(out var approval) &&
                     approval is not null)
            {
                FunctionCallContent? call = approval.ToolCall as FunctionCallContent;
                string arguments = call?.Arguments is { Count: > 0 }
                    ? string.Join(", ", call.Arguments.Select(argument => $"{argument.Key}={argument.Value}"))
                    : "the proposed itinerary";

                Console.Write($"\nApprove {arguments}? [y/N] ");
                bool approved = Console.ReadLine()?.Trim().StartsWith("y", StringComparison.OrdinalIgnoreCase) is true;

                await run.SendResponseAsync(requestEvent.Request.CreateResponse(
                    approval.CreateResponse(approved, approved ? "Approved." : "Rejected.")));
            }
        }
    }

    private static string[] FindHotels([Description("The destination city.")] string city) =>
        city.Equals("Seattle", StringComparison.OrdinalIgnoreCase)
            ? ["Harbor Hotel — $160/night", "Market Inn — $210/night", "Lakeview Hotel — $260/night"]
            : [$"Central Hotel {city} — $180/night"];

    private static string[] FindRides([Description("The selected hotel.")] string hotel) =>
        [$"City Cab from {hotel} — $24", $"Metro Rides from {hotel} — $18"];

    private static string BookItinerary(
        [Description("The selected hotel.")] string hotel,
        [Description("The selected ride.")] string ride) =>
        $"Booked {hotel} and {ride}.";
}
