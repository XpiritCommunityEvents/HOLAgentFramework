using System.Net;
using System.Net.Http.Json;

namespace GloboTicket.Ordering.Services;

public sealed record CatalogEvent(Guid EventId, string Name, decimal Price);

public interface IOrderingCatalog
{
    Task<CatalogEvent> GetEventAsync(Guid eventId, CancellationToken cancellationToken);
}

public sealed class OrderingCatalog(HttpClient http) : IOrderingCatalog
{
    public async Task<CatalogEvent> GetEventAsync(Guid eventId, CancellationToken cancellationToken)
    {
        using var response = await http.GetAsync($"event/{eventId}", cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
            throw new OrderException($"Catalog event not found: {eventId}.", 404);
        response.EnsureSuccessStatusCode();
        var item = await response.Content.ReadFromJsonAsync<CatalogEvent>(cancellationToken)
            ?? throw new OrderException("The catalog returned an empty event.", 502);
        if (item.EventId != eventId || string.IsNullOrWhiteSpace(item.Name))
            throw new OrderException("The catalog returned an invalid event.", 502);
        return item;
    }
}
