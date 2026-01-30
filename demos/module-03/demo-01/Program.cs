using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.AI;
using Microsoft.Agents.AI;
using OpenAI;
using System.ClientModel;
using modulerag;

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

var chatCompletionClient = openAIClient.GetChatClient(model);

AIAgent agent = chatCompletionClient.AsAIAgent(
    instructions: "You are good at telling jokes.",
    name: "Joker");

// var kernelBuilder = Kernel
//     .CreateBuilder()
//     .AddOpenAIChatCompletion(model, new Uri(endpoint), token);

// var kernel = kernelBuilder.Build();

//await new ChatWithRag().RAG_with_single_prompt(kernel);
