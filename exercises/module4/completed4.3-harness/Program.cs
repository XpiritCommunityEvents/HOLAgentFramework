using System.ClientModel;
using System.ComponentModel;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using OpenAI;

IConfiguration configuration = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false)
    .AddUserSecrets<Program>()
    .Build();

string model = RequiredSetting("OpenAI:Model");
string endpoint = RequiredSetting("OpenAI:Endpoint");
string apiKey = RequiredSetting("OpenAI:ApiKey");

var openAIClient = new OpenAIClient(
    new ApiKeyCredential(apiKey),
    new OpenAIClientOptions { Endpoint = new Uri(endpoint) });
using IChatClient chatClient = openAIClient.GetChatClient(model).AsIChatClient();

AIFunction bookRide = new ApprovalRequiredAIFunction(AIFunctionFactory.Create(
    BookRide,
    "book_ride",
    "Book the agreed ride. This action requires the user's approval."));

AIAgent agent = chatClient.AsHarnessAgent(new HarnessAgentOptions
{
    Name = "TransportationHarnessAgent",
    Description = "Plans transportation and books a ride after approval.",
    HarnessInstructions = "Use a short todo list for multi-step requests. Plan in plan mode and act only in execute mode.",
    ChatOptions = new ChatOptions
    {
        Instructions = "Help the user plan a ride to an event. Ask for missing details and never claim a booking succeeded before the tool returns.",
        Tools = [bookRide]
    },
    AgentModeProviderOptions = new AgentModeProviderOptions { DefaultMode = "plan" }
});

using var cancellation = new CancellationTokenSource();
Console.CancelKeyPress += (_, eventArgs) =>
{
    eventArgs.Cancel = true;
    cancellation.Cancel();
};

AgentSession session = await agent.CreateSessionAsync(cancellation.Token);
TodoProvider todos = agent.GetService<TodoProvider>()
    ?? throw new InvalidOperationException("Harness did not provide todo support.");
AgentModeProvider modes = agent.GetService<AgentModeProvider>()
    ?? throw new InvalidOperationException("Harness did not provide mode support.");

Console.WriteLine("Harness ride assistant (starts in plan mode)");
Console.WriteLine("Commands: /plan, /execute, /todos, exit");
Console.WriteLine("Try: Plan a taxi from my hotel to a 7 PM concert.");
await ShowHarnessStateAsync();

try
{
    while (!cancellation.IsCancellationRequested)
    {
        Console.Write("\nYou: ");
        string? input = await Console.In.ReadLineAsync(cancellation.Token);
        if (input is null || input.Trim().Equals("exit", StringComparison.OrdinalIgnoreCase))
        {
            break;
        }

        input = input.Trim();
        if (input.Length == 0)
        {
            continue;
        }

        if (input is "/plan" or "/execute")
        {
            await modes.SetModeAsync(session, input[1..], cancellation.Token);
            await ShowHarnessStateAsync();
            continue;
        }

        if (input == "/todos")
        {
            await ShowHarnessStateAsync();
            continue;
        }

        AgentResponse response = await agent.RunAsync(input, session, cancellationToken: cancellation.Token);
        await CompleteTurnAsync(response);
        await ShowHarnessStateAsync();
    }
}
catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
{
    Console.WriteLine("\nConversation cancelled.");
}

async Task CompleteTurnAsync(AgentResponse response)
{
    while (true)
    {
        if (!string.IsNullOrWhiteSpace(response.Text))
        {
            Console.WriteLine($"Agent: {response.Text}");
        }

        ToolApprovalRequestContent[] requests = response.Messages
            .SelectMany(message => message.Contents)
            .OfType<ToolApprovalRequestContent>()
            .ToArray();
        if (requests.Length == 0)
        {
            return;
        }

        List<AIContent> decisions = [];
        foreach (ToolApprovalRequestContent request in requests)
        {
            Console.Write($"Approve {Describe(request.ToolCall)}? [y/N] ");
            string? answer = await Console.In.ReadLineAsync(cancellation.Token);
            bool approved = answer?.Trim().Equals("y", StringComparison.OrdinalIgnoreCase) is true;
            decisions.Add(request.CreateResponse(approved, approved ? "Approved by the user." : "Rejected by the user."));
        }

        response = await agent.RunAsync(
            new ChatMessage(ChatRole.User, decisions),
            session,
            cancellationToken: cancellation.Token);
    }
}

async Task ShowHarnessStateAsync()
{
    string mode = await modes.GetModeAsync(session, cancellation.Token);
    IReadOnlyList<TodoItem> items = await todos.GetAllTodosAsync(session, cancellation.Token);

    Console.WriteLine($"[Harness mode: {mode}; todos: {items.Count}]");
    foreach (TodoItem item in items)
    {
        Console.WriteLine($"  [{(item.IsComplete ? 'x' : ' ')}] {item.Title}");
    }
}

string RequiredSetting(string key)
{
    string? value = configuration[key];
    return string.IsNullOrWhiteSpace(value)
        ? throw new InvalidOperationException($"Missing configuration setting '{key}'.")
        : value;
}

static string Describe(ToolCallContent toolCall) => toolCall is FunctionCallContent call
    ? $"{call.Name}({string.Join(", ", call.Arguments?.Select(argument => $"{argument.Key}={argument.Value}") ?? [])})"
    : toolCall.GetType().Name;

static string BookRide(
    [Description("Pickup location.")] string pickup,
    [Description("Destination.")] string destination,
    [Description("Requested local pickup time.")] DateTime pickupTime) =>
    $"Booked a ride from {pickup} to {destination} at {pickupTime:g}. Confirmation: EXERCISE-123.";
