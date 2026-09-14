using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;

namespace AgentFramework101.Scenarios;

/// <summary>
/// Scenario 03: tools, and approvals
/// </summary>
internal static class Scenario_03
{
    /// <summary>
    /// Tool: current date and time
    /// </summary>
    static string GetCurrentUtcTime() =>
        DateTimeOffset.UtcNow.ToString("yyyy-MM-dd HH:mm:ss 'UTC'");

    public static async Task Run(IChatClient chatClient, IConfiguration configuration)
    {
        Console.WriteLine("SCENARIO 03 --- tools, and approvals");

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
                AIFunctionFactory.Create(
                    GetCurrentUtcTime,
                    "get_current_utc_time",
                    "Get the current date and time in UTC.")
        ];

        AIAgent agent = chatClient
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
                    Tools = tools
                }
            })
            .AsBuilder()
            //    .Use(anonymousUserFilter.InvokeAsync) // Apply the anonymous user filter to each agent turn
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

            await foreach (AgentResponseUpdate update in agent.RunStreamingAsync(prompt, session))
            {
                AgentResponseHelper.HandleResponseUpdate(update);
            }

            #region Approval loop
            //ChatMessage message = new(ChatRole.User, prompt);
            //while (true)
            //{
            //    List<ToolApprovalRequestContent> approvalRequests = [];

            //    await foreach (AgentResponseUpdate update in agent.RunStreamingAsync(message, session))
            //    {
            //        approvalRequests.AddRange(update.Contents.OfType<ToolApprovalRequestContent>());
            //        AgentResponseHelper.HandleResponseUpdate(update);
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
            #endregion

            Console.WriteLine();
        }
    }
}
