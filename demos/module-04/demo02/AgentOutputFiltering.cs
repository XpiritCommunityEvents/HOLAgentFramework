using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using System.Runtime.CompilerServices;

namespace ModuleWorkflow;

internal static class AgentOutputFiltering
{
    /// <summary>
    /// Removes an agent's function call/result plumbing from its streamed output so that only
    /// conversational content is chained to the next agent in a sequential workflow.
    /// </summary>
    /// <remarks>
    /// WORKAROUND for a known gap in Microsoft.Agents.AI.Workflows (verified against 1.20.0):
    /// <see cref="AgentWorkflowBuilder.BuildSequential(bool, IEnumerable{AIAgent})"/> forwards the
    /// previous agent's FunctionCallContent/FunctionResultContent/ToolApprovalRequestContent verbatim
    /// (see AIAgentHostExecutor.FilterForwardableMessages). After a tool approval round-trip, the
    /// assistant message carrying the FunctionCallContent is no longer emitted, leaving the matching
    /// 'tool' result orphaned and the provider rejects the request with HTTP 400.
    /// Tracked upstream: microsoft/agent-framework#6874 (requests a built-in filterToolCallMessages
    /// option) and #5600 / #5023 (same failure in the group chat and Python sequential paths).
    /// Delete this file and drop the WithTextOnlyOutput() call once the framework filters this itself.
    /// </remarks>
    public static AIAgent WithTextOnlyOutput(this AIAgent agent) =>
        agent.AsBuilder()
            .Use(runFunc: null, runStreamingFunc: FilterAsync)
            .Build();

    private static async IAsyncEnumerable<AgentResponseUpdate> FilterAsync(
        IEnumerable<ChatMessage> messages,
        AgentSession? session,
        AgentRunOptions? options,
        AIAgent inner,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await foreach (AgentResponseUpdate update in inner.RunStreamingAsync(messages, session, options, cancellationToken))
        {
            List<AIContent> kept = [.. update.Contents.Where(c => c is not FunctionCallContent and not FunctionResultContent)];
            if (kept.Count == 0)
            {
                continue;
            }

            update.Contents = kept;
            yield return update;
        }
    }
}
