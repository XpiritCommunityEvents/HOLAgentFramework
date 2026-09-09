# Agent Framework assessment: module 04 demo 05

## Result

This demo is now a focused Microsoft Agent Framework 1.20 handoff example.

- Three clearly named agents demonstrate coordinator-to-specialist handoffs.
- Four explicit routes make the allowed conversation flow visible in code.
- Autonomous execution is bounded to eight turns and stops on `ITINERARY COMPLETE`.
- Small in-memory tools replace service classes and large domain/result models.
- One `ApprovalRequiredAIFunction` pauses the workflow before the combined itinerary is booked, demonstrating HITL without duplicating approval flows.
- The streaming event loop shows agent output, receives the approval request, and sends the user's decision back to the workflow.
- Configuration uses the existing `appsettings.json` keys and a single direct presence check.
- Checkpoints, restore/retry logic, custom monitors, fake clients, and in-app self-tests are intentionally absent from this attendee demo.

## Validation

- `dotnet build module-04-demo-05.csproj -c Debug -nologo` passed with 0 warnings and 0 errors.
- `dotnet build module-04-demo-05.csproj -c Release -nologo` passed with 0 warnings and 0 errors.
- No Semantic Kernel agent APIs remain in this project.
- Runtime execution still requires valid OpenAI-compatible endpoint credentials in user secrets.
