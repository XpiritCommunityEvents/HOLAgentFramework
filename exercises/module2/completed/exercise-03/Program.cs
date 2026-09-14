using AgentFramework101;
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
var apiKey = configuration["OpenAI:ApiKey"] ?? throw new InvalidOperationException("Set OpenAI:ApiKey in appsettings.json or your environment.");

var openAIClient = new OpenAIClient(new ApiKeyCredential(apiKey), new OpenAIClientOptions
{
    Endpoint = new Uri(endpoint)
});

using IChatClient chatClient = openAIClient.GetChatClient(model).AsIChatClient();

Console.WriteLine("GloboTicket assistant.");

// Fake user context for demonstration purposes. Set the Application:UserId in appsettings.json or your environment.
var userContext = new UserSessionContext(configuration["Application:UserId"]?.Trim());

Console.WriteLine(userContext.IsAuthenticated
    ? $"Signed in as {userContext.UserId}."
    : "Not signed in; discount requests will be blocked.");

var discountTools = new DiscountTools(userContext);
var anonymousUserFilter = new AnonymousUserFilter(userContext);

List<AITool> tools =
[
    AIFunctionFactory.Create(
        discountTools.GetDiscountCode,
        DiscountTools.ToolName,
        "Generate a discount code for the signed-in user."),
    new ApprovalRequiredAIFunction(AIFunctionFactory.Create(
        GetCurrentUtcTime,
        "get_current_utc_time",
        "Get the current date and time in UTC.")),
    AIFunctionFactory.Create(PdfSkillSupport.DownloadFile)
];

var skillsPath = Path.Combine(AppContext.BaseDirectory, "skills");
using var skillsProvider = new AgentSkillsProvider(
    skillsPath,
    options: new AgentSkillsProviderOptions
    {
        DisableLoadSkillApproval = false,
        DisableRunSkillScriptApproval = true,
        DisableReadSkillResourceApproval = true,
        IncludeDetailedErrors = true
    },
    scriptRunner: PdfSkillSupport.RunPythonScriptAsync);

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
        .Use(anonymousUserFilter.InvokeAsync) // Apply the anonymous user filter to each agent turn
   .Build();

var runOptions = new ChatClientAgentRunOptions(new ChatOptions
{
   TopP = 0.5f,
   TopK = 40,
   Temperature = 0f,
   FrequencyPenalty = 0.5f,
   MaxOutputTokens = 500
});

AgentSession session = await agent.CreateSessionAsync();

// Use this prompt to test the skills. You can also try other prompts that might trigger the skills.
var question = """
I have tickets to Martin Garrix, you can find them here: https://github.com/vriesmarcel/vslive-2026-vegas-sk/blob/main/CT4EB6AF_mobile_267842.pdf
""";

while (true)
{
    Console.Write("\n> ");
    string? prompt = Console.ReadLine();

    if (string.IsNullOrWhiteSpace(prompt))
    {
        continue;
    }

    ChatMessage message = new(ChatRole.User, prompt);
    while (true)
    {
        List<ToolApprovalRequestContent> approvalRequests = [];

        await foreach (AgentResponseUpdate update in agent.RunStreamingAsync(message, session))
        {
            approvalRequests.AddRange(update.Contents.OfType<ToolApprovalRequestContent>());
            AgentResponseHelper.HandleResponseUpdate(update);
        }

        Console.WriteLine();

        if (approvalRequests.Count == 0)
        {
            break;
        }

        List<AIContent> responses = [];
        foreach (var approvalRequest in approvalRequests)
        {
            Console.Write($"Approve tool call {approvalRequest.ToolCall.CallId}? [y/N] ");
            var approved = string.Equals(Console.ReadLine(), "y", StringComparison.OrdinalIgnoreCase);
            responses.Add(approvalRequest.CreateResponse(approved, null));
        }

        message = new ChatMessage(ChatRole.User, responses);
    }

    Console.WriteLine();
}

/// <summary>
/// Tool: current date and time
/// </summary>
static string GetCurrentUtcTime() =>
    DateTimeOffset.UtcNow.ToString("yyyy-MM-dd HH:mm:ss 'UTC'");
