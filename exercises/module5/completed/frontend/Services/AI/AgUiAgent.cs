using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace GloboTicket.Frontend.Services.AI;

public static class AgUiAgent
{
    public static AIAgent WithSessionBoundApprovals(AIAgent agent) => agent.AsBuilder().Use(
        runFunc: (messages, session, options, next, token) =>
            next.RunAsync(ApprovalResponsesOnResume(messages), session, options, token),
        runStreamingFunc: (messages, session, options, next, token) =>
            next.RunStreamingAsync(ApprovalResponsesOnResume(messages), session, options, token)).Build();

    // Workaround for the AG-UI adapter (verified on 1.28): on resume the client replays the full
    // transcript, plus a synthesized approval request and an approval-payload tool result. The session
    // already holds the conversation, so forwarding any of it duplicates the stored approval request,
    // skips the real invocation, or makes the model re-propose submit_order. Forward only the approval.
    private static IEnumerable<ChatMessage> ApprovalResponsesOnResume(IEnumerable<ChatMessage> messages)
    {
        var input = messages.ToList();
        var responses = input.Where(m => m.Contents.Any(c => c is ToolApprovalResponseContent)).ToList();
        return responses.Count > 0 ? responses : input;
    }
}
