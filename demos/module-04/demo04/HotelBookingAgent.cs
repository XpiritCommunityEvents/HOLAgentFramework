using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace modulerag;

internal static class HotelBookingAgent
{
    public static AIAgent Create(IChatClient chatClient)
    {
        AIFunction findRooms = AIFunctionFactory.Create(
            HotelBookingFunctions.GetAvailableRooms,
            "get_available_rooms");
        AIFunction bookRoom = new ApprovalRequiredAIFunction(AIFunctionFactory.Create(
            HotelBookingFunctions.BookRoom,
            "book_room"));

        return chatClient.AsAIAgent(
            name: "HotelReservationAgent",
            description: "Finds and books a hotel room near the concert.",
            instructions: """
                Find available rooms in Seattle, choose a suitable option, and call book_room.
                Finish with the selected hotel's name so the next workflow stage can arrange a ride.
                """,
            tools: [findRooms, bookRoom]);
    }
}
