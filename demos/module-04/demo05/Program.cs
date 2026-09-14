using System.ClientModel;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Module04.Demo05.Agents;
using Module04.Demo05.Orchestration;
using OpenAI.Chat;

var config = new ConfigurationBuilder()
        .SetBasePath(Directory.GetCurrentDirectory())
        .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
        .AddUserSecrets<Program>()
        .AddEnvironmentVariables()
        .Build();

string model = config["OpenAI:Model"] ?? throw new InvalidOperationException("OpenAI:Model is not configured.");
string endpoint = config["OpenAI:EndPoint"] ?? throw new InvalidOperationException("OpenAI:EndPoint is not configured.");
string token = config["OpenAI:ApiKey"] ?? throw new InvalidOperationException("OpenAI:ApiKey is not configured.");

IChatClient chatClient = new ChatClient(
        model,
        new ApiKeyCredential(token),
        new OpenAI.OpenAIClientOptions { Endpoint = new Uri(new Uri(endpoint), "openai/v1/") })
        .AsIChatClient();

AIAgent ticketAgent = TicketReaderAgent.Create(chatClient);
AIAgent hotelAgent = HotelBookingAgent.Create(chatClient);
AIAgent rideAgent = RideBookingAgent.Create(chatClient);
AIAgent itineraryAgent = ItineraryDocumentAgent.Create(chatClient);
AIAgent concierge = TravelConciergeAgent.Create(
        chatClient,
        [ticketAgent.AsAIFunction(), hotelAgent.AsAIFunction(), rideAgent.AsAIFunction(), itineraryAgent.AsAIFunction()]);

string pattern = args.FirstOrDefault()?.ToLowerInvariant() ?? "tools";
string question = """
        I have tickets to Martin Garrix. The ticket PDF is here:
        https://github.com/vriesmarcel/vslive-2026-vegas-sk/blob/main/CT4EB6AF_mobile_267842.pdf
        Read the ticket, find a hotel and ride, and ask me before booking.
        """;

Console.WriteLine($"Orchestration pattern: {pattern}");
switch (pattern)
{
        case "tools":
                await AgentsAsToolsOrchestration.RunAsync(concierge, question);
                break;
        case "sequential":
                await SequentialOrchestration.RunAsync(question, ticketAgent, hotelAgent, rideAgent, itineraryAgent);
                break;
        case "concurrent":
                await ConcurrentOrchestration.RunAsync(question, ticketAgent, hotelAgent, rideAgent, itineraryAgent);
                break;
        case "handoff":
                await HandoffOrchestration.RunAsync(question, concierge, ticketAgent, hotelAgent, rideAgent, itineraryAgent);
                break;
        default:
                throw new ArgumentException("Choose one of: tools, sequential, concurrent, handoff.", nameof(args));
}
