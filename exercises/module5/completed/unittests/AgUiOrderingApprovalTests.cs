using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Diagnostics;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Hosting.AGUI.AspNetCore;
using Microsoft.Agents.AI.Hosting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using GloboTicket.Frontend.Services.AI;

namespace GloboTicket.UnitTests;

[TestClass]
public sealed class AgUiOrderingApprovalTests
{
    [TestMethod]
    [DataRow(true, 1)]
    [DataRow(false, 0)]
    public async Task Devui_responses_endpoint_supports_native_tool_approval(bool approved, int expectedSubmissions)
    {
        int submissions = 0;
        using var model = new BookingClient(Guid.NewGuid());
        AIAgent agent = model.AsAIAgent(new ChatClientAgentOptions
        {
            Name = "BookingAgent",
            ChatOptions = new()
            {
                Tools = [new ApprovalRequiredAIFunction(AIFunctionFactory.Create(
                    (Guid quoteId, string name, string email) => { submissions++; return "Simulated order recorded."; },
                    "submit_order"))]
            }
        });
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Logging.ClearProviders();
        builder.Services.AddSingleton(agent);
        builder.AddAIAgent("BookingAgent", (services, _) => services.GetRequiredService<AIAgent>())
            .WithInMemorySessionStore(withIsolation: false);
#pragma warning disable MAAI001 // The production host also persists pending approvals.
        builder.Services.AddSingleton<AgentSessionStore, InMemoryAgentSessionStore>();
#pragma warning restore MAAI001
        builder.Services.AddOpenAIResponses();
        builder.Services.AddOpenAIConversations();
        await using var app = builder.Build();
        app.MapOpenAIResponses();
        app.MapOpenAIConversations();
        await app.StartAsync();
        try
        {
            string address = app.Services.GetRequiredService<IServer>().Features
                .Get<IServerAddressesFeature>()!.Addresses.Single();
            using var http = new HttpClient { BaseAddress = new Uri(address), Timeout = TimeSpan.FromSeconds(15) };
            using var conversationResponse = await http.PostAsJsonAsync("/v1/conversations", new
                { metadata = new { agent_id = "BookingAgent" } });
            conversationResponse.EnsureSuccessStatusCode();
            var conversation = await conversationResponse.Content.ReadFromJsonAsync<JsonElement>();
            string? conversationId = conversation.GetProperty("id").GetString();
            using var first = await http.PostAsJsonAsync("/v1/responses", new
            {
                model = "BookingAgent", metadata = new { entity_id = "BookingAgent" },
                conversation = conversationId, stream = true,
                input = "Book the simulated order."
            });
            string stream = await first.Content.ReadAsStringAsync();
            Assert.IsTrue(first.IsSuccessStatusCode, stream);
            Assert.AreEqual(0, submissions);
            var approvals = Events(stream).Where(e => e.GetProperty("type").GetString() == "response.function_approval.requested").ToArray();
            Assert.HasCount(1, approvals, stream);
            var approval = approvals[0];
            using var resumed = await http.PostAsJsonAsync("/v1/responses", new
            {
                model = "BookingAgent", metadata = new { entity_id = "BookingAgent" },
                conversation = conversationId, stream = true,
                input = new[]
                {
                    new
                    {
                        type = "message", role = "user", content = new[]
                        {
                            new
                            {
                                type = "function_approval_response", approved,
                                request_id = approval.GetProperty("request_id").GetString(),
                                function_call = approval.GetProperty("function_call")
                            }
                        }
                    }
                }
            });
            string continuation = await resumed.Content.ReadAsStringAsync();
            Assert.IsTrue(resumed.IsSuccessStatusCode, continuation);
            Assert.AreEqual(expectedSubmissions, submissions, continuation);
            Assert.IsFalse(Events(continuation).Any(e => e.GetProperty("type").GetString() is "error" or "response.failed"), continuation);
        }
        finally { await app.StopAsync(); }
    }

