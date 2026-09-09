using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace modulerag;

internal sealed class ChatWithAgent
{
    private readonly IChatClient chatClient;

    public ChatWithAgent(IChatClient chatClient) => this.chatClient = chatClient;

    public async Task LetAgentFindRideAsync()
    {
        const string question = """
            I stay at the Westin Seattle and the venue is Seattle Kraken Stadium.
            The concert starts at 7:30 pm on November 20 this year.
            Find suitable rides and book the option you recommend.
            """;

        AIAgent transportationAgent = CreateTransportationAgent();
        AgentSession session = await transportationAgent.CreateSessionAsync();

        AgentResponse response = await transportationAgent.RunAsync(question, session);
        Console.WriteLine(response);

        ToolApprovalRequestContent? approval = response.Messages
            .SelectMany(message => message.Contents)
            .OfType<ToolApprovalRequestContent>()
            .FirstOrDefault();

        if (approval is null)
        {
            return;
        }

        Console.Write($"Approve {approval.ToolCall}? [y/N] ");
        bool approved = Console.ReadLine()?.Equals("y", StringComparison.OrdinalIgnoreCase) is true;

        AgentResponse finalResponse = await transportationAgent.RunAsync(
            new ChatMessage(ChatRole.User,
                [approval.CreateResponse(approved, approved ? "Approved." : "Rejected.")]),
            session);

        Console.WriteLine(finalResponse);
    }

    private AIAgent CreateTransportationAgent()
    {
        var rideService = new RideInformationSystemService();
        AIFunction findRides = AIFunctionFactory.Create(
            rideService.GetAvailableRides,
            "get_available_rides",
            "Get available rides in a city for a given date.");
        AIFunction bookRide = new ApprovalRequiredAIFunction(AIFunctionFactory.Create(
            rideService.BookARide,
            "book_a_ride",
            "Book a selected ride."));

        return chatClient.AsAIAgent(
            name: "TransportationAgent",
            description: "Finds transportation from a hotel to a concert venue and books an approved ride.",
            instructions: """
                You are an expert in finding transportation from a hotel to a concert venue.
                Suggest up to three available options with different prices that arrive at least
                30 minutes before the concert. Explain the exact ride you recommend, then call
                book_a_ride. The framework will pause for human approval before booking executes.
                Never claim that a ride is booked unless the tool reports success.
                """,
            tools: [findRides, bookRide]);
    }
}
