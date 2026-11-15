#nullable enable
using System.ComponentModel;
using GloboTicket.Catalog.Repositories;
using ModelContextProtocol;
using ModelContextProtocol.Server;

namespace GloboTicket.Catalog.MCP;

[McpServerToolType]
public sealed class CatalogUiPrototypeTool(IEventRepository events)
{
    [McpServerTool(Name = "browse_concerts", ReadOnly = true, Destructive = false,
        OpenWorld = false, UseStructuredContent = true)]
    [Description("Prototype: browse up to 20 concerts with current USD prices and an interactive comparison UI. Supply an artist or location, or both. Capacity is not ticket availability.")]
    public async Task<PrototypeConcertResults> BrowseConcerts(string? artist = null, string? location = null,
        CancellationToken cancellationToken = default)
    {
        artist = artist?.Trim();
        location = location?.Trim();
        if (string.IsNullOrEmpty(artist) && string.IsNullOrEmpty(location))
            throw new McpException("Supply an artist or location.");

        var matches = !string.IsNullOrEmpty(artist)
            ? await events.GetEventsByArtist(artist, 20, cancellationToken)
            : await events.GetEventsInLocation(location!, 20, cancellationToken);
        if (!string.IsNullOrEmpty(artist) && !string.IsNullOrEmpty(location))
        {
            var localIds = (await events.GetEventsInLocation(location, 20, cancellationToken))
                .Select(item => item.EventId).ToHashSet();
            matches = matches.Where(item => localIds.Contains(item.EventId)).ToList();
        }
        return new(matches.Select(item => new PrototypeConcert(
            item.EventId, item.Name, item.Artist, item.Date, item.Price, "USD",
            item.Venue?.Name, item.Venue?.City, item.Venue?.SeatsAvailable)).ToArray(),
            "Bounded search: up to 20 matches per artist/location lookup. Venue capacity is not available inventory. Request a quote including fees; no seats are reserved.");
    }
}

public sealed record PrototypeConcertResults(IReadOnlyList<PrototypeConcert> Events, string Notice);
public sealed record PrototypeConcert(Guid EventId, string Name, string Artist, DateTime Date,
    decimal Price, string Currency, string? Venue, string? City, int? VenueCapacity);
