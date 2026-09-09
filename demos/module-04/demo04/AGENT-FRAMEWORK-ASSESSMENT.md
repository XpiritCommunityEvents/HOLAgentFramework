# Agent Framework assessment: module 04 demo 04

## Verdict

Migration status: **converted and simplified**. This sample now focuses on one lesson: a fixed, sequential Agent Framework workflow with human approval for side-effecting tools.

## What the demo teaches

- `AgentWorkflowBuilder.BuildSequential` connects the hotel and transportation agents in a clear, fixed order.
- `ApprovalRequiredAIFunction` turns each booking call into a human-in-the-loop request.
- `RequestInfoEvent` and `SendResponseAsync` pause and resume the workflow with the user's decision.
- `AgentResponseUpdateEvent` streams each agent's response, while `WorkflowOutputEvent` marks completion.

Checkpoint persistence, telemetry, recovery policies, self-tests, and handoff routing are intentionally outside this introductory sample. Demo 05 covers handoff orchestration.

## Validation

- `dotnet build module-04-demo-04.csproj -c Debug -nologo` — passed with 0 warnings and 0 errors
- `dotnet build module-04-demo-04.csproj -c Release -nologo` — passed with 0 warnings and 0 errors
- `appsettings.json` values preserved unchanged
