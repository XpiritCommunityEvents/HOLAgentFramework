namespace GloboTicket.Catalog.Repositories;

public class EventRepository : IEventRepository
{
    private List<Event> events = new List<Event>();
    private readonly ILogger<EventRepository> logger;

    public EventRepository(ILogger<EventRepository> logger)
    {
        this.logger = logger;

        LoadSampleData();
    }

    private void LoadSampleData()
    {
        var johnEgbertGuid = Guid.Parse("{CFB88E29-4744-48C0-94FA-B25B92DEA317}");
        var nickSailorGuid = Guid.Parse("{CFB88E29-4744-48C0-94FA-B25B92DEA318}");
        var michaelJohnsonGuid = Guid.Parse("{CFB88E29-4744-48C0-94FA-B25B92DEA319}");

        events.Add(new Event
        {
            EventId = johnEgbertGuid,
            Name = "John Egbert Live",
            Price = 65,
            OriginalPrice = 65,
            Artist = "John Egbert",
            Date = DateTime.Now.AddMonths(6),
            Description = "Join John for his farwell tour across 15 continents. John really needs no introduction since he has already mesmerized the world with his banjo.",
            ImageUrl = "/img/banjo.jpg",
            IsOnSpecialOffer = false
        });

        events.Add(new Event
        {
            EventId = michaelJohnsonGuid,
            Name = "The State of Affairs: Michael Live!",
            Price = 85,
            OriginalPrice = 85,
            Artist = "Michael Johnson",
            Date = DateTime.Now.AddMonths(9),
            Description = "Michael Johnson doesn't need an introduction. His 25 concert across the globe last year were seen by thousands. Can we add you to the list?",
            ImageUrl = "/img/michael.jpg",
            IsOnSpecialOffer = false
        });

        events.Add(new Event
        {
            EventId = nickSailorGuid,
            Name = "To the Moon and Back",
            Price = 135,
            OriginalPrice = 135,
            Artist = "Nick Sailor",
            Date = DateTime.Now.AddMonths(8),
            Description = "The critics are over the moon and so will you after you've watched this sing and dance extravaganza written by Nick Sailor, the man from 'My dad and sister'.",
            ImageUrl = "/img/musical.jpg",
            IsOnSpecialOffer = false
        });
    }

    public Task<IReadOnlyList<Event>> GetEvents(CancellationToken cancellationToken = default)
    {
        // Sort events by promotion status (promotions first) and then by date
        var sortedEvents = events.ToList()
            .OrderByDescending(e => e.IsOnSpecialOffer)
            .ThenBy(e => e.Artist)
            .ThenBy(e => e.Date)
            .ToList();
            
        // Return the sorted list
        return Task.FromResult((IReadOnlyList<Event>)sortedEvents);
    }


    public Task<IReadOnlyList<Event>> GetEventsByArtist(string artist, int limit, CancellationToken cancellationToken = default) => Task.FromResult((IReadOnlyList<Event>)events.Where(e => e.Artist.Contains(artist, StringComparison.OrdinalIgnoreCase)).Take(Math.Clamp(limit, 1, 50)).ToList());
    public Task<IReadOnlyList<Event>> GetEventsInDateRange(DateTime startDate, DateTime endDate, int limit, CancellationToken cancellationToken = default) => Task.FromResult((IReadOnlyList<Event>)events.Where(e => e.Date >= startDate && e.Date <= endDate).Take(Math.Clamp(limit, 1, 50)).ToList());
    public Task<IReadOnlyList<Event>> GetEventsInLocation(string location, int limit, CancellationToken cancellationToken = default) => Task.FromResult((IReadOnlyList<Event>)events.Where(e => e.Venue != null && (e.Venue.City.Contains(location, StringComparison.OrdinalIgnoreCase) || e.Venue.State.Contains(location, StringComparison.OrdinalIgnoreCase))).Take(Math.Clamp(limit, 1, 50)).ToList());
    public Task<IReadOnlyList<string>> GetArtists(int limit, CancellationToken cancellationToken = default) => Task.FromResult((IReadOnlyList<string>)events.Select(e => e.Artist).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(a => a).Take(Math.Clamp(limit, 1, 50)).ToList());

    public Task<Event> GetEventById(Guid eventId, CancellationToken cancellationToken = default)
    {
        var @event = events.ToList().FirstOrDefault(e => e.EventId == eventId);
        if (@event == null)
        {
            return Task.FromResult<Event>(null);
        }
        return Task.FromResult(@event);
    }

    // scheduled task calls this periodically to put one item on special offer
    public void UpdateSpecialOffer()
    {
        // reset all tickets to their default
        events.Clear();
        LoadSampleData();
        // pick a random one to put on special offer
        var random = new Random();
        var specialOfferEvent = events[random.Next(0, events.Count)];
        
        // Store the original price
        specialOfferEvent.OriginalPrice = specialOfferEvent.Price;
        
        // Apply 20 percent discount and round to psychological price
        var discountedPrice = specialOfferEvent.Price * 0.8m;
        specialOfferEvent.Price = Math.Round(discountedPrice - 0.01m, 2);
        
        // Mark as special offer
        specialOfferEvent.IsOnSpecialOffer = true;
        
        logger.LogInformation($"Event {specialOfferEvent.Name} is now on special offer at ${specialOfferEvent.Price} (was ${specialOfferEvent.OriginalPrice})");
    }
}
