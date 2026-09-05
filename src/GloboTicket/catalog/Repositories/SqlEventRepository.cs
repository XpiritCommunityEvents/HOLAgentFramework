using GloboTicket.Catalog.DbContexts;
using Microsoft.EntityFrameworkCore;


namespace GloboTicket.Catalog.Repositories;

public class SqlEventRepository : IEventRepository
{
    private readonly EventCatalogDbContext _eventCatalogDbContext;


    private readonly ILogger<SqlEventRepository> logger;

    public SqlEventRepository(EventCatalogDbContext eventCatalogDbContext,
        ILogger<SqlEventRepository> logger)
    {
        this.logger = logger;
        _eventCatalogDbContext = eventCatalogDbContext;
    }

    private static int ClampLimit(int limit) => Math.Clamp(limit, 1, 50);

    private IQueryable<Event> OrderedEvents() => _eventCatalogDbContext.Events
        .AsNoTracking().Include(e => e.Venue)
        .OrderByDescending(e => e.IsOnSpecialOffer).ThenBy(e => e.Date).ThenBy(e => e.Venue!.City);

    public async Task<IReadOnlyList<Event>> GetEvents(CancellationToken cancellationToken = default)
    {
        return await OrderedEvents().ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Event>> GetEventsByArtist(string artist, int limit, CancellationToken cancellationToken = default) => await OrderedEvents().Where(e => e.Artist != null && e.Artist.Contains(artist)).Take(ClampLimit(limit)).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Event>> GetEventsInDateRange(DateTime startDate, DateTime endDate, int limit, CancellationToken cancellationToken = default) => await OrderedEvents().Where(e => e.Date >= startDate && e.Date <= endDate).Take(ClampLimit(limit)).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Event>> GetEventsInLocation(string location, int limit, CancellationToken cancellationToken = default) => await OrderedEvents().Where(e => e.Venue != null && ((e.Venue.City != null && e.Venue.City.Contains(location)) || (e.Venue.State != null && e.Venue.State.Contains(location)))).Take(ClampLimit(limit)).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<string>> GetArtists(int limit, CancellationToken cancellationToken = default) => await _eventCatalogDbContext.Events.AsNoTracking().Where(e => e.Artist != null).Select(e => e.Artist!).Distinct().OrderBy(a => a).Take(ClampLimit(limit)).ToListAsync(cancellationToken);

    public async Task<Event> GetEventById(Guid eventId, CancellationToken cancellationToken = default)
    {
        var @event = await _eventCatalogDbContext.Events
            .AsNoTracking().Include(e => e.Venue)
            .FirstOrDefaultAsync(e => e.EventId == eventId, cancellationToken);
        return @event;
    }

    void IEventRepository.UpdateSpecialOffer()
    {
        // Reset any existing special offers first
        var currentOffers = _eventCatalogDbContext.Events.Where(e => e.IsOnSpecialOffer).ToList();
        foreach (var offer in currentOffers)
        {
            offer.Price = offer.OriginalPrice;
            offer.IsOnSpecialOffer = false;
        }

        // Get all events to select a random one
        var allEvents = _eventCatalogDbContext.Events.ToList();
        if (allEvents.Count == 0)
        {
            logger.LogWarning("No events found to put on special offer");
            return;
        }

        // Pick a random event for special offer
        var random = new Random();
        var specialOfferEvent = allEvents[random.Next(0, allEvents.Count)];
        
        // Store the original price before discount
        specialOfferEvent.OriginalPrice = specialOfferEvent.Price;
        
        // Apply 20 percent discount and round to psychological price
        var discountedPrice = specialOfferEvent.Price * 0.8m;
        specialOfferEvent.Price = Math.Round(discountedPrice - 0.01m, 2);
        
        // Mark as special offer
        specialOfferEvent.IsOnSpecialOffer = true;
        
        // Save changes to the database
        _eventCatalogDbContext.SaveChanges();
        
        logger.LogInformation($"Event {specialOfferEvent.Name} is now on special offer at ${specialOfferEvent.Price} (was ${specialOfferEvent.OriginalPrice})");
    }
}
