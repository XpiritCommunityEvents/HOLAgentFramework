using System.ComponentModel;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Module04.Demo05.Models;
using Module04.Demo05.Tools;

namespace Module04.Demo05.Agents;

public static class TravelConciergeAgent
{
    public const string CompletionMarker = "TRAVEL_ITINERARY_COMPLETE";

    public static AIAgent Create(IChatClient chatClient, IEnumerable<AITool> specialistTools) =>
        chatClient.AsAIAgent(
            name: "TravelConciergeAgent",
            description: "Coordinates ticket reading, hotel and ride selection, and final booking.",
            instructions: """
                You orchestrate a concert trip. Delegate ticket reading, hotel search, and ride search to
                the specialist agents. If information is missing, call ask_user; never ask a question in
                normal text. Present the selected hotel and ride, then call book_itinerary exactly once.
                Never invent ticket details. After booking, ask the itinerary specialist to create a PDF.
                When the PDF is created or the user cancels, end your response with the exact marker
                TRAVEL_ITINERARY_COMPLETE on its own line. Do not end before that marker.
                """,
            tools: specialistTools
                .Append(AIFunctionFactory.Create(ConversationFunctions.AskUser, "ask_user"))
                .Append(new ApprovalRequiredAIFunction(AIFunctionFactory.Create(BookItinerary, "book_itinerary")))
                .ToArray());

    [Description("Books the selected hotel and ride after the user approves the complete itinerary.")]
    private static string BookItinerary(
        [Description("The hotel room identifier.")] int roomId,
        [Description("The ride identifier.")] int rideId,
        [Description("The concert city.")] string city) =>
        HotelBookingFunctions.BookSelectedRoom(roomId) && RideInformationSystemService.BookARide(rideId)
            ? $"Booked room {roomId} and ride {rideId} in {city}."
            : "The itinerary could not be booked.";
}
