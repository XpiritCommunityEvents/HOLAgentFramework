using System.ComponentModel;

namespace modulerag;

internal sealed class RideInformationSystemService
{
    [Description("Get available rides in a city for a given date.")]
    public Ride[] GetAvailableRides(
        [Description("City where the ride is needed.")] string city,
        [Description("Date and time when the ride is needed.")] DateTime bookingDate)
    {
        Console.WriteLine($"Searching for rides in {city} on {bookingDate:g}.");

        return AvailableRides
            .Where(ride => ride.City.Equals(city, StringComparison.OrdinalIgnoreCase))
            .ToArray();
    }

    [Description("Book a selected ride.")]
    public bool BookARide([Description("ID of the ride to book.")] int rideId)
    {
        Console.WriteLine($"Booking ride {rideId}.");
        return AvailableRides.Any(ride => ride.RideId == rideId);
    }

    private static readonly Ride[] AvailableRides =
    [
        new(1, "Bus", 3.00m, "King County Metro", "Seattle"),
        new(2, "Ride share", 18.00m, "Contoso Cars", "Seattle"),
        new(3, "Taxi", 29.00m, "City Cabs", "Seattle")
    ];
}

public sealed record Ride(int RideId, string RideType, decimal Price, string ServiceName, string City);
