using System.Diagnostics;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using modulerag;
using OpenTelemetry;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

internal class ChatWithAgent(IChatClient chatClient)
{
    private const string SourceName = "demo04-chatClient";
    private static readonly ActivitySource s_activitySource = new(SourceName);

    public async Task LetAgentFindRideAndHotel()
    {

        // Configure OpenTelemetry for Aspire dashboard
        var otlpEndpoint = Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT") ?? "http://localhost:4317";

        var resourceBuilder = ResourceBuilder
                    .CreateDefault()
                    .AddService("demo-04");

        var traceProvider = Sdk.CreateTracerProviderBuilder()
            .SetResourceBuilder(resourceBuilder)
            .AddSource(SourceName)
            // The following source is only required if not specifying
            // the `activitySource` in the WithOpenTelemetry call below
            .AddSource("Microsoft.Agents.AI.Workflows*")
            .AddOtlpExporter(options => options.Endpoint = new Uri(otlpEndpoint))
            .Build();

        // Start a root activity for the application
        var activity = s_activitySource.StartActivity("LetAgentFindRideAndHotel");
        Console.WriteLine($"Operation/Trace ID: {Activity.Current?.TraceId}");


        var question =
        """
        I am going to a concert that is held at the Seattle Kraken Stadium. The Concert starts at 7:30 pm and is November 20th this year. 
        """;

        Console.WriteLine("******** Create the ride agent ***********");
        var rideAgent = CreateTransportationAgent()
            .AsBuilder()
            .UseOpenTelemetry(sourceName: SourceName, configure: (cfg) => cfg.EnableSensitiveData = true) // enable telemetry at the agent level
            .Build();

        Console.WriteLine("******** Create the hotel agent ***********");
        var hotelAgent = HotelBookingAgent.CreateChatCompletionAgent(chatClient)
            .AsBuilder()
            .UseOpenTelemetry(sourceName: SourceName, configure: (cfg) => cfg.EnableSensitiveData = true) // enable telemetry at the agent level
            .Build();

        var travelAgent = chatClient.AsAIAgent(
            instructions: """
                You are a helpful and efficient travel concierge agent. Your main goal is to assist users in planning their trips by coordinating with specialized agents for different tasks, such as booking hotels and arranging transportation. 
                You will work with the HotelReservationAgent to find and book a hotel room close to the concert location, and with the TransportationAgent to find and book transportation options from the hotel to the concert venue. 
                You will coordinate the flow of information between the user and the specialized agents, ensuring that all necessary details are collected and that the user's preferences are taken into account. 
                You will also handle any follow-up questions or adjustments needed based on the user's feedback or changes in plans.
                Your ultimate goal is to ensure that the user has a seamless and enjoyable experience planning their trip to the concert.
            """)
            .AsBuilder()
            .UseOpenTelemetry(sourceName: SourceName, configure: (cfg) => cfg.EnableSensitiveData = true) // enable telemetry at the agent level
            .Build();

#pragma warning disable MAAIW001 // This demo is specifically to show handoffs
        var workflow = AgentWorkflowBuilder
            .CreateHandoffBuilderWith(travelAgent)
            .WithHandoff(travelAgent, rideAgent, "Book a ride to the concert")
            .WithHandoff(travelAgent, hotelAgent, "Book a hotel room")
            .WithHandoffs(rideAgent, [travelAgent, hotelAgent])
            .WithHandoffs(hotelAgent, [travelAgent, rideAgent])
            .EnableReturnToPrevious()
            .Build();
#pragma warning restore MAAIW001

        await RunWorkflowAsync(workflow, startMessage: question);
    }

    private AIAgent CreateTransportationAgent()
    {
        var instructions = """
            You are an expert in finding transportation options from a given hotel location to the concert location.
            You will try to get the best options available for an affordable price. Make sure the customer will be there at least 30 minutes before the concert starts at the venue.
            You always suggest 3 options with different price ranges.
            You will ask for approval before you make the booking. 
            You are not allowed to make a booking without user confirmation!

            After you successfully booked the ride you will respond with [** GOAL REACHED **] in your message.            
            """;

        return chatClient.AsAIAgent(
                name: "TransportationAgent",
                description: "An agent that finds transportation options for the user from their hotel to the concert venue.",
                instructions: instructions,
                tools: [AIFunctionFactory.Create(RideInformationSystemService.GetAvailableRides),
                        AIFunctionFactory.Create(RideInformationSystemService.BookARide)]
            );
    }

    static async Task RunWorkflowAsync(Workflow workflow, string? startMessage = null)
    {
        using CancellationTokenSource cts = CreateConsoleCancelKeySource();
        await using StreamingRun run = await InProcessExecution.OpenStreamingAsync(workflow, cancellationToken: cts.Token)
                                                               .ConfigureAwait(false);

        bool hadError = false;

        bool sendingStartMessage = false;

        if (startMessage != null)
        {
            await run.TrySendMessageAsync(startMessage);
            sendingStartMessage = true;
        }

        do
        {
            if (!sendingStartMessage)
            {
                Console.Write("> ");
                string userInput = Console.ReadLine() ?? string.Empty;

                if (userInput.Equals("exit", StringComparison.OrdinalIgnoreCase))
                {
                    break;
                }

                await run.TrySendMessageAsync(userInput);
            }
            else
            {
                sendingStartMessage = false;
            }

            string? speakingAgent = null;
            await foreach (WorkflowEvent evt in run.WatchStreamAsync(cts.Token))
            {
                switch (evt)
                {
                    case AgentResponseUpdateEvent update:
                        {
                            if (speakingAgent == null || speakingAgent != update.Update.AuthorName)
                            {
                                speakingAgent = update.Update.AuthorName;
                                Console.Write($"\n{speakingAgent}: ");
                            }

                            Console.Write(update.Update.Text);
                            break;
                        }

                    case WorkflowErrorEvent workflowError:
                        {
                            Console.ForegroundColor = ConsoleColor.Red;

                            if (workflowError.Exception != null)
                            {
                                Console.WriteLine($"\nWorkflow error: {workflowError.Exception}");
                            }
                            else
                            {
                                Console.WriteLine("\nUnknown workflow error occurred.");
                            }

                            Console.ResetColor();

                            hadError = true;
                            break;
                        }

                    case WorkflowWarningEvent workflowWarning when workflowWarning.Data is string message:
                        {
                            Console.ForegroundColor = ConsoleColor.Yellow;
                            Console.WriteLine(message);
                            Console.ResetColor();
                            break;
                        }
                }
            }
        } while (!hadError);
    }

    static CancellationTokenSource CreateConsoleCancelKeySource()
    {
        CancellationTokenSource cts = new();

        // Normally, support a way to detach events, but in this case this is a termination signal, so cleanup will happen
        // as part of application shutdown.
        Console.CancelKeyPress += (s, args) =>
        {
            cts.Cancel();

            // We handle cleanup + termination ourselves
            args.Cancel = true;
        };

        return cts;
    }
}