using System.ClientModel;
using System.ClientModel.Primitives;
using System.ComponentModel;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using modulerag;
using OpenAI.Chat;
#pragma warning disable MAAI001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.


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
        var session = await transportationAgent.CreateSessionAsync();
        
        var agentresult =  transportationAgent.RunStreamingAsync(question, session);
        await foreach (var item in agentresult)
        {
           await PrintResult(item);
        }
        Console.WriteLine("******** Done ***********");
        
    }

    private static async Task PrintResult(AgentResponseUpdate agentResponse)
    {
        
        foreach (var item in agentResponse.Contents)
        {
            //print information about every AIContent item
            if(item is TextContent textContent)
                Console.WriteLine($"{textContent.RawRepresentation}");
            if(item is DataContent dataContent)     
                Console.WriteLine($"{dataContent.MediaType}");
            if(item is UriContent uriContent)
                Console.WriteLine($"{uriContent.Uri}");
            if(item is ToolApprovalRequestContent toolApprovalRequestContent)
                Console.WriteLine($"{toolApprovalRequestContent.RawRepresentation}");
            if(item is FunctionCallContent functionCallContent)
                Console.WriteLine($"{functionCallContent.RawRepresentation}");
            if(item is FunctionResultContent functionResultContent)
                Console.WriteLine($"{functionResultContent.RawRepresentation}");
        }
    }

    private AIAgent CreateTransportationAgent(IConfiguration config)
    {
      

        var instructions = """
            You are an expert in finding transportation options from a given hotel location to the concert location.
            You will try to get the best options available for an afordable price.Make sure the customer will be there at least 30 minutes
            before the concert starts at the venue. You always suggest 3 options with different price ranges.
            When you need input from the user, ask for input using the available functions. 
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
            tools: [
             AIFunctionFactory.Create(AskForUserInput),
             AIFunctionFactory.Create(ShowMessage),
             AIFunctionFactory.Create(RideInformationSystemService.GetAvailableRides),
             AIFunctionFactory.Create(RideInformationSystemService.BookARide)
            ],
            instructions: instructions,
            name: "TransportationAgent");

        //now give it an agent loop with an evaluator to see if a 
        // booking has taken place
        AIAgent loopAgent = new LoopAgent(
            innerAgent: agent,
            evaluator: new BookingEvaluator(config),
            options: new LoopAgentOptions()
            {
                
            }
        );
        return loopAgent;
    }

    [Description("Asks the user for input based on the provided question.")]
    public string AskForUserInput([Description("The question to ask the user")]string questionToASk)
    {
        Console.WriteLine(questionToASk);
        return Console.ReadLine() ?? string.Empty;
    }
    
    [Description("Displays a message to the user. Use this to show your output to the end user")]
    public void ShowMessage([Description("The message to display")]string message)
    {
        Console.WriteLine(message);
    }
}
#pragma warning restore MAAI001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
