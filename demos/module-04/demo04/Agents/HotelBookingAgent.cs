using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Module04.Demo05.Tools;

namespace Module04.Demo05.Agents;

public static class HotelBookingAgent
{
    public static AIAgent Create(IChatClient chatClient) => chatClient.AsAIAgent(
        name: "HotelBookingAgent",
        description: "Finds suitable hotel rooms near a concert city.",
        instructions: """
            You are a hotel search specialist. You always use the function find_available_rooms for the city and event date, to find rooms.
            Return exactly one JSON object containing the three best options and a recommended RoomId.
            You only search and recommend; you never book, ask for approval, or ask the user questions.
            """,
        tools: [AIFunctionFactory.Create(HotelBookingFunctions.FindAvailableRooms, "find_available_rooms")]);
}
