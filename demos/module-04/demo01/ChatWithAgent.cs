using System.ClientModel;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;



namespace ModuleAgent;

internal class ChatWithAgent
{
    public async Task LetAgentFindRide(IConfiguration config)
    {
        var question = """
        I stay at the WestIn Seattle and the venue is the Seattle Kraken stadium.
        the Concert starts at 7:30 pm and is November 20th this year. 
        """;

        Console.WriteLine("******** Create the agent ***********");
        var transportationAgent = CreateTransportationAgent(config);

        Console.WriteLine("******** Start the agent ***********");
        var agentresult = await transportationAgent.RunAsync(question);

        Console.WriteLine("******** RESPONSE ***********");
        PrintResult(agentresult);
    }

    private static void PrintResult(AgentResponse agentResponse)
    {
        foreach (var item in agentResponse.Messages)
        {
            Console.WriteLine("----------------------------------------");
            Console.WriteLine($"Message id: {item.MessageId}");
            Console.WriteLine($"Author: {item.AuthorName}");

            foreach (var content in item.Contents)
            {
                switch (content)
                {
                    case TextContent textContent:
                        Console.WriteLine($"Text: {textContent.Text}");
                        break;
                    case FunctionCallContent functionCallContent:
                        Console.WriteLine($"Function call: {functionCallContent.RawRepresentation}");
                        break;
                    case FunctionResultContent functionResultContent:
                        Console.WriteLine($"Function result: {functionResultContent.RawRepresentation}");
                        break;
                    case ToolApprovalRequestContent toolApprovalRequestContent:
                        Console.WriteLine($"Tool approval request: {toolApprovalRequestContent.RawRepresentation}");
                        break;
                    default:
                        Console.WriteLine($"{content.GetType().Name}: {content.RawRepresentation}");
                        break;
                }
            }
        }

        Console.WriteLine("----------------------------------------");
    }

    private AIAgent CreateTransportationAgent(IConfiguration config)
    {
      
        var instructions = """
            You are an expert in finding transportation options from a given hotel location to the concert location.
            You will try to get the best options available for an afordable price.
            Make sure the customer will be there at least 30 minutes before the concert starts at the venue.
            You always suggest 3 options with different price ranges.
            You will ask for approval before you make the booking
            """;

        var model = config["OpenAI:Model"] ?? throw new InvalidOperationException("OpenAI:Model is not configured.");
        var endpoint = config["OpenAI:EndPoint"] ?? throw new InvalidOperationException("OpenAI:EndPoint is not configured.");
        var token = config["OpenAI:ApiKey"] ?? throw new InvalidOperationException("OpenAI:ApiKey is not configured.");

        AIAgent agent = new OpenAI.Chat.ChatClient(
        model,
        new ApiKeyCredential(token),
        new OpenAI.OpenAIClientOptions
        {
            Endpoint = new Uri(new Uri(endpoint), "openai/v1/")
        })
        .AsIChatClient()
        .AsAIAgent( 
             tools:[
                AIFunctionFactory.Create(RideInformationSystemService.GetAvailableRides),
                AIFunctionFactory.Create(RideInformationSystemService.BookARide)
            ],
            instructions: instructions,
            name: "TransportationAgent");

        return agent;
    }
}
