using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.AI;
using System.ClientModel;
using modulerag;
using OpenAI;

var builder = new ConfigurationBuilder();
builder.SetBasePath(Directory.GetCurrentDirectory())
        .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
        .AddUserSecrets<Program>();

IConfiguration config = builder.Build();

var model = config["OpenAI:Model"];
var endpoint = config["OpenAI:EndPoint"];
var token = config["OpenAI:ApiKey"];

// Create OpenAI client with custom endpoint
var openAIClient = new OpenAIClient(new ApiKeyCredential(token), new OpenAIClientOptions
{
    Endpoint = new Uri(endpoint)
});

var chatCompletionClient = openAIClient.GetChatClient(model).AsIChatClient();

await new ChatWithRag().RAG_with_single_prompt(chatCompletionClient);
