using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace AgentFramework101;

/// <summary>
/// Helper class for handling and pretty-printing agent response updates.
/// </summary>
internal static class AgentResponseHelper
{
    /// <summary>
    /// Pretty prints the agent response update to the console, with color coding for different content types.
    /// </summary>
    public static void HandleResponseUpdate(AgentResponseUpdate agentResponse)
    {
        foreach (var item in agentResponse.Contents)
        {
            // Ordered most-derived first; ToolCallContent/ToolResultContent catch the remaining built-in tool types.
            switch (item)
            {
                case TextContent content:
                    Console.Write(content.Text);
                    break;

                case TextReasoningContent content:
                    Write(ConsoleColor.DarkGray, content.Text);
                    break;

                case FunctionCallContent content:
                    Write(ConsoleColor.Cyan, $"\n[call] {content.Name}({FormatArguments(content.Arguments)})\n");
                    break;

                case FunctionResultContent content:
                    Write(ConsoleColor.DarkCyan, $"[result] {content.CallId} -> {content.Exception?.Message ?? content.Result}\n");
                    break;

                case ToolApprovalRequestContent content:
                    Write(ConsoleColor.Yellow, $"\n[approval requested] {DescribeToolCall(content.ToolCall)}\n");
                    break;

                case ToolApprovalResponseContent content:
                    Write(ConsoleColor.Yellow, $"[approval {(content.Approved ? "granted" : "denied")}] {DescribeToolCall(content.ToolCall)}\n");
                    break;

                case InputRequestContent content:
                    Write(ConsoleColor.Green, $"\n[input requested] {content.RequestId}\n");
                    break;

                case InputResponseContent content:
                    Write(ConsoleColor.Green, $"[input provided] {content.RequestId}\n");
                    break;

                case ErrorContent content:
                    Write(ConsoleColor.Red, $"\n[error] {content.ErrorCode}: {content.Message}\n");
                    break;

                case UsageContent content:
                    Write(ConsoleColor.DarkGray, $"\n[usage] in={content.Details.InputTokenCount} out={content.Details.OutputTokenCount} total={content.Details.TotalTokenCount}\n");
                    break;

                case UriContent content:
                    Console.WriteLine($"[uri] {content.Uri} ({content.MediaType})");
                    break;

                case DataContent content:
                    Console.WriteLine($"[data] {content.MediaType} ({content.Data.Length} bytes)");
                    break;

                case HostedFileContent content:
                    Console.WriteLine($"[file] {content.Name ?? content.FileId} ({content.MediaType})");
                    break;

                case McpServerToolCallContent content:
                    Write(ConsoleColor.Cyan, $"\n[mcp call] {content.ServerName}/{content.Name}\n");
                    break;

                case McpServerToolResultContent content:
                    Write(ConsoleColor.DarkCyan, $"[{content.GetType().Name}] {content.CallId}\n");
                    break;

                case ToolCallContent content:
                    Write(ConsoleColor.Cyan, $"\n[{content.GetType().Name}] {content.CallId}\n");
                    break;

                case ToolResultContent content:
                    Write(ConsoleColor.DarkCyan, $"[{content.GetType().Name}] {content.CallId}\n");
                    break;

                default:
                    Write(ConsoleColor.Magenta, $"\n[unhandled {item.GetType().Name}] {item}\n");
                    break;
            }
        }
    }

    private static string DescribeToolCall(ToolCallContent toolCall) =>
        toolCall is FunctionCallContent function
            ? $"{function.Name}({FormatArguments(function.Arguments)})"
            : $"{toolCall.GetType().Name} {toolCall.CallId}";

    private static string FormatArguments(IDictionary<string, object?>? arguments) =>
        arguments is null ? string.Empty : string.Join(", ", arguments.Select(a => $"{a.Key}={a.Value}"));

    private static void Write(ConsoleColor color, string? text)
    {
        var previous = Console.ForegroundColor;
        Console.ForegroundColor = color;
        Console.Write(text);
        Console.ForegroundColor = previous;
    }
}