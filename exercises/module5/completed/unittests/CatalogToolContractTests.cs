using System.Collections.Concurrent;
using System.Reflection;
using GloboTicket.Catalog;
using GloboTicket.Catalog.MCP;
using GloboTicket.Catalog.Repositories;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ModelContextProtocol;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace GloboTicket.UnitTests;

[TestClass]
public sealed class CatalogToolContractTests
{
    [TestMethod]
    [DataRow("get_events_by_artist")]
    [DataRow("get_event_details")]
    public async Task Mcp_tools_call_result_serializes_catalog_content(string toolName)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Logging.ClearProviders();
        var failures = new CapturedFailures();
        builder.Logging.AddProvider(failures);
        builder.Services.AddSingleton<IEventRepository, RecordingEventRepository>();
        builder.Services.AddMcpServer().WithHttpTransport().WithTools<CatalogTool>();
        await using var app = builder.Build();
        app.MapMcp("/mcp");
        await app.StartAsync();
        try
        {
            string address = app.Services.GetRequiredService<IServer>()
                .Features.Get<IServerAddressesFeature>()!.Addresses.Single();
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            await using var client = await McpClient.CreateAsync(new HttpClientTransport(
                new HttpClientTransportOptions { Endpoint = new Uri(address + "/mcp") }),
                cancellationToken: timeout.Token);
            var arguments = toolName == "get_event_details"
                ? new Dictionary<string, object?> { ["id"] = Guid.Empty.ToString() }
                : new Dictionary<string, object?> { ["artist"] = "The Example Band" };
            CallToolResult result = await client.CallToolAsync(toolName, arguments,
                cancellationToken: timeout.Token);
            Assert.AreNotEqual(true, result.IsError);
            var content = (TextContentBlock)result.Content.Single();
            StringAssert.Contains(content.Text, toolName == "get_event_details"
                ? "Validation error" : "The Example Band");
        }
        catch (McpProtocolException)
        {
            Assert.Fail(string.Join("\n", failures.Exceptions));
        }
        finally
        {
            await app.StopAsync();
        }
    }

    private sealed class CapturedFailures : ILoggerProvider, ILogger
    {
        internal ConcurrentQueue<Exception> Exceptions { get; } = new();
        public ILogger CreateLogger(string categoryName) => this;
        public void Dispose() { }
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state,
            Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (exception is not null) Exceptions.Enqueue(exception);
        }
    }

    [TestMethod]
    public void Every_catalog_tool_is_explicitly_read_only_and_non_destructive()
    {
        MethodInfo[] toolMethods = typeof(CatalogTool).GetMethods(BindingFlags.Instance | BindingFlags.Public)
            .Where(method => method.GetCustomAttribute<McpServerToolAttribute>() is not null)
            .ToArray();

        Assert.HasCount(5, toolMethods);
        foreach (MethodInfo method in toolMethods)
        {
            McpServerToolAttribute annotation = method.GetCustomAttribute<McpServerToolAttribute>()!;
            Assert.IsTrue(annotation.ReadOnly, $"{method.Name} must declare ReadOnly=true.");
            Assert.IsFalse(annotation.Destructive, $"{method.Name} must declare Destructive=false.");
        }
    }

    [TestMethod]
    public async Task Artist_lookup_trims_input_bounds_results_and_propagates_cancellation()
    {
        var repository = new RecordingEventRepository();
        var tool = new CatalogTool(repository);
        using var cancellation = new CancellationTokenSource();

        string result = await tool.GetEventsByArtist(
            "  The Example Band  ",
            cancellation.Token);

        Assert.AreEqual("The Example Band", repository.Artist);
        Assert.AreEqual(20, repository.Limit);
        Assert.AreEqual(cancellation.Token, repository.CancellationToken);
        Assert.Contains("ID: aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa", result);
        Assert.Contains("Source: catalog/events/aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa", result);
    }

    [TestMethod]
    public async Task Invalid_tool_input_returns_stable_validation_content_without_repository_call()
    {
        var repository = new RecordingEventRepository();
        var tool = new CatalogTool(repository);

        string artist = await tool.GetEventsByArtist("   ");
        string dates = await tool.GetEventsInDateRange(
            new DateTime(2026, 9, 5),
            new DateTime(2026, 9, 4));
        string emptyId = await tool.GetEventDetails(Guid.Empty);

        Assert.AreEqual(0, repository.InvocationCount);
        Assert.Contains("Validation error", artist);
        Assert.Contains("startDate", dates);
        Assert.Contains("must not be empty", emptyId);
    }

    private sealed class RecordingEventRepository : IEventRepository
    {
        internal int InvocationCount { get; private set; }
        internal string? Artist { get; private set; }
        internal int Limit { get; private set; }
        internal CancellationToken CancellationToken { get; private set; }

        public Task<Event> GetEventById(Guid eventId, CancellationToken cancellationToken = default)
        {
            InvocationCount++;
            return Task.FromResult<Event>(null!);
        }

        public Task<IReadOnlyList<Event>> GetEventsByArtist(
            string artist,
            int limit,
            CancellationToken cancellationToken = default)
        {
            InvocationCount++;
            Artist = artist;
            Limit = limit;
            CancellationToken = cancellationToken;
            IReadOnlyList<Event> events =
            [
                new Event
                {
                    EventId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                    Artist = artist,
                    Date = new DateTime(2026, 10, 1),
                    Venue = new Venue { Name = "Test Hall", City = "Rotterdam", SeatsAvailable = 10 }
                }
            ];
            return Task.FromResult(events);
        }

        public Task<IReadOnlyList<Event>> GetEventsInDateRange(
            DateTime startDate,
            DateTime endDate,
            int limit,
            CancellationToken cancellationToken = default)
        {
            InvocationCount++;
            return Task.FromResult<IReadOnlyList<Event>>([]);
        }

        public Task<IReadOnlyList<Event>> GetEventsInLocation(
            string location,
            int limit,
            CancellationToken cancellationToken = default)
        {
            InvocationCount++;
            return Task.FromResult<IReadOnlyList<Event>>([]);
        }

        public Task<IReadOnlyList<string>> GetArtists(
            int limit,
            CancellationToken cancellationToken = default,
            string? prefix = null)
        {
            InvocationCount++;
            return Task.FromResult<IReadOnlyList<string>>([]);
        }

        public Task<IReadOnlyList<Event>> GetEvents(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Event>>([]);

        public void UpdateSpecialOffer()
        {
        }
    }
}
