namespace modulerag;

public static partial class HotelBookingFunctions
{
    public class AvailableRoom
    {
        public int RoomId { get; set; }
        public string RoomType { get; set; }
        public decimal PricePerNight { get; set; }
        public string HotelName { get; set; }
        public int NumberOfAdultsAllowedInRoom { get; set; }
        public string Amenities { get; set; }
        public string City { get; set; }
    }
}
