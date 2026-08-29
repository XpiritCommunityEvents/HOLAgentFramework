using Azure;
using Azure.AI.Inference;
using Microsoft.Extensions.Configuration;

var config = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .AddUserSecrets<Program>()
    .Build();

var model = config["OpenAI:Model"] ?? throw new InvalidOperationException("Missing OpenAI:Model configuration.");
var endpoint = config["OpenAI:EndPoint"] ?? throw new InvalidOperationException("Missing OpenAI:EndPoint configuration.");
var token = config["OpenAI:ApiKey"];

if (string.IsNullOrWhiteSpace(token) || token == "<set this in your user secrets>")
{
    throw new InvalidOperationException("Set OpenAI:ApiKey with dotnet user-secrets before running the app.");
}

Console.WriteLine($"Model: {model}");
Console.WriteLine($"Endpoint: {endpoint}");

var client = new ChatCompletionsClient(
    new Uri(endpoint),
    new AzureKeyCredential(token),
    new AzureAIInferenceClientOptions());

var requestOptions = new ChatCompletionsOptions()
{
    Model = model,
    Messages =
    [
        new ChatRequestUserMessage("Tell me a joke about computers")
    ]
};

var resp = await client.CompleteAsync(requestOptions);
Console.WriteLine(resp.Value.Content);