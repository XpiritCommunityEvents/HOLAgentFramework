# Lab 3.1 - Using Agents

In this lab, we will explore the use of agents as a way to interact with LLM's.

## Create an Agent that can book rides

**Goal:** At the end of this lab, you should have an agent that can find affordable rides for you and can also book them at a booking provider.

> 👉🏻 We assume that you use a clean startup solution that we created that contains the boilerplate to get you started. You can find this in `HolSemanticKernel/exercises/module4/start`.

### Steps

1. Open the folder in your codespace and load the solution `module-agent-start.sln`:

In the solution you find two classes. `Program.cs`, which is the entrypoint of the console application and `ChatWithAgent.cs`
The `program.cs` is similar as in the other excersises where we create an instance of the class `ChatWithAgent` and we call the method we want to execute.

The boilerplate for the method `let_agent_find_ride` is created and you are now going to create an Agent to first have asimilar conversation as you had with the `IChatCompletion` interface in the previous excersises.

When you run the application the output should look as follows:
``` console
******** Create the kernel ***********
******** Create the agent ***********
******** Start the agent ***********
******** RESPONSE ***********
```

2. Create the semantic Kernel object, with the provided model, endpoint and apiKey.

>:warning: Since this is a new solution, you need to add the Api key to the user secret store again. The empty project, does not contain the api key yet.

This coude should be famiiar by now. You create the KernelBuilder, you create the credentials and then Add an `AIChatCompletion`endpoint to the `KernelBuilder`. Next you build the KernelBuilder and you now have your `Kernel` object.

The code should look as follows (Azure Open AI):
``` c#
 IKernelBuilder kernelBuilder = Kernel.CreateBuilder();
 var credential = new AzureKeyCredential(apiKey);
 var client = new AzureOpenAIClient(new Uri(endpoint), credential);
 kernelBuilder.AddAzureOpenAIChatCompletion(deploymentName, client);
 Kernel kernel = kernelBuilder.Build();
```
Or it should look like this for OpenAI
``` c#
IKernelBuilder kernelBuilder = Kernel.CreateBuilder();
kernelBuilder.AddOpenAIChatCompletion(deploymentName,new Uri(endpoint), apiKey);
Kernel kernel = kernelBuilder.Build();
```

You can validate if this works by simply adding one line for testing purposes and run the program again.

```c#
var result = await kernel.InvokePromptAsync("what color is the sky?");
  ```

3. Create the agent with the `kernel` object we just constructed.

Now add a function where you pass in the kernel object and return a `ChatCompletionAgent`, name the fuction `CreateChatCompletionAgent`

We start by defining the instructions for the agent. The agent we want to create is one that can find us a Ride from our hotel to the concert venue. Feel free to experiment with your own instructions, these are the instructions we used when creating this hands-on lab:

``` c#
var instructions = """
    You are an expert in finding transportation options from a given hotel location to the concert location.
    You will try to get the best options available for an afordable price.Make sure the customer will be there at least 30 minutes
    before the concert starts at the venue. You always suggest 3 options with different price ranges.
    You will ask for approval before you make the booking
    """
```
An `ChatCompletionAgent` can be constructed by providing it information during construction. It need a `name`, `description` and a `kernel` object at a minimum

:warning: Note that using whitespace in the name of the agent, or leaving out the description will result in failure when using the agent.

The code for creating the agent should look like follows:
``` c#
ChatCompletionAgent transportationAgent =
    new()
    {
        Name = "TransportationAgent",
        Instructions = instructions,
        Description = "An agent that finds transportation options from hotel to concert location",
        Kernel = kernel,
    };
```
> If you want to get logging information wat the agent is doing, you can add your own logger factory to the agent. This is similar as to addign logging to the kernel object before. Note that providing a logger to the kernel, will not give you log information about what the agent is doing. It will only show information about the usage of the kernel object itself and calls to the LLM. Adding a logger is done by specifying the logger in the construction of the agent like this:
```c#
LoggerFactory = LoggerFactory.Create(builder =>
        {
            // Add Console logging provider
            builder.AddConsole().SetMinimumLevel(LogLevel.Trace);
        })
```

Now you have the agent object, which you can return to complete the function.

4. Create the agent and ask it to do something for you.

Now call the just created agent creation function and use this agent to ask it to do work for you. For this you can call the agent function `InvokeAsync`

Here you can immediately observe that the response of askign an agent to do somethign for you, is that you will get a response that contains much more information. The call will return an `IAsyncEnumerable<AgentResponseItem<ChatMessageContent>>` which is not a direct response with ChatMessageContent.

The `AgentResponseItem` contains for example information about the `AgentThread` that is created to facilitate an agent orchestration. We like to print this information, so we can see the results.

for this we create a print function that uses an aync foreach loop to print each item.

The function to print the result looks as follows:
``` c#
private static async Task PrintResult(IAsyncEnumerable<AgentResponseItem<ChatMessageContent>> agentResponse)
{
    await
    foreach (var item in agentResponse)
    {
        Console.WriteLine($"Thread: {item.Thread.Id}");
        Console.WriteLine($"Thread data: {item.Thread.ToString()}");
        Console.WriteLine($"Author: {item.Message.AuthorName}");
        Console.WriteLine($"Message:{item.Message}");
    }
}
```

Your call to the agent and printing the result should look as follows:

``` c#
var agentresult = agent.InvokeAsync("");
Console.WriteLine("******** RESPONSE ***********");
await PrintResult(agentresult);
```

The final step is to provide the right information so the agent can do actual work. So instead of the empty prompt, experiment with the information you rpovide to the agent. You can use for example: 
```c#
var information =
"""
I stay at the WestIn Seattle and the venue is the seattle cracken stadium.
the Concert starts at 7:30 pm and is November 20th this year. 
""";
```
Now change the call to `InvokeAsync("")` to use this information and run the program.

The result should be something similar to the folowing:
``` console
******** Create the kernel ***********
******** Create the agent ***********
******** Start the agent ***********
******** RESPONSE ***********
Thread: 9716e329c6a0438d89cacad2ce3230da
Thread data: Microsoft.SemanticKernel.Agents.ChatHistoryAgentThread
Author: TransportationAgent
Message:Thank you for providing the details! Let me identify the best transportation options for you. To ensure you'll arrive at the Seattle Kraken Stadium (Climate Pledge Arena) at least 30 minutes before the concert (so by 7:00 PM), I'll aim for an arrival time around 6:50 PM. Here are three options with different price ranges:

---

### Option 1: Budget-Friendly - Public Transit (King County Metro or Monorail)
- **Cost**: Approximately **$2.75 per person** (one-way King County Metro fare) or $3.75 for Downtown Monorail.
- **Details**: Take a Quick Bus on **Metro Route 8 or D Line** from near Westin or hop directly to Downton skywalk bridge monorail stops .It's an estimated 10-20 ride before nearing these location all upto weather .
```
