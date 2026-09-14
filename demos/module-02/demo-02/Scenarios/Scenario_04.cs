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
    }
}
