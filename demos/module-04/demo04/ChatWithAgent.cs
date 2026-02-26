using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace modulerag;

internal class ChatWithAgent(IChatClient chatClient)
{
    public async Task LetAgentFindRideAndHotel()
    {
        var question =
        """
        I am going to a concert that is held at the Seattle Kraken Stadium. The Concert starts at 7:30 pm and is November 20th this year. 
        """;

        var userMessage = new ChatMessage(ChatRole.User, question);

        Console.WriteLine("******** Create the hotel agent ***********");
        var hotelAgent = HotelBookingAgent.CreateChatCompletionAgent(chatClient);

        Console.WriteLine("******** Create the ride agent ***********");
        var rideAgent = CreateTransportationAgent();

        var hotelSession = await hotelAgent.CreateSessionAsync();
        await RunUntilGoalReached(hotelAgent, [userMessage], hotelSession);

        var provider = hotelAgent.GetService<InMemoryChatHistoryProvider>();
        List<ChatMessage>? messages = provider?.GetMessages(hotelSession);

        Console.WriteLine("******** hotel agent done ***********");
        var rideSession = await rideAgent.CreateSessionAsync();

        await RunUntilGoalReached(rideAgent, messages, rideSession);

        Console.WriteLine("******** Done ***********");
    }

    private async Task RunUntilGoalReached(AIAgent agent, IEnumerable<ChatMessage> messages, AgentSession session)
    {
        var agentResponse = await agent.RunAsync(messages, session);

        PrintResult(agentResponse);
        while (!IsGoalReached(agentResponse))
        {
            var input = Console.ReadLine();
            if(string.IsNullOrEmpty(input))
            {
                continue;
            }

            agentResponse = await agent.RunAsync(input, session);

            PrintResult(agentResponse);
        }
    }

    public async Task LetAgentFindRide()
    {
        var question =
        """
        I stay at the Westin Seattle and the venue is the Seattle Kraken stadium.
        the Concert starts at 7:30 pm and is November 20th this year. 
        """;

        Console.WriteLine("******** Create the agent ***********");
        var transportationAgent = CreateTransportationAgent();

        Console.WriteLine("******** Start the agent ***********");
        var session = await transportationAgent.CreateSessionAsync();

        var agentResponse = await transportationAgent.RunAsync(question, session);

        Console.WriteLine("******** RESPONSE 1 ***********");
        PrintResult(agentResponse);

        while (!IsGoalReached(agentResponse))
        {
            var input = Console.ReadLine();

            agentResponse = await transportationAgent.RunAsync(input, session);

            Console.WriteLine("******** RESPONSE ***********");
            PrintResult(agentResponse);
        }
        Console.WriteLine("******** Terminating, goal reached ***********");
    }

    private bool IsGoalReached(AgentResponse agentResponse) => agentResponse.Text.Contains("[** GOAL REACHED **]");

    private AIAgent CreateTransportationAgent()
    {
        var instructions = """
            You are an expert in finding transportation options from a given hotel location to the concert location.
            You will try to get the best options available for an affordable price. Make sure the customer will be there at least 30 minutes before the concert starts at the venue.
            You always suggest 3 options with different price ranges.
            You will ask for approval before you make the booking. 
            You are not allowed to make a booking without user confirmation!

            After you successfully booked the ride you will respond with [** GOAL REACHED **] in your message.            
            """;

        return chatClient.AsAIAgent(
                name: "TransportationAgent",
                description: "An agent that finds transportation options for the user from their hotel to the concert venue.",
                instructions: instructions,
                tools: [AIFunctionFactory.Create(RideInformationSystemService.GetAvailableRides),
                        AIFunctionFactory.Create(RideInformationSystemService.BookARide)]
            );
    }

    private static void PrintResult(AgentResponse agentResponse)
    {
        foreach (var message in agentResponse.Messages)
        {
            //Console.WriteLine($"Thread: {session.Id}");
            //Console.WriteLine($"Thread data: {message.}");
            Console.WriteLine($"Author: {message.AuthorName}");
            Console.WriteLine($"Message:{message.Text}");
        }
    }
}