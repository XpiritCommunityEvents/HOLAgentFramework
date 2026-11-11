using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OpenAI.Responses;
#pragma warning disable OPENAI001 // The pinned SDK marks the Responses API as experimental.

namespace AgentFramework101.Scenarios;

/// <summary>
/// Scenario 01: synchronous agent, structured responses and sessions.
/// </summary>
internal static class Scenario_01
{
    public static async Task Run(ResponsesClient responsesClient, string model)
    {
        Console.WriteLine("SCENARIO 01 --- synchronous, structured responses and sessions");

        AIAgent agent = responsesClient
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
                }
            }, model: model);

        //var runOptions = new ChatClientAgentRunOptions(new ChatOptions
        //{
        //    Reasoning = new() { Effort = ReasoningEffort.Medium },
        //    MaxOutputTokens = 500
        //});

        //AgentSession session = await agent.CreateSessionAsync();

        while (true)
        {
            Console.Write("\n> ");
            string? prompt = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(prompt))
            {
                continue;
            }

            // synchronous response:
            var response = await agent.RunAsync(prompt);
            Console.Write(response.Text);

            #region Session contents
            //var serialized = await agent.SerializeSessionAsync(session);
            //var sessionJson = JsonSerializer.Serialize(serialized, new JsonSerializerOptions { WriteIndented = true });
            //Console.WriteLine(sessionJson);
            #endregion

            #region Structured responses
            // synchronous structured response:
            //var structuredResponse = await agent.RunAsync<TicketSummary>(prompt, session);
            //Console.WriteLine($"Ticket number: {structuredResponse.Result.TicketNumber}");
            //Console.WriteLine($"Artist: {structuredResponse.Result.Artist}");
            //Console.WriteLine($"Title: {structuredResponse.Result.Title}");
            //Console.WriteLine($"Venue: {structuredResponse.Result.Venue}");
            //Console.WriteLine($"Description: {structuredResponse.Result.Description}");
            //Console.WriteLine($"Date: {structuredResponse.Result.Date}");
            #endregion

            Console.WriteLine();
        }
    }
}
