using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace ModuleWorkflow;

internal static class TransportationAgent
{
    public static AIAgent Create(IChatClient chatClient)
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
}
