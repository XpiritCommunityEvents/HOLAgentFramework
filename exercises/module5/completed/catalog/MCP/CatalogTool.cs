using System.ComponentModel;
using GloboTicket.Catalog.Repositories;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace GloboTicket.Catalog.MCP;

[McpServerToolType]
public sealed class CatalogTool(IEventRepository eventRepository)
{
    private const int MaxResults = 20;

    [McpServerTool(ReadOnly = true, Destructive = false), Description("Find up to 20 catalog events for an artist.")]
    public async Task<IReadOnlyList<ContentBlock>> GetEventsByArtist(
        [Description("Artist name (required)")] string artist,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(artist)) return Error("artist must not be empty");

        var events = await eventRepository.GetEventsByArtist(artist.Trim(), MaxResults, cancellationToken);
        return events.Select(MapEvent).ToList();
    }

    [McpServerTool(ReadOnly = true, Destructive = false), Description("Find up to 20 catalog events in an inclusive date range.")]
    public async Task<IReadOnlyList<ContentBlock>> GetEventsInDateRange(
        [Description("Inclusive start date")] DateTime startDate,
        [Description("Inclusive end date")] DateTime endDate,
        CancellationToken cancellationToken = default)
    {
        if (startDate > endDate) return Error("startDate must be earlier than or equal to endDate");

        var events = await eventRepository.GetEventsInDateRange(startDate, endDate, MaxResults, cancellationToken);
        return events.Select(MapEvent).ToList();
    }

    [McpServerTool(ReadOnly = true, Destructive = false), Description("Find up to 20 catalog events in a city or state.")]
    public async Task<IReadOnlyList<ContentBlock>> GetEventsInLocation(
        [Description("City or state (required)")] string location,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(location)) return Error("location must not be empty");

        var events = await eventRepository.GetEventsInLocation(location.Trim(), MaxResults, cancellationToken);
        return events.Select(MapEvent).ToList();
    }

    [McpServerTool(ReadOnly = true, Destructive = false), Description("Get up to 20 distinct artist names in the catalog.")]
    public async Task<IReadOnlyList<ContentBlock>> GetArtists(CancellationToken cancellationToken = default)
    {
        var artists = await eventRepository.GetArtists(MaxResults, cancellationToken);
        return artists.Select(artist => Text($"Artist: {artist}")).ToList();
    }

    [McpServerTool(ReadOnly = true, Destructive = false), Description("Get full details for a catalog event by stable identifier.")]
    public async Task<ContentBlock> GetEventDetails(
        [Description("Event identifier (required)")] Guid id,
        CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty) return Text("Validation error: event id must not be empty");

        var @event = await eventRepository.GetEventById(id, cancellationToken);
        if (@event is null) return Text($"Event not found: {id}");

        return Text($$"""
            ID: {{@event.EventId}}
            Source: catalog/events/{{@event.EventId}}
            {{@event.Name}}
            Artist: {{@event.Artist}}
            Date: {{@event.Date:yyyy-MM-dd}} at {{@event.Venue?.Name}}, {{@event.Venue?.Address}}, {{@event.Venue?.City}}, {{@event.Venue?.State}}, {{@event.Venue?.ZipCode}}.
            Seats available: {{@event.Venue?.SeatsAvailable ?? 0}}

            {{@event.Description}}

            Price: {{@event.Price}}
            """);
    }

    private static ContentBlock MapEvent(Event @event) => Text($$"""
        ID: {{@event.EventId}}
        Source: catalog/events/{{@event.EventId}}
        {{@event.Artist}} - {{@event.Date:yyyy-MM-dd}} at {{@event.Venue?.Name}} in {{@event.Venue?.City}}. Seats available: {{@event.Venue?.SeatsAvailable ?? 0}}
        """);

    private static IReadOnlyList<ContentBlock> Error(string message) => [Text($"Validation error: {message}")];

    private static TextContentBlock Text(string value) => new() { Text = value, Annotations = new() { Audience = [Role.Assistant] } };
}
