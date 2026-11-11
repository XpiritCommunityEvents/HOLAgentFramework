using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OpenAI.Responses;
#pragma warning disable OPENAI001 // The pinned SDK marks the Responses API as experimental.

namespace AgentFramework101.Scenarios;

/// <summary>
/// Scenario 02: context providers, streaming
/// </summary>
internal static class Scenario_02
{
    public static async Task Run(ResponsesClient responsesClient, string model)
    {
        Console.WriteLine("SCENARIO 02 --- function calling, approvals, skills and middleware");

        AIAgent agent = responsesClient
            .AsAIAgent(new ChatClientAgentOptions
            {
                Name = "GloboTicketAssistant",
                AIContextProviders = [new MyContextProvider()],
                ChatOptions = new ChatOptions
                {
                    Instructions = """
                        You are a digital assistant for GloboTicket, a concert ticketing company. You help customers with their ticket purchasing.
                        Tone: warm and friendly, but to the point. Do not make things up when you don't know the answer. Just tell the user that 
                        you don't know the answer based on your knowledge.
                        Load an available skill when its description matches the user's request.
                        """
                }
            }, model: model);

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

            // streaming response:
            await foreach (AgentResponseUpdate update in agent.RunStreamingAsync(prompt, session))
            {
                Console.Write(update);

                //AgentResponseHelper.HandleResponseUpdate(update);
            }

            Console.WriteLine();
        }
    }
}