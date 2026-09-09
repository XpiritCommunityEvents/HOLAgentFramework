using System.ClientModel;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using OpenAI;
using modulerag;

var builder = new ConfigurationBuilder();
builder.SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
    .AddUserSecrets<Program>(optional: true);

IConfiguration config = builder.Build();
string model = config["OpenAI:Model"]
    ?? throw new InvalidOperationException("OpenAI:Model is not configured.");
string endpoint = config["OpenAI:Endpoint"]
    ?? throw new InvalidOperationException("OpenAI:Endpoint is not configured.");
string apiKey = config["OpenAI:ApiKey"]
    ?? throw new InvalidOperationException("OpenAI:ApiKey is not configured.");

var openAIClient = new OpenAIClient(
    new ApiKeyCredential(apiKey),
    new OpenAIClientOptions { Endpoint = new Uri(endpoint) });

IChatClient chatClient = openAIClient.GetChatClient(model).AsIChatClient();

await new ChatWithAgent(chatClient).LetAgentFindRideAsync();
