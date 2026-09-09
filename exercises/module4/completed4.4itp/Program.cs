using System.ClientModel;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using OpenAI;
using modulerag;

IConfiguration config = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
    .AddUserSecrets<Program>()
    .Build();

string? model = config["OpenAI:Model"];
string? endpoint = config["OpenAI:Endpoint"];
string? apiKey = config["OpenAI:ApiKey"];

if (string.IsNullOrWhiteSpace(model) ||
    string.IsNullOrWhiteSpace(endpoint) ||
    string.IsNullOrWhiteSpace(apiKey) ||
    endpoint.Contains("[[", StringComparison.Ordinal) ||
    apiKey.StartsWith('<'))
{
    throw new InvalidOperationException(
        "Set OpenAI:Model, OpenAI:Endpoint, and OpenAI:ApiKey in user secrets or appsettings.json.");
}

var openAIClient = new OpenAIClient(new ApiKeyCredential(apiKey), new OpenAIClientOptions
{
    Endpoint = new Uri(endpoint)
});

using IChatClient chatClient = openAIClient.GetChatClient(model).AsIChatClient();
await ChatWithAgent.RunAsync(chatClient);
