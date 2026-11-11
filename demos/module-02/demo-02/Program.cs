using AgentFramework101.Scenarios;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using OpenAI;
using OpenAI.Responses;
using System.ClientModel;
#pragma warning disable OPENAI001 // The pinned SDK marks the Responses API as experimental.

var configuration = new ConfigurationBuilder()
    .AddJsonFile(Path.Combine(AppContext.BaseDirectory, "appsettings.json"))
    .AddUserSecrets<Program>()
    .AddEnvironmentVariables()
    .Build();

var model = configuration["OpenAI:Model"] ?? throw new InvalidOperationException("Set OpenAI:Model in appsettings.json or your environment.");
var endpoint = configuration["OpenAI:Endpoint"] ?? throw new InvalidOperationException("Set OpenAI:Endpoint in appsettings.json or your environment.");
var apiKey = configuration["OpenAI:ApiKey"] ?? throw new InvalidOperationException("Set OpenAI:ApiKey in appsettings.json or your environment.");

var openAIClient = new OpenAIClient(new ApiKeyCredential(apiKey), new OpenAIClientOptions
{
    Endpoint = new Uri(endpoint)
});

var responsesClient = openAIClient.GetResponsesClient();

Console.WriteLine("GloboTicket assistant.");

await Scenario_01.Run(responsesClient, model);

//await Scenario_02.Run(responsesClient, model);

//await Scenario_03.Run(responsesClient, model, configuration);

//await Scenario_04.Run(responsesClient, model, configuration);

//await Scenario_05.Run(responsesClient, model, configuration);
