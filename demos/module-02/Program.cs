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

//var skillsPath = Path.Combine(AppContext.BaseDirectory, "skills");
//using var skillsProvider = new AgentSkillsProvider(
//    skillsPath,
//    options: new AgentSkillsProviderOptions
//    {
//        DisableLoadSkillApproval = true,
//        DisableRunSkillScriptApproval = true,
//        DisableReadSkillResourceApproval = true,
//    });

// Fake user context for demonstration purposes. Set the Application:UserId in appsettings.json or your environment.
var userContext = new UserSessionContext(configuration["Application:UserId"]?.Trim());
//var discountTools = new DiscountTools(userContext);
//var anonymousUserFilter = new AnonymousUserFilter(userContext);

List<AITool> tools = [];
//[
//    AIFunctionFactory.Create(
//        discountTools.GetDiscountCode,
//        DiscountTools.ToolName,
//        "Generate a discount code for the signed-in user."),
//    new ApprovalRequiredAIFunction(
//        AIFunctionFactory.Create(
//            GetCurrentUtcTime,
//            "get_current_utc_time",
//            "Get the current date and time in UTC."))
//];

AIAgent agent = chatClient
    .AsAIAgent(new ChatClientAgentOptions
    {
        Name = "GloboTicketAssistant",
//        AIContextProviders = [skillsProvider],
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
//    .Use(anonymousUserFilter.InvokeAsync) // Apply the anonymous user filter to each agent turn
    .Build();

//var runOptions = new ChatClientAgentRunOptions(new ChatOptions
//{
//    MaxOutputTokens = 500,
//    Temperature = 0.5f,
//    TopP = 1.0f,
//    FrequencyPenalty = 0.0f,
//    PresencePenalty = 0.0f
//});

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

    // synchronous response:
    var response = await agent.RunAsync(prompt, session);
    Console.Write(response.Text);

    // streaming response:
    //await foreach (AgentResponseUpdate update in agent.RunStreamingAsync(prompt, session))
    //{
    //    Console.Write(update);
    //}

    // synchronous structured response:
    // var structuredResponse = await agent.RunAsync<ShowSummary>(prompt, session);
    // Console.WriteLine($"Artist: {structuredResponse.Result.Artist}");
    // Console.WriteLine($"Title: {structuredResponse.Result.Title}");
    // Console.WriteLine($"Venue: {structuredResponse.Result.Venue}");
    // Console.WriteLine($"Description: {structuredResponse.Result.Description}");
    // Console.WriteLine($"Date: {structuredResponse.Result.Date}");

    //ChatMessage message = new(ChatRole.User, prompt);

    //while (true)
    //{
    //    List<ToolApprovalRequestContent> approvalRequests = [];

    //    await foreach (var update in agent.RunStreamingAsync(message, session))
    //    {
    //        approvalRequests.AddRange(update.Contents.OfType<ToolApprovalRequestContent>());
    //        Console.Write(update);
    //    }

    //    Console.WriteLine();

    //    if (approvalRequests.Count == 0)
    //    {
    //        break;
    //    }

    //    List<AIContent> responses = [];
    //    foreach (var approvalRequest in approvalRequests)
    //    {
    //        Console.Write($"Approve tool call {approvalRequest.ToolCall.CallId}? [y/N] ");
    //        var approved = string.Equals(Console.ReadLine(), "y", StringComparison.OrdinalIgnoreCase);
    //        responses.Add(approvalRequest.CreateResponse(approved, null));
    //    }

    //    message = new ChatMessage(ChatRole.User, responses);
    //}


    Console.WriteLine();
}

static string GetCurrentUtcTime() =>
    DateTimeOffset.UtcNow.ToString("yyyy-MM-dd HH:mm:ss 'UTC'");

// TODO : compaction https://learn.microsoft.com/en-us/agent-framework/concepts/agents/conversations/compaction?pivots=programming-language-csharp
