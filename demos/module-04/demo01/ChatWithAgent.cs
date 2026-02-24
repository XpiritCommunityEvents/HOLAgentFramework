using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace modulerag;

internal class ChatWithAgent(IChatClient chatClient)
{
    public async Task LetAgentFindRide()
    {
        var question = """
        I stay at the WestIn Seattle and the venue is the Seattle Kraken stadium.
        the Concert starts at 7:30 pm and is November 20th this year. 
        """;

        Console.WriteLine("******** Create the agent ***********");
        var transportationAgent = CreateTransportationAgent();

        Console.WriteLine("******** Start the agent ***********");
        var session = await transportationAgent.CreateSessionAsync();

        Console.WriteLine("******** RESPONSE ***********");
        await foreach (var update in transportationAgent.RunStreamingAsync(question, session))
        {
            Console.Write(update);
        }

        Console.WriteLine();
    }

    private AIAgent CreateTransportationAgent()
    {
        var instructions = """
            You are an expert in finding transportation options from a given hotel location to the concert location.
            You will try to get the best options available for an afordable price. Make sure the customer will be there at least 30 minutes
            before the concert starts at the venue. You always suggest 3 options with different price ranges.
            You will ask for approval before you make the booking
            """;

        return chatClient.AsAIAgent(
                name: "TransportationAgent",
                description: "An agent that finds transportation options for the user from their hotel to the concert venue.",
                instructions: instructions
            );
    }
}
