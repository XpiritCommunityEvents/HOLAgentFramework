namespace Module04.Demo05.Models;

public sealed record ConcertTicket(
    string Artist,
    string VenueName,
    string City,
    DateTime EventDate,
    string? Seat = null);

public sealed record HotelOption(
    int RoomId,
    string HotelName,
    string RoomType,
    decimal PricePerNight,
    string Amenities,
    string City);

public sealed record RideOption(
    int RideId,
    string RideType,
    string ServiceName,
    decimal Price,
    string City);

public sealed record TravelItinerary(
    ConcertTicket Ticket,
    HotelOption Hotel,
    RideOption Ride);
