using System.ClientModel;
using System.ComponentModel;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;

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
    
        List<ChatMessage> input = [new ChatMessage(ChatRole.User, question)];

        var response =  await transportationAgent.RunAsync(input, session);

        
        AgentResponsePrinter.PrintResponse(response);
        Console.WriteLine("******** Done ***********");
    }

    private AIAgent CreateTransportationAgent(IConfiguration config)
    {
      

        var instructions = """
            You are an expert in finding transportation options from a given hotel location to the concert location.
            You will try to get the best options available for an afordable price.
            Make sure the customer will be there at least 30 minutes before the concert starts at the venue.
            You always suggest 3 options with different price ranges.
            
            Never ask the user a question as plain text: nobody reads it. Whenever you need information from
            the user, call the AskForUserInput function and wait for its result. Booking functions request
            approval on their own, so call them directly instead of asking for permission in text first.

            ##Exit condition: 
            The agent should exit once a ride has been successfully booked or the user cancels the process.
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
        //booking has taken place
        AIAgent loopAgent = new LoopAgent(
            innerAgent: agent,
            evaluator: new BookingEvaluator(config),
            options: new LoopAgentOptions()
            {
                
            }
        );
        return agent;
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
