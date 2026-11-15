using System.Runtime.CompilerServices;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Module04.Demo05.Orchestration;
using Module04.Demo05.Tools;

namespace Module04.Demo05.Tests;

[TestClass]
[DoNotParallelize]
public sealed class ApprovalOrchestrationTests
{
    [TestMethod]
    [DataRow("groupchat", "y", 1, false)]
    [DataRow("groupchat", "n", 0, false)]
    [DataRow("groupchat", "", 0, false)]
    [DataRow("groupchat", null, 0, false)]
    [DataRow("groupchat", " YES ", 1, false)]
    [DataRow("groupchat", "y", 1, true)]
    [DataRow("tools", "y", 1, false)]
    [DataRow("tools", "n", 0, false)]
    [DataRow("tools", "y", 1, true)]
    public async Task Approval_is_prompted_and_returned_to_the_originating_agent(
        string pattern, string? answer, int expectedBookings, bool prematureCompletionMarker)
    {
        int bookings = 0;
        var client = new BookingClient(prematureCompletionMarker);
        AIAgent bookingAgent = new ChatClientAgent(client, name: "BookingAgent", tools:
        [
            new ApprovalRequiredAIFunction(AIFunctionFactory.Create(
                (int roomId, int rideId, string city) =>
                {
                    bookings++;
                    return $"Booked room {roomId} and ride {rideId} in {city}.";
                }, "book_itinerary"))
        ]);
        AIAgent Specialist(string name) => new ChatClientAgent(new SpecialistClient(), name: name);

        TextReader originalIn = Console.In;
        TextWriter originalOut = Console.Out;
        using var output = new StringWriter();
        using var input = new ApprovalInput(answer, () => bookings);
        Console.SetIn(input);
        Console.SetOut(output);
        try
        {
            Task run = pattern switch
            {
                "groupchat" => GroupChatOrchestration.RunAsync("Book the trip.",
                    Specialist("Ticket"), Specialist("Hotel"), Specialist("Ride"), bookingAgent),
                "tools" => AgentsAsToolsOrchestration.RunAsync(bookingAgent, "Book the trip."),
                _ => throw new ArgumentOutOfRangeException(nameof(pattern))
            };

            try
            {
                await run.WaitAsync(TimeSpan.FromSeconds(5));
            }
            catch (TimeoutException)
            {
                Assert.Fail($"The approval was not handled. Output:\n{output}");
            }
        }
        finally
        {
            Console.SetIn(originalIn);
            Console.SetOut(originalOut);
        }

        string text = output.ToString();
        StringAssert.Contains(text, "[approval requested] book_itinerary");
        StringAssert.Contains(text, "Approve book_itinerary(");
        StringAssert.Contains(text, "\"roomId\":310");
        StringAssert.Contains(text, "[reasoning] Checking the itinerary.");
        StringAssert.Contains(text, "[usage] in=10 out=5 total=15");
        StringAssert.Contains(text, "[call] book_itinerary(");
        StringAssert.Contains(text, "[result] booking-call");
        StringAssert.Contains(text, "TRAVEL_ITINERARY_COMPLETE");
        Assert.AreEqual(1, input.ReadCount, "Each pending approval should be prompted exactly once.");
        Assert.AreEqual(expectedBookings, bookings);
        Assert.IsTrue(client.SawFunctionResult, "The agent did not resume with the tool result.");
    }

    [TestMethod]
    [DataRow("y", 1)]
    [DataRow("n", 0)]
    public async Task Group_chat_asks_for_approval_after_the_agent_asks_its_own_question(
        string approvalAnswer, int expectedBookings)
    {
        int bookings = 0;
        var client = new QuestionThenBookingClient();
        AIAgent concierge = new ChatClientAgent(client, name: "Concierge", tools:
        [
            AIFunctionFactory.Create(ConversationFunctions.AskUser, "ask_user"),
            new ApprovalRequiredAIFunction(AIFunctionFactory.Create(() =>
            {
                bookings++;
                return "Booked.";
            }, "book_itinerary"))
        ]);
        AIAgent Specialist(string name) => new ChatClientAgent(new SpecialistClient(), name: name);

        TextReader originalIn = Console.In;
        TextWriter originalOut = Console.Out;
        using var output = new StringWriter();
        using var input = new StringReader($"yes\n{approvalAnswer}\n");
        Console.SetIn(input);
        Console.SetOut(output);
        try
        {
            await GroupChatOrchestration.RunAsync("Book the trip.", Specialist("Ticket"),
                Specialist("Hotel"), Specialist("Ride"), concierge)
                .WaitAsync(TimeSpan.FromSeconds(5));
        }
        catch (InvalidOperationException exception)
        {
            Assert.Fail($"{exception.Message}\n{output}");
        }
        finally
        {
            Console.SetIn(originalIn);
            Console.SetOut(originalOut);
        }

        string text = output.ToString();
        int question = text.IndexOf("Shall I book the trip?", StringComparison.Ordinal);
        int approval = text.IndexOf("Approve book_itinerary(", StringComparison.Ordinal);
        Assert.IsTrue(question >= 0 && approval > question, text);
        Assert.AreEqual(expectedBookings, bookings);
        Assert.IsTrue(client.SawQuestionAnswer, "The agent did not receive the user's first answer.");
        Assert.IsTrue(client.SawBookingResult, "The agent did not resume after the separate approval.");
    }

