using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using ModelContextProtocol.Client;

namespace AgentFramework101;

internal static class Scenario_03
{
    public static async Task Run(IChatClient chatClient, IConfiguration configuration)
    {
        Console.WriteLine("SCENARIO 03 --- MCP integration");

        var githubToken = configuration["GitHubToken"]
            ?? throw new InvalidOperationException("Set GitHubToken in user secrets.");

        await using McpClient mcpClient = await McpClient.CreateAsync(
            new HttpClientTransport(new HttpClientTransportOptions
            {
                Name = "GitHub",
                Endpoint = new Uri("https://api.githubcopilot.com/mcp/"),
                AdditionalHeaders = new Dictionary<string, string>
                {
                    ["Authorization"] = $"Bearer {githubToken}"
                }
            }));

        IList<McpClientTool> tools = await mcpClient.ListToolsAsync();

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
