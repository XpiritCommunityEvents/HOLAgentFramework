# Lab 3.2 - Adding Function Calling

In this lab, you will add function calling to your transportation agent.

## Create the functions

**Goal:** At the end of this lab, you should have an agent that can find and book affordable rides for you by calling functions you provide that can interact with actual systems. 

> 👉🏻 We assume that you completed the hands-on lab 3.1
if you have not completed lab 3.1, you can use the provided completed lab 3.1 here: `HolSemanticKernel/exercises/module4/completed3.1`

## Steps
1. Create a new class that will provide the functions the agent can use to retrieve available ride options and booking a ride that is approved by the end-user.

The class needs two functions that we can mark as `KernelFunction` so it can be added to the kernel so the agent can call them as part of its work.

We crete a class with the name `RideInformationSystemService` and we add a function to it that returns a list of available `Rides`.

We start with creating a class that defines the `Ride` options.
This class looks as follows:
``` c#
public class Ride
{
    public int RideId { get; set; }
    public string RideType { get; set; }
    public decimal Price { get; set; }
    public string ServiceName { get; set; }
    public string City { get; set; }
}
```

Next we create the function the agent can call the moment it needs options for rides. We give the function the name `RetrieveAvailableRides` and we provide it information about the city where we need a ride and the date of the ride.

this function looks as follows:
``` c#
[KernelFunction("get_available_rides"),
Description("Get available rides in a city for a given date")]
public Ride[] GetAvailableRides([Description("City where you need the ride")] string city, [Description("Date the ride is required")] DateTime bookingDate)
{

}
```
To simulate retrieval from a system with available ride options, we simply create a property that contains an array of available rides. We select the available rides from this list and return this as result of the functions.

The property where we can select rides from can be copied from this code:
```c#
private Ride[] availableRides =
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
Now you can implement the function to select the ride from this list.
This code looks as follows:
```c#
return availableRides
    Console.ForegroundColor = ConsoleColor.Cyan;
    Console.WriteLine($"Searching for Rides for City: {city} and Date: {bookingDate}");
    Console.ResetColor();
    .Where(r => r.City.Equals(city, StringComparison.OrdinalIgnoreCase))
    .ToArray();
```
We also create a function to actualy book the ride afer the user approved the suggestion. For this we create the function `BookARide` and we pass it in the `id` of the ride. We also mark it as a kernel function and provide the description information so the LLM knows how to call it when it wants to create a booking.

the function should look like follows:
``` c#
[KernelFunction("book_a_ride"),
Description("makes it possible to book a selected ride")]
[return: Description("returns true if the booking was successfull")]
public bool BookARide(int rideID)
{
    Console.ForegroundColor = ConsoleColor.Cyan;
    Console.WriteLine($"Booked the ride with id: {rideID}");
    Console.ResetColor();
    return true;
}
```

2. Add the functions to the agent
In the agent creation code we need to make some changes so we get the kernel functions added and the funcion call behavior of the LLM configured.

We start by changing the way we create the Kernel, and we provide it the functions it can call.
 Modify the `CreateChatcompletionAgent` function in the `ChatWithAgent` class. Add in the constructor of the agent the option `Arguments` and provide it a new instance of `KernelArguments` that set the `FunctionChoiceBehavior` to `FunctionChoiceBehavior.Auto()`
 By doing this, you enable function calling in the agent. 

 The agent constructot should now look as follows:
```c#
ChatCompletionAgent agent = new()
{
    Name = "TransportationAgent",
    Instructions = instructions,
    Description = "An agent that finds transportation options from hotel to concert location",
    Kernel = kernel,
    Arguments = new KernelArguments(new AzureOpenAIPromptExecutionSettings()
    {
        FunctionChoiceBehavior = FunctionChoiceBehavior.Auto(),
    }),
};
```
Now we only need to add the kernel functions to the agent. We do this right after we constructed the agent. 
Find the line where you constructed the agent, by calling `CreateChatCompletionAgent` and after this statement add the line `agent.Kernel.ImportPluginFromType<RideInformationSystemService>()`
The code fragment should look as follows:
``` c#
Console.WriteLine("******** Create the agent ***********");
var agent = CreateChatcompletionAgent(kernel);
agent.Kernel.ImportPluginFromType<RideInformationSystemService>();
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

:bulb: You see the agent now calls the functions and now asks you what to do next. This is where thee is a `handoff` to the user required. In the next lab we will add the interaction and handoff to the user and back to the agent to completely fullfill the booking of a ride.