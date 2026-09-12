using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace AgentFramework101;

/// <summary>
/// Scenario 01: synchronous and streaming agent interaction with session.
/// </summary>
internal static class Scenario_01
{
    public static async Task Run(IChatClient chatClient)
    {
        Console.WriteLine("SCENARIO 01 --- synchronous, structured and streaming responses and sessions");

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
                }
            });

        //var runOptions = new ChatClientAgentRunOptions(new ChatOptions
        //{
        //    MaxOutputTokens = 500,
        //    Temperature = 0.5f,
        //    TopP = 1.0f,
        //    FrequencyPenalty = 0.0f,
        //    PresencePenalty = 0.0f
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

            #region Structured responses
            // synchronous structured response:
            // var structuredResponse = await agent.RunAsync<ShowSummary>(prompt, session);
            // Console.WriteLine($"Artist: {structuredResponse.Result.Artist}");
            // Console.WriteLine($"Title: {structuredResponse.Result.Title}");
            // Console.WriteLine($"Venue: {structuredResponse.Result.Venue}");
            // Console.WriteLine($"Description: {structuredResponse.Result.Description}");
            // Console.WriteLine($"Date: {structuredResponse.Result.Date}");
            #endregion

            #region Streaming responses
            // streaming response:
            //await foreach (AgentResponseUpdate update in agent.RunStreamingAsync(prompt, session))
            //{
            //    Console.Write(update);
            //}
            #endregion

            Console.WriteLine();
        }
    }
}
