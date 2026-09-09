using System.ClientModel;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using OpenAI;
using modulerag;

var builder = new ConfigurationBuilder();
builder.SetBasePath(Directory.GetCurrentDirectory())
        .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
        .AddUserSecrets<Program>();

IConfiguration config = builder.Build();

string? model = config["OpenAI:Model"];
string? endpoint = config["OpenAI:Endpoint"];
string? apiKey = config["OpenAI:ApiKey"];

if (string.IsNullOrWhiteSpace(model) ||
    string.IsNullOrWhiteSpace(endpoint) ||
    string.IsNullOrWhiteSpace(apiKey))
{
    throw new InvalidOperationException("Set OpenAI:Model, OpenAI:Endpoint, and OpenAI:ApiKey.");
}

var openAIClient = new OpenAIClient(new ApiKeyCredential(apiKey), new OpenAIClientOptions
{
    Endpoint = new Uri(endpoint)
});

var chatClient = openAIClient.GetChatClient(model).AsIChatClient();
await HandoffDemo.RunAsync(chatClient);
