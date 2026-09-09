using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace modulerag;

internal static class HotelBookingAgent
{
    public static AIAgent Create(
        IChatClient chatClient,
        HotelBookingFunctions bookingFunctions)
    {
        AIFunction findRooms = AIFunctionFactory.Create(
            bookingFunctions.FindAvailableRooms,
            "find_available_rooms",
            "Find available hotel rooms in a city for a date.");

        AIFunction bookRoom = new ApprovalRequiredAIFunction(AIFunctionFactory.Create(
            bookingFunctions.BookRoom,
            "book_room",
            "Book a selected hotel room."));

        return chatClient.AsAIAgent(
            name: "HotelReservationAgent",
            description: "Finds hotel rooms near a concert and books an approved selection.",
            instructions: """
                Suggest up to three suitable rooms at different prices. Explain your recommendation,
                then call book_room. Never claim a booking succeeded before the tool returns success.
                """,
            tools: [findRooms, bookRoom]);
    }
}
