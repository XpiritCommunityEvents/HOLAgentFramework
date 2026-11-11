using System.ClientModel;
using System.ClientModel.Primitives;
using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OpenAI;
using OpenAI.Responses;

#pragma warning disable OPENAI001 // The pinned SDK marks the Responses API as experimental.

namespace GloboTicket.UnitTests;

[TestClass]
public sealed class ResponsesClientContractTests
{
    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task Reasoning_tool_calls_preserve_approval_and_session_history(bool approve)
    {
        using var handler = new ResponsesHandler(
            """
            [
              {"type":"reasoning","id":"rs_1","summary":[],"encrypted_content":"opaque-reasoning"},
              {"type":"function_call","id":"fc_1","call_id":"call_1","name":"book_ride","arguments":"{}","status":"completed"}
            ]
            """,
            MessageOutput("Booking processed."),
            MessageOutput("Previous turn remembered."));
        using var http = new HttpClient(handler);
        ResponsesClient client = CreateClient(http);
        int bookings = 0;
        var tool = new ApprovalRequiredAIFunction(AIFunctionFactory.Create(() =>
        {
            bookings++;
            return "BOOKING-901";
        }, "book_ride"));
        AIAgent agent = client.AsAIAgent(
            model: "reasoning-model",
            instructions: "A synthetic booking assistant.",
            tools: [tool]);
        AgentSession session = await agent.CreateSessionAsync();
        var options = new ChatClientAgentRunOptions(new ChatOptions
        {
            Reasoning = new() { Effort = ReasoningEffort.Medium }
        });

        AgentResponse pending = await agent.RunAsync("Book a ride.", session, options);
        ToolApprovalRequestContent approval = pending.Messages.SelectMany(message => message.Contents)
            .OfType<ToolApprovalRequestContent>().Single();
        Assert.AreEqual(0, bookings);
        Assert.HasCount(1, handler.Requests);

        await agent.RunAsync(
            new ChatMessage(ChatRole.User, [approval.CreateResponse(approve)]), session, options);
        Assert.AreEqual(approve ? 1 : 0, bookings);
        Assert.HasCount(2, handler.Requests);

        JsonElement continuation = handler.Requests[1];
        Assert.AreEqual("medium", continuation.GetProperty("reasoning").GetProperty("effort").GetString());
        JsonElement[] input = continuation.GetProperty("input").EnumerateArray().ToArray();
        JsonElement toolResult = input.Single(item => item.GetProperty("type").GetString() == "function_call_output");
        Assert.AreEqual("call_1", toolResult.GetProperty("call_id").GetString());
        Assert.AreEqual(approve, toolResult.GetProperty("output").ToString().Contains("BOOKING-901"));
        Assert.IsTrue(input.Any(item =>
            item.GetProperty("type").GetString() == "reasoning" &&
            item.GetProperty("encrypted_content").GetString() == "opaque-reasoning"));

        AgentResponse followUp = await agent.RunAsync("What happened earlier?", session, options);
        Assert.AreEqual("Previous turn remembered.", followUp.Text);
        Assert.HasCount(3, handler.Requests);
        string history = handler.Requests[2].GetProperty("input").GetRawText();
        StringAssert.Contains(history, "Book a ride.");
        StringAssert.Contains(history, "Booking processed.");
        Assert.IsTrue(handler.Paths.All(path => path == "/openai/v1/responses"));
        Assert.IsTrue(handler.Requests.All(request =>
            request.GetProperty("model").GetString() == "reasoning-model"));
    }

    [TestMethod]
    public async Task Streaming_response_preserves_text_and_session_history()
    {
        using var handler = new ResponsesHandler(
            MessageOutput("Streaming response."),
            MessageOutput("Follow-up response."));
        using var http = new HttpClient(handler);
        AIAgent agent = CreateClient(http).AsAIAgent(model: "reasoning-model");
        AgentSession session = await agent.CreateSessionAsync();
        var text = new StringBuilder();

        await foreach (AgentResponseUpdate update in agent.RunStreamingAsync("Say hello.", session))
        {
            text.Append(update.Text);
        }

        Assert.AreEqual("Streaming response.", text.ToString());
        Assert.IsTrue(handler.Requests[0].GetProperty("stream").GetBoolean());
        Assert.AreEqual("Follow-up response.", (await agent.RunAsync("Continue.", session)).Text);
        string history = handler.Requests[1].GetProperty("input").GetRawText();
        StringAssert.Contains(history, "Say hello.");
        StringAssert.Contains(history, "Streaming response.");
    }

