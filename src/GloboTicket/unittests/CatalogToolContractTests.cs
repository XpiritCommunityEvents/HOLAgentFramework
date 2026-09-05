using System.Reflection;
using GloboTicket.Catalog;
using GloboTicket.Catalog.MCP;
using GloboTicket.Catalog.Repositories;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace GloboTicket.UnitTests;

[TestClass]
public sealed class CatalogToolContractTests
{
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

        IReadOnlyList<ContentBlock> result = await tool.GetEventsByArtist(
            "  The Example Band  ",
            cancellation.Token);

        Assert.AreEqual("The Example Band", repository.Artist);
        Assert.AreEqual(20, repository.Limit);
        Assert.AreEqual(cancellation.Token, repository.CancellationToken);
        TextContentBlock content = (TextContentBlock)result.Single();
        Assert.Contains("ID: aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa", content.Text);
        Assert.Contains("Source: catalog/events/aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa", content.Text);
    }

    [TestMethod]
    public async Task Invalid_tool_input_returns_stable_validation_content_without_repository_call()
    {
        var repository = new RecordingEventRepository();
        var tool = new CatalogTool(repository);

        IReadOnlyList<ContentBlock> artist = await tool.GetEventsByArtist("   ");
        IReadOnlyList<ContentBlock> dates = await tool.GetEventsInDateRange(
            new DateTime(2026, 9, 5),
            new DateTime(2026, 9, 4));
        ContentBlock emptyId = await tool.GetEventDetails(Guid.Empty);

        Assert.AreEqual(0, repository.InvocationCount);
        Assert.Contains("Validation error", ((TextContentBlock)artist.Single()).Text);
        Assert.Contains("startDate", ((TextContentBlock)dates.Single()).Text);
        Assert.Contains("must not be empty", ((TextContentBlock)emptyId).Text);
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
            CancellationToken cancellationToken = default)
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
