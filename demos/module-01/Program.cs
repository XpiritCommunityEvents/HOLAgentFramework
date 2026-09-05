using Azure;
using Azure.AI.Inference;
using Microsoft.Extensions.Configuration;

var configuration = new ConfigurationBuilder()
    .AddJsonFile(Path.Combine(AppContext.BaseDirectory, "appsettings.json"))
    .AddUserSecrets<Program>()
    .AddEnvironmentVariables()
    .Build();

var model = configuration["OpenAI:Model"] ?? throw new InvalidOperationException("Set OpenAI:Model in appsettings.json or your environment.");
var endpoint = configuration["OpenAI:Endpoint"] ?? throw new InvalidOperationException("Set OpenAI:Endpoint in appsettings.json or your environment.");
var apiKey = configuration["OpenAI:ApiKey"] ?? throw new InvalidOperationException("Set OpenAI:ApiKey in appsettings.json or your environment.");

Console.WriteLine($"Model: {model}");
Console.WriteLine($"Endpoint: {endpoint}");

var client = new ChatCompletionsClient(
    new Uri(endpoint),
    new AzureKeyCredential(apiKey),
    new AzureAIInferenceClientOptions());

var requestOptions = new ChatCompletionsOptions()
{
    Model = model,
    Messages =
    [
        new ChatRequestUserMessage("Tell me a joke about computers")
    ]
};

var response = await client.CompleteAsync(requestOptions);
Console.WriteLine(response.Value.Content);
