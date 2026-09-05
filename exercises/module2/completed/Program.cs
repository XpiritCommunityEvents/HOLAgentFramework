using AgentFrameworkWorkshop.Module2.Completed;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using OpenAI;
using System.ClientModel;

var configuration = new ConfigurationBuilder()
    .AddJsonFile(Path.Combine(AppContext.BaseDirectory, "appsettings.json"))
    .AddUserSecrets<Program>()
    .AddEnvironmentVariables()
    .Build();

var model = configuration["OpenAI:Model"] ?? throw new InvalidOperationException("Set OpenAI:Model in appsettings.json or your environment.");
var endpoint = configuration["OpenAI:Endpoint"] ?? throw new InvalidOperationException("Set OpenAI:Endpoint in appsettings.json or your environment.");
var apiKey = configuration["OpenAI:ApiKey"] ?? throw new InvalidOperationException("Set OpenAI:ApiKey in user secrets or your environment.");

var openAIClient = new OpenAIClient(new ApiKeyCredential(apiKey), new OpenAIClientOptions
{
    Endpoint = new Uri(endpoint)
});

using IChatClient chatClient = openAIClient.GetChatClient(model).AsIChatClient();

var skillsPath = Path.Combine(AppContext.BaseDirectory, "skills");
using var skillsProvider = new AgentSkillsProvider(
    skillsPath,
    options: new AgentSkillsProviderOptions
    {
        // These skills contain trusted instructions only, so loading them does not require user approval.
        DisableLoadSkillApproval = true
    });

// Fake user context for demonstration purposes. Set Application:UserId in appsettings.json or your environment.
var userContext = new UserSessionContext(configuration["Application:UserId"]?.Trim());
var discountTools = new DiscountTools(userContext);
var anonymousUserFilter = new AnonymousUserFilter(userContext);

List<AITool> tools =
[
    AIFunctionFactory.Create(
        discountTools.GetDiscountCode,
        DiscountTools.ToolName,
        "Generate a GloboTicket discount code for the signed-in user."),
    AIFunctionFactory.Create(
        GetCurrentUtcTime,
        "get_current_utc_time",
        "Get the current date and time in UTC.")
];

AIAgent agent = chatClient
    .AsAIAgent(new ChatClientAgentOptions
    {
        Name = "GloboTicketAssistant",
        AIContextProviders = [skillsProvider],
        ChatOptions = new ChatOptions
        {
            Instructions = """
                You are a digital assistant for GloboTicket, a concert ticketing company. You help customers with their ticket purchasing.
                Tone: warm and friendly, but to the point. Do not make things up when you don't know the answer. Just tell the user that
                you don't know the answer based on your knowledge.
                Load an available skill when its description matches the user's request.
                """,
            Tools = tools
        }
    })
    .AsBuilder()
    .Use(anonymousUserFilter.InvokeAsync)
    .Build();

// Reuse one session so each turn includes the conversation so far.
AgentSession session = await agent.CreateSessionAsync();

Console.WriteLine("GloboTicket assistant.");
Console.WriteLine(userContext.IsAuthenticated
    ? $"Signed in as {userContext.UserId}."
    : "Not signed in; discount requests will be blocked.");

while (true)
{
    Console.Write("\n> ");
    string? prompt = Console.ReadLine();

    if (string.IsNullOrWhiteSpace(prompt))
    {
        continue;
    }

    await foreach (var update in agent.RunStreamingAsync(prompt, session))
    {
        Console.Write(update);
    }

    Console.WriteLine();
}

static string GetCurrentUtcTime() =>
    DateTimeOffset.UtcNow.ToString("yyyy-MM-dd HH:mm:ss 'UTC'");
