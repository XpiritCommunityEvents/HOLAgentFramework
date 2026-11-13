using System.ComponentModel;
using System.Text.Json;
using GloboTicket.Catalog.Repositories;
using Microsoft.Extensions.AI;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace GloboTicket.Catalog.MCP;

[McpServerResourceType, McpServerPromptType]
public sealed class CatalogContext(IEventRepository events)
{
    [McpServerResource(UriTemplate = "catalog://events/{eventId}", MimeType = "application/json")]
    [Description("Current concert details and USD ticket price. Venue capacity is not reservable ticket inventory.")]
    public async Task<string> EventDetails(string eventId, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(eventId, out var id) || id == Guid.Empty)
            throw new McpException("Supply a non-empty event UUID.");
        var item = await events.GetEventById(id, cancellationToken)
            ?? throw new McpException("Catalog event not found.");
        return JsonSerializer.Serialize(new
        {
            item.EventId, item.Name, item.Artist, item.Date, item.Description,
            item.Price, Currency = "USD", Venue = item.Venue?.Name,
            City = item.Venue?.City, VenueCapacity = item.Venue?.SeatsAvailable,
            Notice = "Demo catalog. Capacity does not guarantee available tickets; no seats are reserved."
        });
    }

    [McpServerPrompt(Name = "discover_concerts")]
    [Description("Start a concert search by artist, city and total USD budget. Supports artist argument completion.")]
    public static ChatMessage DiscoverConcerts(string artist, string city, string budgetUsd) =>
        new(ChatRole.User, $"""
            Find concerts for artist "{artist}" in "{city}" within a total budget of USD {budgetUsd}.
            Use catalog tools, intersect artist/location matches by event ID, and do not invent availability.
            Read the event's catalog resource for context. Ask how many tickets I want.
            Use ordering quotes to check the full price including discounts and fees before proposing a simulated order.
            """);

    public static async ValueTask<CompleteResult> CompleteArtist(
        RequestContext<CompleteRequestParams> request, CancellationToken cancellationToken)
    {
        if (request.Params.Ref is not PromptReference { Name: "discover_concerts" } ||
            request.Params.Argument.Name != "artist")
            return new() { Completion = new() { Values = [] } };
        var repository = request.Services!.GetRequiredService<IEventRepository>();
        var matches = await repository.GetArtists(21, cancellationToken, request.Params.Argument.Value.Trim());
        return new() { Completion = new()
        {
            Values = matches.Take(20).ToList(), HasMore = matches.Count > 20
        } };
    }
}
