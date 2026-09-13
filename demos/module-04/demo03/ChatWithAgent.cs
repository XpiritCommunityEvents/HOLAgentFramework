using System.ClientModel;
using System.ClientModel.Primitives;
using System.ComponentModel;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OpenAI.Chat;
using ChatMessage = Microsoft.Extensions.AI.ChatMessage;
#pragma warning disable MAAI001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.


namespace ModuleAgent;

internal class ChatWithAgent
{
    public async Task LetAgentFindRideAndHotel(IConfiguration config)
    {
        var question = """
        I have tickets to Martin Garrix, you can find them here: https://github.com/vriesmarcel/vslive-2026-vegas-sk/blob/main/CT4EB6AF_mobile_267842.pdf
        """;

        Console.WriteLine("******** Create the agent ***********");
        var transportationAgent = CreateTransportationAgent(config);

        Console.WriteLine("******** Start the agent ***********");
        var session = await transportationAgent.CreateSessionAsync();

        List<ChatMessage> input = [new ChatMessage(ChatRole.User, question)];

        // Each pass runs until the agent needs something from the user; the reply becomes the next pass's input.
        while (input.Count > 0)
        {
            var updates = new List<AgentResponseUpdate>();
            await foreach (var item in transportationAgent.RunStreamingAsync(input, session))
            {
                HandleResponseUpdate(item);
                updates.Add(item);
            }

            input = CollectUserReply(updates.ToAgentResponse());
        }

        Console.WriteLine("******** Done ***********");
    }

    private static List<ChatMessage> CollectUserReply(AgentResponse response)
    {
        var approvals = response.Messages
            .SelectMany(message => message.Contents)
            .OfType<ToolApprovalRequestContent>()
            .ToList();

        if (approvals.Count > 0)
        {
            List<AIContent> replies = [];
            foreach (var request in approvals)
            {
                Write(ConsoleColor.Yellow, $"\nApprove {DescribeToolCall(request.ToolCall)}? (y/n) ");
                var approved = (Console.ReadLine() ?? "n").TrimStart().StartsWith('y');
                replies.Add(request.CreateResponse(approved, approved ? "Approved by user." : "Denied by user."));
            }

            return [new ChatMessage(ChatRole.User, replies)];
        }

        Write(ConsoleColor.Green, "\nyou> ");
        var answer = Console.ReadLine();
        return string.IsNullOrWhiteSpace(answer) ? [] : [new ChatMessage(ChatRole.User, answer)];
    }

    private static void HandleResponseUpdate(AgentResponseUpdate agentResponse)
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

    private AIAgent CreateTransportationAgent(IConfiguration config)
    {
      
        var harnessAgentInstructions ="""
        You are an expert in coordinating interactions between the transportation and hotel booking agents.
        You will ensure that the user receives consistent and accurate information from both agents.
        Never ask the user a question as plain text: nobody reads it. Whenever you need information from
        the user, call the AskForUserInput function and wait for its result. Booking functions request
        approval on their own, so call them directly instead of asking for permission in text first.

        When the user references a document by URL, call DownloadFile to fetch it into the working
        directory first, then use the matching skill to read it. For a PDF ticket, load the 'pdf'
        skill and call run_skill_script with scriptName='scripts/extract_text.py' and the downloaded
        PDF path as its only argument. Use the returned text to identify the event date, venue, and
        city. Do not call check_bounding_boxes.py, check_fillable_fields.py, or extract_form_*.py for
        a ticket. Use convert_pdf_to_images.py only after extract_text.py reports no text layer, and
        only when the required image/OCR dependencies are available. Never guess document contents.

        You are done the moment both a hotel and a ride have been successfully booked or when the user
        cancels the process.
        """;

        var model = config["OpenAI:Model"] ?? throw new InvalidOperationException("OpenAI:Model is not configured.");
        var endpoint = config["OpenAI:EndPoint"] ?? throw new InvalidOperationException("OpenAI:EndPoint is not configured.");
        var token = config["OpenAI:ApiKey"] ?? throw new InvalidOperationException("OpenAI:ApiKey is not configured.");

        var chatclient = new OpenAI.Chat.ChatClient(
        model,
        new ApiKeyCredential(token),

        new OpenAI.OpenAIClientOptions
        {
            Endpoint = new Uri(new Uri(endpoint), "openai/v1/")
        })
        .AsIChatClient();

        var agent = chatclient.AsHarnessAgent( new HarnessAgentOptions()
        {
            HarnessInstructions = harnessAgentInstructions,
            //BackgroundAgents = [rideFinderAgent, hotelFinderAgent],
            DisableWebSearch = true,
            // Without this the harness auto-approves every tool call, so approval requests never reach the caller.
            DisableToolAutoApproval = true,
            // Discovers demo03/skills/*/SKILL.md and gives the harness load_skill / read_skill_resource / run_skill_script.
            AgentSkillsSource = new AgentFileSkillsSource(
                PdfSkillSupport.SkillsDirectory,
                PdfSkillSupport.RunPythonScriptAsync,
                new AgentFileSkillsSourceOptions { AllowedScriptExtensions = [".py"] }),
            // Lets the agent read files the skill scripts produce, rooted at the sandbox directory.
            FileAccessStore = new FileSystemAgentFileStore(PdfSkillSupport.WorkingDirectory),
            // Reading skill text is harmless; executing a script still prompts the user.
            ToolApprovalAgentOptions = new ToolApprovalAgentOptions
            {
                AutoApprovalRules = [AgentSkillsProvider.ReadOnlyToolsAutoApprovalRule],
            },
            LoopEvaluators = [new BookingEvaluator(config)],
            Description = "An agent that helps users find a hotel and transportation options based on a concert booking.",
            ChatOptions = new ChatOptions
            {
                Tools = [
                            AIFunctionFactory.Create(AskForUserInput),
                            AIFunctionFactory.Create(PdfSkillSupport.DownloadFile),
                            AIFunctionFactory.Create(RideInformationSystemService.GetAvailableRides),
                            new ApprovalRequiredAIFunction(AIFunctionFactory.Create(RideInformationSystemService.BookARide)),
                            AIFunctionFactory.Create(HotelBookingFunctions.SelectRoomPreference),
                            new ApprovalRequiredAIFunction(AIFunctionFactory.Create(HotelBookingFunctions.BookSelectedRoom)),
                            AIFunctionFactory.Create(HotelBookingFunctions.GetApprovalForBooking)
                        ],
         //       Reasoning = new() { Effort = ReasoningEffort.Medium },
            },
        } );
        
        return agent;
    }

    [Description("Asks the user for input based on the provided question.")]
    public string AskForUserInput([Description("The question to ask the user")]string questionToASk)
    {
        Console.WriteLine(questionToASk);
        return Console.ReadLine() ?? string.Empty;
    }
    
    [Description("Displays a message to the user. Use this to show your output to the end user")]
    public void ShowMessage([Description("The message to display")]string message)
    {
        Console.WriteLine($"your input>: {message}");
    }
}
#pragma warning restore MAAI001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
