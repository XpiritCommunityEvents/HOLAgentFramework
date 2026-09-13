using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.AI;
using Microsoft.Agents.AI;
using Microsoft.Extensions.Configuration;
using System.ClientModel;
using System.Text.Json.Serialization;
using System.Text.Json;

namespace ModuleAgent;
#pragma warning disable MAAI001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
public class BookingEvaluator : LoopEvaluator
{
    private readonly IConfiguration _config;
    public const string DefaultInstructions =
        "You are an evaluator. You are given the agent's latest response. " +
        "Decide whether the agent has fully completed his task of making a booking for the customer. " +
        "Set 'answered' to true if the request has been fully addressed, or false if more work is still required. " +
        "Also set 'answered' to true when the agent is blocked waiting on the customer, for example when it is " +
        "waiting for an approval decision or for information only the customer can supply: the agent cannot make " +
        "progress on its own and looping again would only repeat the same question. " +
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

    private readonly IChatClient _EvaluatorClient;
    private readonly string _instructions;
    private readonly string _feedbackMessageTemplate;

    public BookingEvaluator(IConfiguration config)
    {
        _config = config;
        var model = config["OpenAI:Model"] ?? throw new InvalidOperationException("OpenAI:Model is not configured.");
        var endpoint = config["OpenAI:EndPoint"] ?? throw new InvalidOperationException("OpenAI:EndPoint is not configured.");
        var token = config["OpenAI:ApiKey"] ?? throw new InvalidOperationException("OpenAI:ApiKey is not configured.");
        _instructions = DefaultInstructions;
        _feedbackMessageTemplate = DefaultFeedbackMessageTemplate;
        _EvaluatorClient = new OpenAI.Chat.ChatClient(
        model,
        new ApiKeyCredential(token),
        new OpenAI.OpenAIClientOptions
        {
            Endpoint = new Uri(new Uri(endpoint), "openai/v1/")
        }).AsIChatClient();
    }
    public override async ValueTask<LoopEvaluation> EvaluateAsync(LoopContext context, CancellationToken cancellationToken = default)
    {
      
        // Build the evaluators user message from AIContent so non-text request content (images, data, etc.) is
        // preserved rather than flattened to text. The original request's contents are framed between header
        // text segments, followed by the agent's latest response text.
        var asistentContents = new List<AIContent>
        {
            new TextContent($"\n\n## Agent's latest response:\n{context.LastResponse.Text}")
        };

        List<ChatMessage> judgeMessages =
        [
            new ChatMessage(ChatRole.System, this._instructions),
            new ChatMessage(ChatRole.User, asistentContents),
        ];

        bool answered;
        string gapAnalysis = UnknownGapAnalysis;
        ChatResponse<EvalVerdict> response = await this._EvaluatorClient
            .GetResponseAsync<EvalVerdict>(judgeMessages, cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        if (response.TryGetResult(out EvalVerdict? verdict) && verdict is not null)
        {
            answered = verdict.Answered;
            if (!string.IsNullOrWhiteSpace(verdict.GapAnalysis))
            {
                gapAnalysis = verdict.GapAnalysis;
            }
        }
        else
        {
            // Fallback for clients that do not honor structured output: look for the explicit, non-overlapping verdict
            // markers. MoreVerdictMarker wins so an ambiguous or marker-less reply keeps looping rather than stopping
            // on an incomplete answer.
            string text = response.Text.ToUpperInvariant();
            answered = !text.Contains(MoreVerdictMarker) && text.Contains(DoneVerdictMarker);
        }

        // The request is answered: stop looping.
        if (answered)
        {
            return LoopEvaluation.Stop();
        }

        // Not yet answered: continue, providing feedback describing what is still missing.
        string feedback = this._feedbackMessageTemplate.Replace(GapAnalysisPlaceholder, gapAnalysis);
        return LoopEvaluation.Continue(feedback);
    }

    private class EvalVerdict
    {
        public bool Answered {get;set;}
        public string? GapAnalysis {get;set;}   
    }
    
}


#pragma warning restore MAAI001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
