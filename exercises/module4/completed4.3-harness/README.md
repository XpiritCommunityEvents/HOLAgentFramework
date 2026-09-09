# Module 4.3 follow-up: Agent Harness

This checkpoint takes one small step beyond `../completed4.3`: it keeps the
same multi-turn `AgentSession` and wraps the configured OpenAI agent with Agent
Harness.

The sample adds only two Harness features:

- a visible plan/execute mode and session-backed todo list;
- an approval request before the `book_ride` tool can run.

The console prints Harness state after each turn. Use `/plan`, `/execute`, and
`/todos` to inspect the behavior directly. Type `exit` to stop.

## Configure and run

Keep the API key out of `appsettings.json`:

```powershell
dotnet user-secrets set "OpenAI:Model" "<deployment-name>" --project exercises/module4/completed4.3-harness/module-agent-completed3-harness.csproj
dotnet user-secrets set "OpenAI:Endpoint" "https://<resource>.openai.azure.com/openai/v1" --project exercises/module4/completed4.3-harness/module-agent-completed3-harness.csproj
dotnet user-secrets set "OpenAI:ApiKey" "<api-key>" --project exercises/module4/completed4.3-harness/module-agent-completed3-harness.csproj
dotnet run --project exercises/module4/completed4.3-harness/module-agent-completed3-harness.csproj
```

Try asking the agent to plan a ride. Review the todos, switch with `/execute`,
then ask it to book the agreed ride. The console shows the tool arguments and
waits for `y` before continuing.
