using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace ModuleAgent;

internal static class AgentResponsePrinter
{
    public static void PrintResponseUpdate(AgentResponseUpdate agentResponse)
    {
        foreach (var item in agentResponse.Contents)
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

    public static string DescribeToolCall(ToolCallContent toolCall) =>
        toolCall is FunctionCallContent function
            ? $"{function.Name}({FormatArguments(function.Arguments)})"
            : $"{toolCall.GetType().Name} {toolCall.CallId}";

    public static void Write(ConsoleColor color, string? text)
    {
        var previous = Console.ForegroundColor;
        Console.ForegroundColor = color;
        Console.Write(text);
        Console.ForegroundColor = previous;
    }

    private static string FormatArguments(IDictionary<string, object?>? arguments) =>
        arguments is null ? string.Empty : string.Join(", ", arguments.Select(a => $"{a.Key}={a.Value}"));

     internal static void PrintResponse(AgentResponse response)
    {
        //implement the printing of the agent response
        // using the same option as in PrintResponseUpdate
        foreach (var item in response.Messages.SelectMany(m => m.Contents))
        {
            switch (item)
            {
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
}