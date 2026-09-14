# Demo 01: Agent Harness

This sample shows how to use Agent Harness agents, which adds planning, todos, and tool approval.

In addition to retaining conversation history in one `AgentSession`, this sample adds two Harness features:

- a visible plan/execute mode and session-backed todo list;
- an approval request before the `book_ride` tool can run.

Hosted web search is disabled with `HarnessAgentOptions.DisableWebSearch = true`.
Harness enables it by default, which makes the OpenAI Chat Completions adapter
send `web_search_options`. Endpoints or models that do not support this option
reject the first prompt with `Unknown parameter: 'web_search_options'`.
This demo does not need web search; planning, todos, and ride approval remain enabled.

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

Try switching between `gpt-5.6-luna` and `gpt-5.6-terra` and observe the different planning and todo behavior. The Terra model is more advanced and more thorough.