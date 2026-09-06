using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using System.ClientModel;

var config = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .AddUserSecrets<Program>()
    .AddEnvironmentVariables()
    .Build();

var model = config["OpenAI:Model"] ?? throw new InvalidOperationException("OpenAI:Model is not configured.");
var endpoint = config["OpenAI:Endpoint"] ?? throw new InvalidOperationException("OpenAI:Endpoint is not configured.");
var token = config["OpenAI:ApiKey"] ?? throw new InvalidOperationException("OpenAI:ApiKey is not configured.");

AIAgent agent = new OpenAI.Chat.ChatClient(
    model,
    new ApiKeyCredential(token),
    new OpenAI.OpenAIClientOptions
    {
        Endpoint = new Uri(new Uri(endpoint), "openai/v1/")
    })
    .AsIChatClient()
    .AsAIAgent(
        instructions: """
            You are a digital assistant for GloboTicket, a concert ticketing company. You help customers with their ticket purchasing.
            Tone: warm and friendly, but to the point. Do not make things up when you don't know the answer. Just tell the user that
            you don't know the answer based on your knowledge.
        """,
        name: "Assistant");

// Continue the exercise by creating the embedding generator and vector store,
// ingesting the venue policies, and using the retrieved policy chunks with the agent.