    [TestMethod]
    public async Task Multiple_approval_requests_keep_each_decision_with_its_tool()
    {
        int hotelsBooked = 0;
        int ridesBooked = 0;
        var client = new MultipleBookingClient();
        AIAgent bookingAgent = new ChatClientAgent(client, name: "BookingAgent", tools:
        [
            new ApprovalRequiredAIFunction(AIFunctionFactory.Create(() => { hotelsBooked++; return "Hotel booked."; }, "book_hotel")),
            new ApprovalRequiredAIFunction(AIFunctionFactory.Create(() => { ridesBooked++; return "Ride booked."; }, "book_ride"))
        ]);
        TextReader originalIn = Console.In;
        TextWriter originalOut = Console.Out;
        using var output = new StringWriter();
        using var input = new StringReader("y\nn\n");
        Console.SetIn(input);
        Console.SetOut(output);
        try
        {
            Task run = AgentsAsToolsOrchestration.RunAsync(bookingAgent, "Book the trip.");
            await run.WaitAsync(TimeSpan.FromSeconds(5));
        }
        catch (InvalidOperationException exception)
        {
            Assert.Fail($"{exception}\n{output}");
        }
        finally
        {
            Console.SetIn(originalIn);
            Console.SetOut(originalOut);
        }

        string text = output.ToString();
        int hotelPrompt = text.IndexOf("Approve book_hotel(", StringComparison.Ordinal);
        int ridePrompt = text.IndexOf("Approve book_ride(", StringComparison.Ordinal);
        Assert.IsTrue(hotelPrompt >= 0 && ridePrompt >= 0, text);
        Assert.AreEqual(2, text.Split("Approve book_", StringSplitOptions.None).Length - 1, text);
        Assert.AreEqual(hotelPrompt < ridePrompt ? 1 : 0, hotelsBooked, text);
        Assert.AreEqual(ridePrompt < hotelPrompt ? 1 : 0, ridesBooked, text);
        Assert.IsTrue(client.SawBothResults, text);
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task Group_chat_preserves_incomplete_and_failed_workflow_errors(bool fail)
    {
        AIAgent Specialist(string name, bool failOnRun = false) =>
            new ChatClientAgent(new SpecialistClient(failOnRun), name: name);
        TextWriter originalOut = Console.Out;
        using var output = new StringWriter();
        Console.SetOut(output);
        try
        {
            var exception = await Assert.ThrowsExactlyAsync<InvalidOperationException>(() =>
                GroupChatOrchestration.RunAsync("Plan the trip.", Specialist("Ticket", fail),
                    Specialist("Hotel"), Specialist("Ride"), Specialist("Concierge"))
                    .WaitAsync(TimeSpan.FromSeconds(5)));

            Assert.AreEqual(fail
                ? "The group chat workflow failed."
                : "The group chat ended without completing or cancelling the booking.", exception.Message);
            if (fail)
            {
                Assert.IsNotNull(exception.InnerException);
                StringAssert.Contains(output.ToString(), "[workflow error]");
            }
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    private sealed class ApprovalInput(string? answer, Func<int> bookings)
        : StringReader(answer is null ? string.Empty : answer + "\n")
    {
        public int ReadCount { get; private set; }

        public override string? ReadLine()
        {
            Assert.AreEqual(0, bookings(), "The booking ran before the approval prompt.");
            ReadCount++;
            return base.ReadLine();
        }
    }

    private sealed class BookingClient(bool prematureCompletionMarker) : IChatClient
    {
        public bool SawFunctionResult { get; private set; }

        public Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages,
            ChatOptions? options = null, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
            IEnumerable<ChatMessage> messages, ChatOptions? options = null,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await Task.Yield();

            if (messages.SelectMany(message => message.Contents).OfType<FunctionResultContent>()
                .Any(result => result.CallId == "booking-call"))
            {
                SawFunctionResult = true;
                yield return new ChatResponseUpdate
                {
                    Role = ChatRole.Assistant,
                    Contents =
                    [
                        new TextContent("TRAVEL_ITINERARY_COMPLETE"),
                        new UsageContent(new UsageDetails
                        {
                            InputTokenCount = 10, OutputTokenCount = 5, TotalTokenCount = 15
                        })
                    ]
                };
                yield break;
            }

            if (prematureCompletionMarker)
            {
                yield return new(ChatRole.Assistant, "TRAVEL_ITINERARY_COMPLETE");
            }

            yield return new ChatResponseUpdate
            {
                Role = ChatRole.Assistant,
                Contents =
                [
                    new TextReasoningContent("Checking the itinerary."),
                    new FunctionCallContent("booking-call", "book_itinerary", new Dictionary<string, object?>
                    {
                        ["roomId"] = 310,
                        ["rideId"] = 6,
                        ["city"] = "Ibiza"
                    })
                ]
            };
        }

        public object? GetService(Type serviceType, object? serviceKey = null) =>
            serviceKey is null && serviceType.IsInstanceOfType(this) ? this : null;

        public void Dispose() { }
    }

    private sealed class QuestionThenBookingClient : IChatClient
    {
        public bool SawQuestionAnswer { get; private set; }
        public bool SawBookingResult { get; private set; }

        public Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages,
            ChatOptions? options = null, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
            IEnumerable<ChatMessage> messages, ChatOptions? options = null,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await Task.Yield();
            var results = messages.SelectMany(message => message.Contents).OfType<FunctionResultContent>().ToList();
            if (results.Any(result => result.CallId == "booking-call"))
            {
                SawBookingResult = true;
                yield return new(ChatRole.Assistant, "TRAVEL_ITINERARY_COMPLETE");
                yield break;
            }

            if (results.Any(result => result.CallId == "question-call" && result.Result?.ToString() == "yes"))
            {
                SawQuestionAnswer = true;
                yield return new ChatResponseUpdate
                {
                    Role = ChatRole.Assistant,
                    Contents = [new FunctionCallContent("booking-call", "book_itinerary", new Dictionary<string, object?>())]
                };
                yield break;
            }

            yield return new ChatResponseUpdate
            {
                Role = ChatRole.Assistant,
                Contents =
                [
                    new FunctionCallContent("question-call", "ask_user", new Dictionary<string, object?>
                    {
                        ["question"] = "Shall I book the trip?"
                    })
                ]
            };
        }

        public object? GetService(Type serviceType, object? serviceKey = null) =>
            serviceKey is null && serviceType.IsInstanceOfType(this) ? this : null;

        public void Dispose() { }
    }

    private sealed class MultipleBookingClient : IChatClient
    {
        public bool SawBothResults { get; private set; }

        public Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages,
            ChatOptions? options = null, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
            IEnumerable<ChatMessage> messages, ChatOptions? options = null,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await Task.Yield();
            var results = messages.SelectMany(message => message.Contents).OfType<FunctionResultContent>().ToList();
            if (results.Count > 0)
            {
                SawBothResults = results.Any(result => result.CallId == "hotel-call") &&
                    results.Any(result => result.CallId == "ride-call");
                yield return new(ChatRole.Assistant, SawBothResults ? "TRAVEL_ITINERARY_COMPLETE" : "Waiting.");
                yield break;
            }

            yield return new ChatResponseUpdate
            {
                Role = ChatRole.Assistant,
                Contents =
                [
                    new FunctionCallContent("hotel-call", "book_hotel", new Dictionary<string, object?>()),
                    new FunctionCallContent("ride-call", "book_ride", new Dictionary<string, object?>())
                ]
            };
        }

        public object? GetService(Type serviceType, object? serviceKey = null) =>
            serviceKey is null && serviceType.IsInstanceOfType(this) ? this : null;

        public void Dispose() { }
    }

    private sealed class SpecialistClient(bool fail = false) : IChatClient
    {
        public Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages,
            ChatOptions? options = null, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
            IEnumerable<ChatMessage> messages, ChatOptions? options = null,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await Task.Yield();
            if (fail)
            {
                throw new InvalidOperationException("Simulated specialist failure.");
            }
            yield return new(ChatRole.Assistant, "Specialist completed.");
        }

        public object? GetService(Type serviceType, object? serviceKey = null) =>
            serviceKey is null && serviceType.IsInstanceOfType(this) ? this : null;

        public void Dispose() { }
    }
}
