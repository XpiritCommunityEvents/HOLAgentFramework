using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;

namespace Module04.Demo05.Orchestration;

internal static class ConsoleAgentInteraction
{
    public static async Task HandleWorkflowAsync(StreamingRun run, Action<WorkflowEvent>? onEvent = null)
    {
        var printer = new AgentResponsePrinter();
        await foreach (WorkflowEvent evt in run.WatchStreamAsync())
        {
            printer.PrintWorkflowEvent(evt);
            onEvent?.Invoke(evt);

            if (evt is RequestInfoEvent request)
            {
                if (!request.Request.TryGetDataAs<ToolApprovalRequestContent>(out var approval))
                {
                    throw new InvalidOperationException(
                        $"Unsupported workflow request type '{request.Request.PortInfo.RequestType.TypeName}' " +
                        $"on port '{request.Request.PortInfo.PortId}'.");
                }

                await run.SendResponseAsync(request.Request.CreateResponse(PromptForApproval(approval)));
            }
        }
    }

    public static ToolApprovalResponseContent PromptForApproval(ToolApprovalRequestContent approval)
    {
        Console.Write($"\nApprove {AgentResponsePrinter.DescribeToolCall(approval.ToolCall)}? [y/N] ");
        bool approved = Console.ReadLine()?.Trim().StartsWith("y", StringComparison.OrdinalIgnoreCase) is true;
        return approval.CreateResponse(approved, approved ? "Approved." : "Rejected.");
    }
}
