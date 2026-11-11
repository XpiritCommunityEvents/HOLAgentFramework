# Module 4 starter: Transportation agent

Start with the transportation agent and its ride lookup/booking function tools,
then follow the Wiki to add orchestration. This scaffold prints one response;
it does not yet include Harness planning, todos, or an enforced approval gate.

Use the existing Responses-backed `IChatClient` when extending the agent:
`GetResponsesClient().AsIChatClient(model)`. Do not revert to Chat Completions
or disable reasoning to make function tools work.

## Configure and run

Keep the API key out of `appsettings.json`:

```powershell
dotnet user-secrets set "OpenAI:Model" "<deployment-name>" --project exercises\module4\start\module-04-agentic.csproj
dotnet user-secrets set "OpenAI:Endpoint" "https://<resource>.services.ai.azure.com/" --project exercises\module4\start\module-04-agentic.csproj
dotnet user-secrets set "OpenAI:ApiKey" "<api-key>" --project exercises\module4\start\module-04-agentic.csproj
dotnet run --project exercises\module4\start\module-04-agentic.csproj
```

The endpoint is the Azure resource root; code appends `openai/v1/`.
See the [API conventions](../../../README.md#model-api-conventions).