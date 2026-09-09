using System.ClientModel;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using OpenAI;
using modulerag;

IConfiguration configuration = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false)
    .AddUserSecrets<Program>()
    .Build();

string model = configuration["OpenAI:Model"]
    ?? throw new InvalidOperationException("Missing OpenAI:Model setting.");
string endpoint = configuration["OpenAI:Endpoint"]
    ?? throw new InvalidOperationException("Missing OpenAI:Endpoint setting.");
string apiKey = configuration["OpenAI:ApiKey"]
    ?? throw new InvalidOperationException("Missing OpenAI:ApiKey setting.");

var openAIClient = new OpenAIClient(
    new ApiKeyCredential(apiKey),
    new OpenAIClientOptions { Endpoint = new Uri(endpoint) });

using IChatClient chatClient = openAIClient.GetChatClient(model).AsIChatClient();
await new ChatWithAgent(chatClient).RunAsync();
