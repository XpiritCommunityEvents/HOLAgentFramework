using System.ComponentModel;

namespace ModuleWorkflow;

internal static class RideInformationSystemService
{
    private static readonly Ride[] Rides =
    [
        new(14, "Taxi", 29, "City Cabs", "Seattle"),
        new(15, "Ride Share", 22, "Contoso Rides", "Seattle"),
        new(16, "Shuttle", 16, "Concert Shuttle", "Seattle")
    ];

    [Description("Gets available rides in a city")]
    public static Ride[] GetAvailableRides(
        [Description("City where the guest needs a ride")] string city) =>
        Rides.Where(ride => ride.City.Equals(city, StringComparison.OrdinalIgnoreCase)).ToArray();

    [Description("Books the selected ride")]
    public static string BookRide(
        [Description("ID of the selected ride")] int rideId)
    {
        Ride? ride = Rides.FirstOrDefault(ride => ride.RideId == rideId);
        return ride is null
            ? $"Ride {rideId} was not found."
            : $"Booked ride {rideId} with {ride.ServiceName}.";
    }
}
