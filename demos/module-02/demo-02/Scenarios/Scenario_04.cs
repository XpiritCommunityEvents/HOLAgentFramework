using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;

namespace AgentFramework101.Scenarios;

/// <summary>
/// Scenario 04: skills
/// </summary>
internal static class Scenario_04
{
    public static async Task Run(IChatClient chatClient, IConfiguration configuration)
    {
        Console.WriteLine("SCENARIO 04 --- Skills");

        // Fake user context for demonstration purposes. Set the Application:UserId in appsettings.json or your environment.
        var userContext = new UserSessionContext(configuration["Application:UserId"]?.Trim());

        Console.WriteLine(userContext.IsAuthenticated
            ? $"Signed in as {userContext.UserId}."
            : "Not signed in; discount requests will be blocked.");

        var skillsPath = Path.Combine(AppContext.BaseDirectory, "skills");
        using var skillsProvider = new AgentSkillsProvider(
            skillsPath,
            options: new AgentSkillsProviderOptions
            {
                DisableLoadSkillApproval = false,
                DisableRunSkillScriptApproval = true,
                DisableReadSkillResourceApproval = true,
                IncludeDetailedErrors = true
            });
//            scriptRunner: PdfSkillSupport.RunPythonScriptAsync);

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
//                    Tools = [AIFunctionFactory.Create(PdfSkillSupport.DownloadFile)]
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

                await foreach (AgentResponseUpdate update in agent.RunStreamingAsync(message, session))
                {
                    approvalRequests.AddRange(update.Contents.OfType<ToolApprovalRequestContent>());
                    HandleResponseUpdate(update);
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

    /// <summary>
    /// Pretty prints the agent response update to the console, with color coding for different content types.
    /// </summary>
    private static void HandleResponseUpdate(AgentResponseUpdate agentResponse)
    {
        foreach (var item in agentResponse.Contents)
        {
            // Ordered most-derived first; ToolCallContent/ToolResultContent catch the remaining built-in tool types.
            switch (item)
            {
                case TextContent content:
                    Console.Write(content.Text);
                    break;

                case TextReasoningContent content:
                    Write(ConsoleColor.DarkGray, content.Text);
                    break;

                case FunctionCallContent content:
                    Write(ConsoleColor.Cyan, $"\n[call] {content.Name}({FormatArguments(content.Arguments)})\n");
                    break;

                case FunctionResultContent content:
                    Write(ConsoleColor.DarkCyan, $"[result] {content.CallId} -> {content.Exception?.Message ?? content.Result}\n");
                    break;

                case ToolApprovalRequestContent content:
                    Write(ConsoleColor.Yellow, $"\n[approval requested] {DescribeToolCall(content.ToolCall)}\n");
                    break;

                case ToolApprovalResponseContent content:
                    Write(ConsoleColor.Yellow, $"[approval {(content.Approved ? "granted" : "denied")}] {DescribeToolCall(content.ToolCall)}\n");
                    break;

                case InputRequestContent content:
                    Write(ConsoleColor.Green, $"\n[input requested] {content.RequestId}\n");
                    break;

                case InputResponseContent content:
                    Write(ConsoleColor.Green, $"[input provided] {content.RequestId}\n");
                    break;

                case ErrorContent content:
                    Write(ConsoleColor.Red, $"\n[error] {content.ErrorCode}: {content.Message}\n");
                    break;

                case UsageContent content:
                    Write(ConsoleColor.DarkGray, $"\n[usage] in={content.Details.InputTokenCount} out={content.Details.OutputTokenCount} total={content.Details.TotalTokenCount}\n");
                    break;

                case UriContent content:
                    Console.WriteLine($"[uri] {content.Uri} ({content.MediaType})");
                    break;

                case DataContent content:
                    Console.WriteLine($"[data] {content.MediaType} ({content.Data.Length} bytes)");
                    break;

                case HostedFileContent content:
                    Console.WriteLine($"[file] {content.Name ?? content.FileId} ({content.MediaType})");
                    break;

                case McpServerToolCallContent content:
                    Write(ConsoleColor.Cyan, $"\n[mcp call] {content.ServerName}/{content.Name}\n");
                    break;

                case McpServerToolResultContent content:
                    Write(ConsoleColor.DarkCyan, $"[{content.GetType().Name}] {content.CallId}\n");
                    break;

                case ToolCallContent content:
                    Write(ConsoleColor.Cyan, $"\n[{content.GetType().Name}] {content.CallId}\n");
                    break;

                case ToolResultContent content:
                    Write(ConsoleColor.DarkCyan, $"[{content.GetType().Name}] {content.CallId}\n");
                    break;

                default:
                    Write(ConsoleColor.Magenta, $"\n[unhandled {item.GetType().Name}] {item}\n");
                    break;
            }
        }
    }

    private static string DescribeToolCall(ToolCallContent toolCall) =>
        toolCall is FunctionCallContent function
            ? $"{function.Name}({FormatArguments(function.Arguments)})"
            : $"{toolCall.GetType().Name} {toolCall.CallId}";

    private static string FormatArguments(IDictionary<string, object?>? arguments) =>
        arguments is null ? string.Empty : string.Join(", ", arguments.Select(a => $"{a.Key}={a.Value}"));

    private static void Write(ConsoleColor color, string? text)
    {
        var previous = Console.ForegroundColor;
        Console.ForegroundColor = color;
        Console.Write(text);
        Console.ForegroundColor = previous;
    }
}
