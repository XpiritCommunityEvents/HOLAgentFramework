using System.ClientModel;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using OpenAI;

IConfiguration config = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
    .AddUserSecrets<Program>()
    .Build();

string model = config["OpenAI:Model"]
    ?? throw new InvalidOperationException("OpenAI:Model is not configured.");
string endpoint = config["OpenAI:Endpoint"]
    ?? throw new InvalidOperationException("OpenAI:Endpoint is not configured.");
string apiKey = config["OpenAI:ApiKey"]
    ?? throw new InvalidOperationException("OpenAI:ApiKey is not configured.");

var openAIClient = new OpenAIClient(new ApiKeyCredential(apiKey), new OpenAIClientOptions
{
    Endpoint = new Uri(endpoint)
});

using IChatClient chatClient = openAIClient.GetChatClient(model).AsIChatClient();
AIAgent agent = chatClient.AsAIAgent(
    name: "TravelAssistant",
    instructions: "You are a helpful travel assistant. Remember details from earlier turns.");
AgentSession session = await agent.CreateSessionAsync();

Console.WriteLine("Chat with the travel assistant. Type 'exit' to stop.");

while (true)
{
    Console.Write("You: ");
    string? input = Console.ReadLine();

    if (input is null || input.Equals("exit", StringComparison.OrdinalIgnoreCase))
    {
        break;
    }

    if (string.IsNullOrWhiteSpace(input))
    {
        continue;
    }

    AgentResponse response = await agent.RunAsync(input, session);
    Console.WriteLine($"Agent: {response.Text}");
}
