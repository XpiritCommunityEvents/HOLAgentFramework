using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;

namespace modulerag;

internal class ChatWithAgent
{

    public async Task LetAgentFindRide(Kernel kernel)
    {
        var question = """
        I stay at the WestIn Seattle and the venue is the Seattle Kraken stadium.
        the Concert starts at 7:30 pm and is November 20th this year. 
        """;

        Console.WriteLine("******** Create the agent ***********");
        var transportationAgent = CreateTransportationAgent(kernel);

        Console.WriteLine("******** Start the agent ***********");
        var agentresult = transportationAgent.InvokeAsync(question);

        Console.WriteLine("******** RESPONSE ***********");
        await PrintResult(agentresult);
    }


    private static async Task PrintResult(IAsyncEnumerable<AgentResponseItem<ChatMessageContent>> agentResponse)
    {
        await
        foreach (var item in agentResponse)
        {
            Console.WriteLine($"Thread: {item.Thread.Id}");
            Console.WriteLine($"Thread data: {item.Thread.ToString()}");
            Console.WriteLine($"Author: {item.Message.AuthorName}");
            Console.WriteLine($"Message:{item.Message}");
        }
    }

    private ChatCompletionAgent CreateTransportationAgent(Kernel kernel)
    {
        var instructions = """
            You are an expert in finding transportation options from a given hotel location to the concert location.
            You will try to get the best options available for an afordable price.Make sure the customer will be there at least 30 minutes
            before the concert starts at the venue. You always suggest 3 options with different price ranges.
            You will ask for approval before you make the booking
            """;

        ChatCompletionAgent agent = new()
        {
            Name = "TransportationAgent",
            Instructions = instructions,
            Description = "An agent that finds transportation options from hotel to concert location",
            Kernel = kernel,
            LoggerFactory = LoggerFactory.Create(builder =>
            {
                // Add Console logging provider
                builder.AddConsole().SetMinimumLevel(LogLevel.Trace);
            })
        };
        return agent;
    }
}
