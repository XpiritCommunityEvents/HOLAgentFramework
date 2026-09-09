using System.ComponentModel;

namespace modulerag;

public sealed class HotelBookingFunctions
{
    private static readonly AvailableRoom[] Rooms =
    [
        new(101, "Standard", 145m, "Harbor Hotel", "Seattle"),
        new(102, "Deluxe", 210m, "Market Inn", "Seattle"),
        new(103, "Suite", 320m, "Lakeview Hotel", "Seattle")
    ];

    [Description("Find available hotel rooms in a city for a date.")]
    public AvailableRoom[] FindAvailableRooms(
        [Description("City where the room is needed.")] string city,
        [Description("Date of the hotel stay.")] DateTime stayDate)
    {
        Console.WriteLine($"Searching for rooms in {city} on {stayDate:d}.");
        return Rooms
            .Where(room => room.City.Equals(city, StringComparison.OrdinalIgnoreCase))
            .ToArray();
    }

    [Description("Book a room selected from the available rooms.")]
    public bool BookRoom([Description("ID of the room to book.")] int roomId)
    {
        Console.WriteLine($"Booking room {roomId}.");
        return Rooms.Any(room => room.RoomId == roomId);
    }
}

public sealed record AvailableRoom(
    int RoomId,
    string RoomType,
    decimal PricePerNight,
    string HotelName,
    string City);
