using System.ClientModel;
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
    private readonly IConfiguration _config;
    public const string DefaultInstructions =
        "You are an evaluator for a travel-booking agent. You are given the customer's latest input, the agent's " +
        "tool activity, and the agent's latest response. The agent's task is to book a hotel room and a ride for " +
        "the customer. The customer already owns the event tickets; ticket purchases never need to be verified. " +
        "Set 'answered' to true in any of these cases: " +
        "(1) the tool activity shows both a hotel room and a ride were booked successfully — treat the booking " +
        "tools' success results as authoritative and do not demand confirmation numbers, stay dates, receipts, or " +
        "other details those tools do not return; " +
        "(2) the customer indicates they are done, satisfied, or wants to stop (for example 'I'm good', 'we are " +
        "good', 'stop', 'thanks', 'bye'), or cancels the process; " +
        "(3) the agent is blocked waiting on the customer, for example when it is waiting for an approval decision " +
        "or for information only the customer can supply: the agent cannot make progress on its own and looping " +
        "again would only repeat the same question. " +
        "Set 'answered' to false only when booking work the agent can still perform on its own remains outstanding. " +
        "When 'answered' is false, use 'gapAnalysis' to explain what is still missing or what work remains. " +
        "If you cannot return structured output, reply with " + DoneVerdictMarker + " when the request has been fully " +
        "addressed, or " + MoreVerdictMarker + " when more work is still required." +
        CriteriaPlaceholder;

    /// <summary>
    /// The verdict marker the judge is asked to emit (for clients that do not honor structured output) when the
    /// original request has been fully addressed.
    /// </summary>
    /// <remarks>
    /// <see cref="DoneVerdictMarker"/> and <see cref="MoreVerdictMarker"/> are deliberately non-overlapping (neither is
    /// a substring of the other), so the text fallback cannot misclassify one verdict as the other. When the marker is
    /// ambiguous or absent, <see cref="MoreVerdictMarker"/> wins so the loop keeps running rather than stopping on an
    /// incomplete answer.
    /// </remarks>
    public const string DoneVerdictMarker = "VERDICT: DONE";

    /// <summary>
    /// The verdict marker the judge is asked to emit (for clients that do not honor structured output) when more work
    /// is still required. Takes precedence over <see cref="DoneVerdictMarker"/> when both (or neither) are present.
    /// </summary>
    public const string MoreVerdictMarker = "VERDICT: MORE";

    /// <summary>
    /// The placeholder token within <see cref="DefaultInstructions"/> (or a custom
    /// <see cref="AIJudgeLoopEvaluatorOptions.Instructions"/>) that is replaced with the rendered
    /// <see cref="AIJudgeLoopEvaluatorOptions.Criteria"/>. When no criteria are supplied, the placeholder is removed.
    /// </summary>
    public const string CriteriaPlaceholder = "{criteria}";

    /// <summary>
    /// The placeholder token within <see cref="DefaultFeedbackMessageTemplate"/> (or a custom
    /// <see cref="AIJudgeLoopEvaluatorOptions.FeedbackMessageTemplate"/>) that is replaced with the judge's gap analysis.
    /// </summary>
    public const string GapAnalysisPlaceholder = "{gap_analysis}";

    /// <summary>The default template used to build the feedback produced when the request is not yet answered.</summary>
    public const string DefaultFeedbackMessageTemplate =
        "Your previous response did not fully address the original request. " +
        "The following is still missing or incomplete: " + GapAnalysisPlaceholder + " " +
        "Please continue and fully address the original request.";

    /// <summary>The value substituted for the gap analysis when the judge did not provide one.</summary>
    private const string UnknownGapAnalysis = "<unknown>";

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
        // Build the judge's user message from the loop context. The agent's bare response text is not enough to
        // judge completion: successful BookRoom/BookARide results and the customer's own words (new user messages
        // and AskForUserInput tool results such as "I'm good, stop") only appear in the initial messages and the
        // last response's tool contents, so surface those to the judge as well.
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

        List<ChatMessage> judgeMessages =
        [
            new ChatMessage(ChatRole.System, this._instructions),
            new ChatMessage(ChatRole.User, evidence.ToString()),
        ];

        bool answered;
        string gapAnalysis = UnknownGapAnalysis;
        AgentResponse<EvalVerdict> response = await this._EvaluatorAgent
            .RunAsync<EvalVerdict>(judgeMessages, cancellationToken: cancellationToken)
            .ConfigureAwait(false);

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

    private class EvalVerdict
    {
        public bool Answered { get; set; }
        public string? GapAnalysis { get; set; }
    }
}

#pragma warning restore MAAI001
#pragma warning restore OPENAI001