    [TestMethod]
    public async Task Structured_evaluator_verdict_uses_Responses_json_schema()
    {
        using var handler = new ResponsesHandler(
            MessageOutput("""{"answered":true,"gapAnalysis":""}"""));
        using var http = new HttpClient(handler);
        ResponsesClient client = CreateClient(http);
        AIAgent agent = client.AsAIAgent(
            model: "reasoning-model",
            instructions: "Evaluate whether the booking is complete.");
        AgentSession session = await agent.CreateSessionAsync();

        AgentResponse<BookingVerdict> response = await agent.RunAsync<BookingVerdict>(
            "The booking is complete.", session);

        Assert.IsTrue(response.Result.Answered);
        Assert.AreEqual("", response.Result.GapAnalysis);
        Assert.AreEqual("/openai/v1/responses", handler.Paths.Single());
        JsonElement format = handler.Requests.Single().GetProperty("text").GetProperty("format");
        Assert.AreEqual("json_schema", format.GetProperty("type").GetString());
        Assert.AreEqual("object", format.GetProperty("schema").GetProperty("type").GetString());
    }

    [TestMethod]
    public async Task Unstructured_evaluator_response_keeps_its_text_for_fallback()
    {
        using var handler = new ResponsesHandler(MessageOutput("VERDICT: DONE"));
        using var http = new HttpClient(handler);
        ResponsesClient client = CreateClient(http);
        AIAgent agent = client.AsAIAgent(
            model: "reasoning-model",
            instructions: "Evaluate whether the booking is complete.");
        AgentSession session = await agent.CreateSessionAsync();

        AgentResponse<BookingVerdict> response = await agent.RunAsync<BookingVerdict>(
            "The booking is complete.", session);

        bool invalidStructuredResult = false;
        try
        {
            _ = response.Result;
        }
        catch (JsonException)
        {
            invalidStructuredResult = true;
        }

        Assert.IsTrue(invalidStructuredResult);
        Assert.AreEqual("VERDICT: DONE", response.Text);
    }

    private static ResponsesClient CreateClient(HttpClient http) =>
        new OpenAIClient(new ApiKeyCredential("test-key"), new OpenAIClientOptions
        {
            Endpoint = new Uri("https://example.test/openai/v1/"),
            Transport = new HttpClientPipelineTransport(http)
        }).GetResponsesClient();

    private static string MessageOutput(string text) => JsonSerializer.Serialize(new[]
    {
        new
        {
            type = "message",
            id = "msg_1",
            role = "assistant",
            status = "completed",
            content = new[] { new { type = "output_text", text, annotations = Array.Empty<object>() } }
        }
    });

    public sealed class BookingVerdict
    {
        public bool Answered { get; set; }
        public string? GapAnalysis { get; set; }
    }

    private sealed class ResponsesHandler(params string[] outputs) : HttpMessageHandler
    {
        internal List<JsonElement> Requests { get; } = [];
        internal List<string> Paths { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Paths.Add(request.RequestUri!.AbsolutePath);
            using JsonDocument body = JsonDocument.Parse(
                await request.Content!.ReadAsStringAsync(cancellationToken));
            Requests.Add(body.RootElement.Clone());
            Assert.IsTrue(Requests.Count <= outputs.Length, "Unexpected extra model request.");
            string response = $$"""
                {
                  "id":"resp_{{Requests.Count}}",
                  "object":"response",
                  "created_at":1700000000,
                  "model":"reasoning-model",
                  "status":"completed",
                  "store":false,
                  "output":{{outputs[Requests.Count - 1]}}
                }
                """;
            bool streaming = body.RootElement.TryGetProperty("stream", out JsonElement stream) &&
                stream.GetBoolean();
            if (streaming)
            {
                using JsonDocument output = JsonDocument.Parse(outputs[Requests.Count - 1]);
                JsonElement item = output.RootElement[0];
                string text = item.GetProperty("content")[0].GetProperty("text").GetString()!;
                response =
                    Event("response.created", new
                    {
                        response = new
                        {
                            id = $"resp_{Requests.Count}", @object = "response",
                            created_at = 1700000000, model = "reasoning-model",
                            status = "in_progress", store = false, output = Array.Empty<object>()
                        }
                    }) +
                    Event("response.output_item.added", new
                    {
                        output_index = 0,
                        item = new
                        {
                            type = "message", id = "msg_1", role = "assistant",
                            status = "in_progress", content = Array.Empty<object>()
                        }
                    }) +
                    Event("response.output_text.delta", new
                    {
                        item_id = "msg_1", output_index = 0, content_index = 0, delta = text
                    }) +
                    Event("response.output_item.done", new { output_index = 0, item }) +
                    Event("response.completed", new
                    {
                        response = JsonSerializer.Deserialize<JsonElement>(response)
                    });
            }

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    response, Encoding.UTF8, streaming ? "text/event-stream" : "application/json")
            };
        }

        private static string Event(string type, object payload)
        {
            var data = JsonSerializer.SerializeToNode(payload)!.AsObject();
            data["type"] = type;
            return $"event: {type}\ndata: {data.ToJsonString(new JsonSerializerOptions { WriteIndented = false })}\n\n";
        }
    }
}
