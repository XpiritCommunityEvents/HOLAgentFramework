using System.ClientModel;
using System.Text;
using System.Text.Json;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using OpenAI.Responses;

#pragma warning disable OPENAI001 // The pinned SDK marks the Responses API as experimental.
#pragma warning disable MAAI001 // Loop APIs are experimental.

namespace ModuleAgent;

public class BookingEvaluator : LoopEvaluator
{
    private const string Instructions =
        "You are an evaluator for a travel-booking agent. You are given the customer's latest input, the agent's " +
        "tool activity, and the agent's latest response. The agent's task is to book a hotel room and a ride for " +
        "the customer. The customer already owns the event tickets; ticket purchases never need to be verified. " +
        "Set 'answered' to true in any of these cases: " +
        "(1) the tool activity shows both a hotel room and a ride were booked successfully - treat the booking " +
        "tools' success results as authoritative and do not demand confirmation numbers, stay dates, receipts, or " +
        "other details those tools do not return; " +
        "(2) the customer indicates they are done, satisfied, or wants to stop (for example 'I'm good', 'we are " +
        "good', 'stop', 'thanks', 'bye'), or cancels the process; " +
        "(3) the agent is blocked waiting on the customer, for example on an approval decision or information only " +
        "the customer can supply: looping again would only repeat the same question. " +
        "Set 'answered' to false only when booking work the agent can still perform on its own remains outstanding. " +
        "When false, use 'gapAnalysis' to explain what remains. " +
        "If structured output is unavailable, respond with VERDICT: DONE or VERDICT: MORE.";

    private readonly AIAgent _evaluatorAgent;

    public BookingEvaluator(IConfiguration config)
    {
        var model = config["OpenAI:Model"] ?? throw new InvalidOperationException("OpenAI:Model is not configured.");
        var endpoint = config["OpenAI:Endpoint"] ?? throw new InvalidOperationException("OpenAI:Endpoint is not configured.");
        var token = config["OpenAI:ApiKey"] ?? throw new InvalidOperationException("OpenAI:ApiKey is not configured.");

        _evaluatorAgent = new OpenAI.OpenAIClient(
            new ApiKeyCredential(token),
            new OpenAI.OpenAIClientOptions { Endpoint = new Uri(new Uri(endpoint), "openai/v1/") })
            .GetResponsesClient()
            .AsAIAgent(model: model, instructions: Instructions, name: "BookingEvaluator");
    }

    internal BookingEvaluator(AIAgent evaluatorAgent) => _evaluatorAgent = evaluatorAgent;

    public override async ValueTask<LoopEvaluation> EvaluateAsync(LoopContext context, CancellationToken cancellationToken = default)
    {
        // A pending approval must reach the caller, even when the response has no text.
        if (context.LastResponse.Messages.SelectMany(message => message.Contents)
            .OfType<ToolApprovalRequestContent>().Any())
        {
            return LoopEvaluation.Stop();
        }

        var response = await _evaluatorAgent.RunAsync<EvalVerdict>(
            BuildEvidence(context), cancellationToken: cancellationToken).ConfigureAwait(false);

        EvalVerdict? verdict = null;
        try
        {
            verdict = response.Result;
        }
        catch (JsonException)
        {
            // Some providers return a marker instead of the requested JSON.
        }

        bool answered = verdict?.Answered ??
            (!response.Text.Contains("VERDICT: MORE", StringComparison.OrdinalIgnoreCase) &&
             response.Text.Contains("VERDICT: DONE", StringComparison.OrdinalIgnoreCase));

        return answered
            ? LoopEvaluation.Stop()
            : LoopEvaluation.Continue($"The booking is incomplete: {verdict?.GapAnalysis ?? "<unknown>"}. Please continue.");
    }

    /// <summary>
    /// Builds the judge's user message from the loop context. The agent's bare response text is not enough to judge
    /// completion: successful BookRoom/BookARide results and the customer's own words (new user messages and
    /// AskForUserInput tool results such as "I'm good, stop") only appear in the initial messages and the last
    /// response's tool contents, so those are surfaced to the judge as well.
    /// </summary>
    private static string BuildEvidence(LoopContext context)
    {
        var evidence = new StringBuilder();

        evidence.AppendLine("## Customer's latest input:");
        foreach (ChatMessage message in context.InitialMessages)
        {
            if (!string.IsNullOrWhiteSpace(message.Text))
            {
                evidence.AppendLine(message.Text);
            }
        }

        evidence.AppendLine();
        evidence.AppendLine("## Agent's tool activity during the latest response:");
        var toolCallNames = new Dictionary<string, string>();
        foreach (ChatMessage message in context.LastResponse.Messages)
        {
            foreach (AIContent content in message.Contents)
            {
                switch (content)
                {
                    case FunctionCallContent call:
                        toolCallNames[call.CallId] = call.Name;
                        evidence.AppendLine($"[tool call] {call.Name}");
                        break;
                    case FunctionResultContent result:
                        string toolName = toolCallNames.TryGetValue(result.CallId, out string? name) ? name : "<unknown tool>";
                        evidence.AppendLine($"[tool result] {toolName} -> {result.Result}");
                        break;
                }
            }
        }

        evidence.AppendLine();
        evidence.AppendLine("## Agent's latest response:");
        evidence.AppendLine(context.LastResponse.Text);

        return evidence.ToString();
    }

    private class EvalVerdict
    {
        public bool Answered { get; set; }
        public string? GapAnalysis { get; set; }
    }
}

#pragma warning restore MAAI001
#pragma warning restore OPENAI001