    [TestMethod]
    [DataRow(true, 1, "valid")]
    [DataRow(false, 0, "valid")]
    [DataRow(true, 1, "full-history")]
    [DataRow(true, 1, "browser-history")]
    [DataRow(false, 0, "browser-history")]
    [DataRow(true, 1, "changed-quote")]
    [DataRow(true, 0, "unknown-interrupt")]
    public async Task Agui_interrupt_requires_explicit_approval_before_executing_the_proposed_order(
        bool approved, int expectedSubmissions, string responseKind)
    {
        Guid quoteId = Guid.NewGuid();
        var errors = new List<string>();
        using var listener = new ActivityListener
        {
            ShouldListenTo = _ => true,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = activity =>
            {
                if (activity.Status == ActivityStatusCode.Error) errors.Add(activity.StatusDescription ?? "Unknown error");
            }
        };
        ActivitySource.AddActivityListener(listener);
        int submissions = 0;
        Guid? submittedQuoteId = null;
        using var model = new BookingClient(quoteId);
        var tool = AIFunctionFactory.Create((Guid quoteId, string name, string email) =>
        {
            submissions++;
            submittedQuoteId = quoteId;
            return $"Simulated receipt for {quoteId}";
        }, "submit_order");
        AIAgent agent = model.AsAIAgent(new ChatClientAgentOptions
        {
            ChatOptions = new() { Tools = [new ApprovalRequiredAIFunction(tool)] }
        });
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Logging.ClearProviders();
        builder.Services.AddAGUIServer();
#pragma warning disable MAAI001 // Hosted session stores are experimental in the pinned hosting integration.
        builder.Services.AddSingleton<AgentSessionStore, InMemoryAgentSessionStore>();
#pragma warning restore MAAI001
        await using var app = builder.Build();
        app.MapAGUIServer("/ag-ui", AgUiAgent.WithSessionBoundApprovals(agent));
        await app.StartAsync();
        try
        {
            string address = app.Services.GetRequiredService<IServer>().Features
                .Get<IServerAddressesFeature>()!.Addresses.Single();
            using var http = new HttpClient { BaseAddress = new Uri(address), Timeout = TimeSpan.FromSeconds(15) };
            string threadId = Guid.NewGuid().ToString();
            var messages = new[] { new { id = "u", role = "user", content = "Book the simulated order." } };
            using var first = await http.PostAsJsonAsync("/ag-ui", new
            {
                threadId, runId = Guid.NewGuid().ToString(), messages,
                tools = Array.Empty<object>(), context = Array.Empty<object>(), state = new { }, forwardedProps = new { }
            });
            first.EnsureSuccessStatusCode();
            string stream = await first.Content.ReadAsStringAsync();
            Assert.AreEqual(0, submissions);
            JsonElement finished = Events(stream).Single(e => e.GetProperty("type").GetString() == "RUN_FINISHED");
            string? interruptId = finished.GetProperty("outcome").GetProperty("interrupts")[0].GetProperty("id").GetString();
            Assert.IsFalse(string.IsNullOrEmpty(interruptId), stream);
            object[] history = responseKind is "full-history" or "browser-history"
                ? [messages[0], new
                {
                    id = "proposal", role = "assistant", toolCalls = new[]
                    {
                        new
                        {
                            id = "order-call", type = "function", function = new
                            {
                                name = "submit_order",
                                arguments = JsonSerializer.Serialize(new { quoteId, name = "Demo", email = "demo@example.test" })
                            }
                        }
                    }
                }]
                : [messages[0]];
            if (responseKind == "browser-history")
                history = [.. history, new
                {
                    id = "approval-result", role = "tool", toolCallId = "order-call",
                    content = JsonSerializer.Serialize(new
                    {
                        approved, toolCall = new
                        {
                            callId = "order-call", name = "submit_order",
                            arguments = new { quoteId, name = "Demo", email = "demo@example.test" }
                        }
                    })
                }];

            using var resumed = await http.PostAsJsonAsync("/ag-ui", new
            {
                threadId, runId = Guid.NewGuid().ToString(), messages = history,
                tools = Array.Empty<object>(), context = Array.Empty<object>(), state = new { }, forwardedProps = new { },
                resume = new[]
                {
                    new
                    {
                        interruptId = responseKind == "unknown-interrupt" ? "unknown" : interruptId,
                        status = "resolved", payload = new
                        {
                            approved,
                            toolCall = new { callId = "order-call", name = "submit_order",
                                arguments = new
                                {
                                    quoteId = responseKind == "changed-quote" ? Guid.NewGuid() : quoteId,
                                    name = "Demo", email = "demo@example.test"
                                } }
                        }
                    }
                }
            });
            resumed.EnsureSuccessStatusCode();
            string continuation = await resumed.Content.ReadAsStringAsync();
            Assert.AreEqual(expectedSubmissions, submissions, continuation + string.Join("\n", errors));
            if (approved && expectedSubmissions > 0) StringAssert.Contains(continuation, "Simulated receipt");
            if (expectedSubmissions > 0) Assert.AreEqual(quoteId, submittedQuoteId);
            if (responseKind == "unknown-interrupt")
                StringAssert.Contains(continuation, "No approval response was provided");
            Assert.IsFalse(Events(continuation).Any(e => e.GetProperty("type").GetString() == "RUN_ERROR"),
                continuation + string.Join("\n", errors));
        }
        finally { await app.StopAsync(); }
    }

    private static IEnumerable<JsonElement> Events(string stream) =>
        stream.Split('\n').Where(l => l.StartsWith("data:") && l["data:".Length..].Trim() != "[DONE]")
            .Select(l => JsonSerializer.Deserialize<JsonElement>(l["data:".Length..]));

    private sealed class BookingClient(Guid quoteId) : IChatClient
    {
        public Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> messages,
            ChatOptions? options = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await Task.Yield();
            if (messages.SelectMany(m => m.Contents).Any(c => c is FunctionResultContent))
            {
                yield return new(ChatRole.Assistant, "The proposed order was handled.");
                yield break;
            }
            yield return new ChatResponseUpdate
            {
                Role = ChatRole.Assistant, MessageId = "proposal",
                Contents = [new FunctionCallContent("order-call", "submit_order", new Dictionary<string, object?>
                {
                    ["quoteId"] = quoteId, ["name"] = "Demo", ["email"] = "demo@example.test"
                })]
            };
        }

        public object? GetService(Type serviceType, object? serviceKey = null) =>
            serviceKey is null && serviceType.IsInstanceOfType(this) ? this : null;
        public void Dispose() { }
    }
}
