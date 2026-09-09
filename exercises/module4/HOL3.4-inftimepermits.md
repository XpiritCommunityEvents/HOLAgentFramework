# Optional checkpoint: Handoff workflow

This checkpoint replaces manual agent-to-agent coordination with the Microsoft Agent Framework 1.20 handoff workflow API.

- Project: [module-agent-completed4.csproj](./completed4.4itp/module-agent-completed4.csproj)
- Implementation: [ChatWithAgent.cs](./completed4.4itp/ChatWithAgent.cs)

## Exercise

1. Create concierge, hotel, and ride `AIAgent` instances. Give every specialist one clear responsibility.
2. Register lookup methods with `AIFunctionFactory.Create(...)`.
3. Wrap the side-effecting booking tool in `ApprovalRequiredAIFunction`.
4. Connect the agents with a handoff workflow:

   ```csharp
   Workflow workflow = AgentWorkflowBuilder
       .CreateHandoffBuilderWith(concierge)
       .WithHandoff(concierge, hotelAgent, "Choose the hotel first")
       .WithHandoff(hotelAgent, concierge, "Return the hotel choice")
       .WithHandoff(concierge, rideAgent, "Choose a ride after the hotel")
       .WithHandoff(rideAgent, concierge, "Return the ride choice")
       .WithAutonomousMode(turnLimit: 8)
       .WithTerminationCondition(messages =>
           messages.Any(message => message.Text?.Contains("ITINERARY COMPLETE") is true))
       .Build();
   ```

5. Open a streaming run, send the travel request, and watch `WorkflowEvent` values. When a `RequestInfoEvent` contains `ToolApprovalRequestContent`, ask the user and send the approval response back to the run.
6. Configure `OpenAI:Model`, `OpenAI:Endpoint`, and `OpenAI:ApiKey` as described in the [Module 4 instructions](./README.md), then run:

   ```powershell
   dotnet run --project exercises/module4/completed4.4itp/module-agent-completed4.csproj
   ```

Try both approving and rejecting the booking. In either case, the workflow should terminate within the configured turn limit.
