using Microsoft.Agents.AI;
using Module04.Demo05.Models;

namespace Module04.Demo05.Orchestration;

public static class ConcurrentOrchestration
{
    public static async Task RunAsync(
        string request,
        AIAgent ticketAgent,
        AIAgent hotelAgent,
        AIAgent rideAgent,
        AIAgent itineraryAgent)
    {
        string ticket = await RunToTextAsync(ticketAgent, request);
        string hotelRequest = $"Concert ticket details:\n{ticket}\nFind three hotel options and recommend one.";
        string rideRequest = $"Concert ticket details:\n{ticket}\nFind ride options and recommend one.";

        Task<string> hotelTask = RunToTextAsync(hotelAgent, hotelRequest);
        Task<string> rideTask = RunToTextAsync(rideAgent, rideRequest);
        await Task.WhenAll(hotelTask, rideTask);

        string itineraryRequest = $"""
            Create a PDF itinerary from these results.
            Ticket: {ticket}
            Hotel: {hotelTask.Result}
            Ride: {rideTask.Result}
            """;
        Console.WriteLine(await RunToTextAsync(itineraryAgent, itineraryRequest));
    }

    private static async Task<string> RunToTextAsync(AIAgent agent, string request)
    {
        var text = new System.Text.StringBuilder();
        await foreach (var update in agent.RunStreamingAsync(request))
        {
            text.Append(update.Text);
        }

        return text.ToString();
    }
}
