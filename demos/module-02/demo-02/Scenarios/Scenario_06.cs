using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using OllamaSharp;
using OpenAI.Responses;

namespace AgentFramework101.Scenarios;

/// <summary>
/// Scenario 06: local LLM with Ollama provider
/// </summary>
internal static class Scenario_06
{
    /// <summary>
    /// Tool: current date and time
    /// </summary>
    static string GetCurrentUtcTime() =>
        DateTimeOffset.UtcNow.ToString("yyyy-MM-dd HH:mm:ss 'UTC'");

    public static async Task Run(IConfiguration configuration)
    {
        Console.WriteLine("SCENARIO 06 --- Using local Ollama provider");

        // Demo: using a local LLM
        var ollamaClient = new OllamaApiClient(new Uri("http://localhost:11434"), "gemma4");

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
                    "Get the current date and time in UTC."))
        ];

        AIAgent agent = ollamaClient
            .AsAIAgent(new ChatClientAgentOptions
            {
                Name = "GloboTicketAssistant",
                ChatOptions = new ChatOptions
                {
                    Instructions = """
                        You are a digital assistant for GloboTicket, a concert ticketing company. You help customers with their ticket purchasing.
                        Tone: warm and friendly, but to the point. Do not make things up when you don't know the answer. Just tell the user that 
                        you don't know the answer based on your knowledge.
                        Load an available skill when its description matches the user's request.
                        """,
                    Tools = tools.ToArray()
                }
            })
            .AsBuilder()
            .Build();

        // Reuse one session so each turn includes the conversation so far.
        AgentSession session = await agent.CreateSessionAsync();

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

                await foreach (var update in agent.RunStreamingAsync(message, session))
                {
                    approvalRequests.AddRange(update.Contents.OfType<ToolApprovalRequestContent>());
                    Console.Write(update);
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
    }
}
