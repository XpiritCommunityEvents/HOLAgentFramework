using System.ComponentModel;

namespace ModuleAgent;

public static class HotelBookingFunctions
{
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
            : $"Booked room {roomId} at {room.HotelName} in {room.City}.";
    }


        private static readonly AvailableRoom[] Rooms =
    [
        // Seattle
        new(301, "Standard", 180, "The Edgewater Hotel", "WiFi, ocean view", "Seattle"),
        new(302, "Deluxe", 260, "Fairmont Olympic Hotel", "WiFi, spa, room service", "Seattle"),
        new(303, "Suite", 410, "Four Seasons Hotel Seattle", "WiFi, infinity pool, spa", "Seattle"),
        new(304, "Standard", 160, "Hotel Theodore", "WiFi, coffee bar", "Seattle"),
        new(305, "Deluxe", 220, "W Seattle", "WiFi, fitness center, pet friendly", "Seattle"),

        // Las Vegas
        new(401, "Standard", 150, "Bellagio Hotel & Casino", "WiFi, casino, fountain view", "Las Vegas"),
        new(402, "Deluxe", 230, "The Venetian Resort", "WiFi, casino, gondola rides", "Las Vegas"),
        new(403, "Suite", 450, "Wynn Las Vegas", "WiFi, spa, golf course", "Las Vegas"),
        new(404, "Standard", 120, "Caesars Palace", "WiFi, casino, pools", "Las Vegas"),
        new(405, "Suite", 520, "ARIAL Resort & Casino", "WiFi, modern tech, dining", "Las Vegas"),
        new(406, "Deluxe", 290, "The Cosmopolitan of Las Vegas", "WiFi, balcony, nightlife", "Las Vegas"),
        new(407, "Standard", 110, "MGM Grand", "WiFi, lazy river, casino", "Las Vegas"),
        new(408, "Suite", 380, "Mandalay Bay", "WiFi, beach pool, aquarium", "Las Vegas"),

        // Ibiza
        new(501, "Deluxe", 340, "Ushuaïa Ibiza Beach Hotel", "WiFi, pool parties, stage view", "Ibiza"),
        new(502, "Suite", 620, "Ibiza Gran Hotel", "WiFi, spa, luxury casino", "Ibiza"),
        new(503, "Standard", 210, "Hard Rock Hotel Ibiza", "WiFi, beach access, live music", "Ibiza"),
        new(504, "Deluxe", 450, "Nobu Hotel Ibiza Bay", "WiFi, oceanfront, fine dining", "Ibiza"),
        new(505, "Standard", 190, "Pikes Ibiza", "WiFi, cocktail bar, boutique vibe", "Ibiza"),
        new(506, "Suite", 580, "Six Senses Ibiza", "WiFi, infinity pool, wellness center", "Ibiza"),
        new(507, "Deluxe", 310, "Destino Pacha Ibiza Resort", "WiFi, sunset view, DJ events", "Ibiza"),

        // Amsterdam
        new(601, "Standard", 195, "The Dylan Amsterdam", "WiFi, canal view, courtyard garden", "Amsterdam"),
        new(602, "Deluxe", 360, "Waldorf Astoria Amsterdam", "WiFi, historic canal palace, spa", "Amsterdam"),
        new(603, "Suite", 490, "Conservatorium Hotel", "WiFi, indoor pool, spa, museum district", "Amsterdam"),
        new(604, "Standard", 175, "Pulitzer Amsterdam", "WiFi, canal boat tour, terrace", "Amsterdam"),
        new(605, "Deluxe", 280, "W Amsterdam", "WiFi, rooftop pool, lounge", "Amsterdam"),
        new(606, "Standard", 150, "Hotel Okura Amsterdam", "WiFi, Michelin dining, spa", "Amsterdam"),
        new(607, "Deluxe", 310, "Sofitel Legend The Grand Amsterdam", "WiFi, spa, historic building", "Amsterdam"),

        // Paris
        new(701, "Suite", 750, "The Ritz Paris", "WiFi, spa, historic bar", "Paris"),
        new(702, "Deluxe", 580, "Le Meurice", "WiFi, Tuileries garden view, dining", "Paris"),
        new(703, "Standard", 290, "Hôtel Plaza Athénée", "WiFi, Eiffel tower view, spa", "Paris"),
        new(704, "Suite", 820, "Four Seasons Hotel George V", "WiFi, courtyard, 3 Michelin stars", "Paris"),
        new(705, "Standard", 220, "Mama Shelter Paris East", "WiFi, rooftop bar, modern design", "Paris"),

        // New York
        new(801, "Deluxe", 420, "The Plaza Hotel", "WiFi, Central Park view, champagne bar", "New York"),
        new(802, "Suite", 650, "The Carlyle, A Rosewood Hotel", "WiFi, live jazz, historic charm", "New York"),
        new(803, "Standard", 260, "The Standard, High Line", "WiFi, rooftop lounge, river view", "New York"),
        new(804, "Deluxe", 390, "1 Hotel Central Park", "WiFi, eco-friendly, fitness center", "New York"),
        new(805, "Suite", 540, "The NoMad Hotel", "WiFi, library bar, classic decor", "New York"),

        // Tokyo
        new(901, "Deluxe", 460, "Aman Tokyo", "WiFi, city panorama, traditional onsen", "Tokyo"),
        new(902, "Suite", 510, "Park Hyatt Tokyo", "WiFi, Shinjuku view, indoor pool", "Tokyo"),
        new(903, "Standard", 210, "Hotel Gracery Shinjuku", "WiFi, Godzilla view, central location", "Tokyo"),
        new(904, "Deluxe", 370, "The Ritz-Carlton Tokyo", "WiFi, Roppongi views, club lounge", "Tokyo"),
        new(905, "Standard", 185, "Hoshinoya Tokyo", "WiFi, hot spring, Japanese breakfast", "Tokyo"),

        // London
        new(1001, "Suite", 680, "The Savoy", "WiFi, Thames view, butler service", "London"),
        new(1002, "Deluxe", 520, "Claridge's", "WiFi, afternoon tea, art deco style", "London"),
        new(1003, "Standard", 240, "The Hoxton, Shoreditch", "WiFi, restaurant, vibrant lounge", "London"),
        new(1004, "Deluxe", 410, "The Langham London", "WiFi, spa, cocktail bar", "London"),
        new(1005, "Suite", 600, "The Shard Shangri-La", "WiFi, skyline infinity pool, skyline view", "London"),

        // Rome
        new(1101, "Deluxe", 360, "Hotel de Russie", "WiFi, terraced gardens, spa", "Rome"),
        new(1102, "Suite", 490, "Hassler Roma", "WiFi, Spanish Steps view, terrace", "Rome"),
        new(1103, "Standard", 210, "Hotel Eden", "WiFi, panoramic rooftop, central", "Rome"),
        new(1104, "Deluxe", 330, "The St. Regis Rome", "WiFi, historic ballroom, butler service", "Rome")
    ];
}
