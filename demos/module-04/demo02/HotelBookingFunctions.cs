using System.ComponentModel;

namespace modulerag;

public static class HotelBookingFunctions
{
    private static readonly AvailableRoom[] Rooms =
    [
        new(309, "Suite", 320, "Budget Stay", "WiFi, kitchenette", "Seattle"),
        new(318, "Suite", 410, "Mountain Lodge", "WiFi, fireplace", "Seattle"),
        new(339, "Suite", 355, "Opera Hotel", "WiFi, kitchenette", "Seattle")
    ];

    [Description("Gets available hotel rooms in a city")]
    public static AvailableRoom[] GetAvailableRooms(
        [Description("City where the guest needs a room")] string city) =>
        Rooms.Where(room => room.City.Equals(city, StringComparison.OrdinalIgnoreCase)).ToArray();

    [Description("Books the selected hotel room")]
    public static string BookRoom(
        [Description("ID of the selected room")] int roomId)
    {
        AvailableRoom? room = Rooms.FirstOrDefault(room => room.RoomId == roomId);
        return room is null
            ? $"Room {roomId} was not found."
            : $"Booked room {roomId} at {room.HotelName}.";
    }
}
