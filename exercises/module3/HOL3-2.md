# Lab 3.2 - Adding Function Calling

In this lab, you will add function calling to your transportation agent.

## Create the functions

**Goal:** At the end of this lab, you should have an agent that can find and book affordable rides for you by calling functions you provide that can interact with actual systems. 

> 👉🏻 We assume that you completed the hands-on lab 3.1
if you have not completed lab 3.1, you can use the provided completed lab 3.1 here: `exercises/module4/completed3.1`

## Steps

1. You are going to create class with a method that returns a list of available `Ride`s. First create a new class `Ride` class for this return value. The `Ride` class defines the available options:

```csharp
public class Ride
{
    public int RideId { get; set; }
    public string RideType { get; set; }
    public decimal Price { get; set; }
    public string ServiceName { get; set; }
    public string City { get; set; }
}
```

Create a new class named `RideInformationSystemService` that will provide the functions the agent can use to retrieve available ride options and booking a ride that is approved by the end-user.

The class needs two functions that we can mark with `KernelFunction` attributes so it can be added to the kernel so the agent can call them as part of its work.

Create a function the agent can call when needs options for rides. Name it `RetrieveAvailableRides` and we provide it information about the city where we need a ride and the date of the ride.

This function looks as follows:

```csharp
[KernelFunction("get_available_rides"),
Description("Get available rides in a city for a given date")]
public Ride[] GetAvailableRides([Description("City where you need the ride")] string city, [Description("Date the ride is required")] DateTime bookingDate)
{

}
```

To simulate retrieval from a system with available ride options, we simply create a private field in the `RideInformationSystemService` that contains an array of available rides. We select the available rides from this list and return this as result of the functions.

The field where we can select rides from can be copied from this code:

```csharp
private readonly Ride[] availableRides =
[
    new Ride { RideId = 1, RideType = "Taxi", Price = 25.00m, ServiceName = "City Cabs", City = "New York" },
    new Ride { RideId = 2, RideType = "Ride Share", Price = 20.00m, ServiceName = "Uber", City = "New York" },
    new Ride { RideId = 3, RideType = "Limousine", Price = 100.00m, ServiceName = "Luxury Rides", City = "Los Angeles" },
    new Ride { RideId = 4, RideType = "Taxi", Price = 30.00m, ServiceName = "LA Cabs", City = "Los Angeles" },
    new Ride { RideId = 5, RideType = "Ride Share", Price = 22.00m, ServiceName = "Lyft", City = "Chicago" },
    new Ride { RideId = 6, RideType = "Taxi", Price = 28.00m, ServiceName = "Chicago Taxis", City = "Chicago" },
    new Ride { RideId = 7, RideType = "Shuttle", Price = 15.00m, ServiceName = "City Shuttle", City = "Miami" },
    new Ride { RideId = 8, RideType = "Ride Share", Price = 18.00m, ServiceName = "Uber", City = "Miami" },
    new Ride { RideId = 9, RideType = "Taxi", Price = 27.00m, ServiceName = "Miami Cabs", City = "Miami" },
    new Ride { RideId = 10, RideType = "Limousine", Price = 120.00m, ServiceName = "Elite Rides", City = "New York" },
    new Ride { RideId = 11, RideType = "Taxi", Price = 26.00m, ServiceName = "Downtown Taxis", City = "San Francisco" },
    new Ride { RideId = 12, RideType = "Ride Share", Price = 21.00m, ServiceName = "Lyft", City = "San Francisco" },
    new Ride { RideId = 13, RideType = "Shuttle", Price = 16.00m, ServiceName = "Airport Shuttle", City = "San Francisco" },
    new Ride { RideId = 14, RideType = "Taxi", Price = 29.00m, ServiceName = "City Cabs", City = "Seattle" },
];
```

Now you can implement the `GetAvailableRides` function to select the ride from this list.
This code looks as follows:

```csharp
    Console.ForegroundColor = ConsoleColor.Cyan;
    Console.WriteLine($"Searching for Rides for City: {city} and Date: {bookingDate}");
    Console.ResetColor();
    return availableRides
        .Where(r => r.City.Equals(city, StringComparison.OrdinalIgnoreCase))
        .ToArray();
```

We also create a function to actually book the ride after the user approved the suggestion. For this we create the function `BookARide` and we pass it the `id` of the ride. We also mark it as a kernel function and provide the description information so the LLM knows how to call it when it wants to create a booking.

The function should look as follows:

```csharp
[KernelFunction("book_a_ride"),
Description("makes it possible to book a selected ride")]
[return: Description("returns true if the booking was successful")]
public bool BookARide(int rideID)
{
    Console.ForegroundColor = ConsoleColor.Cyan;
    Console.WriteLine($"Booked the ride with id: {rideID}");
    Console.ResetColor();
    return true;
}
```

2. Add the functions to the agent

In the `CreateTransportationAgent` method, we need to make some changes so can add the kernel functions added and set the `FunctionChoiceBehavior` behavior of the LLM configured.

We start by changing the way we initialize the transportation agent. Modify the `CreateTransportationAgent` function in the `ChatWithAgent` class. In the constructor of the `ChatCompletionAgent`, add the `Arguments` property and provide it a new instance of `KernelArguments` that with `FunctionChoiceBehavior` set to `FunctionChoiceBehavior.Auto()`.

By doing this, you enable function calling in the agent. 

The agent constructor should now look as follows:

```csharp
ChatCompletionAgent agent = new()
{
    Name = "TransportationAgent",
    Instructions = instructions,
    Description = "An agent that finds transportation options from hotel to concert location",
    Kernel = kernel,
    Arguments = new KernelArguments(new OpenAIPromptExecutionSettings()
    {
        FunctionChoiceBehavior = FunctionChoiceBehavior.Auto(),
    }),
};
```

Now, we only need to provide the kernel functions to the agent. We do this right after constructing the agent. 

Find the line where you call `CreateTransportationAgent` and provide the `RideInformationSystemService` to the agent's kernel.

The code fragment should look as follows:

```csharp
Console.WriteLine("******** Create the agent ***********");
var transportationAgent = CreateTransportationAgent(kernel);
transportationAgent.Kernel.ImportPluginFromType<RideInformationSystemService>();
```

3. Now see if the functions are called

Run the program and the results should look like this:

```console
******** Create the kernel ***********
******** Create the agent ***********
******** Start the agent ***********
******** RESPONSE ***********
Searching for Rides for City: Seattle and Date: 11/20/2023 12:00:00 AM
Searching for Rides for City: Seattle and Date: 11/20/2023 12:00:00 AM
Searching for Rides for City: Seattle and Date: 11/20/2023 12:00:00 AM
Thread: d13ff68066cd4efeab894211e1e36611
Thread data: Microsoft.SemanticKernel.Agents.ChatHistoryAgentThread
Author: TransportationAgent
Message:It seems there are not many ride options showing up for now, and the previously mentioned **Taxi by City Cabs** at **$29.00** is the only one available.

Would you like me to proceed with booking this ride for you, or would you prefer to consider other transportation methods independently? Let me know!
```

> :bulb: You see the agent now calls the functions and now asks you what to do next. This is where thee is a `handoff` to the user required. In the next lab we will add the interaction and handoff to the user and back to the agent to completely fullfill the booking of a ride.

This concludes Lab 3.2