using System.Text.Json;
using System.Net.Http.Json;
using GloboTicket.Ordering.MCP;
using GloboTicket.Ordering.Controllers;
using GloboTicket.Ordering.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.AspNetCore;

namespace GloboTicket.UnitTests;

[TestClass]
public sealed class OrderingMcpContractTests
{
    [TestMethod]
    [DataRow("accept", false)]
    [DataRow("decline", true)]
    [DataRow("cancel", true)]
    [DataRow("invalid", true)]
    public async Task Optional_elicitation_accepts_demo_details_or_returns_an_explicit_non_submission(string action, bool isError)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Logging.ClearProviders();
        builder.Services.AddMcpServer()
            .WithHttpTransport(options => options.SessionMode = HttpServerSessionMode.Stateful)
            .WithTools<OrderingContext>();
        await using var app = builder.Build();
        app.MapMcp("/mcp");
        await app.StartAsync();
        try
        {
            string address = app.Services.GetRequiredService<IServer>().Features
                .Get<IServerAddressesFeature>()!.Addresses.Single();
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            int requests = 0;
            await using var client = await McpClient.CreateAsync(new HttpClientTransport(
                new HttpClientTransportOptions { Endpoint = new Uri(address + "/mcp") }), new McpClientOptions
            {
                Capabilities = new() { Elicitation = new() { Form = new() } },
                Handlers = new()
                {
                    ElicitationHandler = (_, token) =>
                    {
                        token.ThrowIfCancellationRequested();
                        requests++;
                        return ValueTask.FromResult(new ElicitResult
                        {
                            Action = action == "invalid" ? "accept" : action,
                            Content = action is "accept" or "invalid" ? new Dictionary<string, JsonElement>
                            {
                                ["name"] = JsonSerializer.SerializeToElement("Demo Customer"),
                                ["email"] = JsonSerializer.SerializeToElement(action == "invalid" ? "not-an-email" : "demo@example.test")
                            } : null
                        });
                    }
                }
            }, cancellationToken: timeout.Token);
            var result = await client.CallToolAsync("collect_demo_customer", cancellationToken: timeout.Token);
            Assert.AreEqual(1, requests, JsonSerializer.Serialize(result.Content));
            Assert.AreEqual(isError, result.IsError == true, JsonSerializer.Serialize(result.Content));
            if (!isError)
                Assert.AreEqual("demo@example.test", result.StructuredContent!.Value.GetProperty("email").GetString());
            else
                StringAssert.Contains(((TextContentBlock)result.Content.Single()).Text, "No order was submitted");
        }
        finally { await app.StopAsync(); }
    }

    [TestMethod]
    public async Task Protocol_quotes_submits_retries_and_reads_structured_receipts()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Logging.ClearProviders();
        builder.Services.AddDbContext<OrderingDbContext>(o => o.UseSqlite(connection));
        builder.Services.AddSingleton<IOrderingCatalog, DemoCatalog>();
        builder.Services.AddSingleton(TimeProvider.System);
        builder.Services.AddScoped<OrderingService>();
        builder.Services.AddControllers().AddApplicationPart(typeof(OrderController).Assembly);
        builder.Services.AddMcpServer().WithHttpTransport().WithTools<OrderingTools>()
            .WithTools<OrderingContext>().WithResources<OrderingContext>().WithPrompts<OrderingContext>();
        await using var app = builder.Build();
        using (var scope = app.Services.CreateScope())
            await scope.ServiceProvider.GetRequiredService<OrderingDbContext>().Database.EnsureCreatedAsync();
        app.MapMcp("/mcp");
        app.MapControllers();
        await app.StartAsync();
        try
        {
            string address = app.Services.GetRequiredService<IServer>().Features
                .Get<IServerAddressesFeature>()!.Addresses.Single();
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            await using var client = await McpClient.CreateAsync(new HttpClientTransport(
                new HttpClientTransportOptions { Endpoint = new Uri(address + "/mcp") }),
                cancellationToken: timeout.Token);
            var quote = await client.CallToolAsync("quote_tickets", new Dictionary<string, object?>
            {
                ["lines"] = new[] { new { eventId = Guid.NewGuid(), ticketCount = 2 } },
                ["promoCode"] = "SAVE10", ["maxTotal"] = 200
            }, cancellationToken: timeout.Token);
            Assert.AreNotEqual(true, quote.IsError, JsonSerializer.Serialize(quote.Content));
            Assert.IsNotNull(quote.StructuredContent);
            Guid quoteId = quote.StructuredContent.Value.GetProperty("quoteId").GetGuid();
            var submission = new Dictionary<string, object?>
            {
                ["quoteId"] = quoteId, ["name"] = "Demo", ["email"] = "demo@example.test"
            };
            var first = await client.CallToolAsync("submit_order", submission, cancellationToken: timeout.Token);
            var retry = await client.CallToolAsync("submit_order", submission, cancellationToken: timeout.Token);
            Assert.AreNotEqual(true, first.IsError, JsonSerializer.Serialize(first.Content));
            var orderId = first.StructuredContent!.Value.GetProperty("orderId").GetGuid();
            Assert.AreEqual(orderId, retry.StructuredContent!.Value.GetProperty("orderId").GetGuid());
            using var http = new HttpClient { BaseAddress = new Uri(address) };
            using var httpQuote = await http.PostAsJsonAsync("/order/quotes", new
            {
                lines = new[] { new { eventId = Guid.NewGuid(), ticketCount = 2 } }, promoCode = "SAVE10"
            }, timeout.Token);
            httpQuote.EnsureSuccessStatusCode();
            var webQuote = await httpQuote.Content.ReadFromJsonAsync<JsonElement>(timeout.Token);
            Assert.AreEqual(quote.StructuredContent.Value.GetProperty("total").GetDecimal(),
                webQuote.GetProperty("total").GetDecimal());
            using var webRetry = await http.PostAsJsonAsync("/order", new
                { quoteId, name = "Demo", email = "demo@example.test" }, timeout.Token);
            webRetry.EnsureSuccessStatusCode();
            Assert.AreEqual(orderId, (await webRetry.Content.ReadFromJsonAsync<JsonElement>(timeout.Token))
                .GetProperty("orderId").GetGuid());
            var receipt = await client.CallToolAsync("get_order", new Dictionary<string, object?>
                { ["orderId"] = orderId }, cancellationToken: timeout.Token);
            Assert.AreEqual(189m, receipt.StructuredContent!.Value
                .GetProperty("quote").GetProperty("total").GetDecimal());
            Assert.IsFalse(receipt.StructuredContent.Value.GetRawText().Contains("demo@example.test"));
            var invalid = await client.CallToolAsync("quote_tickets", new Dictionary<string, object?>
                { ["lines"] = Array.Empty<object>() }, cancellationToken: timeout.Token);
            Assert.IsTrue(invalid.IsError);
            var policy = await client.ReadResourceAsync(new ReadResourceRequestParams
                { Uri = "ordering://policy" }, timeout.Token);
            StringAssert.Contains(((TextResourceContents)policy.Contents.Single()).Text, "15 minutes");
            var resource = await client.ReadResourceAsync(new ReadResourceRequestParams
                { Uri = $"ordering://orders/{orderId}" }, timeout.Token);
            StringAssert.Contains(((TextResourceContents)resource.Contents.Single()).Text, orderId.ToString());
            var prompts = await client.ListPromptsAsync(cancellationToken: timeout.Token);
            Assert.IsTrue(prompts.Any(p => p.Name == "guided_checkout"));
            var unsupported = await client.CallToolAsync("collect_demo_customer",
                cancellationToken: timeout.Token);
            Assert.IsTrue(unsupported.IsError);
        }
        finally { await app.StopAsync(); }
    }

    private sealed class DemoCatalog : IOrderingCatalog
    {
        public Task<CatalogEvent> GetEventAsync(Guid eventId, CancellationToken cancellationToken) =>
            Task.FromResult(new CatalogEvent(eventId, "Demo concert", 100m));
    }
}
