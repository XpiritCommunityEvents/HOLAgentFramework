using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace modulerag;

internal class HotelBookingAgent
{
    public static AIAgent CreateChatCompletionAgent(IChatClient chatClient)
    {
        return chatClient.AsAIAgent(
            name: "HotelReservationAgent",
            instructions: """
                You are an expert in finding hotel rooms close to music concert locations.
                You provide some options what you have found and ask for approval before you 
                make the booking. You always suggest 3 options with different price ranges.
                You will ask for approval before you make the booking. 
                You are not allowed to make a booking without user confirmation!

                After you succesfully booked the ride you will respond with [** GOAL REACHED **] in your message.            
                """,
            description: "An agent that finds and books a hotel room close to the concert location",
            tools: [AIFunctionFactory.Create(HotelBookingFunctions.SelectRoomPreference),
                    AIFunctionFactory.Create(HotelBookingFunctions.BookSelectedRoom),
                    AIFunctionFactory.Create(HotelBookingFunctions.GetApprovalForBooking)]
            );
    }
}
