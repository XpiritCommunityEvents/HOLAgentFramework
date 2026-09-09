namespace modulerag;

public sealed record AvailableRoom(
    int RoomId,
    string RoomType,
    decimal PricePerNight,
    string HotelName,
    string Amenities,
    string City);
