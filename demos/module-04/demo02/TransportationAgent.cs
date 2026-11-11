using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OpenAI.Responses;
#pragma warning disable OPENAI001 // The pinned SDK marks the Responses API as experimental.

namespace ModuleAgent;

internal static class TransportationAgent
{
    public static AIAgent Create(ResponsesClient responsesClient, string model)
    {
        AIFunction findRides = AIFunctionFactory.Create(
            RideInformationSystemService.GetAvailableRides,
            "get_available_rides");
        AIFunction bookRide = AIFunctionFactory.Create(
            RideInformationSystemService.BookARide,
            "book_ride");

        return responsesClient.AsAIAgent(
            model: model,
            name: "TransportationAgent",
            description: "Finds transportation from the selected hotel to the concert.",
            instructions: """
                Use the hotel booked by a previous agent or provided by the user.
                Find available rides from and to the hotel and the venue where a concert takes place.
                Choose an affordable option, and call book_ride the moment the ride is confirmed by the user. 
                Summarize the complete itinerary, when done.
                """,
            tools: [findRides, bookRide]);
    }
}
