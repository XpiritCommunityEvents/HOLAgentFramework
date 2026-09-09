# Demo 03: Agent Harness

This sample makes the small step from the multi-turn agent in `../demo03` to an
Agent Harness agent.

`demo03` retains conversation history in one `AgentSession`. This sample keeps
that same pattern and adds two Harness features:

- a visible plan/execute mode and session-backed todo list;
- an approval request before the `book_ride` tool can run.

The console prints Harness state after each turn. Use `/plan`, `/execute`, and
`/todos` to inspect the behavior directly. Type `exit` to stop.

## Configure and run

Keep the API key out of `appsettings.json`:

```powershell
dotnet user-secrets set "OpenAI:Model" "<deployment-name>" --project demos/module-04/demo03-harness/module-04-demo-03-harness.csproj
dotnet user-secrets set "OpenAI:Endpoint" "https://<resource>.openai.azure.com/openai/v1" --project demos/module-04/demo03-harness/module-04-demo-03-harness.csproj
dotnet user-secrets set "OpenAI:ApiKey" "<api-key>" --project demos/module-04/demo03-harness/module-04-demo-03-harness.csproj
dotnet run --project demos/module-04/demo03-harness/module-04-demo-03-harness.csproj
```

Try asking the agent to plan a ride. Review the todos, switch with `/execute`,
then ask it to book the agreed ride. The console will show the tool arguments
and wait for `y` before continuing.
