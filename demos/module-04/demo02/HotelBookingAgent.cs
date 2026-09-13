using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace ModuleAgent;

internal static class HotelBookingAgent
{
    public static AIAgent Create(IChatClient chatClient)
    {
        AIFunction findRooms = AIFunctionFactory.Create(
            HotelBookingFunctions.GetAvailableRooms,
            "get_available_rooms");
        AIFunction bookRoom = AIFunctionFactory.Create(
            HotelBookingFunctions.BookRoom,
            "book_room");

        return chatClient.AsAIAgent(
            name: "HotelReservationAgent",
            description: "Finds and books a hotel room near the concert.",
            instructions: """
                Find available rooms in based on the venue information or city information for a concert.
                Choose a suitable option, and call book_room.
                Mention the the selected hotel's name and city in your response so the next workflow stage can arrange a ride.
                """,
            tools: [findRooms, bookRoom]);
    }
}
