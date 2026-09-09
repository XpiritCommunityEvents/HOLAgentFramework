using System.ClientModel;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using modulerag;
using OpenAI;

IConfiguration config = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
    .AddUserSecrets<Program>()
    .Build();

string model = config["OpenAI:Model"]
    ?? throw new InvalidOperationException("Set OpenAI:Model in appsettings.json or user secrets.");
string endpointValue = config["OpenAI:Endpoint"]
    ?? throw new InvalidOperationException("Set OpenAI:Endpoint in appsettings.json or user secrets.");
string apiKey = config["OpenAI:ApiKey"]
    ?? throw new InvalidOperationException("Set OpenAI:ApiKey in appsettings.json or user secrets.");

if (!Uri.TryCreate(endpointValue, UriKind.Absolute, out Uri? endpoint))
{
    throw new InvalidOperationException("OpenAI:Endpoint must be an absolute URI.");
}

var openAIClient = new OpenAIClient(
    new ApiKeyCredential(apiKey),
    new OpenAIClientOptions { Endpoint = endpoint });

using IChatClient chatClient = openAIClient.GetChatClient(model).AsIChatClient();
using var cancellationSource = new CancellationTokenSource();

ConsoleCancelEventHandler cancelHandler = (_, eventArgs) =>
{
    eventArgs.Cancel = true;
    cancellationSource.Cancel();
};

Console.CancelKeyPress += cancelHandler;
try
{
    await new ChatWithAgent(chatClient).LetAgentFindRideAsync(cancellationSource.Token);
}
catch (OperationCanceledException) when (cancellationSource.IsCancellationRequested)
{
    Console.WriteLine("The request was cancelled.");
}
finally
{
    Console.CancelKeyPress -= cancelHandler;
}
