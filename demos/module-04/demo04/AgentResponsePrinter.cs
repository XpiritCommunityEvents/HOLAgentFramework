using System.Text.Json;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;

namespace Module04.Demo05;

internal sealed class AgentResponsePrinter
{
    private readonly object _consoleLock = new();
    private string? _lastAgent;

    public void PrintWorkflowEvent(WorkflowEvent evt)
    {
        switch (evt)
        {
            case AgentResponseUpdateEvent update:
                PrintResponseUpdate(update.Update, update.ExecutorId);
                break;
            case ExecutorInvokedEvent invoked:
                Write(ConsoleColor.DarkGray, $"\n[started] {invoked.ExecutorId}\n");
                break;
            case ExecutorCompletedEvent completed:
                Write(ConsoleColor.DarkGray, $"\n[completed] {completed.ExecutorId}\n");
                break;
            case WorkflowErrorEvent error:
                Write(ConsoleColor.Red, $"\n[workflow error] {error.Exception}\n");
                break;
        }
    }

    public void PrintResponseUpdate(AgentResponseUpdate update, string? agentName = null)
    {
        lock (_consoleLock)
        {
            if (update.Contents.Count == 0)
            {
                return;
            }

            string agent = update.AuthorName ?? agentName ?? update.AgentId ?? "agent";
            if (!string.Equals(_lastAgent, agent, StringComparison.Ordinal))
            {
                Write(ConsoleColor.White, $"\n[{agent}]\n");
                _lastAgent = agent;
            }

            foreach (AIContent item in update.Contents)
            {
                switch (item)
                {
                    case TextContent content:
                        Console.Write(content.Text);
                        break;
                    case TextReasoningContent content:
                        Write(ConsoleColor.DarkGray, $"\n[reasoning] {content.Text}");
                        break;
                    case FunctionCallContent content:
                        Write(ConsoleColor.Cyan, $"\n[call] {content.Name}({FormatArguments(content.Arguments)}) id={content.CallId}\n");
                        break;
                    case FunctionResultContent content:
                        Write(content.Exception is null ? ConsoleColor.DarkCyan : ConsoleColor.Red,
                            $"\n[result] {content.CallId} -> {content.Exception?.Message ?? JsonSerializer.Serialize(content.Result)}\n");
                        break;
                    case ToolApprovalRequestContent content:
                        Write(ConsoleColor.Yellow, $"\n[approval requested] {DescribeToolCall(content.ToolCall)}\n");
                        break;
                    case ToolApprovalResponseContent content:
                        Write(ConsoleColor.Yellow, $"\n[approval {(content.Approved ? "granted" : "denied")}] {DescribeToolCall(content.ToolCall)}\n");
                        break;
                    case InputRequestContent content:
                        Write(ConsoleColor.Green, $"\n[input requested] {content.RequestId}\n");
                        break;
                    case InputResponseContent content:
                        Write(ConsoleColor.Green, $"\n[input provided] {content.RequestId}\n");
                        break;
                    case ErrorContent content:
                        Write(ConsoleColor.Red, $"\n[error] {content.ErrorCode}: {content.Message}\n");
                        break;
                    case UsageContent content:
                        Write(ConsoleColor.DarkGray, $"\n[usage] in={content.Details.InputTokenCount} out={content.Details.OutputTokenCount} total={content.Details.TotalTokenCount}\n");
                        break;
                    case UriContent content:
                        Console.WriteLine($"\n[uri] {content.Uri} ({content.MediaType})");
                        break;
                    case DataContent content:
                        Console.WriteLine($"\n[data] {content.MediaType} ({content.Data.Length} bytes)");
                        break;
                    case HostedFileContent content:
                        Console.WriteLine($"\n[file] {content.Name ?? content.FileId} ({content.MediaType})");
                        break;
                    case McpServerToolCallContent content:
                        Write(ConsoleColor.Cyan, $"\n[mcp call] {content.ServerName}/{content.Name} id={content.CallId}\n");
                        break;
                    case ToolCallContent content:
                        Write(ConsoleColor.Cyan, $"\n[{content.GetType().Name}] {content.CallId}\n");
                        break;
                    case ToolResultContent content:
                        Write(ConsoleColor.DarkCyan, $"\n[{content.GetType().Name}] {content.CallId}\n");
                        break;
                    default:
                        Write(ConsoleColor.Magenta, $"\n[unhandled {item.GetType().Name}] {item}\n");
                        break;
                }
            }
        }
    }

    internal static string DescribeToolCall(ToolCallContent toolCall) =>
        toolCall is FunctionCallContent function
            ? $"{function.Name}({FormatArguments(function.Arguments)})"
            : $"{toolCall.GetType().Name} {toolCall.CallId}";

    private static string FormatArguments(IDictionary<string, object?>? arguments) =>
        arguments is null ? string.Empty : JsonSerializer.Serialize(arguments);

    private static void Write(ConsoleColor color, string? text)
    {
        var previous = Console.ForegroundColor;
        try
        {
            Console.ForegroundColor = color;
            Console.Write(text);
        }
        finally
        {
            Console.ForegroundColor = previous;
        }
    }
}
