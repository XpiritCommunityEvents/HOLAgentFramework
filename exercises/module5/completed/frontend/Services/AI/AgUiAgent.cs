using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace GloboTicket.Frontend.Services.AI;

public static class AgUiAgent
{
    public static AIAgent WithSessionBoundApprovals(AIAgent agent) => agent.AsBuilder().Use(
        runFunc: (messages, session, options, next, token) =>
            next.RunAsync(WithoutClientApprovalArtifacts(messages), session, options, token),
        runStreamingFunc: (messages, session, options, next, token) =>
            next.RunStreamingAsync(WithoutClientApprovalArtifacts(messages), session, options, token)).Build();

    private static IEnumerable<ChatMessage> WithoutClientApprovalArtifacts(IEnumerable<ChatMessage> messages)
    {
        var input = messages.ToList();
        var resumedCallIds = input.SelectMany(message => message.Contents)
            .OfType<ToolApprovalResponseContent>()
            .Select(response => response.ToolCall).OfType<FunctionCallContent>()
            .Select(call => call.CallId).ToHashSet(StringComparer.Ordinal);

        // The pinned AG-UI adapter synthesizes approval requests on resume. MAF 1.28
        // binds responses to the server's stored request; replaying both duplicates its key.
        // CopilotKit also inserts the approval payload as a tool result. It is not a
        // receipt: leaving it in history makes the framework skip the real invocation.
        bool IsClientArtifact(AIContent content) => content is ToolApprovalRequestContent ||
            content is FunctionResultContent result && resumedCallIds.Contains(result.CallId);

        foreach (var message in input)
        {
            if (!message.Contents.Any(IsClientArtifact))
            {
                yield return message;
                continue;
            }
            var contents = message.Contents.Where(content => !IsClientArtifact(content)).ToList();
            if (contents.Count == 0) continue;
            var filtered = message.Clone();
            filtered.Contents = contents;
            yield return filtered;
        }
    }
}
