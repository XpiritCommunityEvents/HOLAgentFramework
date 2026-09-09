# Agent Framework assessment: module 04 completed 4.3 Harness

## Status

This is the focused Agent Framework 1.20 Harness follow-up to `completed4.3`.

## What it demonstrates

- Creates a real OpenAI-backed `AIAgent` with `AsHarnessAgent`.
- Reuses one `AgentSession`, as `completed4.3` does.
- Shows the Harness-provided plan/execute mode and todo list after each turn.
- Lets the user change mode with `/plan` and `/execute`.
- Wraps one side-effecting tool in `ApprovalRequiredAIFunction` and resumes the
  same session with the user's approval or rejection.

Compared with `completed4.3`, Harness adds structured working state (mode and
todos) and a standard approval request/response protocol around tools.
Compaction, storage, telemetry, capability configuration, fake clients, and
self-test paths are intentionally omitted so those two additions remain easy
to see.

## Verification

- `dotnet build exercises/module4/completed4.3-harness/module-agent-completed3-harness.csproj --configuration Debug`
- `dotnet build exercises/module4/completed4.3-harness/module-agent-completed3-harness.csproj --configuration Release`

Both configurations build with zero warnings and zero errors.

Running the live conversation requires OpenAI configuration and an API key
stored outside source control as described in `README.md`.
