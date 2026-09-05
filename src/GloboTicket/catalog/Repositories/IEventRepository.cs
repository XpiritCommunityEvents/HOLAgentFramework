namespace GloboTicket.Catalog.Repositories;

public interface IEventRepository
{
  Task<IReadOnlyList<Event>> GetEvents(CancellationToken cancellationToken = default);
  Task<IReadOnlyList<Event>> GetEventsByArtist(string artist, int limit, CancellationToken cancellationToken = default);
  Task<IReadOnlyList<Event>> GetEventsInDateRange(DateTime startDate, DateTime endDate, int limit, CancellationToken cancellationToken = default);
  Task<IReadOnlyList<Event>> GetEventsInLocation(string location, int limit, CancellationToken cancellationToken = default);
  Task<IReadOnlyList<string>> GetArtists(int limit, CancellationToken cancellationToken = default);
  Task<Event> GetEventById(Guid eventId, CancellationToken cancellationToken = default);
  void UpdateSpecialOffer();
}
