using System.ClientModel;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using modulerag;
using OpenAI;

var builder = new ConfigurationBuilder();
builder.SetBasePath(Directory.GetCurrentDirectory())
        .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
        .AddUserSecrets<Program>();

IConfiguration config = builder.Build();

var model = config["OpenAI:Model"] ?? throw new InvalidOperationException("Missing OpenAI model setting.");
var endpoint = config["OpenAI:Endpoint"] ?? config["OpenAI:EndPoint"] ??
        throw new InvalidOperationException("Missing OpenAI endpoint setting.");
var apiKey = config["OpenAI:ApiKey"] ?? throw new InvalidOperationException("Missing OpenAI API key.");

// Create OpenAI client with custom endpoint
var openAIClient = new OpenAIClient(new ApiKeyCredential(apiKey), new OpenAIClientOptions
{
    Endpoint = new Uri(endpoint)
});

var chatClient = openAIClient.GetChatClient(model).AsIChatClient();

await new ChatWithAgent(chatClient).LetAgentFindRide();
