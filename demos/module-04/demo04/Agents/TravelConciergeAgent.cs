using System.ComponentModel;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Module04.Demo05.Models;
using Module04.Demo05.Tools;
using OpenAI.Responses;
#pragma warning disable OPENAI001 // The pinned SDK marks the Responses API as experimental.

namespace Module04.Demo05.Agents;

public static class TravelConciergeAgent
{
    public const string CompletionMarker = "TRAVEL_ITINERARY_COMPLETE";

    public static AIAgent Create(ResponsesClient responsesClient, string model, IEnumerable<AITool> specialistTools) =>
        responsesClient.AsAIAgent(
            model: model,
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

    public static AIAgent CreateForGroupChat(ResponsesClient responsesClient, string model) =>
        responsesClient.AsAIAgent(
            model: model,
            name: "TravelConciergeAgent",
            description: "Coordinates the group chat and books the selected hotel and ride with user approval.",
            instructions: """
                Coordinate the concert trip using the ticket, hotel and ride information shared by the
                other agents. Never invent ticket details or options. If information is missing, call
                ask_user and share the answer with the group. When both a hotel and ride are available,
                call book_itinerary with their IDs, city and event date. The tool asks the user for
                approval before booking. Report the result accurately. When booking succeeds or the user
                cancels, end your response with the exact marker TRAVEL_ITINERARY_COMPLETE on its own
                line. If booking fails, explain the failure and do not claim it succeeded.
                """,
            tools:
            [
                AIFunctionFactory.Create(ConversationFunctions.AskUser, "ask_user"),
                AIFunctionFactory.Create(BookGroupChatItinerary, "book_itinerary")
            ]);

    [Description("Shows the selected hotel and ride and books them only after user approval.")]
    private static string BookGroupChatItinerary(
        [Description("The selected hotel room identifier.")] int roomId,
        [Description("The selected ride identifier.")] int rideId,
        [Description("The city to search for the hotel and ride.")] string city,
        [Description("The concert date.")] DateTime eventDate)
    {
        var room = HotelBookingFunctions.FindAvailableRooms(city, eventDate)
            .FirstOrDefault(option => option.RoomId == roomId);
        var ride = RideInformationSystemService.GetAvailableRides(city, eventDate)
            .FirstOrDefault(option => option.RideId == rideId);
        if (room is null || ride is null)
        {
            return "Booking failed: the selected hotel or ride is not available in the specified city.";
        }

        Console.WriteLine($"\nHotel: {room.HotelName}, {room.RoomType}, {room.PricePerNight} per night (room {room.RoomId})");
        Console.WriteLine($"Ride: {ride.ServiceName}, {ride.RideType}, {ride.Price} (ride {ride.RideId})");
        Console.Write($"Book both in {city} for {eventDate:d}? [y/N] ");
        string? confirmation = Console.ReadLine()?.Trim();
        if (!string.Equals(confirmation, "y", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(confirmation, "yes", StringComparison.OrdinalIgnoreCase))
        {
            return "Booking cancelled by the user; no hotel or ride was booked.";
        }

        return BookItinerary(roomId, rideId, city);
    }

    [Description("Books the selected hotel and ride after the user approves the complete itinerary.")]
    private static string BookItinerary(
        [Description("The hotel room identifier.")] int roomId,
        [Description("The ride identifier.")] int rideId,
        [Description("The concert city.")] string city) =>
        HotelBookingFunctions.BookSelectedRoom(roomId) && RideInformationSystemService.BookARide(rideId)
            ? $"Booked room {roomId} and ride {rideId} in {city}."
            : "The itinerary could not be booked.";
}
