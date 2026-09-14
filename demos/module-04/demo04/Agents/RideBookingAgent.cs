using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Module04.Demo05.Tools;

namespace Module04.Demo05.Agents;

public static class RideBookingAgent
{
    public static AIAgent Create(IChatClient chatClient) => chatClient.AsAIAgent(
        name: "RideBookingAgent",
        description: "Finds suitable rides in the concert city.",
        instructions: """
            You are a ride search specialist. Use find_available_rides for the city and event date.
            Return exactly one JSON object containing the available options and a recommended RideId.
            You only search and recommend; you never book, ask for approval, or ask the user questions.
            """,
        tools: [AIFunctionFactory.Create(RideInformationSystemService.GetAvailableRides, "find_available_rides")]);
}
