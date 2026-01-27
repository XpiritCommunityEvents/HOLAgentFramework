using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.AI;
using Microsoft.Agents.AI;
using OpenAI;
using System.ClientModel;
using AgentFramework101;

// Make sure to add ApiKey to your dotnet user secrets...
// dotnet user-secrets set "ApiKey"="<your API key>" -p .\module2.csproj
// PLEASE DO NOT COMMIT YOUR API SECRET TO GIT!

var config = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .AddUserSecrets<Program>()
    .AddEnvironmentVariables()
    .Build();

var token = config["OpenAI:ApiKey"] ?? throw new InvalidOperationException("Missing API Key");
var model = "openai/gpt-4o";
var endpoint = "https://models.github.ai/orgs/XpiritCommunityEvents/inference";

// Use Azure AI Foundry
// var model = "gpt-4o";
// var endpoint = "https://vries-mfc1t50l-swedencentral.cognitiveservices.azure.com";

// Create OpenAI client with custom endpoint
var openAIClient = new OpenAIClient(new ApiKeyCredential(token), new OpenAIClientOptions
{
    Endpoint = new Uri(endpoint)
});

// Create IChatClient from OpenAI client with OpenTelemetry instrumentation
// Set enableSensitiveData to true only in development environments
// Note: Per the docs, enabling telemetry on both chat client and agent may cause duplication.
//       We enable it on the chat client here for tracing inference calls.
IChatClient chatClient = openAIClient
    .GetChatClient(model)
    .AsIChatClient()
    .AsBuilder()
    .UseOpenTelemetry(
        sourceName: TelemetryExtensions.SourceName, 
        configure: cfg => cfg.EnableSensitiveData = false)  // Set to true to capture prompts/responses (dev only!)
    .Build();

// Create tools from the DiscountPlugin
var discountTools = new DiscountTools();
var tools = new List<AITool>
{
    AIFunctionFactory.Create(discountTools.GetDiscountCode),
    AIFunctionFactory.Create(GetCurrentTime)
};

// Create agent with tools
var instructions = """
    You are a digital assistant for GloboTicket, a concert ticketing company. You help customers with their ticket purchasing.
    Tone: warm and friendly, but to the point. Do not make things up when you don't know the answer. Just tell the user that 
    you don't know the answer based on your knowledge.
    """;

// Create a SummarizingChatReducer to manage chat history size
// This replaces the Semantic Kernel ChatHistorySummarizationReducer
// targetCount: target number of messages to keep after reduction
// threshold: number of messages allowed beyond targetCount before summarization is triggered
#pragma warning disable MEAI001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
var chatReducer = new SummarizingChatReducer(chatClient, targetCount: 2, threshold: 4);
#pragma warning restore MEAI001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

// Create agent with ChatMessageStoreFactory to enable chat history reduction
// Note: ChatClientAgentOptions uses ChatOptions.Instructions for system instructions
AIAgent baseAgent = chatClient.AsAIAgent(new ChatClientAgentOptions
{
    Name = "GloboTicketAssistant",
    ChatOptions = new ChatOptions 
    { 
        Instructions = instructions,
        Tools = tools  // Tools are passed via ChatOptions
    },
    // Configure the chat message store with the summarizing reducer
    // The reducer will automatically summarize older messages when the conversation exceeds the threshold
    ChatMessageStoreFactory = (ctx, ct) => new ValueTask<ChatMessageStore>(
        new InMemoryChatMessageStore(
            chatReducer,
            ctx.SerializedState,
            ctx.JsonSerializerOptions,
            InMemoryChatMessageStore.ChatReducerTriggerEvent.AfterMessageAdded))
});

// Wrap the agent with function calling middleware for anonymous user filtering
AIAgent agent = baseAgent
    .AsBuilder()
    .Use(AnonymousUserFilter.FilterAnonymousUsers)
    .Build();

// Agent options
var runOptions = new ChatClientAgentRunOptions(new ChatOptions
{
    MaxOutputTokens = 500,
    Temperature = 0.5f,
    TopP = 1.0f,
    FrequencyPenalty = 0.0f,
    PresencePenalty = 0.0f
});

Console.WriteLine("Hi! I am your AI assistant. Talk to me:");

// Create a thread for the conversation
var thread = await agent.GetNewThreadAsync();

while (true)
{
    Console.WriteLine();

    var prompt = Console.ReadLine();
    if (string.IsNullOrEmpty(prompt)) continue;

    // Non-streaming call using Agent Framework
    // var response = await agent.RunAsync(prompt, thread, runOptions);
    // Console.WriteLine(response.Text);  // or response.ToString()

    // Streaming call using Agent Framework
    await foreach (var update in agent.RunStreamingAsync(prompt, thread, runOptions))
    {
        Console.Write(update);
    }
}

// Local function to replace TimePlugin functionality
static string GetCurrentTime()
{
    return DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
}
